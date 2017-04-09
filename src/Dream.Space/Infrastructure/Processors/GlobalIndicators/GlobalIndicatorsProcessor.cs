using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dream.Space.Calculators.IndicatorProcessor;
using Dream.Space.Data.Entities.Indicators;
using Dream.Space.Data.Entities.Jobs;
using Dream.Space.Data.Extensions;
using Dream.Space.Data.Requests;
using Dream.Space.Data.Services;
using Dream.Space.Infrastructure.Loggers;
using Dream.Space.Models.Companies;
using Dream.Space.Models.Enums;
using Dream.Space.Models.Indicators;

namespace Dream.Space.Infrastructure.Processors.GlobalIndicators
{
    public class GlobalIndicatorsProcessor : IProcessor
    {
        #region Constructor

        private readonly GlobalIndicatorsProcessorConfig _config;
        private readonly IScheduledJobsService _jobsService;
        private readonly ICompanyService _companyService;
        private readonly IIndicatorService _indicatorService;
        private readonly CalculatorFactory _calculatorFactory;
        private readonly IGlobalIndicatorService _globalIndicatorService;
        private readonly IProcessorLogger _logger;
        public string Name => "Global Indicators Processor";

        public GlobalIndicatorsProcessor(
            GlobalIndicatorsProcessorConfig config, 
            IScheduledJobsService jobsService, 
            ICompanyService companyService,
            IIndicatorService indicatorService,
            CalculatorFactory calculatorFactory,
            IGlobalIndicatorService globalIndicatorService,
            IProcessorLogger logger)
        {
            _config = config;
            _jobsService = jobsService;
            _companyService = companyService;
            _indicatorService = indicatorService;
            _calculatorFactory = calculatorFactory;
            _globalIndicatorService = globalIndicatorService;
            _logger = logger;
        }


        #endregion


        public void Start(CancellationToken token)
        {
            Task.Run(async () =>
            {
                using (var waitHandle = token.WaitHandle)
                {
                    var interval = _config.Interval;
                    do
                    {
                        try
                        {
                            var job = await FindPendingJob();
                            if (job != null)
                            {
                                var total = await _companyService.GetSP500CountAsync();
                                var indicators = _indicatorService.GetGlobalIndicators();
                                var state = ProcessorState.InProgress;

                                while (state == ProcessorState.InProgress && !job.IsFinished())
                                {
                                    state = await Execute(job, indicators, total);
                                    job = await _jobsService.GetJobAsync(job.JobId);
                                }

                                if (state == ProcessorState.Completed)
                                {
                                    foreach (var indicator in indicators)
                                    {
                                        var indicatorResults = await GetIntermediateResults(job.JobId, indicator.IndicatorId);
                                        var calculator = _calculatorFactory.Create(indicator);
                                        indicatorResults = calculator.Combine(indicatorResults);

                                        await _globalIndicatorService.Save(new GlobalIndicator
                                        {
                                            SectorId = 0, 
                                            IndicatorId = indicator.IndicatorId,
                                            Values = indicatorResults,
                                            StartDate = indicatorResults.Last().Date,
                                            EndDate = indicatorResults.First().Date,
                                            CalculatedSuccessful = true,
                                            CompanyCount = total,
                                            LastCalculated = DateTime.UtcNow
                                        });

                                        await _jobsService.CompleteJobAsync(job.JobId);
                                        await ClearIntermediateResults(job.JobId, indicator.IndicatorId);

                                        _logger.Info(new ProcessorInfo
                                            {
                                                ProcessName = Name, JobId = job.JobId, JobType = ScheduledJobType.CalculateGlobalIndicators, JobState = JobStatus.Completed
                                            }, "Job completed successfully");
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(
                                new ProcessorInfo {ProcessName = Name},
                                $"Failed while executing: {Name}", ex);
                        }
                    } while (!waitHandle.WaitOne(interval));
                }
            }, token);
        }



        private async Task<ProcessorState> Execute(ScheduledJob job, List<Indicator> indicators, int total)
        {
            try
            {
                var companies = await FetchNextAsync(job.JobId);
                if (companies == null || !companies.Any())
                {
                    return ProcessorState.Completed;
                }

                var indicatorResults = await CalculateGlobalIndicators(companies, indicators, job.JobId);
                foreach (var result in indicatorResults)
                {
                    await _indicatorService.StoreIntermediateResultsAsync(job.JobId, result.Key, result.Value);
                }

                var tickers = companies.Select(c => c.Ticker).ToArray();
                await _jobsService.UpdateProgressAsync(job.JobId, tickers, total);

                _logger.Info(new ProcessorInfo
                {
                    JobId = job.JobId,
                    JobType = job.JobType,
                    JobState = JobStatus.Error,
                    ProcessName = Name
                }, $"Successfully processed companies: {string.Join(", ", tickers)}");
                return ProcessorState.InProgress;
                
            }
            catch (Exception exception)
            {
                _logger.Error(new ProcessorInfo
                {
                    JobId = job.JobId,
                    JobType = job.JobType,
                    JobState = JobStatus.Error,
                    ProcessName = Name
                }, $"Failed to execute {Name}. Reason: {exception.Message}", exception );
                return ProcessorState.Error;
            }
        }



        private async Task<Dictionary<int, List<IndicatorResult>>> CalculateGlobalIndicators(IList<CompanyQuotesModel> companies, List<Indicator> indicators, int jobId)
        {
            var result = new Dictionary<int, List<IndicatorResult>>();

            foreach (var indicator in indicators)
            {
                var calculator = _calculatorFactory.Create(indicator);
                var calculatorResult = new List<IndicatorResult>();

                foreach (var company in companies)
                {
                    var quotes = company.HistoryQuotes;
                    if (indicator.Period == QuotePeriod.Weekly)
                    {
                        quotes = quotes.ToWeeekly();
                    }

                    calculatorResult.AddRange(calculator.Calculate(indicator, quotes));
                }

                var indicatorResult = calculator.Merge(calculatorResult);
                var intermediateResult = await GetIntermediateResults(jobId, indicator.IndicatorId);
                if (intermediateResult != null && intermediateResult.Any())
                {
                    indicatorResult.AddRange(intermediateResult);
                    indicatorResult = calculator.Merge(indicatorResult);
                }

                result.Add(indicator.IndicatorId, indicatorResult);
            }

            return result;
        }



        #region Helper Methods

        public async Task<List<CompanyQuotesModel>> FetchNextAsync(int jobId)
        {
            return await _companyService.FindCompaniesForJob(new FindCompaniesForJobRequest
            {
                JobId = jobId.ToString(),
                MaxRecordCount = 10
            });
        }

        private async Task<List<IndicatorResult>> GetIntermediateResults(int jobId, int indicatorId)
        {
            return await _indicatorService.GetIntermediateResultsAsync(jobId, indicatorId);
        }

        private async Task ClearIntermediateResults(int jobId, int indicatorId)
        {
            await _indicatorService.ClearIntermediateResultsAsync(jobId, indicatorId);
        }


        private async Task<ScheduledJob> FindPendingJob()
        {
            return await _jobsService.FindPendingJobAsync(ScheduledJobType.CalculateGlobalIndicators);
        }


        #endregion

    }



}
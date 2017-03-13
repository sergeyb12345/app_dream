﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Dream.Space.Calculators.IndicatorProcessor;
using Dream.Space.Data.Extensions;
using Dream.Space.Data.Models;
using Dream.Space.Data.Requests;
using Dream.Space.Data.Services;
using Dream.Space.Import.CompanyImport;
using Dream.Space.Reader;
using Dream.Space.Reader.Models;
using Dream.Space.Stock;
using Dream.Space.Stock.Requests;

namespace Dream.Space.Import.QuotesImport
{
    public class QuotesImportJob : IQuotesImportJob
    {
        private readonly IMarketStockClient _marketStockClient;
        private readonly ICompanyService _companyService;
        private readonly IQuotesFileReader _quotesFileReader;
        private readonly IGlobalIndicatorProcessor _indicatorProcessor;

        public QuotesImportJob(IMarketStockClient marketStockClient,
            ICompanyService companyService,
            IQuotesFileReader quotesFileReader,
            IGlobalIndicatorProcessor indicatorProcessor)
        {
            _marketStockClient = marketStockClient;
            _companyService = companyService;
            _quotesFileReader = quotesFileReader;
            _indicatorProcessor = indicatorProcessor;
        }


        public void Start()
        {
            var log = new Logger();

            try
            {
                var findRequest = new FindCompaniesForUpdateRequest
                {
                    FromTimeAgo = new TimeSpan(1, 0, 0, 0),
                    MaxRecordCount = 10
                };

                var companies = _companyService.FindCompaniesForUpdate(findRequest);
                while (companies != null && companies.Any())
                {
                    foreach (var company in companies)
                    {
                        UpdateQuotes(company);
                    }
                    companies = _companyService.FindCompaniesForUpdate(findRequest);
                }
            }
            catch (Exception ex)
            {
                log.Error("FindCompaniesForUpdate", ex);
            }

        }

        private void UpdateQuotes(CompanyToUpdate company)
        {
            var log = new Logger();

            if (!company.HistoryQuotes.Any())
            {
                company.LastUpdated = DateTime.Today.AddYears(-10);
                company.HistoryQuotes = new List<QuotesModel>();
            }

            var historyRequest = new GetStockHistoryRequest(company.Ticker, company.LastUpdated);

            var quotes = new List<QuotesModel>();
            var errorMessage = string.Empty;

            try
            {
                var csvQuotes =
                    Task.Run(() => _marketStockClient.GetStockHistory(historyRequest)).Result;
                quotes = _quotesFileReader.Read(csvQuotes);
                quotes = quotes.Merge(company.HistoryQuotes);


            }
            catch (AggregateException ex)
            {
                log.Error($"Failed to read Company quotes: {company.Ticker}", ex);
                foreach (var exception in ex.InnerExceptions)
                {
                    errorMessage += exception.Message + " ";

                }
            }
            catch (Exception ex)
            {
                log.Error($"Failed to read Company quotes: {company.Ticker}", ex);
                errorMessage = ex.Message;
            }

            try
            {
                _companyService.UpdateQuotes(new UpdateQuotesRequest(company.Ticker, quotes)
                {
                    ErrorMessage = errorMessage
                });

                _indicatorProcessor.Calculate(quotes, company.Ticker);

                if (string.IsNullOrEmpty(errorMessage))
                {
                    
                    log.Info($"Company quotes updated successfully: {company.Ticker}");
                }
                else
                {
                    log.Error($"Failed to update Company: {company.Ticker}", new Exception(errorMessage));
                }

            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to update Company: {company.Ticker}";
                log.Error(errorMessage, ex);
            }

        }
    }

    public class Logger : ILogger
    {
        public void Info(string message)
        {
            Console.WriteLine(message);
        }

        public void Error(string message, Exception exception)
        {
            Console.WriteLine(message);
            Debug.WriteLine(exception.ToString());
        }
    }

    public interface ILogger
    {
        void Info(string message);
        void Error(string message, Exception exception);
    }

    public interface IQuotesImportJob : IJob
    {
        
    }
}

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using Dream.Space.Data.Repositories;
using Dream.Space.Models.Enums;
using Dream.Space.Data.Entities.Layouts;
using Dream.Space.Data.Requests;
using Dream.Space.Models.Layouts;

namespace Dream.Space.Data.Services
{
    public interface IChartLayoutService
    {
        Task<ChartLayoutModel> GetDefaultLayoutAsync(QuotePeriod period);
        Task<IList<ChartLayoutModel>> GetLayoutsForPeriodAsync(QuotePeriod period);
        Task<ChartLayoutModel> GetLayoutAsync(int layoutId);
        Task SaveLayoutAsync(ChartLayoutModel model);
        Task<ChartLayoutData> GetChartLayoutDataAsync(GetChartLayoutDataRequest request);

    }

    public class ChartLayoutService : IChartLayoutService
    {
        private readonly ILifetimeScope _container;

        public ChartLayoutService(ILifetimeScope container)
        {
            _container = container;
        }

        public async Task<IList<ChartLayoutModel>> GetLayoutsForPeriodAsync(QuotePeriod period)
        {
            var result = new List<ChartLayoutModel>();
            using (var scope = _container.BeginLifetimeScope())
            {
                var repository = scope.Resolve<IChartLayoutRepository>();
                var layouts = await repository.GetForPeriodAsync(period);

                if (layouts == null || !layouts.Any())
                {
                    return result;
                }

                result.AddRange(layouts.Select(layout => new ChartLayoutModel
                {
                    LayoutId = layout.LayoutId,
                    Deleted = layout.Deleted,
                    Title = layout.Title,
                    //Period = layout.Period,
                    Default = layout.Default,
                    Description = layout.Description
                }));

                foreach (var layout in result)
                {
                    layout.Plots = await GetChartPlotsForLayout(scope, layout.LayoutId);
                }
            }

            return result;
        }

        public async Task<ChartLayoutModel> GetDefaultLayoutAsync(QuotePeriod period)
        {
            var result = new ChartLayoutModel();

            using (var scope = _container.BeginLifetimeScope())
            {
                var repository = scope.Resolve<IChartLayoutRepository>();
                var layout = await repository.GetDefaultAsync(period);

                if (layout == null)
                {
                    return result;
                }

                result.LayoutId = layout.LayoutId;
                result.Deleted = layout.Deleted;
                result.Title = layout.Title;
                //result.Period = layout.Period;
                result.Default = layout.Default;
                result.Description = layout.Description;
                result.Plots = await GetChartPlotsForLayout(scope, result.LayoutId);
            }

            return result;
        }

        private async Task<IList<ChartPlotModel>> GetChartPlotsForLayout(ILifetimeScope scope, int layoutId)
        {
            var result = new List<ChartPlotModel>();

            var plotRepository = scope.Resolve<IChartPlotRepository>();
            var plots = await plotRepository.GetAllAsync(layoutId);

            var indicatorRepository = scope.Resolve<IIndicatorRepository>();
            var indicators = await indicatorRepository.GetIndicatorsForLayoutAsync(layoutId);

            var layoutRepository = scope.Resolve<IChartIndicatorRepository>();
            var layoutIndicators = await layoutRepository.GetForLayoutAsync(layoutId);

            var li = layoutIndicators
                    .Join(indicators,
                        l => l.IndicatorId,
                        i => i.IndicatorId,
                        (l, i) => new LayoutIndicatorModel()
                        {
                            Id = l.Id,
                            Indicator = new IndicatorModel(i),
                            IndicatorId = l.IndicatorId,
                            PlotId = l.PlotId,
                            Name = i.Description,
                            OrderId = l.OrderId,
                            LineColor = l.LineColor
                        }
                    ).ToList();

            foreach (var plot in plots)
            {

                var plotModel = new ChartPlotModel(plot)
                {
                    Indicators = li.Where(i => i.PlotId == plot.PlotId).ToList()
                };

                result.Add(plotModel);
            }

            return result;
        }

        public async Task<ChartLayoutModel> GetLayoutAsync(int layoutId)
        {
            var result = new ChartLayoutModel();

            using (var scope = _container.BeginLifetimeScope())
            {
                var repository = scope.Resolve<IChartLayoutRepository>();
                var layout = await repository.GetAsync(layoutId);

                if (layout == null)
                {
                    return result;
                }

                result.LayoutId = layout.LayoutId;
                result.Deleted = layout.Deleted;
                result.Title = layout.Title;
                //result.Period = layout.Period;
                result.Default = layout.Default;
                result.Description = layout.Description;
                result.Plots = await GetChartPlotsForLayout(scope, layoutId);
            }

            return result;
        }

        public async Task SaveLayoutAsync(ChartLayoutModel model)
        {
            using (var scope = _container.BeginLifetimeScope())
            {
                var repository = scope.Resolve<IChartLayoutRepository>();
                IChartLayoutEntity layout = null;

                if (model.LayoutId > 0)
                {
                    layout = await repository.GetAsync(model.LayoutId);
                } else
                {
                    layout = repository.Add(new ChartLayout());
                }

                if(layout != null)
                {
                    layout.Title = model.Title;
                    layout.Description = model.Description;
                    //layout.Period = model.Period;

                    repository.Commit();
        
                    if(model.LayoutId == 0)
                    {
                        model.LayoutId = layout.LayoutId;
                    }

                    if (model.Plots.Any())
                    {
                        var plotRepository = scope.Resolve<IChartPlotRepository>();
                        var indicatorRepository = scope.Resolve<IChartIndicatorRepository>();

                        foreach (var plot in model.Plots)
                        {
                            var entity = await plotRepository.GetAsync(plot.PlotId) ??
                                         plotRepository.Add(new ChartPlot
                                         {
                                             LayoutId = plot.LayoutId,
                                         });

                            entity.OrderId = model.Plots.IndexOf(plot);
                            entity.Height = plot.Height;
                            plotRepository.Commit();

                            if (plot.PlotId == 0)
                            {
                                plot.PlotId = entity.PlotId;
                            }

                            foreach (var indicator in plot.Indicators)
                            {
                                var record = await indicatorRepository.GetAsync(indicator.PlotId, indicator.IndicatorId) ??
                                             indicatorRepository.Add(new ChartIndicator
                                             {
                                                 IndicatorId = indicator.IndicatorId,
                                                 PlotId = indicator.PlotId
                                             });

                                record.OrderId = plot.Indicators.IndexOf(indicator);
                                record.LineColor = indicator.LineColor;

                                indicatorRepository.Commit();

                                if (indicator.Id == 0)
                                {
                                    indicator.Id = record.Id;
                                }
                            }
                        }

                    }

                }
            }
        }

        public async Task<ChartLayoutData> GetChartLayoutDataAsync(GetChartLayoutDataRequest request)
        {
            var layout = await GetLayoutAsync(request.LayoutId);
            using (var scope = _container.BeginLifetimeScope())
            {
                var repository = scope.Resolve<ICompanyRepository>();
                var company = repository.Get(request.Ticker);

                IList<ChartPlotData> plots = layout.Plots.Select(p => new ChartPlotData(p)).ToList();

                var periods = new List<ChartLayoutPeriodData>
                {
                    new ChartLayoutPeriodData
                    {
                        Period = layout.Period,
                        Plots = plots
                    }
                };
                var layoutData = new ChartLayoutData
                {
                    Company = new CompanyHeaderInfo {Ticker = company.Ticker, Name = company.Name},
                    Periods = periods
                };

                return layoutData;
            }
        }
    }
}

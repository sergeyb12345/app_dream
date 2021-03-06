﻿using System.Collections.Generic;
using System.Linq;
using Dream.Space.Indicators.Enums;
using Dream.Space.Indicators.Extensions;
using Dream.Space.Indicators.IndicatorParams;
using Dream.Space.Models.Indicators;
using Dream.Space.Models.Quotes;
using Dream.Space.Reader.Models;

namespace Dream.Space.Indicators
{
    /*
        For any given stock or underlying security:

        1. Calculate a 12 day EMA of closing prices
        2. Calculate a 26 day EMA of closing prices
        3. Subtract the longer EMA in (2) from the shorter EMA in (1)
        4. Calculate a 9 day EMA of the MACD line gotten in (3)
    */

    public class MACD : IIndicator<IndicatorResult, MacdParams>
    {
        public string Name => "MACD";

        public List<IndicatorResult> Calculate(List<QuotesModel> quotes, MacdParams inputParams)
        {
            if (!Validate(quotes, inputParams))
            {
                return null;
            }

            var slowEma = new EMA().Calculate(quotes, inputParams.SlowEmaPeriod);
            var fastEma = new EMA().Calculate(quotes, inputParams.FastEmaPeriod);
            var macdLine = fastEma.Substract(slowEma);
            var signalEma = new EMA().Calculate(macdLine.AsQuotesModel(QuoteModelField.Close), inputParams.SignalEmaPeriod);
            var macdHist = macdLine.Substract(signalEma);

            var result = macdHist.Select(c => new IndicatorResult(c.Date) { Value = c.Value}).ToList();
            
            return result.OrderByDescending(r => r.Date).ToList(); 
        }

        private bool Validate(List<QuotesModel> quotes, MacdParams inputParams)
        {
            if (inputParams.SlowEmaPeriod < 12 || 
                inputParams.FastEmaPeriod < 9 ||
                inputParams.SlowEmaPeriod <= inputParams.FastEmaPeriod ||
                inputParams.SlowEmaPeriod <= inputParams.SignalEmaPeriod ||
                inputParams.FastEmaPeriod <= inputParams.SignalEmaPeriod ||
                quotes == null || 
                quotes.Count <= inputParams.SlowEmaPeriod)
            {
                return false;
            }
            return true;
        }
    }

}

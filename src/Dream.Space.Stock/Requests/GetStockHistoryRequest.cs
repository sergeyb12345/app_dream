using System;
using Dream.Space.Stock.Enums;

namespace Dream.Space.Stock.Requests
{
    public class GetStockHistoryRequest
    {

        public GetStockHistoryRequest() { }

        public GetStockHistoryRequest(string ticker)
        {
            Ticker = ticker;
            TimeFrame = QuoteTimeFrame.Year;
            TimeFrameValue = 10;
            FromDate = DateTime.Today.AddYears(-10);
        }

        public GetStockHistoryRequest(string ticker, DateTime lastUpdated)
        {
            Ticker = ticker;

            FromDate = lastUpdated;

            if (FromDate.AddMonths(1) > DateTime.Today)
            {
                FromDate = DateTime.Today.AddMonths(-1);
            }

            var months = 0;
            var years = 1;

            while (lastUpdated < DateTime.Today)
            {
                months += 1;
                if (months%12 == 0)
                {
                    years += 1;
                }
                lastUpdated = lastUpdated.AddMonths(1);
            }

            if (years > 1)
            {
                TimeFrame = QuoteTimeFrame.Year;
                TimeFrameValue = years;
            }
            else
            {
                TimeFrame = QuoteTimeFrame.Month;
                TimeFrameValue = months;
            }
        }


        public QuoteTimeFrame TimeFrame { get; set; }
        public int TimeFrameValue { get; set; }
        public string Ticker { get; set; }
        public DateTime FromDate { get; set; }
    }
}
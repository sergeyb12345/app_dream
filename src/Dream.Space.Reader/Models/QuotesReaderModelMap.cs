﻿using System;
using CsvHelper.Configuration;
using Dream.Space.Models.Quotes;

namespace Dream.Space.Reader.Models
{
    public class QuotesReaderModelMap : CsvClassMap<StockQuote>
    {
        public QuotesReaderModelMap()
        {
            Map(m => m.Date).Name("date").Default(DateTime.MinValue);
            Map(m => m.Close).Name("close").Default(0);
            Map(m => m.VolumeAsText).Name("volume").Default("0");
            Map(m => m.Open).Name("open").Default(0);
            Map(m => m.High).Name("high").Default(0);
            Map(m => m.Low).Name("low").Default(0);
        }
    }
}
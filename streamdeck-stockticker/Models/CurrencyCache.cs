using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace StockTicker.Models
{
    internal class CurrencyCache
    {
        public DateTime LastRefresh { get; private set; }

        public string Symbol { get; private set; }
        public float Value { get; private set; }

        public CurrencyCache(DateTime lastRefresh, string symbol, float value)
        {
            LastRefresh = lastRefresh;
            Symbol = symbol;
            Value = value;
        }
    }
}

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace StockTicker.Wrappers
{
    internal class CurrencyCache
    {
        public DateTime LastRefresh { get; private set; }

        public JObject CurrencyData { get; private set; }

        public CurrencyCache(DateTime lastRefresh, JObject currencyData)
        {
            LastRefresh = lastRefresh;
            CurrencyData = currencyData;
        }
    }
}

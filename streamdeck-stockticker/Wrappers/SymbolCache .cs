using Newtonsoft.Json.Linq;
using StockTicker.Backend;
using System;
using System.Collections.Generic;
using System.Text;

namespace StockTicker.Wrappers
{
    internal class SymbolCache
    {
        public DateTime LastRefresh { get; private set; }

        public SymbolData SymbolData { get; private set; }

        public SymbolCache(DateTime lastRefresh, SymbolData symbolData)
        {
            LastRefresh = lastRefresh;
            SymbolData = symbolData;
        }
    }
}

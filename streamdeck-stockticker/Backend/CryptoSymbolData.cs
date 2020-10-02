using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace StockTicker.Backend
{
    public class CryptoSymbolData
    {
        [JsonProperty(PropertyName = "symbol")]
        public string SymbolName { get; set; }

        [JsonProperty(PropertyName = "price")]
        public double Price { get; set; }
    }
}

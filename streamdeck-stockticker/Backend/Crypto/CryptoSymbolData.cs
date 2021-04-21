using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace StockTicker.Backend.Crypto
{
    public class CryptoSymbolData
    {
        [JsonProperty(PropertyName = "symbol")]
        public string SymbolName { get; set; }

        [JsonProperty(PropertyName = "price")]
        public double Price { get; set; }
    }
}

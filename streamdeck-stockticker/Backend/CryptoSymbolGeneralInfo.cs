using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace StockTicker.Backend
{
    public class CryptoSymbolGeneralInfo
    {
        [JsonProperty(PropertyName = "baseAsset")]
        public string BaseCurrency { get; set; }

        [JsonProperty(PropertyName = "quoteAsset")]
        public string Quote { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonIgnore]
        public string SymbolName
        {
            get
            {
                return $"{BaseCurrency}{Quote}";
            }
        }
    }
}

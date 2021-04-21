using Newtonsoft.Json;
using System;

namespace StockTicker.Backend.Stocks
{
    [Serializable]
    internal class APIToken
    {
        [JsonProperty(PropertyName = "stockToken")]
        public string StockToken { get; set; }

        [JsonProperty(PropertyName = "currencyToken")]
        public string CurrencyToken { get; set; }

        [JsonIgnore]
        public DateTime TokenLastRefresh { get; set; }
    }
}

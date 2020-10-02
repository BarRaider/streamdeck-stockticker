using Newtonsoft.Json;
using System;

namespace StockTicker.Backend
{
    public class StockQuote
    {
        [JsonProperty(PropertyName = "symbol")]
        public string Symbol { get; set; }

        [JsonProperty(PropertyName = "close")]
        public double? Close { get; set; }

        [JsonProperty(PropertyName = "high")]
        public double? High { get; set; }

        [JsonProperty(PropertyName = "low")]
        public double? Low { get; set; }

        [JsonProperty(PropertyName = "latestPrice")]
        public double? LatestPrice { get; set; }

        [JsonProperty(PropertyName = "latestSource")]
        public string LatestSource { get; set; }

        [JsonProperty(PropertyName = "change")]
        public double? Change { get; set; }

        [JsonProperty(PropertyName = "changePercent")]
        public double? ChangePercent { get; set; }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.StockTicker
{
    public class StockQuote
    {
        [JsonProperty(PropertyName = "symbol")]
        public string Symbol { get; set; }

        [JsonProperty(PropertyName = "companyName")]
        public string CompanyName { get; set; }

        [JsonProperty(PropertyName = "calculationPrice")]
        public string CalculationPrice { get; set; }

        [JsonProperty(PropertyName = "open")]
        public double Open { get; set; }

        [JsonProperty(PropertyName = "openTime")]
        public long OpenTimeEpoch { get; set; }

        [JsonProperty(PropertyName = "close")]
        public double Close { get; set; }

        [JsonProperty(PropertyName = "closeTime")]
        public long CloseTimeEpoch { get; set; }

        [JsonProperty(PropertyName = "high")]
        public double High { get; set; }

        [JsonProperty(PropertyName = "low")]
        public double Low { get; set; }

        [JsonProperty(PropertyName = "latestPrice")]
        public double LatestPrice { get; set; }

        [JsonProperty(PropertyName = "latestSource")]
        public string LatestSource { get; set; }

        [JsonProperty(PropertyName = "latestTime")]
        public DateTime LatestTime { get; set; }

        [JsonProperty(PropertyName = "latestUpdate")]
        public long LatestUpdateEpoch { get; set; }

        [JsonProperty(PropertyName = "latestVolume")]
        public long LatestVolume { get; set; }

        [JsonProperty(PropertyName = "change")]
        public double Change { get; set; }

        [JsonProperty(PropertyName = "changePercent")]
        public double ChangePercent { get; set; }

        [JsonProperty(PropertyName = "marketCap")]
        public long MarketCap { get; set; }

        [JsonProperty(PropertyName = "week52High")]
        public double Week52High { get; set; }

        [JsonProperty(PropertyName = "week52Low")]
        public double Week52Low { get; set; }

        [JsonProperty(PropertyName = "ytdChange")]
        public double YtdChange { get; set; }
    }
}

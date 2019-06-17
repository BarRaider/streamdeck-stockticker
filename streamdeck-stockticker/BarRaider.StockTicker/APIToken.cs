using Newtonsoft.Json;
using System;

namespace BarRaider.StockTicker
{
    [Serializable]
    internal class APIToken
    {
        [JsonProperty(PropertyName = "stockToken")]
        public string StockToken { get; set; }

        [JsonIgnore]
        public DateTime TokenLastRefresh { get; set; }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace StockTicker.Backend
{
    public class TickerGlobalSettings
    {
        [JsonProperty(PropertyName = "useUSEndpoint")]
        public bool UseUSEndpoint { get; set; }
    }
}

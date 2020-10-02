using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace StockTicker.Wrappers
{
    public class Currency
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}

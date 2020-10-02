using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace StockTicker.Wrappers
{
    public class Quote
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}

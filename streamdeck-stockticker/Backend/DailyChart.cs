using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTicker.Backend
{
    public class DailyChart : ChartBase
    {
        [JsonProperty(PropertyName = "close")]
        public override double Value { get; set; }

        [JsonProperty(PropertyName = "date")]
        public override DateTime Date { get; set; }
    }
}

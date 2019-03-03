﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.StockTicker
{
    public class MinuteChart : ChartBase
    {
        [JsonProperty(PropertyName = "average")]
        public override double Value { get; set; }

        [JsonProperty(PropertyName = "date")]
        [JsonConverter(typeof(CustomDateConverter))]
        public DateTime DateOnly { get; set; }

        [JsonProperty(PropertyName = "minute")]
        public DateTime TimeOnly { get; set; }

        public override DateTime Date
        {
            get
            {
                return Convert.ToDateTime($"{DateOnly.ToString("yyyy-MM-dd")} {TimeOnly.ToString("HH:mm")}");
            }

            set
            {

            }
        }

    }
}

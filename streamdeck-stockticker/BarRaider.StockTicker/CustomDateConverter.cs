using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.StockTicker
{
    class CustomDateConverter : IsoDateTimeConverter
    {
        public CustomDateConverter()
        {
            base.DateTimeFormat = "yyyyMMdd";
        }
    }
}

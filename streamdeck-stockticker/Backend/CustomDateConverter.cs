using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockTicker.Backend
{
    class CustomDateConverter : IsoDateTimeConverter
    {
        public CustomDateConverter()
        {
            base.DateTimeFormat = "yyyyMMdd";
        }
    }
}

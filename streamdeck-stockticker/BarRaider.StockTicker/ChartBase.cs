using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarRaider.StockTicker
{
    public abstract class ChartBase
    {
        public abstract double Value  { get; set; }
        public abstract DateTime Date { get; set;  }
    }
}

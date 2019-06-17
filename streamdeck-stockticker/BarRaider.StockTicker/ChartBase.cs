using System;

namespace BarRaider.StockTicker
{
    public abstract class ChartBase
    {
        public abstract double Value  { get; set; }
        public abstract DateTime Date { get; set;  }
    }
}

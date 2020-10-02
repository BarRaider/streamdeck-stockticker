using System;

namespace StockTicker.Backend
{
    public abstract class ChartBase
    {
        public abstract double Value  { get; set; }
        public abstract DateTime Date { get; set;  }
    }
}

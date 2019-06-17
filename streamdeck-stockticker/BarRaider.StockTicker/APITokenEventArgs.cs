using System;
using System.Collections.Generic;
using System.Text;

namespace BarRaider.StockTicker
{
    internal class APITokenEventArgs : EventArgs
    {
        public bool TokenExists { get; private set; }

        public APITokenEventArgs(bool tokenExists)
        {
            TokenExists = tokenExists;
        }
    }
}

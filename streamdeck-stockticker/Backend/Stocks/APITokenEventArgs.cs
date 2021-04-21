using System;
using System.Collections.Generic;
using System.Text;

namespace StockTicker.Backend.Stocks
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

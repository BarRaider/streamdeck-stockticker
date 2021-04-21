using System;
using System.Collections.Generic;
using System.Text;

namespace StockTicker.Backend.Crypto
{
    public class CryptoCurrencyUpdatedEventArgs : EventArgs
    {
        public List<CryptoSymbolData> Symbols;

        public CryptoCurrencyUpdatedEventArgs(List<CryptoSymbolData> symbols)
        {
            Symbols = symbols;
        }
    }
}

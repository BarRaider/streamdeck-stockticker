using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace StockTicker.Backend
{
    internal interface IStockInfoProvider
    {
        bool TokenExists();
        Task<SymbolData> GetSymbol(string stockSymbol, int cooldownTimeMs);

        void SetStockToken(string token);
    }
}

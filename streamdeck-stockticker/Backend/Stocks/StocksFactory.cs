using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.Text;

namespace StockTicker.Backend.Stocks
{
    internal static class StocksFactory
    {
        public static IStockInfoProvider Build(StockProviders provider)
        {
            switch (provider)
            {
                case StockProviders.YAHOO_V6:
                    return YahooV6StockProvider.Instance;
                case StockProviders.YAHOO_V11:
                    return YahooV11StockProvider.Instance;
                case StockProviders.IEXAPIS:
                    return EXCloudStockProvider.Instance;

                default:
                    Logger.Instance.LogMessage(TracingLevel.ERROR, $"StocksFactory.Build() Invalid Provider {provider}");
                    return null;

            }
        }
    }
}

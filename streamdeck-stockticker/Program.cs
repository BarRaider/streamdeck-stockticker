using BarRaider.SdTools;
using System.Collections.Generic;

namespace StockTicker
{
    class Program
    {
        static void Main(string[] args)
        {
            // Uncomment this line of code to allow for debugging
            //while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }

            List<PluginActionId> supportedActionIds = new List<PluginActionId>();
            supportedActionIds.Add(new PluginActionId("com.barraider.stockticker", typeof(SymbolTicker)));

            SDWrapper.Run(args, supportedActionIds.ToArray());
        }
    }
}

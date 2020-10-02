using BarRaider.SdTools;
using StockTicker.Backend;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;

namespace StockTicker.Actions
{
    [PluginActionId("com.barraider.tarkovticker")]
    public class TarkovTickerAction : CryptoTickerAction
    {
        #region Private Members
      
        private const double TARKOV_MULTIPLIER = 0.2;
        private const string DEFAULT_IMAGE_LOCATION = @"images\tarkov.png";

        #endregion

        public TarkovTickerAction(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {
            settings.BaseCurrency = "BTC";
            settings.Quote = "RUB";
            defaultImageLocation = DEFAULT_IMAGE_LOCATION;
            Connection.SetSettingsAsync(JObject.FromObject(settings));
            InitializeSettings();
        }

        public override void Dispose()
        {
            CryptoComm.Instance.CryptoCurrencyUpdated -= Instance_CryptoCurrencyUpdated;
            Logger.Instance.LogMessage(TracingLevel.INFO, $"{this.GetType()} Destructor called");
        }

        public override void KeyPressed(KeyPayload payload) { }

        public override void KeyReleased(KeyPayload payload) { }

        public async override void OnTick()
        {
            if (symbolData == null)
            {
                return;
            }

            await DrawTarkovData(symbolData.Price);
        }

        private void Instance_CryptoCurrencyUpdated(object sender, CryptoCurrencyUpdatedEventArgs e)
        {
            symbolData = e.Symbols.FirstOrDefault(x => x.SymbolName == CryptoSymbol);
        }

        private async Task DrawTarkovData(double currency)
        {
            currency *= TARKOV_MULTIPLIER;
            await DrawCurrencyData(currency, TARKOV_MULTIPLIER);
        }
    }
}

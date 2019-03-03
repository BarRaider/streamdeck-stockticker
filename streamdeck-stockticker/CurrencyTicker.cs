using BarRaider.SdTools;
using BarRaider.StockTicker;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace StockTicker
{
    [PluginActionId("com.barraider.currencyexchange")]
    public class CurrencyTicker : PluginBase
    {
        private const int DEFAULT_REFRESH_TIME = 60;

        #region Settings

        private class PluginSettings
        {
            public static PluginSettings CreateDefaultSettings()
            {
                PluginSettings instance = new PluginSettings();
                instance.BaseCurrency = "USD";
                instance.Symbol = "EUR";
                instance.ForegroundColor = "#ffffff";
                instance.BackgroundColor = "#000000";

                return instance;
            }

            [JsonProperty(PropertyName = "baseCurrency")]
            public string BaseCurrency { get; set; }

            [JsonProperty(PropertyName = "symbol")]
            public string Symbol { get; set; }

            [JsonProperty(PropertyName = "foregroundColor")]
            public string ForegroundColor { get; set; }

            [JsonProperty(PropertyName = "backgroundColor")]
            public string BackgroundColor { get; set; }
        }

        #endregion

        private PluginSettings settings;
        private DateTime lastRefresh;
        private StockComm stockComm = new StockComm();

        public CurrencyTicker(SDConnection connection, InitialPayload payload) : base(connection, payload)
        {

            if (payload.Settings == null || payload.Settings.Count == 0) // Called the first time you drop a new action into the Stream Deck
            {
                this.settings = PluginSettings.CreateDefaultSettings();
                Connection.SetSettingsAsync(JObject.FromObject(settings));
            }
            else
            {
                this.settings = payload.Settings.ToObject<PluginSettings>();
            }
        }

        public override void Dispose()
        {
        }

        public override void KeyPressed(KeyPayload payload)
        {
        }

        public override void KeyReleased(KeyPayload payload)
        {
        }

        public async override void OnTick()
        {
            try
            {
                if ((DateTime.Now - lastRefresh).TotalSeconds >= DEFAULT_REFRESH_TIME)
                {
                    JObject obj = await stockComm.FetchCurrencyData(settings.BaseCurrency, settings.Symbol);
                    var token = obj["rates"];
                    var value = token[settings.Symbol];

                    await DrawCurrencyData(Convert.ToDouble(value.ToString()));
                    lastRefresh = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"OnTick error: {ex}");
            }
        }

        public override void ReceivedSettings(ReceivedSettingsPayload payload)
        {
            Tools.AutoPopulateSettings(settings, payload.Settings);
            lastRefresh = DateTime.MinValue;
        }

        public override void ReceivedGlobalSettings(ReceivedGlobalSettingsPayload payload)
        {
        }

        private async Task DrawCurrencyData(double currency)
        {
            try
            {
                Graphics graphics;
                Bitmap bmp = Tools.GenerateKeyImage(out graphics);

                SizeF stringSize;
                float stringPos;
                var fontDefault = new Font("Verdana", 10, FontStyle.Bold);
                var fontCurrency = new Font("Verdana", 12, FontStyle.Bold);

                // Background
                var bgBrush = new SolidBrush(ColorTranslator.FromHtml(settings.BackgroundColor));
                var fgBrush = new SolidBrush(ColorTranslator.FromHtml(settings.ForegroundColor));
                graphics.FillRectangle(bgBrush, 0, 0, Tools.KEY_DEFAULT_WIDTH, Tools.KEY_DEFAULT_HEIGHT);

                // Top title
                string title = $"1 {settings.BaseCurrency}:";
                stringSize = graphics.MeasureString(title, fontDefault);
                stringPos = Math.Abs((Tools.KEY_DEFAULT_WIDTH - stringSize.Width)) / 2;
                graphics.DrawString(title, fontDefault, fgBrush, new PointF(stringPos, 5));

                stringSize = graphics.MeasureString(currency.ToString("0.00"), fontCurrency);
                stringPos = Math.Abs((Tools.KEY_DEFAULT_WIDTH - stringSize.Width)) / 2;
                graphics.DrawString(currency.ToString("0.00"), fontCurrency, fgBrush, new PointF(stringPos, 25));

                stringSize = graphics.MeasureString(settings.Symbol, fontCurrency);
                stringPos = Math.Abs((Tools.KEY_DEFAULT_WIDTH - stringSize.Width)) / 2;
                graphics.DrawString(settings.Symbol, fontCurrency, fgBrush, new PointF(stringPos, 50));
                await Connection.SetImageAsync(bmp);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Error drawing currency data {ex}");
            }
        }
    }
}

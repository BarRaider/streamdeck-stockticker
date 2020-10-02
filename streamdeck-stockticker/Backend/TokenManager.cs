using BarRaider.SdTools;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace StockTicker.Backend
{
    internal class TokenManager
    {
        #region Private Members
        private const string TOKEN_FILE = "stock.dat";

        private static TokenManager instance = null;
        private static readonly object objLock = new object();

        private APIToken token;
        private bool failedToken = false;

        #endregion

        #region Public Members

        public event EventHandler<APITokenEventArgs> TokensChanged;
        #endregion

        #region Constructors

        public static TokenManager Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                lock (objLock)
                {
                    if (instance == null)
                    {
                        instance = new TokenManager();
                    }
                    return instance;
                }
            }
        }

        private TokenManager()
        {
            LoadToken();
        }

        #endregion

        #region Public Methods

        public bool TokenExists
        {
            get
            {
                return (token != null && !string.IsNullOrWhiteSpace(token.StockToken) && !failedToken);
            }
        }

        internal APIToken Token
        {
            get
            {
                if (!TokenExists)
                {
                    return null;
                }
                return new APIToken() { StockToken = token.StockToken, TokenLastRefresh = token.TokenLastRefresh};
            }
        }

        internal void InitTokens(string stockToken, DateTime tokenCreateDate)
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, "InitTokens called");
            if (token == null || token.TokenLastRefresh < tokenCreateDate)
            {
                if (String.IsNullOrWhiteSpace(stockToken))
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, "InitTokens: Token revocation!");
                }
                token = new APIToken() { StockToken = stockToken, TokenLastRefresh = tokenCreateDate };
                failedToken = false;
                SaveToken();
                Logger.Instance.LogMessage(TracingLevel.INFO, $"New token saved: {stockToken}");
            }
            TokensChanged?.Invoke(this, new APITokenEventArgs(TokenExists));
        }

        internal void SetTokenFailed()
        {
            Logger.Instance.LogMessage(TracingLevel.INFO, $"SetTokenFailed Called!");
            failedToken = true;
        }

        #endregion

        #region Private Methods

        private void LoadToken()
        {
            try
            {
                string fileName = Path.Combine(System.AppContext.BaseDirectory, TOKEN_FILE);
                if (File.Exists(fileName))
                {
                    using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                    {
                        var formatter = new BinaryFormatter();
                        token = (APIToken)formatter.Deserialize(stream);
                        if (token == null)
                        {
                            Logger.Instance.LogMessage(TracingLevel.ERROR, "Failed to load tokens, deserialized token is null");
                            return;
                        }
                        Logger.Instance.LogMessage(TracingLevel.INFO, $"Token initialized. Last refresh date was: {token.TokenLastRefresh}");
                    }
                }
                else
                {
                    Logger.Instance.LogMessage(TracingLevel.WARN, $"Failed to load tokens, token file does not exist: {fileName}");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Exception loading tokens: {ex}");
            }
        }

        private void SaveToken()
        {
            try
            {
                var formatter = new BinaryFormatter();
                using (var stream = new FileStream(Path.Combine(System.AppContext.BaseDirectory, TOKEN_FILE), FileMode.Create, FileAccess.Write))
                {

                    formatter.Serialize(stream, token);
                    stream.Close();
                    Logger.Instance.LogMessage(TracingLevel.INFO, $"New token saved. Last refresh date was: {token.TokenLastRefresh}");
                }
            }
            catch (Exception ex)
            {
                Logger.Instance.LogMessage(TracingLevel.ERROR, $"Exception saving tokens: {ex}");
            }
        }

        #endregion
    }
}

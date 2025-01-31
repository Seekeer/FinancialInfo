namespace FinancialInfo
{
    public class AppConfig
    {
        public const string APP = "App";
        public TiingoConfig Tiingo { get; set; }
    }

    public class TiingoConfig
    {
        public string TiingoAPIKey { get; set; }
        public TiingoEndpointConfig TiingoCryptoConfig { get; set; }
        public TiingoEndpointConfig TiingoForexConfig { get; set; }

    }
    public class TiingoEndpointConfig
    {
        public string APIUrl { get; set; }
        public string[] Instruments { get; set; }

    }
}

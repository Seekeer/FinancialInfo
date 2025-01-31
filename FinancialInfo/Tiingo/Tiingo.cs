using System.Text.Json;
using System.Text.Json.Serialization;

namespace FinancialInfo.Tiingo
{

    [JsonConverter(typeof(JsonStringEnumConverter<TiingoWebSocketMethod>))]
    public enum TiingoWebSocketMethod
    {
        subscribe,
        unsubscribe
    }

    public class TiingoAPIData
    {
        [JsonPropertyName("eventName")]
        public TiingoWebSocketMethod Method { get; set; }
        [JsonPropertyName("authorization")]
        public string API_KEY { get; set; } 
        [JsonPropertyName("eventData")]
        public TiingoRequestData Data { get; set; } 
    }

    public class TiingoRequestData
    {
        [JsonPropertyName("thresholdLevel")]
        public int Level { get; set; } = 5;
        [JsonPropertyName("tickers")]
        public IEnumerable<string> Tickers { get; set; } 
    }

    public class TiingoPriceUpdate
    {
        [JsonPropertyName("data")]
        public JsonElement[] Data { get; set; }

        // https://www.tiingo.com/documentation/websockets/forex Section: Top-of-Book (Quote) Update Messages.
        // Index 5 - Mid Price
        public decimal GetPrice()
        {
            return Data[5].GetDecimal();
        }
        // Index 1 - Ticker
        public string GetSymbol()
        {
            return Data[1].ToString().ToUpper();
        }
    }
}
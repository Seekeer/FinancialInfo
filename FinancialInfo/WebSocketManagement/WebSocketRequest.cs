using System.Text.Json.Serialization;

namespace FinancialInfo.WebSocketManagement
{
    [JsonConverter(typeof(JsonStringEnumConverter<WebSocketMethod>))]
    public enum WebSocketMethod
    {
        SUBSCRIBE,
        UNSUBSCRIBE
    }

    public class WebSocketRequest
    {
        public WebSocketMethod? Method { get; set; }
        public string[] InstrumentNames { get; set; } = [];
        public int Id { get; set; }
    }

    public class WebSocketResult
    {
        public string Error { get; set; }
        public int Id { get; set; }
    }
}
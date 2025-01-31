using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using System.Text;
using FinancialInfo.FinancialData;

namespace FinancialInfo.WebSocketManagement
{
    public interface IUpdateMessagerService
    {
        Task ProcessWebSocket(WebSocket webSocket);
    }

    public class UpdateMessagerService : IUpdateMessagerService, IAsyncDisposable
    {
        public const int WEB_SOCKET_BUFFER_SIZE = 1024;

        private readonly ILogger<UpdateMessagerService> _logger;
        private static Dictionary<string, ConcurrentDictionary<WebSocket, bool>> _clients = new Dictionary<string, ConcurrentDictionary<WebSocket, bool>>();

        public UpdateMessagerService(FinancialDataRepo financialDataRepo, ILogger<UpdateMessagerService> logger)
        {
            _logger = logger;
            // When price updated - notify all subscribers.
            financialDataRepo.FinanceDataUpdated += FinancialDataRepo_FinanceDataUpdated;

            foreach (var instrument in financialDataRepo.GetAllInstruments())
                _clients.TryAdd(instrument, new ConcurrentDictionary<WebSocket, bool>());
        }

        private async Task FinancialDataRepo_FinanceDataUpdated(object sender, FinanceDataEventArgs data)
        {
            var dataStr = JsonSerializer.Serialize(data);
            byte[] buffer = Encoding.UTF8.GetBytes(dataStr);
            var segment = new ArraySegment<byte>(buffer);

            await Task.Delay(5000).ContinueWith(t => Console.WriteLine($"Price updated:{data.InstrumentName}"));

            //Thread.Sleep(5000);
            //Console.WriteLine($"Price updated:{data.InstrumentName}");

            //foreach (var webSocket in _clients[data.InstrumentName].Keys)
            //    await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async Task ProcessWebSocket(WebSocket webSocket)
        {
            try
            {
                var buffer = new byte[WEB_SOCKET_BUFFER_SIZE];
                WebSocketReceiveResult? receiveResult = null;

                do
                {
                    var request = new MemoryStream();

                    do
                    {
                        receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        request.Write(buffer, 0, receiveResult.Count);
                    }
                    while (!receiveResult.EndOfMessage);

                    await ProcessData(request, webSocket);
                }
                while (!receiveResult.CloseStatus.HasValue);

                await webSocket.CloseAsync(
                    receiveResult.CloseStatus.Value,
                    receiveResult.CloseStatusDescription,
                    CancellationToken.None);
            }
            catch (WebSocketException ex)
            {
                _logger.LogError(ex, "WebSocket error");

                if (webSocket.State == WebSocketState.Open)
                    await webSocket.CloseAsync(WebSocketCloseStatus.InternalServerError, ex.Message, CancellationToken.None);
            }

            foreach (var dict in _clients.Values.Select(x => x))
                dict.Remove(webSocket, out _);
        }

        private async Task ProcessData(MemoryStream stream, WebSocket webSocket)
        {
            try
            {
                stream.Position = 0;
                using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                string dataStr = reader.ReadToEnd();

                if (string.IsNullOrEmpty(dataStr))
                    return;

                WebSocketRequest? data = null;
                try
                {
                    data = JsonSerializer.Deserialize<WebSocketRequest>(dataStr);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, $"Wrong user request:{dataStr}");
                }

                if(data == null || data.Method == null)
                {
                    await SendError(webSocket, data, ErrorMessages.WRONG_REQUEST_ERROR_MESSAGE);
                    return;
                }

                if(!data.InstrumentNames.Any())
                {
                    await SendError(webSocket, data, ErrorMessages.NO_INSTRUMENT_ERROR_MESSAGE);
                    return;
                }

                switch (data.Method)
                {
                    case WebSocketMethod.SUBSCRIBE:
                        foreach (var instrument in data.InstrumentNames)
                            await SubscribeClient(webSocket, data, instrument);
                        break;
                    case WebSocketMethod.UNSUBSCRIBE:
                        foreach (var instrument in data.InstrumentNames)
                            await UnsubscribeClient(webSocket, data, instrument);
                        break;
                    default:
                        break;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "WebSocket data process error");
            }
        }

        private async Task UnsubscribeClient(WebSocket webSocket, WebSocketRequest data, string instrument)
        {
            if (!_clients.ContainsKey(instrument))
                await SendError(webSocket, data, ErrorMessages.INSTRUMENT_NOT_FOUND_ERROR_MESSAGE);
            else
            {
                if (!_clients[instrument].TryRemove(webSocket, out var res))
                    await SendError(webSocket, data, ErrorMessages.SUBSCRIBTION_NOT_FOUND_ERROR_MESSAGE);
                else
                    await SendSuccessfulResponse(webSocket, data);
            }
        }

        private async Task SubscribeClient(WebSocket webSocket, WebSocketRequest data, string instrument)
        {
            if (!_clients.ContainsKey(instrument))
                await SendError(webSocket, data, ErrorMessages.INSTRUMENT_NOT_FOUND_ERROR_MESSAGE);
            else
            {
                var bag = _clients[instrument];
                if (bag.ContainsKey(webSocket))
                    await SendError(webSocket, data, ErrorMessages.ALREADY_SUBSCRIBED_ERROR_MESSAGE);
                else
                {
                    _logger.LogInformation("Client subscribed to {0}", instrument);
                    bag.TryAdd(webSocket, true);
                    await SendSuccessfulResponse(webSocket, data);
                }
            }
        }

        private async Task SendSuccessfulResponse(WebSocket webSocket, WebSocketRequest data)
        {
            await SendResult(webSocket, new WebSocketResult { Id = data.Id, Error = null});
        }

        private async Task SendError(WebSocket webSocket, WebSocketRequest data, string errorMessage)
        {
            await SendResult(webSocket, new WebSocketResult { Error = errorMessage, Id = data?.Id ?? 0});
        }

        private static async Task SendResult(WebSocket webSocket, WebSocketResult result)
        {
            var dataStr = JsonSerializer.Serialize(result);
            byte[] buffer = Encoding.UTF8.GetBytes(dataStr);
            var segment = new ArraySegment<byte>(buffer);
            await webSocket.SendAsync(segment, WebSocketMessageType.Text, true, CancellationToken.None);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var socket in _clients.Values.SelectMany(x => x.Keys))
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                socket.Dispose();
            }
        }
    }
}

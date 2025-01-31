using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using FinancialInfo.FinancialData;
using FinancialInfo.WebSocketManagement;
using Microsoft.Extensions.Options;

namespace FinancialInfo.Tiingo
{
    public class TiingoWebSocketClient(FinancialDataRepo financialDataRepo, IOptions<AppConfig> appConfig,
        ILogger<TiingoWebSocketClient> logger) 
    {
        private TiingoConfig _config = appConfig.Value.Tiingo;

        public async Task StartRecievingAsync(CancellationToken stoppingToken)
        {
            await StartRecievingAsync(stoppingToken, _config.TiingoAPIKey, _config.TiingoForexConfig);
            await StartRecievingAsync(stoppingToken, _config.TiingoAPIKey, _config.TiingoCryptoConfig);
        }

        private async Task StartRecievingAsync(CancellationToken stoppingToken, string token, TiingoEndpointConfig tiingoConfig)
        {
            try
            {
                var webSocketClient = new ClientWebSocket();
                await webSocketClient.ConnectAsync(new Uri(tiingoConfig.APIUrl), stoppingToken);
                logger.LogInformation("Tiingo webSocket connection opened");

                _ = Task.Run(async () =>
                {
                    try
                    {

                        while (webSocketClient.State == WebSocketState.Open)
                        {
                            var buffer = new byte[UpdateMessagerService.WEB_SOCKET_BUFFER_SIZE];
                            WebSocketReceiveResult? receiveResult = null;

                            do
                            {
                                var responseStream = new MemoryStream();

                                do
                                {
                                    receiveResult = await webSocketClient.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                                    responseStream.Write(buffer, 0, receiveResult.Count);
                                }
                                while (!receiveResult.EndOfMessage);

                                if (receiveResult.MessageType == WebSocketMessageType.Close)
                                {
                                    logger.LogInformation("WebSocket connection has been closed");

                                    break;
                                }
                                else
                                    await ProcessUpdate(responseStream);
                            }
                            while (!receiveResult.CloseStatus.HasValue);

                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error on Tiingo client");
                    }

                    // Restart if it hasn't been canceled. 
                    if (!stoppingToken.IsCancellationRequested)
                    {
                        if (webSocketClient.State != WebSocketState.CloseReceived && webSocketClient.State != WebSocketState.Closed)
                            await webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);

                        webSocketClient.Dispose();
                        logger.LogInformation("Restarting Tiingo webSocket connection");
                        await StartRecievingAsync(stoppingToken, token, tiingoConfig);
                    }
                    else
                    {
                        if (webSocketClient.State  != WebSocketState.Closed)
                            await webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                        webSocketClient.Dispose();
                    }
                });

                await SubscribeToUpdates(webSocketClient, token, tiingoConfig.Instruments);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on Tiingo client");
            }
        }

        private async Task ProcessUpdate(MemoryStream stream)
        {
            try
            {
                stream.Position = 0;
                using StreamReader reader = new StreamReader(stream);
                string message = reader.ReadToEnd();
                logger.LogInformation("Received message from Tiingo: {0}", message);

                if (string.IsNullOrEmpty(message) || !message.Contains("\"messageType\":\"A\""))
                    return;

                var data = JsonSerializer.Deserialize<TiingoPriceUpdate>(message.Trim());
                if (data != null)
                    financialDataRepo.SetPrice(data.GetSymbol(), data.GetPrice());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error on Tiingo client");
            }
        }

        private async Task SubscribeToUpdates(ClientWebSocket webSocket, string token, IEnumerable<string> instruments)
        {
            var parameters = instruments.Select(x => $"{x.ToLower()}");
            var data = new TiingoAPIData { 
                Method = TiingoWebSocketMethod.subscribe,
                API_KEY= token,
                Data = new TiingoRequestData { Tickers = parameters} };

            var dataStr = JsonSerializer.Serialize(data);
            ArraySegment<byte> sendBuffer = new ArraySegment<byte>(Encoding.UTF8.GetBytes(dataStr));
            await webSocket.SendAsync(sendBuffer, WebSocketMessageType.Text, true, CancellationToken.None);
            logger.LogInformation("Sending request for subscribing to {0} price update from Tiingo", parameters);
        }
    }
}

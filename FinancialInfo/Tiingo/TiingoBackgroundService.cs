namespace FinancialInfo.Tiingo
{
    public class TiingoBackgroundService (TiingoWebSocketClient TiingoWebSocketClient) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await TiingoWebSocketClient.StartRecievingAsync(stoppingToken);
        }
    }
}

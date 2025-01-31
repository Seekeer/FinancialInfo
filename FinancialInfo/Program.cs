using FinancialInfo.Tiingo;
using FinancialInfo.FinancialData;
using FinancialInfo.WebSocketManagement;
using FinancialInfo;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        var sect = builder.Configuration.GetSection(AppConfig.APP);
        builder.Services.Configure<AppConfig>(builder.Configuration.GetSection(AppConfig.APP));
        builder.Services.AddSingleton<FinancialDataRepo, FinanceDataConcurrentDictRepo>();
        builder.Services.AddSingleton<IUpdateMessagerService, UpdateMessagerService>();
        builder.Services.AddSingleton<TiingoWebSocketClient, TiingoWebSocketClient>();
        builder.Services.AddHostedService<TiingoBackgroundService>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        builder.Logging.AddConsole();

        var app = builder.Build();

        app.UseWebSockets();

        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
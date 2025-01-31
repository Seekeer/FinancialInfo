# FinancialInfo

## Description
ASP.NET Core service which provide REST API and Websocket endpoits for live financial instruments. Data is taken from Tiingo. Server could handle > 1000 simultaneous websocket clients. 

## Quickstart
1. Clone code.
2. Install .NET 8 SDK. 
3. Replace string **REPLACE_WITH_REAL_API_KEY** in **[appsettings.json](https://github.com/Seekeer/FinancialInfo/blob/main/FinancialInfo/appsettings.json)** with real [API key from Tiingo](https://www.tiingo.com/account/api/token).
4. Open folder with project in VS Code.
5. Execute command `dotnet build` in terminal.
6. Execute command `dotnet run` in terminal.
7. Open `http://localhost:6080/Instruments/list` in your browser.
8. Open `http://localhost:6080/Instruments/BTCUSD` in your browser.
9. Use your websocket client and open connection with `ws://localhost:6080/ws`.
10. Send `"{"Method":"SUBSCRIBE","InstrumentNames":["BTCUSD"],"Id":1}"` by socket.

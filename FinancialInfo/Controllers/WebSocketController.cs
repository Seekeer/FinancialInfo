using FinancialInfo.WebSocketManagement;
using Microsoft.AspNetCore.Mvc;

namespace FinancialInfo.Controllers
{
    public class WebSocketController(IUpdateMessagerService updateMessagerService) : ControllerBase
    {
        [HttpGet("/ws")]
        public async Task Get()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                await updateMessagerService.ProcessWebSocket(webSocket);
            }
            else
            {
                HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }
    }
}
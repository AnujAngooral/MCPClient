using MCPClient.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.AI;

namespace MCPClient.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IMcpChatService _chatService;

        public ChatController(IMcpChatService chatService)
        {
            _chatService = chatService;
        }

        [HttpPost]
        public async Task<IActionResult> Index(ChatRequest request, CancellationToken cancellationToken)
        {
            var answer = await _chatService.AskAsync(
            request.Question,
            cancellationToken);

            return Ok(new Models.ChatResponse(answer));

        }
    }
}

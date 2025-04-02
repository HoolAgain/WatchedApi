using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WatchedApi.Infrastructure;
using WatchedApi.Infrastructure.Data;
using WatchedApi.Infrastructure.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace WatchedApi.Ports.Rest.Controllers
{
    [ApiController]
    [Route("api/AI")]
    public class AIController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AIService _aiService;

        public AIController(ApplicationDbContext context, AIService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> ChatWithGemini([FromBody] PromptRequest request)
        {
            //check prompt
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return BadRequest("Prompt is required.");
            }

            try
            {

                //call service and the model prompt
                var response = await _aiService.GenerateGeminiResponse(request.Prompt);
                return Ok(new { response });
            }
            catch (Exception ex)
            {
                //exception
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
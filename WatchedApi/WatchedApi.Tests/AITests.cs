using Xunit;
using Microsoft.AspNetCore.Mvc;
using WatchedApi.Ports.Rest.Controllers;
using WatchedApi.Infrastructure;
using WatchedApi.Infrastructure.Data.Models;
using WatchedApi.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using System.Collections.Generic;
using Moq;
using Moq.Protected;
using System.Threading;

namespace WatchedApi.Tests
{
    public class AITests
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly AIService _realAIService;
        private readonly AIController _controller;

        public AITests()
        {
            // 1. In-memory EF Core DB
            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            _dbContext = new ApplicationDbContext(dbOptions);

            // 2. Fake IConfiguration with dummy API key
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "GoogleAI:ApiKey", "fake-api-key" }
                })
                .Build();

            // 3. Mocked HttpClient with fake JSON response from Gemini
            var fakeResponseJson = @"
            {
                ""candidates"": [
                    {
                        ""content"": {
                            ""parts"": [
                                { ""text"": ""This is a mocked Gemini response."" }
                            ]
                        }
                    }
                ]
            }";

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(fakeResponseJson, Encoding.UTF8, "application/json"),
                });

            var httpClient = new HttpClient(handlerMock.Object);

            // 4. Real AIService with mocks
            _realAIService = new AIService(config, _dbContext, httpClient);

            // 5. AIController with real service
            _controller = new AIController(_dbContext, _realAIService);
        }

        [Fact]
        public async Task ChatWithGemini_ReturnsBadRequest_WhenPromptIsNull()
        {
            var request = new PromptRequest { Prompt = null! };

            var result = await _controller.ChatWithGemini(request);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ChatWithGemini_ReturnsOk_WithFakeGeminiResponse()
        {
            var request = new PromptRequest { Prompt = "What is the Watched app?" };

            var result = await _controller.ChatWithGemini(request);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("mocked Gemini response", okResult.Value?.ToString());
        }
    }
}
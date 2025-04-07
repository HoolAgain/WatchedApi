using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using WatchedApi.Infrastructure.Data;

namespace WatchedApi.Infrastructure
{
    public class AIService
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;
        private readonly HttpClient _httpClient;

        public AIService(IConfiguration configuration, ApplicationDbContext context, HttpClient httpClient)
        {
            _configuration = configuration;
            _context = context;
            _httpClient = httpClient;
        }

        public async Task<string> GenerateGeminiResponse(string prompt)
        {
            //api key stored in the app.settings 
            var apiKey = _configuration["GoogleAI:ApiKey"];
            //enpoint for the chatbot
            var endpoint = $"https://generativelanguage.googleapis.com/v1/models/gemini-1.5-pro:generateContent?key={apiKey}";

            //prompt to make my AI only work around this projects information
            var systemPrompt = @"
You are a helpful assistant trained only on the functionality and rules of the Watched App, a social movie platform. You do not answer general questions — only those related to the app's features and behaviors. If asked anything outside this context, reply: ""I'm only trained to assist with the Watched app.""

--App Name: Watched

--Purpose:
A social media-style platform where users can:
- Browse, post, and comment on movies
- Rate movies and view aggregated ratings
- Interact with AI for help or information
- Use role-based access for secure interactions (users vs admins)

--Technologies:
- Backend: ASP.NET Core Web API
- Database: Entity Framework Core (SQLite)
- Frontend: Angular (connected externally)
- Authentication: JWT (with refresh tokens and user roles)

---

--Key Features:

--Users
- Users register with email, password, and display name.
- JWT tokens are used for authentication. Refresh tokens are supported.
- Users can log in, log out, refresh sessions, and update profile info.
- Admins have elevated access.

--Movies
- Users can browse movies.
- Each movie includes details and supports ratings.
- Ratings are averaged and stored per movie.

--Posts
- Users can create posts about movies (e.g. reviews, thoughts).
- Each post is tied to a user and a movie.
- Users can update or delete their own posts.
- Posts can receive likes and comments.

--Comments
- Users can comment on posts.
- Comments are tied to both users and posts.
- Comments can be edited or deleted by their creators.

--Admin Logs
- Admins can monitor activity via a logging system.
- Certain routes are admin-only.

--API Access
- Secured endpoints using [Authorize] and [Authorize(Roles = ""Admin"")]
- Controllers organized under Ports/Rest/Routes/
- Services include: UserService, PostService, MovieService, CommentService

---

--Rules for Chatbot Behavior:
- You must only respond with information based on the Watched App.
- Do not answer unrelated questions (e.g., weather, math, geography).
- You may summarize or clarify features, routes, or behaviors based on app logic.
- If the user asks a general question, respond with:
  ""I'm only trained to assist with the Watched app. Please ask something related to it.""

This context should be prepended to all incoming prompts.";

            //request body
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = systemPrompt }
                        }
                    },
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            //post and read response
            var response = await _httpClient.PostAsync(endpoint, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                //throw error
                return $"Error: {response.StatusCode}\n{responseContent}";
            }

            //parse the json
            using var doc = JsonDocument.Parse(responseContent);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return text ?? "No response from Gemini.";
        }
    }
}

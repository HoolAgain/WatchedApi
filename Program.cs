using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using WatchedApi.Infrastructure.Data;
using WatchedApi.Infrastructure;
using System.Text.Json.Serialization;
using WatchedApi.Infrastructure.Data.Models;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

//all my services
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddHttpClient<MovieService>();
builder.Services.AddHttpClient<AIService>();

builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddScoped<Authentication>();
builder.Services.AddScoped<MovieService>();
builder.Services.AddScoped<AIService>();



//needed jwt schema
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.Zero,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });

builder.Services.AddAuthorization();
// DO NOT REMOVE / ALTER: ReferenceHandler.Preserve is mandatory for handling our data structure without recursive cycling
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        policy =>
        {
            policy.WithOrigins("http://localhost:50162")
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Seed admin user if not exists
    if (!context.Users.Any(u => u.IsAdmin))
    {
        var adminUser = new User
        {
            Username = "Admin",
            Email = "admin@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("adpass123"),
            IsAdmin = true,
            CreatedAt = DateTime.UtcNow
        };

        context.Users.Add(adminUser);
        await context.SaveChangesAsync();
        Console.WriteLine("Admin user seeded.");
    }

    if (!context.SiteActivityLogs.Any())
    {
        int adminUserId = 11;
        context.SiteActivityLogs.AddRange(
            new SiteActivityLog
            {
                Activity = "Post",
                Operation = "Delete",
                TimeOf = new DateTime(2025, 1, 14, 17, 0, 0, DateTimeKind.Utc),
                UserId = adminUserId
            },
            new SiteActivityLog
            {
                Activity = "Post",
                Operation = "Delete",
                TimeOf = new DateTime(2025, 1, 24, 17, 0, 0, DateTimeKind.Utc),
                UserId = adminUserId
            },
            new SiteActivityLog
            {
                Activity = "Post",
                Operation = "Delete",
                TimeOf = new DateTime(2025, 2, 13, 17, 0, 0, DateTimeKind.Utc),
                UserId = adminUserId
            },
            new SiteActivityLog
            {
                Activity = "Post",
                Operation = "Delete",
                TimeOf = new DateTime(2025, 3, 10, 17, 0, 0, DateTimeKind.Utc),
                UserId = adminUserId
            },
            new SiteActivityLog
            {
                Activity = "Post",
                Operation = "Delete",
                TimeOf = new DateTime(2025, 4, 1, 17, 0, 0, DateTimeKind.Utc),
                UserId = adminUserId
            }
        );
        await context.SaveChangesAsync();
        Console.WriteLine("Site activity logs seeded.");
    }
}


app.UseCors("AllowAngularApp");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

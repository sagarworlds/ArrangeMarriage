using Microsoft.EntityFrameworkCore;
using ArrangeMarriage.Infrastructure.Persistence;
using ArrangeMarriage.Application.Interfaces;
using ArrangeMarriage.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using ArrangeMarriage.API.Hubs;
using ArrangeMarriage.API.Security;
using ArrangeMarriage.API.Serialization;
using ArrangeMarriage.API.Validation;
using System.Text.Json.Serialization.Metadata;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers(options =>
{
    // Entities are bound straight from request bodies; their navigation properties must not be required...
    options.ModelMetadataDetailsProviders.Add(new EntityNavigationValidationMetadataProvider());
})
.AddJsonOptions(options =>
{
    // ...and must not be filled in from the body either (see IgnoreEntityNavigationsOnInput).
    options.JsonSerializerOptions.TypeInfoResolver = new DefaultJsonTypeInfoResolver
    {
        Modifiers = { IgnoreEntityNavigationsOnInput.Modify }
    };
});
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://localhost:5000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// JWT Authentication Configuration. The key must come from configuration (user-secrets/env var in
// production); there is deliberately no hardcoded fallback.
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 32 characters. Set it via user-secrets or the Jwt__Key environment variable.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt.Issuer,
        ValidAudience = jwt.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key))
    };

    // Browsers can't set headers on WebSocket connections, so SignalR sends the token in the query string.
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) && context.HttpContext.Request.Path.StartsWithSegments("/chatHub"))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization();

// Database Configuration
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddScoped<IMatchmakingService, MatchmakingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IMeetingService, MeetingService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAstroService, AstroService>();
builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ChatHub>("/chatHub");

app.Run();

// Exposes the generated entry-point class so integration tests can host the app via WebApplicationFactory.
public partial class Program { }

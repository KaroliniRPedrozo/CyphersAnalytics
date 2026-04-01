using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Asp.Versioning;
using AspNetCoreRateLimit;
using FluentValidation;
using FluentValidation.AspNetCore;
using api.database;
using api.services;
using api.validators;

var builder = WebApplication.CreateBuilder(args);

// Lê o arquivo .env
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env");
if (File.Exists(envPath))
{
    foreach (var line in File.ReadAllLines(envPath))
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
        var parts = line.Split('=', 2);
        if (parts.Length == 2)
            Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
    }
}

var dbConnection  = Environment.GetEnvironmentVariable("DB_CONNECTION")!;
var henrikBaseUrl = Environment.GetEnvironmentVariable("HENRIK_BASEURL")!;
var henrikApiKey  = Environment.GetEnvironmentVariable("HENRIK_APIKEY")!;
var jwtSecret     = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "chave-padrao-dev";

// Banco de dados
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dbConnection));

// Henrik Service
builder.Services.AddHttpClient<HenrikService>(client =>
{
    client.BaseAddress = new Uri(henrikBaseUrl);
    client.DefaultRequestHeaders.Add("Authorization", henrikApiKey);
});
builder.Services.AddScoped<HenrikService>();

// Auth Service
builder.Services.AddScoped<AuthService>();

// Cache
builder.Services.AddMemoryCache();

// Rate Limiting — máximo 30 requisições por minuto por IP
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule { Endpoint = "*", Limit = 30, Period = "1m" }
    };
});
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddInMemoryRateLimiting();

// Versionamento
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'V";
    options.SubstituteApiVersionInUrl = true;
});

// JWT
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = "CyphersAnalytics",
            ValidAudience            = "CyphersAnalytics",
            IssuerSigningKey         = new SymmetricSecurityKey(
                                           Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

// Health Check
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("database");

// Validação
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<PartidaValidator>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseIpRateLimiting();
app.UseCors("frontend");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();
using Microsoft.EntityFrameworkCore;
using api.database;
using api.services;

var builder = WebApplication.CreateBuilder(args);

// Lê o arquivo .env manualmente
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

// Configurações vindas do .env
var dbConnection  = Environment.GetEnvironmentVariable("DB_CONNECTION")!;
var henrikBaseUrl = Environment.GetEnvironmentVariable("HENRIK_BASEURL")!;
var henrikApiKey  = Environment.GetEnvironmentVariable("HENRIK_APIKEY")!;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(dbConnection));

builder.Services.AddHttpClient<HenrikService>(client =>
{
    client.BaseAddress = new Uri(henrikBaseUrl);
    client.DefaultRequestHeaders.Add("Authorization", henrikApiKey);
});

builder.Services.AddScoped<HenrikService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS — permite o frontend React se comunicar com a API
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins("http://localhost:5173") // porta padrão do Vite/React
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("frontend"); // ← CORS ativado
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
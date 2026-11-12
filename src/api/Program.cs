using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using System.IO.Compression;
using System.Text;
using Asp.Versioning;
using AspNetCoreRateLimit;
using FluentValidation;
using FluentValidation.AspNetCore;
using api.database;
using api.services;
using api.validators;

var builder = WebApplication.CreateBuilder(args);
var isDev = builder.Environment.IsDevelopment();

// -------------------------------------------------------------------------------------------------
// Configuracao (variaveis de ambiente; em desenvolvimento tambem le um arquivo .env que NAO vai pro git)
// -------------------------------------------------------------------------------------------------
CarregarDotEnv();

static string? Env(string nome) => Environment.GetEnvironmentVariable(nome) is { Length: > 0 } v ? v : null;
static string Exigir(string nome) => Env(nome) ?? throw new InvalidOperationException($"Variavel {nome} nao configurada.");
static bool EnvBool(string nome) => string.Equals(Env(nome), "true", StringComparison.OrdinalIgnoreCase);

var dbConnection = ConverterConnectionString(Env("DB_CONNECTION") ?? Exigir("DATABASE_URL"));

// JWT: sem segredo padrao embutido no codigo. Em producao a aplicacao nao sobe sem um segredo forte.
var jwtSecret = Env("JWT_SECRET");
if (jwtSecret is null)
{
    if (!isDev) throw new InvalidOperationException("JWT_SECRET nao configurado (minimo 32 caracteres).");
    jwtSecret = "dev-only-secret-do-not-use-in-production-0123456789";
}
if (jwtSecret.Length < 32)
    throw new InvalidOperationException("JWT_SECRET precisa ter pelo menos 32 caracteres.");

var origensPermitidas = new List<string> { "http://localhost:5173", "http://localhost:5174", "http://localhost:3000" };
origensPermitidas.AddRange(
    (Env("FRONTEND_URL") ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(o => o.TrimEnd('/')));
var frontendPrincipal = (Env("FRONTEND_URL") ?? "http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[0].TrimEnd('/');

var provedor = (Env("DATA_PROVIDER") ?? "henrik").ToLowerInvariant();

builder.Services.AddSingleton(new JwtSettings(jwtSecret));

var rsoOptions = new RsoOptions(
    Enabled:      EnvBool("RSO_ENABLED"),
    ClientId:     Env("RSO_CLIENT_ID") ?? "",
    ClientSecret: Env("RSO_CLIENT_SECRET") ?? "",
    RedirectUri:  Env("RSO_REDIRECT_URI") ?? "",
    RegionalHost: Env("RIOT_REGION") ?? "americas",
    FrontendUrl:  frontendPrincipal);
if (rsoOptions.Enabled && (rsoOptions.ClientId == "" || rsoOptions.ClientSecret == "" || rsoOptions.RedirectUri == ""))
    throw new InvalidOperationException("RSO_ENABLED=true exige RSO_CLIENT_ID, RSO_CLIENT_SECRET e RSO_REDIRECT_URI.");
builder.Services.AddSingleton(rsoOptions);
builder.Services.AddHttpClient("rso", c => c.Timeout = TimeSpan.FromSeconds(15));

// -------------------------------------------------------------------------------------------------
// Provedor de dados do VALORANT + limitador de chamadas de saida
// -------------------------------------------------------------------------------------------------
// Padroes conservadores. Chave de DEV da Riot: 100 req/2min. Chave de PRODUCAO: 500 req/10s (ajuste via env).
var (maxPadrao, janelaPadrao) = provedor == "riot" ? (90, 120) : (28, 60);
builder.Services.AddSingleton(new UpstreamThrottle(
    int.TryParse(Env("UPSTREAM_MAX_REQUESTS"), out var maxReq) ? maxReq : maxPadrao,
    TimeSpan.FromSeconds(int.TryParse(Env("UPSTREAM_WINDOW_SECONDS"), out var janela) ? janela : janelaPadrao)));
builder.Services.AddTransient<UpstreamResilienceHandler>();

if (provedor == "riot")
{
    var riotKey = Exigir("RIOT_API_KEY");
    builder.Services.AddHttpClient<RiotApiService>(client =>
    {
        client.DefaultRequestHeaders.Add("X-Riot-Token", riotKey);
        client.Timeout = TimeSpan.FromSeconds(20);
    }).AddHttpMessageHandler<UpstreamResilienceHandler>();
    builder.Services.AddScoped<IValorantDataProvider>(sp => sp.GetRequiredService<RiotApiService>());
}
else
{
    var henrikBaseUrl = Env("HENRIK_BASEURL") ?? "https://api.henrikdev.xyz";
    var henrikApiKey  = Exigir("HENRIK_APIKEY");
    builder.Services.AddHttpClient<HenrikService>(client =>
    {
        client.BaseAddress = new Uri(henrikBaseUrl);
        client.DefaultRequestHeaders.Add("Authorization", henrikApiKey);
        client.Timeout = TimeSpan.FromSeconds(20); // antes: 300s, o que deixava requisicoes de usuarios penduradas
    }).AddHttpMessageHandler<UpstreamResilienceHandler>();
    builder.Services.AddScoped<IValorantDataProvider>(sp => sp.GetRequiredService<HenrikService>());
}

builder.Services.AddScoped<JogadorSyncService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddMemoryCache();

// -------------------------------------------------------------------------------------------------
// Infra web: CORS, banco, rate limit de ENTRADA, versionamento, JWT, health, validacao
// -------------------------------------------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", policy =>
        policy.WithOrigins(origensPermitidas.ToArray()).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(dbConnection));

// Atras de proxy (Render/Railway) o IP do cliente vem em X-Forwarded-For; sem isso todos os usuarios
// pareceriam ter o mesmo IP e o rate limit por IP bloquearia todo mundo junto.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.GeneralRules = new List<RateLimitRule>
    {
        new() { Endpoint = "*",                              Period = "1m",  Limit = 60 },
        // Anti forca-bruta de senha e criacao em massa de contas
        new() { Endpoint = "post:/api/v1/auth/login",        Period = "1m",  Limit = 8  },
        new() { Endpoint = "post:/api/v1/auth/registrar",    Period = "10m", Limit = 5  },
        new() { Endpoint = "put:/api/v1/jogadores/*",        Period = "1m",  Limit = 3  },
    };
});
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddInMemoryRateLimiting();

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

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = AuthService.Issuer,
            ValidAudience = AuthService.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<PartidaValidator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});
builder.Services.Configure<BrotliCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);
builder.Services.Configure<GzipCompressionProviderOptions>(o => o.Level = CompressionLevel.Fastest);

var app = builder.Build();

app.Logger.LogInformation("Provedor de dados: {Provedor} | RSO habilitado: {Rso}", provedor, rsoOptions.Enabled);

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

// Deve ser o primeiro: corrige o IP/esquema do cliente antes do rate limit e do restante.
app.UseForwardedHeaders();

// Swagger so em desenvolvimento (ou com ENABLE_SWAGGER=true): nao expor o mapa da API em producao.
if (isDev || EnvBool("ENABLE_SWAGGER"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Use(async (ctx, next) =>
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});

app.UseResponseCompression();
app.UseCors("frontend");
app.UseIpRateLimiting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// -------------------------------------------------------------------------------------------------
static void CarregarDotEnv()
{
    var candidatos = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env"),
        Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".env"),
    };
    var envPath = candidatos.FirstOrDefault(File.Exists);
    if (envPath is null) return;

    foreach (var line in File.ReadAllLines(envPath))
    {
        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#')) continue;
        var parts = line.Split('=', 2);
        if (parts.Length != 2) continue;

        var nome = parts[0].Trim();
        // Variavel ja definida no ambiente tem prioridade sobre o arquivo.
        if (Environment.GetEnvironmentVariable(nome) is null)
        {
            // remove comentario no fim da linha (ex.: RIOT_PLATFORM=br   # explicacao)
            var valor = parts[1].Split(" #", 2)[0].Trim();
            Environment.SetEnvironmentVariable(nome, valor);
        }
    }
}

// Converte "postgres://user:senha@host:porta/banco" (Render/Railway/Supabase) para o formato do Npgsql.
static string ConverterConnectionString(string conn)
{
    if (!conn.StartsWith("postgres://") && !conn.StartsWith("postgresql://"))
        return conn;

    var uri   = new Uri(conn);
    var info  = uri.UserInfo.Split(':', 2);
    var porta = uri.Port > 0 ? uri.Port : 5432;

    return $"Host={uri.Host};Port={porta};Database={uri.AbsolutePath.TrimStart('/')};" +
           $"Username={Uri.UnescapeDataString(info[0])};" +
           $"Password={(info.Length > 1 ? Uri.UnescapeDataString(info[1]) : "")};" +
           "SSL Mode=Require";
}

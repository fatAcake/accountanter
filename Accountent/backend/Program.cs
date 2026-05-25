using backend.Configuration;
using backend.Extensions;
using DotNetEnv;
using Npgsql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

LoadEnvFile();

var builder = WebApplication.CreateBuilder(args);
builder.UseResolvedDefaultConnectionString();

ValidateRequiredConfiguration(builder.Configuration);

builder.Services.AddApplicationServices(builder.Configuration);

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Секция Jwt не настроена.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

var corsOrigins = (builder.Configuration["Cors:Origins"]
    ?? "http://localhost:5173;http://127.0.0.1:5173")
    .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Accountent API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });
});

var app = builder.Build();

if (!app.Configuration.GetValue<bool>("Testing:SkipDatabaseBootstrap"))
    await app.MigrateAndSeedDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var ex = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
        if (ex is null)
            return;

        context.Response.ContentType = "application/json";
        var (status, message) = ex switch
        {
            PostgresException => (503, "База данных недоступна. Запустите PostgreSQL и перезапустите backend."),
            NpgsqlException => (503, "Не удалось подключиться к PostgreSQL. Проверьте POSTGRES_* в .env."),
            _ => (500, "Внутренняя ошибка сервера."),
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { message });
    });
});
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapControllers();

app.Run();

static void LoadEnvFile()
{
    var cwd = Directory.GetCurrentDirectory();
    foreach (var relative in new[] { ".env", Path.Combine("..", ".env"), Path.Combine("..", "..", ".env") })
    {
        var path = Path.GetFullPath(Path.Combine(cwd, relative));
        if (!File.Exists(path))
            continue;

        Env.Load(path);
        return;
    }

    Env.TraversePath().Load();
}

static void ValidateRequiredConfiguration(IConfiguration config)
{
    if (string.IsNullOrWhiteSpace(config.GetConnectionString("DefaultConnection")))
    {
        throw new InvalidOperationException(
            "Не задана строка подключения. Укажите POSTGRES_* в .env");
    }

    var secret = config["Jwt:Secret"];
    if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
    {
        throw new InvalidOperationException(
            "Не задан Jwt__Secret (минимум 32 символа). Скопируйте .env.example в .env.");
    }
}

public partial class Program;

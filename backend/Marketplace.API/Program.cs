using Microsoft.AspNetCore.Authentication.Cookies;
using Marketplace.API;
using Marketplace.API.Services;
using OpenAI.Extensions;
using Marketplace.API.Configuration;
using Microsoft.EntityFrameworkCore;
using Marketplace.API.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Настройка конфигурации (читаем секции из appsettings.json)
builder.Services.Configure<ApiKeys>(builder.Configuration.GetSection("ApiKeys"));
builder.Services.Configure<WorkerSettings>(builder.Configuration.GetSection("WorkerSettings"));

// 1.5. База данных (SQLite)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=marketplace.db"));

// 2. Регистрация HTTP клиентов
builder.Services.AddHttpClient<IWildberriesApiClient, WildberriesApiClient>();
builder.Services.AddHttpClient<IOzonApiClient, OzonApiClient>();

// 3. Регистрация OpenAI
builder.Services.AddOpenAIService(settings =>
{
    settings.ApiKey = builder.Configuration["ApiKeys:OpenAI"] ??
                      throw new InvalidOperationException("OpenAI API key is not configured.");
});
builder.Services.AddTransient<IOpenAiClient, OpenAiClient>();
builder.Services.AddScoped<IFeedbackService, FeedbackService>();

builder.Services.AddSingleton<WorkerStateService>();

// 4. Регистрация Воркера (Фоновая задача)
builder.Services.AddHostedService<Worker>();

// 5. Стандартные сервисы API

// 7. Аутентификация (Cookie)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "MarketplaceAuth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax; // Было Strict, стало Lax
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        // Важно: переопределяем редирект, чтобы API возвращал 401, а не редиректил на /Account/Login
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
    });

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 6. CORS (Чтобы фронтенд мог делать запросы)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Адрес Vite по умолчанию
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials(); // ВОТ ЭТО ВАЖНО
        });
});

var app = builder.Build();

// --- Pipeline ---

if (app.Environment.IsDevelopment())
{
    // 1. Генерируем JSON-файл спецификации
    app.UseSwagger();

    // 2. Подключаем Scalar UI и указываем путь к этому файлу
    app.MapScalarApiReference(options =>
    {
        options.WithOpenApiRoutePattern("/swagger/v1/swagger.json");
        options.WithTitle("Marketplace API");
    });
}

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

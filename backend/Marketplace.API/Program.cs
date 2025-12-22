using Microsoft.AspNetCore.Authentication.Cookies;
using Marketplace.API;
using Marketplace.API.Services;
using Marketplace.API.Configuration;
using Microsoft.EntityFrameworkCore;
using Marketplace.API.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Настройка конфигурации (читаем секции из appsettings.json)
builder.Services.Configure<ApiKeys>(builder.Configuration.GetSection("ApiKeys"));
builder.Services.Configure<WorkerSettings>(builder.Configuration.GetSection("WorkerSettings"));

// 1.5. База данных (SQLite)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=marketplace.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

// 2. Регистрация HTTP клиентов
builder.Services.AddHttpClient<IWildberriesApiClient, WildberriesApiClient>();
builder.Services.AddHttpClient<IOzonApiClient, OzonApiClient>();

// 3. Регистрация OpenAI
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

// 6. CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend",
        policy =>
        {
            policy.SetIsOriginAllowed(origin => true) // <--- РАЗРЕШАЕМ ВСЕ ИСТОЧНИКИ
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

var app = builder.Build();

// --- АВТО-МИГРАЦИЯ БАЗЫ ДАННЫХ ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        // Эта команда применит все миграции и создаст базу, если её нет
        context.Database.Migrate();
        Console.WriteLine("✅ База данных успешно обновлена (Migrate).");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ Ошибка при создании/обновлении базы данных.");
    }
}
// ----------------------------------

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

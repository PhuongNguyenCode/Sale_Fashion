using FashionStore.Configuration;
using FashionStore.Data;
using FashionStore.Services;
using FashionStore.Services.Gateways;
using FashionStore.Services.Webhooks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (builder.Environment.IsProduction() && string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("ConnectionStrings:DefaultConnection must be configured outside the project for Production.");
}

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString)
           .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Configure Payment Gateway Options
builder.Services.Configure<CassoConfiguration>(builder.Configuration.GetSection("PaymentGateways:Casso"));
builder.Services.Configure<VNPayConfiguration>(builder.Configuration.GetSection("PaymentGateways:VNPay"));

// Validate and log gateway configuration status
var config = builder.Configuration;
var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");

ValidateGatewayConfig("Casso", config.GetSection("PaymentGateways:Casso"), logger,
    new[] { "ClientId", "ApiKey", "ChecksumKey", "SecureToken", "BankName", "AccountNumber", "AccountName" }, builder.Environment.IsProduction());
ValidateGatewayConfig("VNPay", config.GetSection("PaymentGateways:VNPay"), logger,
    new[] { "TmnCode", "HashSecret", "PaymentUrl" }, builder.Environment.IsProduction());

// HttpClient for payment gateways
builder.Services.AddHttpClient();
builder.Services.AddHttpClient<CassoApiService>();

// Payment Gateway Services
builder.Services.AddScoped<IPaymentGateway, CodPaymentGateway>();
builder.Services.AddScoped<IPaymentGateway, CassoPaymentGateway>();
builder.Services.AddScoped<IPaymentGateway, VNPayPaymentGateway>();

// Casso API Service for polling
builder.Services.AddScoped<CassoApiService>();

// Webhook Parsers
builder.Services.AddScoped<IWebhookParser, CassoWebhookParser>();
builder.Services.AddScoped<IWebhookParser, VNPayWebhookParser>();

// Domain Services
builder.Services.AddScoped<IDiscountCalculator, DiscountCalculator>();
builder.Services.AddScoped<ITransactionValidator, TransactionValidator>();
builder.Services.AddScoped<IIdempotencyManager, IdempotencyManager>();

// Core Services
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<IWebhookHandler, WebhookHandler>();

// Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// Allow a local UI preview without polling live payment transactions.
if (!builder.Configuration.GetValue<bool>("DisablePaymentPolling"))
    builder.Services.AddHostedService<PaymentBackgroundService>();

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "FashionStore.Auth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.Events = new CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdValue, out var userId))
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return;
                }

                var db = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var isActive = await db.Users.AnyAsync(user => user.UserId == userId && user.IsActive);
                if (!isActive)
                {
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    });

// Session (for Cart, etc. if needed later, though Cart can be saved in DB)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseRouting();

// MapStaticAssets only maps files known at build time. Runtime uploads (admin
// product/category/brand images) need the static-file middleware as well.
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

// Helper method for validating gateway configuration
static void ValidateGatewayConfig(string gatewayName, IConfigurationSection section, ILogger logger, string[] requiredFields, bool enforce)
{
    var enabled = section.GetValue<bool>("Enabled");
    
    if (!enabled)
    {
        logger.LogInformation("Payment gateway {GatewayName} is disabled", gatewayName);
        return;
    }

    var missingFields = new List<string>();
    foreach (var field in requiredFields)
    {
        var value = section.GetValue<string>(field);
        if (string.IsNullOrWhiteSpace(value))
        {
            missingFields.Add(field);
        }
    }

    if (missingFields.Any())
    {
        var missing = string.Join(", ", missingFields);
        if (enforce)
        {
            throw new InvalidOperationException($"Payment gateway {gatewayName} is enabled but missing configuration: {missing}.");
        }

        logger.LogWarning("Payment gateway {GatewayName} is enabled but missing required configuration: {MissingFields}.",
            gatewayName, missing);
    }
    else
    {
        logger.LogInformation("Payment gateway {GatewayName} is enabled and properly configured", gatewayName);
    }
}

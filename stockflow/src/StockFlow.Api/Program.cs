using StockFlow.Api.ErrorHandling;
using StockFlow.Api.Realtime;
using StockFlow.Api.Security;
using StockFlow.Application;
using StockFlow.Application.Abstractions;
using StockFlow.Infrastructure;
using StockFlow.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddScoped<IStockNotifier, NullStockNotifier>();


var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Web", policy =>
    policy.WithOrigins(allowedOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();


var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
app.MapOpenApi();
}

app.UseCors("Web");

app.MapControllers();
app.MapHealthChecks("/health");

await DbInitializer.InitializeAsync(app.Services, app.Configuration);

app.Run();

public partial class Program { }
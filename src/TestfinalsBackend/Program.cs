using TestfinalsBackend.Domain;
using TestfinalsBackend.Endpoints;
using TestfinalsBackend.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Registered as an interface so a test can replace it without replacing
// the host. A concrete registration would leave nothing to substitute.
builder.Services.AddSingleton<IServiceStatus, ServiceStatus>();
builder.Services.AddSingleton<ILogisticsStore, InMemoryLogisticsStore>();

// Describes the API so the contract tests have something to generate
// from. Document only — Swagger UI is a separate package and is not
// referenced, so nothing new is published by the running service.
builder.Services.AddOpenApi();

// A browser only lets a frontend on another origin call this API when that
// origin is listed here. ALPHACI sets CORS_ORIGINS to the deployed
// frontend's address on managed hosting; a local run falls back to the
// frontend dev server.
var corsOrigins = (builder.Configuration["CORS_ORIGINS"] ?? "http://localhost:3000,http://localhost:5173")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(corsOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials()));

var app = builder.Build();

app.UseCors();

// Pipeline Contract Endpoints (REQUIRED BY ALPHACI)
app.MapHealthEndpoints();

// Logistics Platform API (v1)
app.MapLogisticsEndpoints();

app.Run();

// Exposed so the test project can host the application in memory.
public partial class Program;

using TestfinalsBackend.Domain;
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

// ==========================================
// Pipeline Contract Endpoints (REQUIRED BY ALPHACI)
// ==========================================
app.MapGet("/health", (IServiceStatus status) =>
    Results.Ok(new HealthResponse(status.CurrentStatus(), ServiceInfo.Name)))
   .Produces<HealthResponse>(StatusCodes.Status200OK);

app.MapGet("/", () => Results.Ok(new HealthResponse("ready", ServiceInfo.Name)))
   .Produces<HealthResponse>(StatusCodes.Status200OK);

// ==========================================
// Logistics Platform API (v1)
// ==========================================
var api = app.MapGroup("/api/v1");

// Auth Endpoints
api.MapPost("/auth/login", (LoginRequest request, ILogisticsStore store) =>
{
    var user = store.Authenticate(request.Email, request.Password);
    if (user is null)
    {
        return Results.Json(new { message = "Invalid email or password. Use demo credentials (e.g. dispatcher@logipulse.io / password123)." }, statusCode: StatusCodes.Status401Unauthorized);
    }
    return Results.Ok(user);
}).Produces<UserProfile>(StatusCodes.Status200OK);

api.MapGet("/auth/me", (HttpRequest request, ILogisticsStore store) =>
{
    var authHeader = request.Headers.Authorization.ToString();
    var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
        ? authHeader["Bearer ".Length..].Trim()
        : null;

    if (string.IsNullOrEmpty(token))
    {
        return Results.Unauthorized();
    }

    var user = store.GetUserByToken(token);
    return user is not null ? Results.Ok(user) : Results.Unauthorized();
}).Produces<UserProfile>(StatusCodes.Status200OK);

// Analytics
api.MapGet("/analytics/overview", (ILogisticsStore store) =>
    Results.Ok(store.GetAnalytics()))
   .Produces<LogisticsAnalytics>(StatusCodes.Status200OK);

// Shipments
api.MapGet("/shipments", (string? status, string? search, ILogisticsStore store) =>
    Results.Ok(store.GetShipments(status, search)))
   .Produces<IReadOnlyList<Shipment>>(StatusCodes.Status200OK);

api.MapGet("/shipments/{id}", (string id, ILogisticsStore store) =>
{
    var shipment = store.GetShipmentById(id);
    return shipment is not null ? Results.Ok(shipment) : Results.NotFound();
}).Produces<Shipment>(StatusCodes.Status200OK);

api.MapGet("/shipments/track/{trackingNumber}", (string trackingNumber, ILogisticsStore store) =>
{
    var shipment = store.GetShipmentByTrackingNumber(trackingNumber);
    return shipment is not null ? Results.Ok(shipment) : Results.NotFound(new { message = $"Tracking number {trackingNumber} not found." });
}).Produces<Shipment>(StatusCodes.Status200OK);

api.MapPost("/shipments", (CreateShipmentDto dto, ILogisticsStore store) =>
{
    var now = DateTimeOffset.UtcNow;
    var randomSuffix = Guid.NewGuid().ToString("N")[..4].ToUpperInvariant();
    var trackingNumber = $"LP-{Random.Shared.Next(1000, 9999)}-{randomSuffix}";
    var shipmentId = $"shp-{Guid.NewGuid():N}";

    var initialEvent = new TrackingEvent(
        $"evt-{Guid.NewGuid():N}",
        shipmentId,
        ShipmentStatuses.Pending,
        $"{dto.Origin} Sorting Hub",
        "Waybill generated. Awaiting dispatch.",
        now);

    var shipment = new Shipment(
        shipmentId,
        trackingNumber,
        dto.SenderName,
        dto.SenderAddress,
        dto.RecipientName,
        dto.RecipientAddress,
        dto.Origin,
        dto.Destination,
        ShipmentStatuses.Pending,
        dto.Priority,
        dto.WeightKg,
        now.AddDays(dto.EstimatedDays > 0 ? dto.EstimatedDays : 3),
        dto.AssignedDriverId,
        dto.AssignedVehicleId,
        now,
        now,
        new List<TrackingEvent> { initialEvent });

    var created = store.CreateShipment(shipment);
    return Results.Created($"/api/v1/shipments/{created.Id}", created);
}).Produces<Shipment>(StatusCodes.Status201Created);

api.MapPatch("/shipments/{id}/status", (string id, UpdateStatusDto dto, ILogisticsStore store) =>
{
    var updated = store.UpdateShipmentStatus(id, dto.Status, dto.Location, dto.Description);
    return updated is not null ? Results.Ok(updated) : Results.NotFound();
}).Produces<Shipment>(StatusCodes.Status200OK);

api.MapPost("/shipments/{id}/simulate", (string id, ILogisticsStore store) =>
{
    var updated = store.AdvanceShipmentSimulation(id);
    return updated is not null ? Results.Ok(updated) : Results.NotFound();
}).Produces<Shipment>(StatusCodes.Status200OK);

// Fleet & Drivers
api.MapGet("/fleet/vehicles", (ILogisticsStore store) =>
    Results.Ok(store.GetVehicles()))
   .Produces<IReadOnlyList<Vehicle>>(StatusCodes.Status200OK);

api.MapGet("/fleet/drivers", (ILogisticsStore store) =>
    Results.Ok(store.GetDrivers()))
   .Produces<IReadOnlyList<Driver>>(StatusCodes.Status200OK);

api.MapPost("/fleet/dispatch", (DispatchDto dto, ILogisticsStore store) =>
{
    var success = store.AssignDriverToVehicle(dto.VehicleId, dto.DriverId);
    return success ? Results.Ok(new { success = true, message = "Driver assigned and dispatched." }) : Results.BadRequest(new { message = "Vehicle or driver not found." });
});

// Warehouses & Inventory
api.MapGet("/warehouses", (ILogisticsStore store) =>
    Results.Ok(store.GetWarehouses()))
   .Produces<IReadOnlyList<Warehouse>>(StatusCodes.Status200OK);

api.MapGet("/inventory", (string? warehouseId, ILogisticsStore store) =>
    Results.Ok(store.GetInventory(warehouseId)))
   .Produces<IReadOnlyList<InventoryItem>>(StatusCodes.Status200OK);

app.Run();

// DTOs & Records
public record HealthResponse(string Status, string Service);
public record LoginRequest(string Email, string Password);
public record CreateShipmentDto(
    string SenderName,
    string SenderAddress,
    string RecipientName,
    string RecipientAddress,
    string Origin,
    string Destination,
    string Priority,
    double WeightKg,
    int EstimatedDays,
    string? AssignedDriverId,
    string? AssignedVehicleId);
public record UpdateStatusDto(string Status, string Location, string Description);
public record DispatchDto(string VehicleId, string DriverId);

public static class ServiceInfo
{
    public const string Name = "testfinals-backend";
}

// Exposed so the test project can host the application in memory.
public partial class Program;

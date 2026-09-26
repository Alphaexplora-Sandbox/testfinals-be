using TestfinalsBackend.Domain;
using TestfinalsBackend.Infrastructure;

namespace TestfinalsBackend.Endpoints;

public static class LogisticsEndpoints
{
    public static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        app.MapGet("/health", (IServiceStatus status) =>
            Results.Ok(new HealthResponse(status.CurrentStatus(), ServiceInfo.Name)))
           .Produces<HealthResponse>(StatusCodes.Status200OK);

        app.MapGet("/", () => Results.Ok(new HealthResponse("ready", ServiceInfo.Name)))
           .Produces<HealthResponse>(StatusCodes.Status200OK);

        return app;
    }

    public static WebApplication MapLogisticsEndpoints(this WebApplication app)
    {
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

        return app;
    }
}

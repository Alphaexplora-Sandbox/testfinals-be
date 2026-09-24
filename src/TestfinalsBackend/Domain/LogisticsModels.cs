namespace TestfinalsBackend.Domain;

public static class ShipmentStatuses
{
    public const string Pending = "Pending";
    public const string PickedUp = "Picked Up";
    public const string InTransit = "In Transit";
    public const string OutForDelivery = "Out for Delivery";
    public const string Delivered = "Delivered";
    public const string Delayed = "Delayed";
    public const string Cancelled = "Cancelled";
}

public static class ShipmentPriorities
{
    public const string Standard = "Standard";
    public const string Express = "Express";
    public const string Overnight = "Overnight";
    public const string ColdChain = "Cold Chain";
}

public record TrackingEvent(
    string Id,
    string ShipmentId,
    string Status,
    string Location,
    string Description,
    DateTimeOffset Timestamp);

public record Shipment(
    string Id,
    string TrackingNumber,
    string SenderName,
    string SenderAddress,
    string RecipientName,
    string RecipientAddress,
    string Origin,
    string Destination,
    string Status,
    string Priority,
    double WeightKg,
    DateTimeOffset EstimatedDelivery,
    string? AssignedDriverId,
    string? AssignedVehicleId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    List<TrackingEvent> Events);

public record Vehicle(
    string Id,
    string PlateNumber,
    string Model,
    string Type,
    string Status,
    double CapacityKg,
    int FuelOrBatteryPct,
    string CurrentLocation,
    string? AssignedDriverId);

public record Driver(
    string Id,
    string FullName,
    string LicenseNumber,
    string Phone,
    string Status,
    double Rating,
    string? AssignedVehicleId);

public record Warehouse(
    string Id,
    string Code,
    string Name,
    string City,
    string Country,
    double CapacitySqM,
    int OccupancyPct,
    int ActiveShipments);

public record InventoryItem(
    string Id,
    string Sku,
    string Name,
    string Category,
    string WarehouseId,
    int Quantity,
    string Unit,
    int ReorderLevel);

public record UserProfile(
    string Id,
    string Username,
    string Email,
    string FullName,
    string Role,
    string Token);

public record LogisticsAnalytics(
    int TotalShipments,
    int ActiveInTransit,
    int DeliveredToday,
    int DelayedAlerts,
    double FleetUtilizationPct,
    double WarehouseOccupancyAvgPct,
    double OnTimeDeliveryRatePct);

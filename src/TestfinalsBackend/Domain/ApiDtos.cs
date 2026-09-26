namespace TestfinalsBackend.Domain;

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

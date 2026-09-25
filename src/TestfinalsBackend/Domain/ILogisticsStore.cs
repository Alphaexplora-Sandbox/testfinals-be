namespace TestfinalsBackend.Domain;

public interface ILogisticsStore
{
    IReadOnlyList<Shipment> GetShipments(string? status = null, string? search = null);
    Shipment? GetShipmentById(string id);
    Shipment? GetShipmentByTrackingNumber(string trackingNumber);
    Shipment CreateShipment(Shipment shipment);
    Shipment? UpdateShipmentStatus(string id, string newStatus, string location, string description);
    Shipment? AdvanceShipmentSimulation(string id);

    IReadOnlyList<Vehicle> GetVehicles();
    Vehicle? GetVehicleById(string id);
    IReadOnlyList<Driver> GetDrivers();
    Driver? GetDriverById(string id);
    bool AssignDriverToVehicle(string vehicleId, string driverId);

    IReadOnlyList<Warehouse> GetWarehouses();
    IReadOnlyList<InventoryItem> GetInventory(string? warehouseId = null);

    UserProfile? Authenticate(string email, string password);
    UserProfile? GetUserByToken(string token);

    LogisticsAnalytics GetAnalytics();
}

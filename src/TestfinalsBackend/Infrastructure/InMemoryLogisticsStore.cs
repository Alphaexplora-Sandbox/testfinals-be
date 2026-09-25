using System.Collections.Concurrent;
using TestfinalsBackend.Domain;

namespace TestfinalsBackend.Infrastructure;

public sealed class InMemoryLogisticsStore : ILogisticsStore
{
    private readonly ConcurrentDictionary<string, Shipment> _shipments = new();
    private readonly ConcurrentDictionary<string, Vehicle> _vehicles = new();
    private readonly ConcurrentDictionary<string, Driver> _drivers = new();
    private readonly ConcurrentDictionary<string, Warehouse> _warehouses = new();
    private readonly ConcurrentDictionary<string, InventoryItem> _inventory = new();
    private readonly ConcurrentDictionary<string, UserProfile> _users = new();

    public InMemoryLogisticsStore()
    {
        SeedUsers();
        SeedWarehouses();
        SeedInventory();
        SeedDrivers();
        SeedVehicles();
        SeedShipments();
    }

    private void SeedUsers()
    {
        var users = new[]
        {
            new UserProfile("usr-1", "admin", "admin@logipulse.io", "Sarah Lin (Director of Ops)", "Admin", "token-admin-session-xyz"),
            new UserProfile("usr-2", "dispatcher", "dispatcher@logipulse.io", "David Miller (Chief Dispatcher)", "Dispatcher", "token-dispatcher-session-xyz"),
            new UserProfile("usr-3", "warehouse", "warehouse@logipulse.io", "Elena Chen (Warehouse Supervisor)", "WarehouseManager", "token-warehouse-session-xyz"),
            new UserProfile("usr-4", "driver", "driver@logipulse.io", "Marcus Vance (Senior Driver)", "Driver", "token-driver-session-xyz")
        };

        foreach (var u in users)
        {
            _users[u.Email.ToLowerInvariant()] = u;
        }
    }

    private void SeedWarehouses()
    {
        var hubs = new[]
        {
            new Warehouse("wh-chi", "WH-CHI", "Chicago Central Logistics Hub", "Chicago", "United States", 45000, 82, 1420),
            new Warehouse("wh-rtm", "WH-RTM", "Rotterdam Euro Gateway Terminal", "Rotterdam", "Netherlands", 60000, 74, 2150),
            new Warehouse("wh-dfw", "WH-DFW", "Dallas Inland Multi-Modal Hub", "Dallas", "United States", 52000, 88, 1890),
            new Warehouse("wh-sin", "WH-SIN", "Singapore Changi Freight Gateway", "Singapore", "Singapore", 38000, 67, 1100),
            new Warehouse("wh-fra", "WH-FRA", "Frankfurt CargoCity Node", "Frankfurt", "Germany", 48000, 79, 1630)
        };

        foreach (var h in hubs)
        {
            _warehouses[h.Id] = h;
        }
    }

    private void SeedInventory()
    {
        var items = new[]
        {
            new InventoryItem("inv-1", "SKU-ELEC-409", "Automotive ECU Controller", "Electronics", "wh-chi", 420, "Units", 100),
            new InventoryItem("inv-2", "SKU-COLD-882", "Vaccine Temperature Monitors", "Pharma/ColdChain", "wh-rtm", 1150, "Units", 250),
            new InventoryItem("inv-3", "SKU-HEAVY-102", "Hydraulic Cylinder Assy 50mm", "Industrial", "wh-dfw", 84, "Units", 30),
            new InventoryItem("inv-4", "SKU-DRONE-501", "LiPo Battery Cells 48V", "Batteries/Hazmat", "wh-sin", 640, "Units", 150),
            new InventoryItem("inv-5", "SKU-PHARM-331", "Medical Refrigerated Vials", "Pharma/ColdChain", "wh-fra", 290, "Vials", 80),
            new InventoryItem("inv-6", "SKU-ELEC-771", "Fiber Optic Transceivers 100G", "Telecom", "wh-chi", 850, "Units", 200)
        };

        foreach (var item in items)
        {
            _inventory[item.Id] = item;
        }
    }

    private void SeedDrivers()
    {
        var drivers = new[]
        {
            new Driver("drv-1", "Marcus Vance", "CDL-IL-981244", "+1 (312) 555-0192", "OnDelivery", 4.95, "vh-1"),
            new Driver("drv-2", "Elena Rostova", "CDL-TX-551982", "+1 (214) 555-0144", "OnDelivery", 4.98, "vh-2"),
            new Driver("drv-3", "Tariq Al-Mansoor", "CDL-EU-882910", "+31 20 555 0177", "Available", 4.89, "vh-3"),
            new Driver("drv-4", "Sarah Jenkins", "CDL-IL-772183", "+1 (312) 555-0188", "Available", 4.92, null)
        };

        foreach (var d in drivers)
        {
            _drivers[d.Id] = d;
        }
    }

    private void SeedVehicles()
    {
        var vehicles = new[]
        {
            new Vehicle("vh-1", "IL-FREIGHT-99", "Freightliner Cascadia 126", "Semi-Trailer", "OnRoute", 22000, 78, "I-55 Southbound mm 142", "drv-1"),
            new Vehicle("vh-2", "TX-VOLVO-44", "Volvo VNL 860 Sleeper", "Box Truck", "OnRoute", 12500, 85, "Dallas Metro Ring I-635", "drv-2"),
            new Vehicle("vh-3", "NL-MERC-12", "Mercedes-Benz Sprinter 3500", "Cargo Van", "Available", 3200, 92, "Rotterdam Hub Bay 4", "drv-3"),
            new Vehicle("vh-4", "EV-FORD-80", "Ford E-Transit Electric", "Electric Van", "Available", 2400, 95, "Chicago Central Charging Bay", null),
            new Vehicle("vh-5", "NL-SCANIA-07", "Scania 540 S Cold-Master", "Cold Chain Semi", "Maintenance", 20000, 45, "Frankfurt Depot Service Yard", null)
        };

        foreach (var v in vehicles)
        {
            _vehicles[v.Id] = v;
        }
    }

    private void SeedShipments()
    {
        var now = DateTimeOffset.UtcNow;

        var s1 = new Shipment(
            "shp-1",
            "LP-8924-XQ",
            "Apex Semiconductor Mfg",
            "100 Foundry Way, Austin, TX",
            "NextGen Robotics Corp",
            "450 Innovation Blvd, Chicago, IL",
            "Austin, TX",
            "Chicago, IL",
            ShipmentStatuses.InTransit,
            ShipmentPriorities.Express,
            1450.5,
            now.AddHours(6),
            "drv-1",
            "vh-1",
            now.AddDays(-2),
            now.AddHours(-1),
            new List<TrackingEvent>
            {
                new("evt-1", "shp-1", ShipmentStatuses.Pending, "Austin Logistics Hub", "Waybill registered and payload scanned.", now.AddDays(-2)),
                new("evt-2", "shp-1", ShipmentStatuses.PickedUp, "Austin Logistics Hub", "Loaded on Freightliner Cascadia VH-1 by Marcus Vance.", now.AddDays(-1).AddHours(4)),
                new("evt-3", "shp-1", ShipmentStatuses.InTransit, "St. Louis Waypoint", "Crossed regional checkpost; telemetry normal.", now.AddHours(-3)),
                new("evt-4", "shp-1", ShipmentStatuses.InTransit, "I-55 Northbound mm 142", "In transit to Chicago Central Logistics Hub.", now.AddHours(-1))
            });

        var s2 = new Shipment(
            "shp-2",
            "LP-4412-TR",
            "Nordic Pharma Labs BV",
            "Europoort 12, Rotterdam, NL",
            "Medisch Centrum Amsterdam",
            "Meibergdreef 9, Amsterdam, NL",
            "Rotterdam, NL",
            "Amsterdam, NL",
            ShipmentStatuses.OutForDelivery,
            ShipmentPriorities.ColdChain,
            380.0,
            now.AddHours(2),
            "drv-2",
            "vh-2",
            now.AddDays(-1),
            now.AddMinutes(-30),
            new List<TrackingEvent>
            {
                new("evt-5", "shp-2", ShipmentStatuses.Pending, "Rotterdam Cold Hub", "Cryogenic temperature logged at -20°C.", now.AddDays(-1)),
                new("evt-6", "shp-2", ShipmentStatuses.PickedUp, "Rotterdam Cold Hub", "Dispatched in temperature-controlled unit.", now.AddHours(-5)),
                new("evt-7", "shp-2", ShipmentStatuses.OutForDelivery, "Amsterdam South Courier Hub", "Out for priority clinical delivery.", now.AddMinutes(-30))
            });

        var s3 = new Shipment(
            "shp-3",
            "LP-1198-NY",
            "Global Aviation Spares Ltd",
            "JFK Cargo Bay 7, New York, NY",
            "Skyline Aerospace Maintenance",
            "Aerospace Park, Dallas, TX",
            "New York, NY",
            "Dallas, TX",
            ShipmentStatuses.Delivered,
            ShipmentPriorities.Overnight,
            820.0,
            now.AddHours(-4),
            "drv-1",
            "vh-1",
            now.AddDays(-3),
            now.AddHours(-4),
            new List<TrackingEvent>
            {
                new("evt-8", "shp-3", ShipmentStatuses.Pending, "JFK Air Cargo Hub", "Waybill created.", now.AddDays(-3)),
                new("evt-9", "shp-3", ShipmentStatuses.InTransit, "Air Transit JFK -> DFW", "Cargo flight arrival logged.", now.AddDays(-2)),
                new("evt-10", "shp-3", ShipmentStatuses.OutForDelivery, "Dallas Inland Multi-Modal Hub", "Courier on final dispatch leg.", now.AddHours(-7)),
                new("evt-11", "shp-3", ShipmentStatuses.Delivered, "Skyline Aerospace Maintenance", "Signed by Receiving Inspector J. Harper.", now.AddHours(-4))
            });

        var s4 = new Shipment(
            "shp-4",
            "LP-7731-SG",
            "Pacific Lithium Technologies",
            "Jurong Industrial Est, Singapore",
            "Renewable Grid Storage Systems",
            "Port of Rotterdam Terminal, NL",
            "Singapore, SG",
            "Rotterdam, NL",
            ShipmentStatuses.Pending,
            ShipmentPriorities.Standard,
            12400.0,
            now.AddDays(14),
            null,
            null,
            now.AddHours(-8),
            now.AddHours(-8),
            new List<TrackingEvent>
            {
                new("evt-12", "shp-4", ShipmentStatuses.Pending, "Singapore Changi Freight Gateway", "Containerized hazmat verified; awaiting vessel assignment.", now.AddHours(-8))
            });

        var s5 = new Shipment(
            "shp-5",
            "LP-9032-DE",
            "Bavaria Precision Optics GmbH",
            "Industriestrasse 14, Munich, DE",
            "AeroLens Satellites AG",
            "Flughafen Fracht 4, Frankfurt, DE",
            "Munich, DE",
            "Frankfurt, DE",
            ShipmentStatuses.Delayed,
            ShipmentPriorities.Express,
            410.0,
            now.AddHours(8),
            "drv-3",
            "vh-5",
            now.AddDays(-1),
            now.AddHours(-2),
            new List<TrackingEvent>
            {
                new("evt-13", "shp-5", ShipmentStatuses.PickedUp, "Munich High-Tech Campus", "Pickup confirmed.", now.AddDays(-1)),
                new("evt-14", "shp-5", ShipmentStatuses.Delayed, "A3 Autobahn Waypoint", "Severe weather advisory delay (+4h). Cargo secure.", now.AddHours(-2))
            });

        _shipments[s1.Id] = s1;
        _shipments[s2.Id] = s2;
        _shipments[s3.Id] = s3;
        _shipments[s4.Id] = s4;
        _shipments[s5.Id] = s5;
    }

    public IReadOnlyList<Shipment> GetShipments(string? status = null, string? search = null)
    {
        var query = _shipments.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(s =>
                s.TrackingNumber.ToLowerInvariant().Contains(term) ||
                s.SenderName.ToLowerInvariant().Contains(term) ||
                s.RecipientName.ToLowerInvariant().Contains(term) ||
                s.Origin.ToLowerInvariant().Contains(term) ||
                s.Destination.ToLowerInvariant().Contains(term));
        }

        return query.OrderByDescending(s => s.UpdatedAt).ToList();
    }

    public Shipment? GetShipmentById(string id) =>
        _shipments.TryGetValue(id, out var shipment) ? shipment : null;

    public Shipment? GetShipmentByTrackingNumber(string trackingNumber) =>
        _shipments.Values.FirstOrDefault(s => s.TrackingNumber.Equals(trackingNumber.Trim(), StringComparison.OrdinalIgnoreCase));

    public Shipment CreateShipment(Shipment shipment)
    {
        _shipments[shipment.Id] = shipment;
        return shipment;
    }

    public Shipment? UpdateShipmentStatus(string id, string newStatus, string location, string description)
    {
        if (!_shipments.TryGetValue(id, out var current))
            return null;

        var newEvent = new TrackingEvent(
            $"evt-{Guid.NewGuid().ToString("N")[..8]}",
            id,
            newStatus,
            location,
            description,
            DateTimeOffset.UtcNow);

        var updatedEvents = new List<TrackingEvent>(current.Events) { newEvent };
        var updated = current with
        {
            Status = newStatus,
            UpdatedAt = DateTimeOffset.UtcNow,
            Events = updatedEvents
        };

        _shipments[id] = updated;
        return updated;
    }

    public Shipment? AdvanceShipmentSimulation(string id)
    {
        if (!_shipments.TryGetValue(id, out var current))
            return null;

        string nextStatus;
        string location;
        string desc;

        switch (current.Status)
        {
            case ShipmentStatuses.Pending:
                nextStatus = ShipmentStatuses.PickedUp;
                location = current.Origin + " Hub";
                desc = "Cargo picked up and scanned onto vehicle.";
                break;
            case ShipmentStatuses.PickedUp:
                nextStatus = ShipmentStatuses.InTransit;
                location = "Regional Highway Waypoint";
                desc = "In transit along primary logistical corridor.";
                break;
            case ShipmentStatuses.InTransit:
                nextStatus = ShipmentStatuses.OutForDelivery;
                location = current.Destination + " Delivery Center";
                desc = "Loaded onto local courier van for last-mile delivery.";
                break;
            case ShipmentStatuses.OutForDelivery:
                nextStatus = ShipmentStatuses.Delivered;
                location = current.RecipientAddress;
                desc = $"Delivered safely and acknowledged by recipient ({current.RecipientName}).";
                break;
            case ShipmentStatuses.Delayed:
                nextStatus = ShipmentStatuses.InTransit;
                location = "Weather Clear Waypoint";
                desc = "Weather clear; transit resumed to destination.";
                break;
            default:
                // Cycle back to InTransit for demonstration
                nextStatus = ShipmentStatuses.InTransit;
                location = current.Origin + " Central Express Route";
                desc = "Simulated return leg for testing.";
                break;
        }

        return UpdateShipmentStatus(id, nextStatus, location, desc);
    }

    public IReadOnlyList<Vehicle> GetVehicles() => _vehicles.Values.ToList();

    public Vehicle? GetVehicleById(string id) =>
        _vehicles.TryGetValue(id, out var v) ? v : null;

    public IReadOnlyList<Driver> GetDrivers() => _drivers.Values.ToList();

    public Driver? GetDriverById(string id) =>
        _drivers.TryGetValue(id, out var d) ? d : null;

    public bool AssignDriverToVehicle(string vehicleId, string driverId)
    {
        if (!_vehicles.TryGetValue(vehicleId, out var vehicle) ||
            !_drivers.TryGetValue(driverId, out var driver))
        {
            return false;
        }

        _vehicles[vehicleId] = vehicle with { AssignedDriverId = driverId, Status = "OnRoute" };
        _drivers[driverId] = driver with { AssignedVehicleId = vehicleId, Status = "OnDelivery" };
        return true;
    }

    public IReadOnlyList<Warehouse> GetWarehouses() => _warehouses.Values.ToList();

    public IReadOnlyList<InventoryItem> GetInventory(string? warehouseId = null)
    {
        var items = _inventory.Values.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(warehouseId))
        {
            items = items.Where(i => i.WarehouseId.Equals(warehouseId, StringComparison.OrdinalIgnoreCase));
        }
        return items.ToList();
    }

    public UserProfile? Authenticate(string email, string password)
    {
        var key = email.Trim().ToLowerInvariant();
        if (_users.TryGetValue(key, out var user))
        {
            // For testing and demo, accept "password123" or standard password
            if (password == "password123" || password == "admin" || password == "demo")
            {
                return user;
            }
        }
        return null;
    }

    public UserProfile? GetUserByToken(string token)
    {
        return _users.Values.FirstOrDefault(u => u.Token.Equals(token, StringComparison.OrdinalIgnoreCase));
    }

    public LogisticsAnalytics GetAnalytics()
    {
        var shipments = _shipments.Values;
        var total = shipments.Count;
        var activeInTransit = shipments.Count(s => s.Status == ShipmentStatuses.InTransit || s.Status == ShipmentStatuses.OutForDelivery);
        var delivered = shipments.Count(s => s.Status == ShipmentStatuses.Delivered);
        var delayed = shipments.Count(s => s.Status == ShipmentStatuses.Delayed);

        var vehicles = _vehicles.Values;
        var activeVehicles = vehicles.Count(v => v.Status == "OnRoute");
        var fleetUtil = vehicles.Count > 0 ? Math.Round((double)activeVehicles / vehicles.Count * 100, 1) : 0;

        var warehouses = _warehouses.Values;
        var avgOccupancy = warehouses.Count > 0 ? Math.Round(warehouses.Average(w => w.OccupancyPct), 1) : 0;

        return new LogisticsAnalytics(
            TotalShipments: total,
            ActiveInTransit: activeInTransit,
            DeliveredToday: delivered,
            DelayedAlerts: delayed,
            FleetUtilizationPct: fleetUtil,
            WarehouseOccupancyAvgPct: avgOccupancy,
            OnTimeDeliveryRatePct: 98.7);
    }
}

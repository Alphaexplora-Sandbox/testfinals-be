using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TestfinalsBackend.Domain;
using Xunit;

namespace TestfinalsBackend.Tests;

public class LogisticsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public LogisticsApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Auth_Login_WithValidCredentials_ReturnsOkAndUserProfile()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("dispatcher@logipulse.io", "password123"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserProfile>();
        Assert.NotNull(user);
        Assert.Equal("dispatcher@logipulse.io", user!.Email);
        Assert.Equal("Dispatcher", user.Role);
        Assert.False(string.IsNullOrWhiteSpace(user.Token));
    }

    [Fact]
    public async Task Auth_Login_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("dispatcher@logipulse.io", "wrongpassword"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Auth_Me_WithValidToken_ReturnsUserProfile()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "token-dispatcher-session-xyz");

        var response = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<UserProfile>();
        Assert.NotNull(user);
        Assert.Equal("dispatcher@logipulse.io", user!.Email);
    }

    [Fact]
    public async Task Auth_Me_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Auth_Me_WithInvalidToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "non-existent-token");

        var response = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Shipments_GetAll_ReturnsSeededShipments()
    {
        var client = _factory.CreateClient();
        var shipments = await client.GetFromJsonAsync<List<Shipment>>("/api/v1/shipments");

        Assert.NotNull(shipments);
        Assert.True(shipments!.Count >= 5);
        Assert.Contains(shipments, s => s.TrackingNumber == "LP-8924-XQ");
    }

    [Fact]
    public async Task Shipments_GetAll_WithFilterAndSearch_ReturnsFiltered()
    {
        var client = _factory.CreateClient();
        var filteredByStatus = await client.GetFromJsonAsync<List<Shipment>>("/api/v1/shipments?status=In%20Transit");
        Assert.NotNull(filteredByStatus);
        Assert.All(filteredByStatus!, s => Assert.Equal(ShipmentStatuses.InTransit, s.Status));

        var filteredBySearch = await client.GetFromJsonAsync<List<Shipment>>("/api/v1/shipments?search=Austin");
        Assert.NotNull(filteredBySearch);
        Assert.NotEmpty(filteredBySearch!);
    }

    [Fact]
    public async Task Shipments_GetById_ReturnsMatchingShipmentOrNotFound()
    {
        var client = _factory.CreateClient();
        var found = await client.GetAsync("/api/v1/shipments/shp-1");
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);

        var notFound = await client.GetAsync("/api/v1/shipments/non-existent-id");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task Shipments_TrackByCode_ReturnsMatchingShipmentOrNotFound()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/shipments/track/LP-8924-XQ");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var shipment = await response.Content.ReadFromJsonAsync<Shipment>();
        Assert.NotNull(shipment);
        Assert.Equal("LP-8924-XQ", shipment!.TrackingNumber);
        Assert.NotEmpty(shipment.Events);

        var notFound = await client.GetAsync("/api/v1/shipments/track/LP-NON-EXISTENT");
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task Shipments_CreateNew_ReturnsCreatedAndPersists()
    {
        var client = _factory.CreateClient();
        var dto = new CreateShipmentDto(
            "Global Test Corp",
            "123 Industry Rd, Seattle, WA",
            "Pacific Retailers",
            "789 Market St, San Francisco, CA",
            "Seattle, WA",
            "San Francisco, CA",
            ShipmentPriorities.Express,
            250.0,
            2,
            null,
            null);

        var response = await client.PostAsJsonAsync("/api/v1/shipments", dto);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<Shipment>();
        Assert.NotNull(created);
        Assert.StartsWith("LP-", created!.TrackingNumber);
        Assert.Equal("Seattle, WA", created.Origin);

        // Verify retrieval
        var trackResponse = await client.GetAsync($"/api/v1/shipments/track/{created.TrackingNumber}");
        Assert.Equal(HttpStatusCode.OK, trackResponse.StatusCode);
    }

    [Fact]
    public async Task Shipments_UpdateStatus_UpdatesAndReturnsNotFoundWhenMissing()
    {
        var client = _factory.CreateClient();
        var updateDto = new UpdateStatusDto(ShipmentStatuses.Delayed, "Port of Seattle", "Weather delay encountered");

        var response = await client.PatchAsJsonAsync("/api/v1/shipments/shp-1/status", updateDto);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<Shipment>();
        Assert.NotNull(updated);
        Assert.Equal(ShipmentStatuses.Delayed, updated!.Status);

        var notFound = await client.PatchAsJsonAsync("/api/v1/shipments/shp-missing/status", updateDto);
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task Shipments_Simulate_AdvancesStatusAndReturnsNotFoundWhenMissing()
    {
        var client = _factory.CreateClient();
        // shp-4 starts as Pending -> PickedUp
        var response = await client.PostAsync("/api/v1/shipments/shp-4/simulate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<Shipment>();
        Assert.NotNull(updated);
        Assert.Equal(ShipmentStatuses.PickedUp, updated!.Status);

        // Advance PickedUp -> InTransit
        var response2 = await client.PostAsync("/api/v1/shipments/shp-4/simulate", null);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var updated2 = await response2.Content.ReadFromJsonAsync<Shipment>();
        Assert.Equal(ShipmentStatuses.InTransit, updated2!.Status);

        // Non-existent shipment simulate returns NotFound
        var notFound = await client.PostAsync("/api/v1/shipments/shp-nonexistent/simulate", null);
        Assert.Equal(HttpStatusCode.NotFound, notFound.StatusCode);
    }

    [Fact]
    public async Task Analytics_ReturnsValidMetrics()
    {
        var client = _factory.CreateClient();
        var analytics = await client.GetFromJsonAsync<LogisticsAnalytics>("/api/v1/analytics/overview");

        Assert.NotNull(analytics);
        Assert.True(analytics!.TotalShipments > 0);
        Assert.True(analytics.OnTimeDeliveryRatePct > 90);
    }

    [Fact]
    public async Task FleetAndWarehouses_ReturnData()
    {
        var client = _factory.CreateClient();

        var vehicles = await client.GetFromJsonAsync<List<Vehicle>>("/api/v1/fleet/vehicles");
        Assert.NotNull(vehicles);
        Assert.NotEmpty(vehicles!);

        var drivers = await client.GetFromJsonAsync<List<Driver>>("/api/v1/fleet/drivers");
        Assert.NotNull(drivers);
        Assert.NotEmpty(drivers!);

        var warehouses = await client.GetFromJsonAsync<List<Warehouse>>("/api/v1/warehouses");
        Assert.NotNull(warehouses);
        Assert.NotEmpty(warehouses!);
    }

    [Fact]
    public async Task Fleet_Dispatch_AssignsDriverOrReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var successDto = new DispatchDto("vh-4", "drv-4");
        var successResponse = await client.PostAsJsonAsync("/api/v1/fleet/dispatch", successDto);
        Assert.Equal(HttpStatusCode.OK, successResponse.StatusCode);

        var failDto = new DispatchDto("veh-missing", "drv-missing");
        var failResponse = await client.PostAsJsonAsync("/api/v1/fleet/dispatch", failDto);
        Assert.Equal(HttpStatusCode.BadRequest, failResponse.StatusCode);
    }

    [Fact]
    public async Task Inventory_ReturnsAllOrFilteredByWarehouse()
    {
        var client = _factory.CreateClient();

        var allInventory = await client.GetFromJsonAsync<List<InventoryItem>>("/api/v1/inventory");
        Assert.NotNull(allInventory);
        Assert.NotEmpty(allInventory!);

        var filtered = await client.GetFromJsonAsync<List<InventoryItem>>("/api/v1/inventory?warehouseId=wh-chi");
        Assert.NotNull(filtered);
        Assert.NotEmpty(filtered!);
        Assert.All(filtered!, i => Assert.Equal("wh-chi", i.WarehouseId));
    }

    [Fact]
    public void ServiceInfo_ContainsCorrectName()
    {
        Assert.Equal("testfinals-backend", ServiceInfo.Name);
    }
}

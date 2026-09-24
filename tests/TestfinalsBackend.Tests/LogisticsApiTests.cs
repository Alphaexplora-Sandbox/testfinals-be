using System.Net;
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
    public async Task Shipments_GetAll_ReturnsSeededShipments()
    {
        var client = _factory.CreateClient();
        var shipments = await client.GetFromJsonAsync<List<Shipment>>("/api/v1/shipments");

        Assert.NotNull(shipments);
        Assert.True(shipments!.Count >= 5);
        Assert.Contains(shipments, s => s.TrackingNumber == "LP-8924-XQ");
    }

    [Fact]
    public async Task Shipments_TrackByCode_ReturnsMatchingShipment()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/shipments/track/LP-8924-XQ");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var shipment = await response.Content.ReadFromJsonAsync<Shipment>();
        Assert.NotNull(shipment);
        Assert.Equal("LP-8924-XQ", shipment!.TrackingNumber);
        Assert.NotEmpty(shipment.Events);
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
    public async Task Shipments_Simulate_AdvancesStatus()
    {
        var client = _factory.CreateClient();
        // shp-4 starts as Pending
        var response = await client.PostAsync("/api/v1/shipments/shp-4/simulate", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<Shipment>();
        Assert.NotNull(updated);
        Assert.Equal(ShipmentStatuses.PickedUp, updated!.Status);
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
}

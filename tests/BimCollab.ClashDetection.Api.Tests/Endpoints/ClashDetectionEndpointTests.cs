using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BimCollab.ClashDetection.Api.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace BimCollab.ClashDetection.Api.Tests.Endpoints;

public class ClashDetectionEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _client;

    public ClashDetectionEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DetectClashes_WithValidData_Returns200WithClashes()
    {
        var request = new DetectClashesRequest
        {
            SitePlan = new SitePlanRequest(1000, 500),
            Buildings =
            [
                new BuildingRequest("Building A", "Office", 200, 200, 900, 400),
            ]
        };

        var response = await _client.PostAsJsonAsync("/api/clash-detection/detect", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ClashDetectionResponse>(json, JsonOptions);
        Assert.NotNull(result);
        Assert.NotEmpty(result.Clashes);
        Assert.Contains(result.Clashes, c => c.Type == "BoundaryViolation");
    }

    [Fact]
    public async Task DetectClashes_WithNoClashData_Returns200WithEmptyList()
    {
        var request = new DetectClashesRequest
        {
            SitePlan = new SitePlanRequest(1000, 500),
            Buildings =
            [
                new BuildingRequest("Building A", "Office", 100, 100, 0, 0),
                new BuildingRequest("Building B", "Office", 100, 100, 500, 0),
            ]
        };

        var response = await _client.PostAsJsonAsync("/api/clash-detection/detect", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ClashDetectionResponse>(json, JsonOptions);
        Assert.NotNull(result);
        Assert.Empty(result.Clashes);
    }

    [Fact]
    public async Task DetectClashes_WithAssessmentExampleDataset_ReturnsExpectedClashes()
    {
        var request = new DetectClashesRequest
        {
            SitePlan = new SitePlanRequest(1000, 500),
            Buildings =
            [
                new BuildingRequest("School A", "School", 100, 100, 0, 0),
                new BuildingRequest("Office B", "Office", 100, 100, 200, 0),
                new BuildingRequest("Nightclub C", "Nightclub", 100, 100, 50, 50),
                new BuildingRequest("Stadium D", "Stadium", 200, 200, 800, 350),
                new BuildingRequest("Residence E", "ResidentialBuilding", 80, 80, 60, 200),
            ]
        };

        var response = await _client.PostAsJsonAsync("/api/clash-detection/detect", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ClashDetectionResponse>(json, JsonOptions);
        Assert.NotNull(result);

        // Stadium D extends beyond site boundary: y=350, length=200, top=550 > 500
        Assert.Contains(result.Clashes, c =>
            c.Type == "BoundaryViolation" &&
            c.BuildingNames.Contains("Stadium D"));

        // School A [0,100]x[0,100] and Nightclub C [50,150]x[50,150] overlap
        Assert.Contains(result.Clashes, c =>
            c.Type == "Overlap" &&
            c.BuildingNames.Contains("School A") &&
            c.BuildingNames.Contains("Nightclub C"));

        // Nightclub C [50,150]x[50,150] and School A [0,100]x[0,100] zoning violation
        Assert.Contains(result.Clashes, c =>
            c.Type == "ZoningViolation" &&
            c.BuildingNames.Contains("Nightclub C") &&
            c.BuildingNames.Contains("School A"));
    }

    [Fact]
    public async Task DetectClashes_WithEmptyBuildingName_Returns400()
    {
        var request = new DetectClashesRequest
        {
            SitePlan = new SitePlanRequest(1000, 500),
            Buildings =
            [
                new BuildingRequest("", "Office", 100, 100, 0, 0),
            ]
        };

        var response = await _client.PostAsJsonAsync("/api/clash-detection/detect", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Name", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectClashes_WithDuplicateNames_Returns400()
    {
        var request = new DetectClashesRequest
        {
            SitePlan = new SitePlanRequest(1000, 500),
            Buildings =
            [
                new BuildingRequest("Building A", "Office", 100, 100, 0, 0),
                new BuildingRequest("Building A", "School", 100, 100, 200, 0),
            ]
        };

        var response = await _client.PostAsJsonAsync("/api/clash-detection/detect", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("duplicate", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectClashes_WithZeroBuildingWidth_Returns400()
    {
        var request = new DetectClashesRequest
        {
            SitePlan = new SitePlanRequest(1000, 500),
            Buildings =
            [
                new BuildingRequest("Building A", "Office", 0, 100, 0, 0),
            ]
        };

        var response = await _client.PostAsJsonAsync("/api/clash-detection/detect", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Width", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectClashes_WithInvalidBuildingType_Returns400()
    {
        var request = new DetectClashesRequest
        {
            SitePlan = new SitePlanRequest(1000, 500),
            Buildings =
            [
                new BuildingRequest("Building A", "InvalidType", 100, 100, 0, 0),
            ]
        };

        var response = await _client.PostAsJsonAsync("/api/clash-detection/detect", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Type", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectClashes_ValidationError_ReturnsProblemDetails()
    {
        var request = new DetectClashesRequest
        {
            SitePlan = new SitePlanRequest(0, 500),
            Buildings =
            [
                new BuildingRequest("Building A", "Office", 100, 100, 0, 0),
            ]
        };

        var response = await _client.PostAsJsonAsync("/api/clash-detection/detect", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var problemDetails = JsonSerializer.Deserialize<HttpValidationProblemDetails>(json, JsonOptions);
        Assert.NotNull(problemDetails);
        Assert.Equal(400, problemDetails.Status);
        Assert.NotEmpty(problemDetails.Errors);
    }

    [Fact]
    public async Task DetectClashes_WithEmptyJsonBody_Returns400()
    {
        var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/clash-detection/detect", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("invalid", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectClashes_WithMissingSitePlan_Returns400()
    {
        var content = new StringContent(
            """{"buildings":[{"name":"A","type":"Office","width":100,"length":100,"x":0,"y":0}]}""",
            System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/clash-detection/detect", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DetectClashes_WithMalformedJson_Returns400()
    {
        var content = new StringContent("not json at all", System.Text.Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/clash-detection/detect", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

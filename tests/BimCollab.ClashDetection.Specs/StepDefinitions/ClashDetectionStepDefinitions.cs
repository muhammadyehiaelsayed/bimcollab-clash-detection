using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BimCollab.ClashDetection.Api.Contracts;
using BimCollab.ClashDetection.Specs.Support;
using Reqnroll;

namespace BimCollab.ClashDetection.Specs.StepDefinitions;

[Binding]
public class ClashDetectionStepDefinitions : IClassFixture<TestWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly TestWebApplicationFactory _factory;
    private double _sitePlanWidth;
    private double _sitePlanLength;
    private readonly List<BuildingRequest> _buildings = [];
    private HttpResponseMessage? _response;
    private ClashDetectionResponse? _result;

    public ClashDetectionStepDefinitions(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Given("a site plan with width {int} and length {int}")]
    public void GivenASitePlanWithWidthAndLength(int width, int length)
    {
        _sitePlanWidth = width;
        _sitePlanLength = length;
    }

    [Given("the following buildings exist on the site plan:")]
    public void GivenTheFollowingBuildingsExistOnTheSitePlan(Table table)
    {
        foreach (var row in table.Rows)
        {
            _buildings.Add(new BuildingRequest(
                Name: row["Name"],
                Type: row["Type"],
                Width: double.Parse(row["Width"]),
                Length: double.Parse(row["Length"]),
                X: double.Parse(row["X"]),
                Y: double.Parse(row["Y"])));
        }
    }

    [When("I run clash detection")]
    public async Task WhenIRunClashDetection()
    {
        var client = _factory.CreateClient();
        var request = new DetectClashesRequest
        {
            SitePlan = new SitePlanRequest(_sitePlanWidth, _sitePlanLength),
            Buildings = _buildings
        };

        _response = await client.PostAsJsonAsync("/api/clash-detection/detect", request);
        Assert.Equal(HttpStatusCode.OK, _response.StatusCode);

        var json = await _response.Content.ReadAsStringAsync();
        _result = JsonSerializer.Deserialize<ClashDetectionResponse>(json, JsonOptions);
        Assert.NotNull(_result);
    }

    [Then("no clashes should be detected")]
    public void ThenNoClashesShouldBeDetected()
    {
        Assert.NotNull(_result);
        Assert.Empty(_result.Clashes);
    }

    [Then("a clash of type {string} should be detected")]
    public void ThenAClashOfTypeShouldBeDetected(string clashType)
    {
        Assert.NotNull(_result);
        Assert.Contains(_result.Clashes, c => c.Type == clashType);
    }

    [Then("a clash of type {string} with severity {string} should be detected for building {string}")]
    public void ThenAClashOfTypeWithSeverityShouldBeDetectedForBuilding(string clashType, string severity, string buildingName)
    {
        Assert.NotNull(_result);
        Assert.Contains(_result.Clashes, c =>
            c.Type == clashType &&
            c.Severity == severity &&
            c.BuildingNames.Contains(buildingName));
    }

    [Then("a clash of type {string} with severity {string} should be detected for buildings {string} and {string}")]
    public void ThenAClashOfTypeWithSeverityShouldBeDetectedForBuildings(string clashType, string severity, string building1, string building2)
    {
        Assert.NotNull(_result);
        Assert.Contains(_result.Clashes, c =>
            c.Type == clashType &&
            c.Severity == severity &&
            c.BuildingNames.Contains(building1) &&
            c.BuildingNames.Contains(building2));
    }

    [Then("{int} clashes should be detected")]
    public void ThenNClashesShouldBeDetected(int count)
    {
        Assert.NotNull(_result);
        Assert.Equal(count, _result.Clashes.Count);
    }

    [Then("no clash of type {string} should be detected for buildings {string} and {string}")]
    public void ThenNoClashOfTypeShouldBeDetectedForBuildings(string clashType, string building1, string building2)
    {
        Assert.NotNull(_result);
        Assert.DoesNotContain(_result.Clashes, c =>
            c.Type == clashType &&
            c.BuildingNames.Contains(building1) &&
            c.BuildingNames.Contains(building2));
    }
}

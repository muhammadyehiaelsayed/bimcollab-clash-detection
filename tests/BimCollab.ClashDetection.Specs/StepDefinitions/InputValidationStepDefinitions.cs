using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BimCollab.ClashDetection.Api.Contracts;
using BimCollab.ClashDetection.Specs.Support;
using Microsoft.AspNetCore.Http;
using Reqnroll;

namespace BimCollab.ClashDetection.Specs.StepDefinitions;

[Binding]
public class InputValidationStepDefinitions : IClassFixture<TestWebApplicationFactory>
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
    private string? _responseBody;

    public InputValidationStepDefinitions(TestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Given("a site plan of width {int} and length {int}")]
    public void GivenASitePlanOfWidthAndLength(int width, int length)
    {
        _sitePlanWidth = width;
        _sitePlanLength = length;
    }

    [Given("a building with name {string} type {string} width {double} length {double} at position {double}, {double}")]
    public void GivenABuildingWithAttributes(string name, string type, double width, double length, double x, double y)
    {
        _buildings.Add(new BuildingRequest(name, type, width, length, x, y));
    }

    [When("I submit the clash detection request")]
    public async Task WhenISubmitTheClashDetectionRequest()
    {
        var client = _factory.CreateClient();
        var request = new DetectClashesRequest
        {
            SitePlan = new SitePlanRequest(_sitePlanWidth, _sitePlanLength),
            Buildings = _buildings
        };

        _response = await client.PostAsJsonAsync("/api/clash-detection/detect", request);
        _responseBody = await _response.Content.ReadAsStringAsync();
    }

    [Then("I should receive a validation error")]
    public void ThenIShouldReceiveAValidationError()
    {
        Assert.NotNull(_response);
        Assert.Equal(HttpStatusCode.BadRequest, _response.StatusCode);
    }

    [Then("the error should mention {string}")]
    public void ThenTheErrorShouldMention(string expectedText)
    {
        Assert.NotNull(_responseBody);
        Assert.Contains(expectedText, _responseBody, StringComparison.OrdinalIgnoreCase);
    }
}

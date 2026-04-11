using BimCollab.ClashDetection.Api.Contracts;
using BimCollab.ClashDetection.Application.ClashDetection.Commands;
using MediatR;

namespace BimCollab.ClashDetection.Api.Endpoints;

public static class ClashDetectionEndpoints
{
    public static RouteGroupBuilder MapClashDetectionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/clash-detection")
            .WithTags("Clash Detection");

        group.MapPost("/detect", async (DetectClashesRequest request, IMediator mediator) =>
        {
            var command = new DetectClashesCommand
            {
                SitePlan = new SitePlanDto(request.SitePlan.Width, request.SitePlan.Length),
                Buildings = request.Buildings
                    .Select(b => new BuildingDto(b.Name, b.Type, b.Width, b.Length, b.X, b.Y))
                    .ToList()
            };

            var result = await mediator.Send(command);

            var response = new ClashDetectionResponse(
                result.Clashes
                    .Select(c => new ClashResponse(c.BuildingNames, c.Type, c.Severity, c.Description))
                    .ToList());

            return Results.Ok(response);
        })
        .WithName("DetectClashes")
        .WithDescription("Detect clashes in a building site plan")
        .Produces<ClashDetectionResponse>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return group;
    }
}

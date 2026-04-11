using BimCollab.ClashDetection.Application.ClashDetection.Commands;
using BimCollab.ClashDetection.Domain.Enums;
using FluentValidation;

namespace BimCollab.ClashDetection.Application.ClashDetection.Validators;

public class DetectClashesCommandValidator : AbstractValidator<DetectClashesCommand>
{
    public DetectClashesCommandValidator()
    {
        RuleFor(x => x.SitePlan.Width)
            .GreaterThan(0)
            .WithMessage("Site plan Width must be greater than 0.");

        RuleFor(x => x.SitePlan.Length)
            .GreaterThan(0)
            .WithMessage("Site plan Length must be greater than 0.");

        RuleForEach(x => x.Buildings)
            .SetValidator(new BuildingDtoValidator());

        RuleFor(x => x.Buildings)
            .Must(buildings => buildings.Count <= 500)
            .WithMessage("A maximum of 500 buildings is allowed per request.");

        RuleFor(x => x.Buildings)
            .Must(buildings => buildings
                .Select(b => b.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
                .All(g => g.Count() == 1))
            .WithMessage("Building names must be unique; duplicate names are not allowed.");
    }
}

internal class BuildingDtoValidator : AbstractValidator<BuildingDto>
{
    private static readonly HashSet<string> ValidBuildingTypes =
        Enum.GetNames<BuildingType>().ToHashSet(StringComparer.Ordinal);

    public BuildingDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Building Name must not be empty.");

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Building Type must not be empty.");

        RuleFor(x => x.Type)
            .Must(type => ValidBuildingTypes.Contains(type))
            .When(x => !string.IsNullOrWhiteSpace(x.Type))
            .WithMessage(x => $"Building Type '{x.Type}' is not valid. Valid types are: {string.Join(", ", ValidBuildingTypes)}.");

        RuleFor(x => x.Width)
            .GreaterThan(0)
            .WithMessage("Building Width must be greater than 0.");

        RuleFor(x => x.Length)
            .GreaterThan(0)
            .WithMessage("Building Length must be greater than 0.");

        RuleFor(x => x.X)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Building X position must be greater than or equal to 0.");

        RuleFor(x => x.Y)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Building Y position must be greater than or equal to 0.");
    }
}

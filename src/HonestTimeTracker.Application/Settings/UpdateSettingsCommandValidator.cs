using FluentValidation;

namespace HonestTimeTracker.Application.Settings;

public class UpdateSettingsCommandValidator : AbstractValidator<UpdateSettingsCommand>
{
    public UpdateSettingsCommandValidator()
    {
        RuleFor(x => x.DailyWorkHours)
            .GreaterThan(0).WithMessage("Daily work hours must be greater than 0.")
            .LessThanOrEqualTo(24).WithMessage("Daily work hours cannot exceed 24.");

        When(x => x.DefaultTaskPlannedHours.HasValue, () =>
        {
            RuleFor(x => x.DefaultTaskPlannedHours!.Value)
                .GreaterThan(0).WithMessage("Default task planned hours must be greater than 0.")
                .LessThanOrEqualTo(24).WithMessage("Default task planned hours cannot exceed 24.");
        });
    }
}

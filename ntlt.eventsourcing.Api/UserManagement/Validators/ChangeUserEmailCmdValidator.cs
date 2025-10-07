using FluentValidation;
using ntlt.eventsourcing.Api.UserManagement.Cmd;

namespace ntlt.eventsourcing.Api.UserManagement.Validators;

public class ChangeUserEmailCmdValidator : AbstractValidator<ChangeUserEmailCmd>
{
    public ChangeUserEmailCmdValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("UserId is required");

        RuleFor(x => x.NewEmail)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}

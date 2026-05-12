using FluentValidation;
using LMS.Application.Features.Organization.Employees.DTOs;

namespace LMS.Application.Features.Organization.Employees.Validators;

public class InviteUserRequestValidator : AbstractValidator<InviteUserRequest>
{
    public InviteUserRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(50).WithMessage("First name is required.");
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(50).WithMessage("Last name is required.");
        RuleFor(x => x.Gender).IsInEnum().WithMessage("Valid gender is required.");
        RuleFor(x => x.RoleExternalId).NotEmpty().WithMessage("Role is required.");
        RuleFor(x => x.ManagerExternalId).NotEmpty().WithMessage("Manager is required.");
        RuleFor(x => x.DepartmentExternalId).NotEmpty().WithMessage("Department is required.");
    }
}

public class UserSetPasswordValidator : AbstractValidator<SetPasswordRequest>
{
    public UserSetPasswordValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("Token is required");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Please confirm your password.")
            .Must((request, confirmPassword) => confirmPassword == request.Password)
            .WithMessage("Passwords do not match.");
    }
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("A valid email address is required.");
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().WithMessage("Token is required");
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one number.")
            .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Please confirm your password.")
            .Must((request, confirmPassword) => confirmPassword == request.Password)
            .WithMessage("Passwords do not match.");
    }
}

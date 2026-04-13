using FluentValidation;
using LMS.Application.Features.Auth.DTOs;

namespace LMS.Application.Features.Auth.Validators;

public class RegisterCompanyValidator : AbstractValidator<RegisterCompanyRequest>
{
    public RegisterCompanyValidator()
    {
        RuleFor(x => x.RegistrationToken).NotEmpty().WithMessage("Registration token is required");
        RuleFor(x => x.CompanyName).NotEmpty().MinimumLength(3).MaximumLength(100).WithMessage("Company name is required.");
        RuleFor(x => x.Subdomain).NotEmpty().WithMessage("Subdomain is required.");
        RuleFor(x => x.AdminEmail).NotEmpty().EmailAddress().WithMessage("Admin email is required and must be a valid email address.");
        RuleFor(x => x.AdminPassword)
        .NotEmpty().WithMessage("Password is required.")
        .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
        .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
        .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
        .Matches("[0-9]").WithMessage("Password must contain at least one number.")
        .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Email is required");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required");
    }
}

// public class UserInviteValidator : AbstractValidator<InviteUserRequest>
// {
//     public UserInviteValidator()
//     {
//         RuleFor(x => x.
//     }
// }

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

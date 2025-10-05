# Add Validator

Erstelle einen FluentValidation Validator für ein existierendes Command.

## Input

User gibt Command-Namen an (z.B. `CreateUserCmd`).

## Workflow

1. **Finde Command**: Lese `{Domain}/Cmd/{CommandName}.cs`
2. **Analysiere Properties**: Welche Properties brauchen Validation?
3. **Erstelle Validator**: `{Domain}/Validators/{CommandName}Validator.cs`

## Validator Template

```csharp
using FluentValidation;
using MartenAkkaTests.Api.{Domain}.Cmd;

namespace MartenAkkaTests.Api.{Domain}.Validators;

/// <summary>
/// Validator for {CommandName}.
/// </summary>
public class {CommandName}Validator : AbstractValidator<{CommandName}>
{
    public {CommandName}Validator()
    {
        // Für jeden Property-Typ passende Regeln
    }
}
```

## Validation Rules Katalog

### String Properties

```csharp
// Required String
RuleFor(x => x.UserName)
    .NotEmpty().WithMessage("Username is required")
    .MinimumLength(3).WithMessage("Username must be at least 3 characters")
    .MaximumLength(50).WithMessage("Username cannot exceed 50 characters");

// Email
RuleFor(x => x.Email)
    .NotEmpty().WithMessage("Email is required")
    .EmailAddress().WithMessage("Invalid email format")
    .MaximumLength(100).WithMessage("Email cannot exceed 100 characters");

// Pattern Match
RuleFor(x => x.UserName)
    .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Username can only contain letters, numbers, underscores, and hyphens");

// URL
RuleFor(x => x.Website)
    .Must(BeValidUrl).When(x => !string.IsNullOrEmpty(x.Website))
    .WithMessage("Invalid URL format");

private bool BeValidUrl(string url)
{
    return Uri.TryCreate(url, UriKind.Absolute, out _);
}

// Enum String
RuleFor(x => x.Status)
    .Must(x => new[] { "active", "inactive", "pending" }.Contains(x?.ToLower()))
    .WithMessage("Status must be: active, inactive, or pending");
```

### Numeric Properties

```csharp
// Required Number
RuleFor(x => x.Age)
    .GreaterThan(0).WithMessage("Age must be positive")
    .LessThanOrEqualTo(150).WithMessage("Age seems unrealistic");

// Range
RuleFor(x => x.Quantity)
    .InclusiveBetween(1, 1000).WithMessage("Quantity must be between 1 and 1000");

// Decimal with precision
RuleFor(x => x.Price)
    .GreaterThan(0).WithMessage("Price must be positive")
    .ScalePrecision(2, 10).WithMessage("Price can have max 2 decimal places");
```

### Guid Properties

```csharp
// Required Guid
RuleFor(x => x.UserId)
    .NotEmpty().WithMessage("UserId is required");

// Guid must exist (requires async validation)
RuleFor(x => x.UserId)
    .MustAsync(async (userId, cancellation) =>
    {
        await using var session = _store.LightweightSession();
        var user = await session.LoadAsync<User>(userId);
        return user != null;
    })
    .WithMessage("User does not exist");
```

### Password Rules

```csharp
RuleFor(x => x.Password)
    .NotEmpty().WithMessage("Password is required")
    .MinimumLength(8).WithMessage("Password must be at least 8 characters")
    .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
    .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
    .Matches(@"\d").WithMessage("Password must contain at least one digit")
    .Matches(@"[\W_]").WithMessage("Password must contain at least one special character");
```

### Complex Validations

```csharp
// Conditional Validation
RuleFor(x => x.CompanyName)
    .NotEmpty().When(x => x.IsCompany)
    .WithMessage("Company name is required for company accounts");

// Cross-Property Validation
RuleFor(x => x.EndDate)
    .GreaterThan(x => x.StartDate)
    .When(x => x.EndDate.HasValue)
    .WithMessage("End date must be after start date");

// Custom Validation
RuleFor(x => x)
    .Must(HaveValidCombination)
    .WithMessage("Invalid combination of properties");

private bool HaveValidCombination(CreateUserCmd cmd)
{
    // Custom logic
    return cmd.Email.EndsWith("@company.com") || !cmd.IsAdmin;
}
```

## Beispiel Output

```csharp
using FluentValidation;
using MartenAkkaTests.Api.UserManagement.Cmd;

namespace MartenAkkaTests.Api.UserManagement.Validators;

/// <summary>
/// Validator for CreateUserCmd.
/// Ensures username and email meet security and format requirements.
/// </summary>
public class CreateUserCmdValidator : AbstractValidator<CreateUserCmd>
{
    public CreateUserCmdValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("Username is required")
            .MinimumLength(3).WithMessage("Username must be at least 3 characters")
            .MaximumLength(50).WithMessage("Username cannot exceed 50 characters")
            .Matches("^[a-zA-Z0-9_-]+$").WithMessage("Username can only contain letters, numbers, underscores, and hyphens");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(100).WithMessage("Email cannot exceed 100 characters");
    }
}
```

## Testing Validators (Optional)

```csharp
[Test]
public void Validate_WhenUserNameTooShort_ShouldFail()
{
    var validator = new CreateUserCmdValidator();
    var cmd = new CreateUserCmd(null, "ab", "test@example.com");

    var result = validator.Validate(cmd);

    Assert.That(result.IsValid, Is.False);
    Assert.That(result.Errors.Any(e => e.PropertyName == "UserName"), Is.True);
}
```

## Registration

Validator wird **automatisch** registriert via:

```csharp
// Program.cs (bereits vorhanden)
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

---

**Bereit!** Für welches Command soll ein Validator erstellt werden?

namespace MartenAkkaTests.Api.UserManagement;

public record User(Guid UserId, string UserName, string Email, string? Password, bool IsDeactivated, DateTime CreatedAt, DateTime LastUpdatedAt);
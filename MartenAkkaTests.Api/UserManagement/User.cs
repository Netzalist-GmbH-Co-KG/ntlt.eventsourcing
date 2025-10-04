namespace MartenAkkaTests.Api.UserManagement;

public record User(Guid UserId, string UserName, string Email, string? Password, DateTime CreatedAt, DateTime LastUpdatedAt);
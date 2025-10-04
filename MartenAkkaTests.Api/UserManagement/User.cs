namespace MartenAkkaTests.Api.UserManagement;

public record User(Guid UserId, string UserName, string Email, DateTime CreatedAt, DateTime LastUpdatedAt);
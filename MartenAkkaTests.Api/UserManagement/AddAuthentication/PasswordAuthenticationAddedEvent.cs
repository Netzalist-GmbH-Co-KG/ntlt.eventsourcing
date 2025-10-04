namespace MartenAkkaTests.Api.UserManagement.AddAuthentication;

public record PasswordAuthenticationAddedEvent(Guid SessionId, Guid UserId, string PasswordHash);

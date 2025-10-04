namespace MartenAkkaTests.Api.UserManagement.AddAuthentication;

public record AddPasswordAuthenticationCmd(Guid SessionId, Guid UserId, string Password);
public record AddPasswordAuthenticationResult(bool Success, string? ErrorMessage = null);
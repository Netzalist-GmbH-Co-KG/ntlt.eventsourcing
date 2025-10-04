namespace MartenAkkaTests.Api.UserManagement.DeactivateUser;

public record DeactivateUserCmd(Guid SessionId, Guid UserId);

public record DeactivateUserResult(bool Success, string? ErrorMessage = null);
namespace MartenAkkaTests.Api.UserManagement.DeactivateUser;

public record UserDeactivatedEvent(Guid SessionId, Guid UserId);

using MartenAkkaTests.Api.EventSourcing;

namespace MartenAkkaTests.Api.UserManagement.CreateUser;

public record CreateUserCmd(Guid SessionId, string UserName, string Email) : ICmd;

public record CreateUserResult(bool Success, Guid? UserId = null, string? ErrorMessage = null);
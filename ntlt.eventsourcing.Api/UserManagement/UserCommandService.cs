using Marten;
using ntlt.eventsourcing.Api.EventSourcing;
using ntlt.eventsourcing.Api.UserManagement.Cmd;

namespace ntlt.eventsourcing.Api.UserManagement;

/// <summary>
///     Command service for user management operations.
///     Replaces UserManagementCmdRouter and user command handler actors.
/// </summary>
public class UserCommandService : CommandServiceBase
{
    public UserCommandService(IServiceProvider serviceProvider, ILogger<UserCommandService> logger)
        : base(serviceProvider, logger)
    {
    }

    public async Task<CommandResult> CreateUser(CreateUserCmd cmd)
    {
        return await ExecuteCommandInSession(cmd, async (cmd, session, sessionObj) =>
        {
            var existingUser = await session.Query<User>()
                .Where(u => u.UserName == cmd.UserName || u.Email == cmd.Email)
                .FirstOrDefaultAsync();

            if (existingUser != null)
                return existingUser.UserName == cmd.UserName
                    ? new CommandResult(cmd, false, null, "Username already exists")
                    : new CommandResult(cmd, false, null, "Email already exists");

            var userId = GuidProvider.NewGuid();
            session.Events.StartStream<User>(userId,
                new UserCreatedEvent(sessionObj.SessionId, userId, cmd.UserName, cmd.Email, DateTimeProvider.UtcNow));

            return new CommandResult(cmd, true, userId);
        });
    }

    public async Task<CommandResult> AddPasswordAuthentication(AddPasswordAuthenticationCmd cmd)
    {
        return await ExecuteCommandInSession(cmd, async (cmd, session, sessionObj) =>
        {
            var existingUser = await session.Query<User>()
                .Where(u => u.UserId == cmd.UserId)
                .FirstOrDefaultAsync();

            if (existingUser == null)
                return new CommandResult(cmd, false, null, "User does not exist");

            if (existingUser.Password != null)
                return new CommandResult(cmd, false, null, "User already has a password authentication");

            // Use BCrypt for secure password hashing with work factor 12
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(cmd.Password, 12);

            session.Events.Append(cmd.UserId,
                new PasswordAuthenticationAddedEvent(sessionObj.SessionId, cmd.UserId, passwordHash));

            return new CommandResult(cmd, true);
        });
    }

    public async Task<CommandResult> DeactivateUser(DeactivateUserCmd cmd)
    {
        return await ExecuteCommandInSession(cmd, async (cmd, session, sessionObj) =>
        {
            var existingUser = await session.Query<User>()
                .Where(u => u.UserId == cmd.UserId)
                .FirstOrDefaultAsync();

            if (existingUser == null) return new CommandResult(cmd, false, null, "User not found");

            if (existingUser.IsDeactivated)
                // Idempotent - already deactivated
                return new CommandResult(cmd, true);

            session.Events.Append(cmd.UserId,
                new UserDeactivatedEvent(sessionObj.SessionId, cmd.UserId));

            return new CommandResult(cmd, true);
        });
    }

    public async Task<CommandResult> ChangeUserEmail(ChangeUserEmailCmd cmd)
    {
        return await ExecuteCommandInSession(cmd, async (cmd, session, sessionObj) =>
        {
            var user = await session.LoadAsync<User>(cmd.UserId);

            if (user == null)
                return new CommandResult(cmd, false, null, "User not found");

            if (user.IsDeactivated)
                return new CommandResult(cmd, false, null, "Cannot change email for deactivated user");

            if (user.Email == cmd.NewEmail)
                return new CommandResult(cmd, false, null, "New email is the same as current email");

            // Check if new email is already in use
            var existingUserWithEmail = await session.Query<User>()
                .Where(u => u.Email == cmd.NewEmail)
                .FirstOrDefaultAsync();

            if (existingUserWithEmail != null)
                return new CommandResult(cmd, false, null, "Email already in use");

            session.Events.Append(cmd.UserId,
                new UserEmailChangedEvent(sessionObj.SessionId, cmd.UserId, cmd.NewEmail, DateTimeProvider.UtcNow));

            return new CommandResult(cmd, true);
        });
    }
}
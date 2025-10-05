using Marten.Events.Aggregation;
using MartenAkkaTests.Api.UserManagement.Cmd;

namespace MartenAkkaTests.Api.UserManagement;

// Aggregate
public record User(Guid UserId, string UserName, string Email, string? Password, bool IsDeactivated, DateTime CreatedAt, DateTime LastUpdatedAt);

// Projection
public class UserProjection : SingleStreamProjection<User, Guid>
{
    public User Create(UserCreatedEvent evt) =>
        new (evt.UserId, evt.UserName, evt.Email, null, false, evt.CreatedAt, evt.CreatedAt);

    public User Create(PasswordAuthenticationAddedEvent evt) =>
        null!;
    
    public User Apply(User user, PasswordAuthenticationAddedEvent evt) =>
        user with { Password = evt.PasswordHash };
    
    public User Apply(User user, UserDeactivatedEvent _) =>
        user with { IsDeactivated = true };
}
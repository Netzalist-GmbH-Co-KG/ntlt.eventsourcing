using Marten.Events.Aggregation;
using MartenAkkaTests.Api.UserManagement.AddAuthentication;
using MartenAkkaTests.Api.UserManagement.CreateUser;
using MartenAkkaTests.Api.UserManagement.DeactivateUser;

namespace MartenAkkaTests.Api.UserManagement;

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
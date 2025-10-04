using Marten.Events.Aggregation;
using MartenAkkaTests.Api.UserManagement.AddAuthentication;
using MartenAkkaTests.Api.UserManagement.CreateUser;

namespace MartenAkkaTests.Api.UserManagement;

public class UserProjection : SingleStreamProjection<User, Guid>
{
    public User Create(UserCreatedEvent evt) =>
        new (evt.UserId, evt.UserName, evt.Email, null, evt.CreatedAt, evt.CreatedAt);
    
    public User Apply(User user, PasswordAuthenticationAddedEvent evt) =>
        user with { Password = evt.PasswordHash };
}
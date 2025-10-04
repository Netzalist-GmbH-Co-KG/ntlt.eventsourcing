using Marten.Events.Aggregation;
using MartenAkkaTests.Api.UserManagement.CreateUser;

namespace MartenAkkaTests.Api.UserManagement;

public class UserProjection : SingleStreamProjection<User, Guid>
{
    public User Create(UserCreatedEvent evt) =>
        new (evt.UserId, evt.UserName, evt.Email, evt.CreatedAt, evt.CreatedAt);
}
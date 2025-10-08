using System.Collections.Immutable;
using Marten;

namespace ntlt.eventsourcing.autx.UserManagement;

public sealed record UserListItem ( Guid UserId, string UserName, string Email, bool IsDeactivated, bool HasPassword );

public class UserQueryService
{
    private readonly IDocumentStore _documentStore;

    public UserQueryService(IDocumentStore documentStore)
    {
        _documentStore = documentStore;
    }
    
    public async Task<IImmutableList<UserListItem>> GetAllUsers()
    {
        await using var session = _documentStore.LightweightSession();

        var users = await session.Query<User>()
            .ToListAsync();

        var display = users
            .Select(u =>
                new UserListItem(u.UserId, u.UserName, u.Email, u.IsDeactivated, !string.IsNullOrEmpty(u.Password)))
            .ToImmutableList();

        return display;
    }
}
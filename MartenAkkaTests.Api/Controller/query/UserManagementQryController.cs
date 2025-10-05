using Marten;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.UserManagement;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller.query;

public class UserManagementQryController : ControllerBase
{
    private readonly IDocumentStore _documentStore;

    public UserManagementQryController(
        IDocumentStore documentStore        
        )
    {
        _documentStore = documentStore;
    }

    [HttpGet("api/query/user/list")]
    public async Task<IActionResult> GetAllUsers()
    {
        await using var session = _documentStore.LightweightSession();

        var users = await session.Query<User>()
            .ToListAsync();

        var display = users
            .Select(u => new { u.UserId, u.UserName, u.Email, u.IsDeactivated, HasPassword = !string.IsNullOrEmpty(u.Password) });
        
        return new OkObjectResult(display);
    }
}
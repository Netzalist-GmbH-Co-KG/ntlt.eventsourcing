using Marten;
using MartenAkkaTests.Api.UserManagement;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller.v1.qry;

public class UserController : V1QueryControllerBase
{
    private readonly IDocumentStore _documentStore;

    public UserController(IDocumentStore documentStore)
    {
        _documentStore = documentStore;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List()
    {
        await using var session = _documentStore.LightweightSession();

        var users = await session.Query<User>()
            .ToListAsync();

        var display = users
            .Select(u => new { u.UserId, u.UserName, u.Email, u.IsDeactivated, HasPassword = !string.IsNullOrEmpty(u.Password) });

        return Ok(display);
    }
}

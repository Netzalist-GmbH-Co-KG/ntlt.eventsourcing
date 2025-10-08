using Marten;
using Microsoft.AspNetCore.Mvc;
using ntlt.eventsourcing.autx.UserManagement;

namespace ntlt.eventsourcing.autx.Controller.v1.qry;

public class UserController : V1QueryControllerBase
{
    private readonly UserQueryService _userQueryService;

    public UserController(UserQueryService userQueryService)
    {
        _userQueryService = userQueryService;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List()
    {
        var users = await _userQueryService.GetAllUsers();
        return Ok(users);
    }
}
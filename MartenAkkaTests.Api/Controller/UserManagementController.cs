using System.Net;
using Akka.Actor;
using Akka.Hosting;
using Marten;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.UserManagement;
using MartenAkkaTests.Api.UserManagement.AddAuthentication;
using MartenAkkaTests.Api.UserManagement.CreateUser;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller;

public class UserManagementController
{
    private readonly IDocumentStore _documentStore;
    private readonly IActorRef _createUserActor;
    private readonly IActorRef _addPasswordAuthenticationActor;

    public UserManagementController(
        IRequiredActor<CreateUserCmdHandler> createUserActor,
        IRequiredActor<AddPasswordAuthenticationCmdHandler> addPasswordAuthenticationCmdHandler,
        IDocumentStore documentStore        
        )
    {
        _documentStore = documentStore;
        _createUserActor = createUserActor.ActorRef;
        _addPasswordAuthenticationActor = addPasswordAuthenticationCmdHandler.ActorRef;
    }

    [HttpGet("api/user/create")]
    public async Task<IActionResult> CreateUser([FromQuery] Guid sessionId, [FromQuery] string userName, [FromQuery] string email)
    {
        var result = await _createUserActor.Ask<CreateUserResult>(new CreateUserCmd(sessionId, userName, email));
        if (result.Success && result.UserId.HasValue)
        {
            return new OkObjectResult(new { result.UserId.Value });
        }

        var response = new JsonResult(new { result.ErrorMessage })
        {
            StatusCode = (int)HttpStatusCode.InternalServerError
        };
        return response;
    }
    
    
    [HttpGet("api/user/add-password-authentication")]
    public async Task<IActionResult> AddPasswordAuthentication([FromQuery] Guid sessionId, [FromQuery] Guid userId, [FromQuery] string password)
    {
        var result = await _addPasswordAuthenticationActor.Ask<AddPasswordAuthenticationResult>(new AddPasswordAuthenticationCmd(sessionId, userId, password));
        if (result.Success)
        {
            return new OkResult();
        }

        var response = new JsonResult(new { result.ErrorMessage })
        {
            StatusCode = (int)HttpStatusCode.InternalServerError
        };
        return response;
    }
    
    [HttpGet("api/user/list")]
    public async Task<IActionResult> GetAllUsers([FromQuery] Guid sessionId)
    {
        await using var session = _documentStore.LightweightSession();
        var sessionExists = session.Query<Session>()
            .Any(u => u.SessionId == sessionId && u.Closed == false);
        if (!sessionExists)
        {
            return new UnauthorizedResult();
        }

        var users = await session.Query<User>()
            .ToListAsync();

        var display = users
            .Select(u => new { u.UserId, u.UserName, u.Email, HasPassword = !string.IsNullOrEmpty(u.Password) });
        
        return new OkObjectResult(display);
    }
}
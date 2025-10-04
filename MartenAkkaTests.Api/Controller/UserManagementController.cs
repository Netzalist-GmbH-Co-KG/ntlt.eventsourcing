using System.Net;
using Akka.Actor;
using Akka.Hosting;
using Marten;
using MartenAkkaTests.Api.SessionManagement;
using MartenAkkaTests.Api.UserManagement;
using MartenAkkaTests.Api.UserManagement.AddAuthentication;
using MartenAkkaTests.Api.UserManagement.CreateUser;
using MartenAkkaTests.Api.UserManagement.DeactivateUser;
using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller;

public class UserManagementController
{
    private readonly IDocumentStore _documentStore;
    private readonly IActorRef _createUserActor;
    private readonly IActorRef _addPasswordAuthenticationActor;
    private readonly IActorRef _deactivateUserCmdHandler;

    public UserManagementController(
        IRequiredActor<CreateUserCmdHandler> createUserActor,
        IRequiredActor<AddPasswordAuthenticationCmdHandler> addPasswordAuthenticationCmdHandler,
        IRequiredActor<DeactivateUserCmdHandler> deactivateUserCmdHandler,
        IDocumentStore documentStore        
        )
    {
        _documentStore = documentStore;
        _createUserActor = createUserActor.ActorRef;
        _addPasswordAuthenticationActor = addPasswordAuthenticationCmdHandler.ActorRef;
        _deactivateUserCmdHandler = deactivateUserCmdHandler.ActorRef;
    }

    [HttpPost("api/user/create")]
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
    
    
    [HttpPost("api/user/add-password-authentication")]
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
    
    [HttpPost("api/user/deactivate-user")]
    public async Task<IActionResult> DeactivateUser([FromQuery] Guid sessionId, [FromQuery] Guid userId)
    {
        var result = await _deactivateUserCmdHandler.Ask<DeactivateUserResult>( new DeactivateUserCmd(sessionId, userId));
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
            .Select(u => new { u.UserId, u.UserName, u.Email, u.IsDeactivated, HasPassword = !string.IsNullOrEmpty(u.Password) });
        
        return new OkObjectResult(display);
    }
}
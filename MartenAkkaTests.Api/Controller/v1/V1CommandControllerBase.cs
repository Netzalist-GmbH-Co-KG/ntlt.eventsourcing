using Microsoft.AspNetCore.Mvc;

namespace MartenAkkaTests.Api.Controller.v1;

/// <summary>
///     Base controller for v1 Command endpoints (CQRS Write side)
///     Route pattern: /api/v1/cmd/{controller}/{action}
/// </summary>
[ApiController]
[Area("v1")]
[Route("api/v1/cmd/[controller]")]
[ApiExplorerSettings(GroupName = "v1-commands")]
public abstract class V1CommandControllerBase : ControllerBase
{
}
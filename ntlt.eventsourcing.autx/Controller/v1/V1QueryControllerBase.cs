using Microsoft.AspNetCore.Mvc;

namespace ntlt.eventsourcing.autx.Controller.v1;

/// <summary>
///     Base controller for v1 Query endpoints (CQRS Read side)
///     Route pattern: /api/v1/qry/{controller}/{action}
/// </summary>
[ApiController]
[Area("v1")]
[Route("api/v1/qry/[controller]")]
[ApiExplorerSettings(GroupName = "v1-queries")]
public abstract class V1QueryControllerBase : ControllerBase
{
}
using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using UP.Application.HistoryRecords;
using UP.Domain.HistoryRecords;

namespace UP.Api.HistoryRecords;

[ApiController]
[Route(HistoryRecordRoutes.Base)]
[Authorize]
public sealed class HistoryRecordsController(IHistoryRecordService historyRecordService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<HistoryRecordResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<HistoryRecordResponse>>> GetAll(
        [FromQuery, Required, MaxLength(100)] string entityType,
        [FromQuery, Required] Guid entityId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<HistoryRecord> records = await historyRecordService.GetAllAsync(
            entityType,
            entityId,
            cancellationToken);

        return Ok(records.ToResponses());
    }
}

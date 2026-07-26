using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tatakae.Application.Contracts.Common;

namespace Tatakae.Api.Extensions;

public static class ResultDtoActionResultExtensions
{
    public static IActionResult ToActionResult<T>(this ResultDto<T> result, ControllerBase controller)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(controller);

        if (result.IsSuccess)
            return controller.Ok(result.Data);

        return ToErrorResult(result, controller);
    }

    public static IActionResult ToActionResult(this ResultDto result, ControllerBase controller, bool noContentOnSuccess = false)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(controller);

        if (result.IsSuccess)
            return noContentOnSuccess ? controller.NoContent() : controller.Ok(result);

        return ToErrorResult(result, controller);
    }

    private static IActionResult ToErrorResult(ResultDto result, ControllerBase controller)
        => result.Status switch
        {
            ResultStatus.ValidationError => controller.BadRequest(result),
            ResultStatus.Unauthorized => controller.Unauthorized(result),
            ResultStatus.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, result),
            ResultStatus.NotFound => controller.NotFound(result),
            ResultStatus.Conflict => controller.Conflict(result),
            _ => controller.StatusCode(StatusCodes.Status500InternalServerError, result)
        };
}

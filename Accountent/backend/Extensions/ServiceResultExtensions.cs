using backend.Models.Results;
using Microsoft.AspNetCore.Mvc;

namespace backend.Extensions
{
    public static class ServiceResultExtensions
    {
        public static IActionResult ToActionResult<T>(this ServiceResult<T> result, ControllerBase controller)
        {
            if (result.IsSuccess)
                return controller.Ok(result.Data);

            return result.Error!.ToActionResult(controller);
        }

        public static IActionResult ToCreatedAtActionResult<T>(
            this ServiceResult<T> result,
            ControllerBase controller,
            string actionName,
            Func<T, object> routeValues)
        {
            if (result.IsSuccess)
                return controller.CreatedAtAction(actionName, routeValues(result.Data!), result.Data);

            return result.Error!.ToActionResult(controller);
        }

        public static IActionResult ToActionResult(this ServiceResult result, ControllerBase controller)
        {
            if (result.IsSuccess)
                return controller.NoContent();

            return result.Error!.ToActionResult(controller);
        }

        public static IActionResult ToActionResult(this ServiceError error, ControllerBase controller)
        {
            var body = new { message = error.Message };

            return error.Code switch
            {
                ServiceErrorCode.NotFound => controller.NotFound(body),
                ServiceErrorCode.Conflict => controller.Conflict(body),
                ServiceErrorCode.Forbidden => controller.StatusCode(StatusCodes.Status403Forbidden, body),
                ServiceErrorCode.Unauthorized => controller.Unauthorized(body),
                _ => controller.BadRequest(body),
            };
        }
    }
}

using backend.Extensions;
using backend.Models.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace backend.Tests.Unit;

public class ServiceResultExtensionsTests
{
    private readonly StubController _controller = CreateController();

    [Fact]
    public void ToActionResult_успех_возвращает_OkObjectResult()
    {
        var result = ServiceResult<string>.Ok("данные");
        var action = result.ToActionResult(_controller);

        var ok = Assert.IsType<OkObjectResult>(action);
        Assert.Equal("данные", ok.Value);
    }

    [Theory]
    [InlineData(ServiceErrorCode.NotFound, typeof(NotFoundObjectResult))]
    [InlineData(ServiceErrorCode.Conflict, typeof(ConflictObjectResult))]
    [InlineData(ServiceErrorCode.Unauthorized, typeof(UnauthorizedObjectResult))]
    [InlineData(ServiceErrorCode.Validation, typeof(BadRequestObjectResult))]
    public void ToActionResult_ошибка_мапится_в_ожидаемый_статус(
        ServiceErrorCode code,
        Type expectedResultType)
    {
        var result = ServiceResult<string>.Fail(code, "сообщение");
        var action = result.ToActionResult(_controller);

        Assert.IsType(expectedResultType, action);
        AssertMessage(action, "сообщение");
    }

    [Fact]
    public void ToActionResult_Forbidden_возвращает_403()
    {
        var error = ServiceErrors.Forbidden("запрещено");
        var action = error.ToActionResult(_controller);

        var status = Assert.IsType<ObjectResult>(action);
        Assert.Equal(StatusCodes.Status403Forbidden, status.StatusCode);
        AssertMessage(status, "запрещено");
    }

    [Fact]
    public void ToCreatedAtActionResult_успех_возвращает_CreatedAtActionResult()
    {
        var payload = new SampleDto(42, "проводка");
        var result = ServiceResult<SampleDto>.Ok(payload);
        var action = result.ToCreatedAtActionResult(_controller, "GetById", x => new { id = x.Id });

        var created = Assert.IsType<CreatedAtActionResult>(action);
        Assert.Equal("GetById", created.ActionName);
        Assert.Equal(payload, created.Value);
    }

    [Fact]
    public void ToActionResult_без_данных_успех_даёт_NoContent()
    {
        var result = ServiceResult.Ok();
        var action = result.ToActionResult(_controller);

        Assert.IsType<NoContentResult>(action);
    }

    [Fact]
    public void ToActionResult_без_данных_ошибка_мапится_как_обычно()
    {
        var result = ServiceResult.Fail(ServiceErrorCode.NotFound, "не найдено");
        var action = result.ToActionResult(_controller);

        Assert.IsType<NotFoundObjectResult>(action);
    }

    private static void AssertMessage(IActionResult action, string expected)
    {
        var objectResult = Assert.IsAssignableFrom<ObjectResult>(action);
        var message = objectResult.Value?
            .GetType()
            .GetProperty("message")
            ?.GetValue(objectResult.Value) as string;
        Assert.Equal(expected, message);
    }

    private static StubController CreateController()
    {
        var controller = new StubController
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext(),
            },
        };
        return controller;
    }

    private sealed class StubController : ControllerBase;

    private sealed record SampleDto(int Id, string Name);
}

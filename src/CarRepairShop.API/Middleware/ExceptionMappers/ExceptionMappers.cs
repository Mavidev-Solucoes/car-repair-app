using System.Net;
using CarRepairShop.Application.Common.Exceptions;

namespace CarRepairShop.API.Middleware.ExceptionMappers;

public class ValidationExceptionMapper : IExceptionResponseMapper
{
    public bool CanMap(Exception exception) => exception is ValidationException;

    public (HttpStatusCode, ErrorResponse) Map(Exception exception)
    {
        var ve = (ValidationException)exception;
        return (HttpStatusCode.BadRequest, new ErrorResponse
        {
            Title = "Validation Error",
            Status = (int)HttpStatusCode.BadRequest,
            Errors = ve.Errors
        });
    }
}

public class NotFoundExceptionMapper : IExceptionResponseMapper
{
    public bool CanMap(Exception exception) => exception is NotFoundException;

    public (HttpStatusCode, ErrorResponse) Map(Exception exception) =>
        (HttpStatusCode.NotFound, new ErrorResponse
        {
            Title = "Not Found",
            Status = (int)HttpStatusCode.NotFound,
            Detail = exception.Message
        });
}

public class BusinessExceptionMapper : IExceptionResponseMapper
{
    public bool CanMap(Exception exception) => exception is BusinessException;

    public (HttpStatusCode, ErrorResponse) Map(Exception exception) =>
        (HttpStatusCode.UnprocessableEntity, new ErrorResponse
        {
            Title = "Business Rule Violation",
            Status = (int)HttpStatusCode.UnprocessableEntity,
            Detail = exception.Message
        });
}

public class InvalidOperationExceptionMapper : IExceptionResponseMapper
{
    public bool CanMap(Exception exception) => exception is InvalidOperationException;

    public (HttpStatusCode, ErrorResponse) Map(Exception exception) =>
        (HttpStatusCode.UnprocessableEntity, new ErrorResponse
        {
            Title = "Business Rule Violation",
            Status = (int)HttpStatusCode.UnprocessableEntity,
            Detail = exception.Message
        });
}

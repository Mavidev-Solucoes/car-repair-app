using System.Net;

namespace CarRepairShop.API.Middleware;

/// <summary>
/// Maps a specific exception type to an HTTP status code and error response body.
/// Implementations are registered in DI and iterated by <see cref="ExceptionHandlingMiddleware"/>,
/// making it open for extension without modifying the middleware itself (OCP).
/// </summary>
public interface IExceptionResponseMapper
{
    bool CanMap(Exception exception);
    (HttpStatusCode StatusCode, ErrorResponse Response) Map(Exception exception);
}

namespace backend.Models.Results
{
    public sealed record ServiceError(ServiceErrorCode Code, string Message);

    public static class ServiceErrors
    {
        public static ServiceError Validation(string message) =>
            new(ServiceErrorCode.Validation, message);

        public static ServiceError NotFound(string message) =>
            new(ServiceErrorCode.NotFound, message);

        public static ServiceError Conflict(string message) =>
            new(ServiceErrorCode.Conflict, message);

        public static ServiceError Forbidden(string message) =>
            new(ServiceErrorCode.Forbidden, message);

        public static ServiceError Unauthorized(string message) =>
            new(ServiceErrorCode.Unauthorized, message);
    }
}

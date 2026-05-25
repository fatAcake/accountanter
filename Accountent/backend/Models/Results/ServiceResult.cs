namespace backend.Models.Results
{
    public sealed class ServiceResult<T>
    {
        public T? Data { get; private init; }
        public ServiceError? Error { get; private init; }
        public bool IsSuccess => Error is null;

        public static ServiceResult<T> Ok(T data) => new() { Data = data };

        public static ServiceResult<T> Fail(ServiceError error) => new() { Error = error };

        public static ServiceResult<T> Fail(ServiceErrorCode code, string message) =>
            Fail(new ServiceError(code, message));
    }

    public sealed class ServiceResult
    {
        public ServiceError? Error { get; private init; }
        public bool IsSuccess => Error is null;

        public static ServiceResult Ok() => new();

        public static ServiceResult Fail(ServiceError error) => new() { Error = error };

        public static ServiceResult Fail(ServiceErrorCode code, string message) =>
            Fail(new ServiceError(code, message));
    }
}

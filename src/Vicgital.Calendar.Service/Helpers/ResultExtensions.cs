using Grpc.Core;
using Vicgital.Application.Shared.Results;

namespace Vicgital.Calendar.Service.Helpers
{
    public static class ResultExtensions
    {
        /// <summary>Returns the value of a successful <see cref="Result{T}"/>, or throws an <see cref="RpcException"/>
        /// with a status derived from the failure's <see cref="ErrorType"/> otherwise.</summary>
        public static T Unwrap<T>(this Result<T> result)
        {
            if (result.IsSuccess)
                return result.Value;

            var statusCode = result.FirstError!.Type switch
            {
                ErrorType.NotFound => StatusCode.NotFound,
                ErrorType.Validation => StatusCode.InvalidArgument,
                ErrorType.Conflict => StatusCode.AlreadyExists,
                ErrorType.Forbidden => StatusCode.PermissionDenied,
                ErrorType.Unauthorized => StatusCode.Unauthenticated,
                _ => StatusCode.Internal
            };

            var message = string.Join("; ", result.Errors.Select(e => e.Message));
            throw new RpcException(new Status(statusCode, message));
        }
    }
}

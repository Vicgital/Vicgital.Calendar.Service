using Vicgital.Calendar.Service.Definition;

namespace Vicgital.Calendar.Service.Helpers
{
    public static class Validator
    {
        public static void Validate(this QuarterRequest request)
        {
            if (request == null)
                throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.InvalidArgument, "Request cannot be null."));
            if (request.Id <= 0 && string.IsNullOrWhiteSpace(request.Code))
                throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.InvalidArgument, "Either Quarter ID or Code must be provided."));
        }

        public static void Validate(this YearRequest request)
        {
            if (request == null)
                throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.InvalidArgument, "Request cannot be null."));
            if (request.Year <= 0)
                throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.InvalidArgument, "Invalid year provided."));
        }

        public static void Validate(this DateRequest request)
        {
            if (request == null)
                throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.InvalidArgument, "Request cannot be null."));
            if (!DateTime.TryParse(request.Date, out _))
                throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.InvalidArgument, "Invalid date format provided."));
        }

        public static void Validate(this WeekRequest request)
        {
            if (request == null)
                throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.InvalidArgument, "Request cannot be null."));
            if (request.Id <= 0 && string.IsNullOrWhiteSpace(request.Code))
                throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.InvalidArgument, "Either Week ID or Code must be provided."));
        }
    }
}

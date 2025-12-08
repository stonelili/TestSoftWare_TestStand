using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestStandClone.Core.ServiceLayer
{
    public enum ServiceRequestMethod { GET, POST, PUT, DELETE }
    public enum ServiceResponseStatus { Success, Error, NotFound, Unauthorized, BadRequest }

    public class ServiceRequest
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ServiceRequestMethod Method { get; set; } = ServiceRequestMethod.GET;
        public string Endpoint { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
        public Dictionary<string, object> Parameters { get; set; } = new();
        public object? Body { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string ClientId { get; set; } = string.Empty;
    }

    public class ServiceResponse
    {
        public string RequestId { get; set; } = string.Empty;
        public ServiceResponseStatus Status { get; set; } = ServiceResponseStatus.Success;
        public object? Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int ProcessingTimeMs { get; set; }
    }

    public interface IServiceEndpoint
    {
        string Route { get; }
        ServiceRequestMethod[] SupportedMethods { get; }
        Task<ServiceResponse> HandleRequest(ServiceRequest request);
    }

    public class SequenceExecutionEndpoint : IServiceEndpoint
    {
        public string Route => "/api/sequences/execute";
        public ServiceRequestMethod[] SupportedMethods => new[] { ServiceRequestMethod.POST };

        public Task<ServiceResponse> HandleRequest(ServiceRequest request)
        {
            var sequenceId = request.Parameters.GetValueOrDefault("sequenceId")?.ToString();
            return Task.FromResult(new ServiceResponse
            {
                RequestId = request.Id,
                Status = ServiceResponseStatus.Success,
                Data = new { ExecutionId = Guid.NewGuid().ToString(), SequenceId = sequenceId, Status = "Started" },
                Message = "Sequence execution started"
            });
        }
    }

    public class StatusEndpoint : IServiceEndpoint
    {
        public string Route => "/api/status";
        public ServiceRequestMethod[] SupportedMethods => new[] { ServiceRequestMethod.GET };

        public Task<ServiceResponse> HandleRequest(ServiceRequest request)
        {
            return Task.FromResult(new ServiceResponse
            {
                RequestId = request.Id,
                Status = ServiceResponseStatus.Success,
                Data = new { 
                    ServerStatus = "Running", 
                    Uptime = TimeSpan.FromHours(1).ToString(),
                    ActiveSessions = 5,
                    Version = "1.0.0"
                }
            });
        }
    }

    public class ServiceManager
    {
        private static readonly Lazy<ServiceManager> _instance = new(() => new ServiceManager());
        public static ServiceManager Instance => _instance.Value;
        private readonly Dictionary<string, IServiceEndpoint> _endpoints = new();
        private readonly List<ServiceRequest> _requestLog = new();
        private readonly object _lock = new();

        public event EventHandler<ServiceRequest>? RequestReceived;
        public event EventHandler<ServiceResponse>? ResponseSent;

        private ServiceManager()
        {
            RegisterEndpoint(new SequenceExecutionEndpoint());
            RegisterEndpoint(new StatusEndpoint());
        }

        public void RegisterEndpoint(IServiceEndpoint endpoint)
        {
            lock (_lock) { _endpoints[endpoint.Route] = endpoint; }
        }

        public async Task<ServiceResponse> ProcessRequest(ServiceRequest request)
        {
            var startTime = DateTime.Now;
            RequestReceived?.Invoke(this, request);

            lock (_lock) { _requestLog.Add(request); }

            ServiceResponse response;
            if (_endpoints.TryGetValue(request.Endpoint, out var endpoint))
            {
                if (endpoint.SupportedMethods.Contains(request.Method))
                {
                    response = await endpoint.HandleRequest(request);
                }
                else
                {
                    response = new ServiceResponse
                    {
                        RequestId = request.Id,
                        Status = ServiceResponseStatus.BadRequest,
                        Message = $"Method {request.Method} not supported for {request.Endpoint}"
                    };
                }
            }
            else
            {
                response = new ServiceResponse
                {
                    RequestId = request.Id,
                    Status = ServiceResponseStatus.NotFound,
                    Message = $"Endpoint {request.Endpoint} not found"
                };
            }

            response.ProcessingTimeMs = (int)(DateTime.Now - startTime).TotalMilliseconds;
            ResponseSent?.Invoke(this, response);
            return response;
        }

        public List<string> GetRegisteredEndpoints()
        {
            lock (_lock) { return _endpoints.Keys.ToList(); }
        }

        public List<ServiceRequest> GetRequestLog(int count = 100)
        {
            lock (_lock) { return _requestLog.TakeLast(count).ToList(); }
        }
    }
}

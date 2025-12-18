using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.TestStandAPI
{
    // API Server for external integrations
    public class APIServer
    {
        public string Name { get; set; }
        public int Port { get; set; }
        public APIProtocol Protocol { get; set; }
        public bool IsRunning { get; set; }
        public DateTime StartTime { get; set; }
        public List<APIEndpoint> Endpoints { get; set; }

        public APIServer()
        {
            Endpoints = new List<APIEndpoint>();
        }
    }

    public enum APIProtocol { HTTP, HTTPS, REST, SOAP, gRPC }

    public class APIEndpoint
    {
        public string Path { get; set; }
        public string Method { get; set; } // GET, POST, PUT, DELETE
        public Func<APIRequest, APIResponse> Handler { get; set; }
        public bool RequiresAuthentication { get; set; }
    }

    public class APIRequest
    {
        public string Endpoint { get; set; }
        public string Method { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public string Body { get; set; }
        public DateTime Timestamp { get; set; }

        public APIRequest()
        {
            Headers = new Dictionary<string, string>();
            Parameters = new Dictionary<string, object>();
            Timestamp = DateTime.Now;
        }
    }

    public class APIResponse
    {
        public int StatusCode { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }

        public APIResponse()
        {
            Headers = new Dictionary<string, string>();
        }
    }

    // TestStand API Manager
    public class TestStandAPIManager
    {
        private static TestStandAPIManager _instance;
        private static readonly object _lock = new object();
        private Dictionary<string, APIServer> _servers;

        private TestStandAPIManager()
        {
            _servers = new Dictionary<string, APIServer>();
        }

        public static TestStandAPIManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new TestStandAPIManager();
                    }
                }
                return _instance;
            }
        }

        public void RegisterServer(APIServer server)
        {
            _servers[server.Name] = server;
        }

        public APIServer GetServer(string name)
        {
            return _servers.TryGetValue(name, out var server) ? server : null;
        }

        public void StartServer(string name)
        {
            if (_servers.TryGetValue(name, out var server))
            {
                server.IsRunning = true;
                server.StartTime = DateTime.Now;
            }
        }

        public void StopServer(string name)
        {
            if (_servers.TryGetValue(name, out var server))
            {
                server.IsRunning = false;
            }
        }

        public APIResponse HandleRequest(string serverName, APIRequest request)
        {
            var server = GetServer(serverName);
            if (server == null || !server.IsRunning)
            {
                return new APIResponse
                {
                    StatusCode = 503,
                    Success = false,
                    ErrorMessage = "Server not available"
                };
            }

            var endpoint = server.Endpoints.FirstOrDefault(e => 
                e.Path == request.Endpoint && e.Method == request.Method);

            if (endpoint == null)
            {
                return new APIResponse
                {
                    StatusCode = 404,
                    Success = false,
                    ErrorMessage = "Endpoint not found"
                };
            }

            try
            {
                return endpoint.Handler(request);
            }
            catch (Exception ex)
            {
                return new APIResponse
                {
                    StatusCode = 500,
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public List<APIServer> GetAllServers()
        {
            return _servers.Values.ToList();
        }
    }
}

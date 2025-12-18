using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestStandClone.Core.TestStationNetwork
{
    public enum StationStatus { Offline, Idle, Running, Paused, Error, Maintenance }
    public enum StationCapability { Production, Engineering, Calibration, Debug }

    public class TestStation
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public int Port { get; set; } = 8080;
        public StationStatus Status { get; set; } = StationStatus.Offline;
        public List<StationCapability> Capabilities { get; set; } = new List<StationCapability>();
        public string CurrentProgram { get; set; } = string.Empty;
        public string CurrentSerialNumber { get; set; } = string.Empty;
        public DateTime LastHeartbeat { get; set; } = DateTime.Now;
        public int TotalTestsToday { get; set; }
        public int PassedTestsToday { get; set; }
        public int FailedTestsToday { get; set; }
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }

    public class StationGroup
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> StationIds { get; set; } = new List<string>();
    }

    public class StationMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FromStationId { get; set; } = string.Empty;
        public string ToStationId { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public Dictionary<string, object> Payload { get; set; } = new Dictionary<string, object>();
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class StationEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string StationId { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    public class NetworkStatistics
    {
        public DateTime SnapshotTime { get; set; } = DateTime.Now;
        public int TotalStations { get; set; }
        public int OnlineStations { get; set; }
        public int IdleStations { get; set; }
        public int RunningStations { get; set; }
        public int ErrorStations { get; set; }
        public int TotalTestsToday { get; set; }
        public double NetworkYield { get; set; }
    }

    public interface IStationCommunication
    {
        Task<bool> ConnectAsync(TestStation station);
        Task DisconnectAsync(string stationId);
        Task<bool> SendMessageAsync(StationMessage message);
        Task<StationStatus> GetStatusAsync(string stationId);
    }

    public class SimulatedStationCommunication : IStationCommunication
    {
        public Task<bool> ConnectAsync(TestStation station) => Task.FromResult(true);
        public Task DisconnectAsync(string stationId) => Task.CompletedTask;
        public Task<bool> SendMessageAsync(StationMessage message) => Task.FromResult(true);
        public Task<StationStatus> GetStatusAsync(string stationId) => Task.FromResult(StationStatus.Idle);
    }

    public class TestStationNetworkManager
    {
        private static readonly Lazy<TestStationNetworkManager> _instance = new Lazy<TestStationNetworkManager>(() => new TestStationNetworkManager());
        public static TestStationNetworkManager Instance => _instance.Value;

        private readonly Dictionary<string, TestStation> _stations = new Dictionary<string, TestStation>();
        private readonly Dictionary<string, StationGroup> _groups = new Dictionary<string, StationGroup>();
        private readonly List<StationEvent> _events = new List<StationEvent>();
        private readonly IStationCommunication _communication = new SimulatedStationCommunication();

        public event EventHandler<TestStation>? StationStatusChanged;
        public event EventHandler<StationEvent>? StationEventReceived;

        private TestStationNetworkManager() { }

        public TestStation RegisterStation(string name, string location, string ipAddress, int port)
        {
            var station = new TestStation { Name = name, Location = location, IPAddress = ipAddress, Port = port };
            _stations[station.Id] = station;
            return station;
        }

        public void UnregisterStation(string stationId) => _stations.Remove(stationId);
        public TestStation? GetStation(string id) { _stations.TryGetValue(id, out var s); return s; }
        public IEnumerable<TestStation> GetAllStations() => _stations.Values.ToList();
        public IEnumerable<TestStation> GetOnlineStations() => _stations.Values.Where(s => s.Status != StationStatus.Offline).ToList();
        public IEnumerable<TestStation> GetIdleStations() => _stations.Values.Where(s => s.Status == StationStatus.Idle).ToList();
        public IEnumerable<TestStation> GetStationsWithCapability(StationCapability capability) => _stations.Values.Where(s => s.Capabilities.Contains(capability)).ToList();

        public void UpdateStationStatus(string stationId, StationStatus status)
        {
            if (_stations.TryGetValue(stationId, out var station))
            {
                station.Status = status;
                station.LastHeartbeat = DateTime.Now;
                StationStatusChanged?.Invoke(this, station);
            }
        }

        public async Task<bool> ConnectToStationAsync(string stationId)
        {
            if (!_stations.TryGetValue(stationId, out var station)) return false;
            var result = await _communication.ConnectAsync(station);
            if (result) station.Status = StationStatus.Idle;
            return result;
        }

        public async Task DisconnectFromStationAsync(string stationId)
        {
            await _communication.DisconnectAsync(stationId);
            if (_stations.TryGetValue(stationId, out var station)) station.Status = StationStatus.Offline;
        }

        public async Task<bool> SendCommandAsync(string stationId, string command, Dictionary<string, object>? data = null)
        {
            var message = new StationMessage { ToStationId = stationId, MessageType = command, Payload = data ?? new Dictionary<string, object>() };
            return await _communication.SendMessageAsync(message);
        }

        public StationGroup CreateGroup(string name, string description)
        {
            var group = new StationGroup { Name = name, Description = description };
            _groups[group.Id] = group;
            return group;
        }

        public void AddStationToGroup(string groupId, string stationId) { if (_groups.TryGetValue(groupId, out var g) && !g.StationIds.Contains(stationId)) g.StationIds.Add(stationId); }
        public void RemoveStationFromGroup(string groupId, string stationId) { if (_groups.TryGetValue(groupId, out var g)) g.StationIds.Remove(stationId); }
        public IEnumerable<StationGroup> GetAllGroups() => _groups.Values.ToList();

        public void RecordEvent(string stationId, string eventType, string message, Dictionary<string, object>? data = null)
        {
            var evt = new StationEvent { StationId = stationId, EventType = eventType, Message = message, Data = data ?? new Dictionary<string, object>() };
            _events.Add(evt);
            StationEventReceived?.Invoke(this, evt);
        }

        public IEnumerable<StationEvent> GetEvents(string? stationId = null, DateTime? since = null)
        {
            var q = _events.AsEnumerable();
            if (stationId != null) q = q.Where(e => e.StationId == stationId);
            if (since.HasValue) q = q.Where(e => e.Timestamp >= since.Value);
            return q.OrderByDescending(e => e.Timestamp).Take(100).ToList();
        }

        public NetworkStatistics GetNetworkStatistics()
        {
            var stations = _stations.Values.ToList();
            return new NetworkStatistics
            {
                TotalStations = stations.Count,
                OnlineStations = stations.Count(s => s.Status != StationStatus.Offline),
                IdleStations = stations.Count(s => s.Status == StationStatus.Idle),
                RunningStations = stations.Count(s => s.Status == StationStatus.Running),
                ErrorStations = stations.Count(s => s.Status == StationStatus.Error),
                TotalTestsToday = stations.Sum(s => s.TotalTestsToday),
                NetworkYield = stations.Sum(s => s.TotalTestsToday) > 0 ? (double)stations.Sum(s => s.PassedTestsToday) / stations.Sum(s => s.TotalTestsToday) * 100 : 0
            };
        }
    }
}

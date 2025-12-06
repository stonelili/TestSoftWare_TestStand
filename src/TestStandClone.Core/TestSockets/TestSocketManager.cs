// Copyright (c) TestStand Clone. All rights reserved.
// Test Socket Management for parallel testing infrastructure

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace TestStandClone.Core.TestSockets
{
    /// <summary>
    /// Test socket state enumeration
    /// </summary>
    public enum TestSocketState
    {
        Idle,
        Ready,
        Running,
        Paused,
        WaitingForUUT,
        Error,
        Disabled
    }

    /// <summary>
    /// Represents a test socket for parallel testing
    /// </summary>
    public class TestSocket : INotifyPropertyChanged
    {
        private TestSocketState _state = TestSocketState.Idle;
        private string _currentSerialNumber = string.Empty;
        private int _testsCompleted;
        private int _testsPassed;
        private int _testsFailed;
        private Sequence? _currentSequence;
        private DateTime _lastActivity = DateTime.Now;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Unique identifier for the test socket
        /// </summary>
        public int Id { get; }

        /// <summary>
        /// Name of the test socket
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Current state of the test socket
        /// </summary>
        public TestSocketState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _state = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Current UUT serial number being tested
        /// </summary>
        public string CurrentSerialNumber
        {
            get => _currentSerialNumber;
            set
            {
                if (_currentSerialNumber != value)
                {
                    _currentSerialNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Number of tests completed
        /// </summary>
        public int TestsCompleted
        {
            get => _testsCompleted;
            set
            {
                if (_testsCompleted != value)
                {
                    _testsCompleted = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PassRate));
                }
            }
        }

        /// <summary>
        /// Number of tests passed
        /// </summary>
        public int TestsPassed
        {
            get => _testsPassed;
            set
            {
                if (_testsPassed != value)
                {
                    _testsPassed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PassRate));
                }
            }
        }

        /// <summary>
        /// Number of tests failed
        /// </summary>
        public int TestsFailed
        {
            get => _testsFailed;
            set
            {
                if (_testsFailed != value)
                {
                    _testsFailed = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PassRate));
                }
            }
        }

        /// <summary>
        /// Pass rate percentage
        /// </summary>
        public double PassRate => TestsCompleted > 0 ? (double)TestsPassed / TestsCompleted * 100 : 0;

        /// <summary>
        /// Current sequence being executed
        /// </summary>
        public Sequence? CurrentSequence
        {
            get => _currentSequence;
            set
            {
                _currentSequence = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Last activity timestamp
        /// </summary>
        public DateTime LastActivity
        {
            get => _lastActivity;
            set
            {
                _lastActivity = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Whether the socket is enabled
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Custom properties for the socket
        /// </summary>
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        public TestSocket(int id, string? name = null)
        {
            Id = id;
            Name = name ?? $"Socket {id}";
        }

        /// <summary>
        /// Reset socket statistics
        /// </summary>
        public void ResetStatistics()
        {
            TestsCompleted = 0;
            TestsPassed = 0;
            TestsFailed = 0;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Event arguments for test socket events
    /// </summary>
    public class TestSocketEventArgs : EventArgs
    {
        public TestSocket Socket { get; }
        public string? Message { get; }

        public TestSocketEventArgs(TestSocket socket, string? message = null)
        {
            Socket = socket;
            Message = message;
        }
    }

    /// <summary>
    /// Manages test sockets for parallel testing
    /// </summary>
    public sealed class TestSocketManager
    {
        private static readonly Lazy<TestSocketManager> _instance = 
            new Lazy<TestSocketManager>(() => new TestSocketManager());
        
        private readonly ConcurrentDictionary<int, TestSocket> _sockets = 
            new ConcurrentDictionary<int, TestSocket>();
        private readonly SemaphoreSlim _executionLock = new SemaphoreSlim(1, 1);
        private int _maxConcurrentTests = 4;
        private bool _isRunning;
        private CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        /// Gets the singleton instance
        /// </summary>
        public static TestSocketManager Instance => _instance.Value;

        /// <summary>
        /// Event raised when a socket state changes
        /// </summary>
        public event EventHandler<TestSocketEventArgs>? SocketStateChanged;

        /// <summary>
        /// Event raised when a socket completes a test
        /// </summary>
        public event EventHandler<TestSocketEventArgs>? TestCompleted;

        /// <summary>
        /// Event raised when a socket encounters an error
        /// </summary>
        public event EventHandler<TestSocketEventArgs>? SocketError;

        /// <summary>
        /// Maximum number of concurrent tests
        /// </summary>
        public int MaxConcurrentTests
        {
            get => _maxConcurrentTests;
            set => _maxConcurrentTests = Math.Max(1, value);
        }

        /// <summary>
        /// Whether the manager is currently running tests
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// All registered sockets
        /// </summary>
        public IEnumerable<TestSocket> Sockets => _sockets.Values;

        /// <summary>
        /// Number of active sockets
        /// </summary>
        public int ActiveSocketCount => _sockets.Values.Count(s => s.State == TestSocketState.Running);

        private TestSocketManager() { }

        /// <summary>
        /// Create a new test socket
        /// </summary>
        public TestSocket CreateSocket(string? name = null)
        {
            var id = _sockets.Count > 0 ? _sockets.Keys.Max() + 1 : 1;
            var socket = new TestSocket(id, name);
            _sockets[id] = socket;
            return socket;
        }

        /// <summary>
        /// Get a socket by ID
        /// </summary>
        public TestSocket? GetSocket(int id)
        {
            return _sockets.TryGetValue(id, out var socket) ? socket : null;
        }

        /// <summary>
        /// Remove a test socket
        /// </summary>
        public bool RemoveSocket(int id)
        {
            if (_sockets.TryRemove(id, out var socket))
            {
                socket.State = TestSocketState.Disabled;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Enable a test socket
        /// </summary>
        public void EnableSocket(int id)
        {
            if (_sockets.TryGetValue(id, out var socket))
            {
                socket.IsEnabled = true;
                socket.State = TestSocketState.Idle;
            }
        }

        /// <summary>
        /// Disable a test socket
        /// </summary>
        public void DisableSocket(int id)
        {
            if (_sockets.TryGetValue(id, out var socket))
            {
                socket.IsEnabled = false;
                socket.State = TestSocketState.Disabled;
            }
        }

        /// <summary>
        /// Start a test on a specific socket
        /// </summary>
        public async Task<bool> StartTestAsync(int socketId, Sequence sequence, string serialNumber, 
            CancellationToken cancellationToken = default)
        {
            if (!_sockets.TryGetValue(socketId, out var socket))
                return false;

            if (!socket.IsEnabled || socket.State == TestSocketState.Running)
                return false;

            try
            {
                socket.State = TestSocketState.Running;
                socket.CurrentSerialNumber = serialNumber;
                socket.CurrentSequence = sequence;
                socket.LastActivity = DateTime.Now;
                
                SocketStateChanged?.Invoke(this, new TestSocketEventArgs(socket, "Test started"));

                // Execute the sequence
                var engine = new Engine();
                var context = new Context();
                context.SetValue("SerialNumber", serialNumber);
                context.SetValue("SocketId", socketId);

                await engine.ExecuteSequenceAsync(sequence);

                // Update statistics
                socket.TestsCompleted++;
                bool passed = sequence.Steps.All(s => 
                    s.Status == StepStatus.Passed || 
                    s.Status == StepStatus.Idle);
                
                if (passed)
                    socket.TestsPassed++;
                else
                    socket.TestsFailed++;

                socket.State = TestSocketState.Idle;
                socket.LastActivity = DateTime.Now;
                
                TestCompleted?.Invoke(this, new TestSocketEventArgs(socket, 
                    passed ? "Test passed" : "Test failed"));
                
                return true;
            }
            catch (OperationCanceledException)
            {
                socket.State = TestSocketState.Idle;
                return false;
            }
            catch (Exception ex)
            {
                socket.State = TestSocketState.Error;
                SocketError?.Invoke(this, new TestSocketEventArgs(socket, ex.Message));
                return false;
            }
        }

        /// <summary>
        /// Start batch testing across all enabled sockets
        /// </summary>
        public async Task StartBatchTestingAsync(Sequence sequenceTemplate, 
            Func<int, Task<string?>> getSerialNumber,
            CancellationToken cancellationToken = default)
        {
            if (_isRunning)
                return;

            await _executionLock.WaitAsync(cancellationToken);
            
            try
            {
                _isRunning = true;
                _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

                var enabledSockets = _sockets.Values.Where(s => s.IsEnabled).ToList();
                var semaphore = new SemaphoreSlim(MaxConcurrentTests, MaxConcurrentTests);
                var tasks = new List<Task>();

                foreach (var socket in enabledSockets)
                {
                    socket.State = TestSocketState.Ready;
                }

                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    foreach (var socket in enabledSockets)
                    {
                        if (socket.State != TestSocketState.Ready)
                            continue;

                        await semaphore.WaitAsync(_cancellationTokenSource.Token);
                        
                        socket.State = TestSocketState.WaitingForUUT;
                        var serialNumber = await getSerialNumber(socket.Id);
                        
                        if (string.IsNullOrEmpty(serialNumber))
                        {
                            semaphore.Release();
                            socket.State = TestSocketState.Ready;
                            continue;
                        }

                        // Clone the sequence for this socket
                        var sequence = CloneSequence(sequenceTemplate);
                        
                        var task = Task.Run(async () =>
                        {
                            try
                            {
                                await StartTestAsync(socket.Id, sequence, serialNumber, 
                                    _cancellationTokenSource.Token);
                            }
                            finally
                            {
                                socket.State = TestSocketState.Ready;
                                semaphore.Release();
                            }
                        }, _cancellationTokenSource.Token);
                        
                        tasks.Add(task);
                    }

                    await Task.Delay(100, _cancellationTokenSource.Token);
                }

                await Task.WhenAll(tasks);
            }
            finally
            {
                _isRunning = false;
                _executionLock.Release();
            }
        }

        /// <summary>
        /// Stop all testing
        /// </summary>
        public void StopAllTesting()
        {
            _cancellationTokenSource?.Cancel();
            
            foreach (var socket in _sockets.Values)
            {
                if (socket.State == TestSocketState.Running)
                {
                    socket.State = TestSocketState.Idle;
                }
            }
        }

        /// <summary>
        /// Reset all socket statistics
        /// </summary>
        public void ResetAllStatistics()
        {
            foreach (var socket in _sockets.Values)
            {
                socket.ResetStatistics();
            }
        }

        /// <summary>
        /// Get aggregated statistics
        /// </summary>
        public (int totalTests, int totalPassed, int totalFailed, double passRate) GetAggregatedStatistics()
        {
            int totalTests = _sockets.Values.Sum(s => s.TestsCompleted);
            int totalPassed = _sockets.Values.Sum(s => s.TestsPassed);
            int totalFailed = _sockets.Values.Sum(s => s.TestsFailed);
            double passRate = totalTests > 0 ? (double)totalPassed / totalTests * 100 : 0;
            
            return (totalTests, totalPassed, totalFailed, passRate);
        }

        private Sequence CloneSequence(Sequence original)
        {
            var clone = new Sequence
            {
                Name = original.Name,
                Description = original.Description
            };
            
            // Deep clone steps (simplified - real implementation would be more thorough)
            foreach (var step in original.Steps)
            {
                // Use reflection or a clone method to create step copies
                clone.Steps.Add(step);
            }
            
            return clone;
        }
    }
}

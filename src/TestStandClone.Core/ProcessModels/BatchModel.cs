using System.Collections.Concurrent;

namespace TestStandClone.Core.ProcessModels
{
    /// <summary>
    /// Batch process model - tests a group of UUTs with synchronized steps.
    /// Similar to TestStand's Batch Model.
    /// </summary>
    public class BatchModel : ProcessModel
    {
        private readonly ConcurrentDictionary<int, Engine> _engines = new ConcurrentDictionary<int, Engine>();
        private readonly List<UUT> _batch = new List<UUT>();
        private readonly SemaphoreSlim _syncBarrier = new SemaphoreSlim(0);
        private int _completedCount;
        private readonly object _syncLock = new object();

        /// <summary>
        /// Number of sockets/positions in the batch.
        /// </summary>
        public int BatchSize { get; set; } = 4;

        /// <summary>
        /// Creates a new batch process model.
        /// </summary>
        public BatchModel()
        {
            Name = "Batch Model";
        }

        /// <summary>
        /// Runs the sequence for a batch of UUTs with synchronization.
        /// </summary>
        public async Task<List<UUTResult>> RunBatchAsync(Sequence sequence, List<UUT> uuts)
        {
            State = ProcessModelState.Running;
            _batch.Clear();
            _batch.AddRange(uuts);
            _completedCount = 0;

            // Start all UUTs in parallel
            var tasks = uuts.Select(uut => RunAsync(sequence, uut));
            var results = await Task.WhenAll(tasks);

            State = ProcessModelState.Completed;
            return results.ToList();
        }

        /// <summary>
        /// Runs the sequence for a single UUT in the batch.
        /// </summary>
        public override async Task<UUTResult> RunAsync(Sequence sequence, UUT uut)
        {
            var engine = new Engine();
            _engines[uut.SocketIndex] = engine;

            try
            {
                await OnPreTestAsync(uut);
                await engine.ExecuteSequenceAsync(sequence);
                await OnPostTestAsync(uut, sequence);

                return uut.Result;
            }
            catch (Exception ex)
            {
                uut.Result = UUTResult.Error;
                uut.Properties["ErrorMessage"] = ex.Message;
                return UUTResult.Error;
            }
            finally
            {
                _engines.TryRemove(uut.SocketIndex, out _);
                
                lock (_syncLock)
                {
                    _completedCount++;
                    if (_completedCount >= _batch.Count)
                    {
                        State = ProcessModelState.Completed;
                    }
                }
            }
        }

        /// <summary>
        /// Synchronizes all batch threads at a synchronization point.
        /// Call this in custom steps that need batch synchronization.
        /// </summary>
        public async Task SynchronizeAsync(int socketIndex)
        {
            int waitingCount;
            lock (_syncLock)
            {
                waitingCount = Interlocked.Increment(ref _completedCount);
            }

            // If all threads have reached the barrier, release them
            if (waitingCount >= _batch.Count)
            {
                _completedCount = 0;
                // Release all waiting threads
                for (int i = 0; i < _batch.Count; i++)
                {
                    _syncBarrier.Release();
                }
            }
            else
            {
                // Wait for other threads
                await _syncBarrier.WaitAsync();
            }
        }

        /// <summary>
        /// Gets a summary of batch results.
        /// </summary>
        public BatchSummary GetBatchSummary()
        {
            return new BatchSummary
            {
                TotalCount = _batch.Count,
                PassedCount = _batch.Count(u => u.Result == UUTResult.Passed),
                FailedCount = _batch.Count(u => u.Result == UUTResult.Failed),
                ErrorCount = _batch.Count(u => u.Result == UUTResult.Error),
                SkippedCount = _batch.Count(u => u.Result == UUTResult.Skipped),
                UUTs = _batch.ToList()
            };
        }

        /// <summary>
        /// Aborts all running tests in the batch.
        /// </summary>
        public void AbortAll()
        {
            foreach (var engine in _engines.Values)
            {
                engine.Abort();
            }
        }
    }

    /// <summary>
    /// Summary of batch test results.
    /// </summary>
    public class BatchSummary
    {
        public int TotalCount { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public int ErrorCount { get; set; }
        public int SkippedCount { get; set; }
        public List<UUT> UUTs { get; set; } = new List<UUT>();

        public double PassRate => TotalCount > 0 ? (double)PassedCount / TotalCount * 100 : 0;
        public bool AllPassed => PassedCount == TotalCount;
    }
}

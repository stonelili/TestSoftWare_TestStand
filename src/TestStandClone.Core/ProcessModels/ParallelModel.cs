using System.Collections.Concurrent;

namespace TestStandClone.Core.ProcessModels
{
    /// <summary>
    /// Parallel process model - tests multiple UUTs simultaneously.
    /// Similar to TestStand's Parallel Model.
    /// </summary>
    public class ParallelModel : ProcessModel
    {
        private readonly ConcurrentDictionary<int, Engine> _engines = new ConcurrentDictionary<int, Engine>();
        private readonly ConcurrentDictionary<int, UUT> _uuts = new ConcurrentDictionary<int, UUT>();

        /// <summary>
        /// Maximum number of parallel executions.
        /// </summary>
        public int MaxParallelCount { get; set; } = 4;

        /// <summary>
        /// Creates a new parallel process model.
        /// </summary>
        public ParallelModel()
        {
            Name = "Parallel Model";
        }

        /// <summary>
        /// Runs the sequence for a single UUT (non-blocking).
        /// </summary>
        public override async Task<UUTResult> RunAsync(Sequence sequence, UUT uut)
        {
            var engine = new Engine();
            _engines[uut.SocketIndex] = engine;
            _uuts[uut.SocketIndex] = uut;

            State = ProcessModelState.Running;

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
                _uuts.TryRemove(uut.SocketIndex, out _);
                
                if (_engines.IsEmpty)
                {
                    State = ProcessModelState.Completed;
                }
            }
        }

        /// <summary>
        /// Runs the sequence for multiple UUTs in parallel.
        /// </summary>
        public async Task<List<UUTResult>> RunParallelAsync(Sequence sequence, List<UUT> uuts)
        {
            State = ProcessModelState.Running;

            var semaphore = new SemaphoreSlim(MaxParallelCount);
            var tasks = uuts.Select(async uut =>
            {
                await semaphore.WaitAsync();
                try
                {
                    // Create a copy of the sequence for each UUT
                    var sequenceCopy = CloneSequence(sequence);
                    return await RunAsync(sequenceCopy, uut);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(tasks);
            State = ProcessModelState.Completed;
            return results.ToList();
        }

        /// <summary>
        /// Creates a shallow clone of a sequence for parallel execution.
        /// </summary>
        private static Sequence CloneSequence(Sequence original)
        {
            var clone = new Sequence
            {
                Name = original.Name,
                Description = original.Description
            };

            // Clone steps - in a real implementation, this would deep clone
            foreach (var step in original.SetupSteps)
            {
                clone.SetupSteps.Add(step);
            }
            foreach (var step in original.MainSteps)
            {
                clone.MainSteps.Add(step);
            }
            foreach (var step in original.CleanupSteps)
            {
                clone.CleanupSteps.Add(step);
            }

            clone.SyncStepsCollection();
            return clone;
        }

        /// <summary>
        /// Gets the engine for a specific socket.
        /// </summary>
        public Engine? GetEngine(int socketIndex)
        {
            return _engines.TryGetValue(socketIndex, out var engine) ? engine : null;
        }

        /// <summary>
        /// Gets the UUT for a specific socket.
        /// </summary>
        public UUT? GetUUT(int socketIndex)
        {
            return _uuts.TryGetValue(socketIndex, out var uut) ? uut : null;
        }

        /// <summary>
        /// Aborts all running tests.
        /// </summary>
        public void AbortAll()
        {
            foreach (var engine in _engines.Values)
            {
                engine.Abort();
            }
        }
    }
}

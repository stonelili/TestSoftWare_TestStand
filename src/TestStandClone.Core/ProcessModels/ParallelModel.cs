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
        /// Creates a deep clone of a sequence for parallel execution.
        /// Each parallel thread gets its own step instances to avoid race conditions.
        /// </summary>
        private static Sequence CloneSequence(Sequence original)
        {
            var clone = new Sequence
            {
                Name = original.Name,
                Description = original.Description
            };

            // Deep clone steps - each thread needs its own step instances
            // Note: This is a simplified clone that creates new step instances
            // For full functionality, step-specific cloning should be implemented
            foreach (var step in original.SetupSteps)
            {
                clone.SetupSteps.Add(CloneStep(step));
            }
            foreach (var step in original.MainSteps)
            {
                clone.MainSteps.Add(CloneStep(step));
            }
            foreach (var step in original.CleanupSteps)
            {
                clone.CleanupSteps.Add(CloneStep(step));
            }

            clone.SyncStepsCollection();
            return clone;
        }

        /// <summary>
        /// Creates a clone of a step with fresh state.
        /// </summary>
        private static TestStep CloneStep(TestStep original)
        {
            // Create new instance based on step type
            TestStep clone = original switch
            {
                DelayStep delay => new DelayStep(delay.Name, delay.DelayMilliseconds),
                NumericLimitStep numeric => new NumericLimitStep(numeric.Name, numeric.LowerLimit, numeric.UpperLimit)
                {
                    MinGeneratedValue = numeric.MinGeneratedValue,
                    MaxGeneratedValue = numeric.MaxGeneratedValue
                },
                Steps.PassFailStep passFail => new Steps.PassFailStep(passFail.Name, passFail.ExpectedResult),
                Steps.StringValueStep stringVal => new Steps.StringValueStep(stringVal.Name, stringVal.ExpectedValue)
                {
                    ActualValue = stringVal.ActualValue
                },
                Steps.ActionStep action => new Steps.ActionStep(action.Name)
                {
                    Action = action.Action,
                    SyncAction = action.SyncAction
                },
                Steps.LabelStep label => new Steps.LabelStep(label.Name, label.LabelId),
                Steps.GotoStep gotoStep => new Steps.GotoStep(gotoStep.Name, gotoStep.TargetLabelId),
                Steps.LoopStep loop => new Steps.LoopStep(loop.Name, loop.Iterations, loop.Type),
                Steps.MessagePopupStep popup => new Steps.MessagePopupStep(popup.Name, popup.Title, popup.Message)
                {
                    Buttons = popup.Buttons
                },
                _ => throw new NotSupportedException($"Step type {original.GetType().Name} is not supported for cloning")
            };

            clone.Description = original.Description;
            clone.IsEnabled = original.IsEnabled;
            clone.HasBreakpoint = original.HasBreakpoint;

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

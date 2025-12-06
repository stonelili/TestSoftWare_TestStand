namespace TestStandClone.Core.ProcessModels
{
    /// <summary>
    /// Sequential process model - tests one UUT at a time.
    /// Similar to TestStand's Sequential Model.
    /// </summary>
    public class SequentialModel : ProcessModel
    {
        /// <summary>
        /// Creates a new sequential process model.
        /// </summary>
        public SequentialModel()
        {
            Name = "Sequential Model";
        }

        /// <summary>
        /// Runs the sequence for a single UUT.
        /// </summary>
        public override async Task<UUTResult> RunAsync(Sequence sequence, UUT uut)
        {
            State = ProcessModelState.Running;
            CurrentUUT = uut;
            Engine = new Engine();

            try
            {
                // Pre-test
                await OnPreTestAsync(uut);

                // Execute sequence
                await Engine.ExecuteSequenceAsync(sequence);

                // Post-test
                await OnPostTestAsync(uut, sequence);

                State = ProcessModelState.Completed;
                return uut.Result;
            }
            catch (Exception ex)
            {
                uut.Result = UUTResult.Error;
                uut.Properties["ErrorMessage"] = ex.Message;
                State = ProcessModelState.Error;
                return UUTResult.Error;
            }
            finally
            {
                CurrentUUT = null;
            }
        }
    }
}

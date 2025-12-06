namespace TestStandClone.Core.ProcessModels
{
    /// <summary>
    /// Represents a Unit Under Test (UUT) being tested.
    /// </summary>
    public class UUT
    {
        /// <summary>
        /// Unique identifier for the UUT.
        /// </summary>
        public string SerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// Model/Part number of the UUT.
        /// </summary>
        public string ModelNumber { get; set; } = string.Empty;

        /// <summary>
        /// Batch or lot number.
        /// </summary>
        public string BatchNumber { get; set; } = string.Empty;

        /// <summary>
        /// Socket/position number for batch testing.
        /// </summary>
        public int SocketIndex { get; set; }

        /// <summary>
        /// Overall test result.
        /// </summary>
        public UUTResult Result { get; set; } = UUTResult.NotTested;

        /// <summary>
        /// Test start time.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Test end time.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Operator who ran the test.
        /// </summary>
        public string OperatorName { get; set; } = string.Empty;

        /// <summary>
        /// Additional properties.
        /// </summary>
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Total test time.
        /// </summary>
        public TimeSpan? TestTime => EndTime.HasValue && StartTime.HasValue 
            ? EndTime.Value - StartTime.Value 
            : null;

        /// <summary>
        /// Creates a new UUT.
        /// </summary>
        public UUT()
        {
        }

        /// <summary>
        /// Creates a new UUT with serial number.
        /// </summary>
        public UUT(string serialNumber)
        {
            SerialNumber = serialNumber;
        }
    }

    /// <summary>
    /// Result of UUT test.
    /// </summary>
    public enum UUTResult
    {
        NotTested,
        Passed,
        Failed,
        Error,
        Skipped,
        Terminated
    }
}

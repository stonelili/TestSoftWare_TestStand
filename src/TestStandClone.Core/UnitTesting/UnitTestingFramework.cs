using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TestStandClone.Core.UnitTesting
{
    public enum TestCaseStatus { NotRun, Running, Passed, Failed, Skipped, Error }
    public enum AssertionResult { Passed, Failed }

    public class TestAssertion
    {
        public string Description { get; set; } = string.Empty;
        public AssertionResult Result { get; set; } = AssertionResult.Passed;
        public string Expected { get; set; } = string.Empty;
        public string Actual { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public class TestCase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public TestCaseStatus Status { get; set; } = TestCaseStatus.NotRun;
        public List<TestAssertion> Assertions { get; set; } = new();
        public TimeSpan Duration { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public Func<TestContext, Task>? TestMethod { get; set; }
    }

    public class TestSuite
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<TestCase> TestCases { get; set; } = new();
        public Func<Task>? SetUp { get; set; }
        public Func<Task>? TearDown { get; set; }
        public int Passed => TestCases.Count(t => t.Status == TestCaseStatus.Passed);
        public int Failed => TestCases.Count(t => t.Status == TestCaseStatus.Failed);
        public int Skipped => TestCases.Count(t => t.Status == TestCaseStatus.Skipped);
        public int Total => TestCases.Count;
    }

    public class TestContext
    {
        public TestCase CurrentTest { get; set; } = new();
        public Dictionary<string, object> Data { get; set; } = new();

        public void AssertEqual<T>(T expected, T actual, string message = "")
        {
            var assertion = new TestAssertion
            {
                Description = message,
                Expected = expected?.ToString() ?? "null",
                Actual = actual?.ToString() ?? "null"
            };

            if (EqualityComparer<T>.Default.Equals(expected, actual))
            {
                assertion.Result = AssertionResult.Passed;
            }
            else
            {
                assertion.Result = AssertionResult.Failed;
                assertion.Message = $"Expected '{expected}' but got '{actual}'";
            }
            CurrentTest.Assertions.Add(assertion);
        }

        public void AssertTrue(bool condition, string message = "")
        {
            var assertion = new TestAssertion
            {
                Description = message,
                Expected = "True",
                Actual = condition.ToString(),
                Result = condition ? AssertionResult.Passed : AssertionResult.Failed,
                Message = condition ? "" : "Condition was false"
            };
            CurrentTest.Assertions.Add(assertion);
        }

        public void AssertNotNull(object? obj, string message = "")
        {
            var assertion = new TestAssertion
            {
                Description = message,
                Expected = "Not null",
                Actual = obj == null ? "null" : "Not null",
                Result = obj != null ? AssertionResult.Passed : AssertionResult.Failed,
                Message = obj == null ? "Object was null" : ""
            };
            CurrentTest.Assertions.Add(assertion);
        }
    }

    public class TestRunResult
    {
        public string SuiteId { get; set; } = string.Empty;
        public string SuiteName { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime EndTime { get; set; } = DateTime.Now;
        public TimeSpan Duration => EndTime - StartTime;
        public int Passed { get; set; }
        public int Failed { get; set; }
        public int Skipped { get; set; }
        public int Total { get; set; }
        public double PassRate => Total > 0 ? (double)Passed / Total * 100 : 0;
        public List<TestCase> TestCases { get; set; } = new();
    }

    public class UnitTestingManager
    {
        private static readonly Lazy<UnitTestingManager> _instance = new(() => new UnitTestingManager());
        public static UnitTestingManager Instance => _instance.Value;
        private readonly Dictionary<string, TestSuite> _suites = new();
        private readonly List<TestRunResult> _results = new();
        private readonly object _lock = new();

        public event EventHandler<TestCase>? TestStarted;
        public event EventHandler<TestCase>? TestCompleted;
        public event EventHandler<TestRunResult>? SuiteCompleted;

        private UnitTestingManager() { }

        public TestSuite CreateSuite(string name)
        {
            var suite = new TestSuite { Name = name };
            lock (_lock) { _suites[suite.Id] = suite; }
            return suite;
        }

        public void AddTestCase(string suiteId, TestCase testCase)
        {
            lock (_lock)
            {
                if (_suites.TryGetValue(suiteId, out var suite))
                {
                    suite.TestCases.Add(testCase);
                }
            }
        }

        public async Task<TestRunResult> RunSuite(string suiteId)
        {
            TestSuite? suite;
            lock (_lock)
            {
                if (!_suites.TryGetValue(suiteId, out suite))
                    return new TestRunResult { SuiteId = suiteId };
            }

            var result = new TestRunResult
            {
                SuiteId = suiteId,
                SuiteName = suite.Name,
                StartTime = DateTime.Now
            };

            if (suite.SetUp != null)
                await suite.SetUp();

            foreach (var testCase in suite.TestCases)
            {
                await RunTestCase(testCase);
                result.TestCases.Add(testCase);
            }

            if (suite.TearDown != null)
                await suite.TearDown();

            result.EndTime = DateTime.Now;
            result.Passed = suite.Passed;
            result.Failed = suite.Failed;
            result.Skipped = suite.Skipped;
            result.Total = suite.Total;

            lock (_lock) { _results.Add(result); }
            SuiteCompleted?.Invoke(this, result);
            return result;
        }

        private async Task RunTestCase(TestCase testCase)
        {
            testCase.Status = TestCaseStatus.Running;
            testCase.StartTime = DateTime.Now;
            TestStarted?.Invoke(this, testCase);

            try
            {
                if (testCase.TestMethod != null)
                {
                    var context = new TestContext { CurrentTest = testCase };
                    await testCase.TestMethod(context);
                    testCase.Status = testCase.Assertions.All(a => a.Result == AssertionResult.Passed)
                        ? TestCaseStatus.Passed
                        : TestCaseStatus.Failed;
                }
                else
                {
                    testCase.Status = TestCaseStatus.Skipped;
                }
            }
            catch (Exception ex)
            {
                testCase.Status = TestCaseStatus.Error;
                testCase.ErrorMessage = ex.Message;
            }

            testCase.EndTime = DateTime.Now;
            testCase.Duration = testCase.EndTime.Value - testCase.StartTime.Value;
            TestCompleted?.Invoke(this, testCase);
        }

        public List<TestSuite> GetAllSuites()
        {
            lock (_lock) { return _suites.Values.ToList(); }
        }

        public List<TestRunResult> GetResults(int count = 100)
        {
            lock (_lock) { return _results.TakeLast(count).ToList(); }
        }
    }
}

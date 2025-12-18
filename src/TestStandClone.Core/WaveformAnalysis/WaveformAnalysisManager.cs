using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.WaveformAnalysis
{
    /// <summary>
    /// Waveform data point
    /// </summary>
    public class WaveformPoint
    {
        public double Time { get; set; }
        public double Value { get; set; }
    }

    /// <summary>
    /// Waveform data
    /// </summary>
    public class Waveform
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public List<WaveformPoint> Points { get; set; } = new List<WaveformPoint>();
        public double SampleRate { get; set; }
        public string Unit { get; set; } = "V";
        public string TimeUnit { get; set; } = "s";
        public DateTime CapturedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public double[] GetValues() => Points.Select(p => p.Value).ToArray();
        public double[] GetTimes() => Points.Select(p => p.Time).ToArray();
    }

    /// <summary>
    /// Waveform statistics
    /// </summary>
    public class WaveformStatistics
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double Mean { get; set; }
        public double Median { get; set; }
        public double StdDev { get; set; }
        public double RMS { get; set; }
        public double PeakToPeak { get; set; }
        public int SampleCount { get; set; }
        public double Duration { get; set; }
    }

    /// <summary>
    /// Peak detection result
    /// </summary>
    public class PeakInfo
    {
        public int Index { get; set; }
        public double Time { get; set; }
        public double Value { get; set; }
        public bool IsMaximum { get; set; }
        public double Prominence { get; set; }
    }

    /// <summary>
    /// Frequency analysis result
    /// </summary>
    public class FrequencyInfo
    {
        public double DominantFrequency { get; set; }
        public double Amplitude { get; set; }
        public List<(double Frequency, double Amplitude)> Harmonics { get; set; } = new List<(double, double)>();
        public double THD { get; set; } // Total Harmonic Distortion
    }

    /// <summary>
    /// Waveform comparison result
    /// </summary>
    public class WaveformComparisonResult
    {
        public double Correlation { get; set; }
        public double MaxDeviation { get; set; }
        public double MeanDeviation { get; set; }
        public double RMSError { get; set; }
        public bool IsMatch { get; set; }
        public List<int> DeviationIndices { get; set; } = new List<int>();
    }

    /// <summary>
    /// Waveform analysis manager singleton
    /// </summary>
    public class WaveformAnalysisManager
    {
        private static readonly Lazy<WaveformAnalysisManager> _instance = new Lazy<WaveformAnalysisManager>(() => new WaveformAnalysisManager());
        public static WaveformAnalysisManager Instance => _instance.Value;

        private readonly List<Waveform> _storedWaveforms = new List<Waveform>();
        private readonly object _lock = new object();

        private WaveformAnalysisManager() { }

        public WaveformStatistics CalculateStatistics(Waveform waveform)
        {
            var values = waveform.GetValues();
            if (values.Length == 0)
                return new WaveformStatistics();

            var sorted = values.OrderBy(v => v).ToArray();
            var mean = values.Average();
            var sumSquares = values.Sum(v => v * v);

            return new WaveformStatistics
            {
                Min = sorted.First(),
                Max = sorted.Last(),
                Mean = mean,
                Median = values.Length % 2 == 0 
                    ? (sorted[values.Length / 2 - 1] + sorted[values.Length / 2]) / 2 
                    : sorted[values.Length / 2],
                StdDev = Math.Sqrt(values.Sum(v => Math.Pow(v - mean, 2)) / values.Length),
                RMS = Math.Sqrt(sumSquares / values.Length),
                PeakToPeak = sorted.Last() - sorted.First(),
                SampleCount = values.Length,
                Duration = waveform.Points.Count > 1 
                    ? waveform.Points.Last().Time - waveform.Points.First().Time 
                    : 0
            };
        }

        public List<PeakInfo> FindPeaks(Waveform waveform, double threshold = 0, int minDistance = 1)
        {
            var peaks = new List<PeakInfo>();
            var values = waveform.GetValues();
            var times = waveform.GetTimes();

            for (int i = 1; i < values.Length - 1; i++)
            {
                var isLocalMax = values[i] > values[i - 1] && values[i] > values[i + 1];
                var isLocalMin = values[i] < values[i - 1] && values[i] < values[i + 1];

                if ((isLocalMax || isLocalMin) && Math.Abs(values[i]) >= threshold)
                {
                    if (peaks.Count == 0 || i - peaks.Last().Index >= minDistance)
                    {
                        peaks.Add(new PeakInfo
                        {
                            Index = i,
                            Time = times[i],
                            Value = values[i],
                            IsMaximum = isLocalMax,
                            Prominence = CalculateProminence(values, i, isLocalMax)
                        });
                    }
                }
            }

            return peaks;
        }

        private double CalculateProminence(double[] values, int peakIndex, bool isMaximum)
        {
            var peakValue = values[peakIndex];
            double leftMin = peakValue, rightMin = peakValue;

            for (int i = peakIndex - 1; i >= 0; i--)
            {
                if (isMaximum ? values[i] > peakValue : values[i] < peakValue) break;
                leftMin = isMaximum ? Math.Min(leftMin, values[i]) : Math.Max(leftMin, values[i]);
            }

            for (int i = peakIndex + 1; i < values.Length; i++)
            {
                if (isMaximum ? values[i] > peakValue : values[i] < peakValue) break;
                rightMin = isMaximum ? Math.Min(rightMin, values[i]) : Math.Max(rightMin, values[i]);
            }

            var baseLevel = isMaximum ? Math.Max(leftMin, rightMin) : Math.Min(leftMin, rightMin);
            return Math.Abs(peakValue - baseLevel);
        }

        public FrequencyInfo AnalyzeFrequency(Waveform waveform)
        {
            var values = waveform.GetValues();
            var n = values.Length;
            
            if (n == 0 || waveform.SampleRate <= 0)
                return new FrequencyInfo();

            // Simple FFT-like frequency estimation using autocorrelation
            var result = new FrequencyInfo();
            var mean = values.Average();
            var normalized = values.Select(v => v - mean).ToArray();

            // Find dominant frequency using zero-crossings
            int zeroCrossings = 0;
            for (int i = 1; i < normalized.Length; i++)
            {
                if (normalized[i - 1] * normalized[i] < 0)
                    zeroCrossings++;
            }

            var duration = (double)n / waveform.SampleRate;
            result.DominantFrequency = zeroCrossings / (2 * duration);
            result.Amplitude = normalized.Max() - normalized.Min();

            return result;
        }

        public WaveformComparisonResult Compare(Waveform waveform1, Waveform waveform2, double deviationThreshold = 0.1)
        {
            var result = new WaveformComparisonResult();
            var values1 = waveform1.GetValues();
            var values2 = waveform2.GetValues();

            var minLen = Math.Min(values1.Length, values2.Length);
            if (minLen == 0)
            {
                result.IsMatch = false;
                return result;
            }

            var deviations = new List<double>();
            for (int i = 0; i < minLen; i++)
            {
                var deviation = Math.Abs(values1[i] - values2[i]);
                deviations.Add(deviation);
                if (deviation > deviationThreshold)
                    result.DeviationIndices.Add(i);
            }

            result.MaxDeviation = deviations.Max();
            result.MeanDeviation = deviations.Average();
            result.RMSError = Math.Sqrt(deviations.Sum(d => d * d) / minLen);
            result.Correlation = CalculateCorrelation(values1.Take(minLen).ToArray(), values2.Take(minLen).ToArray());
            result.IsMatch = result.MaxDeviation <= deviationThreshold && result.Correlation >= 0.9;

            return result;
        }

        private double CalculateCorrelation(double[] x, double[] y)
        {
            var n = x.Length;
            var meanX = x.Average();
            var meanY = y.Average();

            double sumXY = 0, sumX2 = 0, sumY2 = 0;
            for (int i = 0; i < n; i++)
            {
                var dx = x[i] - meanX;
                var dy = y[i] - meanY;
                sumXY += dx * dy;
                sumX2 += dx * dx;
                sumY2 += dy * dy;
            }

            var denominator = Math.Sqrt(sumX2 * sumY2);
            return denominator > 0 ? sumXY / denominator : 0;
        }

        public Waveform Filter(Waveform waveform, double cutoffFrequency, bool lowPass = true)
        {
            var filtered = new Waveform
            {
                Name = $"{waveform.Name}_filtered",
                SampleRate = waveform.SampleRate,
                Unit = waveform.Unit,
                TimeUnit = waveform.TimeUnit
            };

            var values = waveform.GetValues();
            var times = waveform.GetTimes();
            var alpha = 2 * Math.PI * cutoffFrequency / waveform.SampleRate;
            var smoothing = alpha / (alpha + 1);

            var filteredValues = new double[values.Length];
            filteredValues[0] = values[0];

            for (int i = 1; i < values.Length; i++)
            {
                filteredValues[i] = lowPass
                    ? filteredValues[i - 1] + smoothing * (values[i] - filteredValues[i - 1])
                    : values[i] - (filteredValues[i - 1] + smoothing * (values[i] - filteredValues[i - 1])) + filteredValues[i - 1];
            }

            for (int i = 0; i < values.Length; i++)
                filtered.Points.Add(new WaveformPoint { Time = times[i], Value = filteredValues[i] });

            return filtered;
        }

        public void StoreWaveform(Waveform waveform) { lock (_lock) { _storedWaveforms.Add(waveform); } }
        public Waveform? GetWaveform(string id) { lock (_lock) { return _storedWaveforms.FirstOrDefault(w => w.Id == id); } }
        public List<Waveform> GetAllWaveforms() { lock (_lock) { return _storedWaveforms.ToList(); } }
        public void ClearWaveforms() { lock (_lock) { _storedWaveforms.Clear(); } }
    }
}

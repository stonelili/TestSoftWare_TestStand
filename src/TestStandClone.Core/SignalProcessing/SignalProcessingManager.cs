using System;
using System.Collections.Generic;
using System.Linq;

namespace TestStandClone.Core.SignalProcessing
{
    // Signal with samples
    public class Signal
    {
        public string Name { get; set; }
        public double[] Samples { get; set; }
        public double SampleRate { get; set; }
        public string Unit { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // Filter types
    public enum FilterType { LowPass, HighPass, BandPass, BandStop, Notch }

    // Filter configuration
    public class FilterConfig
    {
        public FilterType Type { get; set; }
        public double CutoffFrequency { get; set; }
        public double? HighCutoffFrequency { get; set; } // For band filters
        public int Order { get; set; }
        public double SampleRate { get; set; }
    }

    // FFT result
    public class FFTResult
    {
        public double[] Frequencies { get; set; }
        public double[] Magnitudes { get; set; }
        public double[] Phases { get; set; }
        public double DominantFrequency { get; set; }
        public double DominantMagnitude { get; set; }
    }

    // Signal statistics
    public class SignalStatistics
    {
        public double Mean { get; set; }
        public double RMS { get; set; }
        public double PeakToPeak { get; set; }
        public double CrestFactor { get; set; }
        public double SNR { get; set; } // Signal-to-Noise Ratio
        public double THD { get; set; } // Total Harmonic Distortion
    }

    // Window types for FFT
    public enum WindowType { Rectangular, Hanning, Hamming, Blackman, Kaiser }

    // Signal Processing Manager
    public class SignalProcessingManager
    {
        private static SignalProcessingManager _instance;
        private static readonly object _lock = new object();
        private Dictionary<string, Signal> _signals;

        private SignalProcessingManager()
        {
            _signals = new Dictionary<string, Signal>();
        }

        public static SignalProcessingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                            _instance = new SignalProcessingManager();
                    }
                }
                return _instance;
            }
        }

        public void RegisterSignal(Signal signal)
        {
            _signals[signal.Name] = signal;
        }

        public Signal GetSignal(string name)
        {
            return _signals.TryGetValue(name, out var signal) ? signal : null;
        }

        // Apply filter to signal
        public Signal ApplyFilter(Signal input, FilterConfig config)
        {
            // Simplified filter implementation
            var filtered = new Signal
            {
                Name = input.Name + "_Filtered",
                SampleRate = input.SampleRate,
                Unit = input.Unit,
                Timestamp = DateTime.Now,
                Samples = new double[input.Samples.Length]
            };

            // Simple moving average as placeholder
            int windowSize = Math.Max(1, config.Order);
            for (int i = 0; i < input.Samples.Length; i++)
            {
                double sum = 0;
                int count = 0;
                for (int j = Math.Max(0, i - windowSize); j <= Math.Min(input.Samples.Length - 1, i + windowSize); j++)
                {
                    sum += input.Samples[j];
                    count++;
                }
                filtered.Samples[i] = sum / count;
            }

            return filtered;
        }

        // Compute FFT
        public FFTResult ComputeFFT(Signal signal, WindowType windowType = WindowType.Hanning)
        {
            // Simplified FFT computation
            int n = signal.Samples.Length;
            var result = new FFTResult
            {
                Frequencies = new double[n / 2],
                Magnitudes = new double[n / 2],
                Phases = new double[n / 2]
            };

            double df = signal.SampleRate / n;
            for (int i = 0; i < n / 2; i++)
            {
                result.Frequencies[i] = i * df;
                // Simplified magnitude calculation
                result.Magnitudes[i] = Math.Abs(signal.Samples[i % signal.Samples.Length]);
                result.Phases[i] = 0;
            }

            // Find dominant frequency
            int maxIndex = 0;
            double maxMag = 0;
            for (int i = 0; i < result.Magnitudes.Length; i++)
            {
                if (result.Magnitudes[i] > maxMag)
                {
                    maxMag = result.Magnitudes[i];
                    maxIndex = i;
                }
            }
            result.DominantFrequency = result.Frequencies[maxIndex];
            result.DominantMagnitude = maxMag;

            return result;
        }

        // Calculate signal statistics
        public SignalStatistics CalculateStatistics(Signal signal)
        {
            var stats = new SignalStatistics();
            
            // Mean
            stats.Mean = signal.Samples.Average();

            // RMS
            stats.RMS = Math.Sqrt(signal.Samples.Select(x => x * x).Average());

            // Peak to Peak
            stats.PeakToPeak = signal.Samples.Max() - signal.Samples.Min();

            // Crest Factor
            double peak = Math.Max(Math.Abs(signal.Samples.Max()), Math.Abs(signal.Samples.Min()));
            stats.CrestFactor = stats.RMS > 0 ? peak / stats.RMS : 0;

            // Simplified SNR and THD (would need proper implementation)
            stats.SNR = 50.0; // Placeholder
            stats.THD = 1.0;  // Placeholder

            return stats;
        }

        // Resample signal
        public Signal Resample(Signal input, double newSampleRate)
        {
            double ratio = newSampleRate / input.SampleRate;
            int newLength = (int)(input.Samples.Length * ratio);

            var resampled = new Signal
            {
                Name = input.Name + "_Resampled",
                SampleRate = newSampleRate,
                Unit = input.Unit,
                Timestamp = DateTime.Now,
                Samples = new double[newLength]
            };

            // Simple linear interpolation
            for (int i = 0; i < newLength; i++)
            {
                double oldIndex = i / ratio;
                int index1 = (int)Math.Floor(oldIndex);
                int index2 = Math.Min(index1 + 1, input.Samples.Length - 1);
                double frac = oldIndex - index1;

                resampled.Samples[i] = input.Samples[index1] * (1 - frac) + 
                                      input.Samples[index2] * frac;
            }

            return resampled;
        }

        // Detect peaks in signal
        public List<int> DetectPeaks(Signal signal, double threshold)
        {
            var peaks = new List<int>();

            for (int i = 1; i < signal.Samples.Length - 1; i++)
            {
                if (signal.Samples[i] > threshold &&
                    signal.Samples[i] > signal.Samples[i - 1] &&
                    signal.Samples[i] > signal.Samples[i + 1])
                {
                    peaks.Add(i);
                }
            }

            return peaks;
        }
    }
}

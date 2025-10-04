using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.Services.PavementCalculation
{
    /// <summary>
    /// Async wrapper for pavement calculations providing UI responsiveness
    /// and background calculation execution with progress reporting
    /// </summary>
    public class AsyncPavementCalculationService : IDisposable
    {
        private readonly HybridPavementCalculationService _calculationService;
        private bool _disposed = false;

        /// <summary>
        /// Event fired when calculation progress is updated
        /// </summary>
        public event Action<CalculationProgressEventArgs>? ProgressUpdated;

        /// <summary>
        /// Gets whether native calculations are available
        /// </summary>
        public bool IsNativeCalculationAvailable => _calculationService.IsNativeCalculationAvailable;

        /// <summary>
        /// Gets the current calculation mode
        /// </summary>
        public CalculationMode CurrentMode => _calculationService.CurrentMode;

        /// <summary>
        /// Gets engine information
        /// </summary>
        public string EngineInfo => _calculationService.EngineInfo;

        /// <summary>
        /// Initialize async calculation service
        /// </summary>
        public AsyncPavementCalculationService()
        {
            _calculationService = new HybridPavementCalculationService();
        }

        /// <summary>
        /// Calculate pavement response asynchronously with progress reporting
        /// </summary>
        /// <param name="structure">Pavement structure configuration</param>
        /// <param name="mode">Calculation mode to use</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task containing calculation results</returns>
        public async Task<SolicitationCalculationResult> CalculateAsync(
            PavementStructure structure,
            CalculationMode? mode = null,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AsyncPavementCalculationService));

            if (structure == null)
                throw new ArgumentNullException(nameof(structure));

            return await Task.Run(async () =>
            {
                try
                {
                    // Report start
                    ReportProgress("Initializing calculation...", 0);
                    cancellationToken.ThrowIfCancellationRequested();

                    // Validate structure
                    ReportProgress("Validating structure...", 10);
                    await Task.Delay(50, cancellationToken); // Small delay to allow UI update
                    
                    // Perform calculation
                    ReportProgress("Calculating pavement response...", 30);
                    var result = _calculationService.CalculateSolicitations(structure, mode);
                    
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    // Process results
                    ReportProgress("Processing results...", 80);
                    await Task.Delay(50, cancellationToken);
                    
                    // Complete
                    ReportProgress("Calculation complete", 100);
                    
                    return result;
                }
                catch (OperationCanceledException)
                {
                    ReportProgress("Calculation cancelled", -1);
                    throw;
                }
                catch (Exception ex)
                {
                    ReportProgress($"Calculation failed: {ex.Message}", -1);
                    throw;
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Calculate multiple structures in parallel with progress tracking
        /// </summary>
        /// <param name="structures">Array of structures to calculate</param>
        /// <param name="mode">Calculation mode to use</param>
        /// <param name="maxConcurrency">Maximum number of parallel calculations</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task containing array of calculation results</returns>
        public async Task<SolicitationCalculationResult[]> CalculateBatchAsync(
            PavementStructure[] structures,
            CalculationMode? mode = null,
            int maxConcurrency = 4,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AsyncPavementCalculationService));

            if (structures == null || structures.Length == 0)
                throw new ArgumentException("Structures array cannot be null or empty", nameof(structures));

            var results = new SolicitationCalculationResult[structures.Length];
            var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
            var completed = 0;

            var tasks = new Task[structures.Length];
            
            for (int i = 0; i < structures.Length; i++)
            {
                int index = i; // Capture for closure
                tasks[i] = Task.Run(async () =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        results[index] = _calculationService.CalculateSolicitations(structures[index], mode);
                        
                        var completedCount = Interlocked.Increment(ref completed);
                        var progress = (completedCount * 100) / structures.Length;
                        ReportProgress($"Completed {completedCount}/{structures.Length} calculations", progress);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, cancellationToken);
            }

            await Task.WhenAll(tasks);
            return results;
        }

        /// <summary>
        /// Test native library asynchronously
        /// </summary>
        /// <returns>Task containing test results</returns>
        public async Task<(bool IsAvailable, string TestResult)> TestNativeLibraryAsync()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AsyncPavementCalculationService));

            return await Task.Run(() =>
            {
                ReportProgress("Testing native library...", 0);
                var result = _calculationService.TestNativeLibrary();
                ReportProgress($"Native library test: {(result.IsAvailable ? "PASSED" : "FAILED")}", 100);
                return result;
            });
        }

        /// <summary>
        /// Get calculation performance metrics
        /// </summary>
        /// <param name="testStructure">Structure to use for performance testing</param>
        /// <param name="iterations">Number of iterations to run</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task containing performance metrics</returns>
        public async Task<CalculationPerformanceMetrics> GetPerformanceMetricsAsync(
            PavementStructure testStructure,
            int iterations = 10,
            CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AsyncPavementCalculationService));

            return await Task.Run(async () =>
            {
                var metrics = new CalculationPerformanceMetrics();
                var nativeTimings = new List<double>();
                var legacyTimings = new List<double>();

                ReportProgress("Starting performance analysis...", 0);

                // Test native calculations if available
                if (IsNativeCalculationAvailable)
                {
                    for (int i = 0; i < iterations; i++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        
                        var result = _calculationService.CalculateSolicitations(testStructure, CalculationMode.Native);
                        if (result.IsSuccessful)
                            nativeTimings.Add(result.CalculationTimeMs);
                        
                        var progress = (i + 1) * 40 / iterations;
                        ReportProgress($"Native test {i + 1}/{iterations}", progress);
                        
                        await Task.Delay(10, cancellationToken); // Small delay for UI responsiveness
                    }
                }

                // Test legacy calculations
                for (int i = 0; i < iterations; i++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    var result = _calculationService.CalculateSolicitations(testStructure, CalculationMode.Legacy);
                    if (result.IsSuccessful)
                        legacyTimings.Add(result.CalculationTimeMs);
                    
                    var progress = 40 + (i + 1) * 40 / iterations;
                    ReportProgress($"Legacy test {i + 1}/{iterations}", progress);
                    
                    await Task.Delay(10, cancellationToken);
                }

                // Calculate metrics
                if (nativeTimings.Count > 0)
                {
                    metrics.NativeAverageMs = nativeTimings.Average();
                    metrics.NativeMinMs = nativeTimings.Min();
                    metrics.NativeMaxMs = nativeTimings.Max();
                }

                if (legacyTimings.Count > 0)
                {
                    metrics.LegacyAverageMs = legacyTimings.Average();
                    metrics.LegacyMinMs = legacyTimings.Min();
                    metrics.LegacyMaxMs = legacyTimings.Max();
                }

                metrics.TestedIterations = iterations;
                metrics.TestedAt = DateTime.Now;

                ReportProgress("Performance analysis complete", 100);
                return metrics;
            }, cancellationToken);
        }

        /// <summary>
        /// Report calculation progress
        /// </summary>
        private void ReportProgress(string message, int percentage)
        {
            ProgressUpdated?.Invoke(new CalculationProgressEventArgs
            {
                Message = message,
                PercentageComplete = percentage,
                Timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _calculationService?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Progress event arguments for calculation operations
    /// </summary>
    public class CalculationProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Progress message
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Percentage complete (0-100, or -1 for error/cancelled)
        /// </summary>
        public int PercentageComplete { get; set; }

        /// <summary>
        /// Timestamp when progress was reported
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Whether this indicates an error state
        /// </summary>
        public bool IsError => PercentageComplete < 0;

        /// <summary>
        /// Whether this indicates completion
        /// </summary>
        public bool IsComplete => PercentageComplete >= 100;
    }

    /// <summary>
    /// Performance metrics for calculation engines
    /// </summary>
    public class CalculationPerformanceMetrics
    {
        /// <summary>
        /// Native engine average calculation time in milliseconds
        /// </summary>
        public double NativeAverageMs { get; set; }

        /// <summary>
        /// Native engine minimum calculation time in milliseconds
        /// </summary>
        public double NativeMinMs { get; set; }

        /// <summary>
        /// Native engine maximum calculation time in milliseconds
        /// </summary>
        public double NativeMaxMs { get; set; }

        /// <summary>
        /// Legacy engine average calculation time in milliseconds
        /// </summary>
        public double LegacyAverageMs { get; set; }

        /// <summary>
        /// Legacy engine minimum calculation time in milliseconds
        /// </summary>
        public double LegacyMinMs { get; set; }

        /// <summary>
        /// Legacy engine maximum calculation time in milliseconds
        /// </summary>
        public double LegacyMaxMs { get; set; }

        /// <summary>
        /// Number of iterations tested
        /// </summary>
        public int TestedIterations { get; set; }

        /// <summary>
        /// When the metrics were gathered
        /// </summary>
        public DateTime TestedAt { get; set; }

        /// <summary>
        /// Performance improvement factor (legacy time / native time)
        /// </summary>
        public double SpeedupFactor => NativeAverageMs > 0 ? LegacyAverageMs / NativeAverageMs : 0;

        /// <summary>
        /// Whether native calculations are available
        /// </summary>
        public bool IsNativeAvailable => NativeAverageMs > 0;

        /// <summary>
        /// Performance summary string
        /// </summary>
        public string Summary => IsNativeAvailable
            ? $"Native: {NativeAverageMs:F1}ms avg, Legacy: {LegacyAverageMs:F1}ms avg, Speedup: {SpeedupFactor:F1}x"
            : $"Legacy only: {LegacyAverageMs:F1}ms avg";
    }
}
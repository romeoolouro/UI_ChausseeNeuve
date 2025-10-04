using System;
using System.Threading.Tasks;
using ChausseeNeuve.Domain.Models;

namespace UI_ChausseeNeuve.Services.PavementCalculation
{
    /// <summary>
    /// Hybrid pavement calculation service that uses native DLL when available,
    /// falls back to legacy calculations when native library is unavailable
    /// </summary>
    public class HybridPavementCalculationService : IDisposable
    {
        private readonly NativePavementCalculator? _nativeCalculator;
        private readonly SolicitationCalculationService _legacyCalculator;
        private bool _disposed = false;

        /// <summary>
        /// Gets whether native calculations are available
        /// </summary>
        public bool IsNativeCalculationAvailable => _nativeCalculator?.IsNativeLibraryAvailable == true;

        /// <summary>
        /// Gets the calculation mode being used
        /// </summary>
        public CalculationMode CurrentMode => IsNativeCalculationAvailable 
            ? CalculationMode.Native 
            : CalculationMode.Legacy;

        /// <summary>
        /// Gets version information about the calculation engines
        /// </summary>
        public string EngineInfo => IsNativeCalculationAvailable
            ? $"Native Engine v{_nativeCalculator!.LibraryVersion}"
            : "Legacy C# Engine";

        /// <summary>
        /// Initialize hybrid calculation service
        /// </summary>
        public HybridPavementCalculationService()
        {
            _legacyCalculator = new SolicitationCalculationService();

            try
            {
                _nativeCalculator = new NativePavementCalculator();
            }
            catch (Exception)
            {
                // Native calculator initialization failed - will use legacy only
                _nativeCalculator = null;
            }
        }

        /// <summary>
        /// Calculate pavement response using the best available method
        /// </summary>
        /// <param name="structure">Pavement structure configuration</param>
        /// <param name="forceMode">Optional: force use of specific calculation mode</param>
        /// <returns>Calculation results</returns>
        public SolicitationCalculationResult CalculateSolicitations(
            PavementStructure structure, 
            CalculationMode? forceMode = null)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(HybridPavementCalculationService));

            var targetMode = forceMode ?? CurrentMode;

            try
            {
                switch (targetMode)
                {
                    case CalculationMode.Native:
                        return CalculateWithNative(structure);

                    case CalculationMode.Legacy:
                        return CalculateWithLegacy(structure);

                    case CalculationMode.Comparison:
                        return CalculateComparison(structure);

                    default:
                        throw new ArgumentException($"Unsupported calculation mode: {targetMode}");
                }
            }
            catch (Exception ex) when (targetMode == CalculationMode.Native)
            {
                // Native calculation failed - fall back to legacy with warning
                var fallbackResult = CalculateWithLegacy(structure);
                fallbackResult.Message = $"Native calculation failed ({ex.Message}). Used legacy fallback: {fallbackResult.Message}";
                return fallbackResult;
            }
        }

        /// <summary>
        /// Calculate pavement response asynchronously
        /// </summary>
        public Task<SolicitationCalculationResult> CalculateSolicitationsAsync(
            PavementStructure structure, 
            CalculationMode? forceMode = null)
        {
            return Task.Run(() => CalculateSolicitations(structure, forceMode));
        }

        /// <summary>
        /// Test native library availability and functionality
        /// </summary>
        public (bool IsAvailable, string TestResult) TestNativeLibrary()
        {
            if (_nativeCalculator == null)
                return (false, "Native calculator not initialized");

            try
            {
                bool testResult = _nativeCalculator.TestNativeLibrary(out string testMessage);
                return (testResult, testMessage);
            }
            catch (Exception ex)
            {
                return (false, $"Native library test failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculate using native DLL
        /// </summary>
        private SolicitationCalculationResult CalculateWithNative(PavementStructure structure)
        {
            if (_nativeCalculator == null || !_nativeCalculator.IsNativeLibraryAvailable)
                throw new NativeLibraryUnavailableException("Native calculation library is not available");

            var result = _nativeCalculator.CalculateSolicitations(structure);
            result.Message = $"[NATIVE] {result.Message}";
            return result;
        }

        /// <summary>
        /// Calculate using legacy C# implementation
        /// </summary>
        private SolicitationCalculationResult CalculateWithLegacy(PavementStructure structure)
        {
            var result = _legacyCalculator.CalculateSolicitations(structure);
            result.Message = $"[LEGACY] {result.Message}";
            return result;
        }

        /// <summary>
        /// Calculate using both engines for comparison
        /// </summary>
        private SolicitationCalculationResult CalculateComparison(PavementStructure structure)
        {
            var legacyResult = CalculateWithLegacy(structure);

            if (!IsNativeCalculationAvailable)
            {
                legacyResult.Message = "[COMPARISON - NATIVE UNAVAILABLE] " + legacyResult.Message;
                return legacyResult;
            }

            try
            {
                var nativeResult = CalculateWithNative(structure);
                
                // Create comparison result based on native results but include legacy info
                nativeResult.Message = $"[COMPARISON] Native: {nativeResult.CalculationTimeMs:F2}ms, Legacy: {legacyResult.CalculationTimeMs:F2}ms";
                
                // Could add more comparison metrics here in the future
                return nativeResult;
            }
            catch (Exception ex)
            {
                legacyResult.Message = $"[COMPARISON - NATIVE FAILED] Native error: {ex.Message}. Legacy: {legacyResult.Message}";
                return legacyResult;
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _nativeCalculator?.Dispose();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Calculation mode enumeration
    /// </summary>
    public enum CalculationMode
    {
        /// <summary>
        /// Use native C++ DLL for calculations (high performance, rigorous)
        /// </summary>
        Native,

        /// <summary>
        /// Use legacy C# implementation (fallback, compatibility)
        /// </summary>
        Legacy,

        /// <summary>
        /// Run both calculations for comparison and validation
        /// </summary>
        Comparison
    }
}
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace UI_ChausseeNeuve.Services.PavementCalculation
{
    /// <summary>
    /// P/Invoke declarations for the native pavement calculation engine
    /// All function signatures must match exactly with PavementAPI.h
    /// </summary>
    internal static class NativeInterop
    {
        // DLL name - will be resolved at runtime based on platform and architecture
        private const string DllName = "PavementCalculationEngine.dll";

        /// <summary>
        /// Main calculation function
        /// 
        /// Performs pavement structure calculation using layered elastic theory.
        /// </summary>
        /// <param name="input">Pointer to input structure (must not be NULL)</param>
        /// <param name="output">Pointer to output structure (must not be NULL, will be populated by DLL)</param>
        /// <returns>PAVEMENT_SUCCESS on success, error code otherwise</returns>
        /// <remarks>
        /// Output arrays are allocated by the DLL and must be freed with PavementFreeOutput.
        /// Input arrays must remain valid during the call but can be freed immediately after.
        /// </remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int PavementCalculate(
            ref PavementInputC input,
            ref PavementOutputC output
        );

        /// <summary>
        /// Stable calculation function using TRMM (Transmission and Reflection Matrix Method)
        /// 
        /// Performs pavement structure calculation using numerically stable TRMM algorithm.
        /// This method avoids exponential overflow issues present in standard TMM for high m*h values.
        /// Recommended for structures with stiff layers (E > 5000 MPa) or thick layers (h > 0.15 m).
        /// </summary>
        /// <param name="input">Pointer to input structure (must not be NULL)</param>
        /// <param name="output">Pointer to output structure (must not be NULL, will be populated by DLL)</param>
        /// <returns>PAVEMENT_SUCCESS on success, error code otherwise</returns>
        /// <remarks>
        /// Uses TRMM algorithm based on:
        /// - Qiu et al. (2025) Transportation Geotechnics
        /// - Dong et al. (2021) PolyU Thesis
        /// - Fan et al. (2022) Soil Dynamics and Earthquake Engineering
        /// Output arrays are allocated by the DLL and must be freed with PavementFreeOutput.
        /// </remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int PavementCalculateStable(
            ref PavementInputC input,
            ref PavementOutputC output
        );

        /// <summary>
        /// Free output structure memory
        /// 
        /// Releases all memory allocated by PavementCalculate in the output structure.
        /// Safe to call multiple times on the same structure (idempotent).
        /// </summary>
        /// <param name="output">Pointer to output structure (can be NULL, in which case no-op)</param>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void PavementFreeOutput(
            ref PavementOutputC output
        );

        /// <summary>
        /// Get library version string
        /// </summary>
        /// <returns>Version string in format "MAJOR.MINOR.PATCH" (e.g., "1.0.0")</returns>
        /// <remarks>Returned string is statically allocated and should not be freed</remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern IntPtr PavementGetVersion();

        /// <summary>
        /// Get detailed error message for last error
        /// </summary>
        /// <returns>Human-readable error description (UTF-8)</returns>
        /// <remarks>
        /// Returned string is statically allocated and should not be freed.
        /// Thread-local storage ensures thread safety.
        /// </remarks>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern IntPtr PavementGetLastError();

        /// <summary>
        /// Validate input data structure
        /// 
        /// Checks input parameters for validity without performing calculation.
        /// Useful for pre-flight validation from managed code.
        /// </summary>
        /// <param name="input">Pointer to input structure (must not be NULL)</param>
        /// <param name="errorMessage">Buffer to receive error message (can be NULL)</param>
        /// <param name="messageSize">Size of error_message buffer (ignored if errorMessage is NULL)</param>
        /// <returns>PAVEMENT_SUCCESS if input is valid, error code otherwise</returns>
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern int PavementValidateInput(
            ref PavementInputC input,
            StringBuilder errorMessage,
            int messageSize
        );

        /// <summary>
        /// Convert native string pointer to managed string
        /// </summary>
        /// <param name="ptr">Pointer to null-terminated C string</param>
        /// <returns>Managed string or empty string if ptr is null</returns>
        internal static string PtrToString(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return string.Empty;

            return Marshal.PtrToStringAnsi(ptr) ?? string.Empty;
        }

        /// <summary>
        /// Get version string from native library
        /// </summary>
        /// <returns>Version string or "Unknown" if call fails</returns>
        internal static string GetVersionString()
        {
            try
            {
                IntPtr versionPtr = PavementGetVersion();
                return PtrToString(versionPtr);
            }
            catch (Exception)
            {
                return "Unknown";
            }
        }

        /// <summary>
        /// Get last error message from native library
        /// </summary>
        /// <returns>Error message or empty string if no error or call fails</returns>
        internal static string GetLastErrorString()
        {
            try
            {
                IntPtr errorPtr = PavementGetLastError();
                return PtrToString(errorPtr);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Check if the native DLL is available and can be loaded
        /// </summary>
        /// <returns>True if DLL can be loaded and basic functions called</returns>
        internal static bool IsNativeLibraryAvailable()
        {
            try
            {
                // Try to call the version function as a simple availability test
                IntPtr versionPtr = PavementGetVersion();
                string version = PtrToString(versionPtr);
                
                // Version should be non-empty if the DLL loaded correctly
                return !string.IsNullOrEmpty(version);
            }
            catch (Exception)
            {
                // DLL not found, wrong architecture, missing dependencies, etc.
                return false;
            }
        }

        /// <summary>
        /// Validate input using native library
        /// </summary>
        /// <param name="input">Input structure to validate</param>
        /// <param name="errorMessage">Receives detailed error message if validation fails</param>
        /// <returns>Error code (PAVEMENT_SUCCESS if valid)</returns>
        internal static PavementErrorCode ValidateInputNative(ref PavementInputC input, out string errorMessage)
        {
            try
            {
                const int bufferSize = 512;
                StringBuilder errorBuffer = new StringBuilder(bufferSize);
                
                int result = PavementValidateInput(ref input, errorBuffer, bufferSize);
                errorMessage = errorBuffer.ToString();
                
                return (PavementErrorCode)result;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to validate input: {ex.Message}";
                return PavementErrorCode.Unknown;
            }
        }
    }

    /// <summary>
    /// Exception thrown when native pavement calculation fails
    /// </summary>
    public class PavementCalculationException : Exception
    {
        public PavementErrorCode ErrorCode { get; }

        public PavementCalculationException(PavementErrorCode errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }

        public PavementCalculationException(PavementErrorCode errorCode, string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }

    /// <summary>
    /// Exception thrown when native DLL is not available or cannot be loaded
    /// </summary>
    public class NativeLibraryUnavailableException : Exception
    {
        public NativeLibraryUnavailableException()
            : base("Native pavement calculation library is not available. Falling back to legacy calculations.")
        {
        }

        public NativeLibraryUnavailableException(string message)
            : base(message)
        {
        }

        public NativeLibraryUnavailableException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
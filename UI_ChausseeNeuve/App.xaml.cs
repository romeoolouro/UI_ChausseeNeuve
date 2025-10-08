using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using UI_ChausseeNeuve.Services.PavementCalculation;

namespace UI_ChausseeNeuve
{
    /// <summary>
    /// Application startup with native DLL verification
    /// </summary>
    public partial class App : Application
    {
        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        protected override void OnStartup(StartupEventArgs e)
        {
            // ✅ PRODUCTION MODE: Console logging disabled for clean deployment
            // Uncomment lines below for debugging if needed:
            // if (!AttachConsole(-1)) { AllocConsole(); }
            // Console.WriteLine("=== APPLICATION WPF DÉMARRÉE AVEC CONSOLE ===");
            // Console.WriteLine($"Heure de démarrage: {DateTime.Now:HH:mm:ss.fff}");
            
            // Perform native library verification on startup
            VerifyNativeLibrary();
            
            base.OnStartup(e);
        }

        /// <summary>
        /// Verify native DLL is available and functional
        /// </summary>
        private void VerifyNativeLibrary()
        {
            try
            {
                // Check if DLL file exists
                string dllPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, 
                    "PavementCalculationEngine.dll");

                if (!File.Exists(dllPath))
                {
                    ShowNativeDllWarning($"PavementCalculationEngine.dll not found at: {dllPath}");
                    return;
                }

                // Test DLL functionality
                using var testService = new HybridPavementCalculationService();
                
                if (!testService.IsNativeCalculationAvailable)
                {
                    ShowNativeDllInfo("Native library is not available. Application will use legacy calculations.");
                    return;
                }

                // Quick functionality test using the hybrid service
                var (testPassed, testResult) = testService.TestNativeLibrary();
                if (!testPassed)
                {
                    ShowNativeDllWarning($"Native library test failed: {testResult}");
                    return;
                }

                // Success - native calculations available
                // Debug logging disabled for production
                // Console.WriteLine($"Native calculation engine loaded successfully: {testService.EngineInfo}");
                // Console.WriteLine($"Test result: {testResult}");
            }
            catch (Exception ex)
            {
                ShowNativeDllWarning($"Failed to verify native library: {ex.Message}");
            }
        }

        /// <summary>
        /// Show warning about native DLL issues
        /// </summary>
        private void ShowNativeDllWarning(string details)
        {
            var result = MessageBox.Show(
                "Native Calculation Engine Warning\n\n" +
                "The high-performance native calculation engine could not be loaded:\n\n" +
                details + "\n\n" +
                "The application will use legacy C# calculations instead.\n" +
                "Performance may be reduced, but all functionality remains available.\n\n" +
                "Do you want to continue?",
                "Native Library Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.No)
            {
                Shutdown(1);
            }
        }

        /// <summary>
        /// Show informational message about native DLL
        /// </summary>
        private void ShowNativeDllInfo(string details)
        {
            MessageBox.Show(
                "Calculation Engine Information\n\n" + details,
                "Native Library Information",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Cleanup any native resources if needed
            base.OnExit(e);
        }
    }
}

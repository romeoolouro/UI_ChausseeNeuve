using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChausseeNeuve.Domain.Models;
using UI_ChausseeNeuve.Services.PavementCalculation;

namespace UI_ChausseeNeuve.Services
{
    /// <summary>
    /// Native pavement calculation service using C++ DLL via P/Invoke
    /// Provides high-performance calculations using rigorous layered elastic theory
    /// </summary>
    public class NativePavementCalculator : IDisposable
    {
        private bool _isLibraryAvailable;
        private readonly string _libraryVersion;
        private bool _disposed = false;

        /// <summary>
        /// Gets whether the native library is available for calculations
        /// </summary>
        public bool IsNativeLibraryAvailable => _isLibraryAvailable;

        /// <summary>
        /// Gets the version of the native calculation library
        /// </summary>
        public string LibraryVersion => _libraryVersion;

        /// <summary>
        /// Initialize native calculator and check library availability
        /// </summary>
        public NativePavementCalculator()
        {
            try
            {
                _isLibraryAvailable = NativeInterop.IsNativeLibraryAvailable();
                _libraryVersion = _isLibraryAvailable ? NativeInterop.GetVersionString() : "Unavailable";
            }
            catch (Exception)
            {
                _isLibraryAvailable = false;
                _libraryVersion = "Unavailable";
            }
        }

        /// <summary>
        /// Calculate pavement response using native DLL
        /// </summary>
        /// <param name="structure">Pavement structure configuration</param>
        /// <returns>Calculation results or throws exception if calculation fails</returns>
        /// <exception cref="NativeLibraryUnavailableException">Thrown when native DLL is not available</exception>
        /// <exception cref="PavementCalculationException">Thrown when calculation fails</exception>
        public SolicitationCalculationResult CalculateSolicitations(PavementStructure structure)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(NativePavementCalculator));

            if (!_isLibraryAvailable)
                throw new NativeLibraryUnavailableException();

            var startTime = DateTime.Now;

            try
            {
                // Validate input structure
                ValidateInputStructure(structure);

                // Prepare input for native calculation
                using var nativeInput = PrepareNativeInput(structure);
                
                // Perform native calculation
                using var nativeOutput = PerformNativeCalculation(nativeInput);
                
                // Convert results back to managed format
                var results = ConvertNativeResults(nativeOutput, structure);
                
                var duration = DateTime.Now - startTime;

                return new SolicitationCalculationResult
                {
                    LayerResults = results,
                    CalculationTimeMs = duration.TotalMilliseconds,
                    IsSuccessful = true,
                    Message = $"Native calculation completed - {results.Count} layers - {nativeOutput.CalculationTimeMs:F2}ms native time",
                    InputData = null // We don't use the legacy CalculationInputData format
                };
            }
            catch (PavementCalculationException)
            {
                // Re-throw native calculation exceptions
                throw;
            }
            catch (Exception ex)
            {
                var duration = DateTime.Now - startTime;
                return new SolicitationCalculationResult
                {
                    LayerResults = new List<LayerSolicitationResult>(),
                    CalculationTimeMs = duration.TotalMilliseconds,
                    IsSuccessful = false,
                    Message = $"Native calculation failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Calculate pavement response asynchronously
        /// </summary>
        public Task<SolicitationCalculationResult> CalculateSolicitationsAsync(PavementStructure structure)
        {
            return Task.Run(() => CalculateSolicitations(structure));
        }

        /// <summary>
        /// Validate input structure for native calculation
        /// </summary>
        private void ValidateInputStructure(PavementStructure structure)
        {
            if (structure?.Layers == null)
                throw new ArgumentNullException(nameof(structure), "Structure cannot be null");

            if (structure.Layers.Count < 2)
                throw new ArgumentException("Structure must contain at least 2 layers", nameof(structure));

            if (structure.Layers.Count > 20)
                throw new ArgumentException("Structure cannot contain more than 20 layers", nameof(structure));

            var platformCount = structure.Layers.Count(l => l.Role == LayerRole.Plateforme);
            if (platformCount != 1)
                throw new ArgumentException("Structure must contain exactly 1 platform layer", nameof(structure));

            // Validate layer properties
            foreach (var layer in structure.Layers)
            {
                if (layer.Modulus_MPa <= 0)
                    throw new ArgumentException($"Invalid Young's modulus for layer {layer.MaterialName}: {layer.Modulus_MPa}");
                
                if (layer.Poisson <= 0 || layer.Poisson >= 0.5)
                    throw new ArgumentException($"Invalid Poisson ratio for layer {layer.MaterialName}: {layer.Poisson}");
                
                if (layer.Role != LayerRole.Plateforme && layer.Thickness_m <= 0)
                    throw new ArgumentException($"Invalid thickness for layer {layer.MaterialName}: {layer.Thickness_m}");
            }

            // Validate load configuration
            if (structure.ChargeReference?.PressionMPa <= 0)
                throw new ArgumentException("Invalid load pressure");

            if (structure.ChargeReference?.RayonMetres <= 0)
                throw new ArgumentException("Invalid wheel radius");
        }

        /// <summary>
        /// Prepare native input structure from domain model
        /// </summary>
        private ManagedPavementInput PrepareNativeInput(PavementStructure structure)
        {
            // Order layers: surface layers first, platform last
            var orderedLayers = structure.Layers
                .Where(l => l.Role != LayerRole.Plateforme)
                .OrderBy(l => l.Order)
                .ToList();

            var platformLayer = structure.Layers.First(l => l.Role == LayerRole.Plateforme);
            orderedLayers.Add(platformLayer);

            var input = new ManagedPavementInput();
            
            // Layer configuration
            input.LayerCount = orderedLayers.Count;
            
            var poissonRatios = orderedLayers.Select(l => l.Poisson).ToArray();
            var youngModuli = orderedLayers.Select(l => l.Modulus_MPa).ToArray();
            var thicknesses = orderedLayers.Select(l => l.Thickness_m).ToArray();
            
            // Interface bonding: assume fully bonded for now (can be enhanced later)
            var bondedInterfaces = Enumerable.Repeat(1, orderedLayers.Count - 1).ToArray();
            
            input.SetLayerProperties(poissonRatios, youngModuli, thicknesses, bondedInterfaces);

            // Load configuration
            input.WheelType = structure.ChargeReference.Type == ChargeType.RoueIsolee 
                ? WheelType.Simple 
                : WheelType.Twin;
            input.PressureKPa = structure.ChargeReference.PressionMPa * 1000; // Convert MPa to kPa
            input.WheelRadiusM = structure.ChargeReference.RayonMetres;
            input.WheelSpacingM = structure.ChargeReference.DistanceRouesMetres;

            // Calculation points: top and bottom of each layer
            var zCoords = new List<double>();
            double currentDepth = 0;
            
            foreach (var layer in orderedLayers)
            {
                zCoords.Add(currentDepth); // Top of layer
                
                if (layer.Role != LayerRole.Plateforme)
                {
                    currentDepth += layer.Thickness_m;
                    zCoords.Add(currentDepth); // Bottom of layer
                }
                else
                {
                    // For platform, add a few points at increasing depths
                    zCoords.Add(currentDepth + 0.5);
                    zCoords.Add(currentDepth + 1.0);
                    zCoords.Add(currentDepth + 2.0);
                    break;
                }
            }

            input.CalculationPointCount = zCoords.Count;
            input.SetCalculationPoints(zCoords.ToArray());

            return input;
        }

        /// <summary>
        /// Perform native calculation using P/Invoke
        /// </summary>
        private ManagedPavementOutput PerformNativeCalculation(ManagedPavementInput input)
        {
            // Pre-validate input using native validation
            var validationResult = NativeInterop.ValidateInputNative(ref input.GetNativeStruct(), out string validationError);
            if (validationResult != PavementErrorCode.Success)
            {
                throw new PavementCalculationException(validationResult, 
                    $"Input validation failed: {validationError}");
            }

            // Initialize output structure
            var nativeOutput = new PavementOutputC();
            
            try
            {
                // Call native TRMM calculation for numerical stability
                // TRMM (Transmission and Reflection Matrix Method) avoids exponential overflow
                // issues in standard TMM for high m*h values (stiff/thick layers)
                int result = NativeInterop.PavementCalculateStable(
                    ref input.GetNativeStruct(), 
                    ref nativeOutput);

                var managedOutput = new ManagedPavementOutput(nativeOutput);

                if (result != (int)PavementErrorCode.Success)
                {
                    string errorMessage = !string.IsNullOrEmpty(managedOutput.ErrorMessage) 
                        ? managedOutput.ErrorMessage 
                        : NativeInterop.GetLastErrorString();
                    
                    throw new PavementCalculationException((PavementErrorCode)result, 
                        $"Native calculation failed: {errorMessage}");
                }

                if (!managedOutput.Success)
                {
                    throw new PavementCalculationException(managedOutput.ErrorCode, 
                        $"Calculation unsuccessful: {managedOutput.ErrorMessage}");
                }

                return managedOutput;
            }
            catch (PavementCalculationException)
            {
                // Free output before re-throwing
                NativeInterop.PavementFreeOutput(ref nativeOutput);
                throw;
            }
            catch (Exception ex)
            {
                // Free output and wrap in native calculation exception
                NativeInterop.PavementFreeOutput(ref nativeOutput);
                throw new PavementCalculationException(PavementErrorCode.Unknown, 
                    $"Unexpected error during native calculation: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Convert native results back to domain model format
        /// </summary>
        private List<LayerSolicitationResult> ConvertNativeResults(ManagedPavementOutput nativeOutput, PavementStructure structure)
        {
            var results = new List<LayerSolicitationResult>();
            
            // Get result arrays
            var deflections = nativeOutput.GetDeflections();
            var verticalStresses = nativeOutput.GetVerticalStresses();
            var horizontalStrains = nativeOutput.GetHorizontalStrains();
            var radialStrains = nativeOutput.GetRadialStrains();
            var shearStresses = nativeOutput.GetShearStresses();

            // DIAGNOSTIC: Log raw data from native calculation
            Console.WriteLine($"=== NATIVE RESULTS DEBUG ===");
            Console.WriteLine($"Deflections length: {deflections.Length}, values: [{string.Join(", ", deflections.Take(10))}]");
            Console.WriteLine($"Vertical stresses length: {verticalStresses.Length}, values: [{string.Join(", ", verticalStresses.Take(10))}]");
            Console.WriteLine($"Horizontal strains length: {horizontalStrains.Length}, values: [{string.Join(", ", horizontalStrains.Take(10))}]");
            Console.WriteLine($"Radial strains length: {radialStrains.Length}, values: [{string.Join(", ", radialStrains.Take(10))}]");
            Console.WriteLine($"Structure layers count: {structure.Layers.Count}");

            // Order layers same as in input preparation
            var orderedLayers = structure.Layers
                .Where(l => l.Role != LayerRole.Plateforme)
                .OrderBy(l => l.Order)
                .ToList();
            
            var platformLayer = structure.Layers.First(l => l.Role == LayerRole.Plateforme);
            orderedLayers.Add(platformLayer);

            // Map results to layers (simplified - assumes 2 points per layer except platform)
            int resultIndex = 0;
            
            for (int layerIndex = 0; layerIndex < orderedLayers.Count; layerIndex++)
            {
                var layer = orderedLayers[layerIndex];
                
                if (resultIndex >= deflections.Length)
                    break;

                var layerResult = new LayerSolicitationResult
                {
                    Layer = layer
                };

                // Top of layer values
                if (resultIndex < deflections.Length)
                {
                    layerResult.DeflectionTop = deflections[resultIndex] / 1000.0; // Convert mm to m
                    layerResult.SigmaZTop = verticalStresses[resultIndex] / 1000.0; // Convert kPa to MPa
                    
                    // Tensile strain (horizontal strain in microstrain)
                    layerResult.EpsilonTTop = horizontalStrains[resultIndex];
                    
                    // Calculate tensile stress from strain using Young's modulus
                    // σT = E * εT * 10^-6 (convert microstrain to strain)
                    layerResult.SigmaTTop = layer.Modulus_MPa * horizontalStrains[resultIndex] * 1e-6;
                    
                    // DIAGNOSTIC: Log calculation details
                    Console.WriteLine($"Layer {layerIndex} ({layer.MaterialName}): E={layer.Modulus_MPa} MPa, εT={horizontalStrains[resultIndex]} μstrain");
                    Console.WriteLine($"  → σT = {layer.Modulus_MPa} × {horizontalStrains[resultIndex]} × 1e-6 = {layerResult.SigmaTTop} MPa");
                    
                    // Vertical strain (radial strain)
                    layerResult.EpsilonZTop = resultIndex < radialStrains.Length ? radialStrains[resultIndex] : 0;
                    
                    resultIndex++;
                }

                // Bottom of layer values (if available and not platform)
                if (layer.Role != LayerRole.Plateforme && resultIndex < deflections.Length)
                {
                    layerResult.DeflectionBottom = deflections[resultIndex] / 1000.0; // Convert mm to m
                    layerResult.SigmaZBottom = verticalStresses[resultIndex] / 1000.0;
                    layerResult.EpsilonTBottom = horizontalStrains[resultIndex];
                    
                    // Calculate tensile stress from strain
                    layerResult.SigmaTBottom = layer.Modulus_MPa * horizontalStrains[resultIndex] * 1e-6;
                    
                    layerResult.EpsilonZBottom = resultIndex < radialStrains.Length ? radialStrains[resultIndex] : 0;
                    resultIndex++;
                }
                else
                {
                    // For platform or when no bottom data, use top values
                    layerResult.DeflectionBottom = layerResult.DeflectionTop;
                    layerResult.SigmaZBottom = layerResult.SigmaZTop;
                    layerResult.EpsilonTBottom = layerResult.EpsilonTTop;
                    layerResult.SigmaTBottom = layerResult.SigmaTTop;
                    layerResult.EpsilonZBottom = layerResult.EpsilonZTop;
                }

                results.Add(layerResult);
            }

            // Free the native output memory
            NativeInterop.PavementFreeOutput(ref nativeOutput.GetNativeStruct());

            return results;
        }

        /// <summary>
        /// Test native library functionality
        /// </summary>
        public bool TestNativeLibrary(out string testResult)
        {
            try
            {
                if (!_isLibraryAvailable)
                {
                    testResult = "Native library not available";
                    return false;
                }

                // Create a simple 2-layer test case
                var testInput = new ManagedPavementInput();
                testInput.LayerCount = 2;
                testInput.SetLayerProperties(
                    new double[] { 0.35, 0.35 },      // Poisson ratios
                    new double[] { 5000.0, 50.0 },    // Young's moduli (MPa)  
                    new double[] { 0.2, 100.0 },      // Thicknesses (m)
                    new int[] { 1 }                    // Bonded interface
                );
                testInput.WheelType = WheelType.Simple;
                testInput.PressureKPa = 662; // 0.662 MPa
                testInput.WheelRadiusM = 0.125;
                testInput.WheelSpacingM = 0;
                testInput.CalculationPointCount = 3;
                testInput.SetCalculationPoints(new double[] { 0.0, 0.2, 1.0 });

                // Validate input
                var validationResult = NativeInterop.ValidateInputNative(ref testInput.GetNativeStruct(), out string validationError);
                if (validationResult != PavementErrorCode.Success)
                {
                    testResult = $"Test validation failed: {validationError}";
                    return false;
                }

                // Perform calculation
                var nativeOutput = new PavementOutputC();
                int calcResult = NativeInterop.PavementCalculate(ref testInput.GetNativeStruct(), ref nativeOutput);
                
                using var managedOutput = new ManagedPavementOutput(nativeOutput);
                
                if (calcResult != (int)PavementErrorCode.Success || !managedOutput.Success)
                {
                    testResult = $"Test calculation failed: {managedOutput.ErrorMessage}";
                    NativeInterop.PavementFreeOutput(ref nativeOutput);
                    return false;
                }

                var deflections = managedOutput.GetDeflections();
                testResult = $"Test successful - Version: {_libraryVersion}, Calculation time: {managedOutput.CalculationTimeMs:F2}ms, Results: {deflections.Length} points";
                
                NativeInterop.PavementFreeOutput(ref nativeOutput);
                return true;
            }
            catch (Exception ex)
            {
                testResult = $"Test exception: {ex.Message}";
                return false;
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // Nothing specific to dispose - the native DLL manages its own memory
                _disposed = true;
            }
        }
    }
}
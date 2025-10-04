using System;
using System.Runtime.InteropServices;

namespace UI_ChausseeNeuve.Services.PavementCalculation
{
    /// <summary>
    /// Error codes returned by the native pavement calculation API
    /// Must match PavementErrorCode enum in PavementAPI.h
    /// </summary>
    public enum PavementErrorCode
    {
        Success = 0,              // PAVEMENT_SUCCESS
        InvalidInput = 1,         // PAVEMENT_ERROR_INVALID_INPUT
        NullPointer = 2,          // PAVEMENT_ERROR_NULL_POINTER
        Allocation = 3,           // PAVEMENT_ERROR_ALLOCATION
        Calculation = 4,          // PAVEMENT_ERROR_CALCULATION
        Unknown = 99              // PAVEMENT_ERROR_UNKNOWN
    }

    /// <summary>
    /// Wheel type configuration
    /// Must match WheelType enum in PavementAPI.h
    /// </summary>
    public enum WheelType
    {
        Simple = 0,    // WHEEL_TYPE_SIMPLE
        Twin = 1       // WHEEL_TYPE_TWIN
    }

    /// <summary>
    /// Input structure for pavement calculation
    /// Must match PavementInputC struct in PavementAPI.h exactly for P/Invoke marshalling
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PavementInputC
    {
        // Layer configuration
        public int nlayer;                    // Number of layers (1 to 20)
        public IntPtr poisson_ratio;          // Poisson's ratios for each layer (nlayer elements)
        public IntPtr young_modulus;          // Young's moduli in MPa (nlayer elements)
        public IntPtr thickness;              // Layer thicknesses in meters (nlayer elements)
        public IntPtr bonded_interface;       // Interface bonding flags (nlayer-1 elements): 1=bonded, 0=unbonded

        // Load configuration
        public int wheel_type;                // Wheel type (0=simple, 1=twin) - see WheelType enum
        public double pressure_kpa;           // Wheel pressure in kPa (0 to 2000)
        public double wheel_radius_m;         // Wheel radius in meters (>0)
        public double wheel_spacing_m;        // Wheel spacing in meters (for twin wheels, >0)

        // Calculation points
        public int nz;                        // Number of vertical calculation points (>0)
        public IntPtr z_coords;               // Z-coordinates for calculation in meters (nz elements)
    }

    /// <summary>
    /// Output structure for pavement calculation
    /// Must match PavementOutputC struct in PavementAPI.h exactly for P/Invoke marshalling
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PavementOutputC
    {
        // Status information
        public int success;                   // 1 if calculation succeeded, 0 otherwise
        public int error_code;                // Error code (see PavementErrorCode enum)
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string error_message;          // Human-readable error message (UTF-8)

        // Calculation metadata
        public int nz;                        // Number of calculation points (matches input if successful)
        public double calculation_time_ms;    // Calculation time in milliseconds

        // Results arrays (allocated by DLL, size = nz)
        public IntPtr deflection_mm;          // Vertical deflections in mm (positive downward)
        public IntPtr vertical_stress_kpa;    // Vertical stresses σz in kPa (positive compression)
        public IntPtr horizontal_strain;      // Horizontal strains εr in microstrain (με)
        public IntPtr radial_strain;          // Radial strains εθ in microstrain (με)
        public IntPtr shear_stress_kpa;       // Shear stresses τrz in kPa
    }

    /// <summary>
    /// Managed wrapper for PavementInputC that handles memory allocation and marshalling
    /// </summary>
    public class ManagedPavementInput : IDisposable
    {
        private PavementInputC _nativeInput;
        private GCHandle[] _arrayHandles;
        private bool _disposed = false;

        public int LayerCount
        {
            get => _nativeInput.nlayer;
            set => _nativeInput.nlayer = value;
        }

        public WheelType WheelType
        {
            get => (WheelType)_nativeInput.wheel_type;
            set => _nativeInput.wheel_type = (int)value;
        }

        public double PressureKPa
        {
            get => _nativeInput.pressure_kpa;
            set => _nativeInput.pressure_kpa = value;
        }

        public double WheelRadiusM
        {
            get => _nativeInput.wheel_radius_m;
            set => _nativeInput.wheel_radius_m = value;
        }

        public double WheelSpacingM
        {
            get => _nativeInput.wheel_spacing_m;
            set => _nativeInput.wheel_spacing_m = value;
        }

        public int CalculationPointCount
        {
            get => _nativeInput.nz;
            set => _nativeInput.nz = value;
        }

        /// <summary>
        /// Initialize managed input wrapper
        /// </summary>
        public ManagedPavementInput()
        {
            _nativeInput = new PavementInputC();
            _arrayHandles = new GCHandle[6]; // 6 arrays total
        }

        /// <summary>
        /// Set layer properties (Poisson ratios, Young moduli, thicknesses)
        /// </summary>
        public void SetLayerProperties(
            double[] poissonRatios,
            double[] youngModuli,
            double[] thicknesses,
            int[] bondedInterfaces)
        {
            if (poissonRatios?.Length != LayerCount)
                throw new ArgumentException($"PoissonRatios array must have {LayerCount} elements");
            if (youngModuli?.Length != LayerCount)
                throw new ArgumentException($"YoungModuli array must have {LayerCount} elements");
            if (thicknesses?.Length != LayerCount)
                throw new ArgumentException($"Thicknesses array must have {LayerCount} elements");
            if (bondedInterfaces?.Length != LayerCount - 1)
                throw new ArgumentException($"BondedInterfaces array must have {LayerCount - 1} elements");

            // Free existing handles
            FreeArrayHandles();

            // Pin arrays and assign pointers
            _arrayHandles[0] = GCHandle.Alloc(poissonRatios, GCHandleType.Pinned);
            _arrayHandles[1] = GCHandle.Alloc(youngModuli, GCHandleType.Pinned);
            _arrayHandles[2] = GCHandle.Alloc(thicknesses, GCHandleType.Pinned);
            _arrayHandles[3] = GCHandle.Alloc(bondedInterfaces, GCHandleType.Pinned);

            _nativeInput.poisson_ratio = _arrayHandles[0].AddrOfPinnedObject();
            _nativeInput.young_modulus = _arrayHandles[1].AddrOfPinnedObject();
            _nativeInput.thickness = _arrayHandles[2].AddrOfPinnedObject();
            _nativeInput.bonded_interface = _arrayHandles[3].AddrOfPinnedObject();
        }

        /// <summary>
        /// Set calculation depth coordinates
        /// </summary>
        public void SetCalculationPoints(double[] zCoordinates)
        {
            if (zCoordinates?.Length != CalculationPointCount)
                throw new ArgumentException($"ZCoordinates array must have {CalculationPointCount} elements");

            // Free existing z_coords handle if allocated
            if (_arrayHandles[4].IsAllocated)
                _arrayHandles[4].Free();

            // Pin z coordinates array
            _arrayHandles[4] = GCHandle.Alloc(zCoordinates, GCHandleType.Pinned);
            _nativeInput.z_coords = _arrayHandles[4].AddrOfPinnedObject();
        }

        /// <summary>
        /// Get the native structure for P/Invoke calls
        /// </summary>
        public ref PavementInputC GetNativeStruct()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ManagedPavementInput));
            return ref _nativeInput;
        }

        /// <summary>
        /// Free array handles
        /// </summary>
        private void FreeArrayHandles()
        {
            for (int i = 0; i < _arrayHandles.Length; i++)
            {
                if (_arrayHandles[i].IsAllocated)
                {
                    _arrayHandles[i].Free();
                }
            }
        }

        /// <summary>
        /// Dispose managed resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                FreeArrayHandles();
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// Managed wrapper for PavementOutputC that handles result array marshalling
    /// </summary>
    public class ManagedPavementOutput : IDisposable
    {
        private PavementOutputC _nativeOutput;
        private bool _disposed = false;

        public bool Success => _nativeOutput.success != 0;
        public PavementErrorCode ErrorCode => (PavementErrorCode)_nativeOutput.error_code;
        public string ErrorMessage => _nativeOutput.error_message ?? string.Empty;
        public int CalculationPointCount => _nativeOutput.nz;
        public double CalculationTimeMs => _nativeOutput.calculation_time_ms;

        /// <summary>
        /// Initialize from native output structure
        /// </summary>
        internal ManagedPavementOutput(PavementOutputC nativeOutput)
        {
            _nativeOutput = nativeOutput;
        }

        /// <summary>
        /// Get deflection results in mm
        /// </summary>
        public double[] GetDeflections()
        {
            if (!Success || _nativeOutput.deflection_mm == IntPtr.Zero)
                return new double[0];

            return MarshalArray(_nativeOutput.deflection_mm, CalculationPointCount);
        }

        /// <summary>
        /// Get vertical stress results in kPa
        /// </summary>
        public double[] GetVerticalStresses()
        {
            if (!Success || _nativeOutput.vertical_stress_kpa == IntPtr.Zero)
                return new double[0];

            return MarshalArray(_nativeOutput.vertical_stress_kpa, CalculationPointCount);
        }

        /// <summary>
        /// Get horizontal strain results in microstrain
        /// </summary>
        public double[] GetHorizontalStrains()
        {
            if (!Success || _nativeOutput.horizontal_strain == IntPtr.Zero)
                return new double[0];

            return MarshalArray(_nativeOutput.horizontal_strain, CalculationPointCount);
        }

        /// <summary>
        /// Get radial strain results in microstrain
        /// </summary>
        public double[] GetRadialStrains()
        {
            if (!Success || _nativeOutput.radial_strain == IntPtr.Zero)
                return new double[0];

            return MarshalArray(_nativeOutput.radial_strain, CalculationPointCount);
        }

        /// <summary>
        /// Get shear stress results in kPa
        /// </summary>
        public double[] GetShearStresses()
        {
            if (!Success || _nativeOutput.shear_stress_kpa == IntPtr.Zero)
                return new double[0];

            return MarshalArray(_nativeOutput.shear_stress_kpa, CalculationPointCount);
        }

        /// <summary>
        /// Get native structure reference for P/Invoke calls
        /// </summary>
        internal ref PavementOutputC GetNativeStruct()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ManagedPavementOutput));
            return ref _nativeOutput;
        }

        /// <summary>
        /// Marshal native double array to managed array
        /// </summary>
        private static double[] MarshalArray(IntPtr arrayPtr, int count)
        {
            if (arrayPtr == IntPtr.Zero || count <= 0)
                return new double[0];

            double[] result = new double[count];
            Marshal.Copy(arrayPtr, result, 0, count);
            return result;
        }

        /// <summary>
        /// Dispose managed resources (output will be freed by NativePavementCalculator)
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                // Note: We don't free the native output here - that's handled by the native API
                _disposed = true;
            }
        }
    }
}
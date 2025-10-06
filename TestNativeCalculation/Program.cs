using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TestNativeCalculation
{
    class Program
    {
        // Import the native DLL functions
        [DllImport("PavementCalculationEngine.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int PavementCalculate(ref PavementInputC input, ref PavementOutputC output);

        [DllImport("PavementCalculationEngine.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern void PavementFreeOutput(ref PavementOutputC output);

        [DllImport("PavementCalculationEngine.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr PavementGetVersion();

        [StructLayout(LayoutKind.Sequential)]
        struct PavementInputC
        {
            public int nlayer;
            public IntPtr poisson_ratio;
            public IntPtr young_modulus;
            public IntPtr thickness;
            public IntPtr bonded_interface;
            public int wheel_type;
            public double pressure_kpa;
            public double wheel_radius_m;
            public double wheel_spacing_m;
            public int nz;
            public IntPtr z_coords;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PavementOutputC
        {
            public int success;
            public int error_code;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string error_message;
            public int nz;
            public double calculation_time_ms;
            public IntPtr deflection_mm;
            public IntPtr vertical_stress_kpa;
            public IntPtr horizontal_strain;
            public IntPtr radial_strain;
            public IntPtr shear_stress_kpa;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("=== Native Pavement Calculation Test ===");
            Console.WriteLine();

            // Get version
            IntPtr versionPtr = PavementGetVersion();
            string version = Marshal.PtrToStringAnsi(versionPtr) ?? "Unknown";
            Console.WriteLine($"Native library version: {version}");
            Console.WriteLine();

            // Create test input - simple 4-layer structure
            double[] youngModuli = new double[] { 7000.0, 23000.0, 23000.0, 120.0 }; // MPa
            double[] poissonRatios = new double[] { 0.35, 0.25, 0.25, 0.35 };
            double[] thicknesses = new double[] { 0.06, 0.15, 0.15, 1000.0 }; // m
            int[] bondedInterfaces = new int[] { 0, 1, 0 }; // bonded/unbonded flags
            double[] zCoords = new double[] { 0.0, 0.06, 0.21, 0.36, 1.0 }; // calculation points

            // Pin arrays in memory
            GCHandle youngHandle = GCHandle.Alloc(youngModuli, GCHandleType.Pinned);
            GCHandle poissonHandle = GCHandle.Alloc(poissonRatios, GCHandleType.Pinned);
            GCHandle thicknessHandle = GCHandle.Alloc(thicknesses, GCHandleType.Pinned);
            GCHandle bondedHandle = GCHandle.Alloc(bondedInterfaces, GCHandleType.Pinned);
            GCHandle zHandle = GCHandle.Alloc(zCoords, GCHandleType.Pinned);

            try
            {
                // Create input structure
                PavementInputC input = new PavementInputC
                {
                    nlayer = 4,
                    poisson_ratio = poissonHandle.AddrOfPinnedObject(),
                    young_modulus = youngHandle.AddrOfPinnedObject(),
                    thickness = thicknessHandle.AddrOfPinnedObject(),
                    bonded_interface = bondedHandle.AddrOfPinnedObject(),
                    wheel_type = 1, // Twin wheels
                    pressure_kpa = 662.0, // kPa
                    wheel_radius_m = 0.125, // m
                    wheel_spacing_m = 0.375, // m
                    nz = 5,
                    z_coords = zHandle.AddrOfPinnedObject()
                };

                Console.WriteLine("Input parameters:");
                Console.WriteLine($"  Layers: {input.nlayer}");
                Console.WriteLine($"  Pressure: {input.pressure_kpa} kPa");
                Console.WriteLine($"  Wheel radius: {input.wheel_radius_m} m");
                Console.WriteLine($"  Young's moduli: [{string.Join(", ", youngModuli)}] MPa");
                Console.WriteLine($"  Poisson ratios: [{string.Join(", ", poissonRatios)}]");
                Console.WriteLine($"  Calculation points: {input.nz}");
                Console.WriteLine();

                // Create output structure
                PavementOutputC output = new PavementOutputC();

                // Call native calculation
                Console.WriteLine("Calling native calculation...");
                int result = PavementCalculate(ref input, ref output);

                Console.WriteLine($"Result code: {result}");
                Console.WriteLine($"Success: {output.success}");
                Console.WriteLine($"Error code: {output.error_code}");
                Console.WriteLine($"Error message: {output.error_message}");
                Console.WriteLine($"Calculation time: {output.calculation_time_ms:F2} ms");
                Console.WriteLine();

                if (output.success != 0 && output.deflection_mm != IntPtr.Zero)
                {
                    // Read results
                    double[] deflections = new double[output.nz];
                    double[] verticalStresses = new double[output.nz];
                    double[] horizontalStrains = new double[output.nz];

                    Marshal.Copy(output.deflection_mm, deflections, 0, output.nz);
                    Marshal.Copy(output.vertical_stress_kpa, verticalStresses, 0, output.nz);
                    Marshal.Copy(output.horizontal_strain, horizontalStrains, 0, output.nz);

                    Console.WriteLine("Results:");
                    Console.WriteLine($"  Deflections (mm): [{string.Join(", ", deflections.Select(d => d.ToString("F4")))}]");
                    Console.WriteLine($"  Vertical stresses (kPa): [{string.Join(", ", verticalStresses.Select(s => s.ToString("F4")))}]");
                    Console.WriteLine($"  Horizontal strains (με): [{string.Join(", ", horizontalStrains.Select(e => e.ToString("F4")))}]");

                    // Free output memory
                    PavementFreeOutput(ref output);
                }
                else
                {
                    Console.WriteLine("Calculation failed or no results.");
                }

                Console.WriteLine();
                Console.WriteLine("Check C:\\Temp\\PavementDebug.txt for detailed debug information.");
            }
            finally
            {
                // Free pinned arrays
                youngHandle.Free();
                poissonHandle.Free();
                thicknessHandle.Free();
                bondedHandle.Free();
                zHandle.Free();
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}

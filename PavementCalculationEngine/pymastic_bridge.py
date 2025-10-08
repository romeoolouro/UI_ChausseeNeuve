#!/usr/bin/env python3
"""
PyMastic Bridge - Standalone calculation interface for C# integration
Provides validated PyMastic computation as subprocess for C++ DLL
"""

import sys
import json
import traceback

# Add PyMastic to path
sys.path.append('extern/PyMastic')

try:
    from Main.MLE import PyMastic
except ImportError as e:
    print(f"ERROR: Cannot import PyMastic: {e}", file=sys.stderr)
    sys.exit(1)

def calculate_pymastic(input_data):
    """
    Calculate pavement response using validated PyMastic Python
    
    Args:
        input_data (dict): {
            "q_kpa": 667.0,                    # Pressure in kPa
            "a_m": 0.1125,                     # Radius in meters
            "z_depths_m": [0.04],              # Measurement depths in meters
            "H_thicknesses_m": [0.04, 0.15],  # Layer thicknesses in meters
            "E_moduli_mpa": [5500, 600, 50],   # Elastic moduli in MPa
            "nu_poisson": [0.35, 0.35, 0.35], # Poisson ratios
            "bonded_interfaces": [1, 1]        # 1=bonded, 0=frictionless
        }
        
    Returns:
        dict: {
            "success": True,
            "displacement_z_m": [6.346e-04],   # Vertical displacement in meters
            "stress_z_mpa": [521.7],           # Vertical stress in MPa  
            "strain_z_microdef": [711.6],      # Vertical strain in microstrain
            "strain_r_microdef": [...]         # Radial strain in microstrain
        }
    """
    
    try:
        # Extract parameters
        q_kpa = input_data["q_kpa"]
        a_m = input_data["a_m"]
        z_depths_m = input_data["z_depths_m"]
        H_thicknesses_m = input_data["H_thicknesses_m"]
        E_moduli_mpa = input_data["E_moduli_mpa"]
        nu_poisson = input_data["nu_poisson"]
        bonded_interfaces = input_data["bonded_interfaces"]
        
        # Convert to PyMastic US units
        q_psi = q_kpa * 0.145038                    # kPa → psi
        a_in = a_m * 39.3701                       # m → inches
        z_depths_in = [z * 39.3701 for z in z_depths_m]  # m → inches
        H_thicknesses_in = [h * 39.3701 for h in H_thicknesses_m]  # m → inches
        E_moduli_ksi = [e * 0.145038 for e in E_moduli_mpa]  # MPa → ksi
        
        # PyMastic calculation setup
        x = [0, 8]  # Center + offset (PyMastic needs multiple x for stability)
        z = z_depths_in
        
        # Call validated PyMastic Python
        result = PyMastic(
            q=q_psi,
            a=a_in, 
            x=x,
            z=z,
            H=H_thicknesses_in,
            E=E_moduli_ksi,
            nu=nu_poisson,
            ZRO=7e-7,
            isBounded=bonded_interfaces,
            iteration=40,
            inverser='solve'
        )
        
        # Extract results (at center x=0)
        displacement_z_in = []
        stress_z_psi = []
        strain_z_fraction = []
        strain_r_fraction = []
        
        for i in range(len(z_depths_m)):
            displacement_z_in.append(float(result['Displacement_Z'][i, 0]))
            stress_z_psi.append(float(result['Stress_Z'][i, 0]))
            strain_z_fraction.append(float(result['Strain_Z'][i, 0]))
            strain_r_fraction.append(float(result['Strain_R'][i, 0]))
        
        # Convert back to metric units
        displacement_z_m = [d / 39.3701 for d in displacement_z_in]  # inches → m
        stress_z_mpa = [s / 0.145038 for s in stress_z_psi]          # psi → MPa
        strain_z_microdef = [s * 1e6 for s in strain_z_fraction]    # fraction → μɛ
        strain_r_microdef = [s * 1e6 for s in strain_r_fraction]    # fraction → μɛ
        
        return {
            "success": True,
            "displacement_z_m": displacement_z_m,
            "stress_z_mpa": stress_z_mpa,
            "strain_z_microdef": strain_z_microdef,
            "strain_r_microdef": strain_r_microdef,
            "error_message": ""
        }
        
    except Exception as e:
        return {
            "success": False,
            "displacement_z_m": [],
            "stress_z_mpa": [],
            "strain_z_microdef": [],
            "strain_r_microdef": [],
            "error_message": f"PyMastic calculation failed: {str(e)}"
        }

def main():
    """Command line interface for C++ subprocess integration"""
    try:
        # Parse input JSON from stdin or command line
        if len(sys.argv) == 2:
            input_json = sys.argv[1]
        else:
            input_json = sys.stdin.read().strip()
        
        input_data = json.loads(input_json)
        
        # Calculate
        result = calculate_pymastic(input_data)
        
        # Output JSON result
        print(json.dumps(result, indent=2))
        
        # Exit with success/failure code
        sys.exit(0 if result["success"] else 1)
        
    except Exception as e:
        error_result = {
            "success": False,
            "displacement_z_m": [],
            "stress_z_mpa": [],
            "strain_z_microdef": [],
            "strain_r_microdef": [],
            "error_message": f"Bridge error: {str(e)}\n{traceback.format_exc()}"
        }
        
        print(json.dumps(error_result, indent=2))
        sys.exit(1)

if __name__ == '__main__':
    main()
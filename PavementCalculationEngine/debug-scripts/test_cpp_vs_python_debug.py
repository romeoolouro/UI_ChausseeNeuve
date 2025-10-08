#!/usr/bin/env python3
"""
Test C++ PyMasticSolver against VALIDATED Python parameters
to isolate the exact error source.
"""

import sys
import os
import json
import subprocess

# Add PyMastic path for Python reference
sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'extern', 'PyMastic'))

from Main.MLE import PyMastic

def test_cpp_vs_python():
    """
    Compare C++ PyMasticSolver vs Python with EXACT validated parameters
    """
    print("=" * 70)
    print("C++ vs PYTHON PYMASTIC COMPARISON")
    print("=" * 70)
    
    # EXACT validated parameters (Tableau I.1, 0.01% error)
    q_kpa = 667.0
    a_m = 0.1125
    E_mpa = [5500, 600, 50]
    h_m = [0.04, 0.15]
    nu = [0.35, 0.35, 0.35]
    
    # Convert to US units (PyMastic requirement)
    q_psi = q_kpa * 0.145038
    a_in = a_m * 39.3701
    H_in = [h_m[0] * 39.3701, h_m[1] * 39.3701]
    E_ksi = [E_mpa[i] * 0.145038 for i in range(3)]
    
    # Measurement at interface BBM/GNT (validated)
    x = [0, 8]  # Center + offset
    z = [H_in[0]]  # Interface BBM/GNT
    
    print(f"\nVALIDATED PARAMETERS:")
    print(f"  q = {q_psi:.2f} psi ({q_kpa} kPa)")
    print(f"  a = {a_in:.2f} inches ({a_m} m)")
    print(f"  H = [{H_in[0]:.3f}, {H_in[1]:.3f}] inches")
    print(f"  E = [{E_ksi[0]:.1f}, {E_ksi[1]:.1f}, {E_ksi[2]:.1f}] ksi")
    print(f"  nu = {nu}")
    print(f"  z = {z[0]:.3f} inches (interface BBM/GNT)")
    print(f"  isBounded = [1, 1] (collée/bonded)")
    
    # 1. Python PyMastic (REFERENCE - validated at 0.01% error)
    print(f"\n{'='*50}")
    print(f"1. PYTHON PYMASTIC (REFERENCE)")
    print(f"{'='*50}")
    
    try:
        RS_python = PyMastic(q_psi, a_in, x, z, H_in, E_ksi, nu, 7e-7, 
                           isBounded=[1, 1], iteration=40, inverser='solve')
        
        strain_z_py = RS_python['Strain_Z'][0, 0]
        stress_z_py = RS_python['Stress_Z'][0, 0]
        disp_z_py = RS_python['Displacement_Z'][0, 0]
        
        strain_micro_py = strain_z_py * 1e6
        
        print(f"Python Results:")
        print(f"  Displacement Z: {disp_z_py:.6e} inches")
        print(f"  Stress Z:       {stress_z_py:.3f} psi")
        print(f"  Strain Z:       {strain_micro_py:.1f} μɛ")
        print(f"  ✅ Expected: ~711.5 μɛ (validated)")
        
    except Exception as e:
        print(f"❌ Python Error: {e}")
        return
    
    # 2. C++ PyMasticSolver (via PavementAPI)
    print(f"\n{'='*50}")
    print(f"2. C++ PYMASTICSOLVER (DEBUG)")
    print(f"{'='*50}")
    
    # Prepare PavementAPI input (convert to PavementAPI units/structure)
    input_data = {
        'pressure_kpa': q_kpa,
        'radius_m': a_m,
        'x_offset_m': 0.0,
        'z_depth_m': h_m[0],  # Interface BBM/GNT
        'layer_thicknesses_m': h_m,
        'elastic_moduli_mpa': E_mpa,
        'poisson_ratios': nu,
        'engine': 'PYMASTIC'  # Force PyMastic engine
    }
    
    print(f"C++ Input (via PavementAPI):")
    print(f"  pressure_kpa: {input_data['pressure_kpa']}")
    print(f"  radius_m: {input_data['radius_m']}")
    print(f"  z_depth_m: {input_data['z_depth_m']}")
    print(f"  layers: {len(E_mpa)} layers")
    
    # Test via pymastic_wrapper.py (Python subprocess calling PyMastic)
    print(f"\n{'='*30}")
    print(f"PYTHON WRAPPER TEST:")
    print(f"{'='*30}")
    
    wrapper_input = {
        'q_psi': q_psi,
        'a_in': a_in,
        'x': x,
        'z': z,
        'H_in': H_in,
        'E_ksi': E_ksi,
        'nu': nu,
        'isBounded': [1, 1],
        'iteration': 40
    }
    
    try:
        # Call Python wrapper
        result = subprocess.run([
            'python', 'pymastic_wrapper.py'
        ], input=json.dumps(wrapper_input), text=True, capture_output=True, cwd='.')
        
        if result.returncode == 0:
            wrapper_output = json.loads(result.stdout)
            strain_micro_wrapper = wrapper_output['strain_z_micro'][0]
            stress_wrapper = wrapper_output['stress_z_psi'][0]
            
            print(f"Wrapper Results:")
            print(f"  Strain Z: {strain_micro_wrapper:.1f} μɛ")
            print(f"  Stress Z: {stress_wrapper:.3f} psi")
            
            # Compare wrapper vs direct
            error_wrapper = abs(strain_micro_wrapper - strain_micro_py) / strain_micro_py * 100
            print(f"  Wrapper vs Direct: {error_wrapper:.3f}% error")
            
        else:
            print(f"Wrapper Error: {result.stderr}")
            
    except Exception as e:
        print(f"Wrapper Exception: {e}")
    
    # 3. Analysis and next steps
    print(f"\n{'='*50}")
    print(f"ANALYSIS & NEXT STEPS")
    print(f"{'='*50}")
    print(f"Reference (Python): εz = {strain_micro_py:.1f} μɛ")
    print(f"\nTo debug C++ PyMasticSolver:")
    print(f"1. Add detailed logging to PyMasticSolver.cpp")
    print(f"2. Compare intermediate calculations step by step:")
    print(f"   - Hankel integration grid setup")
    print(f"   - Bessel function values J0/J1")
    print(f"   - Boundary condition matrices (Left/Right)")
    print(f"   - State vector coefficients A,B,C,D")
    print(f"   - Final response calculation")
    print(f"3. Verify unit conversions at each step")
    print(f"4. Check numerical precision (double vs float)")

if __name__ == "__main__":
    test_cpp_vs_python()
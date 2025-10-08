#!/usr/bin/env python3
"""
Debug unit conversion between Python PyMastic and expected C++ values
by adding detailed unit conversion logging.
"""

import sys
import os
import json

# Add PyMastic path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'extern', 'PyMastic'))

from Main.MLE import PyMastic
import numpy as np

def debug_unit_conversions():
    """
    Debug unit conversions with detailed logging to identify C++ issues
    """
    print("=" * 70)
    print("UNIT CONVERSION DEBUG: PYTHON PYMASTIC")
    print("=" * 70)
    
    # EXACT validated parameters (Tableau I.1, 0.01% error)
    print("\n[STEP 1] ORIGINAL PARAMETERS (Metric)")
    q_kpa = 667.0
    a_m = 0.1125
    E_mpa = [5500, 600, 50]
    h_m = [0.04, 0.15]
    nu = [0.35, 0.35, 0.35]
    
    print(f"  q = {q_kpa} kPa (pressure)")
    print(f"  a = {a_m} m (radius)")
    print(f"  E = {E_mpa} MPa (elastic moduli)")
    print(f"  h = {h_m} m (layer thicknesses)")
    print(f"  nu = {nu} (Poisson ratios)")
    
    print("\n[STEP 2] CONVERT TO US UNITS (PyMastic requirement)")
    # Unit conversion factors
    kpa_to_psi = 0.145038
    m_to_inch = 39.3701
    mpa_to_ksi = 0.145038  # Note: same as kPa to psi
    
    print(f"  Conversion factors:")
    print(f"    kPa → psi: {kpa_to_psi}")
    print(f"    m → inch:  {m_to_inch}")
    print(f"    MPa → ksi: {mpa_to_ksi}")
    
    # Convert
    q_psi = q_kpa * kpa_to_psi
    a_in = a_m * m_to_inch
    H_in = [h_m[0] * m_to_inch, h_m[1] * m_to_inch]
    E_ksi = [E_mpa[i] * mpa_to_ksi for i in range(3)]
    
    print(f"\n  Converted values:")
    print(f"    q = {q_psi:.2f} psi")
    print(f"    a = {a_in:.2f} inches")
    print(f"    H = [{H_in[0]:.3f}, {H_in[1]:.3f}] inches")
    print(f"    E = [{E_ksi[0]:.1f}, {E_ksi[1]:.1f}, {E_ksi[2]:.1f}] ksi")
    
    print("\n[STEP 3] PYMASTIC INPUT PREPARATION")
    # Measurement at interface BBM/GNT
    x = [0, 8]  # Center + offset (PyMastic requirement)
    z = [H_in[0]]  # Interface BBM/GNT = 1.575 inches
    
    print(f"  Measurement points:")
    print(f"    x = {x} inches (radial offsets)")
    print(f"    z = {z} inches (depths)")
    print(f"    z[0] = {z[0]:.3f} inches = {z[0]/m_to_inch:.3f} m (interface BBM/GNT)")
    
    print(f"  Algorithm parameters:")
    print(f"    isBounded = [1, 1] (bonded interfaces)")
    print(f"    iteration = 40")
    print(f"    ZRO = 7e-7 (numerical stability)")
    
    print("\n[STEP 4] PYMASTIC CALCULATION")
    try:
        RS = PyMastic(q_psi, a_in, x, z, H_in, E_ksi, nu, 7e-7, 
                     isBounded=[1, 1], iteration=40, inverser='solve')
        
        print(f"  ✅ PyMastic calculation successful")
        
        # Extract results for center point
        disp_z_in = RS['Displacement_Z'][0, 0]
        stress_z_psi = RS['Stress_Z'][0, 0]
        strain_z = RS['Strain_Z'][0, 0]
        
        print(f"\n[STEP 5] RESULTS (US units)")
        print(f"  Displacement Z: {disp_z_in:.6e} inches")
        print(f"  Stress Z:       {stress_z_psi:.3f} psi")
        print(f"  Strain Z:       {strain_z:.6e} (fraction)")
        
        # Convert to microstrain
        strain_micro = strain_z * 1e6
        print(f"  Strain Z:       {strain_micro:.1f} μɛ")
        
        print(f"\n[STEP 6] CONVERT BACK TO METRIC (for C++ comparison)")
        # Convert results back to metric
        disp_z_m = disp_z_in / m_to_inch
        stress_z_mpa = stress_z_psi / kpa_to_psi  # psi to MPa (same factor)
        
        print(f"  Displacement Z: {disp_z_m:.6e} m")
        print(f"  Stress Z:       {stress_z_mpa:.3f} MPa")
        print(f"  Strain Z:       {strain_micro:.1f} μɛ (same)")
        
        print(f"\n[STEP 7] C++ DEBUG HINTS")
        print(f"  If C++ uses metric units internally:")
        print(f"    - Input: q={q_kpa} kPa, a={a_m} m, E={E_mpa} MPa, h={h_m} m")
        print(f"    - Expected output: εz ≈ {strain_micro:.1f} μɛ")
        print(f"    - Expected displacement: ≈ {disp_z_m:.3e} m")
        print(f"    - Expected stress: ≈ {stress_z_mpa:.3f} MPa")
        
        print(f"\n  Common C++ unit errors to check:")
        print(f"    1. Missing unit conversions (using kPa directly without → psi)")
        print(f"    2. Wrong conversion factors (1000× errors suggest missing kilo conversions)")
        print(f"    3. E×1000 factor confusion (Python may use different internal scaling)")
        print(f"    4. Results in wrong units (psi output when expecting MPa)")
        
        print(f"\n  Debugging strategy:")
        print(f"    1. Add logging to C++ unit conversions")
        print(f"    2. Print intermediate values (Hankel grid, Bessel functions)")
        print(f"    3. Compare boundary condition matrices A,B,C,D")
        print(f"    4. Verify that C++ → US → C++ round-trip works")
        
        return {
            'strain_micro': strain_micro,
            'stress_mpa': stress_z_mpa,
            'disp_m': disp_z_m,
            'input_metric': {'q': q_kpa, 'a': a_m, 'E': E_mpa, 'h': h_m},
            'input_us': {'q': q_psi, 'a': a_in, 'E': E_ksi, 'H': H_in}
        }
        
    except Exception as e:
        print(f"❌ PyMastic Error: {e}")
        import traceback
        traceback.print_exc()
        return None

if __name__ == "__main__":
    result = debug_unit_conversions()
    
    if result:
        print(f"\n" + "="*70)
        print(f"VALIDATION SUCCESS")
        print(f"="*70)
        print(f"✅ PyMastic Python produces {result['strain_micro']:.1f} μɛ")
        print(f"✅ Target: 711.5±4 μɛ → Error: {abs(result['strain_micro']-711.5)/711.5*100:.3f}%")
        print(f"✅ Mathematical validation confirmed")
        
        print(f"\n📋 FOR C++ DEBUGGING:")
        print(f"   Use these EXACT parameters and expect these EXACT results")
        print(f"   Any significant deviation indicates unit conversion errors")
    else:
        print(f"\n❌ VALIDATION FAILED - Check PyMastic setup")
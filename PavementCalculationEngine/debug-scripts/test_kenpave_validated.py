#!/usr/bin/env python3
"""
Test PyMastic with VALIDATED parameters from KenPave comparison
to confirm our understanding of units and signs before debugging Tableaux.
"""

import sys
import os

# Add PyMastic path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'extern', 'PyMastic'))

from Main.MLE import PyMastic
import numpy as np

def test_validated_kenpave():
    """
    Test PyMastic with exact KenPave validated parameters
    From Validation/Strain_Validation.py
    """
    print("=" * 70)
    print("PYMASTIC - KENPAVE VALIDATED TEST")
    print("=" * 70)
    
    # Exact parameters from Validation/Strain_Validation.py
    q = 100.0      # psi (PRESSURE, not total load lb)
    a = 5.99       # inches (radius)
    H = [6, 10]    # inches (AC=6in, Base=10in)
    E = [500, 40, 10]  # ksi
    nu = [0.35, 0.4, 0.45]
    isBD = [1, 1]  # bonded interfaces
    
    # Test at key depths
    x = [0]  # Center of load
    z = [0, 6, 16]  # Surface, AC/Base interface, Base/Subgrade interface
    
    print(f"\nValidated Parameters (from KenPave comparison):")
    print(f"  Pressure: q = {q} psi")
    print(f"  Radius:   a = {a} inches")
    print(f"  Layers:")
    print(f"    AC Layer:   E={E[0]} ksi, h={H[0]}in, ν={nu[0]} (bonded)")
    print(f"    Base Layer: E={E[1]} ksi, h={H[1]}in, ν={nu[1]} (bonded)")
    print(f"    Subgrade:   E={E[2]} ksi, h=∞, ν={nu[2]}")
    
    try:
        RS = PyMastic(q, a, x, z, H, E, nu, 7e-7, isBounded=isBD, iteration=40)
        
        print(f"\n" + "="*50)
        print(f"RESULTS (PyMastic Sign Convention)")
        print(f"  Positive Strain/Stress = COMPRESSION")
        print(f"  Negative Strain/Stress = TENSION")
        print(f"="*50)
        
        for i, z_val in enumerate(z):
            if z_val == 0:
                layer = "Surface (AC top)"
            elif z_val == 6:
                layer = "AC/Base interface"
            else:
                layer = "Base/Subgrade interface"
                
            disp_z = RS['Displacement_Z'][i, 0]
            stress_z = RS['Stress_Z'][i, 0]
            strain_z = RS['Strain_Z'][i, 0]
            
            print(f"\nz = {z_val} inches ({layer}):")
            print(f"  Displacement Z: {disp_z:.6e} inches")
            print(f"  Stress Z:       {stress_z:.3f} psi", end="")
            if stress_z > 0:
                print(f" (COMPRESSION ✓)")
            else:
                print(f" (TENSION)")
                
            strain_micro = strain_z * 1e6
            print(f"  Strain Z:       {strain_micro:.1f} μɛ", end="")
            if strain_z > 0:
                print(f" (COMPRESSION ✓)")
            else:
                print(f" (TENSION)")
        
        print(f"\n" + "="*50)
        print(f"PHYSICAL INTERPRETATION:")
        print(f"="*50)
        print(f"Under surface load at z=0:")
        print(f"  - Expect COMPRESSION (positive stress/strain)")
        print(f"  - Displacement should be POSITIVE (downward, z+ direction)")
        print(f"\nConclusion:")
        if RS['Stress_Z'][0, 0] > 0:
            print(f"  ✅ PyMastic produces POSITIVE stress at surface = COMPRESSION")
            print(f"  ✅ Sign convention confirmed: Positive = Compression")
        
        return RS
        
    except Exception as e:
        print(f"❌ Error: {e}")
        import traceback
        traceback.print_exc()
        return None

if __name__ == "__main__":
    test_validated_kenpave()
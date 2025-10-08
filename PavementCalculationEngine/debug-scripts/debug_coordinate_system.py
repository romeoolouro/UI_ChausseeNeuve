#!/usr/bin/env python3
"""
Debug PyMastic coordinate system - check z convention and stress signs
"""

import sys
import os

# Add PyMastic path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'extern', 'PyMastic'))

from Main.MLE import PyMastic
import numpy as np

def debug_coordinate_system():
    """Test PyMastic coordinate system and stress signs"""
    print("=" * 70)
    print("DEBUG PYMASTIC COORDINATE SYSTEM")
    print("=" * 70)
    
    # Simple test case
    q = 100.0      # psi (reasonable tire pressure)
    a = 6.0        # inches
    H = [10, 6]    # layer thicknesses
    E = [500, 40, 10]  # ksi
    nu = [0.35, 0.4, 0.45]
    isBD = [1, 1]  # bonded interfaces
    it = 10
    ZRO = 7e-7
    
    # Test different z positions
    test_cases = [
        ("Surface z=0", [0], "Should be maximum compression under load"),
        ("Shallow z=2", [2], "Still in top layer, compressed"),
        ("Interface z=10", [10], "At layer 1/2 interface"),
        ("Deep z=15", [15], "In layer 2"),
        ("Very deep z=20", [20], "In bottom layer"),
        ("Negative z=-5", [-5], "Above surface (air) - should give error or strange results")
    ]
    
    x = [0]  # Center of load
    
    for name, z_test, description in test_cases:
        print(f"\n{name}: z={z_test}")
        print(f"  Description: {description}")
        
        try:
            RS = PyMastic(q, a, x, z_test, H, E, nu, ZRO, isBounded=isBD, iteration=it)
            
            disp_z = RS['Displacement_Z'][0, 0]
            stress_z = RS['Stress_Z'][0, 0]
            stress_r = RS['Stress_R'][0, 0] if 'Stress_R' in RS else None
            
            print(f"  Displacement Z: {disp_z:.3e} inches")
            print(f"  Stress Z: {stress_z:.3e} psi")
            if stress_r is not None:
                print(f"  Stress R: {stress_r:.3e} psi")
            
            # Analyze signs
            if disp_z > 0:
                print(f"  📈 Displacement: POSITIVE (upward or away from load)")
            else:
                print(f"  📉 Displacement: NEGATIVE (downward or toward load)")
                
            if stress_z > 0:
                print(f"  🔴 Stress Z: POSITIVE (tension)")
            else:
                print(f"  🔵 Stress Z: NEGATIVE (compression)")
                
        except Exception as e:
            print(f"  ❌ Error: {e}")
            
    print(f"\n" + "="*50)
    print(f"PHYSICAL EXPECTATIONS:")
    print(f"="*50)
    print(f"Under a surface load, we expect:")
    print(f"  - Surface displacement: NEGATIVE (downward)")
    print(f"  - Stress under load: NEGATIVE (compression)")
    print(f"  - Stress magnitude decreases with depth")

if __name__ == "__main__":
    debug_coordinate_system()
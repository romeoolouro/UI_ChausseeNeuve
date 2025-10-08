#!/usr/bin/env python3
"""
Debug PyMastic Test.py example - check if q should be pressure, not total load
"""

import sys
import os
import math

# Add PyMastic path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'extern', 'PyMastic'))

from Main.MLE import PyMastic
import numpy as np

def debug_pymastic_units():
    """Debug PyMastic Test.py - is q pressure or total load?"""
    print("=" * 70)
    print("DEBUG PYMASTIC UNITS")
    print("=" * 70)
    
    # Test.py parameters
    q_test = 100.0      # Test.py says "lb" (force)
    a_test = 5.99       # inches (radius)
    
    # Calculate pressure if q_test is total load
    area = math.pi * a_test**2  # sq inches
    q_pressure = q_test / area  # psi if q_test is in lb
    
    print(f"Test.py analysis:")
    print(f"  q = {q_test} lb (total load)")
    print(f"  a = {a_test} inches (radius)")
    print(f"  Area = π × {a_test}² = {area:.2f} sq inches")
    print(f"  Pressure = {q_test}/{area:.2f} = {q_pressure:.2f} psi")
    
    # Test both interpretations
    x = [0, 8]
    z = [0, 9.99, 10.01]
    H = [10, 6]
    E = [500, 40, 10]
    nu = [0.35, 0.4, 0.45]
    isBD = [0, 0]
    it = 10
    ZRO = 7e-7
    
    print(f"\nTesting q as TOTAL LOAD (Test.py original):")
    try:
        RS1 = PyMastic(q_test, a_test, x, z, H, E, nu, ZRO, isBounded=isBD, iteration=it)
        disp1 = RS1['Displacement_Z'][0, 0]
        stress1 = RS1['Stress_Z'][0, 0]
        print(f"  Displacement: {disp1:.3e} inches")
        print(f"  Stress: {stress1:.3e} psi")
        if stress1 > 0:
            print(f"  ⚠️ POSITIVE stress (tension) - unexpected for loading")
        else:
            print(f"  ✅ NEGATIVE stress (compression) - expected")
    except Exception as e:
        print(f"  ❌ Error: {e}")
    
    print(f"\nTesting q as PRESSURE:")
    try:
        RS2 = PyMastic(q_pressure, a_test, x, z, H, E, nu, ZRO, isBounded=isBD, iteration=it)
        disp2 = RS2['Displacement_Z'][0, 0]
        stress2 = RS2['Stress_Z'][0, 0]
        print(f"  Displacement: {disp2:.3e} inches")
        print(f"  Stress: {stress2:.3e} psi")
        if stress2 > 0:
            print(f"  ⚠️ POSITIVE stress (tension) - unexpected for loading")
        else:
            print(f"  ✅ NEGATIVE stress (compression) - expected")
    except Exception as e:
        print(f"  ❌ Error: {e}")
    
    # Test realistic pressure (typical tire pressure)
    print(f"\nTesting realistic tire pressure (100 psi):")
    try:
        RS3 = PyMastic(100.0, a_test, x, z, H, E, nu, ZRO, isBounded=isBD, iteration=it)
        disp3 = RS3['Displacement_Z'][0, 0]
        stress3 = RS3['Stress_Z'][0, 0]
        print(f"  Displacement: {disp3:.3e} inches")
        print(f"  Stress: {stress3:.3e} psi")
        if stress3 > 0:
            print(f"  ⚠️ POSITIVE stress (tension) - unexpected for loading")
        else:
            print(f"  ✅ NEGATIVE stress (compression) - expected")
    except Exception as e:
        print(f"  ❌ Error: {e}")

if __name__ == "__main__":
    debug_pymastic_units()
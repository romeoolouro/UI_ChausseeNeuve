#!/usr/bin/env python3
"""
Test PyMastic with its own reference example from Test.py
to verify our wrapper implementation is correct.
"""

import sys
import os

# Add PyMastic path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'extern', 'PyMastic'))

from Main.MLE import PyMastic
import numpy as np

def test_pymastic_reference():
    """Test PyMastic with its own Test.py example"""
    print("=" * 70)
    print("TEST PYMASTIC - REFERENCE EXAMPLE (Test.py)")
    print("=" * 70)
    
    # Exact parameters from Test.py
    q = 100.0                   # lb.
    a = 5.99                    # inch
    x = [0, 8]                  # number of columns in response
    z = [0, 9.99, 10.01]        # number of rows in response
    H = [10, 6]                 # inch
    E = [500, 40, 10]           # ksi
    nu = [0.35, 0.4, 0.45]
    ZRO = 7*1e-7                # to avoid numerical instability
    isBD = [0, 0]               # unbonded interfaces
    it = 10
    
    print(f"Parameters (from Test.py):")
    print(f"  q = {q} lb")
    print(f"  a = {a} inch") 
    print(f"  x = {x}")
    print(f"  z = {z}")
    print(f"  H = {H} inch")
    print(f"  E = {E} ksi")
    print(f"  nu = {nu}")
    print(f"  isBounded = {isBD} (unbonded)")
    print(f"  iterations = {it}")
    
    # Run PyMastic
    print(f"\nRunning PyMastic...")
    try:
        RS = PyMastic(q, a, x, z, H, E, nu, ZRO, isBounded=isBD, iteration=it, inverser='solve')
        
        print(f"\nResults (from Test.py expected output):")
        print(f"Displacement [0, 0]: {RS['Displacement_Z'][0, 0]:.6e}")
        print(f"Sigma Z [0, 0]:     {RS['Stress_Z'][0, 0]:.6e}")
        print(f"Displacement_H [0, 0]: {RS['Displacement_H'][0, 0]:.6e}")
        print(f"Sigma T [0, 0]:     {RS['Stress_T'][0, 0]:.6e}")
        
        print(f"\nDisplacement [1, 0]: {RS['Displacement_Z'][1, 0]:.6e}")
        print(f"Sigma Z [1, 0]:     {RS['Stress_Z'][1, 0]:.6e}")
        print(f"Sigma R [1, 0]:     {RS['Stress_R'][1, 0]:.6e}")
        print(f"Sigma T [1, 0]:     {RS['Stress_T'][1, 0]:.6e}")
        
        # Check for signs and magnitudes
        print(f"\n" + "="*50)
        print(f"ANALYSIS:")
        print(f"="*50)
        
        disp_surface = RS['Displacement_Z'][0, 0]
        stress_surface = RS['Stress_Z'][0, 0]
        
        print(f"Surface displacement: {disp_surface:.3e} inches")
        print(f"Surface stress:       {stress_surface:.3e} psi")
        
        if disp_surface < 0:
            print(f"✅ Displacement NEGATIVE (downward) - expected under load")
        else:
            print(f"⚠️  Displacement POSITIVE (upward) - unusual under load")
            
        if stress_surface < 0:
            print(f"✅ Stress NEGATIVE (compression) - expected under load")
        else:
            print(f"⚠️  Stress POSITIVE (tension) - unusual at surface under load")
            
        return RS
        
    except Exception as e:
        print(f"❌ PyMastic ERROR: {e}")
        import traceback
        traceback.print_exc()
        return None

if __name__ == "__main__":
    test_pymastic_reference()
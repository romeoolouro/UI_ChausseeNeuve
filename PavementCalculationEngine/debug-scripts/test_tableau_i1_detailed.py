#!/usr/bin/env python3
"""
Tableau I.1 validation with correct understanding of PyMastic signs
and measurement positions.
"""

import sys
import os

# Add PyMastic path
sys.path.insert(0, os.path.join(os.path.dirname(__file__), 'extern', 'PyMastic'))

from Main.MLE import PyMastic
import numpy as np

def tableau_i1_detailed():
    """
    Test Tableau I.1 with multiple measurement positions to find correct location
    """
    print("=" * 70)
    print("TABLEAU I.1 - STRUCTURE SOUPLE (DETAILED)")
    print("=" * 70)
    
    # EXACT values from user
    E_mpa = [5500, 600, 50]
    h_m = [0.04, 0.15]  # meters
    nu = [0.35, 0.35, 0.35]
    
    # Convert to US units (PyMastic requirement)
    H_in = [h_m[0] * 39.3701, h_m[1] * 39.3701]  # [1.575, 5.906] inches
    E_ksi = [E_mpa[i] * 0.145038 for i in range(3)]  # ksi
    
    # Loading - SUPPOSED values
    q_kpa = 662.0
    a_m = 0.1125
    q_psi = q_kpa * 0.145038
    a_in = a_m * 39.3701
    
    print(f"\nConfiguration:")
    print(f"  Pression: {q_psi:.2f} psi ({q_kpa} kPa) [SUPPOSÉE]")
    print(f"  Rayon: {a_in:.2f} inches ({a_m}m) [SUPPOSÉ]")
    print(f"\nCouches (EXACTES):")
    print(f"  BBM: E={E_ksi[0]:.1f} ksi ({E_mpa[0]} MPa), h={H_in[0]:.3f}in ({h_m[0]}m), ν={nu[0]}")
    print(f"  GNT: E={E_ksi[1]:.1f} ksi ({E_mpa[1]} MPa), h={H_in[1]:.3f}in ({h_m[1]}m), ν={nu[1]}")
    print(f"  PF2: E={E_ksi[2]:.1f} ksi ({E_mpa[2]} MPa), ν={nu[2]}")
    
    # Test multiple positions
    test_positions = [
        ("Surface (z=0)", 0),
        ("Milieu BBM", H_in[0] / 2),
        ("Bas BBM (interface BBM/GNT)", H_in[0]),
        ("Milieu GNT", H_in[0] + H_in[1] / 2),
        ("Bas GNT (interface GNT/PF2)", H_in[0] + H_in[1]),
    ]
    
    x = [0]  # Center
    z = [pos[1] for pos in test_positions]
    
    print(f"\n{'='*70}")
    print(f"TEST AVEC SUPPOSITIONS (q={q_psi:.1f} psi, a={a_in:.2f}in)")
    print(f"{'='*70}")
    
    try:
        RS = PyMastic(q_psi, a_in, x, z, H_in, E_ksi, nu, 7e-7, isBounded=[1, 1], iteration=40)
        
        print(f"\nRÉSULTATS (PyMastic: +Strain = Compression, -Strain = Tension):")
        print(f"-" * 70)
        
        for i, (name, z_val) in enumerate(test_positions):
            strain_z = RS['Strain_Z'][i, 0]
            strain_micro = strain_z * 1e6
            stress_z = RS['Stress_Z'][i, 0]
            
            sign_str = "COMPR" if strain_z > 0 else "TENS"
            
            print(f"{name:35s} z={z_val:6.3f}in:")
            print(f"  εz = {strain_micro:10.1f} μɛ ({sign_str}), σz = {stress_z:8.2f} psi")
            
            # Check if close to target
            error = abs(strain_micro - 711.5)
            if error < 100:  # Within 100 μɛ
                print(f"  → PROCHE de la cible 711.5±4 μɛ (erreur: {error:.1f} μɛ)")
        
        print(f"\n{'='*70}")
        print(f"ANALYSE:")
        print(f"{'='*70}")
        print(f"Target: εz = +711.5±4 μɛ (COMPRESSION)")
        print(f"\nSi aucune position ne donne ~711 μɛ:")
        print(f"  1. Vérifier les paramètres de chargement (q, a)")
        print(f"  2. Vérifier la position de mesure exacte")
        print(f"  3. Essayer différentes valeurs de q et a")
        
    except Exception as e:
        print(f"\n❌ Erreur: {e}")
        import traceback
        traceback.print_exc()

if __name__ == "__main__":
    tableau_i1_detailed()
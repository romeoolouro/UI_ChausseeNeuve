import sys
sys.path.append('extern/PyMastic')
from Main.MLE import PyMastic
import numpy as np

# Instruments MLE.py to print m_values size
import Main.MLE as MLE_module

# Same inputs
q = 100.0
a = 5.99
x = [0, 8]
z = [0]
H = [10, 6]
E = [500, 40, 10]
nu = [0.35, 0.4, 0.45]
ZRO = 7e-7
isBD = [0, 0]
it = 5

print("Calling PyMastic with iteration=5...")
RS = PyMastic(q, a, x, z, H, E, nu, ZRO, isBounded=isBD, iteration=it, inverser='solve')

print(f"Result Stress_Z[0,0] (x=0) = {RS['Stress_Z'][0,0]:.2f} psi")
print(f"Result Stress_Z[0,1] (x=8) = {RS['Stress_Z'][0,1]:.2f} psi")
print(f"Result Displacement_Z[0,0] (x=0) = {RS['Displacement_Z'][0,0]:.6f} µ-in")
print(f"Result Displacement_Z[0,1] (x=8) = {RS['Displacement_Z'][0,1]:.6f} µ-in")

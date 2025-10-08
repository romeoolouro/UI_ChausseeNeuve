import sys
sys.path.append('extern/PyMastic')
from Main.MLE import PyMastic

# Same inputs as C++ test
RS = PyMastic(
    q=100,      # psi
    a=5.99,     # inches
    x=[0, 8],   # Center and 8 inches offset (matching Test.py)
    z=[0, 9.99, 10.01],  # Surface, just before/after interface
    H=[10, 6],  # Layer thicknesses in inches
    E=[500, 40, 10],  # ksi (will be *1000 → psi inside)
    nu=[0.35, 0.4, 0.45],
    ZRO=7e-7,
    isBounded=[0, 0],
    iteration=5,  # Reduced to avoid singular matrix
    inverser='solve'
)

print("="*60)
print("Python PyMastic Reference Values")
print("="*60)
print(f"z=0 inches (surface):")
print(f"  Displacement_Z: {RS['Displacement_Z'][0,0]:.6f} µ-in")
print(f"  Stress_Z: {RS['Stress_Z'][0,0]:.2f} psi")
print(f"  Strain_Z: {RS['Strain_Z'][0,0]:.6e} (fraction)")
print(f"  Strain_R: {RS['Strain_R'][0,0]:.6e} (fraction)")
print()
print(f"z=9.99 inches (before interface):")
print(f"  Displacement_Z: {RS['Displacement_Z'][1,0]:.6f} µ-in")
print(f"  Stress_Z: {RS['Stress_Z'][1,0]:.2f} psi")
print(f"  Strain_Z: {RS['Strain_Z'][1,0]:.6e} (fraction)")
print(f"  Strain_R: {RS['Strain_R'][1,0]:.6e} (fraction)")
print()
print(f"z=10.01 inches (after interface):")
print(f"  Displacement_Z: {RS['Displacement_Z'][2,0]:.6f} µ-in")
print(f"  Stress_Z: {RS['Stress_Z'][2,0]:.2f} psi")
print(f"  Strain_Z: {RS['Strain_Z'][2,0]:.6e} (fraction)")
print(f"  Strain_R: {RS['Strain_R'][2,0]:.6e} (fraction)")
print("="*60)

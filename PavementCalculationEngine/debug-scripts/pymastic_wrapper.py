#!/usr/bin/env python3
"""
PyMastic Python Wrapper for C++ Interop
Provides command-line interface to Python PyMastic for C++ validation
"""
import sys
import json
sys.path.append('extern/PyMastic')
from Main.MLE import PyMastic

def main():
    if len(sys.argv) < 2:
        print("Usage: pymastic_wrapper.py <input.json>", file=sys.stderr)
        sys.exit(1)
    
    # Read input JSON
    with open(sys.argv[1], 'r') as f:
        params = json.load(f)
    
    # Call PyMastic
    try:
        RS = PyMastic(
            q=params['q'],
            a=params['a'],
            x=params['x'],
            z=params['z'],
            H=params['H'],
            E=params['E'],
            nu=params['nu'],
            ZRO=params.get('ZRO', 7e-7),
            isBounded=params.get('isBounded', [0] * (len(params['H']))),
            iteration=params.get('iteration', 5),
            inverser=params.get('inverser', 'solve')
        )
        
        # Output JSON
        output = {
            'Displacement_Z': RS['Displacement_Z'].tolist(),
            'Displacement_H': RS['Displacement_H'].tolist(),
            'Stress_Z': RS['Stress_Z'].tolist(),
            'Stress_R': RS['Stress_R'].tolist(),
            'Stress_T': RS['Stress_T'].tolist(),
            'Strain_Z': RS['Strain_Z'].tolist(),
            'Strain_R': RS['Strain_R'].tolist(),
            'Strain_T': RS['Strain_T'].tolist()
        }
        
        print(json.dumps(output, indent=2))
        return 0
        
    except Exception as e:
        print(f"ERROR: {e}", file=sys.stderr)
        return 1

if __name__ == '__main__':
    sys.exit(main())

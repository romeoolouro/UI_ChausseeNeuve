# C++ PyMastic Port Debugging Strategy

**Status**: Planned for future implementation  
**Priority**: Medium (Python bridge provides immediate working solution)  
**Goal**: Achieve <0.1% accuracy vs validated Python PyMastic  

## Current State Analysis

### ✅ **Python PyMastic (Reference)**
- **Mathematical Validation**: 0.01% error vs Tableau I.1 (711.6 μɛ vs 711.5±4 μɛ)
- **Proven Accuracy**: Mathematically validated against academic reference
- **Performance**: ~1-2 seconds per calculation
- **Status**: Production ready via Python bridge

### ❌ **C++ PyMasticSolver (Current Issues)**
- **Error Magnitude**: 1,578× error (1,123,227 μɛ vs 711.6 μɛ expected)
- **Unit Conversion**: Partially fixed (input q: 667 kPa → 96.74 psi ✅)
- **Algorithmic Issues**: Major discrepancies remain beyond unit conversions
- **Status**: Requires systematic debugging

## Root Cause Investigation Plan

### Phase 1: Unit Conversion Audit
- [ ] **Task 1.1**: Verify all internal unit conversions match Python exactly
  - Pressure: kPa ↔ psi (0.145038) ✅ (partially done)
  - Length: m ↔ inches (39.3701) 
  - Modulus: MPa ↔ ksi (0.145038)
  - **Focus**: E moduli scaling factors (Python MLE.py line 82: `E*1000`)

- [ ] **Task 1.2**: Create unit conversion test harness
  - Input: Exact Python reference parameters
  - Output: Step-by-step unit conversion logging
  - Validation: Each intermediate step matches Python debug

### Phase 2: Algorithm Component Comparison  
- [ ] **Task 2.1**: Bessel Function Validation
  - Compare C++ vs Python Bessel J0/J1 values
  - Test: Same m-values should give identical Bessel results
  - Tools: Boost.Math vs Python scipy.special

- [ ] **Task 2.2**: Hankel Integration Grid Comparison
  - m-values array comparison (C++ vs Python)
  - ft_weights (Gauss quadrature) validation
  - Grid size and distribution analysis

- [ ] **Task 2.3**: Boundary Condition Matrices Debug
  - Matrix A, B, C, D coefficient comparison
  - Layer-by-layer boundary condition analysis  
  - Interface bonding logic validation

### Phase 3: State Vector Propagation
- [ ] **Task 3.1**: Layer Interface Logic
  - Validate layer index calculations
  - Interface depth (lamda) array comparison
  - Bonded vs frictionless boundary logic

- [ ] **Task 3.2**: Matrix Operations Validation
  - Linear algebra solver comparison ("solve" vs alternatives)
  - Matrix inversion accuracy and stability
  - Singular matrix detection and handling

### Phase 4: Response Calculation
- [ ] **Task 4.1**: Displacement Calculation Debug
  - Formula validation: `(1+ν)/E * BesselJ0 * matrix_terms`
  - Scaling factor analysis
  - Sign convention verification

- [ ] **Task 4.2**: Stress/Strain Calculation Debug  
  - Stress formula: `-q * α * stress_sum`
  - Strain-stress relationship verification
  - Output unit consistency check

## Implementation Approach

### **Recommended Strategy: Incremental Validation**

1. **Start with Simplest Case**: Single layer, center point (x=0), surface (z=0)
2. **Add Complexity Gradually**: Multi-layer → Off-center → Deep measurements
3. **Use Python Reference**: Each step validates against exact Python intermediate values
4. **Binary Search Debug**: Isolate which algorithm component introduces error

### **Tools and Resources**

- **Reference Implementation**: `validate_pymastic_tableaux.py` (0.01% proven accuracy)
- **Debug Framework**: `debug_units_detailed.py` (step-by-step unit analysis)
- **Test Parameters**: Tableau I.1 validated parameters (q=667kPa, a=0.1125m, etc.)
- **Target Accuracy**: 711.6 μɛ ± 0.1% (match Python bridge results)

## Success Criteria

- [ ] **C++ PyMasticSolver produces <0.1% error** vs Python reference
- [ ] **Unit tests pass** for all Tableau I.1/I.5 validation cases  
- [ ] **Performance acceptable** (<10 seconds per calculation)
- [ ] **Mathematical validation** against academic Tableaux confirmed

## Timeline Estimate

- **Phase 1-2**: 2-3 weeks (unit audit + component comparison)
- **Phase 3-4**: 2-4 weeks (algorithm debugging + validation) 
- **Total**: 4-7 weeks for complete C++ port accuracy

## Decision Rationale

**Why continue C++ debugging?**
- **Long-term Performance**: C++ will be faster than Python subprocess (~10-100x)
- **Independence**: No Python runtime dependency for deployment
- **Completeness**: Pure C++ solution aligns with project architecture

**Why Python bridge for now?**
- **Immediate Production Use**: 0.0% error, mathematically validated
- **Risk Mitigation**: Guaranteed accuracy while C++ debugging continues
- **User Satisfaction**: Working solution available immediately

---

**Next Action**: When ready to proceed, start with Phase 1 unit conversion audit using existing debug framework.
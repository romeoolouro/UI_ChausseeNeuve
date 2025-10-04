#pragma once

/**
 * @file Constants.h
 * @brief Named constants for pavement calculation engine
 * 
 * This file replaces all magic numbers with named constants to improve
 * code readability, maintainability, and allow easy sensitivity analysis.
 */

namespace Pavement {
namespace Constants {

// ============================================================================
// NUMERICAL INTEGRATION PARAMETERS
// ============================================================================

/** Number of Gauss-Legendre quadrature points for Hankel transform integration */
constexpr int GAUSS_QUADRATURE_POINTS = 4;

/** Gauss-Legendre quadrature points for 4-point integration on [-1, 1] */
constexpr double GAUSS_POINTS_4[4] = {
    -0.8611363115940526,
    -0.3399810435848563,
     0.3399810435848563,
     0.8611363115940526
};

/** Gauss-Legendre quadrature weights for 4-point integration */
constexpr double GAUSS_WEIGHTS_4[4] = {
    0.3478548451374538,
    0.6521451548625461,
    0.6521451548625461,
    0.3478548451374538
};

/** 
 * Integration upper bound factor for Hankel transform.
 * Integration over [0, HANKEL_INTEGRATION_BOUND / contactRadius].
 * Rationale: Bessel function J₁(m·r) decays rapidly for m·r > 70,
 * so contributions beyond this point are negligible (<10⁻¹⁵).
 */
constexpr double HANKEL_INTEGRATION_BOUND = 70.0;

/** 
 * Minimum Hankel parameter m to avoid singularity at m=0.
 * Rationale: Bessel functions have removable singularity at origin.
 */
constexpr double MIN_HANKEL_PARAMETER = 1e-10;

/**
 * Exponential argument limit to prevent overflow.
 * For exp(m·z), if m·z > EXPONENTIAL_OVERFLOW_LIMIT, set result to 0.
 * Rationale: exp(50) ≈ 5×10²¹ approaches double precision limit.
 */
constexpr double EXPONENTIAL_OVERFLOW_LIMIT = 50.0;

// ============================================================================
// MATERIAL PROPERTY LIMITS
// ============================================================================

/** Minimum Poisson's ratio (0 = no lateral expansion) */
constexpr double MIN_POISSON_RATIO = 0.0;

/** Maximum Poisson's ratio (0.5 = incompressible material) */
constexpr double MAX_POISSON_RATIO = 0.5;

/** 
 * Typical minimum Poisson's ratio for warning (concrete, asphalt).
 * Values below this are unusual but physically valid.
 */
constexpr double TYPICAL_MIN_POISSON_RATIO = 0.15;

/** 
 * Typical maximum Poisson's ratio for warning (most materials).
 * Values above this suggest nearly incompressible material (rubber, saturated clay).
 */
constexpr double TYPICAL_MAX_POISSON_RATIO = 0.45;

/** Minimum Young's modulus in MPa (soft soil) */
constexpr double MIN_YOUNG_MODULUS = 0.0;

/** Maximum Young's modulus in MPa (high-performance concrete) */
constexpr double MAX_YOUNG_MODULUS = 100000.0;

/**
 * Warning threshold for very soft materials (MPa).
 * Rationale: E < 10 MPa suggests very soft soil, verify data.
 */
constexpr double SOFT_MATERIAL_WARNING_THRESHOLD = 10.0;

/**
 * Warning threshold for very stiff materials (MPa).
 * Rationale: E > 50000 MPa is typical only for high-grade concrete.
 */
constexpr double STIFF_MATERIAL_WARNING_THRESHOLD = 50000.0;

// ============================================================================
// LAYER GEOMETRY LIMITS
// ============================================================================

/** Minimum layer count */
constexpr int MIN_LAYER_COUNT = 1;

/** Maximum layer count (practical limit for computation) */
constexpr int MAX_LAYER_COUNT = 20;

/** 
 * Minimum layer thickness in meters (10 mm).
 * Rationale: Thinner layers cause numerical instability in matrix assembly.
 */
constexpr double MIN_LAYER_THICKNESS = 0.01;

/**
 * Maximum layer thickness for non-platform layers in meters.
 * Rationale: Real pavement layers rarely exceed 1m. Larger values suggest
 * this should be modeled as platform (semi-infinite).
 */
constexpr double MAX_NON_PLATFORM_THICKNESS = 10.0;

// ============================================================================
// LOAD CONFIGURATION LIMITS
// ============================================================================

/** Wheel type: isolated wheel */
constexpr int WHEEL_TYPE_ISOLATED = 1;

/** Wheel type: twin wheels */
constexpr int WHEEL_TYPE_TWIN = 2;

/** Minimum tire pressure in MPa */
constexpr double MIN_TIRE_PRESSURE = 0.0;

/** 
 * Maximum tire pressure in MPa.
 * Rationale: Typical heavy truck: 0.7-0.9 MPa. 5 MPa is extreme upper bound.
 */
constexpr double MAX_TIRE_PRESSURE = 5.0;

/** Minimum contact radius in meters */
constexpr double MIN_CONTACT_RADIUS = 0.0;

/** 
 * Maximum contact radius in meters.
 * Rationale: Typical truck tire: 0.10-0.15m. 1m is unrealistic.
 */
constexpr double MAX_CONTACT_RADIUS = 1.0;

/** Maximum wheel spacing for twin wheels in meters */
constexpr double MAX_WHEEL_SPACING = 2.0;

// ============================================================================
// NUMERICAL STABILITY THRESHOLDS
// ============================================================================

/**
 * Maximum acceptable modulus contrast ratio.
 * Rationale: Ratio > 10000:1 causes ill-conditioned matrices.
 * Example: Concrete (40000 MPa) over soft soil (4 MPa) = 10000:1.
 */
constexpr double MAX_MODULUS_CONTRAST = 10000.0;

/**
 * Matrix condition number warning threshold.
 * Rationale: κ(A) > 10¹² suggests matrix is nearly singular.
 * Solution may have large relative errors (> 1%).
 */
constexpr double CONDITION_NUMBER_WARNING_THRESHOLD = 1e12;

/**
 * Residual tolerance for solution verification.
 * After solving Ax=b, check ‖Ax - b‖ < RESIDUAL_TOLERANCE.
 * Rationale: Machine epsilon ~10⁻¹⁶, so 10⁻⁶ allows for accumulation
 * of rounding errors in O(n³) algorithm.
 */
constexpr double RESIDUAL_TOLERANCE = 1e-6;

// ============================================================================
// UNIT CONVERSION FACTORS
// ============================================================================

/** Conversion: meters to millimeters */
constexpr double M_TO_MM = 1000.0;

/** Conversion: strain to microstrain (με) */
constexpr double STRAIN_TO_MICROSTRAIN = 1e6;

/** Conversion: MPa to Pa */
constexpr double MPA_TO_PA = 1e6;

// ============================================================================
// INTERFACE TYPES
// ============================================================================

/** Interface type: bonded (full continuity) */
constexpr int INTERFACE_BONDED = 0;

/** Interface type: unbonded/slip (no shear transfer) */
constexpr int INTERFACE_UNBONDED = 1;

/** Interface type: rough (partial shear transfer) */
constexpr int INTERFACE_ROUGH = 2;

} // namespace Constants
} // namespace Pavement

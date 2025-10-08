#include "PyMasticSolver.h"
#include <algorithm>
#include <cmath>
#include <stdexcept>
#include <iostream>

#ifndef M_PI
#define M_PI 3.14159265358979323846
#endif

// Include Boost.Math for Bessel functions if available
#ifdef BOOST_MATH_BESSEL_JY_HPP
#include <boost/math/special_functions/bessel.hpp>
#define BOOST_AVAILABLE
#endif

// Hardcoded Bessel zeros (from PyMastic reference)
static const double BESSEL_J0_ZEROS[] = {
    2.40482555769577, 5.52007811028631, 8.65372791291101, 11.7915344390143, 14.9309177084878,
    18.0710639679109, 21.2116366298793, 24.3524715307493, 27.4934791320403, 30.6346064684320,
    33.7758202135736, 36.9170983536640, 40.0584257646282, 43.1997917131767, 46.3411883716618,
    49.4826098973978, 52.6240518411150, 55.7655107550200, 58.9069839260809, 62.0484691902272,
    65.1899648002069, 68.3314693298568, 71.4729816035937, 74.6145006437018, 77.7560256303881,
    80.8975558711376, 84.0390907769382, 87.1806298436412, 90.3221726372105, 93.4637187819448,
    96.6052679509963, 99.7468198586806, 102.888374254195, 106.029930916452, 109.171489649805,
    112.313050280495, 115.454612653667, 118.596176630873, 121.737742087951, 124.879308913233,
    128.020877006008, 131.162446275214, 134.304016638305, 137.445588020284, 140.587160352854,
    143.728733573690, 146.870307625797, 150.011882456955, 153.153458019228, 156.295034268534
};

static const double BESSEL_J1_ZEROS[] = {
    3.83170597020751, 7.01558666981562, 10.1734681350627, 13.3236919363142, 16.4706300508776,
    19.6158585104682, 22.7600843805928, 25.9036720876184, 29.0468285349169, 32.1896799109744,
    35.3323075500839, 38.4747662347716, 41.6170942128145, 44.759318997652, 47.9014608871855,
    51.0435351835715, 54.1855536410613, 57.3275254379010, 60.4694578453475, 63.6113566984812,
    66.7532267340985, 69.8950718374958, 73.0368952255738, 76.1786995846415, 79.3204871754763,
    82.4622599143736, 85.6040194363502, 88.7457671449263, 91.8875042516950, 95.0292318080447,
    98.1709507307908, 101.312661823039, 104.454365791283, 107.596063259509, 110.737754780899,
    113.879440847595, 117.021121898892, 120.162798328149, 123.304470488636, 126.446138698517,
    129.587803245104, 132.729464388510, 135.871122364789, 139.012777388660, 142.154429655859,
    145.296079345196, 148.437726620342, 151.579371631401, 154.721014516286, 157.862655401930
};

static const int BESSEL_ZEROS_COUNT = 50;

bool PyMasticSolver::Input::Validate() const {
    if (q_kpa <= 0 || a_m <= 0) return false;
    if (x_offsets.empty() || z_depths.empty()) return false;
    if (H_thicknesses.empty() || E_moduli.empty() || nu_poisson.empty()) return false;
    
    // Layer count consistency
    int n_layers = static_cast<int>(E_moduli.size());
    if (static_cast<int>(nu_poisson.size()) != n_layers) return false;
    if (static_cast<int>(H_thicknesses.size()) != n_layers - 1) return false; // Semi-infinite layer
    if (static_cast<int>(bonded_interfaces.size()) != n_layers - 1) return false;
    
    // Physical constraints
    for (double E : E_moduli) if (E <= 0) return false;
    for (double nu : nu_poisson) if (nu < 0 || nu >= 0.5) return false;
    for (double H : H_thicknesses) if (H <= 0) return false;
    
    return true;
}

void PyMasticSolver::Output::Initialize(int n_z, int n_x) {
    displacement_z = Eigen::MatrixXd::Zero(n_z, n_x);
    displacement_h = Eigen::MatrixXd::Zero(n_z, n_x);
    stress_z = Eigen::MatrixXd::Zero(n_z, n_x);
    stress_r = Eigen::MatrixXd::Zero(n_z, n_x);
    stress_t = Eigen::MatrixXd::Zero(n_z, n_x);
    strain_z = Eigen::MatrixXd::Zero(n_z, n_x);
    strain_r = Eigen::MatrixXd::Zero(n_z, n_x);
    strain_t = Eigen::MatrixXd::Zero(n_z, n_x);
}

bool PyMasticSolver::Output::IsValid() const {
    return displacement_z.allFinite() && displacement_h.allFinite() &&
           stress_z.allFinite() && stress_r.allFinite() && stress_t.allFinite() &&
           strain_z.allFinite() && strain_r.allFinite() && strain_t.allFinite();
}

PyMasticSolver::Output PyMasticSolver::Compute(const Input& input) {
    if (!input.Validate()) {
        throw std::invalid_argument("Invalid input parameters");
    }
    
    Output output;
    output.Initialize(static_cast<int>(input.z_depths.size()), 
                     static_cast<int>(input.x_offsets.size()));
    
    try {
        // Setup Hankel integration grid
        std::vector<double> m_values, ft_weights;
        SetupHankelGrid(input, m_values, ft_weights);
        
        // Initialize state vector coefficient matrices
        int n_m = static_cast<int>(m_values.size());
        int n_layers = static_cast<int>(input.E_moduli.size());
        
        Eigen::MatrixXd A = Eigen::MatrixXd::Zero(n_m, n_layers);
        Eigen::MatrixXd B = Eigen::MatrixXd::Zero(n_m, n_layers);
        Eigen::MatrixXd C = Eigen::MatrixXd::Zero(n_m, n_layers);
        Eigen::MatrixXd D = Eigen::MatrixXd::Zero(n_m, n_layers);
        
        // Propagate state vector through boundary conditions
        PropagateStateVector(input, m_values, A, B, C, D);
        
        // Compute final responses
        ComputeResponses(input, m_values, ft_weights, A, B, C, D, output);
        
        return output;
        
    } catch (const std::exception& e) {
        std::cerr << "PyMasticSolver::Compute error: " << e.what() << std::endl;
        throw;
    }
}

std::vector<double> PyMasticSolver::ComputeBesselZeros(int order, int count) {
    std::vector<double> zeros;
    zeros.reserve(count);
    
    const double* source = (order == 0) ? BESSEL_J0_ZEROS : BESSEL_J1_ZEROS;
    int available = std::min(count, BESSEL_ZEROS_COUNT);
    
    for (int i = 0; i < available; ++i) {
        zeros.push_back(source[i]);
    }
    
    return zeros;
}

double PyMasticSolver::BesselJ0(double x) {
#ifdef BOOST_AVAILABLE
    return boost::math::cyl_bessel_j(0, x);
#else
    // Series expansion for small x, asymptotic for large x
    if (std::abs(x) < 8.0) {
        // Series: J0(x) = sum_{k=0}^∞ (-1)^k / (k!)^2 * (x/2)^{2k}
        double sum = 1.0;
        double term = 1.0;
        double x_half_sq = (x * x) / 4.0;
        
        for (int k = 1; k <= 20; ++k) {
            term *= -x_half_sq / (k * k);
            sum += term;
            if (std::abs(term) < 1e-15) break;
        }
        return sum;
    } else {
        // Asymptotic form: J0(x) ≈ sqrt(2/(πx)) * cos(x - π/4)
        return std::sqrt(2.0 / (M_PI * x)) * std::cos(x - M_PI / 4.0);
    }
#endif
}

double PyMasticSolver::BesselJ1(double x) {
#ifdef BOOST_AVAILABLE
    return boost::math::cyl_bessel_j(1, x);
#else
    // Series expansion for small x, asymptotic for large x
    if (std::abs(x) < 8.0) {
        // Series: J1(x) = (x/2) * sum_{k=0}^∞ (-1)^k / (k!(k+1)!) * (x/2)^{2k}
        double sum = 1.0;
        double term = 1.0;
        double x_half_sq = (x * x) / 4.0;
        
        for (int k = 1; k <= 20; ++k) {
            term *= -x_half_sq / (k * (k + 1));
            sum += term;
            if (std::abs(term) < 1e-15) break;
        }
        return (x / 2.0) * sum;
    } else {
        // Asymptotic form: J1(x) ≈ sqrt(2/(πx)) * cos(x - 3π/4)
        return std::sqrt(2.0 / (M_PI * x)) * std::cos(x - 3.0 * M_PI / 4.0);
    }
#endif
}

void PyMasticSolver::SetupHankelGrid(const Input& input, 
                                    std::vector<double>& m_values, 
                                    std::vector<double>& ft_weights) {
    
    // Compute normalized parameters (matching Python lines 89-93)
    double sumH = 0.0;
    for (double h : input.H_thicknesses) sumH += h;
    
    double alpha = input.a_m / sumH;
    
    // Get Bessel zeros (hardcoded arrays from Python PyMastic)
    std::vector<double> j0_zeros(BESSEL_J0_ZEROS, BESSEL_J0_ZEROS + BESSEL_ZEROS_COUNT);
    std::vector<double> j1_zeros(BESSEL_J1_ZEROS, BESSEL_J1_ZEROS + BESSEL_ZEROS_COUNT);
    
    // Scale zeros by radial offsets and radius (Python lines 95-98)
    // firstKindZeroOrder = firstKindZeroOrder / ro[:, None]
    // firstKindFirstOrder = firstKindFirstOrder / alpha
    std::vector<double> scaled_j0, scaled_j1;
    
    for (double x : input.x_offsets) {
        if (x == 0.0) x = 1e-6; // Avoid singularity at center
        double ro = x / sumH;
        for (double zero : j0_zeros) {
            scaled_j0.push_back(zero / ro);
        }
    }
    
    for (double zero : j1_zeros) {
        scaled_j1.push_back(zero / alpha);
    }
    
    // Combine and sort all zeros (Python lines 99-100)
    // BesselZeros = np.hstack((np.array([0]), firstKindZeroOrder.flatten(), firstKindFirstOrder.flatten()))
    std::vector<double> all_zeros;
    all_zeros.push_back(0.0); // Add zero first
    all_zeros.insert(all_zeros.end(), scaled_j0.begin(), scaled_j0.end());
    all_zeros.insert(all_zeros.end(), scaled_j1.begin(), scaled_j1.end());
    
    std::sort(all_zeros.begin(), all_zeros.end());
    all_zeros.erase(std::unique(all_zeros.begin(), all_zeros.end()), all_zeros.end());
    
    // Ensure we have at least 3 zeros for interval generation
    if (all_zeros.size() < 3) {
        throw std::runtime_error("Insufficient Bessel zeros for Hankel integration");
    }
    
    // Limit to iteration count (Python: BesselZeros[3:iteration])
    int max_zeros = std::min(input.iterations + 3, static_cast<int>(all_zeros.size()));
    if (max_zeros > 3) {
        all_zeros.resize(max_zeros);
    }
    
    // Setup integration intervals with specific spacing (Python lines 101-106)
    // D1 = (BesselZeros[1] - BesselZeros[0]) / 6 - 0.00001
    // D2 = (BesselZeros[2] - BesselZeros[1]) / 2 - 0.00001
    double D1 = (all_zeros[1] - all_zeros[0]) / 6.0 - 0.00001;
    double D2 = (all_zeros[2] - all_zeros[1]) / 2.0 - 0.00001;
    
    std::vector<double> mValues_base;
    
    // AUX1 = np.arange(BesselZeros[0], BesselZeros[1], D1)
    for (double val = all_zeros[0]; val < all_zeros[1]; val += D1) {
        mValues_base.push_back(val);
    }
    
    // AUX2 = np.arange(BesselZeros[1], BesselZeros[2], D2)
    // mValues = np.hstack((AUX1, AUX2[1:], BesselZeros[3:iteration]))
    for (double val = all_zeros[1] + D2; val < all_zeros[2]; val += D2) {
        mValues_base.push_back(val);
    }
    
    // Add remaining Bessel zeros
    for (size_t i = 3; i < all_zeros.size(); ++i) {
        mValues_base.push_back(all_zeros[i]);
    }
    
    // Generate 4-point Gauss quadrature for each interval (Python lines 107-118)
    const double gauss_points[4] = {-0.86114, -0.33998, 0.33998, 0.86114};
    const double gauss_weights[4] = {0.34786, 0.65215, 0.65215, 0.34786};
    
    m_values.clear();
    ft_weights.clear();
    
    // Python lines 107-118: Generate coefficient matrix and ftGauss weights
    // getDiff = np.diff(mValues)
    // coefficient[:,0] = getDiff / 2 - 0.86114 * (getDiff / 2)
    // coefficient[:,1] = getDiff / 2 - 0.33998 * (getDiff / 2)
    // coefficient[:,2] = getDiff / 2 + 0.33998 * (getDiff / 2)
    // coefficient[:,3] = getDiff / 2 + 0.86114 * (getDiff / 2)
    // ftGauss[0, :] = 0.34786 * (getDiff / 2)
    // ftGauss[1, :] = 0.65215 * (getDiff / 2)
    // ftGauss[2, :] = 0.65215 * (getDiff / 2)
    // ftGauss[3, :] = 0.34786 * (getDiff / 2)
    
    for (size_t i = 0; i < mValues_base.size() - 1; ++i) {
        double getDiff = mValues_base[i + 1] - mValues_base[i];
        double half_diff = getDiff / 2.0;
        double mid_point = mValues_base[i] + half_diff;
        
        // Generate 4 quadrature points for this interval
        for (int j = 0; j < 4; ++j) {
            // coefficient = getDiff / 2 +/- gauss_point * (getDiff / 2)
            double m_point = mid_point + gauss_points[j] * half_diff;
            double weight = gauss_weights[j] * half_diff;
            
            m_values.push_back(m_point);
            ft_weights.push_back(weight);
        }
    }
    
    // Python line 120: m = np.sort(mNotSorted)
    // Sort m_values and reorder ft_weights accordingly
    std::vector<size_t> indices(m_values.size());
    for (size_t i = 0; i < indices.size(); ++i) indices[i] = i;
    
    std::sort(indices.begin(), indices.end(), [&m_values](size_t a, size_t b) {
        return m_values[a] < m_values[b];
    });
    
    std::vector<double> sorted_m(m_values.size());
    std::vector<double> sorted_ft(ft_weights.size());
    
    for (size_t i = 0; i < indices.size(); ++i) {
        sorted_m[i] = m_values[indices[i]];
        sorted_ft[i] = ft_weights[indices[i]];
    }
    
    m_values = sorted_m;
    ft_weights = sorted_ft;
}

std::vector<double> PyMasticSolver::ComputeLamdaValues(const std::vector<double>& H) {
    double sumH = 0.0;
    for (double h : H) sumH += h;
    
    std::vector<double> lamda = {0.0};
    double cumulative = 0.0;
    for (double h : H) {
        cumulative += h;
        lamda.push_back(cumulative / sumH);
    }
    lamda.push_back(1000.0); // Semi-infinite layer
    
    return lamda;
}

int PyMasticSolver::FindLayerIndex(double depth, const std::vector<double>& lamda) {
    for (size_t i = 1; i < lamda.size(); ++i) {
        if (depth <= lamda[i]) {
            return static_cast<int>(i - 1);
        }
    }
    return static_cast<int>(lamda.size() - 2); // Last finite layer
}

Eigen::Matrix4d PyMasticSolver::BuildLeftMatrix(int i, double m, const Input& input,
                                               const std::vector<double>& lamda_bc) {
    Eigen::Matrix4d left;
    double nu_i = input.nu_poisson[i];
    double F = std::exp(-m * (lamda_bc[i + 1] - lamda_bc[i]));
    
    if (input.bonded_interfaces[i] == 1) {
        // Bonded interface: full continuity
        left << 1, F, -(1 - 2 * nu_i - m * lamda_bc[i]), (1 - 2 * nu_i + m * lamda_bc[i]) * F,
                1, -F, 2 * nu_i + m * lamda_bc[i], (2 * nu_i - m * lamda_bc[i]) * F,
                1, F, 1 + m * lamda_bc[i], -(1 - m * lamda_bc[i]) * F,
                1, -F, -(2 - 4 * nu_i - m * lamda_bc[i]), -(2 - 4 * nu_i + m * lamda_bc[i]) * F;
    } else {
        // Frictionless interface: partial continuity
        left << 1, F, -(1 - 2 * nu_i - m * lamda_bc[i]), (1 - 2 * nu_i + m * lamda_bc[i]) * F,
                1, -F, -(2 - 4 * nu_i - m * lamda_bc[i]), -(2 - 4 * nu_i + m * lamda_bc[i]) * F,
                1, -F, 2 * nu_i + m * lamda_bc[i], (2 * nu_i - m * lamda_bc[i]) * F,
                input.ZRO, input.ZRO, input.ZRO, input.ZRO;
    }
    
    return left;
}

Eigen::Matrix4d PyMasticSolver::BuildRightMatrix(int i, double m, const Input& input,
                                                const std::vector<double>& lamda_bc,
                                                const std::vector<double>& R) {
    Eigen::Matrix4d right;
    double nu_next = input.nu_poisson[i + 1];
    double F_next = std::exp(-m * (lamda_bc[i + 2] - lamda_bc[i + 1]));
    
    if (input.bonded_interfaces[i] == 1) {
        // Bonded interface
        right << F_next, 1, -(1 - 2 * nu_next - m * lamda_bc[i]) * F_next, 1 - 2 * nu_next + m * lamda_bc[i],
                 F_next, -1, (2 * nu_next + m * lamda_bc[i]) * F_next, 2 * nu_next - m * lamda_bc[i],
                 R[i] * F_next, R[i], (1 + m * lamda_bc[i]) * R[i] * F_next, -(1 - m * lamda_bc[i]) * R[i],
                 R[i] * F_next, -R[i], -(2 - 4 * nu_next - m * lamda_bc[i]) * R[i] * F_next, -(2 - 4 * nu_next + m * lamda_bc[i]) * R[i];
    } else {
        // Frictionless interface
        right << F_next, 1, -(1 - 2 * nu_next - m * lamda_bc[i]) * F_next, 1 - 2 * nu_next + m * lamda_bc[i],
                 R[i] * F_next, -R[i], -(2 - 4 * nu_next - m * lamda_bc[i]) * R[i] * F_next, -(2 - 4 * nu_next + m * lamda_bc[i]) * R[i],
                 input.ZRO, input.ZRO, input.ZRO, input.ZRO,
                 F_next, -1, (2 * nu_next + m * lamda_bc[i]) * F_next, 2 * nu_next - m * lamda_bc[i];
    }
    
    return right;
}

Eigen::Matrix4d PyMasticSolver::SolveMatrix(const Eigen::Matrix4d& left_matrix,
                                           const Eigen::Matrix4d& right_matrix,
                                           const std::string& inverser) {
    try {
        if (inverser == "solve") {
            return left_matrix.colPivHouseholderQr().solve(right_matrix);
        } else if (inverser == "inv") {
            return left_matrix.inverse() * right_matrix;
        } else if (inverser == "pinv") {
            Eigen::JacobiSVD<Eigen::Matrix4d> svd(left_matrix, Eigen::ComputeFullU | Eigen::ComputeFullV);
            return svd.solve(right_matrix);
        } else if (inverser == "lu") {
            return left_matrix.partialPivLu().solve(right_matrix);
        } else if (inverser == "svd") {
            Eigen::JacobiSVD<Eigen::Matrix4d> svd(left_matrix, Eigen::ComputeFullU | Eigen::ComputeFullV);
            return svd.solve(right_matrix);
        }
    } catch (...) {
        // Fallback to pseudo-inverse on numerical issues
        std::cerr << "Matrix solver failed, using pseudo-inverse" << std::endl;
        Eigen::JacobiSVD<Eigen::Matrix4d> svd(left_matrix, Eigen::ComputeFullU | Eigen::ComputeFullV);
        return svd.solve(right_matrix);
    }
    
    throw std::invalid_argument("Unknown matrix inverser: " + inverser);
}

void PyMasticSolver::PropagateStateVector(const Input& input,
                                         const std::vector<double>& m_values,
                                         Eigen::MatrixXd& A, Eigen::MatrixXd& B,
                                         Eigen::MatrixXd& C, Eigen::MatrixXd& D) {
    
    int n_layers = static_cast<int>(input.E_moduli.size());
    std::vector<double> lamda_bc = ComputeLamdaValues(input.H_thicknesses);
    
    // Compute elastic ratios
    std::vector<double> R;
    for (int i = 0; i < n_layers - 1; ++i) {
        R.push_back(input.E_moduli[i] / input.E_moduli[i + 1] * 
                    (1 + input.nu_poisson[i + 1]) / (1 + input.nu_poisson[i]));
    }
    
    for (size_t j = 0; j < m_values.size(); ++j) {
        double m = m_values[j];
        
        // Build cascade matrix multiplication (simplified approach)
        Eigen::Matrix4d cascade = Eigen::Matrix4d::Identity();
        
        for (int i = 0; i < n_layers - 1; ++i) {
            Eigen::Matrix4d left = BuildLeftMatrix(i, m, input, lamda_bc);
            Eigen::Matrix4d right = BuildRightMatrix(i, m, input, lamda_bc, R);
            Eigen::Matrix4d solved = SolveMatrix(left, right, input.inverser);
            cascade = cascade * solved;
        }
        
        // Surface boundary conditions (PyMastic Method 1)
        Eigen::Matrix2d surface_left;
        surface_left << std::exp(-m * lamda_bc[0]), 1,
                        std::exp(-m * lamda_bc[0]), -1;
        
        Eigen::Matrix2d surface_right;
        surface_right << -(1 - 2 * input.nu_poisson[0]) * std::exp(-m * lamda_bc[0]), 1 - 2 * input.nu_poisson[0],
                         2 * input.nu_poisson[0] * std::exp(-m * lamda_bc[0]), 2 * input.nu_poisson[0];
        
        // Combine surface matrices with cascade (following PyMastic lines 236-245)
        Eigen::Matrix<double, 2, 4> combined_surface;
        combined_surface.block<2, 2>(0, 0) = surface_left;
        combined_surface.block<2, 2>(0, 2) = surface_right;
        
        // Extract B_n, D_n columns from cascade
        Eigen::Matrix<double, 4, 2> bn_dn_matrix = cascade.block<4, 2>(0, 1); // Columns 1 and 3 (B and D)
        bn_dn_matrix.col(1) = cascade.col(3);
        
        Eigen::Matrix2d final_system = combined_surface * bn_dn_matrix;
        
        Eigen::Vector2d rhs;
        rhs << 1, 0;
        
        Eigen::Vector2d bn_dn;
        try {
            bn_dn = final_system.colPivHouseholderQr().solve(rhs);
        } catch (...) {
            // Fallback to pseudo-inverse
            Eigen::JacobiSVD<Eigen::Matrix2d> svd(final_system, Eigen::ComputeFullU | Eigen::ComputeFullV);
            bn_dn = svd.solve(rhs);
        }
        
        // Set bottom layer coefficients
        B(j, n_layers - 1) = bn_dn(0);
        D(j, n_layers - 1) = bn_dn(1);
        A(j, n_layers - 1) = 0.0; // PyMastic assumption
        C(j, n_layers - 1) = 0.0; // PyMastic assumption
        
        // Back-propagate to get all layer coefficients
        Eigen::Vector4d current_bc;
        current_bc << A(j, n_layers - 1), B(j, n_layers - 1), C(j, n_layers - 1), D(j, n_layers - 1);
        
        for (int i = n_layers - 2; i >= 0; --i) {
            Eigen::Matrix4d left = BuildLeftMatrix(i, m, input, lamda_bc);
            Eigen::Matrix4d right = BuildRightMatrix(i, m, input, lamda_bc, R);
            Eigen::Matrix4d solved = SolveMatrix(left, right, input.inverser);
            
            current_bc = solved * current_bc;
            A(j, i) = current_bc(0);
            B(j, i) = current_bc(1);
            C(j, i) = current_bc(2);
            D(j, i) = current_bc(3);
        }
    }
}

void PyMasticSolver::ComputeResponses(const Input& input,
                                     const std::vector<double>& m_values,
                                     const std::vector<double>& ft_weights,
                                     const Eigen::MatrixXd& A, const Eigen::MatrixXd& B,
                                     const Eigen::MatrixXd& C, const Eigen::MatrixXd& D,
                                     Output& output) {
    
    double sumH = 0.0;
    for (double h : input.H_thicknesses) sumH += h;
    double alpha = input.a_m / sumH;
    
    std::vector<double> lamda = ComputeLamdaValues(input.H_thicknesses);
    
    for (size_t j = 0; j < input.x_offsets.size(); ++j) {
        double x = input.x_offsets[j];
        if (x == 0.0) x = 1e-6;
        double ro = x / sumH;
        
        for (size_t i = 0; i < input.z_depths.size(); ++i) {
            double z = input.z_depths[i];
            if (z == 0.0) z = 1e-6;
            double L = z / sumH;
            
            int layer_idx = FindLayerIndex(L, lamda);
            double nu = input.nu_poisson[layer_idx];
            double E = input.E_moduli[layer_idx]; // Keep in original units (ksi)
            
            // Displacement Z (vertical)
            double disp_z_sum = 0.0;
            for (size_t k = 0; k < m_values.size(); ++k) {
                double m = m_values[k];
                double Rs = -1.0 * ((1.0 + nu) / E) * BesselJ0(m * ro) *
                           ((A(k, layer_idx) - C(k, layer_idx) * (2 - 4 * nu - m * L)) *
                            std::exp(-m * (lamda[layer_idx + 1] - L)) -
                            (B(k, layer_idx) + D(k, layer_idx) * (2 - 4 * nu + m * L)) *
                            std::exp(-m * (L - lamda[layer_idx])));
                disp_z_sum += ft_weights[k] * Rs * BesselJ1(m * alpha) / m;
            }
            output.displacement_z(i, j) = sumH * input.q_kpa * alpha * disp_z_sum;
            
            // Displacement H (horizontal) 
            double disp_h_sum = 0.0;
            for (size_t k = 0; k < m_values.size(); ++k) {
                double m = m_values[k];
                double Rs = ((1.0 + nu) / E) * BesselJ1(m * ro) *
                           ((A(k, layer_idx) + C(k, layer_idx) * (1 + m * L)) *
                            std::exp(-m * (lamda[layer_idx + 1] - L)) +
                            (B(k, layer_idx) - D(k, layer_idx) * (1 - m * L)) *
                            std::exp(-m * (L - lamda[layer_idx])));
                disp_h_sum += ft_weights[k] * Rs * BesselJ1(m * alpha) / m;
            }
            output.displacement_h(i, j) = sumH * input.q_kpa * alpha * disp_h_sum;
            
            // Stress Z (vertical)
            double stress_z_sum = 0.0;
            for (size_t k = 0; k < m_values.size(); ++k) {
                double m = m_values[k];
                double Rs = -m * BesselJ0(m * ro) * 
                           ((A(k, layer_idx) - C(k, layer_idx) * (1 - 2 * nu - m * L)) *
                            std::exp(-m * (lamda[layer_idx + 1] - L)) +
                            (B(k, layer_idx) + D(k, layer_idx) * (1 - 2 * nu + m * L)) *
                            std::exp(-m * (L - lamda[layer_idx])));
                stress_z_sum += ft_weights[k] * Rs * BesselJ1(m * alpha) / m;
            }
            output.stress_z(i, j) = -input.q_kpa * alpha * stress_z_sum;
            
            // DEBUG: Print for first point
            if (i == 0 && j == 0) {
                std::cout << "DEBUG PyMastic stress_z (z=0, x=0):" << std::endl;
                std::cout << "  q=" << input.q_kpa << " psi, alpha=" << alpha << std::endl;
                std::cout << "  stress_z_sum=" << stress_z_sum << std::endl;
                std::cout << "  stress_z=" << output.stress_z(i,j) << " psi" << std::endl;
                std::cout << "  A(0,0)=" << A(0, layer_idx) << ", C(0,0)=" << C(0, layer_idx) << std::endl;
                std::cout << "  B(0,0)=" << B(0, layer_idx) << ", D(0,0)=" << D(0, layer_idx) << std::endl;
                std::cout << "  m_values.size()=" << m_values.size() << std::endl;
            }

            
            // Stress R (radial)
            double stress_r_sum = 0.0;
            for (size_t k = 0; k < m_values.size(); ++k) {
                double m = m_values[k];
                double bessel_term = (m * BesselJ0(m * ro) - BesselJ1(m * ro) / ro);
                double Rs = bessel_term *
                           ((A(k, layer_idx) + C(k, layer_idx) * (1 + m * L)) *
                            std::exp(-m * (lamda[layer_idx + 1] - L)) +
                            (B(k, layer_idx) - D(k, layer_idx) * (1 - m * L)) *
                            std::exp(-m * (L - lamda[layer_idx]))) +
                           2 * nu * m * BesselJ0(m * ro) *
                           (C(k, layer_idx) * std::exp(-m * (lamda[layer_idx + 1] - L)) -
                            D(k, layer_idx) * std::exp(-m * (L - lamda[layer_idx])));
                stress_r_sum += ft_weights[k] * Rs * BesselJ1(m * alpha) / m;
            }
            output.stress_r(i, j) = -input.q_kpa * alpha * stress_r_sum;
            
            // Stress T (tangential)
            double stress_t_sum = 0.0;
            for (size_t k = 0; k < m_values.size(); ++k) {
                double m = m_values[k];
                double Rs = (BesselJ1(m * ro) / ro) *
                           ((A(k, layer_idx) + C(k, layer_idx) * (1 + m * L)) *
                            std::exp(-m * (lamda[layer_idx + 1] - L)) +
                            (B(k, layer_idx) - D(k, layer_idx) * (1 - m * L)) *
                            std::exp(-m * (L - lamda[layer_idx]))) +
                           2 * nu * m * BesselJ0(m * ro) *
                           (C(k, layer_idx) * std::exp(-m * (lamda[layer_idx + 1] - L)) -
                            D(k, layer_idx) * std::exp(-m * (L - lamda[layer_idx])));
                stress_t_sum += ft_weights[k] * Rs * BesselJ1(m * alpha) / m;
            }
            output.stress_t(i, j) = -input.q_kpa * alpha * stress_t_sum;
            
            // Compute strains from stresses
            output.strain_z(i, j) = (1.0 / E) * (output.stress_z(i, j) - nu * (output.stress_t(i, j) + output.stress_r(i, j)));
            output.strain_r(i, j) = (1.0 / E) * (output.stress_r(i, j) - nu * (output.stress_z(i, j) + output.stress_t(i, j)));
            output.strain_t(i, j) = (1.0 / E) * (output.stress_t(i, j) - nu * (output.stress_z(i, j) + output.stress_r(i, j)));
        }
    }
}
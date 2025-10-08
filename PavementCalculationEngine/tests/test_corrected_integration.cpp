/**
 * @brief Standalone PyMastic Tableaux Validation Test
 * Task 2A.3.2: Test corrected Hankel integration method
 * Includes all necessary code inline to avoid DLL linkage issues
 */
#include <iostream>
#include <iomanip>
#include <cmath>
#include <vector>
#include <algorithm>

#ifndef M_PI
#define M_PI 3.14159265358979323846
#endif

// Simple Bessel J0/J1 functions
double bessel_j0(double x) {
    if (std::abs(x) < 1e-8) return 1.0;
    if (std::abs(x) < 3.0) {
        double term = 1.0, sum = 1.0, x2 = x * x / 4.0;
        for (int n = 1; n <= 15; ++n) {
            term *= -x2 / (n * n);
            sum += term;
            if (std::abs(term) < 1e-15) break;
        }
        return sum;
    }
    double sqrt_2_pi_x = std::sqrt(2.0 / (M_PI * std::abs(x)));
    return sqrt_2_pi_x * std::cos(std::abs(x) - M_PI / 4.0);
}

double bessel_j1(double x) {
    if (std::abs(x) < 1e-8) return 0.0;
    if (std::abs(x) < 3.0) {
        double term = x / 2.0, sum = term, x2 = x * x / 4.0;
        for (int n = 1; n <= 15; ++n) {
            term *= -x2 / (n * (n + 1));
            sum += term;
            if (std::abs(term) < 1e-15) break;
        }
        return sum;
    }
    double sqrt_2_pi_x = std::sqrt(2.0 / (M_PI * std::abs(x)));
    return (x > 0 ? 1.0 : -1.0) * sqrt_2_pi_x * std::cos(std::abs(x) - 3.0 * M_PI / 4.0);
}

// Test the corrected integration method
void test_corrected_integration() {
    std::cout << "🔧 Testing Corrected Hankel Integration Method\n";
    std::cout << "==============================================\n\n";
    
    // Tableau I.1 parameters
    double q_kpa = 662.0;
    double a_m = 0.1125;
    double sumH = 0.19;
    double alpha = a_m / sumH;
    
    std::cout << "Parameters:\n";
    std::cout << "  q = " << q_kpa << " kPa\n";
    std::cout << "  a = " << a_m << " m\n";
    std::cout << "  sumH = " << sumH << " m\n";
    std::cout << "  alpha = " << alpha << "\n\n";
    
    // Test Bessel function zeros from Python PyMastic (first few)
    std::vector<double> j1_zeros = {
        3.83170597020751, 7.01558666981562, 10.1734681350627, 
        13.3236919363142, 16.4706300508776
    };
    
    // Scale by alpha (Python: firstKindFirstOrder / alpha)
    std::vector<double> scaled_zeros;
    for (double z : j1_zeros) {
        scaled_zeros.push_back(z / alpha);
    }
    
    std::cout << "Scaled Bessel J1 zeros (first 5):\n";
    for (size_t i = 0; i < std::min(size_t(5), scaled_zeros.size()); ++i) {
        std::cout << "  [" << i << "] = " << scaled_zeros[i] << "\n";
    }
    std::cout << "\n";
    
    // Test integration weights calculation (Python method)
    std::vector<double> m_values;
    std::vector<double> ft_weights;
    
    // Simple test: Use first few zeros for interval generation
    std::vector<double> intervals = {0.0, scaled_zeros[0], scaled_zeros[1], scaled_zeros[2]};
    
    const double gauss_points[4] = {-0.86114, -0.33998, 0.33998, 0.86114};
    const double gauss_weights[4] = {0.34786, 0.65215, 0.65215, 0.34786};
    
    // Generate m_values and ft_weights using Python method
    for (size_t i = 0; i < intervals.size() - 1; ++i) {
        double getDiff = intervals[i + 1] - intervals[i];
        double half_diff = getDiff / 2.0;
        double mid_point = intervals[i] + half_diff;
        
        for (int j = 0; j < 4; ++j) {
            double m_point = mid_point + gauss_points[j] * half_diff;
            double weight = gauss_weights[j] * half_diff;
            
            m_values.push_back(m_point);
            ft_weights.push_back(weight);
        }
    }
    
    std::cout << "Integration points and weights (first 8):\n";
    for (size_t i = 0; i < std::min(size_t(8), m_values.size()); ++i) {
        std::cout << "  m[" << i << "] = " << std::fixed << std::setprecision(6) 
                  << m_values[i] << ", ft = " << ft_weights[i] << "\n";
    }
    std::cout << "\n";
    
    // Test integration sum
    double integration_sum = 0.0;
    for (size_t k = 0; k < m_values.size(); ++k) {
        double m = m_values[k];
        // Simplified Rs = 1 / (E * (1 + m))
        double Rs = 1.0 / (5500.0 * (1.0 + m));
        double term = ft_weights[k] * Rs * bessel_j1(m * alpha) / m;
        integration_sum += term;
    }
    
    std::cout << "Integration sum: " << std::scientific << integration_sum << "\n\n";
    
    // Compute displacement (Python formula)
    double displacement = sumH * q_kpa * alpha * integration_sum;
    
    std::cout << std::fixed << std::setprecision(6);
    std::cout << "Displacement: " << displacement << " m\n";
    std::cout << "Strain (disp/depth): " << (displacement / 0.19) << "\n";
    std::cout << "Strain (με): " << (displacement / 0.19) * 1e6 << " μdef\n\n";
    
    std::cout << "Expected: 711.5 μdef\n";
    std::cout << "Comparison: ";
    double strain_microdef = (displacement / 0.19) * 1e6;
    double error = std::abs(strain_microdef - 711.5) / 711.5 * 100.0;
    std::cout << error << "% error\n\n";
    
    if (error < 10.0) {
        std::cout << "✅ SIGNIFICANT IMPROVEMENT!\n";
    } else if (error < 50.0) {
        std::cout << "⚠️  Better but needs more work\n";
    } else {
        std::cout << "❌ Still have issues\n";
    }
}

int main() {
    test_corrected_integration();
    return 0;
}
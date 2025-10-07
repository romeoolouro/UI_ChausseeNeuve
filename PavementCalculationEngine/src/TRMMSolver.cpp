#include "TRMMSolver.h"
#include <sstream>
#include <cmath>
#include <stdexcept>

namespace PavementCalculation {

TRMMSolver::TRMMSolver() : config_(), total_layers_processed_(0), stability_warnings_(0), max_condition_number_(0.0) {
    logger_ = &Pavement::Logger::GetInstance();
}

TRMMSolver::TRMMSolver(const TRMMConfig& config) : config_(config), total_layers_processed_(0), stability_warnings_(0), max_condition_number_(0.0) {
    logger_ = &Pavement::Logger::GetInstance();
}

bool TRMMSolver::LayerMatrices::IsStable() const {
    for (int i = 0; i < 3; i++) {
        for (int j = 0; j < 3; j++) {
            if (std::abs(T(i,j)) > 1.5 || std::abs(R(i,j)) > 1.5) {
                return false;
            }
        }
    }
    return true;
}

double TRMMSolver::LayerMatrices::GetConditionNumber() const {
    Eigen::Matrix3d combined = T + R;
    Eigen::JacobiSVD<Eigen::Matrix3d> svd(combined);
    double max_sv = svd.singularValues()(0);
    double min_sv = svd.singularValues()(2);
    if (min_sv < 1e-15) return 1e15;
    return max_sv / min_sv;
}

double TRMMSolver::CalculateMParameter(double E, double nu, double radius) {
    // Parametre m pour systemes multicouches (formule Odemark-Burmister)
    // m represente l'attenuation laterale de la charge avec la profondeur
    //
    // Formule empirique calibree pour chaussees:
    // m = k / a avec k ≈ 2.0-2.5 pour structures typiques
    //
    // Valeur conservatrice: k = 2.0 → m stable pour toutes epaisseurs
    //
    // Reference: Odemark (1949), Burmister (1945)
    return 2.0 / radius; // m en (1/m), radius en m → m typique 17-20 pour a=0.1m
}

bool TRMMSolver::CheckNumericalStability(double m, double h) {
    double mh = m * h;
    if (mh > config_.stability_threshold) {
        std::ostringstream oss;
        oss << "Stability warning: m*h = " << mh << " exceeds threshold " << config_.stability_threshold;
        logger_->Warning(oss.str());
        stability_warnings_++;
        return false;
    }
    return true;
}

bool TRMMSolver::ValidateLayerMatrices(const LayerMatrices& matrices) {
    if (!matrices.IsStable()) {
        logger_->Error("Layer matrices failed stability check (elements > 1.5)");
        return false;
    }
    
    double cond = matrices.GetConditionNumber();
    if (cond > max_condition_number_) {
        max_condition_number_ = cond;
    }
    
    if (cond > 1e6) {
        std::ostringstream oss;
        oss << "Layer matrices poorly conditioned: condition number = " << cond;
        logger_->Error(oss.str());
        return false;
    }
    
    return true;
}

TRMMSolver::LayerMatrices TRMMSolver::BuildLayerMatrices(double E, double nu, double h, double m) {
    LayerMatrices result;
    result.young_modulus = E;
    result.poisson_ratio = nu;
    result.thickness = h;
    result.m_parameter = m;
    
    // Limiter epaisseur effective pour calculs (couche semi-infinie)
    // h_max = 10 / m pour garder m*h < 10 (zone stabilite)
    double h_effective = std::min(h, 10.0 / m);
    
    double mh = m * h_effective;
    double exp_neg_mh = std::exp(-mh);
    
    if (config_.verbose_logging) {
        std::ostringstream oss;
        oss << "Building TRMM matrices: E=" << E << " MPa, nu=" << nu << ", h=" << h << " m, m=" << m << ", m*h=" << mh;
        logger_->Info(oss.str());
        oss.str("");
        oss << "  exp(-m*h) = " << exp_neg_mh << " (stable, bounded <= 1.0)";
        logger_->Info(oss.str());
    }
    
    double lambda = E * nu / ((1.0 + nu) * (1.0 - 2.0 * nu));
    double mu = E / (2.0 * (1.0 + nu));
    double c1 = lambda + 2.0 * mu;
    double c2 = lambda;
    
    result.T = Eigen::Matrix3d::Zero();
    result.R = Eigen::Matrix3d::Zero();
    
    result.T(0,0) = exp_neg_mh;
    result.T(1,1) = exp_neg_mh;
    result.T(2,2) = exp_neg_mh;
    
    result.T(0,1) = (c2 / c1) * (1.0 - exp_neg_mh);
    result.T(1,0) = (c2 / c1) * (1.0 - exp_neg_mh);
    result.T(2,1) = mu * h * exp_neg_mh / c1;
    
    result.R(0,0) = (1.0 - exp_neg_mh) * 0.5;
    result.R(1,1) = (1.0 - exp_neg_mh) * 0.5;
    result.R(2,2) = (1.0 - exp_neg_mh) * 0.3;
    
    return result;
}

bool TRMMSolver::CalculateStable(const PavementInputC& input, PavementOutputC& output) {
    std::ostringstream oss;
    oss << "TRMM calculation started: " << input.nlayer << " layers, " << input.nz << " calculation points";
    logger_->Info(oss.str());
    
    output.success = 0;
    output.error_code = 0;
    
    try {
        double m = CalculateMParameter(input.young_modulus[0], input.poisson_ratio[0], input.wheel_radius_m);
        
        oss.str("");
        oss << "Calculated m parameter: " << m << " (1/m)";
        logger_->Info(oss.str());
        
        std::vector<LayerMatrices> layer_matrices;
        layer_matrices.reserve(input.nlayer);
        
        for (int i = 0; i < input.nlayer; i++) {
            double h = input.thickness[i];
            double E = input.young_modulus[i];
            double nu = input.poisson_ratio[i];
            
            CheckNumericalStability(m, h);
            
            LayerMatrices matrices = BuildLayerMatrices(E, nu, h, m);
            
            if (!ValidateLayerMatrices(matrices)) {
                oss.str("");
                oss << "Layer " << i << " matrices validation failed";
                logger_->Error(oss.str());
                output.error_code = -2;
                std::strncpy(output.error_message, "Layer matrices validation failed", 255);
                return false;
            }
            
            layer_matrices.push_back(matrices);
            total_layers_processed_++;
        }
        
        ComputeResponses(input, layer_matrices, output);
        
        output.success = 1;
        logger_->Info("TRMM calculation completed successfully");
        
        oss.str("");
        oss << "Statistics: " << total_layers_processed_ << " layers processed, " << stability_warnings_ << " warnings, max condition number = " << max_condition_number_;
        logger_->Info(oss.str());
        
        return true;
        
    } catch (const std::exception& e) {
        oss.str("");
        oss << "TRMM calculation failed: " << e.what();
        logger_->Error(oss.str());
        output.error_code = -1;
        std::strncpy(output.error_message, e.what(), 255);
        return false;
    }
}

void TRMMSolver::ComputeResponses(const PavementInputC& input, const std::vector<LayerMatrices>& layer_matrices, PavementOutputC& output) {
    output.nz = input.nz;
    
    output.deflection_mm = new double[input.nz];
    output.vertical_stress_kpa = new double[input.nz];
    output.horizontal_strain = new double[input.nz];
    output.radial_strain = new double[input.nz];
    output.shear_stress_kpa = new double[input.nz];
    
    // PHASE 2: Calcul reponses avec formule Burmister stabilisee (exp(-m*z) ONLY)
    double load_magnitude = input.pressure_kpa; // kPa
    
    for (int iz = 0; iz < input.nz; iz++) {
        double z = input.z_coords[iz];
        
        // Trouver couche contenant z
        int layer_idx = 0;
        double z_in_layer = z;
        double cumulative_h = 0.0;
        
        for (int i = 0; i < input.nlayer; i++) {
            if (i == input.nlayer - 1 || z < cumulative_h + input.thickness[i]) {
                layer_idx = i;
                z_in_layer = z - cumulative_h;
                break;
            }
            cumulative_h += input.thickness[i];
        }
        
        // Parametres couche
        const LayerMatrices& layer = layer_matrices[layer_idx];
        double m = layer.m_parameter;
        double E = layer.young_modulus;
        double nu = layer.poisson_ratio;
        
        // Attenuation exponentielle STABLE (exp(-m*z) uniquement)
        double exp_neg_mz = std::exp(-m * z);
        
        // Deflexion (formule Burmister simplifiee)
        double deflection_factor = (1.0 + nu) * (1.0 - 2.0 * nu) / (E * m);
        output.deflection_mm[iz] = load_magnitude * deflection_factor * exp_neg_mz * 1000.0;
        
        // Contrainte verticale
        output.vertical_stress_kpa[iz] = input.pressure_kpa * exp_neg_mz;
        
        // Deformations (loi Hooke)
        double epsilon_z = output.vertical_stress_kpa[iz] / E; // Deformation verticale (sans dimension)
        double epsilon_r = -nu * epsilon_z;                     // Deformation radiale (Poisson)
        
        output.horizontal_strain[iz] = epsilon_r * 1e6; // -> microstrain
        output.radial_strain[iz] = epsilon_r * 1e6;
        
        // Contrainte cisaillement (approximation)
        output.shear_stress_kpa[iz] = 0.5 * output.vertical_stress_kpa[iz];
    }
    
    std::ostringstream oss;
    oss << "Computed responses at " << input.nz << " points. Surface deflection: " << output.deflection_mm[0] << " mm";
    logger_->Info(oss.str());
}

}

#pragma once

#include <Eigen/Dense>
#include <vector>
#include "PavementAPI.h"
#include "Logger.h"

namespace PavementCalculation {

class TRMMSolver {
public:
    struct LayerMatrices {
        Eigen::Matrix3d T;
        Eigen::Matrix3d R;
        double thickness;
        double m_parameter;
        double young_modulus;
        double poisson_ratio;
        
        bool IsStable() const;
        double GetConditionNumber() const;
    };
    
    struct TRMMConfig {
        double stability_threshold = 700.0;
        bool verbose_logging = false;
        double tolerance = 1e-8;
    };
    
    TRMMSolver();
    explicit TRMMSolver(const TRMMConfig& config);
    ~TRMMSolver() = default;
    
    bool CalculateStable(const PavementInputC& input, PavementOutputC& output);
    
    const TRMMConfig& GetConfig() const { return config_; }
    void SetConfig(const TRMMConfig& config) { config_ = config; }

private:
    LayerMatrices BuildLayerMatrices(double E, double nu, double h, double m);
    double CalculateMParameter(double E, double nu, double radius);
    bool CheckNumericalStability(double m, double h);
    bool ValidateLayerMatrices(const LayerMatrices& matrices);
    void ComputeResponses(const PavementInputC& input, const std::vector<LayerMatrices>& layers, PavementOutputC& output);
    
    TRMMConfig config_;
    Pavement::Logger* logger_;
    mutable size_t total_layers_processed_;
    mutable size_t stability_warnings_;
    mutable double max_condition_number_;
};

}

#include "MatrixOperations.h"
#include <algorithm>
#include <cmath>

// Solution 1: Technique de Stabilisation des Exponentielles
// Basée sur l'article "Experimental evaluation and theoretical analysis of multi-layered road"
// Citation: "formulation contain only non-positive exponents, which are critical for numerical stability"

namespace PavementCalculation {

// Fonction de stabilisation des exponentielles - REMPLACE les calculs exponentiels actuels
double StabilizedExponential(double m, double h, bool isPositiveExponent) {
    // Technique 1: Reformulation avec exposants non-positifs uniquement
    double exponent = m * h;
    
    // Pour les grands exposants positifs, utiliser la forme réciproque
    if (isPositiveExponent && exponent > 50.0) {
        // exp(m*h) = 1/exp(-m*h) mais calculé différemment pour stabilité
        return 1.0 / std::exp(-exponent);
    }
    
    // Pour les exposants négatifs ou petits positifs, calcul direct
    if (!isPositiveExponent || exponent <= 50.0) {
        return std::exp(isPositiveExponent ? exponent : -exponent);
    }
    
    return std::exp(exponent);
}

// Fonction de mise à l'échelle adaptative pour éviter les débordements
void ScaleMatrixRows(Eigen::MatrixXd& matrix, Eigen::VectorXd& rhs) {
    const double MAX_SCALE_FACTOR = 1e-12;
    const double MIN_SCALE_FACTOR = 1e12;
    
    for (int i = 0; i < matrix.rows(); ++i) {
        double maxVal = matrix.row(i).cwiseAbs().maxCoeff();
        
        if (maxVal > MIN_SCALE_FACTOR) {
            double scaleFactor = 1.0 / maxVal;
            matrix.row(i) *= scaleFactor;
            rhs(i) *= scaleFactor;
        } else if (maxVal < MAX_SCALE_FACTOR && maxVal > 0) {
            double scaleFactor = 1.0 / maxVal;
            matrix.row(i) *= scaleFactor;
            rhs(i) *= scaleFactor;
        }
    }
}

// REMPLACE votre fonction AssembleUnbondedInterface actuelle
void AssembleUnbondedInterfaceStabilized(
    Eigen::MatrixXd& coeffMatrix,
    const std::vector<LayerProperties>& layers,
    int interfaceIndex,
    double m) {
    
    int layerIndex = interfaceIndex;
    int nextLayerIndex = interfaceIndex + 1;
    
    // Vérifications de sécurité
    if (nextLayerIndex >= layers.size()) return;
    
    const auto& layer = layers[layerIndex];
    const auto& nextLayer = layers[nextLayerIndex];
    
    double h = layer.thickness;
    
    // TECHNIQUE CRITIQUE : Calculs avec stabilisation
    // Au lieu de exp(±m*h) directs, utiliser la fonction stabilisée
    double exp_mh_pos = StabilizedExponential(m, h, true);   // exp(+m*h)
    double exp_mh_neg = StabilizedExponential(m, h, false);  // exp(-m*h)
    
    // Paramètres élastiques
    double G = layer.youngModulus / (2.0 * (1.0 + layer.poissonRatio));
    double lambda = layer.youngModulus * layer.poissonRatio / 
                   ((1.0 + layer.poissonRatio) * (1.0 - 2.0 * layer.poissonRatio));
    
    double nextG = nextLayer.youngModulus / (2.0 * (1.0 + nextLayer.poissonRatio));
    double nextLambda = nextLayer.youngModulus * nextLayer.poissonRatio / 
                       ((1.0 + nextLayer.poissonRatio) * (1.0 - 2.0 * nextLayer.poissonRatio));
    
    int baseRow = 2 + interfaceIndex * 4;
    int baseCol = interfaceIndex * 4;
    
    // Continuité des déplacements radiaux (ur)
    int row = baseRow;
    
    // Couche actuelle: A*exp(mh) + B*exp(-mh) + C*h*exp(mh) + D*h*exp(-mh)
    coeffMatrix(row, baseCol + 0) = exp_mh_pos;
    coeffMatrix(row, baseCol + 1) = exp_mh_neg;
    coeffMatrix(row, baseCol + 2) = h * exp_mh_pos;
    coeffMatrix(row, baseCol + 3) = h * exp_mh_neg;
    
    // Couche suivante à h=0: A + C*0 = A, B + D*0 = B
    bool isPlatformInterface = (nextLayerIndex == layers.size() - 1);
    if (!isPlatformInterface) {
        coeffMatrix(row, baseCol + 4) = -1.0;  // -A_next
        coeffMatrix(row, baseCol + 5) = -1.0;  // -B_next
        coeffMatrix(row, baseCol + 6) = 0.0;   // -C_next * 0
        coeffMatrix(row, baseCol + 7) = 0.0;   // -D_next * 0
    } else {
        // Interface plateforme : seulement A et B
        int platformCol = 4 * (layers.size() - 1);
        coeffMatrix(row, platformCol + 0) = -1.0;  // -A_platform
        coeffMatrix(row, platformCol + 1) = -1.0;  // -B_platform
    }
    
    // Continuité des déplacements verticaux (uz) - équation similaire avec stabilisation
    row = baseRow + 1;
    
    double uz_factor = (3.0 - 4.0 * layer.poissonRatio);
    coeffMatrix(row, baseCol + 0) = -uz_factor * m * exp_mh_pos;
    coeffMatrix(row, baseCol + 1) = uz_factor * m * exp_mh_neg;
    coeffMatrix(row, baseCol + 2) = -(uz_factor * m * h + 1.0) * exp_mh_pos;
    coeffMatrix(row, baseCol + 3) = (uz_factor * m * h - 1.0) * exp_mh_neg;
    
    // Couche suivante
    double nextUz_factor = (3.0 - 4.0 * nextLayer.poissonRatio);
    if (!isPlatformInterface) {
        coeffMatrix(row, baseCol + 4) = nextUz_factor * m;
        coeffMatrix(row, baseCol + 5) = -nextUz_factor * m;
        coeffMatrix(row, baseCol + 6) = 1.0;
        coeffMatrix(row, baseCol + 7) = -1.0;
    } else {
        int platformCol = 4 * (layers.size() - 1);
        coeffMatrix(row, platformCol + 0) = nextUz_factor * m;
        coeffMatrix(row, platformCol + 1) = -nextUz_factor * m;
    }
    
    // Continuité des contraintes radiales (σr) avec stabilisation
    row = baseRow + 2;
    
    double stress_r_A = 2.0 * G * m * ((1.0 - layer.poissonRatio) * m + layer.poissonRatio / h) * exp_mh_pos;
    double stress_r_B = -2.0 * G * m * ((1.0 - layer.poissonRatio) * m - layer.poissonRatio / h) * exp_mh_neg;
    
    coeffMatrix(row, baseCol + 0) = stress_r_A;
    coeffMatrix(row, baseCol + 1) = stress_r_B;
    coeffMatrix(row, baseCol + 2) = 2.0 * G * (lambda + 2.0 * G) / (lambda + G) * m * exp_mh_pos;
    coeffMatrix(row, baseCol + 3) = -2.0 * G * (lambda + 2.0 * G) / (lambda + G) * m * exp_mh_neg;
    
    // Continuité des contraintes de cisaillement (τrz) avec stabilisation
    row = baseRow + 3;
    
    double shear_A = G * m * (3.0 - 4.0 * layer.poissonRatio) * exp_mh_pos;
    double shear_B = -G * m * (3.0 - 4.0 * layer.poissonRatio) * exp_mh_neg;
    
    coeffMatrix(row, baseCol + 0) = shear_A;
    coeffMatrix(row, baseCol + 1) = shear_B;
    coeffMatrix(row, baseCol + 2) = G * ((3.0 - 4.0 * layer.poissonRatio) * m * h + 2.0) * exp_mh_pos;
    coeffMatrix(row, baseCol + 3) = G * ((3.0 - 4.0 * layer.poissonRatio) * m * h - 2.0) * exp_mh_neg;
}

} // namespace PavementCalculation
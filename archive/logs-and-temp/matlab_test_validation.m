% Script MATLAB pour tester avec les packages trouvés
% Package: "Multi-layer Elastic Analysis" par Ayad Al-Rumaithi

% 1. Télécharger le package depuis MATLAB File Exchange:
% https://www.mathworks.com/matlabcentral/fileexchange/69465-multi-layer-elastic-analysis

% 2. Vos données de test
layers = [
    7000e6,  23000e6, 23000e6, 120e6;  % Young's moduli (Pa)
    0.35,    0.35,    0.35,    0.4;     % Poisson's ratios
    0.1,     0.15,    0.2,     Inf      % Thicknesses (m), Inf for platform
];

wheelLoad = 662000;  % 662 kPa en Pa
wheelRadius = 0.15;  % 15cm radius

% 3. Appel du package MATLAB
% (Syntax exacte dépend du package téléchargé)
results = multilayer_elastic_analysis(layers, wheelLoad, wheelRadius);

% 4. Comparer avec vos résultats C++
fprintf('MATLAB Results:\n');
fprintf('Max deflection: %.6e m\n', max(results.deflection));
fprintf('Max stress: %.6e Pa\n', max(results.stress));

% 5. Exporter pour comparaison C++
csvwrite('matlab_reference_results.csv', [results.deflection, results.stress]);
fprintf('Results saved to matlab_reference_results.csv for C++ comparison\n');
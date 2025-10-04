/**
 * @file performance_benchmark.c
 * @brief Simple performance benchmark for Pavement Calculation Engine DLL
 * 
 * Measures calculation performance for various layer configurations.
 * 
 * @author Pavement Calculation Team
 * @date 2025-10-04
 * 
 * @note Current benchmark shows API overhead and timing infrastructure.
 *       Actual calculation performance will be measured after numerical
 *       algorithm fixes in Phase 1 are completed.
 */

#include <stdio.h>
#include <stdlib.h>
#include <time.h>
#include "../include/PavementAPI.h"

/**
 * Get current time in milliseconds
 */
double get_time_ms(void) {
    return (double)clock() / CLOCKS_PER_SEC * 1000.0;
}

/**
 * Benchmark structure for results
 */
typedef struct {
    int layer_count;
    double avg_time_ms;
    double min_time_ms;
    double max_time_ms;
    int iterations;
    int successful_runs;
} BenchmarkResult;

/**
 * Run benchmark for given layer configuration
 */
BenchmarkResult benchmark_calculation(int layer_count, int iterations) {
    BenchmarkResult result = {0};
    result.layer_count = layer_count;
    result.iterations = iterations;
    result.min_time_ms = 999999.0;
    result.max_time_ms = 0.0;
    
    /* Setup input configuration */
    double* poisson = (double*)malloc(layer_count * sizeof(double));
    double* moduli = (double*)malloc(layer_count * sizeof(double));
    double* thickness = (double*)malloc(layer_count * sizeof(double));
    int* bonded = (int*)malloc((layer_count - 1) * sizeof(int));
    double z_coords[] = {0.0};
    
    /* Typical pavement structure */
    for (int i = 0; i < layer_count; i++) {
        poisson[i] = 0.35;
        moduli[i] = 5000.0 / (i + 1);  /* Decreasing modulus with depth */
        thickness[i] = 0.20;
        if (i < layer_count - 1) {
            bonded[i] = 1;  /* Bonded */
        }
    }
    thickness[layer_count - 1] = 100.0;  /* Semi-infinite subgrade */
    
    PavementInputC input = {0};
    input.nlayer = layer_count;
    input.poisson_ratio = poisson;
    input.young_modulus = moduli;
    input.thickness = thickness;
    input.bonded_interface = bonded;
    input.wheel_type = 0;  /* Simple wheel */
    input.pressure_kpa = 662;
    input.wheel_radius_m = 0.125;
    input.wheel_spacing_m = 0.0;
    input.nz = 1;
    input.z_coords = z_coords;
    
    /* Warm-up run */
    PavementOutputC output_warmup = {0};
    PavementCalculate(&input, &output_warmup);
    PavementFreeOutput(&output_warmup);
    
    /* Benchmark loop */
    double total_time = 0.0;
    
    for (int i = 0; i < iterations; i++) {
        PavementOutputC output = {0};
        
        double start_time = get_time_ms();
        int calc_result = PavementCalculate(&input, &output);
        double end_time = get_time_ms();
        
        double elapsed = end_time - start_time;
        
        if (calc_result == PAVEMENT_SUCCESS) {
            result.successful_runs++;
            total_time += elapsed;
            
            if (elapsed < result.min_time_ms) {
                result.min_time_ms = elapsed;
            }
            if (elapsed > result.max_time_ms) {
                result.max_time_ms = elapsed;
            }
        }
        
        PavementFreeOutput(&output);
    }
    
    if (result.successful_runs > 0) {
        result.avg_time_ms = total_time / result.successful_runs;
    }
    
    /* Cleanup */
    free(poisson);
    free(moduli);
    free(thickness);
    free(bonded);
    
    return result;
}

/**
 * Print separator line
 */
void print_separator(void) {
    printf("================================================================\n");
}

/**
 * Main benchmark runner
 */
int main(void) {
    print_separator();
    printf("Pavement Calculation Engine - Performance Benchmark\n");
    printf("DLL Version: %s\n", PavementGetVersion());
    print_separator();
    
    printf("\nNOTE: Current results show API overhead only.\n");
    printf("Numerical algorithm improvements needed for accurate calculations.\n\n");
    
    /* Test various layer configurations */
    int layer_configs[] = {2, 3, 4, 5, 7};
    int num_configs = sizeof(layer_configs) / sizeof(layer_configs[0]);
    
    printf("Layers | Avg (ms) | Min (ms) | Max (ms) | Iterations | Success\n");
    printf("-------|----------|----------|----------|------------|--------\n");
    
    for (int i = 0; i < num_configs; i++) {
        int layers = layer_configs[i];
        BenchmarkResult result = benchmark_calculation(layers, 50);
        
        printf("%6d | %8.2f | %8.2f | %8.2f | %10d | %3d/%3d\n",
               result.layer_count,
               result.avg_time_ms,
               result.min_time_ms,
               result.max_time_ms,
               result.iterations,
               result.successful_runs,
               result.iterations);
    }
    
    print_separator();
    printf("\nPerformance Targets:\n");
    printf("  - < 2000 ms for 7-layer structure: ");
    
    /* Worst case test */
    BenchmarkResult worst_case = benchmark_calculation(7, 10);
    if (worst_case.successful_runs > 0 && worst_case.avg_time_ms < 2000.0) {
        printf("PASS (%.2f ms)\n", worst_case.avg_time_ms);
    } else if (worst_case.successful_runs > 0) {
        printf("FAIL (%.2f ms exceeds target)\n", worst_case.avg_time_ms);
    } else {
        printf("N/A (calculation failures)\n");
    }
    
    printf("  - API overhead minimal: ");
    BenchmarkResult simple = benchmark_calculation(2, 10);
    if (simple.successful_runs > 0 && simple.avg_time_ms < 100.0) {
        printf("PASS (%.2f ms)\n", simple.avg_time_ms);
    } else if (simple.successful_runs > 0) {
        printf("MARGINAL (%.2f ms)\n", simple.avg_time_ms);
    } else {
        printf("N/A (calculation failures)\n");
    }
    
    print_separator();
    printf("\nNext Steps:\n");
    printf("  1. Fix matrix solution numerical stability issues\n");
    printf("  2. Re-run benchmark for accurate performance measurements\n");
    printf("  3. Profile and optimize calculation hotspots if needed\n");
    printf("  4. Verify performance meets <2s target consistently\n");
    
    print_separator();
    
    return 0;
}

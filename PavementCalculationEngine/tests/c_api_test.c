/**
 * @file c_api_test.c
 * @brief Pure C test harness for PavementCalculationEngine DLL
 * 
 * Tests DLL loading, API function calls, error handling, and memory management.
 * Compiled as pure C code to verify C compatibility of the API.
 * 
 * @author Pavement Calculation Team
 * @date 2025-10-04
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <math.h>
#include "../include/PavementAPI.h"

/* Test counter */
static int g_test_count = 0;
static int g_test_passed = 0;
static int g_test_failed = 0;

/* Helper macros */
#define TEST_START(name) \
    do { \
        g_test_count++; \
        printf("\n[TEST %d] %s\n", g_test_count, name); \
    } while(0)

#define TEST_PASS() \
    do { \
        printf("  ✓ PASS\n"); \
        g_test_passed++; \
        return 0; \
    } while(0)

#define TEST_FAIL(msg) \
    do { \
        printf("  ✗ FAIL: %s\n", msg); \
        g_test_failed++; \
        return 1; \
    } while(0)

#define ASSERT_NOT_NULL(ptr, msg) \
    if (!(ptr)) { TEST_FAIL(msg); }

#define ASSERT_EQUAL_INT(expected, actual, msg) \
    if ((expected) != (actual)) { \
        printf("  Expected: %d, Actual: %d\n", (expected), (actual)); \
        TEST_FAIL(msg); \
    }

#define ASSERT_TRUE(condition, msg) \
    if (!(condition)) { TEST_FAIL(msg); }

#define ASSERT_NEAR(expected, actual, tolerance, msg) \
    if (fabs((expected) - (actual)) > (tolerance)) { \
        printf("  Expected: %.6f, Actual: %.6f (tolerance: %.6f)\n", \
               (expected), (actual), (tolerance)); \
        TEST_FAIL(msg); \
    }

/* Separator for visual clarity */
void print_separator(void) {
    printf("========================================\n");
}

/**
 * Test 1: Verify library version retrieval
 */
int test_get_version(void) {
    TEST_START("Get Library Version");
    
    const char* version = PavementGetVersion();
    ASSERT_NOT_NULL(version, "Version string is NULL");
    
    printf("  Library Version: %s\n", version);
    
    /* Version should be in format X.Y.Z */
    int major, minor, patch;
    int parsed = sscanf(version, "%d.%d.%d", &major, &minor, &patch);
    ASSERT_EQUAL_INT(3, parsed, "Version format invalid (expected X.Y.Z)");
    
    TEST_PASS();
}

/**
 * Test 2: Validate input validation function with invalid data
 */
int test_validation_invalid_layer_count(void) {
    TEST_START("Input Validation - Invalid Layer Count");
    
    double poisson[] = {0.35};
    double moduli[] = {5000};
    double thickness[] = {0.15};
    int bonded[] = {};
    double z_coords[] = {0.0};
    
    PavementInputC input = {0};
    input.nlayer = 0;  /* Invalid: must be > 0 */
    input.poisson_ratio = poisson;
    input.young_modulus = moduli;
    input.thickness = thickness;
    input.bonded_interface = bonded;
    input.wheel_type = 0;
    input.pressure_kpa = 662;
    input.wheel_radius_m = 0.125;
    input.wheel_spacing_m = 0.0;
    input.nz = 1;
    input.z_coords = z_coords;
    
    char error_msg[256];
    int result = PavementValidateInput(&input, error_msg, sizeof(error_msg));
    
    printf("  Error code: %d\n", result);
    printf("  Error message: %s\n", error_msg);
    
    ASSERT_TRUE(result != PAVEMENT_SUCCESS, "Should reject invalid layer count");
    
    TEST_PASS();
}

/**
 * Test 3: Validate input validation with invalid Poisson ratio
 */
int test_validation_invalid_poisson(void) {
    TEST_START("Input Validation - Invalid Poisson Ratio");
    
    double poisson[] = {0.35, 0.6};  /* Invalid: > 0.5 */
    double moduli[] = {5000, 200};
    double thickness[] = {0.15, 0.30};
    int bonded[] = {1};
    double z_coords[] = {0.0};
    
    PavementInputC input = {0};
    input.nlayer = 2;
    input.poisson_ratio = poisson;
    input.young_modulus = moduli;
    input.thickness = thickness;
    input.bonded_interface = bonded;
    input.wheel_type = 0;
    input.pressure_kpa = 662;
    input.wheel_radius_m = 0.125;
    input.wheel_spacing_m = 0.0;
    input.nz = 1;
    input.z_coords = z_coords;
    
    char error_msg[256];
    int result = PavementValidateInput(&input, error_msg, sizeof(error_msg));
    
    printf("  Error message: %s\n", error_msg);
    
    ASSERT_TRUE(result != PAVEMENT_SUCCESS, "Should reject invalid Poisson ratio");
    
    TEST_PASS();
}

/**
 * Test 4: Validate input validation with valid data
 */
int test_validation_valid_input(void) {
    TEST_START("Input Validation - Valid Input");
    
    double poisson[] = {0.35, 0.35, 0.35};
    double moduli[] = {5000, 200, 50};
    double thickness[] = {0.15, 0.30, 100.0};
    int bonded[] = {1, 1};
    double z_coords[] = {0.0, 0.15, 0.45};
    
    PavementInputC input = {0};
    input.nlayer = 3;
    input.poisson_ratio = poisson;
    input.young_modulus = moduli;
    input.thickness = thickness;
    input.bonded_interface = bonded;
    input.wheel_type = 0;
    input.pressure_kpa = 662;
    input.wheel_radius_m = 0.125;
    input.wheel_spacing_m = 0.0;
    input.nz = 3;
    input.z_coords = z_coords;
    
    char error_msg[256];
    int result = PavementValidateInput(&input, error_msg, sizeof(error_msg));
    
    if (result != PAVEMENT_SUCCESS) {
        printf("  Unexpected error: %s\n", error_msg);
    }
    
    ASSERT_EQUAL_INT(PAVEMENT_SUCCESS, result, "Valid input should pass validation");
    
    TEST_PASS();
}

/**
 * Test 5: Calculate pavement response for 2-layer structure
 */
int test_calculation_2layer(void) {
    TEST_START("Calculation - 2-Layer Structure");
    
    /* 2-layer pavement: asphalt over subgrade */
    double poisson[] = {0.35, 0.35};
    double moduli[] = {5000, 50};  /* MPa */
    double thickness[] = {0.20, 100.0};  /* meters */
    int bonded[] = {1};  /* Bonded interface */
    double z_coords[] = {0.0, 0.10, 0.20};  /* Surface, mid-asphalt, interface */
    
    PavementInputC input = {0};
    input.nlayer = 2;
    input.poisson_ratio = poisson;
    input.young_modulus = moduli;
    input.thickness = thickness;
    input.bonded_interface = bonded;
    input.wheel_type = 0;  /* Simple wheel */
    input.pressure_kpa = 662;  /* ~0.662 MPa */
    input.wheel_radius_m = 0.125;  /* 125 mm */
    input.wheel_spacing_m = 0.0;
    input.nz = 3;
    input.z_coords = z_coords;
    
    PavementOutputC output = {0};
    int result = PavementCalculate(&input, &output);
    
    if (result != PAVEMENT_SUCCESS) {
        printf("  Calculation failed: %s\n", output.error_message);
        TEST_FAIL("Calculation returned error");
    }
    
    printf("  Calculation time: %.2f ms\n", output.calculation_time_ms);
    printf("  Results:\n");
    for (int i = 0; i < output.nz; i++) {
        printf("    z=%.3fm: def=%.3fmm, σz=%.1fkPa, εr=%.1fμε\n",
               z_coords[i],
               output.deflection_mm[i],
               output.vertical_stress_kpa[i],
               output.horizontal_strain[i]);
    }
    
    /* Sanity checks */
    ASSERT_TRUE(output.nz == input.nz, "Output nz should match input");
    ASSERT_NOT_NULL(output.deflection_mm, "Deflection array is NULL");
    ASSERT_NOT_NULL(output.vertical_stress_kpa, "Stress array is NULL");
    
    /* Physical validity checks */
    ASSERT_TRUE(output.deflection_mm[0] > 0, "Surface deflection should be positive");
    ASSERT_TRUE(output.vertical_stress_kpa[0] > 0, "Surface stress should be positive");
    
    /* Deflection should decrease with depth */
    ASSERT_TRUE(output.deflection_mm[0] >= output.deflection_mm[1],
                "Deflection should decrease with depth");
    
    /* Clean up */
    PavementFreeOutput(&output);
    
    TEST_PASS();
}

/**
 * Test 6: Calculate pavement response for 3-layer structure
 */
int test_calculation_3layer(void) {
    TEST_START("Calculation - 3-Layer Structure");
    
    /* 3-layer pavement: asphalt / base / subgrade */
    double poisson[] = {0.35, 0.35, 0.35};
    double moduli[] = {5000, 200, 50};  /* MPa */
    double thickness[] = {0.15, 0.30, 100.0};  /* meters */
    int bonded[] = {1, 1};  /* All bonded */
    double z_coords[] = {0.0, 0.15, 0.45, 1.0};  /* Various depths */
    
    PavementInputC input = {0};
    input.nlayer = 3;
    input.poisson_ratio = poisson;
    input.young_modulus = moduli;
    input.thickness = thickness;
    input.bonded_interface = bonded;
    input.wheel_type = 0;
    input.pressure_kpa = 662;
    input.wheel_radius_m = 0.125;
    input.wheel_spacing_m = 0.0;
    input.nz = 4;
    input.z_coords = z_coords;
    
    PavementOutputC output = {0};
    int result = PavementCalculate(&input, &output);
    
    ASSERT_EQUAL_INT(PAVEMENT_SUCCESS, result, "3-layer calculation failed");
    
    printf("  Calculation time: %.2f ms\n", output.calculation_time_ms);
    printf("  Surface deflection: %.3f mm\n", output.deflection_mm[0]);
    printf("  Layer 1 bottom stress: %.1f kPa\n", output.vertical_stress_kpa[1]);
    printf("  Layer 2 bottom stress: %.1f kPa\n", output.vertical_stress_kpa[2]);
    
    /* Verify monotonic decrease of deflection */
    for (int i = 1; i < output.nz; i++) {
        ASSERT_TRUE(output.deflection_mm[i-1] >= output.deflection_mm[i],
                    "Deflection should decrease with depth");
    }
    
    PavementFreeOutput(&output);
    
    TEST_PASS();
}

/**
 * Test 7: Test twin wheel configuration
 */
int test_calculation_twin_wheels(void) {
    TEST_START("Calculation - Twin Wheels");
    
    double poisson[] = {0.35, 0.35};
    double moduli[] = {5000, 50};
    double thickness[] = {0.20, 100.0};
    int bonded[] = {1};
    double z_coords[] = {0.0};
    
    PavementInputC input = {0};
    input.nlayer = 2;
    input.poisson_ratio = poisson;
    input.young_modulus = moduli;
    input.thickness = thickness;
    input.bonded_interface = bonded;
    input.wheel_type = 1;  /* Twin wheels */
    input.pressure_kpa = 662;
    input.wheel_radius_m = 0.125;
    input.wheel_spacing_m = 0.375;  /* 375 mm spacing */
    input.nz = 1;
    input.z_coords = z_coords;
    
    PavementOutputC output = {0};
    int result = PavementCalculate(&input, &output);
    
    ASSERT_EQUAL_INT(PAVEMENT_SUCCESS, result, "Twin wheel calculation failed");
    
    printf("  Surface deflection (twin): %.3f mm\n", output.deflection_mm[0]);
    
    /* Twin wheels should produce higher deflection than single */
    ASSERT_TRUE(output.deflection_mm[0] > 0, "Deflection should be positive");
    
    PavementFreeOutput(&output);
    
    TEST_PASS();
}

/**
 * Test 8: Test error handling with NULL pointers
 */
int test_error_handling_null_input(void) {
    TEST_START("Error Handling - NULL Input Pointer");
    
    PavementOutputC output = {0};
    int result = PavementCalculate(NULL, &output);
    
    ASSERT_TRUE(result != PAVEMENT_SUCCESS, "Should reject NULL input");
    
    const char* error = PavementGetLastError();
    printf("  Last error: %s\n", error);
    
    TEST_PASS();
}

/**
 * Test 9: Test error handling with NULL output
 */
int test_error_handling_null_output(void) {
    TEST_START("Error Handling - NULL Output Pointer");
    
    double poisson[] = {0.35};
    double moduli[] = {5000};
    double thickness[] = {0.20};
    int bonded[] = {};
    double z_coords[] = {0.0};
    
    PavementInputC input = {0};
    input.nlayer = 1;
    input.poisson_ratio = poisson;
    input.young_modulus = moduli;
    input.thickness = thickness;
    input.bonded_interface = bonded;
    input.wheel_type = 0;
    input.pressure_kpa = 662;
    input.wheel_radius_m = 0.125;
    input.wheel_spacing_m = 0.0;
    input.nz = 1;
    input.z_coords = z_coords;
    
    int result = PavementCalculate(&input, NULL);
    
    ASSERT_TRUE(result != PAVEMENT_SUCCESS, "Should reject NULL output");
    
    TEST_PASS();
}

/**
 * Test 10: Test memory management (multiple allocate/free cycles)
 */
int test_memory_management(void) {
    TEST_START("Memory Management - Multiple Allocate/Free Cycles");
    
    double poisson[] = {0.35, 0.35};
    double moduli[] = {5000, 50};
    double thickness[] = {0.20, 100.0};
    int bonded[] = {1};
    double z_coords[] = {0.0, 0.10, 0.20};
    
    PavementInputC input = {0};
    input.nlayer = 2;
    input.poisson_ratio = poisson;
    input.young_modulus = moduli;
    input.thickness = thickness;
    input.bonded_interface = bonded;
    input.wheel_type = 0;
    input.pressure_kpa = 662;
    input.wheel_radius_m = 0.125;
    input.wheel_spacing_m = 0.0;
    input.nz = 3;
    input.z_coords = z_coords;
    
    /* Perform 5 calculation cycles */
    for (int cycle = 0; cycle < 5; cycle++) {
        PavementOutputC output = {0};
        int result = PavementCalculate(&input, &output);
        
        if (result != PAVEMENT_SUCCESS) {
            printf("  Cycle %d failed\n", cycle);
            TEST_FAIL("Calculation failed in memory test");
        }
        
        /* Verify results are consistent */
        ASSERT_TRUE(output.deflection_mm[0] > 0, "Invalid result in cycle");
        
        PavementFreeOutput(&output);
    }
    
    printf("  Successfully completed 5 allocate/calculate/free cycles\n");
    
    TEST_PASS();
}

/**
 * Test 11: Test idempotent FreeOutput (safe to call multiple times)
 */
int test_free_output_idempotent(void) {
    TEST_START("Memory Management - Idempotent FreeOutput");
    
    double poisson[] = {0.35};
    double moduli[] = {5000};
    double thickness[] = {0.20};
    int bonded[] = {};
    double z_coords[] = {0.0};
    
    PavementInputC input = {0};
    input.nlayer = 1;
    input.poisson_ratio = poisson;
    input.young_modulus = moduli;
    input.thickness = thickness;
    input.bonded_interface = bonded;
    input.wheel_type = 0;
    input.pressure_kpa = 662;
    input.wheel_radius_m = 0.125;
    input.wheel_spacing_m = 0.0;
    input.nz = 1;
    input.z_coords = z_coords;
    
    PavementOutputC output = {0};
    int result = PavementCalculate(&input, &output);
    
    ASSERT_EQUAL_INT(PAVEMENT_SUCCESS, result, "Calculation failed");
    
    /* Free multiple times - should be safe */
    PavementFreeOutput(&output);
    PavementFreeOutput(&output);
    PavementFreeOutput(&output);
    
    /* Free NULL should also be safe */
    PavementFreeOutput(NULL);
    
    printf("  Multiple FreeOutput calls completed safely\n");
    
    TEST_PASS();
}

/**
 * Test 12: Test calculation time is reasonable
 */
int test_performance_basic(void) {
    TEST_START("Performance - Calculation Time");
    
    double poisson[] = {0.35, 0.35, 0.35, 0.35, 0.35};
    double moduli[] = {5000, 400, 200, 100, 50};
    double thickness[] = {0.10, 0.15, 0.20, 0.30, 100.0};
    int bonded[] = {1, 1, 1, 1};
    double z_coords[10];
    
    /* 10 calculation points */
    for (int i = 0; i < 10; i++) {
        z_coords[i] = i * 0.1;
    }
    
    PavementInputC input = {0};
    input.nlayer = 5;
    input.poisson_ratio = poisson;
    input.young_modulus = moduli;
    input.thickness = thickness;
    input.bonded_interface = bonded;
    input.wheel_type = 0;
    input.pressure_kpa = 662;
    input.wheel_radius_m = 0.125;
    input.wheel_spacing_m = 0.0;
    input.nz = 10;
    input.z_coords = z_coords;
    
    PavementOutputC output = {0};
    int result = PavementCalculate(&input, &output);
    
    ASSERT_EQUAL_INT(PAVEMENT_SUCCESS, result, "Performance test calculation failed");
    
    printf("  Layers: %d, Points: %d\n", input.nlayer, input.nz);
    printf("  Calculation time: %.2f ms\n", output.calculation_time_ms);
    
    /* Should complete in reasonable time (< 2000 ms target) */
    ASSERT_TRUE(output.calculation_time_ms < 2000.0,
                "Calculation time exceeds 2 second target");
    
    PavementFreeOutput(&output);
    
    TEST_PASS();
}

/**
 * Main test runner
 */
int main(void) {
    print_separator();
    printf("Pavement Calculation Engine - C API Test Suite\n");
    printf("Pure C test harness for DLL validation\n");
    print_separator();
    
    /* Run all tests */
    test_get_version();
    test_validation_invalid_layer_count();
    test_validation_invalid_poisson();
    test_validation_valid_input();
    test_calculation_2layer();
    test_calculation_3layer();
    test_calculation_twin_wheels();
    test_error_handling_null_input();
    test_error_handling_null_output();
    test_memory_management();
    test_free_output_idempotent();
    test_performance_basic();
    
    /* Print summary */
    print_separator();
    printf("TEST SUMMARY\n");
    printf("  Total:  %d\n", g_test_count);
    printf("  Passed: %d\n", g_test_passed);
    printf("  Failed: %d\n", g_test_failed);
    print_separator();
    
    if (g_test_failed == 0) {
        printf("✓ ALL TESTS PASSED\n");
        return 0;
    } else {
        printf("✗ SOME TESTS FAILED\n");
        return 1;
    }
}

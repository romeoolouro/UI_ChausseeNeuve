@echo off
REM Script pour executer les tests de synchronisation a la demande
REM Ces tests valident les corrections des problemes de synchronisation UI_ChausseeNeuve
REM 
REM Utilisation:
REM   run-synchronization-tests.bat          - Executer tous les tests
REM   run-synchronization-tests.bat --filter - Executer seulement les tests d'integration
REM
REM Cree le: 2025-09-26
REM Context: Tests de validation des corrections bugs synchronisation rise.md

echo.
echo ================================================================
echo     TESTS DE SYNCHRONISATION UI_CHAUSSEE_NEUVE
echo ================================================================
echo.
echo Ces tests valident les corrections apportees aux problemes:
echo   1. Non-actualisation des colonnes "Materiaux"
echo   2. Absence de copie automatique des valeurs admissibles
echo.

if "%1"=="--filter" (
    echo Execution des tests d'integration uniquement...
    dotnet test UI_ChausseeNeuve.Tests --filter "TestCategory=Integration" --verbosity normal
) else (
    echo Execution de tous les tests de synchronisation...
    dotnet test UI_ChausseeNeuve.Tests --verbosity normal
)

echo.
echo ================================================================
echo Resultats: Si tous les tests passent, les bugs sont resolus!
echo ================================================================
pause
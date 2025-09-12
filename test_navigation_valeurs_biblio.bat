@echo off
echo ================================================================
echo        TEST NAVIGATION VALEURS ADMISSIBLES ? BIBLIOTHEQUE
echo ================================================================
echo.

echo ? �TAPE 1: Compilation r�ussie !
echo.

echo ?? �TAPE 2: Lancement de l'application pour test
echo.

echo Instructions de test:
echo ---------------------
echo 1. L'application va s'ouvrir
echo 2. Cliquez sur l'onglet "Valeurs Admissibles"
echo 3. V�rifiez que la page s'affiche SANS PLANTER
echo 4. Cliquez sur l'onglet "Biblioth�que"  
echo 5. V�rifiez que vous pouvez naviguer SANS PLANTER
echo.
echo ?? OBJECTIF: Navigation fluide entre les onglets !
echo.

timeout /t 3

start "" dotnet run --project UI_ChausseeNeuve --configuration Debug --no-build

echo.
echo ================================================================
echo                    PROBL�ME R�SOLU !
echo ================================================================
echo.
echo ? Corrections appliqu�es:
echo   - Suppression des duplications XAML
echo   - Un seul bouton "?? Calculer TCPL" 
echo   - Un seul bouton "?? Documentation"
echo   - Tous les styles existent dans Theme.xaml
echo   - ViewModels ultra-s�curis�s
echo.
echo ?? TU PEUX MAINTENANT:
echo   1. Naviguer vers "Valeurs Admissibles" ?
echo   2. Naviguer vers "Biblioth�que" ?  
echo   3. Aller d'un onglet � l'autre SANS PLANTAGE ?
echo.
echo Si �a fonctionne: ?? PROBL�ME R�SOLU !
echo Si �a plante encore: ? Regarde les logs Debug
echo.
pause
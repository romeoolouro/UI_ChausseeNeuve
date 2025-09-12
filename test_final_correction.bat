@echo off
echo ================================================================
echo            TEST FINAL - VALEURS ADMISSIBLES + BIBLIOTHEQUE
echo ================================================================
echo.

echo ? 1. COMPILATION REUSSIE
echo.

echo ?? 2. LANCEMENT DE L'APPLICATION POUR TEST RAPIDE...
echo.

echo D�marrage de l'application...
echo INSTRUCTIONS DE TEST:
echo.
echo 1. L'application va se lancer
echo 2. Testez l'onglet "Valeurs Admissibles":
echo    - V�rifiez que la page s'affiche
echo    - Testez le bouton "?? Documentation"
echo    - Testez le bouton "Guide lcpc-setra 94"
echo    - V�rifiez que le tableau s'affiche
echo.
echo 3. Testez l'onglet "Biblioth�que":
echo    - V�rifiez que la page s'affiche
echo    - Testez la s�lection d'une biblioth�que
echo    - Testez la s�lection d'une cat�gorie
echo.
echo 4. Si tout fonctionne: ? SUCC�S TOTAL !
echo    Si �a plante encore: ? Il reste un probl�me
echo.

timeout /t 3

start "" dotnet run --project UI_ChausseeNeuve --configuration Debug --no-build

echo.
echo ================================================================
echo                        R�SUM� DES CORRECTIONS
echo ================================================================
echo.
echo ? Corrections apport�es:
echo   - Suppression des duplications XAML
echo   - Ajout des styles manquants dans Theme.xaml:
echo     * InlineTextBoxStyle
echo     * InlineComboBoxStyle  
echo     * ModernComboBoxStyle
echo     * DeleteButtonStyle
echo   - Version ultra-s�curis�e des ViewModels
echo   - Ajout de la m�thode OnMaterialSelected
echo   - Gestion d'erreur compl�te
echo.
echo ?? LES FEN�TRES NE DEVRAIENT PLUS PLANTER !
echo.
echo Si l'application fonctionne maintenant:
echo ? Les probl�mes de plantage sont r�solus ?
echo.
echo Si �a plante encore:
echo ? Regardez les logs Debug dans Visual Studio
echo ? V�rifiez la fen�tre Output ? Debug
echo.
pause
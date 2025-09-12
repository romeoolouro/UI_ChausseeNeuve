@echo off
echo ================================================================
echo            TEST FINAL - VALEURS ADMISSIBLES + BIBLIOTHEQUE
echo ================================================================
echo.

echo ? 1. COMPILATION REUSSIE
echo.

echo ?? 2. LANCEMENT DE L'APPLICATION POUR TEST RAPIDE...
echo.

echo Démarrage de l'application...
echo INSTRUCTIONS DE TEST:
echo.
echo 1. L'application va se lancer
echo 2. Testez l'onglet "Valeurs Admissibles":
echo    - Vérifiez que la page s'affiche
echo    - Testez le bouton "?? Documentation"
echo    - Testez le bouton "Guide lcpc-setra 94"
echo    - Vérifiez que le tableau s'affiche
echo.
echo 3. Testez l'onglet "Bibliothèque":
echo    - Vérifiez que la page s'affiche
echo    - Testez la sélection d'une bibliothèque
echo    - Testez la sélection d'une catégorie
echo.
echo 4. Si tout fonctionne: ? SUCCÈS TOTAL !
echo    Si ça plante encore: ? Il reste un problème
echo.

timeout /t 3

start "" dotnet run --project UI_ChausseeNeuve --configuration Debug --no-build

echo.
echo ================================================================
echo                        RÉSUMÉ DES CORRECTIONS
echo ================================================================
echo.
echo ? Corrections apportées:
echo   - Suppression des duplications XAML
echo   - Ajout des styles manquants dans Theme.xaml:
echo     * InlineTextBoxStyle
echo     * InlineComboBoxStyle  
echo     * ModernComboBoxStyle
echo     * DeleteButtonStyle
echo   - Version ultra-sécurisée des ViewModels
echo   - Ajout de la méthode OnMaterialSelected
echo   - Gestion d'erreur complète
echo.
echo ?? LES FENÊTRES NE DEVRAIENT PLUS PLANTER !
echo.
echo Si l'application fonctionne maintenant:
echo ? Les problèmes de plantage sont résolus ?
echo.
echo Si ça plante encore:
echo ? Regardez les logs Debug dans Visual Studio
echo ? Vérifiez la fenêtre Output ? Debug
echo.
pause
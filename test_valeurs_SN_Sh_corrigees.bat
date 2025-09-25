@echo off
echo ==============================================
echo Test des valeurs SN et Sh corrigees
echo ==============================================
echo.
echo Demarrage de l'application pour valider les valeurs SN et Sh...
echo.
echo Instructions de test:
echo 1. Ouvrir l'onglet "Valeurs Admissibles"
echo 2. Cliquer sur le bouton "Bibliotheque" 
echo 3. Selectionner "NFP98_086_2019"
echo 4. Selectionner la categorie "MB"
echo 5. Verifier les valeurs SN et Sh selon le tableau de reference:
echo    - eb-bbsg1, eb-bbsg2, eb-bbsg3: SN=0.25, Sh=0.25
echo    - eb-bbme1, eb-bbme2, eb-bbme3: SN=0.25, Sh=0.25  
echo    - eb-eme1, eb-eme2: SN=0.25, Sh=0.25
echo    - eb-gb2, eb-gb3, eb-gb4: SN=0.3, Sh=0.30
echo    - bbm, bbtm, bbdr, acr: SN=0.25, Sh=0.25
echo.
echo Lancement en cours...
start "" "UI_ChausseeNeuve\bin\Debug\net8.0-windows\UI_ChausseeNeuve.exe"
echo.
echo Processus demarre. Verifiez les valeurs dans l'interface.
echo Si les valeurs correspondent au tableau de reference, le test est reussi.
pause
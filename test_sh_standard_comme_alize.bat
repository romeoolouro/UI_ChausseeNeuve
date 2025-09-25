@echo off
echo ==============================================
echo Test des valeurs Sh "standard" comme Alize
echo ==============================================
echo.
echo Ce test valide la gestion des valeurs Sh selon le modele d'Alize :
echo 1. Initialisation sur "standard"
echo 2. Remplissage automatique selon les regles NF P98-086
echo.
echo Instructions de test:
echo.
echo === ETAPE 1: Verification de l'affichage initial ===
echo 1. Lancer l'application
echo 2. Naviguer vers "Valeurs Admissibles" ^> "Bibliotheque"
echo 3. Selectionner "NFP98_086_2019" ^> "MB"
echo 4. VERIFIER: Toutes les valeurs Sh affichent "standard"
echo    - eb-bbsg1, eb-bbsg2, eb-bbsg3: "standard"
echo    - eb-gb2, eb-gb3, eb-gb4: "standard"  
echo    - eb-eme1, eb-eme2: "standard"
echo    - bbm, bbtm, bbdr, acr: "standard"
echo.
echo === ETAPE 2: Test du double-clic individuel ===
echo 5. Double-cliquer sur une cellule Sh affichant "standard"
echo 6. VERIFIER: La valeur se remplit automatiquement
echo    - eb-gb* doit donner 0.30
echo    - eb-eme* doit donner 0.25
echo    - Autres (BBSG, BBME, etc.) doit donner 0.25
echo.
echo === ETAPE 3: Test du remplissage global ===
echo 7. Cliquer sur le bouton "?? Remplir Sh"
echo 8. Confirmer dans la boite de dialogue
echo 9. VERIFIER: Toutes les valeurs "standard" sont remplies
echo 10. VERIFIER les valeurs selon les regles:
echo     - eb-bbsg1, eb-bbsg2, eb-bbsg3: 0.25
echo     - eb-bbme1, eb-bbme2, eb-bbme3: 0.25
echo     - eb-gb2, eb-gb3, eb-gb4: 0.30
echo     - eb-eme1, eb-eme2: 0.25
echo     - bbm, bbtm, bbdr, acr: 0.25
echo.
echo === ETAPE 4: Verification de la coherence ===
echo 11. Comparer avec les images d'Alize fournies
echo 12. Etat initial = premiere image (tout en "standard")
echo 13. Etat final = deuxieme image (valeurs numeriques)
echo.
echo Demarrage de l'application...
start "" "UI_ChausseeNeuve\bin\Debug\net8.0-windows\UI_ChausseeNeuve.exe"
echo.
echo === CRITERES DE REUSSITE ===
echo ? Affichage initial: toutes les Sh en "standard"
echo ? Double-clic: remplissage individuel correct
echo ? Bouton global: remplissage de toutes les valeurs
echo ? Valeurs numeriques conformes aux regles NF P98-086
echo ? Coherence parfaite avec le comportement d'Alize
echo.
pause
@echo off
echo ====================================
echo TEST STYLE ALIZE CLAIR - BIBLIOTHEQUE
echo ====================================
echo.
echo MODIFICATIONS APPLIQUEES:
echo - Fond blanc pour la zone de materiaux
echo - Tableau avec fond blanc et texte noir
echo - En-tetes gris clair style Alize
echo - Lignes de grille visibles
echo - Selection en bleu clair
echo - Hover en bleu tres clair
echo.
echo LANCEMENT DE L'APPLICATION...
echo.

cd /d "%~dp0"
dotnet run --project UI_ChausseeNeuve\UI_ChausseeNeuve.csproj

if errorlevel 1 (
    echo.
    echo ERREUR DE COMPILATION OU D'EXECUTION !
    pause
    exit /b 1
)

echo.
echo APPLICATION FERMEE - Test terminé
pause
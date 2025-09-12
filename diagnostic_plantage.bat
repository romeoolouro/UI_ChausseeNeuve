@echo off
echo ================================================================
echo        DIAGNOSTIC URGENT - PLANTAGE FENETRES
echo ================================================================
echo.

echo ?? 1. COMPILATION AVEC DETAILS D'ERREUR...
echo ------------------------------------------

dotnet clean UI_ChausseeNeuve.sln --configuration Debug >nul 2>&1

echo Compilation avec détails...
dotnet build UI_ChausseeNeuve.sln --configuration Debug --verbosity normal 2>&1 | findstr /i "error\|warning\|exception"

if %ERRORLEVEL% NEQ 0 (
    echo ? ERREURS DE COMPILATION DETECTEES
    echo.
    dotnet build UI_ChausseeNeuve.sln --configuration Debug --verbosity normal > compilation_errors.log 2>&1
    echo ?? Détails complets dans: compilation_errors.log
    goto :error_compilation
) else (
    echo ? Compilation réussie
)

echo.
echo ?? 2. TEST DE CREATION DES VIEWMODELS...
echo ----------------------------------------

echo Test création ValeursAdmissiblesViewModel...
powershell -Command "
try {
    Add-Type -Path 'UI_ChausseeNeuve\bin\Debug\net8.0-windows\UI_ChausseeNeuve.dll'
    $vm = New-Object UI_ChausseeNeuve.ViewModels.ValeursAdmissiblesViewModel
    Write-Host '? ValeursAdmissiblesViewModel créé avec succès'
} catch {
    Write-Host '? ERREUR ValeursAdmissiblesViewModel:' $_.Exception.Message
}
"

echo Test création BibliothequeViewModel...
powershell -Command "
try {
    Add-Type -Path 'UI_ChausseeNeuve\bin\Debug\net8.0-windows\UI_ChausseeNeuve.dll'
    $vm = New-Object UI_ChausseeNeuve.ViewModels.BibliothequeViewModel
    Write-Host '? BibliothequeViewModel créé avec succès'
} catch {
    Write-Host '? ERREUR BibliothequeViewModel:' $_.Exception.Message
}
"

echo.
echo ?? 3. TEST DE DEMARRAGE AVEC LOGS...
echo -----------------------------------

echo Démarrage en mode debug avec capture d'erreurs...
start /wait "" powershell -Command "
try {
    Start-Process -FilePath 'dotnet' -ArgumentList 'run --project UI_ChausseeNeuve --configuration Debug --no-build' -RedirectStandardError 'runtime_errors.log' -NoNewWindow -Wait
} catch {
    Write-Host 'Erreur de démarrage:' $_.Exception.Message
}
"

if exist runtime_errors.log (
    echo ? ERREURS RUNTIME DÉTECTÉES:
    echo.
    type runtime_errors.log
    echo.
) else (
    echo ? Aucune erreur runtime capturée
)

echo.
echo ?? 4. VERIFICATION DES DEPENDANCES CRITIQUES...
echo ----------------------------------------------

echo Vérification AppState...
findstr /c:"public static class AppState" "UI_ChausseeNeuve\AppState.cs" >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ? ERREUR: AppState introuvable ou malformé
    goto :error_appstate
) else (
    echo ? AppState présent
)

echo Vérification RelayCommand...
findstr /c:"public class RelayCommand" "UI_ChausseeNeuve\ViewModels\RelayCommand.cs" >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ? ERREUR: RelayCommand introuvable
    goto :error_relaycommand
) else (
    echo ? RelayCommand présent
)

echo Vérification Converters...
findstr /c:"SimpleInverseBooleanConverter" "UI_ChausseeNeuve\Converters\SimpleConverters.cs" >nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo ? ERREUR: SimpleInverseBooleanConverter manquant
    goto :error_converters
) else (
    echo ? Converters présents
)

echo.
echo ?? 5. TEST MANUEL NECESSAIRE...
echo ------------------------------

echo ================================================================
echo                    DIAGNOSTIC TERMINÉ
echo ================================================================
echo.
echo ?? PROCHAINES ÉTAPES:
echo.
echo 1. Ouvrez Visual Studio: start devenv UI_ChausseeNeuve.sln
echo 2. Mettez des points d'arrêt dans les constructeurs:
echo    - ValeursAdmissiblesViewModel()
echo    - BibliothequeViewModel()
echo 3. Lancez en mode Debug (F5)
echo 4. Regardez la fenêtre Output ? Debug
echo 5. Identifiez l'exception exacte qui cause le plantage
echo.
echo ?? CAUSES PROBABLES:
echo - Exception dans AppState.StructureChanged
echo - Erreur de binding dans les converters
echo - Null reference dans l'initialisation des données
echo - Problème de thread UI
echo.
goto :end

:error_compilation
echo.
echo ? PROBLÈME: Erreurs de compilation
echo ? Corrigez d'abord les erreurs de compilation
goto :end

:error_appstate
echo.
echo ? PROBLÈME: AppState défaillant
echo ? Vérifiez la classe AppState
goto :end

:error_relaycommand
echo.
echo ? PROBLÈME: RelayCommand manquant
echo ? Ajoutez la classe RelayCommand
goto :end

:error_converters
echo.
echo ? PROBLÈME: Converters manquants
echo ? Vérifiez les converters dans SimpleConverters.cs
goto :end

:end
if exist compilation_errors.log echo ?? Logs: compilation_errors.log
if exist runtime_errors.log echo ?? Logs: runtime_errors.log
echo.
pause
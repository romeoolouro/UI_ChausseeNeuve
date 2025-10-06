# Script de lancement WPF avec capture des logs TRMM
Write-Host "`n" -ForegroundColor Cyan
Write-Host "   LANCEMENT APPLICATION WPF - SUIVI LOGS TRMM" -ForegroundColor Yellow
Write-Host "`n" -ForegroundColor Cyan

$appPath = "c:\Users\JOSAPHAT\source\repos\UI_ChausseeNeuve\UI_ChausseeNeuve\bin\Debug\net8.0-windows\UI_ChausseeNeuve.exe"
$logFile = "c:\Users\JOSAPHAT\source\repos\UI_ChausseeNeuve\pavement_calculation.log"

Write-Host "Configuration:" -ForegroundColor Green
Write-Host "  Application: $appPath" -ForegroundColor White
Write-Host "  Fichier logs: $logFile" -ForegroundColor White
Write-Host ""

Write-Host "Instructions:" -ForegroundColor Yellow
Write-Host "  1. L''application va se lancer" -ForegroundColor White
Write-Host "  2. Créez un nouveau projet de chaussée" -ForegroundColor White
Write-Host "  3. Configurez 2 couches: E=5000/50 MPa, h=0.20m (Test 5)" -ForegroundColor White
Write-Host "  4. Lancez le calcul" -ForegroundColor White
Write-Host "  5. Observez les logs TRMM ci-dessous" -ForegroundColor White
Write-Host ""
Write-Host "`n" -ForegroundColor Cyan

# Copier la nouvelle DLL TRMM
Write-Host "[Étape 1/3] Copie de la DLL TRMM..." -ForegroundColor Cyan
Copy-Item "c:\Users\JOSAPHAT\source\repos\UI_ChausseeNeuve\PavementCalculationEngine\build\bin\PavementCalculationEngine.dll" `
          "c:\Users\JOSAPHAT\source\repos\UI_ChausseeNeuve\UI_ChausseeNeuve\bin\Debug\net8.0-windows\" -Force
Write-Host "  OK - DLL TRMM copiée (6.0 MB)" -ForegroundColor Green

# Créer le fichier de logs
Write-Host "`n[Étape 2/3] Initialisation du fichier de logs..." -ForegroundColor Cyan
"=== PAVEMENT CALCULATION LOG - $(Get-Date -Format ''yyyy-MM-dd HH:mm:ss'') ===" | Set-Content $logFile -Encoding UTF8
Write-Host "  OK - Fichier créé: $logFile" -ForegroundColor Green

Write-Host "`n[Étape 3/3] Lancement de l''application..." -ForegroundColor Cyan
Write-Host "  (Les logs s''afficheront ici en temps réel)`n" -ForegroundColor White
Write-Host "`n" -ForegroundColor Cyan

# Démarrer suivi des logs en arrière-plan
$job = Start-Job -ScriptBlock {
    param($logPath)
    Get-Content $logPath -Wait -Tail 0 | ForEach-Object {
        if ($_ -match "TRMM|deflection|calculation") {
            Write-Host $_ -ForegroundColor Yellow
        } elseif ($_ -match "ERROR|FAIL") {
            Write-Host $_ -ForegroundColor Red
        } elseif ($_ -match "SUCCESS|PASS") {
            Write-Host $_ -ForegroundColor Green
        } else {
            Write-Host $_
        }
    }
} -ArgumentList $logFile

# Lancer l''application (bloquant)
& $appPath *>&1 | Tee-Object -FilePath $logFile -Append

# Nettoyer
Stop-Job $job
Remove-Job $job

Write-Host "`n" -ForegroundColor Cyan
Write-Host "   APPLICATION FERMÉE - Logs sauvegardés dans:" -ForegroundColor Yellow
Write-Host "   $logFile" -ForegroundColor White
Write-Host "`n" -ForegroundColor Cyan

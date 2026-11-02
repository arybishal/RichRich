$unityExe = "D:\unity6\Editor\Unity.exe"
$projectDir = "D:\Webapp\RichyRich\RichRich_Unity"
$logFile = "D:\Webapp\RichyRich\Builds\scene_build.log"

if (Test-Path $logFile) {
    Remove-Item -Force $logFile
}

Write-Host "Running Unity batchmode for BuildMainScene..."
$p = Start-Process -FilePath $unityExe -ArgumentList "-batchmode", "-quit", "-projectPath", $projectDir, "-executeMethod", "RichRich.Editor.RichRichSceneBuilder.BuildMainScene", "-logFile", $logFile -PassThru -Wait

Write-Host "Unity process exited with code: $($p.ExitCode)"
if (Test-Path $logFile) {
    Write-Host "--- Tail of scene_build.log ---"
    Get-Content $logFile -Tail 40
}
exit $p.ExitCode

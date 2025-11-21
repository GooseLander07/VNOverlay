<#
.SYNOPSIS
    VNTextMiner Auto-Installer
    Downloads the app, dictionaries, and dependencies, then creates a shortcut.
#>

$ErrorActionPreference = "Stop"
$appName = "VNTextMiner"
$installDir = "$env:LOCALAPPDATA\$appName"
$desktop = [Environment]::GetFolderPath("Desktop")

# --- CONFIGURATION URLS ---
$urlAppZip   = "https://github.com/GooseLander07/VNTextMiner/releases/download/v1.0.0/VNTextMiner_v1.0.zip"
$urlMeCabDic = "https://github.com/GooseLander07/VNTextMiner/releases/download/v1.0.0/NMeCab-dic.7z" 
$urlJitendex = "https://github.com/GooseLander07/VNTextMiner/releases/download/v1.0.0/jitendex.zip" 

Write-Host "=== VNTextMiner Installer ===" -ForegroundColor Cyan

# 1. Check .NET 8
Write-Host "[1/5] Checking for .NET 8..."
try {
    dotnet --list-runtimes | Select-String "Microsoft.WindowsDesktop.App 8." > $null
    Write-Host "   -> .NET 8 found." -ForegroundColor Green
} catch {
    Write-Warning "   -> .NET 8 Desktop Runtime not found! Please download it from: https://dotnet.microsoft.com/download/dotnet/8.0"
    Pause
    Exit
}

# 2. Prepare Directory
Write-Host "[2/5] Preparing Installation Directory: $installDir"
if (Test-Path $installDir) { Remove-Item -Path $installDir -Recurse -Force }
New-Item -Path $installDir -ItemType Directory | Out-Null

# 3. Download & Install App
Write-Host "[3/5] Downloading App..."
try {
    Invoke-WebRequest -Uri $urlAppZip -OutFile "$installDir\app.zip"
    # Extract to install directory. 
    # NOTE: Assumes the zip contains the FILES directly (OverlayApp.exe), 
    # not a subfolder (VNTextMiner/OverlayApp.exe).
    Expand-Archive -Path "$installDir\app.zip" -DestinationPath $installDir -Force
    Remove-Item "$installDir\app.zip"
} catch {
    Write-Error "Failed to download or extract App. Link might be broken."
    Pause
    Exit
}

# 4. Download Dictionaries
Write-Host "[4/5] Downloading Dictionaries..."

# A. Jitendex
Write-Host "   -> Downloading Jitendex..."
try {
    Invoke-WebRequest -Uri $urlJitendex -OutFile "$installDir\jitendex.zip"
} catch {
    Write-Warning "   -> Failed to download Jitendex. You may need to add it manually."
}

# B. MeCab Dictionary
Write-Host "   -> Setting up MeCab Dictionary..."
# Create the 'dic' folder that LibNMeCab expects
$dicDir = "$installDir\dic"
New-Item -Path $dicDir -ItemType Directory -Force | Out-Null

try {
    # Download into the dic folder
    Invoke-WebRequest -Uri $urlMeCabDic -OutFile "$dicDir\mecab-dic.zip"
    
    # Extract inside 'dic'. 
    # RESULT: This should result in $installDir\dic\sys.dic (if flat) 
    # OR $installDir\dic\ipadic\sys.dic (if nested). Both are usually fine.
    Expand-Archive -Path "$dicDir\mecab-dic.zip" -DestinationPath $dicDir -Force
    
    # Cleanup the zip to save space
    Remove-Item "$dicDir\mecab-dic.zip"
} catch {
    Write-Warning "   -> Failed to download MeCab Dictionary."
}

# 5. Create Shortcut
Write-Host "[5/5] Creating Desktop Shortcut..."
try {
    $exePath = "$installDir\OverlayApp.exe"
    
    # Double check the EXE actually exists (in case the zip had a subfolder)
    if (-not (Test-Path $exePath)) {
        # Check if it's in a subfolder (Common mistake when zipping)
        $subItems = Get-ChildItem -Path $installDir -Directory
        if ($subItems.Count -eq 1) {
            # Try to find exe in the subfolder
            $subPath = "$installDir\" + $subItems[0].Name + "\OverlayApp.exe"
            if (Test-Path $subPath) {
                $exePath = $subPath
                $installDir = "$installDir\" + $subItems[0].Name
            }
        }
    }

    if (Test-Path $exePath) {
        $wshShell = New-Object -ComObject WScript.Shell
        $shortcut = $wshShell.CreateShortcut("$desktop\$appName.lnk")
        $shortcut.TargetPath = $exePath
        $shortcut.WorkingDirectory = $installDir
        $shortcut.IconLocation = "$exePath,0"
        $shortcut.Save()
        Write-Host "Success! VNTextMiner has been installed to your Desktop." -ForegroundColor Green
    } else {
        Write-Error "Could not find OverlayApp.exe after extraction. Please check the install folder manually."
        Write-Host "Folder: $installDir"
    }
} catch {
    Write-Error "Failed to create shortcut."
}

Pause
$currentDir = Get-Location

# Set environment variables based on the current directory
$env:MMSOURCE112 = "$currentDir\metamod-source"
$env:HL2SDKCS2 = "$currentDir\hl2sdk"

# Define the build directory path
$buildDir = "$currentDir\build"
# Define the package directory path within the build directory
$packageDir = "$buildDir\package\addons\*"

# Define the target directory for the plugin
$targetDir = "C:\cs2\game\csgo\addons"


# Check if the build directory exists, and delete it if it does
if (Test-Path $buildDir) {
    Remove-Item -Path $buildDir -Recurse -Force
}

# Create a new directory named 'build' and enter it
New-Item -Path $buildDir -Type Directory -Force
Set-Location -Path $buildDir

# Configure the build with specified variables and run the build command

python ..\configure.py -s cs2
ambuild


# Check if the package directory exists and copy it to the target directory
if (Test-Path $packageDir) {
    Copy-Item -Path $packageDir -Destination $targetDir -Recurse -Force
}

# Return to the root directory
Set-Location -Path $currentDir
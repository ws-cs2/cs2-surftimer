param(
    [string]$steamcmdDir = "C:\steamcmd",
    [string]$cs2Dir = "C:\cs2"
)

# Building the command string
$command = "& `"$steamcmdDir/steamcmd.exe`" +force_install_dir `"$cs2Dir`" +login anonymous +app_update 730 validate +quit"

# Execute the command
Invoke-Expression $command

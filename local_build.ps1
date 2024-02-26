# PowerShell script to build a .NET project and copy specific files

# Navigate to the project directory, if necessary
Set-Location "C:\dev\wst\cs2-surftimer\cssharp"

# Run dotnet build
dotnet build WST.csproj

$command = "C:\cs2\game\bin\win64\cs2.exe"

# Check if build was successful
if ($LASTEXITCODE -eq 0) {
    # copy these files
    # Npgsql.dll
    # dapper.dll
    # WST.dll


    $sourcePath = ".\bin\Debug\net7.0"
    $destinationPath = "C:\cs2\game\csgo\addons\counterstrikesharp\plugins\WST"
    $cfgPath = "C:\cs2\game\csgo\cfg\WST"

    # Create the destination directory if it doesn't exist
    if (!(Test-Path -Path $destinationPath)) {
        New-Item -ItemType Directory -Force -Path $destinationPath
    }
    
    if (!(Test-Path -Path $cfgPath)) {
        New-Item -ItemType Directory -Force -Path $cfgPath
    }

    # Copy the files
    Copy-Item -Path ".\server.json" -Destination $cfgPath
    
    Copy-Item -Path $sourcePath"\Npgsql.dll" -Destination $destinationPath
    Copy-Item -Path $sourcePath"\Dapper.dll" -Destination $destinationPath
    Copy-Item -Path $sourcePath"\WST.dll" -Destination $destinationPath
    Copy-Item -Path $sourcePath"\Supabase.dll" -Destination $destinationPath
    Copy-Item -Path $sourcePath"\Supabase.Core.dll" -Destination $destinationPath
    Copy-Item -Path $sourcePath"\Supabase.Storage.dll" -Destination $destinationPath
    Copy-Item -Path $sourcePath"\Supabase.Gotrue.dll" -Destination $destinationPath
    Copy-Item -Path $sourcePath"\Supabase.Realtime.dll" -Destination $destinationPath
    Copy-Item -Path $sourcePath"\Supabase.Postgrest.dll" -Destination $destinationPath
    Copy-Item -Path $sourcePath"\Supabase.Functions.dll" -Destination $destinationPath
    Copy-Item -Path $sourcePath"\Newtonsoft.Json.dll" -Destination $destinationPath
    Copy-Item -Path $sourcePath"\MimeMapping.dll" -Destination $destinationPath
    Copy-Item -Path $sourcePath"\Websocket.Client.dll" -Destination $destinationPath
    Copy-Item -Path $sourcePath"\System.Reactive.dll" -Destination $destinationPath
    
    
    Write-Host "Files copied successfully."

    # check if THERE ARE TWO cs2 processes running
    $isTwo = Get-Process -Name cs2 | Measure-Object | Select-Object -ExpandProperty Count
    # if its not then start it
    if (!($isTwo -eq 2)) {
        #Start-Process -FilePath $command -ArgumentList "-dedicated +map de_dust2 +host_workshop_map 3129698096" 
        Start-Process -FilePath $command -ArgumentList "-dedicated +map de_dust2 +game_type 3 +game_mode 0 +host_workshop_collection 3132701467" 
    }

} else {
    Write-Host "Build failed. Files were not copied."
}


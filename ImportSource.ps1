# Define source and destination directories
$sourceDir = "C:\Users\nac_n\AppData\LocalLow\Colossal Order\Cities Skylines II\ModsData\I18NEverywhere"
$destDir = "F:\AppPool\CS2THMod\Sources\I18NEverywhere"

# Create destination directory if it does not exist
if (!(Test-Path -Path $destDir)) {
    New-Item -ItemType Directory -Force -Path $destDir
}

# Copy all files from source to destination
Copy-Item -Path "$sourceDir\*" -Destination $destDir -Recurse -Force
Write-Host "Copied I18NEverywhere"

# Run the PHP script
$phpScript = "php ./Tools/json2csv.php"
Invoke-Expression $phpScript

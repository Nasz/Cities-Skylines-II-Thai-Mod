# Run PHP script
Write-Host "Downloading CSV from  Google Sheet."
php .\Tools\sheet2txt.php

# Copy files and echo notification
Copy-Item -Path "F:\AppPool\CS2THMod\Sources\Locale\th-TH.loc.txt" -Destination "F:\AppPool\CS2THMod\Tools\th-TH.loc.txt"
Write-Host "Copied th-TH.loc.txt"

Copy-Item -Path "F:\AppPool\CS2THMod\Sources\Locale\en-US.loc" -Destination "F:\AppPool\CS2THMod\Tools\en-US.loc"
Write-Host "Copied en-US.loc"

# Change directory
Set-Location -Path "F:\AppPool\CS2THMod\Tools\"

# Run Python script
python GenLOC.py

# Copy the generated file and echo notification
Copy-Item -Path "F:\AppPool\CS2THMod\Tools\th-TH.loc" -Destination "F:\AppPool\CS2THMod\Content\th-TH.loc"
Write-Host "Copied th-TH.loc to Content folder"

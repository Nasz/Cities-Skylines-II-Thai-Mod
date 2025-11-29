# Run PHP script
Write-Host "Downloading CSV from  Google Sheet."
php .\Tools\sheet2txt.php
php .\Tools\sheet2json.php

# Copy files and echo notification
Copy-Item -Path "E:\AppPool\CS2THMod\Sources\Locale\th-TH.loc.txt" -Destination "E:\AppPool\CS2THMod\Tools\th-TH.loc.txt"
Write-Host "Copied th-TH.loc.txt"

Copy-Item -Path "E:\AppPool\CS2THMod\Sources\Locale\en-US.loc" -Destination "E:\AppPool\CS2THMod\Tools\en-US.loc"
Write-Host "Copied en-US.loc"

# Change directory
Set-Location -Path "E:\AppPool\CS2THMod\Tools\"

# Run Python script
python GenLOC.py

# Copy the generated file and echo notification
Copy-Item -Path "E:\AppPool\CS2THMod\Tools\th-TH.loc" -Destination "E:\AppPool\CS2THMod\Content\th-TH.loc"
Write-Host "Copied th-TH.loc to Content folder"

# Change directory
Set-Location -Path "E:\AppPool\CS2THMod\"
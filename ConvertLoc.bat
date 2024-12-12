@echo off
xcopy "C:\Users\nac_n\AppData\LocalLow\Colossal Order\Cities Skylines II\ModsData\I18NEverywhere\*" "F:\AppPool\CS2THMod\Sources" /E
php ./Tools/txt2csv.php
@echo off
php sheet2txt.php
copy ..\Sources\Locale\th-TH.loc.txt "D:\AppPool\CS2THMod\Tools\th-TH-modify.loc.txt"
D:\python\python.exe loccoverter.py
copy .\ru-RU-modify.loc "D:\AppPool\CS2THMod\Sources\Locale\th-TH.loc"
copy .\ru-RU-modify.loc "E:\SteamLibrary\steamapps\common\Cities Skylines II\Cities2_Data\StreamingAssets\Data~\ru-RU.loc"
pause
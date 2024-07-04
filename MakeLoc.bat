@echo off
php Tools\sheet2txt.php
copy D:\AppPool\CS2THMod\Sources\Locale\th-TH.loc.txt "D:\AppPool\CS2THMod\Tools\th-TH.loc.txt"
copy D:\AppPool\CS2THMod\Sources\Locale\en-US.loc "D:\AppPool\CS2THMod\Tools\en-US.loc"
cd D:\AppPool\CS2THMod\Tools\
python GenLOC.py
copy D:\AppPool\CS2THMod\Tools\th-TH.loc "D:\AppPool\CS2THMod\Sources\Locale\th-TH.dat"
copy D:\AppPool\CS2THMod\Tools\th-TH.loc "D:\AppPool\CS2THMod\ru-RU.loc"
::copy D:\AppPool\CS2THMod\Tools\th-TH.loc "E:\SteamLibrary\steamapps\common\Cities Skylines II\Cities2_Data\StreamingAssets\Data~\th-TH.loc"
::copy D:\AppPool\CS2THMod\Tools\th-TH.loc "E:\SteamLibrary\steamapps\common\Cities Skylines II\Cities2_Data\StreamingAssets\Data~\ru-RU.loc"
::pause
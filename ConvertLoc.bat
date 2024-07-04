@echo off
copy "E:\SteamLibrary\steamapps\common\Cities Skylines II\Cities2_Data\StreamingAssets\Data~\en-US.loc" D:\AppPool\CS2THMod\Tools\en-US.loc
copy "E:\SteamLibrary\steamapps\common\Cities Skylines II\Cities2_Data\StreamingAssets\Data~\zh-HANS.loc" D:\AppPool\CS2THMod\Tools\zh-HANS.loc
copy "E:\SteamLibrary\steamapps\common\Cities Skylines II\Cities2_Data\StreamingAssets\Data~\ru-RU.loc" D:\AppPool\CS2THMod\Tools\ru-RU.loc
copy "E:\SteamLibrary\steamapps\common\Cities Skylines II\Cities2_Data\StreamingAssets\Data~\th-TH.loc" D:\AppPool\CS2THMod\Tools\th-TH.loc
php ./Tools/loc2txt.php
php ./Tools/txt2csv.php
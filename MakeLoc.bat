@echo off
php Tools\sheet2txt.php
copy F:\AppPool\CS2THMod\Sources\Locale\th-TH.loc.txt "F:\AppPool\CS2THMod\Tools\th-TH.loc.txt"
copy F:\AppPool\CS2THMod\Sources\Locale\en-US.loc "F:\AppPool\CS2THMod\Tools\en-US.loc"
cd F:\AppPool\CS2THMod\Tools\
python GenLOC.py
copy F:\AppPool\CS2THMod\Tools\th-TH.loc "F:\AppPool\CS2THMod\Content\th-TH.loc"
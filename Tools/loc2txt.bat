@echo off

REM Drop .locmeta or .json. It can convert the text resources

@if "%~1"=="" goto skip

@pushd %~dp0
D:\python\python.exe export.py "%~1"
@popd

pause
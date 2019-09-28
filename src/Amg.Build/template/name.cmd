@echo off
rem Do not modify below this line
rem Bootstrapper script of https://github.com/sidiandi/Amg.Build
set project=%~dpn0
set fastrun=%project%\out\fastrun.cmd
if exist %fastrun% ( %fastrun% run --project %project% -- %* ) else ( dotnet run --project %project - %* )

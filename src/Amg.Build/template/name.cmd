@echo off
rem Do not modify
rem Bootstrapper script of https://github.com/sidiandi/Amg.Build
setlocal
set AmgBuildTargetFramework=net5.0
set name=%~n0
set exe=%~dp0%name%\%name%\bin\Debug\%AmgBuildTargetFramework%\%name%.exe
set project=%~dp0%name%
if exist %exe% (%exe% %*) else (dotnet test --project %project% && %exe% %*)

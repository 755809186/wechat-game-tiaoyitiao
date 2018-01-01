@echo off

MSBuild /t:rebuild /p:TargetFrameworkVersion=v4.0;Configuration=Release;OutputPath=bin\release4.0 TiaoYiTiao.csproj

pause
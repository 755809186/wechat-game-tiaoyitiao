@echo off

MSBuild /t:rebuild /p:TargetFrameworkVersion=v4.6;Configuration=Release TiaoYiTiao.csproj

pause
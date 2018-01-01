@echo off

MSBuild /t:rebuild /p:TargetFrameworkVersion=v4.0;Configuration=Release TiaoYiTiao.csproj

pause
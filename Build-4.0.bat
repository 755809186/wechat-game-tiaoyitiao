@echo off

MSBuild /t:rebuild /p:TargetFrameworkVersion=v4.0;Configuration=Release;DefineConstants=NET40;OutputPath=bin\release4.0 TiaoYiTiao.4.0.csproj

pause
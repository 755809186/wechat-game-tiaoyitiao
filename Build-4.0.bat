@echo off

::MSBuild TiaoYiTiao.4.0.csproj /t:rebuild 
/p:TargetFrameworkVersion=v4.0;Configuration=Release;DefineConstants=NET40;OutputPath=bin\release40\

MSBuild /t:rebuild /p:TargetFrameworkVersion=v4.0;Configuration=Release;DefineConstants=NET40;OutputPath=bin\release40\ 
TiaoYiTiao.4.0.csproj

pause
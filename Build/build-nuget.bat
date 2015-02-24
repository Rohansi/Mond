@echo off

nuget pack ..\Mond\Mond.csproj -Build
nuget pack ..\Mond.RemoteDebugger\Mond.RemoteDebugger.csproj -Build

@echo off

dotnet pack .\DarkNet.csproj -c Release-Forms -o bin
dotnet pack .\DarkNet.csproj -c Release-WPF -o bin
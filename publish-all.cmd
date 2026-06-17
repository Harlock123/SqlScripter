@echo off
REM Publishes self-contained, single-file builds of SqlScripter for every supported
REM target: Intel & ARM on Windows, macOS and Linux. Run from the project root.
setlocal enabledelayedexpansion

set OUT_ROOT=publish
if not "%~1"=="" set OUT_ROOT=%~1

for %%R in (win-x64 win-arm64 osx-x64 osx-arm64 linux-x64 linux-arm64) do (
  echo ==^> Publishing %%R
  dotnet publish -c Release -r %%R --self-contained true ^
    -p:PublishSingleFile=true ^
    -p:IncludeNativeLibrariesForSelfExtract=true ^
    -o "%OUT_ROOT%\%%R" || exit /b 1
)

echo Done. Artifacts are under %OUT_ROOT%\^<rid^>\
endlocal

@echo off
@setlocal
set ERROR_CODE=0
dotnet restore src\Stratis.DevEx.sln
if not %ERRORLEVEL%==0  (
    echo Error restoring NuGet packages for Stratis.DevEx.sln.
    set ERROR_CODE=1
    goto End
)
dotnet build src\Stratis.CodeAnalysis.Cs\Stratis.CodeAnalysis.Cs.Package\Stratis.CodeAnalysis.Cs.Package.csproj /p:Configuration=Release

if not %ERRORLEVEL%==0  (
    echo Error building Roslyn analyzer.
    set ERROR_CODE=2
    goto End
)

dotnet build src\Stratis.DevEx.Gui\Stratis.DevEx.Gui.csproj /p:Configuration=Release %*

if not %ERRORLEVEL%==0  (
    echo Error building Stratis DevEx GUI.
    set ERROR_CODE=2
    goto End
)

:End
@endlocal
exit /B %ERROR_CODE%


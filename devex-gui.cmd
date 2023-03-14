@echo off
pushd
@setlocal
set ERROR_CODE=0

src\Stratis.DevEx.Gui\bin\Windows\Debug\net48\Stratis.DevEx.Gui.exe %*

:end
@endlocal
popd
exit /B %ERROR_CODE%
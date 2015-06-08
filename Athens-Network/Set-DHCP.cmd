@echo off

set _exitStatus=0
set _argcActual=0
set _argcExpected=1

echo.

for %%i in (%*) do set /A _argcActual+=1

if %_argcActual% NEQ %_argcExpected% (
  call :_ShowUsage %0%, ""

  set _exitStatus=1
  
  goto:_EOF
)

netsh interface ip set address %1 dhcp

echo Reboot to enable DHCP

goto:_EOF

:_ShowUsage
  
  echo Usage: Set-DHCP <Adapter name>
  echo Example: Set-DHCP "Ethernet"

  echo.
    
  goto:eof

:_EOF
 
echo The exit status is %_exitStatus%.

echo.

cmd /c exit %_exitStatus%


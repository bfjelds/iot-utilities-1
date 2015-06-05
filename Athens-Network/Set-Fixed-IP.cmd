@echo off

set _exitStatus=0
set _argcActual=0
set _argcExpected=5

echo.

for %%i in (%*) do set /A _argcActual+=1

if %_argcActual% NEQ %_argcExpected% (

  call :_ShowUsage
  
  set _exitStatus=1
  
  goto:_EOF
)

netsh interface ip set address %1 static %2 %2 %4 %5

echo Reboot to get the fixed IP address.

goto:_EOF

:_ShowUsage
  
  echo Usage: Set-Fixed-IP <Interface name> <static IP address> <Subnet Mask> <Gateway IP address> <Metric>
  echo Example: Set-Fixed-IP "Local Area Connection" 192.168.0.10 255.255.255.0 192.168.0.1 1

  echo.
  
  )
  
  goto:eof

:_EOF
 
echo The exit status is %_exitStatus%.

echo.

cmd /c exit %_exitStatus%


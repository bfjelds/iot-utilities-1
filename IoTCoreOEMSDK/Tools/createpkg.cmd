@echo off
setlocal
REM Input validation
if [%1] == [/?] goto Usage
if [%1] == [-?] goto Usage
if [%1] == [] goto Usage

if NOT DEFINED PKG_OUTPUT (
	echo Environment not defined. Call setenv_arm.cmd or setenv_x86.cmd
	goto End
)

if NOT DEFINED PRODUCT (
	echo PRODUCT not set; Setting it to Undefined
	set PRODUCT=Undefined
)

echo Creating %1 Package
call "%KITSROOT%\Tools\bin\i386\pkggen.exe" "%1" /config:"%PKG_CONFIG_XML%" /output:"%PKG_OUTPUT%" /version:%BSP_VERSION% /build:fre /cpu:%BSP_ARCH% /variables:"HIVE_ROOT=%HIVE_ROOT%;WIM_ROOT=%WIM_ROOT%;_RELEASEDIR=%OUTPUT_DIRECTORY%\;ARCH=%BSP_ARCH%;PROD=%PRODUCT%;"

if errorlevel 1 goto Error

echo Package creation completed
goto End

:Usage
echo Usage: createpkg packagefile.pkg.xml 
echo    packagefile.pkg.xml....... Required, Package definition XML file 
echo    [/?].............. Displays this usage string. 
echo    Example:
echo        createpkg sample.pkg.xml

exit /b 1

:Error
echo "createpkg %1" failed with error %ERRORLEVEL%
exit /b 1

:End
endlocal
exit /b 0
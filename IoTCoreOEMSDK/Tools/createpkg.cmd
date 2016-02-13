@echo off
setlocal
REM Input validation
if [%1] == [/?] goto Usage
if [%1] == [-?] goto Usage
if [%1] == [] goto Usage

if NOT DEFINED PKGBLD_DIR (
	echo Environment not defined. Call setenv
	goto End
)

if NOT DEFINED PRODUCT (
	echo PRODUCT not set; Setting it to ProductA as default
	set PRODUCT=ProductA
)

echo Creating %1 Package
call "%KITSROOT%\Tools\bin\i386\pkggen.exe" "%1" /config:"%PKG_CONFIG_XML%" /output:"%PKGBLD_DIR%" /version:%BSP_VERSION% /build:fre /cpu:%BSP_ARCH% /variables:"HIVE_ROOT=%HIVE_ROOT%;WIM_ROOT=%WIM_ROOT%;_RELEASEDIR=%BLD_DIR%\;ARCH=%BSP_ARCH%;PROD=%PRODUCT%;PRJDIR=%PRJ_DIR%" 

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
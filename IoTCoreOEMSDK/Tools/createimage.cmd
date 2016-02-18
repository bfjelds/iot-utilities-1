@echo off
setlocal
REM Input validation
if [%1] == [/?] goto Usage
if [%1] == [-?] goto Usage
if [%1] == [] goto Usage
if [%2] == [] goto Usage
if NOT [%2] == [Retail] ( if NOT [%2] == [Test] goto Usage )

REM Checking prerequisites
if NOT DEFINED PRJ_DIR (
	echo Environment not defined. Call setenv
	goto End
)
set PRODUCT=%1
set PRODSRC_DIR=%PRJ_DIR%\Products\%PRODUCT%
set PRODBLD_DIR=%BLD_DIR%\%1\%2

if NOT exist %PRJ_DIR%\Products\%PRODUCT% (
   echo %PRODUCT% not found. Available products listed below
   dir /b %PRJ_DIR%\Products
   goto Usage
)
REM Start processing command
echo Creating %1 %2 Image
echo Build Start Time : %TIME%

echo Building Packages with product specific contents
call createpkg.cmd %COMMON_DIR%\Packages\Custom.Cmd\Custom.Cmd.pkg.xml

if NOT exist %PRODSRC_DIR%\prov\%PRODUCT%Prov.ppkg (
 REM Create the provisioning ppkg
 call createprovpkg.cmd %PRODUCT%
)
call createpkg.cmd %COMMON_DIR%\Packages\Provisioning.Auto\Provisioning.Auto.pkg.xml 

echo creating image...
call imggen.cmd "%PRODBLD_DIR%\IoTCore.FFU" "%PRODSRC_DIR%\%2OEMInput.xml" "%KITSROOT%MSPackages" %BSP_ARCH%

if errorlevel 1 goto Error

echo Build End Time : %TIME%
echo Image creation completed
goto End

:Usage
echo Usage: createimage ProductName BuildType 
echo    ProductName....... Required, Name of the product to be created. 
echo    BuildType......... Required, Retail/Test 
echo    [/?].............. Displays this usage string. 
echo    Example:
echo        createimage SampleA Retail

exit /b 1

:Error
echo "CreateImage %1 %2" failed with error %ERRORLEVEL%
exit /b 1

:End
endlocal
exit /b 0
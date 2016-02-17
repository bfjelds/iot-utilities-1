@echo off
setlocal
REM Input validation
if [%1] == [/?] goto Usage
if [%1] == [-?] goto Usage
if [%1] == [] goto Usage
REM Checking prerequisites
if NOT DEFINED PRJ_DIR (
	echo Environment not defined. Call setenv
	goto End
)
REM Start processing command
set PRODUCT=%1
set PRODSRC_DIR=%PRJ_DIR%\Products\%PRODUCT%
set STORE_DIR=%KITSROOT%\Assessment and Deployment Kit\Imaging and Configuration Designer\x86

echo Creating Provisioning Package for %PRODUCT% using %PRODSRC_DIR%\prov\customizations.xml
icd.exe /Build-ProvisioningPackage /CustomizationXML:"%PRODSRC_DIR%\prov\customizations.xml" /PackagePath:%PRODSRC_DIR%\prov\%PRODUCT%Prov /StoreFile:"%STORE_DIR%\Microsoft-IoTUAP-Provisioning.dat,%STORE_DIR%\Microsoft-Common-Provisioning.dat" +Overwrite
if errorlevel 1 goto Error

goto End

:Usage
echo Usage: createprovpkg ProductName customization.xml
echo    ProductName............. Required Product Name 
echo    [/?].............. Displays this usage string. 
echo    Example:
echo        createprovpkg SampleA

exit /b 1

:Error
echo "createprovpkg %1" failed with error %ERRORLEVEL%
exit /b 1

:End
endlocal
exit /b 0
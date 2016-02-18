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
set STORE_DIR=%KITSROOT%\Assessment and Deployment Kit\Imaging and Configuration Designer\x86

echo Creating Provisioning Package using %1\customizations.xml
icd.exe /Build-ProvisioningPackage /CustomizationXML:"%1\customizations.xml" /PackagePath:%1\ProvPkg /StoreFile:"%STORE_DIR%\Microsoft-IoTUAP-Provisioning.dat,%STORE_DIR%\Microsoft-Common-Provisioning.dat" +Overwrite
if errorlevel 1 goto Error

goto End

:Usage
echo Usage: provpkg dir_path 
echo    dir_path.......... Path containing customization.xml; Output ProvPkg.ppkg will be in the same dir 
echo    [/?].............. Displays this usage string. 
echo    Example:
echo        provpkg C:\IotCoreOEMSDK\Templates\

exit /b 1

:Error
echo "provpkg %1" failed with error %ERRORLEVEL%
exit /b 1

:End
endlocal
exit /b 0
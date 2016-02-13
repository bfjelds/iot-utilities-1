@echo off
setlocal
REM Input validation
if [%1] == [/?] goto Usage
if [%1] == [-?] goto Usage
if [%1] == [] goto Usage
if [%2] == [] goto Usage
if NOT [%2] == "Retail" (if NOT [%2] == "Test" goto Usage)

REM Checking prerequisites
if NOT DEFINED PRJ_DIR (
	echo Environment not defined. Call setenv_arm.cmd or setenv_x86.cmd
	goto End
)
REM Start processing command
echo Creating %1 %2 Image
echo Build Start Time : %TIME%

set PRODUCT=%1
set PRODUCT_DIR=%PRJ_DIR%\Products\%PRODUCT%

echo Building product specific packages
call createpkg.cmd %PRJ_DIR%\Products\packages\oemcustcmdpkg\OEMCustCmd.pkg.xml

echo creating image...
call icd.exe /Build-ImageFromPackages /ImagePath:"%OUTPUT_DIRECTORY%\%1\%BSP_ARCH%\%2\IoTCore.FFU" /OEMInputXML:"%PRODUCT_DIR%\%BSP_ARCH%\%2OEMInput.xml" /MSPackageRoot:"%KITSROOT%MSPackages" /CPUType:%BSP_ARCH% /CustomizationXML:"%PRODUCT_DIR%\customizations.xml" /OEMCustomizationVer:"%BSP_VERSION%"

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
echo        createimage ProductA Retail

exit /b 1

:Error
echo "CreateImage %1 %2" failed with error %ERRORLEVEL%
exit /b 1

:End
endlocal
exit /b 0

::icd.exe /Build-ImageFromPackages /ImagePath:<path_to_ffu> [/OEMInputXML:<path_to_xml>] 
::[/BSPConfigFile:<path_to_bsp_config_xml>] [/ImageType:<image_type>] [/MSPackageRoot:<path_to_mspackage_directory>] 
::[/CPUType:<CPUType>] [/ProvisioningPackage:<path_to_ppkg>] [/OEMCustomizationVer:<version>] 
::[/CustomizationXML:<path_to_xml>] [/MCSFCustomizationXML:<path_to_xml>] [/?]  
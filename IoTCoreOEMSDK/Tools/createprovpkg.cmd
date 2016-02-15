@echo off
setlocal
REM Input validation
if [%1] == [/?] goto Usage
if [%1] == [-?] goto Usage
if [%1] == [] goto Usage
if [%2] == [] goto Usage
REM Checking prerequisites
if NOT DEFINED PRJ_DIR (
	echo Environment not defined. Call setenv
	goto End
)
REM Start processing command
set PRODUCT=%1
set PRODSRC_DIR=%PRJ_DIR%\Products-%BSP_ARCH%\%PRODUCT%
set PRODBLD_DIR=%BLD_DIR%\%1\%2

echo Creating Provisioning Package with %PRODUCT%

CustomizationGen-iotcore.cmd %BLD_DIR%\%PRODUCT%\%2 "%PRODSRC_DIR%\%2OEMInput.xml" "%KITSROOT%MSPackages" "%PRODSRC_DIR%\customizations.xml" "%BSP_VERSION%"

::icd.exe /Build-ProvisioningPackage /CustomizationXML:"%1" /PackagePath:"%PKGBLD_DIR%" /StoreFile:"%KITSROOT%\Assessment and Deployment Kit\Imaging and Configuration Designer\x86\Microsoft-Common-Provisioning.dat"
t::"%KITSROOT%\Assessment and Deployment Kit\Imaging and Configuration Designer\x86\icd.exe" 
::icd.exe /Build-ProvisioningPackage /CustomizationXML:<path_to_xml> /PackagePath:<path_to_ppkg> 
::[/StoreFile:<path_to_storefile>]  [/MSPackageRoot:<path_to_mspackage_directory>]  [/OEMInputXML:<path_to_xml>]
::[/ProductName:<product_name>]  [/Variables:<name>:<value>] [[+|-]Encrypted] [[+|-]Overwrite] [/?]  

::/StoreFile:C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Imaging and Configuration Designer\x86\Microsoft-Common-Provisioning.dat
::/MSPackageRoot:"%KITSROOT%MSPackages" /OEMInputXML:"%PRJ_DIR%\Products\%PRODUCT%\%BSP_ARCH%\RetailOEMInput.xml"

:Usage
echo Usage: createprovpkg ProductName customization.xml
echo    ProductName............. Required Product Name 
echo    customization.xml....... Required Customisation input file. 
echo    [/?].............. Displays this usage string. 
echo    Example:
echo        createprovpkg ProductA customization.xml

exit /b 1

:Error
echo "createprovpkg %1" failed with error %ERRORLEVEL%
exit /b 1

:End
endlocal
exit /b 0
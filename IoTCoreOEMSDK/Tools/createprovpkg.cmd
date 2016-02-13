:: Run the following script from “Deployment and Imaging Tools Environment” as Admin.
:: Run setenv before running this script
:: Usage : createpkg packagefile.pkg.xml
@echo off
:: Help 
if "%1"=="/?" (
	echo Usage: createprovpkg customization.xml
	goto CLEANUP
)
:: Checking prerequisites
if "%1"=="" (
	echo createprovpkg customization.xml
	goto CLEANUP
)

if NOT DEFINED PKGBLD_DIR (
	echo Environment not defined. Call setenv_arm.cmd or setenv_x86.cmd
	goto CLEANUP
)

echo Creating Provisioning Package with %1
CustomizationGen.cmd "%BLD_DIR%\%PRODUCT%\%BSP_ARCH%\Retail\" "%PRJ_DIR%\Products\%PRODUCT%\%BSP_ARCH%\RetailOEMInput.xml" "%KITSROOT%MSPackages" "%1" "%BSP_VERSION%"
::icd.exe /Build-ProvisioningPackage /CustomizationXML:"%1" /PackagePath:"%PKGBLD_DIR%" /StoreFile:"%KITSROOT%\Assessment and Deployment Kit\Imaging and Configuration Designer\x86\Microsoft-Common-Provisioning.dat"
::"%KITSROOT%\Assessment and Deployment Kit\Imaging and Configuration Designer\x86\icd.exe" 
::icd.exe /Build-ProvisioningPackage /CustomizationXML:<path_to_xml> /PackagePath:<path_to_ppkg> 
::[/StoreFile:<path_to_storefile>]  [/MSPackageRoot:<path_to_mspackage_directory>]  [/OEMInputXML:<path_to_xml>]
::[/ProductName:<product_name>]  [/Variables:<name>:<value>] [[+|-]Encrypted] [[+|-]Overwrite] [/?]  

::/StoreFile:C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Imaging and Configuration Designer\x86\Microsoft-Common-Provisioning.dat
::/MSPackageRoot:"%KITSROOT%MSPackages" /OEMInputXML:"%PRJ_DIR%\Products\%PRODUCT%\%BSP_ARCH%\RetailOEMInput.xml"
:CLEANUP
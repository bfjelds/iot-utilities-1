:: Run setenv before running this script
:: This script creates the folder structure and copies the template files for a new product
:: usage : newproduct <product name>
@echo off
if [%1] == [/?] goto Usage
if [%1] == [-?] goto Usage
if [%1] == [] goto Usage

if NOT DEFINED PRJ_DIR (
	echo Environment not defined. Call setenv
	goto End
)
:: Error Checks

if /i EXIST %PRJ_DIR%\Products\%1 (
	echo %1 already exists
	goto End
)
:: Start processing command
echo Creating %1 Product
SET PRODUCT=%1
SET PRODSRC_DIR=%PRJ_DIR%\Products\%PRODUCT%

mkdir "%PRODSRC_DIR%"
mkdir "%PRODSRC_DIR%\bsp"
mkdir "%PRODSRC_DIR%\prov"

if [%BSP_ARCH%] ==[arm] (
:: Copying template files
copy "%KITSROOT%\OEMInputSamples\RPi2\RetailOEMInput.xml" %PRODSRC_DIR%\RetailOEMInput.xml
copy "%KITSROOT%\OEMInputSamples\RPi2\ProductionOEMInput.xml" %PRODSRC_DIR%\TestOEMInput.xml
copy "%KITSROOT%\FMFiles\arm\RPi2FM.xml" %PRODSRC_DIR%\bsp\OEM_RPi2FM.xml
)
if [%BSP_ARCH%] ==[x86] (
:: Copying template files
copy "%KITSROOT%\OEMInputSamples\MBM\RetailOEMInput.xml" %PRODSRC_DIR%\RetailOEMInput.xml
copy "%KITSROOT%\OEMInputSamples\MBM\ProductionOEMInput.xml" %PRODSRC_DIR%\TestOEMInput.xml
copy "%KITSROOT%\FMFiles\x86\MBMFM.xml" %PRODSRC_DIR%\bsp\OEM_MBMFM.xml
)

copy "%OEMSDK_ROOT%\Templates\oemcustomization.cmd" %PRODSRC_DIR%\oemcustomization.cmd
copy "%OEMSDK_ROOT%\Templates\customizations.xml" %PRODSRC_DIR%\prov\customizations.xml

echo %1 product directories ready
goto End

:Usage
echo Usage: newproduct ProductName 
echo    ProductName....... Required, Name of the product to be created. 
echo    [/?].............. Displays this usage string. 
echo    Example:
echo        newproduct SampleA 

exit /b 1

:Error
echo "newproduct %1 " failed with error %ERRORLEVEL%
exit /b 1

:End
exit /b 0

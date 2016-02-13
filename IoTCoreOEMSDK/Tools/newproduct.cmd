:: Run setenv before running this script
:: This script creates the folder structure and copies the template files for a new product
:: usage : newproduct <product name>
@echo off
if [%1] == [/?] goto Usage
if [%1] == [-?] goto Usage
if [%1] == [] goto Usage

if NOT DEFINED PRJ_DIR (
	echo Environment not defined. Call setenv_arm.cmd or setenvx86.cmd
	goto End
)
:: Error Checks

if /i EXIST %PRJ_DIR%\%BSP_ARCH%Products\%1 (
	echo %1 already exists
	goto End
)
:: Start processing command
echo Creating %1 Product
set PRODUCT=%1
SET PRODUCT_DIR=%PRJ_DIR%\Products-%BSP_ARCH%\%PRODUCT%

mkdir "%PRODUCT_DIR%"
mkdir "%PRODUCT_DIR%\bsp"

if [%BSP_ARCH%] ==[arm] (
:: Copying template files
copy "%KITSROOT%\OEMInputSamples\RPi2\RetailOEMInput.xml" %PRODUCT_DIR%\RetailOEMInput.xml
copy "%KITSROOT%\OEMInputSamples\RPi2\ProductionOEMInput.xml" %PRODUCT_DIR%\TestOEMInput.xml
copy "%KITSROOT%\FMFiles\arm\RPi2FM.xml" %PRODUCT_DIR%\bsp\OEM_RPi2FM.xml
)
if [%BSP_ARCH%] ==[x86] (
:: Copying template files
copy "%KITSROOT%\OEMInputSamples\MBM\RetailOEMInput.xml" %PRODUCT_DIR%\RetailOEMInput.xml
copy "%KITSROOT%\OEMInputSamples\MBM\ProductionOEMInput.xml" %PRODUCT_DIR%\TestOEMInput.xml
copy "%KITSROOT%\FMFiles\x86\MBMFM.xml" %PRODUCT_DIR%\bsp\OEM_MBMFM.xml
)

copy "%PRJ_DIR%\Templates\oemcustomization.cmd" %PRODUCT_DIR%\oemcustomization.cmd
copy "%PRJ_DIR%\Templates\customizations.xml" %PRODUCT_DIR%\customizations.xml

echo %1 product directories ready
goto End

:Usage
echo Usage: newproduct ProductName 
echo    ProductName....... Required, Name of the product to be created. 
echo    [/?].............. Displays this usage string. 
echo    Example:
echo        newproduct ProductA 

exit /b 1

:Error
echo "newproduct %1 " failed with error %ERRORLEVEL%
exit /b 1

:End
exit /b 0

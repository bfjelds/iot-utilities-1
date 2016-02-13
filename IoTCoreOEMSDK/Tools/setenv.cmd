:: Run the following script from “Deployment and Imaging Tools Environment” as Admin.
@echo off

:: Help 
if "%1"=="/?" (
	echo Usage: setenv arm|x86
	goto CLEANUP
)
:: Checking prerequisites
if "%1"=="" (
	echo Usage: setenv arm|x86
	goto CLEANUP
)

:: Environment configurations
SET KITROOT=%ProgramFiles(x86)%\Windows Kits\10
SET PATH=%KITSROOT%tools\bin\i386;%PATH%
SET AKROOT=%KITSROOT%
SET WPDKCONTENTROOT=%KITSROOT%
SET PKG_CONFIG_XML=%KITSROOT%\Tools\bin\i386\pkggen.cfg.xml
if exist versioninfo.txt (
	SET /P BSP_VERSION=< versioninfo.txt
) else (
	SET BSP_VERSION=10.0.0.0
	echo Version Info not found. Defaulting to %BSP_VERSION%
)
SET BSP_ARCH=%1
SET KIT_VERSION=10.0.10586.0
SET HIVE_ROOT=%KITROOT%\CoreSystem\%KIT_VERSION%\%BSP_ARCH%
SET WIM_ROOT=%KITROOT%\CoreSystem\%KIT_VERSION%\%BSP_ARCH%
:: The following variables ensure the package is appropriately signed
SET SIGN_OEM=1
SET SIGN_WITH_TIMESTAMP=0

echo Environment set for %BSP_ARCH% Version:%BSP_VERSION%

:: Local project settings
SET PRJ_DIR=C:\IoTCoreOEMSDK
SET SRC_DIR=%PRJ_DIR%\Packages
SET OUTPUT_DIRECTORY=%PRJ_DIR%\Build\%BSP_ARCH%
SET PKG_OUTPUT=%OUTPUT_DIRECTORY%\pkgs

set PROMPT=IoTCore:%BSP_ARCH%:%BSP_VERSION%$_$P$G
TITLE IoTCore:%BSP_ARCH%:%BSP_VERSION%
:CLEANUP
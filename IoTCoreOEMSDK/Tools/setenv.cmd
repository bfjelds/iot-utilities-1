:: Run the following script from “Deployment and Imaging Tools Environment” as Admin.
@echo off
if [%1] == [/?] goto Usage
if [%1] == [-?] goto Usage
if [%1] == [] goto Usage
if NOT [%1] == [arm] ( if NOT [%1] == [x86] (
 echo Error: %1 not supported
 goto Usage 
)
)

:: Environment configurations
SET KIT_VERSION=10.0.10586.0

SET PATH=%KITSROOT%tools\bin\i386;%PATH%
SET AKROOT=%KITSROOT%
SET WPDKCONTENTROOT=%KITSROOT%
SET PKG_CONFIG_XML=%KITSROOT%Tools\bin\i386\pkggen.cfg.xml
if exist versioninfo.txt (
	SET /P BSP_VERSION=< versioninfo.txt
) else (
	SET BSP_VERSION=10.0.0.0
	echo Version Info not found. Defaulting to %BSP_VERSION%
)
SET BSP_ARCH=%1
SET HIVE_ROOT=%KITSROOT%CoreSystem\%KIT_VERSION%\%BSP_ARCH%
SET WIM_ROOT=%KITSROOT%CoreSystem\%KIT_VERSION%\%BSP_ARCH%
:: The following variables ensure the package is appropriately signed
SET SIGN_OEM=1
SET SIGN_WITH_TIMESTAMP=0

echo Environment set for %BSP_ARCH% Version:%BSP_VERSION%

:: Local project settings
SET PRJ_DIR=C:\IoTCoreOEMSDK
SET PKGSRC_DIR=%PRJ_DIR%\Packages
SET BLD_DIR=%PRJ_DIR%\Build\%BSP_ARCH%
SET PKGBLD_DIR=%BLD_DIR%\pkgs

set PROMPT=IoTCore %BSP_ARCH% %BSP_VERSION%$_$P$G
TITLE IoTCoreShell %BSP_ARCH% %BSP_VERSION%
goto End

:Usage
echo Usage: setenv arch 
echo    arch....... Required, arm/x86 
echo    [/?].............. Displays this usage string. 
echo    Example:
echo        setenv arm 

exit /b 1

:End
exit /b 0

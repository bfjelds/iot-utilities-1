@echo off
if [%1] == [/?] goto Usage
if [%1] == [-?] goto Usage
if [%1] == [] goto Usage
if NOT [%1] == [arm] ( if NOT [%1] == [x86] (
 echo Error: %1 not supported
 goto Usage 
)
)

REM Environment configurations
SET KIT_VERSION=10.0.10586.0

SET PATH=%KITSROOT%tools\bin\i386;%PATH%
SET AKROOT=%KITSROOT%
SET WPDKCONTENTROOT=%KITSROOT%
SET PKG_CONFIG_XML=%KITSROOT%Tools\bin\i386\pkggen.cfg.xml

SET BSP_ARCH=%1
SET HIVE_ROOT=%KITSROOT%CoreSystem\%KIT_VERSION%\%BSP_ARCH%
SET WIM_ROOT=%KITSROOT%CoreSystem\%KIT_VERSION%\%BSP_ARCH%
REM The following variables ensure the package is appropriately signed
SET SIGN_OEM=1
SET SIGN_WITH_TIMESTAMP=0


REM Local project settings
SET COMMON_DIR=%OEMSDK_ROOT%\Common
SET PRJ_DIR=%OEMSDK_ROOT%\Source-%1
SET PKGSRC_DIR=%PRJ_DIR%\Packages
SET BLD_DIR=%OEMSDK_ROOT%\Build\%BSP_ARCH%
SET PKGBLD_DIR=%BLD_DIR%\pkgs

if exist %PRJ_DIR%\versioninfo.txt (
	SET /P BSP_VERSION=< %PRJ_DIR%\versioninfo.txt
) else (
	SET BSP_VERSION=10.0.0.0
	echo Version Info not found. Defaulting to %BSP_VERSION%
)

set PROMPT=IoTCore %BSP_ARCH% %BSP_VERSION%$_$P$G
TITLE IoTCoreShell %BSP_ARCH% %BSP_VERSION%

echo Environment set for %BSP_ARCH% Version:%BSP_VERSION%
goto End

:Usage
echo Usage: setenv arch 
echo    arch....... Required, arm/x86 
echo    [/?]........Displays this usage string. 
echo    Example:
echo        setenv arm 

exit /b 1

:End
exit /b 0

:: Run the following script from “Deployment and Imaging Tools Environment” as Admin.
@echo off
:: Environment configurations
if NOT DEFINED BSP_VERSION (
	echo BSP_Version not defined. Setting it to default
)
SET BSP_VERSION=%1
echo %BSP_VERSION% > versioninfo.txt

set PROMPT=IoTCore:%BSP_ARCH%:%BSP_VERSION%$_$P$G
TITLE IoTCore:%BSP_ARCH%:%BSP_VERSION%
:: Run the following script from “Deployment and Imaging Tools Environment” as Admin.
@echo off

if NOT DEFINED PRJ_DIR (
	echo Environment not defined. Call setenv_arm.cmd or setenv_x86.cmd
	goto CLEANUP
)

rmdir "%OUTPUT_DIRECTORY%" /S /Q 

echo Build directories cleaned
:CLEANUP
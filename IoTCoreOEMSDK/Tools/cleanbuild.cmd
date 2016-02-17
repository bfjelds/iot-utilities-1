@echo off

if NOT DEFINED PRJ_DIR (
	echo Environment not defined. Call setenv
	goto CLEANUP
)

rmdir "%BLD_DIR%" /S /Q 

echo Build directories cleaned
:CLEANUP
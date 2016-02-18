@Echo off
REM 
REM  Call the DandISetEnv.bat
REM
IF /I %PROCESSOR_ARCHITECTURE%==x86 (
call "%ProgramFiles%\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\DandISetEnv.bat"
) ELSE IF /I %PROCESSOR_ARCHITECTURE%==amd64 (
call "%ProgramFiles(x86)%\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\DandISetEnv.bat"
)

SET OEMSDK_ROOT=%~dp0
REM Getting rid of the \Tools\ at the end
SET OEMSDK_ROOT=%OEMSDK_ROOT:~0,-7%
echo Setting the OEMSDK_ROOT to %OEMSDK_ROOT%
set PATH=%PATH%;%OEMSDK_ROOT%\Tools;
REM Change to Working directory
cd %OEMSDK_ROOT%\Tools
 
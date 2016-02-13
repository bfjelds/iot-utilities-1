@Echo off
REM 
REM  Call the DandISetEnv.bat
REM
call "C:\Program Files (x86)\Windows Kits\10\Assessment and Deployment Kit\Deployment Tools\DandISetEnv.bat"
set PATH=%PATH%;C:\IoTCoreOEMSDK\Tools;
REM Change to Working directory
cd C:\IoTCoreOEMSDK\Tools
 
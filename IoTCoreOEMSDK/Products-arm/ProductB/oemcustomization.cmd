:: OEM Customization Script file

:: Enable Administrator User
net user Administrator p@ssw0rd /active:yes

:: Applying Provisioning packages in order
provtool C:\OEMInstall\Provisioning\ProvSetA.ppkg
provtool C:\OEMInstall\Provisioning\ProvSetA.ppkg

:: Authoring Headless Configuration
:: call reg add HKEY_LOCAL_MACHINE\SYSTEM\currentcontrolset\control\wininit /v Headless /t REG_DWORD /d 1 /f

:: Enable Application Installation
call C:\Appinstall\AppInstall.bat

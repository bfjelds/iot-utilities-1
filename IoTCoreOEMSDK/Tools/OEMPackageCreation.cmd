:: Run the following script from “Deployment and Imaging Tools Environment” as Admin.
echo off

echo Creating OEM Packages
call createpkg.cmd %SRC_DIR%\oemappxpkg\OEMAppx.pkg.xml
call createpkg.cmd %SRC_DIR%\oemregpkg\OEMRegistry.pkg.xml
call createpkg.cmd %SRC_DIR%\oemdrvpkg\oemdrv.pkg.xml
call createpkg.cmd %SRC_DIR%\oemprovpkg\OEMSingleProv.pkg.xml
call createpkg.cmd %SRC_DIR%\oemprovpkg\OEMMultiProv.pkg.xml
call createpkg.cmd %SRC_DIR%\oempoppkg\POPDeviceInfo.pkg.xml
call createpkg.cmd %SRC_DIR%\oempoppkg\POPDeviceTargeting.pkg.xml
call createpkg.cmd %SRC_DIR%\oembsppkg\OEMDeviceLayout.pkg.xml
call createpkg.cmd %SRC_DIR%\oembsppkg\OEMDevicePlatform.pkg.xml
call createpkg.cmd %SRC_DIR%\oembsppkg\OEMSystemInformation.pkg.xml

:: Not including Product specific packages here (oemcustcmd)
:: Product specific packages built before image creation for corresponding product
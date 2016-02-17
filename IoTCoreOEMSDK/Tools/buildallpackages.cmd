@echo off

echo Creating all packages under %PKGSRC_DIR%

dir %PKGSRC_DIR%\*.pkg.xml /S /b > packagelist.txt

for /f "delims=" %%i in (packagelist.txt) do (
   echo Processing %%i
   call createpkg.cmd %%i
)

del packagelist.txt

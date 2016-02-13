:: Run the following script from “Deployment and Imaging Tools Environment” as Admin.
:: Run setenv before running this script
echo off

echo Creating Images

call createimage.cmd ProductA Retail
call createimage.cmd ProductA Production
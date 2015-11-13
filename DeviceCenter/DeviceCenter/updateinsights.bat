echo %1
echo %2
if %2=="" goto end
echo Overwriting ApplicationInsights.config
copy /Y %1\ApplicationInsights.txt %1\ApplicationInsights.config
:end

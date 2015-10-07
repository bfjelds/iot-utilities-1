Before executing the sample UI application:

	1) In %SolutionDir%\COMOnboarding edit register_COM_Component.reg with the path for your project and run "reg import register_COM_Component.reg"
	2) From %SolutionDir%\Debug run regsvr32 COMOnboardingProxy.dll

When I have time I'll include those steps in the project's post build.
del /F /Q /S \\COMPUTESERV1\Shared\MidaxTester\*.*
del /F /Q /S \\COMPUTESERV2\Shared\MidaxTester\*.*
del /F /Q /S \\WEBSERV1\Shared\MidaxTester\*.*
xcopy "../MidaxTester/bin/Release" "\\COMPUTESERV1\Shared\MidaxTester\" /Y /E
xcopy "../MidaxTester/bin/Release" "\\COMPUTESERV2\Shared\MidaxTester\" /Y /E
xcopy "../MidaxTester/bin/Release" "\\WEBSERV1\Shared\MidaxTester\" /Y /E
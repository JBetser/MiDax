del /F /Q /S ..\WebDebug\*.*
md ..\WebDebug
xcopy "../WebUI" "..\WebDebug\" /Y /E
xcopy "../WebTest" "..\WebDebug\" /Y /E
copy /Y web_debug.config ..\WebDebug\web.config
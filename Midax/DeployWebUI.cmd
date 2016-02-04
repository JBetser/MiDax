del /F /Q /S W:\*.*
xcopy "../WebUI" "W:\" /Y /E
md W:\UAT
xcopy "../WebDebug" "W:\UAT\" /Y /E
copy /Y ..\MidaxTrader\Init.js W:\UAT\jscript\Init.js
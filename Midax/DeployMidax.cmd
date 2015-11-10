del /F /Q /S M:\db\node\*.*
del /F /Q /S M:\db\registry\*.*
rd /S /Q M:\db\node
rd /S /Q M:\db\registry
del /F /Q M:\*.*
md M:\db\registry
md M:\db\node
xcopy "./bin/Release" "M:\" /Y /E
xcopy "./db/node" "M:\db\node" /Y /E
xcopy "./db/registry" "M:\db\registry" /Y /E
copy /Y config.grid M:\
copy /Y ..\packages\NLapack.1.0.14\lib\msvcr110.dll M:\
copy /Y ..\packages\NLapack.1.0.14\lib\msvcp110.dll M:\
del /F /Q /S M:\db\node\*.*
del /F /Q /S M:\db\registry\*.*
rd /S /Q M:\db\node
rd /S /Q M:\db\registry
del /F /Q M:\*.*
md M:\db\registry
md M:\db\node
xcopy "./bin/Debug" "M:\" /Y /E
xcopy "./db/node" "M:\db\node" /Y /E
xcopy "./db/registry" "M:\db\registry" /Y /E
copy /Y config.grid M:\
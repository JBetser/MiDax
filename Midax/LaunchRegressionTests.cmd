@ECHO OFF
SET FOLDER=..\MidaxTester\bin\Release\
SET TESTER=MidaxTester.exe
IF EXIST %FOLDER%%TESTER% (
cd %FOLDER%
call %TESTER%
cd %~dp0
) ELSE (
msg "%username%" You need to build the solution in Release mode to run the tests
)

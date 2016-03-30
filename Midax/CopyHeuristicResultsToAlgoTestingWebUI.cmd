@echo off
cd /d %~dp0
cd ..\MidaxTester\expected_results
for /r . %%A in (heuristicgen_*.csv) DO CALL :loopbody %%~nxA
cd ..\..\Midax
msg "%username%" Copy succeeded 
GOTO :EOF
:loopbody
SET curcsv=%1
echo %curcsv%
copy /Y %curcsv% ..\..\AlgoTesting\algotest_%curcsv:~13%
IF NOT ERRORLEVEL 1 GOTO :EOF
msg "%username%" Copy failed
exit
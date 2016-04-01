@ECHO OFF
SET TESTER=C:\Shared\MidaxTester\MidaxTester.exe
start PsExec -accepteula \\COMPUTESERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-08 -FULL
start PsExec -accepteula \\COMPUTESERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-11 -FULL
start PsExec -accepteula \\COMPUTESERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-12 -FULL
start PsExec -accepteula \\COMPUTESERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-13 -FULL
start PsExec -accepteula \\COMPUTESERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-14 -FULL
start PsExec -accepteula \\COMPUTESERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-15 -FULL
start PsExec -accepteula \\COMPUTESERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-16 -FULL
start PsExec -accepteula \\COMPUTESERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-17 -FULL
start PsExec -accepteula \\COMPUTESERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-18 -FULL
start PsExec -accepteula \\COMPUTESERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-19 -FULL
start PsExec -accepteula \\COMPUTESERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-20 -FULL
start PsExec -accepteula \\COMPUTESERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-21 -FULL
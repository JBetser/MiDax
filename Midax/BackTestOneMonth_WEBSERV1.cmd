@ECHO OFF
SET TESTER=C:\Shared\MidaxTester\MidaxTester.exe
start PsExec -accepteula \\WEBSERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-01 -FULL
start PsExec -accepteula \\WEBSERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-02 -FULL
start PsExec -accepteula \\WEBSERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-03 -FULL
start PsExec -accepteula \\WEBSERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-04 -FULL
start PsExec -accepteula \\WEBSERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-05 -FULL
start PsExec -accepteula \\WEBSERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-06 -FULL
start PsExec -accepteula \\WEBSERV1 %TESTER% -G -FROMDB -TODB -%2 -DATE%1-07 -FULL
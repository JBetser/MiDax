Generate real-time trading signals from equity indices
It books automatic trades to an IG Market account. Visit https://www.ig.com to create an account,
or adapt the code to use a different broker api.

IMPORTANT: This is for educational purposes only. Always use with a DEMO account.
IMPORTANT: If you decide to trade with a Live account, trade at your own risk, your losses can exceed your deposits.

IG api uses the lightstreamer protocol in C#.
You will need Visual Studio 2012 Professional and ZeroC Ice 3.5 to be able to build the solution.
To test your algorithms, use MidaxTester.
To visualize your algorithm performance, use the web application locally via the WebDebug project. You will need to follow the instructions from WebUI_README.txt. 
To trade, use MidaxTrader (console application) or Midax (windows service).
The first time you run your application, you need to run it with administator privileges in order to create the Midax event source for the windows logging (accessible via EventViewer)

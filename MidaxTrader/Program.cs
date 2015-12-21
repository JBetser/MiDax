using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MidaxLib;

namespace MidaxTrader
{
    class Program
    {
        static ManualResetEvent _pauseEvent = new ManualResetEvent(true);
    
        static void Main(string[] args)
        {
            Config.Settings = new Dictionary<string, string>();
            Config.Settings["APP_NAME"] = "Midax";
            Config.Settings["API_KEY"] = "8d341413c2eae2c35bb5b47a594ef08ae18cb3b7";
            Config.Settings["USER_NAME"] = "ksbitlsoftdemo";
            Config.Settings["PASSWORD"] = "Kotik0483";
            Config.Settings["PUBLISHING_CONTACTPOINT"] = "192.168.1.26";
            DateTime start = DateTime.Now;
            Config.Settings["PUBLISHING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 6, 45, 0);
            Config.Settings["PUBLISHING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 18, 0, 0);
            Config.Settings["TRADING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 8, 0, 0);
            Config.Settings["TRADING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 17, 0, 0);
            Config.Settings["TRADING_CLOSING_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 16, 55, 0);
            Config.Settings["TRADING_MODE"] = "PRODUCTION";
            //Config.Settings["TRADING_SIGNAL"] = "MacD_2_10_IX.D.DAX.DAILY.IP";
            Config.Settings["TRADING_LIMIT_PER_BP"] = "10";
            Config.Settings["TRADE_EXPIRY_DAYS"] = "1";
            Config.Settings["TRADE_CURRENCY"] = "GBP";
            
            MarketDataConnection.Instance.Connect(null);

            MarketData index = new MarketData("DAX:IX.D.DAX.DAILY.IP");
            Model model = new ModelTest(index, new List<MarketData>());
            Console.WriteLine("Starting signals...");
            model.StartSignals();
            Console.WriteLine("Trading...");
            
            _pauseEvent.WaitOne(Timeout.Infinite);
        }
    }
}

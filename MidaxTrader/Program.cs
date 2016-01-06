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
            DateTime start = DateTime.Now;
            Config.Settings["PUBLISHING_CSV"] = string.Format("..\\..\\..\\TradingActivity\\trading_{0}_{1}_{2}.csv", start.Day, start.Month, start.Year);
            Config.Settings["PUBLISHING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 11, 45, 0);
            Config.Settings["PUBLISHING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 21, 0, 0);
            Config.Settings["TRADING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 13, 0, 0);
            Config.Settings["TRADING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 21, 0, 0);
            Config.Settings["TRADING_CLOSING_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 20, 55, 0);
            Config.Settings["TRADING_MODE"] = "PRODUCTION";
            Config.Settings["TRADING_SIGNAL"] = "MacD_2_10_IX.D.SPTRD.DAILY.IP";
            Config.Settings["TRADING_LIMIT_PER_BP"] = "10";
            Config.Settings["TRADING_CURRENCY"] = "GBP";
            
            MarketDataConnection.Instance.Connect(null);

            MarketData snp = new MarketData("SNP:IX.D.SPTRD.DAILY.IP");
            Model modelSnp = new ModelMacDTest(snp, new List<MarketData>(), new List<MarketData>());
            Console.WriteLine("Starting signals...");
            modelSnp.StartSignals();
            
            Console.WriteLine("Trading...");
            
            _pauseEvent.WaitOne(Timeout.Infinite);
        }
    }
}

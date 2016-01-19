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
            Config.Settings["DB_CONTACTPOINT"] = "192.168.1.26";
            //Config.Settings["PUBLISHING_CSV"] = string.Format("..\\..\\..\\TradingActivity\\trading_{0}_{1}_{2}.csv", start.Day, start.Month, start.Year);
            Config.Settings["PUBLISHING_START_TIME"] = string.Format("{0}:{1}:{2}", 12, 45, 0);
            Config.Settings["PUBLISHING_STOP_TIME"] = string.Format("{0}:{1}:{2}", 22, 0, 0);
            Config.Settings["TRADING_START_TIME"] = string.Format("{0}:{1}:{2}", 13, 0, 0);
            Config.Settings["TRADING_STOP_TIME"] = string.Format("{0}:{1}:{2}", 23, 30, 0);
            Config.Settings["TRADING_CLOSING_TIME"] = string.Format("{0}:{1}:{2}", 23, 15, 0);
            Config.Settings["TRADING_MODE"] = "PRODUCTION";
            Config.Settings["TRADING_SIGNAL"] = "MacD_2_10_IX.D.SPTRD.DAILY.IP";
            Config.Settings["TRADING_LIMIT_PER_BP"] = "10";
            Config.Settings["TRADING_CURRENCY"] = "GBP";
            
            MarketDataConnection.Instance.Connect(null);

            var snp = new MarketData("DAX:IX.D.DAX.DAILY.IP");
            var otherIndices = new List<MarketData>();
            //otherIndices.Add(new MarketData("SNP:IX.D.SNPTRD.DAILY.IP"));
            //otherIndices.Add(new MarketData("CAC:IX.D.CAC.DAILY.IP"));
            Model modelSnp = new ModelMacDTest(snp, new List<MarketData>(), null, otherIndices);
            Console.WriteLine("Starting signals...");

            modelSnp.StartSignals();
            modelSnp.StopSignals();
            
            Console.WriteLine("Trading...");
            
            _pauseEvent.WaitOne(Timeout.Infinite);
        }
    }
}

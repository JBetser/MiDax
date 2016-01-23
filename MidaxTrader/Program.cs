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
            Config.Settings["PUBLISHING_START_TIME"] = string.Format("{0}:{1}:{2}", 6, 45, 0);
            Config.Settings["PUBLISHING_STOP_TIME"] = string.Format("{0}:{1}:{2}", 22, 0, 0);
            Config.Settings["TRADING_START_TIME"] = string.Format("{0}:{1}:{2}", 8, 0, 0);
            Config.Settings["TRADING_STOP_TIME"] = string.Format("{0}:{1}:{2}", 17, 0, 0);
            Config.Settings["TRADING_CLOSING_TIME"] = string.Format("{0}:{1}:{2}", 16, 30, 0);
            Config.Settings["TRADING_MODE"] = "PRODUCTION";
            Config.Settings["TRADING_SIGNAL"] = "MacD_2_10_IX.D.SPTRD.DAILY.IP";
            Config.Settings["TRADING_LIMIT_PER_BP"] = "10";
            Config.Settings["TRADING_CURRENCY"] = "GBP";
            
            var otherIndices = new List<MarketData>();
            otherIndices.Add(new MarketData("SNP:IX.D.SPTRD.DAILY.IP"));
            otherIndices.Add(new MarketData("CAC:IX.D.CAC.DAILY.IP"));
            var models = new List<Model>();
            var macD = new ModelMacD(new MarketData("DAX:IX.D.DAX.DAILY.IP"));
            models.Add(macD);
            models.Add(new ModelANN(macD, new List<MarketData>(), new MarketData("VIX2:IN.D.VIX.MONTH2.IP"), otherIndices));
            models.Add(new ModelMacDCascade(macD));
            models.Add(new ModelMole(macD));
            Console.WriteLine("Starting signals...");
            var trader = new Trader(models);
            trader.Start();            
            Console.WriteLine("Trading...");
            //_pauseEvent.WaitOne(Timeout.Infinite);
            System.Threading.Thread.Sleep(10000);
            trader.Stop();
            Console.WriteLine("Trading stopped");
        }
    }
}

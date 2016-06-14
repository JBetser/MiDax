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
        static AutoResetEvent _stopEvent;
    
        static void Main(string[] args)
        {
            Config.Settings = new Dictionary<string, string>();
            Config.Settings["APP_NAME"] = "Midax";
            Config.Settings["IG_KEY"] = "8d341413c2eae2c35bb5b47a594ef08ae18cb3b7";
            Config.Settings["IG_USER_NAME"] = "ksbitlsoftdemo";
            Config.Settings["IG_PASSWORD"] = "Kotik0483";
            Config.Settings["DB_CONTACTPOINT"] = "192.168.1.26";
            var startDate = DateTime.Now;
            Config.Settings["PUBLISHING_CSV"] = string.Format("..\\..\\..\\TradingActivity\\trading_{0}_{1}_{2}.csv", startDate.Day, startDate.Month, startDate.Year);
            Config.Settings["PUBLISHING_START_TIME"] = string.Format("{0}:{1}:{2}", 6, 45, 0);
            Config.Settings["PUBLISHING_STOP_TIME"] = string.Format("{0}:{1}:{2}", 23, 30, 0);
            Config.Settings["TRADING_START_TIME"] = string.Format("{0}:{1}:{2}", 8, 0, 0);
            Config.Settings["TRADING_STOP_TIME"] = string.Format("{0}:{1}:{2}", 23, 0, 0);
            Config.Settings["TRADING_CLOSING_TIME"] = string.Format("{0}:{1}:{2}", 22, 45, 0);
            Config.Settings["TRADING_MODE"] = "PRODUCTION";
            Config.Settings["TRADING_SIGNAL"] = "MacDCas_30_90_200_IX.D.DAX.DAILY.IP";
            Config.Settings["TRADING_LIMIT_PER_BP"] = "10";
            Config.Settings["TRADING_CURRENCY"] = "GBP";
                     
            var otherIndices = new List<MarketData>();
            otherIndices.Add(new MarketData("DOW:IX.D.DOW.DAILY.IP"));
            otherIndices.Add(new MarketData("CAC:IX.D.CAC.DAILY.IP"));
            var models = new List<Model>();
            var macD_10_30_90 = new ModelMacDV(new MarketData("DAX:IX.D.DAX.DAILY.IP"), 10, 30, 90);
            models.Add(macD_10_30_90);
            models.Add(new ModelANN(macD_10_30_90, null, null, otherIndices));
            models.Add(new ModelMacDCascade(macD_10_30_90));
            //models.Add(new ModelMole(macD));
            Console.WriteLine("Starting signals...");
            var trader = new Trader(models, onShutdown);
            trader.Init(Config.GetNow);            
            Console.WriteLine("Trading...");
            _stopEvent = new AutoResetEvent(false);
            _stopEvent.WaitOne();
            Console.WriteLine("Trading stopped");
        }

        static void onShutdown()
        {
            _stopEvent.Set();
        }

        static void connectionLostCallback(object state)
        {
        }
    }
}

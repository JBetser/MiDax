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
            Config.Settings["DB_CONTACTPOINT"] = "192.168.1.25";
            var startDate = DateTime.Now;
            Config.Settings["PUBLISHING_CSV"] = string.Format("..\\..\\..\\TradingActivity\\trading_{0}_{1}_{2}.csv", startDate.Day, startDate.Month, startDate.Year);
            Config.Settings["PUBLISHING_START_TIME"] = string.Format("{0}:{1}:{2}", 0, 0, 50);
            Config.Settings["PUBLISHING_STOP_TIME"] = string.Format("{0}:{1}:{2}", 23, 59, 59);
            Config.Settings["TRADING_START_TIME"] = string.Format("{0}:{1}:{2}", 0, 1, 0);
            Config.Settings["TRADING_STOP_TIME"] = string.Format("{0}:{1}:{2}", 23, 59, 50);
            Config.Settings["TRADING_CLOSING_TIME"] = string.Format("{0}:{1}:{2}", 23, 59, 55);
            Config.Settings["TRADING_MODE"] = "PRODUCTION";
            Config.Settings["TRADING_SIGNAL"] = "Rob_1_48_20_15_CS.D.EURUSD.TODAY.IP,Rob_1_48_20_15_CS.D.GBPUSD.TODAY.IP,Rob_1_48_20_15_IX.D.DOW.DAILY.IP";
            Config.Settings["TRADING_LIMIT_PER_BP"] = "5";
            Config.Settings["TRADING_CURRENCY"] = "GBP";
            Config.Settings["INDEX_DOW"] = "DOW:IX.D.DOW.DAILY.IP";
            Config.Settings["FX_EURUSD"] = "EURUSD:CS.D.EURUSD.TODAY.IP";
            Config.Settings["FX_GBPUSD"] = "GBPUSD:CS.D.GBPUSD.TODAY.IP";
            Config.Settings["INDEX_ICEDOW"] = "DOW:IceConnection.DJI";
            Config.Settings["CALENDAR_PATH"] = "C:\\Shared\\MidaxTester\\Calendar";
            Config.Settings["TIME_GMT"] = "1";

            var index = IceStreamingMarketData.Instance;
            var models = new List<Model>();
            var dow = new MarketData(Config.Settings["INDEX_DOW"]);
            var eurusd = new MarketData(Config.Settings["FX_EURUSD"]);
            var gbpusd = new MarketData(Config.Settings["FX_GBPUSD"]);
            var icedow = new MarketData(Config.Settings["INDEX_ICEDOW"]);
            var robinhood_icedow = new ModelRobinHood(dow, 1, 48, 0, 20, 15, new IndicatorVolume(icedow, 60));
            var robinhood_eurusd = new ModelRobinHood(eurusd, 1, 48, 0, 20, 15);
            var robinhood_gbpusd = new ModelRobinHood(gbpusd, 1, 48, 0, 20, 15);
            models.Add(robinhood_eurusd);
            models.Add(robinhood_gbpusd);
            models.Add(robinhood_icedow);
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

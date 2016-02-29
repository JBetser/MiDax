using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidaxLib;

namespace DBImporter
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime start = DateTime.Parse(args[0]);
            DateTime end = DateTime.Parse(args[1]);

            Dictionary<string, string> dicSettings = new Dictionary<string, string>();
            dicSettings["APP_NAME"] = "Midax";
            dicSettings["DB_CONTACTPOINT"] = "192.168.1.26";
            dicSettings["REPLAY_MODE"] = "DB";
            dicSettings["REPLAY_POPUP"] = "1";
            dicSettings["TRADING_MODE"] = "REPLAY";
            Config.Settings = dicSettings;

            while (start <= end)
            {
                Config.Settings["PUBLISHING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 5, 45, 0);
                Config.Settings["PUBLISHING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 22, 0, 0);
                Config.Settings["TRADING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 7, 0, 0);
                Config.Settings["TRADING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 21, 0, 0);
                Config.Settings["TRADING_CLOSING_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 17, 0, 0);
                Config.Settings["PUBLISHING_CSV"] = string.Format("..\\..\\..\\MktData\\mktdata_{0}_{1}_{2}.csv", start.Day, start.Month, start.Year);

                MarketDataConnection.Instance.Connect(null);

                var indexDAX = new MarketData("DAX:IX.D.DAX.DAILY.IP");
                indexDAX.Subscribe(OnUpdate, null);
                var indexCAC = new MarketData("CAC:IX.D.CAC.DAILY.IP");
                indexCAC.Subscribe(OnUpdate, null);
                var indexDOW = new MarketData("DOW:IX.D.DOW.DAILY.IP");
                indexDOW.Subscribe(OnUpdate, null);
                MarketDataConnection.Instance.StartListening();
                MarketDataConnection.Instance.StopListening();

                indexDAX.Clear();
                indexCAC.Clear();
                indexDOW.Clear();

                do
                {
                    start = start.AddDays(1);
                }
                while (start.DayOfWeek == DayOfWeek.Saturday || start.DayOfWeek == DayOfWeek.Sunday);
            }
        }

        static void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
        }
    }
}

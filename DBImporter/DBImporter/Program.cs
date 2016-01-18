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

                var index = new MarketData("DAX:IX.D.DAX.DAILY.IP");
                var indicator = new IndicatorWMA(index, 2);
                indicator.Subscribe(OnUpdateIndicatora);  
                MarketDataConnection.Instance.StartListening();
                MarketDataConnection.Instance.StopListening();
                var indicatorLevels = new List<ILevelPublisher>();
                indicatorLevels.Add(new IndicatorLevelPivot(index));
                indicatorLevels.Add(new IndicatorLevelR1(index));
                indicatorLevels.Add(new IndicatorLevelR2(index));
                indicatorLevels.Add(new IndicatorLevelR3(index));
                indicatorLevels.Add(new IndicatorLevelS1(index));
                indicatorLevels.Add(new IndicatorLevelS2(index));
                indicatorLevels.Add(new IndicatorLevelS3(index));
                foreach (var ind in indicatorLevels)
                    ind.Publish(Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]));
                PublisherConnection.Instance.Close();
                indicator.Unsubscribe(OnUpdateIndicatora);
                indicator.Clear();
                index.Clear();

                do
                {
                    start = start.AddDays(1);
                }
                while (start.DayOfWeek == DayOfWeek.Saturday || start.DayOfWeek == DayOfWeek.Sunday);
            }
        }

        static void OnUpdateIndicatora(MarketData mktData, DateTime updateTime, Price value)
        {
        }
    }
}

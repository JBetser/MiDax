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

                var lstIndices = new List<MarketData>();
                var indexDAX = new MarketData("DAX:IX.D.DAX.DAILY.IP");
                indexDAX.Subscribe(OnUpdate, null);
                var indexCAC = new MarketData("CAC:IX.D.CAC.DAILY.IP");
                indexCAC.Subscribe(OnUpdate, null);
                var indexDOW = new MarketData("DOW:IX.D.DOW.DAILY.IP");
                indexDOW.Subscribe(OnUpdate, null);
                lstIndices.Add(indexDAX);
                lstIndices.Add(indexCAC);
                lstIndices.Add(indexDOW);

                var lstIndicators = new List<IndicatorLevel>();
                foreach (var index in lstIndices){
                    lstIndicators.Add(new IndicatorLow(index));
                    lstIndicators.Add(new IndicatorHigh(index));
                    lstIndicators.Add(new IndicatorCloseBid(index));
                    lstIndicators.Add(new IndicatorCloseOffer(index));
                    lstIndicators.Add(new IndicatorLevelPivot(index));
                    lstIndicators.Add(new IndicatorLevelR1(index));
                    lstIndicators.Add(new IndicatorLevelR2(index));
                    lstIndicators.Add(new IndicatorLevelR3(index));
                    lstIndicators.Add(new IndicatorLevelS1(index));
                    lstIndicators.Add(new IndicatorLevelS2(index));
                    lstIndicators.Add(new IndicatorLevelS3(index));                    
                }
                foreach(var indicator in lstIndicators)
                    indicator.Subscribe(OnUpdate, null);

                MarketDataConnection.Instance.StartListening();

                foreach (var indicator in lstIndicators)
                    indicator.Publish(Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]));
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

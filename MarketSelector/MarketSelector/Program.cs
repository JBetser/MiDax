using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidaxLib;

namespace MarketSelector
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime start = DateTime.Parse(args[0]);
            DateTime end = DateTime.Parse(args[1]);
            
            Dictionary<string, string> dicSettings = new Dictionary<string, string>();
            dicSettings["APP_NAME"] = "Midax";
            dicSettings["TIMESERIES_MAX_RECORD_TIME_HOURS"] = "12";
            dicSettings["LIMIT"] = "10";
            dicSettings["REPLAY_MODE"] = "CSV";
            dicSettings["REPLAY_POPUP"] = "1";
            dicSettings["TRADING_MODE"] = "SELECT";
            Config.Settings = dicSettings;

            while (start <= end)
            {
                List<string> mktdataFiles = new List<string>();
                mktdataFiles.Add(string.Format("..\\..\\..\\..\\DBImporter\\MktData\\mktdata_{0}_{1}_{2}.csv", start.Day, start.Month, start.Year));
                Config.Settings["REPLAY_CSV"] = Config.TestList(mktdataFiles);
                Config.Settings["PUBLISHING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 6, 45, 0);
                Config.Settings["PUBLISHING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 18, 0, 0);
                Config.Settings["TRADING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 8, 0, 0);
                Config.Settings["TRADING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 17, 0, 0);
                Config.Settings["TRADING_CLOSING_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 16, 55, 0);
                Config.Settings["PUBLISHING_CSV"] = string.Format("..\\..\\..\\MktSelectorData\\mktselectdata_{0}_{1}_{2}.csv", start.Day, start.Month, start.Year);

                MarketDataConnection.Instance.Connect(null);
                MarketData index = new MarketData("DAX:IX.D.DAX.DAILY.IP");
                index.Subscribe(OnUpdateMktData,null);
                MarketDataConnection.Instance.StartListening();
                MarketDataConnection.Instance.StopListening();
                index.Unsubscribe(OnUpdateMktData,null);
                index.Clear();

                do
                {
                    start = start.AddDays(1);
                }
                while (start.DayOfWeek == DayOfWeek.Saturday || start.DayOfWeek == DayOfWeek.Sunday);
            }
        }

        static void OnUpdateMktData(MarketData mktData, DateTime updateTime, Price value)
        {
        }
    }
}

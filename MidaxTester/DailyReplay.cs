using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidaxLib;

namespace MidaxTester
{
    public class DailyReplay
    {
        public static void Run(List<DateTime> dates, bool generate = false, bool publish_to_db = false)
        {
            Config.Settings = new Dictionary<string, string>();
            Config.Settings["TRADING_MODE"] = "REPLAY";
            Config.Settings["REPLAY_MODE"] = "CSV";
            Config.Settings["TRADING_LIMIT_PER_BP"] = "10";
            Config.Settings["TIMESERIES_MAX_RECORD_TIME_HOURS"] = "12";
            Config.Settings["TRADING_SIGNAL"] = "MacD_10_60_IX.D.DAX.DAILY.IP";

            string action = generate ? "Generating" : "Testing";
            
            List<MarketData> stocks = new List<MarketData>();
            List<MarketData> volIndices = new List<MarketData>();
            //volIndices.Add(new MarketData("IN.D.VIX.MONTH2.IP"));
            //volIndices.Add(new MarketData("IN.D.VIX.MONTH3.IP"));
            foreach (var test in dates)
            {
                List<string> mktdataFiles = new List<string>();
                mktdataFiles.Add(string.Format("..\\..\\expected_results\\mktdata_{0}_{1}_{2}.csv", test.Day, test.Month, test.Year));
                Config.Settings["REPLAY_CSV"] = Config.TestList(mktdataFiles);
                Config.Settings["PUBLISHING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 6, 15, 0);
                Config.Settings["PUBLISHING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 22, 0, 0);
                Config.Settings["TRADING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 7, 30, 0);
                Config.Settings["TRADING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 20, 30, 0);
                Config.Settings["TRADING_CLOSING_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 17, 0, 0);
                if (generate)
                {
                    if (!publish_to_db)
                        Config.Settings["PUBLISHING_CSV"] = string.Format("..\\..\\expected_results\\mktdatagen_{0}_{1}_{2}.csv", test.Day, test.Month, test.Year);
                }

                MarketDataConnection.Instance.Connect(null);
                var index = new MarketData("DAX:IX.D.DAX.DAILY.IP");
                ModelMacDTest model = new ModelMacDTest(index, stocks, volIndices);
                /*
                var indicators = new List<Indicator>();
                indicators.Add(new IndicatorWMA(index, 2));
                indicators.Add(new IndicatorWMA(index, 10));
                indicators.Add(new IndicatorWMA(index, 60));
                ModelANN model = new ModelANN("WMA_4_2", index, stocks, volIndices, indicators);*/
                Console.WriteLine(action + string.Format(" the daily record {0}-{1}-{2}...", test.Year, test.Month, test.Day));
                model.StartSignals();
                model.StopSignals();
            }
        }
    }
}

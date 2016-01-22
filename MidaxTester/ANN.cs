using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidaxLib;

namespace MidaxTester
{
    public class ANN
    {
        public static void Run(List<DateTime> dates, bool generate = false, bool publish_to_db = false)
        {
            Config.Settings = new Dictionary<string, string>();
            Config.Settings["TRADING_MODE"] = "REPLAY";
            Config.Settings["REPLAY_MODE"] = "CSV";
            //Config.Settings["DB_CONTACTPOINT"] = "192.168.1.26";
            Config.Settings["TRADING_LIMIT_PER_BP"] = "10";
            Config.Settings["TIMESERIES_MAX_RECORD_TIME_HOURS"] = "12";
            //Config.Settings["TRADING_SIGNAL"] = "";

            string action = generate ? "Generating" : "Testing";

            List<MarketData> stocks = new List<MarketData>();
            foreach (var test in dates)
            {
                List<string> mktdataFiles = new List<string>();
                mktdataFiles.Add(string.Format("..\\..\\expected_results\\ann_{0}_{1}_{2}.csv", test.Day, test.Month, test.Year));
                Config.Settings["REPLAY_CSV"] = Config.TestList(mktdataFiles);
                Config.Settings["PUBLISHING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 6, 45, 0);
                Config.Settings["PUBLISHING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 10, 30, 0);
                Config.Settings["TRADING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 8, 0, 0);
                Config.Settings["TRADING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 10, 0, 0);
                Config.Settings["TRADING_CLOSING_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 9, 30, 0);
                if (generate)
                {
                    if (!publish_to_db)
                        Config.Settings["PUBLISHING_CSV"] = string.Format("..\\..\\expected_results\\anngen_{0}_{1}_{2}.csv", test.Day, test.Month, test.Year);
                }

                MarketDataConnection.Instance.Connect(null);
                var models = new List<Model>();
                models.Add(new ModelMacDTest(new MarketData("DAX:IX.D.DAX.DAILY.IP"), 2, 10, 60));
                List<MarketData> otherIndices = new List<MarketData>();
                otherIndices.Add(new MarketData("SNP:IX.D.SPTRD.DAILY.IP"));
                otherIndices.Add(new MarketData("CAC:IX.D.CAC.DAILY.IP"));
                models.Add(new ModelANN((ModelMacD)models[0], new List<MarketData>(), new MarketData("VIX2:IN.D.VIX.MONTH2.IP"), otherIndices));
                Console.WriteLine(action + string.Format(" the ANN daily record {0}-{1}-{2}...", test.Year, test.Month, test.Day));
                foreach (var model in models)
                    model.StartSignals(false);
                MarketDataConnection.Instance.StartListening();
                foreach (var model in models)
                    model.StopSignals(false);
                MarketDataConnection.Instance.StopListening();
                PublisherConnection.Instance.Close();
            }
        }
    }
}

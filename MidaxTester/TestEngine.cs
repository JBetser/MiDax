using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MidaxLib;

namespace MidaxTester
{
    class TestEngine
    {
        List<MarketData> _stocks = new List<MarketData>();
        string _testName;
        List<DateTime> _dates;
        bool _publish_to_db;
        bool _fullday;
        static bool _generate;
        static Trader _trader;
        static AutoResetEvent _stopEvent;

        public Dictionary<string, string> Settings { get { return Config.Settings; } }

        public TestEngine(string testName, List<DateTime> dates, bool generate = false, bool generate_from_db = false, bool publish_to_db = false, bool use_uat_db = false, bool fullday = false)
        {
            _testName = testName;
            _dates = dates;
            _publish_to_db = publish_to_db;
            _fullday = fullday;
            Config.Settings = new Dictionary<string, string>();
            if (use_uat_db)
                Config.Settings["TRADING_MODE"] = "REPLAY_UAT";
            else
                Config.Settings["TRADING_MODE"] = "REPLAY";
            Config.Settings["REPLAY_MODE"] = generate_from_db ? "DB" : "CSV";
            if (generate_from_db)
                Config.Settings["DB_CONTACTPOINT"] = "192.168.1.25";
            Config.Settings["TRADING_LIMIT_PER_BP"] = "10";
            Config.Settings["CALENDAR_PATH"] = @"C:\Shared\MidaxTester\Calendar";

            _generate = generate;                     
        }

        public void Run(List<Model> models)
        {            
            string action = _generate ? "Generating" : "Testing";   
            foreach (var test in _dates)
            {
                List<string> mktdataFiles = new List<string>();
                mktdataFiles.Add(string.Format("..\\..\\..\\DBImporter\\MktData\\mktdata_{0}_{1}_{2}.csv", test.Day, test.Month, test.Year));
                Config.Settings["REPLAY_CSV"] = Config.TestList(mktdataFiles);
                Config.Settings["EXPECTEDRESULTS_CSV"] = string.Format("..\\..\\expected_results\\{3}_{0}_{1}_{2}.csv", test.Day, test.Month, test.Year, _testName);
                if (_fullday)
                {
                    Config.Settings["PUBLISHING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 0, 5, 0);
                    Config.Settings["PUBLISHING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 23, 55, 0);
                    Config.Settings["TRADING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 8, 0, 0);
                    Config.Settings["TRADING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 21, 30, 0);
                    Config.Settings["TRADING_CLOSING_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 21, 0, 0);
                    Config.Settings["TIMESERIES_MAX_RECORD_TIME_HOURS"] = "12";
                }
                else
                {
                    Config.Settings["PUBLISHING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 14, 30, 0);
                    Config.Settings["PUBLISHING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 16, 15, 0);
                    Config.Settings["TRADING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 14, 30, 0);
                    Config.Settings["TRADING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 16, 10, 0);
                    Config.Settings["TRADING_CLOSING_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", test.Year, test.Month, test.Day, 16, 5, 0);
                    Config.Settings["TIMESERIES_MAX_RECORD_TIME_HOURS"] = "2";
                }

                if (_generate)
                {
                    if (!_publish_to_db)
                        Config.Settings["PUBLISHING_CSV"] = string.Format("..\\..\\expected_results\\{3}gen_{0}_{1}_{2}.csv", test.Day, test.Month, test.Year, _testName);
                }

                Console.WriteLine(string.Format("{0} the {1} daily record {2}-{3}-{4}...", action, _testName, test.Year, test.Month, test.Day));
                MarketDataConnection.Instance.Connect(null);
                _trader = new Trader(models, onShutdown);
                if (_generate)
                {
                    _trader.Start();
                    _trader.Stop();
                }
                else
                {
                    _trader.Init(GetNow);
                    _stopEvent = new AutoResetEvent(false);
                    _stopEvent.WaitOne();
                }
            }
        }

        static DateTime GetNow()
        {
            return Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]).AddMinutes(-2);
        }

        static void onShutdown()
        {
            if (!_generate)
            {
                if (ReplayTester.Instance.NbProducedTrades != ReplayTester.Instance.NbExpectedTrades)
                    _trader.ProcessError(string.Format("the model did not produced the expected number of trades. It produced {0} trades instead of {1} expected",
                                                    ReplayTester.Instance.NbProducedTrades, ReplayTester.Instance.NbExpectedTrades));
            }
            _stopEvent.Set();
        }
    }
}

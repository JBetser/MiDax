using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using Lightstreamer.DotNet.Client;

namespace MidaxLib
{
    public class ReplayUpdateInfo : IUpdateInfo
    {
        Dictionary<string, string> _itemData = new Dictionary<string, string>();
        string _name;
        string _id;

        public ReplayUpdateInfo(CqlQuote quote)
        {
            _name = quote.n;
            _id = quote.s;
            _itemData["MID_OPEN"] = "0";
            _itemData["HIGH"] = "0";
            _itemData["LOW"] = "0";
            _itemData["CHANGE"] = "0";
            _itemData["CHANGE_PCT"] = "0";
            _itemData["UPDATE_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", quote.t.Year, quote.t.Month,
                                                quote.t.Day, quote.t.Hour, quote.t.Minute, quote.t.Second);
            _itemData["MARKET_DELAY"] = "0";
            _itemData["MARKET_STATE"] = "TRADEABLE";
            _itemData["BID"] = quote.b.ToString();
            _itemData["OFFER"] = quote.o.ToString();
        }

        public string Name { get { return _name; } }
        public string Id { get { return _id; } }

        string IUpdateInfo.ItemName { get { return _name; }}
        int IUpdateInfo.ItemPos { get { return 0; } }
        int IUpdateInfo.NumFields { get { return _itemData.Count; } }
        bool IUpdateInfo.Snapshot { get { return false; } }

        string IUpdateInfo.GetNewValue(int fieldPos)
        {
            return _itemData.ElementAt(fieldPos).Value;
        }

        string IUpdateInfo.GetNewValue(string fieldName)
        {
            return _itemData[fieldName];
        }

        string IUpdateInfo.GetOldValue(int fieldPos)
        {
            return "";
        }

        string IUpdateInfo.GetOldValue(string fieldName)
        {
            return "";
        }

        bool IUpdateInfo.IsValueChanged(int fieldPos)
        {
            return true;
        }

        bool IUpdateInfo.IsValueChanged(string fieldName)
        {
            return true;
        }
    }

    public class ReplayStreamingClient : IAbstractStreamingClient
    {
        IReaderConnection _instance = null;
        DateTime _startTime;
        DateTime _stopTime;
        Dictionary<string, List<CqlQuote>> _expectedIndicatorData = null;
        Dictionary<string, List<CqlQuote>> _expectedSignalData = null;
        bool _replayTest = false;
        List<string> _testReplayFiles = new List<string>();

        public Dictionary<string, List<CqlQuote>> ExpectedIndicatorData { get { return _expectedIndicatorData; } }
        public Dictionary<string, List<CqlQuote>> ExpectedSignalData { get { return _expectedSignalData; } }
        public bool ReplayTest { get { return _replayTest; } }
        
        void IAbstractStreamingClient.Connect(string username, string password, string apiKey)
        {
            _startTime = DateTime.SpecifyKind(DateTime.Parse(Config.Settings["TRADING_START_TIME"]), DateTimeKind.Utc);
            _stopTime = DateTime.SpecifyKind(DateTime.Parse(Config.Settings["TRADING_STOP_TIME"]), DateTimeKind.Utc);
            if (Config.Settings["REPLAY_MODE"] == "DB")
            {
                _testReplayFiles.Add(null); // Add a fake element to trigger the replay from db
                _instance = new CassandraConnection();
            }
            else if (Config.Settings["REPLAY_MODE"] == "CSV")
            {
                _testReplayFiles = Config.Settings["REPLAY_CSV"].Split(';').ToList();
                _instance = new CsvReader(_testReplayFiles[0]);
            }
            else
                _instance = null;
            _replayTest = Config.Settings["REPLAY_MODE"] == "CSV" && !Config.Settings.ContainsKey("PUBLISHING_CSV");
        }

        void IAbstractStreamingClient.Subscribe(string[] epics, IHandyTableListener tableListener)
        {
            while (_testReplayFiles.Count > 0)
            {
                Dictionary<string, List<CqlQuote>> priceData = getReplayData(epics);
                replay(priceData, tableListener);
                _testReplayFiles.RemoveAt(0);
            }
        }

        void IAbstractStreamingClient.Unsubscribe()
        {
        }

        void replay(Dictionary<string, List<CqlQuote>> priceData, IHandyTableListener tableListener)
        {
            DateTime curtime = _startTime;
            while (priceData.Count > 0)
            {
                DateTimeOffset minNextTime = _stopTime;
                ReplayUpdateInfo nextUpdate = null;
                int lastIndex = -1;
                List<string> epicsToDelete = new List<string>();
                foreach (var epicQuotes in priceData)
                {
                    if (epicQuotes.Value.Count == 0)
                        epicsToDelete.Add(epicQuotes.Key);
                    else
                    {
                        lastIndex = epicQuotes.Value.Count -1 ;
                        if (epicQuotes.Value[lastIndex].t <= minNextTime)
                        {
                            minNextTime = epicQuotes.Value[lastIndex].t;
                            nextUpdate = new ReplayUpdateInfo(epicQuotes.Value[lastIndex]);
                        }
                    }
                }
                if (nextUpdate == null)
                {
                    foreach (var epic in epicsToDelete)
                        priceData.Remove(epic);
                }
                else
                {
                    priceData[nextUpdate.Id].RemoveAt(lastIndex);
                    tableListener.OnUpdate(0, nextUpdate.Id, nextUpdate);
                }
            }
        }

        Dictionary<string, List<CqlQuote>> getReplayData(string[] epics)
        {
            Dictionary<string, List<CqlQuote>> priceData = new Dictionary<string, List<CqlQuote>>();
            foreach (string epic in epics)
                priceData[epic] = _instance.GetMarketDataQuotes(_startTime, _stopTime,
                    CassandraConnection.DATATYPE_STOCK, epic);
            if (_replayTest)
            {
                _expectedIndicatorData = new Dictionary<string, List<CqlQuote>>();
                foreach (string epic in epics)
                {
                    List<CqlQuote> quotes = _instance.GetIndicatorDataQuotes(_startTime, _stopTime,
                        CassandraConnection.DATATYPE_INDICATOR, epic);
                    Dictionary<string, List<CqlQuote>> indicatorData = (from quote in quotes
                                                                        group quote by quote.s into g
                                                                        select new { Key = g.Key, Quotes = g.ToList() }).ToDictionary(keyVal => keyVal.Key, keyVal => keyVal.Quotes);
                    indicatorData.Aggregate(_expectedIndicatorData, (agg, keyVal) => { agg.Add(keyVal.Key, keyVal.Value); return agg; });
                }
                _expectedSignalData = new Dictionary<string, List<CqlQuote>>();
                foreach (string epic in epics)
                {
                    List<CqlQuote> quotes = _instance.GetSignalDataQuotes(_startTime, _stopTime,
                        CassandraConnection.DATATYPE_SIGNAL, epic);
                    Dictionary<string, List<CqlQuote>> signalData = (from quote in quotes
                                                                     group quote by quote.s into g
                                                                     select new { Key = g.Key, Quotes = g.ToList() }).ToDictionary(keyVal => keyVal.Key, keyVal => keyVal.Quotes);
                    signalData.Aggregate(_expectedSignalData, (agg, keyVal) => { agg.Add(keyVal.Key, keyVal.Value); return agg; });
                }
                PublisherConnection.Instance.SetExpectedResults(_expectedIndicatorData, _expectedSignalData);
            }
            return priceData;
        }
    }

    public class ReplayConnection : MarketDataConnection
    {
        ReplayStreamingClient _replayStreamingClient;

        public ReplayConnection() : base(new ReplayStreamingClient())
        {
            _replayStreamingClient = (ReplayStreamingClient)_apiStreamingClient;
        }

        public override void Connect(TimerCallback connectionClosed)
        {
            try
            {
                _apiStreamingClient.Connect("A_REPLAYER", "DOESNT_NEED_A_PWD", "NOR_A_KEY");
            }
            catch (Exception ex)
            {
                Log.Instance.WriteEntry(ex.Message, EventLogEntryType.Error);
            }
        }
    }

    public class ReplayPublisher : PublisherConnection
    {
        string _csvFile = null;
        StringBuilder _csvStockStringBuilder;
        StringBuilder _csvIndicatorStringBuilder;
        StringBuilder _csvSignalStringBuilder;

        static public new ReplayPublisher Instance
        {
            get
            {
                return (ReplayPublisher)_instance;
            }
        }

        public ReplayPublisher()
        {
            _csvFile = Config.Settings["PUBLISHING_CSV"];
            _csvStockStringBuilder = new StringBuilder();
            _csvIndicatorStringBuilder = new StringBuilder();
            _csvSignalStringBuilder = new StringBuilder();
        }

        public override void Insert(DateTime updateTime, MarketData mktData, Price price)
        {
            var newLine = string.Format("{0},{1},{2},{3},{4},{5},{6}{7}",
                DATATYPE_STOCK, mktData.Id,
                updateTime, mktData.Name,
                price.Bid, price.Offer,
                price.Volume, Environment.NewLine);
            _csvStockStringBuilder.Append(newLine);
        }

        public override void Insert(DateTime updateTime, Indicator indicator, decimal value)
        {
            var newLine = string.Format("{0},{1},{2},{3}{4}",
                DATATYPE_INDICATOR, indicator.Id,
                updateTime, value, Environment.NewLine);
            _csvIndicatorStringBuilder.Append(newLine);
        }

        public override void Insert(DateTime updateTime, Signal signal, SIGNAL_CODE code)
        {
            var newLine = string.Format("{0},{1},{2},{3}{4}",
                DATATYPE_SIGNAL, signal.Id,
                updateTime, (int)code, Environment.NewLine);
            _csvSignalStringBuilder.Append(newLine);
        }

        public override string Close()
        {
            string csvContent = _csvStockStringBuilder.ToString();
            csvContent += Environment.NewLine;
            csvContent += _csvIndicatorStringBuilder.ToString();
            csvContent += Environment.NewLine;
            csvContent += _csvSignalStringBuilder.ToString();
            File.WriteAllText(_csvFile, csvContent);
            string info = "Generated results in " + _csvFile;
            Log.Instance.WriteEntry(info, EventLogEntryType.Information);
            return info;
        }
    }

    public class ReplayTester : PublisherConnection
    {
        static public new ReplayTester Instance
        {
            get
            {
                return (ReplayTester)_instance;
            }
        }

        public ReplayTester()
        {
        }

        public override void Insert(DateTime updateTime, MarketData mktData, Price price)
        {            
        }

        public override void Insert(DateTime updateTime, Indicator indicator, decimal value)
        {
            if ((_expectedIndicatorData[indicator.Id].Value(updateTime).Value.Value.Bid != value))
            {
                string error = "Test failed: indicator " + indicator.Name + " time " + updateTime.ToShortTimeString() + " expected value " +
                   _expectedIndicatorData[indicator.Id].Value(updateTime).Value.Value.Bid + " != " + value;
                Log.Instance.WriteEntry(error, EventLogEntryType.Error);
                throw new ApplicationException(error);
            }
        }

        public override void Insert(DateTime updateTime, Signal signal, SIGNAL_CODE code)
        {
            if (((SIGNAL_CODE)_expectedSignalData[signal.Id].Value(updateTime).Value.Value.Bid != code))
            {
                string error = "Test failed: signal " + signal.Name + " time " + updateTime.ToShortTimeString() + " expected value " +
                   ((SIGNAL_CODE)_expectedSignalData[signal.Id].Value(updateTime).Value.Value.Bid).ToString() + " != " + code.ToString();
                Log.Instance.WriteEntry(error, EventLogEntryType.Error);
                throw new ApplicationException(error);
            }
        }

        public override string Close()
        {
            string info = "Tests passed successfully";
            Log.Instance.WriteEntry(info, EventLogEntryType.Information);
            return info;
        }
    }
}

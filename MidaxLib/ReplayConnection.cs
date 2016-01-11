using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cassandra;
using dto.endpoint.search;
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
            _itemData["MARKET_STATE"] = "REPLAY";
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
        Dictionary<KeyValuePair<string, DateTime>, Trade> _expectedTradeData = null;
        Dictionary<KeyValuePair<string, DateTime>, double> _expectedProfitData = null;        
        
        bool _hasExpectedResults = false;
        List<string> _testReplayFiles = new List<string>();
        static Portfolio _ptf = null;
        
        public Dictionary<string, List<CqlQuote>> ExpectedIndicatorData { get { return _expectedIndicatorData; } }
        public Dictionary<string, List<CqlQuote>> ExpectedSignalData { get { return _expectedSignalData; } }
        public Dictionary<KeyValuePair<string, DateTime>, Trade> ExpectedTradeData { get { return _expectedTradeData; } }
        public Dictionary<KeyValuePair<string, DateTime>, double> ExpectedProfitData { get { return _expectedProfitData; } }
        public static Portfolio PTF
        {
            get { return _ptf; }
            set { _ptf = value; }
        }

        public void Connect(string username, string password, string apiKey)
        {
            _startTime = Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_START_TIME"]);
            _stopTime = Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]);
            _testReplayFiles.Clear();
            if (Config.Settings["REPLAY_MODE"] == "DB")
            {
                _testReplayFiles.Add(null); // Add a fake element to trigger the replay from db
                _instance = new CassandraConnection();
            }
            else if (Config.Settings["REPLAY_MODE"] == "CSV")
            {
                _testReplayFiles = Config.Settings["REPLAY_CSV"].Split(';').ToList();
                if (_instance != null)
                    _instance.CloseConnection();
                _instance = new CsvReader(_testReplayFiles[0]);
            }
            else
                _instance = null;
            _hasExpectedResults = Config.TestReplayEnabled || Config.MarketSelectorEnabled || Config.CalibratorEnabled;
        }

        public void Connect()
        {
            Connect("A REPLAYER", "DOES NOT NEED", "A PASSWORD");
        }

        public virtual void Subscribe(string[] epics, IHandyTableListener tableListener)
        {
            while (_testReplayFiles.Count > 0)
            {
                Dictionary<string, List<CqlQuote>> priceData = GetReplayData(epics);
                replay(priceData, tableListener);
                _testReplayFiles.RemoveAt(0);
            }
        }

        void IAbstractStreamingClient.Unsubscribe()
        {
        }

        protected IHandyTableListener _tradingEventTable = null;

        SubscribedTableKey IAbstractStreamingClient.SubscribeToPositions(IHandyTableListener tableListener)
        {
            _tradingEventTable = tableListener;
            return null;
        }

        void IAbstractStreamingClient.UnsubscribeTradeSubscription(SubscribedTableKey tableListener)
        {
            _tradingEventTable = null;
        }

        public virtual void BookTrade(Trade trade, Portfolio.TradeBookedEvent onTradeBooked)
        {
            if (trade != null)
            {
                trade.Reference = "###DUMMY_TRADE###";
                trade.ConfirmationTime = trade.TradingTime;
                // TODO: remove this hack
                PTF.GetPosition(trade.Epic).Value += trade.Size * (trade.Direction == SIGNAL_CODE.BUY ? 1 : -1);
                onTradeBooked(trade);
                // TODO: do the real update here instead
                _tradingEventTable.OnUpdate(0, trade.Epic, null);
            }
        }

        void IAbstractStreamingClient.ClosePosition(Trade trade, Portfolio.TradeBookedEvent onTradeClosed)
        {
            if (trade != null)
            {
                trade.Reference = "###CLOSE_DUMMY_TRADE###";
                trade.ConfirmationTime = trade.TradingTime;
                // TODO: remove this hack
                PTF.GetPosition(trade.Epic).Value = 0;
                onTradeClosed(trade);
                // TODO: do the real update here instead
                _tradingEventTable.OnUpdate(0, trade.Epic, null);
            }
        }

        void IAbstractStreamingClient.GetMarketDetails(MarketData mktData, PublisherConnection.PublishMarketLevelsEvent evt)
        {
            MarketLevels mktLevels = ReplayTester.Instance.GetMarketLevels(_startTime, mktData.Id).Value;
            Market mkt = new Market();
            mkt.high = mktLevels.High; mkt.low = mktLevels.Low; mkt.bid = mktLevels.CloseBid; mkt.offer = mktLevels.CloseOffer;
            evt(mkt);
        }

        protected void replay(Dictionary<string, List<CqlQuote>> priceDataSrc, IHandyTableListener tableListener)
        {
            Dictionary<string, List<CqlQuote>> priceData = priceDataSrc.ToDictionary(kv => kv.Key, kv => kv.Value.ToList());
            DateTime curtime = _startTime;
            while (priceData.Count > 0)
            {
                DateTimeOffset minNextTime = _stopTime;
                ReplayUpdateInfo nextUpdate = null;
                List<string> epicsToDelete = new List<string>();
                foreach (var epicQuotes in priceData)
                {
                    if (epicQuotes.Value.Count == 0)
                        epicsToDelete.Add(epicQuotes.Key);
                    else
                    {
                        if (epicQuotes.Value[0].t <= minNextTime)
                        {
                            minNextTime = epicQuotes.Value[0].t;
                            nextUpdate = new ReplayUpdateInfo(epicQuotes.Value[0]);
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
                    priceData[nextUpdate.Id].RemoveAt(0);
                    tableListener.OnUpdate(0, nextUpdate.Id, nextUpdate);
                }
            }
        }

        public Dictionary<string, List<CqlQuote>> GetReplayData(string[] epics)
        {
            Dictionary<string, List<CqlQuote>> priceData = new Dictionary<string, List<CqlQuote>>();
            foreach (string epic in epics)
                priceData[epic] = _instance.GetMarketDataQuotes(_startTime, _stopTime,
                    CassandraConnection.DATATYPE_STOCK, epic);
            if (_hasExpectedResults)
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
                _expectedTradeData = new Dictionary<KeyValuePair<string,DateTime>, Trade>();
                foreach (string epic in epics)
                {
                    List<Trade> trades = _instance.GetTrades(_startTime, _stopTime,
                                                                    CassandraConnection.DATATYPE_TRADE, epic);
                    foreach (Trade trade in trades)
                        _expectedTradeData.Add(new KeyValuePair<string, DateTime>(epic, trade.TradingTime), trade);
                }
                _expectedProfitData = new Dictionary<KeyValuePair<string, DateTime>, double>();
                foreach (string epic in epics)
                {
                    List<KeyValuePair<DateTime, double>> profits = _instance.GetProfits(_startTime, _stopTime,
                                                                    CassandraConnection.DATATYPE_TRADE, epic);
                    foreach (var profit in profits)
                        _expectedProfitData.Add(new KeyValuePair<string, DateTime>(epic, profit.Key), profit.Value);
                }
                PublisherConnection.Instance.SetExpectedResults(_expectedIndicatorData, _expectedSignalData, _expectedTradeData, _expectedProfitData);
            }
            return priceData;
        }
    }

    // this crazy client never updates the positions
    public class ReplayStreamingCrazyClient : ReplayStreamingClient
    {
        public override void BookTrade(Trade trade, Portfolio.TradeBookedEvent onTradeBooked)
        {
            if (trade != null)
            {
                trade.Reference = "###DUMMY_TRADE###";
                trade.ConfirmationTime = trade.TradingTime;
                onTradeBooked(trade);
            }
        }
    }

    public class ReplayConnection : MarketDataConnection, IStaticDataConnection
    {
        ReplayStreamingClient _replayStreamingClient;

        public ReplayConnection() : base(new ReplayStreamingClient())
        {
            _replayStreamingClient = (ReplayStreamingClient)_apiStreamingClient;
        }

        public ReplayConnection(ReplayStreamingClient client)
            : base(client)
        {
            _replayStreamingClient = client;
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

        int IStaticDataConnection.GetAnnLatestVersion(string annid, string stockid)
        {
            return 1;
        }

        List<decimal> IStaticDataConnection.GetAnnWeights(string annid, string stockid, int version)
        {
            return new List<decimal> {  72.3059189398836m, 72.8785271734858m, 81.873849047948m, 71.6605471639554m, 
                99.3678597133658m, -1.39731049606147m, -0.973839446848656m, 0.854679349838304m, 2.10407644665642m, 
                26.6526593970665m, 0.0758744148816272m, -0.15246543443817m, 0.0886880451059489m };
        }
    }

    public class ReplayCrazyConnection : ReplayConnection
    {
        public ReplayCrazyConnection()
            : base(new ReplayStreamingCrazyClient())
        {
        }
    }

    public class ReplayPublisher : PublisherConnection
    {
        string _csvFile = null;
        StringBuilder _csvStockStringBuilder;
        StringBuilder _csvIndicatorStringBuilder;
        StringBuilder _csvSignalStringBuilder;
        StringBuilder _csvTradeStringBuilder;
        StringBuilder _csvProfitStringBuilder;

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
            _csvTradeStringBuilder = new StringBuilder();
            _csvProfitStringBuilder = new StringBuilder();
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

        public override void Insert(DateTime updateTime, Signal signal, SIGNAL_CODE code, decimal stockvalue)
        {
            string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
            var newLine = string.Format("{0},{1},{2},{3},{4},{5}{6}",
                DATATYPE_SIGNAL, signal.Id,
                updateTime, tradeRef, (int)code, stockvalue, Environment.NewLine);
            _csvSignalStringBuilder.Append(newLine);
        }

        public override void Insert(Trade trade)
        {
            if (trade.TradingTime == DateTimeOffset.MinValue || trade.ConfirmationTime == DateTimeOffset.MinValue || trade.Reference == "")
                throw new ApplicationException("Cannot insert a trade without booking information");
            var newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7}{8}",
                DATATYPE_TRADE, trade.Reference, trade.ConfirmationTime, trade.Direction, trade.Price, trade.Size, trade.Epic, trade.TradingTime,
                Environment.NewLine);
            _csvTradeStringBuilder.Append(newLine);
        }

        public override void Insert(DateTime updateTime, Value profit)
        {
            var newLine = string.Format("{0},{1}{2}", updateTime, profit.X,
                Environment.NewLine);
            _csvProfitStringBuilder.Append(newLine);
        }

        public override void Insert(DateTime updateTime, NeuralNetworkForCalibration ann)
        {
            throw new ApplicationException("ANN insertion not implemented");
        }

        public override void Insert(Market mktDetails)
        {
            throw new ApplicationException("End of day data publishing not implemented");
        }

        public override MarketLevels? GetMarketLevels(DateTime updateTime, string epic)
        {
            return new MarketLevels(10200m, 12500m, 11000m, 11100m);
        }

        public override string Close()
        {
            string csvContent = _csvStockStringBuilder.ToString();
            csvContent += Environment.NewLine;
            csvContent += _csvIndicatorStringBuilder.ToString();
            csvContent += Environment.NewLine;
            csvContent += _csvSignalStringBuilder.ToString();
            csvContent += Environment.NewLine;
            csvContent += _csvTradeStringBuilder.ToString();
            csvContent += Environment.NewLine;
            csvContent += _csvProfitStringBuilder.ToString();
            File.WriteAllText(_csvFile, csvContent);
            string info = "Generated results in " + _csvFile;
            Log.Instance.WriteEntry(info, EventLogEntryType.Information);
            _instance = null;
            return info;
        }
    }

    public class ReplayTester : PublisherConnection
    {
        public const decimal TOLERANCE = 1e-4m;
        MarketLevels _mktLevels;
        int _nbTrades = 0;

        public int NbExpectedTrades { get { return _expectedTradeData.Count; } }
        public int NbProducedTrades { get { return _nbTrades; } }

        Model _model = null;
        public Model ModelTest {
            get
            {
                return _model;
            }
            set
            {
                _model = value;
            }
        }

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
            if ((Math.Abs(_expectedIndicatorData[indicator.Id].Value(updateTime).Value.Value.Bid - value) > TOLERANCE))
            {
                string error = "Test failed: indicator " + indicator.Name + " time " + updateTime.ToShortTimeString() + " expected value " +
                   _expectedIndicatorData[indicator.Id].Value(updateTime).Value.Value.Bid + " != " + value;
                Log.Instance.WriteEntry(error, EventLogEntryType.Error);
                throw new ApplicationException(error);
            }
        }

        public override void Insert(DateTime updateTime, Signal signal, SIGNAL_CODE code, decimal stockvalue)
        {
            if (((SIGNAL_CODE)_expectedSignalData[signal.Id].Value(updateTime).Value.Value.Bid != code) ||
                Math.Abs(_expectedSignalData[signal.Id].Value(updateTime).Value.Value.Offer - stockvalue) > TOLERANCE)
            {
                string error;
                if ((SIGNAL_CODE)_expectedSignalData[signal.Id].Value(updateTime).Value.Value.Bid != code)
                    error = "Test failed: signal " + signal.Name + " time " + updateTime.ToShortTimeString() + " expected value " +
                   ((SIGNAL_CODE)_expectedSignalData[signal.Id].Value(updateTime).Value.Value.Bid).ToString() + " != " + code.ToString();
                else
                    error = "Test failed: signal stock value " + signal.Name + " time " + updateTime.ToShortTimeString() + " expected value " +
                   (_expectedSignalData[signal.Id].Value(updateTime).Value.Value.Offer).ToString() + " != " + stockvalue.ToString();
                Log.Instance.WriteEntry(error, EventLogEntryType.Error);
                throw new ApplicationException(error);
            }
        }

        public override void Insert(Trade trade)
        {
            _nbTrades++;
            var tradeKey = new KeyValuePair<string, DateTime>(trade.Epic, trade.TradingTime);
            if (Math.Abs(_expectedTradeData[tradeKey].Price - trade.Price) > TOLERANCE)
            {
                string error = "Test failed: trade " + trade.Epic + " time " + trade.TradingTime.ToShortTimeString() + " expected Price " +
                   _expectedTradeData[tradeKey].Price.ToString() + " != " + trade.Price.ToString();
                Log.Instance.WriteEntry(error, EventLogEntryType.Error);
                throw new ApplicationException(error);
            }
            if (_expectedTradeData[tradeKey].Direction != trade.Direction)
            {
                string error = "Test failed: trade " + trade.Epic + " time " + trade.TradingTime.ToShortTimeString() + " expected Direction " +
                   _expectedTradeData[tradeKey].Direction.ToString() + " != " + trade.Direction.ToString();
                Log.Instance.WriteEntry(error, EventLogEntryType.Error);
                throw new ApplicationException(error);
            }
            if (_expectedTradeData[tradeKey].Size != trade.Size)
            {
                string error = "Test failed: trade " + trade.Epic + " time " + trade.TradingTime.ToShortTimeString() + " expected Size " +
                   _expectedTradeData[tradeKey].Size.ToString() + " != " + trade.Size.ToString();
                Log.Instance.WriteEntry(error, EventLogEntryType.Error);
                throw new ApplicationException(error);
            }
            if (_expectedTradeData[tradeKey].Reference != trade.Reference)
            {
                string error = "Test failed: trade " + trade.Epic + " time " + trade.TradingTime.ToShortTimeString() + " expected Reference " +
                   _expectedTradeData[tradeKey].Reference + " != " + trade.Reference;
                Log.Instance.WriteEntry(error, EventLogEntryType.Error);
                throw new ApplicationException(error);
            }
        }

        public override void Insert(Market mktDetails)
        {
            if (mktDetails.high != _mktLevels.High || mktDetails.low != _mktLevels.Low || 
                mktDetails.bid != _mktLevels.CloseBid || mktDetails.offer != _mktLevels.CloseOffer)
            {
                string error = string.Format("Test failed: market levels " + mktDetails.epic + " time " + Config.ParseDateTimeLocal(mktDetails.updateTime).ToShortTimeString() +
                    " expected levels (high, low, bid, offer) {0} {1} {2} {3} != {4} {5} {6} {7} ", _mktLevels.High, _mktLevels.Low, _mktLevels.CloseBid, _mktLevels.CloseOffer,
                    mktDetails.high, mktDetails.low, mktDetails.bid, mktDetails.offer);
                Log.Instance.WriteEntry(error, EventLogEntryType.Error);
                throw new ApplicationException(error);
            }
        }

        public override void Insert(DateTime updateTime, Value profit)
        {
        }

        public override void Insert(DateTime updateTime, NeuralNetworkForCalibration ann)
        {
        }

        public override MarketLevels? GetMarketLevels(DateTime updateTime, string epic)
        {
            _mktLevels = new MarketLevels(10200m, 12500m, 11000m, 11100m);
            return _mktLevels;
        }

        public override string Close()
        {
            string info = "Tests passed successfully";
            Log.Instance.WriteEntry(info, EventLogEntryType.Information);
            return info;
        }
    }
}

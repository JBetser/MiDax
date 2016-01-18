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
        protected Dictionary<string, string> _itemData = new Dictionary<string, string>();
        protected string _name;
        protected string _id;
        
        public ReplayUpdateInfo(CqlQuote quote)
        {
            if (quote != null)
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

    public class ReplayPositionUpdateInfo : ReplayUpdateInfo
    {
        public ReplayPositionUpdateInfo(string epic, string dealId, string status, string dealStatus, int size, decimal level, SIGNAL_CODE direction) : base(null)
        {
            _name = epic;
            _id = dealId;
            _itemData["status"] = status;
            _itemData["dealStatus"] = dealStatus;
            _itemData["size"] = size.ToString();
            _itemData["level"] = level.ToString();
            _itemData["direction"] = direction == SIGNAL_CODE.BUY ? "BUY" : (direction == SIGNAL_CODE.SELL ? "SELL" : "UNKNOWN");
        }

        public override string ToString()
        {
            return string.Format("[ {{ \"epic\" : \"{0}\", \"dealId\" : \"{1}\", \"status\" : \"{2}\", \"dealStatus\" : \"{3}\", \"size\" : \"{4}\", \"level\" : \"{5}\", \"direction\" : \"{6}\" }} ]",
                _name, _id, _itemData["status"], _itemData["dealStatus"], _itemData["size"], _itemData["level"], _itemData["direction"]);
        }
    }

    public class ReplayStreamingClient : IAbstractStreamingClient
    {
        IReaderConnection _reader = null;
        DateTime _startTime;
        DateTime _stopTime;
        Dictionary<string, List<CqlQuote>> _expectedIndicatorData = null;
        Dictionary<string, List<CqlQuote>> _expectedSignalData = null;
        Dictionary<KeyValuePair<string, DateTime>, Trade> _expectedTradeData = null;
        Dictionary<KeyValuePair<string, DateTime>, double> _expectedProfitData = null;
        Dictionary<string, MarketLevels> _expectedMktLevelsData = null;
        
        bool _hasExpectedResults = false;
        List<string> _testReplayFiles = new List<string>();

        public IReaderConnection Reader { get { return _reader; } }
        
        public Dictionary<string, List<CqlQuote>> ExpectedIndicatorData { get { return _expectedIndicatorData; } }
        public Dictionary<string, List<CqlQuote>> ExpectedSignalData { get { return _expectedSignalData; } }
        public Dictionary<KeyValuePair<string, DateTime>, Trade> ExpectedTradeData { get { return _expectedTradeData; } }
        public Dictionary<KeyValuePair<string, DateTime>, double> ExpectedProfitData { get { return _expectedProfitData; } }

        public void Connect(string username, string password, string apiKey)
        {
            _startTime = Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_START_TIME"]);
            _stopTime = Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]);
            _testReplayFiles.Clear();
            if (Config.Settings["REPLAY_MODE"] == "DB")
            {
                _testReplayFiles.Add(null); // Add a fake element to trigger the replay from db
                _reader = new CassandraConnection();
            }
            else if (Config.Settings["REPLAY_MODE"] == "CSV")
            {
                _testReplayFiles = Config.Settings["REPLAY_CSV"].Split(';').ToList();
                if (_reader != null)
                    _reader.CloseConnection();
                _reader = new CsvReader(_testReplayFiles[0]);
            }
            else
                _reader = null;
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
                trade.Id = "###DUMMY_TRADE_ID###";
                trade.Reference = "###DUMMY_TRADE###";
                trade.ConfirmationTime = trade.TradingTime;
                onTradeBooked(trade);
                _tradingEventTable.OnUpdate(0, trade.Epic, new ReplayPositionUpdateInfo(trade.Epic, trade.Id, "OPEN", "ACCEPTED", trade.Size, trade.Price, trade.Direction));
            }
        }

        void IAbstractStreamingClient.ClosePosition(Trade trade, DateTime time, Portfolio.TradeBookedEvent onTradeBooked)
        {
            if (trade != null)
            {
                var closingTrade = new Trade(trade, true, time);
                closingTrade.Id = "###CLOSE_DUMMY_TRADE_ID###";
                closingTrade.Reference = "###CLOSE_DUMMY_TRADE###";
                closingTrade.ConfirmationTime = time;
                onTradeBooked(closingTrade);
                _tradingEventTable.OnUpdate(0, trade.Epic, new ReplayPositionUpdateInfo(trade.Epic, trade.Id, "DELETED", "ACCEPTED", trade.Size, trade.Price, trade.Direction));
            }
        }

        void IAbstractStreamingClient.GetMarketDetails(MarketData mktData)
        {
            var mktLevels = _reader.GetMarketLevels(_startTime, mktData.Id);
            if (mktLevels.HasValue)
            {
                Market mkt = new Market();
                mkt.epic = mktData.Id;
                mkt.high = mktLevels.Value.High; mkt.low = mktLevels.Value.Low; mkt.bid = mktLevels.Value.CloseBid; mkt.offer = mktLevels.Value.CloseOffer;
                mktData.Levels = new MarketLevels(mkt.epic, mkt.low.Value, mkt.high.Value, mkt.bid.Value, mkt.offer.Value);
            }
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
                priceData[epic] = _reader.GetMarketDataQuotes(_startTime, _stopTime,
                    CassandraConnection.DATATYPE_STOCK, epic);
            if (_hasExpectedResults)
            {
                _expectedIndicatorData = new Dictionary<string, List<CqlQuote>>();
                foreach (string epic in epics)
                {
                    List<CqlQuote> quotes = _reader.GetIndicatorDataQuotes(_startTime, _stopTime,
                        CassandraConnection.DATATYPE_INDICATOR, epic);
                    Dictionary<string, List<CqlQuote>> indicatorData = (from quote in quotes
                                                                        group quote by quote.s into g
                                                                        select new { Key = g.Key, Quotes = g.ToList() }).ToDictionary(keyVal => keyVal.Key, keyVal => keyVal.Quotes);
                    indicatorData.Aggregate(_expectedIndicatorData, (agg, keyVal) => { agg.Add(keyVal.Key, keyVal.Value); return agg; });
                }
                _expectedSignalData = new Dictionary<string, List<CqlQuote>>();
                foreach (string epic in epics)
                {
                    List<CqlQuote> quotes = _reader.GetSignalDataQuotes(_startTime, _stopTime,
                        CassandraConnection.DATATYPE_SIGNAL, epic);
                    Dictionary<string, List<CqlQuote>> signalData = (from quote in quotes
                                                                     group quote by quote.s into g
                                                                     select new { Key = g.Key, Quotes = g.ToList() }).ToDictionary(keyVal => keyVal.Key, keyVal => keyVal.Quotes);
                    signalData.Aggregate(_expectedSignalData, (agg, keyVal) => { agg.Add(keyVal.Key, keyVal.Value); return agg; });
                }
                _expectedTradeData = new Dictionary<KeyValuePair<string,DateTime>, Trade>();
                foreach (string epic in epics)
                {
                    List<Trade> trades = _reader.GetTrades(_startTime, _stopTime,
                                                                    CassandraConnection.DATATYPE_TRADE, epic);
                    foreach (Trade trade in trades)
                        _expectedTradeData.Add(new KeyValuePair<string, DateTime>(epic, trade.TradingTime), trade);
                }
                _expectedProfitData = new Dictionary<KeyValuePair<string, DateTime>, double>();
                foreach (string epic in epics)
                {
                    List<KeyValuePair<DateTime, double>> profits = _reader.GetProfits(_startTime, _stopTime,
                                                                    CassandraConnection.DATATYPE_TRADE, epic);
                    foreach (var profit in profits)
                        _expectedProfitData.Add(new KeyValuePair<string, DateTime>(epic, profit.Key), profit.Value);
                }
                _expectedMktLevelsData = new Dictionary<string, MarketLevels>();
                foreach (string epic in epics)
                    _expectedMktLevelsData[epic] = _reader.GetMarketLevels(_stopTime, epic).Value;
                PublisherConnection.Instance.SetExpectedResults(_expectedIndicatorData, _expectedSignalData, 
                    _expectedTradeData, _expectedProfitData, _expectedMktLevelsData);
            }
            return priceData;
        }
    }

    // this crazy client never updates the positions
    public class ReplayStreamingCrazySeller : ReplayStreamingClient
    {
        public override void BookTrade(Trade trade, Portfolio.TradeBookedEvent onTradeBooked)
        {
            if (trade.Direction == SIGNAL_CODE.SELL)
            {
                trade.Reference = "###DUMMY_TRADE###";
                trade.ConfirmationTime = trade.TradingTime;
                onTradeBooked(trade);
            }
            else
                base.BookTrade(trade, onTradeBooked);
        }
    }

    public class ReplayStreamingCrazyBuyer : ReplayStreamingClient
    {
        public override void BookTrade(Trade trade, Portfolio.TradeBookedEvent onTradeBooked)
        {
            if (trade.Direction == SIGNAL_CODE.BUY)
            {
                trade.Reference = "###DUMMY_TRADE###";
                trade.ConfirmationTime = trade.TradingTime;
                onTradeBooked(trade);
            }
            else
                base.BookTrade(trade, onTradeBooked);
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

    public class ReplayCrazySeller : ReplayConnection
    {
        public ReplayCrazySeller()
            : base(new ReplayStreamingCrazySeller())
        {
        }
    }

    public class ReplayCrazyBuyer : ReplayConnection
    {
        public ReplayCrazyBuyer()
            : base(new ReplayStreamingCrazyBuyer())
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
        StringBuilder _csvMktDetailsStringBuilder;

        static public new ReplayPublisher Instance
        {
            get
            {
                return (ReplayPublisher)_instance;
            }
        }

        public ReplayPublisher()
        {
            // this attaches the database handle from the publisher to our current reader (csvreader / cassandra)
            _database = ((ReplayStreamingClient)MarketDataConnection.Instance.StreamClient).Reader;
            _csvFile = Config.Settings["PUBLISHING_CSV"];
            _csvStockStringBuilder = new StringBuilder();
            _csvIndicatorStringBuilder = new StringBuilder();
            _csvSignalStringBuilder = new StringBuilder();
            _csvTradeStringBuilder = new StringBuilder();
            _csvProfitStringBuilder = new StringBuilder();
            _csvMktDetailsStringBuilder = new StringBuilder();
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

        public override void Insert(MarketLevels mktDetails)
        {
            var newLine = string.Format("marketlevels,{0},{1},{2},{3},{4}{5}", mktDetails.AssetId, mktDetails.Low, mktDetails.High, mktDetails.CloseBid, mktDetails.CloseOffer,
                Environment.NewLine);
            _csvMktDetailsStringBuilder.Append(newLine);
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
            csvContent += Environment.NewLine;
            csvContent += _csvMktDetailsStringBuilder.ToString();
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
            _database = ((ReplayStreamingClient)MarketDataConnection.Instance.StreamClient).Reader;            
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
                string error = "Test failed: trade " + trade.Epic + " expected Price " +
                   _expectedTradeData[tradeKey].Price.ToString() + " != " + trade.Price.ToString();
                Log.Instance.WriteEntry(error, EventLogEntryType.Error);
                throw new ApplicationException(error);
            }
            if (_expectedTradeData[tradeKey].Direction != trade.Direction)
            {
                string error = "Test failed: trade " + trade.Epic + " expected Direction " +
                   _expectedTradeData[tradeKey].Direction.ToString() + " != " + trade.Direction.ToString();
                Log.Instance.WriteEntry(error, EventLogEntryType.Error);
                throw new ApplicationException(error);
            }
            if (_expectedTradeData[tradeKey].Size != trade.Size)
            {
                string error = "Test failed: trade " + trade.Epic + " expected Size " +
                   _expectedTradeData[tradeKey].Size.ToString() + " != " + trade.Size.ToString();
                Log.Instance.WriteEntry(error, EventLogEntryType.Error);
                throw new ApplicationException(error);
            }
            if (_expectedTradeData[tradeKey].Reference != trade.Reference)
            {
                string error = "Test failed: trade " + trade.Epic + " expected Reference " +
                   _expectedTradeData[tradeKey].Reference + " != " + trade.Reference;
                Log.Instance.WriteEntry(error, EventLogEntryType.Error);
                throw new ApplicationException(error);
            }
        }

        public override void Insert(MarketLevels mktDetails)
        {
            if (mktDetails.High != _expectedMktLvlData[mktDetails.AssetId].High || mktDetails.Low != _expectedMktLvlData[mktDetails.AssetId].Low ||
                mktDetails.CloseBid != _expectedMktLvlData[mktDetails.AssetId].CloseBid || mktDetails.CloseOffer != _expectedMktLvlData[mktDetails.AssetId].CloseOffer)
            {
                string error = string.Format("Test failed: market levels " + mktDetails.AssetId + " time " + Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]).ToShortTimeString() +
                    " expected levels (high, low, bid, offer) {0} {1} {2} {3} != {4} {5} {6} {7} ",
                    _expectedMktLvlData[mktDetails.AssetId].High, _expectedMktLvlData[mktDetails.AssetId].Low, _expectedMktLvlData[mktDetails.AssetId].CloseBid, _expectedMktLvlData[mktDetails.AssetId].CloseOffer,
                    mktDetails.High, mktDetails.Low, mktDetails.CloseBid, mktDetails.CloseOffer);
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
        
        public override string Close()
        {
            string info = "Tests passed successfully";
            Log.Instance.WriteEntry(info, EventLogEntryType.Information);
            return info;
        }
    }
}

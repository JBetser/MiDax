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
        protected DateTime _time;
        
        public ReplayUpdateInfo(CqlQuote quote)
        {
            if (quote != null)
            {
                _name = quote.n;
                _id = quote.s;
                _time = quote.t;
                _itemData["MID_OPEN"] = "0";
                _itemData["HIGH"] = "0";
                _itemData["LOW"] = "0";
                _itemData["CHANGE"] = "0";
                _itemData["CHANGE_PCT"] = "0";
                _itemData["UPDATE_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}.{6}", quote.t.Year, quote.t.Month,
                                                    quote.t.Day, quote.t.Hour, quote.t.Minute, quote.t.Second, quote.t.Millisecond);
                _itemData["MARKET_DELAY"] = "0";
                _itemData["MARKET_STATE"] = "REPLAY";
                _itemData["BID"] = quote.b.ToString();
                _itemData["OFFER"] = quote.o.ToString();
                _itemData["VOLUME"] = quote.v.ToString();
            }
        }

        public string Name { get { return _name; } }
        public string Id { get { return _id; } }
        public DateTime Time { get { return _time; } }
                
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
        public ReplayPositionUpdateInfo(DateTime timestamp, string epic, string dealId, string dealRef, string status, string dealStatus, int size, decimal level, SIGNAL_CODE direction)
            : base(null)
        {
            _name = epic;
            _id = dealId;
            _itemData["timestamp"] = string.Format("{0}-{1}-{2}T {3}:{4}:{5}.{6}", timestamp.Year, timestamp.Month, timestamp.Day,
                                                               timestamp.Hour, timestamp.Minute, timestamp.Second, timestamp.Millisecond);
            _itemData["dealReference"] = dealRef;
            _itemData["status"] = status;
            _itemData["dealStatus"] = dealStatus;
            _itemData["size"] = size.ToString();
            _itemData["level"] = level.ToString();
            _itemData["direction"] = direction == SIGNAL_CODE.BUY ? "BUY" : (direction == SIGNAL_CODE.SELL ? "SELL" : "UNKNOWN");
        }

        public override string ToString()
        {
            return string.Format("[ {{ \"timestamp\" : \"{8}\", \"epic\" : \"{0}\", \"dealId\" : \"{1}\", \"dealReference\" : \"{2}\", \"status\" : \"{3}\", \"dealStatus\" : \"{4}\", \"size\" : \"{5}\", \"level\" : \"{6}\", \"direction\" : \"{7}\" }} ]",
                _name, _id, _itemData["dealReference"], _itemData["status"], _itemData["dealStatus"], _itemData["size"], _itemData["level"], _itemData["direction"], _itemData["timestamp"]);
        }
    }

    class TradeBookingEvent : EventWaitHandle
    {
        Trade _trade;
        public Trade Trade { get { return _trade; } }
        public TradeBookingEvent(Trade trade)
            : base(false, EventResetMode.AutoReset)
        {
            _trade = trade;
        }
    }

    public class ReplayStreamingClient : IAbstractStreamingClient
    {
        IReaderConnection _reader = null;
        IReaderConnection _readerExpectedResults = null;
        DateTime _startTime;
        DateTime _stopTime;
        Dictionary<string, List<CqlQuote>> _expectedIndicatorData = null;
        Dictionary<string, List<CqlQuote>> _expectedSignalData = null;
        Dictionary<KeyValuePair<string, DateTime>, Trade> _expectedTradeData = null;
        Dictionary<KeyValuePair<string, DateTime>, double> _expectedProfitData = null;
        int _numId = 1;
        int _numRef = 1;
        int _samplingMs = -1;
        
        bool _hasExpectedResults = false;
        List<string> _testReplayFiles = new List<string>();
        Dictionary<string, List<CqlQuote>> _priceData;
        ClosingWaitHandle _closing = new ClosingWaitHandle();

        public IReaderConnection Reader { get { return _reader; } }
        
        public Dictionary<string, List<CqlQuote>> ExpectedIndicatorData { get { return _expectedIndicatorData; } }
        public Dictionary<string, List<CqlQuote>> ExpectedSignalData { get { return _expectedSignalData; } }
        public Dictionary<KeyValuePair<string, DateTime>, Trade> ExpectedTradeData { get { return _expectedTradeData; } }
        public Dictionary<KeyValuePair<string, DateTime>, double> ExpectedProfitData { get { return _expectedProfitData; } }
                
        public void Connect(string username, string password, string apiKey)
        {
            if (Config.Settings.ContainsKey("PUBLISHING_START_TIME"))
                _startTime = Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_START_TIME"]);
            if (Config.Settings.ContainsKey("PUBLISHING_STOP_TIME"))
                _stopTime = Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]);
            if (Config.Settings.ContainsKey("SAMPLING_MS"))
                _samplingMs = int.Parse(Config.Settings["SAMPLING_MS"]);
            _testReplayFiles.Clear();
            if (Config.Settings["REPLAY_MODE"] == "DB")
            {
                _testReplayFiles.Add(null); // Add a fake element to trigger the replay from db
                _reader = new CassandraConnection();
            }
            else if (Config.Settings["REPLAY_MODE"] == "CSV")
            {
                if (Config.Settings.ContainsKey("REPLAY_CSV"))
                {
                    _testReplayFiles = Config.Settings["REPLAY_CSV"].Split(';').ToList();
                    if (_reader != null)
                        _reader.CloseConnection();
                    _reader = new CsvReader(_testReplayFiles[0]);
                }
                else
                    _reader = new CsvReader(null);
            }
            else
                _reader = null;
            _hasExpectedResults = Config.TestReplayEnabled || Config.MarketSelectorEnabled || Config.CalibratorEnabled;
            if (_hasExpectedResults)
            {
                if (Config.Settings.ContainsKey("EXPECTEDRESULTS_CSV"))
                    _readerExpectedResults = new CsvReader(Config.Settings["EXPECTEDRESULTS_CSV"]);
                else
                    _readerExpectedResults = new CsvReader(_testReplayFiles[0]);
            }
            _numId = 1;
            _numRef = 1;
        }

        public void Connect()
        {
            Connect("A REPLAYER", "DOES NOT NEED", "A PASSWORD");
        }

        public virtual void Subscribe(string[] epics, IHandyTableListener tableListener)
        {
            while (_testReplayFiles.Count > 0)
            {
                _priceData = GetReplayData(epics);
                replay(_priceData, tableListener);
                _testReplayFiles.RemoveAt(0);
            }
        }

        public void Resume(IHandyTableListener tableListener)
        {
            while (_testReplayFiles.Count > 0)
            {
                replay(_priceData, tableListener);
                _testReplayFiles.RemoveAt(0);
            }
        }

        void IAbstractStreamingClient.Unsubscribe()
        {
            _closing = new ClosingWaitHandle();
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

        string[] _placeHolders = new string[100];
        List<Timer> _bookingTimers = new List<Timer>();

        public virtual void BookTrade(Trade trade, Portfolio.TradeBookedEvent onTradeBooked, Portfolio.TradeBookedEvent onBookingFailed)
        {
            if (trade != null)
            {
                trade.Reference = "###DUMMY_TRADE_REF" + _numRef++ + "###";
                trade.ConfirmationTime = trade.TradingTime;                
                onTradeBooked(trade);
                var bookingEvent = new TradeBookingEvent(trade);
                _bookingTimers.Add(new System.Threading.Timer(onCreateTradeNotification, bookingEvent, 10, Timeout.Infinite));
            }
        }

        void onCreateTradeNotification(object state)
        {
            var trade = ((TradeBookingEvent)state).Trade;
            trade.Id = "###DUMMY_TRADE_ID" + _numId++ + "###";
            _placeHolders[trade.PlaceHolder] = trade.Id;  
            if (_tradingEventTable != null)
                _tradingEventTable.OnUpdate(0, trade.Epic, new ReplayPositionUpdateInfo(trade.TradingTime, trade.Epic, trade.Id, trade.Reference, "OPEN", "ACCEPTED", trade.Size, trade.Price, trade.Direction));
        }

        void IAbstractStreamingClient.ClosePosition(Trade closingTrade, DateTime time, Portfolio.TradeBookedEvent onTradeBooked, Portfolio.TradeBookedEvent onBookingFailed)
        {
            if (closingTrade != null)
            {
                closingTrade.Reference = "###CLOSE_DUMMY_TRADE_REF" + _numRef++ + "###";
                closingTrade.ConfirmationTime = time;
                onTradeBooked(closingTrade);
                TradeBookingEvent bookingEvent = new TradeBookingEvent(closingTrade);
                _bookingTimers.Add(new System.Threading.Timer(onClosePositionNotification, bookingEvent, 10, Timeout.Infinite));
            }
        }

        void IAbstractStreamingClient.WaitForClosing()
        {
            _closing.WaitOne(10000);
        }

        void onClosePositionNotification(object state)
        {
            var trade = ((TradeBookingEvent)state).Trade;
            trade.Id = _placeHolders[trade.PlaceHolder];
            if (_tradingEventTable != null)
                _tradingEventTable.OnUpdate(0, trade.Epic, new ReplayPositionUpdateInfo(trade.TradingTime, trade.Epic, trade.Id, trade.Reference, "DELETED", "ACCEPTED", trade.Size, trade.Price, trade.Direction));
        }
        /*
        void IAbstractStreamingClient.GetMarketDetails(MarketData mktData)
        {
            var mktLevels = _reader.GetMarketLevels(_startTime, new List<string> { mktData.Id }).Values.First();
            Market mkt = new Market();
            mkt.epic = mktData.Id;
            mkt.high = mktLevels.High; mkt.low = mktLevels.Low; mkt.bid = mktLevels.CloseBid; mkt.offer = mktLevels.CloseOffer;
            mktData.Levels = new MarketLevels(mkt.epic, mkt.low.Value, mkt.high.Value, mkt.bid.Value, mkt.offer.Value);
        }*/

        protected void replay(Dictionary<string, List<CqlQuote>> priceData, IHandyTableListener tableListener)
        {
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
                    if (_samplingMs != -1)
                    {
                        if ((nextUpdate.Time - curtime).TotalMilliseconds < _samplingMs)
                            continue;
                        curtime = nextUpdate.Time;
                    }
                    tableListener.OnUpdate(0, nextUpdate.Id, nextUpdate);
                    if (_closing.Signaled)
                    {
                        _closing.Set();
                        break;
                    }
                }
            }
        }

        public Dictionary<string, List<CqlQuote>> GetReplayData(string[] epics)
        {
            var epicLst = epics.ToList();
            List<string> stockEpics;
            if (Config.Settings.ContainsKey("VOLATILITY"))
                stockEpics = (from epic in epics where epic != Config.Settings["VOLATILITY"].Split(':')[1] select epic).ToList();
            else
                stockEpics = epicLst;
            var mktLevelsData = _reader.GetMarketLevels(_stopTime, stockEpics);
            var priceData = _reader.GetMarketDataQuotes(_startTime, _stopTime,
                                        CassandraConnection.DATATYPE_STOCK, epicLst);
            if (_hasExpectedResults)
            {
                _readerExpectedResults.GetMarketLevels(_stopTime, stockEpics);
                _readerExpectedResults.GetMarketDataQuotes(_startTime, _stopTime,
                                            CassandraConnection.DATATYPE_STOCK, epicLst);

                _expectedIndicatorData = new Dictionary<string, List<CqlQuote>>();
                var quotes = _readerExpectedResults.GetIndicatorDataQuotes(_startTime, _stopTime,
                        CassandraConnection.DATATYPE_INDICATOR, epicLst);
                foreach (var epicQuotes in quotes)
                {
                    Dictionary<string, List<CqlQuote>> indicatorData = (from quote in epicQuotes.Value
                                                                        group quote by quote.s into g
                                                                        select new { Key = g.Key, Quotes = g.ToList() }).ToDictionary(keyVal => keyVal.Key, keyVal => keyVal.Quotes);
                    indicatorData.Aggregate(_expectedIndicatorData, (agg, keyVal) => { agg.Add(keyVal.Key, keyVal.Value); return agg; });
                }
                _expectedSignalData = new Dictionary<string, List<CqlQuote>>();
                quotes = _readerExpectedResults.GetSignalDataQuotes(_startTime, _stopTime,
                        CassandraConnection.DATATYPE_SIGNAL, epicLst);
                foreach (var epicQuotes in quotes)
                {
                    Dictionary<string, List<CqlQuote>> signalData = (from quote in epicQuotes.Value
                                                                     group quote by quote.s into g
                                                                     select new { Key = g.Key, Quotes = g.ToList() }).ToDictionary(keyVal => keyVal.Key, keyVal => keyVal.Quotes);
                    signalData.Aggregate(_expectedSignalData, (agg, keyVal) => { agg.Add(keyVal.Key, keyVal.Value); return agg; });
                }
                _expectedTradeData = new Dictionary<KeyValuePair<string,DateTime>, Trade>();
                var trades = _readerExpectedResults.GetTrades(_startTime, _stopTime,
                                                                    CassandraConnection.DATATYPE_TRADE, epicLst);
                foreach (var epicTrades in trades)
                {
                    foreach (Trade trade in epicTrades.Value)
                        _expectedTradeData.Add(new KeyValuePair<string, DateTime>(trade.Reference, trade.TradingTime), trade);
                }
                _expectedProfitData = new Dictionary<KeyValuePair<string, DateTime>, double>();
                var profits = _readerExpectedResults.GetProfits(_startTime, _stopTime,
                                                                    CassandraConnection.DATATYPE_PROFIT, epicLst);
                foreach (var epicProfit in profits)
                {
                    foreach (var profit in epicProfit.Value)
                        _expectedProfitData.Add(new KeyValuePair<string, DateTime>(epicProfit.Key, profit.Key), profit.Value);
                }
                PublisherConnection.Instance.SetExpectedResults(_expectedIndicatorData, _expectedSignalData,
                    _expectedTradeData, _expectedProfitData);
            }
            return priceData;
        }
    }

    // this crazy client never updates the positions
    public class ReplayStreamingCrazySeller : ReplayStreamingClient
    {
        static int _numId = 1;
        static int _numRef = 1;

        public override void BookTrade(Trade trade, Portfolio.TradeBookedEvent onTradeBooked, Portfolio.TradeBookedEvent onBookingFailed)
        {
            if (trade.Direction == SIGNAL_CODE.SELL)
            {
                trade.Id = "###DUMMY_TRADE_ID" + _numId++ + "###";
                trade.Reference = "###DUMMY_TRADE_REF" + _numRef++ + "###";
                trade.ConfirmationTime = trade.TradingTime;
                onTradeBooked(trade);
            }
            else
                base.BookTrade(trade, onTradeBooked, onBookingFailed);
        }
    }

    public class ReplayStreamingCrazyBuyer : ReplayStreamingClient
    {
        static int _numId = 1;
        static int _numRef = 1;

        public override void BookTrade(Trade trade, Portfolio.TradeBookedEvent onTradeBooked, Portfolio.TradeBookedEvent onBookingFailed)
        {
            if (trade.Direction == SIGNAL_CODE.BUY)
            {
                trade.Id = "###DUMMY_TRADE_ID" + _numId++ + "###";
                trade.Reference = "###DUMMY_TRADE_REF" + _numRef++ + "###";
                trade.ConfirmationTime = trade.TradingTime;
                onTradeBooked(trade);
            }
            else
                base.BookTrade(trade, onTradeBooked, onBookingFailed);
        }
    }

    public class ReplayStreamingBrokeBuyer : ReplayStreamingClient
    {
        static int _numId = 1;
        static int _numRef = 1;

        public override void BookTrade(Trade trade, Portfolio.TradeBookedEvent onTradeBooked, Portfolio.TradeBookedEvent onBookingFailed)
        {
            if (trade.Direction == SIGNAL_CODE.SELL)
            {
                trade.Id = "###DUMMY_TRADE_ID" + _numId++ + "###";
                trade.Reference = "###DUMMY_TRADE_REF" + _numRef++ + "###";
                trade.ConfirmationTime = trade.TradingTime;
                onBookingFailed(trade);
            }
            else
                base.BookTrade(trade, onTradeBooked, onBookingFailed);
        }
    }

    public class ReplayStreamingLoser : ReplayStreamingClient
    {
        static int _numId = 1;
        static int _numRef = 1;

        public override void BookTrade(Trade trade, Portfolio.TradeBookedEvent onTradeBooked, Portfolio.TradeBookedEvent onBookingFailed)
        {
            if (trade.Direction == SIGNAL_CODE.BUY)
            {
                trade.Id = "###DUMMY_TRADE_ID" + _numId++ + "###";
                trade.Reference = "###DUMMY_TRADE_REF" + _numRef++ + "###";
                trade.ConfirmationTime = trade.TradingTime;
                onBookingFailed(trade);
            }
            else
                base.BookTrade(trade, onTradeBooked, onBookingFailed);
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

        int IStaticDataConnection.GetAnnLatestVersion(string annid, string mktdataid)
        {
            return 1;
        }

        List<decimal> IStaticDataConnection.GetAnnWeights(string annid, string mktdataid, int version)
        {
            return new List<decimal> { -0.436790026694904m, 0.498194032347851m, -0.214276598167734m, -0.141450802162965m, 0.357833455716182m, 0.32193919961431m, -0.441598332459851m, 
                0.0140020817117775m, 0.217530195004088m, -0.373687287268083m, -0.163388582255407m, -0.433217866315142m, -0.13287454267632m, -0.219790011979542m, -0.377691165021477m };
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
            init();
        }

        void init()
        {
            // this attaches the database handle from the publisher to our current reader (csvreader / cassandra)
            try
            {
                _database = ((ReplayStreamingClient)MarketDataConnection.Instance.StreamClient).Reader;
            }
            catch
            {
                _database = null;
            }
            _csvFile = null;
            _csvStockStringBuilder = new StringBuilder();
            _csvIndicatorStringBuilder = new StringBuilder();
            _csvSignalStringBuilder = new StringBuilder();
            _csvTradeStringBuilder = new StringBuilder();
            _csvProfitStringBuilder = new StringBuilder();
            _csvMktDetailsStringBuilder = new StringBuilder();
        }

        void check(DateTime updateTime, string id)
        {
            if (_csvFile == null)
                _csvFile = Config.Settings["PUBLISHING_CSV"];
            if (updateTime == DateTimeOffset.MinValue || id == "")
                throw new ApplicationException("Cannot insert a market data without id and update time");
        }

        string formatDateTime(DateTime dt)
        {
            return string.Format("{0}/{1}/{2} {3}:{4}:{5}", dt.Day, dt.Month, dt.Year,
                dt.Hour, dt.Minute, dt.Second);
        }

        public override void Insert(DateTime updateTime, MarketData mktData, Price price)
        {
            check(updateTime, mktData.Id);
            var newLine = string.Format("{0},{1},{2},{3},{4},{5},{6}{7}",
                DATATYPE_STOCK, mktData.Id,
                formatDateTime(updateTime), mktData.Name,
                price.Bid, price.Offer,
                price.Volume, Environment.NewLine);
            _csvStockStringBuilder.Append(newLine);
        }

        public override void Insert(DateTime updateTime, Indicator indicator, decimal value)
        {
            check(updateTime, indicator.Id);
            if (indicator.Id.Contains("Low") || indicator.Id.Contains("High") ||
                indicator.Id.Contains("CloseBid") || indicator.Id.Contains("CloseOffer")){
                // insert end of day level
                Insert(indicator.Id, value);
            }
            var newLine = string.Format("{0},{1},{2},{3}{4}",
                DATATYPE_INDICATOR, indicator.Id,
                formatDateTime(updateTime), value, Environment.NewLine);
            _csvIndicatorStringBuilder.Append(newLine);
        }

        public override void Insert(DateTime updateTime, Signal signal, SIGNAL_CODE code, decimal stockvalue)
        {
            check(updateTime, signal.Id);
            string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
            var newLine = string.Format("{0},{1},{2},{3},{4},{5}{6}",
                DATATYPE_SIGNAL, signal.Id,
                formatDateTime(updateTime), tradeRef, (int)code, stockvalue, Environment.NewLine);
            _csvSignalStringBuilder.Append(newLine);
        }

        public override void Insert(Trade trade)
        {
            check(trade.TradingTime, trade.Id);
            if (trade.ConfirmationTime == DateTimeOffset.MinValue || trade.Reference == "")
                throw new ApplicationException("Cannot insert a trade without booking information");
            var newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}{9}",
                DATATYPE_TRADE, trade.Epic, formatDateTime(trade.ConfirmationTime), trade.Id, trade.Direction, trade.Price, trade.Size, 
                formatDateTime(trade.TradingTime), trade.Reference,
                Environment.NewLine);
            _csvTradeStringBuilder.Append(newLine);
        }

        public override void Insert(DateTime updateTime, string mktdataid, Value profit)
        {
            var newLine = string.Format("{0},{1},{2}{3}", formatDateTime(updateTime), mktdataid, profit.X,
                Environment.NewLine);
            _csvProfitStringBuilder.Append(newLine);
        }

        public override void Insert(DateTime updateTime, NeuralNetworkForCalibration ann)
        {
            throw new ApplicationException("ANN insertion not implemented");
        }

        public void Insert(string lvlId, decimal value)
        {
            var newLine = string.Format("marketlevels,{0},{1}{2}", lvlId, value, Environment.NewLine);
            _csvMktDetailsStringBuilder.Append(newLine);
        }        

        public override string Close()
        {            
            var csvContent = _csvMktDetailsStringBuilder.ToString();
            csvContent += Environment.NewLine;
            csvContent += _csvStockStringBuilder.ToString();            
            csvContent += Environment.NewLine;
            csvContent += _csvIndicatorStringBuilder.ToString();
            csvContent += Environment.NewLine;
            csvContent += _csvSignalStringBuilder.ToString();
            csvContent += Environment.NewLine;
            csvContent += _csvTradeStringBuilder.ToString();
            csvContent += Environment.NewLine;
            csvContent += _csvProfitStringBuilder.ToString();
            string info = "Generated results in ";
            if (_csvFile != null)
            {
                File.WriteAllText(_csvFile, csvContent);
                info += _csvFile;
            }
            else
                info = "No results to generate";
            Log.Instance.WriteEntry(info, EventLogEntryType.Information);
            Thread.Sleep(1000);            
            init();
            _instance = null;
            return info;
        }
    }

    public class ReplayTester : PublisherConnection
    {
        public const decimal TOLERANCE = 1e-4m;

        public int NbExpectedTrades { get { return _expectedTradeDataCount; } }
        public int NbProducedTrades { get { return _nbPublishedTrades; } }

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

        /*
        public override void Insert(MarketLevels mktDetails)
        {
            // this a source market data, no need to compare
        }*/

        public override void Insert(DateTime updateTime, MarketData mktData, Price price)
        {
            // this a source market data, no need to compare
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
            var time = new DateTime(updateTime.Year, updateTime.Month, updateTime.Day,
                updateTime.Hour, updateTime.Minute, updateTime.Second);
            if (((SIGNAL_CODE)_expectedSignalData[signal.Id].Value(time).Value.Value.Bid != code) ||
                Math.Abs(_expectedSignalData[signal.Id].Value(time).Value.Value.Offer - stockvalue) > TOLERANCE)
            {
                string error;
                if ((SIGNAL_CODE)_expectedSignalData[signal.Id].Value(time).Value.Value.Bid != code)
                    error = "Test failed: signal " + signal.Name + " time " + time.ToShortTimeString() + " expected value " +
                   ((SIGNAL_CODE)_expectedSignalData[signal.Id].Value(time).Value.Value.Bid).ToString() + " != " + code.ToString();
                else
                    error = "Test failed: signal stock value " + signal.Name + " time " + time.ToShortTimeString() + " expected value " +
                   (_expectedSignalData[signal.Id].Value(time).Value.Value.Offer).ToString() + " != " + stockvalue.ToString();
                Log.Instance.WriteEntry(error, EventLogEntryType.Error);
                throw new ApplicationException(error);
            }
        }

        public override void Insert(Trade trade)
        {
            _nbPublishedTrades++;
            var tradeKey = new KeyValuePair<string, DateTime>(trade.Reference, new DateTime(trade.TradingTime.Year, trade.TradingTime.Month, trade.TradingTime.Day, 
                trade.TradingTime.Hour, trade.TradingTime.Minute, trade.TradingTime.Second));
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
        }

        public override void Insert(DateTime updateTime, string mktdataid, Value profit)
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

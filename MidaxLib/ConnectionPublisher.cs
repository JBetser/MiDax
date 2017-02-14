using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using dto.endpoint.search;

namespace MidaxLib
{
    public abstract class PublisherConnection
    {
        public const string DATATYPE_MARKETLEVELS = "marketlevels";
        public const string DATATYPE_STOCK = "mktdata";
        public const string DATATYPE_INDICATOR = "indicators";
        public const string DATATYPE_SIGNAL = "signals";
        public const string DATATYPE_TRADE = "trades";
        public const string DATATYPE_PROFIT = "profit";        

        protected Dictionary<string, TimeSeries> _expectedIndicatorData = null;
        protected Dictionary<string, TimeSeries> _expectedSignalData = null;
        protected Dictionary<KeyValuePair<string, DateTime>, Trade> _expectedTradeData = null;
        protected Dictionary<KeyValuePair<string, DateTime>, double> _expectedProfitData = null;
        protected int _nbPublishedTrades = 0;
        protected int _expectedTradeDataCount = -1;
        
        static protected PublisherConnection _instance = null;
        protected IReaderConnection _database = null;

        public IReaderConnection Database { get { return _database; } }
        
        static public PublisherConnection Instance
        {
            get
            {
                if (_instance == null)
                {
                    bool isCsvConnection = Config.TestReplayCsvGeneratorEnabled || Config.MarketSelectorEnabled;
                    bool isDBConnection = Config.DBPublishingEnabled || Config.ImportEnabled;
                    _instance = (isCsvConnection ? _instance = new ReplayPublisher() :
                                 isDBConnection ? _instance = new CassandraConnection() : _instance = new ReplayTester());
                }
                if (Config.ReplayEnabled)
                {
                    if (_instance != null)
                        _instance._database = ((ReplayStreamingClient)MarketDataConnection.Instance.StreamClient).Reader;
                }
                return _instance;
            }
        }
        
        public abstract void Insert(DateTime updateTime, MarketData mktData, Price price);
        public abstract void Insert(DateTime updateTime, Indicator indicator, decimal value);
        public abstract void Insert(DateTime updateTime, Signal signal, SIGNAL_CODE code, decimal stockvalue);
        public abstract void Insert(Trade trade);
        public abstract void Insert(DateTime updateTime, string mktdataid, Value profit);
        public abstract void Insert(DateTime updateTime, NeuralNetworkForCalibration calibratedNeuralNetwork);
        public abstract void Insert(DateTime updateTime, string mktdataid, int timeframe_mn, IndicatorRobinHood.RobState state);
        
        public void SetExpectedResults(Dictionary<string, List<CqlQuote>> indicatorData, Dictionary<string, List<CqlQuote>> signalData, 
            Dictionary<KeyValuePair<string, DateTime>, Trade> tradeData, Dictionary<KeyValuePair<string, DateTime>, double> profitData)
        {
            _expectedIndicatorData = new Dictionary<string,TimeSeries>();
            _expectedSignalData = new Dictionary<string, TimeSeries>();
            _expectedTradeData = tradeData;
            _expectedTradeDataCount = tradeData.Count;
            _expectedProfitData = profitData;
            _nbPublishedTrades = 0;
            if (indicatorData != null)
            {
                foreach (var indData in indicatorData)
                {
                    if (!_expectedIndicatorData.ContainsKey(indData.Key))
                        _expectedIndicatorData.Add(indData.Key, new TimeSeries());
                    foreach (var value in indData.Value)
                        _expectedIndicatorData[indData.Key].Add(value.t, new Price(value.b.Value, value.o.Value, value.v.Value));
                }
            }
            if (signalData != null)
            {
                foreach (var sigData in signalData)
                {
                    if (!_expectedSignalData.ContainsKey(sigData.Key))
                        _expectedSignalData.Add(sigData.Key, new TimeSeries());
                    foreach (var value in sigData.Value)
                        _expectedSignalData[sigData.Key].Add(value.t, new Price(value.b.Value, value.o.Value, value.v.Value));
                }
            }
        }

        public abstract string Close();

        protected static long ToUnixTimestamp(DateTime dateTime)
        {
            return Convert.ToInt64((DateTime.SpecifyKind(dateTime,DateTimeKind.Utc) - new DateTime(1970, 1, 1).ToUniversalTime()).TotalMilliseconds);
        }

        protected static long ToUnixTimestamp(string dateTime)
        {
            return ToUnixTimestamp(Config.ParseDateTimeLocal(dateTime));
        }
    }
}

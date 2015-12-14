using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public abstract class PublisherConnection
    {
        public const string DATATYPE_STOCK = "stocks";
        public const string DATATYPE_INDICATOR = "indicators";
        public const string DATATYPE_SIGNAL = "signals";
        public const string DATATYPE_TRADE = "trades";

        protected Dictionary<string, TimeSeries> _expectedIndicatorData = null;
        protected Dictionary<string, TimeSeries> _expectedSignalData = null;
        protected Dictionary<KeyValuePair<string, DateTime>, Trade> _expectedTradeData = null;

        static protected PublisherConnection _instance = null;
        
        static public PublisherConnection Instance
        {
            get
            {
                return _instance == null ? ((Config.ReplayEnabled || Config.MarketSelectorEnabled) ? 
                    (Config.Settings.ContainsKey("PUBLISHING_CSV") ? _instance = new ReplayPublisher() :
                                                                        _instance = new ReplayTester()) : 
                                                                        _instance = new CassandraConnection()) 
                : _instance; }
        }

        public abstract void Insert(DateTime updateTime, MarketData mktData, Price price);
        public abstract void Insert(DateTime updateTime, Indicator indicator, decimal value);
        public abstract void Insert(DateTime updateTime, Signal signal, SIGNAL_CODE code);
        public abstract void Insert(Trade trade);
        public abstract void Insert(Value gain);

        public void SetExpectedResults(Dictionary<string, List<CqlQuote>> indicatorData, Dictionary<string, List<CqlQuote>> signalData, Dictionary<KeyValuePair<string, DateTime>, Trade> tradeData)
        {
            _expectedIndicatorData = new Dictionary<string,TimeSeries>();
            _expectedSignalData = new Dictionary<string, TimeSeries>();
            _expectedTradeData = tradeData;
            foreach (var indData in indicatorData)
            {
                if (!_expectedIndicatorData.ContainsKey(indData.Key))
                    _expectedIndicatorData.Add(indData.Key, new TimeSeries());
                foreach (var value in indData.Value)
                    _expectedIndicatorData[indData.Key].Add(value.t.DateTime, new Price(value.b.Value, value.o.Value, value.v.Value));
            }   
            foreach (var sigData in signalData)
            {
                if (!_expectedSignalData.ContainsKey(sigData.Key))
                    _expectedSignalData.Add(sigData.Key, new TimeSeries());
                foreach (var value in sigData.Value)
                    _expectedSignalData[sigData.Key].Add(value.t.DateTime, new Price(value.b.Value, value.o.Value, value.v.Value));
            }
        }

        public abstract string Close();

        protected static long ToUnixTimestamp(DateTime dateTime)
        {
            return Convert.ToInt64((DateTime.SpecifyKind(dateTime,DateTimeKind.Utc) - new DateTime(1970, 1, 1).ToUniversalTime()).TotalMilliseconds);
        }
    }
}

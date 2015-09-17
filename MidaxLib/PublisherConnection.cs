using System;
using System.Collections.Generic;
using System.IO;
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

        static protected PublisherConnection _instance = null;
        
        static public PublisherConnection Instance
        {
            get { return _instance == null ? (Config.Settings["TRADING_MODE"] == "REPLAY" ? 
                                                _instance = new CsvPublisher() : 
                                                _instance = new CassandraConnection()) 
                : _instance; }
        }

        public abstract void Insert(DateTime updateTime, MarketData mktData, Price price);

        public abstract void Insert(DateTime updateTime, Indicator indicator, decimal value);

        public abstract void Insert(DateTime updateTime, Signal signal, SIGNAL_CODE code);

        public virtual void Close()
        {
        }

        protected static long ToUnixTimestamp(DateTime dateTime)
        {
            return Convert.ToInt64((DateTime.SpecifyKind(dateTime,DateTimeKind.Utc) - new DateTime(1970, 1, 1).ToUniversalTime()).TotalMilliseconds);
        }
    }

    public class CsvPublisher : PublisherConnection
    {
        string _csvFile;
        StringBuilder _csvStockStringBuilder;
        StringBuilder _csvIndicatorStringBuilder;
        StringBuilder _csvSignalStringBuilder;

        static public new CsvPublisher Instance
        {
            get
            {
                return (CsvPublisher)_instance;
            }
        }

        public CsvPublisher() 
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

        public override void Close()
        {
            string csvContent = _csvStockStringBuilder.ToString();
            csvContent += Environment.NewLine;
            csvContent += _csvIndicatorStringBuilder.ToString();
            csvContent += Environment.NewLine;
            csvContent += _csvSignalStringBuilder.ToString();
            File.WriteAllText(_csvFile, csvContent);
        }
    }
}

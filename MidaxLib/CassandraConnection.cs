using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;

namespace MidaxLib
{
    public class CassandraConnection
    {
        public const string DATATYPE_STOCK = "stocks";
        public const string DATATYPE_INDICATOR = "indicators";
        public const string DATATYPE_SIGNAL = "signals";

        static CassandraConnection _instance = null;
        Cluster _cluster = null;
        ISession _session = null;

        CassandraConnection() 
        {
            _cluster = Cluster.Builder().AddContactPoint("127.0.0.1").Build();
            _session = _cluster.Connect();
        }

        static public CassandraConnection Instance
        {
            get { return _instance == null ? _instance = new CassandraConnection() : _instance; }
        }

        public void Insert(DateTime updateTime, MarketData mktData, Price price)
        {
            long tmp = ToUnixTimestamp(updateTime);
            _session.Execute(string.Format("insert into historicaldata.{0} (stockid, trading_time,  name,  bid,  offer,  volume) values ('{1}', {2}, '{3}', {4}, {5}, {6})",
                DATATYPE_STOCK, mktData.Id, ToUnixTimestamp(updateTime), mktData.Name, price.Bid, price.Offer, price.Volume));
        }

        public void Insert(DateTime updateTime, Indicator indicator, decimal value)
        {
            _session.Execute(string.Format("insert into historicaldata.{0} (indicatorid, trading_time, value) values ('{1}', {2}, {3})",
                DATATYPE_INDICATOR, indicator.Id, ToUnixTimestamp(updateTime), value));
        }

        public void Insert(DateTime updateTime, Signal signal, SIGNAL_CODE code)
        {
            _session.Execute(string.Format("insert into historicaldata.{0} (signalid, trading_time,  value) values ('{1}', {2}, {3})",
                DATATYPE_SIGNAL, signal.Id, ToUnixTimestamp(updateTime), (int)code));
        }

        public string GetJSON(DateTime startTime, DateTime stopTime, string type, string id)
        {
            RowSet rowSet =  _session.Execute(string.Format("select JSON * from historicaldata.{0} where {1}id='{2}';",
                type, type.Substring(0, type.Length - 1), id));
            string json = "[";
            foreach (var row in rowSet)
                json += row[0] + ",";
            return json.Substring(0, json.Length - 1) + "]";

        }

        protected static long ToUnixTimestamp(DateTime dateTime)
        {
            return Convert.ToInt64((dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalMilliseconds);
        }
        
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;

namespace BPModel
{
    class CassandraConnection
    {
        static CassandraConnection _instance = null;
        Cluster _cluster = null;
        ISession _session = null;

        CassandraConnection() 
        {
            _cluster = Cluster.Builder().AddContactPoint("192.168.1.26").Build();
            _session = _cluster.Connect();
        }

        static public CassandraConnection Instance
        {
            get { return _instance == null ? _instance = new CassandraConnection() : _instance; }
        }

        public void Insert(DateTime updateTime, MarketData mktData, Price price)
        {
            _session.Execute(string.Format("insert into historicaldata.stocks (stockid, trading_time,  name,  bid,  offer,  volume) values ('{0}', {1}, '{2}', {3}, {4}, {5})",
                mktData.Id, ToUnixTimestamp(updateTime), mktData.Name, price.Bid, price.Offer, price.Volume));
        }

        protected static long ToUnixTimestamp(DateTime dateTime)
        {
            return Convert.ToInt64((dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalMilliseconds);
        }
        
    }
}

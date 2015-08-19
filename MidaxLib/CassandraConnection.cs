using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;
using Newtonsoft.Json;

namespace MidaxLib
{
    public struct CqlQuote
    {
        public string s;
        public DateTimeOffset t;
        public decimal b;
        public string n;
        public decimal o;
        public int v;
        public CqlQuote(Row row)
        {
            s = (string)row[0];
            t = (DateTimeOffset)row[1];
            b = (decimal)row[2];
            n = (string)row[3];
            o = (decimal)row[4];
            v = (int)row[5];
        }
    }

    public class Gap
    {
        public decimal value;
        public Tuple<CqlQuote, CqlQuote> quotes;
        public Gap(decimal val, Tuple<CqlQuote, CqlQuote> quotes)
        {
            this.value = val;
            this.quotes = quotes;
        }
    }

    public class GapSorter : IComparer<Gap>
    {
        public int Compare(Gap c1, Gap c2)
        {
            return c2.value.CompareTo(c1.value);
        }
    }

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

        RowSet getRow(DateTime startTime, DateTime stopTime, string type, string id){
            return _session.Execute(string.Format("select * from historicaldata.{0} where {1}id='{2}' and trading_time >= {3} and trading_time <= {4};",
                                type, type.Substring(0, type.Length - 1), id, ToUnixTimestamp(startTime), ToUnixTimestamp(stopTime)));
        }

        public string GetJSON(DateTime startTime, DateTime stopTime, string type, string id)
        {
            RowSet rowSet = getRow(startTime, stopTime, type, id);

            List<CqlQuote> filteredQuotes = new List<CqlQuote>();
            decimal? prevQuoteValue = null;
            CqlQuote prevQuote = new CqlQuote();
            bool? trendUp = null;
            // find local minima
            List<Gap> gaps = new List<Gap>();
            foreach (Row row in rowSet.GetRows())
            {
                CqlQuote cqlQuote = new CqlQuote(row);
                decimal quoteValue = (cqlQuote.b + cqlQuote.o) / 2m;
                if (!prevQuoteValue.HasValue)
                {
                    filteredQuotes.Add(cqlQuote);
                    prevQuoteValue = quoteValue;
                    prevQuote = cqlQuote;
                    continue;
                }                
                if (!trendUp.HasValue)
                {
                    trendUp = quoteValue > prevQuoteValue;
                    prevQuoteValue = quoteValue;
                    prevQuote = cqlQuote;
                    continue;
                }
                if (Math.Abs((cqlQuote.t - prevQuote.t).TotalSeconds) < 10)
                    continue;
                if (Math.Abs(quoteValue - prevQuoteValue.Value) < 2)
                    continue;
                if (((quoteValue < prevQuoteValue) && trendUp.Value) ||
                    ((quoteValue > prevQuoteValue) && !trendUp.Value))
                {
                    filteredQuotes.Add(prevQuote);
                    //gaps.Add(new Gap(Math.Abs(quoteValue - prevQuoteValue.Value), new Tuple<CqlQuote, CqlQuote>(prevQuote, cqlQuote)));
                    trendUp = !trendUp;
                }
                prevQuoteValue = quoteValue;
                prevQuote = cqlQuote;
            }
            filteredQuotes.Add(prevQuote);

            //gaps.Sort(new GapSorter());
                                    
            string json = "[";
            foreach (var row in filteredQuotes)
            {
                json += JsonConvert.SerializeObject(row) + ",";
            }
            return json.Substring(0, json.Length - 1) + "]";
        }

        protected static long ToUnixTimestamp(DateTime dateTime)
        {
            return Convert.ToInt64((dateTime - new DateTime(1970, 1, 1).ToLocalTime()).TotalMilliseconds);
        }
        
    }
}

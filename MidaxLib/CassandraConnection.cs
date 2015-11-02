using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public decimal? b;
        public string n;
        public decimal? o;
        public int? v;
        public CqlQuote(Row row)
        {
            s = (string)row[0];
            t = (DateTimeOffset)row[1];
            b = (decimal)row[2];
            n = (string)row[3];
            o = (decimal)row[4];
            v = (int)row[5];
        }
        public CqlQuote(string stockId, DateTimeOffset tradingTime, string stockName, decimal? bid, decimal? offer, int? volume)
        {
            s = stockId;
            t = tradingTime;
            b = bid;
            n = stockName;
            o = offer;
            v = volume;
        }
    }

    public class Gap
    {
        public decimal value;
        public CqlQuote quoteLow;
        public CqlQuote quoteHigh;
        public Gap(decimal val, Tuple<CqlQuote, CqlQuote> quotes)
        {
            this.value = val;
            this.quoteLow = quotes.Item1;
            this.quoteHigh = quotes.Item2;
        }
    }

    public class GapSorter : IComparer<Gap>
    {
        public int Compare(Gap c1, Gap c2)
        {
            return c2.value.CompareTo(c1.value);
        }
    }

    public class CassandraConnection : PublisherConnection, IReaderConnection
    { 
        Cluster _cluster = null;
        ISession _session = null;

        static public new CassandraConnection Instance
        {
            get
            {
                return (CassandraConnection)(_instance == null ? _instance = new CassandraConnection() : _instance);
            }
        }

        public CassandraConnection() 
        {
            if (Config.Settings != null)
            {
                _cluster = Cluster.Builder().AddContactPoint(Config.Settings["PUBLISHING_CONTACTPOINT"]).Build();
                _session = _cluster.Connect();
            }
        }

        public override string Close()
        {
            string info = "";
            if (_session != null)
            {
                _session.Dispose();
                info = "Closed connection to Cassandra";
                Log.Instance.WriteEntry(info, EventLogEntryType.Information);                
            }
            _instance = null;
            return info;
        }

        public override void Insert(DateTime updateTime, MarketData mktData, Price price)
        {
            if (_session == null || !Config.PublishingEnabled)
                return;
            _session.Execute(string.Format("insert into historicaldata.{0} (stockid, trading_time,  name,  bid,  offer,  volume) values ('{1}', {2}, '{3}', {4}, {5}, {6})",
                DATATYPE_STOCK, mktData.Id, ToUnixTimestamp(updateTime), mktData.Name, price.Bid, price.Offer, price.Volume));
        }

        public override void Insert(DateTime updateTime, Indicator indicator, decimal value)
        {
            if (_session == null || !Config.PublishingEnabled)
                return;
            _session.Execute(string.Format("insert into historicaldata.{0} (indicatorid, trading_time, value) values ('{1}', {2}, {3})",
                DATATYPE_INDICATOR, indicator.Id, ToUnixTimestamp(updateTime), value));
        }

        public override void Insert(DateTime updateTime, Signal signal, SIGNAL_CODE code)
        {
            if (_session == null || !Config.PublishingEnabled)
                return;
            _session.Execute(string.Format("insert into historicaldata.{0} (signalid, trading_time,  value) values ('{1}', {2}, {3})",
                DATATYPE_SIGNAL, signal.Id, ToUnixTimestamp(updateTime), (int)code));
        }

        RowSet getRows(DateTime startTime, DateTime stopTime, string type, string id){
            if (_session == null)
                return null; 
            return _session.Execute(string.Format("select * from historicaldata.{0} where {1}id='{2}' and trading_time >= {3} and trading_time <= {4};",
                                type, type.Substring(0, type.Length - 1), id, ToUnixTimestamp(startTime), ToUnixTimestamp(stopTime)));
        }

        List<CqlQuote> getQuotes(DateTime startTime, DateTime stopTime, string type, string id)
        {
            RowSet rowSet = getRows(startTime, stopTime, type, id);
            List<CqlQuote> quotes = new List<CqlQuote>();
            foreach (Row row in rowSet.GetRows())
                quotes.Add(new CqlQuote(row));
            return quotes;
        }

        List<CqlQuote> IReaderConnection.GetMarketDataQuotes(DateTime startTime, DateTime stopTime, string type, string id)
        {
            return getQuotes(startTime, stopTime, type, id);            
        }

        List<CqlQuote> IReaderConnection.GetIndicatorDataQuotes(DateTime startTime, DateTime stopTime, string type, string id)
        {
            return getQuotes(startTime, stopTime, type, id);
        }

        List<CqlQuote> IReaderConnection.GetSignalDataQuotes(DateTime startTime, DateTime stopTime, string type, string id)
        {
            return getQuotes(startTime, stopTime, type, id);
        }

        public string GetJSON(DateTime startTime, DateTime stopTime, string type, string id)
        {
            if (_session == null)
                return "[]";
            RowSet rowSet = getRows(startTime, stopTime, type, id);
            double intervalSeconds = Math.Ceiling((stopTime - startTime).TotalHours);
            List<CqlQuote> filteredQuotes = new List<CqlQuote>();
            decimal? prevQuoteValue = null;
            CqlQuote prevQuote = new CqlQuote();
            bool? trendUp = null;
            // find local minima
            List<Gap> gaps = new List<Gap>();
            foreach (Row row in rowSet.GetRows())
            {
                CqlQuote cqlQuote = new CqlQuote(row);
                decimal quoteValue = (cqlQuote.b + cqlQuote.o).Value / 2m;
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
                if (Math.Abs((cqlQuote.t - prevQuote.t).TotalSeconds) < intervalSeconds)
                    continue;
                if (Math.Abs(quoteValue - prevQuoteValue.Value) < 2)
                    continue;
                if (((quoteValue < prevQuoteValue) && trendUp.Value) ||
                    ((quoteValue > prevQuoteValue) && !trendUp.Value))
                {
                    trendUp = !trendUp;
                    filteredQuotes.Add(prevQuote);
                    gaps.Add(new Gap(Math.Abs(quoteValue - prevQuoteValue.Value), 
                        new Tuple<CqlQuote,CqlQuote>(trendUp.Value ? prevQuote : cqlQuote, trendUp.Value ? cqlQuote : prevQuote)));
                }
                prevQuoteValue = quoteValue;
                prevQuote = cqlQuote;
            }
            filteredQuotes.Add(prevQuote);

            gaps.Sort(new GapSorter());
            while (filteredQuotes.Count > 500)
            {
                filteredQuotes.Remove(gaps.ElementAt(0).quoteLow);
                gaps.RemoveAt(0);
            }
                                    
            string json = "[";
            foreach (var row in filteredQuotes)
            {
                json += JsonConvert.SerializeObject(row) + ",";
            }
            return json.Substring(0, json.Length - 1) + "]";
        } 
    }
}

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
    public class CqlQuote
    {
        public string s;
        public DateTimeOffset t;
        public decimal? b;
        public string n;
        public decimal? o;
        public int? v;
        public CqlQuote()
        {
        }
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
        public virtual decimal ScaleValue(decimal avg, decimal scale)
        {
            b = o = (b + o).Value / 2m;
            return b.Value;
        }         
        static public CqlQuote CreateInstance(string type, Row row)
        {
            switch(type)
            {
                case PublisherConnection.DATATYPE_STOCK:
                    return new CqlQuote(row);
                case PublisherConnection.DATATYPE_INDICATOR:
                    return new CqlIndicator(row);
                case PublisherConnection.DATATYPE_SIGNAL:
                    return new CqlSignal(row);
            }
            return null;
        }
    }

    public class CqlIndicator : CqlQuote
    {
        public CqlIndicator(Row row)
        {
            s = (string)row[0];
            t = (DateTimeOffset)row[1];
            b = (decimal)row[2];
            n = (string)row[0];
            o = (decimal)row[2];
            v = 0;
        }
        public CqlIndicator(string stockId, DateTimeOffset tradingTime, string stockName, decimal? value)
        {
            s = stockId;
            t = tradingTime;
            b = value;
            n = stockName;
            o = value;
            v = 0;
        }
        public override decimal ScaleValue(decimal avg, decimal scale)
        {
            if (s.StartsWith("LR"))
                b = o = avg + b.Value * scale;
            return b.Value;
        }  
    }

    public class CqlSignal : CqlQuote
    {
        public CqlSignal(Row row)
        {
            s = (string)row[0];
            t = (DateTimeOffset)row[1];
            b = Convert.ToInt32(row[2]);
            n = (string)row[0];
            o = Convert.ToInt32(row[2]);
            v = 0;
        }
        public CqlSignal(string stockId, DateTimeOffset tradingTime, string stockName, SIGNAL_CODE? value)
        {
            s = stockId;
            t = tradingTime;
            b = Convert.ToInt32(value);
            n = stockName;
            o = Convert.ToInt32(value);
            v = 0;
        }
        public override decimal ScaleValue(decimal avg, decimal scale)
        {
            b = o = avg + (b.Value - 2) * scale;
            return b.Value;
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
        decimal _avg = 0m;
        decimal _scale = 0m;

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
            _session.Execute(string.Format("insert into historicaldata.{0} (signalid, trading_time, value) values ('{1}', {2}, {3})",
                DATATYPE_SIGNAL, signal.Id, ToUnixTimestamp(updateTime), Convert.ToInt32(code)));
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
            double intervalSeconds = Math.Max(1, Math.Ceiling((stopTime - startTime).TotalSeconds) / 250);
            double intervalSecondsLarge = Math.Max(1, Math.Ceiling((stopTime - startTime).TotalSeconds) / 100);
            List<CqlQuote> filteredQuotes = new List<CqlQuote>();
            decimal? prevQuoteValue = null;
            CqlQuote prevQuote = new CqlQuote();
            bool? trendUp = null;
            // find local minima
            List<Gap> gaps = new List<Gap>();
            SortedList<decimal, CqlQuote> buffer = new SortedList<decimal,CqlQuote>();

            decimal min = 1000000;
            decimal max = 0;
            List<CqlQuote> quotes = new List<CqlQuote>();
            foreach (Row row in rowSet.GetRows())
            {
                CqlQuote cqlQuote = CqlQuote.CreateInstance(type, row);
                if (cqlQuote.b < min)
                    min = cqlQuote.b.Value;
                if (cqlQuote.b > max)
                    max = cqlQuote.b.Value;
                quotes.Add(cqlQuote);
            }
            if (type == PublisherConnection.DATATYPE_STOCK)
            {
                _avg = (min + max) / 2m;
                _scale = (max - min) / 2m;
            }
            foreach (CqlQuote cqlQuote in quotes)
            {
                decimal quoteValue = cqlQuote.ScaleValue(_avg, _scale);
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
                    if ((prevQuote.t - cqlQuote.t).TotalSeconds < intervalSeconds)
                        buffer.Add(quoteValue, cqlQuote);
                    else
                        filteredQuotes.Add(cqlQuote);
                    continue;
                }
                if (((quoteValue < prevQuoteValue) && trendUp.Value) ||
                    ((quoteValue > prevQuoteValue) && !trendUp.Value))
                {
                    if ((prevQuote.t - cqlQuote.t).TotalSeconds < intervalSeconds)
                    {
                        if (!buffer.ContainsKey(quoteValue))
                            buffer.Add(quoteValue, cqlQuote);
                        continue;
                    }
                    if (buffer.Count > 1)
                    {
                        if (buffer.First().Value.t > buffer.Last().Value.t)
                        {
                            filteredQuotes.Add(buffer.First().Value);
                            filteredQuotes.Add(buffer.Last().Value);
                        }
                        else
                        {
                            filteredQuotes.Add(buffer.Last().Value);
                            filteredQuotes.Add(buffer.First().Value);
                        }
                    }
                    else if (buffer.Count == 1)
                        filteredQuotes.Add(buffer.First().Value);
                    buffer.Clear();
                    trendUp = !trendUp;
                    //gaps.Add(new Gap(Math.Abs(quoteValue - prevQuoteValue.Value), 
                    //    new Tuple<CqlQuote,CqlQuote>(trendUp.Value ? prevQuote : cqlQuote, trendUp.Value ? cqlQuote : prevQuote)));
                }
                else
                {
                    if ((prevQuote.t - cqlQuote.t).TotalSeconds < intervalSecondsLarge)
                    {
                        if (!buffer.ContainsKey(quoteValue))
                            buffer.Add(quoteValue, cqlQuote);
                        continue;
                    }
                    if (buffer.Count > 1)
                    {
                        if (buffer.First().Value.t > buffer.Last().Value.t)
                        {
                            filteredQuotes.Add(buffer.First().Value);
                            filteredQuotes.Add(buffer.Last().Value);
                        }
                        else
                        {
                            filteredQuotes.Add(buffer.Last().Value);
                            filteredQuotes.Add(buffer.First().Value);
                        }
                    }
                    else if (buffer.Count == 1)
                        filteredQuotes.Add(buffer.First().Value);
                    buffer.Clear();
                }
                prevQuoteValue = quoteValue;
                prevQuote = cqlQuote;
            }
            if (filteredQuotes.Last() != prevQuote)
                filteredQuotes.Add(prevQuote);
            /*
            gaps.Sort(new GapSorter());
            while (filteredQuotes.Count > 500)
            {
                filteredQuotes.Remove(gaps.ElementAt(0).quoteLow);
                gaps.RemoveAt(0);
            }*/
                                    
            string json = "[";
            foreach (var row in filteredQuotes)
            {
                json += JsonConvert.SerializeObject(row) + ",";
            }
            return json.Substring(0, json.Length - 1) + "]";
        } 
    }
}

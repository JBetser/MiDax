using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;
using dto.endpoint.search;
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
        public decimal MidPrice()
        {
            return ScaleValue(0,0);
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
                b = o = avg + b.Value * scale * 2m;
            else if (s.StartsWith("WMVol"))
                b = o = avg - scale / 2m + b.Value * scale / 20m;
            return b.Value;
        }  
    }

    public class CqlSignal : CqlQuote
    {
        public CqlSignal(Row row)
        {
            s = (string)row[0];
            t = (DateTimeOffset)row[1];
            b = Convert.ToInt32(row[4]);
            n = (string)row[0];
            o = (decimal)row[2];
            v = 0;
        }
        public CqlSignal(string stockId, DateTimeOffset tradingTime, string stockName, SIGNAL_CODE? value, decimal stockvalue)
        {
            s = stockId;
            t = tradingTime;
            b = Convert.ToInt32(value);
            n = stockName;
            o = stockvalue;
            v = 0;
        }
        public override decimal ScaleValue(decimal avg, decimal scale)
        {
            b = avg + (b.Value - 2) * scale;
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

    public class CassandraConnection : PublisherConnection, IReaderConnection, IStaticDataConnection
    { 
        Cluster _cluster = null;
        ISession _session = null;
        Dictionary<string, decimal> _avg = new Dictionary<string, decimal>();
        Dictionary<string, decimal> _scale = new Dictionary<string, decimal>();
        const string DB_BUSINESSDATA = "business";
        const string DB_HISTORICALDATA = "historical";
        const string EXCEPTION_CONNECTION_CLOSED = "Cassandra session is closed";
        static string DB_INSERTION = "insert into {0}data." + (Config.ReplayEnabled ? "dummy" : "") + "{1} ";
        static string DB_SELECTION = "select * from {0}data." + (Config.UATSourceDB ? "dummy" : "") + "{1} ";

        public CassandraConnection() 
        {
            _database = this;
            if (Config.Settings != null)
            {
                _cluster = Cluster.Builder().AddContactPoint(Config.Settings["DB_CONTACTPOINT"]).Build();
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
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            _session.Execute(string.Format(DB_INSERTION + "(stockid, trading_time,  name,  bid,  offer,  volume) values ('{2}', {3}, '{4}', {5}, {6}, {7})",
                DB_HISTORICALDATA, DATATYPE_STOCK, mktData.Id, ToUnixTimestamp(updateTime), mktData.Name, price.Bid, price.Offer, price.Volume));
        }

        public override void Insert(DateTime updateTime, Indicator indicator, decimal value)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            _session.Execute(string.Format(DB_INSERTION + "(indicatorid, trading_time, value) values ('{2}', {3}, {4})",
                DB_HISTORICALDATA, DATATYPE_INDICATOR, indicator.Id, ToUnixTimestamp(updateTime), value));
        }

        public override void Insert(DateTime updateTime, Signal signal, SIGNAL_CODE code, decimal stockvalue)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            string tradeRef = signal.Trade == null ? null : " " + signal.Trade.Reference;
            _session.Execute(string.Format(DB_INSERTION + "(signalid, trading_time, tradeid, value, stockvalue) values ('{2}', {3}, '{4}', {5}, {6})",
                DB_HISTORICALDATA, DATATYPE_SIGNAL, signal.Id, ToUnixTimestamp(updateTime), tradeRef, Convert.ToInt32(code), stockvalue));
        }

        public override void Insert(Trade trade)
        {
            if (trade.TradingTime == DateTimeOffset.MinValue || trade.ConfirmationTime == DateTimeOffset.MinValue || trade.Reference == "")
                throw new ApplicationException("Cannot insert a trade without booking information");
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            _session.Execute(string.Format(DB_INSERTION + "(tradeid, trading_time, confirmation_time, stockid, direction, size) values ('{2}', {3}, {4}, '{5}', {6}, {7})",
                DB_BUSINESSDATA, DATATYPE_TRADE, trade.Reference, ToUnixTimestamp(trade.TradingTime), ToUnixTimestamp(trade.ConfirmationTime), trade.Epic, Convert.ToInt32(trade.Direction), trade.Size));
        }

        public override void Insert(DateTime updateTime, Value profit)
        {
            throw new ApplicationException("Profit insertion not implemented in Cassandra");
        }

        public override void Insert(DateTime insertTime, NeuralNetworkForCalibration ann)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            _session.Execute(string.Format("insert into staticdata.anncalibration(annid, stockid, version, insert_time, weights) values ('{0}', '{1}', {2}, {3}, {4})",
                ann.AnnId, ann.StockId, ann.Version, ToUnixTimestamp(insertTime), JsonConvert.SerializeObject(ann.Weights)));
        }

        public override void Insert(MarketLevels mktDetails)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            var publishTime = Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]);
            var ts = ToUnixTimestamp(publishTime);
            _session.Execute(string.Format(DB_INSERTION + "(indicatorid, trading_time, value) values ('{2}', {3}, {4})",
                DB_HISTORICALDATA, DATATYPE_INDICATOR, "LVLHigh_" + mktDetails.AssetId, ts, mktDetails.High));
            _session.Execute(string.Format(DB_INSERTION + "(indicatorid, trading_time, value) values ('{2}', {3}, {4})",
                DB_HISTORICALDATA, DATATYPE_INDICATOR, "LVLLow_" + mktDetails.AssetId, ts, mktDetails.Low));
            _session.Execute(string.Format(DB_INSERTION + "(indicatorid, trading_time, value) values ('{2}', {3}, {4})",
                DB_HISTORICALDATA, DATATYPE_INDICATOR, "LVLCloseBid_" + mktDetails.AssetId, ts, mktDetails.CloseBid));
            _session.Execute(string.Format(DB_INSERTION + "(indicatorid, trading_time, value) values ('{2}', {3}, {4})",
                DB_HISTORICALDATA, DATATYPE_INDICATOR, "LVLCloseOffer_" + mktDetails.AssetId, ts, mktDetails.CloseOffer));
        }

        MarketLevels? IReaderConnection.GetMarketLevels(DateTime updateTime, string epic)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            // process previous day
            updateTime = updateTime.AddDays(-1);
            RowSet value = null;
            decimal high = 0m;
            string indicator = "";
            try
            {
                indicator = "LVLHigh_" + epic;
                value = _session.Execute(string.Format("select value from historicaldata.indicators where indicatorid='{0}' and trading_time={1}",
                    indicator, ToUnixTimestamp(updateTime)));
                high = (decimal)value.First()[0];
            }
            catch
            {
                value = null;
                Log.Instance.WriteEntry("Could not retrieve level indicators for previous COB date. Indicator: " + indicator, EventLogEntryType.Warning);
            }
            if (value == null)
                return null;             
            value = _session.Execute(string.Format("select value from historicaldata.indicators where indicatorid='{0}' and trading_time={1}",
                "LVLLow_" + epic, ToUnixTimestamp(updateTime)));
            decimal low = (decimal)value.First()[0];
            value = _session.Execute(string.Format("select value from historicaldata.indicators where indicatorid='{0}' and trading_time={1}",
                "LVLCloseBid_" + epic, ToUnixTimestamp(updateTime)));
            decimal closeBid = (decimal)value.First()[0];
            value = _session.Execute(string.Format("select value from historicaldata.indicators where indicatorid='{0}' and trading_time={1}",
                "LVLCloseOffer_" + epic, ToUnixTimestamp(updateTime)));
            decimal closeOffer = (decimal)value.First()[0];
            return new MarketLevels(epic, low, high, closeBid, closeOffer);
        }

        int IStaticDataConnection.GetAnnLatestVersion(string annid, string stockid)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            RowSet versions =  _session.Execute(string.Format("select version from staticdata.anncalibration where annid='{0}' and stockid='{1}' ALLOW FILTERING",
                annid, stockid));
            Row lastVersion = null;
            foreach (var version in versions)
                lastVersion = version;
            return (int)lastVersion[0];
        }

        List<decimal> IStaticDataConnection.GetAnnWeights(string annid, string stockid, int version)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            RowSet weights = _session.Execute(string.Format("select weights from staticdata.anncalibration where annid='{0}' and stockid='{1}' and version={2} ALLOW FILTERING",
                annid, stockid, version));
            if (weights.Count() != 1)
                return null;
            return (List<decimal>)weights.First()[0];
        }

        RowSet getRows(DateTime startTime, DateTime stopTime, string type, string id){
            if (_session == null)
                return null;
            return _session.Execute(string.Format(DB_SELECTION + "where {2}id='{3}' and trading_time >= {4} and trading_time <= {5}",
                                DB_HISTORICALDATA, type, type.Substring(0, type.Length - 1), id, ToUnixTimestamp(startTime), ToUnixTimestamp(stopTime)));
        }

        List<Trade> getTrades(DateTime startTime, DateTime stopTime, string type, string stockid)
        {
            throw new ApplicationException("Trade reading not implemented");
        }

        List<CqlQuote> getQuotes(DateTime startTime, DateTime stopTime, string type, string id)
        {
            RowSet rowSet = getRows(startTime, stopTime, type, id);
            List<CqlQuote> quotes = new List<CqlQuote>();
            foreach (Row row in rowSet.GetRows())
                quotes.Add(new CqlQuote(row));
            quotes.Reverse();
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

        List<Trade> IReaderConnection.GetTrades(DateTime startTime, DateTime stopTime, string type, string id)
        {
            return getTrades(startTime, stopTime, type, id);
        }

        List<KeyValuePair<DateTime, double>> IReaderConnection.GetProfits(DateTime startTime, DateTime stopTime, string type, string id)
        {
            return null;
        }

        void IReaderConnection.CloseConnection()
        {
            Close();
        }

        public string GetJSON(DateTime startTime, DateTime stopTime, string type, string id, bool auto_select)
        {
            if (_session == null)
                return "[]";
            RowSet rowSet = getRows(startTime, stopTime, type, id);
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
            DateTime ts = new DateTime(quotes.Last().t.Ticks);
            DateTime te = new DateTime(quotes.First().t.Ticks);
            startTime = startTime > te ? startTime : ts;
            stopTime = stopTime < te ? stopTime : te;
            double intervalSeconds = Math.Max(1, Math.Ceiling((stopTime - startTime).TotalSeconds) / 250);
            double intervalSecondsLarge = Math.Max(1, Math.Ceiling((stopTime - startTime).TotalSeconds) / 100);            
            string keyAvg = id.Split('_').Last() + "_" +startTime.ToShortDateString();
            if (type == PublisherConnection.DATATYPE_STOCK)
            {
                _avg[keyAvg] = (min + max) / 2m;
                _scale[keyAvg] = (max - min) / 2m;
            }
            foreach (CqlQuote cqlQuote in quotes)
            {
                decimal quoteValue = cqlQuote.ScaleValue(_avg[keyAvg], _scale[keyAvg]);
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
                    if (auto_select && (prevQuote.t - cqlQuote.t).TotalSeconds < intervalSeconds)
                        buffer.Add(quoteValue, cqlQuote);
                    else
                        filteredQuotes.Add(cqlQuote);
                    continue;
                }
                if (((quoteValue < prevQuoteValue) && trendUp.Value) ||
                    ((quoteValue > prevQuoteValue) && !trendUp.Value))
                {
                    if (auto_select && (prevQuote.t - cqlQuote.t).TotalSeconds < intervalSeconds)
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
                }
                else
                {
                    if (auto_select && (prevQuote.t - cqlQuote.t).TotalSeconds < intervalSecondsLarge)
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
                buffer.Add(quoteValue, cqlQuote);
                prevQuoteValue = quoteValue;
                prevQuote = cqlQuote;
            }
            if (filteredQuotes.Last() != prevQuote)
                filteredQuotes.Add(prevQuote);
                                    
            string json = "[";
            foreach (var row in filteredQuotes)
            {
                json += JsonConvert.SerializeObject(row) + ",";
            }
            return json.Substring(0, json.Length - 1) + "]";
        } 
    }
}

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
            if ((int)b.Value == (int)SIGNAL_CODE.FAILED)
                b = 0;
            else
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
            executeQuery(string.Format(DB_INSERTION + "(stockid, trading_time,  name,  bid,  offer,  volume) values ('{2}', {3}, '{4}', {5}, {6}, {7})",
                DB_HISTORICALDATA, DATATYPE_STOCK, mktData.Id, ToUnixTimestamp(updateTime), mktData.Name, price.Bid, price.Offer, price.Volume));
        }

        public override void Insert(DateTime updateTime, Indicator indicator, decimal value)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            executeQuery(string.Format(DB_INSERTION + "(indicatorid, trading_time, value) values ('{2}', {3}, {4})",
                DB_HISTORICALDATA, DATATYPE_INDICATOR, indicator.Id, ToUnixTimestamp(updateTime), value));
        }

        public override void Insert(DateTime updateTime, Signal signal, SIGNAL_CODE code, decimal stockvalue)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            string tradeRef = signal.Trade == null ? null : " " + signal.Trade.Reference;
            executeQuery(string.Format(DB_INSERTION + "(signalid, trading_time, tradeid, value, stockvalue) values ('{2}', {3}, '{4}', {5}, {6})",
                DB_HISTORICALDATA, DATATYPE_SIGNAL, signal.Id, ToUnixTimestamp(updateTime), tradeRef, Convert.ToInt32(code), stockvalue));
        }

        public override void Insert(Trade trade)
        {
            if (trade.TradingTime == DateTimeOffset.MinValue || trade.ConfirmationTime == DateTimeOffset.MinValue || trade.Reference == "")
                throw new ApplicationException("Cannot insert a trade without booking information");
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            executeQuery(string.Format(DB_INSERTION + "(tradeid, trading_time, confirmation_time, stockid, direction, size, price, traderef) values ('{2}', {3}, {4}, '{5}', {6}, {7}, {8}, '{9}')",
                DB_BUSINESSDATA, DATATYPE_TRADE, trade.Id, ToUnixTimestamp(trade.TradingTime), ToUnixTimestamp(trade.ConfirmationTime), trade.Epic,
                Convert.ToInt32(trade.Direction), trade.Size, trade.Price, trade.Reference));
        }

        public override void Insert(DateTime updateTime, Value profit)
        {
            throw new ApplicationException("Profit insertion not implemented in Cassandra");
        }

        public override void Insert(DateTime insertTime, NeuralNetworkForCalibration ann)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            executeQuery(string.Format("insert into staticdata.anncalibration(annid, stockid, version, insert_time, weights) values ('{0}', '{1}', {2}, {3}, {4})",
                ann.AnnId, ann.StockId, ann.Version, ToUnixTimestamp(insertTime), JsonConvert.SerializeObject(ann.Weights)));
        }

        /*
        public override void Insert(MarketLevels mktDetails)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            var publishTime = Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]);
            publishTime = new DateTime(publishTime.Year, publishTime.Month, publishTime.Day, 22, 0, 0, DateTimeKind.Local);
            var ts = ToUnixTimestamp(publishTime);
            executeQuery(string.Format(DB_INSERTION + "(indicatorid, trading_time, value) values ('{2}', {3}, {4})",
                DB_HISTORICALDATA, DATATYPE_INDICATOR, "LVLHigh_" + mktDetails.AssetId, ts, mktDetails.High));
            executeQuery(string.Format(DB_INSERTION + "(indicatorid, trading_time, value) values ('{2}', {3}, {4})",
                DB_HISTORICALDATA, DATATYPE_INDICATOR, "LVLLow_" + mktDetails.AssetId, ts, mktDetails.Low));
            executeQuery(string.Format(DB_INSERTION + "(indicatorid, trading_time, value) values ('{2}', {3}, {4})",
                DB_HISTORICALDATA, DATATYPE_INDICATOR, "LVLCloseBid_" + mktDetails.AssetId, ts, mktDetails.CloseBid));
            executeQuery(string.Format(DB_INSERTION + "(indicatorid, trading_time, value) values ('{2}', {3}, {4})",
                DB_HISTORICALDATA, DATATYPE_INDICATOR, "LVLCloseOffer_" + mktDetails.AssetId, ts, mktDetails.CloseOffer));
        }*/
                
        int IStaticDataConnection.GetAnnLatestVersion(string annid, string stockid)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            RowSet versions =  executeQuery(string.Format("select version from staticdata.anncalibration where annid='{0}' and stockid='{1}' ALLOW FILTERING",
                annid, stockid));
            Row lastVersion = null;
            foreach (var version in versions)
                lastVersion = version;
            if (lastVersion == null)
                return -1;
            return (int)lastVersion[0];
        }

        List<decimal> IStaticDataConnection.GetAnnWeights(string annid, string stockid, int version)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            RowSet weights = executeQuery(string.Format("select weights from staticdata.anncalibration where annid='{0}' and stockid='{1}' and version={2} ALLOW FILTERING",
                annid, stockid, version));
            var weightLst = new List<decimal>();
            foreach (var row in weights)
                foreach (var weight in row)
                    weightLst.AddRange((IEnumerable<decimal>)weight);
            return weightLst;
        }

        Dictionary<string, RowSet> getRows(DateTime startTime, DateTime stopTime, string type, List<string> ids)
        {
            if (_session == null)
                return null;
            var sets = new Dictionary<string, RowSet>();
            foreach(var id in ids)
                sets[id] = executeQuery(string.Format(DB_SELECTION + "where {2}id='{3}' and trading_time >= {4} and trading_time <= {5}",
                                DB_HISTORICALDATA, type, type.Substring(0, type.Length - 1), id, ToUnixTimestamp(startTime), ToUnixTimestamp(stopTime)));
            return sets;
        }

        Dictionary<string, List<Trade>> getTrades(DateTime startTime, DateTime stopTime, string type, List<string> stockids)
        {
            throw new ApplicationException("Trade reading not implemented");
        }

        Dictionary<string, List<CqlQuote>> getQuotes(DateTime startTime, DateTime stopTime, string type, List<string> ids)
        {
            var epicQuotes = new Dictionary<string, List<CqlQuote>>();
            var rowSets = getRows(startTime, stopTime, type, ids);
            foreach (var rowSet in rowSets)
            {
                if (!epicQuotes.ContainsKey(rowSet.Key))
                    epicQuotes[rowSet.Key] = new List<CqlQuote>();
                foreach (Row row in rowSet.Value.GetRows())
                    epicQuotes[rowSet.Key].Add(new CqlQuote(row));
                epicQuotes[rowSet.Key].Reverse();
            }
            return epicQuotes;
        }

        Dictionary<string, List<CqlQuote>> IReaderConnection.GetMarketDataQuotes(DateTime startTime, DateTime stopTime, string type, List<string> ids)
        {
            return getQuotes(startTime, stopTime, type, ids);            
        }

        Dictionary<string, List<CqlQuote>> IReaderConnection.GetIndicatorDataQuotes(DateTime startTime, DateTime stopTime, string type, List<string> ids)
        {
            return getQuotes(startTime, stopTime, type, ids);
        }

        Dictionary<string, List<CqlQuote>> IReaderConnection.GetSignalDataQuotes(DateTime startTime, DateTime stopTime, string type, List<string> ids)
        {
            return getQuotes(startTime, stopTime, type, ids);
        }

        Dictionary<string, List<Trade>> IReaderConnection.GetTrades(DateTime startTime, DateTime stopTime, string type, List<string> ids)
        {
            return getTrades(startTime, stopTime, type, ids);
        }

        Dictionary<string, List<KeyValuePair<DateTime, double>>> IReaderConnection.GetProfits(DateTime startTime, DateTime stopTime, string type, List<string> ids)
        {
            throw new ApplicationException("Profit reading not implemented");
        }

        MarketLevels? GetMarketLevels(DateTime updateTime, string epic)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            // process previous day
            updateTime = updateTime.AddDays(-1);
            updateTime = new DateTime(updateTime.Year, updateTime.Month, updateTime.Day, 22, 0, 0, DateTimeKind.Local);
            RowSet value = null;
            decimal high = 0m;
            string indicator = "";
            try
            {
                indicator = "High_" + epic;
                value = executeQuery(string.Format("select value from historicaldata.indicators where indicatorid='{0}' and trading_time={1}",
                    indicator, ToUnixTimestamp(updateTime)));
                high = (decimal)value.First()[0];
            }
            catch
            {
                var errorMsg = "Could not retrieve level indicators for previous COB date. Indicator: " + indicator;
                Log.Instance.WriteEntry(errorMsg, EventLogEntryType.Error);
                return null;
            }
            value = executeQuery(string.Format("select value from historicaldata.indicators where indicatorid='{0}' and trading_time={1}",
                "Low_" + epic, ToUnixTimestamp(updateTime)));
            decimal low = (decimal)value.First()[0];
            value = executeQuery(string.Format("select value from historicaldata.indicators where indicatorid='{0}' and trading_time={1}",
                "CloseBid_" + epic, ToUnixTimestamp(updateTime)));
            decimal closeBid = (decimal)value.First()[0];
            value = executeQuery(string.Format("select value from historicaldata.indicators where indicatorid='{0}' and trading_time={1}",
                "CloseOffer_" + epic, ToUnixTimestamp(updateTime)));
            decimal closeOffer = (decimal)value.First()[0];
            return new MarketLevels(epic, low, high, closeBid, closeOffer);
        }

        Dictionary<string, MarketLevels> IReaderConnection.GetMarketLevels(DateTime updateTime, List<string> ids)
        {
            var marketLevels = new Dictionary<string, MarketLevels>();
            foreach (var id in ids)
            {
                MarketLevels? mktLevels = GetMarketLevels(updateTime, id);
                if (mktLevels.HasValue)
                    marketLevels[id] = mktLevels.Value;
            }
            return marketLevels;
        }

        void IReaderConnection.CloseConnection()
        {
            Close();
        }

        RowSet executeQuery(string query)
        {
            try
            {
                return _session.Execute(query);
            }
            catch (Exception exc)
            {
                Log.Instance.WriteEntry("The following query: " + query + " failed with the following exception: " + exc.Message + ". source: " + exc.Source + ". Helplink: " + exc.HelpLink + ". Stack: " + exc.StackTrace + ". Infos: " + exc.ToString(), EventLogEntryType.Error);
            }
            return null;
        }

        public string GetJSON(DateTime startTime, DateTime stopTime, string type, string id, bool auto_select)
        {
            if (_session == null)
                return "[]";
            var ids = new List<string> { id };
            var rowSets = getRows(startTime, stopTime, type, ids);
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
            foreach (Row row in rowSets[id].GetRows())
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

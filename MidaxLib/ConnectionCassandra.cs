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
        public DateTime t;
        public decimal? b;
        public string n;
        public decimal? o;
        public decimal? v;
        public CqlQuote()
        {
        }
        public CqlQuote(Row row)
        {
            s = (string)row[0];
            t = new DateTime(((DateTimeOffset)row[1]).Ticks, DateTimeKind.Local);
            b = (decimal)row[2];
            n = (string)row[3];
            o = (decimal)row[4];
            v = (decimal?)row[5];
        }
        public CqlQuote(string mktdataId, DateTimeOffset tradingTime, string mktdataName, decimal? bid, decimal? offer, decimal? volume)
        {
            s = mktdataId;
            t = new DateTime(tradingTime.Ticks, DateTimeKind.Local);
            b = bid;
            n = mktdataName;
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
            t = new DateTime(((DateTimeOffset)row[1]).Ticks, DateTimeKind.Local);
            b = (decimal)row[2];
            n = (string)row[0];
            o = (decimal)row[2];
            v = 0m;
        }
        public CqlIndicator(string mktdataId, DateTimeOffset tradingTime, string mktdataName, decimal? value)
        {
            s = mktdataId;
            t = new DateTime(tradingTime.Ticks, DateTimeKind.Local);
            b = value;
            n = mktdataName;
            o = value;
            v = 0m;
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
            t = new DateTime(((DateTimeOffset)row[1]).Ticks, DateTimeKind.Local);
            b = Convert.ToInt32(row[4]);
            n = (string)row[0];
            o = (decimal)row[2];
            v = 0m;
        }
        public CqlSignal(string mktdataId, DateTimeOffset tradingTime, string mktdataName, SIGNAL_CODE? value, decimal mktdatavalue)
        {
            s = mktdataId;
            t = new DateTime(tradingTime.Ticks, DateTimeKind.Local);
            b = Convert.ToInt32(value);
            n = mktdataName;
            o = mktdatavalue;
            v = 0m;
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
        const string DB_BUSINESSDATA = "business";
        const string DB_HISTORICALDATA = "historical";
        const string EXCEPTION_CONNECTION_CLOSED = "Cassandra session is closed";
        static string DB_INSERTION = "insert into {0}data." + ((Config.ReplayEnabled && !(Config.ImportEnabled && !Config.ImportUATEnabled)) ? "dummy" : "") + "{1} ";
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
            executeQuery(string.Format(DB_INSERTION + "(mktdataid, trading_time,  name,  bid,  offer,  volume) values ('{2}', {3}, '{4}', {5}, {6}, {7})",
                DB_HISTORICALDATA, DATATYPE_STOCK, mktData.Id, ToUnixTimestamp(updateTime), mktData.Name, price.Bid, price.Offer, price.Volume));
        }

        public override void Insert(DateTime updateTime, Indicator indicator, decimal value)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            executeQuery(string.Format(DB_INSERTION + "(indicatorid, trading_time, value) values ('{2}', {3}, {4})",
                DB_HISTORICALDATA, DATATYPE_INDICATOR, indicator.Id, ToUnixTimestamp(updateTime), value));
        }

        public override void Insert(DateTime updateTime, Signal signal, SIGNAL_CODE code, decimal mktdatavalue)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            string tradeRef = signal.Trade == null ? null : " " + signal.Trade.Reference;
            executeQuery(string.Format(DB_INSERTION + "(signalid, trading_time, tradeid, value, mktdatavalue) values ('{2}', {3}, '{4}', {5}, {6})",
                DB_HISTORICALDATA, DATATYPE_SIGNAL, signal.Id, ToUnixTimestamp(updateTime), tradeRef, Convert.ToInt32(code), mktdatavalue));
        }

        public override void Insert(Trade trade)
        {
            if (trade.TradingTime == DateTimeOffset.MinValue || trade.ConfirmationTime == DateTimeOffset.MinValue || trade.Reference == "")
                throw new ApplicationException("Cannot insert a trade without booking information");
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            executeQuery(string.Format(DB_INSERTION + "(tradeid, trading_time, confirmation_time, mktdataid, direction, size, price, traderef) values ('{2}', {3}, {4}, '{5}', {6}, {7}, {8}, '{9}')",
                DB_BUSINESSDATA, DATATYPE_TRADE, trade.Id, ToUnixTimestamp(trade.TradingTime), ToUnixTimestamp(trade.ConfirmationTime), trade.Epic,
                Convert.ToInt32(trade.Direction), trade.Size, trade.Price, trade.Reference));
        }

        public override void Insert(DateTime updateTime, string mktdataid, Value profit)
        {
            throw new ApplicationException("Profit insertion not implemented in Cassandra");
        }

        public override void Insert(DateTime insertTime, NeuralNetworkForCalibration ann)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            executeQuery(string.Format("insert into staticdata.anncalibration(annid, mktdataid, version, insert_time, weights, error, learning_rate) values ('{0}', '{1}', {2}, {3}, {4}, {5}, {6})",
                ann.AnnId, ann.StockId, ann.Version, ToUnixTimestamp(insertTime), JsonConvert.SerializeObject(ann.Weights), ann.Error, ann.LearningRatePct));
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

        public int GetAnnLatestVersion(string annid, string mktdataid)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            RowSet versions = executeQuery(string.Format("select version from staticdata.anncalibration where annid='{0}' and mktdataid='{1}' ALLOW FILTERING",
                annid, mktdataid));
            if (versions == null)
                return -1;
            Row lastVersion = null;
            foreach (var version in versions)
                lastVersion = version;
            if (lastVersion == null)
                return -1;
            return (int)lastVersion[0];
        }

        public List<decimal> GetAnnWeights(string annid, string mktdataid, int version)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            RowSet weights = executeQuery(string.Format("select weights from staticdata.anncalibration where annid='{0}' and mktdataid='{1}' and version={2} ALLOW FILTERING",
                annid, mktdataid, version));
            var weightLst = new List<decimal>();
            foreach (var row in weights)
                foreach (var weight in row)
                    weightLst.AddRange((IEnumerable<decimal>)weight);
            return weightLst;
        }

        public decimal GetAnnError(string annid, string mktdataid, int version)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            RowSet error = executeQuery(string.Format("select error from staticdata.anncalibration where annid='{0}' and mktdataid='{1}' and version={2} ALLOW FILTERING",
                annid, mktdataid, version));
            return (decimal)error.First()[0];
        }

        public decimal GetAnnLearningRate(string annid, string mktdataid, int version)
        {
            if (_session == null)
                throw new ApplicationException(EXCEPTION_CONNECTION_CLOSED);
            RowSet error = executeQuery(string.Format("select learning_rate from staticdata.anncalibration where annid='{0}' and mktdataid='{1}' and version={2} ALLOW FILTERING",
                annid, mktdataid, version));
            return (decimal)error.First()[0];
        }

        Dictionary<string, RowSet> getRows(DateTime startTime, DateTime stopTime, string type, List<string> ids)
        {
            if (_session == null)
                return null;
            var sets = new Dictionary<string, RowSet>();
            string table = type == DATATYPE_STOCK ? DATATYPE_STOCK : type.Substring(0, type.Length - 1);
            foreach (var id in ids)
            {
                DateTime curStartTime, curStopTime;
                if (id.StartsWith("LVL") || id.StartsWith("High") || id.StartsWith("Low") || id.StartsWith("Close"))
                    curStartTime = curStopTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, 22, 45, 0);
                else
                {
                    curStartTime = startTime;
                    curStopTime = stopTime;
                }

                sets[id] = executeQuery(string.Format(DB_SELECTION + "where {2}id='{3}' and trading_time >= {4} and trading_time <= {5}",
                                DB_HISTORICALDATA, type, table, id, ToUnixTimestamp(curStartTime), ToUnixTimestamp(curStopTime)));
            }
            return sets;
        }

        public Dictionary<string, List<CqlQuote>> GetRows(DateTime startTime, DateTime stopTime, string type, List<string> ids)
        {
            var epicQuotes = new Dictionary<string, List<CqlQuote>>();
            var rowSets = getRows(startTime, stopTime, type, ids);
            foreach (var rowSet in rowSets)
            {
                if (!epicQuotes.ContainsKey(rowSet.Key))
                    epicQuotes[rowSet.Key] = new List<CqlQuote>();
                foreach (Row row in rowSet.Value.GetRows())
                    epicQuotes[rowSet.Key].Add(CqlQuote.CreateInstance(type, row));
                epicQuotes[rowSet.Key].Reverse();
            }
            return epicQuotes;
        }


        Dictionary<string, List<Trade>> getTrades(DateTime startTime, DateTime stopTime, string type, List<string> mktdataids)
        {
            throw new ApplicationException("Trade reading not implemented");
        }

        Dictionary<string, List<CqlQuote>> IReaderConnection.GetMarketDataQuotes(DateTime startTime, DateTime stopTime, string type, List<string> ids)
        {
            return GetRows(startTime, stopTime, type, ids);
        }

        Dictionary<string, List<CqlQuote>> IReaderConnection.GetIndicatorDataQuotes(DateTime startTime, DateTime stopTime, string type, List<string> ids)
        {
            return GetRows(startTime, stopTime, type, ids);
        }

        Dictionary<string, List<CqlQuote>> IReaderConnection.GetSignalDataQuotes(DateTime startTime, DateTime stopTime, string type, List<string> ids)
        {
            return GetRows(startTime, stopTime, type, ids);
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
            updateTime = new DateTime(updateTime.Year, updateTime.Month, updateTime.Day, 22, 45, 0, DateTimeKind.Local);
            RowSet value = null;
            decimal high = 0m;
            string indicator = "";
            try
            {
                indicator = "High_" + epic;
                value = executeQuery(string.Format("select value from historicaldata.{0}indicators where indicatorid='{1}' and trading_time={2}",
                    Config.UATSourceDB ? "dummy" : "", indicator, ToUnixTimestamp(updateTime)));
                high = (decimal)value.First()[0];
            }
            catch
            {
                var errorMsg = "Could not retrieve level indicators for previous COB date. Indicator: " + indicator;
                Log.Instance.WriteEntry(errorMsg, EventLogEntryType.Error);
                return null;
            }
            value = executeQuery(string.Format("select value from historicaldata.{0}indicators where indicatorid='{1}' and trading_time={2}",
                Config.UATSourceDB ? "dummy" : "", "Low_" + epic, ToUnixTimestamp(updateTime)));
            decimal low = (decimal)value.First()[0];
            value = executeQuery(string.Format("select value from historicaldata.{0}indicators where indicatorid='{1}' and trading_time={2}",
                Config.UATSourceDB ? "dummy" : "", "CloseBid_" + epic, ToUnixTimestamp(updateTime)));
            decimal closeBid = (decimal)value.First()[0];
            value = executeQuery(string.Format("select value from historicaldata.{0}indicators where indicatorid='{1}' and trading_time={2}",
                Config.UATSourceDB ? "dummy" : "", "CloseOffer_" + epic, ToUnixTimestamp(updateTime)));
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

    }
}

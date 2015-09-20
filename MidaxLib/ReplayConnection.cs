using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;
using Lightstreamer.DotNet.Client;

namespace MidaxLib
{
    public class ReplayUpdateInfo : IUpdateInfo
    {
        Dictionary<string, string> _itemData = new Dictionary<string, string>();
        string _name;
        string _id;

        public ReplayUpdateInfo(CqlQuote quote)
        {
            _name = quote.n;
            _id = quote.s;
            _itemData["MID_OPEN"] = "0";
            _itemData["HIGH"] = "0";
            _itemData["LOW"] = "0";
            _itemData["CHANGE"] = "0";
            _itemData["CHANGE_PCT"] = "0";
            _itemData["UPDATE_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", quote.t.Year, quote.t.Month,
                                                quote.t.Day, quote.t.Hour, quote.t.Minute, quote.t.Second);
            _itemData["MARKET_DELAY"] = "0";
            _itemData["MARKET_STATE"] = "TRADEABLE";
            _itemData["BID"] = quote.b.ToString();
            _itemData["OFFER"] = quote.o.ToString();
        }

        public string Name { get { return _name; } }
        public string Id { get { return _id; } }

        string IUpdateInfo.ItemName { get { return _name; }}
        int IUpdateInfo.ItemPos { get { return 0; } }
        int IUpdateInfo.NumFields { get { return _itemData.Count; } }
        bool IUpdateInfo.Snapshot { get { return false; } }

        string IUpdateInfo.GetNewValue(int fieldPos)
        {
            return _itemData.ElementAt(fieldPos).Value;
        }

        string IUpdateInfo.GetNewValue(string fieldName)
        {
            return _itemData[fieldName];
        }

        string IUpdateInfo.GetOldValue(int fieldPos)
        {
            return "";
        }

        string IUpdateInfo.GetOldValue(string fieldName)
        {
            return "";
        }

        bool IUpdateInfo.IsValueChanged(int fieldPos)
        {
            return true;
        }

        bool IUpdateInfo.IsValueChanged(string fieldName)
        {
            return true;
        }
    }

    public class ReplayStreamingClient : IAbstractStreamingClient
    {
        IReaderConnection _instance = null;
        DateTime _startTime;
        DateTime _stopTime; 
        
        void IAbstractStreamingClient.Connect(string username, string password, string apiKey)
        {
            _startTime = DateTime.SpecifyKind(DateTime.Parse(Config.Settings["TRADING_START_TIME"]), DateTimeKind.Utc);
            _stopTime = DateTime.SpecifyKind(DateTime.Parse(Config.Settings["TRADING_STOP_TIME"]), DateTimeKind.Utc);
            _instance = (Config.Settings["REPLAY_MODE"] == "DB" ? new CassandraConnection()
                : (IReaderConnection)(Config.Settings["REPLAY_MODE"] == "CSV" ? new CsvReader() : null));
        }

        void IAbstractStreamingClient.Subscribe(string[] epics, IHandyTableListener tableListener)
        {
            Dictionary<string, List<CqlQuote>> priceData = new Dictionary<string, List<CqlQuote>>();
            foreach(string epic in epics)
                priceData[epic] = _instance.GetRows(_startTime, _stopTime,
                    CassandraConnection.DATATYPE_STOCK, epic);
            Replay(priceData, tableListener);
        }

        void IAbstractStreamingClient.Unsubscribe()
        {
        }

        void Replay(Dictionary<string, List<CqlQuote>> priceData, IHandyTableListener tableListener)
        {
            DateTime curtime = _startTime;
            while (priceData.Count > 0)
            {
                DateTimeOffset minNextTime = _stopTime;
                ReplayUpdateInfo nextUpdate = null;
                int lastIndex = -1;
                List<string> epicsToDelete = new List<string>();
                foreach (var epicQuotes in priceData)
                {
                    if (epicQuotes.Value.Count == 0)
                        epicsToDelete.Add(epicQuotes.Key);
                    else
                    {
                        lastIndex = epicQuotes.Value.Count -1 ;
                        if (epicQuotes.Value[lastIndex].t <= minNextTime)
                        {
                            minNextTime = epicQuotes.Value[lastIndex].t;
                            nextUpdate = new ReplayUpdateInfo(epicQuotes.Value[lastIndex]);
                        }
                    }
                }
                if (nextUpdate == null)
                {
                    foreach (var epic in epicsToDelete)
                        priceData.Remove(epic);
                }
                else
                {
                    priceData[nextUpdate.Id].RemoveAt(lastIndex);
                    tableListener.OnUpdate(0, nextUpdate.Id, nextUpdate);
                }
            }
        }
    }

    public class ReplayConnection : MarketDataConnection
    {
        public ReplayConnection()
        {
            _apiStreamingClient = new ReplayStreamingClient();

        }
        public override void Connect()
        {
            try
            {
                _apiStreamingClient.Connect("A_REPLAYER", "DOESNT_NEED_A_PWD", "NOR_A_KEY");
            }
            catch (Exception ex)
            {
                Log.Instance.WriteEntry(ex.Message, EventLogEntryType.Error);
            }
        }
    }
}

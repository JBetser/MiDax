using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Lightstreamer.DotNet.Client;

namespace MidaxLib
{
    public class Position
    {
        string _name;
        bool _trendAssumption;
        int _quantity = 0;
        decimal _assetValue = 0.0m;
        Trade _lastTrade = null;
        List<Trade> _incomingTrades = new List<Trade>();
        Dictionary<string, int> _tradePositions = new Dictionary<string, int>();
        public int Quantity
        {
            get { return _quantity; }
            set { _quantity = value; }
        }
        public decimal AssetValue
        {
            get { return _assetValue; }
        }
        public string Epic { get { return _name; } }
        public Trade Trade { get { return _lastTrade; } }
        public void AddIncomingTrade(Trade trade) 
        {
            _incomingTrades.Add(trade);
        }
        
        public bool AwaitingTrade
        {
            get { return _incomingTrades.Count != 0; }
        }

        bool pullTrade(SIGNAL_CODE direction, int tradeSize, out Trade trade)
        {

            for (int idxTrade = 0; idxTrade < _incomingTrades.Count; idxTrade++)
            {
                if (_incomingTrades[idxTrade].Direction == direction && _incomingTrades[idxTrade].Size == tradeSize)
                {
                    trade = _incomingTrades[idxTrade];
                    _incomingTrades.Remove(trade);
                    return true;
                }
            }
            trade = null;
            return false;
        }

        public Position(string name)
        {
            _trendAssumption = Config.Settings.ContainsKey("ASSUMPTION_TREND");
            _name = name;
        }

        public void OnRawUpdatesLost(int lostUpdates)
        {
            Log.Instance.WriteEntry(string.Format("Position {0}: {1} Raw Updates Lost", _name, lostUpdates), System.Diagnostics.EventLogEntryType.Warning);
        }

        public void OnSnapshotEnd()
        {
        }

        public void OnUnsubscr()
        {
            Log.Instance.WriteEntry(string.Format("Unsubscribed {0}, last position was: {1}", _name, _quantity), System.Diagnostics.EventLogEntryType.Warning);
        }

        public void OnUnsubscrAll()
        {
            Log.Instance.WriteEntry(string.Format("Unsubscribed {0}, last position was: {1}", _name, _quantity), System.Diagnostics.EventLogEntryType.Warning);
        }

        public bool OnUpdate(IUpdateInfo update)
        {
            if (update == null)
                return false;
            if (update.NumFields == 0)
                return false;
            JavaScriptSerializer json_serializer = new JavaScriptSerializer();
            if ((update.ToString().Replace(" ", "") == "[(null)]") || (update.ToString().Replace(" ", "") == "[null]"))
                return false;
            Log.Instance.WriteEntry("Incoming position update: " + update.ToString());
            var json = json_serializer.DeserializeObject(update.ToString());
            if (json.GetType().ToString() == "System.Object[]")
            {
                object[] objs = (object[])json;
                if (objs != null)
                {
                    foreach (var obj in objs)
                    {
                        var trade_notification = (Dictionary<string, object>)obj;
                        if (trade_notification != null)
                        {
                            if (trade_notification["dealStatus"].ToString() == "ACCEPTED")
                            {
                                if (trade_notification["epic"].ToString() == _name)
                                {
                                    if (trade_notification["status"].ToString() == "OPEN")
                                    {
                                        string dealId = trade_notification["dealId"].ToString();
                                        if (!AwaitingTrade)
                                            Log.Instance.WriteEntry("An unexpected trade has been booked: " + dealId, System.Diagnostics.EventLogEntryType.Error);
                                        var direction = trade_notification["direction"].ToString() == "SELL" ? SIGNAL_CODE.SELL: SIGNAL_CODE.BUY;
                                        var tradeSize = int.Parse(trade_notification["size"].ToString());                                        
                                        Trade trade;
                                        lock (_incomingTrades)
                                        {
                                            if (pullTrade(direction, tradeSize, out trade))
                                                _lastTrade = trade;
                                            else
                                                return false;
                                            if (direction == SIGNAL_CODE.SELL)
                                                tradeSize *= -1;
                                            _lastTrade.Id = dealId;
                                            _assetValue = _lastTrade.Price;
                                            _tradePositions[_lastTrade.Id] = tradeSize;
                                            _quantity += tradeSize;
                                            Log.Instance.WriteEntry("Created a new trade: " + _lastTrade.Id);
                                            _lastTrade.Publish();
                                        }
                                        return true;
                                    }
                                    else if (trade_notification["status"].ToString() == "DELETED")
                                    {
                                        string dealId = trade_notification["dealId"].ToString();
                                        Log.Instance.WriteEntry("Closed a position: " + dealId);
                                        if (!AwaitingTrade || _tradePositions.Count == 0)
                                            Log.Instance.WriteEntry("An unexpected trade has been closed: " + dealId, System.Diagnostics.EventLogEntryType.Error);
                                        var direction = trade_notification["direction"].ToString() == "SELL" ? SIGNAL_CODE.SELL : SIGNAL_CODE.BUY;
                                        var tradeSize = int.Parse(trade_notification["size"].ToString());
                                        Trade trade;
                                        lock (_incomingTrades)
                                        {
                                            if (!pullTrade(direction, tradeSize, out trade))
                                                return false;
                                            if (direction == SIGNAL_CODE.SELL)
                                                tradeSize *= -1;
                                            if (_tradePositions.ContainsKey(dealId))
                                                _tradePositions[dealId] = _tradePositions[dealId] + tradeSize;
                                            else
                                                _tradePositions[dealId] = tradeSize;
                                            _quantity += tradeSize;
                                            if (_quantity != 0)
                                                Log.Instance.WriteEntry("Position has not been closed successfully: " + dealId, System.Diagnostics.EventLogEntryType.Error);
                                            trade.Id = dealId;
                                            trade.Publish();
                                            _lastTrade = null;
                                        }
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Log.Instance.WriteEntry("Could not process an update: " + update.ToString(), System.Diagnostics.EventLogEntryType.Error);
            return false;
        }
    }
}

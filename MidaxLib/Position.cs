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
            lock (_incomingTrades)
            {
                _incomingTrades.Add(trade);
            }
        }

        public bool AwaitingTrade
        {
            get
            {
                lock (_incomingTrades)
                {
                    return _incomingTrades.Count != 0;
                }
            }
        }

        bool pullTrade(string epic, out Trade trade)
        {
            lock (_incomingTrades)
            {
                for (int idxTrade = 0; idxTrade < _incomingTrades.Count; idxTrade++)
                {
                    if (_incomingTrades[idxTrade].Epic == epic)
                    {
                        trade = _incomingTrades[idxTrade];
                        _incomingTrades.Remove(trade);
                        return true;
                    }
                }
                trade = null;
                return false;
            }
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

        bool openTrade(Dictionary<string, object> trade_notification) 
        {
            string dealId = trade_notification["dealId"].ToString();
            if (!AwaitingTrade)
                Log.Instance.WriteEntry("An unexpected trade has been booked: " + dealId, System.Diagnostics.EventLogEntryType.Error);
            var reference = trade_notification["dealReference"].ToString();
            var direction = trade_notification["direction"].ToString() == "SELL" ? SIGNAL_CODE.SELL : SIGNAL_CODE.BUY;
            var tradeSize = int.Parse(trade_notification["size"].ToString());
            Trade trade;
            if (pullTrade(_name, out trade))
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
            return true;
        }

        bool closeTrade(Dictionary<string, object> trade_notification)
        {
            string dealId = trade_notification["dealId"].ToString();
            Log.Instance.WriteEntry("Closed a position: " + dealId);
            if (!AwaitingTrade || _tradePositions.Count == 0)
                Log.Instance.WriteEntry("An unexpected trade has been closed: " + dealId, System.Diagnostics.EventLogEntryType.Error);
            var reference = trade_notification["dealReference"].ToString();
            var direction = trade_notification["direction"].ToString() == "SELL" ? SIGNAL_CODE.SELL : SIGNAL_CODE.BUY;
            Trade trade;
            if (!pullTrade(_name, out trade))
                return false;
            var tradeSize = trade.Size;
            if (trade.Direction == SIGNAL_CODE.SELL)
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
            return true;
        }

        bool rejectTrade(Dictionary<string, object> trade_notification)
        {
            var reference = trade_notification["dealReference"].ToString();
            Log.Instance.WriteEntry("A deal has been rejected: " + reference, System.Diagnostics.EventLogEntryType.Warning);
            Trade trade;
            if (!pullTrade(reference, out trade))
            {
                Log.Instance.WriteEntry("Cannot process a rejected deal: " + reference, System.Diagnostics.EventLogEntryType.Error);
                return false;
            }
            trade.OnRejected(trade.ConfirmationTime, trade.Price, trade_notification["status"].ToString() == "OPEN");
            return true;
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
                            if (trade_notification["epic"].ToString() == _name)
                            {
                                if (trade_notification["dealStatus"].ToString() == "ACCEPTED")
                                {
                                    if (trade_notification["status"].ToString() == "OPEN")
                                    {
                                        if (!openTrade(trade_notification))
                                        {
                                            Log.Instance.WriteEntry("Could not process an open order: " + update.ToString(), System.Diagnostics.EventLogEntryType.Error);
                                            return false;
                                        }
                                        return true;
                                    }
                                    else if (trade_notification["status"].ToString() == "DELETED")
                                    {
                                        if (!closeTrade(trade_notification))
                                        {
                                            Log.Instance.WriteEntry("Could not process an close order: " + update.ToString(), System.Diagnostics.EventLogEntryType.Error); 
                                            return false;
                                        }
                                        return true;
                                    }
                                }
                                else
                                {
                                    // deal rejected
                                    return rejectTrade(trade_notification);
                                }
                            }                            
                        }
                    }
                }
            }            
            return false;
        }
    }
}

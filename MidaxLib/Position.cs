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
        bool _manualOverride = false;
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
        public bool ManualOverride { get { return _manualOverride; } set { _manualOverride = value; } }
        public void AddIncomingTrade(Trade trade)
        {
            if (trade.Id != null)
            {
                if (_incomingTrades.Select(trd => trd.Id).ToDictionary(id => id).ContainsKey(trade.Id))
                    return;
            }
            lock (_incomingTrades)
            {
                _incomingTrades.Add(trade);
            }
        }
        public void RemoveIncomingTrade(Trade trade)
        {
            lock (_incomingTrades)
            {
                _incomingTrades.Remove(trade);
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
            lock (_incomingTrades)
            {
                string dealId = trade_notification["dealId"].ToString();
                if (_tradePositions.ContainsKey(dealId))
                    return true;
                Trade trade = null;
                bool res = false;
                if (AwaitingTrade)
                    res = pullTrade(_name, out trade);
                else
                {
                    _manualOverride = true;
                    res = true;
                    Log.Instance.WriteEntry("Detected a manual position opening: " + _name + ". deal: " + dealId, System.Diagnostics.EventLogEntryType.Warning);
                }
                var reference = trade_notification["dealReference"].ToString();
                var direction = trade_notification["direction"].ToString() == "SELL" ? SIGNAL_CODE.SELL : SIGNAL_CODE.BUY;
                var tradeSize = int.Parse(trade_notification["size"].ToString());
                if (direction == SIGNAL_CODE.SELL)
                    tradeSize *= -1;
                _lastTrade = trade;
                _tradePositions[dealId] = tradeSize;
                _quantity += tradeSize;
                if (_lastTrade != null)
                {
                    _lastTrade.Id = dealId;
                    _assetValue = _lastTrade.Price;
                    _lastTrade.Publish();
                    Log.Instance.WriteEntry("Created a new trade: " + _lastTrade.Id);
                }
                return res;
            }
        }

        bool closeTrade(Dictionary<string, object> trade_notification)
        {
            lock (_incomingTrades)
            {
                string dealId = trade_notification["dealId"].ToString();
                _lastTrade = null;
                if (!AwaitingTrade)
                {
                    if (_tradePositions.ContainsKey(dealId))
                    {
                        if (_tradePositions[dealId] == 0)
                            return true;
                    }
                    _manualOverride = true;
                    Log.Instance.WriteEntry("Detected a manual position closing: " + _name + ". deal: " + dealId, System.Diagnostics.EventLogEntryType.Warning);
                    _tradePositions.Clear();
                    _tradePositions[dealId] = 0;
                    _quantity = 0;
                    return true;
                }
                Trade trade;
                bool res = pullTrade(_name, out trade);
                if (_tradePositions.Count == 0)
                {
                    Log.Instance.WriteEntry("Unexpected position closing: " + _name + ". deal: " + dealId, System.Diagnostics.EventLogEntryType.Error);
                    _quantity = 0;
                    return false;
                }
                if (_tradePositions[dealId] != 0)
                {
                    Log.Instance.WriteEntry("Closed a position: " + dealId);
                    _quantity -= _tradePositions[dealId];
                    _tradePositions[dealId] = 0;
                }
                if (_quantity != 0)
                    Log.Instance.WriteEntry("Position has not been closed completely: " + dealId + ". Position: " + _quantity, System.Diagnostics.EventLogEntryType.Error);
                if (trade != null)
                {
                    trade.Id = dealId;
                    trade.Publish();
                }
                return res;
            }
        }

        bool rejectTrade(Dictionary<string, object> trade_notification)
        {
            lock (_incomingTrades)
            {
                var reference = trade_notification["dealReference"].ToString();
                Log.Instance.WriteEntry("A deal has been rejected: " + reference, System.Diagnostics.EventLogEntryType.Error);
                Trade trade;
                if (!pullTrade(reference, out trade))
                {
                    Log.Instance.WriteEntry("Cannot process a rejected deal: " + reference, System.Diagnostics.EventLogEntryType.Error);
                    return false;
                }
                //trade.OnRejected(trade.ConfirmationTime, trade.Price, trade_notification["status"].ToString() == "OPEN");
                return true;
            }
        }

        public bool OnUpdate(IUpdateInfo update)
        {
            try
            {
                JavaScriptSerializer json_serializer = new JavaScriptSerializer();
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
                                                Log.Instance.WriteEntry("Could not process an open order: " + _name, System.Diagnostics.EventLogEntryType.Error);
                                        }
                                        else if (trade_notification["status"].ToString() == "DELETED")
                                        {
                                            if (!closeTrade(trade_notification))
                                                Log.Instance.WriteEntry("Could not process an close order: " + _name, System.Diagnostics.EventLogEntryType.Error);
                                        }
                                    }
                                    else
                                    {
                                        // deal rejected
                                        // return rejectTrade(trade_notification);
                                        Log.Instance.WriteEntry("A deal has been rejected: " + _name, System.Diagnostics.EventLogEntryType.Error);
                                    }
                                    return true;
                                }
                                else
                                    return false;
                            }
                        }
                    }
                }
                Log.Instance.WriteEntry("Unable to process a position update", System.Diagnostics.EventLogEntryType.Error);
            }
            catch (Exception e)
            {
                Log.Instance.WriteEntry("Caught an exception during a position update: " + e.ToString(), System.Diagnostics.EventLogEntryType.Error);
            }
            if (Portfolio.Instance.ShutDownFunc != null)
            {
                Log.Instance.WriteEntry("Terminating...", System.Diagnostics.EventLogEntryType.Error);
                Portfolio.Instance.ShutDownFunc();
            }
            return true;
        }
    }
}

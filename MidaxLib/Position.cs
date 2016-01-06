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
        int _pos = 0;
        Trade _lastTrade = null;
        Dictionary<string, int> _tradePositions = new Dictionary<string, int>();
        public int Value
        {
            get { return _pos; }
            set { _pos = value; }
        }
        public string Epic { get { return _name; } }
        public Trade Trade { get { return _lastTrade; } set { _lastTrade = value; } }

        public Position(string name)
        {
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
            Log.Instance.WriteEntry(string.Format("Unsubscribed {0}, last position was: {1}", _name, _pos), System.Diagnostics.EventLogEntryType.Warning);
        }

        public void OnUnsubscrAll()
        {
            Log.Instance.WriteEntry(string.Format("Unsubscribed {0}, last position was: {1}", _name, _pos), System.Diagnostics.EventLogEntryType.Warning);
        }

        public void OnUpdate(IUpdateInfo update)
        {
            if (update == null)
                return;
            if (update.NumFields > 0)
            {
                JavaScriptSerializer json_serializer = new JavaScriptSerializer();
                if (update.ToString() != "[ (null) ]")
                {
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
                                                var tradeSize = int.Parse(trade_notification["size"].ToString());
                                                _tradePositions[trade_notification["dealId"].ToString()] = tradeSize;
                                                _pos += tradeSize;
                                            }
                                            else if (trade_notification["status"].ToString() == "DELETED")
                                            {
                                                _pos -= _tradePositions[trade_notification["dealId"].ToString()];
                                                _tradePositions.Remove(trade_notification["dealId"].ToString());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

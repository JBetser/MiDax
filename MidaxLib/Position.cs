using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lightstreamer.DotNet.Client;

namespace MidaxLib
{
    public class Position
    {
        string _name;
        int _pos = 0;
        public int Value { get { return _pos; } }

        public Position(string name)
        {
            _name = name;
        }

        public void OnRawUpdatesLost(int itemPos, int lostUpdates)
        {
            _pos = itemPos;
            Log.Instance.WriteEntry(string.Format("Position {0}: {1} Raw Updates Lost", _name, lostUpdates), System.Diagnostics.EventLogEntryType.Warning);
        }

        public void OnSnapshotEnd(int itemPos)
        {
            _pos = itemPos;
        }

        public void OnUnsubscr(int itemPos)
        {
            _pos = itemPos;
            Log.Instance.WriteEntry(string.Format("Unsubscribed {0}, last position was: {1}", _name, _pos), System.Diagnostics.EventLogEntryType.Warning);
        }

        public void OnUnsubscrAll()
        {
            Log.Instance.WriteEntry(string.Format("Unsubscribed {0}, last position was: {1}", _name, _pos), System.Diagnostics.EventLogEntryType.Warning);
        }

        public void OnUpdate(int itemPos, IUpdateInfo update)
        {
            _pos = itemPos;
        }
    }
}

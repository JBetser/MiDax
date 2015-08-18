using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Midax;
using System.Threading;
using MidaxLib;
using System.Diagnostics;

namespace Midax
{
    class MidaxIceI : MidaxIceDisp_
    {
        string _serverName;
        Model _model = null;

        public MidaxIceI(Model model, string name)
        {
            _serverName = name;
            _model = model;
        }
        
        public override string ping(Ice.Current current)
        {
            string pong = "pong";
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": " + pong, EventLogEntryType.Information);
            return pong;
        }

        public override void startsignals(Ice.Current current)
        {
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": Starting signals", EventLogEntryType.Information);
            _model.StartSignals();
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": Signals started", EventLogEntryType.Information);
        }

        public override void stopsignals(Ice.Current current)
        {
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": Stopping signals", EventLogEntryType.Information);
            _model.StopSignals();
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": Signals stopped", EventLogEntryType.Information);
        }

        public override void shutdown(Ice.Current current)
        {
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": Shutting down", EventLogEntryType.Information);
            _model.StopSignals();
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": Signals stopped", EventLogEntryType.Information);
            current.adapter.getCommunicator().shutdown();
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": Disconnected", EventLogEntryType.Information);
        }
        
        public override string getStatus(Ice.Current current)
        {
            return  "OK";
        }
    }
}

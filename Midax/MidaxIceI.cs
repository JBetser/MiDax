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
        List<Model> _models = null;

        public MidaxIceI(List<Model> models, string name)
        {
            _serverName = name;
            _models = models;
        }
        
        public override string ping(Ice.Current current)
        {
            string pong = "pong";
            Log.Instance.WriteEntry(pong, EventLogEntryType.Information);
            return pong;
        }

        public override void startsignals(Ice.Current current)
        {
            foreach (var model in _models)
            {
                Log.Instance.WriteEntry(model.GetType().ToString() + ": Starting signals", EventLogEntryType.Information);
                model.StartSignals();
                Log.Instance.WriteEntry(model.GetType().ToString() + ": Signals started", EventLogEntryType.Information);
            }
        }

        public override void stopsignals(Ice.Current current)
        {
            foreach (var model in _models)
            {
                Log.Instance.WriteEntry(model.GetType().ToString() + ": Stopping signals", EventLogEntryType.Information);
                model.StopSignals();
                Log.Instance.WriteEntry(model.GetType().ToString() + ": Signals stopped", EventLogEntryType.Information);
            }
        }

        public override void shutdown(Ice.Current current)
        {
            foreach (var model in _models)
            {
                Log.Instance.WriteEntry(model.GetType().ToString() + ": Shutting down", EventLogEntryType.Information);
                model.StopSignals();
                Log.Instance.WriteEntry(model.GetType().ToString() + ": Signals stopped", EventLogEntryType.Information);                
            }
            current.adapter.getCommunicator().shutdown();
            Log.Instance.WriteEntry("Disconnection failed", EventLogEntryType.Error);
        }
        
        public override string getStatus(Ice.Current current)
        {
            return  "OK";
        }
    }
}

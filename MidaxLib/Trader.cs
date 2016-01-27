using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class Trader
    {
        List<Model> _models;

        public Trader(List<Model> models)
        {
            _models = models;            
        }

        public void Start()
        {
            MarketDataConnection.Instance.Connect(connectionLostCallback);
            foreach (var model in _models)
            {
                Log.Instance.WriteEntry(model.GetType().ToString() + ": Starting signals...", EventLogEntryType.Information);
                model.StartSignals(false);
                Log.Instance.WriteEntry(model.GetType().ToString() + ": Signals started", EventLogEntryType.Information);
            }
            MarketDataConnection.Instance.StartListening();
        }

        public void Stop()
        {
            foreach (var model in _models)
            {
                Log.Instance.WriteEntry(model.GetType().ToString() + ": Stopping signals...", EventLogEntryType.Information);
                model.StopSignals(false);
                Log.Instance.WriteEntry(model.GetType().ToString() + ": Signals stopped", EventLogEntryType.Information);
            }
            MarketDataConnection.Instance.StopListening();
        }

        public void CloseAllPositions(DateTime time)
        {
            Log.Instance.WriteEntry("Closing positions...", EventLogEntryType.Information);
            Portfolio.Instance.CloseAllPositions(time);
            Log.Instance.WriteEntry("All positions closed", EventLogEntryType.Information);
        }

        void connectionLostCallback(object state)
        {
            Log.Instance.WriteEntry(": Connection lost. Reconnecting...", EventLogEntryType.Warning);
            Start();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class Trader
    {
        List<Model> _models;

        public delegate void shutdown();
        public delegate DateTime getNow();
        shutdown _onShutdown;
        DateTime _startTime;
        getNow _getNow;

        public Trader(List<Model> models, shutdown communicatorShutdown = null)
        {
            _models = models;
            _onShutdown = communicatorShutdown;
        }

        public void Init(getNow getNow)
        {
            Assembly thisAssem = typeof(Trader).Assembly;
            AssemblyName thisAssemName = thisAssem.GetName();
            Version ver = thisAssemName.Version;
            _getNow = getNow;
            DateTime now = getNow();

            Log.Instance.WriteEntry("Midax " + ver + " service initialized", EventLogEntryType.Information);

            var timerStart = new System.Threading.Timer(startSignalCallback);
            var timerStop = new System.Threading.Timer(stopSignalCallback);

            // Figure how much time until PUBLISHING_STOP_TIME
            _startTime = Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_START_TIME"]);
            DateTime stopTime = Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]);

            // If it's already past PUBLISHING_STOP_TIME, wait until PUBLISHING_STOP_TIME tomorrow  
            int msUntilStartTime = 10;
            if (now > _startTime)
            {
                if (now > stopTime)
                {
                    var nextDay = _startTime.AddDays(1.0);
                    stopTime = stopTime.AddDays(1.0);
                    msUntilStartTime = (int)((nextDay - now).TotalMilliseconds);
                }
            }
            else
                msUntilStartTime = (int)((_startTime - now).TotalMilliseconds);
            int msUntilStopTime = (int)((stopTime - now).TotalMilliseconds);

            Log.Instance.WriteEntry(string.Format("Next scheduling in {0}h{1}mn", msUntilStartTime / (3600 * 1000), 
                (msUntilStartTime - 3600 * 1000 * (msUntilStartTime / (3600 * 1000))) / (60 * 1000)), EventLogEntryType.Information);

            // Set the timers to elapse only once, at their respective scheduled times
            timerStart.Change(msUntilStartTime, Timeout.Infinite);
            timerStop.Change(msUntilStopTime, Timeout.Infinite);
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
            //TWSConnection.Instance.Connect(connectionLostCallback);
        }

        public void Stop()
        {
            MarketDataConnection.Instance.StreamClient.WaitForClosing();
            foreach (var model in (from m in _models select m).Reverse())
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

        public void ProcessError(string message, string expected = "")
        {
            _models[0].ProcessError(message, expected);
        }

        void connectionLostCallback(object state)
        {
            Log.Instance.WriteEntry(": Connection lost. Reconnecting...", EventLogEntryType.Warning);
            Start();
        }

        void startSignalCallback(object state)
        {
            if (_getNow().Date != _startTime.Date)
            {
                Log.Instance.WriteEntry("Restarting the service", EventLogEntryType.Information);
                if (_onShutdown != null)
                    _onShutdown();
            }
            Start();
        }

        void stopSignalCallback(object state)
        {
            Stop();
            if (_onShutdown != null)
                _onShutdown();
        } 
    }
}

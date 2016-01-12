using System;
using System.Configuration;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidaxLib;
using Midax;
using System.Collections.Specialized;
using System.Reflection;
using System.Threading;
using NLapack.Matrices;
using System.Runtime.InteropServices;

public class Server
{
    public class App : Ice.Application
    {
        Model _model = null;        

        public override int run(string[] args)
        {           
            try
            {
                if (args.Length != 0)
                    throw new ApplicationException("starting: too many arguments in application call.");

                Log.APPNAME = Midax.Properties.Settings.Default.APP_NAME;
                
                Dictionary<string, string> dicSettings = new Dictionary<string, string>();
                List<string> stockList = new List<string>();
                foreach (SettingsPropertyValue prop in Midax.Properties.Settings.Default.PropertyValues)
                {
                    if (prop.Name == "STOCKS")
                    {
                        string[] stockArray = new string[100];
                        ((StringCollection)prop.PropertyValue).CopyTo(stockArray, 0);
                        foreach (string stock in stockArray)
                        {
                            if (stock != null && stock != "")
                                stockList.Add(stock);
                        }
                    }
                    else
                        dicSettings.Add(prop.Name, (string)prop.PropertyValue);
                }
                Config.Settings = dicSettings;
            
                Ice.ObjectAdapter adapter = communicator().createObjectAdapter("MidaxIce");
                Ice.Properties properties = communicator().getProperties();
                Ice.Identity id = communicator().stringToIdentity(properties.getProperty("Identity"));

                MarketDataConnection.Instance.Connect(connectionLostCallback);

                var index = new Asset(dicSettings["INDEX"], Config.ParseDateTimeLocal(dicSettings["PUBLISHING_STOP_TIME"]));
                List<MarketData> stocks = new List<MarketData>();
                foreach (string stock in stockList)
                    stocks.Add(new MarketData(stock));
                List<MarketData> volIndices = new List<MarketData>();
                volIndices.Add(new MarketData(dicSettings["VOLATILITY_2M"]));
                volIndices.Add(new MarketData(dicSettings["VOLATILITY_3M"]));
                _model = new ModelMacD(index, stocks, volIndices);
                adapter.add(new MidaxIceI(_model, properties.getProperty("Ice.ProgramName")), id);                
                adapter.activate();

                Assembly thisAssem = typeof(Server).Assembly;
                AssemblyName thisAssemName = thisAssem.GetName();
                Version ver = thisAssemName.Version;

                Log.Instance.WriteEntry("Midax " + ver + " service initialized", EventLogEntryType.Information);
                
                var timerStart = new System.Threading.Timer(startSignalCallback);
                var timerStop = new System.Threading.Timer(stopSignalCallback);
                var timerClosePositions = new System.Threading.Timer(closePositionsCallback);
                
                // Figure how much time until PUBLISHING_STOP_TIME
                DateTime now = DateTime.Now;
                DateTime startTime = Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_START_TIME"]);
                DateTime stopTime = Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]);
                DateTime closePositionsTime = Config.ParseDateTimeLocal(Config.Settings["FORCE_CLOSE_POSITIONS_TIME"]);

                // If it's already past PUBLISHING_STOP_TIME, wait until PUBLISHING_STOP_TIME tomorrow  
                int msUntilStartTime = 10;
                if (now > startTime)
                {
                    if (now > stopTime)
                    {
                        startTime = startTime.AddDays(1.0);
                        stopTime = stopTime.AddDays(1.0);
                        if (now > closePositionsTime)
                            closePositionsTime = closePositionsTime.AddDays(1.0);
                        msUntilStartTime = (int)((startTime - now).TotalMilliseconds);
                    }
                }
                else
                    msUntilStartTime = (int)((startTime - now).TotalMilliseconds);
                int msUntilStopTime = (int)((stopTime - now).TotalMilliseconds);
                int msUntilCloseTime = (int)((closePositionsTime - now).TotalMilliseconds);
                
                // Set the timers to elapse only once, at their respective scheduled times
                timerStart.Change(msUntilStartTime, Timeout.Infinite);
                timerStop.Change(msUntilStopTime, Timeout.Infinite);
                timerClosePositions.Change(msUntilCloseTime, Timeout.Infinite);

                communicator().waitForShutdown();
            }
            catch (SEHException exc)
            {
                Log.Instance.WriteEntry("Midax server interop error: " + exc.ToString() + ", Error code: " + exc.ErrorCode, EventLogEntryType.Error);
            }
            catch (Exception exc)
            {
                Log.Instance.WriteEntry("Midax server error: " + exc.ToString(), EventLogEntryType.Error);
            }

            return 0;
        }

        void startSignalCallback(object state)
        {
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": Starting signals", EventLogEntryType.Information);
            _model.StartSignals();
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": Signals started", EventLogEntryType.Information);
        }

        void stopSignalCallback(object state)
        {
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": Stopping signals", EventLogEntryType.Information);
            _model.StopSignals();
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": Signals stopped", EventLogEntryType.Information);
            communicator().shutdown();
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": Disconnected", EventLogEntryType.Information);
        }

        void closePositionsCallback(object state)
        {
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": Closing positions", EventLogEntryType.Information);
            _model.CloseAllPositions();
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": All positions closed", EventLogEntryType.Information);
        }

        void connectionLostCallback(object state)
        {
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": Connection lost. Reconnecting...", EventLogEntryType.Warning);
            MarketDataConnection.Instance.Connect(connectionLostCallback);
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": Connected. Restarting the signals...", EventLogEntryType.Information);
            MarketDataConnection.Instance.StartListening();
            Log.Instance.WriteEntry(_model.GetType().ToString() + ": Signals started", EventLogEntryType.Information);
        }
    }

    static public int Main(string[] args)
    {
        App app = new App();
        return app.main(args);
    }
}


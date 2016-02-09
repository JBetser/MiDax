using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class Log
    {
        public static string APPNAME = "Midax";
        public static string SOURCE = "MidaxLogger";
        public static string APPNAME_TEST = "MidaxTest";
        public static string SOURCE_TEST = "MidaxLoggerTest";
        static EventLog _logMgr = null;        

        Log() { }

        static EventLog newEventLog()
        {
            if (!EventLog.SourceExists(Config.ReplayEnabled ? SOURCE_TEST : SOURCE))
                EventLog.CreateEventSource(new EventSourceCreationData(Config.ReplayEnabled ? SOURCE_TEST : SOURCE, Config.ReplayEnabled ? APPNAME_TEST : APPNAME));
            return new EventLog(Config.ReplayEnabled ? APPNAME_TEST : APPNAME, Environment.MachineName, Config.ReplayEnabled ? SOURCE_TEST : SOURCE);
        }

        static public EventLog Instance
        {
            get { return _logMgr == null ? _logMgr = newEventLog() : _logMgr; }
        }
    }
}

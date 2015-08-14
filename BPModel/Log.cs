using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BPModel
{
    public class Log
    {
        public static string APPNAME = "Midax";
        public static string SOURCE = "MidaxLogger";
        static EventLog _logMgr = null;        

        Log() { }

        static EventLog newEventLog()
        {
            if (!EventLog.SourceExists(SOURCE))
                EventLog.CreateEventSource(new EventSourceCreationData(SOURCE, APPNAME));
            return new EventLog(APPNAME, Environment.MachineName, SOURCE);
        }

        static public EventLog Instance
        {
            get { return _logMgr == null ? _logMgr = newEventLog() : _logMgr; }
        }
    }
}

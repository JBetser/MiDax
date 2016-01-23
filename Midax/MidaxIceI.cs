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
        Trader _trader = null;

        public MidaxIceI(Trader trader, string name)
        {
            _serverName = name;
            _trader = trader;
        }
        
        public override string ping(Ice.Current current)
        {
            string pong = "pong";
            Log.Instance.WriteEntry(pong, EventLogEntryType.Information);
            return pong;
        }

        public override void startsignals(Ice.Current current)
        {
            _trader.Start();
        }

        public override void stopsignals(Ice.Current current)
        {
            _trader.Stop();
        }

        public override void shutdown(Ice.Current current)
        {
            _trader.Stop();
            current.adapter.getCommunicator().shutdown();
        }
        
        public override string getStatus(Ice.Current current)
        {
            return  "OK";
        }
    }
}

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
        TimeZoneInfo _est = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                
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
            return "OK";
        }

        public override void log(string message, long logType, Ice.Current current)
        {
            Log.Instance.WriteEntry(message, (EventLogEntryType)logType);
        }

        public override void tick(string mktDataId, long year, long month, long day,
            long hours, long minutes, long seconds, long milliseconds, 
            double price, long volume, Ice.Current current)
        {
            try
            {
                var dowStockId = (DOW_STOCK)Enum.Parse(typeof(DOW_STOCK), mktDataId);
                DateTime utcTime = TimeZoneInfo.ConvertTimeToUtc(new DateTime((int)year, (int)month, (int)day, (int)hours, (int)minutes, (int)seconds, (int)milliseconds), _est);
                IceStreamingMarketData.Instance.OnTick((int)dowStockId, utcTime,
                                                    (decimal)price, (decimal)volume);
            }
            catch(Exception e)
            {
                Log.Instance.WriteEntry("IceConnection error: Could not register price for stock: " + mktDataId + 
                    ". Exception: " + e.ToString(), EventLogEntryType.Error);
            }
        }
    }
}

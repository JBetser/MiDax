using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class Config
    {
        static protected Dictionary<string, string> _settings = null;
        
        public static Dictionary<string, string> Settings
        {
            get { return _settings; }
            set { _settings = value; }
        }

        public static bool TradingEnabled
        {
            get
            {
                return _settings["TRADING_MODE"] == "PRODUCTION";
            }
        }

        public static bool ReplayEnabled
        {
            get
            {
                return (_settings["TRADING_MODE"] == "REPLAY" || _settings["TRADING_MODE"] == "REPLAY_UAT");
            }
        }

        public static bool CalibratorEnabled
        {
            get
            {
                return _settings["TRADING_MODE"] == "CALIBRATION";
            }
        }

        public static bool MarketSelectorEnabled
        {
            get
            {
                return _settings["TRADING_MODE"] == "SELECT";
            }
        }

        public static bool TestReplayEnabled
        {
            get
            {
                if (!Config.Settings.ContainsKey("REPLAY_MODE"))
                    return false;
                return ReplayEnabled && (!Config.Settings.ContainsKey("PUBLISHING_CSV") && !Config.Settings.ContainsKey("DB_CONTACTPOINT"));
            }
        }

        public static bool TestReplayGeneratorEnabled
        {
            get
            {
                return ReplayEnabled && Config.Settings.ContainsKey("PUBLISHING_CSV");
            }
        }

        public static bool UATSourceDB
        {
            get
            {
                return Config.Settings["TRADING_MODE"] == "REPLAY_UAT";
            }
        }

        static bool _tradingOpen = false;
        static bool _publishingOpen = false;
                
        public static bool TradingOpen(DateTime time)
        {
            bool open = (time.TimeOfDay >= Config.ParseDateTimeLocal(_settings["TRADING_START_TIME"]).TimeOfDay &&
                    time.TimeOfDay < Config.ParseDateTimeLocal(_settings["TRADING_CLOSING_TIME"]).TimeOfDay);
            if (open != _tradingOpen){
                _tradingOpen = open;
                Log.Instance.WriteEntry("Trading " + (_tradingOpen ? "started" : "stopped"), EventLogEntryType.Information);
            }
            return open;
        }

        public static bool PublishingOpen(DateTime time)
        {
            if (ReplayEnabled || MarketSelectorEnabled)
                return true;
            bool open = (time.TimeOfDay >= Config.ParseDateTimeLocal(_settings["PUBLISHING_START_TIME"]).TimeOfDay &&
                time.TimeOfDay < Config.ParseDateTimeLocal(_settings["PUBLISHING_STOP_TIME"]).TimeOfDay);
            if (open != _publishingOpen)
            {
                _publishingOpen = open;
                Log.Instance.WriteEntry("Publishing " + (_publishingOpen ? "started" : "stopped"), EventLogEntryType.Information);
            }
            return open;
        }        

        public static string TestList(List<string> tests)
        {
            return tests.Aggregate("", (prev, next) => prev + next + ";", res => res.Substring(0, res.Length - 1));
        }

        static DateTime parseDateTime(string dt)
        {
            if (dt.Length > 13)
            {
                var dateTimeComponents = dt.Split(' ');
                var hrmnsec = (from cmpnt in dateTimeComponents[1].Split(':') select int.Parse(cmpnt)).ToArray();
                if (dt[1] == '/' || dt[2] == '/')
                {
                    var yrmntday = (from cmpnt in dateTimeComponents[0].Split('/') select int.Parse(cmpnt)).ToArray();
                    return new DateTime(yrmntday[2], yrmntday[1], yrmntday[0], hrmnsec[0], hrmnsec[1], hrmnsec[2]);
                }
                else
                {
                    var yrmntday = (from cmpnt in dateTimeComponents[0].Split(dt[4]) select int.Parse(cmpnt)).ToArray();
                    return new DateTime(yrmntday[0], yrmntday[1], yrmntday[2], hrmnsec[0], hrmnsec[1], hrmnsec[2]);
                }
            }
            return DateTime.Parse(dt);
        }

        public static DateTime ParseDateTimeUTC(string dt)
        {
            return DateTime.SpecifyKind(parseDateTime(dt), DateTimeKind.Utc);
        }

        public static DateTime ParseDateTimeLocal(string dt)
        {
            return DateTime.SpecifyKind(parseDateTime(dt), DateTimeKind.Local);
        }

        public static DateTime GetNow()
        {
            return DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
        }
    }
}

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
                return (_settings["TRADING_MODE"] == "REPLAY" || _settings["TRADING_MODE"] == "REPLAY_UAT" || ImportEnabled);
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

        public static bool ImportEnabled
        {
            get
            {
                return _settings["TRADING_MODE"] == "IMPORT" || _settings["TRADING_MODE"] == "IMPORT_UAT";
            }
        }

        public static bool ImportUATEnabled
        {
            get
            {
                return _settings["TRADING_MODE"] == "IMPORT_UAT";
            }
        }

        public static bool ReplayDBEnabled
        {
            get
            {
                if (!_settings.ContainsKey("REPLAY_MODE"))
                    return false;
                return _settings["REPLAY_MODE"] == "DB";
            }
        }
        
        public static bool TestReplayEnabled
        {
            get
            {
                if (!_settings.ContainsKey("REPLAY_MODE"))
                    return false;
                return ReplayEnabled && (!_settings.ContainsKey("PUBLISHING_CSV") && !ReplayDBEnabled && !ImportEnabled);
            }
        }

        public static bool DBPublishingEnabled
        {
            get
            {
                return Config.ReplayDBEnabled || Config.TradingEnabled || Config.CalibratorEnabled;
            }
        }

        public static bool TestReplayGeneratorEnabled
        {
            get
            {
                return ReplayEnabled && (_settings.ContainsKey("PUBLISHING_CSV") || ReplayDBEnabled || ImportEnabled);
            }
        }

        public static bool TestReplayCsvGeneratorEnabled
        {
            get
            {
                return TestReplayGeneratorEnabled && _settings.ContainsKey("PUBLISHING_CSV");
            }
        }

        public static bool UATSourceDB
        {
            get
            {
                return _settings["TRADING_MODE"] == "REPLAY_UAT" || _settings["TRADING_MODE"] == "IMPORT_UAT";
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

        public static bool RushHour(DateTime time)
        {
            return (time.TimeOfDay >= Config.ParseDateTimeLocal(_settings["RUSH_START_TIME"]).TimeOfDay &&
                time.TimeOfDay < Config.ParseDateTimeLocal(_settings["RUSH_STOP_TIME"]).TimeOfDay);
        }

        public static bool StartRushHour(DateTime time)
        {
            return (time.TimeOfDay >= Config.ParseDateTimeLocal(_settings["RUSH_START_TIME"]).TimeOfDay &&
                time.TimeOfDay < Config.ParseDateTimeLocal(_settings["RUSH_START_TIME"]).AddMinutes(30).TimeOfDay);
        }

        public static bool StopRushHour(DateTime time)
        {
            return (time.TimeOfDay >= Config.ParseDateTimeLocal(_settings["RUSH_STOP_TIME"]).AddMinutes(-30).TimeOfDay &&
                time.TimeOfDay < Config.ParseDateTimeLocal(_settings["RUSH_STOP_TIME"]).TimeOfDay);
        }

        public static string TestList(List<string> tests)
        {
            return tests.Aggregate("", (prev, next) => prev + next + ";", res => res.Substring(0, res.Length - 1));
        }

        static DateTime parseDateTime(string dt)
        {
            // covers string like "2016-1-1 8:0", "2016/1/1 8:0", "28/1/2016 8:0", "28-1-2016 8:0", 
            // "2016-1-1 13:0:0", "2016/1/1 13:0:0", "28/1/2016 13:45:10", "28-1-2016 13:45:10"
            if (dt.Length > 11) 
            {
                var dateTimeComponents = dt.Split(' ');
                dateTimeComponents[0] = dateTimeComponents[0].Replace('T','\0');
                var hourmnsec_ms = dateTimeComponents[1].Split('.');
                dateTimeComponents[1] = hourmnsec_ms[0];
                var ms = hourmnsec_ms.Length > 1 ? int.Parse(hourmnsec_ms[1]) : 0;
                var hrmnsec = (from cmpnt in dateTimeComponents[1].Split(':') select int.Parse(cmpnt)).ToArray();
                var splitChar = (dt[1] == '/' || dt[2] == '/') ? '/' : ((dt[1] == '-' || dt[2] == '-') ? '-' : dt[4]);
                var yrmntday = (from cmpnt in dateTimeComponents[0].Split(splitChar) select int.Parse(cmpnt)).ToArray();
                var year = yrmntday[2] > 1900 ? yrmntday[2] : yrmntday[0];
                var day = yrmntday[2] > 1900 ? yrmntday[0] : yrmntday[2];
                DateTime ret; 
                if (hrmnsec.Length == 3)
                    ret = new DateTime(year, yrmntday[1], day, hrmnsec[0], hrmnsec[1], hrmnsec[2]);
                else
                    ret = hrmnsec.Length == 2 ? new DateTime(year, yrmntday[1], day, hrmnsec[0], hrmnsec[1], 0)
                        : new DateTime(year, yrmntday[1], day, hrmnsec[0], hrmnsec[1], hrmnsec[2], ms);
                return ret;
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

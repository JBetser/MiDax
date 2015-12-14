using System;
using System.Collections.Generic;
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
                return _settings["TRADING_MODE"] == "REPLAY";
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
                return ReplayEnabled && (Config.Settings["REPLAY_MODE"] == "CSV" && !Config.Settings.ContainsKey("PUBLISHING_CSV"));
            }
        }

        public static bool TestReplayGeneratorEnabled
        {
            get
            {
                return ReplayEnabled && Config.Settings.ContainsKey("PUBLISHING_CSV");
            }
        }
        
        public static bool TradingOpen
        {
            get
            {
                return DateTime.Now.TimeOfDay > DateTime.Parse(_settings["TRADING_START_TIME"]).TimeOfDay &&
                    DateTime.Now.TimeOfDay < DateTime.Parse(_settings["TRADING_STOP_TIME"]).TimeOfDay;
            }
        }
        
        public static bool PublishingOpen
        {
            get
            {
                if (ReplayEnabled || MarketSelectorEnabled)
                    return true;
                return DateTime.Now.TimeOfDay > DateTime.Parse(_settings["PUBLISHING_START_TIME"]).TimeOfDay &&
                    DateTime.Now.TimeOfDay < DateTime.Parse(_settings["PUBLISHING_STOP_TIME"]).TimeOfDay;
            }
        }

        public static string TestList(List<string> tests)
        {
            return tests.Aggregate("", (prev, next) => prev + next + ";", res => res.Substring(0, res.Length - 1));
        }
    }
}

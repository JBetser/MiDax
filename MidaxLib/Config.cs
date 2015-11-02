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
        
        public static bool TradingOpen
        {
            get
            {
                return DateTime.Now.TimeOfDay > DateTime.Parse(_settings["TRADING_START_TIME"]).TimeOfDay &&
                    DateTime.Now.TimeOfDay < DateTime.Parse(_settings["TRADING_STOP_TIME"]).TimeOfDay;
            }
        }

        public static bool PublishingEnabled
        {
            get
            {
                return int.Parse(_settings["PUBLISHING_DISABLED"]) == 0;
            }
        }

        public static bool PublishingOpen
        {
            get
            {
                return DateTime.Now.TimeOfDay > DateTime.Parse(_settings["PUBLISHING_START_TIME"]).TimeOfDay &&
                    DateTime.Now.TimeOfDay < DateTime.Parse(_settings["PUBLISHING_STOP_TIME"]).TimeOfDay;
            }
        } 
    }
}

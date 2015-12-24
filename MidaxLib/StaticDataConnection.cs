using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public interface IStaticDataConnection
    {
        int GetAnnLatestVersion(string annid, string stockid);
    }

    public class StaticDataConnection
    {
        static IStaticDataConnection _instance = null;

        static public IStaticDataConnection Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                if (Config.ReplayEnabled)
                    _instance = new ReplayConnection();
                else
                    _instance = new CassandraConnection();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }
    }
}

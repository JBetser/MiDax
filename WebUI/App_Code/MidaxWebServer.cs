using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Net;
using System.Net.Sockets;
using System.Web.SessionState;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using System.Web.UI;
using System.Drawing.Imaging;
using System.Drawing;
using System.Configuration;
using System.Web.Configuration;
using System.Net.Mail;
using System.Security;
using MidaxLib;

namespace MidaxWebService
{
    public class MidaxWebServer
    {
        public static readonly string WEBSERVICE_RESULT_OK = "{\"Status\":\"OK\"}";
        public static readonly string WEBSERVICE_RESULT_ERROR = "{\"Status\":\"Error\"}";
        public static readonly string WEBSERVICE_RESULT_ERROR_MSG = "{\"Status\":\"Error\",\"Message\":\"{0}\"}";
        public static readonly string WEBSERVICE_RESULT_CONNECTED = "{\"Status\":\"Connected\"}";
        public static readonly string WEBSERVICE_RESULT_DISCONNECTED = "{\"Status\":\"Disconnected\"}";

        static MidaxWebServer _instance = null;
        CassandraConnection _midaxDB = null;

        MidaxWebServer() 
        {}

        static public MidaxWebServer Instance
        {
            get { return _instance == null ? _instance = new MidaxWebServer() : _instance; }
        }

        public bool OpenSession()
        {
            if (IsStarted())
                return true;
            try
            {
                Configuration rootWebConfig = null;
                try
                {
                    rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/Midax/Web.config");
                }
                catch (Exception)
                {
                    rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/WebDebug/Web.config");
                }
                Dictionary<string,string> dicSettings = new Dictionary<string,string>();
                foreach (var key in rootWebConfig.AppSettings.Settings.AllKeys)
                    dicSettings[key] = rootWebConfig.AppSettings.Settings[key].Value;
                Config.Settings = dicSettings;
                _midaxDB = (CassandraConnection)PublisherConnection.Instance;
            }
            catch (Exception exc)
            {
                Log.Instance.WriteEntry(exc.ToString(), System.Diagnostics.EventLogEntryType.Error);
            }            
            return true;
        }

        public bool IsStarted()
        {
            return (_midaxDB != null);
        }        

        public string GetStatus(HttpSessionState userSession)
        {
            if (!IsStarted())
                return WEBSERVICE_RESULT_DISCONNECTED;
            return WEBSERVICE_RESULT_OK;
        }

        public string GetStockData(string begin, string end, string stockId)
        {
            return _midaxDB.GetJSON(DateTime.Parse(begin), DateTime.Parse(end), CassandraConnection.DATATYPE_STOCK, stockId);
        }

        public string GetIndicatorData(string begin, string end, string indicatorId)
        {
            return _midaxDB.GetJSON(DateTime.Parse(begin), DateTime.Parse(end), CassandraConnection.DATATYPE_INDICATOR, indicatorId);
        }

        public string GetSignalData(string begin, string end, string signalId)
        {
            return _midaxDB.GetJSON(DateTime.Parse(begin), DateTime.Parse(end), CassandraConnection.DATATYPE_SIGNAL, signalId);
        }
    }    
}
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
                    rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/Web.config");
                }
                catch (Exception)
                {
                    rootWebConfig = WebConfigurationManager.OpenWebConfiguration("/WebDebug/Web.config");
                }
                _midaxDB = CassandraConnection.Instance;
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

        public string GetStockData(string stockId)
        {
            return _midaxDB.GetJSON(DateTime.Now, DateTime.Now, CassandraConnection.DATATYPE_STOCK, stockId);
        }

        public string GetIndicatorData(string indicatorId)
        {
            return _midaxDB.GetJSON(DateTime.Now, DateTime.Now, CassandraConnection.DATATYPE_INDICATOR, indicatorId);
        }

        public string GetSignalData(string signalId)
        {
            return _midaxDB.GetJSON(DateTime.Now, DateTime.Now, CassandraConnection.DATATYPE_SIGNAL, signalId);
        }
    }    
}
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
using Newtonsoft.Json;

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
        Dictionary<string, decimal> _avg = new Dictionary<string, decimal>();
        Dictionary<string, decimal> _scale = new Dictionary<string, decimal>();

        MidaxWebServer() 
        {
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
                Dictionary<string, string> dicSettings = new Dictionary<string, string>();
                foreach (var key in rootWebConfig.AppSettings.Settings.AllKeys)
                    dicSettings[key] = rootWebConfig.AppSettings.Settings[key].Value;
                Config.Settings = dicSettings;
                MarketDataConnection.Instance.Connect(null);
            }
            catch (Exception exc)
            {
                Log.Instance.WriteEntry(exc.ToString(), System.Diagnostics.EventLogEntryType.Error);
            } 
        }

        static public MidaxWebServer Instance
        {
            get { return _instance == null ? _instance = new MidaxWebServer() : _instance; }
        }

        public bool IsStarted()
        {
            return (Config.Settings != null);
        }  

        public string GetStatus(HttpSessionState userSession)
        {
            if (!IsStarted())
                return WEBSERVICE_RESULT_DISCONNECTED;
            return WEBSERVICE_RESULT_OK;
        }

        public string GetStockData(string begin, string end, string stockId)
        {
            return GetJSON(Config.ParseDateTimeLocal(begin), Config.ParseDateTimeLocal(end), CassandraConnection.DATATYPE_STOCK, stockId, true);
        }

        public string GetIndicatorData(string begin, string end, string indicatorId)
        {
            return GetJSON(Config.ParseDateTimeLocal(begin), Config.ParseDateTimeLocal(end), CassandraConnection.DATATYPE_INDICATOR, indicatorId, true);
        }

        public string GetSignalData(string begin, string end, string signalId)
        {
            return GetJSON(Config.ParseDateTimeLocal(begin), Config.ParseDateTimeLocal(end), CassandraConnection.DATATYPE_SIGNAL, signalId, false);
        }

        public string GetJSON(DateTime startTime, DateTime stopTime, string type, string id, bool auto_select)
        {
            var ids = new List<string> { id };
            var rowSets = PublisherConnection.Instance.Database.GetRows(startTime, stopTime, type, ids);            
            if (rowSets == null)
                return @"[]";
            foreach (var quoteSet in rowSets)
                quoteSet.Value.Reverse();
            List<CqlQuote> filteredQuotes = new List<CqlQuote>();
            decimal? prevQuoteValue = null;
            CqlQuote prevQuote = new CqlQuote();
            bool? trendUp = null;
            // find local minima
            List<Gap> gaps = new List<Gap>();
            SortedList<decimal, CqlQuote> buffer = new SortedList<decimal,CqlQuote>();

            decimal min = 1000000;
            decimal max = 0;
            List<CqlQuote> quotes = new List<CqlQuote>();
            try
            {
                foreach (CqlQuote cqlQuote in rowSets[id])
                {
                    if (cqlQuote.b < min)
                        min = cqlQuote.b.Value;
                    if (cqlQuote.b > max)
                        max = cqlQuote.b.Value;
                    quotes.Add(cqlQuote);
                }
            }
            catch
            {
                return @"[]";
            }
            if (quotes.Count == 0)
                return @"[]";
            DateTime ts = new DateTime(quotes.Last().t.Ticks);
            DateTime te = new DateTime(quotes.First().t.Ticks);
            startTime = startTime > te ? startTime : ts;
            stopTime = stopTime < te ? stopTime : te;
            double intervalSeconds = Math.Max(1, Math.Ceiling((stopTime - startTime).TotalSeconds) / 250);
            double intervalSecondsLarge = Math.Max(1, Math.Ceiling((stopTime - startTime).TotalSeconds) / 100);            
            string keyAvg = id.Split('_').Last() + "_" +startTime.ToShortDateString();
            if (type == PublisherConnection.DATATYPE_STOCK)
            {
                _avg[keyAvg] = (min + max) / 2m;
                _scale[keyAvg] = (max - min) / 2m;
            }
            foreach (CqlQuote cqlQuote in quotes)
            {
                decimal quoteValue = cqlQuote.ScaleValue(_avg[keyAvg], _scale[keyAvg]);
                if (!prevQuoteValue.HasValue)
                {
                    filteredQuotes.Add(cqlQuote);
                    prevQuoteValue = quoteValue;
                    prevQuote = cqlQuote;
                    continue;
                }                
                if (!trendUp.HasValue)
                {
                    trendUp = quoteValue > prevQuoteValue;
                    prevQuoteValue = quoteValue;
                    prevQuote = cqlQuote;
                    if (auto_select && (prevQuote.t - cqlQuote.t).TotalSeconds < intervalSeconds)
                        buffer.Add(quoteValue, cqlQuote);
                    else
                        filteredQuotes.Add(cqlQuote);
                    continue;
                }
                if (((quoteValue < prevQuoteValue) && trendUp.Value) ||
                    ((quoteValue > prevQuoteValue) && !trendUp.Value))
                {
                    if (auto_select && (prevQuote.t - cqlQuote.t).TotalSeconds < intervalSeconds)
                    {
                        if (!buffer.ContainsKey(quoteValue))
                            buffer.Add(quoteValue, cqlQuote);
                        continue;
                    }
                    if (buffer.Count > 1)
                    {
                        if (buffer.First().Value.t > buffer.Last().Value.t)
                        {
                            filteredQuotes.Add(buffer.First().Value);
                            filteredQuotes.Add(buffer.Last().Value);
                        }
                        else
                        {
                            filteredQuotes.Add(buffer.Last().Value);
                            filteredQuotes.Add(buffer.First().Value);
                        }
                    }
                    else if (buffer.Count == 1)
                        filteredQuotes.Add(buffer.First().Value);                        
                    buffer.Clear();
                    trendUp = !trendUp;
                }
                else
                {
                    if (auto_select && (prevQuote.t - cqlQuote.t).TotalSeconds < intervalSecondsLarge)
                    {
                        if (!buffer.ContainsKey(quoteValue))
                            buffer.Add(quoteValue, cqlQuote);
                        continue;
                    }
                    if (buffer.Count > 1)
                    {
                        if (buffer.First().Value.t > buffer.Last().Value.t)
                        {
                            filteredQuotes.Add(buffer.First().Value);
                            filteredQuotes.Add(buffer.Last().Value);
                        }
                        else
                        {
                            filteredQuotes.Add(buffer.Last().Value);
                            filteredQuotes.Add(buffer.First().Value);
                        }
                    }
                    else if (buffer.Count == 1)
                        filteredQuotes.Add(buffer.First().Value);
                    buffer.Clear();
                }
                buffer.Add(quoteValue, cqlQuote);
                prevQuoteValue = quoteValue;
                prevQuote = cqlQuote;
            }
            if (filteredQuotes.Last() != prevQuote)
                filteredQuotes.Add(prevQuote);
                                    
            string json = "[";
            foreach (var row in filteredQuotes)
            {
                json += JsonConvert.SerializeObject(row) + ",";
            }
            return json.Substring(0, json.Length - 1) + "]";
        } 
    }   
}
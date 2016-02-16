using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using MidaxLib;

namespace MarkitDataFeedTest
{
    class Program
    {
        const string DATA_SRC = "http://dev.markitondemand.com/Api/v2/Quote/jsonp";
        static Dictionary<string, KeyValuePair<DateTime, decimal>> stockPriceCache = new Dictionary<string, KeyValuePair<DateTime, decimal>>();

        static void GET(string symbol)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format("{0}?symbol={1}", DATA_SRC, symbol));
                request.Method = "GET";

                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var reader = new StreamReader(response.GetResponseStream()))
                    {
                        JavaScriptSerializer js = new JavaScriptSerializer();
                        var responseData = reader.ReadToEnd();
                        var dict = js.Deserialize<dynamic>(responseData.Substring(18, responseData.Length - 19));

                        var marketData = new MarketData(symbol);
                        var dateComponents = dict["Timestamp"].Split(' ');
                        var timeComponents = dateComponents[3].Split(':');
                        var month = DateTime.ParseExact(dateComponents[1], "MMM", CultureInfo.CurrentCulture).Month;
                        var updateTime = new DateTime(int.Parse(dateComponents[5]), month, int.Parse(dateComponents[2]), int.Parse(timeComponents[0]), int.Parse(timeComponents[1]), int.Parse(timeComponents[2]), DateTimeKind.Utc);
                        var hourOffset = int.Parse(dateComponents[4].Split('-')[1].Split(':')[0]);
                        updateTime = updateTime.AddHours(hourOffset);
                        if (!stockPriceCache.ContainsKey(symbol))
                            stockPriceCache[symbol] = new KeyValuePair<DateTime, decimal>(DateTime.MinValue, 0m);
                        if (stockPriceCache[symbol].Key != updateTime && stockPriceCache[symbol].Value != (decimal)dict["LastPrice"])
                        {
                            stockPriceCache[symbol] = new KeyValuePair<DateTime, decimal>(updateTime, dict["LastPrice"]);
                            var stockPrice = new Price(dict["LastPrice"]);
                            stockPrice.Volume = dict["Volume"];
                            CassandraConnection.Instance.Insert(updateTime, marketData, stockPrice);
                        }
                    }
                }
            }
            catch (WebException ex)
            {
                if (ex.Status != WebExceptionStatus.ProtocolError)
                    Log.Instance.WriteEntry(ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
            }
        }

        static void Main(string[] args)
        {
            Config.Settings = new Dictionary<string, string>();
            Config.Settings["DB_CONTACTPOINT"] = "192.168.1.26";
            Config.Settings["PUBLISHING_START_TIME"] = string.Format("{0}:{1}:{2}", 6, 45, 0);
            Config.Settings["PUBLISHING_STOP_TIME"] = string.Format("{0}:{1}:{2}", 23, 30, 0);
            Config.Settings["TRADING_START_TIME"] = string.Format("{0}:{1}:{2}", 8, 0, 0);
            Config.Settings["TRADING_STOP_TIME"] = string.Format("{0}:{1}:{2}", 23, 0, 0);
            Config.Settings["TRADING_CLOSING_TIME"] = string.Format("{0}:{1}:{2}", 22, 45, 0);
            Config.Settings["TRADING_MODE"] = "REPLAY_UAT";
            Config.Settings["REPLAY_MODE"] = "DB";
            Config.Settings["TRADING_LIMIT_PER_BP"] = "10";
            Config.Settings["TRADING_CURRENCY"] = "GBP";

            while (true)
            {
                GET("AAPL");
                Thread.Sleep(1500);
            }
        }
    }
}

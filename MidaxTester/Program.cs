using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MidaxLib;

namespace MidaxTester
{   
    class Program
    {
        static void Main(string[] args)
        {
            Dictionary<string, string> dicSettings = new Dictionary<string, string>();
            dicSettings["APP_NAME"] = "Midax";
            dicSettings["STOP_LOSS"] = "50";
            dicSettings["STOP_GAIN"] = "200";
            dicSettings["LIMIT"] = "200";
            dicSettings["PUBLISHING_START_TIME"] = "00:00:01";
            dicSettings["PUBLISHING_STOP_TIME"] = "23:59:59";
            dicSettings["PUBLISHING_DISABLED"] = "1";
            dicSettings["PUBLISHING_CONTACTPOINT"] = "192.168.1.26";
            dicSettings["PUBLISHING_CSV"] = @"D:\Shared\Tests\test.csv";
            dicSettings["TRADING_START_TIME"] = "2015-08-26 08:00:00";
            dicSettings["TRADING_STOP_TIME"] = "2015-08-26 09:00:00";
            dicSettings["TRADING_MODE"] = "REPLAY";
            dicSettings["MINIMUM_BET"] = "2";
            Config.Settings = dicSettings;

            MarketDataConnection.Instance.Connect();
            MarketData index = new MarketData("DAX:IX.D.DAX.DAILY.IP", new TimeSeries());
            List<MarketData> marketData = new List<MarketData>();
            marketData.Add(new MarketData("Adidas AG:ED.D.ADSGY.DAILY.IP", new TimeSeries()));
            marketData.Add(new MarketData("Allianz SE:ED.D.ALVGY.DAILY.IP", new TimeSeries()));
            marketData.Add(new MarketData("BASF SE:ED.D.BAS.DAILY.IP", new TimeSeries()));
            marketData.Add(new MarketData("Bayer AG:ED.D.BAY.DAILY.IP", new TimeSeries()));
        
            ModelMidax model = new ModelMidax(index, marketData);
            model.StartSignals();

            PublisherConnection.Instance.Close();
        }
    }
}

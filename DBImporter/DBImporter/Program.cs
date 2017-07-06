using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidaxLib;

namespace DBImporter
{
    class Program
    {
        static void Main(string[] args)
        {
            int curArg = 0;
            DateTime start = DateTime.MinValue;
            DateTime end = DateTime.MinValue;
            bool wholeMonth = false;
            if (args[0].Split('-').Length == 2)
            {
                var year = int.Parse(args[0].Split('-')[0]);
                var month =  int.Parse(args[0].Split('-')[1]);
                start = new DateTime(year, month, 1);
                end = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59);
                wholeMonth = true;
                curArg++;
            }
            else
            {
                start = DateTime.Parse(args[curArg++]);
                end = DateTime.Parse(args[curArg++]);
            }

            bool restoreDB = false;
            bool fromDB = true;
            bool deleteDB = false;
            bool fromProd = false;
            bool perHour = false;
            if (args.Length > curArg)
            {
                restoreDB = (args[curArg++].ToUpper() == "-RESTOREDB");
                fromDB = false;
            }
            if (args.Length > curArg)
            {
                fromDB = (args[curArg].ToUpper() == "-FROMDB");
                deleteDB = (args[curArg].ToUpper() == "-DELETEDB");
                if (fromDB || deleteDB)
                    curArg++;
            }
            if (args.Length > curArg)
            {
                fromProd = (args[curArg].ToUpper() == "-PROD");
                if (fromProd)
                    curArg++;
            }
            if (args.Length > curArg)
            {
                perHour = (args[curArg].ToUpper() == "-PERHOUR");
                curArg++;
            }
            Dictionary<string, string> dicSettings = new Dictionary<string, string>();
            dicSettings["APP_NAME"] = "Midax";
            dicSettings["DB_CONTACTPOINT"] = "192.168.1.25";
            dicSettings["REPLAY_MODE"] = (!deleteDB && !fromDB) ? "CSV" : "DB";
            dicSettings["REPLAY_POPUP"] = "1";
            dicSettings["TRADING_MODE"] = fromProd ? "IMPORT" : "IMPORT_UAT";
            dicSettings["SAMPLING_MS"] = "1000";
            dicSettings["FX_GBPUSD"] = "GBPUSD:CS.D.GBPUSD.TODAY.IP";
            dicSettings["FX_GBPEUR"] = "GBPEUR:CS.D.GBPEUR.TODAY.IP";
            dicSettings["FX_EURUSD"] = "EURUSD:CS.D.EURUSD.TODAY.IP";
            dicSettings["FX_USDJPY"] = "USDJPY:CS.D.USDJPY.TODAY.IP";
            dicSettings["FX_AUDUSD"] = "AUDUSD:CS.D.AUDUSD.TODAY.IP";
            dicSettings["TIME_GMT_MARKETDATA"] = "5";
            Config.Settings = dicSettings;

            while (start <= end)
            {
                Config.Settings["PUBLISHING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, start.Hour, start.Minute, start.Second);
                if (perHour)
                {
                    Config.Settings["PUBLISHING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, start.Hour == 23 ? 23 : (start.Hour + 1), start.Hour == 23 ? 59 : start.Minute, start.Hour == 23 ? 59 : start.Second);
                }
                else
                {
                    Config.Settings["PUBLISHING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 23, 55, 0);
                }
                Config.Settings["TRADING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 8, 0, 0);
                Config.Settings["TRADING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 21, 30, 0);
                //Config.Settings["TRADING_CLOSING_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 17, 0, 0);
                if (deleteDB)
                    Config.Settings["DELETEDB"] = "1";
                else
                {
                    if (!restoreDB)
                    {
                        if (wholeMonth)
                            Config.Settings["PUBLISHING_CSV"] = string.Format("..\\..\\..\\MktData\\mktdata_{0}_{1}.csv", start.Month, start.Year);
                        else
                            Config.Settings["PUBLISHING_CSV"] = string.Format("..\\..\\..\\MktData\\mktdata_{0}_{1}_{2}.csv", start.Day, start.Month, start.Year);
                    }
                    if (!fromDB)
                    {
                        List<string> mktdataFiles = new List<string>();
                        if (wholeMonth)
                            mktdataFiles.Add(string.Format("..\\..\\..\\MktData\\mktdata_eurusd_{0}_{1}.csv", start.Month, start.Year));
                        else
                            mktdataFiles.Add(string.Format("..\\..\\..\\MktData\\mktdata_{0}_{1}_{2}.csv", start.Day, start.Month, start.Year));
                        Config.Settings["REPLAY_CSV"] = Config.TestList(mktdataFiles);
                    }
                }              

                MarketDataConnection.Instance.Connect(null);

                var lstIndices = new List<MarketData>();
                var gbpusd = new MarketData(dicSettings["FX_GBPUSD"]);
                var gbpeur = new MarketData(dicSettings["FX_GBPEUR"]);
                var eurusd = new MarketData(dicSettings["FX_EURUSD"]);
                var usdjpy = new MarketData(dicSettings["FX_USDJPY"]);
                var audusd = new MarketData(dicSettings["FX_AUDUSD"]);
                //lstIndices.Add(gbpusd);
                //lstIndices.Add(gbpeur);
                lstIndices.Add(eurusd);
                //lstIndices.Add(usdjpy);
                //lstIndices.Add(audusd);
                /*
                var indexDAX = new MarketData("DAX:IX.D.DAX.DAILY.IP");
                indexDAX.Subscribe(OnUpdate, null);
                var indexCAC = new MarketData("CAC:IX.D.CAC.DAILY.IP");
                indexCAC.Subscribe(OnUpdate, null);
                var indexDOW = new MarketData("DOW:IX.D.DOW.DAILY.IP");
                indexDOW.Subscribe(OnUpdate, null);
                var indexNYSE_DOW = new MarketData("DOW:IceConnection.DOW", "IX.D.DOW.DAILY.IP");
                //indexNYSE_DOW.Subscribe(OnUpdate, null);
                lstIndices.Add(indexDAX);
                lstIndices.Add(indexCAC);
                lstIndices.Add(indexDOW);*/

                var lstIndicators = new List<IndicatorLevel>();
                foreach (var index in lstIndices){
                    lstIndicators.Add(new IndicatorLow(index));
                    lstIndicators.Add(new IndicatorHigh(index));
                    lstIndicators.Add(new IndicatorCloseBid(index));
                    lstIndicators.Add(new IndicatorCloseOffer(index));                   
                }
                foreach(var indicator in lstIndicators)
                    indicator.Subscribe(OnUpdate, null);
                foreach (MarketData mktData in lstIndices)
                    mktData.GetMarketLevels();  

                MarketDataConnection.Instance.StartListening();

                if (!deleteDB)
                {
                    foreach (var indicator in lstIndicators)
                        indicator.Publish(Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]));
                }
                MarketDataConnection.Instance.StopListening();

                foreach (var indicator in lstIndices)
                    indicator.Clear();

                do
                {
                    if (perHour)
                        start = start.AddHours(1);
                    else
                        start = start.AddDays(1);
                }
                while (start.DayOfWeek == DayOfWeek.Saturday || start.DayOfWeek == DayOfWeek.Sunday);
            }
        }

        static void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
        }
    }
}

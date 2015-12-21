using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MidaxLib;

namespace Calibrator
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime start = DateTime.Parse(args[0]);
            DateTime end = DateTime.Parse(args[1]);

            Dictionary<string, string> dicSettings = new Dictionary<string, string>();
            dicSettings["APP_NAME"] = "Midax";
            dicSettings["LIMIT"] = "10";
            dicSettings["PUBLISHING_CONTACTPOINT"] = "192.168.1.26";
            dicSettings["REPLAY_MODE"] = "CSV";
            dicSettings["REPLAY_POPUP"] = "1";
            dicSettings["TRADING_MODE"] = "CALIBRATION";
            Config.Settings = dicSettings;

            // read market data and indicator values
            var marketData = new Dictionary<string, List<CqlQuote>>();
            var indicatorData = new Dictionary<string, List<CqlQuote>>();
            var profitData = new Dictionary<string, List<double>>();
            while (start <= end)
            {
                List<string> mktdataFiles = new List<string>();
                mktdataFiles.Add(string.Format("..\\..\\..\\MarketSelector\\MktSelectorData\\mktselectdata_{0}_{1}_{2}.csv", start.Day, start.Month, start.Year));
                Config.Settings["REPLAY_CSV"] = Config.TestList(mktdataFiles);
                Config.Settings["PUBLISHING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 6, 45, 0);
                Config.Settings["PUBLISHING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 18, 0, 0);
                Config.Settings["TRADING_START_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 8, 0, 0);
                Config.Settings["TRADING_STOP_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 17, 0, 0);
                Config.Settings["TRADING_CLOSING_TIME"] = string.Format("{0}-{1}-{2} {3}:{4}:{5}", start.Year, start.Month, start.Day, 16, 55, 0);
                //Config.Settings["PUBLISHING_CSV"] = string.Format("..\\..\\CalibrationData\\calibdata_{0}_{1}_{2}.csv", start.Day, start.Month, start.Year);

                var client = new ReplayStreamingClient();
                client.Connect();
                string[] ids = new string[1];
                ids[0] = "IX.D.DAX.DAILY.IP";
                Dictionary<string, List<CqlQuote>> curDayMktData = client.GetReplayData(ids);
                foreach (var keyVal in curDayMktData)
                {
                    var processableMktData = new List<CqlQuote>();
                    foreach (var quote in keyVal.Value)
                    {
                        if (client.ExpectedIndicatorData["WMA_60_IX.D.DAX.DAILY.IP"].Select(cqlq => cqlq.t).Contains(quote.t))
                            processableMktData.Add(quote);
                    }
                    if (marketData.ContainsKey(keyVal.Key))
                        marketData[keyVal.Key].AddRange(processableMktData);
                    else
                        marketData[keyVal.Key] = processableMktData;
                }
                foreach (var keyVal in client.ExpectedIndicatorData)
                {
                    if (indicatorData.ContainsKey(keyVal.Key))
                        indicatorData[keyVal.Key].AddRange(keyVal.Value);
                    else
                        indicatorData[keyVal.Key] = keyVal.Value;
                }
                foreach (var keyVal in client.ExpectedProfitData)
                {
                    if (!profitData.ContainsKey(keyVal.Key.Key))
                        profitData[keyVal.Key.Key] = new List<double>();
                    profitData[keyVal.Key.Key].Add(keyVal.Value);                        
                }

                // process next day
                do
                {
                    start = start.AddDays(1);
                }
                while (start.DayOfWeek == DayOfWeek.Saturday || start.DayOfWeek == DayOfWeek.Sunday);
            }

            // build the neural network and the training set
            var ann = new NeuralNetwork(4, 1, new List<int>() { 2 });
            var daxQuotes = new List<double>();
            foreach (var quote in marketData["IX.D.DAX.DAILY.IP"])
                daxQuotes.Add((double)quote.MidPrice());
            var wma2 = new List<double>();
            foreach (var quote in indicatorData["WMA_2_IX.D.DAX.DAILY.IP"])
                wma2.Add((double)quote.MidPrice());
            var wma10 = new List<double>();
            foreach (var quote in indicatorData["WMA_10_IX.D.DAX.DAILY.IP"])
                wma10.Add((double)quote.MidPrice());
            var wma60 = new List<double>();
            foreach (var quote in indicatorData["WMA_60_IX.D.DAX.DAILY.IP"])
                wma60.Add((double)quote.MidPrice());
            var annInputs = new List<List<double>>();
            var annOutputs = new List<List<double>>();
            for (int idxQuote = 0; idxQuote < daxQuotes.Count; idxQuote++)
            {
                var newInputset = new List<double>();
                newInputset.Add(daxQuotes[idxQuote]);
                newInputset.Add(wma2[idxQuote]);
                newInputset.Add(wma10[idxQuote]);
                newInputset.Add(wma60[idxQuote]);
                annInputs.Add(newInputset);
                var newOutputset = new List<double>();
                newOutputset.Add(profitData["IX.D.DAX.DAILY.IP"][idxQuote]);
                annOutputs.Add(newOutputset);
            }
            ann.Train(annInputs, annOutputs);
        }

        static void OnUpdateMktData(MarketData mktData, DateTime updateTime, Price value)
        {
        }
    }
}

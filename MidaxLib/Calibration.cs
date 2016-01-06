using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lightstreamer.DotNet.Client;

namespace MidaxLib
{
    // generates the set of market data and objective function for calibration
    class CalibrationStreamingClient : ReplayStreamingClient
    {
        public override void Subscribe(string[] epics, IHandyTableListener tableListener)
        {
            Dictionary<string, List<CqlQuote>> priceData = GetReplayData(epics);

            foreach (var epic in epics)
            {
                // for each quote, associate the observed gains in the near future
                var mktData = new Asset(epic, priceData[epic].First().t.UtcDateTime);
                var wmaLow = new IndicatorWMA(mktData, 2);
                var wmaMid = new IndicatorWMA(mktData, 10);
                var wmaHigh = new IndicatorWMA(mktData, 60);
                var wmaDailyAvg = new IndicatorLevelMean(mktData);

                foreach (var quote in priceData[epic])
                    mktData.TimeSeries.Add(quote.t.UtcDateTime, new Price(quote.MidPrice()));
                foreach (var quote in ExpectedIndicatorData[wmaLow.Id])
                    wmaLow.TimeSeries.Add(quote.t.UtcDateTime, new Price(quote.ScaleValue(0, 1)));
                foreach (var quote in ExpectedIndicatorData[wmaLow.Id])
                    wmaLow.TimeSeries.Add(quote.t.UtcDateTime, new Price(quote.ScaleValue(0, 1)));
                
                var expectations = new Dictionary<DateTime, KeyValuePair<CqlQuote, decimal>>();
                var gainDistribution = new SortedList<int, DateTime>();
                KeyValuePair<int, DateTime> minProfit = new KeyValuePair<int, DateTime>(1000000, DateTime.MinValue);
                KeyValuePair<int, DateTime> maxProfit = new KeyValuePair<int, DateTime>(-1000000, DateTime.MinValue);
                var rnd = new Random(155);
                var openPrice = priceData[epic][0];
                foreach (var quote in priceData[epic])
                {
                    if (quote.t.TimeOfDay < Config.ParseDateTimeLocal(Config.Settings["TRADING_START_TIME"]).TimeOfDay)
                        continue;
                    var futureVal = wmaLow.Average(mktData, quote.t.UtcDateTime.AddMinutes(2));
                    var profit = (int)Math.Round(futureVal.Mid() - quote.MidPrice());
                    expectations.Add(quote.t.UtcDateTime, new KeyValuePair<CqlQuote, decimal>(quote, profit));
                    if (gainDistribution.ContainsKey(profit))
                    {
                        if ((quote.t.UtcDateTime - gainDistribution[profit]).Hours > 3 && (rnd.Next(100) == 0))
                            gainDistribution[profit] = quote.t.UtcDateTime;
                    }
                    else
                        gainDistribution[profit] = quote.t.UtcDateTime;
                    if (profit < minProfit.Key)
                        minProfit = new KeyValuePair<int, DateTime>(profit, gainDistribution[profit]);
                    if (profit > maxProfit.Key)
                        maxProfit = new KeyValuePair<int, DateTime>(profit, gainDistribution[profit]);
                    quote.b -= openPrice.MidPrice();
                    quote.o -= openPrice.MidPrice();
                }
                int nbPoints = 10;
                int idxProfit = 0;
                KeyValuePair<int, DateTime> nextProfit = minProfit;
                var selection = new SortedList<DateTime, KeyValuePair<int, CqlQuote>>();
                while (idxProfit++ < nbPoints)
                {
                    PublisherConnection.Instance.Insert(nextProfit.Value, new Value(nextProfit.Key));
                    selection.Add(nextProfit.Value, new KeyValuePair<int, CqlQuote>(nextProfit.Key, expectations[nextProfit.Value].Key));
                    nextProfit = gainDistribution.First(keyVal => keyVal.Key >= ((decimal)minProfit.Key + (decimal)idxProfit * (decimal)(maxProfit.Key - minProfit.Key) / (decimal)nbPoints));
                }
                foreach (var profit in selection)
                {
                    PublisherConnection.Instance.Insert(gainDistribution[profit.Value.Key], wmaLow, wmaLow.Average(mktData, gainDistribution[profit.Value.Key]).Mid() - openPrice.MidPrice());
                    PublisherConnection.Instance.Insert(gainDistribution[profit.Value.Key], wmaMid, wmaMid.Average(mktData, gainDistribution[profit.Value.Key]).Mid() - openPrice.MidPrice());
                    PublisherConnection.Instance.Insert(gainDistribution[profit.Value.Key], wmaHigh, wmaHigh.Average(mktData, gainDistribution[profit.Value.Key]).Mid() - openPrice.MidPrice());
                    PublisherConnection.Instance.Insert(gainDistribution[profit.Value.Key], wmaDailyAvg, wmaDailyAvg.Average().Mid() - openPrice.MidPrice());
                }
                priceData[epic] = selection.Values.Select(keyVal => keyVal.Value).ToList();
            }
            replay(priceData, tableListener);
        }
    }

    public class CalibrationConnection : ReplayConnection
    {
        public CalibrationConnection()
            : base(new CalibrationStreamingClient())
        {
        }
    }

    public class NeuralNetworkForCalibration : NeuralNetwork
    {
        protected List<List<double>> _annInputs = new List<List<double>>();
        protected List<List<double>> _annOutputs = new List<List<double>>();

        string _annid;
        public string AnnId
        {
            get { return _annid; }
        }
        string _stockid;
        public string StockId
        {
            get { return _stockid; }
        }
        int _version;
        public int Version
        {
            get { return _version; }
        }

        public NeuralNetworkForCalibration(string id, string stockid, int nbInputNeurons, int nbOutputNeurons, List<int> intermediaryNeuronNbs, int version = -1)
            : base(nbInputNeurons, nbOutputNeurons, intermediaryNeuronNbs)
        {
            _annid = id;
            _stockid = stockid;
            if (version == -1)
                version = StaticDataConnection.Instance.GetAnnLatestVersion(_annid, _stockid) + 1;
            _version = version;
        }

        public void Train(double max_error)
        {
            base.Train(_annInputs, _annOutputs, (double)_annInputs.Count * 0.01, max_error);
        }
    }
}

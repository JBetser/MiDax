using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lightstreamer.DotNet.Client;

namespace MidaxLib
{
    class CalibrationStreamingClient : ReplayStreamingClient
    {
        public override void Subscribe(string[] epics, IHandyTableListener tableListener)
        {
            Dictionary<string, List<CqlQuote>> priceData = GetReplayData(epics);

            foreach (var epic in epics)
            {
                // for each quote, associate the observed gains in the near future
                var mktData = new MarketData(epic);
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
                int minGain = 1000000;
                int maxGain = -1000000;
                var rnd = new Random(155);
                var openPrice = priceData[epic][0];
                foreach (var quote in priceData[epic])
                {
                    if (quote.t.TimeOfDay < DateTime.Parse(Config.Settings["TRADING_START_TIME"]).TimeOfDay)
                        continue;
                    var futureVal = wmaLow.Average(mktData, quote.t.UtcDateTime.AddMinutes(2));
                    var gain = (int)Math.Round(futureVal.Mid() - quote.MidPrice());
                    expectations.Add(quote.t.UtcDateTime, new KeyValuePair<CqlQuote, decimal>(quote, gain));
                    if (gainDistribution.ContainsKey(gain))
                    {
                        if ((quote.t.UtcDateTime - gainDistribution[gain]).Hours > 3 && (rnd.Next(100) == 0))
                            gainDistribution[gain] = quote.t.UtcDateTime;
                    }
                    else
                        gainDistribution[gain] = quote.t.UtcDateTime;
                    if (gain < minGain)
                        minGain = gain;
                    if (gain > maxGain)
                        maxGain = gain;
                    quote.b -= openPrice.MidPrice();
                    quote.o -= openPrice.MidPrice();
                }
                int nbPoints = 10;
                int idxGain = 0;
                int nextGain = minGain;
                var selection = new SortedList<DateTime, KeyValuePair<int, CqlQuote>>();
                while (idxGain++ < nbPoints)
                {
                    PublisherConnection.Instance.Insert(new Value(nextGain));
                    selection.Add(gainDistribution[nextGain], new KeyValuePair<int, CqlQuote>(nextGain, expectations[gainDistribution[nextGain]].Key));
                    nextGain = gainDistribution.First(keyVal => keyVal.Key >= ((decimal)minGain + (decimal)idxGain * (decimal)(maxGain - minGain) / (decimal)nbPoints)).Key;
                }
                foreach (var gain in selection)
                {
                    PublisherConnection.Instance.Insert(gainDistribution[gain.Value.Key], wmaLow, wmaLow.Average(mktData, gainDistribution[gain.Value.Key]).Mid() - openPrice.MidPrice());
                    PublisherConnection.Instance.Insert(gainDistribution[gain.Value.Key], wmaMid, wmaMid.Average(mktData, gainDistribution[gain.Value.Key]).Mid() - openPrice.MidPrice());
                    PublisherConnection.Instance.Insert(gainDistribution[gain.Value.Key], wmaHigh, wmaHigh.Average(mktData, gainDistribution[gain.Value.Key]).Mid() - openPrice.MidPrice());
                    PublisherConnection.Instance.Insert(gainDistribution[gain.Value.Key], wmaDailyAvg, wmaDailyAvg.Average().Mid() - openPrice.MidPrice());
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
}

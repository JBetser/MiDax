using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lightstreamer.DotNet.Client;

namespace MidaxLib
{
    public class MarketSelectorStreamingClient : ReplayStreamingClient
    {
        public override void Subscribe(string[] epics, IHandyTableListener tableListener)
        {
            Dictionary<string, List<CqlQuote>> priceData = getReplayData(epics);

            foreach(var epic in epics){
                // for each quote, associate the observed gains in the near future
                TimeSeries mktDataTS = new TimeSeries();
                TimeSeries wmaLowTS = new TimeSeries();

                foreach (var quote in priceData[epic])
                    mktDataTS.Add(quote.t.UtcDateTime, new Price(quote.MidPrice()));
                foreach (var quote in ExpectedIndicatorData["WMA_2_" + epic])
                    wmaLowTS.Add(quote.t.UtcDateTime, new Price(quote.ScaleValue(0, 1)));

                MarketData mktData = new MarketData(epic);
                mktData.TimeSeries = mktDataTS;
                IndicatorLevelMean dailyMean = new IndicatorLevelMean(mktData);
                Price avg = dailyMean.Average();

                var expectations = new Dictionary<DateTime, KeyValuePair<CqlQuote, decimal>>();
                var gainDistribution = new SortedList<int, DateTime>();
                int minGain = 1000000;
                int maxGain = -1000000;
                var rnd = new Random(155);
                foreach (var quote in priceData[epic])
                {
                    var futureVal = wmaLowTS.Value(quote.t.UtcDateTime.AddMinutes(2));
                    if (!futureVal.HasValue)
                        break;
                    var gain = (int)Math.Round(futureVal.Value.Value.Mid() - quote.MidPrice());
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
                }
                int nbPoints = 10;
                int idxGain = 0;
                int nextGain = minGain;
                var selection = new SortedList<DateTime, CqlQuote>();
                while (idxGain++ < nbPoints)
                {
                    selection.Add(gainDistribution[nextGain], expectations[gainDistribution[nextGain]].Key);
                    nextGain = gainDistribution.First(keyVal => keyVal.Key >= ((decimal)minGain + (decimal)idxGain * (decimal)(maxGain - minGain) / (decimal)nbPoints)).Key;
                }
                priceData[epic] = selection.Values.ToList();
            }
            replay(priceData, tableListener);
        }
    }

    public class MarketSelectorConnection : ReplayConnection
    {
        public MarketSelectorConnection()
            : base(new MarketSelectorStreamingClient())
        {
        }
    }
}

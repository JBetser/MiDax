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
        static bool isTooClose(KeyValuePair<int, DateTime> keyVal, SortedList<int, DateTime> lst)
        {
            if (keyVal.Key == lst.First().Key || keyVal.Key == lst.Last().Key)
                return false;
            foreach (var kv in lst)
            {
                if (kv.Key > keyVal.Key && Math.Abs((kv.Value - keyVal.Value).TotalMinutes) < 2)
                    return true;
            }
            return false;
        }

        public override void Subscribe(string[] epics, IHandyTableListener tableListener)
        {
            Dictionary<string, List<CqlQuote>> priceData = GetReplayData(epics);
            if (priceData.Count == 0)
                return;

            foreach(var epic in epics){
                // for each quote, associate the observed gains in the near future
                var mktData = new MarketData(epic);
                var wmaLow = new IndicatorWMA(mktData, 2);
                var wmaMid = new IndicatorWMA(mktData, 10);
                var wmaHigh = new IndicatorWMA(mktData, 30);
                var wmaVeryHigh = new IndicatorWMA(mktData, 90);

                foreach (var quote in priceData[epic])
                    mktData.TimeSeries.Add(quote.t.UtcDateTime, new Price(quote.MidPrice()));
                
                var expectations = new Dictionary<DateTime, KeyValuePair<CqlQuote, decimal>>();
                var gainDistribution = new SortedList<int, DateTime>();
                KeyValuePair<int, DateTime> minProfit = new KeyValuePair<int, DateTime>(1000000, DateTime.MinValue);
                KeyValuePair<int, DateTime> maxProfit = new KeyValuePair<int, DateTime>(-1000000, DateTime.MinValue);
                var rnd = new Random(155);
                /*
                var lowPrice = new Price(1000000.0m);
                var highPrice = new Price(-1000000.0m);
                foreach (var quote in priceData[epic])
                {
                    if (quote.t.TimeOfDay < Config.ParseDateTimeLocal(Config.Settings["TRADING_START_TIME"]).TimeOfDay)
                        continue;
                    if (lowPrice.Mid() > quote.MidPrice())
                        lowPrice = new Price(quote);
                    if (highPrice.Mid() < quote.MidPrice())
                        highPrice = new Price(quote);
                }*/
                var tradingStart = Config.ParseDateTimeLocal(Config.Settings["TRADING_START_TIME"]);
                var wmaVeryHighStart = wmaHigh.Average(tradingStart);
                var amplitude = 100.0m;
                foreach (var quote in priceData[epic])
                {
                    if (quote.t.TimeOfDay < tradingStart.TimeOfDay)
                        continue;
                    var futureVal = wmaLow.Average(quote.t.UtcDateTime.AddMinutes(2));
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
                    quote.b = (quote.b - wmaVeryHighStart.Bid) / amplitude;
                    quote.o = (quote.o - wmaVeryHighStart.Offer) / amplitude;
                }
                gainDistribution = new SortedList<int,DateTime>((from elt in gainDistribution
                                                                 where !isTooClose(elt, gainDistribution)
                                                                 select elt).ToDictionary(keyVal => keyVal.Key, keyVal => keyVal.Value));
                int nbPoints = 10;
                int idxProfit = 0;
                KeyValuePair<int, DateTime> nextProfit = minProfit;
                var selection = new SortedList<DateTime, KeyValuePair<int, CqlQuote>>();
                while (idxProfit++ < nbPoints)
                {
                    selection.Add(gainDistribution[nextProfit.Key], new KeyValuePair<int, CqlQuote>(nextProfit.Key, expectations[gainDistribution[nextProfit.Key]].Key));
                    var nextKeyVal = gainDistribution.FirstOrDefault(keyVal => keyVal.Key > nextProfit.Key &&
                        keyVal.Key >= ((decimal)minProfit.Key + (decimal)idxProfit * (decimal)(maxProfit.Key - minProfit.Key) / (decimal)nbPoints));
                    if (nextKeyVal.Equals(default(KeyValuePair<int, DateTime>)))
                        break;
                    nextProfit = nextKeyVal;
                }
                foreach (var dt in selection.Keys)
                {
                    var wmaLowAvg = wmaLow.Average(dt);
                    var wmaMidAvg = wmaMid.Average(dt);
                    var wmaHighAvg = wmaHigh.Average(dt);
                    var wmaVeryHighAvg = wmaVeryHigh.Average(dt);
                    if (wmaLowAvg == null || wmaMidAvg == null || wmaHighAvg == null || wmaVeryHighAvg == null)
                        continue;
                    PublisherConnection.Instance.Insert(dt, wmaLow, (wmaLowAvg.Mid() - wmaVeryHighStart.Mid()) / amplitude);
                    PublisherConnection.Instance.Insert(dt, wmaMid, (wmaMidAvg.Mid() - wmaVeryHighStart.Mid()) / amplitude);
                    PublisherConnection.Instance.Insert(dt, wmaHigh, (wmaHighAvg.Mid() - wmaVeryHighStart.Mid()) / amplitude);
                    PublisherConnection.Instance.Insert(dt, wmaVeryHigh, (wmaVeryHighAvg.Mid() - wmaVeryHighStart.Mid()) / amplitude);
                    PublisherConnection.Instance.Insert(dt, epic, new Value((double)selection[dt].Key / ((double)amplitude / 2.0)));                    
                }
                //PublisherConnection.Instance.Insert(Config.ParseDateTimeLocal(Config.Settings["PUBLISHING_STOP_TIME"]), wmaDailyAvg, (wmaDailyAvg.Average().Mid() - midLevel.Mid()) / amplitude);
                priceData[epic] = selection.Values.Select(kv => kv.Value).ToList();
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

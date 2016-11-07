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

        static bool isNearEvent(KeyValuePair<int, DateTime> keyVal, Calendar dayCalendar, string ccyPair)
        {
            string evtName = "";
            return dayCalendar.IsNearEvent(ccyPair, keyVal.Value, ref evtName);
        }

        public override void Subscribe(string[] epics, IHandyTableListener tableListener)
        {
            Dictionary<string, List<CqlQuote>> priceData = GetReplayData(epics);
            if (priceData.Count == 0)
                return;

            Calendar dayCalendar = new Calendar(priceData.First().Value[0].t);

            foreach(var epic in epics){
                // for each quote, associate the observed gains in the near future
                var mktData = new MarketData(epic);
                var wmaLow = new IndicatorEMA(mktData, 2);
                var wmaMid = new IndicatorEMA(mktData, 10);
                var wmaHigh = new IndicatorEMA(mktData, 30);
                var wmaVeryHigh = new IndicatorEMA(mktData, 90);
                var rsiShort = new IndicatorRSI(mktData, 1, 14);
                var rsiLong = new IndicatorRSI(mktData, 2, 14);
                var trendShort = new IndicatorTrend(mktData, 90, 14, false);
                var trendLong = new IndicatorTrend(mktData, 180, 14, false);
                var wmvolLow = new IndicatorWMVol(mktData, wmaLow, 60, 90);
                var wmvolHigh = new IndicatorWMVol(mktData, wmaMid, 60, 90);
                var volTrendLow = new IndicatorTrend(wmvolLow, 30, 6, true);
                var volTrendHigh = new IndicatorTrend(wmvolHigh, 60, 6, true);
                var allIndicators = new List<IndicatorWMA>();
                allIndicators.Add(wmaLow);
                allIndicators.Add(wmaMid);
                allIndicators.Add(wmaHigh);
                allIndicators.Add(wmaVeryHigh);
                allIndicators.Add(rsiShort);
                allIndicators.Add(rsiLong);
                allIndicators.Add(trendShort);
                allIndicators.Add(trendLong);
                allIndicators.Add(wmvolLow);
                allIndicators.Add(wmvolHigh);
                allIndicators.Add(volTrendLow);
                allIndicators.Add(volTrendHigh);

                foreach (var quote in priceData[epic])
                {
                    var mktDataValue = new Price(quote.MidPrice());
                    mktData.Process(quote.t, mktDataValue);
                    foreach (var ind in allIndicators)
                        ind.Process(quote.t, mktDataValue);
                }
                
                var expectations = new Dictionary<DateTime, KeyValuePair<CqlQuote, decimal>>();
                var gainDistribution = new SortedList<int, DateTime>();
                KeyValuePair<int, DateTime> minProfit = new KeyValuePair<int, DateTime>(1000000, DateTime.MinValue);
                KeyValuePair<int, DateTime> maxProfit = new KeyValuePair<int, DateTime>(-1000000, DateTime.MinValue);
                var rnd = new Random(155);
                var tradingStart = Config.ParseDateTimeLocal(Config.Settings["TRADING_START_TIME"]);
                var tradingStop = Config.ParseDateTimeLocal(Config.Settings["TRADING_STOP_TIME"]);
                var wmaVeryHighStart = wmaVeryHigh.Average(tradingStart);
                var amplitude = 100.0m;
                foreach (var quote in priceData[epic])
                {
                    if (quote.t.TimeOfDay < tradingStart.TimeOfDay || quote.t.TimeOfDay > tradingStop.TimeOfDay)
                        continue;
                    string evtName = "";
                    if (dayCalendar.IsNearEvent(mktData.Name, quote.t, ref evtName))
                        continue;
                    var futureVal = (mktData.TimeSeries.Max(quote.t.AddMinutes(5), quote.t.AddMinutes(20)) +
                        mktData.TimeSeries.Min(quote.t.AddMinutes(5), quote.t.AddMinutes(20))) / 2m;
                    var profit = (int)Math.Round(futureVal - quote.MidPrice());
                    expectations.Add(quote.t, new KeyValuePair<CqlQuote, decimal>(quote, profit));
                    if (gainDistribution.ContainsKey(profit))
                    {
                        if ((quote.t - gainDistribution[profit]).Hours > 3 && (rnd.Next(100) == 0))
                            gainDistribution[profit] = quote.t;
                    }
                    else
                        gainDistribution[profit] = quote.t;
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
                    bool allValid = true;
                    foreach (var ind in allIndicators)
                    {
                        if (ind.TimeSeries[dt] == null)
                        {
                            allValid = false;
                            break;
                        }
                    }
                    if (!allValid)
                        continue;
                    PublisherConnection.Instance.Insert(dt, wmaLow, (wmaLow.TimeSeries[dt].Value.Value.Mid() - wmaVeryHighStart.Mid()) / amplitude);
                    PublisherConnection.Instance.Insert(dt, wmaMid, (wmaMid.TimeSeries[dt].Value.Value.Mid() - wmaVeryHighStart.Mid()) / amplitude);
                    PublisherConnection.Instance.Insert(dt, wmaHigh, (wmaHigh.TimeSeries[dt].Value.Value.Mid() - wmaVeryHighStart.Mid()) / amplitude);
                    PublisherConnection.Instance.Insert(dt, wmaVeryHigh, (wmaVeryHigh.TimeSeries[dt].Value.Value.Mid() - wmaVeryHighStart.Mid()) / amplitude);
                    PublisherConnection.Instance.Insert(dt, rsiShort, (rsiShort.TimeSeries[dt].Value.Value.Mid() - 50m) / amplitude);
                    PublisherConnection.Instance.Insert(dt, rsiLong, (rsiLong.TimeSeries[dt].Value.Value.Mid() - 50m) / amplitude);
                    PublisherConnection.Instance.Insert(dt, trendShort, trendShort.TimeSeries[dt].Value.Value.Mid() / 1000m);
                    PublisherConnection.Instance.Insert(dt, trendLong, trendLong.TimeSeries[dt].Value.Value.Mid() / 1000m);
                    PublisherConnection.Instance.Insert(dt, wmvolLow, wmvolLow.TimeSeries[dt].Value.Value.Mid() / 10m);
                    PublisherConnection.Instance.Insert(dt, wmvolHigh, wmvolHigh.TimeSeries[dt].Value.Value.Mid() / 10m);
                    PublisherConnection.Instance.Insert(dt, volTrendLow, volTrendLow.TimeSeries[dt].Value.Value.Mid());
                    PublisherConnection.Instance.Insert(dt, volTrendHigh, volTrendHigh.TimeSeries[dt].Value.Value.Mid());
                    PublisherConnection.Instance.Insert(dt, epic, new Value((double)selection[dt].Key / ((double)amplitude / 2.0)));                    
                }
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

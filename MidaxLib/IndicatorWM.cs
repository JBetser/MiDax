using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    /// <summary>
    /// Weighted Moving Average
    /// it gives more weight to recent data and as a result is more volatile than SMA
    /// Use TIME_DECAY_FACTOR setting to specify a linear time decay factor: latest_weight = TIME_DECAY_FACTOR * first_weight
    /// 
    /// WMA = sum(weighted averages) / sum(weight)
    /// </summary> 
    public class IndicatorWMA : Indicator
    {
        protected int _periodSeconds;
        protected decimal _periodMilliSeconds;
        protected TimeSpan _periodTimeSpan;
        protected TimeDecay _timeDecay;
        protected Price _curAvg = null;
        protected DateTime _curAvgTime;
        protected DateTime _decrementStartTime = DateTime.MinValue;
        protected Price _incrementAvg = null;

        public MarketData MarketData { get { return _mktData[0]; } }
        public int Period { get { return _periodSeconds; } }
        public TimeDecay TimeDecay { get { return _timeDecay; } }

        public IndicatorWMA(MarketData mktData, int periodMinutes)
            : base("WMA_" + periodMinutes + "_" + mktData.Id, new List<MarketData> { mktData })
        {
            _periodSeconds = periodMinutes * 60;
            _periodMilliSeconds = _periodSeconds * 1000m;
            _periodTimeSpan = new TimeSpan(0, 0, _periodSeconds);
            if (Config.Settings.ContainsKey("TIME_DECAY_FACTOR"))
                _timeDecay = new TimeDecayLinear(int.Parse(Config.Settings["TIME_DECAY_FACTOR"]), new TimeSpan(0, periodMinutes, 0));
            else
                _timeDecay = new TimeDecayNull(new TimeSpan(0, periodMinutes, 0));
        }

        public IndicatorWMA(string id, MarketData mktData, int periodMinutes)
            : base(id, new List<MarketData> { mktData })
        {
            _periodSeconds = periodMinutes * 60;
            _periodMilliSeconds = _periodSeconds * 1000m;
            if (Config.Settings.ContainsKey("TIME_DECAY_FACTOR"))
                _timeDecay = new TimeDecayLinear(int.Parse(Config.Settings["TIME_DECAY_FACTOR"]), new TimeSpan(0, periodMinutes, 0));
            else
                _timeDecay = new TimeDecayNull(new TimeSpan(0, periodMinutes, 0));
        }

        public IndicatorWMA(IndicatorWMA indicator)
            : base(indicator.Id, new List<MarketData> { indicator.MarketData })
        {
            _periodSeconds = indicator.Period;
            _periodMilliSeconds = _periodSeconds * 1000m;
            _timeDecay = indicator.TimeDecay;
        }

        public Price Average(DateTime updateTime)
        {
            DateTime startTime;
            startTime = updateTime.AddSeconds(-_periodSeconds);
            if (MarketData.TimeSeries.Count == 0)
                return null;
            //if (MarketData.TimeSeries.StartTime() > startTime)
            //    return null;
            var curAvg = new Price();
            if (!MobileAverage(ref curAvg, startTime, updateTime))
                return null;
            return curAvg;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            Price avgPrice = IndicatorFunc(mktData, updateTime);
            if (avgPrice != null)
            {
                base.OnUpdate(mktData, updateTime, avgPrice);
                Publish(updateTime, avgPrice.MidPrice());
            }
        }

        protected virtual Price IndicatorFunc(MarketData mktData, DateTime updateTime)
        {
            return Average(updateTime);
            /*
            DateTime startTime;
            if (_curAvg == null)
            {
                startTime = updateTime.AddSeconds(-_periodSeconds);
                if (MarketData.TimeSeries.Count == 0)
                    return null;
                if (MarketData.TimeSeries.StartTime() > startTime)
                    return null;                
                _curAvg = new Price();
                Price decrementWeight = new Price();
                if (MobileAverage(ref _curAvg, ref decrementWeight, startTime, updateTime, false, false))
                    return _curAvg;            
            }
            else
            {
                startTime = _curAvgTime;
                var curCumDecayWeight = _timeDecay.CumDecayWeight;
                var incrementAvg = new Price();
                Price incrementWeight = new Price();
                MobileAverage(ref incrementAvg, ref incrementWeight, startTime, updateTime);
                var decrementAvg = new Price();
                Price decrementWeight = new Price();
                MobileAverage(ref decrementAvg, ref decrementWeight, startTime.AddSeconds(-_periodSeconds), updateTime.AddSeconds(-_periodSeconds));
                _curAvg = (_curAvg - decrementAvg * curCumDecayWeight) * _timeDecay.CurDecayWeight + incrementAvg;
            }
            _curAvgTime = updateTime;
            return null;*/
        }

        static IEnumerable<KeyValuePair<T, U>> Zip<T, U>(IEnumerable<T> first, IEnumerable<U> second)
        {
            IEnumerator<T> firstEnumerator = first.GetEnumerator();
            IEnumerator<U> secondEnumerator = second.GetEnumerator();

            while (firstEnumerator.MoveNext())
            {
                if (secondEnumerator.MoveNext())
                {
                    yield return new KeyValuePair<T, U>(firstEnumerator.Current, secondEnumerator.Current);
                }
                else
                {
                    yield return new KeyValuePair<T, U>(firstEnumerator.Current, default(U));
                }
            }
            while (secondEnumerator.MoveNext())
            {
                yield return new KeyValuePair<T, U>(default(T), secondEnumerator.Current);
            }
        }


        protected virtual bool MobileAverage(ref Price curavg, DateTime startTime, DateTime updateTime)
        {       
            bool started = false;
            TimeDecay timeDecay = _timeDecay;
            DateTime originTime = startTime;
            DateTime startTimePrev = DateTime.MinValue;
            IEnumerable<KeyValuePair<DateTime, Price>> generator = MarketData.TimeSeries.ValueGenerator(startTime, updateTime);
            KeyValuePair<DateTime, Price> beginPeriodValue = new KeyValuePair<DateTime, Price>();           
            decimal curTimeDecayWeight = 1m;
            foreach (var endPeriodValue in generator)
            {
                if (!started)
                {
                    started = true;
                    var diffTime = (int)(startTime - endPeriodValue.Key).TotalMilliseconds;
                    if (diffTime < 0)
                        return false;
                    startTimePrev = endPeriodValue.Key;
                    beginPeriodValue = new KeyValuePair<DateTime, Price>(startTime, endPeriodValue.Value);
                    _timeDecay.Update(beginPeriodValue.Key, endPeriodValue.Key, updateTime);
                    continue;
                }
                curTimeDecayWeight = _timeDecay.Update(beginPeriodValue.Key, endPeriodValue.Key, updateTime);

                curavg += beginPeriodValue.Value * curTimeDecayWeight;
                beginPeriodValue = endPeriodValue;
            }
            if (beginPeriodValue.Value != null && beginPeriodValue.Key != updateTime)
                curavg += beginPeriodValue.Value * curTimeDecayWeight;
            return true;

            /*
            bool started = false;
            DateTime startTimePrev = DateTime.MinValue;
            IEnumerable<KeyValuePair<DateTime, Price>> generator = MarketData.TimeSeries.ValueGenerator(startTime, updateTime);
            KeyValuePair<DateTime, Price> beginPeriodValue = new KeyValuePair<DateTime, Price>();
            var tmp = new StreamWriter(File.OpenWrite(string.Format("tmp_{0}_{1}.csv", updateTime.Minute, updateTime.Second)));
            decimal curTimeDecayWeight = 1m;
            if (!incremental)
            {
                foreach (var endPeriodValue in generator)
                {
                    if (!started)
                    {
                        started = true;
                        var diffTime = (int)(startTime - endPeriodValue.Key).TotalMilliseconds;
                        if (!acceptMissingValues && diffTime < 0)
                        {
                            tmp.Close();
                            return false;
                        }
                        startTimePrev = endPeriodValue.Key;
                        beginPeriodValue = new KeyValuePair<DateTime, Price>(startTime, endPeriodValue.Value);
                        _timeDecay.Update(beginPeriodValue.Key, endPeriodValue.Key, updateTime);
                        continue;
                    }
                    curTimeDecayWeight = _timeDecay.Update(beginPeriodValue.Key, endPeriodValue.Key, updateTime);
                    tmp.WriteLine(string.Format("{0},{1}", beginPeriodValue.Value.Bid, curTimeDecayWeight));

                    curavg += beginPeriodValue.Value * curTimeDecayWeight;
                    beginPeriodValue = endPeriodValue;
                }
                if (beginPeriodValue.Value != null && beginPeriodValue.Key != updateTime)
                    curavg += beginPeriodValue.Value * curTimeDecayWeight;
            }
            else
            {
                IEnumerable<KeyValuePair<DateTime, Price>> generatorTimeDecay = _timeDecay.TimeDecaySeries.ValueGenerator(startTime, updateTime);
                foreach (var endPeriodValue in Zip(generator, generatorTimeDecay))
                {
                    if (!started)
                    {
                        started = true;
                        var diffTime = (int)(startTime - endPeriodValue.Key.Key).TotalMilliseconds;
                        if (!acceptMissingValues && diffTime < 0)
                        {
                            tmp.Close();
                            return false;
                        }
                        startTimePrev = endPeriodValue.Key.Key;
                        beginPeriodValue = new KeyValuePair<DateTime, Price>(startTime, endPeriodValue.Key.Value);
                        continue;
                    }
                    else if (endPeriodValue.Key.Key == default(DateTime))
                        break;
                    if (incremental)
                        curTimeDecayWeight = _timeDecay.Update(beginPeriodValue.Key, endPeriodValue.Key.Key, updateTime);
                    else
                        curTimeDecayWeight = endPeriodValue.Value.Value.Bid;
                    tmp.WriteLine(string.Format("{0},{1}", beginPeriodValue.Value.Bid, curTimeDecayWeight));

                    curavg += beginPeriodValue.Value * curTimeDecayWeight;
                    weight += curTimeDecayWeight;
                    beginPeriodValue = endPeriodValue.Key;
                }
                if (beginPeriodValue.Value != null && beginPeriodValue.Key != updateTime)
                {
                    curavg += beginPeriodValue.Value * _timeDecay.Update(beginPeriodValue.Key, updateTime, updateTime);
                    weight += curTimeDecayWeight;
                }   
            }                     
            tmp.Close();
            return true;*/
        } 
    }

    /// <summary>
    /// Volume Weighted Moving Average
    /// Like WMA but adding a weight corresponding to the relative volume increment
    /// <summary>
    public class IndicatorVWMA : IndicatorWMA
    {
        IndicatorVolume _avgVolume;
        protected decimal _prevWeight = 1.0m;
        
        public IndicatorVolume AverageVolume { get { return _avgVolume; } }

        public IndicatorVWMA(MarketData mktData, int periodMinutes, IndicatorVolume cumVol = null)
            : base("VWMA_" + periodMinutes + "_" + mktData.Id, mktData, periodMinutes)
        {
            _avgVolume = cumVol == null ? new IndicatorVolume(mktData, periodMinutes) : cumVol;
        }

        public IndicatorVWMA(string id, MarketData mktData, int periodMinutes, IndicatorVolume cumVol = null)
            : base(id, mktData, periodMinutes)
        {
            _avgVolume = cumVol == null ? new IndicatorVolume(mktData, periodMinutes) : cumVol;
        }

        public IndicatorVWMA(IndicatorVWMA indicator)
            : base(indicator.Id, indicator.MarketData, indicator.Period / 60)
        {
            _avgVolume = indicator.AverageVolume == null ? new IndicatorVolume(indicator.MarketData, indicator.Period / 60) : indicator.AverageVolume;
        }

        public override void Subscribe(Tick updateHandler, Tick tickerHandler)
        {
            _avgVolume.Subscribe(updateHandler, tickerHandler);
            base.Subscribe(updateHandler, tickerHandler);
        }

        public override void Unsubscribe(Tick updateHandler, Tick tickerHandler)
        {
            base.Unsubscribe(updateHandler, tickerHandler);
            _avgVolume.Unsubscribe(updateHandler, tickerHandler);
        }

        /*
        protected override Price IndicatorFunc(MarketData mktData, DateTime updateTime)
        {
            DateTime startTime;
            if (_curAvg == null)
            {
                startTime = updateTime.AddSeconds(-_periodSeconds);
                if (MarketData.TimeSeries.Count == 0)
                    return null;
                if (MarketData.TimeSeries.StartTime() > startTime)
                    return null;
                if (_avgVolume.TimeSeries.Count == 0)
                    return null;
                if (_avgVolume.TimeSeries.StartTime() > updateTime)
                    return null;
                _decrementStartTime = startTime;
                _curAvg = new Price();
                MobileVolumeWeightedPrice(ref _curAvg, startTime, updateTime, updateTime.AddSeconds(-_periodSeconds));
                _prevWeight = _avgVolume.TimeSeries[updateTime].Value.Value.Volume.Value;
                _curAvg /= _prevWeight;
                _prevWeight = 1m / _prevWeight;
            }
            else
            {
                startTime = _curAvgTime;
                var curCumDecayWeight = _timeDecay.CumDecayWeight;
                var decrementUpdateTime = updateTime.AddSeconds(-_periodSeconds);
                var incrementAvg = new Price();
                MobileVolumeWeightedPrice(ref incrementAvg, startTime, updateTime, updateTime.AddSeconds(-_periodSeconds));
                var decrementAvg = new Price();
                MobileVolumeWeightedPrice(ref decrementAvg, _decrementStartTime, decrementUpdateTime);
                var curWeight = 1m / _avgVolume.TimeSeries[updateTime].Value.Value.Volume.Value; // 1m - (decimal)(updateTime - startTime).TotalMilliseconds / (decimal)(_periodSeconds * 1000);
                //_curAvg = _timeDecay.Update(updateTime - startTime, updateTime, _curAvg, incrementAvg, decrementAvg, _prevWeight, curWeight);
                _curAvg = (_curAvg - decrementAvg * curCumDecayWeight * _prevWeight) * _timeDecay.CurDecayWeight + incrementAvg * curWeight;
                _decrementStartTime = decrementUpdateTime;
                _prevWeight = curWeight;
            }
            _curAvgTime = updateTime;
            return _curAvg;
        }*/

        protected override bool MobileAverage(ref Price volPriceOut, DateTime startTime, DateTime updateTime)
        {            
            bool started = false;
            DateTime startTimePrev = DateTime.MinValue;
            IEnumerable<KeyValuePair<DateTime, Price>> generator = MarketData.TimeSeries.ValueGenerator(startTime, updateTime);
            KeyValuePair<DateTime, Price> beginPeriodValue = new KeyValuePair<DateTime, Price>();
            var curVolPrice = 0m;
            var curTimeDecayWeight = 1m;
            foreach (var endPeriodValue in generator)
            {
                if (!started)
                {
                    started = true;
                    var diffTime = (int)(startTime - endPeriodValue.Key).TotalMilliseconds;
                    if (diffTime < 0)
                        return false;                    
                    startTimePrev = endPeriodValue.Key;
                    curVolPrice = endPeriodValue.Value.Bid * Math.Abs(endPeriodValue.Value.Volume.Value); 
                    beginPeriodValue = new KeyValuePair<DateTime, Price>(startTime, endPeriodValue.Value);
                    continue;
                }
                if (!beginPeriodValue.Value.Volume.HasValue)
                    return false;
                if (beginPeriodValue.Value.Volume.Value == 0m)
                    return false;
                curTimeDecayWeight = _timeDecay.Update(beginPeriodValue.Key, endPeriodValue.Key, updateTime);
                volPriceOut += curVolPrice * curTimeDecayWeight;
                curVolPrice = endPeriodValue.Value.Bid * Math.Abs(endPeriodValue.Value.Volume.Value);
                beginPeriodValue = endPeriodValue;
            }
            if (beginPeriodValue.Value != null && beginPeriodValue.Key != updateTime)
                volPriceOut += curVolPrice * curTimeDecayWeight;
            volPriceOut /= _avgVolume.TimeSeries[updateTime].Value.Value.Volume.Value;
            return true;
        } 
    }


    /// <summary>
    /// 
    /// </summary>
    public class IndicatorWMVol : IndicatorWMA
    {
        IndicatorWMA _avg;

        public IndicatorWMVol(MarketData mktData, IndicatorWMA avg)
            : base("WMVol_" + (avg.Period / 60) + "_" + mktData.Id, mktData, avg.Period / 60)
        {
            _avg = avg;
        }

        protected override bool MobileAverage(ref Price curVolAvg, DateTime startTime, DateTime updateTime)
        {
            Price var = new Price();
            if (_avg.TimeSeries.TotalMinutes(updateTime) < (double)_periodSeconds / 60.0)
            {
                curVolAvg = var;
                return false;
            }

            bool started = false;
            var avg = _avg.TimeSeries[updateTime];

            IEnumerable<KeyValuePair<DateTime, Price>> generator = MarketData.TimeSeries.ValueGenerator(startTime, updateTime);
            KeyValuePair<DateTime, Price> beginPeriodValue = new KeyValuePair<DateTime, Price>();
            foreach (var endPeriodValue in generator)
            {
                if (!started)
                {
                    started = true;
                    if (Math.Max(0, (int)(endPeriodValue.Key - startTime).TotalMilliseconds) != 0)
                        return false;
                    beginPeriodValue = new KeyValuePair<DateTime, Price>(startTime, endPeriodValue.Value);
                    continue;
                }
                curVolAvg += (beginPeriodValue.Value - avg.Value.Value).Abs() * _timeDecay.Update(beginPeriodValue.Key, endPeriodValue.Key, updateTime);
                beginPeriodValue = endPeriodValue;
            }
            if (beginPeriodValue.Value == null)
                return false;
            curVolAvg += (beginPeriodValue.Value - avg.Value.Value).Abs() * _timeDecay.Update(beginPeriodValue.Key, updateTime, updateTime);
            return true;
        }
    }

    /// <summary>
    /// Exponential Moving Average
    /// it gives more weight to recent data and as a result is more volatile than SMA
    /// 
    /// EMA = Price(t) * k + EMA(y) * (1 – k)
    /// with:
    ///     t = now, y = previous, N = number of periods in EMA, k = 2/(N+1)
    /// </summary>
    /// TODO: public class IndicatorEMA : IndicatorWMA

}

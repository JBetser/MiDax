using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class IndicatorVolume: Indicator
    {
        protected int _periodSeconds;
        protected decimal _periodMilliSeconds;
        protected Price _curAvg = null;
        protected DateTime _curAvgTime;
        protected DateTime _decrementStartTime;
        protected TimeDecay _timeDecay;
        protected decimal _prevWeight = 1.0m;
        
        public MarketData MarketData { get { return _mktData[0]; } }
        public int Period { get { return _periodSeconds; } }

        public IndicatorVolume(MarketData mktData, int periodMinutes)
            : base("Volume_" + periodMinutes + "_" + mktData.Id, new List<MarketData> { mktData })
        {
            _periodSeconds = periodMinutes * 60;
            _periodMilliSeconds = _periodSeconds * 1000m;
            _timeDecay = new TimeDecayNull(new TimeSpan(0, periodMinutes, 0));
        }

        public IndicatorVolume(string id, MarketData mktData, int periodMinutes)
            : base(id, new List<MarketData> { mktData })
        {
            _periodSeconds = periodMinutes * 60;
            _periodMilliSeconds = _periodSeconds * 1000m;
            _timeDecay = new TimeDecayNull(new TimeSpan(0, periodMinutes, 0));
        }

        public IndicatorVolume(IndicatorVolume indicator)
            : base(indicator.Id, new List<MarketData> { indicator.MarketData })
        {
            _periodSeconds = indicator.Period;
            _periodMilliSeconds = _periodSeconds * 1000m;
            _timeDecay = new TimeDecayNull(new TimeSpan(0, 0, _periodSeconds));
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            /*
            DateTime startTime;
            if (_curCumul == null)
            {
                startTime = updateTime.AddSeconds(-_periodSeconds);
                if (MarketData.TimeSeries.Count == 0)
                    return;
                if (MarketData.TimeSeries.StartTime() > startTime)
                    return;
                _decrementStartTime = startTime;
                _curCumul = new Price();
            }
            else
            {
                startTime = _curCumulTime;
                var decrementUpdateTime = updateTime.AddSeconds(-_periodSeconds);
                var decrementCumul = new Price();
                MobileVolume(ref decrementCumul, _decrementStartTime, decrementUpdateTime);
                _curCumul.Volume -= decrementCumul.Volume.Value;
                _curCumul.Bid = _curCumul.Volume.Value;
                _curCumul.Offer = _curCumul.Volume.Value;
                _decrementStartTime = decrementUpdateTime;
            }
            _incrementCumul = new Price();
            if (!MobileVolume(ref _incrementCumul, startTime, updateTime, updateTime.AddSeconds(-_periodSeconds)))
            {
                _curCumul = null;
                return;
            } */

            /*
            DateTime startTime = _curAvg != null ? _curAvgTime : updateTime.AddSeconds(-_periodSeconds);
            var curCumDecayWeight = _timeDecay.CumDecayWeight;
            var decrementUpdateTime = updateTime.AddSeconds(-_periodSeconds);
            var incrementAvg = new Price();
            if (MobileVolume(ref incrementAvg, startTime, updateTime))
            {
                incrementAvg.Bid = incrementAvg.Volume.Value;
                incrementAvg.Offer = incrementAvg.Volume.Value;
                if (_decrementStartTime == DateTime.MinValue)
                {
                    _decrementStartTime = startTime;
                    _curAvg = incrementAvg;
                    _curAvgTime = updateTime;
                }
                else
                {
                    var decrementAvg = new Price();
                    if (MobileVolume(ref decrementAvg, _decrementStartTime, decrementUpdateTime, false))
                    {
                        decrementAvg.Bid = decrementAvg.Volume.Value;
                        decrementAvg.Offer = decrementAvg.Volume.Value;
                        _decrementStartTime = decrementUpdateTime;
                        _curAvg = (_curAvg - decrementAvg * curCumDecayWeight) * _timeDecay.CurDecayWeight + incrementAvg;
                        _curAvg.Volume = _curAvg.Bid;
                        _curAvgTime = updateTime;
                    }
                }
            }
            if (_curAvg == null)
                return;*/
            
            DateTime startTime;
            if (_curAvg == null)
            {
                startTime = updateTime.AddSeconds(-_periodSeconds);
                if (MarketData.TimeSeries.Count == 0)
                    return;
                //if (MarketData.TimeSeries.StartTime() > startTime)
                //    return;
                _decrementStartTime = startTime;
                _curAvg = new Price();
                MobileVolume(ref _curAvg, startTime, updateTime);
            }
            else
            {
                startTime = _curAvgTime;
                var curCumDecayWeight = _timeDecay.CumDecayWeight;
                var decrementUpdateTime = updateTime.AddSeconds(-_periodSeconds);
                var incrementAvg = new Price();
                MobileVolume(ref incrementAvg, startTime, updateTime);
                incrementAvg.Bid = incrementAvg.Volume.Value;
                incrementAvg.Offer = incrementAvg.Volume.Value;
                var decrementAvg = new Price();
                MobileVolume(ref decrementAvg, _decrementStartTime, decrementUpdateTime);
                decrementAvg.Bid = decrementAvg.Volume.Value;
                decrementAvg.Offer = decrementAvg.Volume.Value;
                var curWeight = 1m; // 1m - (decimal)(updateTime - startTime).TotalMilliseconds / (decimal)(_periodSeconds * 1000);
                _curAvg = (_curAvg - decrementAvg * curCumDecayWeight * _prevWeight) * _timeDecay.CurDecayWeight + incrementAvg * curWeight;
                
                _curAvg.Volume = _curAvg.Bid;
                _decrementStartTime = decrementUpdateTime;
                _prevWeight = curWeight;
            }                       
            
            _curAvg.Bid = _curAvg.Volume.Value;
            _curAvg.Offer = _curAvg.Volume.Value;
            _curAvgTime = updateTime;
            base.OnUpdate(mktData, updateTime, new Price(_curAvg));
            Publish(updateTime, _curAvg.Volume.Value);
        }

        bool MobileVolume(ref Price curCumul, DateTime startTime, DateTime updateTime)
        {
            bool started = false;
            DateTime startTimePrev = DateTime.MinValue;
            IEnumerable<KeyValuePair<DateTime, Price>> generator = MarketData.TimeSeries.ValueGenerator(startTime, updateTime);
            //IEnumerable<KeyValuePair<DateTime, Price>> generatorTimeDecay = _timeDecay.TimeDecaySeries.ValueGenerator(startTime, updateTime);
            KeyValuePair<DateTime, Price> beginPeriodValue = new KeyValuePair<DateTime, Price>();
            //DateTime originTime = origin.HasValue ? origin.Value : startTime;
            var curVol = 0m;
            StreamWriter tmp = null;
            //if (updateTime >= new DateTime(updateTime.Year, updateTime.Month, updateTime.Day, 12, 41, 37))
            //    tmp = new StreamWriter(File.OpenWrite("tmpV.csv")); 
            foreach (var endPeriodValue in generator)
            {
                if (!started)
                {
                    started = true;
                    var diffTime = (int)(startTime - endPeriodValue.Key).TotalMilliseconds;
                    if (diffTime < 0)
                        return false;
                    startTimePrev = endPeriodValue.Key;
                    curVol = Math.Abs(endPeriodValue.Value.Volume.Value);
                    beginPeriodValue = new KeyValuePair<DateTime, Price>(startTime, endPeriodValue.Value);
                    continue;
                }
                decimal curTimeDecayWeight = _timeDecay.Update(beginPeriodValue.Key, endPeriodValue.Key, updateTime);
                //else
                //    curTimeDecayWeight = generatorTimeDecay.Take(1).First().Value.Bid;
                if (tmp != null)
                    tmp.WriteLine(string.Format("{0},{1}", curVol, curTimeDecayWeight));

                curCumul.Volume += curVol * curTimeDecayWeight;
                curVol = Math.Abs(endPeriodValue.Value.Volume.Value);
                beginPeriodValue = endPeriodValue;
            }
            if (beginPeriodValue.Value != null)
            {
                decimal lastTimeDecayWeightValue = _timeDecay.Update(beginPeriodValue.Key, updateTime, updateTime);
                //else
                //    lastTimeDecayWeightValue = generatorTimeDecay.Take(1).First().Value.Bid;
                if (tmp != null)
                    tmp.WriteLine(string.Format("{0},{1}", curVol, lastTimeDecayWeightValue));
                curCumul.Volume += curVol * lastTimeDecayWeightValue;
            }
            if (tmp != null) 
                tmp.Close();
            return true;
        } 
    }
}

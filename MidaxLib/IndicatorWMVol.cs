using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    /// <summary>
    /// 
    /// </summary>
    public class IndicatorWMVol : IndicatorEMA
    {
        int _nbPeriods;
        public int NbPeriods { get { return _nbPeriods; } }
        public List<Candle> Candles { get { return _candles; } }
        DateTime _nextTime = DateTime.MinValue;
        List<Candle> _candles = new List<Candle>();
        Candle _curCandle = null;

        protected Price _curValue;
        protected decimal? _prevWMVol;
        DateTime _startVolTime;
        IndicatorEMA _ema;

        public IndicatorWMVol(MarketData mktData, IndicatorEMA ema, int subPeriodSeconds, int nbPeriods)
            : base("WMVol_" + ema.Period / 60 + "_" + mktData.Id, mktData, ema.Period / 60)
        {
            _ema = ema;
            _subPeriodSeconds = subPeriodSeconds;
            _nbPeriods = nbPeriods;
        }

        protected override Price IndicatorFunc(MarketData mktData, DateTime updateTime, Price value)
        {
            var wmvol = CalcWMVol(mktData, updateTime, value);

            if (updateTime >= _nextTime)
            {
                if (_nextTime == DateTime.MinValue)
                    _nextTime = updateTime.AddSeconds(_subPeriodSeconds);
                else
                    _nextTime = _nextTime.AddSeconds(_subPeriodSeconds);
                DateTime rsiTime = _nextTime.AddSeconds(-_subPeriodSeconds);
                if (mktData.TimeSeries.StartTime() > rsiTime)
                    return null;
                if (_candles.Count == _nbPeriods)
                    _candles.RemoveAt(0);
                _curCandle = new Candle(wmvol.Bid, wmvol.Bid, updateTime, updateTime);
                _candles.Add(_curCandle);
            }
            else
                _curCandle = _candles.Last();

            if (wmvol.Bid > _curCandle.Max)
            {
                _curCandle.Max = wmvol.Bid;
                _curCandle.MaxTime = updateTime;
            }
            else if (wmvol.Bid < _curCandle.Min)
            {
                _curCandle.Min = wmvol.Bid;
                _curCandle.MinTime = updateTime;
            }

            return wmvol;
        }

        public Price CalcWMVol(MarketData mktData, DateTime updateTime, Price value)
        {
            Price avg = base.IndicatorFunc(mktData, updateTime, value);
            var curWMVol = 0m;
            if (_prevWMVol.HasValue)
            {
                var curPeriod = (decimal)(updateTime - _startVolTime).TotalMilliseconds;
                if (curPeriod > _periodMilliSeconds)
                    curPeriod = _periodMilliSeconds;
                var timeDecay = _timeDecayWeight * (decimal)(updateTime - _startTime).TotalSeconds;
                var k = (curPeriod + timeDecay) / _periodMilliSeconds;
                curWMVol = Math.Abs(value.Mid() - _ema.TimeSeries.Last().Mid()) * k + _prevWMVol.Value * (1m - k);
            }
            else
            {
                curWMVol = 0m;
            }
            _prevWMVol = curWMVol;
            _startVolTime = updateTime;
            return new Price(curWMVol);
        }

        public decimal Max()
        {
            decimal max = decimal.MinValue;
            for (int idxCandle = _candles.Count - _nbPeriods; idxCandle < _candles.Count; idxCandle++)
            {
                if (_candles[idxCandle].Max > max)
                    max = _candles[idxCandle].Max;
            }
            return max;
        }

        public decimal Min()
        {
            decimal min = decimal.MaxValue;
            for (int idxCandle = _candles.Count - _nbPeriods; idxCandle < _candles.Count; idxCandle++)
            {
                if (_candles[idxCandle].Min < min)
                    min = _candles[idxCandle].Min;
            }
            return min;
        }
    }
}

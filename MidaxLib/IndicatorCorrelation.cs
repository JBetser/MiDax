using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    class IndicatorCorrelation : Indicator
    {
        protected int _periodSeconds;
        protected decimal _periodMilliSeconds;
        protected IndicatorWMA _wma = null;
        protected IndicatorWMA _wmaRef = null;
        protected decimal _timeDecayWeight = 0m;
        
        public MarketData MarketData { get { return _mktData[0]; } }
        public MarketData MarketDataRef { get { return _mktData[1]; } }
        public IndicatorWMA WMA { get { return _wma; } }
        public IndicatorWMA WMARef { get { return _wmaRef; } }
        public int Period { get { return _periodSeconds; } }
        protected decimal? _prevCorrel;
        protected double? _prevVar;
        protected double? _prevVarRef;
        protected Price _curValue;
        protected Price _curVarValue;
        protected Price _curVarRefValue;
        protected DateTime _startTime;
        protected double _var;
        protected double _varRef;

        public IndicatorCorrelation(IndicatorWMA wma, IndicatorWMA wmaRef)
            : base("Cor_" + (wma.Period / 60) + "_" + wmaRef.MarketData.Id + "_" + wma.MarketData.Id, new List<MarketData> { wma.MarketData })
        {
            _wma = wma;
            _wmaRef = wmaRef;
            _periodSeconds = wma.Period;
            _periodMilliSeconds = _periodSeconds * 1000m;
            if (Config.Settings.ContainsKey("TIME_DECAY_FACTOR"))
                _timeDecayWeight = decimal.Parse(Config.Settings["TIME_DECAY_FACTOR"]) / 10.0m;
        }
        
        public IndicatorCorrelation(string id, IndicatorWMA wma, IndicatorWMA wmaRef, int periodMinutes)
            : base(id, new List<MarketData> { wma.MarketData })
        {
            _wma = wma;
            _wmaRef = wmaRef;
            _periodSeconds = periodMinutes * 60;
            _periodMilliSeconds = _periodSeconds * 1000m;
            if (Config.Settings.ContainsKey("TIME_DECAY_FACTOR"))
                _timeDecayWeight = decimal.Parse(Config.Settings["TIME_DECAY_FACTOR"]) / 10.0m;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            if (mktData.TimeSeries.TotalMinutes(updateTime) > (double)_periodSeconds / 60.0)
            {
                Price avgPrice = IndicatorFunc(mktData, updateTime, value);
                if (avgPrice != null)
                {
                    base.OnUpdate(mktData, updateTime, avgPrice);
                    Publish(updateTime, avgPrice.MidPrice());
                }
            }
        }

        protected Price IndicatorFunc(MarketData mktData, DateTime updateTime, Price value)
        {
            if (_wma.TimeSeries.Count == 0 || _wmaRef.TimeSeries.Count == 0)
                return null;            
            var curCorrel = 0m;
            var curValue = 0m;
            var curVar = 0.0;
            var curVarRef = 0.0;
            Price priceCurCorrel = null;
            if (_prevCorrel.HasValue)
            {
                if (_wma.TimeSeries.StartTime() > _startTime)
                    return null;
                if (_wmaRef.TimeSeries.StartTime() > _startTime)
                    return null;
                if (_wmaRef.TimeSeries.EndTime() != _wma.TimeSeries.EndTime())
                    return null;
                var curPeriod = (decimal)(updateTime - _startTime).TotalMilliseconds;
                if (curPeriod > _periodMilliSeconds)
                    curPeriod = _periodMilliSeconds;
                var timeDecay = _timeDecayWeight * (decimal)(updateTime - _startTime).TotalSeconds;
                var k = (curPeriod + timeDecay) / _periodMilliSeconds;
                var avg = _wma.TimeSeries.Last();
                var avgRef = _wmaRef.TimeSeries.Last();
                var curVarValue = Math.Pow((double)(value.Bid - avg.Bid), 2);
                var curVarRefValue = Math.Pow((double)(_wmaRef.MarketData.TimeSeries.Last().Bid - avgRef.Bid), 2);
                curVar = (double)(_curVarValue.Bid * k) + _prevVar.Value * (1.0 - (double)k);
                curVarRef = (double)(_curVarRefValue.Bid * k) + _prevVarRef.Value * (1.0 - (double)k);
                if (curVar * curVarRef == 0)
                {
                    if (Math.Abs(curVarValue * curVarRefValue - curVar * curVarRef) < 0.01)
                        curValue = 1m;
                    else
                        curValue = 0m;
                }
                else
                    curValue = (decimal)(Math.Sqrt((curVarValue * curVarRefValue) / (curVar * curVarRef)));                
                curCorrel = _curValue.Bid * k + _prevCorrel.Value * (1m - k);
                _curVarValue = new Price((decimal)curVarValue);
                _curVarRefValue = new Price((decimal)curVarRefValue);
                priceCurCorrel = new Price(curCorrel);
            }
            else
            {
                _curVarValue = new Price(0m);
                _curVarRefValue = new Price(0m);
                curValue = 0m;
                curCorrel = 0m; 
            }
            _curValue = new Price(curValue);
            _prevCorrel = curCorrel;
            _prevVar = curVar;
            _prevVarRef = curVarRef; 
            _startTime = updateTime;
            return priceCurCorrel;
        }     
    }
}

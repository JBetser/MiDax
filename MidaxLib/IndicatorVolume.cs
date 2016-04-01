using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class IndicatorCumulVolume: Indicator
    {
        protected int _periodSeconds;
        protected decimal _periodMilliSeconds;
        protected Price _curCumul = null;
        protected DateTime _curCumulTime;
        protected DateTime _decrementStartTime;
        protected Price _incrementCumul = null;

        public MarketData MarketData { get { return _mktData[0]; } }
        public int Period { get { return _periodSeconds; } }

        public IndicatorCumulVolume(MarketData mktData, int periodMinutes)
            : base("CumVol_" + periodMinutes + "_" + mktData.Id, new List<MarketData> { mktData })
        {
            _periodSeconds = periodMinutes * 60;
            _periodMilliSeconds = _periodSeconds * 1000m;
        }

        public IndicatorCumulVolume(string id, MarketData mktData, int periodMinutes)
            : base(id, new List<MarketData> { mktData })
        {
            _periodSeconds = periodMinutes * 60;
            _periodMilliSeconds = _periodSeconds * 1000m;
        }

        public IndicatorCumulVolume(IndicatorCumulVolume indicator)
            : base(indicator.Id, new List<MarketData> { indicator.MarketData })
        {
            _periodSeconds = indicator.Period;
            _periodMilliSeconds = _periodSeconds * 1000m;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            DateTime startTime;
            if (_curCumul == null)
            {
                startTime = updateTime.AddSeconds(-_periodSeconds);
                _decrementStartTime = startTime;
                _curCumul = new Price();
            }
            else
            {
                startTime = _curCumulTime;
                var decrementUpdateTime = updateTime.AddSeconds(-_periodSeconds);
                var decrementCumul = new Price();
                MobileCumulatedVolume(ref decrementCumul, _decrementStartTime, decrementUpdateTime);
                _curCumul -= decrementCumul;
                _decrementStartTime = decrementUpdateTime;
            }
            _incrementCumul = new Price();
            MobileCumulatedVolume(ref _incrementCumul, startTime, updateTime);
            _curCumul += _incrementCumul;
            _curCumulTime = updateTime;
            base.OnUpdate(mktData, updateTime, _curCumul);
            Publish(updateTime, _curCumul.Volume.Value);
        }

        bool MobileCumulatedVolume(ref Price curCumul, DateTime startTime, DateTime updateTime)
        {
            bool started = false;
            DateTime startTimePrev = DateTime.MinValue;
            IEnumerable<KeyValuePair<DateTime, Price>> generator = MarketData.TimeSeries.ValueGenerator(startTime, updateTime);
            KeyValuePair<DateTime, Price> beginPeriodValue = new KeyValuePair<DateTime, Price>();
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
                    continue;
                }
                curCumul += beginPeriodValue.Value.Volume.Value;
                beginPeriodValue = endPeriodValue;
            }
            curCumul += beginPeriodValue.Value.Volume.Value;
            return true;
        } 
    }
}

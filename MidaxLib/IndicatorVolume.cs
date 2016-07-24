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
        
        public MarketData MarketData { get { return _mktData[0]; } }
        public int Period { get { return _periodSeconds; } }
        public Price Average { get { return _curAvg; } }

        public IndicatorVolume(MarketData mktData, int periodMinutes)
            : base("Volume_" + periodMinutes + "_" + mktData.Id, new List<MarketData> { mktData })
        {
            _periodSeconds = periodMinutes * 60;
            _periodMilliSeconds = _periodSeconds * 1000m;
        }

        public IndicatorVolume(string id, MarketData mktData, int periodMinutes)
            : base(id, new List<MarketData> { mktData })
        {
            _periodSeconds = periodMinutes * 60;
            _periodMilliSeconds = _periodSeconds * 1000m;
        }

        public IndicatorVolume(IndicatorVolume indicator)
            : base(indicator.Id, new List<MarketData> { indicator.MarketData })
        {
            _periodSeconds = indicator.Period;
            _periodMilliSeconds = _periodSeconds * 1000m;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {            
            DateTime startTime;
            if (_curAvg == null)
            {
                startTime = updateTime.AddSeconds(-_periodSeconds);
                if (MarketData.TimeSeries.Count == 0)
                    return;
                if (MarketData.TimeSeries.StartTime() > startTime)
                    return;
                _decrementStartTime = startTime;
                var curAvg = new Price();
                MobileVolume(ref curAvg, startTime, updateTime);
                if (curAvg == null)
                    return;
                _curAvg = curAvg;
            }
            else
            {
                startTime = _curAvgTime;
                var decrementUpdateTime = updateTime.AddSeconds(-_periodSeconds);

                var decrementAvg = new Price(0m,0m,0m);
                MobileVolume(ref decrementAvg, _decrementStartTime, decrementUpdateTime);
                decrementAvg.Bid = decrementAvg.Volume.Value;
                decrementAvg.Offer = decrementAvg.Volume.Value;
                _curAvg = _curAvg - decrementAvg + Math.Abs(value.Volume.Value) / (decimal)_periodSeconds;
                
                _curAvg.Volume = _curAvg.Bid;
                _decrementStartTime = decrementUpdateTime;
            }                       
            
            _curAvg.Bid = _curAvg.Volume.Value;
            _curAvg.Offer = _curAvg.Volume.Value;
            _curAvgTime = updateTime;
            Publish(updateTime, _curAvg.Volume.Value);
        }

        void MobileVolume(ref Price curCumul, DateTime startTime, DateTime updateTime)
        {            
            IEnumerable<KeyValuePair<DateTime, Price>> generator = MarketData.TimeSeries.ValueGenerator(startTime, updateTime, true);
            foreach (var endPeriodValue in generator)
                curCumul.Volume += Math.Abs(endPeriodValue.Value.Volume.Value);
            curCumul.Volume /= (decimal)_periodSeconds;
        } 
    }
}

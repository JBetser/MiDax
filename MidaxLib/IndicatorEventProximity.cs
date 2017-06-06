using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class IndicatorEventProximity : Indicator
    {
        protected decimal _periodSeconds;
        Calendar _calendar;

        public MarketData MarketData { get { return _mktData[0]; } }

        public IndicatorEventProximity(MarketData mktData, int signalPeriodMinutes)
            : base("EvtProx_" + signalPeriodMinutes + "_" + mktData.Id, new List<MarketData> { mktData })
        {
            _periodSeconds = signalPeriodMinutes * 60m / 3m;
        }

        public IndicatorEventProximity(string id, MarketData mktData, int periodMinutes)
            : base(id, new List<MarketData> { mktData })
        {
            _periodSeconds = periodMinutes * 60m / 3m;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            if (_calendar == null)
                _calendar = new Calendar(updateTime);
            decimal proximity = 100m - (Math.Min((decimal)_calendar.SecondsToEvent(updateTime), _periodSeconds) * 100m / _periodSeconds);
            Publish(updateTime, proximity);
        }
    }
}

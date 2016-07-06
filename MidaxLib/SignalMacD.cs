using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class SignalMacD : Signal
    {
        protected IndicatorWMA _low = null;
        protected IndicatorWMA _high = null;
        protected SIGNAL_CODE _trendAssumption = SIGNAL_CODE.UNKNOWN;

        public IndicatorWMA IndicatorLow { get { return _low; } }
        public IndicatorWMA IndicatorHigh { get { return _high; } }

        public SignalMacD(MarketData asset, int lowPeriod, int highPeriod, IndicatorWMA low = null, IndicatorWMA high = null, MarketData tradingAsset = null)
            : base("MacD_" + lowPeriod + "_" + highPeriod + "_" + asset.Id, asset, tradingAsset)
        {
            if (Config.Settings.ContainsKey("ASSUMPTION_TREND"))
                _trendAssumption = Config.Settings["ASSUMPTION_TREND"] == "BULL" ? SIGNAL_CODE.BUY : SIGNAL_CODE.SELL;
            _low = low == null ? new IndicatorEMA(asset, lowPeriod) : new IndicatorEMA(low);
            if (low != null)
                _low.PublishingEnabled = false;
            _high = high == null ? new IndicatorEMA(asset, highPeriod) : new IndicatorEMA(high);
            if (high != null)
                _high.PublishingEnabled = false;
            _mktIndicator.Add(_low);
            _mktIndicator.Add(_high);
        }

        public SignalMacD(string id, MarketData asset, int lowPeriod, int highPeriod, IndicatorWMA low = null, IndicatorWMA high = null, MarketData tradingAsset = null)
            : this(asset, lowPeriod, highPeriod, low, high, tradingAsset)
        {
            _id = id;
        }

        protected override bool Process(MarketData indicator, DateTime updateTime, Price value, ref Signal.Tick tradingOrder)
        {
            KeyValuePair<DateTime, Price>? timeValueLow = _low.TimeSeries[updateTime];
            KeyValuePair<DateTime, Price>? timeValueHigh = _high.TimeSeries[updateTime];
            if (timeValueLow == null || timeValueHigh == null)
                return false;
            if (_high.TimeSeries.TotalMinutes(updateTime) < _high.Period / 60)
                return false;
            Price lowWMA = timeValueLow.Value.Value;
            Price highWMA = timeValueHigh.Value.Value;
            var signalValue = lowWMA - highWMA;
            SIGNAL_CODE oldSignalCode = _signalCode;            
            if (signalValue.Offer > 0)
            {
                tradingOrder = _onBuy;
                _signalCode = SIGNAL_CODE.BUY;
            }
            else if (signalValue.Bid < 0)
            {
                tradingOrder = _onSell;
                _signalCode = SIGNAL_CODE.SELL;
            }
            else
                return false;
            return oldSignalCode != SIGNAL_CODE.UNKNOWN && oldSignalCode != SIGNAL_CODE.HOLD && oldSignalCode != _signalCode;
        }
    }

    public class SignalMacDV : SignalMacD
    {
        public SignalMacDV(MarketData asset, int lowPeriod, int highPeriod, IndicatorVEMA low = null, IndicatorVEMA high = null, MarketData tradingAsset = null)
            : base("MacDV_" + lowPeriod + "_" + highPeriod + "_" + asset.Id, asset, lowPeriod, highPeriod, low, high, tradingAsset)
        {
            _low = low == null ? new IndicatorVEMA(asset, lowPeriod) : new IndicatorVEMA(low);
            if (low != null)
                _low.PublishingEnabled = false;
            _high = high == null ? new IndicatorVEMA(asset, highPeriod) : new IndicatorVEMA(high);
            if (high != null)
                _high.PublishingEnabled = false;
            _mktIndicator = new List<Indicator>();
            _mktIndicator.Add(_low);
            _mktIndicator.Add(_high);
        }

        public SignalMacDV(string id, MarketData asset, int lowPeriod, int highPeriod, IndicatorVEMA low = null, IndicatorVEMA high = null, MarketData tradingAsset = null)
            : this(asset, lowPeriod, highPeriod, low, high, tradingAsset)
        {
            _id = id;
        }
    }
}

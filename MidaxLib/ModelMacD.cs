using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class ModelMacD : Model
    {
        protected MarketData _index = null;
        protected MarketData _tradingIndex = null;
        protected SignalMacD _macD_low = null;
        protected SignalMacD _macD_high = null;
        protected SIGNAL_CODE _trendAssumption = SIGNAL_CODE.UNKNOWN;
        protected int _tradeSize = 0;
        protected int _lowPeriod = 0;
        protected int _midPeriod = 0;
        protected int _highPeriod = 0;

        public MarketData Index { get { return _index; } }
        public MarketData TradingIndex { get { return _tradingIndex; } }
        public SignalMacD SignalLow { get { return _macD_low; } }
        public SignalMacD SignalHigh { get { return _macD_high; } }

        public int LowPeriod { get { return _lowPeriod; } }
        public int MidPeriod { get { return _midPeriod; } }
        public int HighPeriod { get { return _highPeriod; } }

        public ModelMacD(MarketData index, int lowPeriod = 2, int midPeriod = 10, int highPeriod = 60, MarketData tradingIndex = null)
        {
            if (Config.Settings.ContainsKey("ASSUMPTION_TREND"))
                _trendAssumption = Config.Settings["ASSUMPTION_TREND"] == "BULL" ? SIGNAL_CODE.BUY : SIGNAL_CODE.SELL;
            List<MarketData> mktData = new List<MarketData>();
            mktData.Add(index);
            _mktData = mktData;
            _index = index;
            _tradingIndex = tradingIndex;
            _lowPeriod = lowPeriod;
            _midPeriod = midPeriod;
            _highPeriod = highPeriod;
        }

        protected override void Init()
        {
            base.Init();
            _macD_low = new SignalMacD(_index, _lowPeriod, _midPeriod, null, null, _tradingIndex);
            _macD_high = new SignalMacD(_index, _midPeriod, _highPeriod, _macD_low.IndicatorHigh, null, _tradingIndex);
            _mktSignals.Add(_macD_low);
            _mktSignals.Add(_macD_high);
        }

        protected override bool Buy(Signal signal, DateTime time, Price stockValue)
        {
            if (_ptf.GetPosition(_tradingIndex.Id).Quantity < 0)
            {
                if (time >= _closingTime)
                {
                    signal.Trade.Price = stockValue.Offer;
                    _ptf.ClosePosition(signal.Trade, time, null, null, signal);
                    string closeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                    Log.Instance.WriteEntry(time + closeRef + " Signal close " + signal.Id + ": BUY " + signal.TradingAsset.Id + " " + stockValue.Bid, EventLogEntryType.Information);
                    return false;
                }
                else
                {
                    if (_trendAssumption != SIGNAL_CODE.SELL)
                        signal.Trade.Size = _tradeSize * 2;
                    _ptf.BookTrade(signal.Trade);
                }
            }
            else if (_trendAssumption != SIGNAL_CODE.SELL && _ptf.GetPosition(_tradingIndex.Id).Quantity == 0)
            {
                if (time <= _closingTime)
                {
                    _tradeSize = signal.Trade.Size;
                    _ptf.BookTrade(signal.Trade);
                }
                else
                    return false;
            }
            else
                return false;
            string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
            Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": BUY " + signal.TradingAsset.Id + " " + stockValue.Bid, EventLogEntryType.Information);
            return true;
        }

        protected override bool Sell(Signal signal, DateTime time, Price stockValue)
        {
            if (_ptf.GetPosition(_tradingIndex.Id).Quantity > 0)
            {
                if (time >= _closingTime)
                {
                    signal.Trade.Price = stockValue.Bid;
                    _ptf.ClosePosition(signal.Trade, time, null, null, signal);
                    string closeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                    Log.Instance.WriteEntry(time + closeRef + " Signal close " + signal.Id + ": SELL " + signal.TradingAsset.Id + " " + stockValue.Offer, EventLogEntryType.Information);
                    return false;
                }
                else
                {
                    if (_trendAssumption != SIGNAL_CODE.BUY)
                        signal.Trade.Size = _tradeSize * 2;
                    _ptf.BookTrade(signal.Trade);
                }
            }
            else if (_trendAssumption != SIGNAL_CODE.BUY && _ptf.GetPosition(_tradingIndex.Id).Quantity == 0)
            {
                if (time <= _closingTime)
                {
                    _tradeSize = signal.Trade.Size;
                    _ptf.BookTrade(signal.Trade);
                }
                else
                    return false;
            }
            else
                return false;
            string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
            Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": SELL " + signal.TradingAsset.Id + " " + stockValue.Offer, EventLogEntryType.Information);
            return true;
        }
    }

    public class ModelMacDV : ModelMacD
    {
        public ModelMacDV(MarketData index, int lowPeriod = 2, int midPeriod = 10, int highPeriod = 60, MarketData tradingIndex = null) :
            base(index, lowPeriod, midPeriod, highPeriod, tradingIndex)
        {
        }

        protected override void Init()
        {
            base.Init();
            _macD_low = new SignalMacDV(_index, _lowPeriod, _midPeriod, null, null, _tradingIndex);
            _macD_high = new SignalMacDV(_index, _midPeriod, _highPeriod, (IndicatorVEMA)_macD_low.IndicatorHigh, null, _tradingIndex);
            _mktSignals = new List<Signal>();
            _mktSignals.Add(_macD_low);
            _mktSignals.Add(_macD_high);            
        }
    }
}

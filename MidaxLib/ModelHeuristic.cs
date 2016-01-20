using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class ModelMacDCascade : Model
    {
        protected MarketData _daxIndex = null;

        public ModelMacDCascade(ModelMacD macD)
        {
            _daxIndex = macD.Index;
            _mktSignals.Add(new SignalMacDCascade(_daxIndex, macD.SignalHigh.IndicatorLow.Period / 60, macD.SignalHigh.IndicatorHigh.Period / 60, 0.5m, macD.SignalHigh.IndicatorLow, macD.SignalHigh.IndicatorHigh));
            _mktSignals.Add(new SignalMacDCascade(_daxIndex, macD.SignalHigh.IndicatorLow.Period / 60, macD.SignalHigh.IndicatorHigh.Period / 60, 1.0m, macD.SignalHigh.IndicatorLow, macD.SignalHigh.IndicatorHigh));
            _mktSignals.Add(new SignalMacDCascade(_daxIndex, macD.SignalHigh.IndicatorLow.Period / 60, macD.SignalHigh.IndicatorHigh.Period / 60, 2.0m, macD.SignalHigh.IndicatorLow, macD.SignalHigh.IndicatorHigh));
        }

        protected override void Buy(Signal signal, DateTime time, Price value)
        {
            if (_ptf.GetPosition(_daxIndex.Id).Value < 0)
            {
                signal.Trade.Price = value.Offer;
                _ptf.ClosePosition(signal.Trade, time);
                string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": BUY " + signal.MarketData.Id + " " + value.Offer, EventLogEntryType.Information);
            }
        }

        protected override void Sell(Signal signal, DateTime time, Price value)
        {
            if (_ptf.GetPosition(_daxIndex.Id).Value > 0)
            {
                _ptf.BookTrade(signal.Trade);
                Log.Instance.WriteEntry(time + " Signal " + signal.Id + ": Unexpected positive position. SELL " + signal.Trade.Id + " " + value.Offer, EventLogEntryType.Error);
            }
            else if (_ptf.GetPosition(_daxIndex.Id).Value == 0)
            {
                if (time <= _closingTime)
                {
                    _ptf.BookTrade(signal.Trade);
                    string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                    Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": SELL " + signal.MarketData.Id + " " + value.Bid, EventLogEntryType.Information);
                }
            }
        }
    }

    public class ModelMole : Model
    {
        protected MarketData _daxIndex = null;
        protected SignalMacD _signal = null;

        public ModelMole(ModelMacD macD)
        {
            _daxIndex = macD.Index;
            List<MarketData> mktData = new List<MarketData>();
            mktData.Add(_daxIndex);
            _mktData = mktData;
            _signal = new SignalMole(_daxIndex, macD.SignalLow.IndicatorLow.Period / 60, macD.SignalLow.IndicatorHigh.Period / 60);
            _mktSignals.Add(_signal);
        }

        protected override void Buy(Signal signal, DateTime time, Price value)
        {
            if (_ptf.GetPosition(_daxIndex.Id).Value < 0)
            {
                signal.Trade.Price = value.Offer;
                _ptf.ClosePosition(signal.Trade, time);
                string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                Log.Instance.WriteEntry(time + tradeRef + " Mole MacD Signal " + signal.Id + ": BUY " + signal.MarketData.Id + " " + value.Offer, EventLogEntryType.Information);
            }
        }

        protected override void Sell(Signal signal, DateTime time, Price value)
        {
            if (_ptf.GetPosition(_daxIndex.Id).Value > 0)
            {
                _ptf.BookTrade(signal.Trade);
                Log.Instance.WriteEntry(time + " Mole MacD Signal " + signal.Id + ": Unexpected positive position. SELL " + signal.Trade.Id + " " + value.Offer, EventLogEntryType.Error);
            }
            else if (_ptf.GetPosition(_daxIndex.Id).Value == 0)
            {
                if (time <= _closingTime)
                {
                    _ptf.BookTrade(signal.Trade);
                    string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                    Log.Instance.WriteEntry(time + tradeRef + " Mole MacD Signal " + signal.Id + ": SELL " + signal.MarketData.Id + " " + value.Bid, EventLogEntryType.Information);
                }
            }
        }  
    }
}

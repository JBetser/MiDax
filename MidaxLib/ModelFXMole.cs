using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class ModelFXMole : Model
    {
        List<MarketData> _fx = null;
        decimal _volCoeff;
        SignalFXMole _fxMole = null;
        ModelMacD _macD;
        IndicatorRSI _rsi;
        IndicatorRSI[] _mapRsiRef = null;

        public ModelFXMole(List<MarketData> fx, ModelMacD macD, decimal volCoeff)
        {
            _fx = fx;
            _macD = macD;
            _volCoeff = volCoeff;
            _rsi = new IndicatorRSI(_fx[0], 3, 45);
            _mapRsiRef = new IndicatorRSI[_fx.Count - 1];
            for (int idxFX = 1; idxFX < _fx.Count; idxFX++)
                _mapRsiRef[idxFX - 1] = new IndicatorRSI(_fx[idxFX], 1, 14);
        }

        public override void Init()
        {
            base.Init();
            List<MarketData> mktData = new List<MarketData>();
            mktData.AddRange(_fx);
            _mktIndices.AddRange(_fx);
            _mktData = mktData;

            foreach (var rsiRef in _mapRsiRef)
                _mktIndicators.Add(rsiRef);

            _fxMole = new SignalFXMole(_fx[0], _rsi, _macD, _mapRsiRef, _volCoeff);
            _mktSignals.Add(_fxMole);
        }

        protected override void Reset(DateTime cancelTime, decimal stockValue, bool openPosition)
        {
            if (openPosition)
                Log.Instance.WriteEntry(cancelTime + " ModelFXMole could not book a new trade, stock value: " + stockValue, EventLogEntryType.Warning);
            else
                Log.Instance.WriteEntry(cancelTime + " ModelFXMole could not close a trade, stock value: " + stockValue, EventLogEntryType.Error);
            var pos = _ptf.GetPosition(_fxMole.TradingAsset.Id);
            if (pos.Quantity != 0)
                _ptf.ClosePosition(pos, cancelTime, null, null, stockValue);
            _fxMole.Reset();
        }

        protected override bool Buy(Signal signal, DateTime time, Price stockValue)
        {
            if (!_ptf.BookTrade(signal.Trade, signal.TradingAsset.Name))
            {
                Reset(time, stockValue.Mid(), true);
                return false;
            }
            string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
            Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": BUY " + signal.TradingAsset.Id + " " + stockValue.Offer + " spread: " + (stockValue.Offer - stockValue.Bid), EventLogEntryType.Information);
            return true;
        }

        protected override bool Sell(Signal signal, DateTime time, Price stockValue)
        {
            if (!_ptf.BookTrade(signal.Trade, signal.TradingAsset.Name))
            {
                Reset(time, stockValue.Mid(), true);
                return false;
            }
            string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
            Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": SELL " + signal.TradingAsset.Id + " " + stockValue.Bid + " spread: " + (stockValue.Offer - stockValue.Bid), EventLogEntryType.Information);
            return true;
        }
    }
}

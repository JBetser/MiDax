﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class ModelANN : Model
    {
        protected MarketData _index = null;
        protected List<MarketData> _daxStocks = null;
        protected List<MarketData> _otherIndices = null;
        protected MarketData _vix = null;
        protected IndicatorEMA _wma_verylow = null;
        protected IndicatorEMA _wma_low = null;
        protected IndicatorEMA _wma_mid = null;
        protected IndicatorEMA _wma_high = null;
        protected SignalANN _ann = null;
        protected List<decimal> _annWeights = null;
        ModelMacD _macD;
        string _annId;

        public ModelANN(string annid, ModelMacD macD, List<MarketData> daxStocks, MarketData vix, List<MarketData> otherIndices)
        {
            _annId = annid;
            _macD = macD;
            _index = macD.Index;
            _daxStocks = daxStocks;
            _otherIndices = otherIndices;
            _vix = vix;
            if (_vix != null)
                _mktIndices.Add(_vix);           
        }

        protected override void Reset(DateTime cancelTime, decimal stockValue, bool openPosition)
        {
        }

        public override void Init()
        {
            base.Init();
            _wma_verylow = new IndicatorEMA(_macD.Index, 2);
            _wma_low = new IndicatorEMA(_macD.SignalLow.IndicatorLow);
            _wma_low.PublishingEnabled = false;
            _wma_mid = new IndicatorEMA(_macD.SignalLow.IndicatorHigh);
            _wma_mid.PublishingEnabled = false;
            _wma_high = new IndicatorEMA(_macD.SignalHigh.IndicatorHigh);
            _wma_high.PublishingEnabled = false;
            var rsiShort = new IndicatorRSI(_macD.Index, 1, 14);
            rsiShort.PublishingEnabled = false;
            var rsiLong = new IndicatorRSI(_macD.Index, 2, 14);
            rsiLong.PublishingEnabled = false;
            var wmVol = new IndicatorWMVol(_macD.Index, _wma_low, 60, 90);
            wmVol.PublishingEnabled = false;
            _mktIndices.AddRange(_otherIndices);
            //_mktIndicators.Add(_wma_verylow);
            //_mktIndicators.Add(_wma_low);
            //_mktIndicators.Add(_wma_mid);
            //_mktIndicators.Add(_wma_high);
            //_mktIndicators.Add(wmVol);
            //_mktIndicators.Add(new IndicatorWMVol(_index, _wma_mid, 60, 90));
            _mktIndicators.Add(new IndicatorNearestLevel(_index));

            int lastversion = StaticDataConnection.Instance.GetAnnLatestVersion(_annId, _index.Id);
            _annWeights = StaticDataConnection.Instance.GetAnnWeights(_annId, _index.Id, lastversion);
            var signalType = Type.GetType("MidaxLib.SignalANN" + _annId);
            List<Indicator> annIndicators = new List<Indicator>();
            annIndicators.Add(_wma_verylow);
            annIndicators.Add(_wma_low);
            annIndicators.Add(_wma_mid);
            annIndicators.Add(_wma_high);
            annIndicators.Add(rsiShort);
            annIndicators.Add(rsiLong);
            annIndicators.Add(wmVol);
            List<object> signalParams = new List<object>();
            signalParams.Add(_index);
            signalParams.Add(annIndicators);
            signalParams.Add(_annWeights);
            this._ann = (SignalANN)Activator.CreateInstance(signalType, signalParams.ToArray());
            this._mktSignals.Add(this._ann);

            var allIndices = new List<MarketData>();
            allIndices.Add(_index);
            allIndices.AddRange(_otherIndices);
            foreach (var index in allIndices)
            {
                var indicatorLow = new IndicatorLow(index);
                var indicatorHigh = new IndicatorHigh(index);
                var indicatorCloseBid = new IndicatorCloseBid(index);
                var indicatorCloseOffer = new IndicatorCloseOffer(index);
                _mktIndicators.Add(indicatorLow);
                _mktIndicators.Add(indicatorHigh);
                _mktIndicators.Add(indicatorCloseBid);
                _mktIndicators.Add(indicatorCloseOffer);
                _mktEODIndicators.Add(indicatorLow);
                _mktEODIndicators.Add(indicatorHigh);
                _mktEODIndicators.Add(indicatorCloseBid);
                _mktEODIndicators.Add(indicatorCloseOffer);
                _mktEODIndicators.Add(new IndicatorLevelPivot(index));
                _mktEODIndicators.Add(new IndicatorLevelR1(index));
                _mktEODIndicators.Add(new IndicatorLevelR2(index));
                _mktEODIndicators.Add(new IndicatorLevelR3(index));
                _mktEODIndicators.Add(new IndicatorLevelS1(index));
                _mktEODIndicators.Add(new IndicatorLevelS2(index));
                _mktEODIndicators.Add(new IndicatorLevelS3(index));
            }
        }

        protected override bool Buy(Signal signal, DateTime time, Price stockValue)
        {
            if (_ptf.GetPosition(_index.Id).Quantity < 0)
            {
                signal.Trade.Price = stockValue.Offer;
                _ptf.ClosePosition(signal.Trade, time);
                string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": BUY " + signal.TradingAsset.Id + " " + stockValue.Offer, EventLogEntryType.Information);
            }
            return true;
        }

        protected override bool Sell(Signal signal, DateTime time, Price stockValue)
        {
            if (_ptf.GetPosition(_index.Id).Quantity > 0)
            {
                _ptf.BookTrade(signal.Trade, signal.TradingAsset.Name);
                Log.Instance.WriteEntry(time + " Signal " + signal.Id + ": Unexpected positive position. SELL " + signal.Trade.Id + " " + stockValue.Offer, EventLogEntryType.Error);
            }
            else if (_ptf.GetPosition(_index.Id).Quantity == 0)
            {
                if (!_ptf.BookTrade(signal.Trade, signal.TradingAsset.Name))
                    return false;
                string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": SELL " + signal.TradingAsset.Id + " " + stockValue.Bid, EventLogEntryType.Information);
            }
            return true;
        }
    }
}

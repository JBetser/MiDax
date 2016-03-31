using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class ModelANN : Model
    {
        protected MarketData _daxIndex = null;
        protected List<MarketData> _daxStocks = null;
        protected List<MarketData> _otherIndices = null;
        protected MarketData _vix = null;
        protected IndicatorWMA _wma_low = null;
        protected IndicatorWMA _wma_mid = null;
        protected IndicatorWMA _wma_high = null;
        protected SignalANN _ann = null;
        protected List<decimal> _annWeights = null;
        ModelMacD _macD;

        public ModelANN(ModelMacD macD, List<MarketData> daxStocks, MarketData vix, List<MarketData> otherIndices)
        {
            _macD = macD;
            _daxIndex = macD.Index;
            _daxStocks = daxStocks;
            _otherIndices = otherIndices;
            _vix = vix;
            if (_vix != null)
                _mktIndices.Add(_vix);           
        }

        protected override void Init()
        {
            base.Init();
            _wma_low = new IndicatorWMA(_macD.SignalLow.IndicatorLow);
            _wma_low.PublishingEnabled = false;
            _wma_mid = new IndicatorWMA(_macD.SignalLow.IndicatorHigh);
            _wma_mid.PublishingEnabled = false;
            _wma_high = new IndicatorWMA(_macD.SignalHigh.IndicatorHigh);
            _wma_high.PublishingEnabled = false;
            _mktIndices.AddRange(_otherIndices);
            _mktIndicators.Add(_wma_low);
            _mktIndicators.Add(_wma_mid);
            _mktIndicators.Add(_wma_high);
            _mktIndicators.Add(new IndicatorWMVol(_daxIndex, 10));
            _mktIndicators.Add(new IndicatorWMVol(_daxIndex, 60));
            _mktIndicators.Add(new IndicatorNearestLevel(_daxIndex));

            var annId = "WMA_4_2";
            int lastversion = StaticDataConnection.Instance.GetAnnLatestVersion(annId, _daxIndex.Id);
            _annWeights = StaticDataConnection.Instance.GetAnnWeights(annId, _daxIndex.Id, lastversion);
            var signalType = Type.GetType("MidaxLib.SignalANN" + annId);
            List<Indicator> annIndicators = new List<Indicator>();
            annIndicators.Add(_wma_low);
            annIndicators.Add(_wma_mid);
            annIndicators.Add(_wma_high);
            List<object> signalParams = new List<object>();
            signalParams.Add(_daxIndex);
            signalParams.Add(annIndicators);
            signalParams.Add(_annWeights);
            this._ann = (SignalANN)Activator.CreateInstance(signalType, signalParams.ToArray());
            this._mktSignals.Add(this._ann);

            var allIndices = new List<MarketData>();
            allIndices.Add(_daxIndex);
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
            if (_ptf.GetPosition(_daxIndex.Id).Quantity < 0)
            {
                signal.Trade.Price = stockValue.Offer;
                _ptf.ClosePosition(signal.Trade, time);
                string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": BUY " + signal.MarketData.Id + " " + stockValue.Offer, EventLogEntryType.Information);
            }
            return true;
        }

        protected override bool Sell(Signal signal, DateTime time, Price stockValue)
        {
            if (_ptf.GetPosition(_daxIndex.Id).Quantity > 0)
            {
                _ptf.BookTrade(signal.Trade);
                Log.Instance.WriteEntry(time + " Signal " + signal.Id + ": Unexpected positive position. SELL " + signal.Trade.Id + " " + stockValue.Offer, EventLogEntryType.Error);
            }
            else if (_ptf.GetPosition(_daxIndex.Id).Quantity == 0)
            {
                if (time <= _closingTime)
                {
                    _ptf.BookTrade(signal.Trade);
                    string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                    Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": SELL " + signal.MarketData.Id + " " + stockValue.Bid, EventLogEntryType.Information);
                }
            }
            return true;
        }
    }
}

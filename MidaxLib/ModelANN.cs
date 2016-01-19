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
        protected DateTime _closingTime = DateTime.MinValue;
        protected MarketData _daxIndex = null;
        protected List<MarketData> _daxStocks = null;
        protected MarketData _vix = null;
        protected IndicatorWMA _wma_low = null;
        protected IndicatorWMA _wma_mid = null;
        protected IndicatorWMA _wma_high = null;
        protected SignalANN _ann = null;
        protected List<decimal> _annWeights = null;

        public ModelANN(MarketData daxIndex, List<MarketData> daxStocks, MarketData vix, List<MarketData> otherIndices)
        {
            List<MarketData> mktData = new List<MarketData>();
            mktData.Add(daxIndex);
            mktData.AddRange(daxStocks);
            this._closingTime = Config.ParseDateTimeLocal(Config.Settings["TRADING_CLOSING_TIME"]);
            this._mktData = mktData;
            this._daxIndex = daxIndex;
            this._daxStocks = daxStocks;
            this._vix = vix;
            if (this._vix != null)
                this._mktIndices.Add(this._vix);
            this._mktIndices.AddRange(otherIndices);
            this._mktIndicators.Add(_wma_low);
            this._mktIndicators.Add(_wma_mid);
            this._mktIndicators.Add(_wma_high);
            this._mktIndicators.Add(new IndicatorWMVol(_daxIndex, 10));
            this._mktIndicators.Add(new IndicatorWMVol(_daxIndex, 60));
            this._mktEODIndicators.Add(new IndicatorLevelMean(_daxIndex));
            this._mktEODIndicators.Add(new IndicatorLevelPivot(_daxIndex));
            this._mktEODIndicators.Add(new IndicatorLevelR1(_daxIndex));
            this._mktEODIndicators.Add(new IndicatorLevelR2(_daxIndex));
            this._mktEODIndicators.Add(new IndicatorLevelR3(_daxIndex));
            this._mktEODIndicators.Add(new IndicatorLevelS1(_daxIndex));
            this._mktEODIndicators.Add(new IndicatorLevelS2(_daxIndex));
            this._mktEODIndicators.Add(new IndicatorLevelS3(_daxIndex));

            var annId = "WMA_4_2";
            int lastversion = StaticDataConnection.Instance.GetAnnLatestVersion(annId, daxIndex.Id);
            this._annWeights = StaticDataConnection.Instance.GetAnnWeights(annId, daxIndex.Id, lastversion);
            var signalType = Type.GetType("MidaxLib.SignalANN" + annId);
            List<Indicator> annIndicators = new List<Indicator>();
            annIndicators.Add(this._wma_low);
            annIndicators.Add(this._wma_mid);
            annIndicators.Add(this._wma_high);
            List<object> signalParams = new List<object>();
            signalParams.Add(daxIndex);
            signalParams.Add(annIndicators);
            signalParams.Add(this._annWeights);
            //this._ann = (SignalANN)Activator.CreateInstance(signalType, signalParams.ToArray());
            //this._mktSignals.Add(this._ann);
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
}

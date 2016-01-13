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
        protected List<MarketData> _volatilityIndices = null;
        protected SignalANN _signal = null;
        protected List<decimal> _weights = null;

        public ModelANN(string annId, MarketData daxIndex, List<MarketData> daxStocks, List<MarketData> volatilityIndices, List<Indicator> indicators)
        {
            List<MarketData> mktData = new List<MarketData>();
            mktData.Add(daxIndex);
            mktData.AddRange(daxStocks);
            this._closingTime = Config.ParseDateTimeLocal(Config.Settings["TRADING_CLOSING_TIME"]);
            this._mktData = mktData;
            this._daxIndex = daxIndex;
            this._daxStocks = daxStocks;
            this._volatilityIndices = volatilityIndices;
            this._mktIndices.AddRange(volatilityIndices);
            int lastversion = StaticDataConnection.Instance.GetAnnLatestVersion(annId, daxIndex.Id);
            this._weights = StaticDataConnection.Instance.GetAnnWeights(annId, daxIndex.Id, lastversion);
            var signalType = Type.GetType("MidaxLib.SignalANN" + annId);
            List<object> signalParams = new List<object>();
            signalParams.Add(daxIndex);
            signalParams.Add(indicators);
            signalParams.Add(this._weights);
            this._signal = (SignalANN)Activator.CreateInstance(signalType, signalParams.ToArray());
            this._mktSignals.Add(this._signal);
        }

        protected override void Buy(Signal signal, DateTime time, Price value)
        {
            if (_ptf.GetPosition(_daxIndex.Id).Value < 0)
            {
                if (time >= _closingTime)
                {
                    _ptf.ClosePosition(signal.Trade, time);
                }
                else
                {
                    _ptf.BookTrade(signal.Trade);
                    string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                    Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": BUY " + signal.Asset.Id + " " + value.Offer, EventLogEntryType.Information);
                }
            }
        }

        protected override void Sell(Signal signal, DateTime time, Price value)
        {
            if (_ptf.GetPosition(_daxIndex.Id).Value >= 0)
            {
                if (time >= _closingTime)
                {
                    if (_ptf.GetPosition(_daxIndex.Id).Value > 0)
                        _ptf.ClosePosition(signal.Trade, time);
                }
                else
                {
                    _ptf.BookTrade(signal.Trade);
                    string tradeRef = signal.Trade == null ? "" : " " + signal.Trade.Reference;
                    Log.Instance.WriteEntry(time + tradeRef + " Signal " + signal.Id + ": SELL " + signal.Asset.Id + " " + value.Bid, EventLogEntryType.Information);
                }
            }
        }
    }
}

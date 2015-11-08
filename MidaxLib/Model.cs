using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IGPublicPcl;

namespace MidaxLib
{
    public abstract class Model
    {
        protected List<MarketData> _mktData = new List<MarketData>();
        protected List<Signal> _mktSignals = new List<Signal>();
        protected List<MarketData> _mktIndices = new List<MarketData>();
        protected List<Indicator> _mktIndicators = new List<Indicator>();
        protected List<IndicatorLevel> _mktUnsubscribedIndicators = new List<IndicatorLevel>();
 
        public Model()
        {            
        }

        protected abstract void OnBuy(Signal signal, DateTime time, Price value);
        protected abstract void OnSell(Signal signal, DateTime time, Price value);

        protected virtual void OnUpdateMktData(MarketData mktData, DateTime updateTime, Price value)
        {
        }
        protected virtual void OnUpdateIndicator(MarketData mktData, DateTime updateTime, Price value)
        {
        }
        
        public void StartSignals()
        {
            foreach (MarketData idx in _mktIndices)
                idx.Subscribe(OnUpdateMktData);
            foreach (Indicator ind in _mktIndicators)
                ind.Subscribe(OnUpdateIndicator);
            foreach (Signal sig in _mktSignals)
                sig.Subscribe(OnBuy, OnSell);            
            MarketDataConnection.Instance.StartListening();
        }

        public string StopSignals()
        {
            MarketDataConnection.Instance.StopListening();
            Log.Instance.WriteEntry("Publishing indicator levels...", EventLogEntryType.Information);
            foreach (var indicator in _mktUnsubscribedIndicators)
                indicator.Publish(DateTime.SpecifyKind(DateTime.Parse(Config.Settings["PUBLISHING_STOP_TIME"]), DateTimeKind.Utc));
            return PublisherConnection.Instance.Close();
        }
    }

    public class ModelMidax : Model
    {
        protected MarketData _daxIndex = null;
        protected List<MarketData> _daxStocks = null;
        protected List<MarketData> _volatilityIndices = null;
        protected SignalMacD _macD_low = null;
        protected SignalMacD _macD_high = null;

        public ModelMidax(MarketData daxIndex, List<MarketData> daxStocks, List<MarketData> volatilityIndices, int lowPeriod = 2, int midPeriod = 10, int highPeriod = 60)
        {
            List<MarketData> mktData = new List<MarketData>();
            mktData.Add(daxIndex);
            mktData.AddRange(daxStocks);
            this._mktData = mktData;
            this._daxIndex = daxIndex;
            this._daxStocks = daxStocks;
            this._volatilityIndices = volatilityIndices;
            this._mktIndices.AddRange(volatilityIndices);
            this._macD_low = new SignalMacD(_daxIndex, lowPeriod, midPeriod);
            this._macD_high = new SignalMacD(_daxIndex, midPeriod, highPeriod, this._macD_low.IndicatorHigh);
            this._mktSignals.Add(this._macD_low);
            this._mktSignals.Add(this._macD_high);
            this._mktIndicators.Add(new IndicatorLinearRegression(_daxIndex, new TimeSpan(0, 0, lowPeriod * 30)));
            this._mktIndicators.Add(new IndicatorLinearRegression(_daxIndex, new TimeSpan(0, 0, midPeriod * 30)));
            this._mktIndicators.Add(new IndicatorLinearRegression(_daxIndex, new TimeSpan(0, 0, highPeriod * 30)));
            this._mktUnsubscribedIndicators.Add(new IndicatorLevelMean(_daxIndex));
        }

        protected override void OnBuy(Signal signal, DateTime time, Price value)
        {
            Log.Instance.WriteEntry(time + "Signal " + signal.Id + ": BUY " + signal.Asset.Id + " " + value.Offer, EventLogEntryType.Information);
        }

        protected override void OnSell(Signal signal, DateTime time, Price value)
        {
            Log.Instance.WriteEntry(time + "Signal " + signal.Id + ": SELL " + signal.Asset.Id + " " + value.Bid, EventLogEntryType.Information);
        }
    }
}

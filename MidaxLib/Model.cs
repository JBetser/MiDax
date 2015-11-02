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
        protected List<MarketData> _mktData = null;
        protected List<Signal> _mktSignals = null;
        protected List<IndicatorLevel> _mktUnsubscribedIndicators = new List<IndicatorLevel>();
 
        public Model()
        {            
        }

        public Model(List<MarketData> mktData, List<Signal> mktSignals)
        {
            this._mktData = mktData;     
            this._mktSignals = mktSignals;            
        }

        protected abstract void OnBuy(MarketData mktData, DateTime time, Price value);
        protected abstract void OnSell(MarketData mktData, DateTime time, Price value);
        
        public void StartSignals()
        {
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
        protected SignalMacD _macD = null;

        public ModelMidax(MarketData daxIndex, List<MarketData> daxStocks, int lowPeriod = 5, int highPeriod = 60)
        {
            List<MarketData> mktData = new List<MarketData>();
            mktData.Add(daxIndex);
            mktData.AddRange(daxStocks);
            this._mktData = mktData;
            this._daxIndex = daxIndex;
            this._daxStocks = daxStocks;
            this._macD = new SignalMacD(_daxIndex, lowPeriod, highPeriod);
            _mktSignals = new List<Signal>();
            _mktSignals.Add(this._macD);
            this._mktUnsubscribedIndicators.Add(new IndicatorLevelMean(_daxIndex));
        }

        protected override void OnBuy(MarketData mktData, DateTime time, Price value)
        {
            Log.Instance.WriteEntry(time + ": BUY " + mktData.Id + " " + value.Offer, EventLogEntryType.Information);
        }

        protected override void OnSell(MarketData mktData, DateTime time, Price value)
        {
            Log.Instance.WriteEntry(time + ": SELL " + mktData.Id + " " + value.Bid, EventLogEntryType.Information);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IGPublicPcl;

namespace MidaxLib
{
    public abstract class Model
    {
        protected List<MarketData> _mktData = null;
        protected List<Signal> _mktSignals = null;
        static protected Dictionary<string, string> _settings = null;
        public Model()
        {
        }

        public Model(List<MarketData> mktData, List<Signal> mktSignals)
        {
            this._mktData = mktData;
            this._mktSignals = mktSignals;
        }

        public static Dictionary<string, string> Settings
        {
            get { return _settings; }
            set { _settings = value; }
        }

        public static bool TradingEnabled
        {
            get
            {
                return DateTime.Now.TimeOfDay > TimeSpan.Parse(Model.Settings["TRADING_START_TIME"]) &&
                    DateTime.Now.TimeOfDay < TimeSpan.Parse(Model.Settings["TRADING_STOP_TIME"]);
            }
        }

        protected abstract void OnBuy(MarketData mktData, DateTime time, Price value);
        protected abstract void OnSell(MarketData mktData, DateTime time, Price value);

        public void StartSignals()
        {
            foreach (Signal sig in _mktSignals)
                sig.Subscribe(OnBuy, OnSell);
            IGConnection.Instance.StartListening();
        }

        public void StopSignals()
        {
            IGConnection.Instance.StopListening();
        }
    }

    public class ModelMidax : Model
    {
        MarketData _daxIndex = null;
        List<MarketData> _daxStocks = null;
        SignalMacD _macD = null;

        public ModelMidax(MarketData daxIndex, List<MarketData> daxStocks)
        {
            List<MarketData> mktData = new List<MarketData>();
            mktData.Add(daxIndex);
            mktData.AddRange(daxStocks);
            this._mktData = mktData;
            this._daxIndex = daxIndex;
            this._daxStocks = daxStocks;
            this._macD = new SignalMacD(_daxIndex);
            _mktSignals = new List<Signal>();
            _mktSignals.Add(this._macD);
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

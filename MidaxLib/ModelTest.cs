using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class ModelTest : Model
    {
        MarketData _daxIndex = null;
        List<MarketData> _daxStocks = null;
        SignalMacD _macD = null;

        public ModelTest(MarketData daxIndex, List<MarketData> daxStocks)
        {
            List<MarketData> mktData = new List<MarketData>();
            mktData.Add(daxIndex);
            mktData.AddRange(daxStocks);
            this._mktData = mktData;
            this._daxIndex = daxIndex;
            this._daxStocks = daxStocks;
            this._macD = new SignalMacD(_daxIndex, 1, 5);
            _mktSignals = new List<Signal>();
            _mktSignals.Add(this._macD);
        }

        protected override void OnBuy(MarketData mktData, DateTime time, Price value)
        {
            string info = "buy " + mktData.Id + " " + value.Offer;
            Log.Instance.WriteEntry(time + ": TESTING " + info, EventLogEntryType.Information);
            Console.WriteLine(info);
        }

        protected override void OnSell(MarketData mktData, DateTime time, Price value)
        {
            string info = "sell " + mktData.Id + " " + value.Bid;
            Log.Instance.WriteEntry(time + ": TESTING " + info, EventLogEntryType.Information);
            Console.WriteLine(info);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class ModelTest : ModelMidax
    {
        public ModelTest(MarketData daxIndex, List<MarketData> daxStocks) : base(daxIndex, daxStocks, new List<MarketData>(), 1, 5)
        {
            this._mktIndicators = new List<Indicator>();
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

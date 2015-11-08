using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MidaxLib
{
    public class ModelTest : ModelMidax
    {
        bool _replayPopup;

        public ModelTest(MarketData daxIndex, List<MarketData> daxStocks) : base(daxIndex, daxStocks, new List<MarketData>(), 1, 5, 10)
        {
            _replayPopup = Config.Settings["REPLAY_POPUP"] == "1";
        }

        protected override void OnBuy(Signal signal, DateTime time, Price value)
        {
            string info = signal.Id + " buy " + signal.Asset.Id + " " + value.Offer;
            Log.Instance.WriteEntry(time + ": TESTING " + info, EventLogEntryType.Information);
            Console.WriteLine(info);
        }

        protected override void OnSell(Signal signal, DateTime time, Price value)
        {
            string info = signal.Id + " sell " + signal.Asset.Id + " " + value.Bid;
            Log.Instance.WriteEntry(time + ": TESTING " + info, EventLogEntryType.Information);
            Console.WriteLine(info);
        }

        public void ProcessError(string message, string expected = "")
        {
            string info = "An exception message test failed; " + (expected == "" ? message :
                "expected \"" + expected + "\" != \"" + message + "\"");
            Console.WriteLine(info);
            if (_replayPopup)
                MessageBox.Show(info);
            throw new ApplicationException(info);
        }
    }
}

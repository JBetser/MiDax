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
        bool _replayPopup = false;

        public ModelTest(MarketData daxIndex, List<MarketData> daxStocks, List<MarketData> volIndices, int lowPeriod = 2, int midPeriod = 10, int highPeriod = 60)
            : base(daxIndex, daxStocks, volIndices, lowPeriod, midPeriod, highPeriod)
        {
            if (Config.Settings.ContainsKey("REPLAY_POPUP"))
                _replayPopup = Config.Settings["REPLAY_POPUP"] == "1";
        }

        protected override void Buy(Signal signal, DateTime time, Price value)
        {
            base.Buy(signal, time, value);
            string info = time + " Signal " + signal.Id + " buy " + signal.Asset.Id + " " + value.Bid;
            Console.WriteLine(info);
        }

        protected override void Sell(Signal signal, DateTime time, Price value)
        {
            base.Sell(signal, time, value);
            string info = time + " Signal " + signal.Id + " sell " + signal.Asset.Id + " " + value.Bid;
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

    public class ModelQuickTest : ModelTest
    {
        public ModelQuickTest(MarketData daxIndex)
            : base(daxIndex, new List<MarketData>(), new List<MarketData>(), 1, 5, 10)
        {
        }
    }
}

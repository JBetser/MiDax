using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MidaxLib
{
    public class ModelMacDTest : ModelMacD
    {        
        public ModelMacDTest(MarketData daxIndex, List<MarketData> daxStocks, List<MarketData> volIndices, int lowPeriod = 2, int midPeriod = 10, int highPeriod = 60)
            : base(daxIndex, daxStocks, volIndices, lowPeriod, midPeriod, highPeriod)
        {            
        }

        protected override void Buy(Signal signal, DateTime time, Price value)
        {
            if (signal.Id == _tradingSignal)
            {
                if (_ptf.GetPosition(_daxIndex.Id).Value < 0)
                    Console.WriteLine(time + " Signal " + signal.Id + " buy " + signal.MarketData.Id + " " + value.Bid);
            }
            base.Buy(signal, time, value);
            
        }

        protected override void Sell(Signal signal, DateTime time, Price value)
        {
            if (signal.Id == _tradingSignal)
            {
                if (_ptf.GetPosition(_daxIndex.Id).Value == 0)
                    Console.WriteLine(time + " Signal " + signal.Id + " sell " + signal.MarketData.Id + " " + value.Bid);
            }
            base.Sell(signal, time, value);
            
        }     
   
        public override void ProcessError(string message, string expected = "")
        {
            string info = "An exception message test failed; " + (expected == "" ? message :
                "expected \"" + expected + "\" != \"" + message + "\"");
            Console.WriteLine(info);
            if (_replayPopup)
                MessageBox.Show(info);
            throw new ApplicationException(info);
        }
    }

    public class ModelQuickTest : ModelMacDTest
    {
        public ModelQuickTest(MarketData daxIndex)
            : base(daxIndex, new List<MarketData>(), new List<MarketData>(), 1, 5, 10)
        {
        }
    }
}

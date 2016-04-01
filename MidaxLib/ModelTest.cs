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
        public ModelMacDTest(MarketData index, int lowPeriod = 1, int midPeriod = 5, int highPeriod = 10, MarketData tradingIndex = null)
            : base(index, lowPeriod, midPeriod, highPeriod, tradingIndex)
        {            
        }

        protected override bool Buy(Signal signal, DateTime time, Price stockValue)
        {
            if (base.Buy(signal, time, stockValue))
            {
                Console.WriteLine(time + " Signal " + signal.Id + " buy " + signal.MarketData.Id + " " + stockValue.Bid);
                return true;
            }
            return false;
        }

        protected override bool Sell(Signal signal, DateTime time, Price stockValue)
        {
            if (base.Sell(signal, time, stockValue))
            {
                Console.WriteLine(time + " Signal " + signal.Id + " sell " + signal.MarketData.Id + " " + stockValue.Bid);
                return true;
            }
            return false;
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

    public class ModelMacDCascadeTest : ModelMacDCascade
    {
        public ModelMacDCascadeTest(ModelMacD macD)
            : base(macD)
        {
        }

        protected override bool Buy(Signal signal, DateTime time, Price stockValue)
        {
            if (base.Buy(signal, time, stockValue))
            {
                Console.WriteLine(time + " Signal " + signal.Id + " buy " + signal.MarketData.Id + " " + stockValue.Bid);
                return true;
            }
            return false;

        }

        protected override bool Sell(Signal signal, DateTime time, Price stockValue)
        {
            if (base.Sell(signal, time, stockValue))
            {
                Console.WriteLine(time + " Signal " + signal.Id + " sell " + signal.MarketData.Id + " " + stockValue.Bid);
                return true;
            }
            return false;

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

    public class ModelMoleTest : ModelMole
    {
        public ModelMoleTest(ModelMacD macD)
            : base(macD)
        {
        }
        
        protected override bool Sell(Signal signal, DateTime time, Price stockValue)
        {
            if (_tradingSet.PlaceTrade(signal.Trade, stockValue.Bid))
                Console.WriteLine(time + " Signal " + signal.Id + " sell " + signal.MarketData.Id + " " + stockValue.Bid);
            return false;
        }

        protected override void OnUpdateIndex(MarketData mktData, DateTime updateTime, Price stockValue)
        {
            if (_tradingSet.UpdateIndex(updateTime, stockValue.Offer))
                Console.WriteLine(updateTime + " Signal " + _signal.Id + " buy " + _signal.MarketData.Id + " " + stockValue.Offer);
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
}

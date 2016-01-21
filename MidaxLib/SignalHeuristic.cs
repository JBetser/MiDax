using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class SignalMole : SignalMacD
    {
        public SignalMole(MarketData asset, int lowPeriod, int highPeriod, IndicatorWMA low = null, IndicatorWMA high = null)
            : base("Mole_" + lowPeriod + "_" + highPeriod + "_" + asset.Id, asset, lowPeriod, highPeriod, low, high)
        {
        }

        public SignalMole(string id, MarketData asset, int lowPeriod, int highPeriod, IndicatorWMA low = null, IndicatorWMA high = null)
            : base(id, asset, lowPeriod, highPeriod, low, high)
        {
        }

        protected override bool Process(MarketData indicator, DateTime updateTime, Price value, ref Signal.Tick tradingOrder)
        {
            if (base.Process(indicator, updateTime, value, ref tradingOrder)){
                return true;
            }
            return false;
        }
    }
}

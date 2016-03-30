using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IGPublicPcl;


namespace MidaxLib
{
    /************************************************************************************************************
     * RSI Indicator
     * Inputs:
     *      1.Upper band    : Overbought area
     *      2.Lower band    : Oversold area
     *      3.Look-back     : usually 14 periods, lower for more sensitivity depending on the security's volatility
     * Return: value between 0 and 100
     * ---------------------------------------------------------------------------------------------------------
     * A momentum indicator to show which of the bulls or the bears have the momentum
     * in the market. It compares the average of n periods of gains against n periods 
     * of loss using the formula:
     *      RSI = (100 - (100/(1 + RS)))
     *      
     * with RS = 14d Exp.MovingAverage(gains) / 14d Exp.MovingAverage(loss)
     * if period = 14d
     * *********************************************************************************************************/
    public class IndicatorRSI : IndicatorWMA
    {

        public IndicatorRSI(MarketData mktData, int periodMinutes)
            : base("RSI_" + periodMinutes + "_" + mktData.Id, mktData, periodMinutes)
        {
            _periodSeconds = periodMinutes * 60;
        }

        protected override Price IndicatorFunc(MarketData mktData, DateTime updateTime)
        {
            return calcRSI(mktData, updateTime);
        }

        public Price calcRSI(MarketData mktData, DateTime upDateTime)
        {
            Price val = new Price();
            Price gains = new Price();
            Price avgGains = new Price();
            Price loss = new Price();
            Price avgLoss = new Price();
            Price prevPrice = new Price();
            decimal rs;
            bool started = false;
            DateTime startTime = upDateTime.AddSeconds(-_periodSeconds);
            IEnumerable<KeyValuePair<DateTime, Price>> generator = MarketData.TimeSeries.ValueGenerator(startTime, upDateTime);
            KeyValuePair<DateTime, Price> beginPeriodValue = new KeyValuePair<DateTime, Price>();
            foreach (var endPeriodValue in generator)
            {
                if (!started)
                {
                    started = true;
                    if (Math.Max(0, (int)(endPeriodValue.Key - startTime).TotalMilliseconds) != 0)
                        return null;
                    beginPeriodValue = new KeyValuePair<DateTime, Price>(startTime, endPeriodValue.Value);
                    continue;
                }
                else
                {
                    if (endPeriodValue.Value > beginPeriodValue.Value)
                    {
                        gains += endPeriodValue.Value - beginPeriodValue.Value;
                    }
                    else
                    {
                        loss += beginPeriodValue.Value - endPeriodValue.Value;
                    }

                    val += beginPeriodValue.Value * _timeDecay.Weight(beginPeriodValue.Key, endPeriodValue.Key, _periodMilliSeconds, startTime);
                }
                beginPeriodValue = endPeriodValue;
            }

            avgGains = gains / Period;
            avgLoss = loss / Period;
            rs = avgGains.Bid / avgLoss.Bid;
            val.set(100 - 100 / (1 + rs));
            return val;
        }
    }
}
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
        int _nbPeriods;
        int _subPeriodSeconds;
        DateTime _nextRsiTime = DateTime.MinValue;

        public IndicatorRSI(MarketData mktData, int subPeriodMinutes, int nbPeriods)
            : base("RSI_" + subPeriodMinutes + "_" + nbPeriods + "_" + mktData.Id, mktData, nbPeriods * subPeriodMinutes)
        {
            _periodSeconds = nbPeriods * subPeriodMinutes * 60;
            _subPeriodSeconds = subPeriodMinutes * 60;
            _nbPeriods = nbPeriods;
        }

        protected override Price IndicatorFunc(MarketData mktData, DateTime updateTime, Price value)
        {            
            return calcRSI(mktData, updateTime);
        }

        public Price calcRSI(MarketData mktData, DateTime upDateTime)
        {
            Price avgGains = new Price();
            Price avgLosses = new Price();
            Price curGains = new Price();
            Price curLosses = new Price();
            bool started = false;
            DateTime startTime = upDateTime.AddSeconds(-_periodSeconds);
            if (MarketData.TimeSeries.StartTime() > startTime)
                return null;
            if ((upDateTime - _nextRsiTime).TotalMilliseconds > _subPeriodSeconds * 1000)
                _nextRsiTime = upDateTime;
            IEnumerable<KeyValuePair<DateTime, Price>> generator = MarketData.TimeSeries.ValueGenerator(startTime, upDateTime, false);
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
                    if ((endPeriodValue.Key - beginPeriodValue.Key).TotalSeconds > _subPeriodSeconds)
                    {
                        if (endPeriodValue.Value > beginPeriodValue.Value)
                        {
                            curGains = endPeriodValue.Value - beginPeriodValue.Value;
                            curLosses = new Price(0m);
                            avgGains += curGains;
                        }
                        else
                        {
                            curLosses = beginPeriodValue.Value - endPeriodValue.Value;
                            curGains = new Price(0m);
                            avgLosses += curLosses;
                        }
                        beginPeriodValue = endPeriodValue;
                    }
                }                
            }

            avgGains /= (decimal)_nbPeriods;
            avgLosses /= (decimal)_nbPeriods;

            decimal rs = avgLosses.Bid == 0 ? 1m : (avgGains.Bid * (_nbPeriods - 1) + curGains.Bid) / (avgLosses.Bid * (_nbPeriods - 1) + curLosses.Bid);
            return new Price(100m - 100m / (1m + rs));
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            Price avgPrice = IndicatorFunc(mktData, updateTime, value);
            if (avgPrice != null)
            {
                base.OnUpdate(mktData, updateTime, avgPrice);
                Publish(_nextRsiTime, avgPrice.MidPrice());
            }
        }
    }
}
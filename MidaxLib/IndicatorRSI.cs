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
        public int NbPeriods { get { return _nbPeriods; } }
        public int PeriodSizeMn { get { return _subPeriodSeconds / 60; } }
        DateTime _nextRsiTime = DateTime.MinValue;
        List<RsiCandle> _rsiCandles = new List<RsiCandle>();
        List<RsiCandle> _history = new List<RsiCandle>();
        RsiCandle _curCandle = null;

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

        class RsiCandle
        {
            public bool GainRsi;
            public bool GainAsset;
            public decimal DiffRsi;
            public decimal DiffAsset;
            public decimal StartRsiValue;
            public decimal StartAssetValue;

            public RsiCandle(decimal startRsiValue, decimal starAssetValue, bool gain = false, decimal diffRsi = 0m, decimal diffAsset = 0m)
            {
                GainAsset = gain;
                GainRsi = false;
                DiffRsi = diffRsi;
                DiffAsset = diffAsset;
                StartRsiValue = startRsiValue;
                StartAssetValue = starAssetValue;
            }
        }

        Price calcRSI(MarketData mktData, DateTime updateTime)
        {
            if (updateTime >= _nextRsiTime)
            {
                if (mktData.TimeSeries.Count < 2)
                    return null;
                if (_nextRsiTime == DateTime.MinValue)
                    _nextRsiTime = updateTime.AddSeconds(_subPeriodSeconds);
                else
                    _nextRsiTime = _nextRsiTime.AddSeconds(_subPeriodSeconds);
                DateTime rsiTime = _nextRsiTime.AddSeconds(-_subPeriodSeconds);
                if (mktData.TimeSeries.StartTime() > rsiTime)
                    return null;
                if (_rsiCandles.Count == _nbPeriods)
                    _rsiCandles.RemoveAt(0);
                if (_history.Count == 120)
                    _history.RemoveAt(0);
                if (_rsiCandles.Count > 0){
                    var curStartRsi = _rsiCandles[_rsiCandles.Count - 1].StartRsiValue + (_rsiCandles[_rsiCandles.Count - 1].GainRsi ? _rsiCandles[_rsiCandles.Count - 1].DiffRsi:
                                            -_rsiCandles[_rsiCandles.Count - 1].DiffRsi);
                    _curCandle = new RsiCandle(curStartRsi, mktData.TimeSeries.Last().Bid);
                }
                else
                    _curCandle = new RsiCandle(50m, mktData.TimeSeries.Last().Bid);
                _rsiCandles.Add(_curCandle);
                _history.Add(_curCandle);
            }
            else
                _curCandle = _rsiCandles.Last();
            DateTime startTime = _nextRsiTime.AddSeconds(-_subPeriodSeconds);
            if (mktData.TimeSeries.StartTime() > startTime)
                return null;
            var prevValStart = mktData.TimeSeries.PrevValue(startTime);
            if (!prevValStart.HasValue)
                return null;
            Price valStart = prevValStart.Value.Value;
            Price valEnd = mktData.TimeSeries.Last();
            Price valRsiStart = prevValStart.Value.Value;
            Price valRsiEnd = mktData.TimeSeries.Last();
            if (valEnd > valStart)
            {
                _curCandle.GainAsset = true;
                _curCandle.DiffAsset = valEnd.Mid() - valStart.Mid();                
            }
            else
            {
                _curCandle.GainAsset = false;
                _curCandle.DiffAsset = valStart.Mid() - valEnd.Mid();
            }
            var sumGain = 0m;
            var sumLosses = 0m;
            foreach (var candle in _rsiCandles)
            {
                if (candle.GainAsset)
                    sumGain += candle.DiffAsset;
                else
                    sumLosses += candle.DiffAsset;
            }
            decimal rs = Math.Abs(sumLosses) < 0.1m ? (Math.Abs(sumGain) < 0.1m ? 1m : 1000m) : sumGain / sumLosses;
            Price rsi = new Price(100m - 100m / (1m + rs));
            if (rsi.Bid > _curCandle.StartRsiValue){
                _curCandle.GainRsi = true;
                _curCandle.DiffRsi = rsi.Bid - _curCandle.StartRsiValue;
            }
            else{
                _curCandle.GainRsi = false;
                _curCandle.DiffRsi = _curCandle.StartRsiValue - rsi.Bid;
            }
            return rsi;
        }

        protected override void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            Price rsi = IndicatorFunc(mktData, updateTime, value);
            if (rsi != null)
            {
                _values.Add(_nextRsiTime, rsi);
                Publish(_nextRsiTime, rsi.Bid);
            }
        }

        public decimal MaxRsi(int periodMn)
        {
            decimal max = decimal.MinValue;
            for (int idxCandle = _history.Count - periodMn; idxCandle < _history.Count; idxCandle++)
            {
                if (_history[idxCandle].GainRsi)
                {
                    if (max < _history[idxCandle].StartRsiValue + _history[idxCandle].DiffRsi)
                        max = _history[idxCandle].StartRsiValue + _history[idxCandle].DiffRsi;
                }
                else
                {
                    if (max < _history[idxCandle].StartRsiValue)
                        max = _history[idxCandle].StartRsiValue;
                }
            }
            return max;
        }

        public decimal MinRsi(int periodMn)
        {
            decimal min = decimal.MaxValue;
            for (int idxCandle = _history.Count - periodMn; idxCandle < _history.Count; idxCandle++)
            {
                if (_history[idxCandle].GainRsi)
                {
                    if (min > _history[idxCandle].StartRsiValue)
                        min = _history[idxCandle].StartRsiValue;
                }
                else
                {
                    if (min > _history[idxCandle].StartRsiValue - _history[idxCandle].DiffRsi)
                        min = _history[idxCandle].StartRsiValue - _history[idxCandle].DiffRsi;
                }
            }
            return min;
        }

        public decimal MaxAsset(int periodMn)
        {
            decimal max = decimal.MinValue;
            for (int idxCandle = _history.Count - periodMn; idxCandle < _history.Count; idxCandle++)
            {
                if (_history[idxCandle].GainAsset)
                {
                    if (max < _history[idxCandle].StartAssetValue + _history[idxCandle].DiffAsset)
                        max = _history[idxCandle].StartAssetValue + _history[idxCandle].DiffAsset;
                }
                else
                {
                    if (max < _history[idxCandle].StartAssetValue)
                        max = _history[idxCandle].StartAssetValue;
                }
            }
            return max;
        }

        public decimal MinAsset(int periodMn)
        {
            decimal min = decimal.MaxValue;
            for (int idxCandle = _history.Count - periodMn; idxCandle < _history.Count; idxCandle++)
            {
                if (_history[idxCandle].GainAsset)
                {
                    if (min > _history[idxCandle].StartAssetValue)
                        min = _history[idxCandle].StartAssetValue;
                }
                else
                {
                    if (min > _history[idxCandle].StartAssetValue - _history[idxCandle].DiffAsset)
                        min = _history[idxCandle].StartAssetValue - _history[idxCandle].DiffAsset;
                }
            }
            return min;
        }
    }
}
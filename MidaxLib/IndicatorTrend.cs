using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IGPublicPcl;
using NLapack.Matrices;


namespace MidaxLib
{
    public class Candle
    {
        public decimal Min;
        public decimal Max;
        public DateTime MinTime;
        public DateTime MaxTime;

        public Candle(decimal minValue, decimal maxValue, DateTime minTime, DateTime maxTime)
        {
            Min = minValue;
            Max = maxValue;
            MinTime = minTime;
            MaxTime = maxTime;
        }

        public decimal AdjustedMax(DateTime startTime, decimal centralCoeff)
        {
            return Max + (decimal)(MaxTime - startTime).TotalSeconds * centralCoeff;
        }

        public decimal AdjustedMin(DateTime startTime, decimal centralCoeff)
        {
            return Min + (decimal)(MaxTime - startTime).TotalSeconds * centralCoeff;
        }
    }

    /************************************************************************************************************
     * Trend Indicator
     * *********************************************************************************************************/
    public class IndicatorTrend : IndicatorWMA
    {
        int _nbPeriods;
        public int NbPeriods { get { return _nbPeriods; } }
        public int NbPeaks { get { return _nbPeaks; } }
        public List<Candle> Candles { get { return _candles; } }
        DateTime _nextTime = DateTime.MinValue;
        List<Candle> _candles = new List<Candle>();
        Candle _curCandle = null;
        int _nbPeaks = 3;
        bool _simple = false;

        public IndicatorTrend(MarketData mktData, int subPeriodSeconds, int nbPeriods, bool simple)
            : base("Trend_" + subPeriodSeconds + "_" + nbPeriods + "_" + mktData.Id, mktData, 0)
        {
            _subPeriodSeconds = subPeriodSeconds;
            _periodMilliSeconds = _subPeriodSeconds * 1000m;
            _periodTimeSpan = new TimeSpan(0, 0, _subPeriodSeconds);
            _nbPeriods = nbPeriods;
            _simple = simple;
        }

        protected override Price IndicatorFunc(MarketData mktData, DateTime updateTime, Price value)
        {
            return CalcTrend(mktData, updateTime);
        }

        public Price CalcTrend(MarketData mktData, DateTime updateTime)
        {
            if (mktData.TimeSeries.Count < 2)
                return null;
            var curVal = mktData.TimeSeries.Last().Mid();
            if (updateTime >= _nextTime)
            {
                if (_nextTime == DateTime.MinValue)
                    _nextTime = updateTime.AddSeconds(_subPeriodSeconds);
                else
                    _nextTime = _nextTime.AddSeconds(_subPeriodSeconds);
                DateTime rsiTime = _nextTime.AddSeconds(-_subPeriodSeconds);
                if (mktData.TimeSeries.StartTime() > rsiTime)
                    return null;
                if (_candles.Count == _nbPeriods)
                    _candles.RemoveAt(0);
                _curCandle = new Candle(curVal, curVal, updateTime, updateTime);
                _candles.Add(_curCandle);
            }
            else
                _curCandle = _candles.Last();

            if (curVal > _curCandle.Max)
            {
                _curCandle.Max = curVal;
                _curCandle.MaxTime = updateTime;
            }
            else if (curVal < _curCandle.Min)
            {
                _curCandle.Min = curVal;
                _curCandle.MinTime = updateTime;
            }

            if (_candles.Count < _nbPeriods)
                return null;
            /*
            List<KeyValuePair<DateTime, decimal>> centralValues = new List<KeyValuePair<DateTime, decimal>>();
            var startCenter = new KeyValuePair<DateTime, decimal>(new DateTime(_candles[0].MinTime.Ticks / 2 + _candles[0].MaxTime.Ticks / 2), (_candles[0].Min + _candles[0].Max) / 2m);
            centralValues.Add(startCenter);
            centralValues.Add(new KeyValuePair<DateTime, decimal>(new DateTime(_candles[_candles.Count / 3].MinTime.Ticks / 2 + _candles[_candles.Count / 3].MaxTime.Ticks / 2),
                                                                            (_candles[_candles.Count / 3].Min + _candles[_candles.Count / 3].Max) / 2m));
            centralValues.Add(new KeyValuePair<DateTime, decimal>(new DateTime(_candles[_candles.Count * 2 / 3].MinTime.Ticks / 2 + _candles[_candles.Count * 2 / 3].MaxTime.Ticks / 2),
                                                                            (_candles[_candles.Count * 2 / 3].Min + _candles[_candles.Count * 2 / 3].Max) / 2m));
            centralValues.Add(new KeyValuePair<DateTime, decimal>(new DateTime(_candles[_candles.Count - 2].MinTime.Ticks / 2 + _candles[_candles.Count - 2].MaxTime.Ticks / 2),
                                                                            (_candles[_candles.Count - 2].Min + _candles[_candles.Count - 2].Max) / 2m));
            decimal centralCoeff = calcDirCoeff(centralValues, updateTime) / (decimal)(updateTime - centralValues[0].Key).TotalSeconds;*/

            IEnumerable<KeyValuePair<decimal, DateTime>> topValues = null;
            IEnumerable<KeyValuePair<decimal, DateTime>> bottomValues = null;

            if (_simple)
            {
                var first = _candles.First();
                var last = _candles.Last();
                decimal timeLength = (decimal)(last.MaxTime - first.MaxTime).TotalSeconds;
                decimal topCoeff = timeLength < 1m ? 0m : (last.Max - first.Max) / (decimal)(last.MaxTime - first.MaxTime).TotalSeconds;
                timeLength = (decimal)(last.MinTime - first.MinTime).TotalSeconds;
                decimal bottomCoeff = timeLength < 1m ? 0m : (last.Min - first.Min) / (decimal)(last.MinTime - first.MinTime).TotalSeconds;
                if (topCoeff == 0m && bottomCoeff == 0m)
                    return null;
                return new Price(100m * (topCoeff + bottomCoeff) / 2m);
            }
            else
            {
                if (_candles.Count > _nbPeaks)
                {
                    topValues = searchTopValues();
                    bottomValues = searchBottomValues();
                }
                else
                {
                    var candidateTopValues = new SortedDictionary<decimal, DateTime>();
                    var candidateBottomValues = new SortedDictionary<decimal, DateTime>();
                    foreach (var candle in _candles)
                    {
                        candidateTopValues[candle.Max] = candle.MaxTime;
                        candidateBottomValues[candle.Min] = candle.MinTime;
                    }
                    topValues = candidateTopValues;
                    bottomValues = candidateBottomValues;
                }

                decimal topCoeff = calcDirCoeff(topValues, updateTime);
                decimal bottomCoeff = calcDirCoeff(bottomValues, updateTime);

                Price trend = new Price(100m * (topCoeff + bottomCoeff) / 2m);
                return trend;
            }
        }

        public bool IsMax(int nbPeriods, decimal value, decimal tolerance = 0.1m)
        {
            for (int idxCandle = _candles.Count - nbPeriods; idxCandle < _candles.Count; idxCandle++)
            {
                if (_candles[idxCandle].Max > value + tolerance)
                    return false;
            }
            return true;
        }

        public bool IsMin(int nbPeriods, decimal value, decimal tolerance = 0.1m)
        {
            for (int idxCandle = _candles.Count - nbPeriods; idxCandle < _candles.Count; idxCandle++)
            {
                if (_candles[idxCandle].Min < value - tolerance)
                    return false;
            }
            return true;
        }

        decimal calcDirCoeff(IEnumerable<KeyValuePair<decimal, DateTime>> values, DateTime updateTime)
        {
            if (values.Count() < 2)
                return 0m;
            if (values.Count() > _nbPeaks)
                values = values.Take(_nbPeaks);
            /*
            if (values.Count() > 2)
            {
                if (values.ElementAt(0).Value > values.ElementAt(2).Value && values.ElementAt(1).Value > values.ElementAt(2).Value && Math.Abs((values.ElementAt(0).Value - values.ElementAt(1).Value).TotalSeconds) > 120)
                    values = values.Take(2);
            }*/
            var V = new NRealMatrix(values.Count(), 2);
            var Vt = new NRealMatrix(values.Count(), 2);
            var Y = new NRealMatrix(values.Count(), 1);
            int idxRow = 0;
            var startTime = DateTime.MaxValue;
            var startValue = 0m;
            foreach (var kvp in values)
            {
                if (kvp.Value < startTime)
                {
                    startTime = kvp.Value;
                    startValue = kvp.Key;
                }
            }
            var interval = (updateTime - startTime);
            foreach (var keyVal in values)
            {
                V[idxRow, 0] = 1;
                V[idxRow, 1] = (double)(keyVal.Value - startTime).TotalMilliseconds / 1000.0;
                Vt[idxRow, 0] = V[idxRow, 0];
                Vt[idxRow, 1] = V[idxRow, 1];
                Y[idxRow, 0] = Convert.ToDouble(keyVal.Key - startValue) * interval.TotalSeconds;
                idxRow += 1;
            }
            Vt.Transpose();
            var VtV = Vt * V;
            var VtY = Vt * Y;

            var X = new NRealMatrix(2, 1);
            //solve VtV * X = VtY
            LapackLib.Instance.SolveSle(VtV, VtY, X);
            if (X[1, 0] == double.NaN)
            {
                Log.Instance.WriteEntry("IndicatorTrend: Invalid linear regression " + _mktData[0].Name, EventLogEntryType.Error);
                return 0m;
            }
            return Convert.ToDecimal(X[1, 0]);
        }

        IEnumerable<KeyValuePair<decimal, DateTime>> searchTopValues()
        {
            SortedDictionary<decimal, DateTime> candidates = new SortedDictionary<decimal, DateTime>();
            Candle prevCandle = null;
            Candle curCandle = null;
            Candle curMax = null;
            foreach (var candle in _candles)
            {
                if (curCandle != null)
                {
                    if (candle.Max < curCandle.Max)
                    {
                        candidates[curCandle.Max] = curCandle.MaxTime;
                    }
                    else
                    {
                        if (candle.Max > curMax.Max)
                        {
                            curMax = candle;
                        }
                    }
                }
                else
                    curMax = candle;
                prevCandle = curCandle;
                curCandle = candle;
            }
            if (curCandle.Max < prevCandle.Max)
                candidates[prevCandle.Max] = prevCandle.MaxTime;
            else
                candidates[curCandle.Max] = curCandle.MaxTime;
            return candidates.Reverse();
        }

        IEnumerable<KeyValuePair<decimal, DateTime>> searchBottomValues()
        {
            SortedDictionary<decimal, DateTime> candidates = new SortedDictionary<decimal, DateTime>();
            Candle prevCandle = null;
            Candle curCandle = null;
            Candle curMin = null;
            foreach (var candle in _candles)
            {
                if (curCandle != null)
                {
                    if (candle.Min > curCandle.Min)
                    {
                        candidates[curCandle.Min] = curCandle.MinTime;
                    }
                    else
                    {
                        if (candle.Min < curMin.Min)
                        {
                            curMin = candle;
                        }
                    }
                }
                else
                    curMin = candle;
                prevCandle = curCandle;
                curCandle = candle;
            }
            if (curCandle.Min > prevCandle.Min)
                candidates[prevCandle.Min] = prevCandle.MinTime;
            else
                candidates[curCandle.Min] = curCandle.MinTime;
            return candidates;
        }
    }

    public class CurvCandle
    {
        public bool GainAsset;
        public decimal DiffAsset;
        public decimal StartAssetValue;
        public decimal StdDevGain = 0m;
        public decimal StdDevLoss = 0m;
        public DateTime EndTime = DateTime.MaxValue;

        public CurvCandle(decimal starAssetValue, bool gain = false, decimal diffAsset = 0m)
        {
            GainAsset = gain;
            DiffAsset = diffAsset;
            StartAssetValue = starAssetValue;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class IndicatorCurve : IndicatorEMA
    {
        IndicatorTrend _trend;
        int _nbCandles = 0;
        DateTime _nextCurveTime = DateTime.MinValue;
        List<CurvCandle> _curvCandles = new List<CurvCandle>();
        CurvCandle _curCandle = null;

        public IndicatorCurve(IndicatorTrend trend)
            : base("Curve_" + trend.Period + "_" + trend.SignalStock.Id, trend.SignalStock, trend.Period / 60)
        {
            _trend = trend;
            _nbCandles = trend.NbPeriods;
            _subPeriodSeconds = trend.Period;           
        }

        protected override Price IndicatorFunc(MarketData mktData, DateTime updateTime, Price value)
        {
            return CalcCurve(mktData, updateTime);
        }

        decimal curvature(decimal x1, decimal y1, decimal x2, decimal y2, decimal x3, decimal y3)
        {
            if (Math.Abs(y2 - y1) < 0.00001m || Math.Abs(y3 - y1) < 0.00001m || Math.Abs(y2 - y3) < 0.00001m)
                return 0m;
            if (Math.Abs(x2 - x1) < 0.00001m || Math.Abs(x3 - x1) < 0.00001m || Math.Abs(x2 - x3) < 0.00001m)
                return 1m;
            decimal denom = (x1 - x2) * (x1 - x3) * (x2 - x3);
            decimal A = (x3 * (y2 - y1) + x2 * (y1 - y3) + x1 * (y3 - y2)) / denom;
            return 200m * A;
        }

        decimal calcStdDevGain()
        {
            if ((_mktData[0].TimeSeries.EndTime() - _curvCandles[_curvCandles.Count - 1].EndTime).TotalSeconds > 30)
                return _curvCandles[_curvCandles.Count - 2].StdDevGain < 0.001m ? 1000m : _curvCandles[_curvCandles.Count - 1].StdDevGain / _curvCandles[_curvCandles.Count - 2].StdDevGain;
            else
                return _curvCandles[_curvCandles.Count - 3].StdDevGain < 0.001m ? 1000m : Math.Max(_curvCandles[_curvCandles.Count - 1].StdDevGain, _curvCandles[_curvCandles.Count - 2].StdDevGain) / _curvCandles[_curvCandles.Count - 3].StdDevGain;
        }

        decimal calcStdDevLoss()
        {
            if ((_mktData[0].TimeSeries.EndTime() - _curvCandles[_curvCandles.Count - 1].EndTime).TotalSeconds > 30)
                return _curvCandles[_curvCandles.Count - 2].StdDevLoss < 0.001m ? 1000m : _curvCandles[_curvCandles.Count - 1].StdDevLoss / _curvCandles[_curvCandles.Count - 2].StdDevLoss;
            else
                return _curvCandles[_curvCandles.Count - 3].StdDevLoss < 0.001m ? 1000m : Math.Max(_curvCandles[_curvCandles.Count - 1].StdDevLoss, _curvCandles[_curvCandles.Count - 2].StdDevLoss) / _curvCandles[_curvCandles.Count - 3].StdDevLoss;
        }

        public Price CalcCurve(MarketData mktData, DateTime updateTime)
        {
            /*
            var topValues = new List<KeyValuePair<decimal, decimal>>();
            var bottomValues = new List<KeyValuePair<decimal, decimal>>();
            var timeOrig = updateTime.AddSeconds(_periodSec * _nbCandles);
            foreach (var candle in _trend.Candles)
            {
                topValues.Add(new KeyValuePair<decimal, decimal>((decimal)(candle.MaxTime - timeOrig).TotalSeconds, candle.Max));
                bottomValues.Add(new KeyValuePair<decimal, decimal>((decimal)(candle.MinTime - timeOrig).TotalSeconds, candle.Min));
            }
            var topCurve = curvature(topValues[0].Key, topValues[0].Value, topValues[1].Key, topValues[1].Value, topValues[2].Key, topValues[2].Value);
            var bottomCurve = curvature(topValues[0].Key, topValues[0].Value, topValues[1].Key, topValues[1].Value, topValues[2].Key, topValues[2].Value);

            return new Price((topCurve + bottomCurve) / 2m);*/

            if (updateTime >= _nextCurveTime)
            {
                if (mktData.TimeSeries.Count < 2)
                    return null;
                if (_nextCurveTime == DateTime.MinValue)
                    _nextCurveTime = updateTime.AddSeconds(_subPeriodSeconds);
                else
                    _nextCurveTime = _nextCurveTime.AddSeconds(_subPeriodSeconds);
                DateTime rsiTime = _nextCurveTime.AddSeconds(-_subPeriodSeconds);
                if (mktData.TimeSeries.StartTime() > rsiTime)
                    return null;
                if (_curvCandles.Count == _nbCandles)
                    _curvCandles.RemoveAt(0);
                if (_curvCandles.Count > 0)
                {
                    _curCandle.EndTime = updateTime;
                    _curCandle = new CurvCandle(mktData.TimeSeries.Last().Mid());
                }
                else
                    _curCandle = new CurvCandle(mktData.TimeSeries.Last().Mid());
                _curvCandles.Add(_curCandle);
            }
            else
                _curCandle = _curvCandles.Last();
            DateTime startTime = _nextCurveTime.AddSeconds(-_subPeriodSeconds);
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
            var nbGain = 0;
            var nbLoss = 0;
            foreach (var candle in _curvCandles)
            {
                if (candle.GainAsset)
                {
                    sumGain += candle.DiffAsset;
                    nbGain++;
                }
                else
                {
                    sumLosses += candle.DiffAsset;
                    nbLoss++;
                }
            }
            var avgGain = nbGain == 0 ? 0 : sumGain / nbGain;
            var avgLoss = nbLoss == 0 ? 0 : sumLosses / nbLoss;
            var stdDevGain = 0m;
            var stdDevLoss = 0m;
            foreach (var candle in _curvCandles)
            {
                if (candle.GainAsset)
                    stdDevGain += (decimal)Math.Pow((double)(candle.DiffAsset - avgGain), 2.0);
                else
                    stdDevLoss += (decimal)Math.Pow((double)(candle.DiffAsset - avgLoss), 2.0);
            }
            _curCandle.StdDevGain = nbGain == 0 ? 0m : (decimal)Math.Sqrt((double)stdDevGain / nbGain);
            _curCandle.StdDevLoss = nbLoss == 0 ? 0m : (decimal)Math.Sqrt((double)stdDevLoss / nbLoss);

            decimal curv = Math.Abs(sumLosses) < 0.1m ? (Math.Abs(sumGain) < 0.1m ? 1m : 1000m) : sumGain / sumLosses;
            if (curv > 1m)
                curv = 1m / curv;
            return new Price(100m * curv);
        }
    }
}
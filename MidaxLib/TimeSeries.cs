using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class TimeSeries
    {
        const int DEFAULT_MAX_RECORD_TIME_HOURS = 2;
        int _maxRecordTime = DEFAULT_MAX_RECORD_TIME_HOURS;
        public int MaxTimeHours
        {
            get { return _maxRecordTime; }
            set { _maxRecordTime = value; }
        }
        const int DEFAULT_INTERVAL_MINUTES = 20;
        int _tsInterval = DEFAULT_INTERVAL_MINUTES;
        public int IntervalMinutes
        {
            get { return _tsInterval; }
            set { _tsInterval = value; }
        }

        public TimeSeries()
        {
            if (Config.Settings.ContainsKey("TIMESERIES_MAX_RECORD_TIME_HOURS"))
                _maxRecordTime = int.Parse(Config.Settings["TIMESERIES_MAX_RECORD_TIME_HOURS"]);
        }

        List<List<KeyValuePair<DateTime, Price>>> _series = new List<List<KeyValuePair<DateTime, Price>>>();
        DateTime _latest = DateTime.MinValue;
            
        public void Add(DateTime updateTime, Price price)
        {
            if (updateTime < _latest)
                throw new ApplicationException("Time series do not accept values in the past");
            else if (updateTime == _latest)
            {
                if (_series.Last().Last().Key != updateTime)
                    throw new ApplicationException("Time series is inconsistent");
                _series.Last()[_series.Last().Count - 1] = new KeyValuePair<DateTime, Price>(updateTime, price);
            }
            else
            {
                if (_series.Count == 0)
                    _series.Add(new List<KeyValuePair<DateTime, Price>>());
                else if (lastIntervalMinutes(updateTime) > _tsInterval)
                {
                    deleteOldData(updateTime);
                    _series.Add(new List<KeyValuePair<DateTime, Price>>());
                }
                if (updateTime > _latest)
                    _latest = updateTime;
                //else
                //    updateTime = _latest;
                _series.Last().Add(new KeyValuePair<DateTime, Price>(updateTime, price));
            }
        }

        double lastIntervalMinutes(DateTime updateTime)
        {
            return (updateTime - _series.Last().First().Key).TotalMinutes;
        }
        
        public double TotalMinutes(DateTime updateTime)
        {
            if (_series.Count == 0)
                return 0.0;
            return (updateTime - _series.First().First().Key).TotalMinutes;
        }
        
        public virtual IEnumerable<KeyValuePair<DateTime, Price>> ValueGenerator(DateTime timeStart, DateTime timeEnd, bool startAfter)
        {
            if (_series.Count == 0)
                yield break;
            if (timeEnd < _series.First().First().Key || timeStart > _series.Last().Last().Key)
                yield break;
            if (timeStart < _series.First().First().Key)
                timeStart = _series.First().First().Key;
            int curIdx = _series.Count - 1;
            int curPos = 0;
            for (int idx = 0; idx < curIdx; idx++)
            {
                if (timeStart < _series[idx + 1].First().Key)
                {
                    KeyValuePair<DateTime, Price>? start = ClosestValue(idx, timeStart);
                    if (!start.HasValue)
                        break;
                    curPos = _series[idx].IndexOf(start.Value);
                    if (startAfter && start.Value.Key <= timeStart)
                        curPos += 1;                  
                    curIdx = idx;
                    break;
                }
            }
            if (curIdx == _series.Count - 1)
            {
                if (timeStart <= _series[curIdx].Last().Key)
                {
                    KeyValuePair<DateTime, Price>? start = ClosestValue(curIdx, timeStart);
                    if (!start.HasValue)
                        yield break;    
                    curPos = _series[curIdx].IndexOf(start.Value);
                    if (startAfter && start.Value.Key <= timeStart)
                        curPos += 1;
                }
                else
                    yield break;
            }
            while(curIdx < _series.Count)
            {
                while(curPos < _series[curIdx].Count)
                {                    
                    var curElt = _series[curIdx][curPos];
                    if (curElt.Key > timeEnd)
                        yield break;
                    yield return curElt;
                    curPos = curPos + 1;
                }
                curPos = 0;
                curIdx = curIdx + 1;
            }
            yield break;
        }

        public decimal Min(DateTime timeStart, DateTime timeEnd)
        {
            decimal minVal = decimal.MaxValue;
            foreach (var val in ValueGenerator(timeStart, timeEnd, false))
            {
                if (val.Value.Mid() < minVal)
                    minVal = val.Value.Mid();
            }
            return minVal;
        }

        public decimal Max(DateTime timeStart, DateTime timeEnd)
        {
            decimal maxVal = decimal.MinValue;
            foreach (var val in ValueGenerator(timeStart, timeEnd, false))
            {
                if (val.Value.Mid() > maxVal)
                    maxVal = val.Value.Mid();
            }
            return maxVal;
        }

        class SeriesComparer : IComparer<KeyValuePair<DateTime, Price>>
        {
            int IComparer<KeyValuePair<DateTime, Price>>.Compare(KeyValuePair<DateTime, Price> x, KeyValuePair<DateTime, Price> y)
            {
                return x.Key.CompareTo(y.Key);
            }
        }
        static SeriesComparer _seriesComparer = new SeriesComparer();

        public KeyValuePair<DateTime, Price>? ClosestValue(int seriesIdx, DateTime time)
        {
            int closestIndex = _series[seriesIdx].BinarySearch(new KeyValuePair<DateTime, Price>(time, null), _seriesComparer);
            if (closestIndex < 0)
                closestIndex = ~closestIndex - 1;
            if (closestIndex == -1)
                return null;
            return _series[seriesIdx][closestIndex];
        }

        public KeyValuePair<DateTime, Price>? NextValue(int seriesIdx, DateTime time)
        {
            int closestIndex = _series[seriesIdx].BinarySearch(new KeyValuePair<DateTime, Price>(time, null), _seriesComparer);
            if (closestIndex < 0)
                closestIndex = ~closestIndex - 1;
            if (closestIndex == -1)
                return null;
            if (closestIndex < _series[seriesIdx].Count - 1)
                return _series[seriesIdx][closestIndex + 1];
            else if (seriesIdx < _series.Count - 1)
                return _series[seriesIdx + 1][0];
            else
                return null;
        }

        public KeyValuePair<DateTime, Price>? PrevValue(int seriesIdx, DateTime time)
        {
            int closestIndex = _series[seriesIdx].BinarySearch(new KeyValuePair<DateTime, Price>(time, null), _seriesComparer);
            if (closestIndex < 0)
                closestIndex = ~closestIndex - 1;
            if (closestIndex == -1)
                return null;
            if (closestIndex > 0)
                return _series[seriesIdx][closestIndex - 1];
            else if (seriesIdx > 0)
                return _series[seriesIdx - 1].Last();
            else
                return null;
        }

        public KeyValuePair<DateTime, Price>? Value(DateTime time)
        {
            if (_series.Count == 0)
                return null;
            for (int idx = 0; idx < _series.Count - 1; idx++)
            {
                if (time < _series[idx + 1].First().Key)
                    return ClosestValue(idx, time);                 
            }
            return ClosestValue(_series.Count - 1, time);
        }

        public KeyValuePair<DateTime, Price>? PrevValue(DateTime time)
        {
            var val = Value(time);
            if (!val.HasValue)
                return null;
            return Prev(val.Value.Key);
        }

        public KeyValuePair<DateTime, Price>? Next(DateTime time)
        {
            if (_series.Count == 0)
                return null;
            for (int idx = 0; idx < _series.Count - 1; idx++)
            {
                if (time < _series[idx + 1].First().Key)
                    return NextValue(idx, time);
            }
            return NextValue(_series.Count - 1, time);
        }

        public KeyValuePair<DateTime, Price>? Prev(DateTime time)
        {
            if (_series.Count == 0)
                return null;
            for (int idx = 0; idx < _series.Count - 1; idx++)
            {
                if (time < _series[idx + 1].First().Key)
                    return PrevValue(idx, time);
            }
            return PrevValue(_series.Count - 1, time);
        }

        public List<KeyValuePair<DateTime, Price>> Values(DateTime endTime, TimeSpan interval, bool startAfter)
        {
            if (_series.Count == 0)
                return null;
            if (endTime < _series.First().First().Key)
                return null;
            DateTime startTime = endTime - interval;
            if (startTime < _series.First().First().Key)
                return null;
            return ValueGenerator(startTime, endTime, startAfter).ToList(); 
        }

        public virtual KeyValuePair<DateTime, Price>? this[DateTime time]
        {
            get { return Value(time); }
        }

        public int Count
        {
            get { return (from List<KeyValuePair<DateTime, Price>> dic in _series select dic.Count).Sum(); }
        }

        public Price First()
        {
            return _series.First().First().Value;
        }

        public Price Last()
        {
            return _series.Last().Last().Value;
        }

        public DateTime StartTime()
        {
            return _series.Count == 0 ? DateTime.MaxValue : _series.First().First().Key;
        }

        public DateTime EndTime()
        {
            return _series.Count == 0 ? DateTime.MinValue : _series.Last().Last().Key;
        }

        public bool Empty()
        {
            if (_series.Count == 0)
                return true;
            return _series.First().Count == 0;
        }

        void deleteOldData(DateTime updateTime)
        {
            while (_series.Count > 1)
            {
                if ((updateTime - _series.First().First().Key).TotalHours > _maxRecordTime)
                    _series.RemoveAt(0);
                else
                    break;
            }
        }
    }

    public class TimeSeriesConstant : TimeSeries
    {
        Price _val;
        
        public TimeSeriesConstant(decimal val)
        {
            _val = new Price(val);
        }

        public override KeyValuePair<DateTime, Price>? this[DateTime time]
        {
            get { return new KeyValuePair<DateTime, Price>(time, _val); }
        }

        public override IEnumerable<KeyValuePair<DateTime, Price>> ValueGenerator(DateTime timeStart, DateTime timeEnd, bool startAfter)
        {
            while (true)
                yield return new KeyValuePair<DateTime, Price>(timeEnd, _val);
        }
    }
}

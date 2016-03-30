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
                else
                    updateTime = _latest;
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
        
        public IEnumerable<KeyValuePair<DateTime, Price>> ValueGenerator(DateTime timeStart, DateTime timeEnd)
        {
            if (_series.Count == 0)
                yield break;
            if (timeEnd < _series.First().First().Key || timeStart > _series.Last().Last().Key)
                yield break;
            if (timeStart < _series.First().First().Key)
                timeStart = _series.First().First().Key;
            DateTime curTime = DateTime.MinValue;
            int curIdx = _series.Count - 1;
            int curPos = 0;
            for (int idx = 0; idx < curIdx; idx++)
            {
                if (timeStart < _series[idx + 1].First().Key)
                {
                    KeyValuePair<DateTime, Price> start = ClosestValue(idx, timeStart);
                    curPos = _series[idx].IndexOf(start);
                    curTime = start.Key;                    
                    curIdx = idx;
                    break;
                }
            }
            if (curIdx == _series.Count - 1)
            {
                if (timeStart <= _series[curIdx].Last().Key)
                {
                    KeyValuePair<DateTime, Price> start = ClosestValue(curIdx, timeStart);
                    curPos = _series[curIdx].IndexOf(start);
                    curTime = start.Key;
                }
                else
                    yield break;
            }
            while(curIdx < _series.Count)
            {
                while(curPos < _series[curIdx].Count)
                {                    
                    curTime = _series[curIdx][curPos].Key;
                    if (curTime > timeEnd)
                        yield break;
                    yield return _series[curIdx][curPos];
                    curPos = curPos + 1;
                }
                curPos = 0;
                curIdx = curIdx + 1;
            }
            yield break;
        }

        class SeriesComparer : IComparer<KeyValuePair<DateTime, Price>>
        {
            int IComparer<KeyValuePair<DateTime, Price>>.Compare(KeyValuePair<DateTime, Price> x, KeyValuePair<DateTime, Price> y)
            {
                return x.Key.CompareTo(y.Key);
            }
        }
        static SeriesComparer _seriesComparer = new SeriesComparer();

        public KeyValuePair<DateTime, Price> ClosestValue(int seriesIdx, DateTime time)
        {
            int closestIndex = _series[seriesIdx].BinarySearch(new KeyValuePair<DateTime, Price>(time, null), _seriesComparer);
            if (closestIndex < 0)
                closestIndex = ~closestIndex - 1;
            return _series[seriesIdx][closestIndex];
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

        public List<KeyValuePair<DateTime, Price>> Values(DateTime endTime, TimeSpan interval)
        {
            if (_series.Count == 0)
                return null;
            if (endTime < _series.First().First().Key)
                return null;
            DateTime startTime = endTime - interval;
            if (startTime < _series.First().First().Key)
                return null;
            return ValueGenerator(startTime, endTime).ToList(); 
        }

        public KeyValuePair<DateTime, Price>? this[DateTime time]
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
}

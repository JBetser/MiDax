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
                _latest = updateTime > _latest ? updateTime : _latest;
                _series.Last().Add(new KeyValuePair<DateTime, Price>(updateTime > _latest ? updateTime : _latest, price));
            }
        }

        double lastIntervalMinutes(DateTime updateTime)
        {
            return (updateTime - _series.Last().First().Key).TotalMinutes;
        }
        
        public double TotalMinutes(DateTime updateTime)
        {
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
            int curIdx = 0;
            int curPos = 0;
            for (int idx = 0; idx < _series.Count; idx++)
            {
                if (timeStart <= _series[idx].Last().Key)
                {
                    KeyValuePair<DateTime, Price> start = _series[idx].LastOrDefault(keyValue => keyValue.Key <= timeStart);
                    KeyValuePair<DateTime, Price> defaultVal = default(KeyValuePair<DateTime, Price>);
                    if (start.Key == defaultVal.Key && start.Value == defaultVal.Value)
                    {
                        start = _series[idx - 1].Last();
                        curPos = 0;
                    }
                    else
                        curPos = _series[idx].IndexOf(start);
                    curTime = start.Key;                    
                    curIdx = idx;
                    break;
                }
            }
            if (curTime == DateTime.MinValue)
                yield break;
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

        public KeyValuePair<DateTime, Price>? Value(DateTime time)
        {
            IEnumerable<KeyValuePair<DateTime, Price>> valueEnum = ValueGenerator(time, time);
            KeyValuePair<DateTime, Price>[] valueArray = valueEnum.ToArray();
            if (valueArray.Length != 1)
                return null;
            return new KeyValuePair<DateTime, Price>(time, valueArray[0].Value);
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
            bool started = false;
            List<KeyValuePair<DateTime, Price>> values = null;
            for (int idx = 0; idx < _series.Count; idx++)
            {
                if (started)
                {
                    if (endTime <= _series[idx].Last().Key)
                    {
                        values.AddRange(from keyval in _series[idx]
                                          where keyval.Key <= endTime
                                          select keyval);
                        break;
                    }
                    else
                    {
                        values.AddRange(from keyval in _series[idx] select keyval);
                    }
                }
                else
                {
                    if (startTime <= _series[idx].Last().Key)
                    {
                        values = (from keyval in _series[idx]
                                  where keyval.Key >= startTime && keyval.Key <= endTime
                                  select keyval).ToList();
                        if (endTime <= _series[idx].Last().Key)
                            break;
                        started = true;
                    }
                }
            }
            return values;
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

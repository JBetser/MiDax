using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class TimeSeries
    {
        const int MAX_RECORD_TIME_HOURS = 2;
        const int TIMESERIES_INTERVAL_MINUTES = 5;

        List<List<KeyValuePair<DateTime, Price>>> _series = new List<List<KeyValuePair<DateTime, Price>>>();
        DateTime _latest = DateTime.MinValue;
            
        public void Add(DateTime updateTime, Price price)
        {
            if (updateTime <= _latest)
                throw new ApplicationException("Time series do not accept values in the past");
            _latest = updateTime;
            if (_series.Count == 0)
                _series.Add(new List<KeyValuePair<DateTime, Price>>());
            else if ((updateTime - _series[_series.Count - 1][_series[_series.Count - 1].Count - 1].Key).Minutes > TIMESERIES_INTERVAL_MINUTES)
            {
                deleteOldData(updateTime);
                _series.Add(new List<KeyValuePair<DateTime, Price>>());
            }
            _series[_series.Count - 1].Add(new KeyValuePair<DateTime, Price>(updateTime, price));            
        }

        public IEnumerable<KeyValuePair<DateTime, Price>> ValueGenerator(DateTime timeStart, DateTime timeEnd)
        {
            if (_series.Count == 0)
                yield break;
            if (timeEnd < _series[0][0].Key || timeStart > _series[_series.Count - 1][_series[_series.Count - 1].Count - 1].Key)
                yield break;
            if (timeStart < _series[0][0].Key)
                timeStart = _series[0][0].Key;
            DateTime curTime = DateTime.MinValue;
            int curIdx = 0;
            int curPos = 0;
            for (int idx = 0; idx < _series.Count; idx++)
            {
                if (timeStart <= _series[idx][_series[idx].Count - 1].Key)
                {
                    KeyValuePair<DateTime, Price> start = _series[idx].Last(keyValue => keyValue.Key <= timeStart);
                    curTime = start.Key;
                    curPos = _series[idx].IndexOf(start);
                    curIdx = idx;
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
            if (endTime < _series[0][0].Key)
                return null;
            DateTime startTime = endTime - interval;
            if (startTime < _series[0][0].Key)
                return null;
            bool started = false;
            List<KeyValuePair<DateTime, Price>> values = null;
            for (int idx = 0; idx < _series.Count; idx++)
            {
                if (started)
                {
                    if (endTime <= _series[idx][_series[idx].Count - 1].Key)
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
                    if (startTime <= _series[idx][_series[idx].Count - 1].Key)
                    {
                        values = (from keyval in _series[idx]
                                  where keyval.Key >= startTime && keyval.Key <= endTime
                                  select keyval).ToList();
                        if (endTime <= _series[idx][_series[idx].Count - 1].Key)
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

        void deleteOldData(DateTime updateTime)
        {
            while (_series.Count > 0)
            {
                if ((updateTime - _series[0][0].Key).Hours > MAX_RECORD_TIME_HOURS)
                    _series.RemoveAt(0);
                else
                    break;
            }
        }
    }
}

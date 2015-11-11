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
        const int TIMESERIES_INTERVAL_MINUTES = 10;

        List<SortedList<DateTime, Price>> _series = new List<SortedList<DateTime, Price>>();

        public void Add(DateTime updateTime, Price price)
        {
            if (_series.Count == 0)
                _series.Add(new SortedList<DateTime, Price>());
            else if ((updateTime - _series[_series.Count - 1].Keys.ElementAt(_series[_series.Count - 1].Count - 1)).Minutes > TIMESERIES_INTERVAL_MINUTES)
            {
                deleteOldData(updateTime);
                _series.Add(new SortedList<DateTime, Price>());
            }
            _series[_series.Count - 1].Add(updateTime, price);            
        }

        public KeyValuePair<DateTime, Price>? Value(DateTime time, ref int index)
        {
            if (_series.Count == 0)
                return null;
            if (time < _series[0].Keys.ElementAt(0))
                return null;
            for (int idx = index; idx < _series.Count; idx++)
            {
                if (time < _series[idx].Keys.ElementAt(0))
                {
                    if (idx == 0)
                        return null;
                    index = idx;
                    return _series[idx - 1].Last(keyValue => keyValue.Key <= time);
                }
                else
                {
                    if (time <= _series[idx].Keys.ElementAt(_series[idx].Keys.Count - 1))
                    {
                        index = idx;
                        return _series[idx].Last(keyValue => keyValue.Key <= time);
                    }
                }
            }
            return null;
        }

        public KeyValuePair<DateTime, Price>? Value(DateTime time)
        {
            int index = 0;
            return Value(time, ref index);
        }

        public List<KeyValuePair<DateTime, Price>> Values(DateTime endTime, TimeSpan interval)
        {
            if (_series.Count == 0)
                return null;
            if (endTime < _series[0].Keys.ElementAt(0))
                return null;
            DateTime startTime = endTime - interval;
            if (startTime < _series[0].Keys.ElementAt(0))
                return null;
            bool started = false;
            List<KeyValuePair<DateTime, Price>> values = null;
            for (int idx = 0; idx < _series.Count; idx++)
            {
                if (started)
                {
                    if (endTime <= _series[idx].Keys.ElementAt(_series[idx].Keys.Count - 1))
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
                    if (startTime <= _series[idx].Keys.ElementAt(_series[idx].Keys.Count - 1))
                    {
                        values = (from keyval in _series[idx]
                                  where keyval.Key >= startTime && keyval.Key <= endTime
                                  select keyval).ToList();
                        if (endTime <= _series[idx].Keys.ElementAt(_series[idx].Keys.Count - 1))
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
            get { return (from SortedList<DateTime, Price> dic in _series select dic.Count).Sum(); }
        }

        void deleteOldData(DateTime updateTime)
        {
            while (_series.Count > 0)
            {
                if ((updateTime - _series[0].Keys.ElementAt(_series[0].Count - 1)).Hours > MAX_RECORD_TIME_HOURS)
                    _series.RemoveAt(0);
                else
                    break;
            }
        }
    }
}

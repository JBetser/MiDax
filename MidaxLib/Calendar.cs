using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public class Calendar
    {
        Dictionary<string, List<KeyValuePair<DateTime,string>>> _events;

        public Calendar(DateTime date)
        {
            string folder = (string)Config.Settings["CALENDAR_PATH"];
            int time_offset = int.Parse(Config.Settings["TIME_GMT"]);
            int day = date.Day - (int)date.DayOfWeek;
            int month = date.Month;
            int year = date.Year;
            if (day <= 0)
            {
                month -= 1;
                if (month == 0)
                {
                    month = 12;
                    year -= 1;
                }
                day = DateTime.DaysInMonth(year, month) + day;  
            }
            StreamReader csvReader = new StreamReader(File.OpenRead(folder + string.Format("\\Calendar-{0:00}-{1:00}-{2}.csv", month, day, year)));
            _events = new Dictionary<string, List<KeyValuePair<DateTime, string>>>();
            DateTime? curDate = null;
            while (!csvReader.EndOfStream)
            {
                var line = csvReader.ReadLine();
                if (line == "")
                    continue;                
                string curDateStr = line.Split(',')[0];
                if (curDateStr == "")
                {
                    if (curDate == null)
                        continue;
                }
                else {
                    DateTime nextDate;
                    if (!DateTime.TryParse(line.Split(' ')[0] + "-" + date.Year, out nextDate))
                        continue;
                    curDate = nextDate;
                }
                string curCcy = line.Split(',')[1];
                if (!_events.ContainsKey(curCcy))
                    _events[curCcy] = new List<KeyValuePair<DateTime, string>>();
                string dateTimeStr = line.Split(',')[3];
                if (dateTimeStr == "")
                    continue;
                string eventName = "";
                if (line.Contains("\""))
                    eventName = line.Split('\"')[1];
                else
                    eventName = line.Split(',')[2];
                line = line.Replace(eventName, "");
                var eventDateTime = Config.ParseDateTimeLocal(curDate.Value.Day + "-" + curDate.Value.Month + "-" + curDate.Value.Year + " " + line.Split(',')[3] + ":00");
                eventDateTime = eventDateTime.AddHours(time_offset);           
                _events[curCcy].Add(new KeyValuePair<DateTime, string>(eventDateTime, eventName));
            }
        }

        public bool IsNearEvent(string ccyPair, DateTime curTime, ref string eventName)
        {
            string ccy1 = ccyPair.Substring(0, 3);
            string ccy2 = ccyPair.Substring(3, 3);
            var events = new List<KeyValuePair<DateTime, string>>();
            if (_events.ContainsKey(ccy1))
                events.AddRange(_events[ccy1]);
            if (_events.ContainsKey(ccy2))
                events.AddRange(_events[ccy2]);
            foreach (var ev in events)
            {
                if ((ev.Key >= curTime && (ev.Key - curTime).TotalSeconds < 40.0 * 60.0) || (ev.Key <= curTime && (curTime - ev.Key).TotalSeconds < 20.0 * 60.0))
                {
                    eventName = ev.Value;
                    return true;
                }
            }
            return false;
        }
    }
}

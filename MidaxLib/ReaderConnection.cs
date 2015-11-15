using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    interface IReaderConnection
    {
        List<CqlQuote> GetMarketDataQuotes(DateTime startTime, DateTime stopTime, string type, string id);
        List<CqlQuote> GetIndicatorDataQuotes(DateTime startTime, DateTime stopTime, string type, string id);
        List<CqlQuote> GetSignalDataQuotes(DateTime startTime, DateTime stopTime, string type, string id);
    }

    public class CsvReader : IReaderConnection
    {
        string _csvFile = null;
        StreamReader _csvReader;

        public CsvReader(string csv)
        {
            _csvFile = csv;
            _csvReader = new StreamReader(File.OpenRead(_csvFile));
        }

        delegate void funcReadData(List<CqlQuote> quotes, string[] values);

        List<CqlQuote> getRows(DateTime startTime, DateTime stopTime, string type, string id, funcReadData readData)
        {
            List<CqlQuote> quotes = new List<CqlQuote>();
            while (!_csvReader.EndOfStream)
            {
                var line = _csvReader.ReadLine();
                if (line == "")
                    break;
                var values = line.Split(',');
                bool empty = true;
                foreach (var val in values)
                {
                    if (val != "")
                    {
                        empty = false;
                        break;
                    }
                }
                if (empty)
                    break;
                DateTime curTime = DateTime.SpecifyKind(DateTime.Parse(values[2]), DateTimeKind.Local);
                if (!values[1].StartsWith("WMA_1D")) // Do not check time for daily market data
                {
                    if (curTime < startTime)
                        continue;
                    if (curTime > stopTime)
                        continue;
                }
                readData(quotes, values);
            }
            return quotes;
        }

        void readMarketData(List<CqlQuote> quotes, string[] values)
        {
            decimal? bid = values.Length >= 5 ? (decimal)double.Parse(values[4]) : default(decimal?);
            decimal? offer = values.Length >= 6 ? (decimal)double.Parse(values[5]) : default(decimal?);
            int? volume = values.Length >= 7 ? int.Parse(values[6]) : default(int?);
            quotes.Add(new CqlQuote(values[1], DateTimeOffset.Parse(values[2]), values[3], bid, offer, volume)); 
        }

        void readIndicatorData(List<CqlQuote> quotes, string[] values)
        {
            decimal? value = values.Length >= 2 ? (decimal)double.Parse(values[3]) : default(decimal?);
            quotes.Add(new CqlQuote(values[1], DateTimeOffset.Parse(values[2]), values[1], value, value, 0));
        }

        void readSignalData(List<CqlQuote> quotes, string[] values)
        {
            decimal? value = values.Length >= 2 ? (decimal)double.Parse(values[3]) : default(decimal?);
            quotes.Add(new CqlQuote(values[1], DateTimeOffset.Parse(values[2]), values[1], value, value, 0));
        }

        List<CqlQuote> IReaderConnection.GetMarketDataQuotes(DateTime startTime, DateTime stopTime, string type, string id)
        {
            return getRows(startTime, stopTime, type, id, readMarketData);
        }

        List<CqlQuote> IReaderConnection.GetIndicatorDataQuotes(DateTime startTime, DateTime stopTime, string type, string id)
        {
            return getRows(startTime, stopTime, type, id, readIndicatorData);
        }

        List<CqlQuote> IReaderConnection.GetSignalDataQuotes(DateTime startTime, DateTime stopTime, string type, string id)
        {
            return getRows(startTime, stopTime, type, id, readSignalData);
        }
    }
}

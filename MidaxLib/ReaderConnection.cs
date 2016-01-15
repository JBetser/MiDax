using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MidaxLib
{
    public interface IReaderConnection
    {
        List<CqlQuote> GetMarketDataQuotes(DateTime startTime, DateTime stopTime, string type, string id);
        List<CqlQuote> GetIndicatorDataQuotes(DateTime startTime, DateTime stopTime, string type, string id);
        List<CqlQuote> GetSignalDataQuotes(DateTime startTime, DateTime stopTime, string type, string id);
        List<Trade> GetTrades(DateTime startTime, DateTime stopTime, string type, string id);
        List<KeyValuePair<DateTime, double>> GetProfits(DateTime startTime, DateTime stopTime, string type, string id);
        void CloseConnection();
        MarketLevels? GetMarketLevels(DateTime updateTime, string epic);        
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

        delegate void funcReadData<T>(List<T> data, string[] values);

        List<T> getRows<T>(DateTime startTime, DateTime stopTime, string type, string id, funcReadData<T> readData, int idxTime = 2)
        {
            var data = new List<T>();
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
                if ((type == PublisherConnection.DATATYPE_STOCK || 
                    type == PublisherConnection.DATATYPE_INDICATOR || 
                    type == PublisherConnection.DATATYPE_SIGNAL) && !values[1].EndsWith(id))
                    continue;
                DateTime curTime = Config.ParseDateTimeLocal(values[idxTime]);
                if (!values[1].StartsWith("WMA_1D") && !values[1].StartsWith("LVL")) // Do not check time for daily market data
                {
                    if (curTime < startTime)
                        continue;
                    if (curTime > stopTime)
                        continue;
                }
                readData(data, values);
            }
            return data;
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
            decimal? value = (decimal)double.Parse(values[4]);
            decimal? stockvalue = (decimal)double.Parse(values[5]);
            quotes.Add(new CqlQuote(values[1], DateTimeOffset.Parse(values[2]), values[1], value, stockvalue, 0));
        }

        void readTradeData(List<Trade> trades, string[] values)
        {
            Trade trade = new Trade(Config.ParseDateTimeLocal(values[7]), values[6], (SIGNAL_CODE)Enum.Parse(typeof(SIGNAL_CODE), values[3]), int.Parse(values[5]), (decimal)double.Parse(values[4]));
            trade.Reference = values[1];
            trades.Add(trade);
        }

        void readProfitData(List<KeyValuePair<DateTime,double>> profits, string[] values)
        {
            profits.Add(new KeyValuePair<DateTime,double>(Config.ParseDateTimeLocal(values[0]), double.Parse(values[1])));
        }

        List<CqlQuote> IReaderConnection.GetMarketDataQuotes(DateTime startTime, DateTime stopTime, string type, string id)
        {
            _csvReader.Close();
            _csvReader = new StreamReader(File.OpenRead(_csvFile));
            return getRows<CqlQuote>(startTime, stopTime, type, id, readMarketData);
        }

        List<CqlQuote> IReaderConnection.GetIndicatorDataQuotes(DateTime startTime, DateTime stopTime, string type, string id)
        {
            return getRows<CqlQuote>(startTime, stopTime, type, id, readIndicatorData);
        }

        List<CqlQuote> IReaderConnection.GetSignalDataQuotes(DateTime startTime, DateTime stopTime, string type, string id)
        {
            return getRows<CqlQuote>(startTime, stopTime, type, id, readSignalData);
        }

        List<Trade> IReaderConnection.GetTrades(DateTime startTime, DateTime stopTime, string type, string id)
        {
            return getRows<Trade>(startTime, stopTime, type, id, readTradeData);
        }

        List<KeyValuePair<DateTime, double>> IReaderConnection.GetProfits(DateTime startTime, DateTime stopTime, string type, string id)
        {
            return getRows<KeyValuePair<DateTime, double>>(startTime, stopTime, type, id, readProfitData, 0);
        }

        void IReaderConnection.CloseConnection()
        {
            _csvReader.Close();
        }

        MarketLevels? IReaderConnection.GetMarketLevels(DateTime updateTime, string epic)
        {
            return new MarketLevels(epic, 10200m, 12500m, 11000m, 11100m);
        }
    }
}

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
        Dictionary<string,List<CqlQuote>> GetMarketDataQuotes(DateTime startTime, DateTime stopTime, string type, List<string> ids);
        Dictionary<string,List<CqlQuote>> GetIndicatorDataQuotes(DateTime startTime, DateTime stopTime, string type, List<string> ids);
        Dictionary<string,List<CqlQuote>> GetSignalDataQuotes(DateTime startTime, DateTime stopTime, string type, List<string> ids);
        Dictionary<string,List<Trade>> GetTrades(DateTime startTime, DateTime stopTime, string type, List<string> ids);
        Dictionary<string,List<KeyValuePair<DateTime, double>>> GetProfits(DateTime startTime, DateTime stopTime, string type, List<string> ids);
        Dictionary<string, MarketLevels> GetMarketLevels(DateTime updateTime, List<string> ids);      
        void CloseConnection();        
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

        Dictionary<string,List<T>> getRows<T>(DateTime startTime, DateTime stopTime, string type, List<string> ids, funcReadData<T> readData, int idxTime = 2)
        {
            var data = new Dictionary<string,List<T>>();
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
                var curid = "";
                foreach (var id in ids)
                {
                    if (values[1].EndsWith(id))
                    {
                        curid = id;
                        break;
                    }
                }
                if ((type == PublisherConnection.DATATYPE_STOCK || 
                    type == PublisherConnection.DATATYPE_INDICATOR ||
                    type == PublisherConnection.DATATYPE_SIGNAL ||
                    type == PublisherConnection.DATATYPE_TRADE) && curid == "")
                    continue;
                if (type != PublisherConnection.DATATYPE_MARKETLEVELS)
                {
                    DateTime curTime = Config.ParseDateTimeLocal(values[idxTime]);
                    if (!values[1].StartsWith("WMA_1D") && !values[1].StartsWith("LVL")) // Do not check time for daily market data
                    {
                        if (curTime < startTime)
                            continue;
                        if (curTime > stopTime)
                            continue;
                    }
                }
                if (!data.ContainsKey(curid))
                    data[curid] = new List<T>();
                readData(data[curid], values);
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
            Trade trade = new Trade(Config.ParseDateTimeLocal(values[7]), values[1], (SIGNAL_CODE)Enum.Parse(typeof(SIGNAL_CODE), values[4]), int.Parse(values[6]), (decimal)double.Parse(values[5]));
            trade.ConfirmationTime = Config.ParseDateTimeLocal(values[2]);
            trade.Reference = values[3];
            trades.Add(trade);
        }

        void readProfitData(List<KeyValuePair<DateTime,double>> profits, string[] values)
        {
            profits.Add(new KeyValuePair<DateTime,double>(Config.ParseDateTimeLocal(values[0]), double.Parse(values[1])));
        }

        void readMarketLevelData(List<MarketLevels> mktLevels, string[] values)
        {
            if (mktLevels.Count == 0)
                mktLevels.Add(new MarketLevels(values[1].Split('_')[1], 0m, 0m, 0m, 0m));
            if (values[1].Contains("Low"))
                mktLevels[0] = new MarketLevels(mktLevels[0].AssetId, (decimal)double.Parse(values[2]), mktLevels[0].High, mktLevels[0].CloseBid, mktLevels[0].CloseOffer);
            else if (values[1].Contains("High"))
                mktLevels[0] = new MarketLevels(mktLevels[0].AssetId, mktLevels[0].Low, (decimal)double.Parse(values[2]), mktLevels[0].CloseBid, mktLevels[0].CloseOffer);
            else if (values[1].Contains("CloseBid"))
                mktLevels[0] = new MarketLevels(mktLevels[0].AssetId, mktLevels[0].Low, mktLevels[0].High, (decimal)double.Parse(values[2]), mktLevels[0].CloseOffer);
            else if (values[1].Contains("CloseOffer"))
                mktLevels[0] = new MarketLevels(mktLevels[0].AssetId, mktLevels[0].Low, mktLevels[0].High, mktLevels[0].CloseBid, (decimal)double.Parse(values[2]));
        }

        Dictionary<string, MarketLevels> IReaderConnection.GetMarketLevels(DateTime updateTime, List<string> ids)
        {
            _csvReader.Close();
            _csvReader = new StreamReader(File.OpenRead(_csvFile));
            var mktLevelsGen = getRows<MarketLevels>(updateTime, updateTime, PublisherConnection.DATATYPE_MARKETLEVELS, ids, readMarketLevelData);
            var mktLevels = new Dictionary<string, MarketLevels>();
            mktLevels.Add(mktLevelsGen.Keys.First(), mktLevelsGen.Values.First()[0]);
            return mktLevels;
        }

        Dictionary<string, List<CqlQuote>> IReaderConnection.GetMarketDataQuotes(DateTime startTime, DateTime stopTime, string type, List<string> ids)
        {            
            return getRows<CqlQuote>(startTime, stopTime, type, ids, readMarketData);
        }

        Dictionary<string, List<CqlQuote>> IReaderConnection.GetIndicatorDataQuotes(DateTime startTime, DateTime stopTime, string type, List<string> ids)
        {
            return getRows<CqlQuote>(startTime, stopTime, type, ids, readIndicatorData);
        }

        Dictionary<string, List<CqlQuote>> IReaderConnection.GetSignalDataQuotes(DateTime startTime, DateTime stopTime, string type, List<string> ids)
        {
            return getRows<CqlQuote>(startTime, stopTime, type, ids, readSignalData);
        }

        Dictionary<string, List<Trade>> IReaderConnection.GetTrades(DateTime startTime, DateTime stopTime, string type, List<string> ids)
        {
            return getRows<Trade>(startTime, stopTime, type, ids, readTradeData);
        }

        Dictionary<string, List<KeyValuePair<DateTime, double>>> IReaderConnection.GetProfits(DateTime startTime, DateTime stopTime, string type, List<string> ids)
        {
            return getRows<KeyValuePair<DateTime, double>>(startTime, stopTime, type, ids, readProfitData, 0);
        }

        void IReaderConnection.CloseConnection()
        {
            _csvReader.Close();
        }        
    }
}

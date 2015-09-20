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
        List<CqlQuote> GetRows(DateTime startTime, DateTime stopTime, string type, string id);
    }

    public class CsvReader : IReaderConnection
    {
        string _csvFile;
        StreamReader _csvReader;
        
        public CsvReader()
        {
            _csvFile = Config.Settings["REPLAY_CSV"];
            _csvReader = new StreamReader(File.OpenRead(_csvFile));
        }

        public List<CqlQuote> GetRows(DateTime startTime, DateTime stopTime, string type, string id)
        {
            List<CqlQuote> quotes = new List<CqlQuote>();
            while (!_csvReader.EndOfStream)
            {
                var line = _csvReader.ReadLine();
                if (line == "")
                    break;
                var values = line.Split(',');

                DateTime curTime = DateTime.SpecifyKind(DateTime.Parse(values[2]), DateTimeKind.Local);
                if (curTime < startTime)
                    continue;
                if (curTime > stopTime)
                    break;
                quotes.Add(new CqlQuote(values[1], DateTimeOffset.Parse(values[2]), values[3], decimal.Parse(values[4]), decimal.Parse(values[5]), int.Parse(values[6]))); 
            }
            quotes.Reverse();
            return quotes;
        }
    }
}

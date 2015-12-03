using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cassandra;

namespace MidaxLib
{
    public class Trade
    {
        string _epic;
        SIGNAL_CODE _direction = SIGNAL_CODE.UNKNOWN;
        int _size = 0;
        string _reference;
        DateTime _tradingTime = DateTime.MinValue;
        DateTime _confirmationTime = DateTime.MinValue;
        decimal _price;
        
        public string Epic { get { return _epic; } }
        public SIGNAL_CODE Direction { get { return _direction; } }
        public int Size { get { return _size; } }
        public string Reference { get { return _reference; } set { _reference = value; } }
        public DateTime TradingTime { get { return _tradingTime; } }
        public DateTime ConfirmationTime { get { return _confirmationTime; } set { _confirmationTime = value; } }
        public decimal Price { get { return _price; } }

        public Trade(DateTime tradingTime, string epic, SIGNAL_CODE direction, int size, decimal price)
        {
            _tradingTime = tradingTime;
            _epic = epic;
            _direction = direction;
            _size = size;
            _price = price;
        }

        public Trade(Row row)
        {
            _reference = (string)row[0];
            _confirmationTime = (DateTime)row[1];
            _direction = (SIGNAL_CODE)(int)row[2];
            _price = (decimal)row[3];
            _size = (int)row[4];
            _epic = (string)row[5];
            _tradingTime = (DateTime)row[6]; 
        }

        public void Publish()
        {
            PublisherConnection.Instance.Insert(this);
        }
    }
}

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
        string _id;
        DateTime _tradingTime = DateTime.MinValue;
        DateTime _confirmationTime = DateTime.MinValue;
        decimal _price;
        int _placeHolder = 0;
        
        public string Epic { get { return _epic; } }
        public SIGNAL_CODE Direction { get { return _direction; } }
        public int Size { get { return _size; } }
        public string Id { get { return _id; } set { _id = value; } }
        public string Reference { get { return _reference; } set { _reference = value; } }
        public DateTime TradingTime { get { return _tradingTime; } }
        public DateTime ConfirmationTime { get { return _confirmationTime; } set { _confirmationTime = value; } }
        public decimal Price { get { return _price; } set { _price = value; } }
        public int PlaceHolder { get { return _placeHolder; } set { _placeHolder = value; } }

        public Trade(DateTime tradingTime, string epic, SIGNAL_CODE direction, int size, decimal price)
        {
            _tradingTime = tradingTime;
            _epic = epic;
            _direction = direction;
            _size = size;
            _price = price;
        }

        public Trade(Trade cpy, bool opposite = false, DateTime? trading_time = null)
        {
            this._epic = cpy._epic;
            if (opposite)
            {
                this._direction = (cpy._direction == SIGNAL_CODE.BUY ? SIGNAL_CODE.SELL : SIGNAL_CODE.BUY);
                this._tradingTime = trading_time.Value;
            }
            else
            {
                this._direction = cpy._direction;
                this._tradingTime = cpy._tradingTime;
                this._confirmationTime = cpy._confirmationTime;
            }
            this._size = cpy._size;
            this._reference = cpy._reference;
            this._id = cpy._id;
            this._price = cpy._price;
        }

        public Trade(Row row)
        {
            _id = (string)row[0];
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

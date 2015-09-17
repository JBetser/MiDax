using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IGPublicPcl;
using Lightstreamer.DotNet.Client;

namespace MidaxLib
{
    public class MarketData
    {
        public MarketData(string name_id, TimeSeries values)
        {
            this._id = name_id.Split(':').Count() > 1 ? name_id.Split(':')[1] : name_id;
            this._name = name_id.Split(':')[0];
            this._values = values;
            this._eventHandlers = new List<Tick>();
        }

        public delegate void Tick(MarketData mktData, DateTime time, Price value);

        public virtual void Subscribe(Tick eventHandler)
        {
            bool subscribe = (this._eventHandlers.Count == 0);
            this._eventHandlers.Add(eventHandler);
            if (subscribe)
                IGConnection.Instance.SubscribeMarketData(this); 
        }

        public void FireTick(DateTime updateTime, L1LsPriceData value)
        {
            Price livePrice = new Price(value);
            _values.Add(updateTime, livePrice);            
            foreach (Tick ticker in this._eventHandlers)
                ticker(this, updateTime, livePrice);
            Publish(updateTime, livePrice);
        }

        public virtual void Publish(DateTime updateTime, Price price)
        {
            PublisherConnection.Instance.Insert(updateTime, this, price);
        }

        protected TimeSeries _values;
        protected string _id;
        protected string _name;
        protected List<Tick> _eventHandlers;

        public string Id
        {
            get { return _id; }
        }

        public string Name
        {
            get { return _name; }
        }

        public TimeSeries Values
        {
            get { return _values; }
        }
    }
}

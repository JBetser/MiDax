using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IGPublicPcl;

namespace MidaxLib
{
    public abstract class Indicator : MarketData
    {
        protected List<MarketData> _mktData = null;
        bool _publishingEnabled = true;
        bool _subscribed = false;

        public bool PublishingEnabled { get { return _publishingEnabled; } set { _publishingEnabled = value; } }
        public MarketData SignalStock { get { return _mktData[0]; } }

        public Indicator(string id, List<MarketData> mktData)
            : base(id)
        {
            _mktData = mktData;
        }

        public override void Subscribe(Tick updateHandler, Tick tickerHandler)
        {
            Clear();
            if (!_subscribed)
            {
                _subscribed = true;
                foreach (MarketData mktData in _mktData)
                    mktData.Subscribe(OnUpdate, OnTick);
            }
            if (updateHandler != null)
                this._updateHandlers.Add(updateHandler);
            else
                this._updateHandlers.Add(OnUpdate);
            //if (tickerHandler != null)
            //    throw new ApplicationException("tickerHandler should not be used by indicators");
        }

        public override void Unsubscribe(Tick updateHandler, Tick tickerHandler)
        {
            this._updateHandlers.Remove(updateHandler);
            if (_subscribed)
            {
                _subscribed = false;
                foreach (MarketData mktData in (from m in _mktData select m).Reverse())
                    mktData.Unsubscribe(OnUpdate, OnTick);
            }
        }

        protected virtual Price IndicatorFunc(MarketData mktData, DateTime updateTime, Price value)
        {
            return null;
        }

        public override void Process(DateTime dt, Price p)
        {
            var newValue = IndicatorFunc(SignalStock, dt, p);
            TimeSeries.Add(dt, newValue);
        }

        protected virtual void OnUpdate(MarketData mktData, DateTime updateTime, Price value)
        {
            _values.Add(updateTime, value);
        }

        public virtual void OnTick(MarketData mktData, DateTime updateTime, Price value)
        {
            foreach (Tick ticker in this._updateHandlers)
                ticker(this, updateTime, value);
        }

        public override void Publish(DateTime updateTime, Price price)
        {
            if (price.Bid != price.Offer)
            {
                string error = "Inconsistent indicator " + _name + " values";
                Log.Instance.WriteEntry(error, EventLogEntryType.Error);
                throw new ApplicationException(error);
            }
            if (_publishingEnabled)
                PublisherConnection.Instance.Insert(updateTime, this, price.Bid);
        }

        public void Publish(DateTime updateTime, decimal price)
        {
            if (_publishingEnabled)
                PublisherConnection.Instance.Insert(updateTime, this, price);
        }
    }
}

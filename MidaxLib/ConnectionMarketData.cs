﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dto.endpoint.search;
using IGPublicPcl;
using Lightstreamer.DotNet.Client;

namespace MidaxLib
{
    public abstract class MarketDataConnection
    {
        protected MarketDataSubscription _mktDataListener = null;
        static MarketDataConnection _instance = null;
        static int _refCount = 0;
        protected IAbstractStreamingClient _apiStreamingClient = null;
        protected IgRestApiClient _igRestApiClient = null;
        protected TimerCallback _callbackConnectionClosed = null;

        protected MarketDataConnection() {
            _mktDataListener = new MarketDataSubscription();
        }

        protected MarketDataConnection(IAbstractStreamingClient iclient)
        {
            _apiStreamingClient = iclient;
            _mktDataListener = new MarketDataSubscription(); 
        }
        
        static public MarketDataConnection Instance
        {
            get
            {
                if (_instance != null)
                    return _instance;
                if (Config.ReplayEnabled)
                    _instance = new ReplayConnection(Config.TestReplayEnabled);
                else if (Config.MarketSelectorEnabled)
                    _instance = new MarketSelectorConnection();
                else if (Config.TradingEnabled)
                    _instance = new IGConnection();
                return _instance;
            }
            set
            {
                _instance = value;
            }
        }

        public abstract void Connect(TimerCallback connectionClosed);
        
        public IAbstractStreamingClient StreamClient
        {
            get { return _apiStreamingClient; }
        }

        public IgRestApiClient RestClient
        {
            get { return _igRestApiClient; }
        }

        public virtual void SubscribeMarketData(MarketData mktData)
        {
            if (_mktDataListener.MarketData.Select(mktdata => mktdata.Id).Contains(mktData.Id))
                UnsubscribeMarketData(_mktDataListener.MarketData.Where(mktdata => mktdata.Id == mktData.Id).First());
            _mktDataListener.MarketData.Add(mktData);
        }

        public virtual void UnsubscribeMarketData(MarketData mktData)
        {
            var lstRemove = _mktDataListener.MarketData.Where(mktdata => mktdata.Id == mktData.Id).ToList();
            foreach (var selmktdata in lstRemove)
                _mktDataListener.MarketData.Remove(selmktdata);
        }

        public void SetListeningState(bool state)
        {
            foreach (MarketData mktData in _mktDataListener.MarketData)
                mktData.Ready = state;
        }

        public virtual void StartListening()
        {
            if (_refCount++ == 0)
            {

                var task = Portfolio.Instance.Subscribe();
                task.Wait();
                SetListeningState(true);
                _mktDataListener.StartListening();
            }
        }

        public virtual void StopListening()
        {
            if (--_refCount <= 0)
            {
                _refCount = 0;
                _mktDataListener.StopListening();
                SetListeningState(false);
                Portfolio.Instance.Unsubscribe();
                PublisherConnection.Instance.Close();
            }
        }
        
        public void Resume()
        {
            _mktDataListener.Resume();
        }
        /*
        public void PublishMarketLevels(List<MarketData> mktData)
        {
            foreach (var mkt in mktData)
            {
                _apiStreamingClient.GetMarketDetails(mkt);
                if (mkt.Levels.HasValue)
                    PublisherConnection.Instance.Insert(mkt.Levels.Value);
                else
                    Log.Instance.WriteEntry("Cannot publish market levels", EventLogEntryType.Error);
            }
        }*/
    }

    public class MarketDataSubscription : HandyTableListenerAdapter
    {
        public List<MarketData> MarketData = new List<MarketData>();
        public SubscribedTableKey MarketDataTableKey = null;
        int time_offset_hours = 0;
        
        public void StartListening()
        {
            if (Config.Settings.ContainsKey("TIME_GMT_MARKETDATA"))
                time_offset_hours = int.Parse(Config.Settings["TIME_GMT_MARKETDATA"]);            
            string[] epics = (from MarketData mktData in MarketData select mktData.Id).ToArray();
            string epicsMsg = string.Concat((from string epic in epics select epic + ", ").ToArray()).TrimEnd(new char[] { ',', ' ' });
            Log.Instance.WriteEntry("Subscribing to market data: " + epicsMsg + "...", System.Diagnostics.EventLogEntryType.Information);
            if (epics.Length > 0)
                MarketDataConnection.Instance.StreamClient.Subscribe(epics, this);
        }

        public void StopListening()
        {
            string[] epics = (from MarketData mktData in MarketData select mktData.Id).ToArray();
            string epicsMsg = string.Concat((from string epic in epics select epic + ", ").ToArray());
            Log.Instance.WriteEntry("Unsubscribing to market data: " + epicsMsg + "...", System.Diagnostics.EventLogEntryType.Information);
            if (epics.Length > 0)
                MarketDataConnection.Instance.StreamClient.Unsubscribe();            
        }

        public void Resume()
        {
            MarketDataConnection.Instance.StreamClient.Resume(this);
        }

        public override void OnUpdate(int itemPos, string itemName, IUpdateInfo update)
        {
            try
            {
                L1LsPriceData priceData = L1LsPriceUpdateData(itemPos, itemName, update);
                foreach (var data in (from MarketData mktData in MarketData where itemName.Contains(mktData.Id) select mktData).ToList())
                {
                    var curTime = Config.ParseDateTimeUTC(priceData.UpdateTime).AddHours(time_offset_hours);
                    if (Config.PublishingOpen(curTime))
                        data.FireTick(curTime, priceData); // Timestamps from IG are GMT (i.e. equivalent to UTC)
                }
            }
            catch (SEHException exc)
            {
                Log.Instance.WriteEntry("Market data update interop error: " + exc.ToString() + ", Error code: " + exc.ErrorCode, EventLogEntryType.Error);
            }
            catch (Exception ex)
            {
                Log.Instance.WriteEntry("Market data update error: " + ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
                throw;
            }
        }
    }
}

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using dto.colibri.endpoint.auth.v2;
using dto.endpoint.positions.close.v1;
using dto.endpoint.positions.create.otc.v2;
using dto.endpoint.search;
using IGPublicPcl;
using Lightstreamer.DotNet.Client;
using Newtonsoft.Json;

namespace MidaxLib
{
    public class ClosingWaitHandle : EventWaitHandle
    {
        bool _signaled = false;
        public bool Signaled { get { return _signaled; } set { _signaled = value; } }

        public ClosingWaitHandle()
            : base(false, EventResetMode.AutoReset)
        {
        }

        public override bool WaitOne()
        {
            _signaled = true;
            return base.WaitOne();
        }

        public override bool WaitOne(int ms)
        {
            _signaled = true;
            return base.WaitOne(ms);
        }

        public new bool Set()
        {
            _signaled = false;
            return base.Set();
        }
    }

    public interface IAbstractStreamingClient
    {
        void Connect(string username, string password, string apiKey, IgRestApiClient igRestApiClient);
        void Subscribe(string[] epics, IHandyTableListener tableListener);
        void Unsubscribe();
        void Resume(IHandyTableListener tableListener);
        SubscribedTableKey SubscribeToPositions(IHandyTableListener tableListener);
        void UnsubscribeTradeSubscription(SubscribedTableKey tableListener);
        void BookTrade(Trade trade, Portfolio.TradeBookedEvent onTradeBooked, Portfolio.TradeBookedEvent onBookingFailed);
        void ClosePosition(Trade trade, DateTime time, Portfolio.TradeBookedEvent onTradeClosed, Portfolio.TradeBookedEvent onBookingFailed);
        void WaitForClosing();
        //void GetMarketDetails(MarketData mktData);
    }

    public class IGMidaxStreamingApiClient : IGStreamingApiClient
    {
        public TimerCallback ConnectionClosed = null;

        public override void OnActivityWarning(bool warningOn)
        {
            Log.Instance.WriteEntry("Lost market data updates: " + warningOn, EventLogEntryType.Error);
            base.OnActivityWarning(warningOn);
        }

        public override void OnClose()
        {
            Log.Instance.WriteEntry("Connection closed", EventLogEntryType.Information);
            base.OnClose();
            if (ConnectionClosed != null)
                ConnectionClosed(null);
        }

        public override void OnConnectionEstablished()
        {
            Log.Instance.WriteEntry("Connection established", EventLogEntryType.Information);
            base.OnConnectionEstablished();
        }

        public override void OnDataError(PushServerException e)
        {
            Log.Instance.WriteEntry("Data error: " + e.ToString(), EventLogEntryType.Error);
            base.OnDataError(e);
        }

        public override void OnEnd(int cause)
        {
            Log.Instance.WriteEntry("End, cause: " + cause, EventLogEntryType.Error);
            base.OnEnd(cause);
        }

        public override void OnFailure(PushConnException e)
        {
            Log.Instance.WriteEntry("Failure: " + e.ToString(), EventLogEntryType.Error);
            base.OnFailure(e);
        }

        public override void OnFailure(PushServerException e)
        {
            Log.Instance.WriteEntry("Failure: " + e.ToString(), EventLogEntryType.Error);
            base.OnFailure(e);
        }

        public override void OnSessionStarted(bool isPolling)
        {
            Log.Instance.WriteEntry("Session started, polling: " + isPolling, EventLogEntryType.Information);
            base.OnSessionStarted(isPolling);
        }
    }
    
    public class IGTradingStreamingClient : IAbstractStreamingClient
    {
        public TimerCallback ConnectionClosed = null;

        IGMidaxStreamingApiClient _igStreamApiClient = new IGMidaxStreamingApiClient();
        IgRestApiClient _igRestApiClient = null;
        SubscribedTableKey _igSubscribedTableKey = null;
        string _currentAccount = null;
        ClosingWaitHandle _closing = new ClosingWaitHandle();

        public async void Connect(string username, string password, string apikey, IgRestApiClient igRestApiClient)
        {
            if (String.IsNullOrEmpty(apikey) || String.IsNullOrEmpty(password) ||
                String.IsNullOrEmpty(username))
            {
                Log.Instance.WriteEntry("Please enter API key, Password and Username", EventLogEntryType.Error);
                return;
            }
            _igRestApiClient = igRestApiClient;

            // use v1 login...
            var ar = new AuthenticationRequest();
            ar.identifier = username;
            ar.password = password;

            //log in...
            var authenticationResponse = await _igRestApiClient.SecureAuthenticate(ar, apikey);

            if (authenticationResponse && (authenticationResponse.Response != null) && (authenticationResponse.Response.accounts != null))
            {
                if (authenticationResponse.Response.accounts.Count > 0)
                {
                    Log.Instance.WriteEntry(JsonConvert.SerializeObject(authenticationResponse, Formatting.Indented), EventLogEntryType.Information);
                    Log.Instance.WriteEntry("Logged in, current account: " + authenticationResponse.Response.currentAccountId, EventLogEntryType.Information);

                    _currentAccount = authenticationResponse.Response.currentAccountId;

                    ConversationContext context = _igRestApiClient.GetConversationContext();

                    Log.Instance.WriteEntry("establishing streaming data connection", EventLogEntryType.Information);

                    if ((context != null) && (authenticationResponse.Response.currentAccountId != null) &&
                        (authenticationResponse.Response.lightstreamerEndpoint != null))
                    {
                        if (_igStreamApiClient != null)
                        {
                            _igStreamApiClient.ConnectionClosed = ConnectionClosed;
                            var connectionEstablished =
                                _igStreamApiClient.Connect(authenticationResponse.Response.currentAccountId, context.cst,
                                                           context.xSecurityToken, context.apiKey,
                                                           authenticationResponse.Response.lightstreamerEndpoint);
                            if (connectionEstablished)
                            {
                                Log.Instance.WriteEntry("streaming data connection established", EventLogEntryType.Information);                                
                            }
                            else
                            {
                                Log.Instance.WriteEntry("streaming data connection could NOT be established", EventLogEntryType.Error);
                            }
                        }
                    }
                    else
                    {
                        Log.Instance.WriteEntry("Could not establish streaming data connection.", EventLogEntryType.Error);
                    }
                }
                else
                {
                    Log.Instance.WriteEntry("no accounts", EventLogEntryType.Error);
                }
            }
            else
            {
                Log.Instance.WriteEntry("Authentication Rest Response error : " + authenticationResponse.StatusCode, EventLogEntryType.Error);
            }
        }

        void IAbstractStreamingClient.Subscribe(string[] epics, IHandyTableListener tableListener)
        {
            _igSubscribedTableKey = _igStreamApiClient.subscribeToMarketDetails(epics, tableListener);
        }

        void IAbstractStreamingClient.Unsubscribe()
        {
            _igStreamApiClient.UnsubscribeTableKey(_igSubscribedTableKey);
        }
        
        void IAbstractStreamingClient.Resume(IHandyTableListener tableListener)
        {
        }

        SubscribedTableKey IAbstractStreamingClient.SubscribeToPositions(IHandyTableListener tableListener)
        {
            return _igStreamApiClient.SubscribeToPositions(_currentAccount, tableListener);
        }

        void IAbstractStreamingClient.UnsubscribeTradeSubscription(SubscribedTableKey tableListener)
        {
            _igStreamApiClient.UnsubscribeTableKey(tableListener);
        }

        public async void BookTrade(Trade trade, Portfolio.TradeBookedEvent onTradeBooked, Portfolio.TradeBookedEvent onBookingFailed)
        {
            if (_closing.Signaled)
            {
                Log.Instance.WriteEntry("Application is stopping: cannot book trade", EventLogEntryType.Warning);
                return;
            }
            CreatePositionRequest cpr = new CreatePositionRequest();
            cpr.epic = trade.Epic;
            cpr.expiry = "DFB";
            cpr.direction = trade.Direction.ToString();
            cpr.size = trade.Size;
            cpr.orderType = "MARKET";
            cpr.guaranteedStop = false;
            cpr.forceOpen = false;
            cpr.currencyCode = Config.Settings["TRADING_CURRENCY"];

            if (onTradeBooked != null)
                onTradeBooked(trade);

            var createPositionResponse = await _igRestApiClient.createPositionV2(cpr);

            if (createPositionResponse && (createPositionResponse.Response != null) && (createPositionResponse.Response.dealReference != null))
            {
                Log.Instance.WriteEntry("New trade booked successfully : " + createPositionResponse.Response.dealReference, EventLogEntryType.Information);
                trade.Reference = createPositionResponse.Response.dealReference;
                trade.ConfirmationTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);                
            }
            else
            {                
                Log.Instance.WriteEntry("Trade booking failed : " + createPositionResponse.StatusCode, EventLogEntryType.Error);
                if (onBookingFailed != null)
                    onBookingFailed(trade);
            }
        }

        public void ClosePosition(Trade trade, DateTime time, Portfolio.TradeBookedEvent onTradeClosed, Portfolio.TradeBookedEvent onBookingFailed)
        {
            if (trade == null)
            {
                Log.Instance.WriteEntry("Cannot close trade", EventLogEntryType.Error);
                return;
            }
            else
                Log.Instance.WriteEntry("Closing trade id " + (trade.Id == null ? "null" : trade.Id) + " ref " + (trade.Reference == null ? "null" : trade.Reference) + "...");
            BookTrade(trade, onTradeClosed, onBookingFailed);
        }

        void IAbstractStreamingClient.WaitForClosing()
        {
            _closing.WaitOne(60000);
        }

        /*
        void IAbstractStreamingClient.GetMarketDetails(MarketData mktData)
        {
            Log.Instance.WriteEntry("Get Market Details for " + mktData.Name, EventLogEntryType.Information);
            var response = _igRestApiClient.searchMarket(mktData.Name);
            if (response.Result)
            {
                Log.Instance.WriteEntry("Get Market Details status code: " + response.Result.StatusCode, EventLogEntryType.Information);
                if (response.Result.Response != null)
                {
                    foreach (var mkt in response.Result.Response.markets)
                    {
                        if (mkt.epic == mktData.Id)
                            mktData.Levels = new MarketLevels(mkt.epic, mkt.low.Value, mkt.high.Value, mkt.bid.Value, mkt.offer.Value);
                    }
                }
                else
                    Log.Instance.WriteEntry("GetMarketDetails received a null response", EventLogEntryType.Error);
            }
            else
                Log.Instance.WriteEntry("GetMarketDetails received a null result", EventLogEntryType.Error);
        }  */      
    }

    public class IGConnection : MarketDataConnection
    {
        public IGConnection()
        {
            _igRestApiClient = new IgRestApiClient();
            _apiStreamingClient = new IGTradingStreamingClient();
        }

        public override void Connect(TimerCallback connectionClosed)
        {
            try
            {
                ((IGTradingStreamingClient)_apiStreamingClient).ConnectionClosed = connectionClosed;
                _apiStreamingClient.Connect(Config.Settings["IG_USER_NAME"], Config.Settings["IG_PASSWORD"], Config.Settings["IG_KEY"], _igRestApiClient);                
            }
            catch (Exception ex)
            {
                Log.Instance.WriteEntry("IGConnection error: " + ex.Message, EventLogEntryType.Error);
            }
        }

        bool isIG(MarketData mktData)        
        {
            return mktData.Id.StartsWith("IX.") || mktData.Id.StartsWith("CS.");
        }

        public override void SubscribeMarketData(MarketData mktData)
        {
            if (isIG(mktData))
                base.SubscribeMarketData(mktData);
        }

        public override void UnsubscribeMarketData(MarketData mktData)
        {
            if (isIG(mktData))
                base.UnsubscribeMarketData(mktData);
        }
    }
}

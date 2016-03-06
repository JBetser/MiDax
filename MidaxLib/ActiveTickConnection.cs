using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lightstreamer.DotNet.Client;

namespace MidaxLib
{
    class ActiveTickStreamingClient : IAbstractStreamingClient
    {
        public static ActiveTickFeedLib.Feed feed;

        public ActiveTickStreamingClient()
        {
            //create new instance of IFeed
            feed = new ActiveTickFeedLib.Feed();

            feed.PrimaryServerHostname = "activetick1.activetick.com";
            feed.BackupServerHostname = "activetick2.activetick.com";
            feed.ServerPort = 443;
            feed.APIUserId = Config.Settings["ACTIVETICK_KEY"];

            feed.OnLoginResponse += new ActiveTickFeedLib.IFeedEvents_OnLoginResponseEventHandler(feed_OnLoginResponse);
            feed.OnStreamQuote += new ActiveTickFeedLib.IFeedEvents_OnStreamQuoteEventHandler(feed_OnStreamQuote);            
        }

        public void Connect(string username, string password, string apikey)
        {
            if (String.IsNullOrEmpty(apikey) || String.IsNullOrEmpty(password) ||
                String.IsNullOrEmpty(username))
            {
                Log.Instance.WriteEntry("Please enter API key, Password and Username", EventLogEntryType.Error);
                return;
            }

            feed.StopServerSession();
            feed.StartServerSession();
        }

        void IAbstractStreamingClient.Subscribe(string[] epics, IHandyTableListener tableListener)
        {
            string[] symbols = new string[1];
            symbols[0] = "MSFT";

            feed.SendQuoteStreamRequest(symbols, (short)ActiveTickFeedLib.ATStreamRequestEnum.ATStreamRequestSubscribe);
        }

        void IAbstractStreamingClient.Unsubscribe()
        {
            string[] symbols = new string[1];
            symbols[0] = "MSFT";

            int requestId = feed.SendQuoteStreamRequest(symbols, (short)ActiveTickFeedLib.ATStreamRequestEnum.ATStreamRequestUnsubscribe);
        }

        void feed_OnLoginResponse(short loginStatus, object entitlements)
        {
            bool[] entitlementsArray = (bool[])entitlements;

            switch ((ActiveTickFeedLib.ATLoginResponseEnum)loginStatus)
            {
                case ActiveTickFeedLib.ATLoginResponseEnum.ATLoginResponseSuccess:
                    if (entitlementsArray[(int)ActiveTickFeedLib.ATEntitlementEnum.ATEntitlementExchangeNYSE] == false ||
                        entitlementsArray[(int)ActiveTickFeedLib.ATEntitlementEnum.ATEntitlementExchangeNASDAQ] == false)
                        Log.Instance.WriteEntry("ActiveTick entitlements missing", EventLogEntryType.Error); 
                    break;
                case ActiveTickFeedLib.ATLoginResponseEnum.ATLoginResponseInvalidPassword:
                    Log.Instance.WriteEntry("ActiveTick invalid pwd", EventLogEntryType.Error); 
                    break;
                case ActiveTickFeedLib.ATLoginResponseEnum.ATLoginResponseInvalidRequest:
                    Log.Instance.WriteEntry("ActiveTick invalid request", EventLogEntryType.Error); 
                    break;
                case ActiveTickFeedLib.ATLoginResponseEnum.ATLoginResponseInvalidUserid:
                    Log.Instance.WriteEntry("ActiveTick invalid Userid", EventLogEntryType.Error); 
                    break;
                case ActiveTickFeedLib.ATLoginResponseEnum.ATLoginResponseLoginDenied:
                    Log.Instance.WriteEntry("ActiveTick login denied", EventLogEntryType.Error); 
                    break;
                case ActiveTickFeedLib.ATLoginResponseEnum.ATLoginResponseServerError:
                    Log.Instance.WriteEntry("ActiveTick server error", EventLogEntryType.Error); 
                    break;
            }
        }

        void feed_OnStreamQuote(string symbol, byte quoteCondition, byte bidExchange, byte askExchange, double bidPrice, double askPrice, int bidSize, int askSize, DateTime quoteDatetime)
        {
            //create data string
            string data = "Stream Quote: " + symbol + "," + quoteCondition + "," + bidExchange + "," + askExchange + "," + bidPrice + "," + askPrice + "," + bidSize + "," + askSize + "," + quoteDatetime.ToString();

            //show on data string on the screen
            Log.Instance.WriteEntry(data);
        }

        void IAbstractStreamingClient.Resume(IHandyTableListener tableListener)
        {
        }

        SubscribedTableKey IAbstractStreamingClient.SubscribeToPositions(IHandyTableListener tableListener)
        {
            throw new ApplicationException("ActiveTick API is not for trading, only for market data");
        }

        void IAbstractStreamingClient.UnsubscribeTradeSubscription(SubscribedTableKey tableListener)
        {
            throw new ApplicationException("ActiveTick API is not for trading, only for market data");
        }

        void IAbstractStreamingClient.BookTrade(Trade trade, Portfolio.TradeBookedEvent onTradeBooked, Portfolio.TradeBookedEvent onBookingFailed)
        {
            throw new ApplicationException("ActiveTick API is not for trading, only for market data");
        }

        void IAbstractStreamingClient.ClosePosition(Trade trade, DateTime time, Portfolio.TradeBookedEvent onTradeClosed, Portfolio.TradeBookedEvent onBookingFailed)
        {
            throw new ApplicationException("ActiveTick API is not for trading, only for market data");
        }

        void IAbstractStreamingClient.WaitForClosing()
        {
            throw new ApplicationException("ActiveTick API is not for trading, only for market data");
        }
    }
}

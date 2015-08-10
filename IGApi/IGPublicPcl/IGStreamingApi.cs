using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Lightstreamer.DotNet.Client;
using Newtonsoft.Json;

namespace IGPublicPcl
{
    /// <Summary>
    ///
    /// IG API - Sample Client
    ///
    /// Copyright 2014 IG Index
    ///
    /// Licensed under the Apache License, Version 2.0 (the 'License')
    /// You may not use this file except in compliance with the License.
    /// You may obtain a copy of the license at 
    /// http://www.apache.org/licenses/LICENSE-2.0
    ///
    /// Unless required by applicable law or agreen to in writing, software
    /// distributed under the License is distributed on an 'AS IS' BASIS,
    /// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    /// See the License for the specifi language governing permissions and
    /// limitations under the License.
    ///
    /// </Summary>

    public enum StreamingDirectionEnum
    {
        BUY,
        SELL
    }

    public enum StreamingStatusEnum
    {
        OPEN,
        UPDATED,
        AMENDED,
        CLOSED,
        DELETED,
    }

    public enum StreamingDealStatusEnum
    {
        ACCEPTED,
        REJECTED,
    }

    public enum TradeSubscriptionTypeEnum
    {
        WOU = 0,
        OPU = 1,
        TRADE = 2,
    }

	public class AffectedDeals
	{
		public string dealId;
		public string status;
	}

    public class LsTradeSubscriptionData
    {
        public StreamingDirectionEnum? direction;
        public string limitLevel; // if this is null we get an exception  - should be a decimal
        public string dealId;
        public string affectedDealId;
        public string stopLevel; // should be decimal but throws an exception if null.
        public string expiry;
        public string size; // should be decimal ...
        public StreamingStatusEnum? status;
        public string epic;
        public string level; // decimal
        public bool? guaranteedStop;
        public string dealReference;
        public StreamingDealStatusEnum? dealStatus;
	    public List<AffectedDeals> affectedDeals;
    }   

    public class L1LsPriceData
    {
        public decimal? MidOpen;
        public decimal? High;
        public decimal? Low;
        public decimal? Change;
        public decimal? ChangePct;
        public string UpdateTime;
        public int? MarketDelay;
        public string MarketState;
        public decimal? Bid;
        public decimal? Offer;       
    }

    public class StreamingAccountData
    {
        public decimal? ProfitAndLoss;
        public decimal? Deposit;
        public decimal? UsedMargin;
        public decimal? AmountDue;
        public decimal? AvailableCash;
    }   

    public class MarketStatus
    {
        public string marketstatus { get; set; }       
    }
   
    public class HandyTableListenerAdapter : Lightstreamer.DotNet.Client.IHandyTableListener
    {              
        public L1LsPriceData L1LsPriceUpdateData (int itemPos, string itemName, IUpdateInfo update)
        {
            var lsL1PriceData = new L1LsPriceData();
            try
            {
                var midOpen = update.GetNewValue("MID_OPEN");
                var high = update.GetNewValue("HIGH");
                var low = update.GetNewValue("LOW");
                var change = update.GetNewValue("CHANGE");
                var changePct = update.GetNewValue("CHANGE_PCT");
                var updateTime = update.GetNewValue("UPDATE_TIME");
                var marketDelay = update.GetNewValue("MARKET_DELAY");
                var marketState = update.GetNewValue("MARKET_STATE");
                var bid = update.GetNewValue("BID");
                var offer = update.GetNewValue("OFFER");

                if (!String.IsNullOrEmpty(midOpen))               
                {
                    lsL1PriceData.MidOpen = Convert.ToDecimal(midOpen);
                }
                if (!String.IsNullOrEmpty(high))        
                {
                    lsL1PriceData.High = Convert.ToDecimal(high);
                }
                if (!String.IsNullOrEmpty(low))  
                {
                    lsL1PriceData.Low = Convert.ToDecimal(low);
                }
                if (!String.IsNullOrEmpty(change))
                {
                    lsL1PriceData.Change = Convert.ToDecimal(change);
                }
                if (!String.IsNullOrEmpty(changePct))
                {
                    lsL1PriceData.ChangePct = Convert.ToDecimal(changePct);
                }
                if (!String.IsNullOrEmpty(updateTime))               
                {
                    lsL1PriceData.UpdateTime = updateTime;
                }
                if (!String.IsNullOrEmpty(marketDelay))
                {
                    lsL1PriceData.MarketDelay = Convert.ToInt32(marketDelay);
                }
                if (!String.IsNullOrEmpty(marketState))
                {              
                    lsL1PriceData.MarketState = marketState;
                }
                if (!String.IsNullOrEmpty(bid))
                {
                    lsL1PriceData.Bid = Convert.ToDecimal(bid);
                }
                if (!String.IsNullOrEmpty(offer))
                {
                    lsL1PriceData.Offer = Convert.ToDecimal(offer);
                }
            }
            catch (Exception)
            {                
            }
            return lsL1PriceData;
        }

        public StreamingAccountData StreamingAccountDataUpdates(int itemPos, string itemName, IUpdateInfo update)
        {
            var streamingAccountData = new StreamingAccountData();
            try
            {
                var pnl = update.GetNewValue("PNL");
                var deposit = update.GetNewValue("DEPOSIT");
                var usedMargin = update.GetNewValue("USED_MARGIN");
                var amountDue = update.GetNewValue("AMOUNT_DUE");
                var availableCash = update.GetNewValue("AVAILABLE_CASH");
                                       
                if (!String.IsNullOrEmpty(pnl))
                {
                    streamingAccountData.ProfitAndLoss = Convert.ToDecimal(pnl);
                }
                if (!String.IsNullOrEmpty(deposit))
                {
                    streamingAccountData.Deposit = Convert.ToDecimal(deposit);
                }
                if (!String.IsNullOrEmpty(usedMargin))
                {
                    streamingAccountData.UsedMargin = Convert.ToDecimal(usedMargin);
                }
                if (!String.IsNullOrEmpty(amountDue))
                {
                    streamingAccountData.AmountDue = Convert.ToDecimal(amountDue);
                }
                if (!String.IsNullOrEmpty(availableCash))
                {
                    streamingAccountData.AmountDue = Convert.ToDecimal(availableCash);
                }
               
            }
            catch (Exception)
            {
            }
            return streamingAccountData;
        }
               
        public string L1PriceUpdates(int itemPos, string itemName, IUpdateInfo update)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Item Position: " + itemPos);
            sb.AppendLine("Item Name: " + itemName);

            try
            {
                var midOpen = update.GetNewValue("MID_OPEN");
                var high = update.GetNewValue("HIGH");
                var low = update.GetNewValue("LOW");
                var change = update.GetNewValue("CHANGE");
                var changePct = update.GetNewValue("CHANGE_PCT");
                var updateTime = update.GetNewValue("UPDATE_TIME");
                var marketDelay = update.GetNewValue("MARKET_DELAY");
                var marketState = update.GetNewValue("MARKET_STATE");
                var bid = update.GetNewValue("BID");
                var offer = update.GetNewValue("OFFER");

                if (!String.IsNullOrEmpty(midOpen))
                {
                    sb.AppendLine("mid open: " + midOpen);
                }
                if (!String.IsNullOrEmpty(high))
                {
                    sb.AppendLine("high: " + high);
                }
                if (!String.IsNullOrEmpty(low))
                {
                    sb.AppendLine("low: " + low);
                }
                if (!String.IsNullOrEmpty(change))
                {
                    sb.AppendLine("change: " + change);
                }
                if (!String.IsNullOrEmpty(changePct))
                {
                    sb.AppendLine("change percent: " + changePct);
                }
                if (!String.IsNullOrEmpty(updateTime))
                {
                    sb.AppendLine("update time: " + updateTime);
                }
                if (!String.IsNullOrEmpty(marketDelay))
                {
                    sb.AppendLine("market delay: " + marketDelay);
                }
                if (!String.IsNullOrEmpty(marketState))
                {
                    sb.AppendLine("market state: " + marketState);
                }
                if (!String.IsNullOrEmpty(bid))
                {
                    sb.AppendLine("bid: " + bid);
                }
                if (!String.IsNullOrEmpty(offer))
                {
                    sb.AppendLine("offer: " + offer);
                }
            }
            catch (Exception ex)
            {
                sb.AppendLine("Exception in L1 Prices");
                sb.AppendLine(ex.Message);
            }
            return sb.ToString();
        }
       
        public virtual void OnRawUpdatesLost(int itemPos, string itemName, int lostUpdates)
        {
        }

        public virtual void OnSnapshotEnd(int itemPos, string itemName)
        {
        }

        public virtual void OnUnsubscr(int itemPos, string itemName)
        {
        }

        public virtual void OnUnsubscrAll()
        {
        }

        public virtual void OnUpdate(int itemPos, string itemName, IUpdateInfo update)
        {
        }

    }

    public class IGStreamingApiClient : IConnectionListener
    {

        private LSClient lsClient;

        public IGStreamingApiClient()
        {
            try
            {
                lsClient = new LSClient();
            }
            catch (Exception)
            {
                
            }
        }

        public bool Connect(string username, string cstToken, string xSecurityToken, string apiKey, string lsHost)
        {
            bool connectionEstablished = false;

            ConnectionInfo connectionInfo = new ConnectionInfo();
            connectionInfo.Adapter = "DEFAULT";
            connectionInfo.User = username;
            connectionInfo.Password = "CST-" + cstToken + "|XST-" + xSecurityToken;
            connectionInfo.PushServerUrl = lsHost;
            try
            {
                if (lsClient != null)
                {
                    lsClient.OpenConnection(connectionInfo, this);
                    connectionEstablished = true;
                }
            }
            catch (Exception)
            {
                connectionEstablished = false;
            }
            return connectionEstablished;
        }

        public void disconnect()
        {
            if (lsClient != null)
            {
                lsClient.CloseConnection();
            }
        }

        /// <summary>
        /// account details subscription
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="tableListener"></param>

        public void subscribeToAccountDetails(string accountId, IHandyTableListener tableListener)
        {
            subscribeToAccountDetails(accountId, tableListener, new string[] { "PNL", "DEPOSIT", "USED_MARGIN", "AMOUNT_DUE", "AVAILABLE_CASH" });
        }

        /// <summary>
        /// account details subscription
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="tableListener"></param>

        public void subscribeToAccountDetails(string accountId, IHandyTableListener tableListener, string[] fields)
        {
            ExtendedTableInfo extTableInfo = new ExtendedTableInfo(
                new string[] { "ACCOUNT:" + accountId },
                "MERGE",
                fields,
                true
                );
            SubscribedTableKey tableKey = lsClient.SubscribeTable(extTableInfo, tableListener, false);
        }


        public SubscribedTableKey subscribeToAccountDetailsKey(string accountId, IHandyTableListener tableListener)
        {
            return subscribeToAccountDetailsKey(accountId, tableListener, new string[] { "PNL", "DEPOSIT", "USED_MARGIN", "AMOUNT_DUE", "AVAILABLE_CASH" });
        }

        public SubscribedTableKey subscribeToAccountDetailsKey(string accountId, IHandyTableListener tableListener, string[] fields)
        {
            ExtendedTableInfo extTableInfo = new ExtendedTableInfo(
                new string[] { "ACCOUNT:" + accountId },
                "MERGE",
                fields,
                true
                );

            return lsClient.SubscribeTable(extTableInfo, tableListener, false);
        }


        /// <summary>
        /// L1 Prices subscription
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="tableListener"></param>
        public SubscribedTableKey subscribeToMarketDetails(string[] epics, IHandyTableListener tableListener)
        {
            return subscribeToMarketDetails(epics, tableListener,
                new string[] { 
                    "MID_OPEN", "HIGH", "LOW", "CHANGE", "CHANGE_PCT", "UPDATE_TIME", 
                    "MARKET_DELAY", "MARKET_STATE", "BID", "OFFER" 
                    
                    /*, "BID_QUOTE_ID", "OFR_QUOTE_ID"*/
                });
        }

        /// <summary>
        /// L1 Prices subscription
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="tableListener"></param>
        public SubscribedTableKey subscribeToMarketDetails(string[] epics, IHandyTableListener tableListener, string[] fields)
        {
            string[] items = new string[epics.Length];
            for (int i = 0; i < epics.Length; i++)
            {
                items[i] = "L1:" + epics[i];
            }
            ExtendedTableInfo extTableInfo = new ExtendedTableInfo(
                items,
                "MERGE",
                fields,
                true
                );
            return lsClient.SubscribeTable(extTableInfo, tableListener, false);
        }

        /// <summary>
        /// Positions Subscription details
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="tableListener"></param>

        public SubscribedTableKey SubscribeToPositions(string accountId, IHandyTableListener tableListener)
        {
            return subscribeToTradeSubscription(accountId, tableListener,
               new string[] { 
                   "OPU"
                });
        }

        /// <summary>
        /// Working Orders Subscription details
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="tableListener"></param>

        public SubscribedTableKey SubscribeToWorkingOrders(string accountId, IHandyTableListener tableListener)
        {
            return subscribeToTradeSubscription(accountId, tableListener,
               new string[] { 
                   "WOU"
                });
        }

        /// <summary>
        /// trade subscription details
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="tableListener"></param>

        public SubscribedTableKey subscribeToTradeSubscription(string accountId, IHandyTableListener tableListener)
        {
            return subscribeToTradeSubscription(accountId, tableListener,
                new string[] { 
                    "CONFIRMS", "OPU", "WOU"
                });
        }

        public SubscribedTableKey subscribeToTradeSubscription(string accountId, IHandyTableListener tableListener, string[] fields)
        {
            ExtendedTableInfo extTableInfo = new ExtendedTableInfo(
                new string[] { "TRADE:" + accountId },
                "DISTINCT",
                fields,
                true
                );
            return lsClient.SubscribeTable(extTableInfo, tableListener, false);
        }

        public void UnsubscribeTableKey(SubscribedTableKey stk)
        {
            try
            {              
                if (lsClient != null)
                {
                    lsClient.UnsubscribeTable(stk);
                }
            }
            catch (Exception)
            {
                
            }
        }


        public virtual void OnActivityWarning(bool warningOn)
        {

        }

        public virtual void OnClose()
        {
            if (lsClient != null)
            {
                lsClient.CloseConnection();
            }
        }

        public virtual void OnConnectionEstablished()
        {

        }

        public virtual void OnDataError(PushServerException e)
        {

        }

        public virtual void OnEnd(int cause)
        {

        }

        public virtual void OnFailure(PushConnException e)
        {

        }

        public virtual void OnFailure(PushServerException e)
        {

        }

        public virtual void OnNewBytes(long bytes)
        {

        }

        public virtual void OnSessionStarted(bool isPolling)
        {            
        }

    }

}

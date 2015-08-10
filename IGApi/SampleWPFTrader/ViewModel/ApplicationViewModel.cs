using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using dto.colibri.endpoint.auth.v2;
using dto.endpoint.auth.session;
using IGPublicPcl;
using Lightstreamer.DotNet.Client;
using Newtonsoft.Json;
using SampleWPFTrader.Model;

using dto.endpoint.application.operation;
using SampleWPFTrader.Common;

namespace SampleWPFTrader.ViewModel
{
    /// <Summary>
    ///
    /// IG Trader WPF Sample Application 
    /// 
    /// ApplicationViewModel : This file contains all the business logic for handling the Log on command, and subscribing to the 
    /// Trade lightstreamer subscriptions ( Working Order updates, Position Updates and Trade Confirmation messages )
    ///
    /// Copyright 2014 IG Index
    ///
    /// Licensed under the Apache License, Version 2.0 (the 'License')
    /// You may not use this file except in compliance with the License.
    /// You may obtain a copy of the license at 
    /// http://www.apache.org/licenses/LICENSE-2.0
    ///
    /// Unless required by applicable law or agreed to in writing, software
    /// distributed under the License is distributed on an 'AS IS' BASIS,
    /// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    /// See the License for the specific language governing permissions and
    /// limitations under the License.
    ///
    /// </Summary>
    public class ApplicationViewModel : ViewModelBase
    {       
        private Application _currentApplication;                       
        
		private AccountBalanceSubscription _accountBalanceSubscription;        
		private TradeSubscription _tradeSubscription;                  
    
        private SubscribedTableKey _accountBalanceStk;
        private SubscribedTableKey _tradeSubscriptionStk;

		private ObservableCollection<IgPublicApiData.AccountModel> _accounts;
        private ObservableCollection<IgPublicApiData.TradeSubscriptionModel> _tradeSubscriptions;
		private ObservableCollection<IgPublicApiData.AffectedDealModel> _affectedDeals;

        public ApplicationViewModel()
        {
            InitialiseViewModel();

            LoggedIn = false;

            // This data structure is used to contain all the account info that we can bind to in our view, and will update automatically...
			_accounts = new ObservableCollection<IgPublicApiData.AccountModel>();
            _tradeSubscriptions = new ObservableCollection<IgPublicApiData.TradeSubscriptionModel>();
			_affectedDeals = new ObservableCollection<IgPublicApiData.AffectedDealModel>();

            WireCommands();   
        						
			_accountBalanceSubscription = new AccountBalanceSubscription(this);
			_tradeSubscription = new TradeSubscription(this);                     
        }

        private string _applicationDebugData;
        public string ApplicationDebugData
        {
            get
            {
                return _applicationDebugData;
            }
            set
            {
                if (_applicationDebugData != value)
                {
                    _applicationDebugData = value;
                    RaisePropertyChanged("ApplicationDebugData");
                }
            }
        }
        public void UpdateDebugMessage(string message)
        {
            if (ApplicationDebugData != message)
            {
                ApplicationDebugData += message + Environment.NewLine;
            }
        }
		      
        private void WireCommands()
        {                       
            ExitCommand = new RelayCommand(Exit);
            ExitCommand.IsEnabled = true;
        }

		public ObservableCollection<IgPublicApiData.AccountModel> Accounts
		{
			get { return _accounts; }
			set { _accounts = value; }
		}

		public ObservableCollection<IgPublicApiData.AffectedDealModel> AffectedDeals
		{
			get { return _affectedDeals; }
			set { _affectedDeals = value; }
		}

        public ObservableCollection<IgPublicApiData.TradeSubscriptionModel> TradeSubscriptions
        {
            get { return _tradeSubscriptions; }
            set { _tradeSubscriptions = value; }
        }          
   
        public RelayCommand ExitCommand
        {
            get;
            private set;
        }
       
        public Application CurrentApplication
        {
            get
            {
                return _currentApplication;
            }

            set
            {
                if (_currentApplication != value)
                {
                    _currentApplication = value;
                    RaisePropertyChanged("CurrentApplication");                                       
                }
            }
        }
                     
        private bool _loginTabSelected;
        public bool LoginTabSelected
        {
            get
            {
                return _loginTabSelected;
            }
            set
            {
                if (_loginTabSelected != value)
                {
                    _loginTabSelected = value;
                    LoginTabChanged();
                    RaisePropertyChanged("LoginTabSelected");
                }
            }
        }    


        private string _applicationPassword;
        public string ApplicationPassword
        {
            get
            {
                return _applicationPassword;
            }
            set
            {
                if (_applicationPassword != value)
                {
                    _applicationPassword = value;
                    RaisePropertyChanged("ApplicationPassword");
                }
            }
        }
            
     

        public void LoginTabChanged()
        {
            if (LoginTabSelected)
            {
                UpdateDebugMessage("Login Tab selected");

                if (LoggedIn == false)
                {
                    Login();
                }
                else
                {
                    SubscribeToAccountDetails();
                    SubscribeToTradeSubscription();
                }
            }
            else
            {
                UpdateDebugMessage("Login Tab de-selected");
               
                UnsubscribefromTradeSubscription();
                UnsubscribeFromAccountDetailsSubscription();

                TradeSubscriptions.Clear();                
            }
        }

        private void UnsubscribefromTradeSubscription()
        {
            if ((_tradeSubscriptionStk != null) && ( igStreamApiClient != null))
            {	            
		        igStreamApiClient.UnsubscribeTableKey(_tradeSubscriptionStk);
		        _tradeSubscriptionStk = null;	            
	            UpdateDebugMessage("Successfully unsubscribed from Trade Subscription");
            }
        }

        private void UnsubscribeFromAccountDetailsSubscription()
        {
            if ((_accountBalanceStk != null) && (igStreamApiClient != null))
            {
                igStreamApiClient.UnsubscribeTableKey(_accountBalanceStk);
                _accountBalanceStk = null;

                UpdateDebugMessage("Successfully unsubscribed from Account Balance Subscription");
            }
        }

        public void Exit()
        {
            try
            {

                // Unsubscribe from LS account balance and trade subscriptions...
                if (igStreamApiClient != null)
                {
					UnsubscribeFromAccountDetailsSubscription();
                    UnsubscribefromTradeSubscription();               
	                Accounts = null;                   
                }

                if (igRestApiClient != null)
                {
                    igRestApiClient.logout();

                    LoggedIn = false;
                    UpdateDebugMessage("Logged out");                                                       
                }

                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                UpdateDebugMessage(ex.Message);
            }
        }
       

        public async void Login()
        {
            UpdateDebugMessage("Attempting login");
			
			var UserName		= ""; //*** TODO enter your username here ***;            			
			var ApiKey			= ""; //*** TODO enter your APIKey here ***;
			var Password		= ""; //*** TODO enter your password here ***;
	
            if (String.IsNullOrEmpty(UserName) || String.IsNullOrEmpty(Password) || String.IsNullOrEmpty(ApiKey))
            {               
                UpdateDebugMessage("Please enter a valid username / password / ApiKey combination in ApplicationViewModel ( Login method )");
                return;
            }

			// use v2 secure login...			
			var ar = new dto.colibri.endpoint.auth.v2.AuthenticationRequest();
            ar.identifier       = UserName;
            ar.password         = Password;
        
            try
            {                                            
				var response = await igRestApiClient.SecureAuthenticate(ar, ApiKey);				
                if (response && (response.Response != null) && (response.Response.accounts.Count > 0))
                {
					Accounts.Clear();

	                foreach (var account in response.Response.accounts)
	                {
		                var igAccount = new IgPublicApiData.AccountModel();

		                igAccount.ClientId = response.Response.clientId;
		                igAccount.ProfitLoss = response.Response.accountInfo.profitLoss;
		                igAccount.AvailableCash = response.Response.accountInfo.available;
		                igAccount.Deposit = response.Response.accountInfo.deposit;
		                igAccount.Balance = response.Response.accountInfo.balance;
		                igAccount.LsEndpoint = response.Response.lightstreamerEndpoint;
		                igAccount.AvailableCash = response.Response.accountInfo.available;
		                igAccount.Balance = response.Response.accountInfo.balance;					
		                						
						igAccount.AccountId = account.accountId;
		                igAccount.AccountName = account.accountName;
		                igAccount.AccountType = account.accountType;

		                _accounts.Add(igAccount);
	                }
	               
                    LoggedIn = true;

                    UpdateDebugMessage("Logged in, current account: " + response.Response.currentAccountId);

                    ConversationContext context = igRestApiClient.GetConversationContext();

                    UpdateDebugMessage("establishing datastream connection");

                    if ((context != null) && (response.Response.lightstreamerEndpoint != null) &&
                        (context.apiKey != null) && (context.xSecurityToken != null) && (context.cst != null))
                    {
                        try
                        {
                            CurrentAccountId = response.Response.currentAccountId;
							
                            var connectionEstablished =
                                igStreamApiClient.Connect(response.Response.currentAccountId,
                                                            context.cst,
                                                            context.xSecurityToken, context.apiKey,
                                                            response.Response.lightstreamerEndpoint);
                            if (connectionEstablished)
                            {
                                UpdateDebugMessage(String.Format("Connecting to Lightstreamer. Endpoint ={0}",
                                                                    response.Response.lightstreamerEndpoint));

                                // Subscribe to Account Details and Trade Subscriptions...
                                SubscribeToAccountDetails();
                                SubscribeToTradeSubscription();
                            }
                            else
                            {
                                igStreamApiClient = null;
                                UpdateDebugMessage(String.Format(
                                    "Could NOT connect to Lightstreamer. Endpoint ={0}",
                                    response.Response.lightstreamerEndpoint));
                            }
                        }
                        catch (Exception ex)
                        {
                            UpdateDebugMessage(ex.Message);
                        }
                    }
                }
                else
                {
                    UpdateDebugMessage("Failed to login. HttpResponse StatusCode = " +
                                        response.StatusCode);
                }                                  
            }
            catch (Exception ex)
            {
                UpdateDebugMessage("ApplicationViewModel exception : " + ex.Message);
            }                          
        }

#region LightStreamerSubscriptions
      
        public void SubscribeToAccountDetails()
        {
            try
            {
                if (CurrentAccountId != null)
                {
					_accountBalanceStk = new SubscribedTableKey();
                    _accountBalanceStk = igStreamApiClient.subscribeToAccountDetailsKey(CurrentAccountId, _accountBalanceSubscription);
                    UpdateDebugMessage("Lightstreamer - Subscribing to Account Details");    
                }
            }
            catch (Exception ex)
            {
                UpdateDebugMessage("ApplicationViewModel - SubscribeToAccountDetails" + ex.Message);
            }

        }

        public void SubscribeToTradeSubscription()
        {
            try
            {
                if (CurrentAccountId != null)
                {
					_tradeSubscriptionStk = new SubscribedTableKey();
                    _tradeSubscriptionStk = igStreamApiClient.subscribeToTradeSubscription(CurrentAccountId, _tradeSubscription);
                    UpdateDebugMessage("Lightstreamer - Subscribing to CONFIRMS, Working order updates and open position updates");                   
                }
            }
            catch (Exception ex)
            {
                UpdateDebugMessage("ApplicationViewModel - SubscribeToTradeSubscription" + ex.Message);
            }

        }
      
#endregion // LightstreamerSubscriptions

        public class AccountBalanceSubscription : HandyTableListenerAdapter
        {
            private readonly ApplicationViewModel _applicationViewModel;
            public AccountBalanceSubscription(ApplicationViewModel avm)
            {
                _applicationViewModel = avm;
            }

            public override void OnUpdate(int itemPos, string itemName, IUpdateInfo update)
            {
                var accountUpdates = StreamingAccountDataUpdates(itemPos, itemName, update);

                if ((itemPos != 0) && (itemPos <= _applicationViewModel.Accounts.Count))
                {                    
                    var index = 0; // we are subscription to the current account ( which will be account index 0 ).                                     
                    
                    _applicationViewModel.Accounts[index].AmountDue = accountUpdates.AmountDue;
                    _applicationViewModel.Accounts[index].AvailableCash = accountUpdates.AvailableCash;
                    _applicationViewModel.Accounts[index].Deposit = accountUpdates.Deposit;
                    _applicationViewModel.Accounts[index].ProfitLoss = accountUpdates.ProfitAndLoss;
                    _applicationViewModel.Accounts[index].UsedMargin = accountUpdates.UsedMargin;
                }                
            }
        }
        
        public class TradeSubscription : HandyTableListenerAdapter
        {
            private readonly ApplicationViewModel _applicationViewModel;
            public TradeSubscription(ApplicationViewModel avm)
            {
                _applicationViewModel = avm;
            }
          
            public IgPublicApiData.TradeSubscriptionModel UpdateTs(int itemPos, string itemName, IUpdateInfo update, string inputData, TradeSubscriptionType updateType)
            {
                var tsm = new IgPublicApiData.TradeSubscriptionModel();	           

                try
                {                                    
                    var tradeSubUpdate = JsonConvert.DeserializeObject<LsTradeSubscriptionData>(inputData);
                                         
                    tsm.DealId = tradeSubUpdate.dealId;              
                    tsm.AffectedDealId = tradeSubUpdate.affectedDealId;                        
                    tsm.DealReference = tradeSubUpdate.dealReference;
                    tsm.DealStatus = tradeSubUpdate.dealStatus.ToString();
                    tsm.Direction = tradeSubUpdate.direction.ToString();                    
                    tsm.ItemName = itemName;
                    tsm.Epic = tradeSubUpdate.epic;
                    tsm.Expiry = tradeSubUpdate.expiry;
                    tsm.GuaranteedStop = tradeSubUpdate.guaranteedStop;
                    tsm.Level = tradeSubUpdate.level;
                    tsm.Limitlevel = tradeSubUpdate.limitLevel;
                    tsm.Size = tradeSubUpdate.size;
                    tsm.Status = tradeSubUpdate.status.ToString();                                       
                    tsm.StopLevel = tradeSubUpdate.stopLevel;	            
	               				               
	                switch (updateType)
                    {
                        case TradeSubscriptionType.Opu:
                            tsm.TradeType = "OPU";
                            break;
                        case TradeSubscriptionType.Wou:
                            tsm.TradeType = "WOU";
                            break;
                        case TradeSubscriptionType.Confirm:
                            tsm.TradeType = "CONFIRM";
                            break;
                    }

                    SmartDispatcher.BeginInvoke(() =>
                    {
                        if (_applicationViewModel != null)
                        {
                            _applicationViewModel.UpdateDebugMessage("TradeSubscription received : " + tsm.TradeType);
                            _applicationViewModel.TradeSubscriptions.Add(tsm);

	                        if ((tradeSubUpdate.affectedDeals != null) && (tradeSubUpdate.affectedDeals.Count > 0))
	                        {
		                        foreach (var ad in tradeSubUpdate.affectedDeals)
		                        {
			                        var adm = new IgPublicApiData.AffectedDealModel
			                        {
				                        AffectedDeal_Id = ad.dealId,
				                        AffectedDeal_Status = ad.status
			                        };
			                        _applicationViewModel.AffectedDeals.Add(adm);
		                        }
	                        }

                        }
                    });                   
                }
                catch (Exception ex)
                {
                    _applicationViewModel.ApplicationDebugData += ex.Message;
                }
                return tsm;
            }

            public override void OnUpdate(int itemPos, string itemName, IUpdateInfo update)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Trade Subscription Update");
               
                try
                {
                    var confirms = update.GetNewValue("CONFIRMS");
                    var opu = update.GetNewValue("OPU");
                    var wou = update.GetNewValue("WOU");                   

                    if (!(String.IsNullOrEmpty(opu)))
                    {                                       
                        UpdateTs(itemPos, itemName, update, opu, TradeSubscriptionType.Opu);                       
                    }
                    if (!(String.IsNullOrEmpty(wou)))
                    {                                    
                        UpdateTs(itemPos, itemName, update, wou, TradeSubscriptionType.Wou);                       
                    }
                    if (!(String.IsNullOrEmpty(confirms)))
                    {                        
                        UpdateTs(itemPos, itemName, update, confirms, TradeSubscriptionType.Confirm);
                    }

                }
                catch (Exception ex)
                {
                    _applicationViewModel.ApplicationDebugData += "Exception thrown in TradeSubscription Lightstreamer update" + ex.Message;                                  
                }                       
            }            
        }           
    }
}

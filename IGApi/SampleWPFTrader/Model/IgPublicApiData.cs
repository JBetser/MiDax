using System.Collections.Generic;
using System.Windows.Documents;
using dto.endpoint.settings;
using IGPublicPcl;
using SampleWPFTrader.ViewModel;

namespace SampleWPFTrader.Model
{
    /// <Summary>
    ///
    /// IG Trader WPF Sample Application 
    /// 
    /// IgPublicApiData : This file contains properties which can be bound too from the View layer.
    /// The properties are get or set in the business layer ( ViewModels ) and will be automatically 
    /// updated in the View. ( MVVM design pattern ).
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
    public class IgPublicApiData
    {
        public class ClientSentimentModel : PropertyChangedBase
        {
            private decimal? _clientShort;
            public decimal? ClientShort
            {
                get { return _clientShort; }
                set
                {
                    if (_clientShort != value)
                    {
                        _clientShort = value;
                        RaisePropertyChanged("ClientShort");
                    }
                }
            }

            private decimal? _clientLong;
            public decimal? ClientLong
            {
                get { return _clientLong; }
                set
                {
                    if (_clientLong != value)
                    {
                        _clientLong = value;
                        RaisePropertyChanged("ClientLong");
                    }
                }
            }

            private string _epic;
            public string Epic
            {
                get { return _epic; }
                set
                {
                    if ((_epic != value) && (value != null))
                    {
                        _epic = value;
                        RaisePropertyChanged("Epic");
                    }
                }
            }
        }

        public class TradeSubscriptionModel : PropertyChangedBase
        {
            private string _tradeType;
            public string TradeType
            {
                get { return _tradeType; }
                set
                {
                    if (_tradeType != value)
                    {
                        _tradeType = value;
                        RaisePropertyChanged("TradeType");
                    }
                }
            }

            private string _itemName;
            public string ItemName
            {
                get { return _itemName; }
                set
                {
                    if (_itemName != value)
                    {
                        _itemName = value;
                        RaisePropertyChanged("ItemName");
                    }
                }
            }

            private string _direction;
            public string Direction
            {
                get { return _direction; }
                set
                {
                    if (_direction != value)
                    {
                        _direction = value;
                        RaisePropertyChanged("Direction");
                    }
                }
            }
            private string _limitlevel;
            public string Limitlevel
            {
                get { return _limitlevel; }
                set
                {
                    if (_limitlevel != value)
                    {
                        _limitlevel = value;
                        RaisePropertyChanged("Limitlevel");
                    }
                }
            }
            private string _dealId;
            public string DealId
            {
                get { return _dealId; }
                set
                {
                    if (_dealId != value)
                    {
                        _dealId = value;
                        RaisePropertyChanged("DealId");
                    }
                }
            }
            private string _affectedDealId;
            public string AffectedDealId
            {
                get { return _affectedDealId; }
                set
                {
                    if (_affectedDealId != value)
                    {
                        _affectedDealId = value;
                        RaisePropertyChanged("AffectedDealId");
                    }
                }
            }
            private string _stopLevel;
            public string StopLevel
            {
                get { return _stopLevel; }
                set
                {
                    if (_stopLevel != value)
                    {
                        _stopLevel = value;
                        RaisePropertyChanged("StopLevel");
                    }
                }
            }

            private string _expiry;
            public string Expiry
            {
                get { return _expiry; }
                set
                {
                    if (_expiry != value)
                    {
                        _expiry = value;
                        RaisePropertyChanged("Expiry");
                    }
                }
            }
            private string _size;
            public string Size
            {
                get { return _size; }
                set
                {
                    if (_size != value)
                    {
                        _size = value;
                        RaisePropertyChanged("Size");
                    }
                }
            }
            private string _status;
            public string Status
            {
                get { return _status; }
                set
                {
                    if (_status != value)
                    {
                        _status = value;
                        RaisePropertyChanged("Status");
                    }
                }
            }
            private string _epic;
            public string Epic
            {
                get { return _epic; }
                set
                {
                    if (_epic != value)
                    {
                        _epic = value;
                        RaisePropertyChanged("Epic");
                    }
                }
            }
            private string _level;
            public string Level
            {
                get { return _level; }
                set
                {
                    if (_level != value)
                    {
                        _level = value;
                        RaisePropertyChanged("Level");
                    }
                }
            }
            private bool? _guaranteedStop;
            public bool? GuaranteedStop
            {
                get { return _guaranteedStop; }
                set
                {
                    if (_guaranteedStop != value)
                    {
                        _guaranteedStop = value;
                        RaisePropertyChanged("GuaranteedStop");
                    }
                }
            }
            private string _dealReference;
            public string DealReference
            {
                get { return _dealReference; }
                set
                {
                    if (_dealReference != value)
                    {
                        _dealReference = value;
                        RaisePropertyChanged("DealReference");
                    }
                }
            }
            private string _dealStatus;
            public string DealStatus
            {
                get { return _dealStatus; }
                set
                {
                    if (_dealStatus != value)
                    {
                        _dealStatus = value;
                        RaisePropertyChanged("DealStatus");
                    }
                }
            }
			
        }

	    public class AffectedDealModel : PropertyChangedBase
	    {
			private string _affectedDeal_Status;
			public string AffectedDeal_Status
			{
				get { return _affectedDeal_Status; }
				set
				{
					if (_affectedDeal_Status != value)
					{
						_affectedDeal_Status = value;
						RaisePropertyChanged("AffectedDeal_Status");
					}
				}
			}

			private string _affectedDeal_Id;
			public string AffectedDeal_Id
			{
				get { return _affectedDeal_Id; }
				set
				{
					if (_affectedDeal_Id != value)
					{
						_affectedDeal_Id = value;
						RaisePropertyChanged("AffectedDeal_Id");
					}
				}
			}
	    }


        public class AccountModel : PropertyChangedBase
        {
			private string _accountId;
			public string AccountId
			{
				get { return _accountId; }
				set
				{
					if (_accountId != value)
					{
						_accountId = value;
						RaisePropertyChanged("AccountId");
					}
				}
			}

			private string _accountType;
			public string AccountType
			{
				get { return _accountType; }
				set
				{
					if (_accountType != value)
					{
						_accountType = value;
						RaisePropertyChanged("AccountType");
					}
				}
			}

			private string _accountName;
			public string AccountName
			{
				get { return _accountName; }
				set
				{
					if (_accountName != value)
					{
						_accountName = value;
						RaisePropertyChanged("AccountName");
					}
				}
			}
			private string _clientId;
			public string ClientId
			{
				get { return _clientId; }
				set
				{
					if (_clientId != value)
					{
						_clientId = value;
						RaisePropertyChanged("ClientId");
					}
				}
			}

			private string _userName;
            public string UserName
            {
                get { return _userName; }
                set
                {
                    if (_userName != value)
                    {
                        _userName = value;
                        RaisePropertyChanged("UserName");
                    }
                }
            }

            private string _lsEndpoint;
            public string LsEndpoint
            {
                get { return _lsEndpoint; }
                set
                {
                    if (_lsEndpoint != value)
                    {
                        _lsEndpoint = value;
                        RaisePropertyChanged("LsEndpoint");
                    }
                }
            }

            private string _password;
            public string Password
            {
                get { return _password; }
                set
                {
                    if (_password != value)
                    {
                        _password = value;
                        RaisePropertyChanged("Password");
                    }
                }
            }

            private string _apiKey;
            public string ApiKey
            {
                get { return _apiKey; }
                set
                {
                    if (_apiKey != value)
                    {
                        _apiKey = value;
                        RaisePropertyChanged("ApiKey");
                    }
                }
            }

         
            private decimal? _profitLoss;
            public decimal? ProfitLoss
            {
                get { return _profitLoss; }
                set
                {
                    if (_profitLoss != value) 
                    {
                        _profitLoss = value;
                        RaisePropertyChanged("ProfitLoss");
                    }
                }
            }

            private decimal? _deposit;
            public decimal? Deposit
            {
                get { return _deposit; }
                set
                {
                    if (_deposit != value) 
                    {
                        _deposit = value;
                        RaisePropertyChanged("Deposit");
                    }
                }
            }

            private decimal? _usedMargin;
            public decimal? UsedMargin
            {
                get { return _usedMargin; }
                set
                {
                    if (_usedMargin != value) 
                    {
                        _usedMargin = value;
                        RaisePropertyChanged("UsedMargin");
                    }
                }
            }

            private decimal? _amountDue;
            public decimal? AmountDue
            {
                get { return _amountDue; }
                set
                {
                    if (_amountDue != value) 
                    {
                        _amountDue = value;
                        RaisePropertyChanged("AmountDue");
                    }
                }
            }

            private decimal? _availableCash;
            public decimal? AvailableCash
            {
                get { return _availableCash; }
                set
                {
                    if (_availableCash != value) 
                    {
                        _availableCash = value;
                        RaisePropertyChanged("AvailableCash");
                    }
                }
            }

			private decimal? _balance;
			public decimal? Balance
			{
				get { return _balance; }
				set
				{
					if (_balance != value)
					{
						_balance = value;
						RaisePropertyChanged("Balance");
					}
				}
			}      
      
        }

        public class BrowseModel : PropertyChangedBase
        {
            private InstrumentModel _model;
            public InstrumentModel Model
            {
                get { return _model; }
                set
                {
                    if ((_model != value) && (value != null))
                    {
                        _model = value;
                        RaisePropertyChanged("Model");
                    }
                }
            }
        }		

        public class WatchlistModel : PropertyChangedBase
        {           
            private string _watchlistName;
            public string WatchlistName
            {
                get { return _watchlistName; }
                set
                {
                    if ((_watchlistName != value) && (value != null))
                    {
                        _watchlistName = value;
                        RaisePropertyChanged("WatchlistName");
                    }
                }
            }

            private string _watchlistId;
            public string WatchlistId
            {
                get { return _watchlistId; }
                set
                {
                    if ((_watchlistId != value) && (value != null))
                    {
                        _watchlistId = value;
                        RaisePropertyChanged("WatchlistId");
                    }
                }
            }

            private bool _editable;
            public bool Editable
            {
                get { return _editable; }
                set
                {
                    if (_editable != value)
                    {
                        _editable = value;
                        RaisePropertyChanged("Editable");
                    }
                }
            }

            private bool _deletable;
            public bool Deletable
            {
                get { return _deletable; }
                set
                {
                    if (_deletable != value)
                    {
                        _deletable = value;
                        RaisePropertyChanged("Deletable");
                    }
                }
            }
          
        }

        public class PositionModel : PropertyChangedBase
        {
            private string _createdDate;
            public string CreatedDate
            {
                get { return _createdDate; }
                set
                {
                    if ((_createdDate != value) && (value != null))
                    {
                        _createdDate = value;
                        RaisePropertyChanged("CreatedDate");
                    }
                }
            }

            private decimal? _dealSize;
            public decimal? DealSize
            {
                get { return _dealSize; }
                set
                {
                    if (_dealSize != value)
                    {
                        _dealSize = value;
                        RaisePropertyChanged("DealSize");
                    }
                }
            }

            private string _direction;
            public string Direction
            {
                get { return _direction; }
                set
                {
                    if (_direction != value)
                    {
                        _direction = value;
                        RaisePropertyChanged("Direction");
                    }
                }
            }



            private decimal? _openLevel;
            public decimal? OpenLevel
            {
                get { return _openLevel; }
                set
                {
                    if (_openLevel != value)
                    {
                        _openLevel = value;
                        RaisePropertyChanged("OpenLevel");
                    }
                }
            }

            private decimal? _stopLevel;
            public decimal? StopLevel
            {
                get { return _stopLevel; }
                set
                {
                    if (_stopLevel != value)
                    {
                        _stopLevel = value;
                        RaisePropertyChanged("StopLevel");
                    }
                }
            }

            private decimal? _limitLevel;
            public decimal? LimitLevel
            {
                get { return _limitLevel; }
                set
                {
                    if (_limitLevel != value)
                    {
                        _limitLevel = value;
                        RaisePropertyChanged("LimitLevel");
                    }
                }
            }

            private InstrumentModel _model;
            public InstrumentModel Model
            {
                get { return _model; }
                set
                {
                    if ((_model!= value) && (value != null))
                    {
                        _model = value;
                        RaisePropertyChanged("Model");
                    }
                }
            }
        }

        public class OrderModel : PropertyChangedBase
        {
            private string _dealId;
            public string DealId
            {
                get { return _dealId; }
                set
                {
                    if ((_dealId != value) && (value != null))
                    {
                        _dealId = value;
                        RaisePropertyChanged("DealId");
                    }
                }
            }

            private decimal? _orderSize;
            public decimal? OrderSize
            {
                get { return _orderSize; }
                set
                {
                    if (_orderSize != value)
                    {
                        _orderSize = value;
                        RaisePropertyChanged("OrderSize");
                    }
                }
            }

            private string _direction;
            public string Direction
            {
                get { return _direction; }
                set
                {
                    if (_direction != value)
                    {
                        _direction = value;
                        RaisePropertyChanged("Direction");
                    }
                }
            }


            private string _creationDate;
            public string CreationDate
            {
                get { return _creationDate; }
                set
                {
                    if ((_creationDate != value) && (value != null))
                    {
                        _creationDate = value;
                        RaisePropertyChanged("CreationDate");
                    }
                }
            }

            private InstrumentModel _model;
            public InstrumentModel Model
            {
                get { return _model; }
                set
                {
                    if ((_model != value) && (value != null))
                    {
                        _model = value;
                        RaisePropertyChanged("Model");
                    }
                }
            }
        }

        public class WatchlistMarketModel : PropertyChangedBase
        {           
            private string _updateTime;
            public string UpdateTime
            {
                get { return _updateTime; }
                set
                {
                    if ((_updateTime != value) && (value != null))
                    {
                        _updateTime = value;
                        RaisePropertyChanged("UpdateTime");
                    }
                }
            }
           
            private InstrumentModel _model;
            public InstrumentModel Model
            {
                get { return _model; }
                set
                {
                    if ((_model != value) && (value != null))
                    {
                        _model = value;
                        RaisePropertyChanged("Model");
                    }
                }
            }
        }

        public class InstrumentModel : PropertyChangedBase
        {
            private ClientSentimentModel _clientSentiment;
            public ClientSentimentModel ClientSentiment
            {
                get { return _clientSentiment; }
                set
                {
                    if (_clientSentiment != value)
                    {
                        _clientSentiment = value;
                        RaisePropertyChanged("ClientSentiment");
                    }
                }
            }

            private string _marketStatus;
            public string MarketStatus
            {
                get { return _marketStatus; }
                set
                {
                    if (_marketStatus != value)
                    {
                        _marketStatus = value;
                        RaisePropertyChanged("MarketStatus");
                    }
                }
            }

            private string _lsItemName;
            public string LsItemName
            {
                get { return _lsItemName; }
                set
                {
                    if ((_lsItemName != value) && (value != null))
                    {
                        _lsItemName = value;
                        RaisePropertyChanged("LsItemName");
                    }
                }
            }

         
            private string _epic;
            public string Epic
            {
                get { return _epic; }
                set
                {
                    if ((_epic != value) && (value != null))
                    {
                        _epic = value;
                        RaisePropertyChanged("Epic");
                    }
                }
            }

            private decimal? _bid;
            public decimal? Bid
            {
                get { return _bid; }
                set
                {
                    if (_bid != value)
                    {
                        _bid = value;
                        RaisePropertyChanged("Bid");
                    }
                }
            }

            private decimal? _offer;
            public decimal? Offer
            {
                get { return _offer; }
                set
                {
                    if (_offer != value)
                    {
                        _offer = value;
                        RaisePropertyChanged("Offer");
                    }
                }
            }

            private decimal? _open;
            public decimal? Open
            {
                get { return _open; }
                set
                {
                    if (_open != value)
                    {
                        _open = value;
                        RaisePropertyChanged("Open");
                    }
                }
            }

            private string _instrumentName;
            public string InstrumentName
            {
                get
                {
                    return _instrumentName;
                }
                set
                {
                    if (_instrumentName != value)
                    {
                        _instrumentName = value;
                        RaisePropertyChanged("InstrumentName");
                    }
                }
            }


            private decimal? _netChange;
            public decimal? NetChange
            {
                get { return _netChange; }
                set
                {
                    if (_netChange != value)
                    {
                        _netChange = value;
                        RaisePropertyChanged("NetChange");
                    }
                }
            }

            private decimal? _pctChange;
            public decimal? PctChange
            {
                get { return _pctChange; }
                set
                {
                    if (_pctChange != value)
                    {
                        _pctChange = value;
                        RaisePropertyChanged("PctChange");
                    }
                }
            }

            private decimal? _high;
            public decimal? High
            {
                get { return _high; }
                set
                {
                    if (_high != value)
                    {
                        _high = value;
                        RaisePropertyChanged("High");
                    }
                }
            }

            private decimal? _low;
            public decimal? Low
            {
                get { return _low; }
                set
                {
                    if (_low != value)
                    {
                        _low = value;
                        RaisePropertyChanged("Low");
                    }
                }
            }

           
            private bool? _streamingPricesAvailable;
            public bool? StreamingPricesAvailable
            {
                get { return _streamingPricesAvailable; }
                set
                {
                    if (_streamingPricesAvailable != value)
                    {
                        _streamingPricesAvailable = value;
                        RaisePropertyChanged("StreamingPricesAvailable");
                    }
                }
            }
        }
    }
}

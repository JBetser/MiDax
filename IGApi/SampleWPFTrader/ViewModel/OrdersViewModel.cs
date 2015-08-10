using System;
using System.Collections.ObjectModel;
using System.Linq;
using IGPublicPcl;
using Lightstreamer.DotNet.Client;
using SampleWPFTrader.Model;
using dto.endpoint.workingorders.get.v2;

namespace SampleWPFTrader.ViewModel
{
    /// <Summary>
    ///
    /// IG Trader WPF Sample Application 
    /// 
    /// OrdersViewModel : This file handles all the business logic for the orders. i.e. requesting Rest data for the orders, 
    /// and subscribing to live streaming data from lightstreamer for live price updates.
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
    public class OrdersViewModel : ViewModelBase
    {
        private ObservableCollection<IgPublicApiData.OrderModel> _orders;

        private WorkingOrder _currentOrder;
             
        // L1 Prices subscription...
        private L1OrderPricesSubscription _l1OrderPricesSubscription;       
        // LS subscription table keys ....
        private SubscribedTableKey _orderPricesSubscriptionTableKey;      

        public OrdersViewModel()
        {
            InitialiseViewModel();
           
            _orders = new ObservableCollection<IgPublicApiData.OrderModel>();

            WireCommands();

            // Initialise LS subscriptions            
            _l1OrderPricesSubscription   = new L1OrderPricesSubscription(this);
          
            // initialise the LS SubscriptionTableKeys          
            _orderPricesSubscriptionTableKey = new SubscribedTableKey();

            // initialise to null
            _orderPricesSubscriptionTableKey = null;
        }

        private void WireCommands()
        {            
            GetOrdersCommand = new RelayCommand(GetRestOrders);
            ClearOrdersCommand = new RelayCommand(ClearOrders);
           
            GetOrdersCommand.IsEnabled = true;
            ClearOrdersCommand.IsEnabled = true;           
        }

        public RelayCommand UpdateOrderCommand
        {
            get;
            private set;
        }
     
        public RelayCommand GetOrdersCommand
        {
            get;
            private set;
        }

        public RelayCommand ClearOrdersCommand
        {
            get;
            private set;
        }

        public ObservableCollection<IgPublicApiData.OrderModel> Orders
        {
            get { return _orders; }
            set { _orders = value; }
        }

        private bool _ordersTabSelected;
        public bool OrdersTabSelected
        {
            get
            {
                return _ordersTabSelected;
            }
            set
            {
                if (_ordersTabSelected != value)
                {
                    _ordersTabSelected = value;
                    OrdersTabChanged();
                    RaisePropertyChanged("OrdersTabSelected");
                }
            }
        }

        public void OrdersTabChanged()
        {
            if (OrdersTabSelected)
            {
                UpdateOrdersMessage("OrderTab selected");

                if (LoggedIn)
                {                   
                    GetRestOrders();
                }
                else
                {
                    UpdateOrdersMessage("Please log in first");
                }
            }
            else
            {
                UpdateOrdersMessage("OrderTab de-selected");

                // Unsubscribe from Streaming Orders...
                UnsubscribeFromOrders();
            }
        }


        private void UnsubscribeFromOrders()
        {
            if ((igStreamApiClient != null) && (_orderPricesSubscriptionTableKey != null) && (LoggedIn))
            {
                igStreamApiClient.UnsubscribeTableKey(_orderPricesSubscriptionTableKey);
                _orderPricesSubscriptionTableKey = null;
                UpdateOrdersMessage("Unsubscribed from OrderPrices");
            }
        }

        public WorkingOrder CurrentOrder
        {
            get
            {
                return _currentOrder;
            }

            set
            {
                if (_currentOrder != value)
                {
                    _currentOrder = value;
                    RaisePropertyChanged("CurrentOrder");
                    UpdateOrderCommand.IsEnabled = true;
                }
            }
        }

        private string _orderData;
        public string OrderData
        {
            get { return _orderData; }
            set
            {
                if ((_orderData != value) && (value != null))
                {
                    _orderData = value;
                    RaisePropertyChanged("OrderData");
                }
            }
        }
      
        private WorkingOrder _selectedItem;
        public WorkingOrder SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if ((_selectedItem != value) && (value != null))
                {
                    _selectedItem = value;
                    RaisePropertyChanged("SelectedItem");
                }
            }
        }
     
        public void UpdateOrdersMessage(string message)
        {
            // This is the text box displayed on the OrdersTab...
            OrderData += message + Environment.NewLine;
        }       

        public void ClearOrders()
        {
            if (Orders != null)
            {
                Orders.Clear();
                UpdateOrdersMessage("Orders cleared");
            }
        }
   
        public void SubscribeToL1OrderPrices(string[] orderSubs)
        {                 
            try
            {
                // Subscribe to L1 price updates for these orders...   
                if (igStreamApiClient != null)
                {
                    _orderPricesSubscriptionTableKey = igStreamApiClient.subscribeToMarketDetails(orderSubs,
                                                                                                  _l1OrderPricesSubscription);
                    UpdateOrdersMessage("Subscribed Successfully to orders");
                }
            }
            catch (Exception ex)
            {
                UpdateOrdersMessage("Could not subscribe to L1 Prices for orders" + ex.Message);                                                         
            }           
        }       

        public async void GetRestOrders()
        {
            try
            {
                UpdateOrdersMessage("Retrieving working orders");

                var response = await igRestApiClient.workingOrdersV2();

                if (response && (response.Response != null) && (response.Response.workingOrders != null))
                {                    
                    Orders.Clear();

                    if (response.Response.workingOrders.Count != 0)
                    {
                        foreach (var order in response.Response.workingOrders)
                        {                                                        
                            var igOrder = new IgPublicApiData.OrderModel();
                            igOrder.Model = new IgPublicApiData.InstrumentModel();

                            igOrder.Model.Bid = order.marketData.bid;
                            igOrder.Model.Offer = order.marketData.offer;
                            igOrder.Model.Epic = order.marketData.epic;
                            igOrder.Model.InstrumentName = order.marketData.instrumentName;
                            igOrder.OrderSize = order.workingOrderData.orderSize;
                            igOrder.Direction = order.workingOrderData.direction;
                            igOrder.Model.Offer = order.marketData.offer;
                            igOrder.Model.NetChange = order.marketData.netChange;
                            igOrder.Model.PctChange = order.marketData.percentageChange;
                            igOrder.Model.Low = order.marketData.low;
                            igOrder.Model.High = order.marketData.high;
                            igOrder.Model.StreamingPricesAvailable = order.marketData.streamingPricesAvailable;
                            igOrder.DealId = order.workingOrderData.dealId;
                            igOrder.CreationDate = order.workingOrderData.createdDate;
                            igOrder.Model.MarketStatus = order.marketData.marketStatus;

                            if (Orders != null)
                            {
                                Orders.Add(igOrder);
                            }

                        }

                        // Get unique epics for these orders ( we don't want to subscribe to the same epic twice )
                        var uniqueEpics = (from dbo in Orders
                                                 where dbo.Model.StreamingPricesAvailable == true
                                                 select dbo.Model.Epic).Distinct().ToArray();

	                    if (uniqueEpics.Length != 0)
	                    {
		                    SubscribeToL1OrderPrices(uniqueEpics);
	                    }
	                    else
	                    {
		                    UpdateOrdersMessage("There are no orders with streaming prices enabled");
	                    }

                    }
                    else
                    {
                        UpdateOrdersMessage("GetRestOrders: no workingOrders for this account.");
                    }                                     
                }
                else
                {
                    UpdateOrdersMessage("GetRestOrders: HttpStatusCode error" + response.StatusCode);
                }
            }
            catch (Exception ex)
            {
                UpdateOrdersMessage(ex.Message);
            }
        }
      
        public class L1OrderPricesSubscription : HandyTableListenerAdapter
        {
            private readonly OrdersViewModel _ordersViewModel;

            public L1OrderPricesSubscription(OrdersViewModel ovm)
            {
                _ordersViewModel = ovm;
            }

            public override void OnUpdate(int itemPos, string itemName, IUpdateInfo update)
            {               
                try
                {                    
                    var ordUpdate = L1LsPriceUpdateData(itemPos, itemName, update);                   
                    var epic = itemName.Replace("L1:", "");
                   
                    foreach (var order in _ordersViewModel.Orders)                    
                    {
                        if (order.Model.Epic == epic)
                        {                    
                            order.Model.LsItemName = itemName;
                            order.Model.Bid = ordUpdate.Bid;
                            order.Model.Offer = ordUpdate.Offer;
                            order.Model.NetChange = ordUpdate.Change;
                            order.Model.PctChange = ordUpdate.ChangePct;
                            order.Model.Low = ordUpdate.Low;
                            order.Model.High = ordUpdate.High;
                            order.Model.Open = ordUpdate.MidOpen;
                            order.Model.MarketStatus = ordUpdate.MarketState;
                        }
                    }

                }
                catch (Exception ex)
                {
                    // Display in text box.
                    _ordersViewModel.OrderData = ex.Message;
                }
            }
        }
    }

}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using SampleWPFTrader.ViewModel;

namespace SampleWPFTrader.Model
{     
    public class Message : ViewModelBase
    {        
        private string _messageData;
        public string MessageData
        {
            get {return _messageData; }
            set 
            {
                if ((_messageData != value) && ( value != null))
                {
                    _messageData = value;                                   
                    OnPropertyChanged("MessageData");                    
                }
            }
        }

        private string _positionData;
        public string PositionData
        {
            get { return _positionData; }
            set
            {
                if ((_positionData != value) && (value != null))
                {
                    _positionData = value;
                    OnPropertyChanged("PositionData");
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
                    OnPropertyChanged("OrderData");
                }
            }
        }
    }   
}

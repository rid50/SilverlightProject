using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.ServiceModel;
using System.Windows.Data;
using System.Collections;

using System.Windows.Printing;
using System.Windows.Browser;
using System.Text;
using System.Security.Cryptography;
using System.Collections.ObjectModel;
//using System.Windows.Printing;

using FaihaProject.ContractPaymentServiceReference;

namespace FaihaProject
{
    public partial class ContractPayment : UserControl
    {
        private string _reportName = "r_payment_stat";
        private string _contractId;
        private decimal _contractValue;
        ContractPaymentServiceClient _service;

        public ContractPayment()
        {
            InitializeComponent();

            Loaded += new RoutedEventHandler(MainPage_Loaded);

            dfContract.IsEnabledChanged += 
                    new DependencyPropertyChangedEventHandler(dfContract_IsEnabledChanged);
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e) {

           // RowDefinition myRow = TopGrid.RowDefinitions[0];
            
           // object i = Grid.HeightProperty;
            var serviceUrl = new Uri(Application.Current.Host.Source, "../ContractPaymentService.svc");
            _service = new ContractPaymentServiceClient("CustomBinding_ContractPaymentService", serviceUrl.AbsoluteUri);
//            _service = new ContractPaymentServiceClient();
            _service.GetContractsListCompleted += new EventHandler<GetContractsListCompletedEventArgs>(service_GetContractListCompleted);
            _service.GetContractsListAsync();

            busyIndicator1.IsBusy = true;
            dfContract.Visibility = System.Windows.Visibility.Collapsed;
            //dgContractWorkOrder.Visibility = System.Windows.Visibility.Collapsed;
            dgPayment.Visibility = System.Windows.Visibility.Collapsed;

//            serviceUrl = new Uri(Application.Current.Host.Source, "../ContractPaymentService.svc");
//            _service = new C("CustomBinding_WorkOrderDetailService", serviceUrl.AbsoluteUri);
            _service.GetPaymentRunningSumCompleted += (s, eventArgs) => {
                //ObservableCollection<PaymentRunningSumDto> list = eventArgs.Result as ObservableCollection<PaymentRunningSumDto>;
                IList<PaymentRunningSum> paymentDetails = new List<PaymentRunningSum>();
                decimal runningSum = 0;
                foreach (PaymentRunningSumDto p in eventArgs.Result)
                {
                    PaymentRunningSum rsum = new PaymentRunningSum();
                    runningSum += p.PaymentValue;

                    rsum.PaymentNo = p.PaymentNo;
                    rsum.PaymentDate = p.PaymentDate;
                    rsum.PaymentValue = p.PaymentValue;
                    rsum.RunningSum = runningSum;
                    rsum.RemainingValue = _contractValue - runningSum;
                    rsum.PercentPaid = runningSum / _contractValue;

                    paymentDetails.Add(rsum);
                }
/*
                var paymentDetails = from p in eventArgs.Result
                                    select new PaymentRunningSum
                                    {
                                        PaymentNo = p.PaymentNo,
                                        PaymentDate = p.PaymentDate,
                                        PaymentValue = p.PaymentValue,
                                        RunningSum = p.PaymentValue,
                                        RemainingValue = p.PaymentValue,
                                        PercentPaid = p.PaymentValue
                                    };
*/
                dgPayment.ItemsSource = paymentDetails;
                chartControl.DataContext = paymentDetails;


//                dgPayment.ItemsSource = eventArgs.Result;
                dgPayment.Visibility = System.Windows.Visibility.Visible;
                accordionControl.Visibility = System.Windows.Visibility.Visible;

                busyIndicator1.IsBusy = false;
                //VisualStateManager.GoToState(this, "ShowState", false);

/*
                for (int i = 0; i < dgWorkOrderDetail.Columns.Count; i++)
                {
                    if (i == dgWorkOrderDetail.Columns.Count - 2 || i == dgWorkOrderDetail.Columns.Count - 1)
                    {
                        dgWorkOrderDetail.Columns[i].Width = new DataGridLength(dgWorkOrderDetail.Columns[i].ActualWidth * 2);
                    }
                }
*/
            };
        }

        void service_GetContractListCompleted(object sender, GetContractsListCompletedEventArgs e)
        {
//            busyIndicator1.IsBusy = false;
            dfContract.Visibility = System.Windows.Visibility.Visible;

            try
            {
                if (e.Error == null)
                {
                    dfContract.ItemsSource = e.Result;

//                    dfContractWorkOrder.Header = (dfContractWorkOrder.CurrentIndex + 1).ToString() + "/" +
//                                                (dfContractWorkOrder.ItemsSource as Collection<ContractWorkOrderDto>).Count();
                    //dfContractWorkOrder.Header = e.Result;
//                    dfContractWorkOrder.Header = (e.Result[0] as ContractWorkOrderDto).ClassID;
                    //ContractWorkOrderDto dto = dfContractWorkOrder.CurrentItem as ContractWorkOrderDto;
                    //_contractId = dto.ContractID;

                    //var requstXPS = new RequestXPS();
                    //"[Contract No] = '2003 / 2002 (135)'"
                    //requstXPS.getXPSReport(_reportName, "[Contract No] = '" + _contractId + "'", xps_viewer, statusText);
                }
                else
                {
                    if (e.Error is FaultException)
                    {
                        //var fault = e.Error as FaultException<ExceptionDetail>;
                        var fault = e.Error as FaultException;
                        //MessageBox.Show(fault.Detail.Message);
                        MessageBox.Show(" fault.Message: " + fault.Message.ToString() + Environment.NewLine +
                                        " fault.Reason: " + fault.Reason.ToString() + Environment.NewLine +
                                        " fault.Code: " + fault.Code.ToString());
                        //ErrorPanel.DataContext = fault;
                    }
                    else
                    {
                        Exception ex = e.Error.InnerException;
                        string msg = e.Error.Message;
                        while (ex != null)
                        {
                            msg += Environment.NewLine + ex.ToString();
                            ex = ex.InnerException;
                        }
                        MessageBox.Show(" Error Message: " + msg);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                MessageBox.Show(ex.ToString());
            }
        }

//        private void dgContractWorkOrder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        private void dfContract_CurrentItemChanged(object sender, EventArgs e)
        {
            dfContract.Header = (dfContract.CurrentIndex + 1).ToString() + "/" +
                                        (dfContract.ItemsSource as Collection<ContractDto>).Count();

            busyIndicator1.IsBusy = true;

            ContractDto dto = ((sender as DataForm).CurrentItem) as ContractDto;
            _contractId = dto.ContractID;
            _contractValue = dto.ContractValue;

            var requstXPS = new RequestXPS();
            //"[Contract No] = '2003 / 2002 (135)'"
            requstXPS.getXPSReport(_reportName, "[Contract No] = '" + _contractId + "'",
                                                reportPanel, xps_viewer, statusText, false);

            _service.GetPaymentRunningSumAsync(dto.ContractID);
        }

        private void dfContract_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!IsEnabled)
            {
                VisualStateManager.GoToState(dfContract, "Disabled", true);
            }
            else
            {
                VisualStateManager.GoToState(dfContract, "Normal", true);
            }
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            HtmlPopupWindowOptions options = new HtmlPopupWindowOptions();
            options.Left = 0;
            options.Top = 0;
            options.Width = 1024;
            options.Height = 800;
            options.Resizeable = true;
            options.Scrollbars = true;
            options.Menubar = false;
            options.Toolbar = true;

            Uri url = new Uri(Application.Current.Host.Source, "../XPS/" +
                        hashPredicateExpression("[Contract No] = '" + _contractId + "'") + "_" + _reportName + ".xps");

            HtmlPage.PopupWindow(url, "_blank", options);
        }

        private string hashPredicateExpression(string predicate) {
            byte[] hash;

            //byte[] data = Encoding.UTF8.GetBytes(predicate);
            UTF8Encoding enc = new UTF8Encoding();
            byte[] data = enc.GetBytes(predicate);
            SHA1 sha = new SHA1Managed();
            hash = sha.ComputeHash(data);
            //System.BitConverter.ToString(hash).Replace("-", "").ToUpper();

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        private void dfContract_ContentLoaded(object sender, DataFormContentLoadEventArgs e)
        {
            new Utils().setToolTip(dfContract);
            accordionControl.SelectedItem = accordionChartItem;
        }

        public class PaymentRunningSum
        {
            public short PaymentNo { get; set; }

            public DateTime PaymentDate { get; set; }

            public decimal PaymentValue { get; set; }

            public decimal RunningSum { get; set; }

            public decimal RemainingValue { get; set; }

            public decimal PercentPaid { get; set; }
        }
    }
}

using System;
using System.Data;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HeatSensingGUI.Common;
using HeatSensingGUI.Model;
using Telerik.Windows.Controls;

namespace HeatSensingGUI.Controls
{
    /// <summary>
    /// Interaction logic for Connection.xaml
    /// </summary>
    public partial class Connection : UserControl
    {
        #region Private Variables
        private SerialPort serial;
        private bool IsMachineConnected, IsFingerTips, IsPhalanges, IsPalm = false;
        private TemparatureModel TemperatureModel = new TemparatureModel();

        //Used for chart model
        private ChartObject fingerObject;
        //Used for chart model

        //Used for binding grid
        private StringBuilder SerialPortData = new StringBuilder();
        private string[] splitData;
        private string[] _item = new string[70];
        private string[] _Griditem = new string[3];
        private string currData;
        private SystemAnalysis systemAnalysis;
        private DispatcherTimer timExcelObj = new DispatcherTimer();
        public static DataTemplate DataTemp;
        #endregion

        #region Constructor
        /// <summary>
        /// Initialize constructor
        /// </summary>
        public Connection()
        {
            //Initialize component
            InitializeComponent();
            //Bind model to view and create timer for auto save excel.
            serial = new SerialPort();
            this.DataContext = TemperatureModel;
            timExcelObj.Tick += timExcelObj_Tick;
            timExcelObj.Interval = new TimeSpan(0, 0, 30, 0, 0);
            DataTemp = this.Resources["IconTemplate"] as DataTemplate;
        }
        #endregion

        #region Control Events
        /// <summary>
        /// timer event for release memory
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timExcelObj_Tick(object sender, EventArgs e)
        {

            IsFingerTips = IsPalm = IsPhalanges = false;
            //set file saving message in footer
            Login.Model.FileMessage = "Sensor data is saving...";
            System.Windows.Forms.Application.DoEvents();


            //timer stop
            timExcelObj.Stop();

            //save real time chart excel method.
            systemAnalysis.CreateSystemExcel(true);

            //save real time curve excel method.
            Application.Current.MainWindow.FindChildByType<Monitor>().CreateExcelTemplate();

            //set file saved message in footer
            //Login.Model.FileMessage = "Sensor data is saved";

            // update data in xml for autotun functionality
            CommonHelper.UpdateGUIDetailModel(Constants.BaudRate, ((ContentControl)rcBaudRate.SelectedItem).Content.ToString());
            CommonHelper.UpdateGUIDetailModel(Constants.IsRestarted, "True");

            //machine disconnected.
            Disconnect(true);

            //restart application.
            Application.Current.Shutdown();
            System.Windows.Forms.Application.Restart();
        }

        /// <summary>
        /// FingerTip button click event 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbtnFingerTip_Click(object sender, RoutedEventArgs e)
        {
            //check machine is connected or not.if yes then start displaying fingertip data
            if (CheckMachineConnection())
            {
                CommonHelper.UpdateGUIDetailModel(Constants.IsActiveFingerTip, "True");
                IsFingerTips = true;
                timExcelObj.Start();
                CommonHelper.SuccessMsg(Constants.FingertipCalibration);
            }
        }

        /// <summary>
        /// Phalanges button click event 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbtnPhalanges_Click(object sender, RoutedEventArgs e)
        {
            //check machine is connected or not.if yes then start displaying phalanges data
            if (CheckMachineConnection())
            {
                CommonHelper.UpdateGUIDetailModel(Constants.IsActivePhalanges, "True");
                IsPhalanges = true;
                timExcelObj.Start();
                CommonHelper.SuccessMsg(Constants.PhalangeCalibration);
            }
        }

        /// <summary>
        /// Palm button click event 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbtnPalm_Click(object sender, RoutedEventArgs e)
        {
            //check machine is connected or not.if yes then start displaying palm data
            if (CheckMachineConnection())
            {
                CommonHelper.UpdateGUIDetailModel(Constants.IsActivePalm, "True");
                IsPalm = true;
                timExcelObj.Start();
                CommonHelper.SuccessMsg(Constants.PalmCalibration);
            }
        }

        /// <summary>
        /// Calibrate button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbtnCalibrate_Click(object sender, RoutedEventArgs e)
        {

            //check machine is connected or not.if yes then start displaying all data
            if (CheckMachineConnection())
            {
                CommonHelper.UpdateGUIDetailModel(Constants.IsActiveFingerTip, "True");
                CommonHelper.UpdateGUIDetailModel(Constants.IsActivePhalanges, "True");
                CommonHelper.UpdateGUIDetailModel(Constants.IsActivePalm, "True");
                IsFingerTips = IsPalm = IsPhalanges = true;
                timExcelObj.Start();
                CommonHelper.SuccessMsg(Constants.Calibration);
            }
        }

        /// <summary>
        /// connect machine button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbtnConnect_Click(object sender, RoutedEventArgs e)
        {
            Connect();
        }

        /// <summary>
        /// disconnect machine button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();
        }

        /// <summary>
        /// reset button click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rbtnReset_Click(object sender, RoutedEventArgs e)
        {
            CommonHelper.UpdateGUIDetailModel(Constants.IsActiveFingerTip, "false");
            CommonHelper.UpdateGUIDetailModel(Constants.IsActivePhalanges, "false");
            CommonHelper.UpdateGUIDetailModel(Constants.IsActivePalm, "false");
            IsFingerTips = IsPalm = IsPhalanges = false;
            RestGrid();
        }

        /// <summary>
        /// bind port itemsource when screen loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Connection_Loaded(object sender, RoutedEventArgs e)
        {
            //Bind port value in combobox
            string[] ports = SerialPort.GetPortNames();
            if (ports?.Length > 0)
            {
                rcPort.ItemsSource = ports;
                rcPort.SelectedIndex = 0;
            }

            //check windows is auto run or not.if yes then re initialize data
            if (CommonHelper.IsRestartWindow())
            {
                ReInitializeConnection();
            }
        }

        /// <summary>
        /// read data from Machine
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        [STAThread]
        private void Receive(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPortData.Append(serial.ReadExisting());

                //get data from machine
                while (!SerialPortData.ToString().EndsWith("\n"))
                {
                    SerialPortData.Append(serial.ReadExisting());
                }
                //serial.DiscardInBuffer();
                serial.DiscardOutBuffer();
                splitData = null;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.
                ReleaseMemory();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed. Consider applying the 'await' operator to the result of the call.

                splitData = CommonHelper.SplitData(SerialPortData.ToString());
                if (splitData.Length > 4)
                {
                    splitData = splitData.Skip(splitData.Length - 4).Take(4).ToArray();
                }

                //Bind data in UI
                Parallel.ForEach(splitData, data =>
                {
                    currData = data;
                    _item = currData.Replace("-", ",").Split(new[] { "," }, StringSplitOptions.RemoveEmptyEntries);//.Select(int.Parse).ToArray();
                    if (_item?.Length > 69)
                    {
                        if (IsFingerTips || IsPalm || IsPhalanges)
                        {
                            _Griditem = currData.Split('-');
                            if (IsFingerTips)
                            {
                                TemperatureModel.FingertTips = _Griditem[0].Split(',');
                            }
                            if (IsPalm)
                            {
                                TemperatureModel.Palm = _Griditem[2].Split(',');
                            }
                            if (IsPhalanges)
                            {
                                TemperatureModel.Phalnges = _Griditem[1].Split(',');
                            }

                            #region pass data in system analysis screen.
                            Dispatcher.BeginInvoke(DispatcherPriority.Background, new ThreadStart(
                               delegate
                               {
                                   systemAnalysis.BindRealTimeData(data.Replace(" ", string.Empty));
                               }));
                            #endregion pass data in system analysis screen.

                            //Display data in Connection screen
                            //TemperatureModel.ConnGridData = _item;
                            //set background color in monitor screen.
                            SetColor(_item);

                            #region Display chart according selected sensors for Monitor screen.
                            if (Monitor.IsPlotGraph)
                            {
                                Dispatcher.Invoke(DispatcherPriority.Background, new ThreadStart(
                                  delegate
                                  {
                                      if (Monitor.IsPlotGraph)
                                      {
                                          var time = new DateTime(DateTime.Now.Ticks - Monitor.startDateTime.Ticks);
                                          Monitor.Model.dtSensorChartData?.Rows.Add(Monitor.Model.dtSensorChartData.NewRow());

                                          Monitor.Model.dtSensorChartData.Rows[Monitor.Model.dtSensorChartData.Rows.Count - 1][0] = time.ToString("HH:mm:ss.fff");
                                          Parallel.ForEach(Monitor.Model.SensorChart, (FingerChart) =>
                                          {
                                              fingerObject = new ChartObject
                                              {
                                                  Value = Convert.ToDouble(_item[FingerChart.Tag]),
                                                  Time = time
                                              };

                                              if (FingerChart.SensorData.Count > 20)
                                              {
                                                  FingerChart.SensorData.SuspendNotifications();
                                                  FingerChart.SensorData.RemoveAt(0);
                                                  FingerChart.SensorData.Add(fingerObject);
                                                  FingerChart.SensorData.ResumeNotifications();
                                              }
                                              else
                                              {
                                                  FingerChart.SensorData.Add(fingerObject);
                                              }
                                              Monitor.Model.dtSensorChartData.AsEnumerable().FirstOrDefault(x => x.Field<string>("Time") == time.ToString("HH:mm:ss.fff"))[FingerChart.SensorName] = _item[FingerChart.Tag];
                                          });
                                      }
                                  }));
                            }
                            #endregion Display chart according selected sensors for Monitor screen.
                        }

                        //Set Temparature value
                        SetTemparature();
                    }
                });
                SerialPortData.Clear();
                serial.DiscardOutBuffer();
                serial.DiscardInBuffer();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains(Constants.ClosedPort))
                {
                    if (ex.Message.Contains(Constants.OutOfMemoryException))
                    {
                        SerialPortData = new StringBuilder();
                        return;
                    }
                    Application.Current.Dispatcher.Invoke(delegate
                    {
                        RadWindow.Alert(ex.Message);
                    });
                }
            }
            finally
            {
                SerialPortData.Clear();
            }
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Set color for monitor screen.
        /// </summary>
        /// <param name="data"></param>
        private void SetColor(string[] _data)
        {
            Parallel.For(0, 67, f =>
            {
                App.BackgroundModel.ButtonColor[f] = CommonHelper.SetColor(Convert.ToInt32(_data[f]));
            });
            App.BackgroundModel.ButtonColor = App.BackgroundModel.ButtonColor;
        }

        /// <summary>
        /// Machine disconnect 
        /// </summary>
        private void Disconnect(bool isReRun = false)
        {
            if (serial.IsOpen)
            {
                serial.DtrEnable = false;
                serial.RtsEnable = false;
                serial.DiscardInBuffer();
                serial.DiscardOutBuffer();
                serial.DataReceived -= Receive;
                IsMachineConnected = false;
                IsFingerTips = false;
                IsPalm = false;
                IsPhalanges = false;
                Monitor.IsPlotGraph = false;
                systemAnalysis.SensorSelected = false;
                rbtnConnect.Visibility = Visibility.Visible;
                rbtnDisconnect.Visibility = Visibility.Collapsed;
                timExcelObj.Stop();
                Thread CloseDown = new Thread(new ThreadStart(serial.Close));
                CloseDown.Start();
                if (!isReRun)
                {
                    CommonHelper.SuccessMsg(Constants.MachineDisconnected);
                }
            }
        }

#pragma warning disable CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        private async Task ReleaseMemory()
#pragma warning restore CS1998 // This async method lacks 'await' operators and will run synchronously. Consider using the 'await' operator to await non-blocking API calls, or 'await Task.Run(...)' to do CPU-bound work on a background thread.
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        /// <summary>
        /// reset data for all grid.
        /// </summary>
        private void RestGrid()
        {
            TemperatureModel.FingertTips = Enumerable.Repeat("0", 45).ToArray();
            TemperatureModel.Phalnges = Enumerable.Repeat("0", 10).ToArray();
            TemperatureModel.Palm = Enumerable.Repeat("0", 12).ToArray();
        }

        /// <summary>
        /// Check machine is connected or not
        /// </summary>
        /// <returns></returns>
        private bool CheckMachineConnection()
        {
            if (!IsMachineConnected)
            {
                RadWindow.Alert(Constants.MachineNotConnected);
            }
            return IsMachineConnected;
        }

        /// <summary>
        /// Set Temparature value
        /// </summary>
        private void SetTemparature()
        {
            TemperatureModel.Temperature = Convert.ToDouble(_item[68]);
            TemperatureModel.Humidity = Convert.ToDouble(_item[69]);
        }

        /// <summary>
        /// create connection to machine
        /// </summary>
        private void Connect()
        {
            systemAnalysis = Application.Current.MainWindow.FindChildByType<SystemAnalysis>();

            if (rcPort.SelectedValue == null)
            {
                RadWindow.Alert(Constants.SelectPort);
                return;
            }
            if (rcBaudRate.SelectedValue == null)
            {
                RadWindow.Alert(Constants.SelectBaudRate);
                return;
            }

            serial.PortName = rcPort.SelectedValue.ToString();
            serial.BaudRate = Convert.ToInt32(rcBaudRate.SelectionBoxItem);
            serial.Handshake = Handshake.None;
            serial.Parity = Parity.None;
            serial.DataBits = 8;
            serial.ReadTimeout = 800;
            serial.WriteTimeout = 500;
            serial.Open();
            rbtnConnect.Visibility = Visibility.Collapsed;
            rbtnDisconnect.Visibility = Visibility.Visible;

            serial.DataReceived += Receive;
            IsMachineConnected = true;
        }

        /// <summary>
        /// reinitialize connection screen when  windo is auto run.
        /// </summary>
        private void ReInitializeConnection()
        {
            rcBaudRate.SelectedValuePath = "Content";
            rcBaudRate.SelectedValue = App.LastGUIDetails.BaudRate;
            Connect();
            IsFingerTips = Convert.ToBoolean(App.LastGUIDetails.IsActiveFingerTip);
            IsPalm = Convert.ToBoolean(App.LastGUIDetails.IsActivePalm);
            IsPhalanges = Convert.ToBoolean(App.LastGUIDetails.IsActivePhalanges);
            //IsFingerTips =  IsPalm = IsPhalanges = true;
            timExcelObj.Start();

            //Set userName and Version
            Login.Model.UserName = App.LastGUIDetails.UserName;
            Login.Model.Version = App.LastGUIDetails.Version;

        }
        #endregion
    }
}

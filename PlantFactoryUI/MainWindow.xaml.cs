using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PlantFactoryUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private SerialPort ComDevice = new SerialPort();
        public delegate void HandleInterfaceUpdate(string text);
        private HandleInterfaceUpdate HIU;

        public void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ComDevice.PortName = "COM3";
            ComDevice.BaudRate = 115200;
            ComDevice.Parity = Parity.None;
            ComDevice.StopBits = StopBits.One;
            PortOpen();
        }

        public bool PortOpen()
        {
            ComDevice.DataReceived += new SerialDataReceivedEventHandler(DataReceive);
            ComDevice.ReceivedBytesThreshold = 1;
            ComDevice.RtsEnable = true;
            try
            {
                if (!ComDevice.IsOpen)
                {
                    ComDevice.Open();
                }
            }
            catch
            {
            }
            if (ComDevice.IsOpen)
            {
                return true;
            }
            else
            {
                MessageBox.Show("PortOpen Unsuccessful!!", "ShimaRin Warning", MessageBoxButton.OK);
                return false;
            }
        }

        private void DataReceive(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort SP = (SerialPort)(sender);
            Thread.Sleep(100);
            int num = SP.BytesToRead;
            byte[] readBuffer = new byte[num];
            SP.Read(readBuffer, 0, num);
            string str = Encoding.UTF8.GetString(readBuffer);
            MessageBox.Show(str, "ShimaRin Warning", MessageBoxButton.OK);

            //HIU = new HandleInterfaceUpdate(UpdateTextBox);
            Dispatcher.Invoke(HIU, new string[] { Encoding.ASCII.GetString(readBuffer) });
        }


        public void PortSend(string Command)
        {
            byte[] writeBuffer = Encoding.ASCII.GetBytes(Command);
            ComDevice.Write(writeBuffer, 0, writeBuffer.Length);
        }
    }
}

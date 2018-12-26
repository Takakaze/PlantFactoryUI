using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Net;
using Newtonsoft.Json;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Win32;

namespace PlantFactoryUI
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initialize Window
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Set SerialPort ready
        /// </summary>
        private SerialPort ComDevice = new SerialPort();
        public delegate void HandleInterfaceUpdate(string text);
        private HandleInterfaceUpdate HIU;

        /// <summary>
        /// using List to store data
        /// </summary>
        List<data> LST = new List<data>();

        /// <summary>
        /// initialize port status before opening
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ComDevice.PortName = "COM3";
            ComDevice.BaudRate = 115200;
            ComDevice.Parity = Parity.None;
            ComDevice.StopBits = StopBits.One;
            PortOpen();
        }

        /// <summary>
        /// open/close port
        /// </summary>
        /// <returns></returns>
        public void PortOpen()
        {
            if (ComDevice.IsOpen)
            {
                try
                {
                    if (ComDevice.IsOpen)
                    {
                        ComDevice.Close();
                    }
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.ToString(), "ShimaRin Warning", MessageBoxButton.OK);
                }
                if (!ComDevice.IsOpen)
                {
                    Ports.Header = "OpenPort";
                }
                else
                {
                    MessageBox.Show("PortClose Unsuccessful!!", "ShimaRin Warning", MessageBoxButton.OK);
                }
            }
            else
            {
                ComDevice.ReceivedBytesThreshold = 1;
                ComDevice.DataReceived += new SerialDataReceivedEventHandler(DataReceive);
                ComDevice.RtsEnable = true;
                try
                {
                    if (!ComDevice.IsOpen)
                    {
                        ComDevice.Open();
                    }
                }
                catch(Exception e)
                {
                    MessageBox.Show(e.ToString(), "ShimaRin Warning", MessageBoxButton.OK);
                }
                if (ComDevice.IsOpen)
                {
                    Ports.Header = "ClosePort";
                }
                else
                {
                    MessageBox.Show("PortOpen Unsuccessful!!", "ShimaRin Warning", MessageBoxButton.OK);
                }

            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataReceive(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort SP = (SerialPort)(sender);
            Thread.Sleep(100);
            int num = SP.BytesToRead;
            byte[] readBuffer = new byte[num];
            SP.Read(readBuffer, 0, num);
            string str = Encoding.UTF8.GetString(readBuffer);
            MessageBox.Show(str, "ShimaRin Warning", MessageBoxButton.OK);
            LST.Add(new data
            {
                buffer = readBuffer,
                strMem = str
            });
            //HIU = new HandleInterfaceUpdate(UpdateTextBox);
            Dispatcher.Invoke(HIU, new string[] { Encoding.ASCII.GetString(readBuffer) });
        }

        /// <summary>
        /// sendCommand through Serial Port
        /// </summary>
        /// <param name="Command"></param>
        public void PortSend(string Command)
        {
            byte[] writeBuffer = Encoding.ASCII.GetBytes(Command);
            ComDevice.Write(writeBuffer, 0, writeBuffer.Length);
        }

        /// <summary>
        /// list all ports in listbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchPort(object sender, RoutedEventArgs e)
        {
            String[] arrayPort = SerialPort.GetPortNames();
            CMBox.Items.Clear();
            for (int i = 0; i < arrayPort.Length; i++)
            {
                CMBox.Items.Add(arrayPort[i]);
            }
        }


        byte[] buffer = new byte[1024];
        int count = 0;


        /// <summary>
        /// open server and made client able to connect
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SocketStart(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Dispatcher.Invoke(new Action(() => SocketStatus.Text += DateTime.Now.ToString("MM-dd HH:mm:ss")));
                SocketStatus.Text += "Server:Ready";
                SocketStatus.Text += "\n";
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Any, 4096));
                socket.Listen(10000);
                socket.BeginAccept(new AsyncCallback(ClientConnected), socket);
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString(), "ShimaRin Warning", MessageBoxButton.OK);
            }
        }

        /// <summary>
        /// client connected to server 
        /// </summary>
        /// <param name="IAR"></param>
        public void ClientConnected(IAsyncResult IAR)
        {
            count++;
            var socket = IAR.AsyncState as Socket;
            var client = socket.EndAccept(IAR);
            IPEndPoint IPEP = (IPEndPoint)client.RemoteEndPoint;
            this.Dispatcher.Invoke(new Action(() => SocketStatus.Text += DateTime.Now.ToString("MM-dd HH:mm:ss")));
            this.Dispatcher.Invoke(new Action(() => SocketStatus.Text += (IPEP + "is connected\n" + "Total Connects:" + count.ToString() + "\n")));
            client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(MSGreceive), client);
            socket.BeginAccept(new AsyncCallback(ClientConnected), socket);
        }

        /// <summary>
        /// receive message from client
        /// </summary>
        /// <param name="IAR"></param>
        public void MSGreceive(IAsyncResult IAR)
        {
            int length = 0;
            string msg = "";
            var socket = IAR.AsyncState as Socket;
            IPEndPoint IPEP = (IPEndPoint)socket.RemoteEndPoint;

            try
            {
                length = socket.EndReceive(IAR);
                msg = Encoding.UTF8.GetString(buffer, 0, length);
                LST.Add(new data
                {
                    buffer = buffer,
                    strMem = msg
                });
                this.Dispatcher.Invoke(new Action(() => SocketStatus.Text += DateTime.Now.ToString("MM-dd HH:mm:ss")));
                this.Dispatcher.Invoke(new Action(() => SocketStatus.Text += (IPEP + ":" + msg + "\n")));
                socket.Send(Encoding.UTF8.GetBytes("Data Received"));
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(MSGreceive), socket);
            }
            catch (Exception ex)
            {
                count--;
                this.Dispatcher.Invoke(new Action(() => SocketStatus.Text += DateTime.Now.ToString("MM-dd HH:mm:ss")));
                this.Dispatcher.Invoke(new Action(() => SocketStatus.Text += (IPEP + " disconnected\n" + "Total Connects:" + count.ToString() + "\n")));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveJSON(object sender, RoutedEventArgs e)
        {
            string jsn = JsonConvert.SerializeObject(LST, Formatting.Indented);
            SaveFileDialog SFD = new SaveFileDialog();
//            SFD.
        }

        private void LoadJSON(object sender, RoutedEventArgs e)
        {

        }
    }
}

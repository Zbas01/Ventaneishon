using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WpfApplication2
{
    /// <summary>
    /// Lógica de interacción para Ventana_Partida.xaml
    /// </summary>
    /// 
    public partial class Ventana_Partida : Window
    {
        string[] list;
        
        public Ventana_Partida()
        {

            InitializeComponent();

            list = GetLocalIPv4();
            cmbColors.ItemsSource = list;
            cmbColors.Text = "127.0.0.1";

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (cmbColors.Text.Length > 8)
            {
                var window = new WpfApplication1.Window1(cmbColors.Text, 2);
                window.Show();
            }
        }

        private void Button_Copy_Click(object sender, RoutedEventArgs e)
        {
            if (cmbColors.Text.Length > 8) {
                var window = new WpfApplication1.Window1(cmbColors.Text, 1);
                window.Show();
            }
        }
        public string[] GetLocalIPv4()
        {
            List<string> output = new List<string>();
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            output.Add(ip.Address.ToString());
                        }
                    }
                }
            }
            return output.ToArray();
        }

        private void CmbColors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }


}

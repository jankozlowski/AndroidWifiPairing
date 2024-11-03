using AndroidMultipleDeviceLauncher.Services;
using System.Windows;

namespace AndroidWifiPairing
{

    public partial class WifiPairing : Window
    {
        private readonly Adb Adb;

        public WifiPairing()
        {
            InitializeComponent();
            Adb = new Adb();

            pairingIp.Text = PairingSettings.PairIP;
            pairingPort.Text = PairingSettings.PairPort;

            connectionIp.Text = PairingSettings.ConnectionIP;
            connectionPort.Text = PairingSettings.ConnectionPort;
        }

        private void CheckAdbClick(object sender, RoutedEventArgs e)
        {

        }

        private void OkButtonClick(object sender, RoutedEventArgs e)
        {
            PairingSettings.PairIP = pairingIp.Text.Trim();
            PairingSettings.PairPort = pairingPort.Text.Trim();

            PairingSettings.ConnectionIP = connectionIp.Text.Trim();
            PairingSettings.ConnectionPort = connectionPort.Text.Trim();

            if (!string.IsNullOrEmpty(pairingCode.Text.Trim())) { 
                string connectionString = $"{PairingSettings.PairIP}:{PairingSettings.PairPort} {pairingCode.Text.Trim()}";

                string result = Adb.AdbCommandWithResult($"pair {connectionString}");
                System.Windows.MessageBox.Show(string.Format(System.Globalization.CultureInfo.CurrentUICulture, result), "Pair result", MessageBoxButton.OK);
            }
            Close();
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void pairingIp_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            connectionIp.Text = pairingIp.Text.Trim();
        }
    }
}

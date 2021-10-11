using Microsoft.WindowsAPICodePack.Net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GoProCSharpDev.Utils
{
    class WifiUtils
    {
        public static List<Network> GetConnectedWifiSsid()
        {
            List<Network> networkConnected = new List<Network>();
            NetworkCollection networks = NetworkListManager.GetNetworks(NetworkConnectivityLevels.Connected);
            foreach (Network network in networks)
            {
                string isConnected = network.IsConnected ? " (connected)" : " (disconnected)";
                Debug.Print("Network : " + network.Name + " - Category : " + network.Category.ToString() + isConnected);
                if (network.IsConnected) networkConnected.Add(network);
            }
            return networkConnected;
        }

        public static bool IsGoProWifiConnected(string goproSsid)
        {
            var connectedNetworks = GetConnectedWifiSsid();
            foreach (Network network in connectedNetworks)
            {
                if (network.IsConnected && network.Description.Equals(goproSsid)) return true;
            }
            return false;
        }
    }
}

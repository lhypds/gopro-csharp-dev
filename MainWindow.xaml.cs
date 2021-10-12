/* MainWindow.xaml.cs/Open GoPro, Version 2.0 (C) Copyright 2021 GoPro, Inc. (http://gopro.com/OpenGoPro). */
/* This copyright was auto-generated on Wed, Sep  1, 2021  5:05:38 PM */

using GoProCSharpDev.Utils;
using MediaDevices;
using Microsoft.WindowsAPICodePack.Net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace GoProCSharpDev
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Device Information
        public class GDeviceInformation
        {
            public GDeviceInformation(DeviceInformation inDeviceInformation, bool inPresent, bool inConnected)
            {
                DeviceInfo = inDeviceInformation;
                IsPresent = inPresent;
                IsConnected = inConnected;
            }

            public DeviceInformation DeviceInfo { get; set; } = null;

            public bool IsPresent { get; set; } = false;

            public bool IsConnected { get; set; } = false;

            public bool IsVisible { get { return IsPresent || IsConnected; } }

            private GDeviceInformation() { }
        }

        public ObservableCollection<GDeviceInformation> Devices { get; set; } = new ObservableCollection<GDeviceInformation>();

        // Encoding
        private bool _Encoding = false;

        public bool Encoding
        {
            get => _Encoding;
            set { _Encoding = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Encoding")); }
        }

        // Battery Level
        private int _Batterylevel = 0;

        public int BatteryLevel
        {
            get => _Batterylevel;
            set { _Batterylevel = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("BatteryLevel")); }
        }

        // Wifi
        private bool _WifiStatus = false;

        public bool WifiStatus
        {
            get => _WifiStatus;
            set { _WifiStatus = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("WifiStatus")); }
        }

        // Is Bluetooth Connected
        private bool _IsBluetoothConnected = false;

        public bool IsBluetoothConnected
        {
            get => _IsBluetoothConnected;
            set { _IsBluetoothConnected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsBluetoothConnected")); }
        }

        // Battery Level
        private int _Zoomlevel = 0;

        public int ZoomLevel
        {
            get => _Zoomlevel;
            set { _Zoomlevel = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Zoomlevel")); }
        }

        // Resolution
        private string _Resolution = "";

        public string Resolution
        {
            get => _Resolution;
            set { _Resolution = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Resolution")); }
        }

        // Bluetooth
        private BluetoothLEDevice _BleDevice = null;

        // GATT Characteristics
        public GattCharacteristic _GattWifiApSsid = null;
        public GattCharacteristic _GattWifiApPass = null;
        public GattCharacteristic _GattSendCmds = null;
        public GattCharacteristic _GattNotifyCmds = null;
        public GattCharacteristic _GattSetSettings = null;
        public GattCharacteristic _GattNotifySettings = null;
        public GattCharacteristic _GattSendQueries = null;
        public GattCharacteristic _GattNotifyQueryResp = null;

        // Wifi AP
        private string _WifiApSsidS = "";

        // Bluetooth GATT Characteristic
        private readonly List<byte> _QueryBuf = new List<byte>();
        private int _ExpectedLengthQuery = 0;
        private readonly List<byte> _CommandBuf = new List<byte>();
        private int _ExpectedLengthCmd = 0;
        private readonly List<byte> _SettingBuf = new List<byte>();
        private int _ExpectedLengthSet = 0;

        // Devices
        private DeviceWatcher _BluetoothDeviceWatcher = null;
        private readonly Dictionary<string, DeviceInformation> _AllDevices = new Dictionary<string, DeviceInformation>();
        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            InitializeComponent();
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }

        private void UpdateStatusBar(string status)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                txtStatusBar.Text = status;
            }));
        }

        #region Bluetooth

        // Scannning Devices

        private void BtnScanBle_Click(object sender, RoutedEventArgs e)
        {
            if (lbDevices.Items.Count > 0) lbDevices.Items.Clear();

            string BleSelector = "System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\"";
            DeviceInformationKind deviceInformationKind = DeviceInformationKind.AssociationEndpoint;
            string[] requiredProperties = { "System.Devices.Aep.Bluetooth.Le.IsConnectable", "System.Devices.Aep.IsConnected" };

            _BluetoothDeviceWatcher = DeviceInformation.CreateWatcher(BleSelector, requiredProperties, deviceInformationKind);
            _BluetoothDeviceWatcher.Added += BluetoothDeviceWatcher_Added;
            _BluetoothDeviceWatcher.Updated += BluetoothDeviceWatcher_Updated;
            _BluetoothDeviceWatcher.Removed += BluetoothDeviceWatcher_Removed;
            _BluetoothDeviceWatcher.EnumerationCompleted += BluetoothDeviceWatcher_EnumerationCompleted;
            _BluetoothDeviceWatcher.Stopped += BluetoothDeviceWatcher_Stopped;

            txtStatusBar.Text = "Scanning for devices...";
            _BluetoothDeviceWatcher.Start();
        }

        private void BluetoothDeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                txtStatusBar.Text = "Scan Stopped!";
            }));
        }

        private void BluetoothDeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                txtStatusBar.Text = "Scan Complete";
            }));
        }

        private void BluetoothDeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                if (Devices.FirstOrDefault(d => d.DeviceInfo.Id == args.Id) != null)
                    Devices.Remove(Devices.FirstOrDefault(d => d.DeviceInfo.Id == args.Id));
            }));
        }

        private void BluetoothDeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            bool isPresent = false, isConnected = false, found = false;

            if (args.Properties.ContainsKey("System.Devices.Aep.Bluetooth.Le.IsConnectable"))
            {
                isPresent = (bool)args.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"];
            }

            if (args.Properties.ContainsKey("System.Devices.Aep.IsConnected"))
            {
                isConnected = (bool)args.Properties["System.Devices.Aep.IsConnected"];
            }

            for (int i = 0; i < Devices.Count; i++)
            {
                if (Devices[i].DeviceInfo.Id == args.Id)
                {
                    found = true;
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Devices[i].DeviceInfo.Update(args);
                        Devices[i].IsPresent = isPresent;
                        Devices[i].IsConnected = isConnected;
                    }));
                    break;
                }
            }

            if (!found && (isPresent || isConnected))
            {
                if (_AllDevices.ContainsKey(args.Id))
                {
                    _AllDevices[args.Id].Update(args);
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Devices.Add(new GDeviceInformation(_AllDevices[args.Id], isPresent, isConnected));
                    }));
                }
            }
        }

        private void BluetoothDeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
        {
            bool isPresent = false;
            bool isConnected = false;

            if (args.Properties.ContainsKey("System.Devices.Aep.Bluetooth.Le.IsConnectable"))
            {
                isPresent = (bool)args.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"];
            }

            if (args.Properties.ContainsKey("System.Devices.Aep.IsConnected"))
            {
                isConnected = (bool)args.Properties["System.Devices.Aep.IsConnected"];
            }

            if (args.Name != "" && args.Name.Contains("GoPro"))
            {
                bool found = false;
                if (!_AllDevices.ContainsKey(args.Id))
                {
                    _AllDevices.Add(args.Id, args);
                }

                for (int i = 0; i < Devices.Count; i++)
                {
                    if (Devices[i].DeviceInfo.Id == args.Id)
                    {
                        found = true;
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            Devices[i].DeviceInfo = args;
                            Devices[i].IsPresent = isPresent;
                            Devices[i].IsConnected = isConnected;
                        }));
                        break;
                    }
                }

                if (!found && (isPresent || isConnected))
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        Devices.Add(new GDeviceInformation(args, isPresent, isConnected));
                    }));
                }
            }
        }

        // Pairing and Connecting

        private async void BtnPair_Click(object sender, RoutedEventArgs e)
        {
            GDeviceInformation lDevice = (GDeviceInformation)lbDevices.SelectedItem;
            if (lDevice != null)
            {
                UpdateStatusBar("Pairing started");
                _BleDevice = await BluetoothLEDevice.FromIdAsync(lDevice.DeviceInfo.Id);
                _BleDevice.DeviceInformation.Pairing.Custom.PairingRequested += Custom_PairingRequested;
                if (_BleDevice.DeviceInformation.Pairing.CanPair)
                {
                    DevicePairingProtectionLevel dppl = _BleDevice.DeviceInformation.Pairing.ProtectionLevel;
                    DevicePairingResult dpr = await _BleDevice.DeviceInformation.Pairing.Custom.PairAsync(DevicePairingKinds.ConfirmOnly, dppl);
                    UpdateStatusBar("Pairing result = " + dpr.Status.ToString());
                }
                else { UpdateStatusBar("Pairing failed"); }
            }
            else { UpdateStatusBar("Select a device"); }
        }

        private void Custom_PairingRequested(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            UpdateStatusBar("Pairing request...");
            args.Accept();
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            if (IsBluetoothConnected)
            {
                UpdateStatusBar("Deivce already connected");
                return;
            }
            else { UpdateStatusBar("Connecting..."); }

            GDeviceInformation deviceInfo = (GDeviceInformation)lbDevices.SelectedItem;
            if (deviceInfo == null)
            {
                UpdateStatusBar("No device selected");
                return;
            }

            // Get device
            _BleDevice = await BluetoothLEDevice.FromIdAsync(deviceInfo.DeviceInfo.Id);
            if (!_BleDevice.DeviceInformation.Pairing.IsPaired)
            {
                UpdateStatusBar("Device not paired");
                return;
            }

            // Get GATT services
            GattDeviceServicesResult gattDeviceServicesResult = await _BleDevice.GetGattServicesAsync();
            _BleDevice.ConnectionStatusChanged += BleDevice_ConnectionStatusChanged;

            if (gattDeviceServicesResult.Status == GattCommunicationStatus.Unreachable)
            {
                UpdateStatusBar("Connection unreachable");
                return;
            }

            if (gattDeviceServicesResult.Status == GattCommunicationStatus.AccessDenied)
            {
                UpdateStatusBar("Connection access denied");
                return;
            }

            if (gattDeviceServicesResult.Status == GattCommunicationStatus.ProtocolError)
            {
                UpdateStatusBar("Connection protocol error");
                return;
            }

            // Success, then get GATT Characteristics
            if (gattDeviceServicesResult.Status == GattCommunicationStatus.Success)
            {
                IReadOnlyList<GattDeviceService> services = gattDeviceServicesResult.Services;
                foreach (GattDeviceService gatt in services)
                {
                    GattCharacteristicsResult gattCharacteristicsResult = await gatt.GetCharacteristicsAsync();
                    if (gattCharacteristicsResult.Status == GattCommunicationStatus.Success)
                    {
                        IReadOnlyList<GattCharacteristic> characteristics = gattCharacteristicsResult.Characteristics;
                        foreach (GattCharacteristic characteristic in characteristics)
                        {
                            // Properties
                            GattCharacteristicProperties charProperties = characteristic.CharacteristicProperties;
                            if (charProperties.HasFlag(GattCharacteristicProperties.Read))
                            {
                              // This characteristic supports reading from it.
                            }

                            if (charProperties.HasFlag(GattCharacteristicProperties.Write))
                            {
                              // This characteristic supports writing to it.
                            }

                            if (charProperties.HasFlag(GattCharacteristicProperties.Notify))
                            {
                              // This characteristic supports subscribing to notifications.
                            }

                            // Services and Characteristics
                            // Refer: https://gopro.github.io/OpenGoPro/ble_1_0 for GoPro 9

                            // GoPro WiFi Access Point
                            // WiFi AP SSID
                            if (characteristic.Uuid.ToString().Equals(BluetoothUtils.GetUuid128("0002")))
                            {
                                Debug.Print("Set GATT Characteristic: WiFi AP SSID");
                                _GattWifiApSsid = characteristic;
                            }

                            // WiFi AP Password
                            if (characteristic.Uuid.ToString().Equals(BluetoothUtils.GetUuid128("0003")))
                            {
                                Debug.Print("Set GATT Characteristic: WiFi AP Password");
                                _GattWifiApPass = characteristic;
                            }

                            // Control and Query
                            // Command
                            if (characteristic.Uuid.ToString().Equals(BluetoothUtils.GetUuid128("0072")))
                            {
                                Debug.Print("Set GATT Characteristic: Command");
                                _GattSendCmds = characteristic;
                            }

                            // Command Response
                            if (characteristic.Uuid.ToString().Equals(BluetoothUtils.GetUuid128("0073")))
                            {
                                Debug.Print("Set GATT Characteristic: Command Response");
                                _GattNotifyCmds = characteristic;
                                try
                                {
                                    GattCommunicationStatus status = await _GattNotifyCmds.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                    if (status == GattCommunicationStatus.Success)
                                    {
                                        _GattNotifyCmds.ValueChanged += NotifyCommands_ValueChanged;
                                    }
                                    else { UpdateStatusBar("Failed to attach notify cmd " + status); }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message, "Exception");
                                }
                            }

                            // Settings
                            if (characteristic.Uuid.ToString().Equals(BluetoothUtils.GetUuid128("0074")))
                            {
                                Debug.Print("Set GATT Characteristic: Settings");
                                _GattSetSettings = characteristic;
                            }

                            // Settings Response
                            if (characteristic.Uuid.ToString().Equals(BluetoothUtils.GetUuid128("0075")))
                            {
                                Debug.Print("Set GATT Characteristic: Settings Response");
                                _GattNotifySettings = characteristic;
                                try
                                {
                                    GattCommunicationStatus status = await _GattNotifySettings.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                    if (status == GattCommunicationStatus.Success)
                                    {
                                        _GattNotifySettings.ValueChanged += NotifySettings_ValueChanged;
                                    }
                                    else { UpdateStatusBar("Failed to attach notify settings " + status); }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message, "Exception");
                                }
                            }

                            // Query
                            if (characteristic.Uuid.ToString().Equals(BluetoothUtils.GetUuid128("0076")))
                            {
                                Debug.Print("Set GATT Characteristic: Query");
                                _GattSendQueries = characteristic;
                            }

                            // Query Response
                            if (characteristic.Uuid.ToString().Equals(BluetoothUtils.GetUuid128("0077")))
                            {
                                Debug.Print("Set GATT Characteristic: Query Response");
                                _GattNotifyQueryResp = characteristic;
                                try
                                {
                                    GattCommunicationStatus status = await _GattNotifyQueryResp.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                    if (status == GattCommunicationStatus.Success)
                                    {
                                        _GattNotifyQueryResp.ValueChanged += NotifyQueryResp_ValueChanged;
                                        if (_GattSendQueries != null)
                                        {
                                            // Register for settings and status updates
                                            DataWriter writer = new DataWriter();
                                            writer.WriteBytes(new byte[] { 1, 0x52 });
                                            GattCommunicationStatus gat = await _GattSendQueries.WriteValueAsync(writer.DetachBuffer());

                                            writer = new DataWriter();
                                            writer.WriteBytes(new byte[] { 1, 0x53 });
                                            gat = await _GattSendQueries.WriteValueAsync(writer.DetachBuffer());
                                        }
                                        else { UpdateStatusBar("send queries was null!"); }
                                    }
                                    else { UpdateStatusBar("Failed to attach notify query " + status); }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message, "Exception");
                                }
                            }
                        }
                    }
                }
                SetThirdPartySource();
            }
        }

        private async void SetThirdPartySource()
        {
            DataWriter writer = new DataWriter();
            writer.WriteBytes(new byte[] { 0x01, 0x50 });

            GattCommunicationStatus res = GattCommunicationStatus.Unreachable;
            if (_GattSendCmds != null)
            {
                res = await _GattSendCmds.WriteValueAsync(writer.DetachBuffer());
            }

            if (res != GattCommunicationStatus.Success && _GattSendCmds != null)
            {
                UpdateStatusBar("Failed to set command source: " + res.ToString());
            }
        }

        // Bluetooth GATT Characteristic Notification Handlers
        // A GATT characteristic is a basic data element used to construct a GATT service

        private void BleResponse(string responseText)
        {
            string logText = "";
            TxtBleResponse.Dispatcher.Invoke(new Action(() => { logText = TxtBleResponse.Text; }));
            if (logText != null)
            {
                if (!logText.Equals(string.Empty))
                {
                    logText += "\r\n";
                }
            }
            logText += "[" + DateTime.Now.ToLongTimeString() + "] ";
            logText += responseText;
            TxtBleResponse.Dispatcher.Invoke(new Action(() => {
                TxtBleResponse.Text = logText;
                TxtBleResponse.ScrollToEnd();
            }));
        }

        private void NotifyCommands_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            DataReader reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] myBytes = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(myBytes);

            int newLength = ReadBytesIntoBuffer(myBytes, _CommandBuf);
            BleResponse("Command Response: " + BitConverter.ToString(myBytes));
            Debug.Print("Commands response recieved: " + BitConverter.ToString(myBytes));

            if (newLength > 0)
                _ExpectedLengthCmd = newLength;

            if (_ExpectedLengthCmd == _CommandBuf.Count)
            {
                _CommandBuf.Clear();
            }
        }

        private void NotifySettings_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] myBytes = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(myBytes);

            int newLength = ReadBytesIntoBuffer(myBytes, _SettingBuf);
            BleResponse("Setting Response: " + BitConverter.ToString(myBytes));
            Debug.Print("Setting changed response recieved: " + BitConverter.ToString(myBytes));

            if (newLength > 0)
            {
                _ExpectedLengthSet = newLength;
            }

            if (_ExpectedLengthSet == _SettingBuf.Count)
            {
                _SettingBuf.Clear();
            }
        }

        private void NotifyQueryResp_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            DataReader reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] myBytes = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(myBytes);

            int newLength = ReadBytesIntoBuffer(myBytes, _QueryBuf);
            BleResponse("Query Response: " + BitConverter.ToString(myBytes));
            Debug.Print("Query response recieved: " + BitConverter.ToString(myBytes));

            if (newLength > 0)
            {
                _ExpectedLengthQuery = newLength;
            }

            if (_ExpectedLengthQuery == _QueryBuf.Count)
            {
                // Check the first byte is 83(0x53) or 147(0x93)
                if ((_QueryBuf[0] == 0x53 || _QueryBuf[0] == 0x93) && _QueryBuf[1] == 0)
                {
                    // Status messages
                    for (int k = 0; k < _QueryBuf.Count;)
                    {
                        if (_QueryBuf[k] == 10)
                        {
                            Encoding = _QueryBuf[k + 2] > 0;
                            Debug.Print("Encoding: " + Encoding);
                        }

                        if (_QueryBuf[k] == 70)
                        {
                            BatteryLevel = _QueryBuf[k + 2];
                            Debug.Print("Battery Level: " + BatteryLevel);
                        }

                        if (_QueryBuf[k] == 69)
                        {
                            WifiStatus = _QueryBuf[k + 2] == 1;
                            Debug.Print("Wifi Status: " + WifiStatus);
                        }

                        if (_QueryBuf[k] == 75)
                        {
                            ZoomLevel = _QueryBuf[k + 2];
                            Debug.Print("Digital Zoom Level: " + ZoomLevel);
                        }
                        k += 2 + _QueryBuf[k + 1];
                    }
                }
                else
                {
                    // Unhandled Query Message
                }
                _QueryBuf.Clear();
                _ExpectedLengthQuery = 0;
            }
        }

        // Fill recieved bytes to target buffer
        private int ReadBytesIntoBuffer(byte[] bytes, List<byte> targetBuffer)
        {
            int returnLength = -1;
            int startbyte = 1;  // This will ignore the first byte
            int bytesLength = bytes.Length;

            if ((bytes[0] & 32) > 0)
            {
                // extended 13 bit header
                startbyte = 2;
                int len = ((bytes[0] & 0xF) << 8) | bytes[1];
                returnLength = len;
            }
            else if ((bytes[0] & 64) > 0)
            {
                // extended 16 bit header
                startbyte = 3;
                int len = (bytes[1] << 8) | bytes[2];
                returnLength = len;
            }
            else if ((bytes[0] & 128) > 0)
            {
                // its a continuation packet
            }
            else
            {
                // 8 bit header
                returnLength = bytes[0];
            }

            for (int k = startbyte; k < bytesLength; k++)
            {
                targetBuffer.Add(bytes[k]);
            }
            return returnLength;
        }

        private void BleDevice_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            UpdateStatusBar(sender.ConnectionStatus == BluetoothConnectionStatus.Connected ? "Connected" : "Disconnected");
            IsBluetoothConnected = sender.ConnectionStatus == BluetoothConnectionStatus.Connected;
        }

        // Bluetooth Funcitons

        // 1. GoPro WiFi Access Point

        private async void BtnReadWifiApName_Click(object sender, RoutedEventArgs e)
        {
            if (_GattWifiApSsid != null)
            {
                GattReadResult gattReadResult = await _GattWifiApSsid.ReadValueAsync();
                if (gattReadResult.Status == GattCommunicationStatus.Success)
                {
                    DataReader dataReader = DataReader.FromBuffer(gattReadResult.Value);
                    string result = dataReader.ReadString(gattReadResult.Value.Length);
                    txtAPName.Text = result;
                    _WifiApSsidS = result;
                    Debug.Print("Wifi AP SSID: " + result);
                }
                else { UpdateStatusBar("Failed to read ap name"); }
            }
            else { UpdateStatusBar("Not connected"); }
        }

        private async void BtnSetApName_Click(object sender, RoutedEventArgs e)
        {
            if (_GattWifiApSsid != null)
            {
                // Write
                DataWriter writer = new DataWriter();
                writer.WriteString(txtAPName.Text);
                GattWriteResult gattWriteResult = await _GattWifiApSsid.WriteValueWithResultAsync(writer.DetachBuffer());
                if (gattWriteResult.Status == GattCommunicationStatus.Success)
                {
                    Debug.Print("Wifi AP SSID updated to " + txtAPName.Text);
                }
                else { UpdateStatusBar("Failed to update ap name"); }

                // Read
                GattReadResult gattReadResult = await _GattWifiApSsid.ReadValueAsync();
                if (gattReadResult.Status == GattCommunicationStatus.Success)
                {
                    DataReader dataReader = DataReader.FromBuffer(gattReadResult.Value);
                    string result = dataReader.ReadString(gattReadResult.Value.Length);
                    txtAPName.Text = result;
                    _WifiApSsidS = result;
                    Debug.Print("Latest Wifi AP SSID: " + result);
                }
                else { UpdateStatusBar("Failed to read ap name"); }
            }
            else { UpdateStatusBar("Not connected"); }
        }

        private async void BtnReadWifiApPass_Click(object sender, RoutedEventArgs e)
        {
            if (_GattWifiApPass != null)
            {
                GattReadResult gattReadResult = await _GattWifiApPass.ReadValueAsync();
                if (gattReadResult.Status == GattCommunicationStatus.Success)
                {
                    DataReader dataReader = DataReader.FromBuffer(gattReadResult.Value);
                    string result = dataReader.ReadString(gattReadResult.Value.Length);
                    txtAPPassword.Text = result;
                    Debug.Print("Wifi AP Password: " + result);
                }
                else { UpdateStatusBar("Failed to read password"); }
            }
            else { UpdateStatusBar("Not connected"); }
        }

        private async void BtnSetApPass_Click(object sender, RoutedEventArgs e)
        {
            if (_GattWifiApPass != null)
            {
                // Write
                DataWriter writer = new DataWriter();
                writer.WriteString(txtAPPassword.Text);
                GattWriteResult gattWriteResult = await _GattWifiApPass.WriteValueWithResultAsync(writer.DetachBuffer());
                if (gattWriteResult.Status == GattCommunicationStatus.Success)
                {
                    Debug.Print("Wifi AP Password updated to " + txtAPPassword.Text);
                }
                else { UpdateStatusBar("Failed to update ap name"); }

                // Read
                GattReadResult gattReadResult = await _GattWifiApPass.ReadValueAsync();
                if (gattReadResult.Status == GattCommunicationStatus.Success)
                {
                    DataReader dataReader = DataReader.FromBuffer(gattReadResult.Value);
                    string result = dataReader.ReadString(gattReadResult.Value.Length);
                    txtAPPassword.Text = result;
                    Debug.Print("Latest Wifi AP password: " + result);
                }
                else { UpdateStatusBar("Failed to read ap name"); }
            }
            else { UpdateStatusBar("Not connected"); }
        }

        // 2. Control & Query

        // Send with GP-0072
        // Response with GP-0073
        private async void SendBleCommand(byte[] value, string function)
        {
            if (!IsBluetoothConnected)
            {
                UpdateStatusBar("Bluetooth not connected");
                return;
            }

            Debug.Print("Send command: " + function);
            DataWriter writer = new DataWriter();
            writer.WriteBytes(value);

            GattCommunicationStatus res = GattCommunicationStatus.Unreachable;
            if (_GattSendCmds != null)
            {
                res = await _GattSendCmds.WriteValueAsync(writer.DetachBuffer());
            }
            if (res != GattCommunicationStatus.Success)
            {
                UpdateStatusBar("Failed! Respose: " + res.ToString());
            }
        }

        // Send with GP-0074
        // Response with GP-0075
        private async void SendBleSetting(byte[] value, string function)
        {
            if (!IsBluetoothConnected)
            {
                UpdateStatusBar("Bluetooth not connected");
                return;
            }

            Debug.Print("Send setting: " + function);
            DataWriter writer = new DataWriter();
            writer.WriteBytes(value);

            GattCommunicationStatus res = GattCommunicationStatus.Unreachable;
            if (_GattSetSettings != null)
            {
                res = await _GattSetSettings.WriteValueAsync(writer.DetachBuffer());
            }
            if (res != GattCommunicationStatus.Success)
            {
                UpdateStatusBar("Failed! Respose: " + res.ToString());
            }
        }

        private void BtnTurnWifiOn_Click(object sender, RoutedEventArgs e)
        {
            int onOff = 1;
            SendBleCommand(new byte[] { 0x03, 0x17, 0x01, (byte)onOff }, "Wifi On");
        }

        private void BtnTurnWifiOff_Click(object sender, RoutedEventArgs e)
        {
            int onOff = 0;
            SendBleCommand(new byte[] { 0x03, 0x17, 0x01, (byte)onOff }, "Wifi Off");
        }

        private void BtnShutterOn_Click(object sender, RoutedEventArgs e)
        {
            int onOff = 1;
            SendBleCommand(new byte[] { 0x03, 0x01, 0x01, (byte)onOff }, "Shutter On");
        }

        private void BtnShutterOff_Click(object sender, RoutedEventArgs e)
        {
            int onOff = 0;
            SendBleCommand(new byte[] { 0x03, 0x01, 0x01, (byte)onOff }, "Shutter Off");
        }

        private void BtnSleep_Click(object sender, RoutedEventArgs e)
        {
            SendBleCommand(new byte[] { 0x01, 0x05 }, "Put camera to sleep");
        }

        private void BtnOpenGoProVersion_Click(object sender, RoutedEventArgs e)
        {
            SendBleCommand(new byte[] { 0x01, 0x51 }, "Get Open GoPro version");
        }

        private void BtnHardwareInfo_Click(object sender, RoutedEventArgs e)
        {
            SendBleCommand(new byte[] { 0x01, 0x3C }, "Get camera hardware info");
        }

        private void CmbItemVideo_Selected(object sender, RoutedEventArgs e)
        {
            SendBleCommand(new byte[] { 0x04, 0x3E, 0x02, 0x03, 0xE8 }, "Presets: Load Group - Video");
        }

        private void CmbItemPhoto_Selected(object sender, RoutedEventArgs e)
        {
            SendBleCommand(new byte[] { 0x04, 0x3E, 0x02, 0x03, 0xE8 }, "Presets: Load Group - Photo");
        }

        private void CmbItemTimelapse_Selected(object sender, RoutedEventArgs e)
        {
            SendBleCommand(new byte[] { 0x04, 0x3E, 0x02, 0x03, 0xEA }, "Presets: Load Group - Timelapse");
        }

        private void BtnBleKeepAlive_Click(object sender, RoutedEventArgs e)
        {
            SendBleSetting(new byte[] { 0x03, 0x5B, 0x01, 0x42 }, "Keep Alive");
        }

        private void CmbItem4K_Selected(object sender, RoutedEventArgs e)
        {
            SendBleSetting(new byte[] { 0x03, 0x02, 0x01, 0x01 }, "Set video resolution (id: 2) to 4k (value: 1)");
        }

        private void CmbItem5K_Selected(object sender, RoutedEventArgs e)
        {
            SendBleSetting(new byte[] { 0x03, 0x02, 0x01, 0x18 }, "Set video resolution (id: 2) to 5k (value: 24)");
        }

        private void CmbItem1080_Selected(object sender, RoutedEventArgs e)
        {
            SendBleSetting(new byte[] { 0x03, 0x02, 0x01, 0x09 }, "Set video resolution (id: 2) to 1080 (value: 9)");
        }

        private void CmbItem1440_Selected(object sender, RoutedEventArgs e)
        {
            SendBleSetting(new byte[] { 0x03, 0x02, 0x01, 0x07 }, "Set video resolution (id: 2) to 1440 (value: 7)");
        }

        #endregion Bluetooth

        #region Wifi

        private void WebResponse(string responseText)
        {
            TxtWebResponse.Dispatcher.Invoke(new Action(() => {
                TxtWebResponse.Text = responseText;
            }));
        }

        private void BtnSendApiRequest_Click(object sender, RoutedEventArgs e)
        {
            if (_WifiApSsidS.Equals(string.Empty))
            {
                TxtWebResponse.Text = "Please get Wifi AP SSID";
                return;
            }

            if (!WifiUtils.IsGoProWifiConnected(_WifiApSsidS))
            {
                TxtWebResponse.Text = "This SSID for GoPro WIFI is not connected";
                return;
            }
            TxtWebResponse.Text = "Waiting for response...";

            bool useAsync = true;
            if (TxtRequestUrl.Text.Contains("gpmf"))
            {
                // File response
                WebResponse(WebRequestUtils.Get(TxtRequestUrl.Text, Path.Combine(TxtOutputFolderPath.Text, "FILE_" + TxtFileName.Text + "_GPMF"), useAsync));
            }
            else if (TxtRequestUrl.Text.Contains("screennail"))
            {
                // JPEG file response for screennail
                WebResponse(WebRequestUtils.Get(TxtRequestUrl.Text, Path.Combine(TxtOutputFolderPath.Text, "FILE_" + TxtFileName.Text + "_SCREENNAIL.JPEG"), useAsync));
            }
            else if (TxtRequestUrl.Text.Contains("thumbnail"))
            {
                // JPEG file response for thumbnail
                WebResponse(WebRequestUtils.Get(TxtRequestUrl.Text, Path.Combine(TxtOutputFolderPath.Text, "FILE_" + TxtFileName.Text + "_THUMBNAIL.JPEG"), useAsync));
            }
            else if (TxtRequestUrl.Text.Contains("8080"))
            {
                // Get File from SD card with GoPro server
                WebResponse(WebRequestUtils.Get(TxtRequestUrl.Text, Path.Combine(TxtOutputFolderPath.Text, TxtFileName.Text), useAsync));
            }
            else
            {
                // Text response
                WebResponse(WebRequestUtils.Get(TxtRequestUrl.Text));
            }
        }

        private void BtnOpenOutput_Click(object sender, RoutedEventArgs e)
        {
            if (TxtOutputFolderPath.Text.Equals(string.Empty)) return;
            if (!Directory.Exists(TxtOutputFolderPath.Text))
                Directory.CreateDirectory(TxtOutputFolderPath.Text);
            Process.Start(TxtOutputFolderPath.Text);
        }

        private void BtnListMedia_Click(object sender, RoutedEventArgs e)
        {
            if (!WebRequestUtils.ValidateIPv4(TxtIpAddress.Text)) { UpdateStatusBar("Please input valid IP Address"); return; }
            string requestSuffix = "/gopro/media/list";
            TxtRequestUrl.Text = "http://" + TxtIpAddress.Text + requestSuffix;
        }

        private void BtnGetMediaFile_Click(object sender, RoutedEventArgs e)
        {
            if (!WebRequestUtils.ValidateIPv4(TxtIpAddress.Text)) { UpdateStatusBar("Please input valid IP Address"); return; }
            string requestSuffix = "/gopro/media/gpmf?path=100GOPRO/";
            TxtRequestUrl.Text = "http://" + TxtIpAddress.Text + requestSuffix + TxtFileName.Text;
        }

        private void BtnWifiKeepAlive_Click(object sender, RoutedEventArgs e)
        {
            if (!WebRequestUtils.ValidateIPv4(TxtIpAddress.Text)) { UpdateStatusBar("Please input valid IP Address"); return; }
            string requestSuffix = "/gopro/camera/keep_alive";
            TxtRequestUrl.Text = "http://" + TxtIpAddress.Text + requestSuffix;
        }

        private void BtnGetThumbnail_Click(object sender, RoutedEventArgs e)
        {
            if (!WebRequestUtils.ValidateIPv4(TxtIpAddress.Text)) { UpdateStatusBar("Please input valid IP Address"); return; }
            string requestSuffix = "/gopro/media/thumbnail?path=100GOPRO/";
            TxtRequestUrl.Text = "http://" + TxtIpAddress.Text + requestSuffix + TxtFileName.Text;
        }

        private void BtnGetInfo_Click(object sender, RoutedEventArgs e)
        {
            if (!WebRequestUtils.ValidateIPv4(TxtIpAddress.Text)) { UpdateStatusBar("Please input valid IP Address"); return; }
            string requestSuffix = "/gopro/media/info?path=100GOPRO/";
            TxtRequestUrl.Text = "http://" + TxtIpAddress.Text + requestSuffix + TxtFileName.Text;
        }

        private void BtnGetScreennail_Click(object sender, RoutedEventArgs e)
        {
            if (!WebRequestUtils.ValidateIPv4(TxtIpAddress.Text)) { UpdateStatusBar("Please input valid IP Address"); return; }
            string requestSuffix = "/gopro/media/screennail?path=100GOPRO/";
            TxtRequestUrl.Text = "http://" + TxtIpAddress.Text + requestSuffix + TxtFileName.Text;
        }

        private void BtnDigitalZoom_Click(object sender, RoutedEventArgs e)
        {
            if (!WebRequestUtils.ValidateIPv4(TxtIpAddress.Text)) { UpdateStatusBar("Please input valid IP Address"); return; }
            string requestSuffix = "/gopro/camera/digital_zoom?percent=";
            int.TryParse(TxtDigitalZoomPercent.Text, out int percent);
            TxtRequestUrl.Text = "http://" + TxtIpAddress.Text + requestSuffix + percent;
        }

        private void BtnGet_Click(object sender, RoutedEventArgs e)
        {
            if (!WebRequestUtils.ValidateIPv4(TxtIpAddress.Text)) { UpdateStatusBar("Please input valid IP Address"); return; }
            string requestSuffix = ":8080/videos/DCIM/100GOPRO/";
            TxtRequestUrl.Text = "http://" + TxtIpAddress.Text + requestSuffix + TxtFileName.Text;
        }

        #endregion Wifi

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            IEnumerable<MediaDevice> mtpDevices = MediaDevice.GetDevices();
            if (mtpDevices.FirstOrDefault(d => d.Description == "HERO9 BLACK") == null)
            {
                UpdateStatusBar("Cannot find MTP device");
                return;
            }

            using (MediaDevice device = mtpDevices.FirstOrDefault(d => d.Description == "HERO9 BLACK"))
            {
                device.Connect();
                MediaDirectoryInfo photoDir = device.GetDirectoryInfo(@"\GoPro MTP Client Disk Volume\DCIM\100GOPRO");
                IEnumerable<MediaFileInfo> files = photoDir.EnumerateFiles("*.*", SearchOption.AllDirectories);
                foreach (MediaFileInfo file in files)
                {
                    if (file.Name.Equals(TxtFileName.Text))
                    {
                        MemoryStream memoryStream = new MemoryStream();
                        device.DownloadFile(file.FullName, memoryStream);
                        memoryStream.Position = 0;
                        WriteSreamToDisk(Path.Combine(TxtOutputFolderPath.Text, file.Name), memoryStream);
                        UpdateStatusBar("File copied " + file.Name);
                    }
                }
                device.Disconnect();
            }
        }

        private static void WriteSreamToDisk(string filePath, MemoryStream memoryStream)
        {
            using (FileStream file = new FileStream(filePath, FileMode.Create, System.IO.FileAccess.Write))
            {
                byte[] bytes = new byte[memoryStream.Length];
                memoryStream.Read(bytes, 0, (int)memoryStream.Length);
                file.Write(bytes, 0, bytes.Length);
                memoryStream.Close();
            }
        }
    }

    public class BrushBoolColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Color color = !(bool)value ? Color.FromRgb(255, 100, 100) : Color.FromRgb(100, 220, 100);
            return new SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

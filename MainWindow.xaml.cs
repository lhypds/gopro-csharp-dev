/* MainWindow.xaml.cs/Open GoPro, Version 2.0 (C) Copyright 2021 GoPro, Inc. (http://gopro.com/OpenGoPro). */
/* This copyright was auto-generated on Wed, Sep  1, 2021  5:05:38 PM */

using GoProCSharpDev.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
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
        private bool _WifiOn = false;

        public bool WifiOn
        {
            get => _WifiOn;
            set { _WifiOn = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("WifiOn")); }
        }

        // Is Bluetooth Connected
        private bool _IsBluetoothConnected = false;

        public bool IsBluetoothConnected
        {
            get => _IsBluetoothConnected;
            set { _IsBluetoothConnected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("IsBluetoothConnected")); }
        }

        // Bluetooth
        private BluetoothLEDevice _BleDevice = null;

        // GATT Characteristics
        public GattCharacteristic _WifiApSsid = null;
        public GattCharacteristic _WifiApPass = null;
        public GattCharacteristic _SendCmds = null;
        public GattCharacteristic _NotifyCmds = null;
        public GattCharacteristic _SetSettings = null;
        public GattCharacteristic _NotifySettings = null;
        public GattCharacteristic _SendQueries = null;
        public GattCharacteristic _NotifyQueryResp = null;

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

        private void UpateStatusBar(string status)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.txtStatusBar.Text = status;
            }));
        }

        #region Bluetooth

        // Scannning Devices

        private void BtnScanBle_Click(object sender, RoutedEventArgs e)
        {
            string BleSelector = "System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\"";
            DeviceInformationKind deviceInformationKind = DeviceInformationKind.AssociationEndpoint;
            string[] requiredProperties = { "System.Devices.Aep.Bluetooth.Le.IsConnectable", "System.Devices.Aep.IsConnected" };

            _BluetoothDeviceWatcher = DeviceInformation.CreateWatcher(BleSelector, requiredProperties, deviceInformationKind);
            _BluetoothDeviceWatcher.Added += BluetoothDeviceWatcher_Added;
            _BluetoothDeviceWatcher.Updated += BluetoothDeviceWatcher_Updated;
            _BluetoothDeviceWatcher.Removed += BluetoothDeviceWatcher_Removed;
            _BluetoothDeviceWatcher.EnumerationCompleted += BluetoothDeviceWatcher_EnumerationCompleted;
            _BluetoothDeviceWatcher.Stopped += BluetoothDeviceWatcher_Stopped;

            this.txtStatusBar.Text = "Scanning for devices...";
            _BluetoothDeviceWatcher.Start();
        }

        private void BluetoothDeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.txtStatusBar.Text = "Scan Stopped!";
            }));
        }

        private void BluetoothDeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.txtStatusBar.Text = "Scan Complete";
            }));
        }

        private void BluetoothDeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            for (int i = 0; i < Devices.Count; i++)
            {
                if (Devices[i].DeviceInfo.Id == args.Id)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => { Devices.RemoveAt(i); }));
                    break;
                }
            }
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
                UpateStatusBar("Pairing started");
                _BleDevice = await BluetoothLEDevice.FromIdAsync(lDevice.DeviceInfo.Id);
                _BleDevice.DeviceInformation.Pairing.Custom.PairingRequested += Custom_PairingRequested;
                if (_BleDevice.DeviceInformation.Pairing.CanPair)
                {
                    DevicePairingProtectionLevel dppl = _BleDevice.DeviceInformation.Pairing.ProtectionLevel;
                    DevicePairingResult dpr = await _BleDevice.DeviceInformation.Pairing.Custom.PairAsync(DevicePairingKinds.ConfirmOnly, dppl);
                    UpateStatusBar("Pairing result = " + dpr.Status.ToString());
                }
                else { UpateStatusBar("Pairing failed"); }
            }
            else { UpateStatusBar("Select a device"); }
        }

        private void Custom_PairingRequested(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            UpateStatusBar("Pairing request...");
            args.Accept();
        }

        private async void BtnConnect_Click(object sender, RoutedEventArgs e)
        {
            UpateStatusBar("Connecting...");
            GDeviceInformation deviceInfo = (GDeviceInformation)lbDevices.SelectedItem;
            if (deviceInfo == null)
            {
                UpateStatusBar("No device selected");
                return;
            }

            // Get device
            _BleDevice = await BluetoothLEDevice.FromIdAsync(deviceInfo.DeviceInfo.Id);
            if (!_BleDevice.DeviceInformation.Pairing.IsPaired)
            {
                UpateStatusBar("Device not paired");
                return;
            }

            // Get GATT services
            GattDeviceServicesResult gattDeviceServicesResult = await _BleDevice.GetGattServicesAsync();
            _BleDevice.ConnectionStatusChanged += BleDevice_ConnectionStatusChanged;

            if (gattDeviceServicesResult.Status == GattCommunicationStatus.Unreachable)
            {
                UpateStatusBar("Connection unreachable");
                return;
            }

            if (gattDeviceServicesResult.Status == GattCommunicationStatus.AccessDenied)
            {
                UpateStatusBar("Connection access denied");
                return;
            }

            if (gattDeviceServicesResult.Status == GattCommunicationStatus.ProtocolError)
            {
                UpateStatusBar("Connection protocol error");
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
                                _WifiApSsid = characteristic;
                            }

                            // WiFi AP Password
                            if (characteristic.Uuid.ToString().Equals(BluetoothUtils.GetUuid128("0003")))
                            {
                                Debug.Print("Set GATT Characteristic: WiFi AP Password");
                                _WifiApPass = characteristic;
                            }

                            // Control and Query
                            // Command
                            if (characteristic.Uuid.ToString().Equals(BluetoothUtils.GetUuid128("0072")))
                            {
                                Debug.Print("Set GATT Characteristic: Command");
                                _SendCmds = characteristic;
                            }

                            // Command Response
                            if (characteristic.Uuid.ToString().Equals(BluetoothUtils.GetUuid128("0073")))
                            {
                                Debug.Print("Set GATT Characteristic: Command Response");
                                _NotifyCmds = characteristic;
                                GattCommunicationStatus status = await _NotifyCmds.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                if (status == GattCommunicationStatus.Success)
                                {
                                    _NotifyCmds.ValueChanged += NotifyCommands_ValueChanged;
                                }
                                else { UpateStatusBar("Failed to attach notify cmd " + status); }
                            }

                            // Settings
                            if (characteristic.Uuid.ToString().Equals(BluetoothUtils.GetUuid128("0074")))
                            {
                                Debug.Print("Set GATT Characteristic: Settings");
                                _SetSettings = characteristic;
                            }

                            // Settings Response
                            if (characteristic.Uuid.ToString().Equals(BluetoothUtils.GetUuid128("0075")))
                            {
                                Debug.Print("Set GATT Characteristic: Settings Response");
                                _NotifySettings = characteristic;
                                GattCommunicationStatus status = await _NotifySettings.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                if (status == GattCommunicationStatus.Success)
                                {
                                    _NotifySettings.ValueChanged += NotifySettings_ValueChanged;
                                }
                                else { UpateStatusBar("Failed to attach notify settings " + status); }
                            }

                            // Query
                            if (characteristic.Uuid.ToString().Equals(BluetoothUtils.GetUuid128("0076")))
                            {
                                Debug.Print("Set GATT Characteristic: Query");
                                _SendQueries = characteristic;
                            }

                            // Query Response
                            if (characteristic.Uuid.ToString().Equals(BluetoothUtils.GetUuid128("0077")))
                            {
                                Debug.Print("Set GATT Characteristic: Query Response");
                                _NotifyQueryResp = characteristic;
                                GattCommunicationStatus status = await _NotifyQueryResp.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                if (status == GattCommunicationStatus.Success)
                                {
                                    _NotifyQueryResp.ValueChanged += NotifyQueryResp_ValueChanged;
                                    if (_SendQueries != null)
                                    {
                                        // Register for settings and status updates
                                        DataWriter writer = new DataWriter();
                                        writer.WriteBytes(new byte[] { 1, 0x52 });
                                        GattCommunicationStatus gat = await _SendQueries.WriteValueAsync(writer.DetachBuffer());

                                        writer = new DataWriter();
                                        writer.WriteBytes(new byte[] { 1, 0x53 });
                                        gat = await _SendQueries.WriteValueAsync(writer.DetachBuffer());
                                    }
                                    else { UpateStatusBar("send queries was null!"); }
                                }
                                else { UpateStatusBar("Failed to attach notify query " + status); }
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
            if (_SendCmds != null)
            {
                res = await _SendCmds.WriteValueAsync(writer.DetachBuffer());
            }

            if (res != GattCommunicationStatus.Success && _SendCmds != null)
            {
                UpateStatusBar("Failed to set command source: " + res.ToString());
            }
        }

        // Bluetooth GATT Characteristic Notification Handlers
        // A GATT characteristic is a basic data element used to construct a GATT service

        private void NotifyCommands_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            Debug.Print("Commands notification recieved");
            DataReader reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] myBytes = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(myBytes);
            int newLength = ReadBytesIntoBuffer(myBytes, _CommandBuf);
            if (newLength > 0)
                _ExpectedLengthCmd = newLength;

            if (_ExpectedLengthCmd == _CommandBuf.Count)
            {
                /*
                if (mBufCmd[0] == 0xXX)
                {

                }
                */
                _CommandBuf.Clear();
            }
        }

        private void NotifySettings_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            Debug.Print("Setting changed notification recieved");
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] myBytes = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(myBytes);
            int newLength = ReadBytesIntoBuffer(myBytes, _SettingBuf);
            if (newLength > 0)
                _ExpectedLengthSet = newLength;

            if (_ExpectedLengthSet == _SettingBuf.Count)
            {
                /*
                if (mBufSet[0] == 0xXX)
                {

                }
                */
                _SettingBuf.Clear();
            }
        }

        private void NotifyQueryResp_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            Debug.Print("Query response notification recieved");
            DataReader reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] myBytes = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(myBytes);

            int newLength = ReadBytesIntoBuffer(myBytes, _QueryBuf);
            if (newLength > 0)
            {
                _ExpectedLengthQuery = newLength;
            }

            if (_ExpectedLengthQuery == _QueryBuf.Count)
            {
                if ((_QueryBuf[0] == 0x53 || _QueryBuf[0] == 0x93) && _QueryBuf[1] == 0)
                {
                    // Status messages
                    for (int k = 0; k < _QueryBuf.Count;)
                    {
                        if (_QueryBuf[k] == 10) Encoding = _QueryBuf[k + 2] > 0;
                        if (_QueryBuf[k] == 70) BatteryLevel = _QueryBuf[k + 2];
                        if (_QueryBuf[k] == 69) WifiOn = _QueryBuf[k + 2] == 1;
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

        private int ReadBytesIntoBuffer(byte[] bytes, List<byte> targetBuffer)
        {
            int returnLength = -1;
            int startbyte = 1;
            int theseBytes = bytes.Length;

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

            for (int k = startbyte; k < theseBytes; k++)
            {
                targetBuffer.Add(bytes[k]);
            }
            return returnLength;
        }

        private void BleDevice_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            UpateStatusBar(sender.ConnectionStatus == BluetoothConnectionStatus.Connected ? "Connected" : "Disconnected");
            IsBluetoothConnected = sender.ConnectionStatus == BluetoothConnectionStatus.Connected;
        }

        // Bluetooth Funcitons

        private async void BtnReadWifiApName_Click(object sender, RoutedEventArgs e)
        {
            if (_WifiApSsid != null)
            {
                GattReadResult res = await _WifiApSsid.ReadValueAsync();
                if (res.Status == GattCommunicationStatus.Success)
                {
                    DataReader dataReader = DataReader.FromBuffer(res.Value);
                    string output = dataReader.ReadString(res.Value.Length);
                    txtAPName.Text = output;
                }
                else { UpateStatusBar("Failed to read ap name"); }
            }
            else { UpateStatusBar("Not connected"); }
        }

        private async void BtnReadWifiApPass_Click(object sender, RoutedEventArgs e)
        {
            if (_WifiApPass != null)
            {
                GattReadResult res = await _WifiApPass.ReadValueAsync();
                if (res.Status == GattCommunicationStatus.Success)
                {
                    DataReader dataReader = DataReader.FromBuffer(res.Value);
                    string output = dataReader.ReadString(res.Value.Length);
                    txtAPPassword.Text = output;
                }
                else { UpateStatusBar("Failed to read password"); }
            }
            else { UpateStatusBar("Not connected"); }
        }

        private async void SendBleCommand(byte[] value, string function)
        {
            Debug.Print("Send command: " + function);
            DataWriter writer = new DataWriter();
            writer.WriteBytes(value);

            GattCommunicationStatus res = GattCommunicationStatus.Unreachable;
            if (_SendCmds != null)
            {
                res = await _SendCmds.WriteValueAsync(writer.DetachBuffer());
            }
            if (res != GattCommunicationStatus.Success)
            {
                UpateStatusBar("Failed! Respose: " + res.ToString());
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
        private void BtnTimelapseMode_Click(object sender, RoutedEventArgs e)
        {
            SendBleCommand(new byte[] { 0x04, 0x3E, 0x02, 0x03, 0xEA }, "Presets: Load Group - Timelapse");
        }

        private void BtnPhotoMode_Click(object sender, RoutedEventArgs e)
        {
            SendBleCommand(new byte[] { 0x04, 0x3E, 0x02, 0x03, 0xE8 }, "Presets: Load Group - Photo");
        }

        private void BtnVideoMode_Click(object sender, RoutedEventArgs e)
        {
            SendBleCommand(new byte[] { 0x04, 0x3E, 0x02, 0x03, 0xE8 }, "Presets: Load Group - Video");
        }

        #endregion Bluetooth
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

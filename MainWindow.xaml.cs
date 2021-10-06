/* MainWindow.xaml.cs/Open GoPro, Version 2.0 (C) Copyright 2021 GoPro, Inc. (http://gopro.com/OpenGoPro). */
/* This copyright was auto-generated on Wed, Sep  1, 2021  5:05:38 PM */

﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        public GattCharacteristic _NotifyCmds = null;
        public GattCharacteristic _SendCmds = null;
        public GattCharacteristic _SetSettings = null;
        public GattCharacteristic _NotifySettings = null;
        public GattCharacteristic _SendQueries = null;
        public GattCharacteristic _NotifyQueryResp = null;
        public GattCharacteristic _ReadApName = null;
        public GattCharacteristic _ReadApPass = null;

        // Bluetooth GATT Characteristic
        private readonly List<byte> _BufQ = new List<byte>();
        private int _ExpectedLengthQ = 0;
        private readonly List<byte> _BufCmd = new List<byte>();
        private int _ExpectedLengthCmd = 0;
        private readonly List<byte> _BufSet = new List<byte>();
        private int _ExpectedLengthSet = 0;

        // Devices
        private DeviceWatcher _DeviceWatcher = null;
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

        private void BtnScanBle_Click(object sender, RoutedEventArgs e)
        {
            string BleSelector = "System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\"";
            DeviceInformationKind deviceInformationKind = DeviceInformationKind.AssociationEndpoint;
            string[] requiredProperties = { "System.Devices.Aep.Bluetooth.Le.IsConnectable", "System.Devices.Aep.IsConnected" };

            _DeviceWatcher = DeviceInformation.CreateWatcher(BleSelector, requiredProperties, deviceInformationKind);
            _DeviceWatcher.Added += DeviceWatcher_Added;
            _DeviceWatcher.Updated += DeviceWatcher_Updated;
            _DeviceWatcher.Removed += DeviceWatcher_Removed;
            _DeviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            _DeviceWatcher.Stopped += DeviceWatcher_Stopped;

            this.txtStatusBar.Text = "Scanning for devices...";
            _DeviceWatcher.Start();
        }

        #region Device Watcher Event Handlers

        private void DeviceWatcher_Stopped(DeviceWatcher sender, object args)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.txtStatusBar.Text = "Scan Stopped!";
            }));
        }

        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.txtStatusBar.Text = "Scan Complete";
            }));
        }

        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate args)
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

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate args)
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

        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation args)
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

        #endregion

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

            _BleDevice = await BluetoothLEDevice.FromIdAsync(deviceInfo.DeviceInfo.Id);
            if (!_BleDevice.DeviceInformation.Pairing.IsPaired)
            {
                UpateStatusBar("Device not paired");
                return;
            }

            GattDeviceServicesResult result = await _BleDevice.GetGattServicesAsync();
            _BleDevice.ConnectionStatusChanged += BleDevice_ConnectionStatusChanged;

            if (result.Status == GattCommunicationStatus.Success)
            {
                IReadOnlyList<GattDeviceService> services = result.Services;
                foreach (GattDeviceService gatt in services)
                {
                    GattCharacteristicsResult res = await gatt.GetCharacteristicsAsync();
                    if (res.Status == GattCommunicationStatus.Success)
                    {
                        IReadOnlyList<GattCharacteristic> characteristics = res.Characteristics;
                        foreach (GattCharacteristic characteristic in characteristics)
                        {
                            GattCharacteristicProperties properties = characteristic.CharacteristicProperties;
                            if (properties.HasFlag(GattCharacteristicProperties.Read))
                            {
                                // This characteristic supports reading from it.
                            }

                            if (properties.HasFlag(GattCharacteristicProperties.Write))
                            {
                                // This characteristic supports writing to it.
                            }

                            if (properties.HasFlag(GattCharacteristicProperties.Notify))
                            {
                                // This characteristic supports subscribing to notifications.
                            }

                            if (characteristic.Uuid.ToString() == "b5f90002-aa8d-11e3-9046-0002a5d5c51b")
                            {
                                _ReadApName = characteristic;
                            }

                            if (characteristic.Uuid.ToString() == "b5f90003-aa8d-11e3-9046-0002a5d5c51b")
                            {
                                _ReadApPass = characteristic;
                            }

                            if (characteristic.Uuid.ToString() == "b5f90072-aa8d-11e3-9046-0002a5d5c51b")
                            {
                                _SendCmds = characteristic;
                            }

                            if (characteristic.Uuid.ToString() == "b5f90073-aa8d-11e3-9046-0002a5d5c51b")
                            {
                                _NotifyCmds = characteristic;
                                GattCommunicationStatus status = await _NotifyCmds.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                if (status == GattCommunicationStatus.Success)
                                {
                                    _NotifyCmds.ValueChanged += NotifyCmds_ValueChanged;
                                }
                                else { UpateStatusBar("Failed to attach notify cmd " + status); }
                            }

                            if (characteristic.Uuid.ToString() == "b5f90074-aa8d-11e3-9046-0002a5d5c51b")
                            {
                                _SetSettings = characteristic;
                            }

                            if (characteristic.Uuid.ToString() == "b5f90075-aa8d-11e3-9046-0002a5d5c51b")
                            {
                                _NotifySettings = characteristic;
                                GattCommunicationStatus status = await _NotifySettings.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                if (status == GattCommunicationStatus.Success)
                                {
                                    _NotifySettings.ValueChanged += NotifySettings_ValueChanged;
                                }
                                else { UpateStatusBar("Failed to attach notify settings " + status); }
                            }

                            if (characteristic.Uuid.ToString() == "b5f90076-aa8d-11e3-9046-0002a5d5c51b")
                            {
                                _SendQueries = characteristic;
                            }

                            if (characteristic.Uuid.ToString() == "b5f90077-aa8d-11e3-9046-0002a5d5c51b")
                            {
                                _NotifyQueryResp = characteristic;
                                GattCommunicationStatus status = await _NotifyQueryResp.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                                if (status == GattCommunicationStatus.Success)
                                {
                                    _NotifyQueryResp.ValueChanged += NotifyQueryResp_ValueChanged;
                                    if (_SendQueries != null)
                                    {
                                        //Register for settings and status updates
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
            else if (result.Status == GattCommunicationStatus.Unreachable)
            {
                UpateStatusBar("Connection failed");
            }
        }

        private void BleDevice_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            UpateStatusBar(sender.ConnectionStatus == BluetoothConnectionStatus.Connected ? "Connected" : "Disconnected");
            IsBluetoothConnected = sender.ConnectionStatus == BluetoothConnectionStatus.Connected;
        }

        private async void BtnReadApName_Click(object sender, RoutedEventArgs e)
        {
            if (_ReadApName != null)
            {
                GattReadResult res = await _ReadApName.ReadValueAsync();
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

        private async void BtnReadApPass_Click(object sender, RoutedEventArgs e)
        {
            if (_ReadApPass != null)
            {
                GattReadResult res = await _ReadApPass.ReadValueAsync();
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

        private void BtnTurnWifiOn_Click(object sender, RoutedEventArgs e)
        {
            TogglefWifiAp(1);
        }

        private void BtnTurnWifiOff_Click(object sender, RoutedEventArgs e)
        {
            TogglefWifiAp(0);
        }

        private async void TogglefWifiAp(int onOff)
        {
            DataWriter writer = new DataWriter();
            writer.WriteBytes(new byte[] { 0x03, 0x17, 0x01, (byte)onOff });
            GattCommunicationStatus res = GattCommunicationStatus.Unreachable;

            if (onOff != 1 && onOff != 0)
            {
                res = GattCommunicationStatus.AccessDenied;
            }
            else if (_SendCmds != null)
            {
                res = await _SendCmds.WriteValueAsync(writer.DetachBuffer());
            }

            if (res != GattCommunicationStatus.Success)
            {
                UpateStatusBar("Failed to turn on wifi: " + res.ToString());
            }
        }

        private void BtnShutterOn_Click(object sender, RoutedEventArgs e)
        {
            ToggleShutter(1);
        }

        private void BtnShutterOff_Click(object sender, RoutedEventArgs e)
        {
            ToggleShutter(0);
        }

        private async void ToggleShutter(int onOff)
        {
            DataWriter writer = new DataWriter();
            writer.WriteBytes(new byte[] { 3, 1, 1, (byte)onOff });
            GattCommunicationStatus res = GattCommunicationStatus.Unreachable;

            if (onOff != 1 && onOff != 0)
            {
                res = GattCommunicationStatus.AccessDenied;
            }
            else if (_SendCmds != null)
            {
                res = await _SendCmds.WriteValueAsync(writer.DetachBuffer());
            }
            if (res != GattCommunicationStatus.Success)
            {
                UpateStatusBar("Failed to send shutter: " + res.ToString());
            }
        }

        // Bluetooth GATT Characteristic Notification Handlers
        // A GATT characteristic is a basic data element used to construct a GATT service
        private void NotifyQueryResp_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            DataReader reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] myBytes = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(myBytes);

            int newLength = ReadBytesIntoBuffer(myBytes, _BufQ);
            if (newLength > 0)
            {
                _ExpectedLengthQ = newLength;
            }

            if (_ExpectedLengthQ == _BufQ.Count)
            {
                if ((_BufQ[0] == 0x53 || _BufQ[0] == 0x93) && _BufQ[1] == 0)
                {
                    // Status messages
                    for (int k = 0; k < _BufQ.Count;)
                    {
                        if (_BufQ[k] == 10)
                        {
                            Encoding = _BufQ[k + 2] > 0;
                        }

                        if (_BufQ[k] == 70)
                        {
                            BatteryLevel = _BufQ[k + 2];
                        }

                        if(_BufQ[k] == 69)
                        {
                            WifiOn = _BufQ[k + 2] == 1;
                        }
                        k += 2 + _BufQ[k + 1];
                    }
                }
                else
                {
                    // Unhandled Query Message
                }
                _BufQ.Clear();
                _ExpectedLengthQ = 0;
            }
        }

        private void NotifySettings_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            var reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] myBytes = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(myBytes);
            int newLength = ReadBytesIntoBuffer(myBytes, _BufSet);
            if (newLength > 0)
                _ExpectedLengthSet = newLength;

            if (_ExpectedLengthSet == _BufSet.Count)
            {
                /*
                if (mBufSet[0] == 0xXX)
                {

                }
                */
                _BufSet.Clear();
            }
        }

        private void NotifyCmds_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            DataReader reader = DataReader.FromBuffer(args.CharacteristicValue);
            byte[] myBytes = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(myBytes);
            int newLength = ReadBytesIntoBuffer(myBytes, _BufCmd);
            if (newLength > 0)
                _ExpectedLengthCmd = newLength;

            if (_ExpectedLengthCmd == _BufCmd.Count)
            {
                /*
                if (mBufCmd[0] == 0xXX)
                {

                }
                */
                _BufCmd.Clear();
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

        private int ReadBytesIntoBuffer(byte[] bytes, List<byte> mBuf)
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
                mBuf.Add(bytes[k]);
            }
            return returnLength;
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

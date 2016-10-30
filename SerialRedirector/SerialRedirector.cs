using System;
using System.Threading;
using System.Windows.Forms;
using System.IO.Ports;
using System.Management;
using LibUsbDotNet.DeviceNotify;
using System.Text.RegularExpressions;

namespace SerialRedirector
{
    class SerialRedirector
    {
        public static IDeviceNotifier UsbDeviceNotifier = DeviceNotifier.OpenDeviceNotifier();

        private RobustSerial _sp1 = null;
        private RobustSerial _sp2 = null;
        private string _sp1Name;
        private string _sp2Name;
        private int _baudRate;
        private RobustSerial.Parity _parity;
        private int _dataBits;
        private RobustSerial.StopBits _stopBits;

        private const int BUF_SIZE = 8192;
        private const int NOTRAFFIC_INTERVAL = 0; // [ms]

        public SerialRedirector(string sp1Name, string sp2Name, int baudRate, RobustSerial.Parity parity,
            int dataBits, RobustSerial.StopBits stopBits)
        {
            _sp1Name = sp1Name;
            _sp2Name = sp2Name;
            _baudRate = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
        }

        public void DoRedirect()
        {
            // Hook the device notifier event
            UsbDeviceNotifier.OnDeviceNotify += _OnDeviceNotifyEvent;

            try
            {
                _sp1 = new RobustSerial(_sp1Name, _baudRate, _parity, _dataBits, _stopBits);
                _sp2 = new RobustSerial(_sp2Name, _baudRate, _parity, _dataBits, _stopBits);
                _sp1.Open();
                _sp2.Open();
                _sp1.DtrEnable = true;
                _sp1.RtsEnable = true;
                _sp2.DtrEnable = true;
                _sp2.RtsEnable = true;

                _SetMediaProps(_sp1, _sp2);
                _BridgePorts(_sp1, _sp2);

                _sp2.RtsEnable = false;
                _sp2.DtrEnable = false;
                _sp1.RtsEnable = false;
                _sp1.DtrEnable = false;
                _sp2.Close();
                _sp1.Close();
                _sp2.Dispose();
                _sp2.Dispose();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                UsbDeviceNotifier.OnDeviceNotify -= _OnDeviceNotifyEvent;
            }

        }

        private void _BridgePorts(RobustSerial sp1, RobustSerial sp2)
        {
            int avails;
            bool hasTraffic;
            byte[] buf = new byte[BUF_SIZE];

            for (;;)
            {
                hasTraffic = false;
                try
                {
                    avails = Math.Min(sp1.BytesToRead, BUF_SIZE);
                    if (avails > 0)
                    {
                        hasTraffic = true;
                        sp1.Read(buf, 0, avails);
                        sp2.Write(buf, 0, avails);
                    }
                    avails = Math.Min(sp2.BytesToRead, BUF_SIZE);
                    if (avails > 0)
                    {
                        hasTraffic = true;
                        sp2.Read(buf, 0, avails);
                        sp1.Write(buf, 0, avails);
                    }
                }
                catch (Exception)
                {
                    // ignoring exception. :-(
                }

                while (sp1.IsAttached() == false || sp2.IsAttached() == false)
                {
                    Application.DoEvents();
                    Thread.Sleep(NOTRAFFIC_INTERVAL);
                }

                Application.DoEvents();
                if (hasTraffic == false)
                {
                    Thread.Sleep(NOTRAFFIC_INTERVAL);
                }
            }
        }

        private void _SetMediaProps(RobustSerial sp1, RobustSerial sp2)
        {
            var spInstances = new ManagementClass("Win32_SerialPort").GetInstances();
            Console.WriteLine("---- Win32_SerialPort ----");
            foreach (ManagementObject port in spInstances)
            {
                Console.WriteLine("{0}: {1}", port["deviceid"], port["name"]);
                if (_sp1Name.CompareTo(port["deviceid"]) == 0)
                {
                    sp1.SetMedia(RobustSerial.Media.Native, 0, 0, "");
                }
                if (_sp2Name.CompareTo(port["deviceid"]) == 0)
                {
                    sp2.SetMedia(RobustSerial.Media.Native, 0, 0, "");
                }
            }
#if false
            deviceId = @"USB\VID_067B&PID_2303\5&3A4FEDB0&0&2"
            name = @"Prolific USB-to-Serial Comm Port (COM5)"
#endif
            var pnpInstances = new ManagementClass("Win32_PnPEntity").GetInstances();
            var regexVP = new Regex(@"^VID_....&PID_....");
            var regexN = new Regex(@"\(COM[0-9]*\)$");
            Console.WriteLine("---- Win32_PnPEntity ----");
            foreach (ManagementObject port in pnpInstances)
            {
                if (port["deviceid"] == null || port["name"] == null)
                {
                    continue;
                }
                var deviceId = port["deviceid"].ToString();
                var name = port["name"].ToString();
                if (deviceId.StartsWith(@"USB\") == true && name.Contains("(COM") == true)
                {
                    Match matchN = regexN.Match(name);
                    if (matchN.Success == false)
                    {
                        continue;
                    }
                    int nameLen = matchN.Value.Length;
                    string portName = matchN.Value.Substring(1, nameLen - 2);

                    // Console.WriteLine("{0}: {1}: {2}", deviceId, name, portName);
                    string [] part = deviceId.Split('\\');
                    Match matchVP = regexVP.Match(part[1]);
                    string serialId = part[2];
                    if (matchVP.Success)
                    {
                        var vpVal = matchVP.Value;
                        UInt16 vendorId = Convert.ToUInt16(vpVal.Substring(4, 4), 16);
                        UInt16 productId = Convert.ToUInt16(vpVal.Substring(13, 4), 16);

                        Console.WriteLine("{0}: USB Serial VendorID={1}, ProductID={2}, SerialID={3}", 
                            portName, vendorId, productId, serialId);

                        if (_sp1Name.CompareTo(portName) == 0)
                        {
                            sp1.SetMedia(RobustSerial.Media.Usb, vendorId, productId, serialId.ToLowerInvariant());
                        }
                        if (_sp2Name.CompareTo(portName) == 0)
                        {
                            sp2.SetMedia(RobustSerial.Media.Usb, vendorId, productId, serialId.ToLowerInvariant());
                        }
                    }
                }
            }
        }

        private void _OnDeviceNotifyEvent(object sender, DeviceNotifyEventArgs e)
        {
            // A Device system-level event has occured
            if (e.DeviceType == DeviceType.Port)
            {
                string portName = e.Port.Name;
                RobustSerial sp = null;
                if (portName == _sp1Name) sp = _sp1;
                else if (portName == _sp2Name) sp = _sp2;
                
                if (sp != null)
                {
                    if (e.EventType == EventType.DeviceArrival)
                    {
                        sp.FoundConnect();
                    }
                    else // if (e.EventType == EventType.DeviceRemoveComplete)
                    {
                        sp.FoundDisconnect();
                    }
                }
            }
            else if (e.DeviceType == DeviceType.DeviceInterface)
            {
                RobustSerial sp = null;
                
                UsbProps sp1UsbProps = _sp1.GetUsbProps();
                UsbProps sp2UsbProps = _sp2.GetUsbProps();
                if (_sp1.IsMediaUsb() == true &&
                    sp1UsbProps.vendorId == e.Device.IdVendor &&
                    sp1UsbProps.productId == e.Device.IdProduct &&
                    sp1UsbProps.serialId.CompareTo(e.Device.SerialNumber) == 0)
                {
                    sp = _sp1;
                }
                else if (_sp2.IsMediaUsb() == true &&
                    sp2UsbProps.vendorId == e.Device.IdVendor &&
                    sp2UsbProps.productId == e.Device.IdProduct &&
                    sp2UsbProps.serialId.CompareTo(e.Device.SerialNumber) == 0)
                {
                    sp = _sp2;
                }

                if (sp != null)
                {
                    if (e.EventType == EventType.DeviceArrival)
                    {
                        sp.FoundConnect();
                    }
                    else // if (e.EventType == EventType.DeviceRemoveComplete)
                    {
                        sp.FoundDisconnect();
                    }
                }
            }
        }
    }
}

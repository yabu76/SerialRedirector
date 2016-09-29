using System;
using System.Threading;
using System.Windows.Forms;
using LibUsbDotNet.DeviceNotify;
using static SerialRedirector.RobustSerial;

namespace SerialRedirector
{
    class SerialRedirector
    {
        public static IDeviceNotifier UsbDeviceNotifier = DeviceNotifier.OpenDeviceNotifier();
        private volatile bool _sp1Attached;
        private volatile bool _sp2Attached;

        private RobustSerial _sp1 = null;
        private RobustSerial _sp2 = null;
        private string _sp1Name;
        private string _sp2Name;
        private int _baudRate;
        private Parity _parity;
        private int _dataBits;
        private StopBits _stopBits;

        private const int BUF_SIZE = 8192;
        private const int NOTRAFFIC_INTERVAL = 0; // [ms]

        public SerialRedirector(string sp1Name, string sp2Name, int baudRate, Parity parity, int dataBits, StopBits stopBits)
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

                _sp1Attached = true;
                _sp2Attached = true;
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
                    while (_sp1Attached == false || _sp2Attached == false)
                    {
                        Application.DoEvents();
                        Thread.Sleep(NOTRAFFIC_INTERVAL);
                    }
                }

                Application.DoEvents();
                if (hasTraffic == false)
                {
                    Thread.Sleep(NOTRAFFIC_INTERVAL);
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
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;
using System.IO.Ports;
using LibUsbDotNet.DeviceNotify;

namespace SerialRedirector
{
    class RobustSerial
    {
        public enum Parity { None = 0, Odd, Even, Mark, Space, };
        public enum StopBits { None = 0, One, OnePointFive, Two, };

        private const int RETRY_NUM = 5;
        private const int REOPEN_INTERVAL = 3000; // [ms]
        private const int REREAD_INTERVAL = 3000; // [ms]
        private const int REWRITE_INTERVAL = 3000; // [ms]
        private SerialPort sp_;
        private string portName_;
        private int baudRate_;
        private Parity parity_;
        private int dataBits_;
        private StopBits stopBits_;
        private bool isAttached_ = false;
        private const int EVCD_ATTACH = 0;
        private const int EVCD_DETACH = 1;
        private const string EVNAME_ATTACH = "RobustSerial_Attach";
        private const string EVNAME_DETACH = "RobustSerial_Detach";
        private const string usbUniqueId_ = "1fff-ffff-ffff-8000";

        private EventWaitHandle[] attachEventHandles_ = new EventWaitHandle[2];

        private static readonly Dictionary<Parity, System.IO.Ports.Parity> dictParity_ = 
            new Dictionary<Parity, System.IO.Ports.Parity>
        {
            { Parity.None, System.IO.Ports.Parity.None },
            { Parity.Odd, System.IO.Ports.Parity.Odd },
            { Parity.Even, System.IO.Ports.Parity.Even },
            { Parity.Mark, System.IO.Ports.Parity.Mark },
            { Parity.Space, System.IO.Ports.Parity.Space },
        };

        private static readonly Dictionary<StopBits, System.IO.Ports.StopBits> dictStopBits_ =
            new Dictionary<StopBits, System.IO.Ports.StopBits>
        {
            { StopBits.None, System.IO.Ports.StopBits.None },
            { StopBits.One, System.IO.Ports.StopBits.One },
            { StopBits.OnePointFive, System.IO.Ports.StopBits.OnePointFive },
            { StopBits.Two, System.IO.Ports.StopBits.Two },
        };

        static IDeviceNotifier devNotif = DeviceNotifier.OpenDeviceNotifier();

        public int BytesToRead
        {
            get
            {
                int ret = 0;
                try
                {
                    ret = sp_.BytesToRead;
                }
                catch (Exception ex)
                {
                    bool connected = WaitForReconnect_();
                    if (!connected) throw ex;
                    Reopen_();
                    ret = 0;
                }
                return ret;

            }
        }

        public bool DtrEnable
        {
            get
            {
                bool ret;
                try
                {
                    ret = sp_.DtrEnable;
                }
                catch (Exception ex)
                {
                    bool connected = WaitForReconnect_();
                    if (!connected) throw ex;
                    Reopen_();
                    ret = false;
                }
                return ret;
            }
            set
            {
                try
                {
                    sp_.DtrEnable = value;
                }
                catch (Exception ex)
                {
                    bool connected = WaitForReconnect_();
                    if (!connected) throw ex;
                    Reopen_();
                    sp_.DtrEnable = value;
                }
            }
        }

        public bool RtsEnable
        {
            get
            {
                bool ret;
                try
                {
                    ret = sp_.RtsEnable;
                }
                catch (Exception ex)
                {
                    bool connected = WaitForReconnect_();
                    if (!connected) throw ex;
                    Reopen_();
                    ret = false;
                }
                return ret;
            }
            set
            {
                sp_.RtsEnable = value;
            }
        }

        public RobustSerial(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            portName_ = portName;
            baudRate_ = baudRate;
            parity_ = parity;
            dataBits_ = dataBits;
            stopBits_ = stopBits;
            sp_ = new SerialPort(portName, baudRate, toIPParity(parity), dataBits, toIPStopBits(stopBits));
            StartWatchingConnection_();
        }

        public void Dispose()
        {
            StopWatchingConnection_();
            sp_.Dispose();
        }

        public void Open()
        {
            try
            {
                sp_.Open();
            }
            catch (Exception ex)
            {
                bool connected = WaitForReconnect_();
                if (!connected) throw ex;
                Reopen_();
            }
        }

        public void Close()
        {
            sp_.Close();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int ret = 0;
            try
            {
                ret = sp_.Read(buffer, offset, count);
            }
            catch (Exception ex)
            {
                bool connected = WaitForReconnect_();
                if (!connected) throw ex;
                Reopen_();
                ret = 0;
            }
            return ret;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            try
            {
                sp_.Write(buffer, offset, count);
            }
            catch (Exception ex)
            {
                bool connected = WaitForReconnect_();
                if (!connected) throw ex;
                Reopen_();
            }
        }

        private System.IO.Ports.Parity toIPParity(Parity parity)
        {
            return dictParity_[parity];
        }

        private System.IO.Ports.StopBits toIPStopBits(StopBits stopBits)
        {
            return dictStopBits_[stopBits];
        }

        private void Reopen_()
        {
            for (int cnt = 0; cnt < (RETRY_NUM - 1); cnt++)
            {
                Thread.Sleep(REOPEN_INTERVAL);
                sp_.Close();
                sp_.Open();
            }
            Thread.Sleep(REOPEN_INTERVAL);
            sp_.Close();
            sp_.Open();
        }

        private void StartWatchingConnection_()
        {
            attachEventHandles_[EVCD_ATTACH] = new EventWaitHandle(false, EventResetMode.AutoReset,
                EVNAME_ATTACH);
            attachEventHandles_[EVCD_DETACH] = new EventWaitHandle(false, EventResetMode.AutoReset,
                EVNAME_DETACH);
            devNotif.OnDeviceNotify += 
                new EventHandler<DeviceNotifyEventArgs>(USBDev_OnDeviceNotify_);
        }

        private void StopWatchingConnection_()
        {
            devNotif.OnDeviceNotify -=
                new EventHandler<DeviceNotifyEventArgs>(USBDev_OnDeviceNotify_);
            attachEventHandles_[EVCD_ATTACH].Close();
            attachEventHandles_[EVCD_DETACH].Close();
        }

        private void USBDev_OnDeviceNotify_(object sender, DeviceNotifyEventArgs eargs)
        {
            if (eargs.Device.ClassGuid.ToString() == usbUniqueId_)
            {
                if (eargs.EventType == EventType.DeviceArrival)
                {
                    using (EventWaitHandle ewh = EventWaitHandle.OpenExisting(EVNAME_ATTACH))
                    {
                        isAttached_ = true;
                        ewh.Set();
                    }

                    // EventSend(Attached);
                }
                else if (eargs.EventType == EventType.DeviceRemoveComplete)
                {
                    using (EventWaitHandle ewh = EventWaitHandle.OpenExisting(EVNAME_DETACH))
                    {
                        isAttached_ = false;
                        ewh.Set();
                    }
                }
            }
        }

        private bool WaitForReconnect_()
        {
            if (isAttached_)
            {
                return true;
            }
            for (;;)
            {
                int idx;
                idx = EventWaitHandle.WaitAny(attachEventHandles_, 5000, false);
                if (idx == EVCD_ATTACH)
                {
                    return true;
                }
                /* else if (idx == EVCD_DETACH ||
                 * idx == EventWaitHandle.WaitTimeout) continue;
                 */
            }
            // return isAttached_;
        }
    }
}

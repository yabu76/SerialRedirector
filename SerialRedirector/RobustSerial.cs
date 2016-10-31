using System;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;

namespace SerialRedirector
{
    internal struct UsbProps
	{
        public UInt16 vendorId;
        public UInt16 productId;
        public string serialId;
	};

    internal class RobustSerial
    {
        public enum Parity { None = 0, Odd, Even, Mark, Space, };
        public enum StopBits { None = 0, One, OnePointFive, Two, };
        public enum Media { Native = 0, Usb };

        private Stream theBaseStream;

        private SerialPort _sp;
        private string _portName;
        private int _baudRate;
        private Parity _parity;
        private int _dataBits;
        private StopBits _stopBits;
        private Media _media;
        private UsbProps _usbProps;
        private bool _isAttached = false;

        private static readonly Dictionary<Parity, System.IO.Ports.Parity> _dictParity =
            new Dictionary<Parity, System.IO.Ports.Parity>
        {
            { Parity.None, System.IO.Ports.Parity.None },
            { Parity.Odd, System.IO.Ports.Parity.Odd },
            { Parity.Even, System.IO.Ports.Parity.Even },
            { Parity.Mark, System.IO.Ports.Parity.Mark },
            { Parity.Space, System.IO.Ports.Parity.Space },
        };

        private static readonly Dictionary<StopBits, System.IO.Ports.StopBits> _dictStopBits =
            new Dictionary<StopBits, System.IO.Ports.StopBits>
        {
            { StopBits.None, System.IO.Ports.StopBits.None },
            { StopBits.One, System.IO.Ports.StopBits.One },
            { StopBits.OnePointFive, System.IO.Ports.StopBits.OnePointFive },
            { StopBits.Two, System.IO.Ports.StopBits.Two },
        };

        public int BytesToRead
        {
            get
            {
                return _sp.BytesToRead;
            }
        }

        public bool DtrEnable
        {
            get
            {
                return _sp.DtrEnable;
            }
            set
            {
                _sp.DtrEnable = value;
            }
        }

        public bool RtsEnable
        {
            get
            {
                return _sp.RtsEnable;
            }
            set
            {
                _sp.RtsEnable = value;
            }
        }

        public RobustSerial(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            _portName = portName;
            _baudRate = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
            _sp = new SerialPort(portName, baudRate, toIPParity(parity), dataBits, toIPStopBits(stopBits));
        }

        public void SetMedia(Media media, UInt16 vendorId, UInt16 productId, string serialId)
        {
            _media = media;
            _usbProps.vendorId = vendorId;
            _usbProps.productId = productId;
            _usbProps.serialId = serialId;
        }

        public bool IsMediaUsb()
        {
            return _media == Media.Usb;
        }

        public UsbProps GetUsbProps()
        {
            return _usbProps;
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Dispose(bool disposing)
        {
            if (disposing && (_sp.Container != null))
            {
                _sp.Container.Dispose();
            }
            try
            {
                if (theBaseStream.CanRead)
                {
                    theBaseStream.Close();
                    GC.ReRegisterForFinalize(theBaseStream);
                }
            }
            catch (Exception)
            {
                // ignore exception - bug with USB - serial adapters.
            }
            _sp.Dispose();
        }

        private bool _Open()
        {
            bool success = true;
            try
            {
                _sp.Open();
            }
            catch (Exception)
            {
                success = false;
            }
            return success;

        }

        public bool Open()
        {
            if (_isAttached == true) return true;

            bool result = false;
            for (int i = 0; i < 3; i++)
            {
                result = _Open();
                if (result == true) break;
                System.Threading.Thread.Sleep(300);
            }
            if (result == false) return false;

            try
            {
                theBaseStream = _sp.BaseStream;
                GC.SuppressFinalize(_sp.BaseStream);
            }
            catch
            {
                return false;
            }
            _isAttached = true;

            return true;
        }

        public void Close()
        {
            if (_isAttached == false) return;

            _sp.Close();
            _isAttached = false;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            return _sp.Read(buffer, offset, count);
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            _sp.Write(buffer, offset, count);
        }

        public void FoundDisconnect()
        {
            Close();
        }

        public void FoundConnect()
        {
            Open();
        }

        public bool IsAttached()
        {
            return _isAttached;
        }

        private System.IO.Ports.Parity toIPParity(Parity parity)
        {
            return _dictParity[parity];
        }

        private System.IO.Ports.StopBits toIPStopBits(StopBits stopBits)
        {
            return _dictStopBits[stopBits];
        }
    }
}

using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

using SmartLab.MuRata.Config;
using SmartLab.MuRata.ErrorCode;
using SmartLab.MuRata.Indication;
using SmartLab.MuRata.Response;
using SmartLab.MuRata.Type;

namespace SmartLab.MuRata
{
    public delegate void MuRataPowerUpIndication(PowerUpIndication indication);
    public delegate void MuRataScanResultIndication(SSIDRecordIndication indication);
    public delegate void MuRataTCPConnectionIndication(TCPStatusIndication indication);
    public delegate void MuRataSocketReceiveIndication(SocketReceiveInidcation indication);
    public delegate void MuRataUDPReceiveIndication(UDPReceivedIndication indication);
    public delegate void MuRataWIFIConnectionIndication(WIFIConnectionIndication indication);
    public delegate void MuRataHTTPResponseIndication(HTTPResponseIndication indication);

    public class MuRata
    {
        /// <summary>
        /// This event reports the Murata module power up reason. Murata module is ready for serial communication after this report is generated.
        /// </summary>
        public event MuRataPowerUpIndication onPowerUpIndication;

        /// <summary>
        /// Scan result is sent from module to host application using multiple WIFI_SCAN_RESULT_IND indications.
        /// </summary>
        public event MuRataScanResultIndication onScanResultIndication;

        /// <summary>
        /// Indication is originated from the module and sent to the host application.
        /// This event describes the status of a network connection (identified by a socket) using one of the codes defined in Table 18.
        /// </summary>
        public event MuRataTCPConnectionIndication onTcpConnectionStatusIndication;

        /// <summary>
        /// This event is generated when a TCP server or a UDP server (in connected mode) receives a packet.
        /// Since there is no client address and port information, the application may need to call SNIC_SOCKET_GETNAME_REQ to get them.
        /// </summary>
        public event MuRataSocketReceiveIndication onSocketReceiveIndication;

        /// <summary>
        /// This event is generated when a UDP server (in unconnected mode) receives a packet.
        /// </summary>
        public event MuRataUDPReceiveIndication onUDPReceiveIndication;

        /// <summary>
        /// Indication is originated from the module and sent to the host application.
        /// This event describes the status of WIFI network connection for an interface.
        /// Currently only the STA status is reported. Soft AP is always UP.
        /// </summary>
        public event MuRataWIFIConnectionIndication onWIFIConnectionIndication;

        /// <summary>
        /// This indication is used for HTTP chunked response
        /// The most significant bit of Content length is reserved to indicate if there is more data to send to the host.
        /// When this bit is 1, the host application should continue to receive SNIC_HTTP_RSP_IND, until this bit is 0.
        /// The Content length is limited by the receive buffer size specified in SNIC_INIT_REQ and the system resource at that moment.
        /// </summary>
        public event MuRataHTTPResponseIndication onHTTPResponseIndication;

        public const int DEFAULT_BAUDRATE = 921600;
        private const int DEFAULT_WAIT = 10000;

        private EventWaitHandle waitEvent = new AutoResetEvent(false);
        private bool isResponseSignal = false;
        //private bool isIndicationSignal = false;
        //private bool handleIndicationMaunally = false;

        private SerialPort serialPort;
        private byte frameID = 0x00;

        private Payload _sendPayload, _receivePayload;
        private UARTFrame _sendFrame, _receiveFrame;

        public MuRata(String portName)
            : this(portName, DEFAULT_BAUDRATE, Parity.None, 8, StopBits.One)
        { }

        public MuRata(String portName, int baudRate, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            _sendPayload = new Payload();
            _sendFrame = new UARTFrame();
            _receivePayload = new Payload();
            _receiveFrame = new UARTFrame();
            _receiveFrame.SetPayload(_receivePayload);
            _sendPayload = new Payload();
            serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits);
        }

        /// <summary>
        /// !!! this must be called first !!!
        /// in order to open the serial port and start process response
        /// </summary>
        public void Start()
        {
            try
            {
                serialPort.Open();
                new Thread(DataReceiveThread).Start();
            }
            catch
            {
                throw new IOException("unable to open serial port");
            }
        }

        /// <summary>
        /// stop and close the serial port
        /// </summary>
        public void Stop()
        {
            serialPort.Close();
        }

        /*
        public bool HandelIndicationManually 
        {
            set { this.handleIndicationMaunally = value; }
            get { return this.handleIndicationMaunally; }
        }
        */

        #region Send and Receive

        private void Send(bool signal = true)
        {
            _sendPayload.SetResponseFlag(ResponseFlag.Request_or_Indication);
            _sendFrame.SetACKRequired(false);

            isResponseSignal = signal;

            serialPort.BaseStream.WriteByte(UARTFrame.SOM);

            serialPort.BaseStream.WriteByte((byte)(_sendFrame.GetL0() | 0x80));
            serialPort.BaseStream.WriteByte((byte)(_sendFrame.GetL1() | 0x80 | (_sendFrame.GetACKRequired() ? 0x40 : 0x00)));

            serialPort.BaseStream.WriteByte((byte)((byte)_sendFrame.GetCommandID() | 0x80));

            serialPort.Write(_sendPayload.GetData(), 0, _sendPayload.GetPosition());

            serialPort.BaseStream.WriteByte((byte)(_sendFrame.GetChecksum() | 0x80));

            serialPort.BaseStream.WriteByte(UARTFrame.EOM);
        }

        /// <summary>
        /// return null means there is an error
        /// </summary>
        /// <returns>received UARTFrame</returns>
        private bool FrameReceive()
        {
            int value = serialPort.ReadByte();

            while (value != UARTFrame.SOM)
                value = serialPort.ReadByte();

            _receiveFrame.SetL0(serialPort.ReadByte());
            _receiveFrame.SetL1(serialPort.ReadByte());

            _receiveFrame.SetCommandID((byte)serialPort.ReadByte());

            int _size = _receiveFrame.GetPayloadLength();

            _receivePayload.Allocate(_size);
            while (_receivePayload.GetPosition() < _size)
                _receivePayload.SetPosition(_receivePayload.GetPosition() + serialPort.Read(_receivePayload.GetData(), _receivePayload.GetPosition(), _size - _receivePayload.GetPosition()));

            _receiveFrame.SetChecksum(serialPort.ReadByte());

            if (serialPort.ReadByte() == UARTFrame.EOM && _receiveFrame.VerifyChecksum())
                return true;
            else return false;
        }

        private void DataReceiveThread()
        {
            while (true)
            {
                try
                {
                    if (FrameReceive() == false)
                        continue;

                    if (this.isResponseSignal && _receiveFrame.GetCommandID() == _sendFrame.GetCommandID() && _receivePayload.GetSubCommandID() == _sendPayload.GetSubCommandID() && _receivePayload.GetFrameID() == _sendPayload.GetFrameID())
                    {
                        this.isResponseSignal = false;
                        _sendPayload.Rewind();
                        _sendPayload.SetContent(_receivePayload.GetData(), 0, _receivePayload.GetPosition());
                        waitEvent.Set();
                    }
                    else if (_receivePayload.GetResponseFlag() == ResponseFlag.Request_or_Indication)
                    {
                        /*
                        if (handleIndicationMaunally &&  _receiveFrame.GetCommandID() == _sendFrame.GetCommandID() && _receivePayload.GetSubCommandID() == _sendPayload.GetSubCommandID())
                        {
                            this.isIndicationSignal = false;
                            _sendPayload.Rewind();
                            _sendPayload.SetContent(_receivePayload.GetData(), 0, _receivePayload.GetPosition());
                            waitEvent.Set();
                        }
                        else if (!handleIndicationMaunally)
                        {
                        */
                        switch (_receiveFrame.GetCommandID())
                        {
                            case CommandID.CMD_ID_GEN:
                                this.GENIndication();
                                break;
                            case CommandID.CMD_ID_WIFI:
                                this.WIFIIndication();
                                break;
                            case CommandID.CMD_ID_SNIC:
                                this.SNICIndication();
                                break;
                        }
                    }
                }
                catch { break; }
            }
        }

        private void GENIndication()
        {
            switch (_receivePayload.GetSubCommandID())
            {
                case SubCommandID.GEN_PWR_UP_IND:
                    if (onPowerUpIndication != null)
                        onPowerUpIndication(new PowerUpIndication(_receivePayload));
                    break;
            }
        }

        private void WIFIIndication()
        {
            switch (_receivePayload.GetSubCommandID())
            {
                case SubCommandID.WIFI_SCAN_RESULT_IND:
                    if (onScanResultIndication != null)
                        onScanResultIndication(new SSIDRecordIndication(_receivePayload));
                    break;

                case SubCommandID.WIFI_NETWORK_STATUS_IND:
                    if (onWIFIConnectionIndication != null)
                        onWIFIConnectionIndication(new WIFIConnectionIndication(_receivePayload));
                    break;
            }
        }

        private void SNICIndication()
        {
            switch (_receivePayload.GetSubCommandID())
            {
                case SubCommandID.SNIC_TCP_CONNECTION_STATUS_IND:
                    if (onTcpConnectionStatusIndication != null)
                        onTcpConnectionStatusIndication(new TCPStatusIndication(_receivePayload));
                    break;
                case SubCommandID.SNIC_CONNECTION_RECV_IND:
                    if (onSocketReceiveIndication != null)
                        onSocketReceiveIndication(new SocketReceiveInidcation(_receivePayload));
                    break;
                case SubCommandID.SNIC_UDP_RECV_IND:
                    if (onUDPReceiveIndication != null)
                        onUDPReceiveIndication(new UDPReceivedIndication(_receivePayload));
                    break;
                case SubCommandID.SNIC_HTTP_RSP_IND:
                    if (onHTTPResponseIndication != null)
                        onHTTPResponseIndication(new HTTPResponseIndication(_receivePayload));
                    break;
            }
        }

        #endregion

        #region General Management

        /// <summary>
        /// SNIC firmware has a built in version string. Use this command to retrieve the version info.
        /// </summary>
        /// <returns>return null when timeout</returns>
        public VersionInfoResponse GEN_GetFirmwareVersionInfo()
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.GEN_FW_VER_GET_REQ);
            _sendPayload.SetFrameID(frameID++);

            _sendFrame.SetCommandID(CommandID.CMD_ID_GEN);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return null;
            }

            return new VersionInfoResponse(_sendPayload);
        }

        /// <summary>
        /// This command restores the data stored in NVM to factory default values. Any web page update is not affected by this command.
        /// A soft reset will be performed automatically after the NVM has been restored.
        /// Application needs to send WIFI_GET_STATUS_REQ or SNIC_GET_DHCP_INFO_REQ commands to determine the new state of the Murata module.
        /// </summary>
        /// <returns></returns>
        public CMDCode GEN_RestoreNVMtoFactoryDefault()
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.GEN_RESTORE_REQ);
            _sendPayload.SetFrameID(frameID++);

            _sendFrame.SetCommandID(CommandID.CMD_ID_GEN);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return CMDCode.GEN_NORESPONSE;
            }

            return (CMDCode)_sendPayload.GetData()[2];
        }

        /// <summary>
        /// This command resets the module. Application needs to send WIFI_GET_STATUS_REQ or SNIC_GET_DHCP_INFO_REQ commands to determine the new state of the module after the reset.
        /// </summary>
        /// <returns></returns>
        public CMDCode GEN_SoftReset()
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.GEN_RESET_REQ);
            _sendPayload.SetFrameID(frameID++);

            _sendFrame.SetCommandID(CommandID.CMD_ID_GEN);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return CMDCode.GEN_NORESPONSE;
            }

            return (CMDCode)_sendPayload.GetData()[2];
        }

        /// <summary>
        /// This command configures the UART interface. The specified parameters are saved into the NVM and they are used for the specified UART interface in subsequent powerups.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public CMDCode GEN_UARTConfiguration(UARTConfig config)
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.GEN_UART_CFG_REQ);
            _sendPayload.SetFrameID(frameID++);
            _sendPayload.SetContent(config.GetValue());

            _sendFrame.SetCommandID(CommandID.CMD_ID_GEN);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return CMDCode.GEN_NORESPONSE;
            }

            return (CMDCode)_sendPayload.GetData()[2];
        }

        #endregion

        #region Command ID for WIFI

        /// <summary>
        /// This command turns on Wifi on module.
        /// The default country code is “US”, which is one of the startup parameters in NVM. If the WIFI_ON_REQ has no intention of changing the country code, put 0x0000 in the two-byte Country code, so that the firmware will use the country code configured in NVM.
        /// The module supports both soft AP mode and STA mode at the same time. The module has reserved flash space (NVM) to store startup parameters for both the soft AP and the STA. Only STA’s parameters can be dynamically changed at run time.
        /// Turning on WiFi would cause the following to happen:
        /// The following operations occur using the parameters specified in the NVM if the AP mode is enabled.
        /// 1. Turn on the soft AP
        /// 2. Starts DNS server, DHCP server and HTTP server. The HTTP server provides a means for configuring the WLAN access parameters for the STA.
        /// Turn on the STA. If the NVM has valid startup parameters, the STA will try to join the saved SSID with saved authentication information. The NVM also stores whether DHCP or static IP is used for STA. If DHCP is used, DHCP client will be started. After a successful join, STA’s IP will be configured according to the NVM.
        /// By default, the soft AP is turned on to allow user to use a WiFi enabled computer to connect to the soft AP, and instructs the STA to join one of the surrounding APs. So WiFi is turned on by default and this command is not required at startup.
        /// </summary>
        /// <returns></returns>
        public WIFICode WIFI_TurnOn()
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.WIFI_ON_REQ);
            _sendPayload.SetFrameID(frameID++);
            /*
             * Country code is a 2-character ASCII string. E.g., “US” = the United States. For the complete list, see Appendix A. The default country code is “US”, which is one of the startup parameters in NVM. If the WIFI_ON_REQ has no intention of changing the country code, put 0x0000 in the two-byte Country code, so that the firmware will use the country code configured in NVM.
             */
            _sendPayload.SetContent(0x00);
            _sendPayload.SetContent(0x00);

            _sendFrame.SetCommandID(CommandID.CMD_ID_WIFI);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return WIFICode.WIFI_NORESPONSE;
            }

            return (WIFICode)_sendPayload.GetData()[2];

        }

        /// <summary>
        /// This command turns off Wifi on module. Turning off WiFi causes the following to happen:
        /// 1. Turn off the soft AP, including shutting down DNS server, DHCP server and HTTP server.
        /// 2. Disconnect STA from any joined network, and close all sockets opened by application.
        /// </summary>
        /// <returns></returns>
        public WIFICode WIFI_TurnOff()
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.WIFI_OFF_REQ);
            _sendPayload.SetFrameID(frameID++);

            _sendFrame.SetCommandID(CommandID.CMD_ID_WIFI);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return WIFICode.WIFI_NORESPONSE;
            }

            return (WIFICode)_sendPayload.GetData()[2];

        }

        /// <summary>
        /// This command turns on or off the soft AP. The WIFI_ON(OFF)_REQ controls both the soft AP and STA at the same time, while this command only controls the soft AP.
        /// An example use case is, the soft AP (and its web server) is turned on at startup to configure STA to join a network and is no longer needed after the STA is connected.
        /// WIFI_AP_CTRL_REQ can be used to turn the soft AP off.
        /// OnOff = 0 indicates AP is to be turned off. The rest of the parameters are ignored.
        /// OnOff = 1 indicates turning on soft AP using existing NVM parameters,
        /// OnOff = 2 indicates turning on AP with the parameters provided. If the soft AP is already on, it is first turned off.
        /// Persistency=1 indicates the soft AP’s on/off state and parameters (if OnOff = 2) will be saved in NVM. For example, if OnOff =0 and Persistency=1, the soft AP will not be turned on after a reset.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public WIFICode WIFI_SoftAPControl(SoftAPConfig config)
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.WIFI_AP_CTRL_REQ);
            _sendPayload.SetFrameID(frameID++);

            _sendPayload.SetContent(config.GetOnOffStatus());
            _sendPayload.SetContent(config.GetPersistency());
            if (config.GetOnOffStatus() == 0x02)
            {
                _sendPayload.SetContent(config.GetSSID());
                _sendPayload.SetContent(0x00);
            }
            _sendPayload.SetContent(config.GetChannel());
            _sendPayload.SetContent((byte)config.GetSecurityMode());

            _sendPayload.SetContent(config.GetSecurityLength());
            if (config.GetSecurityMode() != SecurityMode.WIFI_SECURITY_OPEN && config.GetSecurityLength() > 0)
                _sendPayload.SetContent(config.GetSecurityKey());

            _sendFrame.SetCommandID(CommandID.CMD_ID_WIFI);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return WIFICode.WIFI_NORESPONSE;
            }

            return (WIFICode)_sendPayload.GetData()[2];
        }

        /// <summary>
        /// This command instructs module to associate to a network.
        /// </summary>
        /// <param name="AP"></param>
        /// <returns></returns>
        public WIFICode WIFI_AssociateNetwork(WIFINetwork AP)
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.WIFI_JOIN_REQ);
            _sendPayload.SetFrameID(frameID++);
            _sendPayload.SetContent(AP.GetSSID());
            _sendPayload.SetContent(0x00);

            _sendPayload.SetContent((byte)AP.GetSecurityMode());
            _sendPayload.SetContent(AP.GetSecurityLength());
            if (AP.GetSecurityLength() > 0)
                _sendPayload.SetContent(AP.GetSecurityKey());

            if (AP.GetBSSID() != null)
            {
                _sendPayload.SetContent(AP.GetChannel());
                _sendPayload.SetContent(AP.GetBSSID());
            }

            _sendFrame.SetCommandID(CommandID.CMD_ID_WIFI);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return WIFICode.WIFI_NORESPONSE;
            }

            return (WIFICode)_sendPayload.GetData()[2];
        }

        /// <summary>
        /// This command instructs the module to disconnect from a network.
        /// Upon a successful reception of the command, the module disconnects from associated network. Sockets opened by application are not closed.
        /// </summary>
        /// <returns></returns>
        public WIFICode WIFI_DisconnectNetwork()
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.WIFI_DISCONNECT_REQ);
            _sendPayload.SetFrameID(frameID++);

            _sendFrame.SetCommandID(CommandID.CMD_ID_WIFI);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return WIFICode.WIFI_NORESPONSE;
            }

            return (WIFICode)_sendPayload.GetData()[2];
        }

        /// <summary>
        /// This command queries the WiFi status from module. This command should be called by application after startup to determine the WiFi status since the module may have joined an AP automatically based on NVM parameters (see 6.1).
        /// </summary>
        /// <param name="WiFiInterface"></param>
        /// <returns></returns>
        public WIFIStatusResponse WIFI_GetStatus(WIFIInterface WiFiInterface)
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.WIFI_GET_STATUS_REQ);
            _sendPayload.SetFrameID(frameID++);
            _sendPayload.SetContent((byte)WiFiInterface);

            _sendFrame.SetCommandID(CommandID.CMD_ID_WIFI);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return null;
            }

            return new WIFIStatusResponse(_sendPayload);
        }

        /// <summary>
        /// This command requests the reporting of the current RSSI from module’s STA interface
        /// </summary>
        /// <returns>RSSI in dBm. 127 means unspecified value</returns>
        public int WIFI_GetRSSI()
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.WIFI_GET_STA_RSSI_REQ);
            _sendPayload.SetFrameID(frameID++);

            _sendFrame.SetCommandID(CommandID.CMD_ID_WIFI);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return 127;
            }

            byte value = _sendPayload.GetData()[2];

            if (value >> 7 == 0x01)
                return (~(_sendPayload.GetData()[2] - 1) & 0x7F) * -1;

            return value;
        }

        /// <summary>
        /// This command requests the module to use WPS to join the network. Two methods are supported: push button and pin-based configuration.
        /// If Mode is 1, Pin value must be present. Pin value is NUL terminated ASCII string. Pin string length of 0, 4, 7, or 8 is valid. When length is 0, the module will use the WPS default pin configured in the NVM by using the SNIC monitor. When length is 8, the 8th digit must be the correct checksum of the first 7 digits. The pin checksum calculation method can be found from the Internet. When the length is 7, the module firmware will calculate the checksum automatically. When the length is 4, no checksum is required.
        /// Upon a successful reception of the command, the module tries to associate to a network using the WPS configuration specified. Upon a successful completion of the join process, the SSID and authentication parameters will be saved in NVM which will be used in subsequent power up (see 6.1).
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="Pin"></param>
        /// <returns></returns>
        public WIFICode WIFI_StartWPSProcess(WPSMode mode, string Pin = null)
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.WIFI_WPS_REQ);
            _sendPayload.SetFrameID(frameID++);
            _sendPayload.SetContent((byte)mode);

            if (mode == WPSMode.Pin)
            {
                if (Pin == null)
                    throw new ArgumentNullException("Pin not present");

                _sendPayload.SetContent(UTF8Encoding.UTF8.GetBytes(Pin));
                _sendPayload.SetContent(0x00);
            }

            _sendFrame.SetCommandID(CommandID.CMD_ID_WIFI);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return WIFICode.WIFI_NORESPONSE;
            }

            return (WIFICode)_sendPayload.GetData()[2];
        }

        /// <summary>
        /// Upon a successful reception of the command, the module starts to scan. The response will indicate only WIFI_SUCCESS if no error. Actual scan result shall be sent from module as multiple indications defined in WIFI_SCAN_RESULT_IND
        /// </summary>
        /// <param name="scan"></param>
        /// <param name="bss"></param>
        /// <param name="BSSID">6 bytes MAC address of the AP or STA.</param>
        /// <param name="channelList">up to 10 array elements</param>
        /// <param name="SSID">string for the AP or STA SSID, up to 32 bytes</param>
        /// <returns></returns>
        public WIFICode WIFI_ScanNetworks(ScanType scan, BSSType bss, byte[] BSSID = null, byte[] channelList = null, string SSID = null)
        {
            if (BSSID != null && BSSID.Length != 6)
                throw new ArgumentException("BSSID: 6 bytes MAC address of the AP or STA.");

            if (channelList != null && channelList.Length > 10)
                throw new ArgumentOutOfRangeException("Channel list: up to 10 array elements");

            byte[] _ssid = null;
            if (SSID != null)
            {
                _ssid = UTF8Encoding.UTF8.GetBytes(SSID);
                if (_ssid.Length > 32)
                    throw new ArgumentOutOfRangeException("AP or STA SSID, up to 32 bytes.");
            }

            /*
             * This command instructs the module to scan available networks. Parameters are as follows:
             * UINT8 Request Sequence
             * UINT8 Scan Type
             * UINT8 BSS Type
             * UINT8 BSSID [6]
             * UINT8 Channel list []
             * UINT8 SSID[]
             * BSSID, Channel List, and SSID are optional fields. All 0’s for BSSID, Channel list or SSID indicates it is not present.
             * - Scan Type: 0 = Active scan, 1= Passive scan
             * - BSS Type: 0 = Infrastructure, 1 = ad hoc, 2 = any
             * - BSSID: 6 bytes MAC address of the AP or STA. 6 bytes of 0’s indicates it is not present.
             * - Channel list: 0 terminated array, up to 10 array elements.
             * - SSID: 0 terminated string for the AP or STA SSID, up to 33 bytes including NUL-termination.
             */

            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.WIFI_SCAN_REQ);
            _sendPayload.SetFrameID(frameID++);
            _sendPayload.SetContent((byte)scan);
            _sendPayload.SetContent((byte)bss);

            if (BSSID == null)
                _sendPayload.SetContent(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            else
                _sendPayload.SetContent(BSSID);

            if (channelList != null)
                _sendPayload.SetContent(channelList);
            _sendPayload.SetContent(0x00);

            if (_ssid != null)
                _sendPayload.SetContent(_ssid);
            _sendPayload.SetContent(0x00);

            _sendFrame.SetCommandID(CommandID.CMD_ID_WIFI);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return WIFICode.WIFI_NORESPONSE;
            }

            return (WIFICode)_sendPayload.GetData()[2];
        }

        #endregion

        #region SNIC API

        /// <summary>
        /// This command initializes the SNIC networking framework on module. TCP/UDP socket communication may be performed only after this command is called.
        /// The Default receive buffer size is the default maximum size of receive buffer in the module. If 0 is specified, a system defined value (2048) will be used. If there is a Receive buffer size field in other commands, then it must be less than or equal to the Default receive buffer size. If the Receive buffer size in any of those commands is 0, the Default receive buffer size will be used.
        /// </summary>
        /// <param name="receiveBufferSize">Upon a successful reception of the command, the module sends to the host the following response. If user specified Default receive buffer size is bigger than what the module can handle, the system defined value will be returned in the response; otherwise, user specified Default receive buffer size will be retuned. Maximum number of UDP and TCP sockets supported by module will also be returned.</param>
        /// <returns></returns>
        public InitializationResponse SNIC_Initialization(int receiveBufferSize = 0)
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.SNIC_INIT_REQ);
            _sendPayload.SetFrameID(frameID++);
            _sendPayload.SetContent((byte)(receiveBufferSize >> 8));
            _sendPayload.SetContent((byte)receiveBufferSize);

            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return null;
            }

            return new InitializationResponse(_sendPayload);
        }

        /// <summary>
        /// This command closes the SNIC networking framework on module. It should cleanup resources for socket communication on module. If some sockets are not closed, this command will close all of them. No more network communication can be performed until SNIC_INIT_REQ is called.
        /// </summary>
        /// <returns></returns>
        public SNICCode SNIC_Cleanup()
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.SNIC_CLEANUP_REQ);
            _sendPayload.SetFrameID(frameID++);

            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return SNICCode.SNIC_NORESPONSE;
            }

            return (SNICCode)_sendPayload.GetData()[2];
        }

        /// <summary>
        /// In TCP server case, Socket is the socket number returned by SNIC_TCP_CLIENT_SOCKET_IND. In TCP client case, Socket can be either from SNIC_CONNECT_TO_TCP_SERVER_RSP, or from the SNIC_TCP_CONNECTION_STATUS_IND with SNIC_CONNECTION_UP status. In UDP case, Socket is the socket number returned by SNIC_UDP_CREATE_SOCKET_REQ and it must be in connected mode.
        /// A success response of this command does not guarantee the receiver receives the packet. If error occurs, a SNIC_TCP_CONNECTION_STATUS_IND with SNIC_SOCKET_CLOSED will be sent to the application in TCP case. No indication will be sent in UDP case.
        /// Option is the action module will perform to the socket after the send operation. Use it when application is sure to close or shutdown the connection after sending. The effect is the same as using SNIC_CLOSE_SOCKET_REQ, but round-trip UART traffic is reduced.
        /// </summary>
        /// <param name="SocketID"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public SendFromSocketResponse SNIC_SendFromSocket(byte SocketID, SocketSentOption option, byte[] payload, int offset, int length)
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.SNIC_SEND_FROM_SOCKET_REQ);
            _sendPayload.SetFrameID(frameID++);
            _sendPayload.SetContent(SocketID);
            _sendPayload.SetContent((byte)option);
            _sendPayload.SetContent((byte)(length >> 8));
            _sendPayload.SetContent((byte)length);
            _sendPayload.SetContent(payload, offset, length);

            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return null;
            }

            return new SendFromSocketResponse(_sendPayload);
        }

        /// <summary>
        /// In TCP server case, Socket is the socket number returned by SNIC_TCP_CLIENT_SOCKET_IND. In TCP client case, Socket can be either from SNIC_CONNECT_TO_TCP_SERVER_RSP, or from the SNIC_TCP_CONNECTION_STATUS_IND with SNIC_CONNECTION_UP status. In UDP case, Socket is the socket number returned by SNIC_UDP_CREATE_SOCKET_REQ and it must be in connected mode.
        /// A success response of this command does not guarantee the receiver receives the packet. If error occurs, a SNIC_TCP_CONNECTION_STATUS_IND with SNIC_SOCKET_CLOSED will be sent to the application in TCP case. No indication will be sent in UDP case.
        /// Option is the action module will perform to the socket after the send operation. Use it when application is sure to close or shutdown the connection after sending. The effect is the same as using SNIC_CLOSE_SOCKET_REQ, but round-trip UART traffic is reduced.
        /// </summary>
        /// <param name="SocketID"></param>
        /// <param name="option"></param>
        /// <returns></returns>
        public SendFromSocketResponse SNIC_SendFromSocket(byte SocketID, SocketSentOption option, byte[] payload) { return SNIC_SendFromSocket(SocketID, option, payload, 0, payload.Length); }

        /// <summary>
        /// This command instructs the module to close a socket.
        /// </summary>
        /// <param name="SocketID"></param>
        /// <returns></returns>
        public SNICCode SNIC_SloseSocket(byte SocketID)
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.SNIC_CLOSE_SOCKET_REQ);
            _sendPayload.SetFrameID(frameID++);
            _sendPayload.SetContent(SocketID);

            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return SNICCode.SNIC_NORESPONSE;
            }

            return (SNICCode)_sendPayload.GetData()[2];
        }

        /// <summary>
        /// This command queries the DHCP information for a particular interface.
        /// </summary>
        /// <param name="wifiInterface"></param>
        /// <returns></returns>
        public DHCPInfoResponse SNIC_GetDHCPInfo(WIFIInterface wifiInterface)
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.SNIC_GET_DHCP_INFO_REQ);
            _sendPayload.SetFrameID(frameID++);
            _sendPayload.SetContent((byte)wifiInterface);

            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return null;
            }

            return new DHCPInfoResponse(_sendPayload);
        }

        /// <summary>
        /// This command converts a remote host name to IP address.
        /// Interface number is either 0 or 1. 0 indicates STA interface. 1 indicates soft AP interface. Currently only STA interface is supported.
        /// If multiple SNIC_RESOLVE_NAME_REQ’s need to be sent, it is required they be sent sequentially due to resource limitation. If the name is not resolved, it takes up to 15 seconds for the failure response to come back. While waiting for the response, host application can send other commands (except for SNIC_RESOLVE_NAME_REQ and SNIC_SEND_ARP_REQ).
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public IPAddress SNIC_ResolveHostName(string host)
        {
            byte[] name = UTF8Encoding.UTF8.GetBytes(host);

            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.SNIC_RESOLVE_NAME_REQ);
            _sendPayload.SetFrameID(frameID++);
            _sendPayload.SetContent((byte)WIFIInterface.STA);
            _sendPayload.SetContent((byte)name.Length);
            _sendPayload.SetContent(name);

            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return null;
            }

            if ((SNICCode)_sendPayload.GetData()[2] == SNICCode.SNIC_SUCCESS)
                return new IPAddress(_sendPayload.GetData(), 3);

            return null;
        }

        /// <summary>
        /// This command instructs module configure the mechanism for obtaining the IP address.
        /// DHCP mode specifies how the address is assigned for the interface.
        ///  0: interface is assigned the static IP, NetMask and Gateway IP. First IP and Last IP are not present. Any active DHCP client or server is stopped.
        ///  1: STA interface uses DHCP to obtain the address. All subsequent fields are not present. STA DHCP client is started if necessary.
        ///  2: only for AP interface. If the soft AP is not started or SNIC_INIT_REQ is not done, this command fails. Otherwise, this command stops the HTTP server, DNS server and DHCP server if configured, and restarts them with new parameters. It assigns IP for clients in range [First IP, Last IP] within the subnet mask. The AP itself is assigned the address within the same subnet specified by IP which must not be in the range of [First IP, Last IP]. The value of GTW IP and IP should be the same. If there are clients connected to the soft AP before this command, make sure the clients reconnect to the soft AP after this command.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public SNICCode SNIC_ConfigureDHCPorStaticIP(DHCPConfig config)
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.SNIC_IP_CONFIG_REQ);
            _sendPayload.SetFrameID(frameID++);
            _sendPayload.SetContent((byte)config.GetInterface());
            _sendPayload.SetContent((byte)config.GetDHCPMode());

            if (config.GetDHCPMode() != DHCPMode.dynamic_IP)
            {
                _sendPayload.SetContent(config.GetLocalIP().GetValue());
                _sendPayload.SetContent(config.GetNetmask().GetValue());
                _sendPayload.SetContent(config.GetGatewayIP().GetValue());
            }

            if (config.GetDHCPMode() == DHCPMode.soft_AP)
            {
                _sendPayload.SetContent(config.GetIPRangeFirst().GetValue());
                _sendPayload.SetContent(config.GetIPRangeLast().GetValue());
            }

            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return SNICCode.SNIC_NORESPONSE;
            }

            return (SNICCode)_sendPayload.GetData()[2];
        }

        /// <summary>
        /// If the connect attempt is immediately completed, the response will contain SNIC_SUCCESS status, with the actual Receive buffer size.
        /// If the connect attempt is not immediately completed, the response will have the SNIC_COMMAND_PENDING status. The Timeout value is the time (in seconds) the module will wait before aborting the connection attempt. If timeout occurs, the SNIC_TCP_CONNECTION_STATUS_IND indication with SNIC_TIMEOUT status will be sent to the application. If connection is successful before timeout, the SNIC_TCP_CONNECTION_STATUS_IND with SNIC_CONNECTION_UP status will be sent to the application. Timeout value should be non-zero.
        /// </summary>
        /// <param name="remoteHost"></param>
        /// <param name="port"></param>
        /// <param name="timeout">in seconds</param>
        /// <param name="receiveBufferSize">Receive buffer size is the maximum packet size the application wants to receive per transmission. It must be less than or equal to the Default receive buffer size from SNIC_INIT_REQ in the module. If it is 0 or exceeds the system capability, the Default receive buffer size is returned.</param>
        public SocketStartReceiveResponse SNIC_ConnectTCPServer(byte SocketID, IPAddress remoteIP, int remotePort, byte timeout, int receiveBufferSize = 0)
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.SNIC_TCP_CONNECT_TO_SERVER_REQ);
            _sendPayload.SetFrameID(frameID++);
            _sendPayload.SetContent(SocketID);
            _sendPayload.SetContent(remoteIP.GetValue());
            _sendPayload.SetContent((byte)(remotePort >> 8));
            _sendPayload.SetContent((byte)remotePort);
            _sendPayload.SetContent((byte)(receiveBufferSize >> 8));
            _sendPayload.SetContent((byte)receiveBufferSize);
            _sendPayload.SetContent(timeout);

            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return null;
            }

            return new SocketStartReceiveResponse(_sendPayload);
        }

        /// <summary>
        /// If Bind option is 0, the socket will not be bound, and Local IP address and Local port should not be present. Otherwise, it will be bound to Local IP address and Local port specified. 0x0 for IP or port are valid, which means system assigned. Port number 5000 is reserved for internal use.
        /// the socket number must get and store separately, since the response payload may change
        /// </summary>
        /// <param name="bing">do not bing if this tcp socket is used as a client</param>
        /// <param name="localIP"></param>
        /// <param name="localPort"></param>
        /// <returns></returns>
        public CreateSocketResponse SNIC_CreateTCPSocket(bool bind = false, IPAddress localIP = null, int localPort = 0) { return SNIC_CreateSocket(SubCommandID.SNIC_TCP_CREATE_SOCKET_REQ, bind, localIP, localPort); }

        /// <summary>
        /// If Bind option is 0, the socket will not be bound, and Local IP address and Local port should not be present. Otherwise, it will be bound to Local IP address and Local port specified. 0x0 for IP or port are valid, which means system assigned. Port number 5000 is reserved for internal use.
        /// the socket number must get and store separately, since the response payload may change
        /// </summary>
        /// <param name="bind"></param>
        /// <param name="localIP"></param>
        /// <param name="localPort"></param>
        /// <returns></returns>
        public CreateSocketResponse SNIC_CreateUDPSocket(bool bind = false, IPAddress localIP = null, int localPort = 0) { return SNIC_CreateSocket(SubCommandID.SNIC_UDP_CREATE_SOCKET_REQ, bind, localIP, localPort); }

        private CreateSocketResponse SNIC_CreateSocket(SubCommandID subID, bool bind = false, IPAddress localIP = null, int localPort = 0)
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(subID);
            _sendPayload.SetFrameID(frameID++);
            _sendPayload.SetContent((byte)(bind ? 0x01 : 0x00));

            if (bind)
            {
                if (localIP != null)
                    _sendPayload.SetContent(localIP.GetValue());
                else
                    _sendPayload.SetContent(IPAddress.ANY.GetValue());

                _sendPayload.SetContent((byte)(localPort >> 8));
                _sendPayload.SetContent((byte)localPort);
            }

            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return null;
            }

            return new CreateSocketResponse(_sendPayload);
        }

        /// <summary>
        /// The Socket should have been created by command SNIC_UDP_CREATE_SOCKET_REQ. The same socket can be used in SNIC_UDP_SEND_FROM_SOCKET_REQ command, so that send and receive can be done via the same socket (port). The application is responsible to close the socket using SNIC_CLOSE_SOCKET_REQ.
        /// Receive buffer size is the maximum packet size the application wants to receive per transmission. It must be less than or equal to the Default receive buffer size from SNIC_INIT_REQ in the module. If 0 or exceeds the system capability, the Default receive buffer size will be used and returned in the response.
        /// After this command, the Socket can receive any UDP sender with connected mode or non-connected mode. The module will generate SNIC_UDP_RECV_IND indication for incoming data, which includes sender’s IP and port info.
        /// But if this Socket is later connected to a peer UDP server by SNIC_UDP_SEND_FROM_SOCKET_REQ with Connection mode set to1, the module will generate SNIC_CONNECTION_RECV_IND indication without the sender’s IP and port info. See Section 5.19. After that, this Socket will only be able to receive from the one sender it connects to.
        /// </summary>
        /// <param name="SocketID"></param>
        /// <param name="receiveBufferSize"></param>
        public SocketStartReceiveResponse SNIC_StartUDPReceive(byte SocketID, int receiveBufferSize = 0)
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.SNIC_UDP_START_RECV_REQ);
            _sendPayload.SetFrameID(frameID++);
            _sendPayload.SetContent(SocketID);
            _sendPayload.SetContent((byte)(receiveBufferSize >> 8));
            _sendPayload.SetContent((byte)receiveBufferSize);

            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return null;
            }

            return new SocketStartReceiveResponse(_sendPayload);
        }

        /// <summary>
        /// A socket will be created for sending the packet out through the default network connection, but will be closed after the transmission. This command can be used when the application just wants to send out one packet to peer, and it also does not expect to receive any packets from peer.
        /// </summary>
        /// <param name="remoteIP"></param>
        /// <param name="remotePort"></param>
        /// <param name="payload"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public SendFromSocketResponse SNIC_SendUDPPacket(IPAddress remoteIP, int remotePort, byte[] payload, int offset, int length)
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.SNIC_UDP_SIMPLE_SEND_REQ);
            _sendPayload.SetFrameID(frameID++);
            _sendPayload.SetContent(remoteIP.GetValue());
            _sendPayload.SetContent((byte)(remotePort >> 8));
            _sendPayload.SetContent((byte)remotePort);
            _sendPayload.SetContent((byte)(length >> 8));
            _sendPayload.SetContent((byte)length);
            _sendPayload.SetContent(payload, offset, length);

            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return null;
            }

            return new SendFromSocketResponse(_sendPayload);
        }

        /// <summary>
        /// A socket will be created for sending the packet out through the default network connection, but will be closed after the transmission. This command can be used when the application just wants to send out one packet to peer, and it also does not expect to receive any packets from peer.
        /// </summary>
        /// <param name="remoteIP"></param>
        /// <param name="remotePort"></param>
        /// <param name="payload"></param>
        public SendFromSocketResponse SNIC_SendUDPPacket(IPAddress remoteIP, int remotePort, byte[] payload) { return SNIC_SendUDPPacket(remoteIP, remotePort, payload, 0, payload.Length); }

        /// <summary>
        /// The Socket should have been created by command SNIC_UDP_CREATE_SOCKET_REQ. If SNIC_UDP_START_RECV_REQ is not called on the socket, the application can only send out UDP packet from this socket. If SNIC_UDP_START_RECV_REQ has been called for this socket, the application can send and receive UDP packets from the socket. This implies the application can send and receive packets from the same local port. The application is responsible to close the socket using SNIC_CLOSE_SOCKET_REQ.
        /// If Connection mode is 1, the module will first connect to the UDP server then send data. Since the socket is still connected after the call, application can send subsequent data using another command SNIC_SEND_FROM_SOCKET_REQ.
        /// The benefit of the connected mode is that subsequent send can use SNIC_SEND_FROM_SOCKET_REQ, which does not require the receiver’s IP and port every time, and thus reduces overhead. If this socket is also used to receive by calling SNIC_UDP_START_RECV_REQ, the receive indication to the host will also omits the sender IP and port info, further reducing overhead.
        /// </summary>
        /// <param name="remoteIP"></param>
        /// <param name="remotePort"></param>
        /// <param name="SocketID"></param>
        /// <param name="connectServer"></param>
        /// <param name="payload"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public SendFromSocketResponse SNIC_SendUDPFromSocket(IPAddress remoteIP, int remotePort, byte SocketID, bool connectServer, byte[] payload, int offset, int length)
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.SNIC_UDP_SEND_FROM_SOCKET_REQ);
            _sendPayload.SetFrameID(frameID++);
            _sendPayload.SetContent(remoteIP.GetValue());
            _sendPayload.SetContent((byte)(remotePort >> 8));
            _sendPayload.SetContent((byte)remotePort);
            _sendPayload.SetContent(SocketID);
            _sendPayload.SetContent((byte)(connectServer ? 0x01 : 0x00));
            _sendPayload.SetContent((byte)(length >> 8));
            _sendPayload.SetContent((byte)length);
            _sendPayload.SetContent(payload, offset, length);

            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return null;
            }

            return new SendFromSocketResponse(_sendPayload);
        }

        /// <summary>
        /// The Socket should have been created by command SNIC_UDP_CREATE_SOCKET_REQ. If SNIC_UDP_START_RECV_REQ is not called on the socket, the application can only send out UDP packet from this socket. If SNIC_UDP_START_RECV_REQ has been called for this socket, the application can send and receive UDP packets from the socket. This implies the application can send and receive packets from the same local port. The application is responsible to close the socket using SNIC_CLOSE_SOCKET_REQ.
        /// If Connection mode is 1, the module will first connect to the UDP server then send data. Since the socket is still connected after the call, application can send subsequent data using another command SNIC_SEND_FROM_SOCKET_REQ.
        /// The benefit of the connected mode is that subsequent send can use SNIC_SEND_FROM_SOCKET_REQ, which does not require the receiver’s IP and port every time, and thus reduces overhead. If this socket is also used to receive by calling SNIC_UDP_START_RECV_REQ, the receive indication to the host will also omits the sender IP and port info, further reducing overhead.
        /// </summary>
        /// <param name="remoteIP"></param>
        /// <param name="remotePort"></param>
        /// <param name="SocketID"></param>
        /// <param name="connectServer"></param>
        /// <param name="payload"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public SendFromSocketResponse SNIC_SendUDPFromSocket(IPAddress remoteIP, int remotePort, byte SocketID, bool connectServer, byte[] payload) { return SNIC_SendUDPFromSocket(remoteIP, remotePort, SocketID, connectServer, payload, 0, payload.Length); }

        /// <summary>
        /// This command instructs the module to send a HTTP request packet to the remote HTTP server.
        /// Post content can be binary. So even if it is text string, it should not contain NUL at the end. The most significant bit of Post content length is reserved to indicate if there is more data to send. If there is more data to send (as indicated by MSBit=1 in the content length), host application should use another API (SNIC_HTTP_MORE_REQ) to send the rest of the data until it is finished. If this bit is set to 1, then the “Transfer-Encoding” in the HTTP request will be set to “chunked” by SNIC. For GET method, the highest bit of Content length must be set to 0 (not chunked).
        /// For HTTP request with chunked encoding, status code of SNIC_SUCCESS in the response only means the HTTP request has been sent. After one or more subsequent SNIC_HTTP_MORE_REQ/RSPs, the last SNIC_HTTP_MORE_RSP with HTTP status code will be sent to host containing the data from HTTP server.
        /// The most significant bit of Content length is reserved to indicate if there is more response data to send to the host. If there is more data to send (Content length MSBit=1), module uses SNIC_HTTP_RSP_IND to send the rest of the data until it is finished, i.e., when this bit is 1, the host application should continue to receive SNIC_HTTP_RSP_IND, until this bit is 0.
        /// The Content length is limited by the receive buffer size specified in SNIC_INIT_REQ and the system resource at that moment.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="isHTTPS"></param>
        /// <param name="chunked"></param>
        /// <returns></returns>
        public HTTPResponse SNIC_SendHTTPRequest(HTTPContent content, bool isHTTPS = false, bool chunked = false)
        {
            _sendPayload.Rewind();
            if (isHTTPS)
                _sendPayload.SetSubCommandID(SubCommandID.SNIC_HTTPS_REQ);
            else
                _sendPayload.SetSubCommandID(SubCommandID.SNIC_HTTP_REQ);

            _sendPayload.SetFrameID(frameID++);
            _sendPayload.SetContent((byte)(content.GetRemotePort() >> 8));
            _sendPayload.SetContent((byte)content.GetRemotePort());
            _sendPayload.SetContent((byte)content.GetMethod());
            _sendPayload.SetContent(content.GetTimeout());

            _sendPayload.SetContent(UTF8Encoding.UTF8.GetBytes(content.GetRemoteHost()));
            _sendPayload.SetContent(0x00);

            _sendPayload.SetContent(UTF8Encoding.UTF8.GetBytes(content.GetURI()));
            _sendPayload.SetContent(0x00);

            _sendPayload.SetContent(UTF8Encoding.UTF8.GetBytes(content.GetContentType()));
            _sendPayload.SetContent(0x00);

            _sendPayload.SetContent(UTF8Encoding.UTF8.GetBytes(content.GetAllOtherHeaders()));
            _sendPayload.SetContent(0x00);

            if (content.GetMethod() == HTTPMethod.POST)
            {
                int length = content.GetContentLength();

                byte msb = (byte)(length >> 8);
                if (chunked)
                    msb |= 0x80;
                else msb &= 0x7F;

                _sendPayload.SetContent(msb);
                _sendPayload.SetContent((byte)length);

                if (length > 0)
                    _sendPayload.SetContent(content.GetBody());
            }

            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendFrame.SetPayload(_sendPayload);

            this.Send();

            waitEvent.WaitOne(content.GetTimeout() * 1000);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return null;
            }

            return new HTTPResponse(_sendPayload);
        }

        /// <summary>
        /// This command instructs the module to send a subsequent HTTP request packet to the remote HTTP server if the initial SNIC_HTTP_REQ cannot finish the packet due to size or other consideration. It is used when the send method is POST.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="chunked"></param>
        /// <returns></returns>
        public HTTPResponse SNIC_SendHTTPMoreRequest(HTTPContent content, bool chunked = false)
        {
            _sendPayload.Rewind();
            _sendPayload.SetSubCommandID(SubCommandID.SNIC_HTTP_MORE_REQ);
            _sendPayload.SetFrameID(frameID++);

            int length = content.GetContentLength();
            byte msb = (byte)(length >> 8);
            if (chunked)
                msb |= 0x80;
            else msb &= 0x7F;

            _sendPayload.SetContent(msb);
            _sendPayload.SetContent((byte)length);

            if (length > 0)
                _sendPayload.SetContent(content.GetBody());

            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendFrame.SetPayload(_sendPayload);

            this.Send(!chunked);

            if (chunked)
                return null;

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (this.isResponseSignal)
            {
                this.isResponseSignal = false;
                return null;
            }

            return new HTTPResponse(_sendPayload);
        }

        /// <summary>
        /// If Bind option is 0, the socket will not be bound, and Local IP address and Local port should not be present. Otherwise, it will be bound to Local IP address and Local port specified. 0x0 for IP or port are valid, which means system assigned. Port number 5000 is reserved for internal use.
        /// the socket number must get and store separately, since the response payload may change
        /// </summary>
        /// <param name="bing">do not bing if this tcp socket is used as a client</param>
        /// <param name="localIP"></param>
        /// <param name="localPort"></param>
        /// <returns></returns>
        public CreateSocketResponse SNIC_CreateAdvancedTLSTCP(bool bind = false, IPAddress localIP = null, int localPort = 0) { return SNIC_CreateSocket(SubCommandID.SNIC_TCP_CREATE_ADV_TLS_SOCKET_REQ, bind, localIP, localPort); }

        /// <summary>
        /// If Bind option is 0, the socket will not be bound, and Local IP address and Local port should not be present. Otherwise, it will be bound to Local IP address and Local port specified. 0x0 for IP or port are valid, which means system assigned. Port number 5000 is reserved for internal use.
        /// the socket number must get and store separately, since the response payload may change
        /// </summary>
        /// <param name="bind"></param>
        /// <param name="localIP"></param>
        /// <param name="localPort"></param>
        /// <returns></returns>
        public CreateSocketResponse SNIC_CreateSimpleTLSTCP(bool bind = false, IPAddress localIP = null, int localPort = 0) { return SNIC_CreateSocket(SubCommandID.SNIC_TCP_CREAET_SIMPLE_TLS_SOCKET_REQ, bind, localIP, localPort); }

        #endregion

        /*
        #region Indication
        
        public SSIDRecordIndication Indication_ScanResult()
        {
            isIndicationSignal = true;
            _sendFrame.SetCommandID(CommandID.CMD_ID_WIFI);
            _sendPayload.SetSubCommandID(SubCommandID.WIFI_SCAN_RESULT_IND);

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (isIndicationSignal)
            {
                isIndicationSignal = false;
                return null;
            }

            return new SSIDRecordIndication(_sendPayload);
        }

        public WIFIConnectionIndication Indication_WiFiStatus()
        {
            isIndicationSignal = true;
            _sendFrame.SetCommandID(CommandID.CMD_ID_WIFI);
            _sendPayload.SetSubCommandID(SubCommandID.WIFI_NETWORK_STATUS_IND);

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (isIndicationSignal)
            {
                isIndicationSignal = false;
                return null;
            }

            return new WIFIConnectionIndication(_sendPayload);
        }

        public PowerUpIndication Indication_Get_PowerUp()
        {
            isIndicationSignal = true;
            _sendFrame.SetCommandID(CommandID.CMD_ID_GEN);
            _sendPayload.SetSubCommandID(SubCommandID.GEN_PWR_UP_IND);

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (isIndicationSignal)
            {
                isIndicationSignal = false;
                return null;
            }
            return new PowerUpIndication(_sendPayload);
        }

        public TCPStatusIndication Indication_TcpConnectionStatus()
        {
            isIndicationSignal = true;
            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendPayload.SetSubCommandID(SubCommandID.SNIC_TCP_CONNECTION_STATUS_IND);

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (isIndicationSignal)
            {
                isIndicationSignal = false;
                return null;
            }
            return new TCPStatusIndication(_sendPayload);
        }

        public SocketReceiveInidcation Indication_SocketReceive()
        {
            isIndicationSignal = true;
            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendPayload.SetSubCommandID(SubCommandID.SNIC_CONNECTION_RECV_IND);

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (isIndicationSignal)
            {
                isIndicationSignal = false;
                return null;
            }
            return new SocketReceiveInidcation(_sendPayload);
        }

        public UDPReceivedIndication Indication_UDPReceive()
        {
            isIndicationSignal = true;
            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendPayload.SetSubCommandID(SubCommandID.SNIC_UDP_RECV_IND);

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (isIndicationSignal)
            {
                isIndicationSignal = false;
                return null;
            }
            return new UDPReceivedIndication(_sendPayload);
        }

        public HTTPResponseIndication Indication_HTTPResponse()
        {
            isIndicationSignal = true;
            _sendFrame.SetCommandID(CommandID.CMD_ID_SNIC);
            _sendPayload.SetSubCommandID(SubCommandID.SNIC_HTTP_RSP_IND);

            waitEvent.WaitOne(DEFAULT_WAIT);

            if (isIndicationSignal)
            {
                isIndicationSignal = false;
                return null;
            }
            return new HTTPResponseIndication(_sendPayload);
        }

        #endregion
        */
    }
}
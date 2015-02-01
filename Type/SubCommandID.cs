
namespace SmartLab.MuRata.Type
{
    public enum SubCommandID
    {
        /// <summary>
        /// Power up indication
        /// </summary>
        GEN_PWR_UP_IND = 0x00,

        /// <summary>
        /// Sleep configuration
        /// </summary>
        GEN_SLEEP_CFG_REQ = 0x05,

        /// <summary>
        /// Get firmware version string
        /// </summary>
        GEN_FW_VER_GET_REQ = 0x08,

        /// <summary>
        /// Restore NVM to factory default
        /// </summary>
        GEN_RESTORE_REQ = 0x09,

        /// <summary>
        /// Soft reset the module
        /// </summary>
        GEN_RESET_REQ = 0x0A,

        /// <summary>
        /// Configure UART interface
        /// </summary>
        GEN_UART_CFG_REQ = 0x0B,


        /// <summary>
        /// Turn on Wifi
        /// </summary>
        WIFI_ON_REQ = 0x00,

        /// <summary>
        /// Turn off Wifi
        /// </summary>
        WIFI_OFF_REQ = 0x01,

        /// <summary>
        /// Associate to a network
        /// </summary>
        WIFI_JOIN_REQ = 0x02,

        /// <summary>
        /// Disconnect from a network
        /// </summary>
        WIFI_DISCONNECT_REQ = 0x03,

        /// <summary>
        /// Get WiFi status
        /// </summary>
        WIFI_GET_STATUS_REQ = 0x04,

        /// <summary>
        /// Scan WiFi networks
        /// </summary>
        WIFI_SCAN_REQ = 0x05,

        /// <summary>
        /// Get STA signal strength (RSSI)
        /// </summary>
        WIFI_GET_STA_RSSI_REQ = 0x06,

        /// <summary>
        /// Soft AP on-off control
        /// </summary>
        WIFI_AP_CTRL_REQ = 0x07,

        /// <summary>
        /// Start WPS process
        /// </summary>
        WIFI_WPS_REQ = 0x08,

        /// <summary>
        /// Get clients that are associated to the soft AP.
        /// </summary>
        WIFI_AP_GET_CLIENT_REQ = 0x0A,

        /// <summary>
        /// Network status indication
        /// </summary>
        WIFI_NETWORK_STATUS_IND = 0x10,

        /// <summary>
        /// Scan result indication
        /// </summary>
        WIFI_SCAN_RESULT_IND = 0x11,



        /// <summary>
        /// SNIC API initialization
        /// </summary>
        SNIC_INIT_REQ = 0x00,

        /// <summary>
        /// SNIC API cleanup
        /// </summary>
        SNIC_CLEANUP_REQ = 0x01,

        /// <summary>
        /// Send from socket
        /// </summary>
        SNIC_SEND_FROM_SOCKET_REQ = 0x02,

        /// <summary>
        /// Close socket
        /// </summary>
        SNIC_CLOSE_SOCKET_REQ = 0x03,

        /// <summary>
        /// Get socket option
        /// </summary>
        SNIC_GETSOCKOPT_REQ = 0x05,

        /// <summary>
        /// Set socket option
        /// </summary>
        SNIC_SETSOCKOPT_REQ = 0x06,

        /// <summary>
        /// Get name or peer name
        /// </summary>
        SNIC_SOCKET_GETNAME_REQ = 0x07,

        /// <summary>
        /// Send ARP request
        /// </summary>
        SNIC_SEND_ARP_REQ = 0x08,

        /// <summary>
        /// Get DHCP info
        /// </summary>
        SNIC_GET_DHCP_INFO_REQ = 0x09,

        /// <summary>
        /// Resolve a host name to IP address
        /// </summary>
        SNIC_RESOLVE_NAME_REQ = 0x0A,

        /// <summary>
        /// Configure DHCP or static IP
        /// </summary>
        SNIC_IP_CONFIG_REQ = 0x0B,

        /// <summary>
        /// ACK configuration for data indications
        /// </summary>
        SNIC_DATA_IND_ACK_CONFIG_REQ = 0x0C,

        /// <summary>
        /// Create TCP socket
        /// </summary>
        SNIC_TCP_CREATE_SOCKET_REQ = 0x10,

        /// <summary>
        /// Create TCP connection server
        /// </summary>
        SNIC_TCP_CREATE_CONNECTION_REQ = 0x11,

        /// <summary>
        /// Connect to TCP server
        /// </summary>
        SNIC_TCP_CONNECT_TO_SERVER_REQ = 0x12,

        /// <summary>
        /// Create UDP socket
        /// </summary>
        SNIC_UDP_CREATE_SOCKET_REQ = 0x13,

        /// <summary>
        /// Start UDP receive on socket
        /// </summary>
        SNIC_UDP_START_RECV_REQ = 0x14,

        /// <summary>
        /// Send UDP packet
        /// </summary>
        SNIC_UDP_SIMPLE_SEND_REQ = 0x15,

        /// <summary>
        /// Send UDP packet from socket
        /// </summary>
        SNIC_UDP_SEND_FROM_SOCKET_REQ = 0x16,

        /// <summary>
        /// Send HTTP request
        /// </summary>
        SNIC_HTTP_REQ = 0x17,

        /// <summary>
        /// Send HTTP more data request
        /// </summary>
        SNIC_HTTP_MORE_REQ = 0x18,

        /// <summary>
        /// Send HTTPS request
        /// </summary>
        SNIC_HTTPS_REQ = 0x19,

        /// <summary>
        /// Create advanced TLS TCP socket
        /// </summary>
        SNIC_TCP_CREATE_ADV_TLS_SOCKET_REQ = 0x1A,

        /// <summary>
        /// Create simple TLS TCP socket
        /// </summary>
        SNIC_TCP_CREAET_SIMPLE_TLS_SOCKET_REQ = 0x1B,

        /// <summary>
        /// Connection status indication
        /// </summary>
        SNIC_TCP_CONNECTION_STATUS_IND = 0x20,

        /// <summary>
        /// TCP client socket indication
        /// </summary>
        SNIC_TCP_CLIENT_SOCKET_IND = 0x21,

        /// <summary>
        /// TCP or connected UDP packet received indication
        /// </summary>
        SNIC_CONNECTION_RECV_IND = 0x22,

        /// <summary>
        /// UCP packet received indication
        /// </summary>
        SNIC_UDP_RECV_IND = 0x23,

        /// <summary>
        /// ARP reply indication
        /// </summary>
        SNIC_ARP_REPLY_IND = 0x24,

        /// <summary>
        /// HTTP response indication
        /// </summary>
        SNIC_HTTP_RSP_IND = 0x25,
    }
}
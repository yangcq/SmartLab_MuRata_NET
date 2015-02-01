namespace SmartLab.MuRata.ErrorCode
{
    public enum WIFICode
    {
        WIFI_NORESPONSE = -1,
        WIFI_SUCCESS = 0x00,
        WIFI_ERR_UNKNOWN_COUNTRY = 0x01,
        WIFI_ERR_INIT_FAIL = 0x02,
        WIFI_ERR_ALREADY_JOINED = 0x03,
        WIFI_ERR_AUTH_TYPE = 0x04,
        WIFI_ERR_JOIN_FAIL = 0x05,
        WIFI_ERR_NOT_JOINED = 0x06,
        WIFI_ERR_LEAVE_FAILED = 0x07,
        WIFI_COMMAND_PENDING = 0x08,
        WIFI_WPS_NO_CONFIG = 0x09,
        WIFI_NETWORK_UP = 0x10,
        WIFI_NETWORK_DOWN = 0x11,
        WIFI_FAIL = 0xFF,
    }
}
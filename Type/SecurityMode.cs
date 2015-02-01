
namespace SmartLab.MuRata.Type
{
    public enum SecurityMode
    {
        WIFI_SECURITY_OPEN = 0x00,
        WEP = 0x01,
        WIFI_SECURITY_WPA_TKIP_PSK = 0x02,
        WIFI_SECURITY_WPA2_AES_PSK = 0x04,
        WIFI_SECURITY_WPA2_MIXED_PSK = 0x06,
        WIFI_SECURITY_WPA_AES_PSK = 0x07,
        NOT_SUPPORTED = 0xFF,
    }
}

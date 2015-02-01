using System;
using System.Text;
using SmartLab.MuRata.Type;

namespace SmartLab.MuRata.Response
{
    public class WIFIStatusResponse : Payload
    {
        public WIFIStatusResponse(Payload payload)
            : base(payload)
        { }

        public WIFIStatusCode GetWiFiStatusCode() { return (WIFIStatusCode)this.GetData()[2]; }

        /// <summary>
        /// Present only if WiFi Status code is not WIFI_OFF.
        /// </summary>
        /// <returns></returns>
        public byte[] GetMACAddress()
        {
            if (this.GetWiFiStatusCode() == WIFIStatusCode.WIFI_OFF)
                return null;

            byte[] value = new byte[6];
            Array.Copy(this.GetData(), 3, value, 0, 6);
            return value;
        }

        /// <summary>
        /// Present only if WiFi Status code is STA_JOINED or AP_STARTED.
        /// </summary>
        /// <returns></returns>
        public string GetSSID()
        {
            WIFIStatusCode code = GetWiFiStatusCode();
            if (code == WIFIStatusCode.STA_JOINED || code == WIFIStatusCode.AP_STARTED)
            {
                byte[] value = new byte[this.GetPosition() - 10];
                Array.Copy(this.GetData(), 9, value, 0, value.Length);
                return UTF8Encoding.UTF8.GetString(value);
            }

            return null;
        }
    }
}
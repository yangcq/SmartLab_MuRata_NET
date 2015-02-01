using System;
using System.Text;

namespace SmartLab.MuRata.Type
{
    public abstract class WIFIInfo
    {
        private byte channel;
        private string ssid;
        private byte[] _ssid;
        private SecurityMode mode;

        public byte[] GetSSID() { return _ssid; }

        public string GetSSIDasString() { return ssid; }

        public SecurityMode GetSecurityMode() { return mode; }

        public byte GetChannel() { return channel; }
        
        public WIFIInfo() { }

        public WIFIInfo(string SSID, SecurityMode securityMode)
        {
            this.SetSSID(SSID).SetSecurityMode(securityMode);
        }

        public WIFIInfo SetSSID(string SSID)
        {
            this._ssid = UTF8Encoding.UTF8.GetBytes(SSID);

            if (this._ssid.Length >= 33)
                throw new ArgumentOutOfRangeException("UINT8 SSID [Up to 32 octets]");

            this.ssid = SSID;

            return this;
        }

        public WIFIInfo SetSecurityMode(SecurityMode securityMode) { this.mode = securityMode; return this; }

        public WIFIInfo SetChannel(byte channel) { this.channel = channel; return this; }

        public override string ToString() { return this.ssid; }
    }
}
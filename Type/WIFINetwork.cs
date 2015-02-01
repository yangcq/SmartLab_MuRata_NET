using System;
using System.Text;

namespace SmartLab.MuRata.Type
{
    public class WIFINetwork : WIFIInfo
    {
        private byte keylength;
        private byte[] key;
        private byte[] BSSID;

        public byte GetSecurityLength() { return keylength; }

        public byte[] GetSecurityKey() { return key; }

        public byte[] GetBSSID() { return BSSID; }
        
        public WIFINetwork() { }

        public WIFINetwork(string SSID, SecurityMode securityMode, string securityKey = null)
            : base(SSID, securityMode)
        {
            SetSecurityKey(securityKey);
        }

        public WIFINetwork SetSecurityKey(string SecurityKey)
        {
            if (SecurityKey != null)
            {
                this.key = UTF8Encoding.UTF8.GetBytes(SecurityKey);
                this.keylength = (byte)this.key.Length;

                if (this.keylength >= 64)
                    throw new ArgumentOutOfRangeException("UINT8 Security key length (0-64)");
            }
            else keylength = 0;
            return this;
        }

        public WIFINetwork SetBSSID(byte[] BSSID)
        {
            if (BSSID.Length != 6)
                throw new ArgumentOutOfRangeException("BSSID must be 6 bytes");

            this.BSSID = BSSID;
            return this;
        }

        public new WIFINetwork SetSSID(string SSID)
        {
            base.SetSSID(SSID);
            return this;
        }

        public new WIFINetwork SetSecurityMode(SecurityMode securityMode)
        {
            base.SetSecurityMode(securityMode);
            return this;
        }

        public new WIFINetwork SetChannel(byte channel)
        {
            base.SetChannel(channel);
            return this;
        }
    }
}

using System;
using SmartLab.MuRata.Config;

namespace SmartLab.MuRata.Type
{
    public class IPAddress : IConfig
    {
        public static readonly IPAddress ANY = new IPAddress(new byte[] { 0x00, 0x00, 0x00, 0x00 });
        public static readonly IPAddress Loopback = new IPAddress(new byte[] { 0x7F, 0x00, 0x00, 0x01 });

        private byte[] address = new byte[4];

        public IPAddress(string ip)
        {
            string[] ips = ip.Split('.');
            if (ips.Length != 4)
                throw new ArgumentException("IP : X.X.X.X");

            address[0] = byte.Parse(ips[0]);
            address[1] = byte.Parse(ips[1]);
            address[2] = byte.Parse(ips[2]);
            address[3] = byte.Parse(ips[3]);
        }

        public IPAddress(byte[] data, int offset = 0) { Array.Copy(data, offset, this.address, 0, 4); }

        public byte[] GetValue() { return this.address; }

        public override string ToString() { return address[0].ToString() + "." + address[1].ToString() + "." + address[2].ToString() + "." + address[3].ToString(); }
    }
}
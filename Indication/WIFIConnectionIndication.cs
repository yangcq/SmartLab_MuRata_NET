using System;
using System.Text;
using SmartLab.MuRata.ErrorCode;
using SmartLab.MuRata.Type;

namespace SmartLab.MuRata.Indication
{
    public class WIFIConnectionIndication : Payload
    {
        public WIFIConnectionIndication(Payload payload)
            : base(payload)
        { }

        public WIFIInterface GetInterface() { return (WIFIInterface)this.GetData()[2]; }

        public WIFICode GetStatus() { return (WIFICode)this.GetData()[3]; }

        public string GetSSID()
        {
            int _position = 4;
            int start = 4;

            while (this.GetData()[_position++] != 0x00) { }

            byte[] _string = new byte[_position - start - 1];
            Array.Copy(this.GetData(), start, _string, 0, _string.Length);
            return UTF8Encoding.UTF8.GetString(_string);
        }
    }
}
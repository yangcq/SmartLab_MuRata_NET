using System;
using System.Text;
using SmartLab.MuRata.ErrorCode;

namespace SmartLab.MuRata.Response
{
    public class VersionInfoResponse : Payload
    {
        public VersionInfoResponse(Payload payload)
            : base(payload)
        { }

        public CMDCode GetStatus() { return (CMDCode)this.GetData()[2]; }

        public byte GetVersionStringLength() { return this.GetData()[3]; }

        public String GetVsersionString()
        {
            int size = this.GetVersionStringLength();
            byte[] _bytes = new byte[size];
            Array.Copy(this.GetData(), 4, _bytes, 0, size);
            return UTF8Encoding.UTF8.GetString(_bytes);
        }
    }
}
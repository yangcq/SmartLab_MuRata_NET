using SmartLab.MuRata.ErrorCode;

namespace SmartLab.MuRata.Response
{
    public class CreateSocketResponse : Payload
    {
        public CreateSocketResponse(Payload payload)
            : base(payload)
        { }

        public SNICCode GetStatus() { return (SNICCode)this.GetData()[2]; }

        public byte GetSocketID() { return this.GetData()[3]; }
    }
}

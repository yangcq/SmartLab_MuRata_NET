using SmartLab.MuRata.ErrorCode;

namespace SmartLab.MuRata.Indication
{
    /// <summary>
    /// This event describes the status of a network connection (identified by a socket)
    /// </summary>
    public class TCPStatusIndication : Payload
    {
        private byte socketID;

        public TCPStatusIndication(Payload payload)
            : base(payload)
        {
            this.socketID = this.GetData()[3];
        }

        public SNICCode GetStatus() { return (SNICCode)this.GetData()[2]; }

        public byte GetSocketID() { return socketID; }
    }
}
using SmartLab.MuRata.Type;

namespace SmartLab.MuRata.Indication
{
    public class PowerUpIndication : Payload
    {
        public PowerUpIndication(Payload payload) : base(payload) 
        { }

        public ResetCode GetResetCode() { return (ResetCode)((this.GetData()[2] << 8) | this.GetData()[3]); }
    }
}

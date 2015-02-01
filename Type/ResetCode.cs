
namespace SmartLab.MuRata.Type
{
    public enum ResetCode
    {
        Window_Watchdog_Reset = 0x4000,
        Independent_Watchdog_Reset = 0x2000,
        Software_Reset = 0x1000,
        POR_PDR_Reset = 0x0800,
        Pin_Reset = 0x0400,
    }
}
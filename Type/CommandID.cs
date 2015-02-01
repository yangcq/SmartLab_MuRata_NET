
namespace SmartLab.MuRata.Type
{
    public enum CommandID
    {
        NAK = 0x00,

        /// <summary>
        /// General Management
        /// </summary>
        CMD_ID_GEN = 0x01,

        /// <summary>
        /// WIFI API
        /// </summary>
        CMD_ID_WIFI = 0x50,

        /// <summary>
        /// SNIC API
        /// </summary>
        CMD_ID_SNIC = 0x70,

        ACK = 0x7F,
    }
}

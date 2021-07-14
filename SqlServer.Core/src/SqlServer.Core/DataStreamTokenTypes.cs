using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SqlServer.Core
{
    internal enum DataStreamTokenTypes : byte
    {
        ALTMETADATA_TOKEN = 0x88,
        ALTROW_TOKEN = 0xD3,
        COLMETADATA_TOKEN = 0x81,
        COLINFO_TOKEN = 0xA5,
        DONE_TOKEN = 0xFD,
        DONEPROC_TOKEN = 0xFE,
        DONEINPROC_TOKEN = 0xFF,
        ENVCHANGE_TOKEN = 0xE3,
        ERROR_TOKEN = 0xAA,
        FEATUREEXTACK_TOKEN = 0xAE,
        FEDAUTHINFO_TOKEN = 0xEE,
        INFO_TOKEN = 0xAB,
        LOGINACK_TOKEN = 0xAD,
        NBCROW_TOKEN = 0xD2,
        OFFSET_TOKEN = 0x78,
        ORDER_TOKEN = 0xA9,
        RETURNSTATUS_TOKEN = 0x79,
        RETURNVALUE_TOKEN = 0xAC,
        ROW_TOKEN = 0xD1,
        SESSIONSTATE_TOKEN = 0xE4,
        SSPI_TOKEN = 0xED,
        TABNAME_TOKEN = 0xA4,
    }
}

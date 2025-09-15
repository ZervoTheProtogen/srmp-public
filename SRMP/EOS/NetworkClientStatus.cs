using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRMultiplayer.Enums
{
    public enum NetworkClientStatus
    {
        None,
        Connecting,
        Authenticating,
        Connected,
        Disconnecting,
        Disconnected
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace ContosoIOT.Common
{
    public class RemoteStartCommand
    {
        public DateTime cloudUTCDateTime { get; set; }
        public int messageId { get; set; }
    }
}

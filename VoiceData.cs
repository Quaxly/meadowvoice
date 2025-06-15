using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace meadowvoice
{
    internal class VoiceData
    {
        public ulong PacketId;
        public int Length;
        public byte[] Data;
        public float[] DecodedData = null;
        public bool IsSilence = false;
    }
}

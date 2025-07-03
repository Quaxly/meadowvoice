using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace meadowvoice
{
    internal static class Enums
    {
        public static SoundID MEADOWVOICE_MUTE { get; } = new(nameof(MEADOWVOICE_MUTE), true);
        public static SoundID MEADOWVOICE_UNMUTE { get; } = new(nameof(MEADOWVOICE_UNMUTE), true);
        public static SoundID MEADOWVOICE_OTHERMUTE { get; } = new(nameof(MEADOWVOICE_OTHERMUTE), true);
        public static void Init()
        {
            _ = MEADOWVOICE_MUTE;
            _ = MEADOWVOICE_UNMUTE;
            _ = MEADOWVOICE_OTHERMUTE;
        }
    }
}

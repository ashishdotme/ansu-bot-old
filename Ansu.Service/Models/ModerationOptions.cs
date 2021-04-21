using System;
using System.Collections.Generic;

namespace Ansu.Service.Models
{
    public class ModerationOptions
    {
        internal ModerationOptions()
            => Blacklist = new List<string>();

        public ulong MassEmojiThreshold { get; set; }

        public List<string> Blacklist { get; set; }
    }
}

using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace meadowvoice
{
    internal class VoiceChatData : OnlineEntity.EntityData
    {
        public string publicKey = "";

        public VoiceChatData() { }

        public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
        {
            return new VoiceChatState(this);
        }

        public class VoiceChatState : EntityDataState
        {
            [OnlineField(group = "voiceChatData")]
            public string publicKey;
            public VoiceChatState() { }
            public VoiceChatState(VoiceChatData onlineEntity)
            {
                publicKey = onlineEntity.publicKey;
            }

            public override Type GetDataType() => typeof(VoiceChatData);

            public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity onlineEntity)
            {
                var voiceChatData = (VoiceChatData)data;

                voiceChatData.publicKey = publicKey;
            }
        }
    }
}

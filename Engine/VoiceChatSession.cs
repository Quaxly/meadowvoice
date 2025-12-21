using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using RainMeadow;
using UnityEngine.PlayerLoop;

namespace meadowvoice
{
    
    partial class VoiceChatSession : OnlineResource
    {
        public const string staticID = "@voice";
        public List<OnlinePlayer> succession;
        public static VoiceChatSession instance = null;
        public VoiceChatSession() : base(null)
        {
            if (instance != null) throw new InvalidOperationException("We already have a voicechat instance.");
            instance = this;
            isNeeded = true;
            isAvailable = false;
            succession = new List<OnlinePlayer>() { OnlineManager.mePlayer };
            VoiceChatHandShakeWithLobby();
        }
        public override string Id() => staticID;
        public override ushort ShortId() => throw new NotImplementedException();

        public override OnlineResource SubresourceFromShortId(ushort shortId) => throw new NotImplementedException();

        protected override void ActivateImpl()
        {
            
        }

        protected override void AvailableImpl()
        {
            
        }

        protected override void DeactivateImpl() => throw new InvalidOperationException($"you can't deactivate {staticID}");

        public void Update()
        {
            if (isActive)
            {
                Tick(OnlineManager.mePlayer.tick);
            }
            else if (isAvailable)
            {
                Activate();
            }
            else if (isRequesting)
            {
                CheckEveryoneAcknoledged();
            }
        }

        protected override void UnavailableImpl()
        {

        }


        class VoiceChatSessionState : ResourceState
        {
            [OnlineField(nullable: true)]
            public RainMeadow.Generics.DynamicOrderedPlayerIDs succession;


            public VoiceChatSessionState() : base() { }
            public VoiceChatSessionState(VoiceChatSession voiceChat, uint ts) : base(voiceChat, ts)
            {
                succession = new RainMeadow.Generics.DynamicOrderedPlayerIDs(voiceChat.succession.Select(x => x.id).ToList());
            }

            public override void ReadTo(OnlineResource resource)
            {
                base.ReadTo(resource);
                if (resource is VoiceChatSession session)
                {
                    if (succession != null)
                    {
                        session.succession = succession.list.Select(id => OnlineManager.players.Where(p => p.id == id).FirstOrDefault()).ToList();
                        session.UpdateParticipants(session.succession);
                    }
                }
            }
        }

        protected override ResourceState MakeState(uint ts) => new VoiceChatSessionState(this, ts);
    }
}

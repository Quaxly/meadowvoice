
using System;
using System.Collections.Generic;
using System.Linq;
using RainMeadow;

namespace meadowvoice
{
    partial class VoiceChatSession : OnlineResource
    {
        List<RPCEvent> requests = new(); 
        public void VoiceChatHandShakeWithLobby()
        {
            RainMeadow.RainMeadow.DebugMe();
            if (isPending) throw new InvalidOperationException("pending");
            if (isAvailable) throw new InvalidOperationException("available");
            if (OnlineManager.players.Count == 1 && OnlineManager.players[0].isMe)
            {
                NewOwner(OnlineManager.mePlayer);
                return;
            }


            isRequesting = true;

            ClearIncommingBuffers();
            foreach (OnlinePlayer player in OnlineManager.players)
            {
                if (player.isMe) continue;
                requests.Add(player.InvokeRPC(VoiceChatHandShake).Then(ResolveVoiceChatHandShake));
            }
        }

        public void CheckEveryoneAcknoledged()
        {
            if (!isRequesting || isAvailable)
            {
                requests.Clear();
                return;
            }

            bool everyoneIgnoredEvent = true;
            foreach (RPCEvent request in requests.ToList())
            {
                if (EventMath.IsNewerOrEqual(request.to.lastAckFromRemote, request.eventId))
                {
                    // they ignored our event.
                    requests.Remove(request);
                    continue;
                }

                everyoneIgnoredEvent = false;
                break;
            }


            if (everyoneIgnoredEvent)
            {
                RainMeadow.RainMeadow.Debug("Claiming ownership because nobody acknoledged voicechat.");
                // nobody in the lobby knows about voice chat. we'll just own the voice session.
                // TODO Log this
                NewOwner(OnlineManager.mePlayer);
            }
        }

        public void HandleDisconnect(OnlinePlayer player)
        {
            if (!isAvailable) return;
            if (owner == player)
            {
                succession.RemoveAll(p => p.hasLeft);
                NewOwner(succession.FirstOrDefault());
                if (owner == null) throw new InvalidProgrammerException("no players to inherit the lobby?");
            }

            OnPlayerDisconnect(player);
        }

        protected override void ParticipantLeftImpl(OnlinePlayer player)
        {
            RainMeadow.RainMeadow.Debug($"{player} left voice chat");
            base.NewParticipantImpl(player);
            this.succession.Remove(player);
        }

        protected override void NewParticipantImpl(OnlinePlayer player)
        {
            RainMeadow.RainMeadow.Debug($"{player} joined voice chat");
            base.NewParticipantImpl(player);
            this.succession.Add(player);
        }



        [SoftRPCMethod]
        public void VoiceChatHandShake(RPCEvent request) => Requested(request); // A soft version of requesting the resource.

        public void ResolveVoiceChatHandShake(GenericResult requestResult)
        {
            if (requestResult is GenericResult.Ok)
            {
                RainMeadow.RainMeadow.Debug($"{requestResult.from} responded to our hand shake for Voice Chat.");
                isRequesting = false;
                requests.Clear();

                NewOwner(requestResult.from);
                if (!isAvailable) 
                {
                    WaitingForState();
                    if (isOwner)
                    {
                        Available();
                    }
                }
            }
        }
    }

}

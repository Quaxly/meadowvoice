using MeadowVoice;
using RainMeadow;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeadowVoice
{
    internal static class VoiceRPC
    {
        [SoftRPCMethod]
        public static void SendVoice(RPCEvent rpcEvent, byte[] voiceDataBuffer, uint bytesRead)
        {
            RainMeadow.RainMeadow.Debug("Recieved voice: " + rpcEvent.from.id.name);
            if (rpcEvent.from.isMe) return;
            foreach (var avatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
            {
                if (avatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue;
                if (avatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac && ac.realizedCreature is not null)
                {
                    if (opo.owner.inLobbyId == rpcEvent.from.inLobbyId)
                    {
                        if (VoiceEmitter.map.TryGetValue(ac.realizedCreature, out var emitter))
                        {
                            emitter.RecieveAudio(voiceDataBuffer, bytesRead, rpcEvent.eventId);
                        }
                    }
                }
            }
        }
    }
}

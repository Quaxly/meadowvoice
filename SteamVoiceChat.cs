using meadowvoice;
using RainMeadow;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace meadowvoice
{
    internal class SteamVoiceChat : IUseCustomPackets
    {
        public static SteamVoiceChat myVoiceChat;
        public static AudioSource playbackClip;

        public OnlinePlayer owner;
        public OnlineCreature ownerEntity;

        public bool recording;

        public const uint sampleRate = 11025;
        public const uint bufferSize = 22050;

        public RPCEvent lastEvent;
        public uint index;

        public ushort debugIndex;

        public bool Active => true;

        public SteamVoiceChat(OnlinePlayer owner, OnlineCreature ownerEntity)
        {
            this.owner = owner;
            this.ownerEntity = ownerEntity;

            CustomManager.Subscribe("meadowvoice", this);
        }

        public void ChangeOwningEntity(OnlineCreature newOwner)
        {
            this.ownerEntity = newOwner;
        }

        public void BeginStream()
        {
            if (!recording)
            {
                SteamUser.StartVoiceRecording();
                recording = true;
            }
        }
        public void EndStream()
        {
            if (recording)
            {
                SteamUser.StopVoiceRecording();
                recording = false;
            }
        }

        public void VoiceUpdate()
        {
            if (OnlineManager.lobby is null || OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                return;
            }
            if (recording)
            {
                uint bytesAvailableCompressed;
                if (SteamUser.GetAvailableVoice(out bytesAvailableCompressed) == EVoiceResult.k_EVoiceResultOK)
                {
                    byte[] voiceDataBuffer = new byte[bytesAvailableCompressed];
                    if (SteamUser.GetVoice(true, voiceDataBuffer, bytesAvailableCompressed, out uint bytesWritten) == EVoiceResult.k_EVoiceResultOK && bytesWritten == bytesAvailableCompressed && ownerEntity != null)
                    {
                        foreach (var op in ownerEntity.roomSession.participants)
                        {
                            if (!op.isMe)
                            {
                                SendVoice(op, voiceDataBuffer, (ushort)bytesWritten);
                            }
                            else
                            {
                                if (SteamVoiceDebug.PLAYBACK && PlaybackDebugger.map.TryGetValue(ownerEntity.realizedCreature, out var pbd))
                                {
                                    pbd.RecieveAudio(voiceDataBuffer, (uint)voiceDataBuffer.Length);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SendVoice(OnlinePlayer op, byte[] voiceDataBuffer, ushort bytesWritten)
        {
            CustomManager.SendCustomData(op, "meadowvoice", voiceDataBuffer, bytesWritten, NetIO.SendType.Unreliable);
            //OnlineManager.netIO.SendP2P(op, new CustomPacket("meadowvoice", voiceDataBuffer, (ushort)voiceDataBuffer.Length), NetIO.SendType.Unreliable);
        }

        public void ProcessPacket(OnlinePlayer fromPlayer, CustomPacket packet)
        {
            if (packet.key != "meadowvoice")
            {
                return;
            }
            foreach (var avatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
            {
                if (avatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue;
                if (avatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac && ac.realizedCreature is not null)
                {
                    if (opo.owner.inLobbyId == fromPlayer.inLobbyId)
                    {
                        if (VoiceEmitter.map.TryGetValue(ac.realizedCreature, out var emitter))
                        {
                            emitter.RecieveAudio(packet.data, (uint)packet.data.Length);
                        }
                    }
                }
            }
        }
    }
}

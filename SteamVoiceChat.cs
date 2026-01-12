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
    /// <summary>
    /// Processes and distributes incoming and outgoing voice data for Steam Clients
    /// </summary>
    internal class SteamVoiceChat : IUseCustomPackets
    {
        public static SteamVoiceChat myVoiceChat;
        public static AudioSource playbackClip;
        public static List<MeadowPlayerId> mutedPlayers = new();

        public OnlinePlayer owner;
        public OnlineCreature ownerEntity;
        public RainWorldGame game;

        public bool recording;
        public bool hasVoice;
        private int voiceTimer;

        //public const uint sampleRate = 11025;
        public const uint sampleRate = 48000;
        public const uint bufferSize = 22050;

        public bool Active => true;

        public SteamVoiceChat(OnlinePlayer owner, OnlineCreature ownerEntity)
        {
            this.owner = owner;
            this.ownerEntity = ownerEntity;
            this.game = ownerEntity.abstractCreature.world.game;

            CustomManager.Subscribe("meadowvoice", this);
        }

        public void ChangeOwningEntity(OnlineCreature newOwner)
        {
            this.ownerEntity = newOwner;
        }

        public void Mute(MeadowPlayerId id)
        {
            mutedPlayers.Add(id);
            game.cameras[0].virtualMicrophone.PlaySound(Enums.MEADOWVOICE_OTHERMUTE, 0f, 0.35f, 1f, 1);
        }
        public void Unmute(MeadowPlayerId id)
        {
            mutedPlayers.RemoveAll(m => m == id);
            game.cameras[0].virtualMicrophone.PlaySound(Enums.MEADOWVOICE_UNMUTE, 0f, 0.35f, 1f, 1);
        }

        public void BeginStream()
        {
            if (!recording)
            {
                SteamUser.StartVoiceRecording();
                game.cameras[0].virtualMicrophone.PlaySound(Enums.MEADOWVOICE_UNMUTE, 0f, 0.35f, 1f, 1);
                recording = true;
            }
        }
        public void EndStream()
        {
            if (recording)
            {
                SteamUser.StopVoiceRecording();
                game.cameras[0].virtualMicrophone.PlaySound(Enums.MEADOWVOICE_MUTE, 0f, 0.35f, 1f, 1);
                recording = false;
            }
        }

        public void VoiceUpdate()
        {
            if (OnlineManager.lobby is null || OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                return;
            }
            if (!recording)
            {
                return;
            }
            // Dear Valve, pweety pwease with a chewwy on towp give us a way to select our audio input in the steam api
            uint bytesAvailableCompressed;
            if (SteamUser.GetAvailableVoice(out bytesAvailableCompressed) == EVoiceResult.k_EVoiceResultOK)
            {
                hasVoice = true;
                voiceTimer = 0;
                byte[] voiceDataBuffer = new byte[bytesAvailableCompressed];
                if (SteamUser.GetVoice(true, voiceDataBuffer, bytesAvailableCompressed, out uint bytesWritten) == EVoiceResult.k_EVoiceResultOK 
                    && bytesWritten == bytesAvailableCompressed 
                    && ownerEntity != null && ownerEntity.currentlyJoinedResource is RoomSession roomSession)
                {
                    foreach (var op in roomSession.participants) // We only want to send voice data to people in the same room as us
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
            else
            {
                if (!hasVoice) return;
                voiceTimer++;
                if (voiceTimer > 30)
                {
                    hasVoice = false;
                }
            }
        }

        public void SendVoice(OnlinePlayer op, byte[] voiceDataBuffer, ushort bytesWritten)
        {
            try
            {
                // Only send voice data to VoiceChatSession participants to avoid wasting precious bandwidth
                if (VoiceChatSession.instance.participants.Contains(op))
                {
                    CustomManager.SendCustomData(op, "meadowvoice", voiceDataBuffer, bytesWritten, NetIO.SendType.Unreliable);
                }
            }
            catch (Exception ex)
            {
                RainMeadow.RainMeadow.Error($"There was an error encoding voice data for {op.id.name}");
                RainMeadow.RainMeadow.Error(ex);
            }
        }

        public void ProcessPacket(OnlinePlayer fromPlayer, CustomPacket packet)
        {
            if (packet.key != "meadowvoice") return;
            if (mutedPlayers.Contains(fromPlayer.id)) return;
            try
            {
                var emitter = VoiceEmitter.FromOnlinePlayer(fromPlayer);
                if (emitter == null)
                {
                    RainMeadow.RainMeadow.Error($"Recieved Voice Packet from {fromPlayer.id.name} who does not have a voice emitter.");
                    return;
                }
                emitter.RecieveAudio(packet.data, (uint)packet.size);
            }
            catch (Exception ex)
            {
                RainMeadow.RainMeadow.Error($"There was an error decoding voice data from {fromPlayer.id.name}");
                RainMeadow.RainMeadow.Error(ex);
            }
        }
    }
}
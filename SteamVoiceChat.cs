using meadowvoice;
using RainMeadow;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MeadowVoice
{
    internal class SteamVoiceChat
    {
        public static SteamVoiceChat myVoiceChat;

        public OnlinePlayer owner;
        public OnlineCreature ownerEntity;

        public bool recording;

        public const uint sampleRate = 11025;
        public const uint bufferSize = 22050;

        public RPCEvent lastEvent;
        public uint index;

        public SteamVoiceChat(OnlinePlayer owner, OnlineCreature ownerEntity)
        {
            this.owner = owner;
            this.ownerEntity = ownerEntity;
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
                    if (SteamUser.GetVoice(true, voiceDataBuffer, bytesAvailableCompressed, out uint bytesWritten) == EVoiceResult.k_EVoiceResultOK && bytesWritten == bytesAvailableCompressed)
                    {
                        foreach (var op in ownerEntity.roomSession.participants)
                        {
                            if (!op.isMe)
                            {
                                op.InvokeRPC(VoiceRPC.SendVoice, voiceDataBuffer, (uint)voiceDataBuffer.Length);
                            }
                        }
                    }
                }
            }
        }

        public void TestTone()
        {
            var tone = GenerateSineWave(440, 500);
            var testBuffer = new byte[tone.Length * 2];
            Buffer.BlockCopy(tone, 0, testBuffer, 0, testBuffer.Length);
            foreach (var op in ownerEntity.roomSession.participants)
            {
                if (!op.isMe)
                {
                    op.InvokeRPC(VoiceRPC.SendVoice, testBuffer, (uint)testBuffer.Length);
                }
            }
        }

        private short[] GenerateSineWave(int frequencyHz, int durationMs, int sampleRate = 11025)
        {
            int sampleCount = (durationMs * sampleRate) / 1000;
            short[] buffer = new short[sampleCount];

            double amplitude = 32760; // Max amplitude for 16-bit audio
            double increment = (2 * Mathf.PI * frequencyHz) / sampleRate;

            for (int i = 0; i < sampleCount; i++)
            {
                buffer[i] = (short)(amplitude * Mathf.Sin((float)(i * increment)));
            }

            return buffer;
        }
    }
}

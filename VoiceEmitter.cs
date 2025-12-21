using RainMeadow;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace meadowvoice
{
    /// <summary>
    /// An object that decodes and plays voice data in it's room.
    /// </summary>
    internal class VoiceEmitter
    {
        public static ConditionalWeakTable<Creature, VoiceEmitter> map = new();

        public Creature owner;
        public OnlineCreature ownerEntity;

        public RainWorldGame game;
        public Room room;

        public SortedList<ulong, float[]> streamingReadQueue = new();

        public ChunkSoundEmitter controller;
        public int voiceTimer;

        public float Volume
        {
            get
            {
                return this.volume;
            }
            set
            {
                this.volume = value;
                if (this.controller != null)
                {
                    this.controller.volume = value;
                }
            }
        }
        public float volume = 1.0f;
        public float pitch = 1.0f;

        private bool buffering = true;
        private float[] currentStream;
        private ulong currentIndex;
        private ulong lastIndex;
        private int streamIndex;
        private int streamPosition;

        private ulong packetId;
        public VoiceEmitter(Creature owner, OnlineCreature ownerEntity)
        {
            this.owner = owner;
            this.ownerEntity = ownerEntity;

            this.game = owner.abstractCreature.world.game;

            map.Add(owner, this);

            if (SteamVoiceDebug.DEBUG)
            {
                SteamVoiceDebug.recievingInfo.RemoveAll(k => k.StartsWith(ownerEntity.owner.id.name));
                SteamVoiceDebug.recievingInfo.Add(ownerEntity.owner.id.name + " - Awaiting");
            }
        }
        public void ChangeOwningCreature(Creature newCreature)
        {
            this.owner = newCreature;
            this.ownerEntity = newCreature.abstractCreature.GetOnlineCreature();
            map.Add(this.owner, this);
            Reset();
        }
        public void Destroy()
        {
            if (map.TryGetValue(this.owner, out var self))
            {
                map.Remove(this.owner);
            }
            controller.Destroy();
            controller.currentSoundObject.Stop();
            controller = null;
        }

        public void Reset()
        {
            streamingReadQueue.Clear();
            streamPosition = 0;
        }
        public void Update()
        {
            if (this.owner.room != null)
            {
                this.room = this.owner.room;
            }
            if (controller != null)
            {
                if (this.owner != null)
                {
                    controller.chunk = this.owner.mainBodyChunk;
                }
                if (controller.room != this.room || controller.slatedForDeletetion)
                {
                    controller.Destroy();
                    controller.currentSoundObject.Stop();
                    controller = null;
                    Reset();
                }
            }
            if (buffering)
            {
                buffering = this.streamingReadQueue.Count < ModOptions.packetBuffer.Value;
            }
            else
            {
                if (!this.owner.dead && this.controller is null)
                {
                    RainMeadow.RainMeadow.Debug("Processing next sample");
                    VirtualMicrophone mic = game.cameras[0].virtualMicrophone;
                    for (int i = 0; i < game.cameras.Length; i++)
                    {
                        if (game.cameras[i].room == owner.room)
                        {
                            mic = game.cameras[i].virtualMicrophone;
                        }
                    }

                    controller = new ChunkSoundEmitter(owner.mainBodyChunk, this.Volume, this.pitch);
                    controller.requireActiveUpkeep = false;
                    this.room.AddObject(controller);
                    SoundLoader.SoundData soundData = mic.GetSoundData(SoundID.Slugcat_Stash_Spear_On_Back, -1);
                    controller.currentSoundObject = new VirtualMicrophone.ObjectSound(mic, soundData, true, controller, this.Volume, this.pitch, false);
                    mic.soundObjects.Add(controller.currentSoundObject);
                    controller.currentSoundObject.audioSource.clip = AudioClip.Create(ownerEntity.owner.inLobbyId + " voice", (int)(SteamVoiceChat.sampleRate * 10), 1, (int)SteamVoiceChat.sampleRate, true, OnAudioRead, OnAudioSetPosition);
                    controller.currentSoundObject.Play();
                }
                if (this.owner.dead)
                {
                    if (this.controller is not null)
                    {
                        this.controller.Destroy();
                        this.controller.currentSoundObject.Stop();
                        this.controller = null;
                    }
                }
            }
            if (this.voiceTimer < 30)
            {
                this.voiceTimer++;
            }
        }

        private void OnAudioSetPosition(int newPosition)
        {
            this.streamPosition = newPosition;
        }

        private void OnAudioRead(float[] data)
        {
            if (buffering)
            {
                int count = 0;
                while (count < data.Length)
                {
                    data[count] = 0;
                    streamPosition++;
                    count++;
                }
            }
            else
            {
                this.voiceTimer = 0;
                if (currentStream == null)
                {
                    Dequeue();
                }

                int count = 0;
                while (count < data.Length)
                {
                    float sample = 0;

                    if (currentStream != null)
                    {
                        sample = currentStream[streamIndex];
                        streamIndex++;

                        lastIndex = currentIndex;

                        if (streamIndex >= currentStream.Length)
                        {
                            Dequeue();
                        }
                    }
                    data[count] = sample;
                    streamPosition++;
                    count++;
                }
            }
        }

        private void Dequeue()
        {
            if (this.streamingReadQueue.Count > 0)
            {
                var pair = this.streamingReadQueue.First();
                var nextStream = pair.Value;
                if (nextStream != null)
                {
                    currentStream = nextStream;
                    currentIndex = pair.Key;
                    this.streamingReadQueue.Remove(pair.Key);
                }
            }
            else
            {
                currentStream = null;
                buffering = true;
            }
            streamIndex = 0;
        }

        public void RecieveAudio(byte[] voiceDataBuffer, uint bytesRead)
        {
            try
            {
                if (SteamVoiceDebug.DEBUG)
                {
                    RainMeadow.RainMeadow.Debug($"vdb leng: {voiceDataBuffer.Length}");
                    RainMeadow.RainMeadow.Debug($"byRd: {bytesRead}");
                }
                int bufferSize = (int)SteamVoiceChat.bufferSize;
                byte[] decompressedBuffer = null;
                var result = EVoiceResult.k_EVoiceResultNoData;
                uint bytesWritten = 0;
                do
                {
                    bufferSize *= 2;
                    decompressedBuffer = new byte[bufferSize];
                    result = SteamUser.DecompressVoice(voiceDataBuffer, (uint)voiceDataBuffer.Length, decompressedBuffer, (uint)decompressedBuffer.Length, out bytesWritten, SteamVoiceChat.sampleRate);
                } while (result == EVoiceResult.k_EVoiceResultBufferTooSmall);
                if (result != EVoiceResult.k_EVoiceResultOK || bytesWritten == 0)
                {
                    RainMeadow.RainMeadow.Debug($"Failed to decompress voice. Result: {result} Bytes Written: {bytesWritten}");
                    return;
                }

                var sampleData = new float[bytesWritten / 2];
                for (int i = 0; i < sampleData.Length; i++)
                {
                    float value = BitConverter.ToInt16(decompressedBuffer, i * 2);
                    sampleData[i] = value * 15f / short.MaxValue;
                }

                this.streamingReadQueue.Add(packetId, sampleData);
                packetId++;

                if (SteamVoiceDebug.DEBUG)
                {
                    for (int i = 0; i < SteamVoiceDebug.recievingInfo.Count; i++)
                    {
                        if (SteamVoiceDebug.recievingInfo[i].StartsWith(ownerEntity.owner.id.name))
                        {
                            SteamVoiceDebug.recievingInfo.RemoveAt(i);
                        }
                    }
                    SteamVoiceDebug.recievingInfo.Add(ownerEntity.owner.id.name + " - " + sampleData.Length);
                }
            } 
            catch (Exception e)
            {
                RainMeadow.RainMeadow.Debug("There was an error decoding voice data");
                RainMeadow.RainMeadow.Debug(e);
            }
        }

        public static VoiceEmitter FromOnlinePlayer(OnlinePlayer op)
        {
            foreach (var avatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
            {
                if (avatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue;
                if (avatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac && ac.realizedCreature is not null)
                {
                    if (opo.owner.inLobbyId == op.inLobbyId)
                    {
                        if (VoiceEmitter.map.TryGetValue(ac.realizedCreature, out var emitter))
                        {
                            return emitter;
                        }
                    }
                }
            }
            return null;
        }
    }
}

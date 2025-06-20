using RainMeadow;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace meadowvoice
{
    internal class VoiceEmitter
    {
        public static ConditionalWeakTable<Creature, VoiceEmitter> map = new();

        public Creature owner;
        public OnlineCreature ownerEntity;

        public RainWorldGame game;
        public Room room;

        public SortedList<ulong, float[]> streamingReadQueue = new();

        public VirtualMicrophone.SoundObject currentSoundObject;
        public ChunkSoundEmitter controller;

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
                SteamVoiceDebug.recievingInfo.Add(ownerEntity.owner.id.name + " - Awaiting");
            }
        }

        public void Update()
        {
            // prevent the pitch from getting set incorrectly
            if (this.currentSoundObject != null)
            {
                this.currentSoundObject.soundData.pitch = this.pitch;
                this.currentSoundObject.audioSource.pitch = this.pitch;
                if (this.owner != null)
                {
                    controller.chunk = this.owner.mainBodyChunk;
                }
            }
            if (this.owner.room != null)
            {
                this.room = this.owner.room;
            }
            if (this.controller != null && this.controller.room != this.room)
            {
                this.controller.Destroy();
            }
            if (this.controller != null && this.controller.slatedForDeletetion)
            {
                this.controller = null;
            }
            if (buffering)
            {
                buffering = this.streamingReadQueue.Count < ModOptions.packetBuffer.Value;
            }
            else
            {
                RainMeadow.RainMeadow.Debug("Processing next sample");
                VirtualMicrophone mic = game.cameras[0].virtualMicrophone;

                for (int i = 0; i < game.cameras.Length; i++)
                {
                    if (game.cameras[i].room == this.room)
                    {
                        mic = game.cameras[i].virtualMicrophone;
                        break;
                    }
                }
                if (!this.owner.dead && this.currentSoundObject == null || this.currentSoundObject.slatedForDeletion || this.controller == null)
                {
                    if (this.currentSoundObject != null)
                    {
                        this.currentSoundObject.Destroy();
                        this.currentSoundObject = null;
                    }
                    this.controller = new ChunkSoundEmitter(this.owner.mainBodyChunk, this.volume, this.pitch);
                    this.controller.requireActiveUpkeep = false;
                    this.room.AddObject(this.controller);
                    SoundLoader.SoundData soundData = mic.GetSoundData(SoundID.Slugcat_Stash_Spear_On_Back, -1);
                    this.currentSoundObject = new VirtualMicrophone.ObjectSound(mic, soundData, true, this.controller, this.volume, this.pitch, false);
                    this.currentSoundObject.audioSource.clip = AudioClip.Create(ownerEntity.owner.inLobbyId + " voice", (int)(SteamVoiceChat.sampleRate * 10), 1, (int)SteamVoiceChat.sampleRate, true, OnAudioRead, OnAudioSetPosition);
                    mic.soundObjects.Add(currentSoundObject);
                    this.currentSoundObject.Play();
                }
                if (this.owner.dead)
                {
                    this.currentSoundObject.Destroy();
                    this.controller.Destroy();
                    this.currentSoundObject = null;
                    this.controller = null;
                }
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
            } catch (Exception e)
            {
                RainMeadow.RainMeadow.Debug("There was an error decoding voice data");
                RainMeadow.RainMeadow.Debug(e);
            }
        }
    }
}

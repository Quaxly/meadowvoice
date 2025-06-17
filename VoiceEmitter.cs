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

        public SortedList<ulong, float[]> streamingReadQueue = new();

        public VirtualMicrophone.SoundObject currentSoundObject;

        public float volume = 1.0f;
        public float pitch = 1.0f;

        private int packetBuffer = 10;

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
            }
            if (buffering)
            {
                buffering = this.streamingReadQueue.Count < packetBuffer;
            }
            else
            {
                RainMeadow.RainMeadow.Debug("Processing next sample");
                VirtualMicrophone mic = game.cameras[0].virtualMicrophone;

                for (int i = 0; i < game.cameras.Length; i++)
                {
                    if (game.cameras[i].room == owner.room)
                    {
                        mic = game.cameras[i].virtualMicrophone;
                        break;
                    }
                }
                if (this.currentSoundObject == null || this.currentSoundObject.slatedForDeletion || (this.currentSoundObject as VirtualMicrophone.ObjectSound).controller == null || (this.currentSoundObject as VirtualMicrophone.ObjectSound).controller.slatedForDeletetion)
                {
                    if (this.currentSoundObject != null)
                    {
                        this.currentSoundObject.Destroy();
                        this.currentSoundObject = null;
                    }
                    ChunkSoundEmitter controller = new ChunkSoundEmitter(owner.mainBodyChunk, this.volume, this.pitch);
                    controller.requireActiveUpkeep = false;
                    SoundLoader.SoundData soundData = mic.GetSoundData(SoundID.Slugcat_Stash_Spear_On_Back, -1);
                    this.currentSoundObject = new VirtualMicrophone.ObjectSound(mic, soundData, true, controller, this.volume, this.pitch, false);
                    this.currentSoundObject.audioSource.clip = AudioClip.Create(ownerEntity.owner.inLobbyId + " voice", 11025 * 10, 1, (int)SteamVoiceChat.sampleRate, true, OnAudioRead, OnAudioSetPosition);
                    mic.soundObjects.Add(currentSoundObject);
                    currentSoundObject.Play();
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

                        // mark down the last packet that was played so that we have an idea of which incoming packets are obsolete.
                        lastIndex = currentIndex;

                        // if we've reached the end of this packet, grab the next one.
                        if (streamIndex >= currentStream.Length)
                        {
                            Dequeue();
                        }
                    }

                    // write the sample to the AudioClip & update it's position
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
                RainMeadow.RainMeadow.Debug($"vdb leng: {voiceDataBuffer.Length}");
                RainMeadow.RainMeadow.Debug($"byRd: {bytesRead}");
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

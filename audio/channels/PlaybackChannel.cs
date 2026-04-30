using RainMeadow;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace meadowvoice
{
    internal abstract class PlaybackChannel
    {
        public static List<AudioClip> clips = new();

        public OnlinePlayer owningPlayer;

        public long lastVoiceTime;

        public int stallTimer, voiceTimer;

        public Opus opus;

        public float[] playbackRing;
        public int bufferedSamples, bufferLength, writePos, readPos, jitterBuffer;
        public bool slatedfordeletion, buffering;
        public float vol = 1f, ptch = 1f;

        private readonly object playbackLock = new object();

        public PlaybackChannel(OnlinePlayer owningPlayer)
        {
            this.owningPlayer = owningPlayer;

            opus = AudioManager.Instance.opus;
            jitterBuffer = AudioManager.Instance.JitterBuffer * 1600;
            bufferLength = AudioManager.SamplesPerFrame * jitterBuffer;
            playbackRing = new float[bufferLength];
        }

        public virtual void RecieveAudio(byte[] opusData) 
        {
            if (slatedfordeletion) return;
            if (opusData == null || opusData.Length == 0) return;

            WritePCM(opus.DecodeToFloat(opusData));
            lastVoiceTime = DateTime.Now.Millisecond;

            voiceTimer = 0;
            stallTimer = 0;

            //RainMeadow.RainMeadow.Info($"Recieved voice: {opusData.Length}");
        }

        public virtual void Update()
        {
            if (slatedfordeletion) return;

            if (voiceTimer < 30) 
                voiceTimer++;
            if (stallTimer < 500)
                stallTimer++;
        }

        public virtual void Destroy()
        {
            slatedfordeletion = true;
        }

        public virtual void AudioUpdate()
        {
        }

        public virtual bool WritePCM(float[] pcmData)
        {
            if (pcmData == null || pcmData.Length == 0) return false;

            lock (playbackLock)
            {
                for(int i = 0; i < pcmData.Length; i++)
                {
                    playbackRing[writePos] = pcmData[i];
                    writePos = (writePos + 1) % playbackRing.Length;
                    if (bufferedSamples < playbackRing.Length) bufferedSamples++;
                    else readPos = (readPos + 1) % playbackRing.Length;
                }
            }

            return true;
        }

        public virtual void OnAudioRead(float[] data)
        {
            lock(playbackLock)
            {
                for(int i = 0; i < data.Length; i++)
                {
                    if (!buffering)
                    {
                        if (bufferedSamples > 0)
                        {
                            data[i] = playbackRing[readPos];
                            readPos = (readPos + 1) % playbackRing.Length;
                            bufferedSamples--;
                        }
                        else
                        {
                            data[i] = 0f;
                            buffering = true;
                        }
                    }
                    else
                    {
                        data[i] = 0f;
                        if (bufferedSamples >= jitterBuffer)
                        {
                            buffering = false;
                        }
                    }
                }
            }
        }

        public virtual void OnAudioSetPosition(int newPos) { }
    }
}

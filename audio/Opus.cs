using IL.MoreSlugcats;
using OpusSharp.Core;
using OpusSharp.Core.Extensions;
using System;
using UnityEngine;

namespace meadowvoice
{
    internal class Opus
    {
        public OpusEncoder Encoder;
        public OpusDecoder Decoder;

        public Opus()
        {
            // 48khz, mono

            Encoder = new(AudioManager.SampleRate, AudioManager.Channels, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
            //Encoder.SetMaxBandwidth(AudioManager.MaxBandwidth);

            Decoder = new(AudioManager.SampleRate, AudioManager.Channels);
        }

        /// <summary>
        /// PCM to Opus
        /// </summary>
        /// <param name="pcmData"></param>
        /// <returns></returns>
        public byte[] Encode(byte[] pcmData)
        {
            if (Encoder == null)
            {
                throw new InvalidOperationException($"Encoder is not initialized!");
            }
            byte[] frame = new byte[AudioManager.SamplesPerFrame * 2];
            try
            {
                var encodedBytes = Encoder.Encode(pcmData, AudioManager.SamplesPerFrame, frame, frame.Length);

                if (encodedBytes > 0)
                {
                    byte[] trimmedFrame = new byte[encodedBytes];
                    Buffer.BlockCopy(frame, 0, trimmedFrame, 0, encodedBytes);
                    return trimmedFrame;
                }

            }
            catch (Exception e)
            {
                RainMeadow.RainMeadow.Warn($"Error encoding: {e.Message}");
            }
            return Array.Empty<byte>();
        }

        public byte[] Encode(float[] pcmData)
        {
            if (Encoder == null)
            {
                throw new InvalidOperationException($"Encoder is not initialized!");
            }
            byte[] frame = new byte[AudioManager.SamplesPerFrame * 2];
            try
            {
                var encodedBytes = Encoder.Encode(pcmData, AudioManager.SamplesPerFrame, frame, frame.Length);

                if (encodedBytes > 0)
                {
                    byte[] trimmedFrame = new byte[encodedBytes];
                    Buffer.BlockCopy(frame, 0, trimmedFrame, 0, encodedBytes);
                    return trimmedFrame;
                }

            }
            catch (Exception e)
            {
                RainMeadow.RainMeadow.Warn($"Error encoding: {e.Message}");
            }
            return Array.Empty<byte>();
        }

        public byte[] Encode(short[] pcmData)
        {
            if (Encoder == null)
            {
                throw new InvalidOperationException($"Encoder is not initialized!");
            }
            byte[] frame = new byte[AudioManager.SamplesPerFrame * 2];
            try
            {
                var encodedBytes = Encoder.Encode(pcmData, AudioManager.SamplesPerFrame, frame, frame.Length);

                if (encodedBytes > 0)
                {
                    byte[] trimmedFrame = new byte[encodedBytes];
                    Buffer.BlockCopy(frame, 0, trimmedFrame, 0, encodedBytes);
                    return trimmedFrame;
                }

            }
            catch (Exception e)
            {
                RainMeadow.RainMeadow.Warn($"Error encoding: {e.Message}");
            }
            return Array.Empty<byte>();
        }

        /// <summary>
        /// Opus to PCM
        /// </summary>
        /// <param name="opusData"></param>
        /// <returns></returns>
        public byte[] Decode(byte[] opusData)
        {
            if (Decoder == null)
            {
                throw new InvalidOperationException($"Decoder is not initialized!");
            }
            try
            {
                short[] pcmData = new short[AudioManager.SamplesPerFrame];
                var decoded = Decoder.Decode(opusData, opusData.Length, pcmData, AudioManager.SamplesPerFrame, false);

                if (decoded > 0)
                {
                    byte[] pcmBytes = new byte[decoded * 2];
                    Buffer.BlockCopy(pcmData, 0, pcmBytes, 0, pcmBytes.Length);
                    return pcmBytes;
                }
            } 
            catch (Exception e)
            {
                RainMeadow.RainMeadow.Warn($"Error decoding: {e.Message}");
            }
            return Array.Empty<byte>();
        }

        public float[] DecodeToFloat(byte[] opusData)
        {
            if (Decoder == null)
            {
                throw new InvalidOperationException($"Decoder is not initialized!");
            }
            try
            {
                float[] pcmData = new float[AudioManager.SamplesPerFrame];
                var decoded = Decoder.Decode(opusData, opusData.Length, pcmData, AudioManager.SamplesPerFrame, false);

                if (decoded > 0)
                {
                    return pcmData;
                }
            }
            catch (Exception e)
            {
                RainMeadow.RainMeadow.Warn($"Error decoding: {e.Message}");
            }
            return Array.Empty<float>();
        }

        public byte[] FloatToBytes(float[] pcmData)
        {
            short[] pcm16 = new short[pcmData.Length];

            for(int i = 0; i < pcmData.Length; i++)
            {
                var sample = Mathf.Clamp(pcmData[i], -1.0f, 1.0f);
                pcm16[i] = (short)Mathf.Clamp(sample * 32768f, short.MinValue, short.MaxValue);
            }

            byte[] pcmBytes = new byte[pcm16.Length * 2];
            Buffer.BlockCopy(pcm16, 0, pcmBytes, 0, pcmBytes.Length);
            return pcmBytes;
        }

        public float[] BytesToFloat(byte[] pcmData)
        {
            int sampleCount = pcmData.Length / 2;
            float[] floats = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                short sample = (short)(pcmData[i * 2] | (pcmData[i * 2 + 1] << 8));
                floats[i] = sample / 32768f;
            }

            return floats;
        }
    }
}

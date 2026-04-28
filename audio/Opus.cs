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
            // 48khz, mono, VOIP
            Encoder = new(AudioManager.SampleRate, AudioManager.Channels, OpusPredefinedValues.OPUS_APPLICATION_VOIP);
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
    }
}

using RainMeadow;
using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace meadowvoice
{
    internal class MicrophoneRecorder
    {
        public event Action<byte[]> OnAudioReady;
        public Coroutine recordCoroutine;

        public AudioClip micClip;

        public string Device => ModOptions.selectedDevice.Value;

        public bool recording;

        public int micPosition = 0;

        public short[] pcm = new short[AudioManager.SamplesPerFrame];
        public float[] buffer = new float[AudioManager.SamplesPerFrame];
        public MicrophoneRecorder()
        {
        }

        public void BeginStream()
        {
            if (micClip != null)
            {
                MonoBehaviour.Destroy(micClip);
            }
            micClip = Microphone.Start(Device, true, 1, AudioManager.SampleRate);
            recording = true;

            recordCoroutine = Plugin.Instance.StartCoroutine(Record());
            RainMeadow.RainMeadow.Info($"Began recording on device: {Device}");
        }

        public void EndStream()
        {
            Microphone.End(Device);
            recording = false;

            if (recordCoroutine != null)
            {
                Plugin.Instance.StopCoroutine(recordCoroutine);
                recordCoroutine = null;
            }

            RainMeadow.RainMeadow.Info($"Stopped recording on device: {Device}");
        }

        private IEnumerator Record()
        {
            buffer = new float[AudioManager.SamplesPerFrame];
            pcm = new short[AudioManager.SamplesPerFrame];

            while (recording)
            {
                int pos = Microphone.GetPosition(Device);
                int samplesAvailable = pos < micPosition ? micClip.samples - micPosition + pos : pos - micPosition;

                while (samplesAvailable >= AudioManager.SamplesPerFrame)
                {
                    micClip.GetData(buffer, micPosition);
                    float sum = 0f;
                    for(int i = 0; i < AudioManager.SamplesPerFrame; i++)
                    {
                        float f = Mathf.Clamp(buffer[i], -1, 1f);
                        pcm[i] = (short)(f * short.MaxValue);
                        sum += f * f;
                    }

                    float rms = Mathf.Sqrt(sum / AudioManager.SamplesPerFrame);

                    var encoded = AudioManager.Instance.opus.Encode(pcm);

                    if (encoded.Length <= 0)
                    {
                        micPosition += AudioManager.SamplesPerFrame;
                        if (micPosition >= micClip.samples) micPosition -= micClip.samples;
                        samplesAvailable -= AudioManager.SamplesPerFrame;
                        continue;
                    }

                    OnAudioReady?.Invoke(encoded);

                    micPosition += AudioManager.SamplesPerFrame;
                    if (micPosition >= micClip.samples) micPosition -= micClip.samples;
                    samplesAvailable -= AudioManager.SamplesPerFrame;
                }

                yield return null;
            }
        }
    }
}

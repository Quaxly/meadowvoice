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

        public int streamHead;
        public int micPosition = 0;

        public float[] processBuffer = new float[AudioManager.SamplesPerFrame];
        public float[] micBuffer = new float[AudioManager.SampleRate];
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
        }

        private IEnumerator Record()
        {
            float[] buffer = new float[AudioManager.SamplesPerFrame];
            short[] pcm = new short[AudioManager.SamplesPerFrame];

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

        public int GetDataLength(int length, int head, int tail)
        {
            if (head < tail) return tail - head;
            return length - head + tail;
        }
    }
}

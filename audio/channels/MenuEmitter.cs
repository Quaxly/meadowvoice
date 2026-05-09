using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace meadowvoice
{
    internal class MenuEmitter : PlaybackChannel
    {
        public ProcessManager manager;
        public MenuMicrophone.MenuSoundObject soundA;
        public MenuEmitter(OnlinePlayer owningPlayer, ProcessManager manager) : base(owningPlayer)
        {
            this.manager = manager;
        }

        public override void Destroy()
        {
            base.Destroy();
            if (soundA != null)
            {
                soundA.Stop();
                soundA.Destroy();
                soundA = null;
            }
        }

        public override void Update()
        {
            if (soundA != null && soundA.slatedForDeletion)
            {
                soundA.Destroy();
                soundA = null;
            }

            if (soundA == null)
            {
                var soundData = new SoundLoader.SoundData(SoundID.Mushroom_Trip_LOOP, 0, 0.5f, 1f, 0.6f, 0.012f);

                soundA = new MenuMicrophone.MenuSoundObject(manager.menuMic, soundData, true, 0f, 1f, 1f, false); ;

                AudioClip oldClip = clips.FirstOrDefault(x => x.name == $"{owningPlayer.inLobbyId}_voiceFeed");
                if (oldClip != null)
                {
                    // destroy stale clip
                    clips.Remove(oldClip);
                    MonoBehaviour.Destroy(oldClip);

                }

                AudioClip newClip = AudioClip.Create($"{owningPlayer.inLobbyId}_voiceFeed", bufferLength, AudioManager.Channels, AudioManager.SampleRate, true, OnAudioRead, OnAudioSetPosition);

                soundA.audioSource.clip = newClip;

                manager.menuMic.soundObjects.Add(soundA);

                soundA.allowPlay = true;

                soundA.Play();
            }

            base.Update();
        }
    }
}

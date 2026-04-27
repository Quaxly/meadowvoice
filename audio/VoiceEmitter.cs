using RainMeadow;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace meadowvoice
{
    internal class VoiceEmitter : PlaybackChannel
    {
        public static ConditionalWeakTable<Creature, VoiceEmitter> map = new();

        public Creature owner;
        public OnlineEntity owningEntity;

        public Room room;
        public ChunkSoundEmitter soundA;

        public float Volume
        {
            get
            {
                return vol;
            }
            set
            {
                vol = value;
                if (soundA != null) soundA.volume = value;
            }
        }

        public int streamPosition;
        public float vol;

        public VoiceEmitter(Creature owner, OnlineCreature owningEntity)
        {
            this.owner = owner;
            this.owningEntity = owningEntity;

            map.Add(owner, this);
        }

        public void ChangeOwningCreature(Creature newOwner)
        {
            owner = newOwner;

            var oldOwner = owningEntity.owner;

            owningEntity = newOwner.abstractCreature.GetOnlineCreature();

            if (owningEntity.owner != oldOwner)
            {
                RainMeadow.RainMeadow.Warn($"VoiceEmitter changed owners, recreating.");
                AudioManager.voices.Remove(oldOwner);
            }
        }

        public override void Update()
        {
            if (owner.room != null)
            {
                room = owner.room;
            }

            if (soundA != null && (bufferedSamples == 0 || soundA.slatedForDeletetion))
            {
                soundA.slatedForDeletetion = true;
                soundA = null;
            }

            if (soundA != null)
            {
                soundA.alive = true;
            }
            else if (bufferedSamples > 0)
            {
                VirtualMicrophone mic = room.game.cameras[0].virtualMicrophone;

                foreach(var camera in room.game.cameras)
                {
                    if (camera.room == room)
                    {
                        mic = camera.virtualMicrophone;
                        break;
                    }
                }

                soundA = new(owner.mainBodyChunk, 1f, 1f);
                soundA.requireActiveUpkeep = false;

                room.AddObject(soundA);
                var soundData = new SoundLoader.SoundData(SoundID.Mushroom_Trip_LOOP, 0, 0.5f, 1f, 0.5f, 0.12f);
                //soundData.dopplerFac = 0.012f;

                soundA.currentSoundObject = new VirtualMicrophone.ObjectSound(mic, soundData, true, soundA, 1f, 1f, false);

                mic.soundObjects.Add(soundA.currentSoundObject);

                soundA.currentSoundObject.audioSource.clip = AudioClip.Create($"{owningEntity.owner.inLobbyId}_voiceFeed", bufferLength, AudioManager.Channels, AudioManager.SampleRate, true, OnAudioRead, OnAudioSetPosition);
                soundA.currentSoundObject.Play();
            }
            base.Update();
        }

        public override void AudioUpdate()
        {
            base.AudioUpdate();

            if (stallTimer > 500 && soundA != null)
            {
                soundA.currentSoundObject.Stop();
                soundA.slatedForDeletetion = true;
                soundA = null;
            }
        }
    }
}

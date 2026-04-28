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

        public float Pitch
        {
            get
            {
                return ptch;
            }
            set
            {
                ptch = value;
                if (soundA != null) soundA.pitch = value;
            }
        }

        public int streamPosition;

        public VoiceEmitter(OnlinePlayer owningPlayer, Creature owner, OnlineCreature owningEntity) : base(owningPlayer)
        {
            this.owner = owner;
            this.owningEntity = owningEntity;

            map.Add(owner, this);
        }

        public void ChangeOwningCreature(Creature newOwner)
        {
            owner = newOwner;

            owningEntity = newOwner.abstractCreature.GetOnlineCreature();

            if (owningEntity.owner != owningPlayer)
            {
                RainMeadow.RainMeadow.Warn($"VoiceEmitter changed owners, recreating.");
                Destroy();
            }
        }

        public override void Destroy()
        {
            base.Destroy();
            if (soundA != null) soundA.Destroy();
        }

        public override void Update()
        {
            if (slatedfordeletion) return;
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

                soundA = new(owner.mainBodyChunk, 1f, 1f)
                {
                    requireActiveUpkeep = true,
                };

                room.AddObject(soundA);
                var soundData = new SoundLoader.SoundData(SoundID.Mushroom_Trip_LOOP, 0, 0.5f, 1f, 0.6f, 0.012f);
                //soundData.dopplerFac = 0.012f;

                soundA.currentSoundObject = new VirtualMicrophone.ObjectSound(mic, soundData, true, soundA, 1f, 1f, false);

                mic.soundObjects.Add(soundA.currentSoundObject);

                soundA.currentSoundObject.audioSource.clip = AudioClip.Create($"{owningEntity.owner.inLobbyId}_voiceFeed", bufferLength, AudioManager.Channels, AudioManager.SampleRate, true, OnAudioRead, OnAudioSetPosition);
                soundA.currentSoundObject.Play();
            }
            base.Update();
        }

        public static VoiceEmitter MakeFromPlayer(OnlinePlayer onlinePlayer, Room room)
        {
            AudioManager.voices.Remove(onlinePlayer);

            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
            {
                if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                if (playerAvatar.FindEntity(true) is OnlineCreature opo && opo.apo is AbstractCreature ac)
                {
                    if (opo.owner == onlinePlayer && opo.realizedCreature != null && opo.realizedCreature.room == room)
                    {
                        VoiceEmitter voiceEmitter = new(onlinePlayer, opo.realizedCreature, opo);
                        AudioManager.voices[onlinePlayer] = voiceEmitter;
                        break;
                    }
                }
            }

            return null;
        }
    }
}

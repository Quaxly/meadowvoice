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
            if (soundA != null)
            {
                soundA.currentSoundObject.Stop();
                soundA.currentSoundObject.Destroy(); // sound object must be destroyed or audiosource -> clip can overlap between playback channels I think
                soundA.Destroy();
                soundA = null;
            }
            base.Destroy();
        }

        public override void Update()
        {
            if (slatedfordeletion) return;
            if (owner.room != null)
            {
                room = owner.room;
            }

            if (soundA != null && (bufferedSamples == 0 || soundA.slatedForDeletetion || soundA.room != room))
            {
                soundA.currentSoundObject.Stop();
                soundA.currentSoundObject.Destroy();
                soundA.Destroy();
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
                var soundData = new SoundLoader.SoundData(SoundID.Mushroom_Trip_LOOP, 0, 0.5f, 1f, 0.6f, 0.012f)
                {
                    dontAutoPlay = true,
                };
                //soundData.dopplerFac = 0.012f;

                soundA.currentSoundObject = new VirtualMicrophone.ObjectSound(mic, soundData, true, soundA, 1f, 1f, false);

                mic.soundObjects.Add(soundA.currentSoundObject);


                // switch out the audioclip with our custom one
                // this audioclip will automatically have incoming pcm data written to it
                AudioClip oldClip = clips.FirstOrDefault(x => x.name == $"{owningPlayer.inLobbyId}_voiceFeed");
                if (oldClip != null)
                {
                    // destroy stale clip
                    clips.Remove(oldClip);
                    MonoBehaviour.Destroy(oldClip);
                    
                }
                AudioClip newClip = AudioClip.Create($"{owningPlayer.inLobbyId}_voiceFeed", bufferLength, AudioManager.Channels, AudioManager.SampleRate, true, OnAudioRead, OnAudioSetPosition);
                clips.Add(newClip);

                soundA.currentSoundObject.audioSource.clip = newClip;
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

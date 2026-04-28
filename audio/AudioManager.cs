using RainMeadow;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace meadowvoice
{
    internal class AudioManager : MainLoopProcess, IUseCustomPackets
    {
        public const string KEY = "mdv";
        public static bool Debugging = false;
        public static AudioManager Instance { get; private set; }
        public static Dictionary<OnlinePlayer, PlaybackChannel> voices = new();
        public static List<MeadowPlayerId> mutedPlayers = new();

        public static ProcessManager.ProcessID MeadowVoice = new("MeadowVoice", true);

        public MicrophoneRecorder microphone;
        public Opus opus;

        public bool Active => true;

        public bool Recording 
        {
            get => microphone.recording;
            set
            {
                if (value != microphone.recording)
                {
                    if (value)
                    {
                        microphone.BeginStream();
                    }
                    else
                    {
                        microphone.EndStream();
                    }
                }
                microphone.recording = value;
            }
        }

        public const int Channels = (int)Phonic.Mono;

        public const int SampleRate = 48000; // 48khz
        public const int MaxBandwidth = 38280;

        public const int FrameSize = 20; // 20 ms
        public static int SamplesPerFrame => SampleRate / (1000 / FrameSize) * Channels;

        public int JitterBuffer => ModOptions.jitterBuffer.Value; // ms

        public int voiceTimer;

        public bool HasVoice => voiceTimer < 30;

        public bool InGame => manager.currentMainLoop is RainWorldGame rainWorldGame;
        public OnlineEntity myAvatar;

        public AudioManager(ProcessManager processManager) : base(processManager, MeadowVoice)
        {
            Instance = this;
            opus = new();
            microphone = new();

            microphone.OnAudioReady += Microphone_OnAudioReady;

            CustomManager.Subscribe(KEY, this);
        }

        public void CleanAndRemoveVoices()
        {
            var collection = voices.Values.ToList();
            for (int i = 0; i < collection.Count; i++)
            {
                collection[i].Destroy();
            }
            voices.Clear();
        }

        public override void Update()
        {
            base.Update();
            //microphone.Update();

            if (voiceTimer < 30)
            {
                voiceTimer++;
            }
        }

        /// <summary>
        /// Mute another player
        /// </summary>
        /// <param name="id"></param>
        public void Mute(MeadowPlayerId id)
        {
            mutedPlayers.Add(id);
            SafePlaySound(Enums.MEADOWVOICE_OTHERMUTE, 0f, 0.35f, 1f, 1);
        }
        /// <summary>
        /// Unmute another player
        /// </summary>
        /// <param name="id"></param>
        public void Unmute(MeadowPlayerId id)
        {
            mutedPlayers.RemoveAll(m => m == id);
            SafePlaySound(Enums.MEADOWVOICE_OTHERUNMUTE, 0f, 0.35f, 1f, 1);
        }

        public void BeginStream()
        {
            Recording = true;
            SafePlaySound(Enums.MEADOWVOICE_UNMUTE, 0f, 0.35f, 1f, 1);
        }

        public void EndStream()
        {
            Recording = false;
            SafePlaySound(Enums.MEADOWVOICE_MUTE, 0f, 0.35f, 1f, 1);
        }

        private void Microphone_OnAudioReady(byte[] opusData)
        {
            if (OnlineManager.lobby is null || OnlineManager.lobby.gameMode is MeadowGameMode) return;
            if (!Recording) return;

            voiceTimer = 0;

            if (Debugging)
            {
                DebugVoice(opusData);
            }

            if (InGame && myAvatar != null && myAvatar.currentlyJoinedResource is RoomSession roomSession)
            {
                BroadcastVoiceInRoom(roomSession, opusData);
            }
            else if (!InGame)
            {
                // TODO menu voice chat support.
            }
        }

        private void DebugVoice(byte[] opusData)
        {
            if (voices.TryGetValue(OnlineManager.mePlayer, out var playback))
            {
                playback.RecieveAudio(opusData);
            }
        }

        public void SendVoice(OnlinePlayer toPlayer, byte[] opusData)
        { 
            try
            {
                if (opusData.Length > ushort.MaxValue) throw new InvalidOperationException();
                if (VoiceChatSession.instance.participants.Contains(toPlayer))
                {
                    CustomManager.SendCustomData(toPlayer, KEY, opusData, (ushort)opusData.Length, NetIO.SendType.Unreliable);
                }
            } 
            catch(Exception e)
            {
                RainMeadow.RainMeadow.Warn($"There was an error encoding voice data to {toPlayer}: {e.Message}");
            }
        }

        public void SendEncryptedVoice(OnlinePlayer toPlayer, byte[] opusData, string publicKey)
        {
            try
            {
                if (string.IsNullOrEmpty(publicKey)) throw new InvalidOperationException();

                var encryptedData = Crypto.Encrypt(opusData, publicKey);

                if (encryptedData.Length > ushort.MaxValue) throw new InvalidOperationException();
                if (VoiceChatSession.instance.participants.Contains(toPlayer))
                {
                    CustomManager.SendCustomData(toPlayer, KEY, encryptedData, (ushort)encryptedData.Length, NetIO.SendType.Unreliable);
                }
            }
            catch (Exception e)
            {
                RainMeadow.RainMeadow.Warn($"There was an error encoding voice data to {toPlayer}: {e.Message}");
            }
        }

        public void BroadcastVoiceInRoom(RoomSession roomSession, byte[] opusData)
        {
            foreach(var op in roomSession.participants)
            {
                if (!op.isMe)
                {
                    SendVoice(op, opusData);
                }
            }
        }

        public void RecieveAudio(OnlinePlayer fromPlayer, byte[] opusData)
        {
            if (voices.TryGetValue(fromPlayer, out var playback))
            {
                playback.RecieveAudio(opusData);
            }
            else
            {
                RainMeadow.RainMeadow.Warn($"Recieved Voice Packet from {fromPlayer.id.name} who does not have a playback channel");
            }
        }

        public void ProcessPacket(OnlinePlayer fromPlayer, CustomPacket packet)
        {
            if (packet.key != KEY) return;
            if (mutedPlayers.Contains(fromPlayer.id)) return;

            RecieveAudio(fromPlayer, packet.data);
        }

        public void SafePlaySound(SoundID soundID, float pan, float vol, float pitch, int volGroup)
        {
            if (manager.menuMic != null)
            {
                manager.menuMic.PlaySound(soundID, pan, vol, pitch);
            }
            else if (manager.currentMainLoop is RainWorldGame game)
            {
                game.cameras[0].virtualMicrophone.PlaySound(soundID, pan, vol, pitch, volGroup);
            }
        }

        public enum Phonic
        {
            Mono = 1,
            Stereo = 2,
        }
    }
}

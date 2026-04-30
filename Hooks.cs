using AssetBundles;
using meadowvoice;
using meadowvoice.HUD;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RainMeadow;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace meadowvoice
{
    internal static class Hooks
    {
        public static bool muteKeyHeld;
        public static List<IDetour> detours = new();

        public static void Apply()
        {
            On.RainWorldGame.Update += RainWorldGame_Update;
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
            On.ProcessManager.PreSwitchMainProcess += ProcessManager_PreSwitchMainProcess;
            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
            On.HUD.HUD.InitMultiplayerHud += HUD_InitMultiplayerHud;

            detours.Add(new Hook(typeof(Lobby).GetMethod("ActivateImpl", BindingFlags.NonPublic | BindingFlags.Instance), Lobby_ActivateImpl));
            detours.Add(new Hook(typeof(OnlineResource).GetMethod(nameof(OnlineResource.OnPlayerDisconnect)), OnlineResource_OnPlayerDisconnect));
            detours.Add(new Hook(typeof(OnlineManager).GetMethod(nameof(OnlineManager.ResourceFromIdentifier)), OnlineManager_ResourceFromIdentifier));
            detours.Add(new Hook(typeof(OnlineManager).GetMethod(nameof(OnlineManager.LeaveLobby)), OnlineManager_LeaveLobby));
            detours.Add(new ILHook(typeof(OnlineManager).GetMethod(nameof(OnlineManager.Update)), OnlineManager_Update));
        }

        public static void Remove()
        {
            On.RainWorldGame.Update -= RainWorldGame_Update;
            On.RainWorldGame.ShutDownProcess -= RainWorldGame_ShutDownProcess;
            On.ProcessManager.PreSwitchMainProcess -= ProcessManager_PreSwitchMainProcess;
            On.HUD.HUD.InitSinglePlayerHud -= HUD_InitSinglePlayerHud;
            On.HUD.HUD.InitMultiplayerHud -= HUD_InitMultiplayerHud;

            foreach (IDetour detour in detours)
            {
                detour.Undo();
            }

            detours.Clear();
        }

        public static OnlineResource OnlineManager_ResourceFromIdentifier(Func<string, OnlineResource> orig, string rid)
        {
            if (rid == VoiceChatSession.staticID) return VoiceChatSession.instance;
            return orig(rid);
        }

        public static void Lobby_ActivateImpl(Action<Lobby> orig, Lobby self)
        {
            orig(self);
            if (RainMeadow.RainMeadow.isStoryMode(out _) || RainMeadow.RainMeadow.isArenaMode(out _))
            {
                new VoiceChatSession();
            }
        }

        public static void OnlineManager_Update(ILContext ctx)
        {
            try
            {
                ILCursor cursor = new(ctx);
                cursor.GotoNext(MoveType.Before,
                    x => x.MatchLdsfld<OnlineManager>(nameof(OnlineManager.players))
                );
                cursor.EmitDelegate(() =>
                {
                    if (VoiceChatSession.instance != null) VoiceChatSession.instance.Update();  
                });

            }
            catch (Exception except)
            {
                RainMeadow.RainMeadow.Error(except);
            }
        }


        public static void OnlineResource_OnPlayerDisconnect(Action<OnlineResource, OnlinePlayer> orig, OnlineResource self, OnlinePlayer player)
        {
            if (self is Lobby)
            {
                if (VoiceChatSession.instance is VoiceChatSession)
                {
                    VoiceChatSession.instance.HandleDisconnect(player);
                }
            }

            orig(self, player);
        }

        public static void OnlineManager_LeaveLobby(Action orig)
        {
            orig();
            VoiceChatSession.instance = null;
        }


        private static void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, global::HUD.HUD self, RoomCamera cam)
        {
            orig(self, cam);
            if (RainMeadow.RainMeadow.isStoryMode(out var gameMode))
            {
                self.AddPart(new VoiceHud(self, cam));
            }
        }

        private static void HUD_InitMultiplayerHud(On.HUD.HUD.orig_InitMultiplayerHud orig, global::HUD.HUD self, ArenaGameSession session)
        {
            orig(self, session);
            if (RainMeadow.RainMeadow.isArenaMode(out var gameMode))
            {
                self.AddPart(new VoiceHud(self, session.game.cameras[0]));
            }
        }

        private static void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        {
            orig(self);
            if (OnlineManager.lobby != null)
            {
                VoiceDebug.RemoveOverlay(self);
            }
        }

        private static void ProcessManager_PreSwitchMainProcess(On.ProcessManager.orig_PreSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            orig(self, ID);
            AudioManager.Instance.CleanAndRemoveVoices();
            AudioManager.Instance.myAvatar = null;
        }

        private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (OnlineManager.lobby is null || OnlineManager.lobby.gameMode is MeadowGameMode || AudioManager.Instance == null)
            {
                return;
            }
            VoiceDebug.Update(self);
            if (ModOptions.pushToTalk.Value)
            {
                if (Input.GetKey(ModOptions.muteKey.Value))
                {
                    AudioManager.Instance.BeginStream();
                }
                else if (AudioManager.Instance.Recording)
                {
                    AudioManager.Instance.EndStream();
                }
            }
            else
            {
                if (Input.GetKey(ModOptions.muteKey.Value))
                {
                    if (!muteKeyHeld)
                    {
                        if (!AudioManager.Instance.Recording)
                        {
                            AudioManager.Instance.BeginStream();
                        } 
                        else
                        {
                            AudioManager.Instance.EndStream();
                        }
                        muteKeyHeld = true;
                    }
                } 
                else
                {
                    muteKeyHeld = false;
                }
            }

            if (AudioManager.Instance.myAvatar != null)
            {
                var roomSession = AudioManager.Instance.myAvatar.joinedResources.FirstOrDefault(x => x is RoomSession) as RoomSession;
                if (roomSession != null && roomSession.absroom.realizedRoom != null)
                {
                    foreach (var player in VoiceChatSession.instance.participants)
                    {
                        if (player.isMe || !roomSession.participants.Contains(player)) continue;
                        if (!AudioManager.voices.TryGetValue(player, out var pb))
                        {
                            if (VoiceEmitter.MakeFromPlayer(player, roomSession.absroom.realizedRoom) == null)
                            {
                                RainMeadow.RainMeadow.Warn($"Could not create VoiceEmitter for {player}, they may not have a valid avatar.");
                            }
                        }
                        else if (pb.slatedfordeletion)
                        {
                            AudioManager.voices.Remove(player);
                        }
                    }
                }
            }
            else
            {
                foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
                {
                    if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                    if (playerAvatar.FindEntity(true) is OnlineCreature opo && opo.apo is AbstractCreature ac)
                    {
                        if (opo.owner == OnlineManager.mePlayer)
                        {
                            AudioManager.Instance.myAvatar = opo;
                            break;
                        }
                    }
                }
            }

            foreach(var playback in AudioManager.voices)
            {
                playback.Value.Update();
                playback.Value.AudioUpdate();
            }
        }
    }
}

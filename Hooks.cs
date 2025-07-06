using Menu;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RainMeadow;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using UnityEngine;
using static Rewired.Utils.ReflectionTools;
using BindingFlags = System.Reflection.BindingFlags;

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
            On.AbstractCreature.Realize += AbstractCreature_Realize;
            On.Creature.Update += Creature_Update;
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
            On.AbstractCreature.Realize -= AbstractCreature_Realize;
            On.Creature.Update -= Creature_Update;
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

        private static void Creature_Update(On.Creature.orig_Update orig, Creature self, bool eu)
        {
            orig(self, eu);
            if (OnlineManager.lobby is not null && OnlineManager.lobby.gameMode is not MeadowGameMode)
            {
                if (VoiceEmitter.map.TryGetValue(self, out var emitter))
                {
                    emitter.Update();
                }
                if (PlaybackDebugger.map.TryGetValue(self, out var pbd))
                {
                    pbd.Update();
                }
            }
        }

        private static void RainWorldGame_ShutDownProcess(On.RainWorldGame.orig_ShutDownProcess orig, RainWorldGame self)
        {
            orig(self);
            if (OnlineManager.lobby != null)
            {
                SteamVoiceDebug.RemoveOverlay(self);
            }
        }

        private static void AbstractCreature_Realize(On.AbstractCreature.orig_Realize orig, AbstractCreature self)
        {
            var wasCreature = self.realizedCreature;
            orig(self);
            if (OnlineManager.lobby is not null && OnlineManager.lobby.gameMode is not MeadowGameMode && self.GetOnlineObject(out var oe))
            {
                if (self.realizedCreature != null && self.realizedCreature != wasCreature && oe is OnlineCreature oc && oc.TryGetData<SlugcatCustomization>(out var data))
                {
                    if (oc.isMine)
                    {
                        if (SteamVoiceChat.myVoiceChat == null)
                        {
                            SteamVoiceChat.myVoiceChat = new SteamVoiceChat(OnlineManager.mePlayer, oc);
                        }
                        else
                        {
                            if (SteamVoiceChat.myVoiceChat.ownerEntity != oc)
                            {
                                SteamVoiceChat.myVoiceChat.ChangeOwningEntity(oc);
                            }
                        }
                        if (SteamVoiceDebug.PLAYBACK)
                        {
                            if (!PlaybackDebugger.map.TryGetValue(oc.realizedCreature, out _))
                            {
                                new PlaybackDebugger(oc.realizedCreature, oc);
                            }
                        }
                    }
                    else
                    {
                        if (!VoiceEmitter.map.TryGetValue(oc.realizedCreature, out var emitter))
                        {
                            new VoiceEmitter(oc.realizedCreature, oc);
                        }
                        else
                        {
                            emitter.ChangeOwningCreature(oc.realizedCreature);
                        }
                    }
                }
            }
        }

        private static void RainWorldGame_Update(On.RainWorldGame.orig_Update orig, RainWorldGame self)
        {
            orig(self);
            if (OnlineManager.lobby is null || OnlineManager.lobby.gameMode is MeadowGameMode || SteamVoiceChat.myVoiceChat == null)
            {
                return;
            }
            SteamVoiceDebug.Update(self);
            SteamVoiceChat.myVoiceChat.VoiceUpdate();
            if (ModOptions.pushToTalk.Value)
            {
                if (Input.GetKey(ModOptions.muteKey.Value))
                {
                    SteamVoiceChat.myVoiceChat.BeginStream();
                }
                else
                {
                    SteamVoiceChat.myVoiceChat.EndStream();
                }
            } 
            else
            {
                if (Input.GetKey(ModOptions.muteKey.Value))
                {
                    if (!muteKeyHeld)
                    {
                        if (!SteamVoiceChat.myVoiceChat.recording)
                        {
                            SteamVoiceChat.myVoiceChat.BeginStream();
                        } 
                        else
                        {
                            SteamVoiceChat.myVoiceChat.EndStream();
                        }
                        muteKeyHeld = true;
                    }
                } 
                else
                {
                    muteKeyHeld = false;
                }
            }
        }
    }
}

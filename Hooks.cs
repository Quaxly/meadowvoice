using AssetBundles;
using meadowvoice;
using meadowvoice.HUD;
using RainMeadow;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace meadowvoice
{
    internal static class Hooks
    {
        public static bool muteKeyHeld;
        public static void Apply()
        {
            On.RainWorldGame.Update += RainWorldGame_Update;
            On.RainWorldGame.ShutDownProcess += RainWorldGame_ShutDownProcess;
            On.AbstractCreature.Realize += AbstractCreature_Realize;
            On.Creature.Update += Creature_Update;
            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;
            On.HUD.HUD.InitMultiplayerHud += HUD_InitMultiplayerHud;
        }

        public static void Remove()
        {
            On.RainWorldGame.Update -= RainWorldGame_Update;
            On.RainWorldGame.ShutDownProcess -= RainWorldGame_ShutDownProcess;
            On.AbstractCreature.Realize -= AbstractCreature_Realize;
            On.Creature.Update -= Creature_Update;
            On.HUD.HUD.InitSinglePlayerHud -= HUD_InitSinglePlayerHud;
            On.HUD.HUD.InitMultiplayerHud -= HUD_InitMultiplayerHud;
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

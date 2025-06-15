using meadowvoice;
using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MeadowVoice
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
        }

        public static void Remove()
        {
            On.RainWorldGame.Update -= RainWorldGame_Update;
            On.RainWorldGame.ShutDownProcess -= RainWorldGame_ShutDownProcess;
            On.AbstractCreature.Realize -= AbstractCreature_Realize;
            On.Creature.Update -= Creature_Update;
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
                    }
                    else
                    {
                        if (!VoiceEmitter.map.TryGetValue(oc.realizedCreature, out _))
                        {
                            new VoiceEmitter(oc.realizedCreature, oc);
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
            if (SteamVoiceDebug.DEBUG && Input.GetKey(KeyCode.PageDown))
            {
                SteamVoiceChat.myVoiceChat.TestTone();
            }
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

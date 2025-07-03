using HUD;
using Menu;
using RainMeadow;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static meadowvoice.Hooks;
using static RainMeadow.SpectatorOverlay;

namespace meadowvoice
{
    internal class MuteButton : SimplerSymbolButton
    {
        public static ConditionalWeakTable<PlayerButton, SimplerSymbolButton> activeMuteButtons = new();
        private OnlinePlayer player;

        private float sin;
        private float lastSin;
        private float red;
        private float lastRed;

        public MuteButton(Menu.Menu menu, MenuObject owner, string singalText, Vector2 pos, OnlinePlayer player) : base(menu, owner, "speakingmuted", singalText, pos)
        {
            this.player = player;
            this.OnClick += (_) =>
            {
                if (SteamVoiceChat.myVoiceChat is null) return;
                if (SteamVoiceChat.mutedPlayers.Contains(this.player.id))
                {
                    SteamVoiceChat.myVoiceChat.Unmute(this.player.id);
                }
                else
                {
                    SteamVoiceChat.myVoiceChat.Mute(this.player.id);
                }
            };
            this.symbolSprite.scale = 0.65f;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            bool currentlyMuted = SteamVoiceChat.mutedPlayers.Contains(player.id);
            if (currentlyMuted)
            {
                this.symbolSprite.element = Futile.atlasManager.GetElementWithName("speakingmuted");
                this.symbolSprite.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkRed);
            }
            else
            {
                this.symbolSprite.color = Color.white;
                var emitter = VoiceEmitter.FromOnlinePlayer(this.player);
                if (emitter is null)
                {
                    this.symbolSprite.element = Futile.atlasManager.GetElementWithName("speakingmuted");
                }
                else
                {
                    this.symbolSprite.element = Futile.atlasManager.GetElementWithName(emitter.voiceTimer <= 30 ? "speakingactive" : "speakingidle");
                }
            }
        }

        public static void NewButton(SpectatorOverlay.PlayerButton self)
        {
            MuteButton muteButton;
            MuteButton.activeMuteButtons.Add(self, muteButton = new MuteButton(self.menu, self, "MEADOWVOICE_MUTE", new(self.size.x + 20, 0), self.player));
            self.subObjects.Add(muteButton);
            self.menu.TryMutualBind(self, muteButton, true);
        }
    }
}

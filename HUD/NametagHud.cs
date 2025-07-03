using HUD;
using RainMeadow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace meadowvoice.HUD
{
    internal class NametagHud : OnlinePlayerHudPart
    {
        public static ConditionalWeakTable<PlayerSpecificOnlineHud, NametagHud> activeHuds = new();
        public FSprite speakerIcon;
        public OnlinePlayerDisplay playerDisplay;
        public NametagHud(PlayerSpecificOnlineHud owner) : base(owner)
        {
            this.owner = owner;

            this.speakerIcon = new FSprite("speakingidle", true)
            {
                alpha = 0f,
                scale = 0.6f,
            };

            owner.hud.fContainers[0].AddChild(speakerIcon);

            activeHuds.Add(owner, this);
        }

        public override void Update()
        {
            base.Update();
        }
        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            var op = this.owner.clientSettings.owner;
            bool hasVoice = false;
            if (SteamVoiceChat.myVoiceChat is null || this.playerDisplay is null)
            {
                this.speakerIcon.alpha = 0f;
                return;
            }
            if (op.isMe)
            {
                hasVoice = SteamVoiceChat.myVoiceChat.hasVoice;
            }
            else
            {
                var emitter = VoiceEmitter.FromOnlinePlayer(op);
                if (emitter != null)
                {
                    hasVoice = emitter.voiceTimer <= 30;
                }
            }
            this.speakerIcon.alpha = this.playerDisplay.username.alpha;
            this.speakerIcon.color = this.playerDisplay.username.color;
            if (SteamVoiceChat.mutedPlayers.Contains(owner.clientSettings.owner.id))
            {
                this.speakerIcon.element = Futile.atlasManager.GetElementWithName("speakingmuted");
            }
            else
            {
                this.speakerIcon.element = Futile.atlasManager.GetElementWithName(hasVoice ? "speakingactive" : "speakingidle");
            }
            this.speakerIcon.x = this.playerDisplay.username.x - (this.playerDisplay.username._textRect.width / 2) - 15f;
            this.speakerIcon.y = this.playerDisplay.username.y;
        }
        public override void ClearSprites()
        {
            base.ClearSprites();
            this.speakerIcon.RemoveFromContainer();
        }
    }
}

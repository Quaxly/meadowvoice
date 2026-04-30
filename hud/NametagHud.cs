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
        public PlaybackChannel emitter;
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
            if (AudioManager.voices.TryGetValue(this.owner.clientSettings.owner, out var playBack))
            {
                emitter = playBack;
            }
        }
        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            var op = this.owner.clientSettings.owner;
            bool hasVoice = false;
            if (AudioManager.Instance is null || this.playerDisplay is null)
            {
                this.speakerIcon.alpha = 0f;
                return;
            }
            if (op.isMe)
            {
                hasVoice = AudioManager.Instance.HasVoice;
            }
            else
            {
                if (!VoiceChatSession.instance.participants.Contains(op))
                {
                    this.speakerIcon.alpha = 0f;
                    return;
                }
                else
                {
                    this.speakerIcon.alpha = 1f;
                }
                
                if (emitter != null)
                {
                    hasVoice = emitter.voiceTimer < 30;
                }
            }
            this.speakerIcon.alpha = this.playerDisplay.username.alpha;
            this.speakerIcon.color = this.playerDisplay.username.color;
            if (AudioManager.mutedPlayers.Contains(owner.clientSettings.owner.id) || (op.isMe && !AudioManager.Instance.microphone.recording))
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

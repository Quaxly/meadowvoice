using HUD;
using meadowvoice.HUD;
using RainMeadow;
using System.Linq;
using UnityEngine;

namespace meadowvoice
{
    internal class VoiceHud : HudPart
    {
        private const float SafeBorder = 35f;

        private RoomCamera camera;
        private RainWorldGame game;

        private FContainer Container => hud.fContainers[1];
        private FSprite micIcon;

        private Color displayColor;
        private Color lastDisplayColor;
        private float fadeColor;

        private float scale;
        private float lastScale;

        private float alpha;
        private float lastAlpha;

        private int activityTimer;

        //private bool pulse;
        private bool state;
        public VoiceHud(global::HUD.HUD hud, RoomCamera camera) : base(hud)
        {
            this.camera = camera;
            this.game = camera.game;

            this.micIcon = new FSprite("recordingoff", true)
            {
                scale = 0.65f
            };

            Container.AddChild(this.micIcon);

            this.micIcon.SetPosition(hud.rainWorld.screenSize.x - SafeBorder, SafeBorder);

            if (SteamVoiceChat.myVoiceChat != null && SteamVoiceChat.myVoiceChat.recording)
            {
                fadeColor = 1f;
            }
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            if (slatedForDeletion || SteamVoiceChat.myVoiceChat == null) return;
            this.micIcon.SetPosition(hud.rainWorld.screenSize.x - SafeBorder, SafeBorder);
            this.micIcon.element = Futile.atlasManager.GetElementWithName(SteamVoiceChat.myVoiceChat.recording ? "recordingon" : "recordingoff"); 
            this.micIcon.color = Color.Lerp(this.lastDisplayColor, this.displayColor, timeStacker);
            this.micIcon.scale = Mathf.Lerp(this.lastScale, this.scale, timeStacker);
            this.micIcon.alpha = Mathf.Lerp(this.lastAlpha, this.alpha, timeStacker);
        }

        public override void Update()
        {
            base.Update();
            if (OnlineManager.lobby == null) return;
            if (SteamVoiceChat.myVoiceChat == null)
            {
                slatedForDeletion = true;
                return;
            }
            // THIS IS HORRIBLE I KNOW
            var onlineHud = hud.parts.OfType<PlayerSpecificOnlineHud>().ToList();
            foreach(var playerHud in onlineHud)
            {
                if (playerHud.playerDisplay != null && !NametagHud.activeHuds.TryGetValue(playerHud, out _))
                {
                    var tag = new NametagHud(playerHud);
                    tag.playerDisplay = playerHud.playerDisplay;
                    playerHud.parts.Add(tag);
                }
            }
            // THIS IS MORE HORRIBLE I ALSO KNOW
            var spectatorHud = hud.parts.OfType<SpectatorHud>().ToList();
            foreach(var spectator in spectatorHud)
            {
                if (spectator.spectatorOverlay == null || spectator.spectatorOverlay.PlayerButtons == null) continue;
                foreach(var playerButton in spectator.spectatorOverlay.PlayerButtons)
                {
                    if (!MuteButton.activeMuteButtons.TryGetValue(playerButton, out _))
                    {
                        MuteButton.NewButton(playerButton);
                    }
                }
            }
            bool justChanged = this.state;
            if (SteamVoiceChat.myVoiceChat.hasVoice) activityTimer = 0;
            this.state = SteamVoiceChat.myVoiceChat.recording;
            this.lastDisplayColor = this.displayColor;
            this.lastScale = this.scale;
            this.lastAlpha = this.alpha;
            this.fadeColor = Mathf.Lerp(this.fadeColor, SteamVoiceChat.myVoiceChat.recording ? 1f : 0f, 0.15f);
            this.displayColor = Color.Lerp(Color.Lerp(Color.red, Color.white, this.fadeColor), Color.black, SteamVoiceChat.myVoiceChat.hasVoice ? 0 : 0.45f);
            this.scale = Mathf.Lerp(this.scale, 0.65f, 0.1f);
            if (justChanged != this.state)
            {
                this.scale += 0.30f;
                activityTimer = 0;
                this.alpha = 0.95f;
            }
            if (activityTimer > 600)
            {
                this.alpha = Mathf.Lerp(this.alpha, 0.30f, 0.05f);
            }
            else
            {
                this.activityTimer++;
            }
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            this.micIcon.RemoveFromContainer();

            hud.parts.Remove(this);
        }
    }
}

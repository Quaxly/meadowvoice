using RainMeadow;
using RainMeadow.UI.Components;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace meadowvoice
{
    internal class ArenaLobbyVoiceHud
    {
        public static ConditionalWeakTable<ArenaPlayerBox, ArenaLobbyVoiceHud> map = new();

        public ArenaPlayerBox owner;
        public ScrollSymbolButton? voiceButton;

        public bool initialized;

        public ArenaLobbyVoiceHud(ArenaPlayerBox playerBox)
        {
            owner = playerBox;
            if (!map.TryGetValue(playerBox, out _))
            {
                map.Add(playerBox, this);
            }
        }

        public void Update()
        {
            if (initialized)
            {
                if (voiceButton != null)
                {
                    if (owner.profileIdentifier.isMe)
                    {
                        if (!AudioManager.Instance.Recording)
                        {
                            voiceButton.symbolSprite.element = Futile.atlasManager.GetElementWithName("speakingmuted");
                        }
                        else
                        {
                            voiceButton.symbolSprite.element = Futile.atlasManager.GetElementWithName(AudioManager.Instance.voiceTimer <= 30 ? "speakingactive" : "speakingidle");
                        }
                    }
                    else
                    {
                        if (AudioManager.mutedPlayers.Contains(owner.profileIdentifier.id))
                        {
                            voiceButton.symbolSprite.element = Futile.atlasManager.GetElementWithName("speakingmuted");
                        }
                        else
                        {
                            if (AudioManager.voices.TryGetValue(owner.profileIdentifier, out var voice))
                            {
                                voiceButton.symbolSprite.element = Futile.atlasManager.GetElementWithName(voice.voiceTimer <= 30 ? "speakingactive" : "speakingidle");
                            }
                            else
                            {
                                voiceButton.symbolSprite.element = Futile.atlasManager.GetElementWithName("speakingmuted");
                            }
                        }
                    }
                }
            }
            else
            {
                if (owner.profileIdentifier.isMe || (VoiceChatSession.instance != null && VoiceChatSession.instance.participants.Contains(owner.profileIdentifier)))
                {
                    Init();
                }
            }
        }

        public void Init()
        {
            if (owner == null) return;
            if (owner.infoKickButton != null)
            {
                voiceButton = new(owner.menu, owner, "speakingactive", "Voice_Button", new(owner.colorInfoButton.pos.x + owner.colorInfoButton.size.x + 30, owner.colorInfoButton.pos.y - 15));
                voiceButton.symbolSprite.scale = 0.5f;
                owner.SafeAddSubobjects(voiceButton);
            }
            initialized = true;
        }
    }
}

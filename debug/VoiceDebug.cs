using RainMeadow;
using RWCustom;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace meadowvoice
{
    internal static class VoiceDebug
    {
        public static bool DEBUG = true; // set this to false to turn off debug overlay
        public static bool PLAYBACK = false;

        private static FContainer overlay;
        public static List<OnlinePlayer> recievingInfo = new();

        public static void Update(RainWorldGame game)
        {
            if (!DEBUG) return;
            if (overlay == null)
            {
                CreateOverlay(game);
            }
            if (overlay == null) return;

            recievingInfo = VoiceChatSession.instance.participants.ToList();

            foreach(var child in overlay._childNodes)
            {
                if (child is FLabel label && label.text.StartsWith("Transmitting (Voice)"))
                {
                    if (AudioManager.Instance != null)
                    {
                        if (AudioManager.Instance.Recording)
                        {
                            label.color = Color.green;
                        }
                        else
                        {
                            label.color = Color.red;
                        }
                    }
                    else
                    {
                        label.color = Color.gray;
                    }
                }
                if (child is FLabel label1 && label1.text.StartsWith("Microphone"))
                {
                    var result = AudioManager.Instance.Recording;
                    if (result)
                    {
                        label1.text = $"Microphone On - p {AudioManager.Instance.microphone.micPosition} b {AudioManager.Instance.microphone.buffer.Length} pcm {AudioManager.Instance.microphone.pcm.Length}";
                        label1.color = Color.green;
                    }
                    else
                    {
                        label1.text = "Microphone Off";
                        label1.color = Color.red;
                    }
                }
                if (child is FLabel label2 && label2.text.StartsWith("Recieving (From)"))
                {
                    label2.text = "Recieving (From)";
                    foreach(var s in recievingInfo)
                    {
                        AudioManager.voices.TryGetValue(s, out var pb);
                        if (pb != null)
                        {
                            label2.text += "\n" + $"@ {s} - pb {pb} t {pb.voiceTimer} b {pb.bufferedSamples}";
                            label2.color = pb.voiceTimer < 30 ? new(1f, 0.66f, 0f) : Color.gray;
                        }
                        else if (s.isMe)
                        {
                            label2.text += "\n" + $"@ {s} - Self";
                            label2.color = AudioManager.Instance.voiceTimer < 30 ? new(1f, 0.66f, 0f) : Color.gray;
                        }
                        else
                        {
                            label2.text += "\n" + $"@ {s} - No Playback Channel";
                            label2.color = Color.red;
                        }
                    }
                }
            }
        }

        private static void CreateOverlay(RainWorldGame game)
        {
            Vector2 screenSize = game.rainWorld.options.ScreenSize;
            overlay = new FContainer();

            overlay.AddChild(new FLabel(Custom.GetFont(), "Transmitting (Voice)")
            {
                alignment = FLabelAlignment.Left,
                x = 5.01f,
                y = screenSize.y - 10,
            });
            overlay.AddChild(new FLabel(Custom.GetFont(), "Microphone")
            {
                alignment = FLabelAlignment.Left,
                x = 5.01f,
                y = screenSize.y - 30,
            });
            overlay.AddChild(new FLabel(Custom.GetFont(), "Recieving (From)")
            {
                alignment = FLabelAlignment.Left,
                x = 5.01f,
                y = screenSize.y - 50 - (10 * recievingInfo.Count),
            });

            Futile.stage.AddChild(overlay);
        }

        public static void RemoveOverlay(RainWorldGame self)
        {
            recievingInfo.Clear();
            overlay?.RemoveFromContainer();
            overlay = null;
        }
    }
}

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

        private static FContainer overlay;
        public static List<OnlinePlayer> recievingInfo = new();
        public static List<FLabel> recievingFrom = new();

        public static void Update(RainWorldGame game)
        {
            if (!DEBUG) return;
            if (overlay == null)
            {
                CreateOverlay(game);
            }
            if (overlay == null) return;

            recievingInfo = VoiceChatSession.instance.participants.ToList();

            foreach(var label in recievingFrom)
            {
                label.RemoveFromContainer();
            }
            recievingFrom.Clear();
            foreach (var p in recievingInfo)
            {
                var label = new FLabel(Custom.GetFont(), "")
                {
                    alignment = FLabelAlignment.Left,
                    x = 5.01f,
                };
                recievingFrom.Add(label);
                overlay.AddChild(label);
            }

            Vector2 screenSize = game.rainWorld.options.ScreenSize;

            foreach (var child in overlay._childNodes)
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

                int offset = 0;

                if (child is FLabel label2 && label2.text.StartsWith("Recieving (From)"))
                {
                    label2.text = $"Recieving (From) - v {AudioManager.voices.Count} c {PlaybackChannel.clips.Count}";
                    label2.color = new(1f, 0.66f, 0f);
                    label2.y = screenSize.y - 50;
                    for (int i = 0; i < recievingInfo.Count; i++)
                    {
                        var s = recievingInfo[i];
                        AudioManager.voices.TryGetValue(s, out var pb);
                        var pLabel = recievingFrom[i];

                        pLabel.y = screenSize.y - (70 + offset);

                        if (pb != null)
                        {
                            pLabel.text += "\n" + $"@ {s} - pb {pb} t {pb.voiceTimer} b {pb.bufferedSamples} uf {pb.underflow} readHead {pb.readPos}/{pb.playbackRing.Length}|{pb.fullRead} writeHead {pb.writePos}/{pb.playbackRing.Length}|{pb.fullWrite}";
                            pLabel.color = pb.voiceTimer < 30 ? new(1f, 0.66f, 0f) : new(0.75f, 0.75f, 0.75f);
                        }
                        else if (s.isMe)
                        {
                            pLabel.text += "\n" + $"@ {s} - Self";
                            pLabel.color = AudioManager.Instance.voiceTimer < 30 ? new(1f, 0.66f, 0f) : Color.gray;
                        }
                        else
                        {
                            pLabel.text += "\n" + $"@ {s} - No Playback Channel";
                            pLabel.color = Color.gray;
                        }

                        offset += 20;
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
                y = screenSize.y - 50,
            });

            Futile.stage.AddChild(overlay);
        }

        public static void RemoveOverlay(RainWorldGame self)
        {
            recievingInfo.Clear();
            foreach(var label in recievingFrom) label.RemoveFromContainer();
            recievingFrom.Clear();
            overlay?.RemoveFromContainer();
            overlay = null;
        }
    }
}

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
    internal static class SteamVoiceDebug
    {
        public static bool DEBUG = false; // set this to false to turn off debug overlay
        public static bool PLAYBACK = false;

        private static FContainer overlay;
        public static List<string> recievingInfo = new();

        public static void Update(RainWorldGame game)
        {
            if (!DEBUG) return;
            if (overlay == null)
            {
                CreateOverlay(game);
            }
            if (overlay == null) return;

            foreach(var child in overlay._childNodes)
            {
                if (child is FLabel label && label.text.StartsWith("Transmitting (Voice)"))
                {
                    if (SteamVoiceChat.myVoiceChat != null)
                    {
                        if (SteamVoiceChat.myVoiceChat.recording)
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
                if (child is FLabel label1 && label1.text.StartsWith("Steam Voice"))
                {
                    var result = SteamUser.GetAvailableVoice(out uint pcb);
                    if (result == EVoiceResult.k_EVoiceResultOK)
                    {
                        label1.text = "Steam Voice OK - " + pcb;
                        label1.color = Color.green;
                    }
                    else
                    {
                        label1.text = "Steam Voice NOT OK - " + result.ToString();
                        label1.color = Color.red;
                    }
                }
                if (child is FLabel label2 && label2.text.StartsWith("Recieving (From)"))
                {
                    label2.text = "Recieving (From)";
                    foreach(var s in recievingInfo)
                    {
                        label2.text += "\n" + s;
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
            overlay.AddChild(new FLabel(Custom.GetFont(), "Steam Voice")
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
            overlay?.RemoveFromContainer();
            overlay = null;
        }
    }
}

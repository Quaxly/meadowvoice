using Menu.Remix.MixedUI;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace meadowvoice
{
    internal class ModOptions : OptionsTemplate
    {
        public static ModOptions Instance { get; } = new();

        public static PlaybackTest playbackTest;

        public static Configurable<bool> pushToTalk { get; } = Instance.config.Bind(nameof(pushToTalk),
            false, new ConfigurableInfo("Use push to talk instead of toggling mute/unmute.", null, "", "Push to Talk"));
        public static Configurable<KeyCode> muteKey { get; } = Instance.config.Bind(nameof(muteKey), 
            KeyCode.M, new ConfigurableInfo("Mute/Unmute or Push to Talk key.", null, "", "Mute Key"));

        public static Configurable<bool> noiseSuppression { get; } = Instance.config.Bind(nameof(noiseSuppression),
            true, new ConfigurableInfo("Remove background noise via RNNoise.", null, "", "Noise Suppression"));

        public static Configurable<bool> alertEnemy { get; } = Instance.config.Bind(nameof(alertEnemy),
            true, new ConfigurableInfo("Allows creature AI to detect your voice.", null, "", "Alert Enemy AI"));

        public static Configurable<bool> silenceOnDeath { get; } = Instance.config.Bind(nameof(silenceOnDeath),
            true, new ConfigurableInfo("Stops transmitting voice if you die.", null, "", "Silent Death"));

        public static Configurable<string> selectedDevice { get; } = Instance.config.Bind(nameof(selectedDevice),
            Microphone.devices.Count() > 0 ? Microphone.devices[0] : "", new ConfigurableInfo("Select a microphone to use.", null, "", "Audio Device"));

        // Advanced
        public static Configurable<int> jitterBuffer { get; } = Instance.config.Bind(nameof(jitterBuffer),
            3, new ConfigurableInfo("Only change this if you're experiencing frequent lagspikes in voice playback, default 3.", null, "", "Jitter Buffer"));

        public static void Register()
        {
            if (MachineConnector.GetRegisteredOI("meadowvoice") != Instance)
            {
                MachineConnector.SetRegisteredOI("meadowvoice", Instance);
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            Tabs = new OpTab[1];
            int tabIndex = -1;
            AddTab(ref tabIndex, "Meadow Voice");

            AddCheckBox(pushToTalk);

            AddNewLine(4);

            AddCheckBox(alertEnemy);
            DrawCheckBoxes(ref Tabs[tabIndex]);

            AddNewLine(4);

            DrawKeybinders(muteKey, ref Tabs[tabIndex]);

            AddNewLine(3);

            AddTestButton(ref Tabs[tabIndex]);

            AddNewLine(4);
            
            AddTextLabel("Meadow Voice uses your default microphone.\nIf you change your default microphone you must restart for changes to take effect.", FLabelAlignment.Center);
            DrawTextLabels(ref Tabs[tabIndex]);

            bool wasRecording = AudioManager.Instance.Recording;

            AudioManager.Instance.Recording = false;

            OnDeactivate += () =>
            {
                playbackTest.Destroy();
                playbackTest = null;
                AudioManager.Instance.Recording = wasRecording;
            };
        }

        private void AddTestButton(ref OpTab tab, Vector2? offset = null, bool newline = true)
        {
            var button = new OpSimpleButton(new Vector2(57f, Pos.y) + (offset ?? Vector2.zero), new(438f, 50f), Translate("Test Audio"));

            button.OnClick += (_) =>
            {
                if (playbackTest == null)
                {
                    playbackTest = new(AudioManager.Instance.manager);
                    AudioManager.Instance.BeginStream();
                }
                else
                {
                    playbackTest.Destroy();
                    playbackTest = null;
                    AudioManager.Instance.EndStream();
                }
            };

            tab.AddItems(
                button
            );
        }
    }
}

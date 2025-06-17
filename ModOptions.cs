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

        public static Configurable<bool> pushToTalk { get; } = Instance.config.Bind(nameof(pushToTalk),
            true, new ConfigurableInfo("Use push to talk instead of toggling mute/unmute.", null, "", "Push to Talk"));
        public static Configurable<KeyCode> muteKey { get; } = Instance.config.Bind(nameof(muteKey), 
            KeyCode.M, new ConfigurableInfo("Mute/Unmute or Push to Talk key.", null, "", "Mute Key"));

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
            DrawCheckBoxes(ref Tabs[tabIndex]);

            AddNewLine();
            
            DrawKeybinders(muteKey, ref Tabs[tabIndex]);
        }
    }
}

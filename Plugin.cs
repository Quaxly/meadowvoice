using System;
using System.Reflection;
using System.Security.Permissions;
using BepInEx;
using UnityEngine;

[assembly: AssemblyVersion(RainMeadow.RainMeadow.MeadowVersionStr)]
#pragma warning disable CS0618
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
namespace meadowvoice
{
    [BepInPlugin("meadowvoice", "Meadow Voice", "1.0.0")]
    public partial class Plugin : BaseUnityPlugin
    {
        private void OnEnable()
        {
            On.RainWorld.OnModsInit += RainWorldOnOnModsInit;
        }
        private bool IsInit;

        public static Plugin Instance;

        private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                if (IsInit) return;

                Instance = this;

                Natives.LoadNatives();

                Enums.Init();
                Hooks.Apply();
                ModOptions.Register();

                Futile.atlasManager.LoadAtlas("atlases/uiMeadowVoice");

                Crypto.Init();

                self.processManager.sideProcesses.Add(new AudioManager(self.processManager));

                RainMeadow.RainMeadow.Info($"DSP Buffer Size - {AudioSettings.GetConfiguration().dspBufferSize}");

                IsInit = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                throw;
            }
        }
    }
}

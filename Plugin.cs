using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using BepInEx;
using RainMeadow;

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

        private void RainWorldOnOnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
        {
            orig(self);
            try
            {
                if (IsInit) return;

                Enums.Init();
                Hooks.Apply();
                ModOptions.Register();

                //Crypto.secretHandler = new();
                //CustomManager.Subscribe("meadowvoicehs", Crypto.secretHandler);

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

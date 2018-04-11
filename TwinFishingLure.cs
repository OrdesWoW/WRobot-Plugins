using System;

using System.ComponentModel;
using System.Threading;
using System.IO;
using System.Configuration;

using robotManager.Helpful;
using robotManager.Products;

using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

public class Main : wManager.Plugin.IPlugin
{
    private bool isRunning;
    private BackgroundWorker pulseThread;
    private static WoWLocalPlayer Me = ObjectManager.Me;

    public void Start()
    {
        pulseThread = new BackgroundWorker();
        pulseThread.DoWork += Pulse;
        pulseThread.RunWorkerAsync();
    }

    public void Pulse(object sender, DoWorkEventArgs args)
    {
        try
        {
            while (isRunning)
            {
                if (!Products.InPause && Products.IsStarted)
                {
                    if (!Lua.LuaDoString<bool>("a = GetWeaponEnchantInfo(); return a;"))
                    {
                        Lua.LuaDoString("for bag = 0, 4, 1 do for slot = 1, 16, 1 do local name = GetContainerItemLink(bag, slot); if name and string.find(name, \""+ TwinFishingLureSettings.CurrentSetting.Lure +"\") then UseContainerItem(bag, slot); PickupInventoryItem(16); end; end; end");
                        Thread.Sleep(Usefuls.Latency + 6000);
                    }
                }
                Thread.Sleep(1000);
            }
        }
        catch (Exception ex)
        {
            Logging.WriteError("TwinFishingLureSettings > Pulse(): " + ex);
        }
    }

    public void Dispose()
    {
        try
        {
            isRunning = false;
        }
        catch (Exception e)
        {

        }
    }

    public void Initialize()
    {
        isRunning = true;
        TwinFishingLureSettings.Load();
        Start();
    }

    public void Settings()
    {
        TwinFishingLureSettings.Load();
        TwinFishingLureSettings.CurrentSetting.ToForm();
        TwinFishingLureSettings.CurrentSetting.Save();
        Logging.Write("[TwinFishingLure] Settings saved.");
    }
}

public class TwinFishingLureSettings : Settings
{
    public TwinFishingLureSettings()
    {
        Lure = "Shiny Bauble";
    }

    public static TwinFishingLureSettings CurrentSetting { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("TwinFishingLure", ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("TwinFishingLureSettings > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("TwinFishingLure", ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting =
                    Load<TwinFishingLureSettings>(AdviserFilePathAndName("TwinFishingLure",
                                                                 ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new TwinFishingLureSettings();
        }
        catch (Exception e)
        {
            Logging.WriteError("TwinFishingLureSettings > Load(): " + e);
        }
        return false;
    }

    [Setting]
    [Category("Settings")]
    [DisplayName("Lure")]
    [Description("Name of the lure to use")]
    public string Lure { get; set; }
}


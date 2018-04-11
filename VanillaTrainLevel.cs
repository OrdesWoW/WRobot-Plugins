using System;
using System.Threading;
using robotManager.Helpful;
using robotManager.Products;
using wManager.Plugin;
using wManager.Wow.Helpers;

public class Main : IPlugin
{
    private bool _isLaunched;

    public void Initialize()
    {
        var l = new System.Collections.Generic.List<uint> { 2, 4, 6, 8, 10, 14, 16, 20, 26, 30, 36, 40, 46, 50, 52, 54, 56, 58, 60 };
        _isLaunched = true;
        Logging.Write("[VanillaTrainLevel] Started.");

        while (_isLaunched && Products.IsStarted)
        {
            try
            {
                if (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause)
                {
		    Logging.Write("[VanillaTrainLevel] Started.");
                    wManager.wManagerSetting.CurrentSetting.TrainNewSkills = l.Contains(wManager.Wow.ObjectManager.ObjectManager.Me.Level);
                }
            }
            catch (Exception e)
            {
                Logging.WriteError("[VanillaTrainLevel]: " + e);
            }
            Thread.Sleep(550);
        }
    }

    public void Dispose()
    {
        _isLaunched = false;
        Logging.Write("[VanillaTrainLevel] Stoped.");
    }

    public void Settings()
    {
        Logging.Write("[VanillaTrainLevel] No setting.");
    }
}
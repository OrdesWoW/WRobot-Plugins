using System;
using System.ComponentModel;
using System.Configuration;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Threading;
using MemoryRobot;

using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using robotManager.Products;
using wManager;
using wManager.Plugin;
using wManager.Wow.Bot.States;
using wManager.Wow.Bot.Tasks;
using wManager.Wow.Class;
using wManager.Wow.Enums;
using wManager.Wow.Helpers;
using wManager.Wow.Helpers.FightClassCreator;
using wManager.Wow.ObjectManager;

using System.Windows.Forms;
using Math = robotManager.Helpful.Math;
using Timer = robotManager.Helpful.Timer;

public class Main : wManager.Plugin.IPlugin
{
    private Random randomizer;
    private Stopwatch timer;
    private bool isRunning;
    private int nextJump;
    private int nextJumpTimes;

    private static WoWLocalPlayer Me = ObjectManager.Me;
    private BackgroundWorker pulseThread;

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
            nextJump = randomizer.Next(
                RandomJumpSettings.CurrentSetting.MinRandomJumpTime,
                RandomJumpSettings.CurrentSetting.MaxRandomJumpTime + 1);
            nextJumpTimes = randomizer.Next(
                RandomJumpSettings.CurrentSetting.MinRandomJumps,
                RandomJumpSettings.CurrentSetting.MaxRandomJumps + 1);
            timer.Start();

            while (isRunning)
            {
                if (timer.Elapsed.TotalMilliseconds > nextJump)
                {
                    if (!Me.IsFlying && (!RandomJumpSettings.CurrentSetting.NotDuringCasting || !Me.IsCast) && (!RandomJumpSettings.CurrentSetting.OutOfCombatOnly || !Me.InCombat))
                    {
                        for (int i = 0; i < nextJumpTimes; i++)
                        {
                            Move.JumpOrAscend(Move.MoveAction.PressKey, 50);
                            Thread.Sleep(1000);
                        }

                        nextJump = randomizer.Next(
                        RandomJumpSettings.CurrentSetting.MinRandomJumpTime,
                        RandomJumpSettings.CurrentSetting.MaxRandomJumpTime + 1);
                        nextJumpTimes = randomizer.Next(
                        RandomJumpSettings.CurrentSetting.MinRandomJumps,
                        RandomJumpSettings.CurrentSetting.MaxRandomJumps + 1);
                        timer.Restart();
                    }
                }
                Thread.Sleep(50);
            }
        }
        catch (Exception e)
        {
            
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
        randomizer = new Random();
        timer = new Stopwatch();
        isRunning = true;

        RandomJumpSettings.Load();
        Start();
    }

    public void Settings()
    {
        RandomJumpSettings.Load();
        RandomJumpSettings.CurrentSetting.ToForm();
        RandomJumpSettings.CurrentSetting.Save();
    }
}

public class RandomJumpSettings : Settings
{
    public RandomJumpSettings()
    {
        MinRandomJumpTime = 10000;
        MaxRandomJumpTime = 25000;
        MinRandomJumps = 1;
        MaxRandomJumps = 2;
        OutOfCombatOnly = true;
        NotDuringCasting = true;
    }

    public static RandomJumpSettings CurrentSetting { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("RandomJumper", ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteDebug("RandomJumpSettings => Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("RandomJumper", ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting =
                    Load<RandomJumpSettings>(AdviserFilePathAndName("RandomJumper",
                                                                    ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new RandomJumpSettings();
        }
        catch (Exception e)
        {
            Logging.WriteDebug("RandomJumps => Load(): " + e);
        }
        return false;
    }

    [Setting]
    [Category("Settings")]
    [DisplayName("Random Minimum Milliseconds")]
    [Description("Minimum time passed since last jump, in milliseconds.")]
    public int MinRandomJumpTime { get; set; }

    [Setting]
    [Category("Settings")]
    [DisplayName("Random Maximum Milliseconds")]
    [Description("Maximum time passed since last jump, in milliseconds.")]
    public int MaxRandomJumpTime { get; set; }

    [Setting]
    [Category("Settings")]
    [DisplayName("Random Minimum Jumps")]
    [Description("Minimum jumps in a row.")]
    public int MinRandomJumps { get; set; }

    [Setting]
    [Category("Settings")]
    [DisplayName("Random Maximum Jumps")]
    [Description("Maximum jumps in a row.")]
    public int MaxRandomJumps { get; set; }

    [Setting]
    [Category("Settings")]
    [DisplayName("Only jump out of combat.")]
    [Description("Only jump out of combat.")]
    public bool OutOfCombatOnly { get; set; }

    [Setting]
    [Category("Settings")]
    [DisplayName("Dont jump during casting.")]
    [Description("Dont jump during casting.")]
    public bool NotDuringCasting { get; set; }
}


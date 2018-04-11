using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;
using System.Diagnostics;
using System.Threading;

using robotManager.Helpful;
using robotManager.Products;
using System.Configuration;
using System.IO;
using System.Runtime.Remoting.Messaging;
using wManager;
using wManager.Wow;
using wManager.Wow.Class;
using wManager.Wow.ObjectManager;
using wManager.Wow.Helpers;
using Timer = robotManager.Helpful.Timer;
public class Main : wManager.Plugin.IPlugin
{
    private bool isRunning;
    private Stance _pullStance;
    private Stance _usualStance;
    private Stance _healthStance;
    private Stance _condStance;
    private condType _condType;

    public Timer spamTimer = new Timer();
    public Timer pullingTimer = new Timer();
    public Spell BattleStanceSpell = new Spell("Battle Stance");
    public Spell DefensiveStanceSpell = new Spell("Defensive Stance");
    public Spell BerserkerStanceSpell = new Spell("Berserker Stance");


    private void FightEventsOnOnFightLoop(WoWUnit woWUnit, CancelEventArgs cancelable)
    {
        if (ObjectManager.Me.HealthPercent <= WarriorHelperSettings.CurrentSetting.healthStance[0] && pullingTimer.IsReady && woWUnit.IsValid && woWUnit.IsAlive)
        {
            StanceLauncher(_healthStance);
        }
        else if (ObjectManager.GetNumberAttackPlayer() >= 3 && pullingTimer.IsReady && woWUnit.IsValid && woWUnit.IsAlive && _condType.Equals(condType.threeEnemies))
        {
            StanceLauncher(_condStance);
        }
        else if (ObjectManager.Target.GetDistance > 7 && pullingTimer.IsReady && woWUnit.IsAlive && woWUnit.IsValid &&
                 _condType.Equals(condType.distanceSeven))
        {
            StanceLauncher(_condStance);
        }
        else if (pullingTimer.IsReady && woWUnit.IsValid && woWUnit.IsAlive)
        {
            StanceLauncher(_usualStance);
        }
    }

    private void FightEventsOnOnFightStart(WoWUnit woWUnit, CancelEventArgs cancelable)
    {
        if (woWUnit.IsValid && woWUnit.IsAlive && !ObjectManager.Me.InCombat)
        {
            StanceLauncher(_pullStance);
            pullingTimer = new Timer(1100 * 2);
        }
    }

    public void Start()
    {
        TransformSettings();
        while (ObjectManager.Me.IsAlive
               && Conditions.InGameAndConnectedAndProductStartedNotInPause)
        {
            if (WarriorHelperSettings.CurrentSetting.useWM)
                WeaponManager();
        }
        Thread.Sleep(350);
    }


    public void Dispose()
    {
        try
        {
            isRunning = false;
            wManager.Events.FightEvents.OnFightLoop -= FightEventsOnOnFightLoop;
            wManager.Events.FightEvents.OnFightLoop -= FightEventsOnOnFightStart;
        }
        catch { }
    }

    public void Initialize()
    {
        isRunning = true;
        WarriorHelperSettings.Load();
        WarriorHelperSettings.CurrentSetting.Save();
        if (WarriorHelperSettings.CurrentSetting.useSM)
        {
            wManager.Events.FightEvents.OnFightStart += FightEventsOnOnFightStart;
            wManager.Events.FightEvents.OnFightLoop += FightEventsOnOnFightLoop;
        }
        Start();
    }

    public void Settings()
    {
        WarriorHelperSettings.Load();
        WarriorHelperSettings.CurrentSetting.ToForm();
        WarriorHelperSettings.CurrentSetting.Save();
    }

    public void TransformSettings()
    {
        _pullStance = GetStanceSettings(WarriorHelperSettings.CurrentSetting.pullStance);
        _usualStance = GetStanceSettings(WarriorHelperSettings.CurrentSetting.usualStance);
        _healthStance = GetStanceSettings(WarriorHelperSettings.CurrentSetting.healthStance[1]);
        _condStance = GetStanceSettings(WarriorHelperSettings.CurrentSetting.conditionalStance[1]);
        if (WarriorHelperSettings.CurrentSetting.conditionalStance[0] == 1)
        {
            _condType = condType.threeEnemies;
        }
        else if (WarriorHelperSettings.CurrentSetting.conditionalStance[0] == 2)
        {
            _condType = condType.distanceSeven;
        }
        else
        {
            _condType = condType.disabled;
        }
    }

    public Stance GetStanceSettings(int stanceNumber)
    {
        switch (stanceNumber)
        {
            case 1:
                return Stance.Battle;
            case 2:
                return Stance.Defensive;
            case 3:
                return Stance.Berserker;
            default:
                return Stance.Battle;
        }
    }

    public void StanceLauncher(Stance stance)
    {
        if (stance == Stance.Battle && !ObjectManager.Me.HaveBuff(BattleStanceSpell.Id) && BattleStanceSpell.KnownSpell)
        {
            BattleStanceSpell.Launch();
            Logging.WriteDebug("WarriorHelper.StanceManager.BattleStance: ");
            spamTimer = new Timer(50);
            return;
        }
        else if (stance == Stance.Defensive && !ObjectManager.Me.HaveBuff(DefensiveStanceSpell.Id) && DefensiveStanceSpell.KnownSpell)
        {
            DefensiveStanceSpell.Launch();
            Logging.WriteDebug("WarriorHelper.StanceManager.DefensiveStance: ");
            spamTimer = new Timer(50);
            return;
        }
        else if (stance == Stance.Berserker && !ObjectManager.Me.HaveBuff(BerserkerStanceSpell.Id) && BerserkerStanceSpell.KnownSpell)
        {
            BerserkerStanceSpell.Launch();
            Logging.WriteDebug("WarriorHelper.StanceManager.BerserkerStance: ");
            spamTimer = new Timer(50);
            return;
        }
    }

    public enum condType
    {
        threeEnemies,
        distanceSeven,
        disabled
    }
    public enum Stance
    {
        Battle,
        Defensive,
        Berserker
    }
    public void GetWeapon(Stance stance)
    {
        switch (stance)
        {
            case Stance.Battle:
                EquipTwoHand();
                break;
            case Stance.Defensive:
                EquipOneHand();
                break;
            case Stance.Berserker:
                if (WarriorHelperSettings.CurrentSetting.dualWield == null)
                    EquipTwoHand();
                else
                    EquipOneHandBerserker();
                break;
        }
    }

    public void EquipOneHand()
    {
        var weapon = EquippedItems.GetEquippedItems();
        foreach (var oh in weapon)
        {
            if (oh.Name == WarriorHelperSettings.CurrentSetting.oneHand)
            {
                return;
            }
        }
        try
        {
            ItemsManager.EquipItemByName(WarriorHelperSettings.CurrentSetting.oneHand);
            ItemsManager.EquipItemByName(WarriorHelperSettings.CurrentSetting.shield);
        }
        catch (Exception e)
        {
            Logging.WriteDebug("WarriorHelper.WeaponManager.Defensive: " + e);
            throw;
        }
    }

    public void EquipTwoHand()
    {
        var weapon = EquippedItems.GetEquippedItems();
        foreach (var th in weapon)
        {
            if (th.Name == WarriorHelperSettings.CurrentSetting.twoHand)
            {
                return;
            }
        }
        try
        {
            ItemsManager.EquipItemByName(WarriorHelperSettings.CurrentSetting.twoHand);
        }
        catch (Exception e)
        {
            Logging.WriteDebug("WarriorHelper.WeaponManager.Battle: " + e);
            throw;
        }
    }

    public void EquipOneHandBerserker()
    {
        try
        {
            ItemsManager.EquipItemByName(WarriorHelperSettings.CurrentSetting.dualWield);
            ItemsManager.EquipItemByName(WarriorHelperSettings.CurrentSetting.dualWield2);
        }
        catch (Exception e)
        {
            Logging.WriteDebug("WarriorHelper.WeaponManager.Berserker: " + e);
            throw;
        }
    }
    public void WeaponManager()
    {
        if (ObjectManager.Me.HaveBuff(BattleStanceSpell.Id))
        {
            GetWeapon(Stance.Battle);
        }
        else if (ObjectManager.Me.HaveBuff(DefensiveStanceSpell.Id))
        {
            GetWeapon(Stance.Defensive);
        }
        else if (ObjectManager.Me.HaveBuff(BerserkerStanceSpell.Id))
        {
            GetWeapon(Stance.Berserker);
        }
    }
}

public class WarriorHelperSettings : Settings
{
    public static WarriorHelperSettings CurrentSetting { get; set; }
    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("WarriorHelperSettings", ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("WarriorHelperSettings > Save(): " + e);
            return false;
        }
    }
    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("WarriorHelperSettings", ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting =
                    Load<WarriorHelperSettings>(AdviserFilePathAndName("WarriorHelperSettings",
                        ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new WarriorHelperSettings()
            {
                twoHand = "Vicious Gladiator's Decapitator",
                oneHand = "Vicious Gladiator's Hacker",
                shield = "Vicious Gladiator's Shield Wall",
                dualWield = "First Weapon",
                dualWield2 = "Secondary Weapon",
                useWM = true,
                useSM = true,
                pullStance = 1,
                usualStance = 1,
                healthStance = new List<int>() { 35, 2 },
                conditionalStance = new List<int>() { 2, 3 }

            };
        }
        catch (Exception e)
        {
            Logging.WriteError("WarriorHelperSettings > Load(): " + e);
        }
        return false;
    }

    [Setting]
    [Category("Weapon Manager")]
    [DisplayName("[ARM] 2 hand name")]
    [Description("put the name of your 2H weapon for Battle stance")]
    public string twoHand { get; set; }
    [Setting]
    [Category("Weapon Manager")]
    [DisplayName("Use Weapon Manager")]
    [Description("disable or active Weapon Manager")]
    public bool useWM { get; set; }
    [Setting]
    [Category("Weapon Manager")]
    [DisplayName("[PROT] 1 hand name")]
    [Description("put the name of your 1H weapon for defensive stance")]
    public string oneHand { get; set; }
    [Setting]
    [Category("Weapon Manager")]
    [DisplayName("[PROT] shield name")]
    [Description("put the name of your shield for defensive stance")]
    public string shield { get; set; }

    [Setting]
    [Category("Weapon Manager")]
    [DisplayName("[FURY] dual Wield name")]
    [Description("first Weapon")]
    public string dualWield { get; set; }

    [Setting]
    [Category("Weapon Manager")]
    [DisplayName("[FURY] dual Wield name")]
    [Description("secondary Weapon")]
    public string dualWield2 { get; set; }

    [Setting]
    [Category("Stance Manager")]
    [DisplayName("Use Stance Manager")]
    [Description("Disable or active Stance Manager")]
    public bool useSM { get; set; }

    [Setting]
    [Category("Stance Manager")]
    [DisplayName("Pull Stance -- Not in combat")]
    [Description("1 : Battle, 2: Defensive, 3: Berserker")]
    public int pullStance { get; set; }

    [Setting]
    [Category("Stance Manager")]
    [DisplayName("Usual Stance in combat")]
    [Description("1 : Battle, 2: Defensive, 3: Berserker")]
    public int usualStance { get; set; }

    [Setting]
    [Category("Stance Manager")]
    [DisplayName("Conditional Health% stance")]
    [Description("Conditional Stance active on Healt percent, set 0 to disable it. then 1 : Battle, 2: Defensive, 3: Berserker")]
    public List<int> healthStance { get; set; }

    [Setting]
    [Category("Stance Manager")]
    [DisplayName("Conditional stance with some option -- 1 : more than 2 mobs, 2 : distance to mobs > 7 yard")]
    [Description("Conditional Stance, set 0 disable it. then 1 : Battle, 2: Defensive, 3: Berserker")]
    public List<int> conditionalStance { get; set; }

    //add condtional stance list of spellID
}


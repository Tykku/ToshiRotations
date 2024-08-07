namespace DefaultRotations.Magical;

[Rotation("Toshi", CombatType.PvE, GameVersion = "7.00")]
[SourceCode(Path = "main/DefaultRotations/Magical/RDM_Toshi.cs")]
[Api(2)]
public sealed class RDM_Toshi : RedMageRotation
{
    #region Config Options
    private static BaseAction VerthunderStartUp { get; } = new BaseAction(ActionID.VerthunderPvE, false);

    [RotationConfig(CombatType.PvE, Name = "Use Vercure for Dualcast when out of combat.")]
    public bool UseVercure { get; set; }
    
    [RotationConfig(CombatType.PvE, Name = "Cast Reprise when moving with no instacast.")]
    public bool RangedSwordplay { get; set; } = false;
    
    [RotationConfig(CombatType.PvE, Name = "DO NOT CAST EMBOLDEN/MANAFICATION OUTSIDE OF MELEE RANGE, I'M SERIOUS YOU HAVE TO MOVE UP FOR IT TO WORK IF THIS IS ON.")]
    public bool AnyonesMeleeRule { get; set; } = false;
    #endregion

    #region Countdown Logic
    protected override IAction? CountDownAction(float remainTime)
    {
        if (remainTime < VerthunderStartUp.Info.CastTime + CountDownAhead
            && VerthunderStartUp.CanUse(out var act)) return act;

        //Remove Swift
        StatusHelper.StatusOff(StatusID.Dualcast);
        StatusHelper.StatusOff(StatusID.Acceleration);
        StatusHelper.StatusOff(StatusID.Swiftcast);

        return base.CountDownAction(remainTime);
    }
    #endregion

    #region oGCD Logic
    protected override bool EmergencyAbility(IAction nextGCD, out IAction? act)
    {
        bool AnyoneInRange = AllHostileTargets.Any(hostile => hostile.DistanceToPlayer() <= 4);
        bool doubleMeleeProtection = (!ResolutionPvE.EnoughLevel && IsLastGCD(ActionID.ScorchPvE)) || (!ScorchPvE.EnoughLevel && (IsLastGCD(ActionID.VerholyPvE) || IsLastGCD(ActionID.VerfirePvE)));

        act = null;
        if (CombatElapsedLess(4)) return false;
        if (!AnyonesMeleeRule)
        {
            if (IsBurst && HasHostilesInRange && EmboldenPvE.CanUse(out act, skipAoeCheck: true)) return true;

        }
        
        if (IsBurst && AnyoneInRange && EmboldenPvE.CanUse(out act, skipAoeCheck: true)) return true;

        //Use Manafication after embolden.
        if ((Player.HasStatus(true, StatusID.Embolden) || IsLastAbility(ActionID.EmboldenPvE))
            && !doubleMeleeProtection && ManaficationPvE.CanUse(out act)) return true;

        return base.EmergencyAbility(nextGCD, out act);
    }

    protected override bool AttackAbility(IAction nextGCD, out IAction? act)
    {
        //Swift
        if (ManaStacks == 0 && (BlackMana < 50 || WhiteMana < 50)
            && (CombatElapsedLess(4) || !ManaficationPvE.EnoughLevel || !ManaficationPvE.Cooldown.WillHaveOneChargeGCD(0, 1)))
        {
            if (InCombat && !Player.HasStatus(true, StatusID.VerfireReady, StatusID.VerstoneReady))
            {
                if (SwiftcastPvE.CanUse(out act)) return true;
                if (AccelerationPvE.CanUse(out act, usedUp: true)) return true;
            }
        }

        if (IsBurst && UseBurstMedicine(out act)) return true;

        //Attack abilities.
        if (ViceOfThornsPvE.CanUse(out act, skipAoeCheck: true)) return true;
        if (PrefulgencePvE.CanUse(out act, skipAoeCheck: true)) return true;
        if (ContreSixtePvE.CanUse(out act, skipAoeCheck: true)) return true;
        if (FlechePvE.CanUse(out act)) return true;

        if (EngagementPvE.CanUse(out act, usedUp: true)) return true;
        if (CorpsacorpsPvE.CanUse(out act) && !IsMoving) return true;

        return base.AttackAbility(nextGCD, out act);
    }
    #endregion

    #region GCD Logic

    protected override bool EmergencyGCD(out IAction? act)
    {
        bool inACombo = (ResolutionPvE.EnoughLevel && IsLastGCD(ActionID.ScorchPvE)) || (ScorchPvE.EnoughLevel && (IsLastGCD(ActionID.VerholyPvE) || IsLastGCD(ActionID.VerfirePvE)));
        bool meleeReady = BlackMana >= 50 && WhiteMana >= 50 || Player.HasStatus(true, StatusID.MagickedSwordplay);
        
        // Hardcode Resolution & Scorch to avoid double melee without finishers
        if (ResolutionPvE.CanUse(out act, skipAoeCheck: true)) return true;
        if (ScorchPvE.CanUse(out act, skipAoeCheck: true)) return true;
        
        /*if (IsLastGCD(ActionID.ScorchPvE))
        {
            if (ResolutionPvE.CanUse(out act, skipStatusProvideCheck: true, skipAoeCheck: true)) return true;
        }
        
        if (IsLastGCD(ActionID.VerholyPvE, ActionID.VerflarePvE))
        {
            if (ScorchPvE.CanUse(out act, skipStatusProvideCheck: true, skipAoeCheck: true)) return true;
        }*/
            
        if (ManaStacks == 3)
        {
            if (BlackMana > WhiteMana)
            {
                if (VerholyPvE.CanUse(out act, skipAoeCheck: true)) return true;
            }
            
            if (VerflarePvE.CanUse(out act, skipAoeCheck: true)) return true;
        }

        if (IsLastGCD(true, MoulinetPvE) && MoulinetPvE.CanUse(out act, skipAoeCheck: true)) return true;
        
        if (RedoublementPvE.CanUse(out act)) return true;
        if (ZwerchhauPvE.CanUse(out act)) return true;


        // Start your Melee
        if (meleeReady && CanStartMeleeCombo && MoulinetPvE.CanUse(out act)) return true;
        if (meleeReady && CanStartMeleeCombo && RipostePvE.CanUse(out act)) return true;
        if (ManaStacks > 0 && ManaStacks < 3 && !inACombo && RipostePvE.CanUse(out act)) return true;
        

        if (IsMoving && RangedSwordplay && (ReprisePvE.CanUse(out act) || EnchantedReprisePvE.CanUse(out act))) return true;

        return base.EmergencyGCD(out act);
    }

    protected override bool GeneralGCD(out IAction? act)
    {
        act = null;
        if (ManaStacks == 3) return false;
        
        if (GrandImpactPvE.CanUse(out act)) return true;
        
        if (!VerthunderIiPvE.CanUse(out _))
        {
            if (VerfirePvE.CanUse(out act)) return true;
            if (VerstonePvE.CanUse(out act)) return true;
        }

        if (ScatterPvE.CanUse(out act)) return true;
        
        if (WhiteMana < BlackMana)
        {
            if (VeraeroIiPvE.CanUse(out act) && BlackMana - WhiteMana != 5) return true;
            if (VeraeroPvE.CanUse(out act) && BlackMana - WhiteMana != 6) return true;
        }
        if (VerthunderIiPvE.CanUse(out act)) return true;
        if (VerthunderPvE.CanUse(out act)) return true;

        if (JoltPvE.CanUse(out act)) return true;

        if (UseVercure && NotInCombatDelay && VercurePvE.CanUse(out act)) return true;

        return base.GeneralGCD(out act);
    }
    #endregion

    #region Extra Methods
    private bool CanStartMeleeCombo
    {
        get
        {
            if (Player.HasStatus(true, StatusID.Embolden) && Player.HasStatus(true, StatusID.Manafication) && (Player.HasStatus(true, StatusID.MagickedSwordplay) || BlackMana == 100 || WhiteMana == 100)) return true;
                
            if (BlackMana == WhiteMana) return false;
            
            else if (WhiteMana < BlackMana)
            {
                if (Player.HasStatus(true, StatusID.VerstoneReady)) return false;
            }
            else
            {
                if (Player.HasStatus(true, StatusID.VerfireReady)) return false;
            }

            if (Player.HasStatus(true, VercurePvE.Setting.StatusProvide ?? [])) return false;

            //Waiting for embolden.
            if (EmboldenPvE.EnoughLevel && EmboldenPvE.Cooldown.WillHaveOneChargeGCD(5)) return false;

            return true;
        }
    }
    #endregion
}

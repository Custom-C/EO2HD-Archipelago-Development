namespace EO2HD_Patcher.Patcher;

/// <summary>
/// PatcherSettings is the single data item sent to the Patcher that contains the information obtained from MainWindow.
/// </summary>
internal class PatcherSettings
{
    public ArchiSettings baseSettings {get; set;}
    public RandomizerSettings randomSettings {get; set;}
    public QOLSettings qolSettings {get; set;}

}


internal class ArchiSettings
{
    //basic settings
    public bool Enabled {get; set;}
    public DeathLinkModeEnum DeathLink {get; set;}
    public Goals Goals {get; set;}
    
    //item settings
    public bool ProLvlCap {get; set;}
    public bool ProStratumAccess {get; set;}
    public bool ProShop {get; set;}
    public bool ProBag {get; set;}
    public bool ProMedals {get; set;}
    public bool ClassLock {get; set;}
    public FillerPackages FillerPackages {get; set;}
    public ShopUnlockMode ShopUnlockMode {get; set;}
    public int ProBagIndex { get; set; } = 0;
    public int ClassLockNum { get; set; } = 0;
    
    //location check settings
    public bool CheckQuests {get; set;}
    public bool CheckGlossary {get; set;}
    public bool CheckArbitraryGlossary {get; set;}
    public bool CheckScaledArbitraryGlossary {get; set;}
    public bool CheckFloors {get; set;}
    public bool CheckLevels {get; set;}
    public bool CheckBosses {get; set;}
    public int ArbitraryGlossaryIndex { get; set; } = 0;
    public int LevelMilestoneIndex { get; set; } = 0;
    
    //hardcoded arrays for the ChoiceBoxes. The Tags correspond to the index of the array found here.
    
    //floats representing the percentage thresholds where glossary checks are given if Arbitrary Glossary Goals are enabled.
    public static readonly float[] CheckArbiGlosVal = { 0.01f, 0.02f, 0.05f, 0.1f, 0.2f};
    //ints representing the increase in bag space per item with Progressive Bag enabled.
    public static readonly int[] ProBagVal = { 0, 20, 10, 5, 2, 1 };
    //ints representing the level intervals where level milestone checks are given.
    public static readonly int[] CheckLvlMilesVal = { 1, 2, 5, 7, 10, 12, 35, 70 };
}

internal class RandomizerSettings
{
    //basic settings.
    public bool LevelCaps {get; set;}
    
    //hardcoded arrays for the ChoiceBoxes. The Tags correspond to the index of the array found here.
    
    //floats representing the EXP multiplier to be applied to all enemies
    public static readonly float[] ExpMultipliers = { 1f, 1.5f, 2f, 3f, 4f, 5f, 10f };
    public int ExpMultIndex { get; set; } = 0;
    
    //ints representing the level cap per strata. 
    //Notably, index 0 is -1 to indicate 'no level caps'. 
    public static readonly int[] LevelCapArray = { -1, 5, 10, 12, 15 };
    public int LevelCapIndex { get; set; } = 0;
}

internal class QOLSettings
{
    //basic settings
    public bool FOEsGiveExperience { get; set; } = true; //FOEs give experience on kill.
    public bool ConditionalsAlwaysDrop { get; set; } = true; //Conditionals, when met, are guaranteed to drop.
    public bool ItemsAlwaysDrop { get; set; } = true; //All non-conditionals are guaranteed to drop.
    public bool NoConditionals { get; set; } = false; //Removes all conditions from conditionals.
    public bool Level99Cap { get; set; } = true; //Sets the hard level cap to 99.
    public bool FreeRests { get; set; } = true; //Resting is free
    public bool FreeRetires { get; set; }= false; //Retiring is free
    public bool NerfMuckdile { get; set; }= true; //Muckdile gets some nerfs
    
    //internal bool array for the planned class reworks.
    //The index is in order of appearance in the XAML, i.e.
    //L, S, P, M, A, T, B, others
    public bool[] ClassReworks { get; set; } = { false, false, false, false, false, false, false, false };


}
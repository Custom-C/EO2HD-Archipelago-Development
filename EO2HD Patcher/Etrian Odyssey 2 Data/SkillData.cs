using System.Collections;
using System.IO;
using Microsoft.Win32;
using Newtonsoft.Json; //Didn't want to have to do this, but my only other option was a manual bitstream...
                       
namespace EO2HD_Patcher.Etrian_Odyssey_2_Data;

//Details about skills.
//
//Because some of these are changed in HD they had to be made into Unity assets, so we can export the .JSONs used.
//That doesn't mean they're simple to decode, though. See skill_decoding.txt for details.

    
    //an abstract class that contains the details for both player and enemy skills.
public abstract class SkillData 
{
    protected string _skillName;   //Japanese name, as found in the JSON.
    protected int _skillID;       //unique ID for each skill.
    protected int _skill_pattern; //No clue what this is yet.
    protected ushort _need_parts; //Parts used, if any. Head = 1, Arm = 2, Leg = 3.

    protected int[] _req_mastery; //Prerequisite weapon mastery to use the skill
    /*Currently, I know:
        1: Axes
        2: Swords
        3: Bows
        4: Shields
        5: Whips
        6: Overhead
        7: Seigan
        8: Iai
        9: Fire Up
        10: Ice Up
        11: Volt Up
        12: Phys Up
        13: War Lore
        14: Songs
        15: Curses
        16: Guns
        17: Katana(?)
        
        
        Enemy skills have this entry as well, but it's an empty array.
    */
    protected ushort _target_area; //Determines valid targets for the ability.
    /* Currently known with 100% certainty:
      5: Self-targetted.

     */
    protected ushort _target_side; //Determines the side of the field (enemy or ally) the ability targets
    //For player skills, 0 is party, 1 is enemy.
    protected ushort _use_scene; //The screen an ability can be used on. 0 = cannot use (i.e. passives or enemy skills), 2 = Can be used only out of combat, 3 = Can only be used in combat, 6 = Can be used both in and out of combat. Notably, Medic's Refresh is the only status cure option that is type 6 (and is the only one that can cure petrification)
    protected ushort _ud_efficacy_type; //no clue.
    protected ushort _ud_efficacy_afbit; //also no clue, but clearly related.
    protected BitArray _bitmask = new BitArray(32); //Bitmask for determining certain effects. The most common is the element the skill deals.
    /* Bits known:
          0: This skill interacts with Cut damage
          1: This skill interacts with Bash damage
          2: This skill interacts with Stab damage
          3: This skill interacts with Fire damage
          4: This skill interacts with Ice damage
          5: This skill interacts with Volt damage
          6: This skill interacts with Head Bind
          7: This skill interacts with Arm Bind
          8: This skill interacts with Leg Bind
          9: This skill interacts with Instant Death
          10: This skill interacts with Petrification
          11: This skill interacts with Sleep
          12: This skill interacts with Paralysis?
          15: This skill interacts with Poison
          21: This skill reacts to an attack against it?
     */

    public override string ToString()
    {
        string toReturn = "";
        toReturn += SkillLocalizer.GetSkillName(_skillID);
        toReturn += ":\n\tSkill Pattern: " + _skill_pattern;
        toReturn += "\n\tUses scene: " + _use_scene;
        string bitString = "";
        //bitmask. I already know bits 0-11, so we just focus from 12 onward.
        for (int i = 12; i < _bitmask.Length; i++)
        {
            if (_bitmask[i])
            {
                if (bitString == "")
                    bitString = "\n\tUnknown Bits: " + i;
                else
                    bitString += ", " + i;
            }
        }
        toReturn += bitString;
        //ud-type.
        toReturn += "\n\tUB Type " + _ud_efficacy_type + ", afbit " + _ud_efficacy_afbit;
        //parts needed if any
        if (_need_parts > 0)
        {
            toReturn += "\n\tNeeds ";
            toReturn += _need_parts switch
            {
                1 => "the Head", 2 => "the Arms", 3 => "the Legs", _ => "an unknown part"
            };
        }
        //currently, any other values will have to be handled in the non-abstract classes.
        return toReturn;
    }
}

//Class that contains the relevant information for Player Skills specifically.
public class PlayerSkillData : SkillData
{
    private int _skillMaxLevel; //Max amount of skill points allowed to be allocated to the skill
    
    //This is a list of flags, of which the index referenced is (the skill's level - 1).
    //Despite the max level of a skill not always being 10, these always have 10 values.
    private Dictionary<int, int[]> _typeValues = new Dictionary<int, int[]>();
    
    //Derived values from the above _typeValues:
    private int[] TPCosts; //type 1 is their TP cost per level.
    private int[] PercentValues; //type 2 is the percentage values of the effect per level.
    private int[] ActionSpeeds; //type 3 is the action speed as a percentage.
    private int[] Accuracy; //type 4 is the accuracy as a percentage.
    private int[] MultiVar1; //type 5 is used as a multi-use variable.
    private int[] ThreeWeightedOdds; //Type 6 describes the weighted odds of a multi-hit skill with three weighted odds of # of hits.
    private int[] FourWeightedOdds; //Type 7 seems to be like 6, but for four weighted odds.
    private int[] OddsToActivate; //type 8 is the odds of a skill activating.
    private int[] OddsReductionPerActivation; //type 9 is how much type 8 is reduced by after each activation
    private int[] HealingPower; //type 10 is typically the Healing Power of a healing skill. However, if it's 1-3, it's actually how many limbs it can unbind, with no healing whatsoever.
    private int[] AilmentsHealed; //type 11 is the maximum type of ailment an ailment-healing skill can heal, according to this list: 1:Blind 2:Poison 3:Sleep 4:Fear 5:Paralyze 6:Curse 7:Confuse 8:Petrify 9:??
    private int[] MinimumHits; //type 12 is the minimum number of hits for a multi-hit attack. 
    private int[] GatheringVals; //type 13 is strange in that it's only shared by the gathering skills, are all the same values, and don't really correspond to how many uses the skills grant.
    private int[] TileDurations; //type 14 is exclusive to field effects, and this is how long they last (in steps)
    private int[] MasteryMults; //type 15 applies only to mastery skills, and is how much a mastery-relevant attack is multiplied by at the end.

    private int[] InflictionRates; //type 18 is the rate of inflicting an ailment, if the skill has one.
    private int[] BasePoisonDmg; //type 19 is the base poison damage for skills that inflict Poison.
    
    private int[] SpellPower; //type 21 is the Spell Power of an Alchemist skill. Yes, only Alchemists use this type.
    private int[] ExtraDamagePercent; //type 22 is used for skills with conditional damage % applied to certain targets. It uses this value instead of the one at type 2.
    private int[] MultiVar2; //type 23 is another multi-variable like type 5, this time used for non-combat effects. 
    private int[] DamageToHealPercent; //type 24 is the amount of damage (as a %) that is converted to healing. It applies to both incoming and outgoing damage skills.
    private int[] HPRelevantIndex; //type 25 only effects skills that use the user's HP as a comparison, because those skills all use a static array of values instead of assigned values like type 2. This is an additional number added to the index after the currHP comparison is done.
    
    private int[] BuffsErased; //type 27 for PCs is how many buffs the effect erases. Presumably for *targets*, but in OG 2 these skills only target enemies.

    private int[] StatAdditive; //Type 50 is an additive to a stat. It's used for all the stat++ passives.

    private int[] StatMultiplier; //Type 51 is a multiplier to a stat, expressed as a percentage. It only ever applies to the HP/TP up passives.

    private int[] MechMultiplier; //Type 52 is a multiplier to a mechanical value. Presumably. In 2, it only affects the Escape Rate++ passive.

    private int[] CritAdditive;//type 53 is an additive to the base critical rate of basic attacks.
    private int[] EXPMultiplier; //type 54 is a multiplier to the EXP gained at the end of combat.
    
    private int[] DamageIncreasePercent; //type 100 is a multiplier to damage dealt. Interestingly, the Gunner's increase to damage to themselves while using Risk shots is here as well.
    private int[] EnemyInaccuracyBuffMult; //type 101 is a multiplier to 'evasion', with 100% meaning all enemy attacks hit you. 
    private int[] AggroIncrease; //type 102 is an additive % chance of being targetted. 
    private int[] AGIMultiplier; //type 103 is a multiplier to a character's AGI stat, as a percentage.
    private int[] DamageDecreasePercent; //type 104 is a multiplier to damage taken.
    private int[] MaxHPMultiplier; //type 105 is a multiplier on maximum HP for the duration.
    private int[] TroubDamageIncreasePercent; //Type 106 is essentially a duplicate type 100 for Troubadour's Ifrit/Ymir/Taranis skills. They apply a type 100 buff to allies, but want to do the same effect to enemies (but >100%).
    private int[] CPRActivationRate; //Type 107 only applies to Medic's CPR skill, and is the activation rate.  Perhaps this is needed because the buff dispels itself afterwards?
    private int[] AilmentChanceMult; //type 108 is a multiplier to the success rate of an ailment applied to the target. Only used by Troubadour's Health skill, but it presumably could work with values > 100% as well.
    private int[] AilmentRecoveryAdditive; //type 109 is an additive value to the chance of a character recovering from an ailment each turn. Only used by Troubadour's Recovery, but, again, could presumably work with values > 0.
    private int[] MinimumWeaknessPercent; //type 110 is used by Hexer's Dampen, and is the minimum value a damage resistance value can be when that debuff is applied.
    private int[] DummiedOut; //type 111 is always 0s, even in enemy skills. On the player side, this is only used by Troubadour's Ifrit/Ymir/Taranis skills. 
    
    //constructor
    public PlayerSkillData(PlayerSkillDeserialized incoming)
    {
        //the easy stuff first.
        _skillName = incoming.SkillName;
        _skillID = incoming.SkillNo;
        _skillMaxLevel = incoming.SkillMaxLevel;
        _skill_pattern = incoming.Skill_pattern;
        _need_parts = (ushort)incoming.Need_parts;
        _req_mastery = incoming.use_masterly;
        _target_area = (ushort)incoming.Trg_area;
        _target_side = (ushort)incoming.Trg_side;
        _use_scene = (ushort)incoming.Use_scene;
        _ud_efficacy_type = (ushort)incoming.Ud_efficacy_type;
        _ud_efficacy_afbit = (ushort)incoming.Ud_efficacy_afbit;
        //Types are a bit expensive computationally.
        //WHY DOES RIDER KEEP BREAKING MY FUCKING STYLE
        foreach (TypeValueArray typ in incoming.TypeValue)
        {
            switch (typ.TypeID)
            {
                case 1: { TPCosts = typ.Value; break; }
                case 2: { PercentValues = typ.Value; break; }
                case 3: { ActionSpeeds = typ.Value; break; }
                case 4: { Accuracy = typ.Value; break; } 
                case 5: {MultiVar1 = typ.Value; break; }
                case 6: {ThreeWeightedOdds = typ.Value; break; }
                case 7: {FourWeightedOdds = typ.Value; break; }
                case 8: { OddsToActivate = typ.Value; break; }
                case 9: { OddsReductionPerActivation = typ.Value; break; } 
                case 10: { HealingPower = typ.Value; break; }
                case 11: { AilmentsHealed = typ.Value; break; }
                case 12: { MinimumHits = typ.Value; break; }
                case 13: { GatheringVals = typ.Value; break; }
                case 14: { TileDurations = typ.Value; break; } 
                case 15: { MasteryMults = typ.Value; break; }
                case 18: { InflictionRates = typ.Value; break; } 
                case 19: { BasePoisonDmg = typ.Value; break; }
                case 21: { SpellPower = typ.Value; break; }
                case 22: { ExtraDamagePercent = typ.Value; break; }
                case 23: { MultiVar2 = typ.Value; break; }
                case 24: { DamageToHealPercent = typ.Value; break; }
                case 25: { HPRelevantIndex = typ.Value; break; }
                case 27: { BuffsErased = typ.Value; break; }
                case 50: { StatAdditive = typ.Value; break; }
                case 51: { StatMultiplier = typ.Value; break; }
                case 52: { MechMultiplier = typ.Value; break; }
                case 53: { CritAdditive = typ.Value; break; }
                case 54: { EXPMultiplier = typ.Value; break; }
                case 100: { DamageIncreasePercent = typ.Value; break; }
                case 101: { EnemyInaccuracyBuffMult = typ.Value; break; }
                case 102: { AggroIncrease = typ.Value; break; }
                case 103: { AGIMultiplier = typ.Value; break; }
                case 104: { DamageDecreasePercent = typ.Value; break; }
                case 105: { MaxHPMultiplier = typ.Value; break; }
                case 106: { TroubDamageIncreasePercent = typ.Value; break; }
                case 107: { CPRActivationRate = typ.Value; break; }
                case 108: { AilmentChanceMult = typ.Value; break; }
                case 109: { AilmentRecoveryAdditive = typ.Value; break; }
                case 110: { MinimumWeaknessPercent = typ.Value; break; }
                case 111: { DummiedOut = typ.Value; break; }
                default: { _typeValues[typ.TypeID] = typ.Value; break; } //have to force-add in case of duplicate entries, like Medic having two type 10s.
            }
        }
        //finally, the bitmask. I thought this would be hard, but apparently BitArray has some good syntax for this.
        _bitmask = new BitArray(new[] { incoming.Bit });
        _bitmask.Length = 32; //ensure it's always 32 bits long regardless of Bit size.
    }

    public override string ToString()
    {
        string toReturn = base.ToString();
        //I only know for player skills that 0 is friendly and 1 is enemy.
        //I don't know if this is actually 0 = players, 1 = NPCs.
        toReturn += "\n\tTargets side ";
        toReturn += (_target_side == 0) ? "friendly" : "enemy";
        toReturn += " with area " + _target_area;
        
        //unknown types
        if (_typeValues.Keys.Count > 0)
        {
            toReturn += "\n\tUnknown Types: ";
            string typeString = "";
            foreach (KeyValuePair<int, int[]> key in _typeValues)
            {
               if (typeString == "") { typeString = key.Key.ToString(); }
               else { typeString += ", " + key.Key.ToString(); }
            }
            toReturn += typeString;
        }
        //required mastery. If it's 0, no mastery is required.
        if (_req_mastery[0] != 0)
        {
            toReturn += "\n\tMastery required: " + GetMastery(_req_mastery[0]);
            if (_req_mastery[1] != 0) //if the second entry isn't 0, then two weapon types can be used.
                toReturn += " OR " + GetMastery(_req_mastery[1]);
        }

        toReturn += "\n\tMaximum Rank: " + _skillMaxLevel;
        toReturn += "\nJapanese name: " + _skillName;
        return toReturn;
    }
    
    //tiny function for ToString to use to determine masteries.
    private string GetMastery(int type)
    {
        return type switch
        {
            1 => "Axes",
            2 => "Swords",
            3 => "Bows",
            4 => "Shields",
            5 => "Whips",
            6 => "Overhead",
            7 => "Seigan",
            8 => "Iai",
            9 => "Fire Up",
            10 => "Ice Up",
            11 => "Volt Up",
            12 => "Phys Up",
            13 => "War Lore",
            14 => "Songs",
            15 => "Curses",
            16 => "Guns",
            17 => "Katana(?)",
            _ => type.ToString()
        };
    }
}
//This is a class that contains details for enemy skills.
public class EnemySkillData : SkillData
{
    //This is a list of flags for the skills.
    //Note that unlike PlayerSkills, these only have one entry.
    private Dictionary<int, int> _typeValues = new Dictionary<int, int>();

    //Despite these sharing some flags with player skills, I can't have them in the abstract class because they're not arrays when it comes to enemies.
    //so, here's some of those flags that I already know are shared.
    private int PercentValue; //type 2 is the percentage value of the effect (usually damage %).
    private int ActionSpeed; //type 3 is the action speed as a percentage.
    private int Accuracy; //type 4 is the accuracy as a percentage.
    
    //constructor
    public EnemySkillData(EnemySkillDeserialized incoming)
    {
        //the easy stuff first.
        _skillName = incoming.SkillName;
        _skillID = incoming.SkillNo;
        _skill_pattern = incoming.Skill_pattern;
        _need_parts = (ushort)incoming.Need_parts;
        _req_mastery = incoming.use_masterly;
        _target_area = (ushort)incoming.Trg_area;
        _target_side = (ushort)incoming.Trg_side;
        _use_scene = (ushort)incoming.Use_scene;
        _ud_efficacy_type = (ushort)incoming.Ud_efficacy_type;
        _ud_efficacy_afbit = (ushort)incoming.Ud_efficacy_afbit;
        //Types are a bit expensive computationally.
        //WHY DOES RIDER STILL KEEP BREAKING MY FUCKING STYLE
        foreach (TypeValueSingle typ in incoming.TypeValue)
        {
            switch (typ.TypeID)
            {
                case 2: { PercentValue = typ.Value; break; }
                case 3: { ActionSpeed = typ.Value; break; }
                case 4: { Accuracy = typ.Value; break; } 
                default: { _typeValues[typ.TypeID] = typ.Value; break; } //just as in PlayerSkills, this is force-added so that duplicate values aren't blocking
            }
        }
    }
    public override string ToString()
    {
        string toReturn = base.ToString();
        //I only know for player skills that 0 is friendly and 1 is enemy.
        //So, for enemy skills, I just show the number both times.
        toReturn += "\n\tTargets side " + _target_side + " with area " + _target_area;
        
        //unknown types
        if (_typeValues.Keys.Count > 0)
        {
            toReturn += "\n\tUnknown Types: ";
            string typeString = "";
            foreach (KeyValuePair<int, int> key in _typeValues)
                //unlike the player skills, we can just show the value here as well.
            {
                if (typeString == "") { typeString = key.Key.ToString() + " (" +  key.Value.ToString() + ")"; }
                else { typeString += ", " + key.Key.ToString() + " (" +  key.Value.ToString() + ")"; }
            }
            toReturn += typeString;
        }
        
        toReturn += "\nJapanese name: " + _skillName;
        return toReturn;
    }
}

//In order to deserialize skills, Newtonsoft.JSON needs a class that ***exactly*** fits the shape.
//So, I need two classes, because I don't really understand JSONConverter. 
public class PlayerSkillDeserialized
{
    public string SkillName { get; set; }
    public int SkillNo { get; set; }
    public int SkillMaxLevel { get; set; }
    public int Skill_pattern { get; set; }
    public int Need_parts { get; set; }
    public int[] use_masterly { get; set; } = new int[2];
    public int Trg_area { get; set; }
    public int Trg_side { get; set; }
    public int Use_scene { get; set; }
    public int Ud_efficacy_type { get; set; }
    public int Ud_efficacy_afbit { get; set; }
    public int Bit { get; set; }
    public TypeValueArray[] TypeValue { get; set; }
}
public class EnemySkillDeserialized
{
    public string SkillName { get; set; }
    public int SkillNo { get; set; }
    public int Skill_pattern { get; set; }
    public int Need_parts { get; set; }
    public int[] use_masterly { get; set; } = [];
    public int Trg_area { get; set; }
    public int Trg_side { get; set; }
    public int Use_scene { get; set; }
    public int Ud_efficacy_type { get; set; }
    public int Ud_efficacy_afbit { get; set; }
    public int Bit { get; set; }
    public TypeValueSingle[] TypeValue { get; set; }
}

//notably, these also need a way for the parser to understand TypeValues. So here's some tiny classes for that.
public abstract class TypeValueBase
{
    [JsonProperty("type")] //because having 'type' as a variable name is REALLY SMART :|
    public int TypeID { get; set; }
}

public class TypeValueSingle : TypeValueBase
{
    public int Value { get; set; }
}

public class TypeValueArray : TypeValueBase
{
    public int[] Value { get; set; }
}

//This class contains the functions for SkillData to be processed into more legible text files.
public class SkillDataDecoder
{
    private static string path;
    private string playerSkillOutputPath = "";
    private string enemySkillOutputPath = "";
    private string sharedSkillDetailOutputPath = "";
    private List<PlayerSkillData> _playerSkillData = new();
    private List<EnemySkillData> _enemySkillData = new();

    //There are several things I want to track to attempt to identify what the effect is.
    //And I want to be able to track them across both enemy and player skills.
    SortedDictionary<int, List<string>> sharedSkillPatterns = new(); //Skill Patterns
    SortedDictionary<int, List<string>> sharedTargetArea = new(); //Trg_areas
    SortedDictionary<int, List<string>> sharedScene = new(); //scenes
    Dictionary<int, List<string>> sharedEfficacyType = new(); //Ud_efficacy_types
    Dictionary<int, List<int>> sharedEfficacyAfbit = new(); //Ud_efficacy_afbits; slightly different since it's tied to efficacy types.

    SortedDictionary<int, List<string>> sharedTypeVals = new(); //Type values (I don't need the whole list, just what the flag is)

    Microsoft.Win32.OpenFileDialog dlg = new OpenFileDialog();
    
    //initializer which starts the decoding process.
    public SkillDataDecoder()
    {
        dlg.FileName = "MasterSkillData.JSON";
        dlg.DefaultExt = ".json";
        dlg.Filter = ".json files (.json)|*.json";

        Nullable<bool> result = dlg.ShowDialog();
        if (result.Value)
        {
            path = dlg.FileName;
            playerSkillOutputPath = Path.Combine(Path.GetDirectoryName(path), "PlayerSkills.txt");
            sharedSkillDetailOutputPath = Path.Combine(Path.GetDirectoryName(path), "SharedSkillDeets.txt");
            enemySkillOutputPath = Path.Combine(Path.GetDirectoryName(path), "EnemySkills.txt");
        }
        else
        {
            System.Environment.Exit(0); //close if the filepath doesn't work.
        }
        
        //streamer and reader for the data
        StreamReader stream = new StreamReader(path);
        JsonTextReader reader = new JsonTextReader(stream);
        
        //stream through data to get through the Unity headers
        while (reader.Read())
        {
            //this took forever to understand and get properly, but...
            //if the reader's token type is a property name and the name is PlayerSkills (since that's first)...
            if ((reader.TokenType == JsonToken.PropertyName) && ((string)reader.Value! == "PlayerSkills"))
            {
                //read one more time to get into the array
                reader.Read();
                //then pass the reader to the decoder function.
                DecodePlayerSkills(reader);
                //EnemySkills is right after, so we can pass it to the enemy skill decoder too
                reader.Read();
                reader.Read(); //We have to read twice to get into the EnemySkills array.
                DecodeEnemySkills(reader);
                //and finally break out
                break;
            }
        }
        //All done! Print it.
        PrintToOutput();
    }
    
    //function to decode player skills from the JSON.
    //Even though skills are almost identical, it's enough that they can't share a function.
    private void DecodePlayerSkills(JsonTextReader reader)
    {
        //JSON serializer
    JsonSerializer serializer = new JsonSerializer();
    
    //When we're passed the reader, we've read into the array, so this should get us the token of the first object:
    while (reader.Read() && reader.TokenType != JsonToken.EndArray)
      //We also watch for the end of the array, because PlayerSkills is an array.
      //Fortunately, my understanding is that the Serializer should consume the EndArray tokens within each entry.
    {
      PlayerSkillDeserialized skillToAdd = serializer.Deserialize<PlayerSkillDeserialized>(reader);
      //for legibility
      string skillName = SkillLocalizer.GetSkillName(skillToAdd.SkillName, skillToAdd.SkillNo);

      //Let's add those things we were tracking before casting this data into the full PlayerSkillData.
      //skill patterns:
      if (!sharedSkillPatterns.TryAdd(skillToAdd.Skill_pattern, [skillName])) //if TryAdd fails because the value already exists
          sharedSkillPatterns[skillToAdd.Skill_pattern].Add(skillName);

      //Shared Trg_area
      if (!sharedTargetArea.TryAdd(skillToAdd.Trg_area, [skillName]))
        sharedTargetArea[skillToAdd.Trg_area].Add(skillName);

      //Scenes
      if (!sharedScene.TryAdd(skillToAdd.Use_scene, [skillName]))
        sharedScene[skillToAdd.Use_scene].Add(skillName);

      //Shared Ud_efficacy_type
      if (!sharedEfficacyType.TryAdd(skillToAdd.Ud_efficacy_type, [skillName]))
      {
          sharedEfficacyType[skillToAdd.Ud_efficacy_type].Add(skillName);
          //also try adding to the afbit list.
          if (!sharedEfficacyAfbit.TryAdd(skillToAdd.Ud_efficacy_type, [skillToAdd.Ud_efficacy_afbit]))
              sharedEfficacyAfbit[skillToAdd.Ud_efficacy_type].Add(skillToAdd.Ud_efficacy_afbit);
      }
      //Shared Ud_efficacy_afbit, again, in case the above check does pass.
      //However, we don't do anything if it doesn't pass, since that adds a duplicate from above.
      sharedEfficacyAfbit.TryAdd(skillToAdd.Ud_efficacy_type, [skillToAdd.Ud_efficacy_afbit]);

      //shared types
      foreach (TypeValueArray typ in skillToAdd.TypeValue)
      { //special: I only care about types I don't already know.
        int[] knownTypes = [1, 2, 3, 4, 8, 9, 14, 18, 50, 51, 52];
        //need to do this in a different if statement to not run TryAdd regardless
        if (!knownTypes.Contains(typ.TypeID))
        {
          if (!sharedTypeVals.TryAdd(typ.TypeID, [skillName]))
          {
            sharedTypeVals[typ.TypeID].Add(skillName);
          }
        }
      }
      //Now that that's done, send it over to the PlayerSkillData constructor and add to the list.
        _playerSkillData.Add(new PlayerSkillData(skillToAdd));
    }
    }
    
    //function to decode enemy skills from the JSON.
    //Very similar to DecodePlayerSkills, so look there for more direct documentation.
    private void DecodeEnemySkills(JsonReader reader)
    {
        JsonSerializer serializer = new JsonSerializer();

        while (reader.Read() && reader.TokenType != JsonToken.EndArray)
        {
            EnemySkillDeserialized skillToAdd = serializer.Deserialize<EnemySkillDeserialized>(reader);
            //for legibility
            string skillName = SkillLocalizer.GetSkillName(skillToAdd.SkillNo);
            
            if (!sharedSkillPatterns.TryAdd(skillToAdd.Skill_pattern, [skillName]))
                sharedSkillPatterns[skillToAdd.Skill_pattern].Add(skillName);

            //Shared Trg_area
            if (!sharedTargetArea.TryAdd(skillToAdd.Trg_area, [skillName]))
                sharedTargetArea[skillToAdd.Trg_area].Add(skillName);

            //Scenes
            if (!sharedScene.TryAdd(skillToAdd.Use_scene, [skillName]))
                sharedScene[skillToAdd.Use_scene].Add(skillName);

            //Shared Ud_efficacy_type and bits
            if (!sharedEfficacyType.TryAdd(skillToAdd.Ud_efficacy_type, [skillName]))
            {
                sharedEfficacyType[skillToAdd.Ud_efficacy_type].Add(skillName);
                if (!sharedEfficacyAfbit.TryAdd(skillToAdd.Ud_efficacy_type, [skillToAdd.Ud_efficacy_afbit]))
                    sharedEfficacyAfbit[skillToAdd.Ud_efficacy_type].Add(skillToAdd.Ud_efficacy_afbit);
            }
            sharedEfficacyAfbit.TryAdd(skillToAdd.Ud_efficacy_type, [skillToAdd.Ud_efficacy_afbit]);

            //shared types
            foreach (TypeValueSingle typ in skillToAdd.TypeValue)
            {
                //I only care about types I don't already know.
                int[] knownTypes = [2, 3, 4];
                if (!knownTypes.Contains(typ.TypeID))
                {
                    if (!sharedTypeVals.TryAdd(typ.TypeID, [skillName]))
                    {
                        sharedTypeVals[typ.TypeID].Add(skillName);
                    }
                }
            }
            _enemySkillData.Add(new EnemySkillData(skillToAdd));
        }
    }
    
    //function to print the results to relevant .txt files.
    private void PrintToOutput()
    {
        //empty the files first
        File.WriteAllText(playerSkillOutputPath, "");
        File.WriteAllText(enemySkillOutputPath, "");
        File.WriteAllText(sharedSkillDetailOutputPath, "");

        //Player skills
        foreach (PlayerSkillData data in _playerSkillData)
        {
            File.AppendAllText(playerSkillOutputPath, (data.ToString() + "\n\n"));
        }

        //Enemy skills
        foreach (EnemySkillData data in _enemySkillData)
        {
            File.AppendAllText(enemySkillOutputPath, (data.ToString() + "\n\n"));
        }
        
        //Shared values for analysis. This needs to be done a little bit more manually.
        
        //Shared TypeValues:
        File.AppendAllText(sharedSkillDetailOutputPath, "SHARED TYPEVALS:\n)");
        foreach (int i in sharedTypeVals.Keys)
        {
            string toWrite = "\ttype " + i + ": \n";
            toWrite = sharedTypeVals[i].Aggregate(toWrite, (current, item) => current + ("\t\t" + item + "\n")); //IDE recommended this one.
            toWrite += '\n';
            File.AppendAllText(sharedSkillDetailOutputPath, toWrite);
        }
        //shared skill patterns
        File.AppendAllText(sharedSkillDetailOutputPath, "\nSHARED SKILL PATTERNS:\n");
        foreach (int i in sharedSkillPatterns.Keys)
        {
            string toWrite = "\ttype " + i + ": \n";
            toWrite = sharedSkillPatterns[i].Aggregate(toWrite, (current, item) => current + ("\t\t" + item + "\n"));
            toWrite += '\n';
            File.AppendAllText(sharedSkillDetailOutputPath, toWrite);
        }
        //shared areas
        File.AppendAllText(sharedSkillDetailOutputPath, "\nSHARED TARGET AREAS:\n");
        foreach (int i in sharedTargetArea.Keys)
        {
            string toWrite = "\ttype " + i + ": \n";
            toWrite = sharedTargetArea[i].Aggregate(toWrite, (current, item) => current + ("\t\t" + item + "\n"));
            toWrite += '\n';
            File.AppendAllText(sharedSkillDetailOutputPath, toWrite);
        }
        
        //shared scenes
        File.AppendAllText(sharedSkillDetailOutputPath, "\nSHARED SCENES:\n");
        foreach (int i in sharedScene.Keys)
        {
            string toWrite = "\ttype " + i + ": \n";
            toWrite = sharedScene[i].Aggregate(toWrite, (current, item) => current + ("\t\t" + item + "\n")); 
            toWrite += '\n';
            File.AppendAllText(sharedSkillDetailOutputPath, toWrite);
        }
        //Right now, I assume the EB_Efficacy parts are related, so the last section will have them both.
        File.AppendAllText(sharedSkillDetailOutputPath, "\nSHARED UD_EFFICACY TYPES AND THEIR BITS:\n");
        foreach (int type in sharedEfficacyType.Keys) //needed to change to help me process this logically
        {
            string toWrite = "\ttype " + type + ": \n";
            for (int i = 0; i < sharedEfficacyType[type].Count; i++)//for every value in EfficacyType[type]...
            {
                toWrite += "\t\t" +
                           sharedEfficacyType[type][i] + //add the value...
                           ", Bit: " +
                           sharedEfficacyAfbit[type][i] + //and the associated bit according to the Afbit dictionary.
                            "\n";
            } //~O(n^2) *barf*
            toWrite += '\n';
            File.AppendAllText(sharedSkillDetailOutputPath, toWrite);
        }
    }
}

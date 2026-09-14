/*using System.Collections;
using System.IO;
using Microsoft.Win32;
using Newtonsoft.Json; //Didn't want to have to do this, but my only other option was a manual bitstream...

namespace EO2HD_Patcher.Etrian_Odyssey_2_Data;

//Details about skills.
//
//Because some of these are changed in HD they had to be made into
//Unity assets, so we can export the .JSONs used.
//That doesn't mean they're simple to decode, though. See skill_decoding.txt for details.

//A class for an individual skill.
//Despite the name, enemy skills are actually structured identically, at least in the .json.
public class OldPlayerSkillData
{
    private string _skillName;   //Japanese name.
    private int _skillID;       //unique ID for each skill.
    private int _skillMaxLevel; //Max level for skill points.
    private int _skill_pattern; //No clue what this is yet.
    private ushort _need_parts; //Parts used, if any. Head = 1, Arm = 2, Leg = 3.
    private int[] _req_mastery = new int [2]; //Prerequisite weapon mastery to use the skill
      /*Currently, I know:
          1: Axes
          2: Swords
          3: Bows
          4: Shields
          5: Whips
      *//*
    private ushort _target_area; //Determines valid targets for the ability.
      /* Currently known with 100% certainty:
        5: Self-targetted.
       
       *//*
    private ushort _target_side; //Determines the side of the field (enemy or ally) the ability targets
      //0 is party, 1 is enemy
    private ushort _use_scene; //Likely the game screen the ability can be used on. 
    private ushort _ud_efficacy_type; //no clue.
    private ushort _ud_efficacy_afbit; //also no clue, but clearly related.
    private BitArray _bitmask = new BitArray(32); */ //Bitmask for determining certain effects. The most common is the element the skill deals.
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
    /*
    private Dictionary<int, int[]> _typeValues = new Dictionary<int, int[]>(); //List of additional flags, usually having to do with elements that change with each level.
    
    //Derived values from the above _typeValues:
    private int[] TPCosts = new int[10]; //type 1 is their TP cost per level.
    private int[] PercentValues = new int[10]; //type 2 is the percentage values of the effect per level.
    private int[] ActionSpeeds = new int[10]; //type 3 is the action speed as a percentage.
    private int[] Accuracy = new int[10]; //type 4 is the accuracy.
    
    private int[] OddsToActivate = new int[10]; //type 8 is the odds of a skill activating.
    private int[] OddsReductionPerActivation = new int[10]; //type 9 is how much type 8 is reduced by after each activation
    
    private int[] TileDurations = new int[10]; //type 14 is exclusive to field effects, and this is how long they last (in steps)
    
    private int[] InflictionRates = new int[10]; //type 18 is the rate of inflicting an ailment, if the skill has one.
    //Type 50 is an additive to a stat. It's used for all the stat++ passives.
    //Type 51 is a multiplier to a stat, expressed as a percentage. It only ever applies to the HP/TP up passives.
    //Type 52 is a multiplier to a mechanical value. Presumably. In 2, it only affects the Escape Rate++ passive.
    
    //Types 101, 102, and 104 I'm reasonably certain of, but I'm unsure if they're purely positive or not.

    public override string ToString()
    {
      string toReturn = "";
      toReturn += "English name: " + ManualLocalization.translationParser(_skillName);
      string usesPart = _need_parts switch
      {
        1 => "\n Uses Head",
        2 => "\n Uses Arms",
        3 => "\n Uses Legs",
        _ => ""
      };
      toReturn += usesPart;
      return toReturn;
    }

    public string ToString(bool verbose)
    {
      string toReturn = "";
      if (verbose)
        toReturn += ToString();
      //verbose = false just returns the name and the unknown values.
      toReturn += "Japanese Name: " + _skillName;
      toReturn += "\n Skill Pattern: " + _skill_pattern; //Still don't know skill patterns.
      toReturn += "\n Target Area:  " + _target_area; //Still don't know every target area.
      toReturn += " (Side: " + _target_side + ")";
      toReturn += "\n Uses Scene: " + _use_scene;   //still unsure what 'scene' means.
      if (_ud_efficacy_type != 0 || _ud_efficacy_afbit != 0) //no idea what this is.
      {
          toReturn += "\n Ud Efficacy: " + _ud_efficacy_type;
          toReturn += ", bit " + _ud_efficacy_afbit;
      }

      string bitString = "";
      //bitmask. I already know bits 0-11, so we just focus from 12 onward.
      for (int i = 12; i < _bitmask.Length; i++)
      {
        if (_bitmask[i])
        {
          if (bitString == "")
            bitString = "\nUnknown Bits: " + i;
          else
            bitString = ", " + i;
        }
        
      }
      toReturn += bitString;

      string typeString = "";
      //unknown types. I already know a few, but not all of them.
      //Here's a HashSet for the ones I know.
      HashSet<int> knownVals = new HashSet<int>
      {
        1, 2, 3, 4, 8, 9,14, 18
      };
      foreach (KeyValuePair<int, int[]> key in _typeValues)
      {
        if (!knownVals.Contains(key.Key))
        {
          if (typeString == "")
          {
            typeString = "\nUnknown Type: " + key.Key;
          }
          else
          {
            typeString += ", " + key.Key;
          }
        }
      }
      toReturn += typeString;
      return toReturn;
    }

    public OldPlayerSkillData(PlayerSkillDeserialized incoming)
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
      foreach (TypeValueDeserialized typ in incoming.TypeValue)
      {
        switch (typ.TypeID)
        {
          case 1: { TPCosts = typ.Value; break; } //TP cost per level
          case 2: { PercentValues = typ.Value; break; } //Damage multipliers values per level, as a %
          case 3: { ActionSpeeds = typ.Value; break; } //Action speed multipliers per level, as a %
          case 4: { Accuracy = typ.Value; break; } //Accuracy multiplier per level, as a %
          case 8: { OddsToActivate = typ.Value; break; } //Odds of initial activation per level, as a %
          case 9: { OddsReductionPerActivation = typ.Value; break; } //Reduction of 8 per activation, as a %
          case 14: { TileDurations = typ.Value; break; } //Amount of steps a field effect lasts
          case 18: { InflictionRates = typ.Value; break; } //infliction odds multiplier per level, as a %
          case 50:  //only for stat++ passives, i.e. STR Up. Additive, as a flat number.
          case 51:  //only for the HP/TP++ passives. Multiplicative, as a %.
          case 52: { break; } //only for the escape rate++ passives. Multiplicative, as a %.
          default: { _typeValues[typ.TypeID] = typ.Value; //have to force-add due to Medic having a skill with two type 10 values.
            break; } 
        }
      }
      //finally, the bitmask. I thought this would be hard, but apparently BitArray has some good syntax for this.
      _bitmask = new BitArray(new[] { incoming.Bit });
      _bitmask.Length = 32; //ensure it's always 32 bits long regardless of Bit size.
    }

    public string getName()
    {
      return _skillName;
    }
}

// In order for JsonSerializer to deserialize the data properly, I need to construct an exact replica of the data.
public class PlayerSkillDeserializedOld
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
  public TypeValueDeserialized[] TypeValue { get; set; }
}

//I also need a type to handle typeValue specifically, since type and value are two different types.
public class TypeValueDeserialized()
{
  
  [JsonProperty("type")] //because having 'type' as a variable name is REALLY SMART :|
  public int TypeID { get; set; }
  public int[] Value { get; set; }

  public TypeValueDeserialized(int typeID, int value) : this()
  {
    this.TypeID = typeID;
    this.Value =
    [
      value, value, value, value, value, value, value, value, value, value
    ];
  }

  public TypeValueDeserialized(int typeID, int[] value) : this()
  {
    this.TypeID = typeID;
    this.Value = value;
  }
}


//The class with the functions to decrypt the .JSONs into more useable information.
public class SkillDataDecoderOld
{
  private static string path;
  private string playerSkillOutputPath = "";
  private string enemySkillOutputPath = "";
  private string sharedSkillDetailOutputPath = "";
  private List<PlayerSkillData> _playerSkillData = new();
  private List<PlayerSkillData> _enemySkillData = new();
  
  //There are several things I want to track to attempt to identify what the effect is.
  SortedDictionary<int, List<string>> sharedSkillPatterns = new(); //Skill Patterns
  SortedDictionary<int, List<string>> sharedMasteries = new(); //Mastery types (I know some already).
  SortedDictionary<int, List<string>> sharedTargetArea = new(); //Trg_areas
  SortedDictionary<int, List<string>> sharedScene = new(); //scenes
  Dictionary<int, List<string>> sharedEfficacyType = new(); //Ud_efficacy_types
  Dictionary<int, List<string>> sharedEfficacyAfbit = new(); //Ud_efficacy_afbits
  SortedDictionary<int, List<string>> sharedTypeVals = new(); //Type values (I don't need the whole list, just what the flag is)

  Microsoft.Win32.OpenFileDialog dlg = new OpenFileDialog();

  public SkillDataDecoderOld()
  {
    dlg.FileName = "SkillData.JSON";
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

    //stream reader data
    StreamReader stream = new StreamReader(path);
    JsonTextReader reader = new JsonTextReader(stream);
    

    //stream through data to get through the Unity headers
    while (reader.Read())
    {
      //this took forever to understand and get properly, but...
      //if the reader's token type is a property name and the name is PlayerSkills...
      if ((reader.TokenType == JsonToken.PropertyName) && ((string)reader.Value! == "PlayerSkills"))
      {
        //read one more time to get into the array
        reader.Read();
        //then pass the reader to the decoder function.
        DecodePlayerSkills(reader, false);
        //EnemySkills is right after, so we can pass it to the enemy skill decoder too
        reader.Read();
        Console.WriteLine(reader.Value);
        reader.Read();
        Console.WriteLine(reader.Value);
        DecodePlayerSkills(reader, true);
        //and finally break out
        break;
      }
    }
    //All done! Print it.
    PrintToOutput();
  }

  private void DecodePlayerSkills(JsonTextReader reader, bool Enemy)
  {
    //JSON serializer
    JsonSerializer serializer = new JsonSerializer();


    //When we're passed the reader, we've read into the array, so this should get us the token of the first object:
    while (reader.Read() && reader.TokenType != JsonToken.EndArray)
      //We also watch for the end of the array, because PlayerSkills is an array.
      //Fortunately, my understanding is that the Serializer should consume the EndArray tokens within each entry.
    {
      //Console.WriteLine($"Before: {reader.TokenType} - {reader.Path}");
      PlayerSkillDeserialized skillToAdd = serializer.Deserialize<PlayerSkillDeserialized>(reader);
      //Console.WriteLine($"After: {reader.TokenType} - {reader.Path}"); //need to check types for debugging

      //Let's add those things we were tracking before casting this data into the full PlayerSkillData.
      //skill patterns:
      if (!sharedSkillPatterns.TryAdd(skillToAdd.Skill_pattern,
            [Localizer.GetSkillName(skillToAdd.SkillName, skillToAdd.SkillNo)])) //if TryAdd fails because the value already exists
      {
        sharedSkillPatterns[skillToAdd.Skill_pattern].Add(Localizer.GetSkillName(skillToAdd.SkillName, skillToAdd.SkillNo));
      }

      //shared masteries:
      foreach (int mastery in skillToAdd.use_masterly)
      {
        if (mastery == 0)
          break; //use_masterly always starts with a value if it has one
        else if (!sharedMasteries.TryAdd(mastery, [Localizer.GetSkillName(skillToAdd.SkillName, skillToAdd.SkillNo)]))
        {
          sharedMasteries[mastery].Add(Localizer.GetSkillName(skillToAdd.SkillName, skillToAdd.SkillNo));
        }
      }

      //Shared Trg_area
      if (!sharedTargetArea.TryAdd(skillToAdd.Trg_area, [Localizer.GetSkillName(skillToAdd.SkillName, skillToAdd.SkillNo)]))
      {
        sharedTargetArea[skillToAdd.Trg_area].Add(Localizer.GetSkillName(skillToAdd.SkillName, skillToAdd.SkillNo));
      }

      //Scenes
      if (!sharedScene.TryAdd(skillToAdd.Use_scene, [ManualLocalization.translationParser((skillToAdd.SkillName))]))
      {
        sharedScene[skillToAdd.Use_scene].Add(Localizer.GetSkillName(skillToAdd.SkillName, skillToAdd.SkillNo));
      }

      //Shared Ud_efficacy_type
      if (!sharedEfficacyType.TryAdd(skillToAdd.Ud_efficacy_type, [Localizer.GetSkillName(skillToAdd.SkillName, skillToAdd.SkillNo)]))
      {
        sharedEfficacyType[skillToAdd.Ud_efficacy_type].Add(Localizer.GetSkillName(skillToAdd.SkillName, skillToAdd.SkillNo));
      }

      //Shared Ud_efficacy_afbit
      if (!sharedEfficacyAfbit.TryAdd(skillToAdd.Ud_efficacy_afbit, [Localizer.GetSkillName(skillToAdd.SkillName, skillToAdd.SkillNo)]))
      {
        sharedEfficacyAfbit[skillToAdd.Ud_efficacy_afbit].Add(Localizer.GetSkillName(skillToAdd.SkillName, skillToAdd.SkillNo));
      }

      //shared types
      foreach (TypeValueDeserialized typ in skillToAdd.TypeValue)
      { //special: I only care about types I don't already know.
        int[] knownTypes = [1, 2, 3, 4, 8, 9, 14, 18, 50, 51, 52];
        //need to do this in a different if statement to not run TryAdd regardless
        if (!knownTypes.Contains(typ.TypeID))
        {
          if (!sharedTypeVals.TryAdd(typ.TypeID, [Localizer.GetSkillName(skillToAdd.SkillName, skillToAdd.SkillNo)]))
          {
            sharedTypeVals[typ.TypeID].Add(Localizer.GetSkillName(skillToAdd.SkillName, skillToAdd.SkillNo));
          }
        }
      }
      //Now that that's done, send it over to the PlayerSkillData constructor and add to the list.
      if (Enemy)
        _enemySkillData.Add(new PlayerSkillData(skillToAdd));
      else
        _playerSkillData.Add(new PlayerSkillData(skillToAdd));
    }
  }

  private void PrintToOutput()
  {
    {
      //empty the files first
      File.WriteAllText(playerSkillOutputPath, "");
      File.WriteAllText(enemySkillOutputPath, "");
      File.WriteAllText(sharedSkillDetailOutputPath, "");
      
      //Player skills

      foreach (PlayerSkillData data in _playerSkillData)
      {
        File.AppendAllText(playerSkillOutputPath, data.ToString(false));
      }
      //Enemy skills
      foreach (PlayerSkillData data in _enemySkillData)
      {
        File.AppendAllText(enemySkillOutputPath, data.ToString(false));
      }
      
      //The details I stored earlier. Have to do it manually at this point.
      File.AppendAllText(sharedSkillDetailOutputPath, "SHARED TYPEVALS:\n)");
      foreach (int i in sharedTypeVals.Keys)
      {
        string toWrite = "type " + i + ": \n";
        toWrite = sharedTypeVals[i].Aggregate(toWrite, (current, item) => current + ("\t" + item + "\n"));
        toWrite += '\n';
        File.AppendAllText(sharedSkillDetailOutputPath, toWrite);
      }
    }
  }
}
*/






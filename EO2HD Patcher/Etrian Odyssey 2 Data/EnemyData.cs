namespace EO2HD_Patcher.Etrian_Odyssey_2_Data;

public enum DropConditional : byte
{
    KILL_1_TURN = 0x41,
    KILL_3_TURNS = 0x43,
    KILL_5_TURNS = 0x45,
    KILL_7_TURNS = 0x47,
    NONE = 0xFF,
    NOT_PHYSICAL = 0x0A,
    POISONED = 0x0E,
    INSTANT_DEATH = 0x0F,
    CUT = 0x00,
    BASH = 0x01,
    STAB = 0x02,
    PHYSICAL = 0x03,
    FIRE = 0x04,
    ICE = 0x05,
    VOLT = 0x06,
    NOT_STAB = 0x09,
    ARM_BIND = 0x10,
    HEAD_BIND = 0x11,
    LEG_BIND = 0x12,
    FULL_BIND = 0x13,
}

public class EnemyData
{
    private byte[]? _raw;
    private byte[]? _tangledData;
    private ushort[] _items = new ushort[3];
    private int[] _itemOdds = new int[3];
    private string _enemyID;
    private string _scriptID;

    public byte[]? RawData
    {
        get => _raw;
        set => _raw = value ?? throw new ArgumentNullException(nameof(value));
    }

    public uint Experience { get; set; }

    public string ScriptId
    {
        get => _scriptID;
        set => _scriptID = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string EnemyId
    {
        get => _enemyID;
        set => _enemyID = value ?? throw new ArgumentNullException(nameof(value));
    }

    public int[] ItemOdds
    {
        get => _itemOdds;
        set => _itemOdds = value ?? throw new ArgumentNullException(nameof(value));
    }

    public ushort[] Items
    {
        get => _items;
        set => _items = value ?? throw new ArgumentNullException(nameof(value));
    }

    public byte[]? TangledData
    {
        get => _tangledData;
        set => _tangledData = value ?? throw new ArgumentNullException(nameof(value));
    }

    public int BasicAcc { get; set; }

    public DAMAGE_TYPES BasicDmgType { get; set; }

    public byte DropConditional { get; set; }

    public int Tp { get; set; }

    public int Hp { get; set; }
    
    public int CodexNum { get; set; }
    
    //These variables contain the data within TangledData.
    private int[] statistics = new int[6]; //In STR/TEC/VIT/AGI/LUC order, like classes are.
    private int[] physResistances = new int[3]; //Cut, Stab, Bash, as listed in the codex.
    private int[] elemResistances = new int[3]; //Fire, Ice, Volt
    private int[] deathResistances = new int[2]; //Instant Death and Petrification
    //In Codex order: Fear, Curse, Poison, Sleep, Confuse, Paralyze, Blind
    private int[] ailmentResistances = new int[7]; 
    public int stunResistance { get; set; }
    private int[] bindResistances = new int[3]; //Head, Arm, Leg
    public int Level {get; set;}
    
    //and the getters/setters
    public int[] Statistics
    {
        get => statistics;
        set => statistics = value ?? throw new ArgumentNullException(nameof(value));
    }
    
    public int[] PhysResistances
    {
        get => physResistances;
        set => physResistances = value ?? throw new ArgumentNullException(nameof(value));
    }

    public int[] ElemResistances
    {
        get => elemResistances;
        set => elemResistances = value ?? throw new ArgumentNullException(nameof(value));
    }

    public int[] DeathResistances
    {
        get => deathResistances;
        set => deathResistances = value ?? throw new ArgumentNullException(nameof(value));
    }

    public int[] AilmentResistances
    {
        get => ailmentResistances;
        set => ailmentResistances = value ?? throw new ArgumentNullException(nameof(value));
    }

    public int[] BindResistances
    {
        get => bindResistances;
        set => bindResistances = value ?? throw new ArgumentNullException(nameof(value));
    }

    //ToString is primarily for development use, not for users to see the data.
    public override string ToString()
    {
        string itemString = "";
        for (int i = 0; i < 3; i++)
        {
            itemString += Items[i] + " @ " + ItemOdds[i] + "%\n";
        }
        if (Items[2] != 0)
            itemString += "CONDITION: 0x" + DropConditional.ToString("X2") + "\n";

        return _enemyID + ", Codex No. " + CodexNum + "\n"
               + "Level: " + Level + "\n"
               + "HP: " + Hp + "\t" + "TP: " + Tp + "\n"
               + "Stun Resist:" + stunResistance + "\n"
               + "Basic Attack Type: " + BasicDmgType + "@ " + BasicAcc + "% Acc\n"
               + "Drops: " + Experience + "EXP\n" + itemString
               + "AI Script: " + ScriptId + "\n"
               + "Stats:  STR: " + statistics[0]
               + "  TEC: " + statistics[1]
               + "  VIT: " + statistics[2]
               + "  AGI: " + statistics[3]
               + "  LUC: " + statistics[4]
               + "\nResistances:\n"
               + "Cut: " + physResistances[0] + "  Stab: " + physResistances[1] + "  Bash: " + physResistances[2]
               + "  Fire: " + elemResistances[0] + "  Ice: " + elemResistances[1] + "  Volt:" + elemResistances[2]
               + "\nInstant Death: " + deathResistances[0] + "  Petrify: " + deathResistances[1] + "\n"
               + "Fear: " + ailmentResistances[0] + "  Curse: " + ailmentResistances[1] + "  Poison: " +
               ailmentResistances[2]
               + "  Sleep: " + ailmentResistances[3] + "   Confuse: " + ailmentResistances[4] + "  Paralyze: " +
               ailmentResistances[5]
               + "  Blind: " + ailmentResistances[6] + "\n"
               + "Head Bind: " + bindResistances[0] + "  Arm Bind: " + bindResistances[1] + "  Leg Bind: " +
               bindResistances[2]
               + "\n\n";
    }
    
    //This function untangles the Lancarse code into something legible. It's very cursed.
    public void DisentangleData()
    {
        if (TangledData == null)
            return; //this shouldn't happen, but it's safe.
        
        //first, the statistics. The first seven bytes are the STR/VIT/AGI/LUC/TEC statistics.
        byte[] statBlock = TangledData[..7];
        //AGI is the only easy one. It's just the value at index 3.
        //Remember that in the EnemyData block above, they're stored in STR/TEC/VIT/AGI/LUC order.
        statistics[3] = statBlock[3];
        //Indices 1/2 are VIT, and 5/6 are TEC.
        //cast them into integers in little endian.
        int VITval = (statBlock[1] | (statBlock[2] << 8));
        int TECval = (statBlock[5] | (statBlock[6] << 8));
        //divide by 16 and plug these values into the stat block of the Enemy.
        statistics[2] = VITval / 16;
        statistics[1] = TECval / 16;
        //finally, STR and LUC.
        //These are similar to VIT and TEC, using index 0 or 4...and the remainder from the VIT/TEC divisions.
        int STRval = statBlock[0] | ((byte)(VITval % 16) << 8);
        int LUCval = statBlock[4] | ((byte)(TECval % 16) << 8);
        //divide by 4 and plug these into the Enemy stat block.
        statistics[0] = STRval / 4;
        statistics[4] = LUCval / 4;
        
        //next, the resistances. The physical/elemental resistances use 4 bytes. Yes. Four.
        //Physicals are listed in Cut/Bash/Stab order, unlike the codex.
        //We also have to do the same carryover nonsense above, in case of values >150.
        byte[] physBlock = TangledData[7..11]; 
        physResistances[1] = physBlock[2] | (physBlock[3] << 8); //Stab
        physResistances[2] = physBlock[1] | ((physResistances[1] % 4) << 8); //Bash
        physResistances[0] = physBlock[0] | ((physResistances[2] % 2) << 8); //Cut

        //because we use the values as references above, we can't do this step until last.
        physResistances[1] /= 4;
        physResistances[2] /= 2;
        //You'll note that, like above, we have to do some arbitrary divisions.
        //There's an implication of weird bit-shifts happening, but I'm not knowledgable enough to infer more.
        //This pattern of division by 1/division by 2/division by 4 happens through the rest of the block.

        //Elementals are listed in codex order, fortunately.
        byte[] elemBlock = TangledData[11..15];
        elemResistances[2] = elemBlock[2] | (elemBlock[3] << 8); //Volt
        elemResistances[1] = elemBlock[1] | ((elemResistances[2] % 4) << 8); //Ice
        elemResistances[0] = elemBlock[0] | ((elemResistances[1] % 2) << 8); //Fire

        elemResistances[2] /= 4;
        elemResistances[1] /= 2;
        
//If the Utility Links are to be believed, the order here is 
        //Death, Petrify, Sleep, Confuse, Fear, Poison, Blind, Curse, Paralyze
        //Whereas the codex order is
        //Death, Petrify, Fear(0), Curse(1), Poison(2), Sleep(3), Confuse(4), Paralyze(5), Blind(6)
        //What a mess.
        byte[] ailBlock = TangledData[17..27];
        //So, in three-type chunks:
        ailmentResistances[3] = ailBlock[0] | (ailBlock[1] << 8); //Sleep
        deathResistances[1] = TangledData[16] | ((ailmentResistances[3] % 4) << 8); //Petrify
        deathResistances[0] = TangledData[15] | ((deathResistances[1] % 2) << 8); //Death
        ailmentResistances[3] /= 4;
        deathResistances[1] /= 2;
        
        ailmentResistances[2] = (ailBlock[4] | (ailBlock[5] << 8)); //Poison
        ailmentResistances[0] = ailBlock[3] | ((ailmentResistances[2] % 4) << 8); //Fear
        ailmentResistances[4] = ailBlock[2] | ((ailmentResistances[0] % 2) << 8); //Confuse
        ailmentResistances[2] /= 4;
        ailmentResistances[0] /= 2;
        
        ailmentResistances[5] = ailBlock[8] | (ailBlock[9] << 8); //Paralyze
        ailmentResistances[1] = ailBlock[7] | ((ailmentResistances[5] % 4) << 8); //Curse
        ailmentResistances[6] = ailBlock[6] | ((ailmentResistances[1] % 2) << 8); //Blind
        ailmentResistances[5] /= 4;
        ailmentResistances[1] /= 2;
        
        //Resistances to binds.
        byte[] bindBlock = TangledData[27..31];
        bindResistances[2] = (bindBlock[2] | (bindBlock[3] << 8)); //Leg
        bindResistances[1] = bindBlock[1] | ((bindResistances[2] % 4) << 8); //Arm
        bindResistances[0] = bindBlock[0] | ((bindResistances[1] % 2) << 8); //Head
        bindResistances[2] /= 4;
        bindResistances[1] /= 2;
        //bindResistances[0] /= 2;
        
        //The last two are the resistance to stun and the level.
        //Resistance to Stun
        stunResistance = TangledData[31];
        //level.
        Level = TangledData[32] / 2;
    }

    //This changes the individual enemy's EXP value. 
    //For cases like the 'FOEs give EXP' flag, it changes it to that amount.
    //If an EXP multiplier flag is applied, it (also) multiplies it by that amount
    //Note that we limit it to the four-byte int limit, just in case.
    public void ChangeEXP(float multiplier = 1, int amount = 0)
    {
        if (amount != 0)
        {
            this.Experience = (uint)amount;
        }

        if (!(multiplier > 1)) return; //break if multiplier isn't important
        try
        {
            this.Experience = (uint)(multiplier * Experience);
        }
        catch (OverflowException)
        {
            this.Experience = uint.MaxValue;
        }
    }

    //This changes the individual enemy's drop rates. 
    //By default, this only sets the drop rate of the conditional item to 100%, as that is the intended use case here.
    //However, it's been generalized in case the need arises. 
    public void ChangeDropRate(int index = 2, int dropRate = 100)
    {
        //verify that the incoming variables are in range (esp. index, which crashes the program otherwise)
        if (index is < 0 or >= 3) return;
        if (dropRate < 0) dropRate = 0;
        if (dropRate > 100) dropRate = 100;
        //verify that there is an item in that index to begin with
        if (_items[index] == 0) return;
        //Item exists, so set the value.
        _itemOdds[index] = dropRate;
    }
}
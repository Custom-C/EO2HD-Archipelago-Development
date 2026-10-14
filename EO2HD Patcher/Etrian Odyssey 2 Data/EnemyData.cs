using System.Buffers.Binary;
using System.IO;
using System.Text;

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

    public byte[] RawData
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
        //temporary
        //string incBytes = BitConverter.ToString(RawData);
        //string outBytes = BitConverter.ToString(Recreate());
        //these work, so I'm leaving this here to let myself know that this has been verified.
        
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
    public void ChangeEXP(float multiplier = 1, uint amount = 0)
    {
        if (amount != 0)
        {
            this.Experience = amount;
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
    //If this flag is set, we nerf Muckdile the tiniest bit.
    //And by that, I mean we make him stunnable via Riot Gun, and killable with Climax.
    //The other part of the nerf is in the skills (making Frolic use the legs).
    public void NerfMuckdile()
    {
        if (EnemyId != "en085")
            return; //make sure it's Muckdile we're nerfing.
        stunResistance = 1;
        deathResistances[0] = 1;
    }
    
    //This re-creates the byte array of this individual enemy so that we can patch it back into the game.
    public byte[] Recreate()
    {
        /* As a reminder, Ur-Child's data is this:
         A8 61 00 00 03 B8 E1 06 64 08 E2 06 64 C8 90 01 64 C8 90 01 00 00 28
         00 0A 14 28 00 0A 0A 28 00 0A 14 28 00 0F A0 00 00 5E 10 00 00 00 00
         64 00 00 FF 01 63 00 00 00 00 65 62 30 30 36 00 00 00 00 00 00 00 00
         00 00 00 73 63 72 5F 61 64 61 6D 00 00 00 00 00 00 00 00 00 00 90 00
        The first two bytes are HP
        Then there's a buffer byte 00
        The next two bytes are TP.
        Then there's the Lancarse bytes. 35 of them, including the buffer at the end.
          */
        
        //we'll use a binary writer for this.
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);

        writer.Write((short)Hp); //short is 2 bytes.
        writer.Write((byte)0); //buffer byte
        //Slight issue: TP is in Big Endian for some reason. Not hard to fix, I don't think.
        writer.Write(BinaryPrimitives.ReverseEndianness((short)Tp));
        //Now, write the Lancarse bits back.
        writer.Write(Rescramble());
        
        //After the Lancarse bits, we still have
        //
        //5E 10 00 00 00 00 64 00 00 FF 01 63 00
        //00 00 00 65 62 30 30 36 00 00 00 00 00
        //00 00 00 00 00 00 73 63 72 5F 61 64 61
        //6D 00 00 00 00 00 00 00 00 00 00 90 00
        //for Ur-Child.
        //And, for posterity's sake, Muckdile:
        //44 10 00 00 45 10 32 00 5A 03 01 63 50
        //C3 00 00 65 6E 30 38 35 00 00 00 00 00
        //00 00 00 00 00 00 73 63 72 5F 64 69 6E
        //6F 67 61 74 6F 72 00 00 00 00 00 50 00
        
        //The next six bytes are item IDs for their drops.
        //first two are their normal drop
        //second two are their second possible drop
        //third two are their conditional.
        
        //Ur-Child always drops one Eden Apple (id 4190, 0x105E)
        //Muckdile has a 50% chance to drop his non-conditional Croc Skin (id 4164, 0x1044),
        //and a 90% chance to drop Iron Fang (id 4165, 0x1045) when killed with physical damage.
        //The way I've stored items makes this extremely easy.
        writer.Write(Items[0]);
        writer.Write(Items[1]);
        writer.Write(Items[2]);
        //also with odds, as one byte rather than two.
        writer.Write((byte)ItemOdds[0]);
        writer.Write((byte)ItemOdds[1]);
        writer.Write((byte)ItemOdds[2]);
        //the next byte is the requisite condition to drop item[2].
        //If a creature doesn't have one, it's FF, so it still exists in the data.
        writer.Write(DropConditional);
        //Next two bytes relate to their basic attacks - namely, type and accuracy.
        writer.Write((byte)BasicDmgType);
        writer.Write((byte)BasicAcc);
        //Next up: 4 bytes indicating the EXP gain.
        writer.Write(Experience);
        //Now we get the strings. 
        //Ur-Child's: 
        //65 62 30 30 36 00 00 00 00 00 00 00 00 00 00 00 (eb006)
        //73 63 72 5F 61 64 61 6D 00 00 00 00 00 00 00 00 (scr_adam)
        //for Muckdile:
        //65 6E 30 38 35 00 00 00 00 00 00 00 00 00 00 00 (en085)
        //73 63 72 5F 64 69 6E 6F 67 61 74 6F 72 00 00 00 (scr_dinogator)
        
        //As you can see, both strings are 16 bytes long, in ASCII format.
        //So, we use the helper function below to get those into the stream.
        WriteBytesFromString(EnemyId, writer);
        WriteBytesFromString(ScriptId, writer);
        
        //We have four bytes left, and only one is relevant.
        writer.Write((short)0); //two-byte buffer.
        writer.Write((byte)CodexNum); //The number in the codex
        writer.Write((byte)0); //one last buffer.
        //finally return.
        return stream.ToArray();
    }

    //helper function to get an always-correct size byte array for the two relevant strings.
    private void WriteBytesFromString(string input, BinaryWriter writer)
    {
        byte[] toWrite = new byte[16];
        //because this function is partly client-facing, I will verify the input's length.
        if (input.Length > 16)
        {
            Console.WriteLine(input + " is an invalid string for WriteBytesFromString, writing empty array");
            
        }
        else //string is within range
        {
            byte[] strBytes = Encoding.ASCII.GetBytes(input); //verify that it's encoded in ASCII
            Array.Copy(strBytes, toWrite, strBytes.Length);
        }
        //now we write to the passed-in writer.
        writer.Write(toWrite);
    }
    

    //Rescramble is the inverse of Disentangle, in that it re-writes the data in the compressed byte form
    //the EO2 engine expects.
    //As of writing, this isn't actually necessary, but if I want to modify any enemy's resistances in the randomizer,
    //this will be necessary to get the data back where it belongs.
    public byte[] Rescramble()
    {
        /*Ur-Child's bytes:
         B8 E1 06 64 08 E2 06 64 C8 90 01 64 C8 90 01 00 00 28
         00 0A 14 28 00 0A 0A 28 00 0A 14 28 00 0F A0 00 00
         Ur-Child's stats:
         110/110/110/100/130 STR/VIT/TEC/AGI/LUC.
         */
        //we'll use another binary writer, since I've learned how convenient they are for this.
        MemoryStream stream = new MemoryStream();
        BinaryWriter writer = new BinaryWriter(stream);
        
        //The first seven bytes are the statistics in STR/VIT/AGI/LUC/TEC order.
        //Our Statistics array is in STR/TEC/VIT/AGI/LUC order, however.
        
        //For Ur-Child, that means the relevant bits are B8 E1 06 64 08 E2 06.
        
        //the first three bytes are STR and VIT. 
        //VIT is simple: the given value * 16.
        short Vit = (short) (Statistics[2] * 16);
        //STR is the weird one.
        //If STR is above a certain value, we had to use VIT % 16 to get it, appending it to the
        //byte given in the code.
        //So, the minimum value of this would be 0x100, which is 64, right?
        //However, we actually divide the whole value (remainder included) by 4 to get the true value.
        //For Ur-Child, 1B8 = 440, /4 = 110.
        //Without the remainder, B8 = 184, /4 = 46.
        //So, the highest value this could be without needing the remainder is FF. FF / 4 = 63.
        
        //Logically, the minimum STR needed for a remainder of 2 is then 128 STR.
        // The highest STR in EO2 is Ur-Child's 110, so that's not an issue.
        //However, it does mean this isn't usable in other games, but that's okay because they're all on the EO3 engine :)
        
        //Ahem, anyway. If STR > 63, we need to increment Vit by 1 so that there is actually a remainder.
        //We also want our written STR value to reflect the fact it's incomplete, so we subtract 64.
        byte Str;
        if (Statistics[0] >= 64)
        {
            Vit += 1;
            Str = (byte)(4*(Statistics[0] - 64));
        }
        else
            Str = (byte)(4*Statistics[0]);
        /*So, for Ur-Child's 110/110:
         Vit = 110 * 16 = 1760
         STR >= 64, so Vit += 1 => 1761
         Str = (110-64) * 4 => 184
         Vit = 0x6E1
         Str = 0xB8
         Yay.
         For Hedgehog's 7/7:
         Vit = 7 * 16 = 112
         STR < 64.
         Str = 7 * 4 = 28.
         Vit = 0x70
         Str = 0x1C
         That also works.
        */
        writer.Write(Str);
        writer.Write(Vit);
        
        //AGI is fortunate in that it is simply the value as a byte. No division needed.
        writer.Write((byte)Statistics[3]);
        
        //Finally, LUC and TEC. Very similar behaviour to above.
        byte Luc;
        short Tec = (short)(Statistics[1] * 16);
        //However, Ur-Child's LUC is above the value needed for a remainder of 2.
        //That requires a minor change. One that I could apply to STR above, but I'm lazy.
        int rem = 0;
        if (Statistics[4] >= 64)
            rem = Statistics[4] % 63; //If Statistics >= 64 and < 128, rem = 1
        //incrementing Tec by rem creates the remainder the game expects
        Tec += (short)rem;
        //And we can subtract LUC by (64 * rem) to get a value that fits the byte.
        Luc = (byte)(4 * (Statistics[4] - (64 * rem)));
        /* Confirmation again with Ur-Child...
         TEC = 110 * 16 = 1760
         LUC = 130, rem = 130 % 63 = 2
         Tec = 1760 + rem = 1762
         Luc = 4 * (130 - (64 * rem)) => 4 * (130 - 128) => 4 * 2 = 8
         Tec = 0x6E2 => E2 06
         Luc = 0x08
         08 E2 06 - Correct!
         ... and Hedgehog.
         TEC = 6 * 16 = 96
         LUC = 12, rem = 0
         Tec = 96 + rem = 96
         Luc = 4 * (12 - (64 * rem)) => 4 * 12 = 48
         Tec = 0x60 => 60 00
         Luc = 0x30
         30 60 00 - Correct!
         */
        writer.Write(Luc);
        writer.Write(Tec);
        
        /*The remaining bytes are, in order:
        Cut,     Bash,    Stab,  
        Fire,    Ice,     Volt,  
        Death,   Petrify, Sleep, 
        Confuse, Fear,    Poison, 
        Blind,   Curse,   Paralyze, 
        Head,    Arms,    Legs,    
        Stun,    Level,   (Buffer)
        Noting that they do a pattern of (1 byte, 1 byte, 2 bytes).
        And they divide each set by 1, 2, and 4 respectively.
        AND they do the carryover stuff for values more than ~128.
        For Ur-Child in particular, his bytes are
        64 C8 90 01 
        64 C8 90 01 
        00 00 28 00 
        0A 14 28 00 
        0A 0A 28 00 
        0A 14 28 00 
        0F A0 00 00
        And the expected values are
        100, 100, 100
        100, 100, 100
          0,   0,  10
         10,  10,  10
         10,   5,  10
         10,  10,  10
         15,  80,   0
         
         As an additional reference, Muckdile:
         32 64 C8 00 
         2C 59 B2 04 
         00 00 90 01 
         64 C8 90 01 
         64 C8 90 01 
         64 C8 90 01 
         00 C6 00 00
         
          50  50  50
         300 300 300
           0   0 100
         100s for all...
           0  99   0
         Noting that our EnemyData's stat block goes in Codex form, which is
        Cut, Stab, Bash, Fire, Ice, Volt,
        Stun, Death, Petrify, 
        Fear, Curse, Poison, Sleep, Confuse, Paralyze, Blind,
        Head, Arms, Legs
        
        Just like Disentangle, we'll go in three-type blocks.
        */
        //I actually did the elemental resistances first to get the logic correct, so see those for why I did things like this here.
        //PHYS:
        short stab = (short)(4 * PhysResistances[1]);
        int cutRem = 0;
        if (PhysResistances[0] >= 256)
            cutRem = PhysResistances[0] / 256;
        int bashRem = 0;
        if (2 * PhysResistances[2] >= 256)
            bashRem = PhysResistances[2] / 256;
        byte cut = (byte)(PhysResistances[0] - 256 * cutRem);
        byte bash = (byte)((2*PhysResistances[2] - 256 * bashRem) + cutRem);
        stab += (short)bashRem;
        writer.Write(cut);
        writer.Write(bash);
        writer.Write(stab);
        
        //ELEM:
        //we need to go in reverse order due to the potential carryover.
        short volt = (short)(4 * ElemResistances[2]); //for Ur-Child, this gets 400/0x190, for Muckdile this gets 1200/0x4B0
        //short ice = (short)(2 * ElemResistances[1]); //for Ur-Child, this gets 200/0xC8, Muckdile gets 600/0x258
        //short fire = (short)ElemResistances[0]; //Ur-Child gets 100/0x64, Muckdile gets 300/0x012C
        //You'll note that for fire, the last byte is correct for Muckdile. This is because of the lack of carryover for the last type.
        //So, fire gets a carryover from ice, which gets a carryover from volt. But how do you get the value?
        //Well, for fire, without the carryover we only get a value of 44. The carryover, then, must represent a value of 256.
        //Similarly, with ice we get a value of 88. The carryover represents 512, or 2*256.
        //So, if the stored resistance value after multiplying goes above 255 (0xFF), we need a carryover.
        //And that carryover is equal to how many times we go over 255.
        //So, similar to the statistics, we subtract (256 * rem) from the outgoing bytes.
        //...Making sure to check if the value is greater than 255.
        int firRem = 0;
        if (ElemResistances[0] >= 256)
            firRem = ElemResistances[0] / 256;
        int iceRem = 0;
        if (2 * ElemResistances[1] >= 256)
            iceRem = (2 * ElemResistances[1]) / 256;
        byte fire = (byte)(ElemResistances[0] - (256 * firRem));
        byte ice = (byte)((2 * ElemResistances[1] - (256 * iceRem)) + firRem); //remember to add the remainder
        volt += (short)iceRem;
        /* verification:
         Ur-Child is (100, 100, 100)
         volt = 400 (0x190) => 90 01
         ice = 200, iceRem = 0 => ice = 0xC8
         fire = 100, fireRem = 0 => fire = 0x64
         writes (64 C8 90 01)
         Correct!
         Muckdile is (300, 300, 300)
         volt = 1200 (0x4B0)
         ice = 600, iceRem = 2 => 600 - (256 * 2) => ice = 88 (0x58)
         volt += iceRem = 1202 (0x4B2) => B2 04
         fire = 300, fireRem = 1 => 300 - 256 => fire = 44 (2C)
         ice += fireRem = 89 (59)
         writes (2C 59 B2 04)
         Correct!
         */
        writer.Write(fire);
        writer.Write(ice);
        writer.Write(volt);
        
        //DEATH AILMENTS + SLEEP
        short sleep = (short)(4 * AilmentResistances[3]);
        int deathRem = 0;
        if (DeathResistances[0] >= 256)
            deathRem = DeathResistances[0] / 256;
        int petRem = 0;
        if (2 * DeathResistances[1] >= 256)
            petRem = DeathResistances[1] / 256;
        byte death = (byte)(DeathResistances[0] - 256 * deathRem);
        byte pet = (byte)((2*DeathResistances[1] - 256 * petRem) + deathRem);
        sleep += (short)petRem;
        writer.Write(death);
        writer.Write(pet);
        writer.Write(sleep);
        
        // CONFUSE/FEAR/POISON
        short poison = (short)(4 * AilmentResistances[2]);
        int conRem = 0;
        if (AilmentResistances[4] >= 256)
            conRem = AilmentResistances[4] / 256;
        int fearRem = 0;
        if (2 * AilmentResistances[0] >= 256)
            fearRem = AilmentResistances[0] / 256;
        byte confuse = (byte)(AilmentResistances[4] - 256 * conRem);
        byte fear = (byte)((2*AilmentResistances[0] - 256 * fearRem) + conRem);
        poison += (short)fearRem;
        writer.Write(confuse);
        writer.Write(fear);
        writer.Write(poison);
        
        // BLIND/CURSE/PARALYZE
        //Reminder: the stored values are ordered Fear, Curse, Poison, Sleep, Confuse, Paralyze, Blind,

        short para = (short)(4 * AilmentResistances[5]);
        int blindRem = 0;
        if (AilmentResistances[6] >= 256)
            blindRem = AilmentResistances[6] / 256;
        int curseRem = 0;
        if (2 * AilmentResistances[1] >= 256)
            curseRem = AilmentResistances[1] / 256;
        byte blind = (byte)(AilmentResistances[6] - 256 * blindRem);
        byte curse = (byte)((2*AilmentResistances[1] - 256 * curseRem) + blindRem);
        para += (short)curseRem;
        writer.Write(blind);
        writer.Write(curse);
        writer.Write(para);
        
        // HEAD/ARM/LEGS
        short legs = (short)(4 * BindResistances[2]);
        int headRem = 0;
        if (BindResistances[0] >= 256)
            headRem = BindResistances[0] / 256;
        int armRem = 0;
        if (2 * BindResistances[1] >= 256)
            armRem = BindResistances[1] / 256;
        byte head = (byte)(BindResistances[0] - 256 * headRem);
        byte arm = (byte)((2*BindResistances[1] - 256 * armRem) + headRem);
        legs += (short)armRem;
        writer.Write(head);
        writer.Write(arm);
        writer.Write(legs);
        
        //Finally, Stun and level. I don't think these have carryover.
        //If they do, it's never used! The highest stun resistance is 125, and the highest level is 99.
        writer.Write((byte)stunResistance);
        //Although the 1-2-4 pattern still exists, so Level needs to be multiplied by 2.
        writer.Write((byte)(2*Level));
        //FINALLY, the two bytes of buffer.
        writer.Write((short)0);
        
        return stream.ToArray();
    }
}
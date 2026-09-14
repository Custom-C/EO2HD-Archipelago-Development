using System.Buffers.Binary;
using System.IO;
using System.Text;
using System.Windows.Controls;
using Microsoft.Win32;

using EO2HD_Patcher.Etrian_Odyssey_2_Data;


namespace EO2HD_Patcher.Enemy_Changes;

/// Enemy data is encoded in a compressed hexadecimal format, so we need methods
/// to decode and recode it into that format if we want to change anything in those fields.
/// For example, the hedgehog enemy looks like this:
/// 2D 00 00 00 01 1C 70 00 0E 30 60 00 64 2C 91 01 96 C8 90 01 64 C8 90
/// 01 64 C8 90 01 64 C8 90 01 64 C8 90 01 64 04 00 00 A2 0F 00 00 A3 0F
/// 32 00 32 41 02 63 4F 00 00 00 65 6E 30 30 34 00 00 00 00 00 00 00 00 
/// 00 00 00 73 63 72 5F 68 65 64 67 65 68 6F 67 00 00 00 00 00 00 02 00
/// Fortunately, Araxxor et al. have done most of the hard work for us, but we need to figure out
/// how these values actually correlate in order to change them for the randomizer/QoL.
///
///


public class HexaCode
{
    //strings to help disentangle the EnemyData file.
    private static string path;
    private string outputPath = "";
    private List<EnemyData> enemies = [];

    [STAThread]
    public void EO2_Decoder()
    {
        Microsoft.Win32.OpenFileDialog dlg = new OpenFileDialog();
        
        dlg.FileName = "EnemyData";
        dlg.DefaultExt = ".tbb";
        dlg.Filter = ".tbb files (.tbb)|*.tbb";
        
        Nullable<bool> result = dlg.ShowDialog();
        if (result.Value)
        {
            path = dlg.FileName;
            outputPath = Path.Combine(Path.GetDirectoryName(path), "output.txt");
        }
        
        FileStream stream = File.OpenRead(path);
        BinaryReader reader = new BinaryReader(stream);
        //enemy blocks appear to be exactly 92 bytes long.
        const int enemySize = 0x5C;
        //also, EnemyData's non-header data starts at offset 30.
        const int enemyStart = 0x30;
        stream.Position = enemyStart;


        while (reader.BaseStream.Position < reader.BaseStream.Length) //we'll also break out if the data doesn't align properly.
        {
            EnemyData toAdd = new EnemyData(); //enemy entry to add
            byte[] enData = reader.ReadBytes(enemySize); //get the data for the next enemy
            int offset = 0; //an offset to help us know where we are.
            toAdd.RawData = enData;
            toAdd.Hp = BitConverter.ToUInt16(enData, offset);
            offset += 2; //get to next section
            offset += 1; //buffer byte
            //TP is, for whatever reason, stored in Big Endian. 
            toAdd.Tp = BinaryPrimitives.ReadUInt16BigEndian(enData.AsSpan(offset));
            offset += 2;
            //Now we get the bytes that I don't know how to process yet.
            //There's 35 of them in total, including their buffer byte at the end.
            byte[] LancarseInsanity = enData[offset..(offset + 35)];
            toAdd.TangledData = LancarseInsanity;
            offset += 35;
            //Item IDs
            toAdd.Items[0] = BitConverter.ToUInt16(enData, offset);
            offset += 2;
            toAdd.Items[1] = BitConverter.ToUInt16(enData, offset);
            offset += 2;
            toAdd.Items[2] = BitConverter.ToUInt16(enData, offset);
            offset += 2;
            //Odds of items dropping
            toAdd.ItemOdds[0] = enData[offset];
            offset += 1;
            toAdd.ItemOdds[1] = enData[offset];
            offset += 1;
            toAdd.ItemOdds[2] = enData[offset];
            offset += 1;
            toAdd.DropConditional = enData[offset];
            offset += 1;
            //Basic attack data (namely, damage & accuracy)
            toAdd.BasicDmgType = (DAMAGE_TYPES)enData[offset];
            offset += 1;
            toAdd.BasicAcc = enData[offset];
            offset += 1;
            //Experience on death
            toAdd.Experience = BitConverter.ToUInt32(enData, offset);
            offset += 4;
            //EnemyID and ScriptIDs.
            //assuming the enemyID and scriptID parts are ASCII...
            toAdd.EnemyId = Encoding.ASCII.GetString(enData[offset..(offset + 6)]).TrimEnd('\0');
            offset += 6;
            offset += 10; //ten buffer bytes, since all enemy IDs are six chars total.
            //Because scripts are arbitrarily-length up to 17 characters with one buffer, I'll add the whole buffer here.
            toAdd.ScriptId = Encoding.ASCII.GetString(enData[offset..(offset + 18)]).TrimEnd('\0');
            offset += 18;
            toAdd.CodexNum = BitConverter.ToUInt16(enData, offset); //Last byte is the location in the codex.
            //run the Lancarse Disentangler
            toAdd.DisentangleData();
            //done?? Let's verify the data with both a naive check and a less naive one. 
            //Naive one is to check that EnemyID actually starts with 'e', because it always should.
            //The less-naive one is to check the last character of the raw data. If it's not null,
            //something went wrong.
            if (toAdd.EnemyId[0] != 'e' || toAdd.RawData[enData.Length - 1] != 0x00)
            {
                break; //exit without adding the garbage data
            }
            
            enemies.Add(toAdd); //add new data to the 'enemies' list, and continue the loop
            
            //This check makes sure that there's enough room in the index to get the next enemy.
            //if there's not, we break.
            if ((reader.BaseStream.Position + enemySize) > reader.BaseStream.Length)
                break;
        }

        stream.Close();
        OutputToFile();
    }

    private void OutputToFile()
        {
            //empty the file first
            File.WriteAllText(outputPath, "");
            foreach (EnemyData enemy in enemies)
            {
                File.AppendAllText(outputPath, enemy.ToString());
            }
            Console.WriteLine(outputPath);
        }

    public List<EnemyData> GetEnemies()
    {
        return enemies;
    }
    }




/* Down here, we're going to try to figure out what is what.



 Enemy data does not start at the en_code part!
 There's a section that has six values that appear to repeat that I thought was part of the block below...
 ...But, the very first dummy has it above it.

 If we assume the codex number is the last piece of relevant data...

 Hedgehog:
 2D 00 00 00 01 1C 70 00 0E 30 60 00 64 2C 91 01 96 C8 90 01 64 C8 90
 01 64 C8 90 01 64 C8 90 01 64 C8 90 01 64 04 00 00 A2 0F 00 00 A3 0F
 32 00 32 41 02 63 4F 00 00 00 65 6E 30 30 34 00 00 00 00 00 00 00 00
 00 00 00 73 63 72 5F 68 65 64 67 65 68 6F 67 00 00 00 00 00 00 02 00

 Shelltor:
 A0 05 00 00 02 BC 00 03 32 C0 F0 02 4B 96 2C 01 64 C8 90 01 0A 14 64
 00 19 32 64 00 19 32 64 00 19 32 64 00 32 58 00 00 10 10 88 11 00 00
 23 5A 00 FF 01 63 00 00 00 00 65 6E 31 30 35 00 00 00 00 00 00 00 00
 00 00 00 73 63 72 5F 69 72 6F 6E 74 75 72 74 6C 65 00 00 00 00 6A 00

 Ur-Child:
 A8 61 00 00 03 B8 E1 06 64 08 E2 06 64 C8 90 01 64 C8 90 01 00 00 28
 00 0A 14 28 00 0A 0A 28 00 0A 14 28 00 0F A0 00 00 5E 10 00 00 00 00
 64 00 00 FF 01 63 00 00 00 00 65 62 30 30 36 00 00 00 00 00 00 00 00
 00 00 00 73 63 72 5F 61 64 61 6D 00 00 00 00 00 00 00 00 00 00 90 00

 Fuck it, let's add one more of each of enemy/FOE/boss.

 Poseidon:
 F4 01 00 00 01 80 90 01 16 64 40 01 4B 96 2C 01 7D 2C F5 01 64 C8 90
 01 64 C8 90 01 64 C8 90 01 64 C8 90 01 64 38 00 00 D6 0F 00 00 00 00
 28 00 00 FF 01 63 E0 05 00 00 65 6E 30 39 37 00 00 00 00 00 00 00 00
 00 00 00 73 63 72 5F 6D 75 62 65 6E 62 65 00 00 00 00 00 00 00 1C 00

 Raptor:
 58 02 00 00 02 58 60 01 18 60 60 01 64 C8 90 01 64 FA 90 01 0A 14 64
 00 19 32 64 00 19 32 64 00 19 32 64 00 32 1E 00 00 BB 0F B3 0F 00 00
 1E 5F 00 FF 00 63 00 00 00 00 65 6E 30 35 38 00 00 00 00 00 00 00 00
 00 00 00 73 63 72 5F 68 69 67 68 6C 61 70 74 65 72 00 00 00 00 62 00

 Chimera:
 78 05 00 00 03 58 60 01 12 50 60 01 64 C8 90 01 64 FA 90 01 00 00 50
 00 14 28 50 00 14 14 50 00 19 32 64 00 19 24 00 00 BE 0F 00 00 2F 10
 3C 00 64 0E 01 63 08 52 00 00 65 62 30 30 31 00 00 00 00 00 00 00 00
 00 00 00 73 63 72 5F 63 68 69 6D 61 69 72 61 00 00 00 00 00 00 80 00


For the Lancarse madness, these are the bytes we don't know.


 Hedgehog
 1C 70 00 0E 30 60 00 64 2C 91 01 96 C8 90 01 64 C8 90
 01 64 C8 90 01 64 C8 90 01 64 C8 90 01 64 04

 Shellgor
 BC 00 03 32 C0 F0 02 4B 96 2C 01 64 C8 90 01 0A 14 64
 00 19 32 64 00 19 32 64 00 19 32 64 00 32 58

 Ur-Child
 B8 E1 06 64 08 E2 06 64 C8 90 01 64 C8 90 01 00 00 28
 00 0A 14 28 00 0A 0A 28 00 0A 14 28 00 0F A0

 Poseidon
 80 90 01 16 64 40 01 4B 96 2C 01 7D 2C F5 01 64 C8 90
 01 64 C8 90 01 64 C8 90 01 64 C8 90 01 64 38

 Raptor
 58 60 01 18 60 60 01 64 C8 90 01 64 FA 90 01 0A 14 64
 00 19 32 64 00 19 32 64 00 19 32 64 00 32 1E

 Chimera
 58 60 01 12 50 60 01 64 C8 90 01 64 FA 90 01 00 00 50
 00 14 28 50 00 14 14 50 00 19 32 64 00 19 24



 Levels:
 Hedgehog: 02
 Shellgor: 2C
 Ur-Child: 50
 Poseidon: 1C
 Raptor:   0F
 Chimera:  12

Okay, so here's what Araxxor had to say on enemy data:

"It goes 1 byte, 1 byte, and then 2 bytes before repeating that pattern again.

For reference, here's Ur-Child's data as EO2 handles it:

The stats are handled worse than I thought. Ur-Child's STR stat is indeed 110, not 46. 
The reason I thought this was because I saw the hex value of B8 (which is 184 in decimal. 
Divide that by 4 to get 46.) where the STR values are stored. But there's a hex value of 6E1 right next to it, 
which translates to 1761. Divide that by 16, and you get the VIT value of 110, but there's a remainder of 1. 
You're supposed to attach that remainder to the front of B8, turning it into 1B8, which is 440 in decimal. 
Then divide that by 4 to get 110. Simple!"

B8 E1 06 64 08 E2 06 64 C8 90 01 64 C8 90 01 00 00 28
00 0A 14 28 00 0A 0A 28 00 0A 14 28 00 0F A0

If that's the case...

B8 E1 06 64 08 E2 06 64 C8 90 01 is the approximate stat block. Or, with the 1-1-2 pattern,

(B8) (E1 06) (64) (08) (E2 06)...

Ur-Child's stat line is 110/110/110/100/130 STR/VIT/TEC/AGI/LUC.

B8 E1 06 gets STR and VIT

So, 64 08 6E2 should get TEC and AGI?

Well, wait. 64 = 100. That is AGI. AGI is the one stat I could always pick out.

6E2 = 1762 = 110r2

0x208 = 520 (520 / 4 = 130)

That's TEC and...LUC. Wat. It works??

So, the bit order is...

(STR) (VIT) (AGI) (LUC) (TEC)?

Well, that's hard to assume when it has stats that are the same. The ones we know for certain are...

(?) (?) (AGI) (LUC) (?)

Shelltor (47/48/50/48/47 STR/VIT/AGI/LUC/TEC)
BC 00 03 32 C0 F0 02
(BC) (00 03) (32) (C0) (F0 02)

2F0 = 752, /16 = 47r0 (which is STR or TEC)...

0C0 = 192/4 = 48 (LUC, as expected)

0x032 = 50 (AGI, as expected)

0x300 => 768 / 16 = 48r0 (VIT)

BC => 188 / 4 = 47 (STR or TEC)...

Okay, so the order in the EO Utility links (specifically in the 'EnemyData.tbb order' section is correct.
That is cursed. 

But that means the rest of the data is resistances and level. If I trust the utility links, the order should be...
Cut, Bash, Stab, Fire, Ice, Volt, Death, Petrify, Sleep, Confuse, Fear, Poison, Blind, Curse, 
Paralyze, Head, Arms, Legs, Stun, Level

Ur-Child's remaining bytes are
64 C8 90 01 64 C8 90 01 00 00 28 00 0A 
14 28 00 0A 0A 28 00 0A 14 28 00 0F A0

And for his resistances:
- He takes 100% from all damage types
- He has a 10% resistance to all ailments (except Curse, which is 5, and Stun, which is 15).
- He's immune to the two Instant Death options.
- He is level 80.

If it continues to do the 1-1-2 pattern here...
(64) (C8) (90 01) (64) (C8) (90 01) (00) (00) (28 00)
(0A) (14) (28 00) (0A) (0A) (28 00) (0A) (14) (28 00) (0F) (A0)

Which in decimal...
(100) (200) (400) (100) (200) (400) (00) (00) (40)
(10) (20) (40) (10) (10) (40) (10) (20) (40) (15) (160)

...Huh. Those get the correct values if we apply division. Divide by 1, Divide by 2, Divide by 4, repeat.
Why are they bitshifted like that?

...Hey, actually, that's almost the same as the stat line, except done twice...
/4  /16  /1  /4  /16

If it continues to do the 1-1-2 pattern here...
(64) (C8) (90 01) 
(64) (C8) (90 01) 
(00) (00) (28 00)

(0A) (14) (28 00)
(0A) (0A) (28 00) 
(0A) (14) (28 00) 
(0F) (A0)

Which in decimal...
(100) (200) (400) 
(100) (200) (400) 
(00) (00) (40)
(10) (20) (40) 
(10) (10) (40) 
(10) (20) (40) 
(15) (160)

Order of things in two byte chunks (* = one byte)
- HP
- TP?? (Report if any of these two bytes aren't 00 00)
- Enemy type* (01 = normal, 02 = FOE, 03 = boss)

- This, I assume, is where the Lancarse insanity happens.

- One buffer byte (report if not empty)
- Item ID (in little endian) of the first dropped item
- Item ID of the second dropped item
- Item ID of the conditional item (i.e. 4003 for the Hedgehog's)
- Chance of drops in first and second slots respectively
- Conditional tags. First byte is % chance (0x64 = 100%), second byte is tag type
- Basic Attack details - 0/1/2 = Cut/Bash/Pierce. Then accuracy? (0x63 = 99, which sounds right.)
- Four bytes for the amount of EXP granted in little Endian (i.e.  Chimera's 21,000 is 08 52 00 00)
- generic enemy id (en, or rarely eb for bosses. 65 6E/62, then numbers starting at 001.
- Eleven null characters as a buffer.
- The name of their AI script, starting with scr_(73 63 72 5F). Usually related to the romaji of
whatever they call the enemy.
- A number of null characters to make the script section take 17 bytes + 1 buffer null
- The position in the codex the enemy can be found*
- One buffer byte



*/
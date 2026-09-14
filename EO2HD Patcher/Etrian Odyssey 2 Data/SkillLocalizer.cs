using System.IO;
using Microsoft.Win32;

namespace EO2HD_Patcher.Etrian_Odyssey_2_Data;

//This contains the information for myself for translated skills.
//Or, rather, the function to create it and give the text.
public class SkillLocalizer
{
    //Classes are not in the files I have to translate just yet.
    private static Dictionary<string, string> classes = new()
    {
        ["SWORDMAN"] = "Landsknecht",
        ["RANGER"] = "Survivalist",
        ["PALADIN"] = "Protector",
        ["MEDIC"] = "Medic",
        ["DARKHUNTER"] = "DarkHunter",
        ["BUSHIDO"] = "Ronin",
        ["ALCHEMIST"] = "Alchemist",
        ["BARD"] = "Troubadour",
        ["CURSEMAKER"] = "Hexer",
        ["GUNNER"] = "Gunner",
        ["SHAMAN"] = "WarMagus",
        ["PET"] = "Beast",
        ["ITEM"] = "Item"
    };
    
    //Items aren't in the label_skills, so I need to bring those over manually:
    private static Dictionary<string, string> itemTranslations = new()
    {
        ["全員生き返り"] = "Ultimate Healing",
        ["斬防御"] = "Cut Mist",
        ["壊防御"] = "Bash Mist",
        ["突防御"] = "Stab Mist",
        ["物理防御"] = "Phys Mist",
        ["火防御"] = "Fire Mist",
        ["氷防御"] = "Ice Mist",
        ["雷防御"] = "Volt Mist",
        ["魔法防御"] = "Evil Mist",
        ["全防御"] = "All Mist",
        ["リザレクション"] = "Nectar",
        ["弱体解除"] = "Metopon", //Debuff removal.
        ["攻撃強化"] = "Bravant",
        ["防御強化"] = "Stonard",
        ["速度強化"] = "Speed Boost Item" //"Speed Boost", unused item
    };

    //idToSkill is a Dictionary to connect the ID of a skill to the localized text,
    //as extracted from the game data itself.
    private static Dictionary<int, string> idToSkill = new();

    private string path = "";
    //private string outputPath = "";

    //On initialization, SkillLocalizer asks for the location of label_skills.txt,
    //then uses it to construct idToSkill.
    public SkillLocalizer()
    {
        Microsoft.Win32.OpenFileDialog dlg = new OpenFileDialog();
        
        dlg.FileName = "label_skills.txt";
        dlg.DefaultExt = ".txt";
        dlg.Filter = ".txt files (.txt)|*.txt";
        
        Nullable<bool> result = dlg.ShowDialog();
        if (result.Value)
        {
            path = dlg.FileName;
            //outputPath = Path.Combine(Path.GetDirectoryName(path), "output.txt");
        }
        else
            Environment.Exit(0);
        
        StreamReader stream = new StreamReader(path);
        while (true) //we'll break out at EOF
        {
            string? datum = stream.ReadLine();
            if (datum == null)
                break;
            //Each line is tab-separated, so it's nice and easy.
            string[] data = datum.Split('\t');
            idToSkill.Add(
                int.Parse(data[0]),
                data[1]);
        }
        //All done!
    }

    //Player skills will have the Japanese name and ID passed in.
    public static string GetSkillName(string JPName, int id)
    {
        //The JP name as stored as, for example, "SHAMAN  ＨＰブースト"
        //In the old method, I needed both of those, and I still do for items.
        string[] JP_split = JPName.Split("  ");
        if (idToSkill.ContainsKey(id))
            return JP_split[0] + " " + GetSkillName(id); //id exists, all good
        //id doesn't exist, so it's an item or unknown.
        string toReturn = "";
        //left side
        toReturn = classes.TryGetValue(JP_split[0], out toReturn) ? toReturn : "UNKNOWN CLASS";
        toReturn += " ";
        //right side
        toReturn += itemTranslations.TryGetValue(JP_split[1], out toReturn) ? toReturn : "UNKNOWN SKILL";
        
        return toReturn;
        
    }
    //Enemy skills will have their ID number passed in.
    public static string GetSkillName(int id)
    {
        if (idToSkill.TryGetValue(id, out string? name))
            return name;
        return "UNKNOWN SKILL ID";
    }

}
using System.IO;
using Microsoft.Win32;

namespace EO2HD_Patcher.Etrian_Odyssey_2_Data;

using Etrian_Odyssey_2_Data;

/// <summary>
/// Enemy formations are fairly easy to determine from EncountData.tbb.
/// They appear to be separated by null values, with each non-null value corresponding to the
/// enemy's ID.
/// No idea how the game pulls them out, though.
/// </summary>
public class FormationData
{
    private static string path;
    private string outputPath = "";

    private List<Formation> _formations = new List<Formation>();


    public FormationData(List<EnemyData> enemies)
    {
        Microsoft.Win32.OpenFileDialog dlg = new OpenFileDialog();

        dlg.FileName = "EncountData";
        dlg.DefaultExt = ".tbb";
        dlg.Filter = ".tbb files (.tbb)|*.tbb";

        Nullable<bool> result = dlg.ShowDialog();
        if (result.Value)
        {
            path = dlg.FileName;
            outputPath = Path.Combine(Path.GetDirectoryName(path), "formation_output.txt");
        }

        FileStream stream = File.OpenRead(path);
        BinaryReader reader = new BinaryReader(stream);
        //To avoid reading the TBB1/TBL1 parts as data, we'll start on the first null character after, which is offset 25.
        stream.Position = 0x025;
        //Similarly, offset 2760 is where the TBL1 footer starts, so we'll only read until that point.
        while (stream.Position < 0x2760)
        {
            byte data; //a single byte to use with the reader.
            //read through non-null bytes
            do
            {
                data = reader.ReadByte();
            } while (data == 0x00);

            //we've reached a non-null byte, so let's get the non-nulls now.
            byte[] formData = reader.ReadBytes(5);
            //kinda sloppy, but I didn't plan what this would be ahead of time. 
            Formation toAdd = new Formation(formData[0], formData[1], formData[2], formData[3],
                formData[4]);
            //For whatever reason, there are values that produce empty formations anyway.
            //So, lazily, we'll just check if the ToString is a certain minimum length.
            if (toAdd.ToString().Length >= 16)
                _formations.Add(toAdd); 

            //continue to loop until we reach the end of our file.
        }

        stream.Close();
        OutputToFile(enemies);
    }

    //actual structure data for any formation.
    //enemy formations require at least one enemy and contain at most five.
    private struct Formation(byte e1, byte? e2, byte? e3, byte? e4, byte? e5)
    {
        private byte[] enemies =
        [
            e1,
            e2 ?? 0x00,
            e3 ?? 0x00,
            e4 ?? 0x00,
            e5 ?? 0x00
        ];

        public override string ToString()
        {
            string toReturn = "Formation of: ";
            string DictLookUp = "";
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == 0x00)
                {
                    //reached the end of the formation already
                    break;
                }

                if (i > 0)
                    toReturn += ", "; //add the comma.

                //Actually look up the enemy according to this program's dictionary.
                DictLookUp = "en" + enemies[i].ToString("D3");
                toReturn += EnemyDictionaries.IDToEnemy.GetValueOrDefault(DictLookUp, "ERROR") +
                            "(" + DictLookUp + ")";
            }

            return toReturn;
        }

        public string ToString(List<EnemyData> enemyData)
        {
            string toReturn = this.ToString(); //start with the basic ToString
            uint expTotal = 0;
            string ListLookUp = "";
            

            //construct the full en_xxx for each, then look it up in the passed-in list
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] == 0x00)
                {
                    //reached the end of the formation already
                    break;
                }

                ListLookUp = "en" + enemies[i].ToString("D3");
                //O(n) lookup, but w/e.
                EnemyData? lookup = enemyData.FirstOrDefault(x => x.EnemyId == ListLookUp);
                if (lookup != null)
                    expTotal += lookup.Experience;
                else
                    Console.WriteLine(ListLookUp + " EXP lookup failed.");
                //special case: Woodmai's are en001, but so are the Dummy. By default,
                //this results in them only being worth 1 EXP in this, but they actually drop 84.
                if (ListLookUp == "en001")
                    expTotal += 83;
            }

            return toReturn + "\nTotal exp: " + expTotal + "\n";
        }
    }

    private void OutputToFile(List<EnemyData> enemies)
    {
        //empty the file first
        File.WriteAllText(outputPath, "");
        foreach (Formation encounter in _formations)
        {
            File.AppendAllText(outputPath, encounter.ToString(enemies));
        }

        Console.WriteLine(outputPath);
    }
}
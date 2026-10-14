using System.IO;

namespace EO2HD_Patcher.Imported_Code;
//The Imported Code folder is primarily so that I have my sources as directly cited as possible.

//Rea's EO3 MasterData reader.
//Sourced from a Discord message by Rea in the Etrian Odyssey Modding discord,
//channel #eo2-modding-chat,
//dated 2023-07-03.
public class MasterData_Reader
{
    MasterData_Reader()
    {
        var dataPath = "MasterData.bin";
        var offsetPath = "MasterOffset.bin";
        var outputPath = "MasterData/";
        var filenames = File.ReadAllLines("EO3MasterDataFilenames.txt"); //"that's a file i made myself using the MasterData file list in the IL2CPP metadata"
        var offsets = new List<int>();
        using var offsetReader = new BinaryReader(new FileStream(offsetPath, FileMode.Open));
        while (offsetReader.BaseStream.Position < offsetReader.BaseStream.Length)
        {
            offsets.Add(offsetReader.ReadInt32() << 2);
        }

        using var dataReader = new BinaryReader(new FileStream(dataPath, FileMode.Open));
        for (int i = 0; i < offsets.Count; i += 1)
        {
            var length = i == (offsets.Count - 1)
                ? dataReader.BaseStream.Length - offsets[i]
                : offsets[i + 1] - offsets[i];
            dataReader.BaseStream.Position = offsets[i];
            var data = dataReader.ReadBytes((int)length);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(outputPath, $"{filenames[i]}")));
            File.WriteAllBytes(Path.Combine(outputPath, $"{filenames[i]}"), data);
        }
    }
}

//A conversation with Asteras on that date established what, approximately, EO3MasterDataFilenames was...

//In the IL2CPP decomp, these are the variables in the nested type 'public enum DataEnum // TypeDefIndex: 10549' under MasterDataTypes in MasterData.cs.
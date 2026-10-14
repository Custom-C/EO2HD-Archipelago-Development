namespace EO2HD_Patcher.Etrian_Odyssey_2_Data.Real_EO2_Data;

public static class DataEnumHelper
{
    public static string GetFileNameAndPath(this MasterDataMasterDataTypesDataEnum dataType)
    {
        string fileName = dataType.ToString(); //store the dataType.ToString() so I don't have to type it every time
        string fileType = fileName.Substring(fileName.LastIndexOf('_')); //get the file type
        fileType = fileType.Replace('_', '.'); //replace the underscore with a period
        int fullPathLength = fileName.Length - fileType.Length; 
        string fileNameAndPath = fileName.Substring(0, fullPathLength); //get the filepath
        fileNameAndPath = fileNameAndPath.Replace('_', '\\'); //replace the filepath's underscores with backslashes

        return fileNameAndPath + fileType;
    }

    public static string GetFileName(this MasterDataMasterDataTypesDataEnum dataType)
    {
        string fileName = dataType.GetFileNameAndPath(); //I did most of the work already, why would I repeat it?
        string toReturn = fileName.Substring(fileName.LastIndexOf('\\') + 1); //get the last \, then advance by one to not get that as part
        return toReturn;
    }
}
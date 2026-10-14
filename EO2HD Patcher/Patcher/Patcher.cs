using System.IO;
using System.Windows;

namespace EO2HD_Patcher.Patcher;

internal class Patcher
{
    //passed-in variables
    private string filepath;
    private PatcherSettings settings;
    
    //directions to the files we need to modify
    private string pathToMaster;
    private string pathToQuests;
    private string pathToItems;
    
    //start-stop bool.
    private bool goTime = false;

    internal Patcher(string incFilePath, PatcherSettings settings)
    {
        //initialize patcher parameters
        this.settings = settings;
        //try to get the directory path for 'Etrian Odyssey 2.exe'. Because this is user-facing, we need to catch a few arguements.
        try
        {
            filepath = Path.GetDirectoryName(incFilePath)!;
        }
        catch (ArgumentNullException)
        {
            Console.WriteLine("The file path " + incFilePath +
                              " is returning null.\nThis shouldn't happen, but this is here anyway.");
            filepath = "";
        }
        catch (IOException)
        {
            Console.WriteLine("IO Exception. Tried to pass " + incFilePath + " as file path.");
        }
        catch (Exception)
        {
            Console.WriteLine("Unknown exception occurred with " + incFilePath + ".\n Program reports " + typeof(Exception));
        }
        
        //no immediate problems, we should be good to go?
        goTime = true;
        //filepath should be good here? We'll still verify that the file exists.
        //find required files
        pathToMaster = incFilePath + @"\Etrian Odyssey 2_Data\StreamingAssets\aa\StandaloneWindows64\masterdata_assets_all_3ab2e96e34efe8531492a1731d22e43b.bundle";
        if (!File.Exists(pathToMaster))
        {
            MessageBox.Show("Master file does not exist (where I expect it)!!");
            goTime = false;
        }
        
        //filepaths have been (attempted to be) located, start the patching process if we're good.
        if (goTime)
            StartPatches();
        //if not, inform the user that patching was NOT done.
        else
            MessageBox.Show(
                "Patching did not occur. \nPlease report the boxes that showed before this one as a bug report!");
    }
    //StartPatches is a function that, in turn, calls all relevant patching functions based on the parameters in settings.
    private void StartPatches()
    {
        
    }
}

using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EO2HD_Patcher.Etrian_Odyssey_2_Data;
using Microsoft.Win32;
using System.Security.Cryptography;
using EO2HD_Patcher.Patcher;

namespace EO2HD_Patcher;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    //Hash variables. In order to make sure the .exe is the correct patch,
    //we compare the hashes to the one I got on my version. 
    //Every patch except the Unity security update changes the .exe, so this should work.
    private const string ExpectedHash = "44F4D81941A536175AED2988D2B9FE1A30AAA25617A6C2EE4DB3803893E8B285";
    private byte[] incHash;
    SHA256 sha256 = SHA256.Create();
    //path storage. EO2Path contains the program's initial label text, but it'll contain the folder of the game.
    //filePath will contain the actual .exe's file path.
    private string EO2Path = "steam/common/steamapps/common/EO2HD/Etrian Odyssey 2.exe";
    private string filePath = "";
    
    //misc variables.
    private bool ready = false; //this lets the UI know to enable the patch button.
    private PatcherSettings settings; //the flags for the various patches done here.

    public MainWindow()
    {
        InitializeComponent();
        EO2PathLabel.Content = EO2Path;
    }

    private void GetFilePath(object sender, RoutedEventArgs e)
    {
        //reset ready var
        ready = false;
        Microsoft.Win32.OpenFileDialog dlg = new OpenFileDialog();
        dlg.FileName = "Etrian Odyssey 2.exe";
        dlg.DefaultExt = ".exe";
        dlg.Filter = ".exe files (.exe)|*.exe";

        Nullable<bool> result = dlg.ShowDialog();
        if (result.Value)
        {
            //set the filepath according to the dialogue
            filePath = dlg.FileName;
            //And verify.
            CheckFile();
        }
        else
        {
            MessageBox.Show("Error with the file path.");
            //reset the label box
            ResetLabel();
        }
    }

    //CheckFile is the logic to check if the EO2 .exe is both a) actually called 'Etrian Odyssey 2.exe',
    //and b) to check the hash to make sure it's the right version.
    //If both are successful, we let the UI know that we can patch.
    private void CheckFile()
    {
        //check 1: filepath names.
        //Steam literally won't run the game unless the executable is named exactly 'Etrian Odyssey 2.exe'.
        if (!string.Equals(
                System.IO.Path.GetFileName(filePath),
                "Etrian Odyssey 2.exe",
                StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show("This does not look like 'Etrian Odyssey 2.exe'.");
            ResetLabel();
            return;
        }
        //filepath check passed, so we can solidify the UI's information.
        EO2PathLabel.Foreground = Brushes.Black;
        EO2PathLabel.Content = filePath;
        //check 2: SHA256 hashes.
        //The patcher requires the non-Denuvo version, i.e. the very first depot.
        //Other versions will not have the same hash that I do.
        incHash = sha256.ComputeHash(File.ReadAllBytes(filePath));
        string compHash = Convert.ToHexString(incHash);
        if (compHash != ExpectedHash) //comparison failed
        {
            MessageBox.Show("Hash comparison failed.\nDid you remember to downgrade to the Denuvo-less version?");
            return;
        }
        //comparison succeeded, so this is the right game and version.
        ready = true;
    }
    private void ResetLabel()
    {
        EO2PathLabel.Content = "steam/common/steamapps/common/EO2HD/Etrian Odyssey 2.exe";
        EO2PathLabel.Foreground = Brushes.LightSlateGray;
    }

    //SetPatcherOptions is a function that ensures that each option in the PatcherSettings object is set to the displayed setting here.
    //It is typically used when the Patch button is clicked.
    private void SetPatcherOptions()
    {
        //We'll start in order of the displayed variables in the MainWindow's GUI.
        settings.baseSettings.Enabled = ArchiToggle.IsChecked ?? false;
        //Immediately, we have to jump around a bit due to Death Link only having one determining variable, its Enum.
        if (DeathLink == null || DeathLink.IsChecked == false) //no Death Link
            settings.baseSettings.DeathLink = 0; //None
        else //Death Link is enabled, get the mode
            settings.baseSettings.DeathLink = (DeathLinkModeEnum)GetComboBoxTag(DeathLinkMode);
        //back to the XAML order, starting the Items section
        settings.baseSettings.ProLvlCap = (ProLvl.IsChecked ?? false) && ProLvl.IsEnabled;
        settings.baseSettings.ProStratumAccess = ProStrat.IsChecked ?? false;
        settings.baseSettings.ProShop = ProShop.IsChecked ?? false;
        settings.baseSettings.ProBag = ProBag.IsChecked ?? false;
        settings.baseSettings.ProMedals = ProMedals.IsChecked ?? false;
        settings.baseSettings.ClassLock = ClassLock.IsChecked ?? false;
        //Check Locations section
        settings.baseSettings.CheckQuests = CheckQuest.IsChecked ?? false;
        settings.baseSettings.CheckGlossary = CheckGloss.IsChecked ?? false;
        settings.baseSettings.CheckArbitraryGlossary = CheckArbGlos.IsChecked ?? false;
        settings.baseSettings.CheckScaledArbitraryGlossary = (CheckArbGlosScaled.IsChecked ?? false) && CheckArbGlosScaled.IsEnabled;
        settings.baseSettings.CheckFloors = CheckFloors.IsChecked ?? false;
        settings.baseSettings.CheckLevels = CheckLvlMiles.IsChecked ?? false;
        settings.baseSettings.CheckBosses = CheckNonSBosses.IsChecked ?? false;
        //And now the combo boxes.
        settings.baseSettings.ArbitraryGlossaryIndex = GetComboBoxTag(CheckArbiGlosVal);
        settings.baseSettings.LevelMilestoneIndex = GetComboBoxTag(CheckLvlMilesVal);
        
        //we'll jump to the right to get the relevant combo boxes for Archipelago that are there, too.
        settings.baseSettings.Goals = (Goals)GetComboBoxTag(GoalVal);
        settings.baseSettings.FillerPackages = (FillerPackages)GetComboBoxTag(FillerItems);
        settings.baseSettings.ShopUnlockMode = (ShopUnlockMode)GetComboBoxTag(ProShopVal);
        settings.baseSettings.ProBagIndex = GetComboBoxTag(ProBagVal);
        settings.baseSettings.ClassLockNum = GetComboBoxTag(ClassLockVal);
        
        //Base Randomization settings. These apply even without Archipelago.
        settings.randomSettings.ExpMultIndex = GetComboBoxTag(ExpMult);
        settings.randomSettings.LevelCapIndex = GetComboBoxTag(LvlCapsVal);
        settings.randomSettings.LevelCaps = LvlCaps.IsChecked ?? false;
        
        //Quality of Life settings. Like the base randomizer settings, these apply without Archipelago, too.
        settings.qolSettings.FOEsGiveExperience = Q_FOEs.IsChecked ?? false;
        settings.qolSettings.ConditionalsAlwaysDrop = Q_Conds.IsChecked ?? false;
        settings.qolSettings.ItemsAlwaysDrop = Q_Drops.IsChecked ?? false;
        settings.qolSettings.NoConditionals = Q_NoCon.IsChecked ?? false;
        settings.qolSettings.Level99Cap = Q_LvlCap.IsChecked ?? false;
        settings.qolSettings.FreeRests = Q_Rest.IsChecked ?? false;
        settings.qolSettings.FreeRetires = Q_Retire.IsChecked ?? false;
        settings.qolSettings.NerfMuckdile = Q_BadCroc.IsChecked ?? false;
        //Class reworks, eventually.
        settings.qolSettings.ClassReworks[0] = Re_L.IsChecked ?? false;
        settings.qolSettings.ClassReworks[1] = Re_S.IsChecked ?? false;
        settings.qolSettings.ClassReworks[2] = Re_P.IsChecked ?? false;
        settings.qolSettings.ClassReworks[3] = Re_M.IsChecked ?? false;
        settings.qolSettings.ClassReworks[4] = Re_A.IsChecked ?? false;
        settings.qolSettings.ClassReworks[5] = Re_T.IsChecked ?? false;
        settings.qolSettings.ClassReworks[6] = Re_B.IsChecked ?? false;
        settings.qolSettings.ClassReworks[7] = Re_Cookie.IsChecked ?? false;
    }
    
    //small helper function to get the tag of a selected ComboBoxItem.
    private int GetComboBoxTag(ComboBox box)
    {
        //Special but common case: ComboBox is disabled. In which case, the value should always be 0.
        if (!box.IsEnabled)
            return 0;
        //get the selected item
        ComboBoxItem? item = box.SelectedItem as ComboBoxItem;
        if (item != null && int.TryParse(item.Tag?.ToString(), out int value)) //return the tag's value as an int
            return value;
        return 0; //item was null or the tag wasn't parsable as an int, return 0.

    }
}
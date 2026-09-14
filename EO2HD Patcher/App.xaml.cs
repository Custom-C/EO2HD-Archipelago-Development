using System.Configuration;
using System.Data;
using System.Text;
using System.Windows;
using EO2HD_Patcher.Enemy_Changes;
using EO2HD_Patcher.Etrian_Odyssey_2_Data;

namespace EO2HD_Patcher;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        HexaCode program = new HexaCode();
        program.EO2_Decoder();
        //FormationData formData = new FormationData(program.GetEnemies());
        //SkillLocalizer l = new SkillLocalizer();
        //SkillDataDecoder a = new SkillDataDecoder();
        
    }
}

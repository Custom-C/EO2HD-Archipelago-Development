using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EO2HD_Patcher.Etrian_Odyssey_2_Data;
using Microsoft.Win32;

namespace EO2HD_Patcher;

public partial class DungeonViewer : Window
{
    private string filePath = "";
    private DungeonFloor floor;
    
    public DungeonViewer()
    {
        InitializeComponent();
        FilePathLabel.Content = filePath;
        GenerateGrid(false);
        
        //DPI makes me want to throw something.
        double scale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        //A map of 24x24 pixels fits fine.I just need enough definition to see colour.
        double cellSize = 24 / scale;

        MapGrid.Width = 35 * cellSize;
        MapGrid.Height = 30 * cellSize;
    }

    private void ChangeDungeon(object sender, RoutedEventArgs e)
    {
        Microsoft.Win32.OpenFileDialog dlg = new OpenFileDialog();
        dlg.FileName = "d*.JSON";
        dlg.DefaultExt = ".json";
        dlg.Filter = ".json files (.json)|*.json";

        Nullable<bool> result = dlg.ShowDialog();
        if (result.Value)
        {
            filePath = dlg.FileName;
            floor = new DungeonFloor(filePath); //importantly, regenerate the floor.
            GenerateGrid(true); //and draw it.
            FilePathLabel.Content = filePath;
        }
        else
        {
            MessageBox.Show("Error with the file path.");
        }
    }
    //GenerateGrid creates the elements for the XML's MapGrid. 
    //The boolean is primarily for the initial vs with-data constructions.
    private void GenerateGrid(bool withDungeon)
    {
        //clear it
        MapGrid.Children.Clear();
        
        if (!withDungeon) //initial construction
        {
            for (int i = 0; i < 1050; i++)
            {
                MapGrid.Children.Add(new Border
                {
                    Background = Brushes.LightGreen,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(0.50)
                });
            }
            return;
        }
        else //Dungeon value's been passed into the program, so we can access it.
        {
            Brush BGColor = Brushes.White;
            for (int i = 0; i < 1050; i++)
            {
                //change the tile based on the type
                //Note that while I originally built it with Kya's image using the index,
                //it turns out that indices are not shared among floors.
                //However, what *does* seem to be shared are ChipNo's...
                switch (floor.GetMapData(i).ChipNo)
                {
                    //Green: Walkable tiles. 
                    case 1:
                    {
                        BGColor = Brushes.Green; 
                        break;
                    }
                    case 24: //walkable pain tiles
                    {
                        BGColor = Brushes.SaddleBrown;
                        break;
                    }
                    case 19: //transparent (or partly transparent) blockers(?)
                    {
                        BGColor = Brushes.Gray;
                        break;
                    }
                    case 5: //south->north only hidden door
                    case 6: //north->south only hidden door
                    case 7: //PRESUMABLY, west->east only hidden door
                    case 8: //east->west only hidden door
                    {
                        BGColor = Brushes.DarkCyan;
                        break;
                    }
                        
                    case 9: //north-south facing doors
                    case 10: //east-west facing doors
                    {
                        BGColor = Brushes.Cyan;
                        break;
                    }
                    case 11: //treasure chests
                    {
                        BGColor = Brushes.Yellow;
                        break;
                    }
                    case 17: //FOE spawns? The one spot in floor 1 so far is an anomaly.
                    {       //CHIP_ENEMY in the game's decompiled code, so yep.
                        BGColor = Brushes.Red;
                        break;
                    }
                    case 18: //Pits
                    {
                        BGColor = Brushes.DarkSlateGray;
                        break;
                    }
                    case 3: //stairs Up
                    {
                        BGColor = Brushes.Magenta;
                        break;
                    }
                    case 4: //stairs down
                    {
                        BGColor = Brushes.Magenta;
                        break;
                    }
                    case 12: //Geomagnetic Field
                    {
                        BGColor = Brushes.HotPink;
                        break;
                    }
                    case 13: //Northbound converyor found in S5
                    case 14: //Southbound conveyor
                    case 15: //Westbound conveyor
                    case 16: //Eastbound conveyor
                    {
                        BGColor = Brushes.Goldenrod;
                        break;
                    }
                    case 22: //Warp panel entrances in S6
                    {
                        BGColor = Brushes.Purple;
                        break;
                    }
                    case 23: //Floating platforms in S4
                    {
                        BGColor = Brushes.DarkGoldenrod;
                        break;
                    }
                    case 25: //Geomagnetic Pole
                    {
                        BGColor = Brushes.OrangeRed;
                        break;
                    }
                    case 26: //Ice tiles
                    {
                        BGColor = Brushes.CornflowerBlue;
                        break;
                    }
                    case 0: //null
                    case 2: //default walls rules?
                    {
                        BGColor = Brushes.Black;
                        break;
                    }
                        //default: White, because I don't know what it is yet.
                    default: { BGColor = Brushes.White; break; }
                }
                //For whatever fucking reason, I can't define the Click event in the Button itself, so I need to pull it out of the MapGrid Child assignment and do it right now.
                Button button = new Button
                {
                    Content = floor.GetTileIndex(i).ToString(), //display the value
                    Foreground = Brushes.DarkGray,
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    HorizontalAlignment = HorizontalAlignment.Stretch, //to make the button the width of the box
                    HorizontalContentAlignment = HorizontalAlignment.Center, //to make it always in the center of the box
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = i,
                };
                button.Click += (_,_) => ButtonClicked((int)button.Tag); //Why can't this be in the button constructor????
                MapGrid.Children.Add(new Border
                {
                    Background = BGColor,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(0.50),
                    Child = button
                });
            }
        }
    }

    private void Window_Loaded(object sender, RoutedEventArgs routedEventArgs)
    {
        Console.WriteLine($"Width: {MapGrid.ActualWidth}");
        Console.WriteLine($"Height: {MapGrid.ActualHeight}");
        Console.WriteLine($"Cell: {MapGrid.Children[0].RenderSize.Width} x {MapGrid.Children[0].RenderSize.Height}");
    }

    private void ButtonClicked(int index)
    {
        (int xpos, int ypos) = floor.GetPosFromIndex(index);
        Console.WriteLine("Clicked button @ index " + index + ", position (" + xpos + ", " + ypos + ")");
        MapData toAssign = floor.GetMapData(index);
        //Display the variables of the clicked tile
        ChipNo.Content = toAssign.ChipNo;
        EncountUpRatio.Content = toAssign.EncountUpRatio;
        EncountFixRatio.Content = toAssign.EncountFixRatio;
        ItemGetType.Content = toAssign.ItemGetType;
        EncountFg.Content = toAssign.EncountFg;
        HitFg.Content = toAssign.HitFg;
        WalkCompFg.Content = toAssign.WalkCompFg;
        FoeRangeCheckFg.Content = toAssign.FoeRangeCheckFg;
        YddModel1CutFg.Content = toAssign.YddModel1CutFg;
        AnimFg.Content = toAssign.AnimFg;
        YddModel2CutFg.Content = toAssign.YddModel2CutFg;
        VisibleFg.Content = toAssign.VisibleFg;
        EncountTblId.Content = toAssign.EncountTblId;
        //AnchorData
        rot.Content = toAssign.AnchorData.rot;
        jumpPos.Content = toAssign.AnchorData.jumpPos;
        enterDir.Content = toAssign.AnchorData.enterDir;
        //StairData
        GoFloor.Content = toAssign.StairData.GoFloor;
        GoPosX.Content = toAssign.StairData.GoPosX;
        GoPosY.Content = toAssign.StairData.GoPosY;
        SDRot.Content = toAssign.StairData.Rot;
        SDEnterDir.Content = toAssign.StairData.EnterDir;
        //TbData
        ItemIdOrSum.Content = toAssign.TbData.ItemIdOrSum;
        TbId.Content = toAssign.TbData.TbId;
        TbType.Content = toAssign.TbData.TbType;
        Enable.Content = toAssign.TbData.Enable;
    }
}
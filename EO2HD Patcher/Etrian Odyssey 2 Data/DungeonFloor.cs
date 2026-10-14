using System.IO;
using System.Reflection.Metadata;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace EO2HD_Patcher.Etrian_Odyssey_2_Data;

//DungeonFloor describes the floor as a while, decrypted from 
//defaultlocalgroup_assets_all....bundle, specifically the
//MonoBehaviour/d*.json files. 
//It's not what I'd call fun.
public class DungeonFloor
{
    
    //d*.JSONs start with the Unity headers, then a   "mapDesignData" section. The only relevant parts
// here, I think, are the FileListNum and the ChipNum.
//For example, for 02F, these are 43 and 76 respectively.
//Actually, I think FileListNum isn't relevant, because the next part is FileNameList, which lists (in 02F)
//43 different files. Locating them in AssetStudio, these are for graphics.
//I'm unsure if ChipNum is relevant, but it is actually mentioned in the tile data...

//Anyway, in the .JSON itself, the first relevant information is CellDatas, which is an array of tuples
//...that describe every tile's type (via an index) and its rotation.
//In the original DS game, the index is apparently visible in a hex editor, to the point where
//you can look at the map themselves in a hex editor. In Unity, not so much.

//Oh, and that 'index'? It isn't complete. See the MapData class...

//We know that each map is always 35x30 tiles.
    private const int MapXSize = 35;
    private const int MapYSize = 30;
    //35x30 = 1050, for reference.
    private (int, int)[] floorTiles = new (int, int)[MapXSize * MapYSize];
    private MapData[] mapData = new MapData[MapXSize * MapYSize];
    //When we pass through the data to get to MapData, we get a few potentially interesting pieces of information.
    private int eventCount = 0;
    private int TreasureBoxCount = 0;
    private int StairCount = 0;
    private ushort EnableRouteBit = 0;
    
    //So, question. With these being just massive line, how do we find what position we're in?
    //0,0, or [0], is the top-left. 
    //34,0, or [34], is the top-right.
    //0,29, or [??], is the bottom-left.
    //34,29, or [1049] is the bottom-right.
    
    //So, does that mean it's just x = index % 35, y = index % 30?
    //Maybe? That makes intuitive sense.
    //Random tile, let's say, 354: (4, 24). Okay. What about the next tile?
    // 355: (5, 25). No.
    //So either x or y needs to be transformed based on the other. Likely y based on x.
    //(index / x) % 30? i.e. ((index / (index % 35)) % 30?
    //Oh, but if I'm doing that, I don't also need the second modulus.
    //y = index / (index % 35)
    //No, that produces too high of numbers. Is it just division?
    //y = index / 35?
    //So let's try those random tiles again?
    //354: (4, 10), 355: (5, 10)
    //Okay!
    
    //So, moving west is -1, moving east is +1
    //Moving north and south is tricky. 
    //Let's try with 354 as our starter, and v as our movement.
    //354 => 4 = 354 % 35, 10 = 354 / 35
    //4 = v % 35, 11 = v / 35
    //4 = v % 35, 385 = v
    //4 = 385 % 35
    //4 = 389 % 35
    //Okay, so index+/-35. That makes sense.
    
    //0,29, or [1014], is the bottom-left.
    
    //Anyway. Let's get these values.

    public DungeonFloor(string filepath)
    {
        //We're passed in the filepath here, so no need to get the JSON stuff.

        //streamer and reader for the data
        StreamReader stream = new StreamReader(filepath);
        JsonTextReader reader = new JsonTextReader(stream);

        //Copying from SkillData, stream through data to get through the Unity headers
        while (reader.Read())
        {
            //We've reach the table we're looking for, so...
            if ((reader.TokenType == JsonToken.PropertyName) && ((string)reader.Value! == "CellDatas"))
            {
                //read one more time to get into the array
                reader.Read();
                //process the Floor data
                DecodeFloor(reader);
                //when we break out, we should be at the end of the object, but haven't read it yet.
                //For my own sanity, I had to do this a token at a time. Yes, with Console.WriteLines. 
                reader.Read(); //EndObject token for CellDatas
                reader.Read(); //EndObject token for mapDesignData
                
                reader.Read(); //StartObject token for ygMapData
                reader.Read(); //Start reading into the object
                reader.Read(); //StartObject token for Head
                reader.Read(); //Read into the object.
                
                reader.Read(); //Gets the property name "head"
                reader.Read(); //Gets the property value for head
                
                reader.Read(); //Gets the property name "ver"
                reader.Read(); //Gets the property value for ver
                
                reader.Read(); //Gets the property name "EventCnt".
                reader.Read(); //Gets the property value for EventCnt.
                eventCount = Convert.ToInt32(reader.Value); //Store that one.
                
                reader.Read(); //Gets the property name "TreasureBoxCnt"
                reader.Read(); //Gets the property value for TreasureBoxCnt.
                TreasureBoxCount = Convert.ToInt32(reader.Value); //Store that one too.
                
                reader.Read(); //Gets the property name "StairCnt"
                reader.Read(); //Gets the property value for StairCnt.
                StairCount = Convert.ToInt32(reader.Value); //store it
                
                reader.Read(); //Gets the property name "EnableRouteBit"
                reader.Read(); //Gets the property value for EnableRouteBit.
                EnableRouteBit = Convert.ToByte(reader.Value); //Store that.
                
                reader.Read(); //EndObject for Head
                //The next object is the StartObject for MapData. We're here.
                DecodeMapData(reader); //send it to the decoder
                //break out
                break;
            }
        }
    }
    //Much like my other JSON decoding functions, this function is my dedicated decoder for the type.
    //This time, it's for data from the floors.
    //Much of this - comments included - is copied from SkillData.
    private void DecodeFloor(JsonTextReader reader)
    {
        
        //an index tracker for plugging into floorTiles
        int indexCount = 0;

        //When we're passed the reader, we've already read into the array, so this should get us the token of the first object.
        //We'll break out when we're at the last index.
        while (reader.Read()) //reads the StartObject token.
        {
            //variables to plug into floorTiles once we've got them
            int index = 0;
            int rot = 0;

            reader.Read(); //Gets the token for the property name, "Index"
            reader.Read(); //Gets the token for the value inside
            index = Convert.ToInt32(reader.Value); //Stores the token value
            reader.Read(); //Gets the token for the property name, "Rot"
            reader.Read(); //Gets the token for the value inside
            rot = Convert.ToInt32(reader.Value); //Stores the token value
            reader.Read(); //Gets the EndObject token.
            
            floorTiles[indexCount] = (index, rot);
            if (++indexCount >= 1050) //indexCount is now at the end, break out.
                break;
        }
    }

    private void DecodeMapData(JsonTextReader reader)
    {
        JsonSerializer serializer = new JsonSerializer();
        
        //an index tracker for plugging into our MapData array.
        int indexCount = 0;
        
        //I thought I was at the right place, but apparently I need to read one more time.
        reader.Read();
        reader.Read();
        while (reader.Read())
        {
            MapData TileToAdd = serializer.Deserialize<MapData>(reader);
            
            mapData[indexCount] = TileToAdd;
            if (++indexCount >= 1050) //indexCount is now at the end, break out.
                break;
        }
    }
    
    //a function to get the index from any particular (x,y) on the map, where
    //the top-left is 0,0.
    //Useful for EnemyRoutes and shortcuts.
    public int GetIndexFromPos(int x, int y)
    {
        //first verify that x and y are within the map itself.
        if (x >= MapXSize || y >= MapYSize)
            return -1; //a negative value signifies a problem, rather than just being at index 0.
        //index can be logically deduced.
        //for y = 0, x (and therefore the index) is (0-34)
        //for y = 1, the index is 35-69 (35 * 1 + x)
        //so index = y * the range of x + x's value
        return ((y * MapXSize) + x);
    }
    //A function to get an (x,y) position on the map from an index.
    //Top-left is 0,0.
    //Mostly useful for debugging.
    public (int, int) GetPosFromIndex(int index)
    {
        //verify that index is within map's maximum size.
        if (index > (MapYSize * MapXSize))
            return (-1, -1);
        int retX = (index % MapXSize);
        int retY = index / MapXSize;
        return (retX, retY);
    }

    //GetTile gets the index and the rotation of the tile at the passed-in index.
    public (int, int) GetTile(int index)
    {
        return floorTiles[index];
    }

    public MapData GetMapData(int index)
    {
        return mapData[index];
    }

    public int GetTileIndex(int index)
    {
        return floorTiles[index].Item1;
    }

    public int GetTileRotation(int index)
    {
        return floorTiles[index].Item2;
    }
}

public class MapData
{
    /* So, for every tile in the .JSON, there's a massive object.
     And yes, it's every tile. There are 1050 instances of this object.
     Here's the top-left tile on 2F.
           {
        "ChipNo": 2,
        "EncountUpRatio": 0,
        "EncountFixRatio": 0,
        "AnchorData": {
          "rot": 0,
          "jumpPos": 0,
          "enterDir": 0
        },
        "ItemGetType": 0,
        "EncountFg": 1,
        "HitFg": 0,
        "WalkCompFg": 0,
        "FoeRangeCheckFg": 0,
        "YddModel1CutFg": 0,
        "AnimFg": 0,
        "YddModel2CutFg": 0,
        "VisibleFg": 0,
        "EncountTblId": 0,
        "StairData": {
          "GoFloor": 0,
          "GoPosX": 0,
          "GoPosY": 0,
          "Rot": 0,
          "EnterDir": 0
        },
        "TbData": {
          "ItemIdOrSum": 0,
          "TbId": 0,
          "TbType": 0,
          "Enable": 0
        }
      }
      ...I'm going to make the app able to display this.
      Time to re-learn XAML!
      Well, we need information to display, so let's create the object here too.
      Fortunately, it can be serialized.
     */
    
    public int ChipNo { get; set; }
    public int EncountUpRatio { get; set; }
    public int EncountFixRatio { get; set; }
    public AnchorData AnchorData { get; set; }
    public int ItemGetType { get; set; }
    public int EncountFg { get; set; }
    public int HitFg { get; set; }
    public int WalkCompFg { get; set; }
    public int FoeRangeCheckFg { get; set; }
    public int YddModel1CutFg { get; set; }
    public int AnimFg { get; set; }
    public int YddModel2CutFg { get; set; }
    public int VisibleFg { get; set; }
    public int EncountTblId { get; set; }
    public StairData StairData { get; set; }
    public TbData TbData { get; set; }
    
    
}
//deserialization class for the MapData's AnchorData
public class AnchorData
{
    public int rot { get; set; }
    public int jumpPos {get; set;}
    public int enterDir { get; set; }
}

//deserialization class for the MapData's StairData
public class StairData
{
    public int GoFloor { get; set; }
    public int GoPosX { get; set; } 
    public int GoPosY { get; set; }
    public int Rot { get; set; }
    public int EnterDir { get; set; }
}
//deserialization class for the MapData's TbData
public class TbData
{
    public int ItemIdOrSum { get; set; }
    public int TbId { get; set; }
    public int TbType { get; set; }
    public int Enable { get; set; }
}


//After the MapData comes the EnemyRoute data.
//This is an array of up to 16 references to the potential routes FOEs take on the map.
//If a map doesn't have that many FOEs that follow a route, the references are empty.

//Now, there are some observations to be made about some of them:
//The LoopFlg flag only matters to FOEs that spawn when the floor is entered.
//However, the *route* is still relevant.
//For example, in the top left of 30F, the Riptors don't spawn until you enter the room.
//However, once you leave, they don't de-spawn, instead following the routes depicted...looping.

//FOEs that either don't move or only chase the party don't use the entries here.

//I have no idea how these are assigned.

//For an individual route, here's a deserializable object.
public class EnemyRoute
{
    public int LoopFlg { get; set; } //Loop flag.
    public int StepNum { get; set; } //How many steps there are. 
    public EnemyStep[] EnemyStep { get; set; } //Describes the position of each step.
}

//deserialization class for EnemyRoute's EnemyStep
public class EnemyStep
{
    public int IdxX { get; set; } 
    public int IdxY { get; set; }
}

//The last relevant item in d*.JSONs are the two-way hidden paths.
//(One-ways are handled in whatever Chips 5-8 are.)

//Note that these do not care about the unlock conditions, just their position.
//Those are handled somewhere completely different - the event handler?
public class DGShortcuts
{
    public int PosX { get; set; }
    public int PosY { get; set; }
}
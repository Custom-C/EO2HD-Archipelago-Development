using System.IO;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace EO2HD_Patcher.Etrian_Odyssey_2_Data;

//EventObjCombine.JSON is the only object in eventscriptobjs....bundle, but why?
//This is an attempt to discern some data from it.
public class EventObjData
{
    //Items for statistical analysis & saving
    List<EventCombineObjs> eventObjs = new List<EventCombineObjs>();
    //HashSets sometimes. to make the values unique.
    HashSet<int> EventIDs = new HashSet<int>();
    HashSet<int> PlaceIDs = new HashSet<int>();
    HashSet<int> TablesIDs = new HashSet<int>();
    List<int> StartFlags = new List<int>();
    List<int> EndFlags = new List<int>();
    List<int> StartTempFlags = new List<int>();
    List<int> EndTempFlags = new List<int>();
    HashSet<int> UniqueFlags = new HashSet<int>();
    HashSet<int> ConditionTypes = new HashSet<int>();
    
    //filepath strings and the dialogue to get them
    private string filePath = "";
    private string mainOutputPath = "";
    private string statOutputPath = "";
    private Microsoft.Win32.OpenFileDialog dlg = new OpenFileDialog();
    
    public EventObjData()
    {
        dlg.FileName = "EventObjCombine.JSON";
        dlg.DefaultExt = ".json";
        dlg.Filter = ".json files (.json)|*.json";

        Nullable<bool> result = dlg.ShowDialog();
        if (result.Value)
        {
            filePath = dlg.FileName;
            mainOutputPath = Path.Combine(Path.GetDirectoryName(filePath), "output.txt");
            statOutputPath = Path.Combine(Path.GetDirectoryName(filePath), "stats.txt");
        }
        else
        {
            MessageBox.Show("Something went wrong with the filepath.");
            System.Environment.Exit(0);
        }
        
        //readers for the JSON
        StreamReader stream = new StreamReader(filePath);
        JsonTextReader reader = new JsonTextReader(stream);
        JsonSerializer serializer = new JsonSerializer();
        

        
        //begin reading
        while (reader.Read())
        {
            //read past the header parts: see SkillData for more info
            if ((reader.TokenType == JsonToken.PropertyName) && ((string)reader.Value! == "EventCombineObjs"))
            {
                //read one more time to get into the arrays
                reader.Read();
                //While we're not at the end
                while (reader.Read() && reader.TokenType != JsonToken.EndArray)
                {
                    //serialize
                    EventCombineObjsDeserialized obj = serializer.Deserialize<EventCombineObjsDeserialized>(reader);
                    
                    //convert to real object
                    EventCombineObjs toAdd = new EventCombineObjs(obj);
                    //get our statistics
                    TablesIDs.Add(toAdd.TableNum);
                    foreach (EvtTblEntries e in toAdd.EventTable)
                    {
                            EventIDs.Add(e.EventIDInfo.EventID);
                            PlaceIDs.Add(e.EventIDInfo.PlaceID);
                            StartFlags.Add(e.EventIDInfo.StartFg);
                            EndFlags.Add(e.EventIDInfo.EndFg);
                            StartTempFlags.Add(e.EventIDInfo.StartTmpFg);
                            EndTempFlags.Add(e.EventIDInfo.EndTmpFg);
                            UniqueFlags.Add(e.EventIDInfo.StartFg);
                            UniqueFlags.Add(e.EventIDInfo.EndFg);
                            UniqueFlags.Add(e.EventIDInfo.StartTmpFg);
                            UniqueFlags.Add(e.EventIDInfo.EndTmpFg);
                            ConditionTypes.Add(e.EventIDInfo.ConditionType);
                    }
                    //add the object to the list of objects, and loop
                    eventObjs.Add(toAdd);
                }
            }
        }
        //should be done here, so output to text files.
        OutputToText();
    }

    void OutputToText()
    {
        //empty the files
        File.WriteAllText(mainOutputPath, "");
        File.WriteAllText(statOutputPath, "");

        //main file first
        foreach (EventCombineObjs obj in eventObjs)
        {
            string toWrite = "";
            //header data first
            toWrite += "DataEnum: " + obj.DataEnum; //the referenced data found in MasterData
            toWrite += "\nTableNum: " + obj.TableNum; //the number of modified table entries
            toWrite += "\nMessage ID: " + obj.MessageID; //?
            //events
            int counter = 1;
            foreach (EvtTblEntries entries in obj.EventTable) //get into the EventTable
            {
                    toWrite += "\n\nEvent " + counter++ + ", ID " + entries.EventIDInfo.EventID;
                    toWrite += "\nPlaceID: " + entries.EventIDInfo.PlaceID;
                    toWrite += "\nLocation: " + entries.EventIDInfo.PosX + ", " + entries.EventIDInfo.PosY
                               + " (naively indexed at " + (35 * entries.EventIDInfo.PosY + entries.EventIDInfo.PosX) +
                               ")";
                    if ((entries.EventIDInfo.TimeStart != 0) || entries.EventIDInfo.TimeEnd != 24)
                        toWrite += "\nEvent happens from hr " + entries.EventIDInfo.TimeStart + " to hr " +
                                   entries.EventIDInfo.TimeEnd;
                    //Direction is a bit of a mess.
                    //If they're all 1s, then direction doesn't matter, the event fires regardless.
                    //There are 1409 cases where that's the case.
                    //There are 2 cases where three of the four are true. I presume this is a three-way intersection.
                    //There are 12 cases where two of the four are true. This could be hallways or turns.
                    //There are 333 cases where only one of the four is true. Shortcuts are unlocked via events; maybe that's what these are?
                    //There are zero cases where all four of them are false.
                    //There are 1756 results in total.
                    //Anyway, we'll combine them into an array to compare them to the biggest case.
                    bool[] directions =
                    {
                        entries.EventIDInfo.DirNorth, entries.EventIDInfo.DirEast, entries.EventIDInfo.DirSouth,
                        entries.EventIDInfo.DirWest
                    };
                    if (!directions.All(d =>
                            d)) //if not all of them are true, we should include directional information.
                    {
                        toWrite += "\nDirection required: ";
                        bool first = true;
                        for (int i = 0;
                             i < directions.Length;
                             i++) //for-each doesn't let me access the index of directions
                        {
                            if (directions[i])
                            {
                                if (!first)
                                    toWrite += ", ";
                                first = false;
                                //StringBuilder is *probably* better for this? But this is not meant to run more than once.
                                switch (i)
                                {
                                    case 0:
                                    {
                                        toWrite += "North";
                                        break;
                                    }
                                    case 1:
                                    {
                                        toWrite += "East";
                                        break;
                                    }
                                    case 2:
                                    {
                                        toWrite += "South";
                                        break;
                                    }
                                    case 3:
                                    {
                                        toWrite += "West";
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    //Action is weird.
                    //As expected, there are 1756 results total.
                    //1315 results where Action = 0, 
                    // 370 results where Action = 1,
                    //  10 results where Action = 2,
                    //  10 results where Action = 3,
                    //  51 results where Action = 12.
                    // WTF does this mean. Is it just coincidence that 1+2=3 numerically and 12 as a string?
                    //It's not bits: 10000000 + 01000000 = 11000000, not 00110000.
                    //Regardless, we'll track if Action != 0.
                    if (entries.EventIDInfo.Action != 0)
                    {
                        toWrite += "\nAction: " + entries.EventIDInfo.Action;
                    }

                    //For each of the flags, we'll only track ones that aren't -1.
                    if (entries.EventIDInfo.StartFg != -1)
                        toWrite += "\nStart flag: " + entries.EventIDInfo.StartFg;
                    if (entries.EventIDInfo.EndFg != -1)
                        toWrite += "\nEnd flag: " + entries.EventIDInfo.EndFg;
                    if (entries.EventIDInfo.StartTmpFg != -1)
                        toWrite += "\nStart temp flag: " + entries.EventIDInfo.StartTmpFg;
                    if (entries.EventIDInfo.EndTmpFg != -1)
                        toWrite += "\nEnd temp flag: " + entries.EventIDInfo.EndTmpFg;
                    //ConditionType likely correlates to ConditionVal, right?
                    //ConditionType = 0 results: 1619
                    //1756 - 1619 = 137 results.
                    //But there are 155 entries in ConditionVals where the array has at least one value that isn't -1.
                    //So we track them separately.
                    if (entries.EventIDInfo.ConditionType != 0)
                        toWrite += "\nCondition type: " + entries.EventIDInfo.ConditionType;
                    int[] toCompare = { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 };
                    if (!toCompare.SequenceEqual(entries.EventIDInfo.ConditionVal))
                        toWrite += "\nCondition type array: " + string.Join(", ",entries.EventIDInfo.ConditionVal);
                    //There are a total of six events where IsInstant is true. wat.
                    if (entries.EventIDInfo.IsInstant)
                        toWrite += "\nIsInstant is true";

                //finally, the nebulous 'array'. I'll print the length of it as well, because I'm curious about something...
                //That 'something' is coming together, so now I need them in hex...
                //toWrite += "\n\nEventObj Array: \n" + string.Join(" ", entries.Array);
                toWrite += "\n\nEventObj Array: \n" + string.Join(" ", entries.Array.Select(x => x.ToString("X2")));
                toWrite += "\nArray length: " + entries.Array.Length + "\n";
            }

            toWrite += "\n~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~\n";
            File.AppendAllText(mainOutputPath, toWrite);
        }

        //statistics file
        string stWrite = "";
        stWrite += "Unique Event IDs: " + string.Join(", ", EventIDs);
        stWrite += "\nUnique Place IDs: " + string.Join(", ",PlaceIDs);
        stWrite += "\nUnique Table IDs: " + string.Join(", ",TablesIDs);
        stWrite += "\nUnique Flags: " + string.Join(", ", UniqueFlags);
        stWrite += "\nUnique Condition Types: " + string.Join(", ",ConditionTypes);
        stWrite += "\n\n\n";
        //It's bad practice to not verify that these are not all the same length, but I can see the file: they are.
        //Every entry has exactly one of these, and unlike above we tracked all of them.
        stWrite += "Flags of every entry (start, end | start temp, end temp):";
        for (int i = 0; i < StartFlags.Count; i++)
        {
            stWrite += "\nEntry " + (i + 1) + "\t" +
                       (StartFlags[i].ToString()) + ", " + EndFlags[i].ToString() +
                       " | " +
                       StartTempFlags[i].ToString() + ", " + EndTempFlags[i].ToString();
        }

        File.AppendAllText(statOutputPath, stWrite);
    }
}

//main object for EventCombineObjs objects.
public class EventCombineObjs
{
    public int DataEnum { get; set; }
    public int TableNum { get; set; }
    public int MessageID { get; set; }
    public List<EvtTblEntries> EventTable { get; set; }

    public EventCombineObjs(EventCombineObjsDeserialized incoming)
    {
        this.DataEnum = incoming.DataEnum;
        this.TableNum = incoming.TableNum;
        this.MessageID = incoming.MessageID;
        //special due to nested JSON deserialization in a list.
        EventTable = incoming.EvtTbl
            .Select(x => new EvtTblEntries(x))
            .ToList();
    }
}

//deserialization object for the main EventCombineObjs objects.
public class EventCombineObjsDeserialized
{
    public int DataEnum { get; set; }
    public int TableNum { get; set; }
    public int MessageID { get; set; }
    public List<EvtTblEntriesDeserialized> EvtTbl { get; set; }
    
}

//Object for the EventCombineObjs' EvtTbl entries.
public class EvtTblEntries
{
    public EvtIdxInfo EventIDInfo { get; set; }
    public int[] Array { get; set; }
    public EvtTblEntries(EvtTblEntriesDeserialized incoming)
    {
        this.EventIDInfo =  new EvtIdxInfo(incoming.EvtIdxInfo);
        this.Array = incoming.Array;
    }
}

//deserialization sub-object for the EventCombineObjs' EvtTbl entries.
public class EvtTblEntriesDeserialized
{
    public EvtIdxInfoDeserialized EvtIdxInfo { get; set; }
    public int[] Array { get; set; }
}

//something I missed is that EvtTblEntries, as you can now see above, is actually only two datatypes, with
//the majority of information actually being in this EvtIdxInfo object. This was a fun refactor. :|
public class EvtIdxInfo
{
    public int EventID { get; set; }
    public int PlaceID { get; set; } //In the decomp, these are EventCheck.CheckPlace enums in Master.cs. Dungeons are 29-58 inclusive.
    public int PosX { get; set; }
    public int PosY { get; set; }
    public int TimeStart { get; set; }
    public int TimeEnd { get; set; }
    public bool DirEast { get; set; }
    public bool DirWest { get; set; }
    public bool DirSouth { get; set; }
    public bool DirNorth { get; set; }
    public int Action { get; set; }
    public int StartFg { get; set; }
    public int EndFg { get; set; }
    public int StartTmpFg { get; set; }
    public int EndTmpFg { get; set; }
    public int ConditionType { get; set; } //In the decomp, these are EventCheck.E_COND enums.
    public int[] ConditionVal { get; set; }
    //"ConditionVal" is a bit special.
    //For the vast majority of entries, it's twelve -1s.
    //However, 155 entries have a value in at least one of those entries that isn't -1. They all are, however, still length 12.
    public bool IsInstant { get; set; }
    
    public EvtIdxInfo(EvtIdxInfoDeserialized incoming) {
    this.EventID = incoming.EventID;
    this.PlaceID = incoming.PlaceID;
    this.PosX = incoming.PosX;
    this.PosY = incoming.PosY;
    this.TimeStart = incoming.TimeStart;
    this.TimeEnd = incoming.TimeEnd;
    this.DirEast = incoming.DirEast;
    this.DirWest = incoming.DirWest;
    this.DirSouth = incoming.DirSouth;
    this.DirNorth = incoming.DirNorth;
    this.Action = incoming.Action;
    this.StartFg = incoming.StartFg;
    this.EndFg = incoming.EndFg;
    this.StartTmpFg = incoming.StartTmpFg;
    this.EndTmpFg = incoming.EndTmpFg;
    this.ConditionType = incoming.ConditionType;
    this.IsInstant = incoming.IsInstant;
    this.ConditionVal = incoming.ConditionVal;
    }
}
    public class EvtIdxInfoDeserialized
    {
        public int EventID { get; set; }
        public int PlaceID { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int TimeStart { get; set; }
        public int TimeEnd { get; set; }
        public bool DirEast { get; set; }
        public bool DirWest { get; set; }
        public bool DirSouth { get; set; }
        public bool DirNorth { get; set; }
        public int Action { get; set; }
        public int StartFg { get; set; }
        public int EndFg { get; set; }
        public int StartTmpFg { get; set; }
        public int EndTmpFg { get; set; }
        public int ConditionType { get; set; }
        public int[] ConditionVal { get; set; }
        public bool IsInstant { get; set; }
    }
    
    //Now I'm curious about something RE: Array. 

    public class BinProcessing
    {
        private string binFolder = "";

        //dialog to get the location of the event\DUN\xxF.cbin files from a ripped MasterData.
        private Microsoft.Win32.OpenFolderDialog dlg = new OpenFolderDialog();

        public BinProcessing()
        {
            Nullable<bool> result = dlg.ShowDialog();
            if (result.Value)
            {
                binFolder = dlg.FolderName;
            }
            else
            {
                MessageBox.Show("Something went wrong with the filepath." +
                                "\nBinProcessing not done.");
                System.Environment.Exit(0);
            }
            //Now we have a folder path, but what do we do with it?
            //Well, this will be done after all the processing above has been completed.
            //This means we can directly compare arrays.
        }
    }
			
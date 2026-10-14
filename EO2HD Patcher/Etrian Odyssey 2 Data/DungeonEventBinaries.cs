namespace EO2HD_Patcher.Etrian_Odyssey_2_Data;


/*
 So, from what I can tell, EventObjCombine only changes or correlates to *some* events - most likely, the events that
 require UI components, with the Array element reflecting what the relevant .cbin file would mostly expect.
 
 I determined this from some observations:
    All Arrays start with '00 38' in hex and end in '00 39'. This also occurs in the relevant .cbin files with
        a '37' in-between, so those must be the starting and ending bytes.
    The DataEnum at the start of each section is a reference to an enumerator in the decomp's MasterData,
    which then refers to a file in the game. For example, DataEnum 331 refers to Event\DUN\10F.cbin.
    10F.cbin has 419 entries of '39 00 37 00 38', which implies 420 event entries total.
        However, if we look to the section that references DataEnum 331, it only modifies 21 Events.
    
    Note that this assumes that, in .cbin files, event IDs are numerically descending starting from ID 0,
     otherwise, I have no clue how they're named.
        
    (For the following section, this references event IDs in 10F.cbin/DataEnum 331).
Some of these changes are extremely mild. Like, 'only removing the assembler ASCII labels' kind of change. (e.g. event ID 0)
Some of them don't change the array at all, implying the only change is the metadata not found in the .cbin (e.g. event ID 1)
Some of them reduce the size a bit? Event ID 6, for example:
    .cbin: 00 38 00 2F 00 00 00 24 00 09 00 06 00 0D 00 34 00 1E 00 30 00 34 00 28 00 39 
    .json: 00 38 00 2F 00 05 00 24 00 09 00 06 00 0D 00 85 00 34 00 12 00 30 00 39
Some of them, however, are quite drastic. 10F doesn't have any examples, but I've seen events be truncated down from 100+ bytes to ~20.
(However, human perception is imperfect, so one of the goals of this is to see for certain if that's the cae.)
Some of them also...modify the same event twice? Events 20 and 21 in the .json both modify Event ID 19.
Their arrays are identical, and the only change between the .cbin and .json are removal of ASCII labels.
    My guess is that the way the Unity wrapper works can only change one flag of a type at a time?
    The *only* difference is the StartFlag - 648 and 649.
    What's interesting is the event - the guard selling Warp Wires/Ariadne Threads.
    

    
 */
public class DungeonEventBinaries
{
    
}
using System.Configuration;
using System.IO;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace EO2HD_Patcher.Etrian_Odyssey_2_Data.Decomp_Data;

/*
   Legally, I cannot include the actual code I found from decompiling the assembly.
   However, that doesn't mean I can't make functions to help disobfuscate it a bit.
   That's what this particular class is for.
 */
public class globalDeBB
{
    //Dictionary for ClearSharedRVAs.
    //We use a complex key for a Dictionary that uses the signature and the RVA.
    //See below for details.
    private SortedDictionary<(string,string), string> FuncsToFullName = new SortedDictionary<(string,string), string>();
    
    //To double-check that RVAs are not repeated across otherwise unique signatures, we'll also do this:
    private string _dupeRVAs = "";
    private HashSet<string> _dupeRVAHashSet = new HashSet<string>();
    
    //filepaths for I/O
    string path = "";
    string outPath = "";
    string patcherOutPath = "";
    
    /*ClearSharedRVAs takes a .txt input from wherever that contains a list of functions from
     the IL2CPPInspector's C# function signatures. 
     It goes through the list line by line and gets their signature type (e.g. 'public void')
     and the comment at the end that lists the addresses the function uses.
     It then attempts to add this to the Dictionary above. 
     A successful TryAdd means this is the first time this duo has been encountered, so we continue.
     If it fails, we need to do some additional processing, which is done in its own function.
     */
    public void ClearSharedRVAs()
    {
        //get the input file
        Microsoft.Win32.OpenFileDialog dlg = new OpenFileDialog();

        dlg.FileName = "input.txt";
        dlg.DefaultExt = ".txt";
        dlg.Filter = ".txt files (.txt)|*.txt";

        Nullable<bool> result = dlg.ShowDialog();
        if (result.Value)
        {
            path = dlg.FileName;
            outPath = Path.Combine(Path.GetDirectoryName(path), "BBO-output.txt");
            patcherOutPath = Path.Combine(Path.GetDirectoryName(path), "BBO-patch-info.txt");
        }
        else
            System.Environment.Exit(0);
        
        StreamReader stream = new StreamReader(path);
        while (!stream.EndOfStream)
        {
            //get the next line
            string line = stream.ReadLine()!; //suppress the warning; it should never be null here.
            //trim any edge whitespace
            line = line.Trim();
            //special: if the line starts with [CompilerGenerated], skip it.
            if (line.Contains("[CompilerGenerated]"))
                line = stream.ReadLine()!.Trim();
            //get the signature and RVAs
            string sig = GetSignature(line);
            string rva = GetRVAs(line);
            //try to add them to the Dictionary, and if it fails, run ClearDupes.
            if (!FuncsToFullName.TryAdd((sig, rva), line))
                ClearDupes(line, sig, rva);
            //if it DOES succeed, also see if the RVA is in the dupeRVAHashSet, and if it isn't, add it.
            //If it is, add it the string that shows the duplicates instead.
            //we're only really curious about the start address, so we'll grab that and cast it as an int.
            //We also remove the comment characters and the space.
            else
            {
                string firstRVA = rva.Substring(3, 18); //these memory addresses are 16 bytes long + the hex marker
                //Console.WriteLine("first RVA listed: " + firstRVA);
                if (!_dupeRVAHashSet.Add(firstRVA))
                {
                    Console.WriteLine("\tDupe RVA listed!!");
                    _dupeRVAs = firstRVA + " ";
                }
            }
        }
        stream.Close();
        //We have the dictionary, so we can finally output to file.
        OutputToFile();
        OutputForPatcher();
    }
    
    /* GetSignature is how we get the nameless signature of a particular function.
     Naively, we could just get the return type from a substring, but some of these have input variables
     that we want to compare as well.
     Now, fortunately, BeeBytes keeps the variable names the same if they are the same variable.
     So, an 'int AAAAAAAAA' could always be deobfuscated as something like 'int charLvl'.
     Because of that, we keep the variable names as well for the signatures.
     BeeBytes also always seems to declare the access specifier, or maybe that's IL2CPPInspector's doing.
     In any case, it makes my job simpler.
     The initial input should always be in the form "access modifer returnType name(inputType 1, inputType 2...); // RVA-comment"
     
     This returns in the format of, for example, 'public static int(int AAAAAAA)'.
     */
    private string GetSignature(string input)
    {
        //So, inputs at this stage resemble this:
        //public (modifier) int __BB_OBFUSCATOR_##(int XXXXXXX, int XXXXXXX); // 0x0000000000000000-0x0000000000000000
        
        //split the input to remove the arguements, and grab the first side.
        string sigSide = input.Split('(')[0];
        //get the index of the last space       
        int last = sigSide.LastIndexOf(' ');
        //use that as the length to get the signature portions.
        string toReturn = input.Substring(0, last);
        
        //Now, the inputs. We can't just get the end of the string starting at '(' because that gets the comments.
        //We also can't just get the third space because arguements have spaces.
        //However, we can use the LAST space. 
        int lastSpace = input.LastIndexOf(' ');
        //then we use that as the starting length, then subtract a few values.
        //Subtract the length from ('s position.
        //We also want to get rid of "; //", so subtract another 4.
        int length = lastSpace - input.LastIndexOf('(') - 4;
        toReturn += input.Substring(input.LastIndexOf('('), length);
        return toReturn;
    }
    //GetRVAs is a helper function to get the RVA comments at the end, before our BBO 'header'.
    //It needs the full string, not just a label or a part of it.
    private string GetRVAs(string input)
    {
        string toReturn = input.Substring(input.IndexOf("//", StringComparison.Ordinal));
        //The 'naive' check: if there only exists one set of comments, then we're fine. 
        if (input.IndexOf("//", StringComparison.Ordinal) == input.LastIndexOf("//", StringComparison.Ordinal))
            return toReturn;
        //Less naively, we know the full range of RVAs are 37 characters long.
        //Plus the three characters of the space and comments.
        //So, we trim toReturn down to that length and return that.
        return toReturn.Substring(0, 40);

    }
    
    //Much like GetRVAs, GetListBBOs is a helper function for ClearDupes to get the existing BBOs, if any.
    //This needs to be used on the full line, not just the label.
    private string GetListBBOs(string input)
    {
        string toReturn = input.Substring(input.LastIndexOf("//", StringComparison.Ordinal));
        //We need to check if this is actually BBOs, however, or if it's the RVAs.
        //If it isn't, return nothing.
        if (!toReturn.StartsWith("//BBO"))
        {
            return "";
        }
        //Now, we strip the text of the //BBO portion and return just the list of BBOs.
        //Fortunately, I know that "//BBO " is 6 characters, so the substring is easy.
        return toReturn.Substring(6);
    }
    
    //GetLabel is a function to get the given name of the function.
    //A full third of the time, this will be some form of "__BB_OBFUSCATOR_##".
    private string GetLabel(string input)
    {
        //Just like GetSignature, we get the left side of the input by splitting with (
        string toReturn = input.Split('(')[0];
        //Also like GetSignature, we also want the last space.
        int last = toReturn.LastIndexOf(' ');
        //Unlike GetSignature, however, the entire return is just from there. +1 to avoid the space.
        //Console.WriteLine(input + "-> " +  toReturn.Substring(last + 1));
        return toReturn.Substring(last + 1);

    }
    
    //GetBBOVal is a helper function in the case of a label being a BBO.
    //It returns just the number.
    //It requires the LABEL of the line, not the whole line.
    private string GetBBOValue(string input)
    {
        //if it's not a BBO function, we inform via the console and return the label untouched.
        if (!input.StartsWith("__BB"))
        {
            Console.WriteLine("GetBBOValue was called on a non-BBO string " + input);
            return input;
        }

        //__BB_OBFUSCATOR_##
        //If it is, we grab the end number by getting the index of the last underscore in the label.
        return input.Substring((input.LastIndexOf('_') + 1));
    }
    
    /*ClearDupes runs when there's a conflict in the TryAdd function of the Dictionary.
     It is essentially our deduplication logic, using Dictionary's requirement of a unique entry to
     detect potential duplicates.
     In short: it replaces any current __BB_OBFUSCATOR with a real string if it's been given,
     or replaces it with a lower __BB_OBFUSCATOR if that's what's incoming instead.
     It then appends the list of identical BBOs at the end.
     */
    private void ClearDupes(string line, string sig, string rva)
    {
        //    First, it checks if the current entry is a __BB_OBFUSCATOR_##.
        string currLabel = GetLabel(FuncsToFullName[(sig, rva)]);
        string currBBO = "-1";

        if (currLabel.Contains("BB_OBFUSCATOR"))
            currBBO = GetBBOValue(currLabel);

        //Now, we do the same for the incoming label.
        string incLabel = GetLabel(line);
        string incBBO = "-1";
        if (incLabel.Contains("BB_OBFUSCATOR"))
            incBBO = GetBBOValue(incLabel);
        //finally, let's see if our current string has any listed BBOs already.
        string currListedBBOs = GetListBBOs(FuncsToFullName[(sig, rva)]);
        
        //first, special case: both strings are real strings. We know this because both BBOs are still -1.
        //This is an exception, but it's an important one. I have yet to actually see this happen when I was doing this manually.
        //It means these two 'real' functions are pointing to the same address in memory, i.e. doing the same thing.
        //There are a couple implications here, but it depends on the individual function.
        if (currBBO == "-1" && incBBO == "-1")
        {
            //We'll simply turn incBBO into the label and let this function fall through to the
            //next part.
            incBBO = incLabel;
        }
        
        
        //first real case: the current string is a real string. We know this because currBBO is still -1.
        if (currBBO == "-1")
        {
            //Check to see if we have any BBOs already.
            if (currListedBBOs == "") //it doesn't, so add the string
                FuncsToFullName[(sig, rva)] += " //BBO ";
            else //it does, so just add a comma and a space
                FuncsToFullName[(sig, rva)] += ", ";
            
            //add the incoming BBO and return.
            FuncsToFullName[(sig, rva)] += incBBO;
            return;

        }
        //second real case: the current string is a BBO, and the incoming is a real string.
        //We know this because the above cases failed, and incBBO is still -1.
        if (incBBO == "-1")
        {
            //If we have any currently listed BBOs, append them to the incoming list,
            //then add the comma.
            if (currListedBBOs != "")
            {
                line += " "
                        + currListedBBOs
                        + ", ";
            }
            else //no currently listed BBOs, so add the text and a space. 
                line += " //BBO ";
            //regardless, we now add the current BBO to the line.
            line += currBBO;
            
            //finally, replace the current value with the incoming one, and return.
            FuncsToFullName[(sig, rva)] = line;
            return;
        }
        
        //Last case: we fell through to the bottom, so neither are a real string.
        //So we compare the two BBO values.
        //special case: For whatever reason, when sorting the BBOs by memory address,
        //a single-digit value is treated as if it's greater than those with said value in the tens place.
        //e.g. 2 > 21-29.
        //So we'll compare them as hex values instead and append an A to a shorter one.
        Int32 compInc = incBBO.Length == 1 ?
            Convert.ToInt32(incBBO + 'A', 16) : Convert.ToInt32(incBBO, 16);
        Int32 compCurr = currBBO.Length == 1 ? 
            Convert.ToInt32(currBBO + 'A', 16) : Convert.ToInt32(currBBO, 16);
        
        //If for whatever reason they're the same BBO value, something has gone very weird, but I'll 
        //just use the current string in the dictionary.
        if (compCurr <= compInc)
        {
            //we don't need to swap, so just add the BBO text and return.
            if (currListedBBOs != "")
                FuncsToFullName[(sig, rva)] += ", ";
            else
                FuncsToFullName[(sig, rva)] += " //BBO ";
            FuncsToFullName[(sig, rva)] += incBBO;
            return;
        }
        //if-statement is false, so we need to swap the two.

        string toReplace = line;
        //first, let's check to make sure that toReplace doesn't already have BBOs listed.
        string incomingBBOs = GetListBBOs(toReplace);
        
        //now, two major logic checks, depending on the state of both lists' BBOs.
        //If toReplace already has a BBO set, we add the comma.
        if (incomingBBOs != "")
            toReplace += ", ";
        //if it DOESN'T, we add the header.
        else
            toReplace += " //BBO ";
        //Secondly, if currListedBBOs has a value, we append it.
        //...And a comma as well.
        if (currListedBBOs != "")
            toReplace += currListedBBOs + ", ";
        
        //Finally, add the old string's BBO value, replace the two, and return.
        toReplace += currBBO;
        FuncsToFullName[(sig, rva)] = toReplace;
        return;
    }

    private void OutputToFile()
    {
        //empty the files first
        File.WriteAllText(outPath, "");

        foreach (string line in FuncsToFullName.Values)
            File.AppendAllText(outPath, line + "\n");
        
        //finally, if _dupeRVAs isn't empty, there are functions with different functions that, regardless, use the same address.
        //print them.
        if (_dupeRVAs.Length != 0)
            File.AppendAllText(outPath, "WARNING: Duplicate memory starts at: " + _dupeRVAs);
        /*
        //debug
        int counter = 1;
        foreach ((string a, string b) in FuncsToFullName.Keys)
            Console.WriteLine("Line " + counter++ + ": " + a + ", " + b); */

    }
    
    //With most of this automated, I can now somewhat automate the creation of listeners for the Patcher
    //to determine whether certain events are called or not.
    //The easiest way to do this is in that relevant code.
    //However, it does need a list of memory addresses and some human-legible labels.

    private void OutputForPatcher()
    {
        //empty the files first
        File.WriteAllText(patcherOutPath, "");
        
        //after all is said and done, an output (i.e. FuncsToFullName.Values) looks like this:
        //public static void __BB_OBFUSCATOR_22(GameData.CHAR_DATA BOIGNKAGOFI, int PJNDNEMOODI); // 0x0000000181281A60-0x0000000181281A70 //BBO 9
        //What we'll want is the signature (minus access and other member modifiers), and the first address.
        //These are primarily stored in the dictionary's keys, so we have a relatively easy way to get them.
        foreach (KeyValuePair<(string, string), string> entry in FuncsToFullName)
        {
            //string sig = entry.Key.Item1; Actually useless for what I want, unfortunately.
            string rva = entry.Key.Item2;
            
            //start with the address. Only the starting address, not the ending one.
            string toPrint = rva.Split('-')[0]; //we don't need a space for reasons shown below.
            //...TIL My RVA values start with the comments, so we'll also remove those.
            //Fortunately, I know that "// " is three characters.
            toPrint = toPrint.Substring(3);
            
            //the signature needs a bit of modification to get just the return type and the label.
            //We can't use either GetSignature nor GetLabel, because those trim different parts.
            //So, we do it manually from the entry.
            //split the string based on where ')' is, then re-add it.
            string sig = entry.Value.Split(')')[0] + ")";
            //we need to get the location of the return type, so we'll need to do some more splits.
            string sigNoParams = sig.Split('(')[0]; //remove parameters
            //then, we get the last space to get the location of the label
            int labelSpace = sigNoParams.LastIndexOf(' ');
            string sigNoLabels = sigNoParams.Substring(0, labelSpace);
            //with this, we can now do the same thing again to just get the location of the return type.
            int returnSpace = sigNoLabels.LastIndexOf(' ');
            //then we can append this (including the space!) to the printed string.
            toPrint += sig.Substring(returnSpace);
            //write it
            File.AppendAllText(patcherOutPath, toPrint + "\n");
        }
    }
    
}



		




using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.GUI.TextPanel; //Adds support for the new LCD panels
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    //The Holy Grail. My old hard-coded ship manager series, completely configurable from a grid's
    //CustomData. It's going to take some work.
    //Components:
    //  Readable Tallies:
    //    Volume, Item, Oxygen, Hydrogen, Power, Jump Charge, Raycast, Max Output (Solar/Wind),
    //    HydrogenWithEngines?, ShieldIntegrity? ShieldHeat? ShieldTimeTilRestore?
    //  Other functions:
    //    Roost, Perch, Raycast, Multifunction Display?, Ascent?, Door Minder?, Damage Report?
    //  Support commands:
    //    IGC, Evaluate, Populate, Clear, Template <Type>, AutoPopulate, Update? ChangeTag? Nuke?

    partial class Program : MyGridProgram
    {
        //The version number of this code
        const double VERSION = .7;
        //The default ID of the script, to be used if no custom ID is set.
        const string DEFAULT_ID = "ShipWare";
        //The ID for this instance of the script. 
        string customID;
        //The prefix used to identify all components of the script, regardless of changes the user
        //makes to the ID
        const string PREFIX = "SW";
        //A combination of PREFIX and ID, constructed during initialization. Used as a section tag
        //for all non-discreet sections in configuration, and as a channel for recognizing IGC 
        //communication.
        string tag;
        //Arrays that store the containers and tallies that this script watches. 
        Container[] containers;
        Tally[] tallies;
        //The reports that tell about what various script elements are doing.
        IReportable[] reports;
        //MFDs that do the same thing as reports, only fancier. MFDs are stored in a dictionary, to
        //facilitate controlling them by name.
        Dictionary<string, MFD> MFDs;
        //Like MFDs, ActionSets need to be stored in a Dictionary, so we can find them at a moment's
        //notice.
        Dictionary<string, ActionSet> sets;
        //Triggers don't even have names. An array will suffice.
        Trigger[] triggers;
        //A raycaster object is used to perform Raycasts and compile reports about them. We address 
        //them by name.
        Dictionary<string, Raycaster> raycasters;
        //The indicators that change color based on what a tally is doing
        Indicator[] indicators;
        //An EventLog that will... log events.
        Hammers.EventLog log;
        //An object that generates ASCII meters for us, used by tallies.
        Hammers.MeterMaid meterMaid;
        //Listens for Inter-Grid Communication
        IMyBroadcastListener listener;
        //Used to read information out of a block's CustomData
        MyIni iniReadWrite;
        //A second instance of MyIni, handy for moving data between two different configurations.
        MyIni iniRead;
        //A custom object patterned off of MyIni, used for manipulating sections while preserving
        //their formatting.
        RawTextIni iniRaw;
        //Used to parse arguments entered as commands
        MyCommandLine argReader;
        //A StringBuilder that we will pass out to the various objects that need one.
        StringBuilder _sb;
        //When active, this script updates all its internal and external elements every 100 tics, 
        //roughly once every second and a half. If the grid the script is running on isn't doing 
        //anything important, the script can be set to skip update tics to reduce processor loads.
        //The logic and variables are handled by this UpdateDistributor object.
        UpdateDistributor distributor;

        public Program()
        {
            bool firstRun = initiate();
            if (!firstRun)
            { evaluate(); }
            //The main method Echos the event log every time it finishes running. But there's a lot
            //of stuff that can go wrong when parsing configuration, so we need an Echo here as well.
            Echo(log.toString());
        }

        public void Save()
        {
            //Clear out any data that may be lurking in the iniReader (It should be clear already, 
            //but for this, we want to be certain.)
            iniReadWrite.Clear();
            //Store the version number of this code in the Config section.
            iniReadWrite.Set("Config", "Version", VERSION);
            //Store the ID of this instance of the script as well.
            iniReadWrite.Set("Config", "ID", customID);
            if (sets != null)
            {
                //For every ActionSet named in our sets dictionary...
                foreach (string setName in sets.Keys)
                //Add an entry to the ActionSets section, with the name of the set as the key, storing
                //the current status of the set.
                { iniReadWrite.Set("ActionSets", setName, sets[setName].state); }
            }
            if (MFDs != null)
            {
                //For every MFD named in our MFD dictionary...
                foreach (string MFDName in MFDs.Keys)
                //Add an entry to the MFDs section, with the name of the MFD as the key, storing
                //the current page shown by the MFD.
                { iniReadWrite.Set("MFDs", MFDName, MFDs[MFDName].pageName); }
            }
            //Commit the contents of the iniReader to the Storage string
            Storage = iniReadWrite.ToString();
            //Clear the contents of the MyIni object. Just because this script is probably about to
            //die is no reason not to be tidy. And it may save us some grief if it turns out the 
            //script isn't about to die.
            iniReadWrite.Clear();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            //Now incorporating the mildly heretical art of bitwise comparitors.
            //Is this the update tic?
            if ((updateSource & UpdateType.Update100) != 0)
            {
                //Notify the distributor that a tic has occurred. If it's time for an update...
                if (distributor.tic())
                {
                    compute();
                    update();
                    //And tell the log about it
                    log.tic();
                }
            }
            //Is this us trying to spread out some of our work load with a delayed evaluate?
            else if ((updateSource & UpdateType.Once) != 0)
            { evaluate(); }
            //Is this the IGC wanting attention?
            else if ((updateSource & UpdateType.IGC) != 0)
            {
                //As long as we have messages waiting for us...
                while (listener.HasPendingMessage)
                {
                    MyIGCMessage message = listener.AcceptMessage();
                    if (message.Tag == tag)
                    {
                        //Pull the data from the message
                        string data = message.Data.ToString();

                        //Try to parse the data with the ArgReader. If it works...
                        if (argReader.TryParse(data))
                        {
                            //The string that will store the result of this IGC interaction. Will be 
                            //sent to the local log and potentially as a reply on the channel specified
                            //by the sender.
                            string outcome = "No reply";
                            //Stores the channel the sender wants us to send a response on.
                            string replyChannel = "";
                            //If a switch is present...
                            if (argReader.Switches.Count > 0)
                            //Make our replyChannel equal to the first switch we come to.
                            //This is really not how switches are meant to be used, but maybe it'll work
                            {
                                MyCommandLine.SwitchEnumerator switches = argReader.Switches.GetEnumerator();
                                switches.MoveNext();
                                replyChannel = switches.Current;
                            }

                            //If the first word in the IGC message is 'reply'...
                            if (argReader.Argument(0).ToLowerInvariant() == "reply")
                            {
                                //Replace 'reply' in the data with an empty string
                                data = data.Replace(argReader.Argument(0), "");
                                //Trim the remaining string
                                data = data.Trim();
                                //Display the remainder of the data string as the reply message.
                                outcome = ($"Received IGC reply: {data}");
                            }
                            //If the first argument is 'action'
                            else if (argReader.Argument(0).ToLowerInvariant() == "action")
                            {
                                //Did we get the correct number of arguments?
                                if (argReader.ArgumentCount == 3)
                                {
                                    //Run tryTakeAction with the directed command and store the
                                    //result in outcome
                                    outcome = tryTakeAction(argReader.Argument(1),
                                        argReader.Argument(2).ToLowerInvariant(), "IGC-directed ");
                                }
                                //If we got an unexpected number of arguments
                                else
                                {
                                    outcome = $"Received IGC-directed command '{data}', which " +
                                        $"has an incorrect number of arguments.";
                                }

                            }
                            //If we got exactly one argument
                            else if (argReader.ArgumentCount == 1)
                            //Run tryTakeAction with the 'switch' command and store the result in 
                            //outcome.
                            { outcome = tryTakeAction(argReader.Argument(0), "switch", "IGC-directed "); }
                            //If we have no idea what's happening
                            else
                            {
                                outcome = $"Received the following unrecognized command from the IGC:" +
                                    $" '{data}'.";
                            }
                            //Add an entry to the local log
                            log.add(outcome);
                            //If we're supposed to send a reply message...
                            if (!String.IsNullOrEmpty(replyChannel))
                            { IGC.SendBroadcastMessage(replyChannel, $"reply [{tag}] {outcome}"); }
                        }
                        //If we couldn't parse the data...
                        else
                        {
                            log.add($"Received IGC-directed command '{data}', which couldn't be " +
                                $"handled by the argument reader.");
                        }
                    }
                }
            }
            //Is this a 'Run' command?
            else
            {
                //Did we get arguments?
                if (argReader.TryParse(argument))
                {
                    //The first argument of a run command will (hopefully) tell us what we need to 
                    //be doing.
                    string command = argReader.Argument(0).ToLowerInvariant();
                    switch (command)
                    {
                        //Simply echo the log. Handy when the script isn't executing.
                        case "log":
                            //The log is automatically echo'd whenever a command is received. So we
                            //literally do nothing here.
                            break;
                        //A slightly less dead-simple method for passing messages to the IGC
                        //Sends the entirety of the command, minus the word "IGC" and the data tag.
                        //Also hopefully allows the user to include all the spaces their little 
                        //heart desires.
                        //Argument format: IGC <tag> <data>
                        //Argument example: IGC RemoteStart GateBay1
                        case "igc":
                            string data = argument.Replace($"IGC", "");
                            data = data.Replace(argReader.Argument(1), "");
                            data = data.Trim();
                            IGC.SendBroadcastMessage(argReader.Argument(1), data);
                            log.add($"Sent the following IGC message on channel '{argReader.Argument(1)}'" +
                                $": {data}.");
                            break;
                        /*
                        //A marginally less dead-simple method for passing messages to the IGC
                        //Sends the entirety of the command, minus the word "IGC" and the data tag.
                        //Argument format: IGC <tag> <data>
                        //Argument example: IGC RemoteStart GateBay1
                        case "IGC":
                            string data = argument.Replace($"IGC {argReader.Argument(1)}", "");
                            IGC.SendBroadcastMessage(argReader.Argument(1), data);
                            log.add($"Sent the following IGC message on channel '{argReader.Argument(1)}'" +
                                $": {data}, as parsed from the command {argument}.");
                            break;
                        */
                        /*
                        //A dead-simple method for passing messages to the IGC
                        //Argument format: IGC <tag> <data>
                        //Argument example: IGC RemoteStart GateBay1
                        case "IGC":
                            IGC.SendBroadcastMessage(argReader.Argument(1), argReader.Argument(2));
                            log.add($"Sent the following IGC message on channel '{argReader.Argument(1)}'" +
                                $": {argReader.Argument(2)}.");
                            break;
                        */
                        //Controls an MFD
                        //Argument format: MFD <name> <command>
                        //Argument example: MFD MainScreen Next
                        case "mfd":
                            //If the user has given us the correct number of arguments...
                            if (argReader.ArgumentCount == 3)
                            {
                                string MFDTarget = argReader.Argument(1);
                                string MFDPageCommand = argReader.Argument(2);
                                if (MFDs == null)
                                { log.add($"Received MFD command, but script configuration isn't loaded."); }
                                //If we have MFDs, and we actually know what MFD the user is talking about...
                                else if (MFDs.ContainsKey(MFDTarget))
                                {
                                    //If it's one of the easy commands...
                                    //Note: Performing toLowerInvariant in the checks is intentional.
                                    //PageCommand could also include the name of a specific page,
                                    //and the dictionary that page is stored in is case-sensitive.
                                    if (MFDPageCommand.ToLowerInvariant() == "next")
                                    { MFDs[MFDTarget].flipPage(true); }
                                    else if (MFDPageCommand.ToLowerInvariant() == "prev")
                                    { MFDs[MFDTarget].flipPage(false); }
                                    //If it isn't one of the easy commands, assume the user is trying 
                                    //to set the MFD to a specific page.
                                    else
                                    {
                                        //If the MFD declines to set the page to the one named in 
                                        //the command...
                                        if (!MFDs[MFDTarget].trySetPage(MFDPageCommand))
                                        {
                                            //... Complain.
                                            log.add($"Received command to set MFD '{MFDTarget}' to unknown " +
                                                $"page '{MFDPageCommand}'.");
                                        }
                                    }
                                }
                                //If we don't know what MFD the user is talking about, complain.
                                else
                                { log.add($"Received '{MFDPageCommand}' command for un-recognized MFD '{MFDTarget}'."); }
                            }
                            //If the user did not give us the correct number of arguments, complain.
                            else
                            { log.add($"Received MFD command with an incorrect number of arguments."); }
                            break;
                        //Manipulates an ActionSet
                        //Argument format: Action <name> <command>
                        //Argument example: Action GateBay1 Switch
                        case "action":
                            //If the user has given us the correct number of arguments...
                            if (argReader.ArgumentCount == 3)
                            {
                                string outcome = tryTakeAction(argReader.Argument(1),
                                    argReader.Argument(2).ToLowerInvariant(), "");
                                //If something happened that we need to tell the user about...
                                if (!String.IsNullOrEmpty(outcome))
                                { log.add(outcome); }
                            }
                            //If the user did not give us the correct number of arguments, complain.
                            else
                            { log.add($"Received Action command with an incorrect number of arguments."); }
                            break;
                        //Triggers a scan from the designated Raycaster
                        //Argument format: Raycast <name>
                        //Argument example: Raycast BowCaster
                        case "raycast":
                            //If the user has given us the correct number of arguments...
                            if (argReader.ArgumentCount == 2)
                            {
                                //Store what should be the Raycaster's name
                                string raycasterName = argReader.Argument(1);
                                if (raycasters == null)
                                { log.add($"Received Racast command, but script configuration isn't loaded."); }
                                //Check to see if we have a raycaster by that name
                                else if (raycasters.ContainsKey(raycasterName))
                                //Order the named Raycaster to perform a scan.
                                { raycasters[raycasterName].scan(); }
                                //If we don't have a raycaster by that name, complain.
                                else
                                { log.add($"Received Raycast command for un-recognized Raycaster '{raycasterName}'."); }
                            }
                            //If the user did not give us the correct number of arguments, complain.
                            else
                            { log.add($"Received Raycast command with an incorrect number of arguments."); }
                            break;
                        //Re-creates the configuration found in the PB's Init section from the 
                        //information stored in memory. Pretty narrow use case, but with writeConfig,
                        //we had the technology.
                        //Argument format: Reconstititute
                        case "reconstitute":
                            //Before we do this, we need to make sure there's actually some data to write
                            if ((tallies == null || tallies.Length == 0) && (sets == null || sets.Count == 0))
                            {
                                log.add("Received Reconstitute command, but there are no objects in " +
                                    "memory to re-create config from. Please only use this command " +
                                    "after the script has successfully evaluated and is running.");
                            }
                            else
                            {
                                Me.CustomData = reconstitute(Me.CustomData, tallies.ToList(), sets.Values.ToList(), triggers.ToList());
                                log.add($"Carried out Reconstitute command. Object initializers on the " +
                                    $"programmable block have been re-created from data in memory.");
                            }
                            break;
                        //Simply replace the CustomData on blocks in the Populate group with the CustomData
                        //from the first block in the Template group
                        //Argument format: Clone
                        case "clone":
                            List<IMyTerminalBlock> cloneBlocks = new List<IMyTerminalBlock>();
                            GridTerminalSystem.GetBlockGroupWithName("Template")?.GetBlocks(cloneBlocks);
                            if (cloneBlocks.Count > 0)
                            {
                                IMyTerminalBlock cloneTemplate = cloneBlocks[0];
                                //Pretty sure I don't need to manually clear this, but I'm going to anyway.
                                cloneBlocks.Clear();
                                GridTerminalSystem.GetBlockGroupWithName("Populate")?.GetBlocks(cloneBlocks);
                                if (cloneBlocks.Count > 0)
                                {
                                    foreach (IMyTerminalBlock block in cloneBlocks)
                                    { block.CustomData = cloneTemplate.CustomData; }
                                    log.add($"Carried out Clone command, replacing the CustomData " +
                                        $"of {cloneBlocks.Count} blocks in the Populate group with " +
                                        $"the CustomData from block '{cloneTemplate.CustomName}'.");
                                }
                                //If there is no populate group on the grid, complain.
                                else
                                {
                                    log.add("Received Clone command, but there is no Populate " +
                                        "block group on the grid.");
                                }
                            }
                            //If there is no Template group on the grid, complain
                            else
                            {
                                log.add("Received Clone command, but there is no Template " +
                                        "block group on the grid.");
                            }
                            break;
                        //Copies the configuration from a 'Populate' section on the PB to members of
                        //the 'Populate' group on the grid
                        //Argument format: Populate (Flag)
                        //Argument example: Populate -add
                        case "populate":
                            MyIniParseResult parseResult;
                            //We should be clearing them after every use but, because we're going to
                            //be using them to generate config, we're going to make sure.
                            iniRead.Clear();
                            iniReadWrite.Clear();
                            //Try and parse the config on the PB. Most of it should be fine, but 
                            //this will catch any errors in the Populate section.
                            if (iniRead.TryParse(Me.CustomData, out parseResult))
                            {
                                //Is this Populate targeting the PB?
                                if (iniRead.Get("Populate", "CustomTag").ToString() == "Me")
                                {
                                    string popPBOutcome = "";
                                    //We'll need to figure out if there's already some config, and 
                                    //for that, we'll need a list of the keys in the init section.
                                    List<MyIniKey> keys = new List<MyIniKey>();
                                    iniRead.GetKeys($"{tag}Init", keys);
                                    //If we have some config already, but we don't have any data in memory...
                                    if (keys.Count != 0 && (tallies == null || tallies.Length == 0) &&
                                        (sets == null || sets.Count == 0))
                                    {
                                        popPBOutcome = "Received Populate command targetting the " +
                                            "programmable block, but the data needed to re-write " +
                                            "the config is missing. Please only use this command " +
                                            "after the script has successfully evaluated and is " +
                                            "running.";
                                    }
                                    else
                                    {
                                        string newConfig = populatePB(Me.CustomData, iniRead, out popPBOutcome);

                                        if (!String.IsNullOrEmpty(newConfig))
                                        {
                                            //If it worked, we just received a new config for the PB.
                                            Me.CustomData = newConfig;
                                            //Queue up an evaluate.
                                            Save();
                                            Runtime.UpdateFrequency = UpdateFrequency.Once;
                                        }
                                    }
                                    log.add(popPBOutcome);
                                    iniRead.Clear();
                                }
                                //Is there even a Populate section on the PB?
                                else if (iniRead.ContainsSection("Populate"))
                                {
                                    //Next question: Is there a Populate group on the grid?
                                    List<IMyTerminalBlock> popBlocks = new List<IMyTerminalBlock>();
                                    GridTerminalSystem.GetBlockGroupWithName("Populate")?.GetBlocks(popBlocks);
                                    if (popBlocks.Count > 0)
                                    {
                                        int keys = 0;
                                        string trouble = "";
                                        keys = populate(popBlocks, iniRead, iniRaw, _sb,
                                            out trouble, argReader.Switch("merge"));
                                        if (keys != -1)
                                        {

                                            log.add($"Carried out Populate command, writing {keys} " +
                                                $"keys to each of the {popBlocks.Count} blocks in " +
                                                $"the Populate group.");
                                        }
                                        //If the Populate method encountered trouble
                                        else
                                        {
                                            log.add($"Attempted to carry out Populate command, but " +
                                                $"encountered the following error: {trouble}");
                                        }

                                        /*
                                        int edits = 0;
                                        //We're cleared to populate. The last thing to do is see if
                                        //the user has set any flags. Like -add
                                        if (argReader.Switch("add"))
                                        { edits = populate(true, popBlocks, iniRead, iniReadWrite); }
                                        else if (argReader.Switch("ow"))
                                        { edits = populate(false, popBlocks, iniRead, iniReadWrite); }
                                        else
                                        //{ edits = populate(popMode.reg, blocks, iniRead, iniReadWrite); }
                                        { edits = populateSimple(popBlocks, iniRead, _sb); }
                                        //Queue up an evaluate to see if we can understand what the
                                        //user is telling us.
                                        Runtime.UpdateFrequency = UpdateFrequency.Once;
                                        //Log what we think happened.
                                        log.add($"Carried out Populate command, performing {edits} edits " +
                                            $"across {popBlocks.Count} blocks.");
                                        */
                                    }
                                    //If there is no populate group on the grid, complain.
                                    else
                                    {
                                        log.add("Received Populate command, but there is no Populate " +
                                            "block group on the grid.");
                                    }
                                }
                                else
                                {
                                    //If there was no populate section, add one
                                    Me.CustomData = "[Populate]\n\n" + Me.CustomData;
                                    //Let the user know that the way is prepared.
                                    log.add("Received Populate command, but there was no Populate " +
                                        "section on the Programmable Block. One has been added, and " +
                                        "configuration can be entered there.");
                                }
                            }
                            else
                            //If the Populate section (Or anything else on the PB) can't be read, 
                            //do the user a solid and let them know why.
                            {
                                log.add($"Received Populate command, but the parser was unable to read " +
                                    $"information from the Programmable Block. Reason: {parseResult.Error}");
                            }
                            break;
                        //Places the config from the specified section (Or the default script config
                        //section if no argument is offered) of the first block in the Populate group
                        //in an [Existing] section on the PB
                        //Argument format: LoadExisting <SectionName>
                        //Argument example: LoadExisting SW.HangarDoors
                        case "loadexisting":
                            string sectionName;
                            //If the user didn't specify a section
                            if (argReader.ArgumentCount == 1)
                            { sectionName = tag; }
                            //If the user did specify a section
                            else if (argReader.ArgumentCount == 2)
                            { sectionName = argReader.Argument(1); }
                            //If the user has done something bizarre
                            else
                            {
                                log.add($"Received LoadExisting command with an incorrect number of arguments.");
                                break;
                            }
                            List<IMyTerminalBlock> existingBlocks = new List<IMyTerminalBlock>();
                            GridTerminalSystem.GetBlockGroupWithName("Template")?.GetBlocks(existingBlocks);
                            if (existingBlocks.Count > 0)
                            {
                                IMyTerminalBlock targetBlock = existingBlocks[0];
                                MyIniParseResult existingParseResult = new MyIniParseResult();
                                if (!iniRaw.tryLoad(iniReadWrite, out existingParseResult, targetBlock.CustomData))
                                {
                                    log.add($"Received LoadExisting command, but config on block " +
                                        $"{targetBlock.CustomName} was unreadable for the following " +
                                        $"reason: {existingParseResult.Error}.");
                                    break;
                                }
                                else
                                {
                                    string readSection;
                                    if (!iniRaw.tryRetrieveSectionContents(sectionName, out readSection))
                                    {
                                        log.add($"Received LoadExisting command, but config on block " +
                                          $"{targetBlock.CustomName} did not contain the specified " +
                                          $"section {sectionName}.");
                                        break;
                                    }
                                    readSection = $";From block {targetBlock.CustomName}, section {sectionName}.\n" + readSection;
                                    //MONITOR: I'm just assuming that the PB has a readable config. That
                                    //might not be the case.
                                    if (!iniRaw.tryLoad(iniReadWrite, out existingParseResult, Me.CustomData))
                                    {
                                        log.add($"Received LoadExisting command, but config on the " +
                                            $"Programmable Block was unreadable for the following " +
                                            $"reason: {existingParseResult.Error}.");
                                        break;
                                    }
                                    //Clear any existing Existing sections from the PB.
                                    iniRaw.tryDeleteSection("Existing");
                                    iniRaw.addSection("Existing", readSection, 0);
                                    Me.CustomData = iniRaw.toString();
                                    iniRaw.clear();
                                    iniReadWrite.Clear();
                                    log.add($"Carried out LoadExisting command, adding config from the " +
                                        $"{sectionName} section of block '{targetBlock.CustomName}' to " +
                                        $"the Existing section of the Programmable Block.");
                                }
                                /*
                                iniReadWrite.TryParse(targetBlock.CustomData, sectionName);
                                //string readSection = readableMyIni(_sb, iniReadWrite, false, sectionName, "Existing");
                                readSection = $";From block {targetBlock.CustomName}, section {sectionName}.\n" + readSection;
                                iniReadWrite.TryParse(Me.CustomData);
                                //Make sure there's no Existing section on the PB's config
                                iniReadWrite.DeleteSection("Existing");
                                Me.CustomData = readSection + readableMyIni(_sb, iniReadWrite, true);
                                log.add($"Carried out LoadExisting command, adding config from the " +
                                    $"{sectionName} section of block '{targetBlock.CustomName}' to " +
                                    $"the Existing section of the Programmable Block.");
                                */
                            }
                            //If there is no populate group on the grid, complain.
                            else
                            {
                                log.add("Received LoadExisting command, but there is no Template " +
                                    "block group on the grid.");
                            }
                            break;
                        //Deletes the contents of CustomData for every block in the Populate group
                        //Argument format: TacticalNuke (Flag)
                        //Argument example: TacticalNuke -confirm
                        case "tacticalnuke":
                            if (argReader.Switch("confirm"))
                            {
                                List<IMyTerminalBlock> tacBlocks = new List<IMyTerminalBlock>();
                                GridTerminalSystem.GetBlockGroupWithName("Populate")?.GetBlocks(tacBlocks);
                                if (tacBlocks.Count > 0)
                                {
                                    foreach (IMyTerminalBlock block in tacBlocks)
                                    { block.CustomData = ""; }
                                    log.add($"Carried out TacticalNuke command, clearing the " +
                                        $"CustomData of {tacBlocks.Count} blocks.");
                                }
                                //If there is no populate group on the grid, complain.
                                else
                                {
                                    log.add("Received TacticalNuke command, but there is no Populate " +
                                        "block group on the grid.");
                                }
                            }
                            else
                            {
                                log.add("Received TacticalNuke command. TacticalNuke will remove " +
                                    "ALL CustomData from blocks in the Populate group. If you are " +
                                    "certain you want to do this, run the command with the " +
                                    "-confirm switch.");
                            }
                            break;
                        //Prints a list of properties of every block type in the Populate group to
                        //the log.
                        //Argument format: Properties
                        case "properties":
                            //Is there a Populate group on the grid?
                            List<IMyTerminalBlock> propBlocks = new List<IMyTerminalBlock>();
                            GridTerminalSystem.GetBlockGroupWithName("Populate")?.GetBlocks(propBlocks);
                            if (propBlocks.Count > 0)
                            {
                                //Holds properties of all the block types in the group
                                Dictionary<Type, string> popPropties = new Dictionary<Type, string>();
                                //Holds properties of a particular block
                                List<ITerminalProperty> properties = new List<ITerminalProperty>();
                                string propertyEntry;
                                foreach (IMyTerminalBlock block in propBlocks)
                                {
                                    //Get the property list for this block type if we don't have it
                                    //yet
                                    if (!popPropties.ContainsKey(block.GetType()))
                                    {
                                        block.GetProperties(properties);
                                        propertyEntry = "";
                                        foreach (ITerminalProperty property in properties)
                                        { propertyEntry += $"  {property.Id}  {property.TypeName}\n"; }
                                        popPropties.Add(block.GetType(), propertyEntry);
                                    }
                                }
                                _sb.Clear();
                                //Use the data we've collected to compile a list of properties for 
                                //the various block types in populate
                                foreach (KeyValuePair<Type, string> entry in popPropties)
                                { _sb.Append($"{entry.Key}\n{entry.Value}"); }
                                log.add(_sb.ToString());
                                _sb.Clear();
                            }
                            //If there is no populate group on the grid, complain.
                            else
                            {
                                log.add("Received Properties command, but there is no Populate " +
                                    "block group on the grid.");
                            }
                            break;
                        case "definitions":
                            //Is there a Populate group on the grid?
                            List<IMyTerminalBlock> defBlocks = new List<IMyTerminalBlock>();
                            GridTerminalSystem.GetBlockGroupWithName("Populate")?.GetBlocks(defBlocks);
                            if (defBlocks.Count > 0)
                            {
                                _sb.Clear();
                                _sb.Append("Block Definitions for members of the Populate group:\n");
                                //Unlike what we did with Properties, we won't try to filter out 
                                //duplicate entries. If the user runs this on the whole grid, they 
                                //can deal with the consequences.
                                foreach (IMyTerminalBlock block in defBlocks)
                                {
                                    _sb.Append(
                                        $" {block.CustomName}:\n" +
                                        //The string of GetType includes the entire pedigree of the 
                                        //object. We just want the last bit.
                                        $"   Interface: {(block.GetType() + "").Replace("SpaceEngineers.Game.Entities.Blocks.", "")}\n" +
                                        $"   TypeID: {block.BlockDefinition.TypeIdString}\n" +
                                        $"   SubTypeID: {block.BlockDefinition.SubtypeId}\n" +
                                        $"\n");
                                }
                                log.add(_sb.ToString());
                                _sb.Clear();
                            }
                            //If there is no populate group on the grid, complain.
                            else
                            {
                                log.add("Received Definitions command, but there is no Populate " +
                                    "block group on the grid.");
                            }
                            break;
                        //Writes a template to the Populate section of the PB for a given 
                        //configuration type.
                        //Argument format: Template <TemplateType> <args>
                        //Argument example: Template Surface Report Script blank Report, which writes
                        //a template for the first, second, and fourth surfaces on a block.
                        case "template":
                            bool success;
                            string templateOutcome = writeTemplate(argReader, _sb, out success);
                            //If writeTemplate was successful, we have a template that needs to be
                            //written to the PB's Populate section.
                            if (success)
                            {
                                List<string> pbConfigSections = new List<string>();
                                int populateIndex = redneckDeleteSection(Me.CustomData, "Populate", ref pbConfigSections);
                                //If we didn't find a Populate section, add an entry for it at the
                                //top of the list.
                                if (populateIndex == -1)
                                {
                                    pbConfigSections.Insert(0, "");
                                    populateIndex = 0;
                                }
                                pbConfigSections[populateIndex] = templateOutcome;
                                _sb.Clear();
                                foreach (string section in pbConfigSections)
                                { _sb.Append($"[{section}"); }
                                Me.CustomData = _sb.ToString();
                                //If we got to this point, we can be pretty sure that the second 
                                //argument of the argReader contains a string.
                                log.add($"Added {argReader.Argument(1)} template to the Populate section of this " +
                                    "Programmable Block.");
                            }
                            //If writeTemplate wasn't successful, we have an error that needs to be
                            //logged.
                            else
                            {
                                log.add(templateOutcome);
                            }
                            _sb.Clear();
                            break;
                        //Search the grid for block types compatible without tallies, and automatically
                        //write the configuration needed to make them work.
                        //Argument format: AutoPopulate
                        case "autopopulate":
                            List<IMyTerminalBlock> apBlocks = new List<IMyTerminalBlock>();
                            int blockCounter = 0;
                            string initTag = $"{tag}Init";
                            //First order of business is to make sure there isn't existing Shipware 
                            //configuration on the grid. We want a clean slate.
                            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(apBlocks, b =>
                                b.IsSameConstructAs(Me) && MyIni.HasSection(b.CustomData, tag));
                            //We also need to check for the presence of Init config on the PB
                            if (MyIni.HasSection(Me.CustomData, initTag))
                            { apBlocks.Add(Me); }
                            //If we find no blocks with configuration, or the 'confirm' flag is set.
                            if (apBlocks.Count == 0 || argReader.Switch("confirm"))
                            {
                                List<Tally> tallyList = tallies.ToList();
                                string outcome = autoPopulate(GridTerminalSystem, ref tallyList, iniReadWrite);
                                /*int apInstructions = Runtime.CurrentInstructionCount;*/
                                Me.CustomData = reconstitute(Me.CustomData, tallyList,
                                    sets.Values.ToList(), triggers.ToList());
                                /*int reconInstructions = Runtime.CurrentInstructionCount - apInstructions;*/
                                log.add(outcome);
                                /*
                                log.add($"{outcome}\nAutoPopulate required {apInstructions} / " +
                                    $"{Runtime.MaxInstructionCount} ({apInstructions / Runtime.MaxInstructionCount * 100}%) " +
                                    $"instructions, with Reconstitute requiring an additional {reconInstructions} / " +
                                    $"{Runtime.MaxInstructionCount} ({reconInstructions / Runtime.MaxInstructionCount * 100}%).");
                                */
                                //Queue up an evaluate
                                Save();
                                Runtime.UpdateFrequency = UpdateFrequency.Once;
                            }
                            //If there's already ShipWare configuration on the grid, complain. And
                            //point the user at a way to fix it.
                            else
                            {
                                //We may have a bunch of these. Time to bust out the StringBuilder.
                                _sb.Clear();
                                _sb.Append("Received AutoPopulate command, but there is already ShipWare " +
                                    "configuration on the grid. Running this command again with the -confirm " +
                                    "switch will not modify existing Tally configuration, but will re-" +
                                    "generate the report on the Programmable Block's screen, and replace " +
                                    "configuration on the following subject blocks:\n");
                                foreach (IMyTerminalBlock block in apBlocks)
                                {
                                    _sb.Append($" -{block.CustomName}\n");
                                    blockCounter++;
                                    //Let's not spam the log. If there's more than 15 blocks, the user
                                    //won't remember them anyway.
                                    if (blockCounter >= 15)
                                    {
                                        _sb.Append($" -And {apBlocks.Count - blockCounter} other blocks.");
                                        break;
                                    }
                                }
                                log.add(_sb.ToString());
                                _sb.Clear();
                            }
                            break;
                        //Clears Shipware sections and their contents from the members of the 
                        //'Populate' group on the grid
                        //Argument format: Clear
                        case "clear":
                            List<IMyTerminalBlock> clearBlocks = new List<IMyTerminalBlock>();
                            //Get the blocks in the Populate group from the grid
                            GridTerminalSystem.GetBlockGroupWithName("Populate")?.GetBlocks(clearBlocks);
                            //Were there actually any blocks in this group?
                            if (clearBlocks.Count > 0)
                            {
                                List<string> sectionNames = new List<string>();
                                string[] splitName;
                                int clearCounter = 0;
                                foreach (IMyTerminalBlock block in clearBlocks)
                                {
                                    //Pull information from this block.
                                    iniReadWrite.TryParse(block.CustomData);
                                    iniReadWrite.GetSections(sectionNames);
                                    foreach (string targetSection in sectionNames)
                                    {
                                        splitName = targetSection.Split('.');
                                        //All ShipWare sections will have this prefix.
                                        if (splitName[0] == PREFIX)
                                        {
                                            //TODO: Monitor. I'm pretty sure DeleteSection can be trusted
                                            //for this.
                                            iniReadWrite.DeleteSection(targetSection);
                                            clearCounter++;
                                        }
                                    }
                                    //Replace the block's CustomData with our altered configuration.
                                    block.CustomData = iniReadWrite.ToString();
                                }
                                //Clear our MyIni.
                                iniReadWrite.Clear();
                                log.add($"Clear command executed on {clearBlocks.Count} blocks. Removed " +
                                    $"{clearCounter} Shipware sections.");
                            }
                            //If there weren't any blocks in the grid's Populate group, complain.
                            else
                            {
                                log.add("Received Clear command, but there is no Populate " +
                                            "block group on the grid.");
                            }
                            break;
                        //Change the ID of this script, and updates the configuration of every block 
                        //on the grid to use the new ID.
                        //Argument format: ChangeID <name>
                        //Argument example: ChangeID Komodo
                        case "changeid":
                            //Did the user include a new ID? And nothing else?
                            if (argReader.ArgumentCount == 2)
                            {
                                //Put a handle on the ID the user wants to use
                                string newID = argReader.Argument(1);
                                //...and the new tag, as long as we're at it.
                                string newTag = $"{PREFIX}.{newID}";
                                //The first step is actually going to be to talk to the grid and 
                                //figure out which blocks we're going to need to modify.
                                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                                //Get all the blocks on this construct using the existing tag.
                                Hammers.findBlocks<IMyTerminalBlock>(GridTerminalSystem, blocks, b =>
                                    (b.IsSameConstructAs(Me) && MyIni.HasSection(b.CustomData, tag)));
                                //For every block that we found...
                                foreach (IMyTerminalBlock block in blocks)
                                //Replace instances of the old tags with the new tag. Include the 
                                //brackets to decrease the odds of us hitting something we shouldn't
                                { block.CustomData = block.CustomData.Replace($"[{tag}]", $"[{newTag}]"); }
                                //We also need to re-tag the Init section on the PB
                                Me.CustomData = Me.CustomData.Replace($"[{tag}Init]", $"[{newTag}Init]");
                                //Now that we've replaced the old tag in the config, go ahead and 
                                //update the tag in memory
                                customID = newID;
                                //The best way to make sure this sticks and then works properly 
                                //afterward is to fully re-initialize the script.
                                Save();
                                initiate();
                                evaluate();
                                log.add($"ChangeID complete, {blocks.Count} blocks modified. The ID " +
                                    $"of this script instance is now '{customID}', and its tag is now '{tag}'.");
                            }
                            //If the user included more than one arguement, complain. We don't know 
                            //what to do with spaces.
                            else if (argReader.ArgumentCount > 2)
                            {
                                log.add($"Received ChangeID command with too many arguments. New IDs " +
                                    $"cannot contain spaces.");
                            }
                            //If the user did not give us a new ID, complain.
                            else
                            { log.add($"Received ChangeID command with no new ID."); }
                            break;
                        //Run the evaluate() method, checking for any changes to the grid or the 
                        //CustomData of its blocks.
                        case "evaluate":
                            //Evaluate will pull the state of ActionSets from the Storage string, 
                            //better make sure that's up to date
                            Save();
                            //Now we should be able to safely call Evaluate.
                            evaluate();
                            break;
                        //If the user just /has/ to have an update, right now, for some reason, we
                        //can accomodate them. In theory, this could also be used to force an update
                        //when the script is partially compiled, which could be extremely helpful or
                        //terrible, depending on the circumstances.
                        case "update":
                            compute();
                            update();
                            break;
                        //Test function. What exactly it does changes from day to day.
                        case "test":
                            iniReadWrite.TryParse(Me.CustomData);
                            iniReadWrite.AddSection("Words");
                            iniReadWrite.AddSection("Words");
                            iniReadWrite.AddSection("words");
                            Me.CustomData = iniReadWrite.ToString();
                            iniReadWrite.Clear();
                            log.add("Test function executed.");
                            break;
                        //If we don't know what the user is telling us to do, complain.
                        default:
                            log.add($"Received un-recognized run command '{command}'.");
                            break;
                    }
                }
            }
            //Re-echo the event log
            Echo(log.toString());
            //We only 'claim' updates at the end of update tics. That way, everything else gets a 
            //crack at them.
            if ((updateSource & UpdateType.Update100) != 0)
            {
                foreach (Raycaster caster in raycasters.Values)
                { caster.updateClaimed(); }
                log.updateClaimed();
            }
        }

        //Polls tally-related blocks for the freshest data
        public void compute()
        {
            //CLEAR ALL THE OLD VALUES
            foreach (Tally tally in tallies)
            { tally.clearCurr(); }
            //CALCULATE ALL THE NEW VALUES
            foreach (Container container in containers)
            { container.sendCurrToTallies(); }
            foreach (Tally tally in tallies)
            { tally.compute(); }
            //Now that all the data has been collected, we can make decisions
            foreach (Trigger trigger in triggers)
            { trigger.check(); }
        }

        //Displays status information about Talies and ActionSets on Reports and Indicators.
        public void update()
        {
            //WRITE ALL THE REPORTS
            foreach (IReportable report in reports)
            { report.update(); }
            //COLOR ALL THE LIGHTS
            foreach (Indicator indicator in indicators)
            { indicator.update(); }
        }

        //Attempt to operate an action set. Returns a string indicating what the method thinks happened
        //string actionTarget: The name of the ActionSet the user is trying to operate.
        //string actionCommand: The command that is to be performed on the ActionSet.
        //string source: The source of the command, used to make error messages more informative.
        //  Left blank for run commands, "IGC-directed " (Note the space!) for the IGC.
        public string tryTakeAction(string actionTarget, string actionCommand, string source)
        {
            //Stores what we think is the result of this method running.
            string outcome = "";
            bool fired = true;
            if (sets == null)
            {
                outcome = "Received Action command, but script configuration isn't loaded.";
                fired = false;
            }
            //If we actually know what ActionSet the user is talking about...
            else if (sets.ContainsKey(actionTarget))
            {
                try
                {
                    //The nice thing about ActionSets? It's /only/ easy commands.
                    if (actionCommand == "on")
                    { sets[actionTarget].setState(true); }
                    else if (actionCommand == "off")
                    { sets[actionTarget].setState(false); }
                    else if (actionCommand == "switch")
                    { sets[actionTarget].switchState(); }
                    //If it isn't one of the easy commands, complain. Spread the 
                    //word of the easy commands.
                    else
                    {
                        outcome = $"Received unknown {source}command '{actionCommand}' for ActionSet " +
                            $"'{actionTarget}'. Valid commands for ActionSets are 'On', 'Off', and " +
                            $"'Switch'.";
                        fired = false;
                    }
                }
                catch (InvalidCastException e)
                {
                    string identifier = "<ID not provided>";
                    if (e.Data.Contains("Identifier"))
                    { identifier = $"{e.Data["Identifier"]}"; }
                    outcome = $"An invalid cast exception occurred while running {source}command " +
                        $"'{actionCommand}' for ActionSet '{actionTarget}' at {identifier}. Make sure " +
                        $"the action specified in configuration can be performed by {identifier}.";
                    fired = false;
                }
                catch (Exception e)
                {
                    string identifier = "<ID not provided>";
                    if (e.Data.Contains("Identifier"))
                    { identifier = $"{e.Data["Identifier"]}"; }
                    outcome = $"An exception occurred while running {source}command '{actionCommand}' " +
                        $"for ActionSet '{actionTarget}' at {identifier}.\n  Raw exception message:\n " +
                        $"{e.Message}\n  Stack trace:\n{e.StackTrace}";
                    fired = false;
                }
                //We'll go ahead and call update() here. Won't be the end of 
                //the world if we waste it on an error.
                update();
            }
            //If we don't know what ActionSet the user is talking about, complain.
            else
            {
                outcome = $"Received {source}command '{actionCommand}' for un-recognized " +
                    $"ActionSet '{actionTarget}'.";
                fired = false;
            }
            //If we think the action fired, and we have a source (IGC-directed, in general)
            if (fired && !String.IsNullOrEmpty(source))
            { outcome = $"Carried out {source}command '{actionCommand}' on ActionSet '{actionTarget}'."; }
            return outcome;
        }

        public string reconstitute(string pbConfig, List<Tally> reconTallies, List<ActionSet> reconActions,
            List<Trigger> reconTriggers)
        {
            string initTag = $"{tag}Init";
            List<string> reconSections = new List<string>();
            int reconTargetSection = redneckDeleteSection(pbConfig, initTag, ref reconSections);
            //Triggers can be enabled or disabled by ActionSets. Configuration for this is in the
            //Trigger block, but the data is stored in the ActionPlans of the ActionSet. So when 
            //we're writing config, we need something to hold on to the data until we're ready for it.
            Dictionary<Trigger, MyTuple<string, ActionPlanTrigger>> triggerPlans = new Dictionary<Trigger, MyTuple<string, ActionPlanTrigger>>();
            //If we didn't find the target section, add a new string on to the end of
            //the list so we can put our config there.
            if (reconTargetSection == -1)
            {
                reconSections.Add("");
                reconTargetSection = reconSections.Count - 1;
            }

            _sb.Clear();
            //Each entry that comes out of deleteSection has its opening bracket
            //missing. We'll add them back later in bulk; for now, this section needs
            //to be consistent with its bretheren.
            _sb.Append($"{initTag}]\n");
            int reconCounter = 0;
            //Tell the objects we were passed to write their configs
            foreach (Tally tally in reconTallies)
            {
                if (!tally.doNotReconstitute)
                {
                    _sb.Append($"{tally.writeConfig(reconCounter)}\n");
                }
                reconCounter++;
            }
            reconCounter = 0;
            foreach (ActionSet action in reconActions)
            {
                _sb.Append($"{action.writeConfig(reconCounter, ref triggerPlans)}\n");
                reconCounter++;
            }
            reconCounter = 0;
            foreach (Trigger trigger in reconTriggers)
            {
                _sb.Append($"{trigger.writeConfig(reconCounter, ref triggerPlans)}\n");
                reconCounter++;
            }
            //Commit the reconstituted config to our sections list
            reconSections[reconTargetSection] = _sb.ToString();
            _sb.Clear();
            //Recompile the config
            foreach (string reconSection in reconSections)
            {
                _sb.Append($"[{reconSection.Trim()}\n\n");
            }
            //Commit the 'new' config to the string we were handed
            pbConfig = _sb.ToString();
            _sb.Clear();
            return pbConfig;
        }

        /* A method used to write configuration data from a section on the PB to blocks in a 'Populate'
         * group on the grid. Can also merge-in non-duplicate keys from a 'Existing' section
         *  -blocks: A list of blocks, containing the members of the block group 'Populate'
         *  -popReference: One of the global MyIni objects, containing a parse of the PB's CustomData.
         *   Initially, it will be used as a reference, but the contents of the object will be changed 
         *   in the process, and its contents will be cleared at the end of the method
         *  -rawIni: A reference to the global RawTextIni object, which will be used to apply config 
         *   to blocks on the grid. Does not need to be loaded beforehand, will be cleared after use.
         *  -targetWriter: A reference to the global StringBuilder. Will be cleared after use.
         *  -trouble: An output string that will contain any error messages
         *  -merge: Has the merge flag been set for this Populate? Default false.
         * Returns: The number of keys that have been added to each individual block.
         */
        public int populate(List<IMyTerminalBlock> blocks, MyIni popReference, RawTextIni rawIni,
            StringBuilder _sb, out string trouble, bool merge = false)
        {
            trouble = "";
            //Try to get the vaule of the CustomTag key, using the script's default tag if we can't
            //find it.
            string populateTag = popReference.Get("Populate", "CustomTag").ToString(tag);
            //We don't actually want to write the CustomTag key to our blocks. Delete it.
            popReference.Delete("Populate", "CustomTag");

            //The next step is to build the section we're going to write to the grid
            List<MyIniKey> popKeys = new List<MyIniKey>();
            popReference.GetKeys("Populate", popKeys);
            //If we're merging, we need to have keys from the Existing section on hand. We'll add
            //them to the new section as well, assuming Populate doesn't specify a value.
            if (merge)
            {
                bool isDuplicate;
                List<MyIniKey> mergeKeys = new List<MyIniKey>();
                popReference.GetKeys("Existing", mergeKeys);
                foreach (MyIniKey mergeKey in mergeKeys)
                {
                    isDuplicate = false;
                    foreach (MyIniKey popKey in popKeys)
                    {
                        if (mergeKey.Name == popKey.Name)
                        {
                            isDuplicate = true;
                            break;
                        }
                    }
                    if (!isDuplicate)
                    { popKeys.Add(mergeKey); }
                }
            }
            _sb.Clear();
            foreach (MyIniKey key in popKeys)
            //Retrieve each key-value pair and write it to the new config, taking into account use 
            //of the multiline functionallity.
            { _sb.Append($"{key.Name} = {newLineToMultiLine(popReference.Get(key).ToString())}\n"); }
            _sb.Append("\n");
            string newSectionContents = _sb.ToString();
            _sb.Clear();

            //Now that we have our new section, we need to go to each member of the Populate group,
            //remove any existing sections using this tag, and add our new section.
            MyIniParseResult parseResult = new MyIniParseResult();
            foreach (IMyTerminalBlock block in blocks)
            {
                if (rawIni.tryLoad(popReference, out parseResult, block.CustomData))
                {
                    rawIni.tryDeleteSection(populateTag);
                    rawIni.addSection(populateTag, newSectionContents);
                    block.CustomData = rawIni.toString();
                }
                //If the parser detects an error with this block's configuration
                else
                {
                    trouble = $"Parse of block '{block.CustomName}' CustomData failed. Reason(s): " +
                        $"{parseResult.Error}";
                    return -1;
                }
            }
            //If we've reached this point, we should have successfully written the new config to the
            //Populate group. Our last steps are to clear out the variables we used...
            popReference.Clear();
            rawIni.clear();
            _sb.Clear();
            //... and return the number of keys we were dealing with.
            return popKeys.Count;
        }

        /* A method that reads a new tally or action, determines if it's acceptable, and returns a 
         * string combining the existing config with the new config tacked on the end. Or, a string
         * telling you what went wrong.
         *  -string pbConfig: The PB's CustomData
         *  -MyIni configReader: A MyIni object, expected to be pre-loaded with a parse of the pbConfig
         *  -out string outcome: A string containing the result of this method. If everything worked,
         *   this will be a new config to plug into the PB. If something went wrong, this will be an
         *   error log.
         * Returns: A boolean which is true if everything worked, false otherwise.
         */
        public string populatePB(string pbConfig, MyIni configReader, out string outcome)
        {
            //The name of the section that holds our initiators.
            string initTag = $"{tag}Init";
            LimitedErrorLog errors = new LimitedErrorLog(_sb, 25);
            MyIniValue iniValue;
            string type = "";
            int ID = -1;
            string initiatorName = "";
            string newConfig = "";
            Tally newTally = null;
            ActionSet newAction = null;
            Trigger newTrigger = null;
            //We'll use these variables to pass information into Reconstitute. If we get that far.
            //For the time being, we'll point them at the globals.
            List<Tally> tallyList = tallies.ToList();
            List<ActionSet> setList = sets.Values.ToList();
            List<Trigger> triggerList = triggers.ToList();
            //Our life will be much easier if we rebuild the Tally dictionary, so that's what we'll do.
            Dictionary<string, Tally> tallyDic = new Dictionary<string, Tally>();
            foreach (Tally tally in tallies)
            { tallyDic.Add(tally.programName, tally); }

            //There's a few pieces of data we need to have. Did we get them?
            iniValue = configReader.Get("Populate", "Type");
            if (iniValue.IsEmpty)
            { errors.add("Type key not found."); }
            else
            { type = iniValue.ToString(); }
            iniValue = configReader.Get("Populate", "ID");
            if (iniValue.IsEmpty)
            { errors.add("ID key not found."); }
            else
            { ID = iniValue.ToInt32(); }

            if (type == "Tally")
            {
                newTally = tryGetTallyFromConfig(configReader, "Populate", ID, iniValue,
                    errors, out initiatorName);
                //We need to make some checks before we move on. Like, did we even just read a tally?
                if (newTally == null)
                {
                    errors.add($"Tally configuration not found. Make sure the numbers in the " +
                        $"configuration keys match the ID at the top of the Populate section.");
                }
                //Leave our blank alone.
                else if (initiatorName.ToLowerInvariant() == "blank")
                {
                    errors.add($"The Tally name '{initiatorName}' is reserved by the script to indicate" +
                            $"where portions of the screen should be left empty. Please choose a " +
                            $"different name.");
                }
                //We need to make a check for duplicates. Unless of course the duplicate is right at
                //the index we've been given, in which case we're fine.
                else if (!(ID < tallies.Count() && initiatorName == tallies[ID].programName))
                {
                    //Check for duplicates in the Tally and Action dictionaries.
                    if (tallyDic.ContainsKey(initiatorName))
                    { errors.add($"The Tally name '{initiatorName}' is already in use by another tally."); }
                    if (sets.ContainsKey(initiatorName))
                    { errors.add($"The Tally name '{initiatorName}' is already in use by an action."); }
                }

                //If we still don't have any errors, prepare our data to be shipped off to the
                //Reconstitute method. That means adding (Or replacing) our new tally into the
                //Tally list, and just pointing at sets.
                if (errors.getErrorTotal() == 0)
                {
                    //Our local recon variables are already pointing at the globals. All we need to
                    //do here is add our new tally.

                    //If this is supposed to go at the end of the list, just use add. Otherwise,
                    //we'll need a replace.
                    if (ID == tallyList.Count)
                    { tallyList.Add(newTally); }
                    else
                    {
                        tallyList.RemoveAt(ID);
                        tallyList.Insert(ID, newTally);
                    }
                    newConfig = reconstitute(pbConfig, tallyList, setList, triggerList);
                    outcome = $"Carried out Populate command, adding a new initiator for the {type} " +
                        $"{initiatorName} to the programmable block.";
                    return newConfig;
                }
            }
            else if (type == "Action")
            {
                //Usually, ActionFromConfig takes one MyIni loaded with configuration data, and another
                //containing a parse of the storage string. There isn't going to be anything in the 
                //storage string for this new object, though, so we'll just feed it the config reader 
                //again. When it tries to find an existing value, it'll fail and substitute false.
                //This seems like a good coding practice.
                newAction = tryGetActionFromConfig(configReader, configReader, "Populate",
                    ID, iniValue, errors, out initiatorName);
                //We need to make some checks before we move on. Like, did we even just read an action?
                if (newAction == null)
                {
                    errors.add($"Action configuration not found. Make sure the numbers in the " +
                        $"configuration keys match the ID at the top of the Populate section.");
                }
                //Leave our blank alone.
                else if (initiatorName.ToLowerInvariant() == "blank")
                {
                    errors.add($"The initiator name '{initiatorName}' is reserved by the script to " +
                        $"indicate where portions of the screen should be left empty. Please choose " +
                        $"a different name.");
                }
                //We need to make a check for duplicates. Unless of course the duplicate is right at
                //the index we've been given, in which case we're fine. 
                else if (!(ID < setList.Count() && initiatorName == setList[ID].programName))
                {
                    if (tallyDic.ContainsKey(initiatorName))
                    { errors.add($"The Action name '{initiatorName}' is already in use by a Tally."); }
                    if (sets.ContainsKey(initiatorName))
                    { errors.add($"The Action name '{initiatorName}' is already in use by another Action."); }
                }

                //If we still don't have any errors, prepare our data to be shipped off to the
                //Reconstitute method. That means adding (Or replacing) our new action into the
                //action list, and making a copy of the tally array as a list.
                if (errors.getErrorTotal() == 0)
                {
                    //If this is supposed to go at the end of the list, just use add. Otherwise,
                    //we'll need a replace.
                    if (ID == setList.Count)
                    { setList.Add(newAction); }
                    else
                    {
                        setList.RemoveAt(ID);
                        setList.Insert(ID, newAction);
                    }
                    newConfig = reconstitute(pbConfig, tallyList, setList, triggerList);
                    outcome = $"Carried out Populate command, adding a new initiator for the {type} " +
                        $"{initiatorName} to the programmable block.";
                    return newConfig;
                }
            }
            else if (type == "Trigger")
            {
                tryGetTriggerFromConfig(configReader, tallyDic, sets, "Populate", ID, iniValue,
                    ref newTally, ref newAction, ref newTrigger, errors);
                //The only thing we check for triggers is, 'Did we get a trigger'?
                if (newTrigger == null)
                {
                    errors.add($"Trigger configuration not found. Make sure the numbers in the " +
                        $"configuration keys match the ID at the top of the Populate section.");
                }
                if (errors.getErrorTotal() == 0)
                {
                    //If this is supposed to go at the end of the list, just use add. Otherwise,
                    //we'll need a replace.
                    if (ID == triggerList.Count)
                    { triggerList.Add(newTrigger); }
                    else
                    {
                        triggerList.RemoveAt(ID);
                        triggerList.Insert(ID, newTrigger);
                    }
                    newConfig = reconstitute(pbConfig, tallyList, setList, triggerList);
                    outcome = $"Carried out Populate command, adding a new initiator for the {type} " +
                        $"{initiatorName} to the programmable block.";
                    return newConfig;
                }
            }
            else
            { errors.add("Initiator type must be either 'Tally', 'Action', or 'Trigger'."); }

            //If one of the type paths executed correctly, they should've already returned. If
            //we reach this point, something has gone wrong.
            outcome = $"Received Populate command, but the contents of the Populate section failed " +
                $"evaluation. Reason(s):\n{errors.toString()}";
            return newConfig;
        }

        public string readableMyIni(StringBuilder _sb, MyIni subject, bool copyComments = false,
            string targetSection = "", string renameSection = "", string filterString = "")
        {
            string config = "";
            string workingString = "";
            List<string> sectionNames = new List<string>();
            //TODO: Consider passing existing instances in
            List<MyIniKey> keys = new List<MyIniKey>();
            MyIniValue iniValue;

            _sb.Clear();
            //If no target section is defined...
            if (targetSection == "")
            { subject.GetSections(sectionNames); }
            else
            { sectionNames.Add(targetSection); }

            foreach (string sectionName in sectionNames)
            {
                //Step one is to add the section tag to the config we're buidling
                if (targetSection != "" && renameSection != "")
                { _sb.Append($"[{renameSection}]\n"); }
                else
                { _sb.Append($"[{sectionName}]\n"); }

                //We'll need a list of all keys in this section
                subject.GetKeys(sectionName, keys);
                foreach (MyIniKey key in keys)
                {
                    iniValue = subject.Get(key);
                    if (copyComments)
                    {

                        workingString = subject.GetComment(key);
                        //The comment stored in the key does not contain any semicolons. So for multi-
                        //line comments, we have to add in our own.
                        //Also, we need a null check, because keys without comments will return null
                        workingString = ";" + workingString;
                        workingString = workingString?.Replace("\n", "\n;");
                        workingString = workingString.Remove(workingString.Length - 1);
                        _sb.Append(workingString);
                    }
                    _sb.Append($"{key.Name} = {iniValue}\n");
                }
                //Last step is to tack on an additional newline at the end of the section
                _sb.Append("\n");
            }
            config = _sb.ToString();
            _sb.Clear();

            return config;
        }

        //This iteration is pretty redneck, to be sure.
        //This method takes an ini config, splits it into a list of sections, and deletes the 
        //specified section. 
        //string config: The ini configuration to be modified
        //string targetSection: The name of the section to be deleted
        //List<string> sections: The list that will hold the remaining sections. IMPORTANT: The 
        //  opening bracket on each section will be missing and needs to be replaced.
        //Returns: The index where the target section was, now empty. Or -1, if the section wasn't found
        public int redneckDeleteSection(string config, string targetSection, ref List<string> sections)
        {
            sections = config.Split('[').ToList<string>();
            //The first entry will be an empty string that comes 'before' the opening bracket of 
            //the first section. Discard it.
            sections.RemoveAt(0);
            int sectionIndex = -1;
            //Figure out which one of these contains the target section.
            for (int i = 0; i < sections.Count; i++)
            {
                //If we find the target section, store its location, delete it, and exit the loop.
                if (sections[i].Contains($"{targetSection}]"))
                {
                    sectionIndex = i;
                    //Delete the targeted section
                    sections[sectionIndex] = "";
                    break;
                }
            }

            return sectionIndex;
        }

        public string writeTemplate(MyCommandLine argReader, StringBuilder _sb, out bool isSuccessful)
        {
            _sb.Clear();
            //Optimism? Not for programmers.
            isSuccessful = false;
            string type = argReader.Argument(1)?.ToLowerInvariant();
            //There're a couple of bits of text that nearly all of our templates will share. It'll 
            //be worth it to go ahead and declare them now.
            string header =
                "Populate]\n" +
                ";  Note: Optional keys are commented out (Their line begins with a semi-\n" +
                ";  colon). They will only be sent to the members of the Populate group \n" +
                ";  if you remove the semi-colons. \n\n";
            string actionSection =
                ";  Any ActionSets this block is tied to. Action Sets are configured from\n" +
                ";  their own section, the template name of which is 'ActionSection'.\n" +
                ";ActionSets = < Comma separated list of ActionSet names, default empty >\n\n";
            //This string is only used in three of the templates, but still.
            string populatePBHeader =
                    ";  Populate uses a specialized method for object initiators, and\n" +
                    ";  that method requires a few additional pieces of data: A unique\n" +
                    ";  CustomTag, a type, and an ID. All of these values are pre-generated\n" +
                    ";  and do not need to be modified.\n";
            //Surface and MFD types basically share the same template
            if (type == "surface" || type == "mfdsection")
            {
                //We'll go ahead and append the header. It'll be a bit before we can figure out if
                //we have everything we need to write a template.
                _sb.Append(header);
                string prefix = "Surface";
                //There're two main diferences between Surfaces and MFDSections: The prefix they 
                //use, and if they have a CustomTag key.
                if (type == "mfdsection")
                {
                    prefix = "Page";
                    _sb.Append(
                        ";  The name of the MFD whose pages this discreet section will\n" +
                        ";  configure. It will need to have the 'SW.' prefix, as in\n" +
                        ";  'SW.Raycaster'.\n");
                    _sb.Append(
                        "CustomTag = < Name of MFD, prefixed with 'SW.' >\n");
                }
                string fontSizeComment =
                    "\n;  The font size to be used.\n";
                string fontComment =
                    "\n;  The name of the font to be used.\n";
                string foreColorComment =
                    "\n;  The foreground color of this surface.\n" +
                    ";  Valid color names are green, lightblue, yellow, orange, and red.\n" +
                    ";  By default, the color that has been configured for this surface in\n" +
                    ";  the terminal will be used.\n";
                string backColorComment =
                    "\n;  The background color of this surface.\n";
                //We'll go ahead and declare variables for the strings we'll use in the loop.
                string problemSubTypes = "";
                string subtype, fontSizeKey, fontKey, foreColorKey, backColorKey;
                int surface;
                //We need a flag for every single comment type, and they all need to be intialized
                //to false. We'll do that in a legal but mildly heretical fashion.
                bool fontFlag, colorFlag, elementsFlag, titleFlag, columnsFlag, mfdFlag, scriptFlag,
                    dataFlag, charCountFlag, linkActionFlag = charCountFlag = dataFlag = scriptFlag =
                    mfdFlag = columnsFlag = titleFlag = elementsFlag = colorFlag = fontFlag = false;
                //Argument 0 told us we need a template, 1 told us it's a surface or MFD. From 2 on,
                //we're being told subtypes.
                for (int i = 2; i < argReader.ArgumentCount; i++)
                {
                    subtype = argReader.Argument(i)?.ToLowerInvariant();
                    //i starts at 2, but our surfaces start at 0. A slight adjustment is needed.
                    surface = i - 2;
                    //As with reports, a ocurrence of 'blank' means that we're leaving a hole.
                    if (subtype != "blank")
                    {
                        //Even subtypes use a lot of the same keys. We'll go ahead and
                        //define them now, so we can re-use them.
                        fontSizeKey = $";{prefix}{surface}FontSize = < Decimal Value, default 1 >\n";
                        fontKey = $";{prefix}{surface}Font = < Font Name, default Debug >\n";
                        foreColorKey = $";{prefix}{surface}ForeColor = < RGB value 0,0,0 or basic color name. >\n";
                        backColorKey = $";{prefix}{surface}BackColor = < RGB value 0,0,0 or basic color name. >\n";
                        //Now we need to decide which flavor of report template to add
                        switch (subtype)
                        {
                            case "report":
                                _sb.Append(elementsFlag ? "" :
                                    "\n;  The names of the Elements (Tallies or ActionSets) whose status will be\n" +
                                    ";  displayed on this Surface. The word 'blank' can be used to indicate\n" +
                                    ";  that a hole should be left before the next element.\n");
                                _sb.Append($"{prefix}{surface}Elements = < Comma separated list of element names >\n");
                                elementsFlag = true;
                                _sb.Append(titleFlag ? "" :
                                    "\n;  The title of this particular report, which will appear at the top of its\n" +
                                    ";  surface.\n");
                                _sb.Append($";{prefix}{surface}Title = < Title, default empty >\n");
                                titleFlag = true;
                                _sb.Append(columnsFlag ? "" :
                                    "\n;  The number of columns in the table that will display this report's \n" +
                                    ";  information.\n");
                                _sb.Append($";{prefix}{surface}Columns = < Integer Value, default 1 >\n");
                                columnsFlag = true;
                                _sb.Append(fontFlag ? "" : fontSizeComment);
                                _sb.Append(fontSizeKey);
                                _sb.Append(fontFlag ? "" : fontComment);
                                _sb.Append(fontKey);
                                fontFlag = true;
                                _sb.Append(colorFlag ? "" : foreColorComment);
                                _sb.Append(foreColorKey);
                                _sb.Append(colorFlag ? "" : backColorComment);
                                _sb.Append(backColorKey);
                                colorFlag = true;
                                isSuccessful = true;
                                break;
                            case "mfd":
                                _sb.Append(mfdFlag ? "" :
                                    "\n;  The name of the Multi-Function Display that will be shown on this\n" +
                                    ";  surface. MFDs are configured from their own section, the template name\n" +
                                    ";  of which is 'MFDSection'.\n");
                                _sb.Append($"{prefix}{surface}MFD = < MFD Name >\n");
                                mfdFlag = true;
                                isSuccessful = true;
                                break;
                            case "script":
                                _sb.Append(scriptFlag ? "" :
                                    "\n;  The name of the Script that will be displayed on this surface. Used\n" +
                                    ";  primarily on MFD pages.\n" +
                                    ";  The names of SE's built in scripts are: TSS_ClockAnalog,\n" +
                                    ";  TSS_ArtificialHorizon, TSS_ClockDigital, TSS_EnergyHydrogen,\n" +
                                    ";  TSS_FactionIcon, TSS_Gravity, TSS_Velocity, TSS_VendingMachine,\n" +
                                    ";  TSS_Weather, TSS_Jukebox\n");
                                _sb.Append($"{prefix}{surface}Script = < Script Name >\n");
                                scriptFlag = true;
                                _sb.Append(colorFlag ? "" : foreColorComment);
                                _sb.Append(foreColorKey);
                                _sb.Append(colorFlag ? "" : backColorComment);
                                _sb.Append(backColorKey);
                                colorFlag = true;
                                isSuccessful = true;
                                break;
                            case "datatype":
                                _sb.Append(dataFlag ? "" :
                                    "\n;  The type of data that will be displayed on this surface. Valid\n" +
                                    ";  DataTypes are: Log, CustomData, DetailInfo, and Raycaster.\n");
                                _sb.Append($"{prefix}{surface}DataType = < DataType Name >\n");
                                _sb.Append(dataFlag ? "" :
                                    "\n;  For the CustomData, DetailInfo, and Raycaster DataTypes, a DataSource\n" +
                                    ";  must be specified. This will be the name of a block on the grid for\n" +
                                    ";  CustomData and DetailInfo types, and the name of a Raycaster for the\n" +
                                    ";  Raycaster type.\n");
                                _sb.Append($";{prefix}{surface}DataSource = < DataSource Name >\n");
                                dataFlag = true;
                                _sb.Append(fontFlag ? "" : fontSizeComment);
                                _sb.Append(fontSizeKey);
                                _sb.Append(fontFlag ? "" : fontComment);
                                _sb.Append(fontKey);
                                fontFlag = true;
                                _sb.Append(charCountFlag ? "" :
                                    "\n;  A rudimentary text wrap can be applied to these reports. It isn't\n" +
                                    ";  particularly efficient, so it cannot be used with the DetailInfo.\n" +
                                    ";  DataType (Due to how frequently that updates).\n" +
                                    ";  Note that this value is not a hard limit. The text will be sent to\n" +
                                    ";  the next line only once the length of a word has exceeded this value.\n");
                                _sb.Append($";{prefix}{surface}CharPerLine = < Integer Value, default empty >\n");
                                charCountFlag = true;
                                _sb.Append(colorFlag ? "" : foreColorComment);
                                _sb.Append(foreColorKey);
                                _sb.Append(colorFlag ? "" : backColorComment);
                                _sb.Append(backColorKey);
                                colorFlag = true;
                                isSuccessful = true;
                                break;
                            default:
                                problemSubTypes += $"{subtype}, ";
                                Me.GetSurface(0).WriteText(problemSubTypes);
                                break;
                        }
                        //There's two more keys we need to talk about that are specific 
                        //to MFDs. We can go ahead and tack them on to _sb even if the 
                        //user gave us a bad subtype because it'll just be discarded in
                        //that case.
                        if (prefix == "Page")
                        {
                            _sb.Append(linkActionFlag ? "" :
                                "\n;  The name of the ActionSet whose On state will cause this MFD\n" +
                                ";  page to be displayed.\n");
                            _sb.Append($";Page{surface}LinkActionSetOn = < Name of ActionSet, default empty >\n");
                            _sb.Append(linkActionFlag ? "" :
                                "\n;  The name of the ActionSet whose Off state will cause this MFD\n" +
                                ";  page to be displayed.\n");
                            _sb.Append($";Page{surface}LinkActionSetOff = < Name of ActionSet, default empty >\n");
                            linkActionFlag = true;
                        }
                        _sb.Append("\n");
                    }
                }
                //If we ran into a problem. 
                if (!String.IsNullOrEmpty(problemSubTypes))
                {
                    isSuccessful = false;
                    //Trim the trailing comma
                    problemSubTypes = problemSubTypes.Remove(problemSubTypes.Length - 2);
                    return $"Received Template command for type {type}, but did not recognize the " +
                        $"following subtype(s):\n{problemSubTypes}\nValid subtypes are: Report, " +
                        $"MFD, Script, DataType, and blank (Which indicates that no template will be " +
                        $"written for that surface).";
                }
                //One thing we also need to account for is if no subtypes were provided
                else if (!isSuccessful)
                {
                    return $"Received Template command for type {type}, but did not receive any " +
                        $"subtypes. A spaced list of subtypes tells the parser which template to " +
                        $"write for the next surface in sequence. Valid subtypes are: Report, MFD, " +
                        $"Script, DataType, and blank (Blank indicates that no template will be " +
                        $"written for that surface).";
                }
            }
            //Format: Template Subject
            else if (type == "subject")
            {
                _sb.Append(header);
                _sb.Append(
                    ";  The Tallies that this block reports to. Inventory-type tallies\n" +
                    ";  included in this list will read data from all inventories on\n" +
                    ";  the block.\n");
                _sb.Append(
                    ";Tallies = < Comma separated list of Tally names, default empty >\n\n");
                _sb.Append(
                    ";  A list of tallies that will read data from this specific inventory,\n" +
                    ";  with the first inventory being 0, the second being 1, and so forth.\n");
                _sb.Append(
                    ";Inv0Tallies = < Comma separated list of Tally names, default empty >\n\n" +
                    $"{actionSection}");
                isSuccessful = true;
            }
            //Format: Template Indicator
            else if (type == "indicator")
            {
                _sb.Append(header);
                _sb.Append(
                    ";  The Tally or ActionSet whose state will be used to define the color\n" +
                    ";  of this light.\n");
                _sb.Append(
                    "Element = < Element Name >\n\n" +
                    $"{actionSection}");
                isSuccessful = true;
            }
            //Format: Template Raycaster
            else if (type == "raycaster")
            {
                _sb.Append(header);
                _sb.Append(
                    ";  The name that will be associated with the Raycaster object that\n" +
                    ";  will be used to perform raycasts with this camera.\n");
                _sb.Append(
                    "RaycasterName = < Name of this Raycaster >\n\n");
                _sb.Append(
                    ";  The name that will be used when displaying the charge of this\n" +
                    ";  Raycaster in an Element.\n");
                _sb.Append(
                    ";RaycasterDisplayName = < Name to be displayed, default empty >\n\n");
                _sb.Append(
                    ";  The maximum distance of the first scan that will be performed\n" +
                    ";  by this Raycaster, in meters.\n");
                _sb.Append(
                    ";RaycasterBaseRange = < Decimal value, default 1000 >\n\n");
                _sb.Append(
                    ";  Multiplier that will be applied to the maximum distance of each\n" +
                    ";  successive scan.\n");
                _sb.Append(
                    ";RaycasterMultiplier = < Decimal value, default 3 >\n\n");
                _sb.Append(
                    ";  The range of the longest scan that will be performed by this\n" +
                    ";  Raycaster, in meters. One scan at this range will always be\n" +
                    ";  performed.\n");
                _sb.Append(
                    ";RaycasterMaxRange = < Decimal value, default 27000 >\n\n" +
                    $"{actionSection}");
                isSuccessful = true;
            }
            //Format: Template ActionSection
            else if (type == "actionsection")
            {
                _sb.Append(header);
                _sb.Append(
                    ";  The name of the ActionSet this discreet section will configure\n" +
                    ";  actions for. It will need to have the 'SW.' prefix, as in\n" +
                    ";  'SW.Raycaster'.\n");
                _sb.Append(
                    "CustomTag = < Name of target ActionSet, prefixed with 'SW.' >\n\n");
                _sb.Append(
                    ";  The action this block will perform when this ActionSet is set\n" +
                    ";  to 'on'. Common actions are EnableOn, BatteryRecharge,\n" +
                    ";  TankStockpileOn, TimerStart, and GatlingDefensive.\n" +
                    ";  A full list can be found at:\n" +
                    ";  TODO: WRITE THE GUIDE \n");
                _sb.Append(
                    ";ActionOn = < Name of action to be taken, default empty >\n\n");
                _sb.Append(
                    ";  The action this block will perform when this ActionSet is set\n" +
                    ";  to 'off'.\n");
                _sb.Append(
                    ";ActionOff = < Name of action to be taken, default empty >\n\n");
                isSuccessful = true;
            }
            //Format: Template ActionSectionTerminal #
            else if (type == "actionsectionterminal")
            {
                int parts;
                int counter = 0;
                //Try to get the number of parts, using a 1 as a default value.
                if (!Int32.TryParse(argReader.Argument(2), out parts))
                { parts = 1; }
                _sb.Append(header);
                _sb.Append(
                    ";  The name of the ActionSet this discreet section will configure\n" +
                    ";  actions for. It will need to have the 'SW.' prefix, as in\n" +
                    ";  'SW.Raycaster'.\n");
                _sb.Append(
                    "CustomTag = < Name of target ActionSet, prefixed with 'SW.' >\n\n");
                _sb.Append(
                    ";  The name of the Terminal Property that will be targeted by this\n" +
                    ";  part of the ActionSet.\n" +
                    ";  A full list of Terminal Properties can be found at:\n" +
                    ";  https://github.com/malware-dev/MDK-SE/wiki/List-Of-Terminal-Properties-and-Actions \n");
                _sb.Append(
                    $"Action{counter}Property = < Name of Terminal Property >\n\n");
                _sb.Append(
                    ";  The value that will be applied to this property when this ActionSet \n" +
                    ";  is set to 'on'.\n");
                _sb.Append(
                    $";Action{counter}ValueOn = < Value to be applied, default empty >\n\n");
                _sb.Append(
                    ";  The value that will be applied to this property when this ActionSet \n" +
                    ";  is set to 'off'.\n");
                _sb.Append(
                    $";Action{counter}ValueOff = < Value to be applied, default empty >\n\n");
                counter++;
                //Fill in any additional part keys, without the comments
                while (counter < parts)
                {
                    _sb.Append(
                        $"Action{counter}Property = < Name of Terminal Property >\n");
                    _sb.Append(
                        $";Action{counter}ValueOn = < Value to be applied, default empty >\n");
                    _sb.Append(
                        $";Action{counter}ValueOff = < Value to be applied, default empty >\n");
                    _sb.Append("\n");
                    counter++;
                }
                isSuccessful = true;
            }
            //Format: Template Tally #
            else if (type == "tally")
            {
                int ID;
                //If the user didn't deign to provide us with an ID...
                if (!Int32.TryParse(argReader.Argument(2), out ID))
                {
                    iniRead.Clear();
                    iniRead.TryParse(Me.CustomData);
                    ID = findOpening($"{tag}Init", "Tally", iniRead);
                    iniRead.Clear();
                }
                _sb.Append(header);
                _sb.Append(populatePBHeader);
                _sb.Append(
                    "CustomTag = Me\n");
                _sb.Append(
                    "Type = Tally\n");
                _sb.Append(
                    $"ID = {ID}\n\n");
                _sb.Append(
                    ";  The name this tally will use in configuration.\n");
                _sb.Append(
                    $"Tally{ID}Name = < Name of tally >\n\n");
                _sb.Append(
                    ";  The type of this tally. Valid tally types are: Inventory, Item,\n" +
                    ";  Battery, Gas, JumpDrive, Raycast, and PowerProducer.\n" +
                    ";  (Also ShieldHealth, ShieldHeat, and ShieldRegen with Defense Shields)\n");
                _sb.Append(
                    $"Tally{ID}Type = < Type of tally >\n\n");
                _sb.Append(
                    ";  The name this tally will display when shown on reports.\n");
                _sb.Append(
                    $";Tally{ID}DisplayName = < Display Name of tally, default empty >\n\n");
                _sb.Append(
                    ";  For Item tallies, the category of the item to be tracked.\n" +
                    ";  Common SE ItemTypeIDs are: MyObjectBuilder_AmmoMagazine,\n" +
                    ";  MyObjectBuilder_Component, MyObjectBuilder_Ingot, and\n" +
                    ";  MyObjectBuilder_Ore. A full list can be found at:\n" +
                    ";  https://github.com/malware-dev/MDK-SE/wiki/Type-Definition-Listing \n");
                _sb.Append(
                    $";Tally{ID}ItemTypeID = < Name of item category >\n\n");
                _sb.Append(
                    ";  For Item tallies, the file name of the item to be tracked.\n" +
                    ";  Common SE ItemSubTypeIDs are: NATO_25x184mm, SteelPlate,\n" +
                    ";  Construction, Uranium, and Stone. A full list can be found at:\n" +
                    ";  https://github.com/malware-dev/MDK-SE/wiki/Type-Definition-Listing \n");
                _sb.Append(
                    $";Tally{ID}ItemSubTypeID = < Name of item sub type >\n\n");
                _sb.Append(
                    ";  Usually, the maximum value of a tally will be calculated based on\n" +
                    ";  the maximum capacity of the blocks in that tally. You can override\n" +
                    ";  that automatically calculated value here. Note that Item and \n" +
                    ";  Raycast tally types must have this set manually.\n");
                _sb.Append(
                    $";Tally{ID}Max = < Integer value, default empty >\n\n");
                _sb.Append(
                    ";  A multiplier that will be applied to both the current and max\n" +
                    ";  values displayed by this tally. Usually a power of ten. Useful for\n" +
                    ";  things like PowerProducers, which generally have a fractional output.\n");
                _sb.Append(
                    $";Tally{ID}Multiplier = < Integer value, default empty >\n\n");
                _sb.Append(
                    ";  Will this element be color-coded using the assumption that low\n" +
                    ";  values are a good thing?\n");
                _sb.Append(
                    $";Tally{ID}LowGood = < True/False, default false except for inventory >\n\n");
                isSuccessful = true;
            }
            //Format: Template ActionSet #
            else if (type == "actionset")
            {
                int ID;
                //If the user didn't deign to provide us with an ID...
                if (!Int32.TryParse(argReader.Argument(2), out ID))
                {
                    iniRead.Clear();
                    iniRead.TryParse(Me.CustomData);
                    ID = findOpening($"{tag}Init", "Action", iniRead);
                    iniRead.Clear();
                }
                _sb.Append(header);
                _sb.Append(populatePBHeader);
                _sb.Append(
                    "CustomTag = Me\n");
                _sb.Append(
                    "Type = Action\n");
                _sb.Append(
                    $"ID = {ID}\n\n");
                _sb.Append(
                    ";  The name this ActionSet will use in configuration.\n");
                _sb.Append(
                    $"Action{ID}Name = < Name of ActionSet >\n\n");
                _sb.Append(
                    ";  The name this ActionSet will display when shown on reports.\n");
                _sb.Append(
                    $";Action{ID}DisplayName = < Display Name of ActionSet, default empty >\n\n");
                _sb.Append(
                    ";  The color that will be displayed by linked elements and\n" +
                    ";  indicators when this ActionSet is 'on'. Valid color names\n" +
                    ";  are green, lightblue, yellow, orange, and red.\n");
                _sb.Append(
                    $";Action{ID}ColorOn = < RGB value 0,0,0 or basic color name. Default green. >\n\n");
                _sb.Append(
                    ";  The color that will be displayed by linked elements and\n" +
                    ";  indicators when this ActionSet is 'off'.\n");
                _sb.Append(
                    $";Action{ID}ColorOff = < RGB value 0,0,0 or basic color name. Default red. >\n\n");
                _sb.Append(
                    ";  The text that will be used to describe the ActionSet's state\n" +
                    ";  on surface elements when the set is 'on'.\n");
                _sb.Append(
                    $";Action{ID}TextOn = < State descriptor, default 'Enabled' >\n\n");
                _sb.Append(
                    ";  The text that will be used to describe the ActionSet's state\n" +
                    ";  on surface elements when the set is 'off'.\n");
                _sb.Append(
                    $";Action{ID}TextOff = < State descriptor, default 'Disabled' >\n\n");
                _sb.Append(
                    ";  The number of tics to wait before polling blocks for new data\n" +
                    ";  when this ActionSet is 'on'. Basically, a way to reduce the amount\n" +
                    ";  of runtime your CPU is spending running this script on idle grids.\n");
                _sb.Append(
                    $";Action{ID}DelayOn = < Integer value, default 0 >\n\n");
                _sb.Append(
                    ";  The number of tics to wait before polling blocks for new data\n" +
                    ";  when this ActionSet is 'off'.\n");
                _sb.Append(
                    $";Action{ID}DelayOff = < Integer value, default 0 >\n\n");
                _sb.Append(
                    ";  The 'channel' that IGC messages will be sent on when this ActionSet\n" +
                    ";  changes states. For other instances of the ShipWare script, the channel\n" +
                    ";  is the Script Tag, which is visible at the top of the log if it's been\n" +
                    ";  changed from the default of 'SW.Shipware'.\n");
                _sb.Append(
                    $";Action{ID}IGCChannel = < Name of channel, default empty >\n\n");
                _sb.Append(
                    ";  The message that will be sent on the above channel when this\n" +
                    ";  ActionSet is switched 'on'. For invoking an action on another instance\n" +
                    ";  of the ShipWare script, the format is:\n" +
                    ";  'Action {set name} {command} -{reply target}'\n" +
                    ";  ...where 'reply target' is the ScriptTag of this script instance. \n" +
                    ";  As an example:\n" +
                    ";  Action StarboardGate Switch -SW.Miner\n");
                _sb.Append(
                    $";Action{ID}IGCMessageOn = < Message to be sent, default empty >\n\n");
                _sb.Append(
                    ";  The message that will be sent on the above channel when this\n" +
                    ";  ActionSet is switched 'off'.\n");
                _sb.Append(
                    $";Action{ID}IGCMessageOff = < Message to be sent, default empty >\n\n");
                isSuccessful = true;
            }
            //Format: Template Trigger #
            else if (type == "trigger")
            {
                int ID;
                //If the user didn't deign to provide us with an ID...
                if (!Int32.TryParse(argReader.Argument(2), out ID))
                {
                    iniRead.Clear();
                    iniRead.TryParse(Me.CustomData);
                    ID = findOpeningTrigger($"{tag}Init", iniRead);
                    iniRead.Clear();
                }
                _sb.Append(header);
                _sb.Append(populatePBHeader);
                _sb.Append(
                    "CustomTag = Me\n");
                _sb.Append(
                    "Type = Trigger\n");
                _sb.Append(
                    $"ID = {ID}\n\n");
                _sb.Append(
                    ";  The name of the Tally this Trigger will watch.\n");
                _sb.Append(
                    $"Trigger{ID}Tally = < Name of target Tally >\n\n");
                _sb.Append(
                    ";  The name of the ActionSet this Trigger will operate in response\n" +
                    ";  to changes in the value of the target Tally.\n");
                _sb.Append(
                    $"Trigger{ID}ActionSet = < Name of operated ActionSet >\n\n");
                _sb.Append(
                    ";  When the percentage of the target Tally meets or falls below this value,\n" +
                    ";  the LessOrEqualCommand will be sent to the operated ActionSet.\n");
                _sb.Append(
                    $";Trigger{ID}LessOrEqualValue = < Value between 0 and 100 >\n\n");
                _sb.Append(
                    ";  The command that will be sent to the operated ActionSet when the\n" +
                    ";  percentage of the target Tally meets or falls below the LessOrEqualValue.\n");
                _sb.Append(
                    $";Trigger{ID}LessOrEqualCommand = < 'on' or 'off' >\n\n");
                _sb.Append(
                    ";  When the percentage of the target Tally meets or exceeds this value,\n" +
                    ";  the GreaterOrEqualCommand will be sent to the operated ActionSet.\n");
                _sb.Append(
                    $";Trigger{ID}GreaterOrEqualValue = < Value between 0 and 100 >\n\n");
                _sb.Append(
                    ";  The command that will be sent to the operated ActionSet when the\n" +
                    ";  percentage of the target Tally meets or exceeds the GreaterOrEqualValue.\n");
                _sb.Append(
                    $";Trigger{ID}GreaterOrEqualCommand = < 'on' or 'off' >\n\n");
                _sb.Append(
                    ";  The name of an ActionSet that enables or disables this Trigger.\n");
                _sb.Append(
                    $";Trigger{ID}LinkedActionSet = < Name of linked ActionSet >\n\n");
                _sb.Append(
                    ";  The action that will be taken when the linked ActionSet is switched 'on'.\n");
                _sb.Append(
                    $";Trigger{ID}LinkedActionOn = < 'enable' or 'disable' >\n\n");
                _sb.Append(
                    ";  The action that will be taken when the linked ActionSet is switched 'off'.\n");
                _sb.Append(
                    $";Trigger{ID}LinkedActionOff = < 'enable' or 'disable' >\n\n");
                isSuccessful = true;
            }
            else
            {
                return $"Received Template command for unrecognized type " +
                    $"{type}. Valid types are Subject, Surface, Indicator, " +
                    $"Raycaster, MFDSection, ActionSection, ActionSectionTerminal, " +
                    $"Tally, ActionSet, and Trigger.";
            }
            //We've already returned out of this method if we hit an error. So we should be good
            //to go at this point.
            string result = _sb.ToString();
            //Leave the StringBuilder as we found it.
            _sb.Clear();
            return result;
        }

        public int findOpening(string section, string type, MyIni pbConfig)
        {
            int ID = -1;
            int counter = 0;
            string targetKey = "";
            //Until we find a valid ID...
            while (ID == -1)
            {
                //Every object initiator will have a name key. Build one with this ID.
                targetKey = $"{type}{counter}Name";
                //If the config already contains the constructed key, go to the next one.
                if (pbConfig.ContainsKey(section, targetKey))
                { counter++; }
                //If the config doesn't contain the constructed key, we've found our hole.
                else
                { ID = counter; }
            }
            return ID;
        }

        //A version of findOpening, especially for Triggers
        public int findOpeningTrigger(string section, MyIni pbConfig)
        {
            int ID = -1;
            int counter = 0;
            //Until we find a valid ID...
            while (ID == -1)
            {
                //If the config already contains the constructed key, go to the next one.
                if (pbConfig.ContainsKey(section, $"Trigger{counter}Tally"))
                { counter++; }
                //If the config doesn't contain the constructed key, we've found our hole.
                else
                { ID = counter; }
            }
            return ID;
        }

        public string autoPopulate(IMyGridTerminalSystem GTS, ref List<Tally> tallyList, MyIni configWriter)
        {
            const string ORE_TYPE = "MyObjectBuilder_Ore";
            const string INGOT_TYPE = "MyObjectBuilder_Ingot";
            //TODO: Open Christmas Morning
            //const string COMP_TYPE = "MyObjectBuilder_Component";
            const string AMMO_TYPE = "MyObjectBuilder_AmmoMagazine";
            //Stores the number of tallies created
            int tallyCounter = 0;
            //Stores the number of blocks this method has altered. 
            int blockCounter = 0;
            //Stores the number of blocks where we've replaced configuration.
            int replacementCounter = 0;
            //We'll be re-using names frequently, so we'll just set this once per type.
            string tallyName = "";
            //We'll need to calculate a max for several of these.
            double max = 0;
            //When we need a new tally, we'll use this variable
            Tally newTally;
            //We'll use this list to store the tally names that we want to add to the generic report
            //generated at the end of AutoPopulate.
            List<string> reportElements = new List<string>();
            //Unlike most of our tallies, where one tally is assigned to one block type, Composite 
            //tallies have multiple tallies, and are often assigned to multiple block types. They
            //are universally cargo and item based.
            //They also often only target a specific inventory on a block.
            //These booleans will tell us if we're actually going to use a certain composite.
            //Some are 'activated' when certain block types are found on the grid, others only 
            //activate when certain flags on the AutoPopulate command are set(NYI)
            //'cargo' isn't just a list of other tallies; it's also a tally itself. So for it we use
            //the standard system.
            bool useOre = false;
            bool useIngot = false;
            bool useComp = false;
            //We won't know what tallies we'll need to submit information to until we've scanned the
            //grid. So we'll build tally listings for the composites as we go along.
            string cargoValue = "";
            string oreValue = "";
            string ingotValue = "";
            string compValue = "";

            //We'll make a good-faith effort to find existing Tallies before making our own. For
            //that, we'll want a dicitonary.
            Dictionary<string, Tally> tallyDic = new Dictionary<string, Tally>();
            foreach (Tally tally in tallyList)
            { tallyDic.Add(tally.programName, tally); }
            configWriter.Clear();
            //The first step will be to figure out how many of the AP tally types we'll need. We do 
            //this by looking at the blocks that are on the grid. AP Tallies are:
            //Power, Hydrogen, Oxygen, Cargo, Drums, Rockets, Ice, Stone, Ore, Uranium, Solar
            //(Also ShieldHealth, ShieldHeat, and ShieldRegen with Defense Shields)
            //Batteries
            List<IMyTerminalBlock> batteries = new List<IMyTerminalBlock>();
            GTS.GetBlocksOfType<IMyBatteryBlock>(batteries, b => b.IsSameConstructAs(Me));
            //Hydrogen and Oxygen tanks
            List<IMyGasTank> tanks = new List<IMyGasTank>();
            List<IMyTerminalBlock> hyTanks = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> oxyTanks = new List<IMyTerminalBlock>();
            //Code for sorting tanks based on their resource sink courtesy of Digi and Frigidman
            MyDefinitionId oxygenID = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Oxygen");
            MyDefinitionId hydrogenID = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Hydrogen");
            GridTerminalSystem.GetBlocksOfType<IMyGasTank>(tanks, b => b.IsSameConstructAs(Me));
            //Next, we sift our tanks
            foreach (IMyGasTank tank in tanks)
            {
                //Again: Not my code. But basically: 'Does this tank hold oxygen?'
                //Because I'm likely to forget: The double questionmark at the end here is a null
                //coalescing operator. If what is on the left isn't null, it uses that. Otherwise, 
                //it uses what's on the right. And they can be chained together.
                if (tank.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(oxygenID) ?? false)
                { oxyTanks.Add(tank); }
                else if (tank.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(hydrogenID) ?? false)
                { hyTanks.Add(tank); }
            }
            //We can now read the amount of hydrogen stored in a Hydrogen engine. So let's add them 
            //into the mix
            List<IMyTerminalBlock> engines = new List<IMyTerminalBlock>();
            GridTerminalSystem.GetBlocksOfType<IMyPowerProducer>(engines, b => b.IsSameConstructAs(Me)
                && b.BlockDefinition.SubtypeId.EndsWith("HydrogenEngine"));
            //Dump them in with other things that're filled with hydrogen.
            hyTanks.AddList(engines);
            /*
            //For Solar, we'll want two seperate lists: One for large-grid panels, the other for 
            //small-grid. We need these becaues we're actually going to try to compute a reasonable
            //max for this tally.
            //Fortunately, some kind person over at Keen took the time to give Solar Panels their 
            //own interface.
            List<IMyTerminalBlock> smallPanels = new List<IMyTerminalBlock>();
            GTS.GetBlocksOfType<IMySolarPanel>(smallPanels, b => b.IsSameConstructAs(Me)
                && b.BlockDefinition.SubtypeId == "SmallBlockSolarPanel");
            List<IMyTerminalBlock> largePanels = new List<IMyTerminalBlock>();
            GTS.GetBlocksOfType<IMySolarPanel>(largePanels, b => b.IsSameConstructAs(Me)
                && b.BlockDefinition.SubtypeId == "LargeBlockSolarPanel");
            */
            //Since Digi pointed me at the ResourceSourceComponent, I no longer need to hard-code 
            //guesstimates for solar panel max values. That means we can stuff all our solar panels
            //into the same group.
            List<IMyTerminalBlock> panels = new List<IMyTerminalBlock>();
            GTS.GetBlocksOfType<IMySolarPanel>(panels, b => b.IsSameConstructAs(Me));
            //We could also do JumpDrives? Let's do that.
            List<IMyTerminalBlock> jumpDrives = new List<IMyTerminalBlock>();
            GTS.GetBlocksOfType<IMyJumpDrive>(jumpDrives, b => b.IsSameConstructAs(Me));
            //Cargo. We ignore blocks that have the CargoContainer type, but can't be conveyor'd.
            List<IMyTerminalBlock> cargo = new List<IMyTerminalBlock>();
            GTS.GetBlocksOfType<IMyTerminalBlock>(cargo,
                b => b.IsSameConstructAs(Me)
                && (b is IMyShipConnector || b is IMyCargoContainer)
                && b.BlockDefinition.SubtypeId != "LargeBlockLockerRoom"
                && b.BlockDefinition.SubtypeId != "LargeBlockLockerRoomCorner"
                && b.BlockDefinition.SubtypeId != "LargeBlockLockers");
            //Start building our variable configs. Universaly compatabile cargo is only listed on one value.
            if (cargo.Count > 0)
            { cargoValue = $"{cargoValue}Cargo, "; }
            //Gatling-based weapons. This is small and large grid turrets, and the small grid cannon.
            List<IMyTerminalBlock> gatlings = new List<IMyTerminalBlock>();
            GTS.GetBlocksOfType<IMyUserControllableGun>(gatlings, b => b.IsSameConstructAs(Me)
                && (b is IMyLargeGatlingTurret
                || b is IMySmallGatlingGun));
            //Drums can be found in all cargo, and also compnent-compatible inventories.
            if (gatlings.Count > 0)
            {
                cargoValue = $"{cargoValue}Drums, ";
                useComp = true;
                compValue = $"{compValue}Drums, ";
            }
            //Rocket-based weapons. This is small and large grid turrets, small and large grid 
            //reloadable launchers, and the small grid pod
            List<IMyTerminalBlock> rockets = new List<IMyTerminalBlock>();
            GTS.GetBlocksOfType<IMyUserControllableGun>(rockets, b => b.IsSameConstructAs(Me)
                && (b is IMyLargeMissileTurret
                || b is IMySmallMissileLauncher
                || b is IMySmallMissileLauncherReload));
            if (rockets.Count > 0)
            {
                cargoValue = $"{cargoValue}Rockets, ";
                useComp = true;
                compValue = $"{compValue}Rockets, ";
            }
            //H2/02 generators.
            List<IMyTerminalBlock> iceCrackers = new List<IMyTerminalBlock>();
            GTS.GetBlocksOfType<IMyGasGenerator>(iceCrackers, b => b.IsSameConstructAs(Me));
            //Ice can't go in refineries, so even though it's an ore, we'll pretend it isn't.
            if (iceCrackers.Count > 0)
            { cargoValue = $"{cargoValue}Ice, "; }
            //Drills. 
            List<IMyTerminalBlock> drills = new List<IMyTerminalBlock>();
            GTS.GetBlocksOfType<IMyShipDrill>(drills, b => b.IsSameConstructAs(Me));
            //Refineries. 
            List<IMyTerminalBlock> refineries = new List<IMyTerminalBlock>();
            GTS.GetBlocksOfType<IMyRefinery>(refineries, b => b.IsSameConstructAs(Me));
            //If either a drill or a refinery is present, we'll generate tallies for Ore and Stone.
            //And we'll add them to universal cargo and ore.
            if (drills.Count > 0 || refineries.Count > 0)
            {
                cargoValue = $"{cargoValue}Ore, Stone, ";
                useOre = true;
                oreValue = $"{oreValue}Ore, Stone, ";
            }
            //Reactors. 
            List<IMyTerminalBlock> reactors = new List<IMyTerminalBlock>();
            GTS.GetBlocksOfType<IMyReactor>(reactors, b => b.IsSameConstructAs(Me));
            //The type of uranium we're looking for is an ingot, and it could technically, briefly,
            //be in a refinery before a reactor gobbles it up. So we'll set ingot-compatible 
            //inventories to report their uranium.
            if (reactors.Count > 0)
            {
                cargoValue = $"{cargoValue}Uranium, ";
                useIngot = true;
                ingotValue = $"{ingotValue}Uranium, ";
            }
            //Assemblers, including Survival Kits.
            List<IMyTerminalBlock> assemblers = new List<IMyTerminalBlock>();
            GTS.GetBlocksOfType<IMyAssembler>(assemblers, b => b.IsSameConstructAs(Me));

            //Our composite configs should be set at this point. As a last step, we'll trim the 
            //trailing comma and space, checking to make sure the strings aren't empty
            if (!String.IsNullOrEmpty(cargoValue))
            { cargoValue = cargoValue.Remove(cargoValue.Length - 2); }
            if (!String.IsNullOrEmpty(oreValue))
            { oreValue = oreValue.Remove(oreValue.Length - 2); }
            if (!String.IsNullOrEmpty(ingotValue))
            { ingotValue = ingotValue.Remove(ingotValue.Length - 2); }
            if (!String.IsNullOrEmpty(compValue))
            { compValue = compValue.Remove(compValue.Length - 2); }

            //Now that we have all the relevant blocks, we can start writing config.
            if (batteries.Count > 0) //Really, what are the odds of not having a battery?
            {
                //Go ahead and generate a tally, in case we need it.
                tallyName = "Power";
                //We'll also go ahead and add this tally to the report element list.
                reportElements.Add(tallyName);
                newTally = new TallyGeneric(meterMaid, tallyName, new BatteryHandler());
                configureTally(configWriter, tallyDic, tallyName, tallyName, newTally, batteries,
                    ref tallyList, ref tallyCounter, ref replacementCounter, ref blockCounter);
            }
            if (hyTanks.Count > 0)
            {
                tallyName = "Hydrogen";
                reportElements.Add(tallyName);
                newTally = new TallyGeneric(meterMaid, tallyName, new GasHandler());
                configureTally(configWriter, tallyDic, tallyName, tallyName, newTally, hyTanks,
                    ref tallyList, ref tallyCounter, ref replacementCounter, ref blockCounter);
            }
            if (oxyTanks.Count > 0)
            {
                tallyName = "Oxygen";
                reportElements.Add(tallyName);
                newTally = new TallyGeneric(meterMaid, tallyName, new GasHandler());
                configureTally(configWriter, tallyDic, tallyName, tallyName, newTally, oxyTanks,
                    ref tallyList, ref tallyCounter, ref replacementCounter, ref blockCounter);
            }
            /*
            if (smallPanels.Count > 0 || largePanels.Count > 0)
            {
                tallyName = "Solar";
                reportElements.Add(tallyName);
                //We're actually going to try to get an accurate max here.
                //Both the curr and the max are affected by a tally's multiplyer. So for each
                //panel, we're going to be adding a decimal to our total
                max = smallPanels.Count * .04 + largePanels.Count * .16;
                newTally = new TallyGeneric(meterMaid, tallyName, new PowerProducerHandler(), false, 1000);
                newTally.forceMax(max);
                //We'll just dump all the panels into the largePanels list to pass to configureTally. 
                //MONITOR: This shouldn't be a problem, but only because I'm never using the 
                //largePanel list again after this.
                largePanels.AddList(smallPanels);
                configureTally(configWriter, tallyDic, tallyName, tallyName, newTally, largePanels,
                    ref tallyList, ref tallyCounter, ref replacementCounter, ref blockCounter);
            }*/
            //PowerProducers can now calculate their own maximums, we no longer need to dictate them.
            if (panels.Count > 0)
            {
                tallyName = "Solar";
                reportElements.Add(tallyName);
                newTally = new TallyGeneric(meterMaid, tallyName, new PowerProducerHandler(), false, 1000);
                configureTally(configWriter, tallyDic, tallyName, tallyName, newTally, panels,
                    ref tallyList, ref tallyCounter, ref replacementCounter, ref blockCounter);
            }
            if (jumpDrives.Count > 0)
            {
                tallyName = "JumpCharge";
                reportElements.Add(tallyName);
                newTally = new TallyGeneric(meterMaid, tallyName, new JumpDriveHandler());
                configureTally(configWriter, tallyDic, tallyName, tallyName, newTally, jumpDrives,
                    ref tallyList, ref tallyCounter, ref replacementCounter, ref blockCounter);
            }
            if (gatlings.Count > 0)
            {
                tallyName = "Drums";
                reportElements.Add(tallyName);
                max = gatlings.Count * 10;
                newTally = new TallyItem(meterMaid, tallyName, AMMO_TYPE, "NATO_25x184mm", max);
                //Gatling guns get just the 'drums' reference. We've already added them to cargo 
                //blocks, and we will shortly add them to assemblers.
                configureTally(configWriter, tallyDic, tallyName, tallyName, newTally, gatlings,
                    ref tallyList, ref tallyCounter, ref replacementCounter, ref blockCounter);
            }
            if (rockets.Count > 0)
            {
                tallyName = "Rockets";
                reportElements.Add(tallyName);
                max = rockets.Count * 10;
                newTally = new TallyItem(meterMaid, tallyName, AMMO_TYPE, "Missile200mm", max);
                configureTally(configWriter, tallyDic, tallyName, tallyName, newTally, rockets,
                    ref tallyList, ref tallyCounter, ref replacementCounter, ref blockCounter);
            }
            if (iceCrackers.Count > 0)
            {
                tallyName = "Ice";
                reportElements.Add(tallyName);
                max = iceCrackers.Count * 1000;
                newTally = new TallyItem(meterMaid, tallyName, ORE_TYPE, "Ice", max);
                configureTally(configWriter, tallyDic, tallyName, tallyName, newTally, iceCrackers,
                    ref tallyList, ref tallyCounter, ref replacementCounter, ref blockCounter);
            }
            //We set up for the Ore and Stone tallies at the same time, because they use a lot of
            //the same information.
            if (refineries.Count > 0 || drills.Count > 0)
            {
                tallyName = "Ore";
                reportElements.Add(tallyName);
                newTally = new TallyCargo(meterMaid, tallyName);
                //We'll do our config of subject blocks with the 'stone' portion of these two tallies
                configureTally(configWriter, tallyDic, tallyName, "", newTally, null,
                    ref tallyList, ref tallyCounter, ref replacementCounter, ref blockCounter);
                tallyName = "Stone";
                reportElements.Add(tallyName);
                max = (refineries.Count + drills.Count) * 1000;
                newTally = new TallyItem(meterMaid, tallyName, ORE_TYPE, "Stone", max, true);
                //We only pass this method the Drills list, because we'll get the refineries later.
                configureTally(configWriter, tallyDic, tallyName, oreValue, newTally, drills,
                    ref tallyList, ref tallyCounter, ref replacementCounter, ref blockCounter);
            }
            if (reactors.Count > 0)
            {
                tallyName = "Uranium";
                reportElements.Add(tallyName);
                max = reactors.Count * 50;
                newTally = new TallyItem(meterMaid, tallyName, INGOT_TYPE, "Uranium", max);
                configureTally(configWriter, tallyDic, tallyName, tallyName, newTally, reactors,
                    ref tallyList, ref tallyCounter, ref replacementCounter, ref blockCounter);
            }
            //Because cargo is a tally in addition to being a composite, this bit is pretty normal.
            if (cargo.Count > 0)
            {
                tallyName = "Cargo";
                reportElements.Add(tallyName);
                newTally = new TallyCargo(meterMaid, tallyName);
                //Cargo containers will use the cargoValue string we've assmebled as their value,
                //not the tallyName.
                configureTally(configWriter, tallyDic, tallyName, cargoValue, newTally, cargo,
                    ref tallyList, ref tallyCounter, ref replacementCounter, ref blockCounter);
            }
            //configureTally isn't suitable for setting up the specialized inventories, so we'll
            //just do the work here in this method.
            string refineryConfig = "";
            string assemblerConfig = "";
            //Ore is only used by inventory 0 on refineries.
            if (useOre)
            { refineryConfig = $"{refineryConfig}\nInv0Tallies = {oreValue}"; }
            //Ingots can be found both in the second refinery inventory and the first assembler inventory.
            if (useIngot)
            {
                refineryConfig = $"{refineryConfig}\nInv1Tallies = {ingotValue}";
                assemblerConfig = $"{assemblerConfig}\nInv0Tallies = {ingotValue}";
            }
            //Components are in the second inventory of Assemblers.
            if (useComp)
            { assemblerConfig = $"{assemblerConfig}\nInv1Tallies = {compValue}"; }
            //If we actually ended up with some config, we still need to attach a header
            if (refineryConfig != "")
            { refineryConfig = $"[{tag}]{refineryConfig}"; }
            if (assemblerConfig != "")
            { assemblerConfig = $"[{tag}]{assemblerConfig}"; }

            //Write the config we've generated to all our refineries
            foreach (IMyTerminalBlock block in refineries)
            {
                if (MyIni.HasSection(block.CustomData, tag))
                {
                    configWriter.TryParse(block.CustomData);
                    configWriter.DeleteSection(tag);
                    block.CustomData = configWriter.ToString();
                    replacementCounter++;
                }
                block.CustomData = refineryConfig;
                blockCounter++;
            }
            //Config for Assemblers
            foreach (IMyTerminalBlock block in assemblers)
            {
                if (MyIni.HasSection(block.CustomData, tag))
                {
                    configWriter.TryParse(block.CustomData);
                    configWriter.DeleteSection(tag);
                    block.CustomData = configWriter.ToString();
                    replacementCounter++;
                }
                block.CustomData = assemblerConfig;
                blockCounter++;
            }

            //The last step is to generate a generic report that will be displayed on the first 
            //surface of the PB itself.
            //We'll start by removing config that's already on the PB.
            if (MyIni.HasSection(Me.CustomData, tag))
            {
                configWriter.TryParse(Me.CustomData);
                configWriter.DeleteSection(tag);
                Me.CustomData = configWriter.ToString();
            }
            //A string that will hold the tallies we'll be displaying on this report.
            string reportConfig = "";
            //Figure out how many columns we'll need. We won't have more than 3 columns.
            const int MAX_COLUMNS = 3;
            int reportColumns = Math.Min(reportElements.Count, MAX_COLUMNS);
            int columnPos = MAX_COLUMNS;
            foreach (string element in reportElements)
            {
                //When we reach the end of the 'line', add a newLine followed by a new multiline 
                //symbol
                if (columnPos == 3)
                {
                    reportConfig = $"{reportConfig}\n|";
                    columnPos = 0;
                }
                reportConfig = $"{reportConfig}{element}, ";
                columnPos++;
            }
            //Trim the trailing comma and space from our reportConfig string, checking to make sure
            //that the string isn't empty. Just in case the user has somehow installed this on a 
            //functioning grid with no cargo or batteries.
            if (!String.IsNullOrEmpty(reportConfig))
            { reportConfig = reportConfig.Remove(reportConfig.Length - 2); }
            Me.CustomData =
                $"[{tag}]\n" +
                $"Surface0Elements = {reportConfig}\n" +
                $"Surface0Columns = {reportColumns}\n" +
                $"Surface0BackColor = 0,0,0";
            //We're done here. Let's talk about what happened.
            return $"Carried out AutoPopulate command, adding {tallyCounter} tallies and " +
                $"configuration on {blockCounter} blocks" +
                $"{(replacementCounter != 0 ? $" (With {replacementCounter} blocks having existing configuration replaced)" : "")}.";
        }

        public void configureTally(MyIni configWriter, Dictionary<string, Tally> tallyDic,
            string tallyName, string tallyValue, Tally newTally, List<IMyTerminalBlock> blocks,
            ref List<Tally> tallyList, ref int tallyCounter, ref int replacementCounter,
            ref int blockCounter, string tallyKey = "Tallies")
        {
            //Check to see if this tally already exists before we make our own.
            if (newTally != null && !tallyDic.ContainsKey(tallyName))
            {
                tallyList.Add(newTally);
                tallyCounter++;
            }
            if (blocks != null)
            {
                foreach (IMyTerminalBlock block in blocks)
                {
                    if (MyIni.HasSection(block.CustomData, tag))
                    {
                        configWriter.TryParse(block.CustomData);
                        configWriter.DeleteSection(tag);
                        block.CustomData = configWriter.ToString();
                        replacementCounter++;
                    }
                    block.CustomData = $"{block.CustomData}\n\n[{tag}]\n{tallyKey} = {tallyValue}";
                    blockCounter++;
                }
            }
        }

        public bool initiate()
        {
            //Initiate some bits and pieces, though most of the work will be done in evaluate()
            iniReadWrite = new MyIni();
            iniRead = new MyIni();
            argReader = new MyCommandLine();
            _sb = new StringBuilder();
            iniRaw = new RawTextIni(_sb);
            bool firstRun = false;
            //One of the first things we need to do is figure out if this code version has changed, 
            //or if script has a custom tag. To do that, we check the Storage string.
            iniReadWrite.TryParse(Storage);
            //Get the version number of the code last used on this PB, using a -1 if we can't find
            //an entry.
            double lastVersion = iniReadWrite.Get("Config", "Version").ToDouble(-1);
            //Try to pull the ID from the Config section of the Storage string, using the default 
            //ID if nothing is found.
            customID = iniReadWrite.Get("Config", "ID").ToString(DEFAULT_ID);
            //Build the tag by combining the constant PREFIX and the user-modifiable ID
            tag = $"{PREFIX}.{customID}";
            //Now that we have the tag, we can start instansiating the stuff that needs it.
            listener = IGC.RegisterBroadcastListener(tag);
            listener.SetMessageCallback(tag);
            //The log that will give us feedback in the PB's Detail Info area
            log = new Hammers.EventLog(_sb, $"ShipWare v{VERSION} - Recent Events", true);
            //The meterMaid that will generate ASCII meters for our tallies
            meterMaid = new Hammers.MeterMaid(_sb);
            //If we have a custom tag, we want to have that information front and center in the log
            if (tag != $"{PREFIX}.{DEFAULT_ID}")
            { log.scriptTag = tag; }
            //The distributer that handles updateDelays
            distributor = new UpdateDistributor(log);
            /*
            //Try to initialize the ShieldBroker, which manages the actual ShieldAPI object used by
            //ShieldTally objects.
            shieldBroker = new ShieldBroker(Me);
            */
            //Clear the MyIni we used in this method.
            iniReadWrite.Clear();
            //Last step is to make some decisions based on the version number.
            if (lastVersion == -1)
            {
                firstRun = true;
                log.add("First initialization complete. Use the AutoPopulate command to generate " +
                    "some basic configuration, or use Template and Populate to make your own. The " +
                    "Evaluate command can be used to scan the grid for configuration and load it " +
                    "into memory.");
            }
            else if (lastVersion != VERSION)
            { log.add($"Code updated from v{lastVersion} to v{VERSION}."); }
            if (!firstRun)
            { log.add("Script initialization complete."); }
            return firstRun;
        }

        public Dictionary<string, Action<IMyTerminalBlock>> compileActions()
        {
            Dictionary<string, Action<IMyTerminalBlock>> actions = new Dictionary<string, Action<IMyTerminalBlock>>();
            //The actions that can be performed by this script, in no particular order:
            //Functional Blocks
            actions.Add("EnableOn", b => ((IMyFunctionalBlock)b).Enabled = true);
            actions.Add("EnableOff", b => ((IMyFunctionalBlock)b).Enabled = false);
            //Battery Blocks
            actions.Add("BatteryAuto", b => ((IMyBatteryBlock)b).ChargeMode = ChargeMode.Auto);
            actions.Add("BatteryRecharge", b => ((IMyBatteryBlock)b).ChargeMode = ChargeMode.Recharge);
            actions.Add("BatteryDischarge", b => ((IMyBatteryBlock)b).ChargeMode = ChargeMode.Discharge);
            //Doors
            actions.Add("DoorOpen", b => ((IMyDoor)b).OpenDoor());
            actions.Add("DoorClose", b => ((IMyDoor)b).CloseDoor());
            //Vents
            actions.Add("VentPressurize", b => ((IMyAirVent)b).Depressurize = false);
            actions.Add("VentDepressurize", b => ((IMyAirVent)b).Depressurize = true);
            //GasTanks
            actions.Add("TankStockpileOn", b => ((IMyGasTank)b).Stockpile = true);
            actions.Add("TankStockpileOff", b => ((IMyGasTank)b).Stockpile = false);
            //Sorters
            actions.Add("SorterDrainOn", b => ((IMyConveyorSorter)b).DrainAll = true);
            actions.Add("SorterDrainOff", b => ((IMyConveyorSorter)b).DrainAll = false);
            //Timers
            actions.Add("TimerTrigger", b => ((IMyTimerBlock)b).Trigger());
            actions.Add("TimerStart", b => ((IMyTimerBlock)b).StartCountdown());
            actions.Add("TimerStop", b => ((IMyTimerBlock)b).StopCountdown());
            //LandingGear
            actions.Add("GearAutoLockOn", b => ((IMyLandingGear)b).AutoLock = true);
            actions.Add("GearAutoLockOff", b => ((IMyLandingGear)b).AutoLock = false);
            actions.Add("GearLock", b => ((IMyLandingGear)b).Lock());
            actions.Add("GearUnlock", b => ((IMyLandingGear)b).Unlock());
            //Parachutes
            actions.Add("ParachuteOpen", b => ((IMyParachute)b).OpenDoor());
            actions.Add("ParachuteClose", b => ((IMyParachute)b).CloseDoor());
            actions.Add("ParachuteAutoDeployOn", b => ((IMyParachute)b).AutoDeploy = true);
            actions.Add("ParachuteAutoDeployOff", b => ((IMyParachute)b).AutoDeploy = false);
            //Pistons
            actions.Add("PistonExtend", b => ((IMyPistonBase)b).Extend());
            actions.Add("PistonRetract", b => ((IMyPistonBase)b).Retract());
            //Rotors
            actions.Add("RotorLock", b => ((IMyMotorAdvancedStator)b).RotorLock = true);
            actions.Add("RotorUnlock", b => ((IMyMotorAdvancedStator)b).RotorLock = false);
            actions.Add("RotorReverse", b => ((IMyMotorAdvancedStator)b).TargetVelocityRPM =
                ((IMyMotorAdvancedStator)b).TargetVelocityRPM * -1);
            actions.Add("RotorPositive", b => ((IMyMotorAdvancedStator)b).TargetVelocityRPM =
                Math.Abs(((IMyMotorAdvancedStator)b).TargetVelocityRPM));
            actions.Add("RotorNegative", b => ((IMyMotorAdvancedStator)b).TargetVelocityRPM =
                Math.Abs(((IMyMotorAdvancedStator)b).TargetVelocityRPM) * -1);
            //Gyros
            //Gyro overides are set in RPM, but we can't say for sure what the max RPM of a given 
            //block may be. So instead, we use arbitrarily high numbers and let the block sort it out.
            actions.Add("GyroYawPositive", b =>
            {
                IMyGyro gyro = (IMyGyro)b;
                gyro.GyroOverride = true;
                gyro.Yaw = 9000;
            });
            actions.Add("GyroYawNegative", b =>
            {
                IMyGyro gyro = (IMyGyro)b;
                gyro.GyroOverride = true;
                gyro.Yaw = -9000;
            });
            actions.Add("GyroPitchPositive", b =>
            {
                IMyGyro gyro = (IMyGyro)b;
                gyro.GyroOverride = true;
                gyro.Pitch = 9000;
            });
            actions.Add("GyroPitchNegative", b =>
            {
                IMyGyro gyro = (IMyGyro)b;
                gyro.GyroOverride = true;
                gyro.Pitch = -9000;
            });
            actions.Add("GyroRollPositive", b =>
            {
                IMyGyro gyro = (IMyGyro)b;
                gyro.GyroOverride = true;
                gyro.Roll = 9000;
            });
            actions.Add("GyroRollNegative", b =>
            {
                IMyGyro gyro = (IMyGyro)b;
                gyro.GyroOverride = true;
                gyro.Roll = -9000;
            });
            actions.Add("GyroOverrideOn", b => ((IMyGyro)b).GyroOverride = true);
            actions.Add("GyroOverrideOff", b =>
            {
                IMyGyro gyro = (IMyGyro)b;
                gyro.Yaw = 0;
                gyro.Pitch = 0;
                gyro.Roll = 0;
                gyro.GyroOverride = false;
            });
            //Connectors
            actions.Add("ConnectorLock", b => ((IMyShipConnector)b).Connect());
            actions.Add("ConnectorUnlock", b => ((IMyShipConnector)b).Disconnect());
            //Wheels
            actions.Add("SuspensionHeightPositive", b => ((IMyMotorSuspension)b).Height = 9000);
            actions.Add("SuspensionHeightNegative", b => ((IMyMotorSuspension)b).Height = -9000);
            actions.Add("SuspensionHeightZero", b => ((IMyMotorSuspension)b).Height = 0);
            actions.Add("SuspensionPropulsionPositive", b => ((IMyMotorSuspension)b).PropulsionOverride = 1);
            actions.Add("SuspensionPropulsionNegative", b => ((IMyMotorSuspension)b).PropulsionOverride = -1);
            actions.Add("SuspensionPropulsionZero", b => ((IMyMotorSuspension)b).PropulsionOverride = 0);
            //Thrusters
            actions.Add("ThrusterOverrideMax", b => ((IMyThrust)b).ThrustOverridePercentage = 1);
            actions.Add("ThrusterOverrideOff", b => ((IMyThrust)b).ThrustOverridePercentage = 0);
            //Warheads
            actions.Add("WarheadArm", b => ((IMyWarhead)b).IsArmed = true);
            actions.Add("WarheadDisarm", b => ((IMyWarhead)b).IsArmed = false);
            actions.Add("WarheadCountdownStart", b => ((IMyWarhead)b).StartCountdown());
            actions.Add("WarheadCountdownStop", b => ((IMyWarhead)b).StopCountdown());
            actions.Add("WarheadDetonate", b => ((IMyWarhead)b).Detonate());
            //Gatling Turrets
            //Character Large Meteor Missile Neutral Small Stations
            actions.Add("GatlingDefensive", b =>
            {
                //When defensive, Gatling Turrets target only missiles.
                //We've got a lot to do with this block. Let's just have a local and make one cast.
                IMyLargeGatlingTurret turret = (IMyLargeGatlingTurret)b;
                turret.TargetStations = false;
                turret.TargetLargeGrids = false;
                turret.TargetSmallGrids = false;
                turret.TargetMissiles = true;
                /*
                turret.SetValueBool("TargetLargeShips", false);
                turret.SetValueBool("TargetMissiles", true);
                turret.SetValueBool("TargetSmallShips", false);
                turret.SetValueBool("TargetStations", false);
                */
            });
            actions.Add("GatlingOffensive", b =>
            {
                //When offensive, Gatling Turrets target large ships, small ships, and stations. 
                //They ignore missiles.
                IMyLargeGatlingTurret turret = (IMyLargeGatlingTurret)b;
                turret.TargetStations = true;
                turret.TargetLargeGrids = true;
                turret.TargetSmallGrids = true;
                turret.TargetMissiles = false;
                /*
                turret.SetValueBool("TargetLargeShips", true);
                turret.SetValueBool("TargetMissiles", false);
                turret.SetValueBool("TargetSmallShips", true);
                turret.SetValueBool("TargetStations", true);
                */
            });
            actions.Add("SwatterDefensive", b =>
            {
                //When defensive, swatters target missiles and meteors. They ignore characters and
                //small ships.
                IMyLargeInteriorTurret turret = (IMyLargeInteriorTurret)b;
                turret.TargetSmallGrids = false;
                turret.TargetCharacters = false;
                /*
                turret.SetValueBool("TargetSmallShips", false);
                turret.SetValueBool("TargetCharacters", false);
                */
            });
            actions.Add("SwatterOffensive", b =>
            {
                //When offensive, swatters target missiles, meteors, characters and small ships.
                IMyLargeInteriorTurret turret = (IMyLargeInteriorTurret)b;
                turret.TargetSmallGrids = true;
                turret.TargetCharacters = true;
                /*
                turret.SetValueBool("TargetSmallShips", true);
                turret.SetValueBool("TargetCharacters", true);
                */
            });
            //Wheels?
            //Thrusters?
            //MergeBlock?
            //Warhead?
            return actions;
        }

        public void evaluate()
        {
            //We'll need the ability to move data around during evaluation. A list will suffice for
            //reports, but we'll need a dictionary to make the tallies, containers, and indicators 
            //work.
            Dictionary<IMyInventory, List<TallyCargo>> evalContainers = new Dictionary<IMyInventory, List<TallyCargo>>();
            Dictionary<string, Tally> evalTallies = new Dictionary<string, Tally>();
            List<IReportable> evalReports = new List<IReportable>();
            Dictionary<string, Indicator> evalIndicators = new Dictionary<string, Indicator>();
            //MFDs, ActionSets, and Raycasters are special, though. We'll leave them in a dictionary.
            MFDs = new Dictionary<string, MFD>();
            sets = new Dictionary<string, ActionSet>();
            raycasters = new Dictionary<string, Raycaster>();
            //We'll also need a dictionary of all possible actions
            Dictionary<string, Action<IMyTerminalBlock>> actions = compileActions();
            //We'll need to pass the GTS around a bit for this. May as well put an easy handle on it.
            IMyGridTerminalSystem GTS = GridTerminalSystem;
            //A couple of extra variables for working directly with MyIni
            MyIniValue iniValue = new MyIniValue();
            MyIniParseResult parseResult = new MyIniParseResult();
            //We'll need to do some configuration on tallies before we send them on their way. Let's
            //use an easy handle for it.
            Tally tally = null;
            //Sometimes, a more specialized handle for a tally is handy. Let's have a round of those.
            TallyCargo tallyCargo;
            TallyGeneric tallyGeneric;
            //ActionSets, too
            ActionSet set = null;
            //On the other hand, sometimes you need something a little bit generic.
            IReportable reportable;
            //Some blocks do multiple jobs, which means a block has to be subjected to multiple 
            //different sorters. This variable will tell us if at least one of those sorters knew 
            //how to handle the block.
            bool handled = false;
            //We'll need a log to store errors.
            //string errors = "";
            LimitedErrorLog errors = new LimitedErrorLog(_sb, 15);
            //We'll use these strings to store the information we need to build a tally.
            string initTag = $"{tag}Init";
            string elementName = "";
            string MFDName = "";
            string addIn1 = "";
            //For things like ActionSets and MFDs, we use a discreet section in the INI for 
            //configuration. We'll store the name for these sections, which is the PREFIX followed
            //by the name of the object, in this string.
            string discreetTag = "";
            //Sometimes, we want a little color.
            Color color = Hammers.cozy;
            //The tallies a block reports to are stored in a delimited string. We'll need something
            //to hold those as something easier to work with.
            string[] elementNames;
            //The ubiquitous list of terminal blocks.
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

            //We'll go ahead and get a parse from the Storage string. 
            iniRead.TryParse(Storage);

            //Our first step will be to check the programmable block for tally configs.
            //From the PB, we read:
            //Tally<#>Name: The name that will be associated with this tally.
            //Tally<#>Type: The type of this tally. Acceptable tally types are:
            //  Inventory, Item, Battery, Gas, JumpDrive, Raycast, PowerProducer (Solar/Wind),
            //  HydrogenWithEngines? 
            //  (With Defense Shields)
            //  ShieldIntegrity? ShieldHeat? ShieldTimeTilRestore?
            //Tally<#>ItemTypeID: For TallyItems, the ID that will be fed into MyItemType
            //Tally<#>ItemSubTypeID: For TallyItems, the sub type ID that will be fed into 
            //  MyItemType
            //Tally<#>Max: A user-definable value that will be used in place of the evaluate-calculated
            //  Max. Required for some tallies, like TallyItems
            //Tally<#>DisplayName: A name that will be shown on screens instead of the tally name
            //Tally<#>Multiplier: (Default = 1) The multiplier that will be applied to this tally. 
            //Tally<#>LowGood: (Default = true for volume, false otherwise) Will this report be 
            //  color-coded using the assumption that low numbers are a good thing?

            //Parse the PB's custom data. If it doesn't return something useable...
            if (!iniReadWrite.TryParse(Me.CustomData, out parseResult))
            //...file a complaint.
            {
                errors.add($"The parser was unable to read information from the Programmable Block. " +
                      $"Reason: {parseResult.Error}");
            }
            //The counter for this loop.
            int counter = 0;
            //As long as the counter isn't -1 (Which indicates that we've run out of tallies)...
            while (counter != -1)
            {
                tally = tryGetTallyFromConfig(iniReadWrite, initTag, counter, iniValue, errors,
                    out elementName);
                if (tally == null)
                //If we didn't find another tally, set the counter equal to -1 to indicate that 
                //we're done in this loop.
                { counter = -1; }
                //First thing's first: There's exactly one tally name we've reserved. Is the user
                //trying to use it?
                else if (elementName.ToLowerInvariant() == "blank")
                //Complain. Righteously.
                {
                    errors.add($"The Tally name '{elementName}' is reserved by the script to indicate" +
                          $"where portions of the screen should be left empty. Please choose a " +
                          $"different name.");
                    //There's no way to recover from this. Stop evaluation until the user gets their
                    //act together.
                    break;
                }
                //We should probably also take this opportunity to make sure this tally name isn't 
                //already in use.
                else if (evalTallies.ContainsKey(elementName))
                {
                    errors.add($"The Tally name '{elementName}' is already in use. Tallies and " +
                        $"ActionSets must have their own, unique names.");
                    break;
                }
                else
                {
                    //That's all the data we can glean from here. It's time to put this tally
                    //somewhere the rest of Evaluate can get to it.
                    evalTallies.Add(elementName, tally);
                    //Last step is to increment the counter, so we can look for the next tally.
                    counter++;
                }
            }

            //ActionSets also get their initial configuration on the PB. 
            counter = 0;
            //As long as the counter isn't -1 (Which indicates that we've run out of ActionSets)...
            while (counter != -1)
            {

                set = tryGetActionFromConfig(iniReadWrite, iniRead, initTag, counter, iniValue,
                    errors, out elementName);
                if (set == null)
                //Again, a value of -1 indicates that we can't find another victim.
                { counter = -1; }
                //Blank still belongs to us.
                else if (elementName.ToLowerInvariant() == "blank")
                {
                    errors.add($"The Action name '{elementName}' is reserved by the script to indicate" +
                          $"where portions of the screen should be left empty. Please choose a " +
                          $"different name.");
                    break;
                }
                //Make sure this Set name isn't in use.
                else if (evalTallies.ContainsKey(elementName) || sets.ContainsKey(elementName))
                {
                    errors.add($"The ActionSet name '{elementName}' is already in use. Tallies and " +
                        $"ActionSets must have their own, unique names, and ActionSets cannot have " +
                        $"the same name as a Tally.");
                    break;
                }
                else
                {
                    //This ActionSet should be ready. Pass it to the list.
                    sets.Add(elementName, set);
                    //On to the next Set! Maybe.
                    counter++;
                }
            }

            //On to Triggers
            List<Trigger> evalTriggers = new List<Trigger>();
            Trigger trigger = null;
            counter = 0;
            //As long as the counter isn't -1 (Which indicates that we've run out of Triggers)...
            while (counter != -1)
            {
                //Try to pull a trigger from the config at the specified index
                tryGetTriggerFromConfig(iniReadWrite, evalTallies, sets, initTag, counter, iniValue,
                    ref tally, ref set, ref trigger, errors);
                //If we found no trigger, end the loop
                if (trigger == null)
                { counter = -1; }
                //If we found a trigger, look for the next one.
                else
                {
                    evalTriggers.Add(trigger);
                    counter++;
                }
            }

            //If we don't have errors, but we also don't have any tallies or ActionSets...
            if (errors.getErrorTotal() == 0 && evalTallies.Count == 0 && sets.Count == 0)
            { errors.add($"No readable configuration found on the programmable block."); }

            //Only if there were no errors with parsing the PB...
            if (errors.getErrorTotal() == 0)
            {
                //...should we get the blocks on the grid with our section tag. But first, we'll
                //see if we need to set up any raycasters
                Hammers.findBlocks<IMyTerminalBlock>(GTS, blocks, b => (b.IsSameConstructAs(Me)
                    && b is IMyCameraBlock && MyIni.HasSection(b.CustomData, tag)));
                //From cameras, we read:
                //RaycasterName: The name that will be associated with this raycaster, in reports
                //  and run commands
                //RaycasterDisplayName: The name that will be shown on the Tally associated with 
                //  this Raycaster. Has no effect on the Raycaster object itself.
                //RaycasterBaseRange (Default: 1000): The distance of the first scan that will be 
                //  performed by this raycaster
                //RaycasterMultiplier (Default: 3): The multipler that will be applied to each 
                //  successive scan's distance
                //RaycasterMaxRange (Default: 27000): The maximum range to be scanned by this 
                //  raycaster. The last scan performed will always be at this distance.
                foreach (IMyTerminalBlock block in blocks)
                {
                    //Try to parse the custom data
                    if (iniReadWrite.TryParse(block.CustomData, out parseResult))
                    {
                        //Does this block have configuration for a raycaster?
                        if (iniReadWrite.ContainsKey(tag, "RaycasterName"))
                        {
                            elementName = iniReadWrite.Get(tag, "RaycasterName").ToString();
                            //Is the user trying to give this Raycaster the same name as a tally?
                            if (!evalTallies.ContainsKey(elementName))
                            {
                                //Since we'll need to do some math to figure out what the maximum 
                                //value of the Tally is going to be, we'll use a set of variables 
                                //to store information.
                                double baseRange = 1000;
                                double multiplier = 3;
                                double maxRange = 27000;
                                //Create a new Raycaster object referencing this Camera block, 
                                //referencing the global Stringbuilder and using our default values
                                Raycaster raycaster = new Raycaster(_sb, (IMyCameraBlock)block,
                                    baseRange, multiplier, maxRange);
                                //Also create a new tally that will report the charge of this Raycaster
                                tallyGeneric = new TallyGeneric(meterMaid, elementName, new RaycastHandler());
                                //We generate this tally automatically when we build a Raycaster. 
                                //That means we don't need config for it in the Init section.
                                tallyGeneric.doNotReconstitute = true;
                                //... And add the camera to it. Won't do much if we don't do that!
                                tallyGeneric.tryAddBlock(block);
                                //Get the optional configuration information for this Raycaster and
                                //its assocaiated tally. 
                                //DisplayName (Affects Tally only)
                                iniValue = iniReadWrite.Get(tag, $"RaycasterDisplayName");
                                if (!iniValue.IsEmpty)
                                { tallyGeneric.displayName = iniValue.ToString(); }
                                //BaseRange
                                iniValue = iniReadWrite.Get(tag, $"RaycasterBaseRange");
                                if (!iniValue.IsEmpty)
                                {
                                    baseRange = iniValue.ToDouble(baseRange);
                                    raycaster.baseRange = baseRange;
                                }
                                //Multiplier
                                iniValue = iniReadWrite.Get(tag, $"RaycasterMultiplier");
                                if (!iniValue.IsEmpty)
                                {
                                    multiplier = iniValue.ToDouble(multiplier);
                                    raycaster.multiplier = multiplier;
                                }
                                //MaxRange
                                iniValue = iniReadWrite.Get(tag, $"RaycasterMaxRange");
                                if (!iniValue.IsEmpty)
                                {
                                    maxRange = iniValue.ToDouble(maxRange);
                                    raycaster.maxRange = maxRange;
                                }
                                //We need to give the tally a maximum value. The number we'll use 
                                //for that is the maximum ammount of charge that the Raycaster can
                                //consume in one scan. Unfortunately, the process of determining 
                                //that value is somewhat involved... 
                                //Start with the base range
                                double scanRange = baseRange;
                                double tallyMax = baseRange;
                                //Add the charge consumed by each successive scan
                                while (scanRange < maxRange)
                                {
                                    //Get the range of the next scan by multiplying the previous
                                    //scan by the multiplyer
                                    scanRange *= multiplier;
                                    //If this new scan range exceeds the max range...
                                    if (scanRange > maxRange)
                                    //...add the maxRange to our running tally instead of the scan
                                    //range.
                                    { tallyMax += maxRange; }
                                    //Otherwise, just add the scan range to our running tally.
                                    else
                                    { tallyMax += scanRange; }
                                }
                                //Use the calculated maximum charge for a single scan as this tally's
                                //max.
                                tallyGeneric.forceMax(tallyMax);
                                //Now that our Raycaster and Tally are ready, we'll add them to their
                                //respective data structures.
                                raycasters.Add(elementName, raycaster);
                                evalTallies.Add(elementName, tallyGeneric);
                                //Set EnableRaycast on the physical block so we can actually, y'know,
                                //raycast?
                                ((IMyCameraBlock)block).EnableRaycast = true;
                            }
                            //If the user is trying to give this raycaster the same name as a tally,
                            //complain. In the most understanding way possible.
                            else
                            {
                                errors.add($"The Raycaster name '{elementName}' is already in use " +
                                    $"by a Tally. Raycasters generate their own tallies, you do not " +
                                    $"need to create tallies for them.");
                                break;
                            }
                        }
                        //If there is no configuration for a raycaster, fail silently. There's other
                        //things the user might want to do with a camera.
                    }
                    //If we can't parse the custom data, fail silently. Let the grid sorter take
                    //care of assigning blame.
                }

                GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(blocks, b =>
                    (b.IsSameConstructAs(Me) && MyIni.HasSection(b.CustomData, tag)));
                //NOTE: This will never throw an error. Back when it used to exclude Me from the
                //  list it could have, but now, in order to reach this point, you must have config
                //  data on the PB.
                if (blocks.Count <= 0)
                { errors.add($"No blocks found on this construct with a [{tag}] INI section."); }
            }

            //Every block we've found has some sort of configuration information for this script.
            //And we're going to read all of it.
            foreach (IMyTerminalBlock block in blocks)
            {
                //Whatever kind of block this is, we're going to need to see what's in its 
                //CustomData. If that isn't useable...
                if (!iniReadWrite.TryParse(block.CustomData, out parseResult))
                //...complain.
                {
                    errors.add($"The parser was unable to read information from block " +
                          $"'{block.CustomName}'. Reason: {parseResult.Error}");
                }
                //My comedic, reference-based genius shall be preserved here for all eternity. Even
                //if it is now largely irrelevant to how ShipManager operates.
                //In the CargoManager, the data is handled by two seperate yet equally important
                //objects: the Tallies that store and calculate information and the Reports that 
                //display it. These are their stories.

                //There's a couple of keys that are present on multiple block types. We'll check for
                //those first.
                //If our block has a 'Tallies' key...
                if (parseResult.Success && iniReadWrite.ContainsKey(tag, "Tallies"))
                {
                    //This is grounds for declaring this block to be handled.
                    handled = true;
                    //Get the 'Tallies' data
                    iniValue = iniReadWrite.Get(tag, "Tallies");
                    //Split the Tallies string into individual tally names
                    elementNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                    foreach (string name in elementNames)
                    {
                        //If this tally name is in evalTallies...
                        if (evalTallies.ContainsKey(name))
                        {
                            //...pull the tally out.
                            tally = evalTallies[name];
                            //Next step is to try and figure out what kind of tally this is
                            if (tally is TallyCargo)
                            {
                                //Tally Cargos require an inventory. It's kinda their thing.
                                if (block.HasInventory)
                                {
                                    tallyCargo = (TallyCargo)tally;
                                    //For configurations tied to the 'Tallies' key, we use the same set of 
                                    //Tallies for every inventory on the block.
                                    for (int i = 0; i < block.InventoryCount; i++)
                                    //There may be additional tallies in this list that will use this
                                    //same inventory, or tallies in Inv0Tallies, etc. For now, we'll
                                    //simply add it to our dictionary and process it later.
                                    {
                                        IMyInventory inventory = block.GetInventory(i);
                                        //If we don't already have a dictionary entry for this 
                                        //inventory...
                                        if (!evalContainers.ContainsKey(inventory))
                                        //...create one
                                        { evalContainers.Add(inventory, new List<TallyCargo>()); }
                                        //Now that we're sure there's a place to put it, add this
                                        //tally to this inventory's entry.
                                        evalContainers[inventory].Add(tallyCargo);
                                    }
                                }
                                //If the block doesn't have an inventory, complain.
                                else
                                {
                                    errors.add($"Block '{block.CustomName}' does not have an " +
                                        $"inventory and is not compatible with the TallyType of " +
                                        $"tally '{name}'.");
                                }
                            }
                            else if (tally is TallyGeneric)
                            {
                                tallyGeneric = (TallyGeneric)tally;

                                if (!tallyGeneric.tryAddBlock(block))
                                {
                                    errors.add($"Block '{block.CustomName}' is not a {tallyGeneric.getTypeAsString()} " +
                                            $"and is not compatible with the TallyType of tally '{name}'.");
                                }
                            }
                            else
                            //If a tally isn't a TallyCargo or a TallyGeneric or a TallyShield, I done goofed.
                            {
                                errors.add($"Block '{block.CustomName}' refrenced the tally '{name}'," +
                                    $"which is neither a TallyCargo or a TallyGeneric. Complain to the " +
                                    $"script writer, this should be impossible.");
                            }
                        }
                        //If we can't find this name in evalTallies, complain.
                        else
                        {
                            errors.add($"Block '{block.CustomName}' tried to reference the " +
                                $"unconfigured tally '{name}'.");
                        }
                    }
                }

                //If the block has an inventory, it may have 'Inv<#>Tallies' keys instead. We need
                //to check for them.
                if (parseResult.Success && block.HasInventory)
                {
                    for (int i = 0; i < block.InventoryCount; i++)
                    {
                        if (iniReadWrite.ContainsKey(tag, $"Inv{i}Tallies"))
                        {
                            //If we manage to find one of these keys, the block can be considered
                            //handled.
                            handled = true;
                            //Get the names of the specified tallies
                            iniValue = iniReadWrite.Get(tag, $"Inv{i}Tallies");
                            elementNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                            foreach (string name in elementNames)
                            {
                                //If this tally name is in evalTallies...
                                if (evalTallies.ContainsKey(name))
                                {
                                    //...pull the tally out.
                                    tally = evalTallies[name];
                                    //Before we move on, we need to make sure this is a tallyCargo.
                                    if (tally is TallyCargo)
                                    {
                                        tallyCargo = (TallyCargo)tally;
                                        //We already know this block has an inventory (That's how 
                                        //we got here). Our next step is to add this inventory to
                                        //evalContainers
                                        IMyInventory inventory = block.GetInventory(i);
                                        //If we don't already have a dictionary entry for this 
                                        //inventory...
                                        if (!evalContainers.ContainsKey(inventory))
                                        //...create one
                                        { evalContainers.Add(inventory, new List<TallyCargo>()); }
                                        //Now that we're sure there's a place to put it, add this
                                        //tally to this inventory's entry.
                                        evalContainers[inventory].Add(tallyCargo);
                                    }
                                    //If the tally isn't a TallyCargo, complain.
                                    else
                                    {
                                        errors.add($"Block '{block.CustomName}' is not compatible " +
                                            $"with the TallyType of tally '{name}' referenced in key " +
                                            $"Inv{i}Tallies.");
                                    }
                                }
                                //If we can't find this name in evalTallies, complain.
                                else
                                {
                                    errors.add($"Block '{block.CustomName}' tried to reference the " +
                                        $"unconfigured tally '{name}' in key Inv{i}Tallies.");
                                }
                            }
                        }
                        //If there is no key, we fail silently.
                    }
                }

                //If the block has an 'ActionSets' key...
                if (parseResult.Success && iniReadWrite.ContainsKey(tag, "ActionSets"))
                {
                    //From the main section, we read:
                    //ActionSets: The ActionSet section names that can be found elsewhere in this 
                    //  block's CustomData.
                    //From each ActionSet section, we read:
                    //ActionOn (Default: null): The action to be performed on this block when this 
                    //  ActionSet is set to 'on'.
                    //ActionOff (Default: null): The action to be performed on this block when this
                    //  ActionSet is set to 'off'.
                    //We found something we understand, declare handled.
                    handled = true;
                    //Get the 'ActionSets' data
                    iniValue = iniReadWrite.Get(tag, "ActionSets");
                    //Pull the individual ActionSet names from the ActionSets key.
                    elementNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                    foreach (string name in elementNames)
                    {
                        //First things first: Does this ActionSet even exist?
                        if (sets.ContainsKey(name))
                        {
                            //The name of the discreet section that will configure this ActionSet 
                            //is the PREFIX plus the name of the ActionSet. We'll be using that a 
                            //lot, so let's put a handle on it.
                            discreetTag = $"{PREFIX}.{name}";
                            //Check to see if the user has included an ACTION SECTION
                            if (iniReadWrite.ContainsSection(discreetTag))
                            {
                                //Make a new, generic action plan.
                                IHasActionPlan actionPlan = null;
                                //If this is a camera block...
                                if (block is IMyCameraBlock)
                                {
                                    //The name of this raycaster should be on this block. Try to 
                                    //find it, using an empty string if we can't.
                                    string raycasterName = iniReadWrite.Get(tag, "RaycasterName").ToString("");
                                    ActionPlanRaycast cameraPlan;
                                    //If we can actually find a raycaster matching the name we 'found'.
                                    if (raycasters.ContainsKey(raycasterName))
                                    //Use the raycaster at the designated key to make a new ActionPlan
                                    { cameraPlan = new ActionPlanRaycast(raycasters[raycasterName]); }
                                    //Otherwise, create a new action plan with a null raycaster. This
                                    //will crash and burn the first time someone tries to use it, 
                                    //but if something went wrong with /this/ process, you can bet
                                    //we're complaining about it elsewhere.
                                    else
                                    { cameraPlan = new ActionPlanRaycast(null); }
                                    //Look for an ActionOn entry
                                    iniValue = iniReadWrite.Get(discreetTag, $"ActionOn");
                                    //There's only one value that we're looking for here
                                    if (iniValue.ToString() == "CameraRaycast")
                                    { cameraPlan.scanOn = true; }
                                    //Repeat the process for ActionOff
                                    iniValue = iniReadWrite.Get(discreetTag, $"ActionOff");
                                    if (iniValue.ToString() == "CameraRaycast")
                                    { cameraPlan.scanOff = true; }
                                    //That should be everything we need for a CameraPlan. Pass it
                                    //to the generic reference.
                                    actionPlan = cameraPlan;
                                }
                                //If we have no plan, or the plan we have sucks because it has no 
                                //actions defined.
                                if (actionPlan == null || !actionPlan.hasAction())
                                {
                                    //Is this config for a terminal action plan?
                                    if (iniReadWrite.ContainsKey(discreetTag, "Action0Property"))
                                    {
                                        //From config for ActionPlanTerminal, we read:
                                        //Action<#>Property: Which block property will be targeted
                                        //  by this ActionPart
                                        //Action<#>OnValue: The value to be applied when this ActionSet
                                        //  is set to 'on'
                                        //Action<#>OffValue: The value to be applied when this ActionSet
                                        //  is set to 'off'
                                        ActionPlanTerminal terminalPlan = new ActionPlanTerminal(block);
                                        ActionPart retreivedPart = null;
                                        counter = 0;
                                        //Add ActionParts to the terminalPlan until we run out of config.
                                        while (counter != -1)
                                        {
                                            retreivedPart = tryGetPartFromConfig(errors, discreetTag,
                                                counter, block, iniReadWrite, iniValue);
                                            if (retreivedPart != null)
                                            {
                                                terminalPlan.addPart(retreivedPart);
                                                counter++;
                                            }
                                            else
                                            { counter = -1; }
                                        }

                                        actionPlan = terminalPlan;
                                    }
                                    else
                                    {
                                        //Create a new block plan with this block as the subject
                                        ActionPlanBlock blockPlan = new ActionPlanBlock(block);
                                        //Check to see if there's an ActionOn in the ACTION SECTION
                                        blockPlan.actionOn = matchAction(errors, discreetTag,
                                            "ActionOn", block, iniReadWrite, actions);
                                        //Check to see if there's an ActionOff in the ACTION SECTION
                                        blockPlan.actionOff = matchAction(errors, discreetTag,
                                            "ActionOff", block, iniReadWrite, actions);
                                        //Pass the BlockPlan to the generic ActionPlan
                                        actionPlan = blockPlan;
                                    }
                                }
                                //If we have successfully registered at least one action...
                                if (actionPlan.hasAction())
                                //Go ahead and add this ActionPlan to the ActionSet
                                { sets[name].addActionPlan(actionPlan); }
                                //If we didn't successfully register an action, complain.
                                else
                                {
                                    errors.add($"Block '{block.CustomName}', discreet section '{discreetTag}', " +
                                        $"does not define either an ActionOn or an ActionOff.");
                                }
                            }
                            //If there is no ACTION SECTION, complain.
                            else
                            {
                                errors.add($"Block '{block.CustomName}' references the ActionSet " +
                                    $"'{name}', but contains no discreet '{discreetTag}' section that would " +
                                    $"define actions.");
                            }
                        }
                        //If the set does not exist, complain.
                        else
                        {
                            errors.add($"Block '{block.CustomName}' tried to reference the " +
                                $"unconfigured ActionSet '{name}'.");
                        }
                    }
                }

                //On to block types.
                //The PB and all camera blocks get their own sorter. Because if we made it this far, 
                //they're handled.
                if (parseResult.Success && (block == Me || block is IMyCameraBlock))
                { handled = true; }

                //If our parse was successful and this block is a surface provider, we need to 
                //configure some reports.
                if (parseResult.Success && block is IMyTextSurfaceProvider)
                {
                    //No matter what happens, we set the handled flag to indicate that we had a
                    //sorter that knew what to do with this.
                    handled = true;
                    //From SurfaceProviders, we read:
                    //Surface<#>Elements: Which tallies we should show on the designated surface. 
                    //  NOTE: A tally by the name of 'blank' (Case insensitive) is used to indicate
                    //  an empty element on the Report's grid.
                    //Surface<#>MFD: As an alternative to the list of tallies to be displayed, the 
                    //  name of the MFD that will be displayed on this surface. MFDs are configured
                    //  using the same catagories as seen below, but with the name of the MFD, 
                    //  followed by the MFD page number, in place of Surface<#>.
                    //Surface<#>Title: (Default = "") The title of this report, which will appear at 
                    //  the top of its surface.
                    //Surface<#>Columns: (Default = 1) The number of columns to use when arranging 
                    //  the reports on the designated surface.
                    //Surface<#>FontSize: (Default = 1f) The font size to be used
                    //Surface<#>Font: (Default = Debug) The font type to be used
                    //Surface<#>ForeColor: (Default = Color of the Surface) Foreground color of this report
                    //Surface<#>BackColor: (Default = Color of the Surface) Background color of this report
                    IMyTextSurfaceProvider surfaceProvider = (IMyTextSurfaceProvider)block;
                    //For every surface on this block...
                    for (int i = 0; i < surfaceProvider.SurfaceCount; i++)
                    {
                        //Are we supposed to display an MFD on this surface?
                        if (iniReadWrite.ContainsKey(tag, $"Surface{i}MFD"))
                        {
                            //Pull the name of the MFD from the main config
                            MFDName = iniReadWrite.Get(tag, $"Surface{i}MFD").ToString();
                            //Construct the discreetTag of the section that will configure this MFD
                            discreetTag = $"{PREFIX}.{MFDName}";
                            //Is there a discreet section with config for this MFD?
                            if (iniReadWrite.ContainsSection(discreetTag))
                            {
                                MFD newMFD = new MFD();
                                counter = 0;
                                //There's several keys that we could be looking for.
                                while (counter != -1)
                                {
                                    reportable = tryGetReportableFromConfig(errors, $"Page{counter}",
                                        discreetTag, surfaceProvider.GetSurface(i), block, iniReadWrite,
                                        evalTallies, sets);
                                    //Did we get a reportable?
                                    if (reportable != null)
                                    {
                                        //Check to see if the user defined a title. If they didn't, 
                                        //we'll have to generate a title based on the MFD name and 
                                        //page number to address this MFD page.
                                        if (reportable is Report && !String.IsNullOrEmpty(((Report)reportable).title))
                                        { addIn1 = ((Report)reportable).title; }
                                        else
                                        { addIn1 = $"{MFDName}{counter}"; }
                                        //There's one bit of configuration that's specific to MFDs, 
                                        //and that's linking to ActionSets. While we have both the 
                                        //MFD and the page name handy, we'll check for ActionSet
                                        //integration
                                        iniValue = iniReadWrite.Get(discreetTag, $"Page{counter}LinkActionSetOn");
                                        if (!iniValue.IsEmpty)
                                        {
                                            //Store the name of the ActionSet the user wants to link 
                                            //to this page
                                            elementName = iniValue.ToString();
                                            //Check to see if this ActionSet even exists
                                            if (sets.ContainsKey(elementName))
                                            {
                                                ActionPlanMFD MFDPlan = new ActionPlanMFD(newMFD);
                                                //Set this page as the ActionPlan's pageOn
                                                MFDPlan.pageOn = addIn1;
                                                //Add the ActionPlan to the ActionSet
                                                sets[elementName].addActionPlan(MFDPlan);
                                            }
                                            //If the ActionSet doesn't exist, complain.
                                            else
                                            {
                                                errors.add($"Surface provider '{block.CustomName}', " +
                                                    $"discreet section '{discreetTag}', tried to " +
                                                    $"reference the unconfigured ActionSet '{elementName}' " +
                                                    $"in its LinkActionSetOn configuration.");
                                            }
                                        }
                                        iniValue = iniReadWrite.Get(discreetTag, $"Page{counter}LinkActionSetOff");
                                        if (!iniValue.IsEmpty)
                                        {
                                            //Store the name of the ActionSet the user wants to link 
                                            //to this page
                                            elementName = iniValue.ToString();
                                            //Check to see if this ActionSet even exists
                                            if (sets.ContainsKey(elementName))
                                            {
                                                ActionPlanMFD MFDPlan = new ActionPlanMFD(newMFD);
                                                //Set this page as the ActionPlan's pageOn
                                                MFDPlan.pageOff = addIn1;
                                                //Add the ActionPlan to the ActionSet
                                                sets[elementName].addActionPlan(MFDPlan);
                                            }
                                            //If the ActionSet doesn't exist, complain.
                                            else
                                            {
                                                errors.add($"Surface provider '{block.CustomName}', " +
                                                    $"discreet section '{discreetTag}', tried to " +
                                                    $"reference the unconfigured ActionSet '{elementName}' " +
                                                    $"in its LinkActionSetOff configuration.");
                                            }
                                        }
                                        //Add our newly configured reportable to the MFD.
                                        newMFD.addPage(addIn1, reportable);
                                        counter++;
                                    }
                                    //If we didn't get a reportable, indcate that it's time to exit 
                                    //the loop.
                                    else
                                    { counter = -1; }
                                }
                                //If we actually got configuration for at least one page...
                                if (newMFD.getPageCount() > 0)
                                {
                                    //Try to set the current page of this MFD to whatever it was 
                                    //prior to the last evaluation.
                                    newMFD.trySetPage(iniRead.Get("MFDs", MFDName).ToString());
                                    //Add the new MFD to our reports and MFDs
                                    evalReports.Add(newMFD);
                                    MFDs.Add(MFDName, newMFD);
                                }
                                //If we didn't get at least one page, complain.
                                else
                                {
                                    errors.add($"Surface provider '{block.CustomName}', Surface {i}, " +
                                        $"specified the use of MFD '{MFDName}' but did not provide " +
                                        $"readable page configuration.");
                                }
                            }
                            //If there is no discreet section, complain.
                            else
                            {
                                errors.add($"Surface provider '{block.CustomName}', Surface {i}, " +
                                    $"declares the MFD '{MFDName}', but contains no discreet " +
                                    $"'{discreetTag}' section that would configure it.");
                            }
                        }
                        else
                        {
                            //If it isn't an MFD, pass it directly to the specialized method for sorting
                            reportable = tryGetReportableFromConfig(errors, $"Surface{i}", tag,
                                surfaceProvider.GetSurface(i), block, iniReadWrite, evalTallies, sets);
                            //Only if we got a reportable...
                            if (reportable != null)
                            //...should we try adding it to our list.
                            { evalReports.Add(reportable); }
                        }
                    }
                }

                //This could also be an indicator light, something I totally didn't forget when I 
                //first wrote this. Let's check!
                if (parseResult.Success && block is IMyLightingBlock)
                {
                    //We'll hold off on setting the 'handled' flag for now.
                    //From lights, we read:
                    //Element: The Element (Singular) that this indicator group watches
                    iniValue = iniReadWrite.Get(tag, "Element");
                    if (!iniValue.IsEmpty)
                    {
                        elementName = iniValue.ToString();
                        IHasElement element = null;
                        //If the element is in evalTallies or sets... 
                        if (evalTallies.ContainsKey(elementName))
                        { element = evalTallies[elementName]; }
                        else if (sets.ContainsKey(elementName))
                        { element = sets[elementName]; }
                        //If we weren't able to find the element, complain.
                        else
                        {
                            errors.add($"Lighting block '{block.CustomName}' tried to reference " +
                                $"the unconfigured element '{elementName}'. Note that lighting blocks can " +
                                $"only monitor one element.");
                        }
                        //If we successfully retreived an element...
                        if (element != null)
                        {
                            //...we first need to see if it's already in the dictionary tracking
                            //Indicator light groups.
                            if (!evalIndicators.ContainsKey(elementName))
                            //If it isn't, we add one.
                            { evalIndicators.Add(elementName, new Indicator(element)); }
                            //Once we're sure there's an Indicator group in the dictionary, add 
                            //this light to it.
                            evalIndicators[elementName].addLight((IMyLightingBlock)block);
                        }
                    }
                    //If we can't find the Element key, and this block hasn't been handled, complain.
                    else if (!handled)
                    {
                        errors.add($"Lighting block {block.CustomName} has missing or unreadable Element. " +
                            $"Note that lighting blocks use the 'Element' key, singular.");
                    }
                    //If there's no Element key, but the block has been handled, fail silently in 
                    //hope that someone, somewhere, knew what they were doing.
                    //Also, go ahead and set the 'handled' flag.
                    handled = true;
                }

                //If we made it here, but the block hasn't been handled, it's time to complain.
                //Previously, this would only occur if a block type couldn't be handled by the 
                //script. Now, though, things are a bit more complicated, and this message is a lot
                //less useful.
                if (parseResult.Success && !handled)
                {
                    errors.add($"Block '{block.CustomName}' is missing proper configuration or is a " +
                        $"block type that cannot be handled by this script.");
                }

                //Set handled to 'false' for the next iteration of the loop.
                handled = false;
            }

            //Time to finalize things. First, we need to build our array of containers using the
            //data we've collected. 
            Container container;
            //Build our execution array, based on how many entries we have in the container dictionary
            containers = new Container[evalContainers.Count];
            counter = 0;
            foreach (IMyInventory inventory in evalContainers.Keys)
            {
                //Build a new Container object based on the data we've collected in evaluation
                container = new Container(inventory, evalContainers[inventory].ToArray());
                //Send the maximum volume of this inventory to its linked tallies.
                container.sendMaxToTallies();
                //Place the container in the array.
                containers[counter] = container;
                counter++;
            }
            //Next, tear down the complicated data structures we've been using for evaluation into
            //the arrays we'll be using during execution
            tallies = evalTallies.Values.ToArray();
            indicators = evalIndicators.Values.ToArray();
            reports = evalReports.ToArray();
            triggers = evalTriggers.ToArray();
            //We'll take this opportunity to call setProfile on all our Reportables
            foreach (IReportable screen in reports)
            { screen.setProfile(); }
            //There's one more step before the tallies are ready. We need to tell them that they
            //have all the data that they're going to get. 
            foreach (Tally finishTally in tallies)
            { finishTally.finishSetup(); }
            //There's probably still data in the iniReader. We don't need it anymore, and we don't
            //want it carrying over to any future evaluations.
            iniReadWrite.Clear();
            iniRead.Clear();

            //That should be it. So if we have no errors...
            if (errors.getErrorTotal() == 0)
            {
                //...Set the script into motion.
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
                //Also, brag.
                log.add($"Grid evaluation complete. Registered {tallies.Length} tallies, " +
                    $"{sets.Count} ActionSets, {triggers.Length} triggers, and {reports.Length} " +
                    $"reports, as configured by data on {blocks.Count} blocks. Evaluation used " +
                    $"{Runtime.CurrentInstructionCount} / {Runtime.MaxInstructionCount} " +
                    $"({(int)(((double)Runtime.CurrentInstructionCount / Runtime.MaxInstructionCount) * 100)}%) " +
                    $"of instructions allowed in this tic.\nScript is now running.");
            }
            else
            {
                //Make sure the script isn't trying to run with errors.
                Runtime.UpdateFrequency = UpdateFrequency.None;
                //Also, complain.
                log.add($"Grid evaluation complete. The following errors are preventing script " +
                    $"execution:\n{errors.toString()}");
            }
        }

        /* Scans the specified MyIni object for tally configuration. Can return a functional tally,
         * a stand-in tally and an error, or a null if no tally was found.
         *  -MyIni configReader: A MyIni object, loaded with the configuration we're wanting to scan
         *  -string targetSection: Which section in the configuration we should be scanning
         *  -int index: The index at which we're looking for a tally configuration.
         *  -MyIniValue iniValue: A reference to an iniValue object, so we don't need to allocate a 
         *   new one
         *  -LimitedErrorLog errors: The error log that we will report errors to.
         *  -out string tallyName: The name of this tally.
         * Returns: A Tally object if some sort of configuration was found at the specified index 
         * (Even if that configuration has errors). A null if no config was found.
         */
        private Tally tryGetTallyFromConfig(MyIni configReader, string targetSection, int index,
            MyIniValue iniValue, LimitedErrorLog errors, out string tallyName)
        {
            Tally tally = null;
            string tallyType;
            /*string errorShieldMissing = "DefenseShields mod not loaded, please remove the following " +
                "tally: ";*/
            //Look to see if there's a tally at this index
            tallyName = configReader.Get(targetSection, $"Tally{index}Name").ToString();
            if (!string.IsNullOrEmpty(tallyName))
            {
                //Our next steps are going to be dictated by the TallyType. We should try and 
                //figure out what that is.
                tallyType = configReader.Get(targetSection, $"Tally{index}Type").ToString();
                //If no type is defined, complain
                if (string.IsNullOrEmpty(tallyType))
                {
                    errors.add($"Tally {tallyName} has a missing or unreadable TallyType.");
                    //Also, create a TallyCargo. This will let the rest of the script execute
                    //as normal, and hopefully prevent 'uninitialized tally' spam
                    tally = new TallyCargo(meterMaid, tallyName);
                }
                //Now, we create a tally based on the type. For the TallyCargo, that's quite straightforward.
                else if (tallyType == "Inventory")
                { tally = new TallyCargo(meterMaid, tallyName); }
                //Creating a TallyItem is a bit more involved.
                else if (tallyType == "Item")
                {
                    string typeID, subTypeID;
                    //We'll need a typeID and a subTypeID, and we'll need to complain if we can't
                    //get them
                    typeID = configReader.Get(targetSection, $"Tally{index}ItemTypeID").ToString();
                    if (string.IsNullOrEmpty(typeID))
                    { errors.add($"Item Tally '{tallyName}' has a missing or unreadable TallyItemTypeID."); }
                    subTypeID = configReader.Get(targetSection, $"Tally{index}ItemSubTypeID").ToString();
                    if (string.IsNullOrEmpty(subTypeID))
                    { errors.add($"Item Tally '{tallyName}' has a missing or unreadable TallyItemSubTypeID."); }
                    //If we have the data we were looking for, we can create a TallyItem
                    if (!string.IsNullOrEmpty(typeID) && !string.IsNullOrEmpty(subTypeID))
                    { tally = new TallyItem(meterMaid, tallyName, typeID, subTypeID); }
                    //If we're missing data, we'll just create a TallyCargo so the script can 
                    //continue. The error message should already be logged.
                    else
                    { tally = new TallyCargo(meterMaid, tallyName); }
                }
                //Power and the other TallyGenerics are only marginally more complicated than Volume
                else if (tallyType == "Battery")
                { tally = new TallyGeneric(meterMaid, tallyName, new BatteryHandler()); }
                //Gas, which works for both Hydrogen and Oxygen
                else if (tallyType == "Gas")
                { tally = new TallyGeneric(meterMaid, tallyName, new GasHandler()); }
                else if (tallyType == "JumpDrive")
                { tally = new TallyGeneric(meterMaid, tallyName, new JumpDriveHandler()); }
                else if (tallyType == "Raycast")
                { tally = new TallyGeneric(meterMaid, tallyName, new RaycastHandler()); }
                else if (tallyType == "PowerProducer")
                { tally = new TallyGeneric(meterMaid, tallyName, new PowerProducerHandler()); }
                //TODO: Aditionally TallyTypes go here
                else
                {
                    //If we've gotten to this point, the user has given us a type that we can't 
                    //recognize. Scold them.
                    errors.add($"Tally {tallyName}'s TallyType of '{tallyType}' cannot be handled " +
                        $"by this script. Be aware that TallyTypes are case-sensitive.");
                    //...Also, create a TallyCargo, so the rest of Evaluate will work.
                    tally = new TallyCargo(meterMaid, tallyName);
                }
                //Now that we have our tally, we need to check to see if there's any further
                //configuration data. 
                //First, the DisplayName
                iniValue = configReader.Get(targetSection, $"Tally{index}DisplayName");
                if (!iniValue.IsEmpty)
                { tally.displayName = iniValue.ToString(); }
                //Up next is the Multiplier. Note that, because of how forceMax works, the multiplier
                //must be applied befire the max.
                iniValue = configReader.Get(targetSection, $"Tally{index}Multiplier");
                if (!iniValue.IsEmpty)
                { tally.multiplier = iniValue.ToDouble(); }
                //Then the Max
                iniValue = configReader.Get(targetSection, $"Tally{index}Max");
                if (!iniValue.IsEmpty)
                { tally.forceMax(iniValue.ToDouble()); }
                //There's a couple of TallyTypes that need to have a Max explicitly set (All 
                //TallyItems, plus the TallyGeneric Raycast (But not PowerProducers, that's fixed). 
                //If that hasn't happened, we need to complain.
                else if (iniValue.IsEmpty && (tally is TallyItem || (tally is TallyGeneric
                    && ((TallyGeneric)tally).handler is RaycastHandler)))
                {
                    errors.add($"Tally {tallyName}'s TallyType of '{tallyType}' requires a Max " +
                        $"to be set in configuration.");
                }
                //Last, LowGood
                iniValue = configReader.Get(targetSection, $"Tally{index}LowGood");
                if (!iniValue.IsEmpty)
                { tally.lowGood(iniValue.ToBoolean()); }
                //We now have a functional tally. Or at least a shambling semblance of one that will
                //allow us to execute the rest of Evaluation.
            }
            //Return the tally we found, the tally we made up, or the absence of tally.
            return tally;
        }

        /* Scans the specified MyIni object for action configuration. Can return a functional action,
         *   an action and an error, or a null if no action was found.
         * MyIni configReader: A MyIni object, loaded with the parse we're wanting to scan
         * MyIni saveReader: A MyIni object, loaded with a parse of the script's Storage string
         * string targetSection: Which section in the configuration we should be scanning
         * int index: The index at which we're looking for an action configuration.
         * MyIniValue iniValue: A reference to an iniValue object, so we don't need to allocate a 
         *   new one
         * LimitedErrorLog errors: The error log that we will report errors to.
         * out string actionName: The name of this action
         * Returns: An action object if some sort of configuration was found at the specified index 
         *   (Even if that configuration has errors). A null if no config was found.
         */
        private ActionSet tryGetActionFromConfig(MyIni configReader, MyIni saveReader, string targetSection, int index,
            MyIniValue iniValue, LimitedErrorLog errors, out string actionName)
        {
            ActionSet set = null;
            Color color = Hammers.cozy;
            //Look for another ActionSet
            actionName = configReader.Get(targetSection, $"Action{index}Name").ToString();
            if (!string.IsNullOrEmpty(actionName))
            {
                //ActionSets have a lot less going on than tallies, initially at least. The only
                //other thing we /need/ to know about them is what their previous state was.
                //We'll try to get that from the storage string, defaulting to false if we can't
                bool state = saveReader.Get("ActionSets", actionName).ToBoolean(false);
                set = new ActionSet(actionName, state);

                //There are a few other bits of configuration that ActionSets may have
                iniValue = configReader.Get(targetSection, $"Action{index}DisplayName");
                if (!iniValue.IsEmpty)
                { set.displayName = iniValue.ToString(); }
                if (tryGetColorFromConfig(errors, ref color, $"Action{index}", "ColorOn",
                    targetSection, configReader, Me))
                { set.colorOn = color; }
                if (tryGetColorFromConfig(errors, ref color, $"Action{index}", "ColorOff",
                    targetSection, configReader, Me))
                { set.colorOff = color; }
                iniValue = configReader.Get(targetSection, $"Action{index}TextOn");
                if (!iniValue.IsEmpty)
                { set.textOn = iniValue.ToString(); }
                iniValue = configReader.Get(targetSection, $"Action{index}TextOff");
                if (!iniValue.IsEmpty)
                { set.textOff = iniValue.ToString(); }
                //DelayOn and DelayOff. These will actually be stored in an ActionPlan, but we
                //need to know if one of the values is present before we create the object.
                int delayOn = configReader.Get(targetSection, $"Action{index}DelayOn").ToInt32();
                int delayOff = configReader.Get(targetSection, $"Action{index}DelayOff").ToInt32();
                //If one of the delay values isn't 0...
                if (delayOn != 0 || delayOff != 0)
                {
                    //Create a new action plan
                    ActionPlanUpdate plan = new ActionPlanUpdate(distributor);
                    //Store the values we got. No need to run any checks here, they'll be fine
                    //if we pass them zeros
                    plan.delayOn = delayOn;
                    plan.delayOff = delayOff;
                    //Add the update plan to this ActionSet.
                    set.addActionPlan(plan);
                }
                //We'll call setState to make sure all the blocks associated with this ActionSet
                //are doing what they're meant to, that updateDelay is properly set, and that
                //statusText and statusColor correctly reflect any newly configured values.
                set.setState(set.state); //Peak readable code, right here.
                //We don't want to be firing off messages after every evaluate, so we'll only check
                //for IGC config after everything else has been set.
                iniValue = configReader.Get(targetSection, $"Action{index}IGCChannel");
                if (!iniValue.IsEmpty)
                {
                    string channel = iniValue.ToString();
                    //Create a new action plan, using the string we collected as the channel
                    ActionPlanIGC plan = new ActionPlanIGC(IGC, channel);
                    iniValue = configReader.Get(targetSection, $"Action{index}IGCMessageOn");
                    if (!iniValue.IsEmpty)
                    { plan.messageOn = iniValue.ToString(); }
                    iniValue = configReader.Get(targetSection, $"Action{index}IGCMessageOff");
                    if (!iniValue.IsEmpty)
                    { plan.messageOff = iniValue.ToString(); }
                    //Last step is to make sure we got some config
                    if (plan.hasAction())
                    { set.addActionPlan(plan); }
                    else
                    {
                        errors.add($"Action '{actionName}' has configuration for sending an IGC " +
                            $"message on the channel '{channel}', but does not have readable config " +
                            $"on what messages should be sent.");
                    }
                }
                //That's it. This ActionSet is now prepared.
            }
            return set;
        }

        public Action<IMyTerminalBlock> matchAction(LimitedErrorLog errors, string discreetTag, string target,
            IMyTerminalBlock block, MyIni iniReader, Dictionary<string, Action<IMyTerminalBlock>> actions)
        {
            //Check the config for the presence of the target key
            MyIniValue iniValue = iniReader.Get(discreetTag, target);
            Action<IMyTerminalBlock> retreivedAction = null;
            //If we retreived some data...
            if (!iniValue.IsEmpty)
            {
                //Store the value we retreived
                string actionName = iniValue.ToString();
                //If this string matches an entry in our 'actions' dictionary, make a note of which one.
                if (actions.ContainsKey(actionName))
                { retreivedAction = actions[actionName]; }
                //If there is no matching action, complain.
                else
                {
                    errors.add($"Block '{block.CustomName}', discreet section '{discreetTag}', " +
                        $"references the unknown action '{actionName}' as its {target}.");
                }
            }
            return retreivedAction;
        }

        /* Scans an iniReader containing a parse of a block's CustomData for Action<index> 
         *   configuration in the given discreet section.
         * LimitedErrorLog errors: The error log that we will report errors to.
         * string discreetTag: The name of the discreet section we'll be reading
         * int index: The specific ActionPart we're trying to get config for
         * IMyTerminalBlock block: The block that we're reading config from, passed in for use in
         *   error messages.
         * MyIni iniReader: The MyIni object containing the parse of the block's CustomData.
         * MyIniValue iniValue: A reference to an iniValue object, so we don't need to allocate a 
         *   new one
         * Returns: An ActionPart object so long as some sort of config was found. A null if no 
         *   config was found. If the configuration is bad, a placeholder ActionPart will be returned,
         *   and error messages will be added to the LimitedErrorLog.
         */
        public ActionPart tryGetPartFromConfig(LimitedErrorLog errors, string discreetTag, int index,
            IMyTerminalBlock block, MyIni iniReader, MyIniValue iniValue)
        {
            //Check the config for the presence of the target key
            string propertyKey = $"Action{index}Property";
            iniValue = iniReader.Get(discreetTag, propertyKey);
            ActionPart retreivedPart = null;
            if (!iniValue.IsEmpty)
            {
                string propertyName = iniValue.ToString("<missing>");
                //Before we can move on, we need to figure out what type of value this property 
                //contains. Or if there's a property at all.
                //bool, StringBuilder, long, float, color
                ITerminalProperty propertyDef = block.GetProperty(propertyName);
                if (propertyDef == null)
                {
                    errors.add($"Block '{block.CustomName}', discreet section '{discreetTag}', " +
                        $"references the unknown property '{propertyName}' as its {propertyKey}.");
                    retreivedPart = new ActionPart<bool>(propertyName);
                }
                else if (propertyDef.TypeName.ToLowerInvariant() == "boolean")
                {
                    //The process for each type is basically the same
                    ActionPart<bool> typedPart = new ActionPart<bool>(propertyName);
                    bool typedValue = false;
                    //Check for an valueOn
                    iniValue = iniReader.Get(discreetTag, $"Action{index}ValueOn");
                    if (!iniValue.IsEmpty && iniValue.TryGetBoolean(out typedValue))
                    { typedPart.setValueOn(typedValue); }
                    //Check for an valueOff
                    iniValue = iniReader.Get(discreetTag, $"Action{index}ValueOff");
                    if (!iniValue.IsEmpty && iniValue.TryGetBoolean(out typedValue))
                    { typedPart.setValueOff(typedValue); }
                    //Pass this ActionPart out to the un-type'd variable.
                    retreivedPart = typedPart;
                }
                else if (propertyDef.TypeName.ToLowerInvariant() == "stringbuilder")
                {
                    ActionPart<StringBuilder> typedPart = new ActionPart<StringBuilder>(propertyName);
                    string typedValue = "";
                    iniValue = iniReader.Get(discreetTag, $"Action{index}ValueOn");
                    if (!iniValue.IsEmpty && iniValue.TryGetString(out typedValue))
                    {
                        StringBuilder builder = new StringBuilder(typedValue); //This hurts my heart.
                        typedPart.setValueOn(builder);
                    }
                    iniValue = iniReader.Get(discreetTag, $"Action{index}ValueOff");
                    if (!iniValue.IsEmpty && iniValue.TryGetString(out typedValue))
                    {
                        StringBuilder builder = new StringBuilder(typedValue);
                        typedPart.setValueOff(builder);
                    }

                    retreivedPart = typedPart;
                }
                else if (propertyDef.TypeName.ToLowerInvariant() == "int64")
                {
                    ActionPart<long> typedPart = new ActionPart<long>(propertyName);
                    long typedValue = -1;
                    iniValue = iniReader.Get(discreetTag, $"Action{index}ValueOn");
                    if (!iniValue.IsEmpty && iniValue.TryGetInt64(out typedValue))
                    { typedPart.setValueOn(typedValue); }
                    iniValue = iniReader.Get(discreetTag, $"Action{index}ValueOff");
                    if (!iniValue.IsEmpty && iniValue.TryGetInt64(out typedValue))
                    { typedPart.setValueOff(typedValue); }

                    retreivedPart = typedPart;
                }
                else if (propertyDef.TypeName.ToLowerInvariant() == "single")
                {
                    ActionPart<float> typedPart = new ActionPart<float>(propertyName);
                    float typedValue = -1;
                    iniValue = iniReader.Get(discreetTag, $"Action{index}ValueOn");
                    if (!iniValue.IsEmpty && iniValue.TryGetSingle(out typedValue))
                    { typedPart.setValueOn(typedValue); }
                    iniValue = iniReader.Get(discreetTag, $"Action{index}ValueOff");
                    if (!iniValue.IsEmpty && iniValue.TryGetSingle(out typedValue))
                    { typedPart.setValueOff(typedValue); }

                    retreivedPart = typedPart;
                }
                else if (propertyDef.TypeName.ToLowerInvariant() == "color")
                {
                    //Colors are a bit different
                    ActionPart<Color> typedPart = new ActionPart<Color>(propertyName);
                    Color typedValue = Hammers.cozy;
                    if (tryGetColorFromConfig(errors, ref typedValue, $"Action{index}", "ValueOn",
                        discreetTag, iniReader, block))
                    { typedPart.setValueOn(typedValue); }
                    if (tryGetColorFromConfig(errors, ref typedValue, $"Action{index}", "ValueOff",
                        discreetTag, iniReader, block))
                    { typedPart.setValueOff(typedValue); }

                    retreivedPart = typedPart;
                }
                else
                {
                    errors.add($"Block '{block.CustomName}', discreet section '{discreetTag}', " +
                        $"references the property '{propertyName}' which uses the non-standard " +
                        $"type {propertyDef.TypeName}. Report this to the scripter, as the script " +
                        $"will need to be altered to handle this.");
                    retreivedPart = new ActionPart<bool>(propertyName);
                }
                //The last step is to make sure that we got a value /somewhere/
                if (!retreivedPart.isHealthy() && propertyDef != null)
                {
                    errors.add($"Block '{block.CustomName}', discreet section '{discreetTag}', " +
                        $"does not specify a working OnValue or OffValue for the property " +
                        $"'{propertyName}'. If one was specified, make sure that it matches the " +
                        $"type '{propertyDef.TypeName}.'");
                }
                //At this point, we either have a functional ActionPart, or a story to tell. Time to
                //head home.
                return retreivedPart;
            }
            else
            //If we didn't find an ActionProperty at this index, assume that we've reached the end
            //of the list and return our existing null ActionPart
            { return retreivedPart; }
        }

        /* Scans the specified MyIni object for trigger configuration. Can return a functional trigger,
         *   a fake trigger and an error, or a null if no trigger config was found.
         * MyIni configReader: A MyIni object, loaded with the parse we're wanting to scan
         * Dictionary evalTallies: A dictionary containing the tallies we've found during evaluation
         * Dictionary evalSets: A dictionary containing the ActionSets we've found during evaluation
         * string targetSection: Which section in the configuration we should be scanning
         * int index: The index at which we're looking for a trigger configuration.
         * MyIniValue iniValue, Tally targetTally, ActionSet targetSet: References to objects that 
         *   we're already using in evaluation, so we don't need to allocate new ones. They will be
         *   filled with junk data when the method completes.
         * Trigger trigger: A reference to a trigger object. This variable is effectively what is 
         *   returned by this method: A functional trigger with its properties defined by the 
         *   configuration, a fake trigger and entries in the error log, or a null if no config was 
         *   found at the specified index.
         * LimitedErrorLog errors: The error log that we will report errors to.
         */
        private void tryGetTriggerFromConfig(MyIni configReader, Dictionary<string, Tally> evalTallies,
            Dictionary<string, ActionSet> evalSets, string targetSection, int index, MyIniValue iniValue,
            ref Tally targetTally, ref ActionSet targetSet, ref Trigger trigger, LimitedErrorLog errors)
        {
            //From trigger configuration, we read:
            //Trigger<#>Tally: The Tally this trigger will watch
            //Trigger<#>ActionSet: The ActionSet this trigger will operate
            //Trigger<#>LessOrEqualValue: When the watched Tally falls below this value, the 
            //  commandLess will be sent
            //Trigger<#>LessOrEqualCommand: The command to be sent when we're under the threshold
            //Trigger<#>GreaterOrEqualValue: When the watched Tally exceeds this value, the 
            //  commandGreater will be sent
            //Trigger<#>GreaterOrEqualCommand: The command to be sent when we're over the threshold
            //Trigger<#>LinkedActionSet: The ActionSet that enables or disables this trigger
            //Trigger<#>LinkedActionOn: Determines if this trigger will be enabled or disabled when
            //  the linked ActionSet is set to 'on'
            //Trigger<#>LinkedActionOff: Determines if this trigger will be enabled or disabled when
            //  the linked ActionSet is set to 'off'

            trigger = null;
            string tallyName = "";
            targetTally = null;
            string setName = "";
            targetSet = null;

            iniValue = configReader.Get(targetSection, $"Trigger{index}Tally");
            if (iniValue.IsEmpty)
            {
                iniValue = configReader.Get(targetSection, $"Trigger{index}ActionSet");
                if (iniValue.IsEmpty)
                //If we're missing both the Tally and the ActionSet, we assume we've hit the end 
                //of the config. 'Return' with all of our variables as null.
                { return; }
            }
            //We have some sort of config. Let's try to figure it out.
            //Re-read the Tally name; we can't be sure that's what's in iniValue.
            tallyName = configReader.Get(targetSection, $"Trigger{index}Tally").ToString();
            if (!string.IsNullOrEmpty(tallyName))
            {
                //Try to match the tallyName to a configured Tally
                if (evalTallies.ContainsKey(tallyName))
                { targetTally = evalTallies[tallyName]; }
                else
                { errors.add($"Trigger {index} tried to reference the unconfigured Tally '{tallyName}'."); }
            }
            else
            { errors.add($"Trigger {index} has a missing or unreadable Tally."); }
            //Try to get the name of the targetSet
            setName = configReader.Get(targetSection, $"Trigger{index}ActionSet").ToString();
            if (!string.IsNullOrEmpty(setName))
            {
                //Try to match the tallyName to a configured Tally
                if (evalSets.ContainsKey(setName))
                { targetSet = evalSets[setName]; }
                else
                { errors.add($"Trigger {index} tried to reference the unconfigured ActionSet '{setName}'."); }
            }
            else
            { errors.add($"Trigger {index} has a missing or unreadable ActionSet."); }

            //If we got a tally and an ActionSet, we can continue.
            if (targetTally != null && targetSet != null)
            {
                trigger = new Trigger(targetTally, targetSet, log);
                //Check for lessOrEqual and greaterOrEqual scenarios
                tryGetCommandFromConfig(configReader, trigger, targetSection, "LessOrEqual", index,
                    true, iniValue, errors);
                tryGetCommandFromConfig(configReader, trigger, targetSection, "GreaterOrEqual", index,
                    false, iniValue, errors);
                //Last step is checking to see if this trigger is governed by an ActionSet.
                //NOTE: The underlying data structures are designed to support triggers being 
                //operated by multiple different ActionSets. However, the current implementation
                //only allows one. More than that will require solving several config-related issues.
                iniValue = configReader.Get(targetSection, $"Trigger{index}LinkedActionSet");
                if (!iniValue.IsEmpty)
                {
                    //We're done with setName. Let's put it to use again!
                    setName = iniValue.ToString();
                    if (evalSets.ContainsKey(setName))
                    {
                        ActionPlanTrigger triggerPlan = new ActionPlanTrigger(trigger);
                        string linkString;
                        iniValue = configReader.Get(targetSection, $"Trigger{index}LinkedActionOn");
                        if (!iniValue.IsEmpty)
                        {
                            linkString = iniValue.ToString().ToLowerInvariant();
                            if (linkString == "enable")
                            { triggerPlan.setActionOn(true); }
                            else if (linkString == "disable")
                            { triggerPlan.setActionOn(false); }
                            else
                            {
                                errors.add($"Trigger {index} uses the unknown command '{linkString}' " +
                                    $"as its LinkedActionOn. Valid commands are 'enable' and 'disable'.");
                            }
                        }
                        iniValue = configReader.Get(targetSection, $"Trigger{index}LinkedActionOff");
                        if (!iniValue.IsEmpty)
                        {
                            linkString = iniValue.ToString().ToLowerInvariant();
                            if (linkString == "enable")
                            { triggerPlan.setActionOff(true); }
                            else if (linkString == "disable")
                            { triggerPlan.setActionOff(false); }
                            else
                            {
                                errors.add($"Trigger {index} uses the unknown command '{linkString}' " +
                                    $"as its LinkedActionOff. Valid commands are 'enable' and 'disable'.");
                            }
                        }
                        if (triggerPlan.hasAction())
                        {
                            targetSet = evalSets[setName];
                            //Use the ActionPlan we just defined to match the state of our linked
                            //ActionSet.
                            triggerPlan.takeAction(targetSet.state);
                            targetSet.addActionPlan(triggerPlan);
                        }
                        else
                        {
                            errors.add($"Trigger {index} links to ActionSet '{setName}', but does " +
                                $"not define a valid ActionOn or ActionOff.");
                        }
                    }
                    else
                    {
                        errors.add($"Trigger {index} tried to reference the unconfigured ActionSet " +
                            $"'{setName}' as a target of LinkedActionSet.");
                    }
                }
                if (!trigger.hasScenario())
                { errors.add($"Trigger {index} does not define a valid LessOrEqual or GreaterOrEqual scenario."); }
            }
            //If we didn't get a Tally and an ActionSet, we'll need to generate a fake trigger so 
            //that evaluation can continue.
            else
            { trigger = new Trigger(null, null, log); }
        }

        private void tryGetCommandFromConfig(MyIni configReader, Trigger trigger, string targetSection,
            string prefix, int index, bool isLess, MyIniValue iniValue, LimitedErrorLog errors)
        {
            double value;
            //Check for lessOrEqual and greaterOrEqual scenarios
            iniValue = configReader.Get(targetSection, $"Trigger{index}{prefix}Value");
            if (!iniValue.IsEmpty)
            {
                value = iniValue.ToDouble();
                iniValue = configReader.Get(targetSection, $"Trigger{index}{prefix}Command");
                if (!iniValue.IsEmpty)
                {
                    string commandString = iniValue.ToString().ToLowerInvariant();
                    //Match the commandString to a boolean that Trigger will understand
                    if (commandString == "on")
                    { trigger.setScenario(isLess, value, true); }
                    else if (commandString == "off")
                    { trigger.setScenario(isLess, value, false); }
                    else if (commandString == "switch")
                    {
                        errors.add($"Trigger {index} specifies a {prefix}Command of 'switch', " +
                            $"which cannot be used for triggers.");
                    }
                    else
                    {
                        errors.add($"Trigger {index} has a missing or invalid {prefix}Command. " +
                            $"Valid commands are 'on' and 'off'.");
                    }
                }
                else
                { errors.add($"Trigger {index} specifies a {prefix}Value but no {prefix}Command."); }
            }
        }

        private IReportable tryGetReportableFromConfig(LimitedErrorLog errors, string prefix, string sectionTag,
            IMyTextSurface surface, IMyTerminalBlock block, MyIni iniReader,
            Dictionary<string, Tally> evalTallies, Dictionary<string, ActionSet> sets)
        {
            MyIniValue iniValue;
            IReportable reportable = null;
            Color color = Hammers.cozy;
            //If this is a report, it will have an 'Elements' key.
            if (iniReader.ContainsKey(sectionTag, $"{prefix}Elements"))
            {
                iniValue = iniReader.Get(sectionTag, $"{prefix}Elements");
                Report report;
                string[] elementNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                List<IHasElement> elementRefs = new List<IHasElement>();
                foreach (string name in elementNames)
                {
                    //Is this a blank slot in the report?
                    if (name.ToLowerInvariant() == "blank")
                    //Just add a null to the list. The report will know how to handle 
                    //this.
                    { elementRefs.Add(null); }
                    else
                    {
                        //If it isn't a blank, we'll need to try and get the element from 
                        //evalTallies or sets
                        if (evalTallies.ContainsKey(name))
                        { elementRefs.Add(evalTallies[name]); }
                        else if (sets.ContainsKey(name))
                        { elementRefs.Add(sets[name]); }
                        else
                        {
                            //And complain, if appropriate.
                            errors.add($"Surface provider '{block.CustomName}', {prefix}" +
                                $", tried to reference the unconfigured element " +
                                $"'{name}'.");
                        }
                    }
                }
                //Create a new report with the data we've collected so far.
                report = new Report(surface, elementRefs);
                //Now that we have a report, we need to see if the user wants anything 
                //special done with it.
                //Title
                iniValue = iniReader.Get(sectionTag, $"{prefix}Title");
                if (!iniValue.IsEmpty)
                { report.title = iniValue.ToString(); }
                //FontSize
                iniValue = iniReader.Get(sectionTag, $"{prefix}FontSize");
                if (!iniValue.IsEmpty)
                { report.fontSize = iniValue.ToSingle(); }
                //Font
                iniValue = iniReader.Get(sectionTag, $"{prefix}Font");
                if (!iniValue.IsEmpty)
                { report.font = iniValue.ToString(); }
                //Foreground color
                if (tryGetColorFromConfig(errors, ref color, prefix, "ForeColor", sectionTag,
                    iniReader, block))
                { report.foreColor = color; }
                //Background color
                if (tryGetColorFromConfig(errors, ref color, prefix, "BackColor", sectionTag,
                    iniReader, block))
                { report.backColor = color; }
                //Columns. IMPORTANT: Set anchors is no longer called during object
                //creation, and therefore MUST be called before the report is finished.
                iniValue = iniReader.Get(sectionTag, $"{prefix}Columns");
                //Call setAnchors, using a default value of 1 if we didn't get 
                //configuration data.
                report.setAnchors(iniValue.ToInt32(1));

                //We've should have all the available configuration for this report. Now we'll point
                //Reportable at it and move on.
                reportable = report;
            }
            //If this is a GameScript, it will have a 'Script' key.
            else if (iniReader.ContainsKey(sectionTag, $"{prefix}Script"))
            {
                GameScript script = new GameScript(surface,
                    iniReader.Get(sectionTag, $"{prefix}Script").ToString());
                //Foreground color
                if (tryGetColorFromConfig(errors, ref color, prefix, "ForeColor", sectionTag,
                    iniReader, block))
                { script.foreColor = color; }
                //Background color
                if (tryGetColorFromConfig(errors, ref color, prefix, "BackColor", sectionTag,
                    iniReader, block))
                { script.backColor = color; }
                //Scripts are pretty straightforward. Off to reportable with them.
                reportable = script;
            }
            //If this is a WallOText, it will have a 'DataType' key.
            else if (iniReader.ContainsKey(sectionTag, $"{prefix}DataType"))
            {
                string type = iniReader.Get(sectionTag, $"{prefix}DataType").ToString().ToLowerInvariant();
                //The broker that will store the data for this WallOText
                IHasData broker = null;

                if (type == "log")
                //Logs and Storage will not need a DataSource; there can be only one
                { broker = new LogBroker(log); }
                else if (type == "storage")
                { broker = new StorageBroker(this); }
                //CustomData, DetailInfo, and Raycasters need to have a data source
                //specified.
                //CustomData and DetailInfo both get their data from blocks.
                else if (type == "customdata" || type == "detailinfo")
                {
                    //Check to see if the user provided a DataSource
                    if (iniReader.ContainsKey(sectionTag, $"{prefix}DataSource"))
                    {
                        string source = iniReader.Get(sectionTag, $"{prefix}DataSource").ToString();
                        //Make a good faith effort to find the block the user is after.
                        IMyTerminalBlock subject = GridTerminalSystem.GetBlockWithName(source);
                        //If we found a block, and we need a CustomDataBroker
                        if (subject != null && type == "customdata")
                        { broker = new CustomDataBroker(subject); }
                        //If we found a block, and we need a DetailInfoBroker
                        else if (subject != null && type == "detailinfo")
                        { broker = new DetailInfoBroker(subject); }
                        //If we didn't find a block, complain.
                        else
                        {
                            errors.add($"Surface provider '{block.CustomName}', {prefix}, tried " +
                                $"to reference the unknown block '{source}' as a DataSource.");
                        }
                    }
                    //If there is no data source, complain.
                    else
                    {
                        errors.add($"Surface provider '{block.CustomName}', {prefix}, has a " +
                            $"DataType of {type}, but a missing or unreadable DataSource.");
                    }
                }
                //Raycasters get their data from Raycaster objects.
                else if (type == "raycaster")
                {
                    //Check to see if the user provided a DataSource
                    if (iniReader.ContainsKey(sectionTag, $"{prefix}DataSource"))
                    {
                        string source = iniReader.Get(sectionTag, $"{prefix}DataSource").ToString();
                        //Check our list of Raycasters to see if one has a matching key
                        if (raycasters.ContainsKey(source))
                        { broker = new RaycastBroker(raycasters[source]); }
                        //If we didn't find matching raycaster, complain.
                        else
                        {
                            errors.add($"Surface provider '{block.CustomName}', {prefix}, tried " +
                                $"to reference the unknown Raycaster '{source}' as a DataSource.");
                        }
                    }
                    //If there is no data source, complain.
                    else
                    {
                        errors.add($"Surface provider '{block.CustomName}', {prefix}, has a " +
                            $"DataType of {type}, but a missing or unreadable DataSource.");
                    }
                }
                else
                //If we don't recognize the DataType, complain.
                {
                    errors.add($"Surface provider '{block.CustomName}', {prefix}, tried to " +
                        $"reference the unknown data type '{type}'.");
                }
                //If we came through that with some sort of broker
                if (broker != null)
                {
                    //Create a new WallOText using our surface and the broker we've found.
                    WallOText wall = new WallOText(surface, broker, _sb);
                    //Configure any other settings that the user has seen fit to specify.
                    //FontSize
                    iniValue = iniReader.Get(sectionTag, $"{prefix}FontSize");
                    if (!iniValue.IsEmpty)
                    { wall.fontSize = iniValue.ToSingle(); }
                    //Font
                    iniValue = iniReader.Get(sectionTag, $"{prefix}Font");
                    if (!iniValue.IsEmpty)
                    { wall.font = iniValue.ToString(); }
                    //CharPerLine
                    iniValue = iniReader.Get(sectionTag, $"{prefix}CharPerLine");
                    if (!iniValue.IsEmpty)
                    //The PrepareText method that applies the charPerLine word wrap is quite 
                    //ineffecient, and I only tolerate it because most of the WoT types include some
                    //sort of mechanism that reduces the number of times it's called. Not so with 
                    //DetailInfo, which conceivably could be calling it every single update. To avoid
                    //that, and because DetailInfo is already formatted, we simply pitch a fit if
                    //the user tries to use the two in conjunction.
                    {
                        if (type != "detailinfo")
                        { wall.setCharPerLine(iniValue.ToInt32()); }
                        else
                        {
                            errors.add($"Surface provider '{block.CustomName}', {prefix}, tried to " +
                                $"set a CharPerLine limit with the DetailInfo DataType. This is not allowed.");
                        }
                    }
                    //Foreground color
                    if (tryGetColorFromConfig(errors, ref color, prefix, "ForeColor", sectionTag,
                        iniReader, block))
                    { wall.foreColor = color; }
                    //Background color
                    if (tryGetColorFromConfig(errors, ref color, prefix, "BackColor", sectionTag,
                        iniReader, block))
                    { wall.backColor = color; }
                    //Send this WallOText on its way with a fond fairwell.
                    reportable = wall;
                }
            }

            //All done? Last step is to add this report to our list of reports. So
            //we'll know where it lives.
            return reportable;
        }

        //Checks an iniReader for a key containing config data for a color. If a key is found and the
        //  color is retrieved, returns true and modifies the color reference. If no key is found, 
        //  returns false. If a key is found but can't be read, returns false and adds an error to 
        //  the errors reference.
        //string errors: The running error log
        //Color color: The color that will store any colors found
        //string prefix: The prefix of the ini key that we're looking for. This may be a surface 
        //  designation, the name of an MFD, or something new that I haven't thought of yet.
        //string target: The target of the ini key we're looking for. This will generally be 
        //  ForeColor or BackColor.
        //string sectionTag: What section of the ini we should be looking for configuration in. 
        //  Usually, this will be SCRIPT_TAG.
        //MyIni iniReader: The ini reader containing a parse of the target block's CustomData.
        //IMyTerminalBlock block: The block that this configuration is found on, for error reporting
        //  purposes.
        public bool tryGetColorFromConfig(LimitedErrorLog errors, ref Color color, string prefix,
            string target, string sectionTag, MyIni iniReader, IMyTerminalBlock block)
        {
            //For once, we will be optimistic.
            bool foundColor = true;
            MyIniValue iniValue = iniReader.Get(sectionTag, $"{prefix}{target}");
            if (!iniValue.IsEmpty)
            {
                //Split the data that we found on a comma delimiter.
                string[] elements = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                //Attempt to get the color using the provided configuration. If it fails...
                if (!Hammers.tryGetColorFromElements(ref color, elements))
                {
                    //...Well, it isn't the end of the world. If there weren't three elements, maybe
                    //the string contains one of our palette colors. We'll check the first element.
                    switch (elements[0].ToLowerInvariant())
                    {
                        case "green":
                            color = Hammers.green;
                            break;
                        case "lightblue":
                            color = Hammers.lightBlue;
                            break;
                        case "yellow":
                            color = Hammers.yellow;
                            break;
                        case "orange":
                            color = Hammers.orange;
                            break;
                        case "red":
                            color = Hammers.red;
                            break;
                        case "cozy":
                            color = Hammers.cozy;
                            break;
                        default:
                            //If it wasn't any of those, complain.
                            errors.add($"Block '{block.CustomName}', section {sectionTag}, prefix " +
                                $"{prefix}, has missing, unreadable, or unknown {target}.");
                            //Also, declare failure
                            foundColor = false;
                            break;
                    }
                }
                //If tryGetColor was successful, we don't need to do anything. foundColor is already
                //true, and our referenced color is already set.
            }
            //If a color was not found at the designated prefix and target, declare failure
            else
            { foundColor = false; }
            return foundColor;
        }

        //Returns either a common palette color name, or the RGB value of the color in question.
        public static string getStringFromColor(Color color)
        {
            if (color == Hammers.cozy)
            { return "cozy"; }
            else if (color == Hammers.green)
            { return "green"; }
            else if (color == Hammers.lightBlue)
            { return "lightBlue"; }
            else if (color == Hammers.yellow)
            { return "yellow"; }
            else if (color == Hammers.orange)
            { return "orange"; }
            else if (color == Hammers.red)
            { return "red"; }
            else
            { return $"{color.R}, {color.G}, {color.B}"; }
        }

        //An object patterened off the built-in MyIni object, RawTextIni specializes in handling 
        //configuration at the section level. While it isn't as fast as MyIni, it does preserve any
        //formatting in the config it works with.
        public class RawTextIni
        {
            //Storage for individual section headers parsed from the tryLoad method and acted upon 
            //by this object's other memthods.
            private List<string> headers;
            //Storage for individual section's contents as raw text. They are stored in the same 
            //order as their headers.
            private List<string> contents;
            //A reference to the global StringBuilder
            private StringBuilder _sb;
            //Returns the number of sections stored in this object.
            public int count { get { return headers.Count; } }
            //Returns true if the headers list contains at least one entry, false otherwise.
            public bool loaded { get { return headers.Count != 0; } }

            public RawTextIni(StringBuilder _sb)
            {
                this._sb = _sb;
                headers = new List<string>();
                contents = new List<string>();
            }

            //Loads a ini-formatted string into this object for further operations.
            //MyIni parser: An instance of the MyIni object, which will be used for error detection.
            // When the method completes, it will contain a parse of the config string passed into
            // this method, which can then be used or discarded.
            //MyIniParseResult parseResult: An instance of the MIPR object, used in conjunction with 
            // the parser object. If an error is detected while parsing, the error message can be 
            // retrieved from this variable.
            //string config: The ini-formatted string to be loaded into this object.
            //returns bool: Returns false if an error was detected by the MyIni TryParse, true otherwise.
            public bool tryLoad(MyIni parser, out MyIniParseResult parseResult, string config)
            {
                //Step one is to piggyback off the power of someone else's code.
                if (!parser.TryParse(config, out parseResult))
                { return false; }
                //Once we're reasonably certain that the config is in a readable ini format, we can
                //start loading our internal lists
                clear();
                char[] bracket = new char[] { '[' };
                //While it's true that we're about to call a split with RemoveEmptyEntries, an entry 
                //that contains a newline isn't technically empty. So we need to call a trim now so
                //that any extraneous 'entries' with no config in them don't gum up the works.
                config = config.Trim();
                //Splitting the config on open brackets will get us the section name, followed by 
                //that section's contents
                string[] rawSections = config.Split(bracket, StringSplitOptions.RemoveEmptyEntries);
                bracket = new char[] { ']' };
                string[] splitSection;
                foreach (string section in rawSections)
                {
                    //Splitting each rawSection on the closing bracket will divide the section name
                    //from the section contents.
                    splitSection = section.Split(bracket, 2);
                    headers.Add(splitSection[0]);
                    //We're getting everything from the closing bracket of one section header to the 
                    //opening bracket of the next, including existing newlines. Trim those away so 
                    //we can keep things consistant.
                    contents.Add(splitSection[1].Trim());
                }
                //We should now have the config split into easily managable sections.
                return true;
            }

            //Clear the data held by this object
            public void clear()
            {
                headers.Clear();
                contents.Clear();
            }

            //Adds a new section and its contents to the internal lists, optionally at a specific index.
            //string sectionName: The name of the new section
            //string sectionContents: The data stored in the new section
            //int index: Optionally specify where in the current order of section this section should
            // be added. If no index is specified (Or the index provided is out of bounds), the new 
            // section will be placed at the end of the list.
            public void addSection(string sectionName, string sectionContents, int index = -1)
            {
                if (index >= 0 && index < headers.Count)
                {
                    headers.Insert(index, sectionName);
                    contents.Insert(index, sectionContents);
                }
                else
                {
                    headers.Add(sectionName);
                    contents.Add(sectionContents);
                }
            }

            //Retrieve the contents of the specified section
            //string sectionName: The name of the section to be retrieved
            //out string outcome: The contents of the specified section, or an error message if the
            // section wasn't found
            //returns bool: True if the sectionName was found, false otherwise.
            public bool tryRetrieveSectionContents(string sectionName, out string outcome)
            {
                int index = headers.IndexOf(sectionName);
                if (index != -1)
                {
                    outcome = contents[index];
                    return true;
                }
                else
                {
                    outcome = "Section not found";
                    return false;
                }
            }

            //Remove the specified section and its contents from this object's lists
            //string sectionName: The name of the section to be deleted
            //returns bool: True if the section was found and deleted, false otherwise.
            public bool tryDeleteSection(string sectionName)
            {
                int index = headers.IndexOf(sectionName);
                if (index != -1)
                {
                    headers.RemoveAt(index);
                    contents.RemoveAt(index);
                    return true;
                }
                else
                { return false; }
            }

            //Replace a section's existing name with a new one
            //string sectionName: The current name of the section
            //string newName: The desired name of the section
            //returns bool: True if the designated section name was found and changed, false otherwise.
            public bool tryRenameSection(string sectionName, string newName)
            {
                int index = headers.IndexOf(sectionName);
                if (index != -1)
                {
                    headers[index] = newName;
                    return true;
                }
                else
                { return false; }
            }

            //Prints all sections and their contents in a lightly formatted string (There will be 
            //an extra line between each section).
            public string toString()
            {
                _sb.Clear();
                for (int i = 0; i < headers.Count; i++)
                {
                    _sb.Append($"[{headers[i]}]\n");
                    _sb.Append($"{contents[i]}\n\n");
                }
                string outcome = _sb.ToString();
                _sb.Clear();
                return outcome;
            }
        }

        //Refering to the distributor cap on an internal combustion engine. Hooray, metaphors.
        public class UpdateDistributor
        {
            //This value is the number of potential update tics that should be skipped between each
            //actual update. The default value is 0.
            int updateDelay;
            //The number potential update tics remaining before the next actual update 
            int delayCounter;
            //Because you can't use events in Space Engineer scripting, a reference to the EventLog
            //will need to be maintained, so we can let it know what the distributor is doing.
            Hammers.EventLog log;

            public UpdateDistributor(Hammers.EventLog log)
            {
                updateDelay = 0;
                delayCounter = 0;
                this.log = log;
            }

            public void setDelay(int delay)
            {
                updateDelay = delay;
                log.scriptUpdateDelay = delay;
                //Is this new value shorter than the ammount of time remaining on the delayCounter?
                if (delay < delayCounter)
                { delayCounter = delay; }
            }

            public bool tic()
            {
                bool fire = false;
                //Take a notch off the delayCounter
                delayCounter--;
                //If we've reached the end of the delay counter...
                if (delayCounter < 0)
                {
                    //Reset the counter to the value dictated by the updateDelay
                    delayCounter = updateDelay;
                    //It's time to perform an update. Set 'fire' to true.
                    fire = true;
                }
                return fire;
            }
        }

        public class LimitedErrorLog
        {
            //A reference to the global StringBuilder
            StringBuilder _sb;
            //The maximum number of errors this log will accept.
            int maxErrors;
            //A list, holding our various error strings
            List<string> errors;
            //A counter, keeping track of the number of errors we've received in excess of maxErrors
            int overflowCounter;

            public LimitedErrorLog(StringBuilder _sb, int maxErrors)
            {
                this._sb = _sb;
                this.maxErrors = maxErrors;
                errors = new List<string>();
                overflowCounter = 0;
            }

            //Add an error to the log, or increment the overflow counter if we've already got too 
            //many errors.
            public void add(string error)
            {
                if (errors.Count < maxErrors)
                { errors.Add(error); }
                else
                { overflowCounter++; }
            }

            //Return the total number of errors encountered by this log.
            public int getErrorTotal()
            { return errors.Count + overflowCounter; }

            //Get the contents of the log
            public string toString()
            {
                string log;
                _sb.Clear();
                foreach (string entry in errors)
                { _sb.Append($" -{entry}\n"); }
                if (overflowCounter > 0)
                { _sb.Append($" -And {overflowCounter} other errors.\n"); }
                log = _sb.ToString();
                _sb.Clear();
                return log;
            }
        }

        //Interface used by things that need to be displayed by a report. Mostly tallies, but also
        //ActionSets
        public interface IHasElement
        {
            //The text of the element
            string assembleElementStack();
            string assembleElementLine();
            //A possible element type that would represent an arrangement specific to this element 
            //type
            //string assembleElementBespoke();
            //The color of the element
            Color statusColor { get; }
        }

        public abstract class Tally : IHasElement
        {
            //The name that this tally will display on reports. Uses the program name unless 
            //explicitly set in the config.
            public string displayName { get; set; }
            //The program only knows the true name of a tally for the duration of evaluation. After
            //that, it's forgotten because we know where it is in an array and because the displayName
            //is the thing that's relevent most of the time. But the writeConfig method needs to know
            //the true name, so we'll just lug it around with us.
            internal string programName { get; private set; }
            //Sometimes, the data we get from blocks doesn't match up with what we see or expect
            //from the in-game interfaces (For instance, the volume measurements we get from
            //invetories are in kilo-liters, but in-game we see them in liters). This multiplier
            //will be applied to the curr and max values calculated by this tally, letting you 
            //adjust the scales to your liking.
            public double multiplier { protected get; set; }
            //The current value of this tally, as adjusted by addInventoryToCurr
            public double curr { get; protected set; }
            //The maximum value of this tally, usually set shortly after object creation.
            public double max { get; protected set; }
            //A flag, indicating if this tally has had its maximum forced to a certain value.
            //FAT: This could be done by setting Max to a negative number when forceMax() is called,
            //ignoring calls to incrementMax() while the number is negative, and then flipping the 
            //sign in finishSetup(). That would save me the one whole bit that this bool carries
            //around with it, un-needed beyond evaluation.
            protected bool maxForced;
            //A flag that will be set if this tally should be ignored by the Reconstitute method. 
            //Currently only used for Raycasters.
            internal bool doNotReconstitute;
            //How 'full' this tally is, as measured by the curr against the max. Shold be between
            //0 and 100
            public double percent { get; protected set; }
            //A representation of the current value of this tally, in a readable string format. It
            //doesn't get any fancy getters or setters, because apparently that causes trouble with
            //passing by reference in the inheriting objects.
            protected string readableCurr;
            //A readable string format for the maximum of this tally.
            protected string readableMax;
            //A reference to the global script object that will build our ASCII meters for us. 
            protected Hammers.MeterMaid meterMaid;
            //Stores the last meter received by this tally.
            internal string meter;
            //A color code for this tally, based on the percentage.
            public Color statusColor { get; protected set; }
            //The function we use to figure out what color to associate with the current value of 
            //this tally. Will be set to either handleColorCodeLow or handleColorCodeHigh.
            protected Func<double, Color> colorHandler { get; private set; }

            public Tally(Hammers.MeterMaid meterMaid, string name, bool isLow = false, double multiplier = 1)
            {
                this.meterMaid = meterMaid;
                programName = name;
                displayName = name;
                lowGood(isLow);
                this.multiplier = multiplier;
                curr = 0;
                max = 0;
                maxForced = false;
                doNotReconstitute = false;
                percent = 0;
                readableCurr = "curr";
                readableMax = "max";
                meter = "[----------]";
                statusColor = Hammers.cozy;
            }

            //Set the color code mode
            internal void lowGood(bool isLow)
            {
                if (isLow)
                { colorHandler = Hammers.handleColorCodeLow; }
                else
                { colorHandler = Hammers.handleColorCodeHigh; }
            }

            //Get the meter
            internal string getMeter()
            { return meter; }

            //Get the readableCurr
            internal string getReadableCurr()
            { return readableCurr; }

            //Get the readableMax
            internal string getReadableMax()
            { return readableMax; }

            //Arrange this tally's information into a reportable-friendly vertical element
            public string assembleElementStack()
            { return $"{displayName}\n{readableCurr} / {readableMax}\n{meter}"; }

            //Arrange this tally's information into a reportable-friendly horizontal element
            //The arrangement is: A left-aligned space measuring 12 characters for the name, another,
            //similar space for the ratio and, lastly, the meter.
            //TODO: Figure out a circumstance in which to test this
            public string assembleElementLine()
            { return $"{displayName,-12}{($"{readableCurr} / {readableMax}"),-12}{meter}"; }

            //Because of the way the data is arranged, Tally has to be told when it has all of its
            //data.
            internal abstract void finishSetup();

            internal void clearCurr()
            { curr = 0; }
            /*
            //Increment the max by the value being passed in. Generally called when a block is added
            //to the tally, directly or indirectly. Has no effect if this Tally has already had its
            //max forced to be a certain value.
            internal void incrementMax(double val)
            {
                if (!maxForced)
                { max += val; }
            }
            */
            //Set the max to the value being passed in, and ignore any future attempts to increment
            //the max.
            //IMPORTANT: If a multiplier is going to be set for a tally, it MUST be done before this
            //is called.
            internal void forceMax(double val)
            {
                //max = val;
                //For better compatability with AutoPopulte, we'll apply the multiplier as we set
                //the max.
                max = val * multiplier;
                maxForced = true;
            }

            //compute compiles and analyzes data from this tally's subject blocks, then builds the 
            //strings and sets the colors needed to display that data
            internal abstract void compute();

            //writeConfig generates an ini config for this tally based on the information that was
            //passed into it at object creation.
            internal abstract string writeConfig(int index);
        }

        public class TallyGeneric : Tally
        {
            //TallyGenerics now store their information in a bespoke object that also has methods
            //for dealing with that block type.
            internal ITallyGenericHandler handler;

            //TallyGeneric is quite similar to the regular Tally externally, only requiring a 
            //handler to be passed in alongside the name. We need to do a bit of work internally,
            //however.
            public TallyGeneric(Hammers.MeterMaid meterMaid, string name, ITallyGenericHandler handler,
                bool isLow = false, double multiplier = 1) : base(meterMaid, name, isLow, multiplier)
            {
                this.handler = handler;
            }

            internal bool tryAddBlock(IMyTerminalBlock block)
            { return handler.tryAddBlock(block); }

            internal string getTypeAsString()
            { return handler.writeConfig(); }

            internal override void finishSetup()
            {
                //TallyGeneric doesn't keep a running tally of its max as it goes along. Instead,
                //max is calculated when we're sure we have all our blocks.
                if (!maxForced)
                { max = handler.getMax() * multiplier; }
                //Max will never change unless re-initialized. So we'll figure out what readableMax
                //is once and just hold on to it.
                Hammers.readableInt(ref readableMax, (int)max);
            }

            //Using curr and max, derive the remaining components needed to form a Report. Unlike
            //TallyCargo, we can compute curr from this method
            internal override void compute()
            {
                if (max != 0)
                {
                    //Get the current value from the handler
                    curr = handler.getCurr();
                    //First thing we need to do is apply the multiplier.
                    curr = curr * multiplier;
                    //Now for the percent. We'll need it for everything else. But things will get
                    //weird if it's more than 100.
                    percent = Math.Min(curr / max, 100) * 100;
                    //... Things will also get weird if it's not actually a number.
                    //NOTE: Now handled by 'if max != 0'
                    //percent = Double.IsNaN(percent) ? 0 : percent;
                    //Next, get the color code from our color handler. 
                    statusColor = colorHandler(percent);
                    //Now, the meter.
                    meterMaid.getMeter(ref meter, percent);
                    //Last, we want to show curr as something you can actually read.
                    Hammers.readableInt(ref readableCurr, (int)curr);
                }
            }

            internal override string writeConfig(int index)
            {   //name type max? displayName multiplier lowgood
                //Default values for this config. 
                double DEFAULT_MULTIPLIER = 1;

                string config = $"Tally{index}Name = {newLineToMultiLine(programName)}\n";
                //Getting the type used to be involved. Now it's cake.
                config += $"Tally{index}Type = {handler.writeConfig()}\n";
                //Moving on to Max
                if (maxForced)
                {
                    //If a multiplier has been set, we'll need to take that into account.
                    if (multiplier != DEFAULT_MULTIPLIER)
                    { config += $"Tally{index}Max = {max / multiplier}\n"; }
                    else
                    { config += $"Tally{index}Max = {max}\n"; }
                }
                if (displayName != programName)
                { config += $"Tally{index}DisplayName = {newLineToMultiLine(displayName)}\n"; }
                if (multiplier != DEFAULT_MULTIPLIER)
                { config += $"Tally{index}Multiplier = {multiplier}\n"; }
                //Low numbers are only good for cargo tallies. We expect generics to use 
                //handleColorCodeHigh.
                if (colorHandler == Hammers.handleColorCodeLow)
                { config += $"Tally{index}LowGood = true\n"; }

                return config;
            }
        }

        public class TallyCargo : Tally
        {
            //The only change to the constructor that TallyCargo needs is setting the default of 
            //isLow to 'true'
            public TallyCargo(Hammers.MeterMaid meterMaid, string name, bool isLow = true, double multiplier = 1)
                : base(meterMaid, name, isLow, multiplier)
            { }

            //Increment the max by the value being passed in. Generally called when a block is added
            //to the tally, directly or indirectly. Has no effect if this Tally has already had its
            //max forced to be a certain value.
            internal void incrementMax(double val)
            {
                if (!maxForced)
                { max += val; }
            }

            internal override void finishSetup()
            {
                //Apply the multiplier to the max.
                //max = max * multiplier;
                //Apply the multiplier to the max, if the max hasn't already been forced. This should
                //help keep AutoPopulate happy.
                if (!maxForced)
                { max = max * multiplier; }
                //Max will never change unless re-initialized. So we'll figure out what readableMax
                //is once and just hold on to it.
                Hammers.readableInt(ref readableMax, (int)max);
            }

            //Take an inventory and see how full it currently is.
            internal virtual void addInventoryToCurr(IMyInventory inventory)
            { curr += (double)inventory.CurrentVolume; }

            //Using curr (Which is determined by calling calculateCurr() on all Containers associated
            //with this tally) and max (Which should already be set long before you consider calling
            //this), derive the remaining components needed to form a Report
            internal override void compute()
            {
                if (max != 0)
                {
                    //First thing we need to do is apply the multiplier.
                    curr = curr * multiplier;
                    //Now for the percent. We'll need it for everything else. But things will get
                    //weird if it's more than 100.
                    percent = Math.Min(curr / max, 100) * 100;
                    //... Things will also get weird if it's not actually a number.
                    //percent = Double.IsNaN(percent) ? 0 : percent;
                    //Next, get the color code from our color handler. 
                    statusColor = colorHandler(percent);
                    //Now, the meter.
                    meterMaid.getMeter(ref meter, percent);
                    //Last, we want to show curr as something you can actually read.
                    Hammers.readableInt(ref readableCurr, (int)curr);
                }
            }

            internal override string writeConfig(int index)
            {   //name type max? displayName multiplier lowgood
                //Default values for this config. 
                double DEFAULT_MULTIPLIER = 1;

                string config = $"Tally{index}Name = {newLineToMultiLine(programName)}\n";
                //Getting the tally type for a CargoTally isn't much of a production.
                config += $"Tally{index}Type = Inventory\n";
                if (maxForced)
                { config += $"Tally{index}Max = {max}\n"; }
                if (displayName != programName)
                { config += $"Tally{index}DisplayName = {newLineToMultiLine(displayName)}\n"; }
                if (multiplier != DEFAULT_MULTIPLIER)
                { config += $"Tally{index}Multiplier = {multiplier}\n"; }
                //We expect TallyCargos to use ColorCodeLow. If this one doesn't, we'll need a
                //config entry.
                if (colorHandler != Hammers.handleColorCodeLow)
                { config += $"Tally{index}LowGood = false\n"; }

                return config;
            }
        }

        public class TallyItem : TallyCargo
        {
            //The item type that this tally will look for in inventories.
            MyItemType itemType;

            //TallyItems need a bit more data, so they'll know what kind of item they're looking
            //for. You can also set the max from the constructor, though I've stopped doing that.
            public TallyItem(Hammers.MeterMaid meterMaid, string name, string typeID, string subTypeID, double max = 0,
                bool isLow = false, double multiplier = 1) : base(meterMaid, name, isLow, multiplier)
            {
                itemType = new MyItemType(typeID, subTypeID);
                forceMax(max);
            }

            //Take an inventory and see how much of the itemType is in it.
            internal override void addInventoryToCurr(IMyInventory inventory)
            { curr += (double)inventory.GetItemAmount(itemType); }

            internal override string writeConfig(int index)
            {   //name type max? displayName multiplier lowgood
                //Default values for this config. 
                double DEFAULT_MULTIPLIER = 1;

                string config = $"Tally{index}Name = {newLineToMultiLine(programName)}\n";
                //Like CargoTallies, we don't need to do a lot of soul searching to get the type of
                //an item tally.
                config += $"Tally{index}Type = Item\n";
                //ItemTallies are required to have these things, so we won't even ask.
                config += $"Tally{index}ItemTypeID = {itemType.TypeId}\n";
                config += $"Tally{index}ItemSubTypeID = {itemType.SubtypeId}\n";
                config += $"Tally{index}Max = {max}\n";
                if (displayName != programName)
                { config += $"Tally{index}DisplayName = {newLineToMultiLine(displayName)}\n"; }
                if (multiplier != DEFAULT_MULTIPLIER)
                { config += $"Tally{index}Multiplier = {multiplier}\n"; }
                //Item tallies are expected to use handleColorCodeHigh.
                if (colorHandler == Hammers.handleColorCodeLow)
                { config += $"Tally{index}LowGood = true\n"; }

                return config;
            }
        }

        //Stores a block inventory and all of the tallies that inventory is to be reported to. Also
        //has a couple of small methods that aids CargoManager in calculating the curr and max of 
        //its tallies. 
        public class Container
        {
            IMyInventory inventory;
            TallyCargo[] tallies;

            public Container(IMyInventory inventory, TallyCargo[] tallies)
            {
                this.inventory = inventory;
                //Pull the array out of the list that we were given.
                this.tallies = tallies;
            }

            public void sendMaxToTallies()
            {
                //For every tally associated with this Container...
                foreach (TallyCargo tally in tallies)
                {
                    //TallyItems have their maximums set manually. We only need to concern 
                    //ourselves will regular tallies.
                    //TODO: Monitor. I disabled this, because there should be logic in place in the
                    //TallyItem for handling it.
                    //if (!(tally is TallyItem))
                    //Add the container's maximum volume to the tally's max
                    { tally.incrementMax((double)inventory.MaxVolume); }
                }
            }

            public void sendCurrToTallies()
            {
                //For every tally associated with this Container...
                foreach (TallyCargo tally in tallies)
                //Add the container's current volume to the tally's curr
                { tally.addInventoryToCurr(inventory); }
            }
        }

        public class Trigger
        {
            //The Tally object this Trigger will watch
            Tally targetTally;
            //The ActionSet this Trigger will operate
            ActionSet targetSet;
            //The thresholds that will activate the greater and less than commands, respectively
            double greaterOrEqual, lessOrEqual;
            //The commands that will be sent when the respective threshold is reached
            bool commandGreater, commandLess;
            //A flag indicating that the respective threshold has been configured
            bool hasGreater, hasLess;
            //A flag indicating if this Trigger should be running checks at the moment
            internal bool enabled;
            //A reference to the log
            Hammers.EventLog log;
            //The raw configuration that determines which ActionSets enable or disable this trigger.
            //These variables are only used by the writeConfig method.
            //DEPRECEATED
            //internal string setsEnabled { private get; set; }
            //internal string setsDisabled { private get; set; }

            public Trigger(Tally targetTally, ActionSet targetSet, Hammers.EventLog log)
            {
                this.targetTally = targetTally;
                this.targetSet = targetSet;
                this.log = log;
                //DEPRECEATED
                //setsEnabled = "";
                //setsDisabled = "";
                greaterOrEqual = -1;
                lessOrEqual = -1;
                commandGreater = false;
                commandLess = false;
                hasGreater = false;
                hasLess = false;
                enabled = true;
            }

            //Define one of this trigger's scenarios
            public void setScenario(bool isLess, double value, bool command)
            {
                if (isLess)
                {
                    lessOrEqual = value;
                    commandLess = command;
                    hasLess = true;
                }
                else
                {
                    greaterOrEqual = value;
                    commandGreater = command;
                    hasGreater = true;
                }
            }

            //Chcks to see if one of the configured thresholds has been reached
            public void check()
            {
                //If the trigger is currently enabled...
                if (enabled)
                {
                    //If our Greater command is configured, our set isn't already in the Greater 
                    //state, and we have exceeded our threshold...
                    if (hasGreater && targetSet.state != commandGreater && targetTally.percent >= greaterOrEqual)
                    { tryTakeAction(commandGreater); }
                    else if (hasLess && targetSet.state != commandLess && targetTally.percent <= lessOrEqual)
                    { tryTakeAction(commandLess); }
                }
            }

            //Operate the ActionSet that this Trigger is tied to.
            private void tryTakeAction(bool command)
            {
                try
                { targetSet.setState(command); }
                catch (InvalidCastException e)
                {
                    string identifier = "<ID not provided>";
                    if (e.Data.Contains("Identifier"))
                    { identifier = $"{e.Data["Identifier"]}"; }
                    log.add($"An invalid cast exception occurred while running a triggered command " +
                        $"for ActionSet '{targetSet.displayName}' at {identifier}. Make sure " +
                        $"the action specified in configuration can be performed by {identifier}.");
                }
                catch (Exception e)
                {
                    string identifier = "<ID not provided>";
                    if (e.Data.Contains("Identifier"))
                    { identifier = $"{e.Data["Identifier"]}"; }
                    log.add($"An exception occurred while running a triggered command for " +
                        $"ActionSet '{targetSet.displayName}' at {identifier}.\n  Raw exception " +
                        $"message:\n{e.Message}\n  Stack trace:\n{e.StackTrace}");
                }
            }

            //Determines if this trigger has at least one scenario configured.
            public bool hasScenario()
            { return hasGreater || hasLess; }

            //Returns a string identifier for use in error messages. Right now, this returns the 
            //name of the linked Tally and ActionSet. But if I end up implementing programNames for
            //these, I could just use that instead.
            public string getIdentifier()
            {
                return $"trigger linked to Tally '{targetTally.programName}' and " +
                    $"ActionSet '{targetSet.programName}'";
            }

            public string writeConfig(int index, ref Dictionary<Trigger, MyTuple<string, ActionPlanTrigger>> triggerPlans)
            {
                //Default values for this config. 
                //DEPRECEATED
                //string DEFAULT_SETS_ENABLED = "";
                //string DEFAULT_SETS_DISABLED = "";
                string config = $"Trigger{index}Tally = {targetTally.programName}\n";
                config += $"Trigger{index}ActionSet = {targetSet.programName}\n";
                //Consult the two 'has' flags to see if we have config for this scenario.
                if (hasLess)
                {
                    string commandAsString = commandLess ? "on" : "off";
                    config += $"Trigger{index}LessOrEqualValue = {lessOrEqual}\n";
                    //Inline statements are a fickle beast. Leave off the parentheses here and it'll
                    //pitch a fit.
                    config += $"Trigger{index}LessOrEqualCommand = {(commandLess ? "on" : "off")}\n";
                }
                if (hasGreater)
                {
                    config += $"Trigger{index}GreaterOrEqualValue = {greaterOrEqual}\n";
                    config += $"Trigger{index}GreaterOrEqualCommand = {(commandGreater ? "on" : "off")}\n";
                }

                //DEPRECEATED
                //Knowing if we have config for enabled/disabled by is as simple as checking to see
                //if we've been lugging strings around this whole time.
                //if (setsEnabled != DEFAULT_SETS_ENABLED)
                //{ config += $"Trigger{index}EnabledBySets = {setsEnabled}\n"; }
                //if (setsDisabled != DEFAULT_SETS_DISABLED)
                //{ config += $"Trigger{index}DisabledBySets = {setsDisabled}\n"; }

                //Last step is to write config for ActionSets that turn this trigger on and off. That
                //information is in the dictionary we retrieved from ActionSet.writeConfig
                if (triggerPlans.ContainsKey(this))
                {
                    config += $"Trigger{index}LinkedActionSet = {triggerPlans[this].Item1}\n";
                    ActionPlanTrigger triggerPlan = triggerPlans[this].Item2;
                    if (triggerPlan.hasOn)
                    { config += $"Trigger{index}LinkedActionOn = {(triggerPlan.actionOn ? "enable" : "disable")}\n"; }
                    if (triggerPlan.hasOff)
                    { config += $"Trigger{index}LinkedActionOff = {(triggerPlan.actionOff ? "enable" : "disable")}\n"; }
                }
                return config;
            }
        }

        //ActionPlans only need one thing in common. Or at least, they did. Now we're getting all 
        //sensitive about things, and we need to know how to ask them if they have an action. And 
        //now we card them.
        public interface IHasActionPlan
        {
            void takeAction(bool isOnAction);
            bool hasAction();
            string getIdentifier();
        }

        //Stores a binary set of actions for a specific terminal block
        public class ActionPlanBlock : IHasActionPlan
        {
            //The TerminalBlock this ActionPlan will be manipulating
            IMyTerminalBlock subjectBlock;
            //The action to be performed on the subject block when the ActionPlan is switched on
            internal Action<IMyTerminalBlock> actionOn { get; set; }
            //The action to be performed on the subject block when the ActionPlan is switched off
            internal Action<IMyTerminalBlock> actionOff { get; set; }

            public ActionPlanBlock(IMyTerminalBlock subject)
            {
                this.subjectBlock = subject;
                actionOn = null;
                actionOff = null;
            }

            //Execute one of the actions associated with this ActionPlan on the subject block. The
            //action that will be taken depends on the boolean being passed in: true causes the 
            //actionOn, false causes the actionOff.
            public void takeAction(bool isOnAction)
            {
                if (isOnAction)
                //MONITOR: Visual studio seems to think I can do this. Normally, I would've checked
                //for a null
                { actionOn?.Invoke(subjectBlock); }
                else
                { actionOff?.Invoke(subjectBlock); }
            }

            //Determine if this ActionPlan has any actions defined
            public bool hasAction()
            { return actionOn != null || actionOff != null; }

            public string getIdentifier()
            { return $"Block '{subjectBlock.CustomName}'"; }
        }

        //Stores a terminal block and a list of ActionParts that will be executed on that block when
        //takeAction is invoked.
        public class ActionPlanTerminal : IHasActionPlan
        {
            //The TerminalBlock this ActionPlan will be manipulating
            IMyTerminalBlock subjectBlock;
            //The ActionParts that store the properties and values that will be applied when 
            //takeAction is called.
            private List<ActionPart> actions;

            public ActionPlanTerminal(IMyTerminalBlock subject)
            {
                this.subjectBlock = subject;
                actions = new List<ActionPart>();
            }

            public void addPart(ActionPart part)
            { actions.Add(part); }

            //Execute one set of the actions associated with this ActionPlan on the subject block. 
            //The actions that will be taken depends on the boolean being passed in: true causes 
            //the actionsOn, false causes the actionsOff.
            public void takeAction(bool isOnAction)
            {
                foreach (ActionPart part in actions)
                { part.takeAction(subjectBlock, isOnAction); }
            }

            //Determine if this ActionPlan has any actions defined
            public bool hasAction()
            { return actions.Count != 0; }

            public string getIdentifier()
            { return $"Block '{subjectBlock.CustomName}'"; }
        }

        //A bit of heresy that makes a genericly typed ActionPart work in a list.
        //Found on: https://stackoverflow.com/questions/353126/c-sharp-multiple-generic-types-in-one-list
        public abstract class ActionPart
        {
            public abstract bool isHealthy();
            public abstract Type getPropertyType();
            public abstract void takeAction(IMyTerminalBlock block, bool isOnAction);
        }

        //Stores the type, propertyID, and values of an individual ActionPart, and allows SetValue to
        //be run.
        public class ActionPart<T> : ActionPart
        {
            //The property this ActionPart will target
            string propertyID;
            //The value this ActionPart will apply when action is taken, with both on and off versions.
            private T valueOn, valueOff;
            //Because we may be dealing with primitives, we can't rely on null to tell us if we've 
            //received a value.
            private bool hasOn, hasOff;

            public ActionPart(string propertyID)
            {
                this.propertyID = propertyID;
                hasOn = false;
                hasOff = false;
            }

            public void setValueOn(T value)
            {
                valueOn = value;
                hasOn = true;
            }

            public void setValueOff(T value)
            {
                valueOff = value;
                hasOff = true;
            }

            public override bool isHealthy()
            { return hasOn || hasOff; }

            public override Type getPropertyType()
            { return typeof(T); }

            public override void takeAction(IMyTerminalBlock block, bool isOnAction)
            {
                if (isOnAction && hasOn)
                { block.SetValue<T>(propertyID, valueOn); }
                else if (!isOnAction && hasOff)
                { block.SetValue<T>(propertyID, valueOff); }
            }
        }

        //Stores a binary set of MFD pages for a specific MFD
        public class ActionPlanMFD : IHasActionPlan
        {
            //The MFD this ActionPlan will be manipulating
            MFD subjectMFD;
            //Which page to switch to when the ActionPlan is switched on
            internal string pageOn { private get; set; }
            //Which page to switch to when the ActionPlan is switched off
            internal string pageOff { private get; set; }

            public ActionPlanMFD(MFD subject)
            {
                this.subjectMFD = subject;
                //Instansiate pageOn and pageOff to empty strings, because dictionaries apparently
                //don't like it when you ask them to find entry null #WisdomDispensed
                pageOn = "";
                pageOff = "";
            }

            //Sets the page of subject MFD to one of the stored pages. Passing in true will activate
            //the pageOn, false will activate pageOff.
            public void takeAction(bool isOnAction)
            {
                if (isOnAction)
                { subjectMFD.trySetPage(pageOn); }
                else
                { subjectMFD.trySetPage(pageOff); }
            }

            //Determine if this ActionPlan has any actions defined
            public bool hasAction()
            { return !String.IsNullOrEmpty(pageOn) || !String.IsNullOrEmpty(pageOff); }

            public string getIdentifier()
            { return "Some MFD (Sorry, MFDs are supposed to work)"; }
        }

        //Stores two possible updateDelays for the UpdateDistributor.
        public class ActionPlanUpdate : IHasActionPlan
        {
            //A reference to the script's update distributor, ie, the thing we'll be manipulating.
            UpdateDistributor distributor;
            //How long the delay will be when this ActionPlan is on
            public int delayOn { get; internal set; }
            //How long the delay will be when this ActionPlan is off
            public int delayOff { get; internal set; }

            public ActionPlanUpdate(UpdateDistributor distributor)
            {
                this.distributor = distributor;
                delayOn = 0;
                delayOff = 0;
            }

            //Sets the delay on the distributor to one of the stored delay times. Passing in true 
            //will set the delay to delayOn, false will set it to delayOff.
            public void takeAction(bool isOnAction)
            {
                if (isOnAction)
                { distributor.setDelay(delayOn); }
                else
                { distributor.setDelay(delayOff); }
            }

            //Determine if this ActionPlan has any actions defined (This will probably never be called)
            public bool hasAction()
            { return delayOn != 0 || delayOff != 0; }

            public string getIdentifier()
            { return "The Distributor"; }
        }

        //Stores and manipulates a Raycaster for an ActionSet
        public class ActionPlanRaycast : IHasActionPlan
        {
            //The Raycaster this ActionPlan will use for its scans
            Raycaster subjectRaycaster;
            //Will a scan be performed when this ActionPlan is switched on?
            internal bool scanOn { private get; set; }
            //Will a scan be performed when this ActionPlan is switched off?
            internal bool scanOff { private get; set; }

            public ActionPlanRaycast(Raycaster subject)
            {
                this.subjectRaycaster = subject;
                scanOn = false;
                scanOff = false;
            }

            //Run a scan with this raycaster, if that's what it's supposed to do for this state.
            //Passing in true will run a scan if scanOn is true, false will run a scan if scanOff
            //is true.
            public void takeAction(bool isOnAction)
            {
                if (isOnAction)
                {
                    if (scanOn)
                    { subjectRaycaster.scan(); }
                }
                else
                {
                    if (scanOff)
                    { subjectRaycaster.scan(); }
                }
            }

            //Determine if this ActionPlan has any actions defined
            public bool hasAction()
            { return scanOn || scanOff; }

            public string getIdentifier()
            { return $"Raycaster tied to camera '{subjectRaycaster.camera.CustomName}'"; }
        }

        //Stores a trigger and the information needed to manipulate it on behalf of an ActionSet.
        public class ActionPlanTrigger : IHasActionPlan
        {
            //The Trigger this ActionPlan is manipulating
            internal Trigger subjectTrigger;
            //The booleans that store what action should be taken when this plan is invoked. A value
            //of 'true' means the linked Trigger will be enabled; a value of 'false' will disable it.
            internal bool actionOn, actionOff;
            //Booleans that indicate if an On or Off action have actually been configured for this 
            //plan.
            internal bool hasOn, hasOff;

            public ActionPlanTrigger(Trigger subject)
            {
                this.subjectTrigger = subject;
                actionOn = false;
                actionOff = false;
                hasOn = false;
                hasOff = false;
            }

            public void setActionOn(bool action)
            {
                actionOn = action;
                hasOn = true;
            }

            public void setActionOff(bool action)
            {
                actionOff = action;
                hasOff = true;
            }

            //Enable or disable the link trigger, based on the configuration.
            public void takeAction(bool isOnAction)
            {
                if (isOnAction)
                {
                    //If an 'on' action is defined...
                    if (hasOn)
                    { subjectTrigger.enabled = actionOn; }
                }
                else
                {
                    if (hasOff)
                    { subjectTrigger.enabled = actionOff; }
                }
            }

            //Determine if this ActionPlan has any actions defined
            public bool hasAction()
            { return hasOn || hasOff; }

            //Get an identifier to make our error messages more helpful
            public string getIdentifier()
            { return subjectTrigger.getIdentifier(); }
        }

        //Stores strings that will be sent by the IGC when this ActionPlan is set.
        public class ActionPlanIGC : IHasActionPlan
        {
            //A reference to the IGC
            IMyIntergridCommunicationSystem IGC;
            //The channel our messages will be sent on.
            internal string channel { get; set; }
            //The messages we'll be sending when our state changes.
            internal string messageOn { get; set; }
            internal string messageOff { get; set; }

            public ActionPlanIGC(IMyIntergridCommunicationSystem IGC, string channel)
            {
                this.IGC = IGC;
                this.channel = channel;
                messageOn = "";
                messageOff = "";
            }

            //Send the appropriate IGC message
            public void takeAction(bool isOnAction)
            {
                if (isOnAction)
                { IGC.SendBroadcastMessage(channel, messageOn); }
                else
                { IGC.SendBroadcastMessage(channel, messageOff); }
            }

            //Determine if this ActionPlan has any actions defined
            public bool hasAction()
            { return !String.IsNullOrEmpty(messageOn) || !String.IsNullOrEmpty(messageOff); }

            public string getIdentifier()
            { return $"IGC on channel '{channel}'"; }
        }

        public class ActionSet : IHasElement
        {
            //The list of ActionPlan objects that makes up this ActionSet
            List<IHasActionPlan> actionPlans;
            //The name of the ActionSet, which will be displayed in its Element
            internal string displayName { get; set; }
            //The true name of this ActionSet, stored primarily to be used in writeConfig.
            internal string programName { get; private set; }
            //The state of the ActionSets, which is used to determine how it will be displayed and
            //what set of actions it will take next.
            internal bool state { get; private set; }
            //Colors used to represent each possible state of the ActionSet
            internal Color colorOn { private get; set; }
            internal Color colorOff { private get; set; }
            //The color that represents the current state of the ActionSet
            public Color statusColor { get; private set; }
            //Strings used to represent each possible state of the ActionSet
            internal string textOn { private get; set; }
            internal string textOff { private get; set; }
            //The string that represents the current state of the ActionSet
            string statusText;

            //NOTE: the ability to pass a state in here is for the eventual Save() implementation.
            public ActionSet(string name, bool state)
            {
                actionPlans = new List<IHasActionPlan>();
                displayName = name;
                programName = name;
                this.state = state;
                //Again, I can't have default values for colors passed in through the constructor,
                //so I'm just setting them here.
                colorOn = Hammers.green;
                colorOff = Hammers.red;
                //DEPREACEATED: Does this work? It should set the statusColor based on the state.
                //Actually, I'm going to be setting two things like this, so I'll just do an if
                //block and make the check once.
                //statusColor = state? onColor:offColor;
                textOn = "Enabled";
                textOff = "Disabled";
                evaluateStatus();
            }

            //Choose a statusColor and a statusText, based on the current value of the 'state' variable.
            private void evaluateStatus()
            {
                if (state)
                {
                    statusColor = colorOn;
                    statusText = textOn;
                }
                else
                {
                    statusColor = colorOff;
                    statusText = textOff;
                }
            }

            //Add a new action plan to this ActionSet
            public void addActionPlan(IHasActionPlan plan)
            {
                //Because we need them for writeConfig, we make sure PB-based ActionPlans are at the 
                //top of the list.
                if (plan is ActionPlanUpdate || plan is ActionPlanIGC || plan is ActionPlanTrigger)
                { actionPlans.Insert(0, plan); }
                else
                { actionPlans.Add(plan); }
            }

            //Switch this ActionSet to its alternate state. The heavy lifting is done by the 
            //setState method.
            public void switchState()
            { setState(!state); }

            //Set this ActionSet to a given state, performing all associated actions and updating 
            //statusColor and statusText.
            public void setState(bool newState)
            {
                state = newState;
                foreach (IHasActionPlan plan in actionPlans)
                {
                    //This try block is intended to catch exceptions caused by using an action 
                    //handler on a block that doesn't match the type of that handler.
                    try
                    { plan.takeAction(state); }
                    catch (Exception e)
                    {
                        e.Data.Add("Identifier", plan.getIdentifier());
                        throw;
                    }
                }
                evaluateStatus();
            }

            //A vertical arrangement of the element representing this ActionSet's current state
            public string assembleElementStack()
            { return $"{displayName}\n{statusText}"; }

            //A horizontal arrangement of the element representing this ActionSet's current state
            public string assembleElementLine()
            //Lines on tallies are 38 characters across (12 for name, 12 for ratio, 12 for meter,
            //plus two guaranteed spaces seperating each component). To keep things consistant, 
            //we'll allocate 19 characters for an ActionSet name and 18 for its status, plus one
            //space of guaranteed seperation between the two.
            { return $"{displayName,-19} {statusText,18}"; }

            public string writeConfig(int index, ref Dictionary<Trigger, MyTuple<string, ActionPlanTrigger>> triggerPlans)
            {
                //Default values for this config. 
                Color DEFAULT_COLOR_ON = Hammers.green;
                Color DEFAULT_COLOR_OFF = Hammers.red;
                string DEFAULT_TEXT_ON = "Enabled";
                string DEFAULT_TEXT_OFF = "Disabled";
                //I can't imagine this ever being anything other than 0, but we may as well be 
                //consistant.
                int DEFAULT_DELAY_ON = 0;
                int DEFAULT_DELAY_OFF = 0;
                //Likewise, I can't imagine these being anything other than empty strings.
                string DEFAULT_IGC_CHANNEL = "";
                string DEFAULT_IGC_MESSAGE_ON = "";
                string DEFAULT_IGC_MESSAGE_OFF = "";
                string config = $"Action{index}Name = {newLineToMultiLine(programName)}\n";
                //If we aren't using a default, we'll need to write some config.
                if (displayName != programName)
                { config += $"Action{index}DisplayName = {newLineToMultiLine(displayName)}\n"; }
                if (colorOn != DEFAULT_COLOR_ON)
                { config += $"Action{index}ColorOn = {getStringFromColor(colorOn)}\n"; }
                if (colorOff != DEFAULT_COLOR_OFF)
                { config += $"Action{index}ColorOff = {getStringFromColor(colorOff)}\n"; }
                if (textOn != DEFAULT_TEXT_ON)
                { config += $"Action{index}TextOn = {newLineToMultiLine(textOn)}\n"; }
                if (textOff != DEFAULT_TEXT_OFF)
                { config += $"Action{index}TextOff = {newLineToMultiLine(textOff)}\n"; }
                //Some of the ActionPlans are configured on the PB itself. We'll need to read those
                //as well to get the correct config. Fortunately, ActionSets keep all those at the
                //top of their ActionPlan lists.
                int planCounter = 0;
                while (planCounter != -1)
                {
                    //Before we run any checks, we need to make sure we haven't reached the end of
                    //the list. This can occur when the ActionSet has no ActionPlans for blocks on
                    //the grid.
                    if (planCounter >= actionPlans.Count)
                    { planCounter = -1; }
                    //Write config for an ActionPlanUpdate
                    else if (actionPlans[planCounter] is ActionPlanUpdate)
                    {
                        ActionPlanUpdate updatePlan = (ActionPlanUpdate)actionPlans[planCounter];
                        if (updatePlan.delayOn != DEFAULT_DELAY_ON)
                        { config += $"Action{index}DelayOn = {updatePlan.delayOn}\n"; }
                        if (updatePlan.delayOff != DEFAULT_DELAY_OFF)
                        { config += $"Action{index}DelayOff = {updatePlan.delayOff}\n"; }
                        planCounter++;
                    }
                    //Write config for an ActionPlanIGC
                    else if (actionPlans[planCounter] is ActionPlanIGC)
                    {
                        ActionPlanIGC IGCPlan = (ActionPlanIGC)actionPlans[planCounter];
                        if (IGCPlan.channel != DEFAULT_IGC_CHANNEL)
                        { config += $"Action{index}IGCChannel = {IGCPlan.channel}\n"; }
                        if (IGCPlan.messageOn != DEFAULT_IGC_MESSAGE_ON)
                        { config += $"Action{index}IGCMessageOn = {IGCPlan.messageOn}\n"; }
                        if (IGCPlan.messageOff != DEFAULT_IGC_MESSAGE_OFF)
                        { config += $"Action{index}IGCMessageOff = {IGCPlan.messageOff}\n"; }
                        planCounter++;
                    }
                    //Prepare any ActionPlans dealing with Triggers for export.
                    else if (actionPlans[planCounter] is ActionPlanTrigger)
                    {
                        ActionPlanTrigger triggerPlan = (ActionPlanTrigger)actionPlans[planCounter];
                        triggerPlans.Add(triggerPlan.subjectTrigger, new MyTuple<string, ActionPlanTrigger>(programName, triggerPlan));
                        planCounter++;
                        /*if (triggerPlan.hasOn)
                        {
                            //If we've already linked at least one ActionSet to this trigger...
                            if (linksOn.ContainsKey(triggerPlan.subject))
                            //...tack the name of this set on to the end of the existing string.
                            { linksOn[triggerPlan.subject] += $", {programName}"; }
                            //Otherwise, generate a new entry using the subject trigger and the name 
                            //of this set.
                            else
                            { linksOn.Add(triggerPlan.subject, programName); }
                        }
                        if (triggerPlan.hasOff)
                        {
                            if (linksOff.ContainsKey(triggerPlan.subject))
                            { linksOff[triggerPlan.subject] += $", {programName}"; }
                            else
                            { linksOff.Add(triggerPlan.subject, programName); }
                        }
                        planCounter++;*/
                    }
                    //If the selected ActionPlan isn't one of the PB-Plans, or if something else has
                    //gone wrong (Like we've hit the end of the list already)
                    else
                    { planCounter = -1; }
                }
                return config;
            }
        }

        public static string newLineToMultiLine(string entry)
        {
            //If we're going to do a multiline, put each part on its own line.
            if (entry.Contains("\n"))
            { entry = $"\n|{entry.Replace("\n", "\n|")}"; }
            return entry;
        }

        public class Raycaster
        {
            //The stringbuilder we'll use to assemble reports.
            StringBuilder _sb;
            //The camera block that will perform our raycasts
            internal IMyCameraBlock camera { get; private set; }
            //The data struct that will hold information about the last entity we detected
            MyDetectedEntityInfo entityInfo;
            //Holds the report on the last entity detected, or informs that no entity was detected.
            string report;
            //The initial scan distance
            internal double baseRange { private get; set; }
            //How much we multiply the scanRange by on each successive scan
            internal double multiplier { private get; set; }
            //The maximum distance we will scan.
            internal double maxRange { private get; set; }
            //Flag indicating if we've recently performed a scan.
            internal bool hasUpdate { get; private set; }

            public Raycaster(StringBuilder _sb, IMyCameraBlock camera, double baseDistance = 1000,
                double multiplier = 3, double max = 27000)
            {
                this._sb = _sb;
                report = "No Data";
                this.camera = camera;
                this.baseRange = baseDistance;
                this.multiplier = multiplier;
                this.maxRange = max;
                hasUpdate = false;
            }

            public void scan()
            {
                //The initial scan range will be the base distance.
                double scanRange = baseRange;
                //Perform a piddling initial scan, for no other purpose than establishing that the
                //isEmpty flag on entityInfo is clear (Or that there's a block right in front of 
                //the camera).
                entityInfo = camera.Raycast(1, 0, 0);
                //While we haven't hit anything, and while we can make another scan...
                while (entityInfo.IsEmpty() && camera.CanScan(scanRange))
                {
                    //Run a scan at the indicated range
                    entityInfo = camera.Raycast(scanRange, 0, 0);
                    //If the scan we just performed was at our stated maximum (Or if we have 
                    //inexplicably exceeded the stated maximum, via user interference, cosmic rays 
                    //striking the memory at just the right angle, or similar phenomena)...
                    if (scanRange >= maxRange)
                    //...break out of this loop
                    { break; }
                    //Prepare for the next iteration of the loop by calculating a new scan range
                    scanRange *= multiplier;
                    //If this new range is going to exceed our maximum...
                    if (scanRange > maxRange)
                    //Replace the calculated scanRange with max
                    { scanRange = maxRange; }
                }
                writeReport();
                //No matter what happened, set hasUpdate to true.
                hasUpdate = true;
            }

            private void writeReport()
            {
                //Clear the current contents of the StringBuilder
                _sb.Clear();
                //If entityInfo actually contains info...
                if (!entityInfo.IsEmpty())
                {
                    //The name of the target, and when it was detected
                    _sb.Append($"{entityInfo.Name} detected at {DateTime.Now.ToString("HH:mm:ss")}\n\n");
                    //The relationship between the player and the target
                    _sb.Append($"Relationship: {entityInfo.Relationship}\n");
                    //The target's type
                    _sb.Append($"Type: {entityInfo.Type}\n");
                    //The target's size
                    _sb.Append($"Size: {entityInfo.BoundingBox.Size.ToString("0.00")}\n");
                    //A handle that will make several of these calculations easier easier.
                    Vector3D target = entityInfo.HitPosition.Value;
                    //Distance to the target
                    _sb.Append($"Distance: {Vector3D.Distance(camera.GetPosition(), target).ToString("0.00")}\n");
                    //Coordinates of the point at which the target was struck
                    _sb.Append($"GPS:Raycast - {entityInfo.Name}:{target.X}:{target.Y}:{target.Z}:\n");
                }
                //If there was no info in entityInfo...
                else
                { _sb.Append("No entity detected."); }
                report = _sb.ToString();
            }

            public void updateClaimed()
            { hasUpdate = false; }

            public string toString()
            { return report; }
        }

        //Interface used by things that can be displayed by an MFD
        public interface IReportable
        {
            void setProfile();
            void update();
        }

        public class MFD : IReportable
        {
            //The Reportable objects managed by this MFD
            Dictionary<string, IReportable> pages;
            //The index of the report currently being displayed by the MFD
            internal int pageNumber { get; private set; }
            //The name of the report currently being displayed by the MFD.
            internal string pageName { get; private set; }

            public MFD()
            {
                pages = new Dictionary<string, IReportable>();
                pageNumber = 0;
                pageName = "";
            }

            //Add a page to this MFD.
            public void addPage(string name, IReportable reportable)
            {
                //Add this new page to the MFD
                pages.Add(name, reportable);
                //If we don't have a pageName yet...
                if (pageName == "")
                { pageName = name; }
            }

            //Returns the number of pages in this MFD
            public int getPageCount()
            { return pages.Count; }

            //'Flip' to the next page, or the previous, based on a bool you pass in.
            public void flipPage(bool forward)
            {
                if (forward)
                {
                    pageNumber++;
                    //If we've reached the end of the dictionary, start over at the beginning
                    if (pageNumber >= pages.Count)
                    { pageNumber = 0; }
                }
                else
                {
                    pageNumber--;
                    //If we've reached the beginning of the dictionary, loop to the end.
                    if (pageNumber < 0)
                    { pageNumber = pages.Count - 1; }
                }
                //Get the name of whatever page we've ended up at
                pageName = pages.ToArray()[pageNumber].Key;
                //Prepare the surface to display the new page.
                pages[pageName].setProfile();
                //Attempt to update the surface right now to show the new page.
                pages[pageName].update();
            }

            //Go to the page with the specified name
            public bool trySetPage(string name)
            {
                //If the page is actually in this MFD...
                if (pages.ContainsKey(name))
                {
                    pageName = name;
                    //Get the index of whatever page we've ended up at.
                    pageNumber = pages.Keys.ToList().IndexOf(name);
                    //Prepare the surface to display the new page.
                    pages[pageName].setProfile();
                    //Attempt to update the surface right now to show the new page.
                    pages[pageName].update();
                    return true;
                }
                else
                { return false; }
            }

            //To set the surface up for the MFD, we simply call setProfile on whatever page we're on.
            public void setProfile()
            { pages[pageName].setProfile(); }

            //To update the MFD, we simply call update on whatever page we're on.
            public void update()
            { pages[pageName].update(); }
        }

        public class GameScript : IReportable
        {
            //The surface this script will be placed on
            IMyTextSurface surface;
            //The colors this script will be displayed with
            public Color foreColor { get; set; }
            public Color backColor { get; set; }
            //The name of the script to be displayed.
            public string scriptName { get; set; }

            public GameScript(IMyTextSurface surface, string scriptName)
            {
                this.surface = surface;
                this.scriptName = scriptName;
                //By default, the script will simply assume the color settings of the surface at 
                //construction are the ones the user wants for it.
                foreColor = surface.ScriptForegroundColor;
                backColor = surface.ScriptBackgroundColor;
            }

            //The script is self contained, we don't need to do anything to make it work. So a call
            //to update() on this object does nothing.
            public void update()
            { }

            //Prepare this suface to display its ingame script.
            public void setProfile()
            {
                surface.ContentType = ContentType.SCRIPT;
                surface.Script = scriptName;
                surface.ScriptForegroundColor = foreColor;
                surface.ScriptBackgroundColor = backColor;
            }
        }

        public class Report : IReportable
        {
            //The surface we'll be drawing this report on.
            IMyTextSurface surface;
            //The tallies that this report will be pulling data from. We use an array because, at
            //this point, everything should be set.
            IHasElement[] elements;
            //The points on the screen that sprites will be anchored to.
            Vector2[] anchors;
            //A float storing the font size used for displaying the Tallies in this report.
            public float fontSize { private get; set; }
            //A string storing the name of the font for displaying Tallies in this report.
            public string font { private get; set; }
            //The colors that this report wants to use for its foreground and background.
            public Color foreColor { get; set; }
            public Color backColor { get; set; }
            //The title of this particular report, which will be displayed at the top of the screen.
            public string title { get; set; }
            //The title gets its very own anchor.
            Vector2 titleAnchor;

            public Report(IMyTextSurface surface, List<IHasElement> elements, string title = "", float fontSize = 1f, string font = "Debug")
            {
                this.surface = surface;
                //We won't be adding or removing tallies at this point, so we'll just pull the 
                //array out of the list and work with it directly.
                this.elements = elements.ToArray();
                //Set the title. We have to do that here, because we need to know if we have one to
                //get the anchors properly set.
                this.title = title;
                //Set the font info.
                this.fontSize = fontSize;
                this.font = font;
                //By default, the script will simply assume the color settings of the surface at 
                //construction are the ones the user wants for it.
                foreColor = surface.ScriptForegroundColor;
                backColor = surface.ScriptBackgroundColor;
                //For every tally that we have, we'll need to have a place on the surface to anchor
                //its sprite.
                anchors = new Vector2[elements.Count];
                //We'll go ahead and figure out the anchors for the default 3 columns. If this needs
                //to change, we can call it with a different number from outside the constructor.
                //NOTE: Until further notice, setAnchors will not be called during construction.
                //Status Date: 20201229
                /*setAnchors(3);*/
            }

            public void setAnchors(int columns)
            {
                //Malware's code for determining the viewport offset, which is the difference 
                //between an LCD's texture size and surface size. I have only the vaguest notions
                //of how it works.
                RectangleF viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f,
                    surface.SurfaceSize);
                //A string builder that we have to have before MeasureStringInPixels will tell us
                //the dimensions of our element
                StringBuilder element = new StringBuilder("");
                //If there's no title, we don't need to leave any space for it.
                float titleY = 0;
                //If there's a title, though, we'll need to make room for that.
                if (!string.IsNullOrEmpty(title))
                {
                    //Feed our title into the stringbuilder
                    element.Append(title);
                    //Figure out how much vertical space we'll need to leave off to accomodate it.
                    titleY = surface.MeasureStringInPixels(element, font, fontSize).Y;
                    //Create the titleAnchor that we'll lash the title sprite to.
                    titleAnchor = new Vector2(surface.SurfaceSize.X / 2, 0);
                    titleAnchor += viewport.Position;
                }
                //The number of rows we'll have is the number of elements, divided by how many 
                //columns we're going to display them across.
                int rows = (int)(Math.Ceiling((double)elements.Count() / columns));
                //The width of a column is our horizontal space divided by the number of columns
                float columnWidth = surface.SurfaceSize.X / columns;
                //The height of a row is the vertical space, minus room for the title, divided by
                //the number of rows.
                float rowHeight = (surface.SurfaceSize.Y - titleY) / rows;
                //Store our current position in the row.
                int rowCounter = 1;
                //Handles for the Vectors we'll be working with: One for our current position in the
                //grid, one that will store our finalized anchor, and one representing how much space
                //this sprite will take up.
                Vector2 sectorCenter, anchor, elementSize;
                //Before we start the loop, we need to make sure our entry point is in the right 
                //place. We'll start by putting it in the middle of the first row and column
                sectorCenter = new Vector2(columnWidth / 2, rowHeight / 2);
                //Then we'll apply the viewport offset
                sectorCenter += viewport.Position;
                //Last, we'll adjust the Y based on the height of the title. If there's no title,
                //this will be 0.
                sectorCenter.Y += titleY;
                for (int i = 0; i < elements.Count(); i++)
                {
                    //If a tally is null, we can safely ignore it.
                    //TODO: Monitor. It /shouldn't/ pitch a fit about uninitiated anchors if it
                    //never has to use them, but you never can tell.
                    if (elements[i] != null)
                    {
                        //Clear the contents of the StringBuilder
                        element.Clear();
                        //Force-feed it the string that we already have a perfectly good method for 
                        //building
                        element.Append(elements[i].assembleElementStack());
                        //Politely request the dimensions of the string we 'built'.
                        elementSize = surface.MeasureStringInPixels(element, font, fontSize);
                        //To figure out where this anchor is going to go, we start with the position
                        //of the sectorCenter
                        anchor = new Vector2(sectorCenter.X, sectorCenter.Y);
                        //Because sprites attach at the center-top, we need to move this anchor up
                        //by half the height of the element to get it in the right place.
                        anchor.Y -= elementSize.Y / 2;
                        //The anchor should be ready. 
                        anchors[i] = anchor;
                    }
                    //If we've reached the end of this row, move back to the center of the first
                    //column, then drop by rowHeight pixels and reset rowCounter
                    if (rowCounter == columns)
                    {
                        sectorCenter.X = columnWidth / 2;
                        sectorCenter.Y += rowHeight;
                        rowCounter = 1;
                    }
                    //Otherwise, move horizontally to the next sector center and increment rowCounter
                    //in preperation for the next iteration
                    else
                    {
                        sectorCenter.X += columnWidth;
                        rowCounter++;
                    }
                }
            }
            /*
            private string assembleElement(Tally tally)
            //I considered including a StringBuilder into this class to make this bit faster. But
            //apparently, if you do it all in one go, it's fast enough.
            { return $"{tally.name}\n{tally.getReadableCurr()} / {tally.getReadableMax()}\n{tally.getMeter()}"; }
            */
            //Re-draws this report, pulling new information from its elements to do so.
            public void update()
            {
                //A handle for tallies we'll be working with
                IHasElement element;
                //The sprite we'll be using to convey our information
                MySprite sprite;
                using (MySpriteDrawFrame frame = surface.DrawFrame())
                {
                    //If this report has a title, we'll start by drawing it.
                    if (!string.IsNullOrEmpty(title))
                    {
                        sprite = MySprite.CreateText(title, font, surface.ScriptForegroundColor,
                                fontSize);
                        sprite.Position = titleAnchor;
                        frame.Add(sprite);
                    }
                    for (int i = 0; i < elements.Count(); i++)
                    {
                        element = elements[i];
                        //If this tally is actually a null, we don't have to do anything at all.
                        if (element != null)
                        {
                            //Create a new TextSprite using information stored in this tally.
                            sprite = MySprite.CreateText(element.assembleElementStack(), font,
                                element.statusColor, fontSize);
                            //Use the anchor associate with this tally to position the sprite.
                            sprite.Position = anchors[i];
                            //Add the sprite to our frame.
                            frame.Add(sprite);
                        }
                    }
                }
            }

            //Prepare this surface for displaying the report.
            public void setProfile()
            {
                surface.ContentType = ContentType.SCRIPT;
                surface.Script = "";
                surface.ScriptForegroundColor = foreColor;
                surface.ScriptBackgroundColor = backColor;
            }
        }

        //An interface shared by various Broker objects.
        public interface IHasData
        {
            //Pull data from the broker's data source
            string getData();
            //Does the broker have new data?
            bool hasUpdate();
        }

        public class CustomDataBroker : IHasData
        {
            //The block this broker is pulling data from
            IMyTerminalBlock block;

            public CustomDataBroker(IMyTerminalBlock block)
            { this.block = block; }

            public string getData()
            //Pull the CustomData from this block
            { return block.CustomData; }

            public bool hasUpdate()
            //The CustomDataBroker never, ever has an update.
            { return false; }
        }

        public class DetailInfoBroker : IHasData
        {
            //The block this broker is pulling data from
            IMyTerminalBlock block;
            //Stores the last piece of information collected by this broker. Used to determine if
            //the broker has an update.
            string oldInfo;

            public DetailInfoBroker(IMyTerminalBlock block)
            {
                this.block = block;
                oldInfo = "";
            }

            public string getData()
            //Pull the DetailInfo from this block
            { return block.DetailedInfo; }

            public bool hasUpdate()
            {
                //TODO: See if there's a more effecient way to do this.
                //If the DetailInfo of our block matches the oldInfo...
                if (block.DetailedInfo == oldInfo)
                //There is no update.
                { return false; }
                else
                {
                    //Store the new info in the oldInfo
                    oldInfo = block.DetailedInfo;
                    //Indicate that we have an update.
                    return true;
                }
            }
        }

        public class LogBroker : IHasData
        {
            //The EventLog this broker is pulling data from (One among the great multitudes)
            Hammers.EventLog log;

            public LogBroker(Hammers.EventLog log)
            { this.log = log; }

            public string getData()
            //Request an event summary from the log
            { return log.toString(); }

            public bool hasUpdate()
            //Ask the log if it has an update.
            { return log.hasUpdate; }
        }

        public class StorageBroker : IHasData
        {
            //The GridProgram object that holds the Storage string.
            MyGridProgram program;

            public StorageBroker(MyGridProgram program)
            { this.program = program; }

            public string getData()
            //Get the Storage string
            { return program.Storage; }

            public bool hasUpdate()
            //Like CustomDataBroker, StorageBroker will never have an update for us.
            { return false; }
        }

        public class RaycastBroker : IHasData
        {
            //The Raycaster this broker is pulling data from
            Raycaster raycaster;

            public RaycastBroker(Raycaster raycaster)
            { this.raycaster = raycaster; }

            public string getData()
            //Request a report on the last detected entity from the Raycaster
            { return raycaster.toString(); }

            public bool hasUpdate()
            //Ask the Raycaster if it has an update.
            { return raycaster.hasUpdate; }
        }

        public class WallOText : IReportable
        {
            //The surface this text will be placed on
            IMyTextSurface surface;
            //The colors this text will be displayed with
            public Color foreColor { get; set; }
            public Color backColor { get; set; }
            //The font to be used by this text
            public string font { get; set; }
            //The size of this text
            public float fontSize { get; set; }
            //The number of characters to be displayed on a line before wrapping to the next.
            //Will only be set if a number greater than or equal to 0 is passed in.
            int charPerLine;
            //TODO: Come back and figure out why this didn't work.
            /*public int charPerLine
            {
                get
                { return charPerLine; }
                set
                {
                    if (value >= 0)
                    { charPerLine = value; }
                }
            }*/
            //The DataBroker we'll be consulting to get the text.
            IHasData broker;
            //The global StringBuilder that we'll use if we need to do text wrapping.
            StringBuilder _sb;

            public WallOText(IMyTextSurface surface, IHasData broker, StringBuilder _sb)
            {
                this.surface = surface;
                this.broker = broker;
                this._sb = _sb;
                //By default, this object will assume the current font and color settings of its
                //surface are what the user wants it to use.
                foreColor = surface.FontColor;
                backColor = surface.BackgroundColor;
                font = surface.Font;
                fontSize = surface.FontSize;
                //charPerLine starts out as 0, indicating that no text wrapping will occur.
                charPerLine = 0;
            }

            //Using this, because apparently I'm not smart enough to handle basic value checking in 
            //a setter.
            public void setCharPerLine(int chars)
            {
                if (chars >= 0)
                { charPerLine = chars; }
            }

            //Prepares the text by applying text wrapping to it.
            //TODO: Improve this, it is currently extremely brute-force.
            private string prepareText(string text)
            {
                //If a charPerLine value has been defined...
                if (charPerLine > 0)
                {
                    //Build an array where each element contains one word from our text string
                    string[] words = text.Split(' ');
                    int charThisLine = 0;
                    _sb.Clear();
                    foreach (string word in words)
                    {
                        //Add the current word to the string builder, along with a trailing space
                        _sb.Append($"{word} ");
                        //If the word we're looking at contains its own newline...
                        if (word.Contains('\n'))
                        //We're moving to a new line. Reset charThisLine
                        { charThisLine = 0; }
                        else
                        {
                            //Increment charThisLine by the number of characters we just added to this 
                            //line
                            charThisLine += word.Length + 1;
                            //If we just exceeded our charPerLine 'limit'
                            if (charThisLine > charPerLine)
                            {
                                //Move to a new line
                                _sb.Append("\n");
                                //Set our char counter back to 0.
                                charThisLine = 0;
                            }
                        }
                    }
                    //Hand our gaily wrapped string back to the variable
                    text = _sb.ToString();
                }
                //Return the text
                return text;
            }

            //See if we need to update this WallOText
            public void update()
            {
                //If there's an update...
                if (broker.hasUpdate())
                //Use data from the broker to re-write the surface.
                { surface.WriteText(prepareText(broker.getData())); }
            }

            //Prepare this suface to display its text, and write the initial text.
            public void setProfile()
            {
                //Apply this Reportable's stored configuration
                surface.ContentType = ContentType.TEXT_AND_IMAGE;
                surface.FontColor = foreColor;
                surface.BackgroundColor = backColor;
                surface.Font = font;
                surface.FontSize = fontSize;
                //Write the text currently held by this object's broker.
                surface.WriteText(prepareText(broker.getData()));
            }
        }

        //Similar to a Report, Indicators refer to a group of lights that reflect the status of an
        //element. It's just a lot simpler, because the only thing you can do with a light is change
        //the color.
        //... Or at least, that's all we're /going/ to do with it.
        public class Indicator
        {
            //The lighting blocks that make up this Indicator
            List<IMyLightingBlock> lights;
            //The Element that tells this Indicator what to do
            IHasElement element;
            //The last color code set by this indicator. Used to make sure we're only changing all 
            //the light colors when we need to.
            Color oldColor;

            public Indicator(IHasElement element)
            {
                lights = new List<IMyLightingBlock>();
                this.element = element;
                oldColor = Hammers.cozy;
            }

            public void addLight(IMyLightingBlock light)
            { lights.Add(light); }

            public void update()
            {
                //If the element's color code has changed...
                if (element.statusColor != oldColor)
                {
                    //Go to each light in lights and change its color.
                    foreach (IMyLightingBlock light in lights)
                    { light.Color = element.statusColor; }
                    //Update oldColor to match the color we just set everything to.
                    oldColor = element.statusColor;
                }
            }
        }

        //An interface for bespoke objects that store and manage specific block types for TallyGenerics
        public interface ITallyGenericHandler
        {
            //Add a block to this TallyHandler
            //DEPRECEATED. Replaced with tryAddBlock.
            //void addBlock(IMyTerminalBlock block);
            //Try to add a block to this TallyHandler. If the block is compatible, it will be added
            //to the internal list and true will be returned. If the block is not compatible, false
            //is returned
            bool tryAddBlock(IMyTerminalBlock block);
            //Return the maximum value of the blocks in this handler
            double getMax();
            //Return the current value of the blocks in this handler
            double getCurr();
            //Return the string that will configure tallies of this type.
            string writeConfig();
        }

        public class BatteryHandler : ITallyGenericHandler
        {
            List<IMyBatteryBlock> subjects;

            public BatteryHandler()
            { subjects = new List<IMyBatteryBlock>(); }

            public bool tryAddBlock(IMyTerminalBlock block)
            {
                IMyBatteryBlock prospect = block as IMyBatteryBlock;
                if (prospect == null)
                { return false; }
                else
                {
                    subjects.Add(prospect);
                    return true;
                }
            }

            public double getMax()
            {
                double max = 0;
                foreach (IMyBatteryBlock battery in subjects)
                { max += battery.MaxStoredPower; }
                return max;
            }

            public double getCurr()
            {
                double curr = 0;
                foreach (IMyBatteryBlock battery in subjects)
                { curr += battery.CurrentStoredPower; }
                return curr;
            }

            public string writeConfig()
            { return "Battery"; }
        }

        //Oxygen and Hydrogen tanks use the same iterface, so we can use the same handler for both.
        //This handler also handles Hydrogen Engines, so it's a little bit different from the others.
        public class GasHandler : ITallyGenericHandler
        {
            List<IMyGasTank> tanks;
            List<IMyTerminalBlock> engines;

            public GasHandler()
            {
                tanks = new List<IMyGasTank>();
                engines = new List<IMyTerminalBlock>();
            }

            public bool tryAddBlock(IMyTerminalBlock block)
            {
                //Try to cast our prospect as a Gas Tank
                IMyGasTank prospectiveTank = block as IMyGasTank;
                if (prospectiveTank != null)
                {
                    tanks.Add(prospectiveTank);
                    return true;
                }
                else
                {
                    //If it isn't a tank, we'll still accept it if it's a hydrogen engine.
                    IMyPowerProducer prospectiveEngine = block as IMyPowerProducer;
                    //All of the vanilla hydrogen engines end with HydrogenEngine. Any modded blocks 
                    //that follow this convention will also work with this tally. Assuming their 
                    //DetailInfo is formatted the same way...
                    if (prospectiveEngine != null
                        && prospectiveEngine.BlockDefinition.SubtypeId.EndsWith("HydrogenEngine"))
                    {
                        engines.Add(prospectiveEngine);
                        return true;
                    }
                    //If it isn't a GasTank and it isn't a Hydrogen engine, we don't want it.
                    else
                    { return false; }
                }
            }

            //Using MyResourceSourceComponent means we no longer need to parse a string to figure
            //out how much hydrogen is in an engine moment to moment. But we still have to do that
            //to figure out how much it can hold.
            //This is the DetailInfo string of a Hydrogen Engine:
            //  Type: Hydrogen Engine
            //  Max Output: 5.00 MW
            //  Current Output: 0 W
            //  Filled: 100.0% (100000L/100000L)
            //Once split with the criteria array, we have:
            //string[0] = Fluff
            //string[1] = Current fill
            //string[2] = Max fill
            //string[3] = Empty string?
            public double getMax()
            {
                double max = 0;
                string[] parts;
                //Seperators that will be used to split the Hydrogen Engine's DetailInfo into useable
                //blocks of data.
                string[] criteria = { "(", "L/", "L)" };
                foreach (IMyGasTank tank in tanks)
                { max += tank.Capacity; }
                foreach (IMyTerminalBlock engine in engines)
                {
                    parts = engine.DetailedInfo.Split(criteria, System.StringSplitOptions.None);
                    //parts[1] = curr, parts[2] = max
                    //MONITOR: This will fail messily if the format of the DetailInfo string doesn't
                    //match expectations. This is intentional; I want to hear about it if this 
                    //assumption is incorrect.
                    max += Double.Parse(parts[2]);
                }
                return max;
            }

            public double getCurr()
            {
                double curr = 0;
                foreach (IMyGasTank tank in tanks)
                { curr += tank.Capacity * tank.FilledRatio; }
                foreach (IMyTerminalBlock engine in engines)
                {
                    //This code provided by Digi. 
                    curr += engine.Components.Get<MyResourceSourceComponent>().RemainingCapacity;
                }
                return curr;
            }

            public string writeConfig()
            { return "Gas"; }
        }

        public class JumpDriveHandler : ITallyGenericHandler
        {
            List<IMyJumpDrive> subjects;

            public JumpDriveHandler()
            { subjects = new List<IMyJumpDrive>(); }

            public bool tryAddBlock(IMyTerminalBlock block)
            {
                IMyJumpDrive prospect = block as IMyJumpDrive;
                if (prospect == null)
                { return false; }
                else
                {
                    subjects.Add(prospect);
                    return true;
                }
            }

            public double getMax()
            {
                double max = 0;
                foreach (IMyJumpDrive drive in subjects)
                { max += drive.MaxStoredPower; }
                return max;
            }

            public double getCurr()
            {
                double curr = 0;
                foreach (IMyJumpDrive drive in subjects)
                { curr += drive.CurrentStoredPower; }
                return curr;
            }

            public string writeConfig()
            { return "JumpDrive"; }
        }

        //The user will probably only have one raycaster per tally. But who are we to judge?
        public class RaycastHandler : ITallyGenericHandler
        {
            List<IMyCameraBlock> subjects;

            public RaycastHandler()
            { subjects = new List<IMyCameraBlock>(); }

            public bool tryAddBlock(IMyTerminalBlock block)
            {
                IMyCameraBlock prospect = block as IMyCameraBlock;
                if (prospect == null)
                { return false; }
                else
                {
                    subjects.Add(prospect);
                    return true;
                }
            }

            //Raycasters don't have a reasonable max, which is why we require them to have their 
            //max set in config. So what do we do here? Shrug.
            public double getMax()
            { return -1; }

            public double getCurr()
            {
                double curr = 0;
                foreach (IMyCameraBlock camera in subjects)
                { curr += camera.AvailableScanRange; }
                return curr;
            }

            public string writeConfig()
            { return "Raycast"; }
        }

        //Counterintuitively, the 'MaxOutput' of things like Solar Panels and Wind Turbines is not
        //fixed. It actually describes the ammount of power that the block is currently receiving
        //in its current enviroment, ie, how much of a panel's surface area is facing the sun, or
        //what kind of weather is the turbine in. The variable you'd expect to describe those 
        //things, CurrentOutput, instead describes how much energy the grid is drawing from this
        //PowerProvider.
        //Also: MaxOutput is in megawatts, while most PowerProducers generate power in the kilowatt
        //range. This handler will generally return a decimal.
        public class PowerProducerHandler : ITallyGenericHandler
        {
            List<IMyPowerProducer> subjects;

            public PowerProducerHandler()
            { subjects = new List<IMyPowerProducer>(); }

            public bool tryAddBlock(IMyTerminalBlock block)
            {
                IMyPowerProducer prospect = block as IMyPowerProducer;
                if (prospect == null)
                { return false; }
                else
                {
                    subjects.Add(prospect);
                    return true;
                }
            }

            //Since Digi pointed me at the ResourceSourceComponent, I can now figure out exactly
            //what the maximum output of a PowerProducer is.
            public double getMax()
            {
                double max = 0;
                foreach (IMyPowerProducer producer in subjects)
                { max += producer.Components.Get<MyResourceSourceComponent>().DefinedOutput; }
                return max;
            }

            public double getCurr()
            {
                double curr = 0;
                foreach (IMyPowerProducer producer in subjects)
                { curr += producer.MaxOutput; }
                return curr;
            }

            public string writeConfig()
            { return "PowerProducer"; }
        }

        //When you have a hammer, everything starts to look like a nail.
        //A collection of tools I find myself using on a regular basis.
        public class Hammers
        {
            //Isn't actually used for anything, this is just the color I've taken to applying to 
            //my lights, and I wanted it handy.
            public static Color cozy = new Color(255, 225, 200);
            //Best state
            public static Color green = new Color(25, 225, 100);
            //Default
            public static Color lightBlue = new Color(100, 200, 225);
            //First warning
            public static Color yellow = new Color(255, 255, 0);
            //Second warning
            public static Color orange = new Color(255, 150, 0);
            //Final warning
            public static Color red = new Color(255, 0, 0);

            //A simple object that stores a configurable number events and formats them into
            //something readable. Intended for use with the Echo() function on PBs
            //... It really isn't that simple anymore.
            public class EventLog
            {
                //Stores the strings that make up the log
                List<string> log;
                //The title that will be displayed before the log entries
                string title;
                //If the script's tag has changed, we'll use the log to remind the user of that.
                public string scriptTag { private get; set; }
                //If the script we're logging has an update delay, we'll have an entry for that in
                //the title. This int stores the current delay setting.
                public int scriptUpdateDelay { private get; set; }
                //Apparently, you're supposed to try to avoid allocating new objects when scripting
                //for SE. So instead of creating a new string every time toString() is called, 
                //we'll just overwrite the contents of this one.
                //Also, string concatentation is slow. And since that's basically all this does, 
                //we'll replace the string with a StringBuilder.
                //Also, the log really doesn't need its own StringBuilder, so we'll let it share
                //the one that the rest of the script uses.
                StringBuilder _sb;
                //When generating a string, the component that will change most often is the ticWidget.
                //The next most frequent change will be the textual representation of the log 
                //entries, but it still shouldn't be changing too often. We'll store the last one
                //we built in this variable, and only update it when something changes in the entry
                //list.
                string entriesText;
                //The number of entries the log will store. If adding a new entry causes the count
                //to exceed this value, the eldest entry will be removed.
                int maxEntries;
                //A flag indicating if there's actually anything new in the log. Needs to be 
                //cleared after an updated log has been accessed by using clearUpdate() 
                public bool hasUpdate { get; private set; }
                //Stores the frames of a progress bar of sorts, that will help the user get an idea
                //of how fast (Or if) the script is current running.
                string[] ticWidget;
                int ticWidgetIndex = -1;
                int ticWidgetDirection;

                public EventLog(StringBuilder _sb, string title, bool showTicWidget = false, int maxEntries = 5)
                {
                    log = new List<string>();
                    this._sb = _sb;
                    entriesText = "";
                    this.title = title;
                    scriptTag = "";
                    this.maxEntries = maxEntries;
                    hasUpdate = false;
                    scriptUpdateDelay = 0;
                    if (showTicWidget)
                    {
                        ticWidget = new string[] { "[|----]", "[-|---]", "[--|--]", "[---|-]", "[----|]" };
                        ticWidgetIndex = 0;
                        ticWidgetDirection = 1;
                    }
                }

                //When the script performs an update tic, this can be called to update the ticWidget
                public void tic()
                {
                    //Move the index in the stored direction
                    ticWidgetIndex += ticWidgetDirection;
                    //If we've reached the beginning or end of the frames...
                    if (ticWidgetIndex == 0 || ticWidgetIndex == 4)
                    //...flip the direction.
                    { ticWidgetDirection *= -1; }
                }

                //Add a string the the event log.
                //string entry: The entry to be added to the log.
                public void add(string newEntry)
                {
                    //Timestamp the new entry and place it at the front of the list.
                    log.Insert(0, $"{DateTime.Now.ToString("HH:mm:ss")}- {newEntry}");
                    //If we've reached the maximum number of entries, remove the last one.
                    if (log.Count > maxEntries)
                    { log.RemoveAt(maxEntries); }

                    //We have a new entry in the log, that means our entryText is out of date. To
                    //update it, we first need to clear the StringBuilder...
                    _sb.Clear();
                    //Then, we take every entry in the log...
                    foreach (string entry in log)
                    //...and tack it onto our Stringbuilder
                    { _sb.Append($"\n{entry}\n"); }
                    //Store our newly re-built string in entriesText
                    entriesText = _sb.ToString();
                    _sb.Clear();

                    //Flag the log as having been recently updated.
                    hasUpdate = true;
                }

                //Sets the 'updated' flag to false. Call after pulling the new log.
                public void updateClaimed()
                { hasUpdate = false; }

                //Get the logged events in a readable format
                public string toString()
                {
                    //Clear the contents of the StringBuilder
                    _sb.Clear();
                    //Start with the title
                    _sb.Append(title);
                    //If the tic widget is currently in use...
                    if (ticWidgetIndex != -1)
                    //... pick the proper frame and add it.
                    { _sb.Append($" {ticWidget[ticWidgetIndex]}"); }
                    _sb.Append("\n");
                    //If we've got a custom scriptTag...
                    if (!String.IsNullOrEmpty(scriptTag))
                    //...tack it on
                    { _sb.Append($"Script Tag: {scriptTag}\n"); }
                    //If scriptUpdateDelay isn't 0...
                    if (scriptUpdateDelay != 0)
                    //...include a notice.
                    { _sb.Append($"Current Update Delay: {scriptUpdateDelay}\n"); }
                    //Get the entriesText and tack it on
                    _sb.Append(entriesText);
                    //Chuck the string we just built out to whom it may concern.
                    return _sb.ToString();
                }
            }

            public class MeterMaid
            {
                //A reference to the global StringBuilder.
                StringBuilder _sb;
                //The default length of bars drawn by this MeterMaid
                int defaultLength;
                //Meters pre-generated at object intiliaziation.
                string[] meters;

                public MeterMaid(StringBuilder _sb, int length = 10)
                {
                    this._sb = _sb;
                    defaultLength = length;
                    //The meters array will need 1 more entry than the length we've been provided, 
                    //to account for a state where no bars are visible.
                    meters = new string[defaultLength + 1];
                    string newMeter = "";
                    for (int i = 0; i < meters.Length; i++)
                    {
                        drawMeter(ref newMeter, i, defaultLength);
                        meters[i] = newMeter;
                    }
                }

                //Gets an ASCII meter for a visual representation of percentages.
                //This version uses pre-generated meters using a pre-defined length
                //ref string meter: The string that will hold the constructed meter.
                //double percent: The percentage (Between 0-100) that will be displayed
                public void getMeter(ref string meter, double percent)
                { meter = meters[getNumBars(percent, defaultLength)]; }

                //Creates an ASCII meter for a visual representation of percentages.
                //This version generates a meter when called, meaning the length is not fixed.
                //ref string meter: The string that will hold the constructed meter.
                //double percent: The percentage (Between 0-100) that will be displayed
                //int length: The number of characters that will be used by the meter, not counting
                //  the book-end brackets.
                public void getMeter(ref string meter, double percent, int length)
                {
                    int bars = getNumBars(percent, length);
                    drawMeter(ref meter, bars, length);
                }

                //The number of solid bars that will be needed for this meter.
                private int getNumBars(double percent, int length)
                {
                    //A lot of my 'max' values are just educated guesses. Percentages greater than a 
                    //hundred happen. And they really screw up the meters. So we're just going to 
                    //pretend that everyone's staying within 100.
                    percent = Math.Min(percent, 100);
                    return (int)((percent / 100) * length);
                }

                private void drawMeter(ref string meter, int bars, int length)
                {
                    //There's bound to be something in the StringBuilder. Clear it.
                    _sb.Clear();
                    _sb.Append('[');
                    //To make the meter, we have the first loop filling in solid lines...
                    for (int i = 0; i < bars; ++i)
                    { _sb.Append('|'); }
                    //... And another loop filling in blanks.
                    for (int i = bars; i < length; ++i)
                    { _sb.Append(' '); }
                    _sb.Append(']');
                    //Hand our shiny new meter to the string we were passed
                    meter = _sb.ToString();
                }
            }

            /* The version of MeterMaid that built meters every single time it was called.
            public class MeterMaid
            {
                //A reference to the global StringBuilder.
                StringBuilder _sb;

                public MeterMaid(StringBuilder _sb)
                { this._sb = _sb; }
                
                //Creates an ASCII meter for a visual representation of percentages.
                //double percent: The percentage (Between 0-100) that will be displayed
                //int length: How many characters will be used to display the percentage, not counting 
                //  the bookend brackets. Defaults to 10
                public void drawMeter(ref string meter, double percent, int length = 10)
                {
                    //There's bound to be something in the StringBuilder. Clear it.
                    _sb.Clear();
                    //A lot of my 'max' values are just educated guesses. Percentages greater than a 
                    //hundred happen. And they really screw up the meters. So we're just going to 
                    //pretend that everyone's staying within 100.
                    percent = Math.Min(percent, 100);
                    _sb.Append('[');
                    //How many bars do we need?
                    int bars = (int)((percent / 100) * length);
                    //To make the meter, we have the first loop filling in solid lines...
                    for (int i = 0; i < bars; ++i)
                    { _sb.Append('|'); }
                    //... And another loop filling in blanks.
                    for (int i = bars; i < length; ++i)
                    { _sb.Append(' '); }
                    _sb.Append(']');
                    //Hand our shiny new meter to the string we were passed
                    meter = _sb.ToString();
                }
            }*/

            //The easiest way to get an individual block would be to use GetBlockWithName, but it 
            //doesn't allow you to run any checks. So I basically use the same method that I do for 
            //getting groups. That does mean it needs some error detection, though, just in case it 
            //doesn't get the number of blocks it expects.
            //string errorLog: The string reference that any errors will be passed out to
            //IMyGridTerminalSystem GTS: The GTS of the grid we're scanning. Because we're in a 
            //  different scope from Program, we need to have this handed to us.
            //List<IMyTerminalBlock> blocks: Instead of creating a block list to pass into the grid
            //  terminal system, just take one that we already have lying around.
            //Func collect: Checks that will be run to see if we got the right block. Defaults to 
            //  null.
            public static T findBlock<T>(ref string errorLog, IMyGridTerminalSystem GTS,
                List<IMyTerminalBlock> blocks, Func<IMyTerminalBlock, bool> collect = null) where T : class
            {
                T block = null;
                GTS.GetBlocksOfType<T>(blocks, collect);

                //We should get exactly one block out of this. Did we?
                if (blocks.Count == 1)
                { block = (T)blocks[0]; }
                else if (blocks.Count == 0)
                { errorLog += typeof(T).FullName + " not found.\n"; }
                else if (blocks.Count > 1)
                { errorLog += "More than one " + typeof(T).FullName + " found.\n"; }

                return block;
            }

            //A helper method, basically identical to the built-in GetBlocksOfType. Main difference
            //is that this one will report any error to the string you pass in.
            //IMyGridTerminalSystem GTS: The GTS of the grid we're scanning. Because we're in a 
            //  different scope from Program, we need to have this handed to us.
            //List<IMyTerminalBlock> blocks: Instead of creating a block list to pass into the grid
            //  terminal system, just take one that we already have lying around. This list will 
            //  have any blocks we found in it when we're done.
            //Func collect: Optional checks that will be run to see if we got the right block. 
            //  Defaults to null.
            //string error: Optional error message that will be returned if no blocks are found
            //  matching these parameters. Useful for providing a readable version of the error 
            //  message, as the one that this method makes on its own is an abomination.
            public static string findBlocks<T>(IMyGridTerminalSystem GTS, List<T> blocks,
                Func<T, bool> collect = null, string error = null) where T : class
            {
                GTS.GetBlocksOfType<T>(blocks, collect);

                //Did we get anything?
                if (blocks.Count == 0)
                {
                    //If we weren't provided an error message...
                    if (error == null)
                    //Make one up.
                    { error = typeof(T).FullName + " not found.\n"; }
                    else
                    { error += "\n"; }
                }
                //If there were no errors, clear the string that we recieved to indicate this.
                else
                { error = ""; }

                return error;
            }

            //Used to pull information from the CustomData section of a Terminal block. 
            //Worth noting that MyIni has a TryParse method that takes a section name, and could 
            //probably do all of this in one go. For the moment, I'm leaving it as-is
            //Also worth noting: There's a few opportunities here to reduce new memory allocation
            public static string getStringFromINI(ref string errorLog, MyIni iniReader,
                IMyTerminalBlock block, string section, string key)
            {
                //The value that we'll be returning
                string value = "";
                //Wonder if I could just output this to a string instead?
                MyIniParseResult result;
                //First, we check to see if the INI data is formatted correctly
                if (!iniReader.TryParse(block.CustomData, out result))
                { errorLog += result.ToString() + "\n"; }
                else
                {
                    //Does the block actually contain the data we're looking for?
                    if (iniReader.ContainsKey(section, key))
                    { value = iniReader.Get(section, key).ToString(); }
                    else
                    { errorLog += block.CustomName + " did not contain key " + key + ".\n"; }
                }
                return value;
            }

            //Sets up text surfaces into a configuration I commonly use. If I need something 
            //slightly different, it's fairly customizable.
            //IMyTextSurface screen: the surface that will have its settings changed.
            //Other (self-explanatory) paramaters:
            //float fontSize, string font, float padding, TextAlignment alignment
            public static void initTextSurface(IMyTextSurface screen, float fontSize = 5f, string font = "Debug",
                float padding = 0f, TextAlignment alignment = VRage.Game.GUI.TextPanel.TextAlignment.CENTER)
            {
                screen.ContentType = ContentType.TEXT_AND_IMAGE;
                //The initialization color. If the screen keeps showing orange, something has 
                //gone wrong.
                screen.FontColor = Color.Orange;
                //I'd like to pass this in, but Color.Black can't be used as a defualt value.
                screen.BackgroundColor = Color.Black;
                screen.Font = font;
                screen.FontSize = fontSize;
                screen.TextPadding = padding;
                screen.Alignment = alignment;
                //If there isn't something written on the screen yet, put in a placeholder so we
                //know that the initialization completed successfully.
                if (screen.GetText() == "")
                { screen.WriteText("Initiated"); }
            }

            //Lot of room for improvement, here
            public static bool tryGetColorFromElements(ref Color color, string[] elements)
            {
                //Split the input string into array elements.
                int[] values = new int[3];
                int counter = 0;
                //If the user didn't include 3 values, we will pre-emptively declare not-success
                bool success = elements.Length == 3;
                //While we haven't gotten all three colors, and if we haven't failed yet...
                while (counter < 3 && success)
                {
                    //Try to parse this element
                    success = Int32.TryParse(elements[counter], out values[counter]);
                    counter++;
                }
                //Make a new color based on the integers we've read
                color = new Color(values[0], values[1], values[2]);
                return success;
            }

            //Returns a color code where high numbers are good. (Batteries, oxygen, etc)
            //double percent: The percentage (Between 0-100) that you want a color code for.
            public static Color handleColorCodeHigh(double percent)
            {
                //Default to the default.
                Color code = lightBlue;

                if (percent >= 100)
                { code = green; }
                else if (percent < 15)
                { code = red; }
                else if (percent < 30)
                { code = orange; }
                else if (percent < 45)
                { code = yellow; }
                return code;
            }

            //Returns a color code where low numbers are good. (Inventory, basically)
            //double percent: The percentage (Between 0-100) that you want a color code for.
            public static Color handleColorCodeLow(double percent)
            {
                //Default to the default.
                Color code = lightBlue;

                if (percent == 0)
                { code = green; }
                else if (percent > 85)
                { code = red; }
                else if (percent > 70)
                { code = orange; }
                else if (percent > 55)
                { code = yellow; }
                return code;
            }

            //Creates an ASCII meter for a visual representation of percentages.
            //double percent: The percentage (Between 0-100) that will be displayed
            //int length: How many characters will be used to display the percentage, not counting 
            //  the bookend brackets. Defaults to 10
            public static void drawMeter(ref StringBuilder meter, double percent, int length = 10)
            {
                //There's bound to be something in the old meter. Clear it.
                meter.Clear();
                //A lot of my 'max' values are just educated guesses. Percentages greater than a 
                //hundred happen. And they really screw up the meters. So we're just going to 
                //pretend that everyone's staying within 100.
                percent = Math.Min(percent, 100);
                meter.Append('[');
                //How many bars do we need?
                int bars = (int)((percent / 100) * length);
                //To make the meter, we have the first loop filling in solid lines...
                for (int i = 0; i < bars; ++i)
                { meter.Append('|'); }
                //... And another loop filling in blanks.
                for (int i = bars; i < length; ++i)
                { meter.Append(' '); }
                meter.Append(']');
            }

            //Replaces powers of ten with Ks or Ms.
            //string readable: The reference to the string that will hold the output.
            //int num: The number that should be rendered into a more readable form.
            public static void readableInt(ref string readable, int num)
            {
                readable = "";
                //If the number is greater than 10 million, just take the last 6 digits and replace them with an M
                if (num >= 10000000)
                { readable = /*(int)*/(num / 1000000) + "M"; }
                //If the number is between 10 million and 1 million, replace the last 6 digits with an M, and keep the 
                //first replaced digit as a decimal
                else if (num >= 1000000)
                { readable = Decimal.Round(((decimal)num / 1000000), 1) + "M"; }
                //If the number is between a million and 10 thousand, replace the last 3 digits with a K
                else if (num >= 10000)
                { readable = /*(int)*/(num / 1000) + "K"; }
                //if the number is between 10 thousand and 1 thousand, replace the last 3 digits with a K and one decimal.
                else if (num >= 1000)
                { readable = Decimal.Round(((decimal)num / 1000), 1) + "K"; }
                //If the number isn't greater than a thousand, why'd you call this in the first place?
                else
                { readable += num; }
            }
        }
    }
}

///<status> <date> </date>
///  <content>
///     
///  </content>
///</status>
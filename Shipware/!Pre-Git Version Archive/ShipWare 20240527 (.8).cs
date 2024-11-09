﻿using Sandbox.Game.EntityComponents;
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
        const double _VERSION = .8;
        //The default ID of the script, to be used if no custom ID is set.
        const string _DEFAULT_ID = "Shipware";
        //The prefix used to identify all components of the script, regardless of changes the user
        //makes to the ID
        const string _SCRIPT_PREFIX = "SW";
        //The prefix we use to quickly identify declarations on the PB.
        const string _DECLARATION_PREFIX = "Dec";
        //The ID for this instance of the script. 
        string _customID;
        //A combination of PREFIX and ID, constructed during initialization. Used as a section tag
        //for all non-discrete sections in configuration, and as a channel for recognizing IGC 
        //communication.
        string _tag;
        //Many script functions refer to the Target and Source groups to figure out what blocks they're
        //supposed to operate on. I've changed the names of these groups several times, so this time
        //I'm making them constants in case I decide to do this again.
        const string _SOURCE_GROUP_NAME = "Source";
        const string _TARGET_GROUP_NAME = "Target";
        //An EventLog that will... log events.
        EventLog _log;
        //Used to read information out of a block's CustomData
        //It should be noted: I /really/ don't need to keep this and iniRead as globals. But at this
        //point, it would be a massive undertaking to go through and localize every instance of use.
        MyIni _iniReadWrite;
        //A second instance of MyIni, handy for moving data between two different configurations.
        MyIni _iniRead;
        //A StringBuilder that we will pass out to the various objects that need one.
        StringBuilder _sb;
        //An object that generates and stores prefab ASCII meters for us, used by tallies.
        MeterMaid _meterMaid;
        //Arrays that store the containers and tallies that this script watches. 
        Container[] _containers;
        Tally[] _tallies;
        //ActionSets that can be used to carry out pre-defined actions on a group of blocks. Stored
        //in a dictionary so we can toggle them by name.
        Dictionary<string, ActionSet> _sets;
        //Triggers may have names now, but we don't ever need to find them. An array will suffice.
        Trigger[] _triggers;
        //A raycaster object is used to perform Raycasts and compile reports about them. We address 
        //them by name so we know which one to scan from.
        Dictionary<string, Raycaster> _raycasters;
        //The reports that tell about what various script elements are doing.
        IReportable[] _reports;
        //Indicators are like reports, except for lights instead of LCDs and they only communicate 
        //with colors.
        Indicator[] _indicators;
        //MFDs bind multiple reports together into a single, Reportable screen. They're stored in a
        //dictionary so the user can control what page they're on.
        Dictionary<string, MFD> _MFDs;
        //A log that tells you what's going wrong isn't much good if it doesn't update when something
        //goes wrong. We'll keep track of our log WOTs here, so we can force them to display even
        //when the rest of the script isn't running
        List<WallOText> _logReports;
        //The IGC listener, which lets us know if we have mail.
        IMyBroadcastListener _listener;
        //When active, this script updates all its internal and external elements every 100 tics, 
        //roughly once every second and a half. If the grid the script is running on isn't doing 
        //anything important, the script can be set to skip update tics to reduce processor loads.
        //The logic and variables are handled by this UpdateDistributor object.
        UpdateDistributor _distributor;
        //Do we have good config?
        bool _haveGoodConfig;
        //And when did we get it?
        DateTime _lastGoodConfigStamp;
        //What's left on the PB when we strip all the declarations out?
        //BAD (20230327): This is the only thing I could think of that would stop me from needing 
        //to read possibly compromised configuration off the PB for Reconstitute. It gets the job
        //done, but there's got to be a better solution.
        //UPDATE (20240425) Still haven't thought of anything better.
        string _nonDeclarationPBConfig;
        //The color palette used by the script. 
        //Optimal
        public static Color green = new Color(25, 225, 100);
        //Normal
        public static Color lightBlue = new Color(100, 200, 225);
        //Caution
        public static Color yellow = new Color(255, 255, 0);
        //Warning
        public static Color orange = new Color(255, 150, 0);
        //Critical
        public static Color red = new Color(255, 0, 0);
        //Isn't actually used for anything, this is just the color I've taken to applying to 
        //my lights, and I wanted it handy.
        public static Color cozy = new Color(255, 225, 200);
        //Goes with everything
        public static Color black = new Color(0, 0, 0);
        //DEBUG USE
        IMyTextSurface _debugDisplay;

        public Program()
        {
            LimitedMessageLog textLog;
            bool firstRun;
            initiate(out textLog, out firstRun);
            if (firstRun)
            { firstRunSetup(textLog); }
            else
            { evaluateFull(textLog); }
            //The main method Echos the event log every time it finishes running. But there's a lot
            //of stuff that can go wrong when parsing configuration, so we need an Echo here as well.
            Echo(_log.toString());
        }

        public void initiate(out LimitedMessageLog textLog, out bool firstRun)
        {
            //Initiate some of the background objects the script needs to operate
            _iniReadWrite = new MyIni();
            _iniRead = new MyIni();
            _sb = new StringBuilder();
            _haveGoodConfig = false;
            _lastGoodConfigStamp = DateTime.Now;
            //These are basically local variables that will be passed to other methods once we're 
            //done here.
            textLog = new LimitedMessageLog(_sb, 15);
            firstRun = false;
            //One of the first things we need to do is figure out if this code version has changed, 
            //or if script has a custom tag. To do that, we check the Storage string.
            _iniReadWrite.TryParse(Storage);
            //Get the version number of the code last used on this PB, using a -1 if we can't find
            //an entry.
            double lastVersion = _iniReadWrite.Get("Data", "Version").ToDouble(-1);
            //Try to pull the ID from the Config section of the Storage string, using the default 
            //ID if nothing is found.
            _customID = _iniReadWrite.Get("Data", "ID").ToString(_DEFAULT_ID);
            //Build the tag by combining the constant PREFIX and the user-modifiable ID
            _tag = $"{_SCRIPT_PREFIX}.{_customID}";
            //Now that we have the tag, we can start instansiating the stuff that needs it.
            _listener = IGC.RegisterBroadcastListener(_tag);
            _listener.SetMessageCallback(_tag);
            //The log that will give us feedback in the PB's Detail Info area
            _log = new EventLog(_sb, $"Shipware v{_VERSION} - Recent Events", true);
            //The meterMaid that will generate ASCII meters for our tallies
            _meterMaid = new MeterMaid(_sb);
            //If we have a custom tag, we want to have that information front and center in the log
            if (_tag != $"{_SCRIPT_PREFIX}.{_DEFAULT_ID}")
            { _log.scriptTag = _tag; }
            //The distributer that handles updateDelays
            _distributor = new UpdateDistributor(_log);
            //DEBUG USE: The text surface we'll be using for debug prints
            _debugDisplay = Me.GetSurface(0);
            /*
            //Try to initialize the ShieldBroker, which manages the actual ShieldAPI object used by
            //ShieldTally objects.
            shieldBroker = new ShieldBroker(Me);
            */
            //Clear the MyIni we used in this method.
            _iniReadWrite.Clear();
            //Last step is to make some decisions based on the version number.
            if (lastVersion == -1)
            { firstRun = true; }
            else if (lastVersion != _VERSION)
            { textLog.addNote($"Code updated from v{lastVersion} to v{_VERSION}."); }

            _log.add("Script initialization complete.");
        }

        public void firstRunSetup(LimitedMessageLog textLog)
        {
            //The only thing we really need to do in a firstRun scenario is add the SW.Init section.
            //But we can only do that if we can make sense of what's on the PB.
            MyIniParseResult parseResult;
            string initTag = "SW.Init";
            if (!_iniReadWrite.TryParse(Me.CustomData, out parseResult))
            {
                textLog.addError($"Cannot generate a {initTag} section because the parser encountered " +
                    $"an error on line {parseResult.LineNo} of the Programmable Block's config: {parseResult.Error}");
            }
            else
            {
                _iniReadWrite.Set(initTag, "ColorOptimal", "Green");
                _iniReadWrite.Set(initTag, "ColorNormal", "LightBlue");
                _iniReadWrite.Set(initTag, "ColorCaution", "Yellow");
                _iniReadWrite.Set(initTag, "ColorWarning", "Orange");
                _iniReadWrite.Set(initTag, "ColorCritical", "Red");
                Me.CustomData = _iniReadWrite.ToString();
            }
            textLog.addNote("Use the AutoPopulate command to generate basic configuration.");
            textLog.addNote("The Clone command can quickly distribute config across identical blocks.");
            textLog.addNote("The Evaluate command scans the grid for config and loads it into memory.");
            string outcome = $"First run complete.\nThe following messages were logged:\n{textLog.notesToString()}";
            if (textLog.getErrorTotal() > 0)
            { outcome += $"The following errors were logged:\n{textLog.errorsToString()}"; }
            _log.add(outcome);
            //We'll let the call at the end of Program() echo the log.
        }

        public void Save()
        {
            //Clear out any data that may be lurking in the iniReader (It should be clear already, 
            //but for this, we want to be certain.)
            _iniReadWrite.Clear();
            //Store the version number of this code in the Config section.
            _iniReadWrite.Set("Data", "Version", _VERSION);
            //Store the ID of this instance of the script as well.
            _iniReadWrite.Set("Data", "ID", _customID);
            //Store the current value of the update delay
            _iniReadWrite.Set("Data", "UpdateDelay", _distributor.updateDelay);
            if (_sets != null)
            {
                //For every ActionSet named in our sets dictionary...
                foreach (ActionSet set in _sets.Values)
                //Add an entry to the ActionSets section, with the name of the set as the key, storing
                //the current status of the set.
                { _iniReadWrite.Set("ActionSets", set.programName, set.isOn); }
            }
            if (_triggers != null)
            {
                foreach (Trigger trigger in _triggers)
                //Add an entry to the Triggers section, with the name of this trigger as the key, 
                //storing if the trigger is currently armed or disarmed.
                { _iniReadWrite.Set("Triggers", trigger.programName, trigger.enabled); }
            }
            if (_MFDs != null)
            {
                //For every MFD named in our MFD dictionary...
                foreach (MFD mfd in _MFDs.Values)
                //Add an entry to the MFDs section, with the name of the MFD as the key, storing
                //the current page shown by the MFD.
                { _iniReadWrite.Set("MFDs", mfd.programName, mfd.pageName); }
            }
            //Commit the contents of the iniReader to the Storage string
            Storage = _iniReadWrite.ToString();
            //Clear the contents of the MyIni object. Just because this script is probably about to
            //die is no reason not to be tidy. And it may save us some grief if it turns out the 
            //script isn't about to die.
            _iniReadWrite.Clear();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            MyCommandLine argReader = null;
            Func<string, bool> tryParseArgumentsRetainCase = (args) =>
            {
                argReader = new MyCommandLine();
                return (argReader.TryParse(args));
            };
            Func<string, bool> tryParseArgumentsAsLower = (args) =>
            {
                argReader = new MyCommandLine();
                return (argReader.TryParse(args.ToLowerInvariant()));
            };
            //Now incorporating the mildly heretical art of bitwise comparitors.
            //Is this the update tic?
            if ((updateSource & UpdateType.Update100) != 0)
            {
                //Notify the distributor that a tic has occurred. If it's time for an update...
                if (_distributor.tic())
                {
                    compute();
                    update();
                    //And tell the log about it
                    _log.tic();
                }
            }
            //Is this us trying to spread out some of our work load with a delayed evaluate?
            else if ((updateSource & UpdateType.Once) != 0)
            { evaluateFull(new LimitedMessageLog(_sb, 15)); }
            //Is this the IGC wanting attention?
            else if ((updateSource & UpdateType.IGC) != 0)
            {
                //As long as we have messages waiting for us...
                while (_listener.HasPendingMessage)
                {
                    MyIGCMessage message = _listener.AcceptMessage();
                    if (message.Tag == _tag)
                    {
                        //Pull the data from the message
                        string data = message.Data.ToString();

                        //Try to parse the data with the ArgReader. If it works...
                        if (tryParseArgumentsAsLower(data))
                        {
                            string outcome = "No reply";
                            string replyChannel = null;
                            bool receivingReply = false;
                            //Make our replyChannel equal to the first switch we come to.
                            //This is really not how switches are meant to be used, but it's worked so far.
                            Action setSwitchAsReplyChannel = () =>
                            {
                                MyCommandLine.SwitchEnumerator switches = argReader.Switches.GetEnumerator();
                                switches.MoveNext();
                                replyChannel = switches.Current;
                            };

                            //If the first word in the IGC message is 'reply'...
                            if (argReader.Argument(0) == "reply")
                            {
                                receivingReply = true;
                                //We'll do a bit of redundant work and re-parse the originl argument.
                                //Case sensitivity isn't required here, but the string we're getting
                                //will likely have some formatting to it.
                                //Also, if we've made it here, we know it's parsable.
                                tryParseArgumentsRetainCase(data);
                                //The first argument is what tells us this is the reply. But the rest
                                //of the text is what we're interested in.
                                data = data.Replace(argReader.Argument(0), "");
                                data = data.Trim();
                                if (argReader.Switches.Count == 1)
                                {
                                    setSwitchAsReplyChannel();
                                    //The switch has done its job. Cut it from the message.
                                    data = data.Replace($"-{replyChannel}", "");
                                    data = data.Trim();
                                    outcome = $"Received IGC reply from {replyChannel}: {data}";
                                }
                                else
                                { outcome = ($"Received IGC reply: {data}"); }
                            }
                            //If the first argument is 'action'
                            else if (argReader.Argument(0) == "action")
                            {
                                //Did we get the correct number of arguments?
                                if (argReader.ArgumentCount == 3)
                                {
                                    //Run tryTakeAction with the directed command and store the
                                    //result in outcome
                                    outcome = tryMatchAction(argReader.Argument(1),
                                        argReader.Argument(2), "IGC-directed ");
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
                            { outcome = tryMatchAction(argReader.Argument(0), "switch", "IGC-directed "); }
                            //If we have no idea what's happening
                            else
                            {
                                outcome = $"Received the following unrecognized command from the IGC:" +
                                    $" '{data}'.";
                            }
                            //If this isn't a reply we're receiving, and we're supposed to send a reply message...
                            if (!receivingReply && argReader.Switches.Count == 1)
                            {
                                tryParseArgumentsRetainCase(data);
                                setSwitchAsReplyChannel();
                                IGC.SendBroadcastMessage(replyChannel, $"reply {outcome} -{_tag}");
                                //DEBUG USE
                                outcome += $"\nSent reply on channel {replyChannel}.";
                            }
                            //Add an entry to the local log
                            _log.add(outcome);
                        }
                        //If we couldn't parse the data...
                        else
                        {
                            _log.add($"Received IGC-directed command '{data}', which couldn't be " +
                                $"handled by the argument reader.");
                        }
                    }
                }
            }
            //Is this a 'Run' command?
            else
            {
                //Did we get arguments?
                if (tryParseArgumentsAsLower(argument))
                {
                    //The first argument of a run command will (hopefully) tell us what we need to 
                    //be doing.
                    string command = argReader.Argument(0);
                    //A string that will hold error messages, if needed.
                    string trouble = "";
                    string outcome = "";
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
                        //Argument example: IGC SW.Crab Action Turrets Switch -SW.Wyvern
                        case "igc":
                            //<20240411>
                            //We want to retain the original string's capitalization here, just in 
                            //case we're sending a message to a case-sensitive receiver. So we'll 
                            //be a little bit redundant and re-parse the arguments
                            tryParseArgumentsRetainCase(argument);
                            //The rest of this I wrote a long time ago. I don't really remember why
                            //it's like it is (I think it's to retain as much of the formatting of 
                            //the original message as possible) but it works so I'm going to go out 
                            //of my way to not disturb it.
                            //</20240411>
                            //string data = argument.Replace($"IGC", "");
                            //Remove the characters for the IGC command, plus the first space.
                            string data = argument.Remove(0, 4);
                            //Argument 1 is the channel we're sending the message on. We don't need
                            //it in the message itself.
                            data = data.Replace(argReader.Argument(1), "");
                            data = data.Trim();
                            IGC.SendBroadcastMessage(argReader.Argument(1), data);
                            _log.add($"Sent the following IGC message on channel '{argReader.Argument(1)}'" +
                                $": {data}.");
                            break;

                        //Controls an MFD
                        //Argument format: MFD <name> <command>
                        //Argument example: MFD MainScreen Next
                        case "mfd":
                            //If the user has given us the correct number of arguments...
                            if (argReader.ArgumentCount == 3)
                            {
                                string MFDTarget = argReader.Argument(1);
                                string MFDPageCommand = argReader.Argument(2);
                                if (_MFDs == null)
                                { _log.add($"Received MFD command, but script configuration isn't loaded."); }
                                //If we have MFDs, and we actually know what MFD the user is talking about...
                                else if (_MFDs.ContainsKey(MFDTarget))
                                {
                                    //If it's one of the easy commands...
                                    //Note: Performing toLowerInvariant in the checks is intentional.
                                    //PageCommand could also include the name of a specific page,
                                    //and the dictionary that page is stored in is case-sensitive.
                                    if (MFDPageCommand == "next")
                                    { _MFDs[MFDTarget].flipPage(true); }
                                    else if (MFDPageCommand == "prev")
                                    { _MFDs[MFDTarget].flipPage(false); }
                                    //If it isn't one of the easy commands, assume the user is trying 
                                    //to set the MFD to a specific page.
                                    else
                                    {
                                        //If the MFD declines to set the page to the one named in 
                                        //the command...
                                        if (!_MFDs[MFDTarget].trySetPage(MFDPageCommand))
                                        {
                                            //... Complain.
                                            _log.add($"Received command to set MFD '{MFDTarget}' to unknown " +
                                                $"page '{MFDPageCommand}'.");
                                        }
                                    }
                                }
                                //If we don't know what MFD the user is talking about, complain.
                                else
                                { _log.add($"Received '{MFDPageCommand}' command for un-recognized MFD '{MFDTarget}'."); }
                            }
                            //If the user did not give us the correct number of arguments, complain.
                            else
                            { _log.add($"Received MFD command with an incorrect number of arguments."); }
                            break;

                        //Manipulates an ActionSet
                        //Argument format: Action <name> <command>
                        //Argument example: Action GateBay1 Switch
                        case "action":
                            //If the user has given us the correct number of arguments...
                            if (argReader.ArgumentCount == 3)
                            {
                                outcome = tryMatchAction(argReader.Argument(1),
                                    argReader.Argument(2), "");
                                //If something happened that we need to tell the user about...
                                if (!String.IsNullOrEmpty(outcome))
                                { _log.add(outcome); }
                            }
                            //If the user did not give us the correct number of arguments, complain.
                            else
                            { _log.add($"Received Action command with an incorrect number of arguments."); }
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
                                if (_raycasters == null)
                                { _log.add($"Received Racast command, but script configuration isn't loaded."); }
                                //Check to see if we have a raycaster by that name
                                else if (_raycasters.ContainsKey(raycasterName))
                                //Order the named Raycaster to perform a scan.
                                { _raycasters[raycasterName].scan(); }
                                //If we don't have a raycaster by that name, complain.
                                else
                                { _log.add($"Received Raycast command for un-recognized Raycaster '{raycasterName}'."); }
                            }
                            //If the user did not give us the correct number of arguments, complain.
                            else
                            { _log.add($"Received Raycast command with an incorrect number of arguments."); }
                            break;

                        //Re-creates the configuration found in the PB's Init section from the 
                        //information stored in memory. Pretty narrow use case, but with writeConfig,
                        //we had the technology.
                        //Argument format: Reconstititute
                        case "reconstitute":
                            //Before we do this, we need to make sure there's actually some data to write
                            if (!_haveGoodConfig)
                            {
                                _log.add("Received Reconstitute command, but there is no last-good " +
                                    "config to reference. Please only use this command after the " +
                                    "script has successfully evaluated.");
                            }
                            else if (!argReader.Switch("force"))
                            {
                                _log.add("Received Reconstitute command, which will regenerate " +
                                    "declarations based on config that was read " +
                                    $"{(DateTime.Now - _lastGoodConfigStamp).Minutes} minutes ago " +
                                    $"({_lastGoodConfigStamp.ToString("HH: mm: ss")}). If this is " +
                                    "acceptable, re-run this command with the -force flag.");
                            }
                            else
                            {
                                Me.CustomData = $"{_nonDeclarationPBConfig}\n";
                                if (!string.IsNullOrEmpty(_nonDeclarationPBConfig))
                                { Me.CustomData += ";=======================================\n\n"; }
                                Me.CustomData += writeDeclarations(_tallies.ToList(), _sets.Values.ToList(),
                                    _triggers.ToList(), _raycasters.Values.ToList());
                                _log.add($"Carried out Reconstitute command. PB config has been reverted " +
                                    $"to last known good.");
                            }
                            break;

                        //Simply replace the CustomData on blocks in the Target group with the CustomData
                        //from the first block in the Source group
                        //Argument format: Clone
                        case "clone":
                            List<IMyTerminalBlock> cloneBlocks = new List<IMyTerminalBlock>();
                            trouble = "Clone command";
                            if (!tryGetBlocksByGroupName(_SOURCE_GROUP_NAME, cloneBlocks, ref trouble))
                            //If there is no Source group on the grid, complain
                            { _log.add(trouble); }
                            else
                            {
                                IMyTerminalBlock cloneTemplate = cloneBlocks[0];
                                //Pretty sure I don't need to manually clear this, but I'm going to anyway.
                                cloneBlocks.Clear();
                                if (!tryGetBlocksByGroupName(_TARGET_GROUP_NAME, cloneBlocks, ref trouble))
                                //If there is no Target group on the grid, complain
                                { _log.add(trouble); }
                                else
                                {
                                    foreach (IMyTerminalBlock block in cloneBlocks)
                                    { block.CustomData = cloneTemplate.CustomData; }
                                    _log.add($"Carried out Clone command, replacing the CustomData " +
                                        $"of {cloneBlocks.Count} blocks in the {_TARGET_GROUP_NAME} " +
                                        $"group with the CustomData from block '{cloneTemplate.CustomName}'.");
                                }
                            }
                            break;

                        //Deletes the contents of CustomData for every block in the Target group
                        //Argument format: TacticalNuke (Flag)
                        //Argument example: TacticalNuke -confirm
                        case "tacticalnuke":
                            if (argReader.Switch("force"))
                            {
                                List<IMyTerminalBlock> tacBlocks = new List<IMyTerminalBlock>();
                                trouble = "TacticalNuke command";
                                if (!tryGetBlocksByGroupName(_TARGET_GROUP_NAME, tacBlocks, ref trouble))
                                //If there is no Target group on the grid, complain
                                { _log.add(trouble); }
                                else
                                {
                                    foreach (IMyTerminalBlock block in tacBlocks)
                                    { block.CustomData = ""; }
                                    _log.add($"Carried out TacticalNuke command, clearing the " +
                                        $"CustomData of {tacBlocks.Count} blocks.");
                                }
                            }
                            else
                            {
                                _log.add("Received TacticalNuke command. TacticalNuke will remove " +
                                    $"ALL CustomData from blocks in the {_TARGET_GROUP_NAME} group. " +
                                    "If you are certain you want to do this, run the command with the " +
                                    "-force switch.");
                            }
                            break;

                        //Prints a list of properties of every block type in the Source group to
                        //the log.
                        //Argument format: TerminalProperties
                        case "terminalproperties":
                            //Is there a Target group on the grid?
                            List<IMyTerminalBlock> propBlocks = new List<IMyTerminalBlock>();
                            trouble = "TerminalProperties command";
                            if (!tryGetBlocksByGroupName(_SOURCE_GROUP_NAME, propBlocks, ref trouble))
                            //If there is no Target group on the grid, complain
                            { _log.add(trouble); }
                            else
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
                                string[] propPath;
                                //Use the data we've collected to compile a list of properties for 
                                //the various block types in Target
                                foreach (KeyValuePair<Type, string> entry in popPropties)
                                //{ _sb.Append($"{entry.Key}\n{entry.Value}"); }
                                {
                                    //The block type we're using for the key of this entry contains 
                                    //the entire path of that object. We really only want the last
                                    //bit.
                                    propPath = entry.Key.ToString().Split('.');
                                    _sb.Append($"Properties for '{propPath[propPath.Length - 1]}'\n{entry.Value}");
                                }
                                _log.add(_sb.ToString());
                                _sb.Clear();
                            }
                            break;

                        case "typedefinitions":
                            List<IMyTerminalBlock> defBlocks = new List<IMyTerminalBlock>();
                            trouble = "TypeDefinitions command";
                            //Is there a Source group on the grid?
                            if (!tryGetBlocksByGroupName(_SOURCE_GROUP_NAME, defBlocks, ref trouble))
                            //If there is no Source group on the grid, complain
                            { _log.add(trouble); }
                            else
                            {
                                //Prepare for the -items flag
                                bool includeItems = argReader.Switch("items");
                                List<MyInventoryItem> items = new List<MyInventoryItem>();
                                string[] defPath;
                                _sb.Clear();
                                _sb.Append($"Type Definitions for members of the {_SOURCE_GROUP_NAME} group:\n");
                                //Unlike what we did with Properties, we won't try to filter out 
                                //duplicate entries. If the user runs this on the whole grid, they 
                                //can deal with the consequences.
                                foreach (IMyTerminalBlock block in defBlocks)
                                {
                                    defPath = block.GetType().ToString().Split('.');
                                    _sb.Append(
                                        $" {block.CustomName}:\n" +
                                        //The string of GetType includes the entire pedigree of the 
                                        //object. We just want the last bit.
                                        $"  Interface: {defPath[defPath.Length - 1]}\n" +
                                        $"  TypeID: {block.BlockDefinition.TypeIdString}\n" +
                                        $"  SubTypeID: {block.BlockDefinition.SubtypeId}\n" +
                                        $"\n");
                                    if (includeItems && block.HasInventory)
                                    {
                                        block.GetInventory().GetItems(items);
                                        _sb.Append("  Items:\n");
                                        foreach (MyInventoryItem item in items)
                                        {
                                            _sb.Append($"   Name: {item.Type.ToString()}\n");
                                            _sb.Append($"    TypeID: {item.Type.TypeId}\n");
                                            _sb.Append($"    SubTypeID: {item.Type.SubtypeId}\n");
                                        }
                                    }
                                }
                                _log.add(_sb.ToString());
                                _sb.Clear();
                            }
                            break;

                        case "surfacescripts":
                            List<string> scripts = new List<string>();
                            Me.GetSurface(0).GetScripts(scripts);
                            _sb.Clear();
                            _sb.Append("Available scripts:\n");
                            foreach (string script in scripts)
                            { _sb.Append($"  {script}\n"); }
                            _log.add(_sb.ToString());
                            _sb.Clear();
                            break;

                        //Search the grid for block types compatible without tallies, and automatically
                        //write the configuration needed to make them work.
                        //Argument format: AutoPopulate
                        case "autopopulate":
                            MyIniParseResult parseResult;
                            //The only thing that can shut down AutoPopulate is not being able to
                            //read the PB. So if we can't, don't bother with anything else.
                            if (!_iniReadWrite.TryParse(Me.CustomData, out parseResult))
                            {
                                _log.add("Received AutoPopulate command, but was unable to carry it " +
                                    $"out due to a parsing error on line {parseResult.LineNo} of the " +
                                    $"Programmable Block's config: {parseResult.Error}");
                            }
                            else
                            {
                                //We want to do all our filtering in the initial get from the GTS. 
                                //That means we need our blacklisted block lists in hand before we
                                //start.
                                HashSet<string> excludedTypes = getAPExclusionsFromInit("APExcludedBlockTypes");
                                HashSet<string> excludedSubTypes = getAPExclusionsFromInit("APExcludedBlockSubTypes");

                                //There are two ways we can get AP's block list from the GTS, and that
                                //hinges on whether or not the target flag is set.
                                string mode = "AutoPopulate";
                                List<IMyTerminalBlock> apBlocks = new List<IMyTerminalBlock>();
                                if (argReader.Switch("target"))
                                {
                                    IMyBlockGroup targetGroup = GridTerminalSystem.GetBlockGroupWithName(_TARGET_GROUP_NAME);
                                    if (targetGroup == null)
                                    {
                                        _log.add("Received AutoPopulate command with the -target flag set, " +
                                            $"but there is no {_TARGET_GROUP_NAME} block group on the grid.");
                                        break;
                                    }
                                    else
                                    {
                                        targetGroup.GetBlocks(apBlocks, b => b.IsSameConstructAs(Me)
                                            && !excludedTypes.Contains(b.BlockDefinition.TypeIdString)
                                            && !excludedSubTypes.Contains(b.BlockDefinition.SubtypeId)
                                            && !MyIni.HasSection(b.CustomData, $"{_SCRIPT_PREFIX}.APIgnore"));
                                        mode = "Targeted AutoPopulate";
                                    }
                                }
                                else
                                {
                                    findBlocks<IMyTerminalBlock>(apBlocks, b => b.IsSameConstructAs(Me)
                                        && !excludedTypes.Contains(b.BlockDefinition.TypeIdString)
                                        && !excludedSubTypes.Contains(b.BlockDefinition.SubtypeId)
                                        && !MyIni.HasSection(b.CustomData, $"{_SCRIPT_PREFIX}.APIgnore"));
                                }

                                //_iniReadWrite is a global, so we don't actually need to pass it in. 
                                //Just doing this to make it clear that it's already loaded with data.
                                bool apSuccessful = AutoPopulate(apBlocks, _iniReadWrite, mode, ref outcome);
                                _log.add(outcome);

                                if (apSuccessful)
                                {
                                    //Queue up an evaluate
                                    Save();
                                    Runtime.UpdateFrequency = UpdateFrequency.Once;
                                }
                            }
                            break;

                        case "apexclusionreport":
                            //The first parts of this method are basically identical to the base 
                            //AutoPopulate method. And that means we need to be able to read the PB.
                            if (!_iniReadWrite.TryParse(Me.CustomData, out parseResult))
                            {
                                _log.add("Received APExclusionReport command, but was unable to carry it " +
                                    $"out due to a parsing error on line {parseResult.LineNo} of the " +
                                    $"Programmable Block's config: {parseResult.Error}");
                            }
                            else
                            {
                                //_debugDisplay.WriteText("Report variable init\n");
                                HashSet<string> excludedTypes = getAPExclusionsFromInit("APExcludedBlockTypes");
                                Dictionary<string, int> typeDiectionary = excludedTypes.ToDictionary(h => h, h => 0);
                                HashSet<string> excludedSubTypes = getAPExclusionsFromInit("APExcludedBlockSubTypes");
                                Dictionary<string, int> subTypeDiectionary = excludedSubTypes.ToDictionary(h => h, h => 0);
                                int ignoreCount = 0;
                                int typeCount = 0;
                                int subTypeCount = 0;

                                //_debugDisplay.WriteText("Getting blocks\n", true);
                                List<IMyTerminalBlock> reportBlocks = new List<IMyTerminalBlock>();
                                findBlocks<IMyTerminalBlock>(reportBlocks, b => b.IsSameConstructAs(Me));

                                //_debugDisplay.WriteText("Analyzing blocks\n", true);
                                foreach (IMyTerminalBlock block in reportBlocks)
                                {
                                    if (MyIni.HasSection(block.CustomData, $"{_SCRIPT_PREFIX}.APIgnore"))
                                    { ignoreCount++; }
                                    if (typeDiectionary.ContainsKey(block.BlockDefinition.TypeIdString))
                                    {
                                        //_debugDisplay.WriteText($"Found block with type {block.BlockDefinition.TypeIdString}\n", true);
                                        typeDiectionary[block.BlockDefinition.TypeIdString]++;
                                        typeCount++;
                                    }
                                    if (subTypeDiectionary.ContainsKey(block.BlockDefinition.SubtypeId))
                                    {
                                        //_debugDisplay.WriteText($"Found block with subtype {block.BlockDefinition.SubtypeId}\n", true);
                                        subTypeDiectionary[block.BlockDefinition.SubtypeId]++;
                                        subTypeCount++;
                                    }
                                }

                                //_debugDisplay.WriteText("Writing report\n", true);
                                outcome = $"Carried out APExclusionReport command. Of the {reportBlocks.Count} " +
                                    $"TerminalBlocks on this construct, the following {ignoreCount + typeCount + subTypeCount} " +
                                    $"blocks are being excluded from consideration by AutoPopulate:\n";
                                outcome += $" -{ignoreCount} blocks excluded by APIgnore\n";

                                outcome += $" -{typeCount} blocks excluded by type\n";
                                foreach (KeyValuePair<string, int> pair in typeDiectionary)
                                { outcome += $"  >{pair.Value} {pair.Key}\n"; }

                                outcome += $" -{subTypeCount} blocks excluded by subype\n";
                                foreach (KeyValuePair<string, int> pair in subTypeDiectionary)
                                { outcome += $"  >{pair.Value} {pair.Key}\n"; }

                                _log.add(outcome);
                            }
                            break;

                        //Clears Shipware sections and their contents from the members of the 
                        //'Target' group on the grid
                        //Argument format: Clear
                        case "clear":
                            List<IMyTerminalBlock> clearBlocks = new List<IMyTerminalBlock>();
                            trouble = "Clear command";
                            if (!tryGetBlocksByGroupName(_TARGET_GROUP_NAME, clearBlocks, ref trouble))
                            //If there is no Target group on the grid, complain
                            { _log.add(trouble); }
                            else
                            {
                                List<string> sectionNames = new List<string>();
                                string[] splitName;
                                int clearCounter = 0;
                                foreach (IMyTerminalBlock block in clearBlocks)
                                {
                                    //Pull information from this block.
                                    _iniReadWrite.TryParse(block.CustomData);
                                    _iniReadWrite.GetSections(sectionNames);
                                    foreach (string targetSection in sectionNames)
                                    {
                                        splitName = targetSection.Split('.');
                                        //All Shipware sections will have this prefix.
                                        if (splitName[0] == _SCRIPT_PREFIX)
                                        {
                                            //TODO: Monitor. I'm pretty sure DeleteSection can be trusted
                                            //for this.
                                            _iniReadWrite.DeleteSection(targetSection);
                                            clearCounter++;
                                        }
                                    }
                                    //Replace the block's CustomData with our altered configuration.
                                    block.CustomData = _iniReadWrite.ToString();
                                }
                                //Clear our MyIni.
                                _iniReadWrite.Clear();
                                _log.add($"Clear command executed on {clearBlocks.Count} blocks. Removed " +
                                    $"{clearCounter} Shipware sections.");
                            }
                            break;

                        //Change the ID of this script, and updates the configuration of every block 
                        //on the grid to use the new ID.
                        //Argument format: ChangeID <name>
                        //Argument example: ChangeID Komodo
                        case "changeid":
                            //We want to retain case here, so we'll re-parse the command string
                            tryParseArgumentsRetainCase(argument);
                            //Did the user include a new ID? And nothing else?
                            if (argReader.ArgumentCount == 2)
                            {
                                //Put a handle on the ID the user wants to use
                                string newID = argReader.Argument(1);
                                //...and the new tag, as long as we're at it.
                                string newTag = $"{_SCRIPT_PREFIX}.{newID}";
                                //The first step is actually going to be to talk to the grid and 
                                //figure out which blocks we're going to need to modify.
                                List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
                                //Get all the blocks on this construct using the existing tag.
                                findBlocks<IMyTerminalBlock>(blocks, b =>
                                    (b.IsSameConstructAs(Me) && MyIni.HasSection(b.CustomData, _tag)));
                                //For every block that we found...
                                foreach (IMyTerminalBlock block in blocks)
                                //Replace instances of the old tags with the new tag. Include the 
                                //brackets to decrease the odds of us hitting something we shouldn't
                                { block.CustomData = block.CustomData.Replace($"[{_tag}]", $"[{newTag}]"); }
                                //We also need to re-tag the Init section on the PB
                                //20240411 - No longer a thing, and hasn't been for a while.
                                //Me.CustomData = Me.CustomData.Replace($"[{_tag}Init]", $"[{newTag}Init]");
                                //Now that we've replaced the old tag in the config, go ahead and 
                                //update the tag in memory
                                _customID = newID;
                                //The best way to make sure this sticks and then works properly 
                                //afterward is to fully re-initialize the script.
                                Save();
                                //Initiate was built with the expectation that it would only be run 
                                //from one place, so we're going to need a couple of variable to 
                                //satisfy its method signature
                                LimitedMessageLog textLog;
                                bool firstRun;
                                initiate(out textLog, out firstRun);
                                //Queue up an evaluate
                                Runtime.UpdateFrequency = UpdateFrequency.Once;
                                _log.add($"ChangeID complete, {blocks.Count} blocks modified. The ID " +
                                    $"of this script instance is now '{_customID}', and its tag is now '{_tag}'.");
                            }
                            //If the user included more than one arguement, complain. We don't know 
                            //what to do with spaces.
                            else if (argReader.ArgumentCount > 2)
                            {
                                _log.add($"Received ChangeID command with too many arguments. Note " +
                                    $"that IDs can't contain spaces.");
                            }
                            //If the user did not give us a new ID, complain.
                            else
                            { _log.add($"Received ChangeID command with no new ID."); }
                            break;

                        //Find any config on the grid with an 'SW.Integrate' section header and 
                        //replace it with the script's section header
                        case "integrate":
                            List<IMyTerminalBlock> intBlocks = new List<IMyTerminalBlock>();
                            findBlocks<IMyTerminalBlock>(intBlocks, b =>
                                (b.IsSameConstructAs(Me) && MyIni.HasSection(b.CustomData, $"{_SCRIPT_PREFIX}.Integrate")));
                            foreach (IMyTerminalBlock block in intBlocks)
                            { block.CustomData = block.CustomData.Replace($"[{_SCRIPT_PREFIX}.Integrate]", $"[{_tag}]"); }
                            _log.add($"Carried out Integrate command, replacing the '{_SCRIPT_PREFIX}.Integrate' " +
                                $"section headers on {intBlocks.Count} blocks with '{_tag}' headers.");
                            break;

                        //Run the evaluate() method, checking for any changes to the grid or the 
                        //CustomData of its blocks.
                        case "evaluate":
                            //Evaluate will pull the state of ActionSets from the Storage string, 
                            //better make sure that's up to date
                            Save();
                            //Now we should be able to safely call Evaluate.
                            evaluateFull(new LimitedMessageLog(_sb, 15));
                            break;

                        //Force a call to setProfile on all this script's reports. This will re-set 
                        //every surface to its stored settings, but it will also send temporary 
                        //sprites in place of each Report element; hopefully forcing the server to
                        //update them
                        case "resetreports":
                            foreach (IReportable reportable in _reports)
                            { reportable.setProfile(); }
                            _log.add($"Carried out ResetReports command, re-applying text surface " +
                                $"variables of {_reports.Length} Reports.");
                            break;

                        //If the user just /has/ to have an update, right now, for some reason, we
                        //can accomodate them. In theory, this could also be used to force an update
                        //when the script is partially configured, which could be extremely helpful or
                        //terrible, depending on the circumstances.
                        case "update":
                            compute();
                            //The -force switch uses forceUpdate instead of regular update
                            if (argReader.Switch("force"))
                            {
                                foreach (IReportable report in _reports)
                                { report.forceUpdate(); }
                                foreach (Indicator indicator in _indicators)
                                { indicator.update(); }
                            }
                            else
                            { update(); }
                            //The -performance switch logs the performance impact of this update
                            if (argReader.Switch("performance"))
                            {
                                _log.add($"Update used {Runtime.CurrentInstructionCount} / {Runtime.MaxInstructionCount} " +
                                    $"({(int)(((double)Runtime.CurrentInstructionCount / Runtime.MaxInstructionCount) * 100)}%) " +
                                    $"of instructions allowed in this tic.\n");
                            }
                            break;

                        //Test function. What exactly it does changes from day to day.
                        case "test":
                            //evaluate();
                            _log.add("Test function executed.");
                            break;

                        //If we don't know what the user is telling us to do, complain.
                        default:
                            _log.add($"Received un-recognized run command '{command}'.");
                            break;
                    }
                }
            }
            //Re-echo the event log
            Echo(_log.toString());
            //We only 'claim' updates at the end of update tics. That way, everything else gets a 
            //crack at them.
            if ((updateSource & UpdateType.Update100) != 0)
            {
                foreach (Raycaster caster in _raycasters.Values)
                { caster.updateClaimed(); }
                _log.updateClaimed();
            }
        }

        //Polls tally-related blocks for the freshest data
        public void compute()
        {
            //CLEAR ALL THE OLD VALUES
            foreach (Tally tally in _tallies)
            { tally.clearCurr(); }
            //CALCULATE ALL THE NEW VALUES
            foreach (Container container in _containers)
            { container.sendCurrToTallies(); }
            foreach (Tally tally in _tallies)
            { tally.compute(); }
            //Now that all the data has been collected, we can make decisions
            ActionSet targetSet = null;
            bool actionRequired, desiredState;
            foreach (Trigger trigger in _triggers)
            {
                actionRequired = trigger.check(out targetSet, out desiredState);
                if (actionRequired)
                {
                    _log.add(tryTakeAction(targetSet, desiredState, desiredState ? "on" : "off",
                        $"Trigger {trigger.programName}'s "));
                }
            }
        }

        //Displays status information about Talies and ActionSets on Reports and Indicators.
        public void update()
        {
            //WRITE ALL THE REPORTS
            foreach (IReportable report in _reports)
            { report.update(); }
            //COLOR ALL THE LIGHTS
            foreach (Indicator indicator in _indicators)
            { indicator.update(); }
        }

        //Attempt to parse an action command. If that is successful, tryTakeAction will be called to
        //execute that command. 
        //string actionTarget: The name of the ActionSet the user is trying to operate.
        //string actionCommand: The command that is to be performed on the ActionSet.
        //string source: The source of the command, used to make error messages more informative.
        //  Left blank for run commands, "IGC-directed " (Note the space!) for the IGC.
        //Returns: A string indicating what the method thinks happened
        public string tryMatchAction(string actionTarget, string actionCommand, string source)
        {
            ActionSet targetSet;
            bool desiredState;
            if (_sets == null)
            { return "Received Action command, but script configuration isn't loaded."; }
            //If we actually know what ActionSet the user is talking about...
            else if (_sets.TryGetValue(actionTarget, out targetSet))
            {
                //And we know what command the user is trying to give us...
                if (actionCommand == "on")
                { desiredState = true; }
                else if (actionCommand == "off")
                { desiredState = false; }
                else if (actionCommand == "switch")
                { desiredState = !targetSet.isOn; }
                //If we don't know the command, complain. 
                else
                {
                    return $"Received unknown {source}command '{actionCommand}' for ActionSet " +
                        $"'{actionTarget}'. Valid commands for ActionSets are 'On', 'Off', and " +
                        $"'Switch'.";
                }
                //We have a target set and we know what state we want it in. Time to go to work.
                return tryTakeAction(targetSet, desiredState, actionCommand, source);
            }
            //If we don't know what ActionSet the user is talking about, complain.
            else
            {
                return $"Received {source}command '{actionCommand}' for un-recognized " +
                    $"ActionSet '{actionTarget}'.";
            }
        }

        //Attempt to change the state of an ActionSet, and handle any exceptions that may occur
        //ActionSet targetSet: The ActionSet to be manipulated.
        //bool desiredState: Which state the targetSet is to be set to. 'true' for on, 'false' for off.
        //string command: A descriptor of the original command issued to the set, used for messaging.
        //string source: A descriptor of the command's source (IGC, Trigger, etc). If blank, an 
        //  outcome string will only be returned if something goes wrong.
        //Returns: A string indicating what the method thinks happened
        public string tryTakeAction(ActionSet targetSet, bool desiredState, string command, string source)
        {
            string outcome = "";
            try
            { targetSet.setState(desiredState); }
            catch (InvalidCastException e)
            {
                string identifier = "<ID not provided>";
                if (e.Data.Contains("Identifier"))
                { identifier = $"{e.Data["Identifier"]}"; }
                outcome = $"An invalid cast exception occurred while running {source}'{command}' " +
                    $"command for ActionSet '{targetSet.programName}' at {identifier}. Make sure " +
                    $"the action specified in configuration can be performed by {identifier}.";
            }
            catch (InvalidOperationException e)
            {
                string trace = "<Trace failed>";
                //The Data dictionary should contain a 'stack trace' of ActionSets that led to the 
                //potential loop
                //The counter is the number of ActionSets involved (Starting at 0). Each ActionSet 
                //involved also has an entry in the list, with the key being its position in the 
                //stack. Zero is the ActionSet in which the fault occurs, and the numbering ascends
                //from there.
                //So to get a reasonable path, we need to start at the 'end' and work our way back.
                if (e.Data.Contains("Counter"))
                {
                    trace = "Set Trace:\n";
                    for (int i = (int)(e.Data["Counter"]); i >= 0; i--)
                    //The value in each of these numbered entries is the program name of the ActionSet
                    { trace += $"{e.Data[i]}\n"; }
                }
                outcome = $"A possible loop was detected while running {source}'{command}' command " +
                    $"for ActionSet '{targetSet.programName}'. Make sure {targetSet.programName} is " +
                    $"not being called by one of the sets it is calling.\n\n{trace}";
            }
            catch (Exception e)
            {
                string identifier = "<ID not provided>";
                if (e.Data.Contains("Identifier"))
                { identifier = $"{e.Data["Identifier"]}"; }
                outcome = $"An exception occurred while running {source}'{command}' command for " +
                    $"ActionSet '{targetSet.programName}' at {identifier}.\n  Raw exception message:\n " +
                    $"{e.Message}\n  Stack trace:\n{e.StackTrace}";
            }
            //We'll go ahead and call update() here. Won't be the end of 
            //the world if we waste it on an error.
            update();
            //We may have set some hasActed flags. But we're done now, so reset them.
            foreach (ActionSet set in _sets.Values)
            { set.resetHasActed(); }
            //If we don't have an outcome string yet (Because nothing has gone horribly wrong), and
            //our source string isn't empty (Indicating that we should generate an outcome string)
            if (String.IsNullOrEmpty(outcome) && !String.IsNullOrEmpty(source))
            {
                outcome = $"Carried out {source}command '{command}' for ActionSet '{targetSet.programName}'. " +
                    $"The set's state is now '{targetSet.statusText}'.";
            }
            return outcome;
        }

        //groupName: Name of the group we're looking for
        //retrievedBlocks: List of blocks that will store the group
        //trouble: Takes a string containing a descriptor of the command that sent this request. If
        //  no group by this name is found, will hold an error message to that effect on return.
        public bool tryGetBlocksByGroupName(string groupName, List<IMyTerminalBlock> retrievedBlocks,
            ref string trouble)
        {
            GridTerminalSystem.GetBlockGroupWithName(groupName)?.GetBlocks(retrievedBlocks);
            if (retrievedBlocks.Count > 0)
            { return true; }
            else
            {
                //'trouble' is expected to contain something along the lines of "Clear Command".
                trouble = $"Received {trouble}, but there is no {groupName} block group on the grid.";
                return false;
            }
        }

        //A simple wrapper for GTS.GetBlocksOfType, which I probably don't use enough to justify
        //implementing. But here we are.
        //List<IMyTerminalBlock> blocks: Will hold any blocks we find when we're done.
        //Func collect: Optional checks that will be run to see if we got the right block. 
        //  Defaults to null.
        public void findBlocks<T>(List<T> blocks, Func<T, bool> collect = null) where T : class
        { GridTerminalSystem.GetBlocksOfType<T>(blocks, collect); }

        //Writes all the linked declarations to a single string.
        public string writeDeclarations(List<Tally> tallies, List<ActionSet> actions, List<Trigger> triggers,
            List<Raycaster> raycasters)
        {
            string declarations;
            int counter = 0;
            _sb.Clear();

            foreach (Tally tally in tallies)
            { _sb.Append(tally.writeConfig(counter++)); }
            if (tallies.Count > 0)
            { _sb.Append(";=======================================\n\n"); }
            counter = 0;

            foreach (ActionSet action in actions)
            { _sb.Append(action.writeConfig(counter++)); }
            if (actions.Count > 0)
            { _sb.Append(";=======================================\n\n"); }
            counter = 0;

            foreach (Trigger trigger in triggers)
            { _sb.Append(trigger.writeConfig(counter++)); }
            if (triggers.Count > 0)
            { _sb.Append(";=======================================\n\n"); }
            counter = 0;

            foreach (Raycaster raycaster in raycasters)
            { _sb.Append(raycaster.writeConfig(counter++)); }

            declarations = _sb.ToString();
            _sb.Clear();
            return declarations;
        }

        public bool AutoPopulate(List<IMyTerminalBlock> apBlocks, MyIni pbParse, string mode, ref string outcome)
        {
            MyIni blockParse = _iniRead;
            MyIniParseResult parseResult;
            LimitedMessageLog apLog = new LimitedMessageLog(_sb, 10);

            //We've already handled block blacklisting, but we still need to read and handle the
            //contents of the |APExcludedDeclarations| key.
            MyIniValue iniValue = _iniReadWrite.Get($"{_SCRIPT_PREFIX}.Init", "APExcludedDeclarations");
            List<string> excludedDeclarations = null;
            if (!String.IsNullOrEmpty(iniValue.ToString()))
            { excludedDeclarations = iniValue.ToString().Split(',').Select(p => p.Trim()).ToList(); }
            //We'll make two (ish) passes through the grid. The first pass is to determine what
            //templates we'll need to be using, the second will be to apply those templates. But 
            //first, we'll need the templates themselves. 
            Dictionary<string, APTemplate> availableTemplates = compileAPTemplates(excludedDeclarations, apLog);

            Dictionary<string, TallyGenericTemplate> tallyGenericsInUse = new Dictionary<string, TallyGenericTemplate>();
            Dictionary<string, TallyInventoryTemplate> tallyInventoriesInUse = new Dictionary<string, TallyInventoryTemplate>();
            Dictionary<string, ActionSetTemplate> actionSetsInUse = new Dictionary<string, ActionSetTemplate>();
            int tallyCount = 0;
            int setCount = 0;
            Action<string, APTemplate> sendTemplateToUsed = (name, t) =>
            {
                if (t is TallyGenericTemplate)
                {
                    tallyGenericsInUse.Add(name, (TallyGenericTemplate)t);
                    tallyCount++;
                }
                else if (t is TallyInventoryTemplate)
                {
                    tallyInventoriesInUse.Add(name, (TallyInventoryTemplate)t);
                    tallyCount++;
                }
                else if (t is ActionSetTemplate)
                {
                    actionSetsInUse.Add(name, (ActionSetTemplate)t);
                    setCount++;
                }
                //Conceivably, a new kind of template could fall through here. But that's going to
                //be a me problem.
                availableTemplates.Remove(name);
            };

            //The first way we can decide we need a given template is if it's already written in 
            //the config. We'll give the PB declarations a pass.
            string declarationSection, programName;
            string declarationType = "Tally";
            int index = 0;
            int currentTallyIndex = 0;
            int currentActionSetIndex = 0;
            //We also need to know specifically if there's already a Roost set, and if so, where.
            int roostIndex = -1;
            bool lookForRoost = false;
            //This will basically be a very cut-down version of the process we use in evaluateDeclarations.
            //Because here, all we want is the name.
            //_debugDisplay.WriteText("Beginning PB declaration pass\n");
            while (index != -1)
            {
                declarationSection = $"{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.{declarationType}{index.ToString("D2")}";
                //_debugDisplay.WriteText($"  Looking for section header {declarationSection}\n", true);
                if (pbParse.ContainsSection(declarationSection))
                {
                    programName = pbParse.Get(declarationSection, "Name").ToString();
                    //_debugDisplay.WriteText($"  Found {declarationType} declaration named {programName}\n", true);
                    if (!string.IsNullOrEmpty(programName) && availableTemplates.ContainsKey(programName))
                    {
                        //_debugDisplay.WriteText($"  >Matched {programName} to existing declaration\n", true);
                        //Once a template is in, we can stop looking for it.
                        sendTemplateToUsed(programName, availableTemplates[programName]);
                    }
                    //Roost is its own thing, so we only need to check to see if we found it if 
                    //we can't match this declaration to a template.
                    else if (lookForRoost && programName == "Roost")
                    {
                        //_debugDisplay.WriteText($"  >Found existing Roost section at index {index}\n", true);
                        roostIndex = index;
                        lookForRoost = false;
                    }
                    index++;
                }
                else
                //The loop is basically the same for Tallies and ActionSets. Only the names have 
                //changed. So instead of having a subroutine that we run through two seperate loops,
                //we'll give this loop two seperate states.
                //Though we do need to do some special handling for Roost.
                {
                    if (declarationType == "Tally")
                    {
                        //If we need to add tallies later on, we'll need to know what index to add 
                        //them at.
                        currentTallyIndex = index;
                        index = 0;
                        declarationType = "ActionSet";
                        lookForRoost = true;
                    }
                    else
                    {
                        currentActionSetIndex = index;
                        index = -1;
                    }
                }
            }

            //DEBUG USE
            /*
            string debugOutcome = "";
            debugOutcome += $"{tallyGenericsInUse.Count + tallyInventoriesInUse.Count + actionSetsInUse.Count} " +
                $"templates identified from existing config.\n" +
                $"Found {currentTallyIndex} existing tallies and {currentActionSetIndex} existing sets.\n";
            foreach (string name in tallyGenericsInUse.Keys)
            { debugOutcome += $"  TallyGeneric {name}\n"; }
            foreach (string name in tallyInventoriesInUse.Keys)
            { debugOutcome += $"  TallyInventory {name}\n"; }
            foreach (string name in actionSetsInUse.Keys)
            { debugOutcome += $"  ActionSet {name}\n"; }
            apLog.addNote(debugOutcome);
            */

            //That should be all the already-configured tallies and sets. Now we need to scan the 
            //grid to see if we need to add to our declarations.
            //We'll be modifying collections on the fly, meaning we can't use a foreach for the 
            //template loop. That means there's a couple of other things to take care of.
            APTemplate template;
            List<APTemplate> templates = availableTemplates.Values.ToList();
            MyDefinitionId blockDef;
            HashSet<MyDefinitionId> alreadyEncounteredTypes = new HashSet<MyDefinitionId>();
            List<MyIniKey> iniKeys = new List<MyIniKey>();
            List<string> iniValues = new List<string>();
            Action<string, MyIni, MyIni> writeConfigToIni = (config, iniParse, iniOut) =>
            {
                //This is straight from the horses's mouth. We'll assume it's good.
                iniParse.TryParse(config);
                iniParse.GetKeys(iniKeys);
                foreach (MyIniKey key in iniKeys)
                { iniValues.Add((blockParse.Get(key)).ToString()); }
                for (int i = 0; i < iniKeys.Count; i++)
                { iniOut.Set(iniKeys[i], iniValues[i]); }
                //We'll be using this variable again, so we'll need to clear it to prevent cross-
                //contamination (MyIni.GetKeys should clear iniKeys for us) 
                iniValues.Clear();
            };
            //_debugDisplay.WriteText("\nBeginning grid identification pass\n", true);
            foreach (IMyTerminalBlock block in apBlocks)
            {
                blockDef = block.BlockDefinition;
                //Before we do anything else, ask: Have we already analyzed this block type?
                if (!alreadyEncounteredTypes.Contains(blockDef))
                {
                    //_debugDisplay.WriteText($" Performing template comparison for novel block {block.CustomName}\n", true);
                    index = 0;
                    //In a real-world setting, the odds of using every single template is /incredibly/
                    //low. But we'll need to account for it anyway.
                    while (index != -1 && templates.Count != 0)
                    {
                        template = templates[index];
                        if (template.blockMatchesConditional(block))
                        {
                            //_debugDisplay.WriteText($"  >Block {block.CustomName} matches conditional of {template.name}\n", true);
                            sendTemplateToUsed(template.name, template);
                            //Again: Once we've identified one use case for this template, we can stop
                            //looking for it. Removing it from the master template dictionary is 
                            //handled by sTTU (It's also moot because we won't use that dictionary again), 
                            //but we still need to remove it from the list we're using for this loop.
                            templates.Remove(template);

                            //Before we move on, we need to actually add this template's declaration 
                            //to the PBconfig. Because templates for existing script objects were removed 
                            //from the pool in the config pass, we shouldn't need to worry about duplicates.
                            //TODO: If I do in fact remove indexes from declarations, this bit could be 
                            //simplified. I only need to know if it's a tally or a set so I know which 
                            //index to use.
                            if (template is ActionSetTemplate)
                            {
                                writeConfigToIni(template.writePBConfig(currentActionSetIndex), blockParse, pbParse);
                                currentActionSetIndex++;
                            }
                            else
                            {
                                writeConfigToIni(template.writePBConfig(currentTallyIndex), blockParse, pbParse);
                                currentTallyIndex++;
                            }

                            //Removing a template from the list is effectively the same as incrementing
                            //the index, so we won't do that on this branch.
                        }
                        else
                        { index++; }
                        if (index >= templates.Count)
                        { index = -1; }
                    }
                    //Last step is to add this definition to our list of already encountered block 
                    //types, so we don't waste time analyzing it again.
                    alreadyEncounteredTypes.Add(blockDef);
                }
            }

            //We have all our templates in hand. Before we finish with the PB, we need to write the
            //write/update the Roost ActionSet and write the generic report that will be displayed
            //on the PB itself.
            //Roost first.
            //There's a couple of questions we need to ask ourselves. First: Did we somehow end
            //up without any ActionSets?
            //_debugDisplay.WriteText("\nSetting up Roost set\n", true);
            if (actionSetsInUse.Count > 0)
            {
                //The next important question: Did we find an existing Roost entry?
                if (roostIndex == -1)
                {
                    ActionSet roostSet = new ActionSet("Roost", false);
                    roostSet.displayName = _customID;
                    roostSet.colorOn = red;
                    roostSet.colorOff = green;
                    roostSet.textOn = "Active";
                    roostSet.textOff = "Roosting";

                    //Have each template tell Roost how to handle it
                    foreach (ActionSetTemplate actionTemplate in actionSetsInUse.Values)
                    { actionTemplate.loadPlansIntoRoostSet(ref roostSet); }

                    //Get the config we need by calling this set's writeConfig method.
                    writeConfigToIni(roostSet.writeConfig(currentActionSetIndex), blockParse, pbParse);
                }
                else
                {
                    //If there's already a Roost set, we'll just check the ActionsLinkedToOn and
                    //ActionsLinkedToOff value to see if our AP sets are all in there.
                    string existingLinks;
                    string roostHeader = $"{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.ActionSet{roostIndex.ToString("D2")}";
                    bool linksEmpty;
                    Action<string, bool> addActionLinksToRoost = (key, isOn) =>
                    {
                        existingLinks = pbParse.Get(roostHeader, key).ToString();
                        //There's a edge case where we we might have a Roost declaraion but not an
                        //ActionsLinkedToOn/Off key or value (Usually due to user tampering with 
                        //the config. The Roost set is only generated if there's an ActionSet to 
                        //link to it, so this should not occur organically). We'll handle it later,
                        //for now, we just need to make a note if we're dealing with an empty string.
                        linksEmpty = String.IsNullOrEmpty(existingLinks);
                        string desiredState;
                        foreach (ActionSetTemplate actionTemplate in actionSetsInUse.Values)
                        {
                            programName = actionTemplate.name;
                            desiredState = actionTemplate.getStateWhenRoost(isOn);
                            if (!existingLinks.Contains(programName) && !String.IsNullOrEmpty(desiredState))
                            { existingLinks += $", {programName}: {desiredState}"; }
                        }
                        //If we started this process with an empty string, we're going to have a 
                        //comma and a space hanging off the front of our value. Trim them.
                        if (linksEmpty)
                        { existingLinks = existingLinks.Remove(0, 1); }
                        //Send the value back to the PB parse.
                        pbParse.Set(roostHeader, key, existingLinks);
                    };
                    addActionLinksToRoost("ActionSetsLinkedToOn", true);
                    addActionLinksToRoost("ActionSetsLinkedToOff", false);
                }
            }

            //Next, the generic report
            //_debugDisplay.WriteText("\nWriting APScreen MFD\n", true);
            string sectionName = "";
            string elementNames;
            Action<string> setCommonSurfaceConfig = (title) =>
            {
                pbParse.Set(sectionName, "Title", title);
                pbParse.Set(sectionName, "Columns", "3");
                pbParse.Set(sectionName, "FontSize", ".5");
                pbParse.Set(sectionName, "ForeColor", "Yellow");
                pbParse.Set(sectionName, "BackColor", "Black");
            };
            //There's not going to be a lot of overlap here, so we'll just have to slog through it.
            //I'm going to make the same check for a non-zero count later when I set up the discrete
            //sections, so I could move these checks there. But this is an easy way of getting the 
            //actual pages in the MFD and the discrete sections that configure them in the right order.
            pbParse.Set(_tag, "Surface0Pages", $"{((tallyGenericsInUse.Count > 0 || tallyInventoriesInUse.Count > 0) ? "TallyReport, " : "")}" +
                $"{(actionSetsInUse.Count > 0 ? "SetReport, " : "")}Log");
            pbParse.Set(_tag, "Surface0MFD", "APScreen");

            if (tallyGenericsInUse.Count > 0 || tallyInventoriesInUse.Count > 0)
            {
                sectionName = "SW.TallyReport";
                List<string> elements = tallyGenericsInUse.Keys.ToList();
                elements.AddList(tallyInventoriesInUse.Keys.ToList());
                elementNames = listToMultiLine(elements, 3, false);
                pbParse.Set(sectionName, "Elements", elementNames);
                setCommonSurfaceConfig("Tallies");
            }

            if (actionSetsInUse.Count > 0)
            {
                sectionName = "SW.SetReport";
                elementNames = listToMultiLine(actionSetsInUse.Keys.ToList(), 3, false);
                pbParse.Set(sectionName, "Elements", elementNames);
                setCommonSurfaceConfig("Action Sets");
            }

            sectionName = "SW.Log";
            pbParse.Set(sectionName, "DataType", "Log");
            pbParse.Set(sectionName, "FontSize", ".8");
            pbParse.Set(sectionName, "CharPerLine", "30");
            pbParse.Set(sectionName, "ForeColor", "LightBlue");
            pbParse.Set(sectionName, "BackColor", "Black");

            //All this stuff we've done so far won't mean much if we don't write to the PB.
            Me.CustomData = pbParse.ToString();

            //We know the templates we need, now it's time to write config for them to the grid.
            //_debugDisplay.WriteText($"Beginning Assignment pass\n", true);
            Dictionary<MyDefinitionId, APBlockConfig> storedConfig = new Dictionary<MyDefinitionId, APBlockConfig>();
            APBlockConfig blockConfig = null;
            int debugBlockConfigsCreated = 0;
            int totalBadParseCount = 0;
            int linkedBlockCount = 0;
            //Returns true if the block was successfully parsed. The parse will be in the blockParse variable.
            //Returns false if the parse fails. badParseCount will be incremented and a note will be 
            //added to badParseList.
            Func<IMyTerminalBlock, MyIni, bool> getParseOrHandleFailure = (b, ini) =>
            {
                if (!ini.TryParse(b.CustomData, out parseResult))
                {
                    totalBadParseCount++;
                    apLog.addWarning($"Block {b.CustomName} failed to parse due to the following " +
                        $"error on line {parseResult.LineNo}: {parseResult.Error}");
                    return false;
                }
                else
                { return true; }
            };
            //If we don't already have blockConfig for this block, parses its CustomData and generates
            //a new instance of APBlockConfig.
            //This subroutine only returns false if the parse fails, which will be logged in the 
            //totalBadParse variables. If we actually already had an instance of blockConfig, true
            //will be returned.
            Func<IMyTerminalBlock, MyIni, bool> tryParseIniAndCreateBlockConfig = (b, ini) =>
            {
                if (blockConfig == null)
                {
                    if (getParseOrHandleFailure(b, ini))
                    {
                        blockConfig = new APBlockConfig(b, ini, _tag);
                        debugBlockConfigsCreated++;
                        //_debugDisplay.WriteText($"  >Created APBlockConfig object for block {b.CustomName}\n", true);

                        return true;
                    }
                    else
                    { return false; }
                }
                return true;
            };
            foreach (IMyTerminalBlock block in apBlocks)
            {
                //Reset for this run of the loop
                blockConfig = null;
                //isBadParse = false;
                blockDef = block.BlockDefinition;
                if (storedConfig.ContainsKey(blockDef))
                {
                    //_debugDisplay.WriteText($"  Retrieving stored configuration for block {block.CustomName}\n", true);
                    blockConfig = storedConfig[blockDef];
                    //If there's an entry in storedConfig for this blockDef, we've encountered it
                    //before. But if we decided we didn't need to add to its config, the value will
                    //be null
                    if (blockConfig != null)
                    {
                        //We've got config for this blockDef. But can we make sense of the config
                        //already on the block?
                        if (getParseOrHandleFailure(block, blockParse))
                        {
                            blockConfig.writeConfigToIni(_tag, blockParse);
                            block.CustomData = blockParse.ToString();
                            linkedBlockCount++;
                        }
                        //No need for a goto here, this is the end of the branch.
                    }
                }
                else
                //We haven't encountered this block type before. We'll need to see if it matches any
                //of our templates.
                {
                    //_debugDisplay.WriteText($"  Finding applicapble templates for block {block.CustomName}\n", true);
                    //This is going to look pretty strange. But it's arranged the way it is so we 
                    //only parse the block's custom data if we know we have a template for it.
                    foreach (TallyGenericTemplate genTemplate in tallyGenericsInUse.Values)
                    {
                        if (genTemplate.blockMatchesConditional(block))
                        {
                            //As the name suggests, this subroutine will create a block config object 
                            //for us if we haven't yet this run, and parse the block's CustomData.
                            //It returns false only if the parse fails, at which point that will be
                            //logged and badParse will be set, canceling the rest of the loop.
                            if (tryParseIniAndCreateBlockConfig(block, blockParse))
                            //As per always, the tally generic version of this doesn't have much going on.
                            { blockConfig.addLink("Tallies", genTemplate.name); }
                            else
                            //If we can't read the CustomData, there's no point in continuing.
                            { goto CannotWriteToThisBlockSoSkipToNext; }
                        }
                    }
                    //_debugDisplay.WriteText($"  >Beginning inventory loop\n", true);
                    foreach (TallyInventoryTemplate invTemplate in tallyInventoriesInUse.Values)
                    {
                        //Inventory templates need to be checked against each inventory on the block.
                        //However, if we only have one inventory, we want to use the 'Tallies' key 
                        //for the config.
                        if (block.InventoryCount == 1)
                        {
                            if (invTemplate.inventoryMatchesConditional(block.GetInventory(0)))
                            {
                                if (tryParseIniAndCreateBlockConfig(block, blockParse))
                                { blockConfig.addLink("Tallies", invTemplate.name); }
                                else
                                { goto CannotWriteToThisBlockSoSkipToNext; }
                            }
                        }
                        else if (block.InventoryCount > 1)
                        {
                            for (int i = 0; i < block.InventoryCount; i++)
                            {
                                if (invTemplate.inventoryMatchesConditional(block.GetInventory(i)))
                                {
                                    if (tryParseIniAndCreateBlockConfig(block, blockParse))
                                    { blockConfig.addLink($"Inv{i}Tallies", invTemplate.name); }
                                    else
                                    { goto CannotWriteToThisBlockSoSkipToNext; }
                                }
                            }
                        }
                        //If the block has no inventories, there's no point in trying to match it
                        //to the inventory templates.
                    }
                    //_debugDisplay.WriteText($"  >Beginning Set loop\n", true); 
                    foreach (ActionSetTemplate setTemplate in actionSetsInUse.Values)
                    {
                        if (setTemplate.blockMatchesConditional(block))
                        {
                            if (tryParseIniAndCreateBlockConfig(block, blockParse))
                            {
                                blockConfig.addLink("ActionSets", setTemplate.name);
                                blockConfig.template = setTemplate;
                            }
                            else
                            { goto CannotWriteToThisBlockSoSkipToNext; }
                        }
                    }

                    //The last step is to add block config to our dictionary with the def as the key.
                    //So now we'll either have ready config the next time we encounter this block type,
                    //or a null that indicates that no templates apply to the type. 
                    storedConfig.Add(blockDef, blockConfig);
                    //We should also actully write the config for any of the applicable templates we found.
                    if (blockConfig != null)
                    {
                        //_debugDisplay.WriteText($"  >Found matching templates, writing config to block\n", true);
                        blockConfig.writeConfigToIni(_tag, blockParse);
                        block.CustomData = blockParse.ToString();
                        linkedBlockCount++;
                    }
                }
            CannotWriteToThisBlockSoSkipToNext:;
            }

            //DEBUG USE
            /*
            debugOutcome = "";
            debugOutcome = $"APBlockConfigs: {debugBlockConfigsCreated}\nTotal block types: {storedConfig.Keys.Count}\n" +
                $"{debugOutcome}";
            debugOutcome += $"{tallyGenericsInUse.Count + tallyInventoriesInUse.Count + actionSetsInUse.Count} " +
                $"templates identified once grid scanned ({apBlocks.Count} blocks).\n";
            foreach (string name in tallyGenericsInUse.Keys)
            { debugOutcome += $"  TallyGeneric {name}\n"; }
            foreach (string name in tallyInventoriesInUse.Keys)
            { debugOutcome += $"  TallyInventory {name}\n"; }
            foreach (string name in actionSetsInUse.Keys)
            { debugOutcome += $"  ActionSet {name}\n"; }
            apLog.addNote(debugOutcome);
            */

            outcome = $"\nCarried out {mode} command. There are now declarations for {tallyCount} AP Tallies " +
                $"and {setCount} AP ActionSets, with linking config written to {linkedBlockCount} / {apBlocks.Count} " +
                $"of considered blocks{(totalBadParseCount > 0 ? $" and {totalBadParseCount} blocks with unparsable config" : "")}.\n" +
                $"Autopopulate used {Runtime.CurrentInstructionCount} / {Runtime.MaxInstructionCount} " +
                $"({(int)(((double)Runtime.CurrentInstructionCount / Runtime.MaxInstructionCount) * 100)}%) " +
                $"of instructions allowed in this tic.\n";
            if (apLog.getErrorTotal() > 0)
            { outcome += $"\nThe following errors prevented AutoPopulate from running:\n{apLog.errorsToString()}"; }
            if (apLog.getWarningTotal() > 0)
            { outcome += $"\nThe following warnings should be addressed:\n{apLog.warningsToString()}"; }
            if (apLog.getNoteTotal() > 0)
            { outcome += $"\nThe following messages were logged:\n{apLog.notesToString()}"; }


            pbParse.Clear();
            blockParse.Clear();
            return true;
        }

        /* KEEP: This is the old version of AP's Assignment pass, relying on breaks and an isBadParse
         * boolean to figure out if templates still need to be checked. Preserved just in case 
         * anyone questions my decision to switch to goto.
        foreach (IMyTerminalBlock block in apBlocks)
        {
            //Reset for this run of the loop
            blockConfig = null;
            isBadParse = false;
            blockDef = block.BlockDefinition;
            if (storedConfig.ContainsKey(blockDef))
            {
                _debugDisplay.WriteText($"  Retrieving stored configuration for block {block.CustomName}\n", true);
                blockConfig = storedConfig[blockDef];
                //If there's an entry in storedConfig for this blockDef, we've encountered it
                //before. But if we decided we didn't need to add to its config, the value will
                //be null
                if (blockConfig != null)
                {
                    //We've got config for this blockDef. But can we make sense of the config
                    //already on the block?
                    if (parsedOrLogged(block, blockParse))
                    {
                        blockConfig.writeConfigToIni(_tag, blockParse);
                        block.CustomData = blockParse.ToString();
                    }
                }
            }
            else
            //We haven't encountered this block type before. We'll need to see if it matches any
            //of our templates.
            {
                _debugDisplay.WriteText($"  Finding applicapble templates for block {block.CustomName}\n", true);
                //This is going to look pretty strange. But it's arranged the way it is so we 
                //only parse the block's custom data if we know we have a template for it.
                foreach (TallyGenericTemplate genTemplate in tallyGenericsInUse.Values)
                {
                    if (genTemplate.blockMatchesConditional(block))
                    {
                        //As the name suggests, this subroutine will create a block config object 
                        //for us if we haven't yet this run, and parse the block's CustomData.
                        //It returns false only if the parse fails, at which point that will be
                        //logged and badParse will be set, canceling the rest of the loop.
                        if (prepareIniAndBlockConfig(block, blockParse))
                        //As per always, the tally generic version of this doesn't have much going on.
                        { blockConfig.addLink("Tallies", genTemplate.name); }
                        else
                        //If we can't read the CustomData, there's no point in continuing.
                        { break; }
                    }
                }
                _debugDisplay.WriteText($"  >Beginning inventory loop\n", true);
                if (!isBadParse)
                {
                    foreach (TallyInventoryTemplate invTemplate in tallyInventoriesInUse.Values)
                    {
                        //If we get a bad parse, that's going to occur in the inventory loop, so
                        //a break there would only carry us that out of that nested loop. Because
                        //we want to stop checking templates entirely, we need to have another
                        //break here
                        if (isBadParse)
                        { break; }
                        //For inventories, we have to check each inventory.
                        for (int i = 0; i < block.InventoryCount; i++)
                        {
                            if (invTemplate.inventoryMatchesConditional(block.GetInventory(i)))
                            {
                                if (prepareIniAndBlockConfig(block, blockParse))
                                { blockConfig.addLink($"Inv{i}Tallies", invTemplate.name); }
                                else
                                { break; }
                            }
                        }
                    }
                }
                _debugDisplay.WriteText($"  >Beginning Set loop\n", true);
                if (!isBadParse)
                {
                    foreach (ActionSetTemplate setTemplate in actionSetsInUse.Values)
                    {
                        if (setTemplate.blockMatchesConditional(block))
                        {
                            if (prepareIniAndBlockConfig(block, blockParse))
                            {
                                blockConfig.addLink("ActionSets", setTemplate.name);
                                blockConfig.template = setTemplate;
                            }
                            else
                            { break; }
                        }
                    }
                }

                //The last step is to add block config to our dictionary with the def as the key.
                //So now we'll either have ready config the next time we encounter this block type,
                //or a null, indicating that no templates apply to the type. 
                storedConfig.Add(blockDef, blockConfig);
                //We should also actully write the config for any of the applicable templates we found.
                if (blockConfig != null)
                {
                    _debugDisplay.WriteText($"  >Found matching templates, writing config to block\n", true);
                    //blockConfig.finish(); 
                    blockConfig.writeConfigToIni(_tag, blockParse);
                    block.CustomData = blockParse.ToString();
                }
            }
        }*/

        public HashSet<string> getAPExclusionsFromInit(string key)
        {
            HashSet<string> blacklist = new HashSet<string>();
            MyIniValue iniValue = _iniReadWrite.Get($"{_SCRIPT_PREFIX}.Init", key);
            string[] elements;
            //The code we're using to split the value will add an empty string 
            //to our element list if the value is empty or doesn't exist. We'll
            //check for that before we commit.
            if (!String.IsNullOrEmpty(iniValue.ToString()))
            {
                elements = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                foreach (string element in elements)
                { blacklist.Add(element); }
            }
            return blacklist;
        }

        public Dictionary<string, APTemplate> compileAPTemplates(List<string> excludedDeclarations,
            LimitedMessageLog apLog)
        {
            Dictionary<string, APTemplate> templates = new Dictionary<string, APTemplate>();

            //APTally types: Power, Hydrogen, Oxygen, Cargo, Ice, Stone, Ore, Uranium, Solar,
            //  JumpDrives, Gatling, Autocannon, Assault, Artillery, RailSmall, RailLarge, Rocket
            //Roost?
            //APSet types: Antenna, Batteries, Reactors, HyGens, TanksHydrogen, TanksOxygen, Gyros,
            //  ThrustersAtmospheric, ThrustersHydrogen, ThrustersIon
            //  RedLight? Beacon? OreDetector? Spotlights?

            const string ORE_TYPE = "MyObjectBuilder_Ore";
            const string INGOT_TYPE = "MyObjectBuilder_Ingot";
            //I may need this one day. May as well leave it here.
            //const string COMP_TYPE = "MyObjectBuilder_Component";
            const string AMMO_TYPE = "MyObjectBuilder_AmmoMagazine";
            MyItemType ICE = new MyItemType(ORE_TYPE, "Ice");
            MyItemType STONE = new MyItemType(ORE_TYPE, "Stone");
            MyItemType IRON_ORE = new MyItemType(ORE_TYPE, "Iron");
            MyItemType URANIUM = new MyItemType(INGOT_TYPE, "Uranium");
            MyItemType GATLING_AMMO = new MyItemType(AMMO_TYPE, "NATO_25x184mm");
            MyItemType AUTOCANNON_AMMO = new MyItemType(AMMO_TYPE, "AutocannonClip");
            MyItemType ASSAULT_AMMO = new MyItemType(AMMO_TYPE, "MediumCalibreAmmo");
            MyItemType ARTILLERY_AMMO = new MyItemType(AMMO_TYPE, "LargeCalibreAmmo");
            MyItemType RAIL_SMALL_AMMO = new MyItemType(AMMO_TYPE, "SmallRailgunAmmo");
            MyItemType RAIL_LARGE_AMMO = new MyItemType(AMMO_TYPE, "LargeRailgunAmmo");
            MyItemType ROCKET_AMMO = new MyItemType(AMMO_TYPE, "Missile200mm");
            //These will be needed to identify tank types.
            MyDefinitionId HYDROGEN_ID = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Hydrogen");
            MyDefinitionId OXYGEN_ID = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Oxygen");

            //We'll be storing a real live tally or ActionSet in each of the templates. 
            Tally tally;
            //Real live tallies do require color coders, so we'll have some of those as well.
            //Of course, for this, the colors don't matter. So we'll skip that bit.
            ColorCoderLow lowGood = new ColorCoderLow();
            ColorCoderHigh highGood = new ColorCoderHigh();
            //For item-based tallies, we need a way to tell if a given inventory can accomodate our item. 
            List<MyItemType> acceptedTypes = new List<MyItemType>();
            Func<IMyInventory, MyItemType, bool> inventoryCanHold = (inv, it) =>
            {
                acceptedTypes.Clear();
                inv.GetAcceptedItems(acceptedTypes);
                return (acceptedTypes.Contains(it));
            };

            //Power
            tally = new TallyGeneric(_meterMaid, "Power", new BatteryHandler(), highGood);
            templates.Add(tally.programName, new TallyGenericTemplate(tally.programName, tally, b => b is IMyBatteryBlock));
            //Hydrogen
            tally = new TallyGeneric(_meterMaid, "Hydrogen", new GasHandler(), highGood);
            templates.Add(tally.programName, new TallyGenericTemplate(tally.programName, tally, b => b is IMyGasTank &&
                (b.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(HYDROGEN_ID) ?? false)));
            //Oxygen
            tally = new TallyGeneric(_meterMaid, "Oxygen", new GasHandler(), highGood);
            templates.Add(tally.programName, new TallyGenericTemplate(tally.programName, tally, b => b is IMyGasTank &&
                (b.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(OXYGEN_ID) ?? false)));
            //Cargo
            //To determine if an inventory can accommodate the cargo tally, we check to see if it can 
            //hold both ice and uranium (Because we've already declared those, and because H2O2 gens
            //are the only thing outside of generic cargo boxes that can hold ice)
            tally = new TallyCargo(_meterMaid, "Cargo", lowGood);
            templates.Add(tally.programName, new TallyInventoryTemplate(tally.programName, tally,
                b => b is IMyCargoContainer, i => inventoryCanHold(i, ICE) && inventoryCanHold(i, URANIUM)));
            //Ice
            tally = new TallyItem(_meterMaid, "Ice", ICE, highGood);
            tally.forceMax(4000);
            templates.Add(tally.programName, new TallyInventoryTemplate(tally.programName, tally,
                b => b is IMyGasGenerator, i => inventoryCanHold(i, ICE)));
            //Stone
            tally = new TallyItem(_meterMaid, "Stone", STONE, lowGood);
            tally.forceMax(5000);
            templates.Add(tally.programName, new TallyInventoryTemplate(tally.programName, tally,
                b => b is IMyShipDrill || b is IMyRefinery, i => inventoryCanHold(i, STONE)));
            //Ore
            //For the Ore tally, we look for any inventory that can hold Iron ore.
            tally = new TallyCargo(_meterMaid, "Ore", lowGood);
            templates.Add(tally.programName, new TallyInventoryTemplate(tally.programName, tally,
                b => b is IMyShipDrill || b is IMyRefinery, i => inventoryCanHold(i, IRON_ORE)));
            //Uranium
            tally = new TallyItem(_meterMaid, "Uranium", URANIUM, highGood);
            tally.forceMax(50);
            templates.Add(tally.programName, new TallyInventoryTemplate(tally.programName, tally,
                b => b is IMyReactor, i => inventoryCanHold(i, URANIUM)));
            //Solar
            tally = new TallyGeneric(_meterMaid, "Solar", new PowerMaxHandler(), highGood);
            tally.multiplier = 100;
            templates.Add(tally.programName, new TallyGenericTemplate(tally.programName, tally, b => b is IMySolarPanel));
            //JumpDrive
            tally = new TallyGeneric(_meterMaid, "JumpDrive", new JumpDriveHandler(), highGood);
            tally.displayName = "Jump Charge";
            templates.Add(tally.programName, new TallyGenericTemplate(tally.programName, tally, b => b is IMyJumpDrive));

            //After the warfare update, weapons no longer get a bespoke interface. So to tell them
            //apart, we look to see what ammo types they can hold. And since AP only deals with base
            //game ammo types, and all base game turrets only have one inventory, for the identification
            //pass, we'll only check that.
            //Note: All the indentification checks for ammo type first check to see if the block is
            //an IMyUserControllableGun. It'd be nice if I could do that check once for all of them,
            //but I think there are enough optimizations elsewhere in AP that it won't be a big deal.
            Func<IMyTerminalBlock, MyItemType, bool> weaponIdentification = (b, i) =>
            { return b is IMyUserControllableGun && inventoryCanHold(b.GetInventory(0), i); };

            //GatlingAmmo
            tally = new TallyItem(_meterMaid, "GatlingAmmo", GATLING_AMMO, highGood);
            tally.forceMax(20);
            tally.displayName = "Gatling\nDrums";
            templates.Add(tally.programName, new TallyInventoryTemplate(tally.programName, tally,
                b => weaponIdentification(b, GATLING_AMMO), i => inventoryCanHold(i, GATLING_AMMO)));
            //AutocannonAmmo
            tally = new TallyItem(_meterMaid, "AutocannonAmmo", AUTOCANNON_AMMO, highGood);
            tally.forceMax(60);
            tally.displayName = "Autocannon\nClips";
            templates.Add(tally.programName, new TallyInventoryTemplate(tally.programName, tally,
                b => weaponIdentification(b, AUTOCANNON_AMMO), i => inventoryCanHold(i, AUTOCANNON_AMMO)));
            //AssaultAmmo
            tally = new TallyItem(_meterMaid, "AssaultAmmo", ASSAULT_AMMO, highGood);
            tally.forceMax(120);
            tally.displayName = "Cannon\nShells";
            templates.Add(tally.programName, new TallyInventoryTemplate(tally.programName, tally,
                b => weaponIdentification(b, ASSAULT_AMMO), i => inventoryCanHold(i, ASSAULT_AMMO)));
            //ArtilleryAmmo
            tally = new TallyItem(_meterMaid, "ArtilleryAmmo", ARTILLERY_AMMO, highGood);
            tally.forceMax(40);
            tally.displayName = "Artillery\nShells";
            templates.Add(tally.programName, new TallyInventoryTemplate(tally.programName, tally,
                b => weaponIdentification(b, ARTILLERY_AMMO), i => inventoryCanHold(i, ARTILLERY_AMMO)));
            //RailSmallAmmo
            tally = new TallyItem(_meterMaid, "RailSmallAmmo", RAIL_SMALL_AMMO, highGood);
            tally.forceMax(36);
            tally.displayName = "Railgun\nS. Sabots";
            templates.Add(tally.programName, new TallyInventoryTemplate(tally.programName, tally,
                b => weaponIdentification(b, RAIL_SMALL_AMMO), i => inventoryCanHold(i, RAIL_SMALL_AMMO)));
            //RailLargeAmmo
            tally = new TallyItem(_meterMaid, "RailLargeAmmo", RAIL_LARGE_AMMO, highGood);
            tally.forceMax(12);
            tally.displayName = "Railgun\nL. Sabots";
            templates.Add(tally.programName, new TallyInventoryTemplate(tally.programName, tally,
                b => weaponIdentification(b, RAIL_LARGE_AMMO), i => inventoryCanHold(i, RAIL_LARGE_AMMO)));
            //RocketAmmo
            tally = new TallyItem(_meterMaid, "RocketAmmo", ROCKET_AMMO, highGood);
            tally.forceMax(24);
            tally.displayName = "Rockets";
            templates.Add(tally.programName, new TallyInventoryTemplate(tally.programName, tally,
                b => weaponIdentification(b, ROCKET_AMMO), i => inventoryCanHold(i, ROCKET_AMMO)));

            //On to the ActionSets.
            ActionSet actionSet;
            //In addition to the config that goes on the PB, each block in an ActionSet needs 
            //instructions on what exactly the set is supposed to do with the block. This writer will
            //write config we'll need for the vast majority of those blocks.
            Action<MyIni, string, string, string> writeDiscreteSection = (ini, sec, on, off) =>
            {
                ini.Set(sec, "ActionOn", on);
                ini.Set(sec, "ActionOff", off);
            };
            //When we just want to turn something off, we'll use this
            /*
            Action<MyIni, string, string, string> writeOffSection = (ini, sec, on, off) =>
            { ini.Set(sec, "ActionOff", off); };
            */
            //And if we run into an antenna, we'll need this.
            Action<MyIni, string, string, string> writeAntennaSection = (ini, sec, on, off) =>
            {
                ini.Set(sec, "Action0Property", "Radius");
                ini.Set(sec, "Action0ValueOn", "1500");
                ini.Set(sec, "Action0ValueOff", "150");

                ini.Set(sec, "Action1Property", "HudText");
                ini.Set(sec, "Action1ValueOn", on);
                ini.Set(sec, "Action1ValueOff", off);
            };
            //Antennas
            //ActionSets require a name and an initial state. But in this case, the initial state 
            //won't do anything. So we'll just pass it false.
            actionSet = new ActionSet("Antennas", false);
            actionSet.displayName = "Antenna Range";
            actionSet.textOn = "Broad";
            actionSet.textOff = "Wifi";
            actionSet.colorOff = yellow;
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyRadioAntenna,
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", $"{_customID}", $"{_customID} Wifi", writeAntennaSection, "Off", "On"));
            //Beacons
            actionSet = new ActionSet("Beacons", false);
            actionSet.displayName = "Beacon";
            actionSet.textOn = "Online";
            actionSet.textOff = "Offline";
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyBeacon,
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "Off", "On"));
            //Spotlights
            actionSet = new ActionSet("Spotlights", false);
            actionSet.textOn = "Online";
            actionSet.textOff = "Offline";
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyReflectorLight,
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "Off", ""));
            //OreDetectors
            actionSet = new ActionSet("OreDetectors", false);
            actionSet.displayName = "Ore Detector";
            actionSet.textOn = "Scanning";
            actionSet.textOff = "Idle";
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyOreDetector,
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "Off", "On"));
            //Batteries
            actionSet = new ActionSet("Batteries", false);
            actionSet.textOn = "On Auto";
            actionSet.textOff = "Recharging";
            actionSet.colorOff = yellow;
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyBatteryBlock,
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "BatteryAuto", "BatteryRecharge", writeDiscreteSection, "Off", "On"));
            //Reactors
            actionSet = new ActionSet("Reactors", false);
            actionSet.textOn = "Active";
            actionSet.textOff = "Inactive";
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyReactor,
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "Off", ""));
            //EnginesHydrogen
            actionSet = new ActionSet("EnginesHydrogen", false);
            actionSet.displayName = "Engines";
            actionSet.textOn = "Running";
            actionSet.textOff = "Idle";
            //Hydrogen engines don't have a bespoke interface, and they're just difficult all around. 
            //But we want to be able to find them, so we look for power producers that consume hydrogen.
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyPowerProducer &&
                (b.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(HYDROGEN_ID) ?? false),
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "Off", ""));
            //TanksHydrogen
            actionSet = new ActionSet("TanksHydrogen", false);
            actionSet.displayName = "Hydrogen\nTanks";
            actionSet.textOn = "Open";
            actionSet.textOff = "Filling";
            actionSet.colorOff = lightBlue;
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyGasTank &&
                (b.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(HYDROGEN_ID) ?? false),
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "TankStockpileOff", "TankStockpileOn", writeDiscreteSection, "Off", "On"));
            //TanksOxygen
            actionSet = new ActionSet("TanksOxygen", false);
            actionSet.displayName = "Oxygen\nTanks";
            actionSet.textOn = "Open";
            actionSet.textOff = "Filling";
            actionSet.colorOff = lightBlue;
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyGasTank &&
                (b.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(OXYGEN_ID) ?? false),
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "TankStockpileOff", "TankStockpileOn", writeDiscreteSection, "Off", "On"));
            //Gyros
            actionSet = new ActionSet("Gyroscopes", false);
            actionSet.displayName = "Gyros";
            actionSet.textOn = "Active";
            actionSet.textOff = "Inactive";
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyGyro,
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "Off", "On"));
            //ThrustersElectric
            actionSet = new ActionSet("ThrustersElectric", false);
            actionSet.displayName = "Electric\nThrusters";
            actionSet.textOn = "Online";
            actionSet.textOff = "Offline";
            //Thrusters are also difficult, because we don't really have a way to tell them apart 
            //(Outside of the IDs), and there's apparently no way to tell ion and atmo thrusters
            //apart without some form of string parsing. So instead, we'll put those two in the same
            //group, and define that group as, 'thrusters that don't use hydrogen'
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyThrust &&
                !(b.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(HYDROGEN_ID) ?? false),
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "Off", "On"));
            //ThrustersHydrogen
            actionSet = new ActionSet("ThrustersHydrogen", false);
            actionSet.displayName = "Hydrogen\nThrusters";
            actionSet.textOn = "Online";
            actionSet.textOff = "Offline";
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyThrust &&
                (b.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(HYDROGEN_ID) ?? false),
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "Off", "On"));

            //From a performance standpoint, the best way to filter out excluded templates would be 
            //to put an if check on the creation of each template that keeps the code from executing
            //at all. But from a code maintainence (And character count) standpoint, it's better to
            //have it all in this loop.
            //Besides, this lets us easily do some error checking and reporting.
            if (excludedDeclarations != null)
            {
                int i = 0;
                string exclusion;
                while (i < excludedDeclarations.Count)
                {
                    exclusion = excludedDeclarations[i];
                    if (templates.ContainsKey(exclusion))
                    {
                        templates.Remove(exclusion);
                        excludedDeclarations.RemoveAt(i);
                    }
                    else
                    //If we can't match the exclusion to a key in the dictionary, we'll hold on to
                    //it so we can complain about it later.
                    { i++; }
                }
                if (i > 0)
                {
                    string orphanedExclusions = "";
                    foreach (string orphan in excludedDeclarations)
                    { orphanedExclusions += $"{orphan}, "; }
                    orphanedExclusions = orphanedExclusions.Remove(orphanedExclusions.Length - 2);

                    apLog.addWarning("The following entries from APExcludedDeclarations could not " +
                        $"be matched to declarations: {orphanedExclusions}. Remember that they are " +
                        $"case sensitive.");
                }
            }
            return templates;
        }

        public class APTemplate
        {
            //The name of the script object associated with this template.
            public string name { get; private set; }
            //The complete script object that this template represents. Used to write config.
            protected IHasConfig declaration;
            //A conditional statement used to see if a given block matches the criteria of this template.
            Func<IMyTerminalBlock, bool> conditional;

            public APTemplate(string name, IHasConfig declaration, Func<IMyTerminalBlock, bool> conditional)
            {
                this.name = name;
                this.declaration = declaration;
                this.conditional = conditional;
            }

            public string writePBConfig(int index)
            { return declaration.writeConfig(index); }

            public bool blockMatchesConditional(IMyTerminalBlock block)
            { return conditional(block); }
        }

        //An extension of APTemplates specifically for Tallies.
        public class TallyGenericTemplate : APTemplate
        {
            //Tally generic templates don't have a lot to them.
            public TallyGenericTemplate(string name, IHasConfig declaration, Func<IMyTerminalBlock, bool> conditional)
                : base(name, declaration, conditional)
            { }
        }

        //An extension of APTemplates specifically for Tallies dealing with inventories.
        public class TallyInventoryTemplate : APTemplate
        {
            //We use different criteria to judge if a block can accomodate an item (Or one of the 
            //cargo tallies). This conditional will handle that.
            Func<IMyInventory, bool> inventoryConditional;

            public TallyInventoryTemplate(string name, IHasConfig declaration, Func<IMyTerminalBlock, bool> conditional,
                Func<IMyInventory, bool> inventoryConditional) : base(name, declaration, conditional)
            { this.inventoryConditional = inventoryConditional; }

            public bool inventoryMatchesConditional(IMyInventory inventory)
            { return inventoryConditional(inventory); }
        }

        //An extension of APTemplates specifically for ActionSets.
        public class ActionSetTemplate : APTemplate
        {
            //The name of the discrete section and the on/off values it'll contain
            string discreteSectionName, onValue, offValue;
            //The action that will write the discrete section. Because we'll sometimes be using
            //ActionPlanTerminal, this had to be variable.
            Action<MyIni, string, string, string> discreteWriter;
            //What will this set be doing when Roost changes state? Value should either be 'on', 
            //'off', or an empty string.
            public string stateWhenRoostOn { get; private set; }
            public string stateWhenRoostOff { get; private set; }

            public ActionSetTemplate(string name, IHasConfig declaration, Func<IMyTerminalBlock, bool> conditional,
                string section, string onValue, string offValue, Action<MyIni, string, string, string> discreteWriter,
                string stateWhenRoostOn, string stateWhenRoostOff)
                : base(name, declaration, conditional)
            {
                discreteSectionName = section;
                this.onValue = onValue;
                this.offValue = offValue;
                this.discreteWriter = discreteWriter;
                this.stateWhenRoostOn = stateWhenRoostOn;
                this.stateWhenRoostOff = stateWhenRoostOff;
            }

            internal string getStateWhenRoost(bool isOn)
            { return isOn ? stateWhenRoostOn : stateWhenRoostOff; }

            //FAT: This method could likely be refactored to share common code.
            internal void loadPlansIntoRoostSet(ref ActionSet roostSet)
            {
                ActionPlanActionSet plan = null;
                if (!string.IsNullOrEmpty(stateWhenRoostOn))
                {
                    plan = new ActionPlanActionSet((ActionSet)declaration);
                    if (stateWhenRoostOn == "On")
                    { plan.setReactionToOn(true); }
                    else
                    { plan.setReactionToOn(false); }
                    roostSet.addActionPlan(plan);
                }
                if (!string.IsNullOrEmpty(stateWhenRoostOff))
                {
                    plan = new ActionPlanActionSet((ActionSet)declaration);
                    if (stateWhenRoostOff == "On")
                    { plan.setReactionToOff(true); }
                    else
                    { plan.setReactionToOff(false); }
                    roostSet.addActionPlan(plan);
                }
            }

            //Arguably, this should be moved to APBlockConfig, given that's what writes config for blocks.
            public void writeDiscreteEntry(MyIni blockParse)
            { discreteWriter.Invoke(blockParse, discreteSectionName, onValue, offValue); }

            //Returns a state list entry for what this set should be doing when Roost is on. If no
            //action is specified, returns an empty string.
            public string writeRoostOnPart()
            { return String.IsNullOrEmpty(stateWhenRoostOn) ? "" : $"{name}: {stateWhenRoostOn}"; }

            //Returns a state list entry for what this set should be doing when Roost is off. If no
            //action is specified, returns an empty string.
            public string writeRoostOffPart()
            { return String.IsNullOrEmpty(stateWhenRoostOff) ? "" : $"{name}: {stateWhenRoostOff}"; }
        }

        public class APBlockConfig
        {
            //Config data for this block type.
            //The key of the dictionary is the key that will be used in the MyIni entry (Tallies, 
            //Inv0Tallies, etc)
            internal Dictionary<string, APConfigEntry> config { get; private set; }
            //A template that tells us how to write a discrete section.
            internal ActionSetTemplate template;

            public APBlockConfig(IMyTerminalBlock block, MyIni blockParse, string section)
            {
                int numInventories = block.InventoryCount;
                config = new Dictionary<string, APConfigEntry>();
                //There may already be config on this block, but we're only interested in common 
                //section entries for Tallies/Inv#Tallies and ActionSets.
                if (numInventories > 1)
                {
                    for (int i = 0; i < numInventories; i++)
                    { tryCreateConfigEntryFromIni(blockParse, section, $"Inv{i}Tallies"); }
                }
                else
                { tryCreateConfigEntryFromIni(blockParse, section, "Tallies"); }
                tryCreateConfigEntryFromIni(blockParse, section, "ActionSets");
                template = null;
            }

            private void createNewConfigEntry(string key, string initialValue, bool isModified = false)
            { config.Add(key, new APConfigEntry(initialValue, isModified)); }

            //Creates an entry in the config dictionary from an entry in a MyIni parse, if the parse
            //actually contains the entry in question.
            //Returns true if the requested entry was found, false if it wasn't.
            private bool tryCreateConfigEntryFromIni(MyIni ini, string section, string key)
            {
                if (ini.ContainsKey(section, key))
                {
                    createNewConfigEntry(key, ini.Get(section, key).ToString());
                    return true;
                }
                return false;
            }

            public void addLink(string key, string link)
            {
                if (config.ContainsKey(key))
                { config[key].addLink(link); }
                else
                //A new entry will need to be printed, so we'll set it as modified.
                { createNewConfigEntry(key, link, true); }
            }

            public void writeConfigToIni(string section, MyIni ini)
            {
                foreach (KeyValuePair<string, APConfigEntry> pair in config)
                { pair.Value.writeEntryToIni(ini, section, pair.Key); }
                //If we've been holding on to a template, tell it to write its discrete section. 
                if (template != null)
                { template.writeDiscreteEntry(ini); }
            }
        }

        public class APConfigEntry
        {
            public string links { get; private set; }
            bool isModified;

            public APConfigEntry(string initialValue, bool startsModified = false)
            {
                links = initialValue;
                isModified = startsModified;
            }

            //Adds a new link to the list of links, if it doesn't already exist.
            public void addLink(string newLink)
            {
                if (!links.Contains(newLink))
                {
                    links = $"{links}, {newLink}";
                    isModified = true;
                }
            }

            //Overwrites an ini entry at the designated section and key, but only if this entry has
            //been modified.
            public void writeEntryToIni(MyIni ini, string section, string key)
            {
                if (isModified)
                { ini.Set(section, key, links); }
            }
        }

        public void evaluateFull(LimitedMessageLog textLog, bool firstRun = false)
        {
            //We'll need a bunch of dictionaries and other lists to move data between the various 
            //sub-evaluates
            //We want our dictionaries to be case agnostic, and this is the comparer to make it happen.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            Dictionary<string, IColorCoder> colorPalette = new Dictionary<string, IColorCoder>(comparer);
            compileColors(colorPalette);
            Dictionary<string, Tally> evalTallies = new Dictionary<string, Tally>(comparer);
            Dictionary<string, ActionSet> evalSets = new Dictionary<string, ActionSet>(comparer);
            Dictionary<string, Trigger> evalTriggers = new Dictionary<string, Trigger>(comparer);
            Dictionary<string, Raycaster> evalRaycasters = new Dictionary<string, Raycaster>(comparer);
            Dictionary<IMyInventory, List<TallyCargo>> evalContainers = new Dictionary<IMyInventory, List<TallyCargo>>();
            List<IReportable> evalReports = new List<IReportable>();
            List<WallOText> evalLogReports = new List<WallOText>();
            Dictionary<string, MFD> evalMFDs = new Dictionary<string, MFD>(comparer);
            Dictionary<string, Indicator> evalIndicators = new Dictionary<string, Indicator>(comparer);
            HashSet<string> usedElementNames = new HashSet<string>(comparer);
            string evalNonDeclarationPBConfig = "";
            MyIniParseResult parseResult;
            //We'll be using this everywhere. May as well declare it once and have done.
            MyIniValue iniValue = new MyIniValue();
            int blockCount = -1;

            /*Dictionary<string, IColorCoder> colorPalette = compileColors();
            Dictionary<string, Tally> evalTallies = new Dictionary<string, Tally>();
            Dictionary<string, ActionSet> evalSets = new Dictionary<string, ActionSet>();
            Dictionary<string, Trigger> evalTriggers = new Dictionary<string, Trigger>();
            Dictionary<string, Raycaster> evalRaycasters = new Dictionary<string, Raycaster>();
            Dictionary<IMyInventory, List<TallyCargo>> evalContainers = new Dictionary<IMyInventory, List<TallyCargo>>();
            List<IReportable> evalReports = new List<IReportable>();
            List<WallOText> evalLogReports = new List<WallOText>();
            Dictionary<string, MFD> evalMFDs = new Dictionary<string, MFD>();
            Dictionary<string, Indicator> evalIndicators = new Dictionary<string, Indicator>();
            HashSet<string> usedElementNames = new HashSet<string>();
            string evalNonDeclarationPBConfig = "";
            MyIniParseResult parseResult;
            //We'll be using this everywhere. May as well declare it once and have done.
            MyIniValue iniValue = new MyIniValue();
            int blockCount = -1;*/

            //_debugDisplay.WriteText("Beginning evaluation\n");
            //We'll go ahead and get a parse from the Storage string. 
            //The storage string can't be directly altered by the user, so we simply assume that it
            //parsed correctly
            _iniRead.TryParse(Storage);

            //Now that we have that, we'll go ahead and set the update delay to whatever we had stored
            _distributor.setDelay(_iniRead.Get("Data", "UpdateDelay").ToInt32(0));

            //Parse the PB's custom data. If it checks out, we can proceed.
            if (!_iniReadWrite.TryParse(Me.CustomData, out parseResult))
            //If we didn't get a useable parse, file a complaint.
            {
                textLog.addError($"The parser encountered an error on line {parseResult.LineNo} of the " +
                    $"Programmable Block's config: {parseResult.Error}");
            }
            else
            {
                //_debugDisplay.WriteText("Entering evaluateInit\n", true);
                evaluateInit(colorPalette, textLog, iniValue);
                //_debugDisplay.WriteText("Entering evaluateDeclarations\n", true);
                evaluateDeclarations(Me, textLog, colorPalette, evalTallies, evalSets, evalTriggers,
                    evalRaycasters, usedElementNames, parseResult, iniValue);

                //_debugDisplay.WriteText("Deciding to evaluateGrid\n", true);
                //If we've logged any errors with the PB config, there's no point in looking at the grid.
                if (textLog.getErrorTotal() > 0)
                {
                    textLog.addWarning("Errors in Programmable Block configuration have prevented grid " +
                        "configuration from being evaluated.");
                }
                else
                {
                    //As long as we've got a good parse of the PB, we'll store the non-declaration 
                    //config on it for use by Reconstitute.
                    evalNonDeclarationPBConfig = stripDeclarations();
                    //_debugDisplay.WriteText("Decision made to evaluateGrid\n", true);
                    blockCount = evaluateGrid(textLog, colorPalette, evalTallies, evalSets, evalTriggers,
                        evalRaycasters, evalContainers, evalMFDs, evalReports, evalLogReports,
                        evalIndicators, parseResult, iniValue);
                }
            }

            //_debugDisplay.WriteText("Config evaluation complete\n", true);
            //It's time to make some decisions about the config we've read, and to tell the user 
            //about it. The first decision has to do with the logReports
            if (_logReports == null || textLog.getErrorTotal() == 0 || evalLogReports.Count >= _logReports.Count)
            { _logReports = evalLogReports; }
            //The main decision is, 'How much do we trust all this config we just read'? 
            string outcome = "Evaluation complete.\n";
            if (textLog.getErrorTotal() > 0)
            {
                outcome += "Errors prevent the use of this configuration. ";
                if (_haveGoodConfig)
                {
                    //There is conceivably a scenario where we changed the updateFrequency coming
                    //into this. Re-affirm the normal updateFrequency.
                    Runtime.UpdateFrequency = UpdateFrequency.Update100;
                    outcome += $"Execution continuing with last good configuration from " +
                        $"{(DateTime.Now - _lastGoodConfigStamp).Minutes} minutes ago " +
                        $"({_lastGoodConfigStamp.ToString("HH: mm: ss")}).\n";
                }
                else
                {
                    //Make sure the script isn't trying to run with errors.
                    Runtime.UpdateFrequency = UpdateFrequency.None;
                    outcome += "Because there is no good configuration loaded, the script has been halted.\n";
                }
                //File a complaint.
                outcome += $"\nThe following errors are preventing the use of this config:\n{textLog.errorsToString()}";
                //(20230426)Color-codes the word 'error' in this message. Needs the color reader in 
                //evaluateInit hooked back up if custom colors are desired.
                //outcome += $"\nThe following [color=#{textLog.errorCode}]errors[/color] are preventing the use of this config:\n{textLog.errorsToString()}";
            }
            else
            {
                //_debugDisplay.WriteText("Entered error-free wrap-up\n", true);
                //_debugDisplay.WriteText($"Warning listing:\n{textLog.warningsToString()}\n", true);
                //We've got good config. We'll brag about that in a moment, but first, we need to 
                //un-pack a small mountain of eval dictionaries.
                Container container;
                int counter = 0;
                _containers = new Container[evalContainers.Count];
                foreach (IMyInventory inventory in evalContainers.Keys)
                {
                    //Build a new Container object based on the data we've collected in evaluation
                    container = new Container(inventory, evalContainers[inventory].ToArray());
                    //Send the maximum volume of this inventory to its linked tallies.
                    container.sendMaxToTallies();
                    //Place the container in the array.
                    _containers[counter] = container;
                    counter++;
                }
                //_debugDisplay.WriteText("Finished containers\n", true);
                //Next, tear down the complicated data structures we've been using for evaluation into
                //the arrays we'll be using during execution
                _tallies = evalTallies.Values.ToArray();
                _triggers = evalTriggers.Values.ToArray();
                _reports = evalReports.ToArray();
                _indicators = evalIndicators.Values.ToArray();
                //_debugDisplay.WriteText("Finished array conversion\n", true);
                //In some cases, we'll port the contents of the eval dictionaries directly to the globals
                _sets = evalSets;
                _raycasters = evalRaycasters;
                _MFDs = evalMFDs;
                //_debugDisplay.WriteText("Finished dictionary hand-over\n", true);
                //There's one more step before the tallies are ready. We need to tell them that they
                //have all the data that they're going to get. 
                foreach (Tally finishTally in _tallies)
                { finishTally.finishSetup(); }
                //_debugDisplay.WriteText("Finished finishSetup tally calls\n", true);
                //We'll take this opportunity to call setProfile on all our Reportables
                foreach (IReportable reportable in _reports)
                { reportable.setProfile(); }
                //{ reportable.setProfile(); }
                //_debugDisplay.WriteText("Finished setProfile calls\n", true);

                //Record this occasion for posterity
                _haveGoodConfig = true;
                _lastGoodConfigStamp = DateTime.Now;
                _nonDeclarationPBConfig = evalNonDeclarationPBConfig;

                //Also, brag
                outcome += $"Script is now running. Registered {_tallies.Length} tallies, " +
                    $"{_sets.Count} ActionSets, {_triggers.Length} triggers, and {_reports.Length} " +
                    $"reports, as configured by data on {blockCount} blocks. Evaluation used " +
                    $"{Runtime.CurrentInstructionCount} / {Runtime.MaxInstructionCount} " +
                    $"({(int)(((double)Runtime.CurrentInstructionCount / Runtime.MaxInstructionCount) * 100)}%) " +
                    $"of instructions allowed in this tic.\n";
                //And now, we set the script into motion.
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
            }
            if (textLog.getNoteTotal() > 0)
            { outcome += $"\nThe following messages were logged:\n{textLog.notesToString()}"; }
            if (textLog.getWarningTotal() > 0)
            { outcome += $"\nThe following warnings were logged:\n{textLog.warningsToString()}"; }
            //{ outcome += $"\nThe following [color=#{textLog.warningCode}]warnings[/color] were logged:\n{textLog.warningsToString()}"; }

            //_debugDisplay.WriteText("Evaluation wrap-up complete:\n", true);

            _log.add(outcome);
            //Force-update our logReports, just in case the script isn't executing.
            foreach (WallOText logReport in _logReports)
            { logReport.update(); }

            //This should be called wherever we came from
            //Echo(_log.toString());

            //There's probably still data in the iniReader. We don't need it anymore, and we don't
            //want it carrying over to any future evaluations.
            _iniReadWrite.Clear();
            _iniRead.Clear();

            //_debugDisplay.WriteText("Exit evaluation\n", true);

            //DEBUG USE
            /*
            List<string> colorNames = colorPalette.Keys.ToList();
            string palettePrint = $"Palette contains {colorNames.Count} colorCoders:\n";
            foreach (string name in colorNames) 
            { palettePrint += $"{name}\n";}
            textLog.addNote(palettePrint);
            */
            /*
            _debugDisplay.WriteText("colorPalette getConfigPart:\n", true);
            foreach (IColorCoder coder in colorPalette.Values)
            { _debugDisplay.WriteText($"  {coder.getConfigPart()}\n", true); }
            _debugDisplay.WriteText("Tally ColorCoder getConfigPart:\n", true);
            foreach (Tally tally in evalTallies.Values)
            { _debugDisplay.WriteText($"  {tally.colorCoder.getConfigPart()}\n", true); }
            */
            //DEBUG USE
            /*
            List<Tally> debugTallyList = evalTallies.Values.ToList();
            for (int i = 0; i < debugTallyList.Count; i++)
            { textLog.addNote(debugTallyList[i].writeConfig(i)); }
            List<ActionSet> debugSetList = evalSets.Values.ToList();
            for (int i = 0; i < debugSetList.Count; i++)
            { textLog.addNote(debugSetList[i].writeConfig(i)); }
            List<Trigger> debugTriggerList = evalTriggers.Values.ToList();
            for (int i = 0; i < debugTriggerList.Count; i++)
            { textLog.addNote(debugTriggerList[i].writeConfig(i)); }
            List<Raycaster> debugRaycasterList = evalRaycasters.Values.ToList();
            for (int i = 0; i < debugRaycasterList.Count; i++)
            { textLog.addNote(debugRaycasterList[i].writeConfig(i)); }

            _log.add($"Test evaluate complete. The following errors were logged:\n" +
                $"{textLog.errorsToString()}\n" +
                $"The following warnings were logged:\n" +
                $"{textLog.warningsToString()}\n" +
                $"The following messages were logged:\n" +
                $"{textLog.notesToString()}\n");
                */
        }

        public void evaluateInit(Dictionary<string, IColorCoder> colorPalette,
            LimitedMessageLog textLog, MyIniValue iniValue)
        {
            Color color = Color.Black;
            IColorCoder colorCoder = null;
            Action<string> troubleLogger = b => textLog.addError(b);
            string initTag = "SW.Init";

            //Because we use the actual color coders frequently, we'll put individual handles on them
            ColorCoderLow lowGood = (ColorCoderLow)(colorPalette["lowgood"]);
            ColorCoderHigh highGood = (ColorCoderHigh)(colorPalette["highgood"]);

            bool hasInitSection = _iniReadWrite.ContainsSection(initTag);
            //We only need to log individual key generation if there's already an init section. 
            bool logKeyGeneration = hasInitSection;
            bool configAltered = false;
            string pbName = Me.CustomName;

            //Because if the Init section is missing entirely, we'll make one note for the entire lot.
            if (!hasInitSection)
            {
                textLog.addNote($"{initTag} section was missing from block '{pbName}' and " +
                  $"has been re-generated.");
            }

            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue,
                lowGood, highGood, initTag, "Optimal", "Green", logKeyGeneration, ref configAltered);
            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue,
                lowGood, highGood, initTag, "Normal", "LightBlue", logKeyGeneration, ref configAltered);
            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue,
                lowGood, highGood, initTag, "Caution", "Yellow", logKeyGeneration, ref configAltered);
            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue,
                lowGood, highGood, initTag, "Warning", "Orange", logKeyGeneration, ref configAltered);
            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue,
                lowGood, highGood, initTag, "Critical", "Red", logKeyGeneration, ref configAltered);
            //(20230426) This is for color-coding text in the PB's DetailInfo.
            //It also might not be working. When I tested it, the color seemed a lot like a blue even
            //though we should've been retrieving a red.
            //textLog.errorColor = color;

            //We also store some config for the AutoPopulate process in the init section. AP will 
            //access that information when it needs it, but for now, we just need to make sure those
            //keys are in place.
            //   |-----------------------------------------------------------|
            tryCheckForDefaultIniKey(initTag, "APExcludedBlockTypes",
                "MyObjectBuilder_ConveyorSorter, MyObjectBuilder_ShipWelder,\n" +
                "MyObjectBuilder_ShipGrinder",
                ref configAltered, logKeyGeneration, pbName, textLog);
            tryCheckForDefaultIniKey(initTag, "APExcludedBlockSubTypes",
                "StoreBlock, ContractBlock, PassengerBench, PassengerSeatLarge,\n" +
                "PassengerSeatSmallNew, PassengerSeatSmallOffset, BuggyCockpit,\n" +
                "OpenCockpitLarge, OpenCockpitSmall, LargeBlockCockpit,\n" +
                "CockpitOpen, SmallBlockStandingCockpit, RoverCockpit,\n" +
                "SpeederCockpitCompact, LargeBlockStandingCockpit,\n" +
                "LargeBlockLockerRoom, LargeBlockLockerRoomCorner, LargeBlockBed,\n" +
                "LargeCrate, LargeBlockHalfBed, LargeBlockHalfBedOffset,\n" +
                "LargeBlockInsetBed, LargeBlockInsetBookshelf, LargeBlockLockers,\n" +
                "LargeBlockInsetKitchen, LargeBlockWeaponRack, SmallBlockWeaponRack",
                ref configAltered, logKeyGeneration, pbName, textLog);
            tryCheckForDefaultIniKey(initTag, "APExcludedDeclarations", "", ref configAltered,
                logKeyGeneration, pbName, textLog);

            //If the configuration has changed (Because we added palette keys to it), we'll take this
            //opportunity to update the actual config on the PB.
            if (configAltered)
            { Me.CustomData = _iniReadWrite.ToString(); }
        }

        //assume that _iniRead has been loaded with a parse from the Save string and that _iniReadWrite
        //has been loaded with a parse from the PB (Or the block we're reading declarations from)
        public void evaluateDeclarations(IMyTerminalBlock sourceBlock, LimitedMessageLog textLog,
            Dictionary<string, IColorCoder> colorPalette, Dictionary<string, Tally> evalTallies,
            Dictionary<string, ActionSet> evalSets, Dictionary<string, Trigger> evalTriggers,
            Dictionary<string, Raycaster> evalRaycasters, HashSet<string> usedElementNames,
            MyIniParseResult parseResult, MyIniValue iniValue)
        {
            //Tallies are up first. From Tally sections, we read:
            //  Name: The name that will be associated with this tally.
            //  DisplayName: A name that will be shown on screens instead of the tally name
            //  Type: The type of this tally. Cargo or Item are common, and there are a number
            //    TallyGenerics like Battery, Gas, and PowerCurrent.
            //  ItemTypeID: For TallyItems, the ID that will be fed into MyItemType
            //  ItemSubyTypeID: For TallyItems, the sub type ID that will be fed into MyItemType
            //  Max: A user-definable value that will be used in place of the evaluate-calculated
            //    max. Required for some tallies, like TallyItems
            //  Multiplyer: The multiplier that will be applied to curr and max of this tally. 
            //  ColorCoder: The color coding scheme this tally will use. Can be lowGood, highGood,
            //    or any color if the color should not change based on the tally's value.
            Tally tally;
            string declarationSection;
            string programName;
            string tallyType;
            string typeID, subTypeID;
            Color color = Color.Black;
            IColorCoder colorCoder = null;
            ColorCoderLow lowGood = (ColorCoderLow)(colorPalette["lowgood"]);
            ColorCoderHigh highGood = (ColorCoderHigh)(colorPalette["highgood"]);
            List<string> raycasterDeclarationSections = new List<string>();
            List<TallyGeneric> raycasterTallies = new List<TallyGeneric>();
            //The troubleLogger we'll use to add errors to the textLog
            Action<string> errorLogger = b => textLog.addError(b);
            int index = 0;
            //_debugDisplay.WriteText("Initial Tally parse\n", true);
            //As long as the counter isn't -1 (Which indicates that we've run out of tallies)...
            while (index != -1)
            {
                tally = null;
                //Build a target declarationSection based on the current declaration type and index
                declarationSection = $"{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.Tally{index.ToString("D2")}";
                //Check to see if our parse actually contains this section
                if (_iniReadWrite.ContainsSection(declarationSection))
                {
                    programName = _iniReadWrite.Get(declarationSection, "Name").ToString();
                    if (string.IsNullOrEmpty(programName))
                    {
                        textLog.addError($"{declarationSection} has a missing or unreadable Name.");
                        //Use the declarationSection as the name so evaluate can continue
                        programName = declarationSection;
                    }

                    //Our next steps are going to be dictated by the TallyType. We should try and 
                    //figure out what that is.
                    tallyType = _iniReadWrite.Get(declarationSection, "Type").ToString().ToLowerInvariant();
                    if (string.IsNullOrEmpty(tallyType))
                    { textLog.addError($"{declarationSection}: {programName} has a missing or unreadable Type."); }
                    //Now, we create a tally based on the type. For the TallyCargo, that's quite straightforward.
                    else if (tallyType == "inventory")
                    { tally = new TallyCargo(_meterMaid, programName, lowGood); }
                    //Creating a TallyItem is a bit more involved.
                    else if (tallyType == "item")
                    {
                        //We'll need a typeID and a subTypeID, and we'll need to complain if we can't
                        //get them
                        typeID = _iniReadWrite.Get(declarationSection, "ItemTypeID").ToString();
                        if (string.IsNullOrEmpty(typeID))
                        { textLog.addError($"{declarationSection}: {programName} has a missing or unreadable ItemTypeID."); }
                        subTypeID = _iniReadWrite.Get(declarationSection, "ItemSubTypeID").ToString();
                        if (string.IsNullOrEmpty(subTypeID))
                        { textLog.addError($"{declarationSection}: {programName} has a missing or unreadable ItemSubTypeID."); }
                        //If we have the data we were looking for, we can create a TallyItem
                        if (!string.IsNullOrEmpty(typeID) && !string.IsNullOrEmpty(subTypeID))
                        { tally = new TallyItem(_meterMaid, programName, typeID, subTypeID, highGood); }
                    }
                    //On to the TallyGenerics. We'll start with Batteries
                    else if (tallyType == "battery")
                    { tally = new TallyGeneric(_meterMaid, programName, new BatteryHandler(), highGood); }
                    //Gas, which works for both Hydrogen and Oxygen
                    else if (tallyType == "gas")
                    { tally = new TallyGeneric(_meterMaid, programName, new GasHandler(), highGood); }
                    else if (tallyType == "jumpdrive")
                    { tally = new TallyGeneric(_meterMaid, programName, new JumpDriveHandler(), highGood); }
                    else if (tallyType == "raycast")
                    {
                        tally = new TallyGeneric(_meterMaid, programName, new RaycastHandler(), highGood);
                        //Raycasters can get some of their information from other script objects,
                        //but those objects haven't been initiated yet. So we'll store the data we'll
                        //need to make decisions about this later.
                        raycasterDeclarationSections.Add(declarationSection);
                        raycasterTallies.Add((TallyGeneric)tally);
                    }
                    else if (tallyType == "powermax")
                    { tally = new TallyGeneric(_meterMaid, programName, new PowerMaxHandler(), highGood); }
                    else if (tallyType == "powercurrent")
                    { tally = new TallyGeneric(_meterMaid, programName, new PowerCurrentHandler(), highGood); }
                    else if (tallyType == "integrity")
                    { tally = new TallyGeneric(_meterMaid, programName, new IntegrityHandler(), highGood); }
                    else if (tallyType == "ventpressure")
                    { tally = new TallyGeneric(_meterMaid, programName, new VentPressureHandler(), highGood); }
                    else if (tallyType == "pistonextension")
                    { tally = new TallyGeneric(_meterMaid, programName, new PistonExtensionHandler(), highGood); }
                    else if (tallyType == "rotorangle")
                    { tally = new TallyGeneric(_meterMaid, programName, new RotorAngleHandler(), highGood); }
                    else if (tallyType == "controllergravity")
                    { tally = new TallyGeneric(_meterMaid, programName, new ControllerGravityHandler(), highGood); }
                    else if (tallyType == "controllerspeed")
                    { tally = new TallyGeneric(_meterMaid, programName, new ControllerSpeedHandler(), highGood); }
                    else if (tallyType == "controllerweight")
                    { tally = new TallyGeneric(_meterMaid, programName, new ControllerWeightHandler(), highGood); }
                    //TODO: Aditional TallyTypes go here
                    else
                    {
                        //If we've gotten to this point, the user has given us a type that we can't 
                        //recognize. Scold them.
                        textLog.addError($"{declarationSection}: {programName} has un-recognized Type of '{tallyType}'.");
                    }
                    //If we've gotten to this point and we haven't put together enough information
                    //to make a proper tally, make a fake one using whatever data we have on hand.
                    //This will allow us to continue evaluation.
                    if (tally == null)
                    { tally = new TallyCargo(_meterMaid, programName, lowGood); }

                    //Now that we have our tally, we need to check to see if there's any further
                    //configuration data. 
                    //First, the DisplayName
                    iniValue = _iniReadWrite.Get(declarationSection, "DisplayName");
                    if (!iniValue.IsEmpty)
                    { tally.displayName = iniValue.ToString(); }
                    //Up next is the Multiplier. Note that, because of how forceMax works, the multiplier
                    //must be applied before the max.
                    iniValue = _iniReadWrite.Get(declarationSection, "Multiplier");
                    if (!iniValue.IsEmpty)
                    { tally.multiplier = iniValue.ToDouble(); }
                    //Then the Max
                    iniValue = _iniReadWrite.Get(declarationSection, "Max");
                    if (!iniValue.IsEmpty)
                    { tally.forceMax(iniValue.ToDouble()); }
                    //There's a couple of TallyTypes that need to have a Max explicitly set (All 
                    //TallyItems, plus the TallyGeneric ControllerWeight (But not PowerProducers, 
                    //that's fixed. And while Raycasters do need to be told what their max is, 
                    //there's two places they can get it, and we decide if they did later.)). 
                    //If that hasn't happened, we need to complain.
                    else if (iniValue.IsEmpty && (tally is TallyItem || (tally is TallyGeneric
                        && ((TallyGeneric)tally).handler is ControllerWeightHandler)))
                    {
                        textLog.addError($"{declarationSection}: {programName}'s TallyType of '{tallyType}' requires a Max " +
                            $"to be set in configuration.");
                    }
                    //Last step is to check for a custom ColorCoder
                    if (tryGetColorFromConfig(errorLogger, colorPalette, _iniReadWrite, iniValue, sourceBlock, ref color,
                        ref colorCoder, declarationSection, "ColorCoder"))
                    { tally.colorCoder = colorCoder; }

                    //Thats all the possible tally config. Our last step is to make sure the tally's 
                    //name isn't going to cause problems
                    if (isElementNameInUse(usedElementNames, tally.programName, declarationSection, textLog))
                    //If the name of our prosepective tally is already in use, exit the loop. Everything 
                    //in this script's config is name based, a naming error isn't something we can recover 
                    //from.
                    { break; }
                    else
                    {
                        //If the name checks out, go ahead and add the tally to our tally dictionary.
                        evalTallies.Add(tally.programName, tally);
                        //And add the name to our list of in-use Element names
                        usedElementNames.Add(tally.programName);
                        //Last step is to increment the counter, so we can look for the next tally.
                        index++;
                    }
                }
                else
                //If we didn't find the next Tally declaration in the sequence, set index to -1 to
                //exit the loop
                { index = -1; }
            }

            //ActionSets are up next. On this pass of ActionSet config, we read:
            //  Name: The name that will be associated with this tally.
            //  DisplayName: A name that will be shown on screens instead of the tally name
            //  ColorOn: The color that will be used for the set's element when it is on
            //  ColorOff: The color that will be used for the set's element when it is off
            //  TextOn: The text that will be diplsayed on the set's element when it is on
            //  TextOff: The text that will be diplsayed on the set's element when it is off
            //This is actually all the config that an ActionSet object holds. The remaining 
            //(multitude) of keys you sometimes see in config set up ActionPlans that manipulate
            //other script objects. We read those once we're sure all the possible script object
            //have been initialized.
            ActionSet set;
            bool state;
            //We make multiple (Two, at the moment) passes through the ActionSet configuration. We
            //need a list to store loaded ActionSets - in the order we read them - for later access.
            List<ActionSet> loadedActionSets = new List<ActionSet>();
            index = 0;
            //_debugDisplay.WriteText("Initial ActionSet parse\n", true);
            while (index != -1)
            {
                set = null;
                declarationSection = $"{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.ActionSet{index.ToString("D2")}";
                //Look for the declarationSection
                if (_iniReadWrite.ContainsSection(declarationSection))
                {
                    iniValue = _iniReadWrite.Get(declarationSection, "Name");
                    if (!iniValue.IsEmpty)
                    //If the user gave us a name, use it.
                    { programName = iniValue.ToString(); }
                    else
                    //If we didn't get a proper name, we'll use the declarationSection as the name
                    //so evaluation can continue.
                    {
                        programName = declarationSection;
                        //We'll also complain about it.
                        textLog.addError($"{declarationSection} has a missing or unreadable Name.");
                    }

                    //ActionSets have a lot less going on than tallies, initially at least. The only
                    //other thing we /need/ to know about them is what their previous state was.
                    //We'll try to get that from the storage string, defaulting to false if we can't
                    //The extra defensiveness here is for situations where we might be dealing with 
                    //non-PB config or a new ActionSet.
                    state = _iniRead?.Get("ActionSets", programName).ToBoolean(false) ?? false;
                    set = new ActionSet(programName, state);

                    //There are a few other bits of configuration that ActionSets may have
                    iniValue = _iniReadWrite.Get(declarationSection, "DisplayName");
                    if (!iniValue.IsEmpty)
                    { set.displayName = iniValue.ToString(); }
                    if (tryGetColorFromConfig(errorLogger, colorPalette, _iniReadWrite, iniValue, sourceBlock,
                        ref color, ref colorCoder, declarationSection, "ColorOn"))
                    { set.colorOn = color; }
                    if (tryGetColorFromConfig(errorLogger, colorPalette, _iniReadWrite, iniValue, sourceBlock,
                        ref color, ref colorCoder, declarationSection, "ColorOff"))
                    { set.colorOff = color; }
                    iniValue = _iniReadWrite.Get(declarationSection, "TextOn");
                    if (!iniValue.IsEmpty)
                    { set.textOn = iniValue.ToString(); }
                    iniValue = _iniReadWrite.Get(declarationSection, "TextOff");
                    if (!iniValue.IsEmpty)
                    { set.textOff = iniValue.ToString(); }
                    //That's it. We should have all the initial configuration for this ActionSet.
                }

                //This process is functionally identical to what we did for Tallies.
                if (set == null)
                { index = -1; }
                else if (isElementNameInUse(usedElementNames, set.programName, declarationSection, textLog))
                { break; }
                else
                {
                    //We might have changed what the set uses for status text or colors. A call to 
                    //evaluateStatus will set things right.
                    set.evaluateStatus();
                    //This ActionSet should be ready. Pass it to the dictionary.
                    evalSets.Add(set.programName, set);
                    //We'll need an ordered list of ActionSets for our second pass. Also add this 
                    //set to that list.
                    loadedActionSets.Add(set);
                    usedElementNames.Add(set.programName);
                    index++;
                }
            }

            //From trigger configuration, we read:
            //Name: The Element name of this Trigger
            //Tally: The name of the Tally this trigger will watch
            //ActionSet: The name of the ActionSet this trigger will operate
            //LessOrEqualValue: When the watched Tally falls below this value, the commandLess will be sent
            //LessOrEqualCommand: The command to be sent when we're under the threshold
            //GreaterOrEqualValue: When the watched Tally exceeds this value, the commandGreater will be sent
            //GreaterOrEqualCommand: The command to be sent when we're over the threshold
            Trigger trigger;
            string tallyName;
            string setName;
            bool initialState;
            index = 0;
            //_debugDisplay.WriteText("Initial Trigger parse\n", true);
            while (index != -1)
            {
                trigger = null;
                declarationSection = $"{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.Trigger{index.ToString("D2")}";
                if (_iniReadWrite.ContainsSection(declarationSection))
                {
                    tallyName = "";
                    tally = null;
                    setName = "";
                    set = null;

                    //Standard process at this point: Get the name or use the section header in its place
                    iniValue = _iniReadWrite.Get(declarationSection, "Name");
                    if (!iniValue.IsEmpty)
                    { programName = iniValue.ToString(); }
                    else
                    {
                        programName = declarationSection;
                        textLog.addError($"{declarationSection} has a missing or unreadable Name.");
                    }

                    //A trigger needs to have two piece of information: A Tally to watch, and an 
                    //ActionSet to manipulate.
                    iniValue = _iniReadWrite.Get(declarationSection, "Tally");
                    if (!iniValue.IsEmpty)
                    {
                        tallyName = iniValue.ToString();
                        //Try to match the tallyName to a configured Tally
                        if (!evalTallies.TryGetValue(tallyName, out tally))
                        {
                            textLog.addError($"{declarationSection}: {programName} tried to reference " +
                                $"the unconfigured Tally '{tallyName}'.");
                        }
                    }
                    else
                    { textLog.addError($"{declarationSection}: {programName} has a missing or unreadable Tally."); }

                    iniValue = _iniReadWrite.Get(declarationSection, "ActionSet");
                    if (!iniValue.IsEmpty)
                    {
                        setName = iniValue.ToString();
                        if (!evalSets.TryGetValue(setName, out set))
                        {
                            textLog.addError($"{declarationSection}: {programName} tried to reference " +
                                $"the unconfigured ActionSet '{setName}'.");
                        }
                    }
                    else
                    { textLog.addError($"{declarationSection}: {programName} has a missing or unreadable ActionSet."); }

                    //Triggers can be armed or disarmed, and this state persists through loads much
                    //like ActionSets. We'll try to figure out if this trigger is supposed to be
                    //armed or disarmed, arming it if we can't tell.
                    initialState = _iniRead?.Get("Triggers", programName).ToBoolean(true) ?? true;

                    /*_debugDisplay.WriteText($"About to check for existence of tally {tally?.programName ?? "<missing>"} " +
                        $"and ActionSet {set?.programName ?? "<missing>"}\n", true);*/

                    //If we've got the data we need, we'll make our new trigger.
                    if (tally != null && set != null)
                    {
                        trigger = new Trigger(programName, tally, set, initialState);
                        //Check for lessOrEqual and greaterOrEqual scenarios
                        tryGetCommandFromConfig(trigger, declarationSection, true, "LessOrEqual",
                            iniValue, textLog);
                        tryGetCommandFromConfig(trigger, declarationSection, false, "GreaterOrEqual",
                            iniValue, textLog);
                        //If I decide to allow customization of Trigger elements, that would go here.

                        //If we didn't find at least one scenario for this trigger, we can assume that 
                        //something is wrong.
                        if (!trigger.hasScenario())
                        {
                            textLog.addError($"{declarationSection}: {programName} does not define a valid " +
                                $"LessOrEqual or GreaterOrEqual scenario.");
                        }
                    }
                }

                if (trigger == null)
                { index = -1; }
                else if (isElementNameInUse(usedElementNames, trigger.programName, declarationSection, textLog))
                { break; }
                else
                {
                    evalTriggers.Add(trigger.programName, trigger);
                    usedElementNames.Add(trigger.programName);
                    index++;
                }
            }

            //Raycasters are now considered full script objects, with the powers and responsibilities
            //thereof. From Raycaster configuration, we read:
            //Name: The Element name of this Raycaster
            //Type: What type of Raycaster this isTally tally;
            string raycasterType;
            Raycaster raycaster;
            RaycasterModuleBase scanModule;
            string[] moduleConfigurationKeys;
            double[] moduleConfigurationValues;
            index = 0;
            //_debugDisplay.WriteText("Initial Raycaster parse\n", true);
            while (index != -1)
            {
                raycaster = null;
                scanModule = null;
                moduleConfigurationKeys = null;
                moduleConfigurationValues = null;
                declarationSection = $"{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.Raycaster{index.ToString("D2")}";
                if (_iniReadWrite.ContainsSection(declarationSection))
                {
                    programName = _iniReadWrite.Get(declarationSection, "Name").ToString();
                    if (string.IsNullOrEmpty(programName))
                    {
                        textLog.addError($"{declarationSection} has a missing or unreadable Name.");
                        programName = declarationSection;
                    }

                    //Create a scanning module based on the Raycaster type
                    raycasterType = _iniReadWrite.Get(declarationSection, "Type").ToString().ToLowerInvariant();
                    if (string.IsNullOrEmpty(raycasterType))
                    { textLog.addError($"{declarationSection}: {programName} has a missing or unreadable Type."); }
                    //For Linear Raycasters, we read:
                    //BaseRange: (Default: 1000): The distance of the first scan that will be 
                    //  performed by this raycaster
                    //Multiplier (Default: 3): The multipler that will be applied to each 
                    //  successive scan's distance
                    //MaxRange (Default: 27000): The maximum range to be scanned by this 
                    //  raycaster. The last scan performed will always be at this distance.
                    else if (raycasterType == "linear")
                    {
                        scanModule = new RaycasterModuleLinear();
                        moduleConfigurationKeys = RaycasterModuleLinear.getModuleConfigurationKeys();
                        //We'll need room for as many values as we have keys.
                        moduleConfigurationValues = new double[moduleConfigurationKeys.Length];
                    }
                    //TODO: Aditional scanner types go here
                    else
                    { textLog.addError($"{declarationSection}: {programName} has un-recognized Type of '{raycasterType}'."); }
                    //Scanning modules have their own configuration, but they tell us everything we 
                    //need to get that configuration for them. They also handle default values on
                    //their end, so we can basically force-feed them raw config.
                    if (scanModule != null)
                    {
                        for (int i = 0; i < moduleConfigurationKeys.Length; i++)
                        {
                            moduleConfigurationValues[i] =
                                _iniReadWrite.Get(declarationSection, moduleConfigurationKeys[i]).ToDouble(-1);
                        }
                        //Send retrieved configuration to the scanning module
                        scanModule.configureModuleByArray(moduleConfigurationValues);
                        //We should have everything we need to make a new Raycaster.
                        raycaster = new Raycaster(_sb, scanModule, programName);
                    }
                    //Perform the standard check for a unique ElementName, even if we didn't end up
                    //with enough config to create a raycaster
                    if (isElementNameInUse(usedElementNames, programName, declarationSection, textLog))
                    { break; }
                    else
                    {
                        if (raycaster != null)
                        { evalRaycasters.Add(raycaster.programName, raycaster); }
                        usedElementNames.Add(programName);
                        index++;
                    }
                }
                else
                //If we didn't find this declaration secion, exit the loop.
                { index = -1; }
            }

            //Now that we have at least a framework for all our script objects, we make a second pass
            //to create any nessecary links from one script object to another (Except Triggers, they 
            //had everything they needed on the first pass.)

            //Our first step on the second pass is to try to link raycaster tallies with their raycasters.
            TallyGeneric raycasterTally;
            string raycasterName;
            //_debugDisplay.WriteText("Tally Raycast pass\n", true);
            for (int i = 0; i < raycasterTallies.Count; i++)
            {
                declarationSection = raycasterDeclarationSections[i];
                raycasterTally = raycasterTallies[i];

                //Go back to this tally's declaration section and check to see if there's a raycaster 
                //defined there
                iniValue = _iniReadWrite.Get(declarationSection, "Raycaster");
                if (iniValue.IsEmpty)
                {
                    //It's fine if we didn't find a value for the Raycaster key... unless we haven't
                    //already forced the tally's max.
                    if (!raycasterTally.maxForced)
                    {
                        textLog.addError($"{declarationSection}: {raycasterTally.programName}'s " +
                            $"Type of 'Raycaster' requires either a Max or a linked Raycaster to " +
                            $"be set in configuration.");
                    }
                }
                else
                {
                    raycasterName = iniValue.ToString();
                    //If we have a raycaster name, but max on the tally has already been forced, we
                    //have a conflict. Inform the user.
                    if (raycasterTally.maxForced)
                    {
                        textLog.addWarning($"{declarationSection}: {raycasterTally.programName} specifies " +
                            $"both a Max and a linked Raycaster, '{raycasterName}'. Only one of these " +
                            $"values is required. The linked Raycaster has been ignored.");
                    }
                    else
                    {
                        //If the string we just retrieved matches the name of one of the raycasters we've 
                        //already retrieved...
                        if (evalRaycasters.TryGetValue(raycasterName, out raycaster))
                        { raycasterTally.forceMax(raycaster.getModuleRequiredCharge()); }
                        else
                        {
                            textLog.addError($"{declarationSection}: {raycasterTally.programName} tried " +
                                $"to reference the unconfigured Raycaster '{raycasterName}'.");
                        }
                    }
                }
            }

            //In the second pass on ActionSets, we read: 
            //  DelayOn: How many update tics should be skipped when this set is On
            //  DelayOff
            //  IGCChannel: The channel that IGC messages should be sent on
            //  IGCMessageOn: The message that will be sent on the IGCChannel when this set is toggled On
            //  IGCMessageOff
            //  ActionSetsLinkedToOn: A list of ActionSets and the states they should be set to when this set is toggled On
            //  ActionSetsLinkedToOff
            //  TriggersLinkedToOn: A list of Triggers and if they should be armed or disarmed when this set is toggled On
            //  TriggersLinkedToOff
            //Now that we have objects corresponding to all the config on the PB, we can make the 
            //final pass where we handle ActionSets that manipulate other script objects.
            //The identifier that will be used to point the user to where an error is ocurring.
            string setTitle, targetKey, troubleID;
            int delayOn, delayOff;
            string channel;
            List<KeyValuePair<string, bool>> parsedData = new List<KeyValuePair<string, bool>>();
            ActionSet targetSet = null;
            Trigger targetTrigger = null;
            Raycaster targetRaycaster = null;
            //_debugDisplay.WriteText("ActionSet script object pass\n", true); 
            for (int i = 0; i < loadedActionSets.Count; i++)
            {
                /*readPBPlansFromConfig(_sets, evalTriggers, this._iniReadWrite, initTag,
                    i, set, iniValue, textLog);*/

                set = loadedActionSets[i];
                //Re-generate the section tag for this ActionSet
                declarationSection = $"{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.ActionSet{i.ToString("D2")}";
                setTitle = $"{declarationSection}: {set.programName}";

                //We'll start with ActionPlanUpdate.
                //DelayOn and DelayOff. These will actually be stored in an ActionPlan, but we
                //need to know if one of the values is present before we create the object.
                //If no value is found, a zero will be returned.
                delayOn = _iniReadWrite.Get(declarationSection, $"DelayOn").ToInt32();
                delayOff = _iniReadWrite.Get(declarationSection, $"DelayOff").ToInt32();
                //If one of the delay values isn't 0...
                if (delayOn != 0 || delayOff != 0)
                {
                    //Create a new action plan
                    ActionPlanUpdate updatePlan = new ActionPlanUpdate(_distributor);
                    //Store the values we got. No need to run any checks here, they'll be fine
                    //if we pass them zeros
                    updatePlan.delayOn = delayOn;
                    updatePlan.delayOff = delayOff;
                    //Add the update plan to this ActionSet.
                    set.addActionPlan(updatePlan);
                }

                //ActionPlanIGC
                iniValue = _iniReadWrite.Get(declarationSection, $"IGCChannel");
                if (!iniValue.IsEmpty)
                {
                    channel = iniValue.ToString();
                    //Create a new action plan, using the string we collected as the channel
                    ActionPlanIGC igcPlan = new ActionPlanIGC(IGC, channel);
                    iniValue = _iniReadWrite.Get(declarationSection, $"IGCMessageOn");
                    if (!iniValue.IsEmpty)
                    { igcPlan.messageOn = iniValue.ToString(); }
                    iniValue = _iniReadWrite.Get(declarationSection, $"IGCMessageOff");
                    if (!iniValue.IsEmpty)
                    { igcPlan.messageOff = iniValue.ToString(); }
                    //Last step is to make sure we got some config
                    if (igcPlan.hasAction())
                    { set.addActionPlan(igcPlan); }
                    else
                    {
                        textLog.addError($"{setTitle} has configuration for sending an IGC message " +
                            $"on the channel '{channel}', but does not have readable config on what " +
                            $"messages should be sent.");
                    }
                }

                //ActionPlanActionSet
                targetKey = "ActionSetsLinkedToOn";
                iniValue = _iniReadWrite.Get(declarationSection, targetKey);
                if (!iniValue.IsEmpty)
                {
                    troubleID = $"{setTitle}'s {targetKey} list";
                    parseStateList(iniValue.ToString(), troubleID, errorLogger, parsedData);
                    foreach (KeyValuePair<string, bool> pair in parsedData)
                    {
                        //Try to match the named set to one of our actual sets
                        if (evalSets.TryGetValue(pair.Key, out targetSet))
                        {
                            ActionPlanActionSet setPlan = new ActionPlanActionSet(targetSet);
                            setPlan.setReactionToOn(pair.Value);
                            set.addActionPlan(setPlan);
                        }
                        //If we can't match the key from this pair to an existing set, log an error.
                        else
                        { textLog.addError($"{troubleID} references the unconfigured ActionSet {pair.Key}."); }
                    }
                }
                //Handling ActionSetOff is functionally identical to the process for ActionSetOn.
                targetKey = "ActionSetsLinkedToOff";
                iniValue = _iniReadWrite.Get(declarationSection, targetKey);
                if (!iniValue.IsEmpty)
                {
                    troubleID = $"{setTitle}'s {targetKey} list";
                    parseStateList(iniValue.ToString(), troubleID, errorLogger, parsedData);
                    foreach (KeyValuePair<string, bool> pair in parsedData)
                    {
                        if (evalSets.TryGetValue(pair.Key, out targetSet))
                        {
                            ActionPlanActionSet setPlan = new ActionPlanActionSet(targetSet);
                            setPlan.setReactionToOff(pair.Value);
                            set.addActionPlan(setPlan);
                        }
                        else
                        { textLog.addError($"{troubleID} references the unconfigured ActionSet {pair.Key}."); }
                    }
                }

                //ActionPlanTrigger
                //Which is functionally identical to how we handle AP:AS
                targetKey = "TriggersLinkedToOn";
                iniValue = _iniReadWrite.Get(declarationSection, targetKey);
                if (!iniValue.IsEmpty)
                {
                    troubleID = $"{setTitle}'s {targetKey} list";
                    parseStateList(iniValue.ToString(), troubleID, errorLogger, parsedData);
                    foreach (KeyValuePair<string, bool> pair in parsedData)
                    {
                        //Try to match the named set to one of our actual sets
                        if (evalTriggers.TryGetValue(pair.Key, out targetTrigger))
                        {
                            ActionPlanTrigger triggerPlan = new ActionPlanTrigger(targetTrigger);
                            triggerPlan.setReactionToOn(pair.Value);
                            set.addActionPlan(triggerPlan);
                        }
                        //If we can't match the key from this pair to an existing set, log an error.
                        else
                        { textLog.addError($"{troubleID} references the unconfigured ActionSet {pair.Key}."); }
                    }
                }
                targetKey = "TriggersLinkedToOff";
                iniValue = _iniReadWrite.Get(declarationSection, targetKey);
                if (!iniValue.IsEmpty)
                {
                    troubleID = $"{setTitle}'s {targetKey} list";
                    parseStateList(iniValue.ToString(), troubleID, errorLogger, parsedData);
                    foreach (KeyValuePair<string, bool> pair in parsedData)
                    {
                        if (evalTriggers.TryGetValue(pair.Key, out targetTrigger))
                        {
                            ActionPlanTrigger triggerPlan = new ActionPlanTrigger(targetTrigger);
                            triggerPlan.setReactionToOff(pair.Value);
                            set.addActionPlan(triggerPlan);
                        }
                        else
                        { textLog.addError($"{troubleID} references the unconfigured ActionSet {pair.Key}."); }
                    }
                }

                //RaycastPerformedOnState
                targetKey = "RaycastPerformedOnState";
                iniValue = _iniReadWrite.Get(declarationSection, targetKey);
                if (!iniValue.IsEmpty)
                {
                    troubleID = $"{setTitle}'s {targetKey} list";
                    parseStateList(iniValue.ToString(), troubleID, errorLogger, parsedData);
                    foreach (KeyValuePair<string, bool> pair in parsedData)
                    {
                        //Try to match the named raycaster to one of our configured raycasters
                        if (evalRaycasters.TryGetValue(pair.Key, out targetRaycaster))
                        {
                            ActionPlanRaycaster raycasterPlan = new ActionPlanRaycaster(targetRaycaster);
                            //Unlike other state lists, for raycasters, the boolean portion of each 
                            //element tells us if we perform the scan when the ActionSet is switched
                            //On, or if we perform the scan when it's switched Off.
                            if (pair.Value)
                            { raycasterPlan.scanOn = true; }
                            else
                            { raycasterPlan.scanOff = true; }
                            set.addActionPlan(raycasterPlan);
                        }
                        //If we can't match the key from this pair to an existing set, log an error.
                        else
                        { textLog.addError($"{troubleID} references the unconfigured Raycaster {pair.Key}."); }
                    }
                }
            }

            //_debugDisplay.WriteText("evaluateDeclaration complete.\n", true);
            //If we don't have errors, but we also don't have any tallies or ActionSets...
            if (textLog.getErrorTotal() == 0 && evalTallies.Count == 0 && evalSets.Count == 0)
            { textLog.addError($"No readable configuration found on the programmable block."); }
        }


        public int evaluateGrid(LimitedMessageLog textLog, Dictionary<string, IColorCoder> colorPalette,
            Dictionary<string, Tally> evalTallies, Dictionary<string, ActionSet> evalSets,
            Dictionary<string, Trigger> evalTriggers, Dictionary<string, Raycaster> evalRaycasters,
            Dictionary<IMyInventory, List<TallyCargo>> evalContainers, Dictionary<string, MFD> evalMFDs,
            List<IReportable> evalReports, List<WallOText> evalLogReports,
            Dictionary<string, Indicator> evalIndicators, MyIniParseResult parseResult, MyIniValue iniValue)
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            Dictionary<string, Action<IMyTerminalBlock>> actions = compileActions();
            List<KeyValuePair<string, bool>> parsedData = new List<KeyValuePair<string, bool>>();
            Action<string> warningLogger = b => textLog.addWarning(b);
            Tally tally;
            ActionSet actionSet;
            IColorCoder colorCoder = null;
            Color color = Color.White;
            string[] elementNames;
            string elementName = "";
            string discreteTag = "";
            string configKey = "";
            string troubleID = "";
            int counter = 0;
            bool handled;

            findBlocks<IMyTerminalBlock>(blocks, b =>
                (b.IsSameConstructAs(Me) && MyIni.HasSection(b.CustomData, _tag)));
            if (blocks.Count <= 0)
            { textLog.addError($"No blocks found on this construct with a {_tag} INI section."); }

            foreach (IMyTerminalBlock block in blocks)
            {
                //_debugDisplay.WriteText($"  Beginning evaluationg for block {block.CustomName}\n", true);
                //Whatever kind of block this is, we're going to need to see what's in its 
                //CustomData. If that isn't useable...
                if (!_iniReadWrite.TryParse(block.CustomData, out parseResult))
                //...complain.
                {
                    textLog.addWarning($"Configuration on block '{block.CustomName}' has been " +
                        $"ignored because of the following parsing error on line {parseResult.LineNo}: " +
                        $"{parseResult.Error}");
                }
                else
                //My comedic, reference-based genius shall be preserved here for all eternity. Even
                //if it is now largely irrelevant to how Shipware operates.
                //In the CargoManager, the data is handled by two seperate yet equally important
                //objects: the Tallies that store and calculate information and the Reports that 
                //display it. These are their stories.
                {
                    handled = false;
                    //On most builds, most of what we'll be dealing with are tallies. So let's start there.
                    //_debugDisplay.WriteText("    Tally handler\n", true);
                    if (_iniReadWrite.ContainsKey(_tag, "Tallies"))
                    { //This is grounds for declaring this block to be handled.
                        handled = true;
                        //Get the 'Tallies' data
                        iniValue = _iniReadWrite.Get(_tag, "Tallies");
                        //Split the Tallies string into individual tally names
                        elementNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                        foreach (string name in elementNames)
                        {
                            if (!evalTallies.ContainsKey(name))
                            {
                                textLog.addWarning($"Block '{block.CustomName}' tried to reference the " +
                                      $"unconfigured Tally '{name}'.");
                            }
                            else
                            {
                                tally = evalTallies[name];
                                if (tally is TallyCargo)
                                {
                                    //Tally Cargos require an inventory. It's kinda their thing.
                                    if (!block.HasInventory)
                                    {
                                        textLog.addWarning($"Block '{block.CustomName}' does not have an " +
                                            $"inventory and is not compatible with the Type of " +
                                            $"Tally '{name}'.");
                                    }
                                    else
                                    {
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
                                            evalContainers[inventory].Add((TallyCargo)tally);
                                        }
                                    }
                                }
                                else if (tally is TallyGeneric)
                                {
                                    if (!((TallyGeneric)tally).tryAddBlock(block))
                                    {
                                        textLog.addWarning($"Block '{block.CustomName}' is not a " +
                                            $"{((TallyGeneric)tally).getTypeAsString()} and is not " +
                                            $"compatible with the Type of Tally '{name}'.");
                                    }
                                }
                                else
                                //If a tally isn't a TallyCargo or a TallyGeneric or a TallyShield, I done goofed.
                                {
                                    textLog.addWarning($"Block '{block.CustomName}' refrenced the Tally " +
                                        $"'{name}', which has an unhandled Tally Type. Complain to " +
                                        $"the script writer, this should be impossible.");
                                }
                            }
                        }
                    }

                    //If the block has an inventory, it may have 'Inv<#>Tallies' keys instead. We need
                    //to check for them.
                    //_debugDisplay.WriteText("    Multiple inventory Tally handler\n", true);
                    if (block.HasInventory)
                    {
                        for (int i = 0; i < block.InventoryCount; i++)
                        {
                            if (!_iniReadWrite.ContainsKey(_tag, $"Inv{i}Tallies"))
                            //If the key does not exist, fail silently.
                            { }
                            else
                            {
                                //If we manage to find one of these keys, the block can be considered
                                //handled.
                                handled = true;
                                //Get the names of the specified tallies
                                iniValue = _iniReadWrite.Get(_tag, $"Inv{i}Tallies");
                                elementNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                                foreach (string name in elementNames)
                                {
                                    if (!evalTallies.ContainsKey(name))
                                    {
                                        textLog.addWarning($"Block '{block.CustomName}' tried to reference the " +
                                            $"unconfigured Tally '{name}' in key Inv{i}Tallies.");
                                    }
                                    else
                                    {
                                        tally = evalTallies[name];
                                        //Before we move on, we need to make sure this is a tallyCargo.
                                        if (!(tally is TallyCargo))
                                        {
                                            textLog.addWarning($"Block '{block.CustomName}' is not compatible " +
                                                $"with the Type of Tally '{name}' referenced in key " +
                                                $"Inv{i}Tallies.");
                                        }
                                        else
                                        {
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
                                            evalContainers[inventory].Add((TallyCargo)tally);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    //Next up: ActionSets.
                    //_debugDisplay.WriteText("    ActionSet handler\n", true);
                    if (_iniReadWrite.ContainsKey(_tag, "ActionSets"))
                    {
                        //From the main section, we read:
                        //ActionSets: The ActionSet section names that can be found elsewhere in this 
                        //  block's CustomData.
                        //We found something we understand, declare handled.
                        handled = true;
                        //Get the 'ActionSets' data
                        iniValue = _iniReadWrite.Get(_tag, "ActionSets");
                        //Pull the individual ActionSet names from the ActionSets key.
                        elementNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                        foreach (string name in elementNames)
                        {
                            //First things first: Does this ActionSet even exist?
                            if (!evalSets.ContainsKey(name))
                            {
                                textLog.addWarning($"Block '{block.CustomName}' tried to reference the " +
                                    $"unconfigured ActionSet '{name}'.");
                            }
                            else
                            {
                                actionSet = evalSets[name];
                                //The name of the discrete section that will configure this ActionSet 
                                //is the PREFIX plus the name of the ActionSet. We'll be using that a 
                                //lot, so let's put a handle on it.
                                discreteTag = $"{_SCRIPT_PREFIX}.{name}";
                                //Check to see if the user has included an ACTION SECTION
                                if (!_iniReadWrite.ContainsSection(discreteTag))
                                {
                                    textLog.addWarning($"Block '{block.CustomName}' references the ActionSet " +
                                        $"'{name}', but contains no discrete '{discreteTag}' section that would " +
                                        $"define actions.");
                                }
                                else
                                {
                                    IHasActionPlan actionPlan = null;
                                    if (_iniReadWrite.ContainsKey(discreteTag, "Action0Property"))
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
                                            retreivedPart = tryGetPartFromConfig(textLog, discreteTag,
                                                counter, block, _iniReadWrite, iniValue, colorPalette, colorCoder);
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
                                    else if (_iniReadWrite.ContainsKey(discreteTag, "ActionsOn")
                                        || _iniReadWrite.ContainsKey(discreteTag, "ActionsOff"))
                                    //If the discrete section contains either ActionsOn or ActionsOff 
                                    //keys, we need to use a MultiActionPlanBlock.
                                    //From config for MultiActionPlanBlock, we read:
                                    //ActionsOn (Default: null): A list of actions to be performed 
                                    //  on this block when this ActionSet is set to 'on'.
                                    //ActionsOff (Default: null): A list of actions to be performed 
                                    //  on this block when thisActionSet is set to 'off'.
                                    {
                                        MultiActionPlanBlock mapb = new MultiActionPlanBlock(block);
                                        mapb.actionsOn = getActionHandlersForMAPB(_iniReadWrite,
                                            discreteTag, "ActionsOn", actions, textLog, block);
                                        mapb.actionsOff = getActionHandlersForMAPB(_iniReadWrite,
                                            discreteTag, "ActionsOff", actions, textLog, block);

                                        actionPlan = mapb;
                                    }
                                    else if (_iniReadWrite.ContainsKey(discreteTag, "ActionOn")
                                        || _iniReadWrite.ContainsKey(discreteTag, "ActionOff"))
                                    //If we've got an ActionOn or ActionOff key, we use ActionPlanBlock.
                                    //APB is functionally identical to MAPB, just with a single action
                                    //instead of a multitude
                                    {
                                        //Create a new block plan with this block as the subject
                                        ActionPlanBlock apb = new ActionPlanBlock(block);
                                        iniValue = _iniReadWrite.Get(discreteTag, "ActionOn");
                                        if (!iniValue.IsEmpty)
                                        {
                                            apb.actionOn = retrieveActionHandler(iniValue.ToString(),
                                            actions, textLog, block, discreteTag, "ActionOn");
                                        }

                                        iniValue = _iniReadWrite.Get(discreteTag, "ActionOff");
                                        if (!iniValue.IsEmpty)
                                        {
                                            apb.actionOff = retrieveActionHandler(iniValue.ToString(),
                                            actions, textLog, block, discreteTag, "ActionOff");
                                        }

                                        actionPlan = apb;
                                    }
                                    //If we have successfully registered at least one action...
                                    if (actionPlan.hasAction())
                                    //Go ahead and add this ActionPlan to the ActionSet
                                    { actionSet.addActionPlan(actionPlan); }
                                    //If we didn't successfully register an action, complain.
                                    else
                                    {
                                        textLog.addWarning($"Block '{block.CustomName}', discrete section '{discreteTag}', " +
                                            $"does not define any actions to be taken when the ActionSet changes state.");
                                    }
                                }
                            }
                        }
                    }

                    //Tally and ActionSet configuration can be on almost any block. But some 
                    //configuration can only be used on certain block types
                    //Raycasters are now largely configured from the PB. But we still have to tell
                    //them where their cameras are.
                    //_debugDisplay.WriteText("    Camera handler\n", true);
                    if (block is IMyCameraBlock)
                    {
                        if (_iniReadWrite.ContainsKey(_tag, "Raycasters"))
                        {
                            handled = true;
                            iniValue = _iniReadWrite.Get(_tag, "Raycasters");
                            elementNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                            foreach (string name in elementNames)
                            {
                                if (!evalRaycasters.ContainsKey(name))
                                {
                                    textLog.addWarning($"Camera '{block.CustomName}' tried to reference the " +
                                          $"unconfigured Raycaster '{name}'.");
                                }
                                else
                                { evalRaycasters[name].addCamera((IMyCameraBlock)block); }
                            }
                        }
                    }

                    //SurfaceProviders can have config for Pages and Reportables
                    //_debugDisplay.WriteText("    Surface handler\n", true);
                    //Note: Nearly everything is a SurfaceProvider now. We could check the surface
                    //count to determine if the block actually has any screens, but we basically
                    //do that after the cast, so adding a check at this step probably isn't needed.
                    if (block is IMyTextSurfaceProvider)
                    {
                        IMyTextSurfaceProvider surfaceProvider = (IMyTextSurfaceProvider)block;
                        for (int i = 0; i < surfaceProvider.SurfaceCount; i++)
                        {
                            //Each surface on the block may have its own config
                            configKey = $"Surface{i}Pages";
                            if (_iniReadWrite.ContainsKey(_tag, configKey))
                            {
                                //We're probably going to put something on this surface. Put a handle on it
                                IMyTextSurface surface = surfaceProvider.GetSurface(i);
                                //We might have an MFD. Go ahead and make a variable so we can make decisions.
                                MFD mfd = null;
                                IReportable reportable = null;
                                //Once we've established that there's config for this particular 
                                //surface, the first part of the process is identical to retrieving
                                //data from the rest of the common config section.
                                handled = true;
                                iniValue = _iniReadWrite.Get(_tag, configKey);
                                elementNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                                //The first difference depends on how many discrete sections we're 
                                //being pointed to. If there's more than one, this is an MFD, and
                                //we need a name.
                                if (elementNames.Length > 1)
                                {
                                    string mfdKey = $"Surface{i}MFD";
                                    if (!_iniReadWrite.ContainsKey(_tag, mfdKey))
                                    {
                                        textLog.addWarning($"Surface provider '{block.CustomName}', key {configKey} " +
                                            $"references multiple pages which must be managed by an MFD, " +
                                            $"but has no {mfdKey} key to define that object's name.");
                                    }
                                    else
                                    {
                                        string mfdName = _iniReadWrite.Get(_tag, mfdKey).ToString();
                                        //_debugDisplay.WriteText("      Have MFD name\n", true);
                                        if (evalMFDs.ContainsKey(mfdName))
                                        {
                                            textLog.addWarning($"Surface provider '{block.CustomName}', key {mfdKey} " +
                                              $"declares the MFD '{mfdName}' but this name is already in use.");
                                        }
                                        else
                                        { mfd = new MFD(mfdName); }
                                    }
                                }
                                foreach (string name in elementNames)
                                {
                                    //_debugDisplay.WriteText($"        Beginning loop for element '{name}'\n", true);
                                    discreteTag = $"{_SCRIPT_PREFIX}.{name}";
                                    if (!_iniReadWrite.ContainsSection(discreteTag))
                                    {
                                        textLog.addWarning($"Surface provider '{block.CustomName}', key {configKey} declares the " +
                                            $"page '{name}', but contains no discrete '{discreteTag}' section that would " +
                                            $"configure that page.");
                                    }
                                    else
                                    {
                                        //At this point, we should have everything we need to start constructing a report.
                                        reportable = null;
                                        //If this is a report, it will have an 'Elements' key.
                                        if (_iniReadWrite.ContainsKey(discreteTag, "Elements"))
                                        {
                                            //_debugDisplay.WriteText("          Entering Report branch\n", true);
                                            iniValue = _iniReadWrite.Get(discreteTag, "Elements");
                                            Report report = null;
                                            List<IHasElement> elementRefs = new List<IHasElement>();
                                            string[] elements = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                                            foreach (string element in elements)
                                            {
                                                //Is this a blank slot in the report?
                                                if (element.ToLowerInvariant() == "blank")
                                                //Just add a null to the list. The report will know how to handle 
                                                //this.
                                                { elementRefs.Add(null); }
                                                else
                                                {
                                                    //If it isn't a blank, we'll need to try and get the element from 
                                                    //evalTallies or sets
                                                    if (evalTallies.ContainsKey(element))
                                                    { elementRefs.Add(evalTallies[element]); }
                                                    else if (evalSets.ContainsKey(element))
                                                    { elementRefs.Add(evalSets[element]); }
                                                    else if (evalTriggers.ContainsKey(element))
                                                    { elementRefs.Add(evalTriggers[element]); }
                                                    else
                                                    {
                                                        //And complain, if appropriate.
                                                        textLog.addWarning($"Surface provider '{block.CustomName}', " +
                                                            $"section {discreteTag} tried to reference the " +
                                                            $"unconfigured element '{element}'.");
                                                    }
                                                }
                                            }
                                            //Create a new report with the data we've collected so far.
                                            report = new Report(surface, elementRefs);
                                            //Now that we have a report, we need to see if the user wants anything 
                                            //special done with it.
                                            //Title
                                            iniValue = _iniReadWrite.Get(discreteTag, "Title");
                                            if (!iniValue.IsEmpty)
                                            { report.title = iniValue.ToString(); }
                                            //FontSize
                                            iniValue = _iniReadWrite.Get(discreteTag, "FontSize");
                                            if (!iniValue.IsEmpty)
                                            { report.fontSize = iniValue.ToSingle(); }
                                            //Font
                                            iniValue = _iniReadWrite.Get(discreteTag, "Font");
                                            if (!iniValue.IsEmpty)
                                            { report.font = iniValue.ToString(); }
                                            //Columns. IMPORTANT: Set anchors is no longer called during object
                                            //creation, and therefore MUST be called before the report is finished.
                                            iniValue = _iniReadWrite.Get(discreteTag, "Columns");
                                            //Call setAnchors, using a default value of 1 if we didn't get 
                                            //configuration data.
                                            report.setAnchors(iniValue.ToInt32(1), _sb);

                                            //We've should have all the available configuration for this report. Now we'll point
                                            //Reportable at it and move on.
                                            reportable = report;
                                        }

                                        //If this is a GameScript, it will have a 'Script' key.
                                        else if (_iniReadWrite.ContainsKey(discreteTag, "Script"))
                                        {
                                            //_debugDisplay.WriteText("          Entering Script branch\n", true);
                                            GameScript script = new GameScript(surface,
                                                _iniReadWrite.Get(discreteTag, "Script").ToString());
                                            //Scripts are pretty straightforward. Off to reportable with them.
                                            reportable = script;
                                        }

                                        //If this is a WallOText, it will have a 'DataType' key.
                                        else if (_iniReadWrite.ContainsKey(discreteTag, "DataType"))
                                        {
                                            //_debugDisplay.WriteText("          Entering WOT branch\n", true);
                                            string type = _iniReadWrite.Get(discreteTag, "DataType").ToString().ToLowerInvariant();
                                            //The broker that will store the data for this WallOText
                                            IHasData broker = null;

                                            if (type == "log")
                                            //Logs and Storage will not need a DataSource; there can be only one
                                            { broker = new LogBroker(_log); }
                                            else if (type == "storage")
                                            { broker = new StorageBroker(this); }
                                            //CustomData, DetailInfo, CustomInfo, and Raycasters need to have a data source
                                            //specified.
                                            //CustomData, DetailInfo, and CustomInfo all get their data from blocks.
                                            else if (type == "customdata" || type == "detailinfo" || type == "custominfo")
                                            {
                                                //Check to see if the user provided a DataSource
                                                if (!_iniReadWrite.ContainsKey(discreteTag, "DataSource"))
                                                {
                                                    textLog.addWarning($"Surface provider '{block.CustomName}', section " +
                                                        $"{discreteTag} has a DataType of {type}, but a missing or " +
                                                        $"unreadable DataSource.");
                                                }
                                                else
                                                {
                                                    string source = _iniReadWrite.Get(discreteTag, "DataSource").ToString();
                                                    //Make a good faith effort to find the block the user is after.
                                                    IMyTerminalBlock subject = GridTerminalSystem.GetBlockWithName(source);
                                                    //If we found a block, and we need a CustomDataBroker
                                                    if (subject != null && type == "customdata")
                                                    { broker = new CustomDataBroker(subject); }
                                                    //If we found a block, and we need a DetailInfoBroker
                                                    else if (subject != null && type == "detailinfo")
                                                    { broker = new DetailInfoBroker(subject); }
                                                    else if (subject != null && type == "custominfo")
                                                    { broker = new CustomInfoBroker(subject); }
                                                    //If we didn't find a block, complain.
                                                    else
                                                    {
                                                        textLog.addWarning($"Surface provider '{block.CustomName}', section " +
                                                            $"{discreteTag} tried to reference the unknown block '{source}' " +
                                                            $"as a DataSource.");
                                                    }
                                                }
                                            }
                                            //Raycasters get their data from Raycaster objects.
                                            else if (type == "raycaster")
                                            {
                                                //_debugDisplay.WriteText("          Entering Raycaster branch\n", true);
                                                //Check to see if the user provided a DataSource
                                                if (!_iniReadWrite.ContainsKey(discreteTag, "DataSource"))
                                                {
                                                    textLog.addWarning($"Surface provider '{block.CustomName}', section " +
                                                        $"{discreteTag} has a DataType of {type}, but a missing or " +
                                                        $"unreadable DataSource.");
                                                }
                                                else
                                                {
                                                    //_debugDisplay.WriteText("          Found DataSource\n", true);
                                                    string source = _iniReadWrite.Get(discreteTag, "DataSource").ToString();
                                                    //Check our list of Raycasters to see if one has a matching key
                                                    if (evalRaycasters.ContainsKey(source))
                                                    { broker = new RaycastBroker(evalRaycasters[source]); }
                                                    //If we didn't find matching raycaster, complain.
                                                    else
                                                    {
                                                        textLog.addWarning($"Surface provider '{block.CustomName}', section " +
                                                            $"{discreteTag} tried to reference the unknown Raycaster " +
                                                            $"'{source}' as a DataSource.");
                                                    }
                                                }
                                            }
                                            else
                                            //If we don't recognize the DataType, complain.
                                            {
                                                textLog.addWarning($"Surface provider '{block.CustomName}', section " +
                                                    $"{discreteTag} tried to reference the unknown data type '{type}'.");
                                            }
                                            //If we came through that with some sort of broker
                                            if (broker != null)
                                            {
                                                //Create a new WallOText using our surface and the broker we've found.
                                                WallOText wall = new WallOText(surface, broker, _sb);
                                                //Configure any other settings that the user has seen fit to specify.
                                                //FontSize
                                                iniValue = _iniReadWrite.Get(discreteTag, "FontSize");
                                                if (!iniValue.IsEmpty)
                                                { wall.fontSize = iniValue.ToSingle(); }
                                                //Font
                                                iniValue = _iniReadWrite.Get(discreteTag, "Font");
                                                if (!iniValue.IsEmpty)
                                                { wall.font = iniValue.ToString(); }
                                                //CharPerLine
                                                iniValue = _iniReadWrite.Get(discreteTag, "CharPerLine");
                                                if (!iniValue.IsEmpty)
                                                //The PrepareText method that applies the charPerLine word wrap is quite 
                                                //ineffecient, and I only tolerate it because most of the WoT types include some
                                                //sort of mechanism that reduces the number of times it's called. Not so with 
                                                //DetailInfo, which conceivably could be calling it every single update. To avoid
                                                //that, and because DetailInfo is already formatted, we simply pitch a fit if
                                                //the user tries to use the two in conjunction.
                                                //The new customInfo is basically the same thing, so we'll add it to the list
                                                {
                                                    if (type == "detailinfo" || type == "custominfo")
                                                    {
                                                        textLog.addWarning($"Surface provider '{block.CustomName}', section " +
                                                            $"{discreteTag} tried to set a CharPerLine limit with the {type} " +
                                                            $"DataType. This is not allowed.");
                                                    }
                                                    else
                                                    { wall.setCharPerLine(iniValue.ToInt32()); }
                                                }
                                                //One last thing: If this is a log, we want to know where it lives.
                                                if (type == "log")
                                                { evalLogReports.Add(wall); }
                                                //Send this WallOText on its way with a fond fairwell.
                                                reportable = wall;
                                            }
                                        }

                                        //One last step: All of our reportables have fore and back colors. If we actually have
                                        //a reportable at this point, we should see if the user wants to set them to something.
                                        if (reportable != null)
                                        {
                                            //Foreground color
                                            if (tryGetColorFromConfig(warningLogger, colorPalette, _iniReadWrite,
                                                iniValue, block, ref color, ref colorCoder, discreteTag, $"ForeColor"))
                                            { ((IHasColors)reportable).foreColor = color; }
                                            //Background color
                                            if (tryGetColorFromConfig(warningLogger, colorPalette, _iniReadWrite,
                                                iniValue, block, ref color, ref colorCoder, discreteTag, $"BackColor"))
                                            { ((IHasColors)reportable).backColor = color; }
                                        }
                                    }
                                    //There's a couple of extra steps that we need to go through if 
                                    //we're dealing with an MFD
                                    if (mfd != null && reportable != null)
                                    {
                                        //This page may have a ShowOnActionState key, meaning we need
                                        //to hook it to an ActionSet.
                                        iniValue = _iniReadWrite.Get(discreteTag, "ShowOnActionState");
                                        if (!iniValue.IsEmpty)
                                        {
                                            troubleID = $"Surface provider '{block.CustomName}', section {discreteTag}";
                                            parseStateList(iniValue.ToString(), troubleID, warningLogger, parsedData);
                                            foreach (KeyValuePair<string, bool> pair in parsedData)
                                            {
                                                //Try to match the named ActionSet to one of our configured ActionSets
                                                if (!evalSets.TryGetValue(pair.Key, out actionSet))
                                                { textLog.addWarning($"{troubleID} tried to reference the unconfigured ActionSet {pair.Key}."); }
                                                else
                                                {
                                                    ActionPlanMFD mfdPlan = new ActionPlanMFD(mfd);
                                                    if (pair.Value)
                                                    { mfdPlan.pageOn = name; }
                                                    else
                                                    { mfdPlan.pageOff = name; }
                                                    actionSet.addActionPlan(mfdPlan);
                                                }
                                            }
                                        }
                                        //We should probably put the page we just configured into the MFD.
                                        mfd.addPage(name, reportable);
                                    }
                                }
                                //_debugDisplay.WriteText("      End of Pages loop\n", true);
                                if (mfd != null)
                                {
                                    if (mfd.getPageCount() == 0)
                                    {
                                        textLog.addWarning($"Surface provider '{block.CustomName}' specified " +
                                            $"the use of MFD '{mfd.programName}' but did not provide readable " +
                                            $"page configuration for that MFD.");
                                    }
                                    else
                                    {

                                        evalMFDs.Add(mfd.programName, mfd);
                                        reportable = mfd;
                                        //Now that we have a MFD, we should see if we previously
                                        //had this MFD. And what it was doing.
                                        mfd.trySetPage(_iniRead.Get("MFDs", mfd.programName).ToString());
                                    }
                                }
                                //At long last, we can commit this page to our reportable listing
                                //If we got one.
                                if (reportable != null)
                                { evalReports.Add(reportable); }
                            }
                        }
                    }

                    //Lighting Blocks might be configured as Indicators
                    //_debugDisplay.WriteText("    Light handler\n", true);
                    if (block is IMyLightingBlock)
                    {
                        //We'll hold off on setting the 'handled' flag for now, so that we can 
                        //complain with greater precision in the future.
                        //From lights, we read:
                        //IndicatorElement: The Element that this indicator group watches
                        iniValue = _iniReadWrite.Get(_tag, "IndicatorElement");
                        if (!iniValue.IsEmpty)
                        {
                            elementName = iniValue.ToString();
                            IHasElement element = null;
                            //Somewhere, there may be an element for this indicator to watch
                            if (evalTallies.ContainsKey(elementName))
                            { element = evalTallies[elementName]; }
                            else if (evalSets.ContainsKey(elementName))
                            { element = evalSets[elementName]; }
                            else if (evalTriggers.ContainsKey(elementName))
                            { element = evalTriggers[elementName]; }
                            //If we weren't able to find the element, complain.
                            else
                            {
                                textLog.addWarning($"Lighting block '{block.CustomName}' tried to reference " +
                                    $"the unconfigured element '{elementName}'.");
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
                            textLog.addWarning($"Lighting block {block.CustomName} has missing or unreadable " +
                                $"IndicatorElement.");
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
                    if (!handled)
                    {
                        textLog.addWarning($"Block '{block.CustomName}' is missing proper configuration or is a " +
                            $"block type that cannot be handled by this script.");
                    }
                }
            }
            return blocks.Count;
        }

        //Removes all declaration sections from the PB and returns what's left.
        //Assumes _iniReadWrite is loaded with a parse of the PB's config (Which will be ruined when 
        //  this method completes)
        public string stripDeclarations()
        {
            List<string> sections = new List<string>();
            string declarationPrefix = $"{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}";

            _iniReadWrite.GetSections(sections);
            foreach (string section in sections)
            {
                if (section.Contains(declarationPrefix))
                { _iniReadWrite.DeleteSection(section); }
            }

            return _iniReadWrite.ToString();
        }

        //20240426: Orphaned, but looks useful
        public string getMultilineConfig(List<string> elements, string key, int MAX_PER_LINE, StringBuilder _sb)
        {
            string outcome = "";
            int elementCounter = 0;

            _sb.Clear();
            if (elements.Count > 0)
            {
                _sb.Append($"{key} = ");
                //We need to go ahead an determine if we'll need a multi-line config.
                //We'll be a little cheeky about this and set things up so our loop check will
                //take care of most of it for us.
                if (elements.Count > MAX_PER_LINE)
                { elementCounter = MAX_PER_LINE; }
                foreach (string element in elements)
                {
                    if (elementCounter >= MAX_PER_LINE)
                    {
                        _sb.Append("\n|");
                        elementCounter = 0;
                    }
                    _sb.Append($"{element}, ");
                    elementCounter++;
                }
                outcome = _sb.ToString();
                //Trim the trailing comma and space from our config
                outcome = outcome.Remove(outcome.Length - 2);
                /*debug.WriteText($"    Wrote the following multiline config: {outcome}\n", true);*/
            }
            return outcome;
        }

        public Dictionary<string, IColorCoder> compileColors(Dictionary<string, IColorCoder> colorPalette)
        {
            //Isn't actually used for anything, this is just the color I've taken to applying to 
            //my lights, and I wanted it handy.
            colorPalette.Add("cozy", new ColorCoderMono(new Color(255, 225, 200), "cozy"));
            //Goes with everything
            colorPalette.Add("black", new ColorCoderMono(new Color(0, 0, 0), "black"));
            //We'll be using these in just a second, so we'll go ahead and put handles on them
            Color optimal = new Color(25, 225, 100);
            colorPalette.Add("green", new ColorCoderMono(optimal, "green"));
            Color normal = new Color(100, 200, 225);
            colorPalette.Add("lightblue", new ColorCoderMono(normal, "lightBlue"));
            Color caution = new Color(255, 255, 0);
            colorPalette.Add("yellow", new ColorCoderMono(caution, "yellow"));
            Color warning = new Color(255, 150, 0);
            colorPalette.Add("orange", new ColorCoderMono(warning, "orange"));
            Color critical = new Color(255, 0, 0);
            colorPalette.Add("red", new ColorCoderMono(critical, "red"));

            colorPalette.Add("lowgood", new ColorCoderLow(optimal, normal, caution, warning, critical));
            colorPalette.Add("highgood", new ColorCoderHigh(optimal, normal, caution, warning, critical));

            return colorPalette;
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
            //Cameras are handled elsewhere, because Raycast is weird.
            //Connectors
            actions.Add("ConnectorLock", b => ((IMyShipConnector)b).Connect());
            actions.Add("ConnectorUnlock", b => ((IMyShipConnector)b).Disconnect());
            //Doors
            actions.Add("DoorOpen", b => ((IMyDoor)b).OpenDoor());
            actions.Add("DoorClose", b => ((IMyDoor)b).CloseDoor());
            //GasTanks
            actions.Add("TankStockpileOn", b => ((IMyGasTank)b).Stockpile = true);
            actions.Add("TankStockpileOff", b => ((IMyGasTank)b).Stockpile = false);
            //Gyros
            //Gyro overides are set in RPM, but we can't say for sure what the max RPM of a given 
            //block may be. So instead, we use arbitrarily high numbers and let the block sort it out.
            actions.Add("GyroOverrideOn", b => ((IMyGyro)b).GyroOverride = true);
            actions.Add("GyroOverrideOff", b => ((IMyGyro)b).GyroOverride = false);
            actions.Add("GyroYawPositive", b => ((IMyGyro)b).Yaw = 9000);
            actions.Add("GyroYawStabilize", b => ((IMyGyro)b).Yaw = 0);
            actions.Add("GyroYawNegative", b => ((IMyGyro)b).Yaw = -9000);
            //Yes, I'm assigning PitchPositive to be -9000. Yes, that makes no sense. No, I don't 
            //know why it has to be this way to make it work correctly.
            actions.Add("GyroPitchPositive", b => ((IMyGyro)b).Pitch = -9000);
            actions.Add("GyroPitchStabilize", b => ((IMyGyro)b).Pitch = 0);
            actions.Add("GyroPitchNegative", b => ((IMyGyro)b).Pitch = 9000);
            actions.Add("GyroRollPositive", b => ((IMyGyro)b).Roll = 9000);
            actions.Add("GyroRollStabilize", b => ((IMyGyro)b).Roll = 0);
            actions.Add("GyroRollNegative", b => ((IMyGyro)b).Roll = -9000);
            /* SCRAP
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
            });*/
            //LandingGear
            actions.Add("GearAutoLockOn", b => ((IMyLandingGear)b).AutoLock = true);
            actions.Add("GearAutoLockOff", b => ((IMyLandingGear)b).AutoLock = false);
            actions.Add("GearLock", b => ((IMyLandingGear)b).Lock());
            actions.Add("GearUnlock", b => ((IMyLandingGear)b).Unlock());
            //Jump Drives
            actions.Add("JumpDriveRechargeOn", b => ((IMyJumpDrive)b).Recharge = true);
            actions.Add("JumpDriveRechargeOff", b => ((IMyJumpDrive)b).Recharge = false);
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
            //Sorters
            actions.Add("SorterDrainOn", b => ((IMyConveyorSorter)b).DrainAll = true);
            actions.Add("SorterDrainOff", b => ((IMyConveyorSorter)b).DrainAll = false);
            //Sound Block
            actions.Add("SoundPlay", b => ((IMySoundBlock)b).Play());
            actions.Add("SoundStop", b => ((IMySoundBlock)b).Stop());
            //Thrusters
            actions.Add("ThrusterOverrideMax", b => ((IMyThrust)b).ThrustOverridePercentage = 1);
            actions.Add("ThrusterOverrideOff", b => ((IMyThrust)b).ThrustOverridePercentage = 0);
            //Timers
            actions.Add("TimerTrigger", b => ((IMyTimerBlock)b).Trigger());
            actions.Add("TimerStart", b => ((IMyTimerBlock)b).StartCountdown());
            actions.Add("TimerStop", b => ((IMyTimerBlock)b).StopCountdown());
            //Turrets
            actions.Add("TurretTargetMeteorsOn", b => ((IMyLargeTurretBase)b).TargetMeteors = true);
            actions.Add("TurretTargetMeteorsOff", b => ((IMyLargeTurretBase)b).TargetMeteors = false);
            actions.Add("TurretTargetMissilesOn", b => ((IMyLargeTurretBase)b).TargetMissiles = true);
            actions.Add("TurretTargetMissilesOff", b => ((IMyLargeTurretBase)b).TargetMissiles = false);
            actions.Add("TurretTargetSmallGridsOn", b => ((IMyLargeTurretBase)b).TargetSmallGrids = true);
            actions.Add("TurretTargetSmallGridsOff", b => ((IMyLargeTurretBase)b).TargetSmallGrids = false);
            actions.Add("TurretTargetLargeGridsOn", b => ((IMyLargeTurretBase)b).TargetLargeGrids = true);
            actions.Add("TurretTargetLargeGridsOff", b => ((IMyLargeTurretBase)b).TargetLargeGrids = false);
            actions.Add("TurretTargetCharactersOn", b => ((IMyLargeTurretBase)b).TargetCharacters = true);
            actions.Add("TurretTargetCharactersOff", b => ((IMyLargeTurretBase)b).TargetCharacters = false);
            actions.Add("TurretTargetStationsOn", b => ((IMyLargeTurretBase)b).TargetStations = true);
            actions.Add("TurretTargetStationsOff", b => ((IMyLargeTurretBase)b).TargetStations = false);
            actions.Add("TurretTargetNeutralsOn", b => ((IMyLargeTurretBase)b).TargetNeutrals = true);
            actions.Add("TurretTargetNeutralsOff", b => ((IMyLargeTurretBase)b).TargetNeutrals = false);
            actions.Add("TurretTargetEnemiesOn", b => ((IMyLargeTurretBase)b).TargetEnemies = true);
            actions.Add("TurretTargetEnemiesOff", b => ((IMyLargeTurretBase)b).TargetEnemies = false);
            actions.Add("TurretSubsystemDefault", b => ((IMyLargeTurretBase)b).SetTargetingGroup(""));
            actions.Add("TurretSubsystemWeapons", b => ((IMyLargeTurretBase)b).SetTargetingGroup("Weapons"));
            actions.Add("TurretSubsystemPropulsion", b => ((IMyLargeTurretBase)b).SetTargetingGroup("Propulsion"));
            actions.Add("TurretSubsystemPowerSystems", b => ((IMyLargeTurretBase)b).SetTargetingGroup("PowerSystems"));
            /* SCRAP
            actions.Add("TurretPassive", b =>
            {
                //meteor missile small large char stations
                IMyLargeTurretBase turret = (IMyLargeTurretBase)b;
                turret.TargetMeteors = false;
                turret.TargetMissiles = false;
                turret.TargetSmallGrids = false;
                turret.TargetLargeGrids = false;
                turret.TargetCharacters = false;
                turret.TargetStations = false;
            });
            //TurretDefensive: Targets meteors and projectiles
            actions.Add("TurretDefensive", b =>
            {
                IMyLargeTurretBase turret = (IMyLargeTurretBase)b;
                turret.TargetMeteors = true;
                turret.TargetMissiles = true;
                turret.TargetSmallGrids = false;
                turret.TargetLargeGrids = false;
                turret.TargetCharacters = false;
                turret.TargetStations = false;
            });
            //TurretPrecision: Targets characters, small grid, meteors, projectiles
            actions.Add("TurretPrecision", b =>
            {
                IMyLargeTurretBase turret = (IMyLargeTurretBase)b;
                turret.TargetMeteors = true;
                turret.TargetMissiles = true;
                turret.TargetSmallGrids = true;
                turret.TargetLargeGrids = false;
                turret.TargetCharacters = true;
                turret.TargetStations = false;
            });
            //TurretFastTracking: Targets small grid, large grid, stations
            actions.Add("TurretFastTracking", b =>
            {
                IMyLargeTurretBase turret = (IMyLargeTurretBase)b;
                turret.TargetMeteors = false;
                turret.TargetMissiles = false;
                turret.TargetSmallGrids = true;
                turret.TargetLargeGrids = true;
                turret.TargetCharacters = false;
                turret.TargetStations = true;
            });
            //TurretAntiArmor: Targets large grids, stations
            actions.Add("TurretAntiArmor", b =>
            {
                IMyLargeTurretBase turret = (IMyLargeTurretBase)b;
                turret.TargetMeteors = false;
                turret.TargetMissiles = false;
                turret.TargetSmallGrids = false;
                turret.TargetLargeGrids = true;
                turret.TargetCharacters = false;
                turret.TargetStations = true;
            });
            //TurretArtillery: Targets stations
            actions.Add("TurretSiege", b =>
            {
                IMyLargeTurretBase turret = (IMyLargeTurretBase)b;
                turret.TargetMeteors = false;
                turret.TargetMissiles = false;
                turret.TargetSmallGrids = false;
                turret.TargetLargeGrids = false;
                turret.TargetCharacters = false;
                turret.TargetStations = true;
            });
            actions.Add("TurretSubsystemDefault", b => ((IMyLargeTurretBase)b).SetTargetingGroup(""));
            actions.Add("TurretSubsystemWeapons", b => ((IMyLargeTurretBase)b).SetTargetingGroup("Weapons"));
            actions.Add("TurretSubsystemPropulsion", b => ((IMyLargeTurretBase)b).SetTargetingGroup("Propulsion"));
            actions.Add("TurretSubsystemPowerSystems", b => ((IMyLargeTurretBase)b).SetTargetingGroup("PowerSystems"));
            actions.Add("TurretSwatOn", b =>
            {
                IMyLargeTurretBase turret = (IMyLargeTurretBase)b;
                turret.TargetSmallGrids = true;
                turret.TargetLargeGrids = false;
                turret.TargetCharacters = true;
                turret.TargetStations = false;
                turret.SetTargetingGroup("");
            });
            actions.Add("TurretSwatOff", b =>
            {
                IMyLargeTurretBase turret = (IMyLargeTurretBase)b;
                turret.TargetSmallGrids = true;
                turret.TargetLargeGrids = true;
                turret.TargetCharacters = false;
                turret.TargetStations = true;
                turret.SetTargetingGroup("Weapons");
            });*/
            //Custom turret controllers
            actions.Add("ControllerTargetMeteorsOn", b => ((IMyTurretControlBlock)b).TargetMeteors = true);
            actions.Add("ControllerTargetMeteorsOff", b => ((IMyTurretControlBlock)b).TargetMeteors = false);
            actions.Add("ControllerTargetMissilesOn", b => ((IMyTurretControlBlock)b).TargetMissiles = true);
            actions.Add("ControllerTargetMissilesOff", b => ((IMyTurretControlBlock)b).TargetMissiles = false);
            actions.Add("ControllerTargetSmallGridsOn", b => ((IMyTurretControlBlock)b).TargetSmallGrids = true);
            actions.Add("ControllerTargetSmallGridsOff", b => ((IMyTurretControlBlock)b).TargetSmallGrids = false);
            actions.Add("ControllerTargetLargeGridsOn", b => ((IMyTurretControlBlock)b).TargetLargeGrids = true);
            actions.Add("ControllerTargetLargeGridsOff", b => ((IMyTurretControlBlock)b).TargetLargeGrids = false);
            actions.Add("ControllerTargetCharactersOn", b => ((IMyTurretControlBlock)b).TargetCharacters = true);
            actions.Add("ControllerTargetCharactersOff", b => ((IMyTurretControlBlock)b).TargetCharacters = false);
            actions.Add("ControllerTargetStationsOn", b => ((IMyTurretControlBlock)b).TargetStations = true);
            actions.Add("ControllerTargetStationsOff", b => ((IMyTurretControlBlock)b).TargetStations = false);
            actions.Add("ControllerTargetNeutralsOn", b => ((IMyTurretControlBlock)b).TargetNeutrals = true);
            actions.Add("ControllerTargetNeutralsOff", b => ((IMyTurretControlBlock)b).TargetNeutrals = false);
            //For some reason, Turret Controller blocks don't have a setter for TargetEnemies. So 
            //instead, we have to use terminal actions.
            actions.Add("ControllerTargetEnemiesOn", b => b.SetValue<bool>("TargetEnemies", true));
            actions.Add("ControllerTargetEnemiesOff", b => b.SetValue<bool>("TargetEnemies", false));
            actions.Add("ControllerSubsystemDefault", b => ((IMyTurretControlBlock)b).SetTargetingGroup(""));
            actions.Add("ControllerSubsystemWeapons", b => ((IMyTurretControlBlock)b).SetTargetingGroup("Weapons"));
            actions.Add("ControllerSubsystemPropulsion", b => ((IMyTurretControlBlock)b).SetTargetingGroup("Propulsion"));
            actions.Add("ControllerSubsystemPowerSystems", b => ((IMyTurretControlBlock)b).SetTargetingGroup("PowerSystems"));
            /* SCRAP
            actions.Add("ControllerPassive", b =>
            {
                //meteor missile small large char stations
                IMyTurretControlBlock controller = (IMyTurretControlBlock)b;
                controller.TargetMeteors = false;
                controller.TargetMissiles = false;
                controller.TargetSmallGrids = false;
                controller.TargetLargeGrids = false;
                controller.TargetCharacters = false;
                controller.TargetStations = false; 
            });
            
            //ControllerDefensive: Targets meteors and projectiles
            actions.Add("ControllerDefensive", b =>
            {
                IMyTurretControlBlock controller = (IMyTurretControlBlock)b;
                controller.TargetMeteors = true;
                controller.TargetMissiles = true;
                controller.TargetSmallGrids = false;
                controller.TargetLargeGrids = false;
                controller.TargetCharacters = false;
                controller.TargetStations = false;
            });
            //ControllerPrecision: Targets characters, small grid, meteors, projectiles
            actions.Add("ControllerPrecision", b =>
            {
                IMyTurretControlBlock controller = (IMyTurretControlBlock)b;
                controller.TargetMeteors = true;
                controller.TargetMissiles = true;
                controller.TargetSmallGrids = true;
                controller.TargetLargeGrids = false;
                controller.TargetCharacters = true;
                controller.TargetStations = false;
            });
            //ControllerFastTracking: Targets small grid, large grid, stations
            actions.Add("ControllerFastTracking", b =>
            {
                IMyTurretControlBlock controller = (IMyTurretControlBlock)b;
                controller.TargetMeteors = false;
                controller.TargetMissiles = false;
                controller.TargetSmallGrids = true;
                controller.TargetLargeGrids = true;
                controller.TargetCharacters = false;
                controller.TargetStations = true;
            });
            //ControllerAntiArmor: Targets large grids, stations
            actions.Add("ControllerAntiArmor", b =>
            {
                IMyTurretControlBlock controller = (IMyTurretControlBlock)b;
                controller.TargetMeteors = false;
                controller.TargetMissiles = false;
                controller.TargetSmallGrids = false;
                controller.TargetLargeGrids = true;
                controller.TargetCharacters = false;
                controller.TargetStations = true;
            });
            //ControllerArtillery: Targets stations
            actions.Add("ControllerSiege", b =>
            {
                IMyTurretControlBlock controller = (IMyTurretControlBlock)b;
                controller.TargetMeteors = false;
                controller.TargetMissiles = false;
                controller.TargetSmallGrids = false;
                controller.TargetLargeGrids = false;
                controller.TargetCharacters = false;
                controller.TargetStations = true;
            });
            actions.Add("ControllerSubsystemDefault", b => ((IMyTurretControlBlock)b).SetTargetingGroup(""));
            actions.Add("ControllerSubsystemWeapons", b => ((IMyTurretControlBlock)b).SetTargetingGroup("Weapons"));
            actions.Add("ControllerSubsystemPropulsion", b => ((IMyTurretControlBlock)b).SetTargetingGroup("Propulsion"));
            actions.Add("ControllerSubsystemPowerSystems", b => ((IMyTurretControlBlock)b).SetTargetingGroup("PowerSystems"));
            actions.Add("ControllerSwatOn", b =>
            {
                IMyTurretControlBlock controller = (IMyTurretControlBlock)b;
                controller.TargetSmallGrids = true;
                controller.TargetLargeGrids = false;
                controller.TargetCharacters = true;
                controller.TargetStations = false;
                controller.SetTargetingGroup("");
            });
            actions.Add("ControllerSwatOff", b =>
            {
                IMyTurretControlBlock controller = (IMyTurretControlBlock)b;
                controller.TargetSmallGrids = true;
                controller.TargetLargeGrids = true;
                controller.TargetCharacters = false;
                controller.TargetStations = true;
                controller.SetTargetingGroup("Weapons");
            });
            */
            //Vents
            actions.Add("VentPressurize", b => ((IMyAirVent)b).Depressurize = false);
            actions.Add("VentDepressurize", b => ((IMyAirVent)b).Depressurize = true);
            //Warheads
            actions.Add("WarheadArm", b => ((IMyWarhead)b).IsArmed = true);
            actions.Add("WarheadDisarm", b => ((IMyWarhead)b).IsArmed = false);
            actions.Add("WarheadCountdownStart", b => ((IMyWarhead)b).StartCountdown());
            actions.Add("WarheadCountdownStop", b => ((IMyWarhead)b).StopCountdown());
            actions.Add("WarheadDetonate", b => ((IMyWarhead)b).Detonate());
            //Weapons
            actions.Add("WeaponFireOnce", b => ((IMyUserControllableGun)b).ShootOnce());
            //Wheels
            actions.Add("SuspensionHeightPositive", b => ((IMyMotorSuspension)b).Height = 9000);
            actions.Add("SuspensionHeightNegative", b => ((IMyMotorSuspension)b).Height = -9000);
            actions.Add("SuspensionHeightZero", b => ((IMyMotorSuspension)b).Height = 0);
            actions.Add("SuspensionPropulsionPositive", b => ((IMyMotorSuspension)b).PropulsionOverride = 1);
            actions.Add("SuspensionPropulsionNegative", b => ((IMyMotorSuspension)b).PropulsionOverride = -1);
            actions.Add("SuspensionPropulsionZero", b => ((IMyMotorSuspension)b).PropulsionOverride = 0);
            //MergeBlock?
            return actions;
        }

        //Returns true if a custom color has been retrieved, false otherwise.
        private bool tryGetPaletteFromConfig(Action<string> troubleLogger, LimitedMessageLog textLog,
            Dictionary<string, IColorCoder> colorPalette, Color color, IColorCoder colorCoder,
            MyIniValue iniValue, ColorCoderLow lowGood, ColorCoderHigh highGood, string targetSection,
            string targetThreshold, string defaultValue, bool logKeyGeneration, ref bool configAltered)
        {
            string targetKey = $"Color{targetThreshold}";
            //We'll go ahead and take a peak at the ini so we can make some decisions.
            iniValue = _iniReadWrite.Get(targetSection, targetKey);
            if (iniValue.IsEmpty)
            {
                //We didn't even find one of these required keys. Generate a new one.
                _iniReadWrite.Set(targetSection, targetKey, defaultValue);
                //We won't commmit the change to the PB's CustomData just yet. Instead, we'll set a
                //flag that'll let us know we need to do that back in evaluateInit
                configAltered = true;
                if (logKeyGeneration)
                {
                    textLog.addNote($"{targetKey} key was missing from block '{Me.CustomName}' and " +
                      $"has been regenerated.");
                }
                return false;
            }
            //If we did find the requested key...
            else
            {
                string targetValue = iniValue.ToString().ToLowerInvariant();
                if (targetValue == defaultValue.ToLowerInvariant())
                //The config is just the default value. We don't need to bother with anything else.
                { return false; }
                else
                //This isn't the default value, so we should probably figure out what it says.
                {
                    //Attempt to read the color
                    if (tryGetColorFromConfig(troubleLogger, colorPalette, _iniReadWrite, iniValue, Me,
                        ref color, ref colorCoder, targetSection, targetKey))
                    //We found and read the color, which should now be stored in both the color
                    //and colorCoder variables. Last step is to update lowGood and highGood
                    {
                        lowGood.tryAssignColorByName(targetThreshold, color);
                        highGood.tryAssignColorByName(targetThreshold, color);
                        return true;
                    }
                    else
                    //We hit some kind of snag. ColorFromConfig should have already logged the error,
                    //so the only thing we need to do is declare that we didn't find a color.
                    { return false; }
                }
            }
        }

        //Checks the specified ini section for the specified key. If the key is found, returns true.
        //If the key is absent, generate a new key with a defualt value and potentially log that 
        //this happened.
        private bool tryCheckForDefaultIniKey(string targetSection, string targetKey, string defaultValue,
            ref bool configAltered, bool logKeyGeneration, string blockName, LimitedMessageLog textLog)
        {
            MyIniValue iniValue;
            iniValue = _iniReadWrite.Get(targetSection, targetKey);
            if (iniValue.IsEmpty)
            {
                //We didn't even find one of these required keys. Generate a new one.
                _iniReadWrite.Set(targetSection, targetKey, defaultValue);
                //We won't commmit the change to the PB's CustomData just yet. Instead, we'll set a
                //flag that'll let us know we need to do that back in evaluateInit
                configAltered = true;
                if (logKeyGeneration)
                {
                    textLog.addNote($"'{targetKey}' key was missing from '{targetSection}' section of " +
                        $"block '{blockName}' and has been regenerated.");
                }
                return false;
            }
            return true;
        }

        private bool isElementNameInUse(HashSet<string> elementNames, string name,
            string declarationSection, LimitedMessageLog textLog)
        {
            //There are two scenarios under which a name is in use. The first is if the user is 
            //trying to use our single solitary reserved word. I mean, really user?
            if (name.ToLowerInvariant() == "blank")
            {
                textLog.addError($"{declarationSection} tried to use the Element name '{name}', " +
                    "which is reserved by the script to indicate portions of a Report that should " +
                    "be left empty. Please choose a different name.");
                return true;
            }
            //The second and more obvious scenario is if the name is already in use.
            else if (elementNames.Contains(name))
            {
                textLog.addError($"{declarationSection} tried to use the Element name '{name}', " +
                    $"which has already been claimed. All Element providers (Tally, ActionSet, " +
                    $"Trigger, Raycaster) must have their own, unique names.");
                return true;
            }
            //Barring those two cases, we're fine, and the element name is not in use.
            else
            { return false; }
        }

        /* Parses a State List in the format 'Batteries: On, Thrusters: On' etc
         * string stateList: The string containing the state list to be parsed
         * string troubleID: How the method will describe where the stateList came from in error 
         *   messages 
         * LimitedMessageLog textLog: The message log we'll be sending our errors to.
         * List<KeyValuePair> parsedData: A reference to a list of KeyValuePairs, containing a string
         *   and a boolean. This reference will contain the data this method has parsed.
        */
        public void parseStateList(string stateList, string troubleID, Action<string> troubleLogger,
            List<KeyValuePair<string, bool>> parsedData)
        {
            string target = "";
            bool state = false;
            bool badState;
            parsedData.Clear();
            string[] pairs = stateList.Split(',').Select(p => p.Trim()).ToArray();

            foreach (string pair in pairs)
            {

                badState = false;
                string[] parts = pair.Split(':').Select(p => p.Trim()).ToArray();
                //The first part of the pair is the identifier.
                target = parts[0];
                if (parts.Length < 2)
                {
                    badState = true;
                    troubleLogger($"{troubleID} does not provide a state for the component " +
                        $"'{target}'. Valid states are 'on' and 'off'.");
                }
                //The second part is the desired state, but it's in the form of on/off
                else if (parts[1].ToLowerInvariant() == "on")
                { state = true; }
                else if (parts[1].ToLowerInvariant() == "off")
                { state = false; }
                else
                {
                    badState = true;
                    troubleLogger($"{troubleID} attempts to set '{target}' to the invalid state " +
                        $"'{parts[1]}'. Valid states are 'on' and 'off'.");
                }

                if (!badState)
                { parsedData.Add(new KeyValuePair<string, bool>(target, state)); }
            }
        }

        //Reminder: MAPB is Multi-ActionPlanBlock
        //Returns: A list of ActionHandlers if the keyName is found, null if not.
        public List<Action<IMyTerminalBlock>> getActionHandlersForMAPB(MyIni iniRW, string discreteTag, string keyName,
            Dictionary<string, Action<IMyTerminalBlock>> actions, LimitedMessageLog textLog, IMyTerminalBlock block)
        {
            MyIniValue iniValue = iniRW.Get(discreteTag, keyName);
            List<Action<IMyTerminalBlock>> actionHandlers = null;
            if (!iniValue.IsEmpty)
            {
                string[] configElements = null;
                actionHandlers = new List<Action<IMyTerminalBlock>>();
                configElements = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                foreach (string actionName in configElements)
                { actionHandlers.Add(retrieveActionHandler(actionName, actions, textLog, block, discreteTag, keyName)); }
            }
            return actionHandlers;
        }

        public Action<IMyTerminalBlock> retrieveActionHandler(string actionName, Dictionary<string, Action<IMyTerminalBlock>> actions,
            LimitedMessageLog textLog, IMyTerminalBlock block, string discreteTag, string keyName)
        {
            Action<IMyTerminalBlock> retrievedAction = null;
            if (actions.ContainsKey(actionName))
            { retrievedAction = actions[actionName]; }
            //If there is no matching action, complain.
            else
            {
                textLog.addWarning($"Block '{block.CustomName}', discrete section '{discreteTag}', " +
                    $"references the unknown action '{actionName}' as its {keyName}.");
            }
            return retrievedAction;
        }

        /* Scans an iniReader containing a parse of a block's CustomData for Action<index> 
         *   configuration in the given discrete section.
         * LimitedErrorLog errors: The error log that we will report errors to.
         * string discreteTag: The name of the discrete section we'll be reading
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
        public ActionPart tryGetPartFromConfig(LimitedMessageLog textLog, string discreteTag, int index,
            IMyTerminalBlock block, MyIni iniReader, MyIniValue iniValue, Dictionary<string, IColorCoder>
            colorPalette, IColorCoder colorCoder)
        {
            //Check the config for the presence of the target key
            string propertyKey = $"Action{index}Property";
            //The troubleLogger we'll use to add errors to the textLog. 
            //Because ActionParts are retrieved from the grid, we use a warning.
            Action<string> warningLogger = b => textLog.addWarning(b);
            iniValue = iniReader.Get(discreteTag, propertyKey);
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
                    textLog.addWarning($"Block '{block.CustomName}', discrete section '{discreteTag}', " +
                        $"references the unknown property '{propertyName}' as its {propertyKey}.");
                    retreivedPart = new ActionPart<bool>(propertyName);
                }
                else if (propertyDef.TypeName.ToLowerInvariant() == "boolean")
                {
                    //The process for each type is basically the same
                    ActionPart<bool> typedPart = new ActionPart<bool>(propertyName);
                    bool typedValue = false;
                    //Check for an valueOn
                    iniValue = iniReader.Get(discreteTag, $"Action{index}ValueOn");
                    if (!iniValue.IsEmpty && iniValue.TryGetBoolean(out typedValue))
                    { typedPart.setValueOn(typedValue); }
                    //Check for an valueOff
                    iniValue = iniReader.Get(discreteTag, $"Action{index}ValueOff");
                    if (!iniValue.IsEmpty && iniValue.TryGetBoolean(out typedValue))
                    { typedPart.setValueOff(typedValue); }
                    //Pass this ActionPart out to the un-type'd variable.
                    retreivedPart = typedPart;
                }
                else if (propertyDef.TypeName.ToLowerInvariant() == "stringbuilder")
                {
                    ActionPart<StringBuilder> typedPart = new ActionPart<StringBuilder>(propertyName);
                    string typedValue = "";
                    iniValue = iniReader.Get(discreteTag, $"Action{index}ValueOn");
                    if (!iniValue.IsEmpty && iniValue.TryGetString(out typedValue))
                    {
                        StringBuilder builder = new StringBuilder(typedValue); //This hurts my heart.
                        typedPart.setValueOn(builder);
                    }
                    iniValue = iniReader.Get(discreteTag, $"Action{index}ValueOff");
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
                    iniValue = iniReader.Get(discreteTag, $"Action{index}ValueOn");
                    if (!iniValue.IsEmpty && iniValue.TryGetInt64(out typedValue))
                    { typedPart.setValueOn(typedValue); }
                    iniValue = iniReader.Get(discreteTag, $"Action{index}ValueOff");
                    if (!iniValue.IsEmpty && iniValue.TryGetInt64(out typedValue))
                    { typedPart.setValueOff(typedValue); }

                    retreivedPart = typedPart;
                }
                else if (propertyDef.TypeName.ToLowerInvariant() == "single")
                {
                    ActionPart<float> typedPart = new ActionPart<float>(propertyName);
                    float typedValue = -1;
                    iniValue = iniReader.Get(discreteTag, $"Action{index}ValueOn");
                    if (!iniValue.IsEmpty && iniValue.TryGetSingle(out typedValue))
                    { typedPart.setValueOn(typedValue); }
                    iniValue = iniReader.Get(discreteTag, $"Action{index}ValueOff");
                    if (!iniValue.IsEmpty && iniValue.TryGetSingle(out typedValue))
                    { typedPart.setValueOff(typedValue); }

                    retreivedPart = typedPart;
                }
                else if (propertyDef.TypeName.ToLowerInvariant() == "color")
                {
                    //Colors are a bit different
                    ActionPart<Color> typedPart = new ActionPart<Color>(propertyName);
                    Color typedValue = Color.White;
                    if (tryGetColorFromConfig(warningLogger, colorPalette, iniReader, iniValue, block,
                        ref typedValue, ref colorCoder, discreteTag, $"Action{index}ValueOn"))
                    { typedPart.setValueOn(typedValue); }
                    if (tryGetColorFromConfig(warningLogger, colorPalette, iniReader, iniValue, block,
                        ref typedValue, ref colorCoder, discreteTag, $"Action{index}ValueOff"))
                    { typedPart.setValueOff(typedValue); }

                    retreivedPart = typedPart;
                }
                else
                {
                    textLog.addError($"Block '{block.CustomName}', discrete section '{discreteTag}', " +
                        $"references the property '{propertyName}' which uses the non-standard " +
                        $"type {propertyDef.TypeName}. Report this to the scripter, as the script " +
                        $"will need to be altered to handle this.");
                    retreivedPart = new ActionPart<bool>(propertyName);
                }
                //The last step is to make sure that we got a value /somewhere/
                if (!retreivedPart.isHealthy() && propertyDef != null)
                {
                    textLog.addWarning($"Block '{block.CustomName}', discrete section '{discreteTag}', " +
                    $"does not specify a working Action{index}ValueOn or Action{index}ValueOff for " +
                    $"the property '{propertyName}'. If one was specified, make sure that it matches the " +
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

        private void tryGetCommandFromConfig(Trigger trigger, string declarationSection, bool isLess,
            string keyPrefix, MyIniValue iniValue, LimitedMessageLog textLog)
        {
            double value;
            //Check for lessOrEqual and greaterOrEqual scenarios
            iniValue = _iniReadWrite.Get(declarationSection, $"{keyPrefix}Value");
            if (!iniValue.IsEmpty)
            {
                value = iniValue.ToDouble();
                iniValue = _iniReadWrite.Get(declarationSection, $"{keyPrefix}Command");
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
                        textLog.addError($"{declarationSection}: {trigger.programName} specifies " +
                            $"a {keyPrefix}Command of 'switch', which cannot be used for triggers.");
                    }
                    else
                    {
                        textLog.addError($"{declarationSection}: {trigger.programName} has a missing " +
                            $"or invalid {keyPrefix}Command. Valid commands are 'on' and 'off'.");
                    }
                }
                else
                {
                    textLog.addError($"{declarationSection}: {trigger.programName} specifies a " +
                    $"{keyPrefix}Value but no {keyPrefix}Command.");
                }
            }
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
        public bool tryGetColorFromConfig(Action<string> troubleLogger, Dictionary<string, IColorCoder> colorPalette,
            MyIni iniReader, MyIniValue iniValue, IMyTerminalBlock block, ref Color color, ref IColorCoder colorCoder,
            string sectionTag, string targetKey)
        {
            //How we will describe this key's location in error messages
            string errorLocation = $"Block '{block.CustomName}', section {sectionTag}, key {targetKey}";

            iniValue = iniReader.Get(sectionTag, targetKey);
            //20230215: Yes, it seems like there should be a more effecient order this could be done 
            //in. No, I don't think there actually is. We can't use the raw string as a dictionary 
            //key because they may not be consistent across config. And we have to parse the integers
            //in order to apply the formatting to them, which we need in order to avoid the possibility
            //that a value less than 100 would result in an identier string that could collide with 
            //a different identifier string.
            if (!iniValue.IsEmpty)
            {
                //Split the data that we found on a comma delimiter.
                string[] elements = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                //If we got three comma seperated elements, we have an RGB value.
                if (elements.Length == 3)
                {
                    int counter = 0;
                    int[] values = new int[3];
                    //The RGB values of this color will serve as its key in the colorPalette dictionary
                    string elementsAsKey = "";
                    //While we haven't gotten all three colors, and if we haven't failed yet...
                    while (counter < 3)
                    {
                        //Try to parse this element
                        if (!Int32.TryParse(elements[counter], out values[counter]))
                        {
                            //If we can't parse this element, we'll log an error and call it a day
                            troubleLogger($"{errorLocation}, element {counter} could not be " +
                                $"parsed as an integer.");
                            /*textLog.addError($"{errorLocation}, element {counter} could not be " +
                                $"parsed as an integer.");*/
                            return false;
                        }
                        elementsAsKey += values[counter].ToString("D3");
                        counter++;
                    }
                    //If we reach this point, we know we have three integer values we can plug into
                    //a color.
                    //If we already have an object for this particular color...
                    if (colorPalette.TryGetValue(elementsAsKey, out colorCoder))
                    //... Asign the color variable and call it a day
                    {
                        color = colorCoder.getColorCode(-1);
                        return true;
                    }
                    //If we haven't encountered this color before...
                    else
                    //...Create new objects for it, add it to the dictionary, then call it a day.
                    {
                        color = new Color(values[0], values[1], values[2]);
                        colorCoder = new ColorCoderMono(color);
                        colorPalette.Add(elementsAsKey, colorCoder);
                        return true;
                    }
                }
                //If we didn't get three comma-seperated values, we might have a pre-defined color
                else
                {
                    //Check to see if the string held in the iniValue is a key in the colorPalette dictionary
                    if (colorPalette.TryGetValue(iniValue.ToString().ToLowerInvariant(), out colorCoder))
                    //If it is, assign the color variable and call it a day
                    {
                        color = colorCoder.getColorCode(-1);
                        return true;
                    }
                    else
                    //If it isn't, complain.
                    {
                        troubleLogger($"{errorLocation} contains unreadable color '{iniValue.ToString()}'.");
                        return false;
                    }
                }
            }
            //If we didn't find something at the designated key, fail silently
            else
            { return false; }
        }

        //Returns either a common palette color name, or the RGB value of the color in question.
        public static string getStringFromColor(Color color)
        {
            if (color == cozy)
            { return "cozy"; }
            else if (color == green)
            { return "green"; }
            else if (color == lightBlue)
            { return "lightBlue"; }
            else if (color == yellow)
            { return "yellow"; }
            else if (color == orange)
            { return "orange"; }
            else if (color == red)
            { return "red"; }
            else
            { return $"{color.R}, {color.G}, {color.B}"; }
        }

        //Refering to the distributor cap on an internal combustion engine. Hooray, metaphors.
        public class UpdateDistributor
        {
            //This value is the number of potential update tics that should be skipped between each
            //actual update. The default value is 0.
            internal int updateDelay { get; private set; }
            //The number potential update tics remaining before the next actual update 
            int delayCounter;
            //Because you can't use events in Space Engineer scripting, a reference to the EventLog
            //will need to be maintained, so we can let it know what the distributor is doing.
            EventLog log;

            public UpdateDistributor(EventLog log)
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

        //The object formerly known as LimitedErrorLog.
        public class LimitedMessageLog
        {
            //A reference to the global StringBuilder
            StringBuilder _sb;
            //The maximum number of errors this log will accept.
            int maxMessages;
            //Lists that hold the log's errors, warnings, and notes.
            List<string> errors, warnings, notes;
            //When the number of items in one of our lists exceeds maxMessages, we stop adding to 
            //that list and start incrementing a counter instead.
            int overflowErrors, overflowWarnings, overflowNotes;
            //The color codes used for the various message types
            public string errorCode { get; private set; }
            public string warningCode { get; private set; }
            public string noteCode { get; private set; }
            public Color errorColor { set { errorCode = colorToHex(value); } }
            public Color warningColor { set { warningCode = colorToHex(value); } }
            public Color noteColor { set { noteCode = colorToHex(value); } }

            public LimitedMessageLog(StringBuilder _sb, int maxMessages)
            {
                this._sb = _sb;
                this.maxMessages = maxMessages;
                overflowErrors = 0;
                overflowWarnings = 0;
                overflowNotes = 0;
                errors = new List<string>();
                warnings = new List<string>();
                notes = new List<string>();
                errorCode = argbToHex(255, 255, 0, 0);
                warningCode = argbToHex(255, 255, 255, 0);
                noteCode = argbToHex(255, 100, 200, 225);
            }

            //Add an error to the log, or increment the overflow counter if we've already got too 
            //many errors.
            public void addError(string error)
            {
                if (errors.Count < maxMessages)
                { errors.Add(error); }
                else
                { overflowErrors++; }
            }

            //Return the total number of errors encountered by this log. This can be used to 
            //determine if the script is in a working state.
            public int getErrorTotal()
            { return errors.Count + overflowErrors; }

            public void clearErrors()
            {
                overflowErrors = 0;
                errors.Clear();
            }

            //Print a formatted list of errors in this log
            public string errorsToString()
            {
                string output;
                _sb.Clear();
                foreach (string entry in errors)
                { _sb.Append($" -{entry}\n"); }
                if (overflowErrors > 0)
                { _sb.Append($" -And {overflowErrors} other errors.\n"); }
                output = _sb.ToString();
                _sb.Clear();
                return output;
            }

            public void addWarning(string warning)
            {
                if (warnings.Count < maxMessages)
                { warnings.Add(warning); }
                else
                { overflowWarnings++; }
            }

            public int getWarningTotal()
            { return warnings.Count + overflowWarnings; }

            public void clearWarnings()
            {
                overflowWarnings = 0;
                warnings.Clear();
            }

            public string warningsToString()
            {
                string output;
                _sb.Clear();
                foreach (string entry in warnings)
                { _sb.Append($" -{entry}\n"); }
                if (overflowWarnings > 0)
                { _sb.Append($" -And {overflowWarnings} other warnings.\n"); }
                output = _sb.ToString();
                _sb.Clear();
                return output;
            }

            public void addNote(string note)
            {
                if (notes.Count < maxMessages)
                { notes.Add(note); }
                else
                { overflowNotes++; }
            }

            public int getNoteTotal()
            { return notes.Count + overflowNotes; }

            public void clearNotes()
            {
                overflowNotes = 0;
                notes.Clear();
            }

            public string notesToString()
            {
                string log;
                _sb.Clear();
                foreach (string entry in notes)
                { _sb.Append($" -{entry}\n"); }
                if (overflowNotes > 0)
                { _sb.Append($" -And {overflowNotes} other notes.\n"); }
                log = _sb.ToString();
                _sb.Clear();
                return log;
            }

            public void clearAll()
            {
                clearErrors();
                clearWarnings();
                clearNotes();
            }
        }

        //This code courtesy of Stack Exchange users Ari Roth and aloisdq
        //https://stackoverflow.com/questions/2395438/convert-system-drawing-color-to-rgb-and-hex-value
        public static string argbToHex(int a, int r, int g, int b)
        { return $"{a:X2}{r:X2}{g:X2}{b:X2}"; }

        public static string colorToHex(Color color)
        { return argbToHex(color.A, color.R, color.G, color.B); }

        public static string newLineToMultiLine(string entry)
        {
            //If we're going to do a multiline, put each part on its own line.
            if (entry.Contains("\n"))
            { entry = $"\n|{entry.Replace("\n", "\n|")}"; }
            return entry;
        }

        public static string listToMultiLine(List<string> elements, int elementsPerLine = 3, bool isRawText = true)
        {
            int elementsThisLine = 0;
            string outcome = "";
            //If we're writing this output as raw text to be appended directly to a block's CustomData, 
            //we'll need to add our own multiline notation. If the output is going to be handed to
            //MyIni.Set, it'll take care of all of that for us, and we only need to worry about 
            //getting everything on the right line.
            string multilineSymbol = isRawText ? "|" : "";
            //Multiline config always starts on the line after the key, with the newLine symbol. If
            //we can already tell that we're going to have multi-line config, best start off right.
            if (elements.Count > elementsPerLine && isRawText)
            { outcome = "\n|"; }

            foreach (string element in elements)
            {
                if (elementsThisLine >= elementsPerLine)
                {
                    outcome += $"\n{multilineSymbol}";
                    elementsThisLine = 0;
                }
                outcome += $"{element}, ";
                elementsThisLine++;
            }
            //Trim the trailing comma and space.
            outcome = outcome.Remove(outcome.Length - 2);
            return outcome;
        }

        //An interface for objects that can write all of their config as key/value pairs.
        public interface IHasConfig
        { string writeConfig(int index); }

        //An interface for objects whose config is expressed as one part of a key (Usually for state lists)
        public interface IHasConfigPart
        {
            string getConfigPart();
            //TODO: Monitor (20221213). Going to try commenting this out so I can use this for 
            //ColorCoders as well (And maybe things like TallyGenerics which only have a name)
            //bool isOn();
        }

        //An interface shared by all the objects that assign colors to elements
        public interface IColorCoder : IHasConfigPart
        { Color getColorCode(double percent); }

        public abstract class ColorCoderBase : IColorCoder
        {
            //The name of this color coder in config. In this case, will be lowGood or highGood
            protected string name;

            //The colors that will be assigned to various numeric values
            public Color colorOptimal { internal get; set; }
            public Color colorNormal { internal get; set; }
            public Color colorCaution { internal get; set; }
            public Color colorWarning { internal get; set; }
            public Color colorCritical { internal get; set; }

            //The thresholds at which various colors will be assigned
            internal int thresholdOptimal;
            internal int thresholdCaution;
            internal int thresholdWarning;
            internal int thresholdCritical;

            public ColorCoderBase(Color optimal, Color normal, Color caution, Color warning, Color critical)
            {
                colorOptimal = optimal;
                colorNormal = normal;
                colorCaution = caution;
                colorWarning = warning;
                colorCritical = critical;
            }

            //This minimalist construvtor is intended for use in AutoPopulate. It is essentially an 
            //empty object. IF YOU TRY TO USE THIS OUTSIDE OF AUTOPOPULATE, BAD THINGS WILL HAPPEN
            public ColorCoderBase()
            { }

            //Assign one of this ColorCoder's colors by using the variable name
            //The name is case sensitive. 
            internal bool tryAssignColorByName(string targetThreshold, Color color)
            {
                switch (targetThreshold)
                {
                    case "Optimal":
                        colorOptimal = color;
                        break;
                    case "Normal":
                        colorNormal = color;
                        break;
                    case "Caution":
                        colorCaution = color;
                        break;
                    case "Warning":
                        colorWarning = color;
                        break;
                    case "Critical":
                        colorCritical = color;
                        break;
                    default:
                        //The user can't get at this function, so we shouldn't ever have bad input
                        //But just in case...
                        return false;
                }
                return true;
            }

            public string getConfigPart()
            { return name; }

            public abstract Color getColorCode(double percent);
        }

        public class ColorCoderLow : ColorCoderBase
        {
            public ColorCoderLow(Color optimal, Color normal, Color caution, Color warning, Color critical) :
                base(optimal, normal, caution, warning, critical)
            {
                name = "LowGood";
                thresholdOptimal = 0;
                thresholdCaution = 55;
                thresholdWarning = 70;
                thresholdCritical = 85;
            }

            //This minimalist construvtor is intended for use in AutoPopulate. It is essentially an 
            //empty object. IF YOU TRY TO USE THIS OUTSIDE OF AUTOPOPULATE, BAD THINGS WILL HAPPEN
            public ColorCoderLow() : base()
            //We do still need a name.
            { name = "LowGood"; }

            public override Color getColorCode(double percent)
            {
                //Default to the default.
                Color code = colorNormal;

                //Default: Green at 0
                if (percent <= thresholdOptimal)
                { code = colorOptimal; }
                //Default: Red when greater than 85
                else if (percent > thresholdCritical)
                { code = colorCritical; }
                //Default: Orange when greather than 70
                else if (percent > thresholdWarning)
                { code = colorWarning; }
                //Default: Yellow when greater than 55
                else if (percent > thresholdCaution)
                { code = colorCaution; }
                return code;
            }
        }

        public class ColorCoderHigh : ColorCoderBase
        {
            public ColorCoderHigh(Color optimal, Color normal, Color caution, Color warning, Color critical) :
                base(optimal, normal, caution, warning, critical)
            {
                name = "HighGood";
                thresholdOptimal = 100;
                thresholdCaution = 45;
                thresholdWarning = 30;
                thresholdCritical = 15;
            }

            //This minimalist construvtor is intended for use in AutoPopulate. It is essentially an 
            //empty object. IF YOU TRY TO USE THIS OUTSIDE OF AUTOPOPULATE, BAD THINGS WILL HAPPEN
            public ColorCoderHigh() : base()
            { name = "HighGood"; }

            public override Color getColorCode(double percent)
            {
                //Default to the default.
                Color code = colorNormal;

                //Default: Green at 100
                if (percent >= thresholdOptimal)
                { code = colorOptimal; }
                //Default: Red when less than 15
                else if (percent < thresholdCritical)
                { code = colorCritical; }
                //Default: Orange when less than 30
                else if (percent < thresholdWarning)
                { code = colorWarning; }
                //Default: Yellow when less than 45
                else if (percent < thresholdCaution)
                { code = colorCaution; }
                return code;
            }
        }

        public class ColorCoderMono : IColorCoder
        {
            //The name of this color coder in config. This may be one of the palette color names, or
            //an RGB value.
            string name;
            //The color this coder will return, regardless of what value this coder is sent.
            public Color colorOverride { private get; set; }

            //Paramater-less constructor for... Well, actually, we didn't end up needing this. So it's
            //for... posterity?
            public ColorCoderMono()
            { }

            //If we actually know what color we want going into this (Like when we're setting up the
            //default palette), we can use this constructor
            public ColorCoderMono(Color colorOverride, string name)
            {
                this.name = name;
                this.colorOverride = colorOverride;
            }

            //For un-named colors (Basically anything we get from config), we use this constructor.
            //It derives the RGB combination from the color and holds on to it, ready to supply it
            //to a passing writeConfig method.
            public ColorCoderMono(Color colorOverride)
            {
                this.colorOverride = colorOverride;
                name = $"{colorOverride.R}, {colorOverride.G}, {colorOverride.B}";
            }

            public Color getColorCode(double percent)
            { return colorOverride; }

            public string getConfigPart()
            { return name; }
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

        //NOTE 20231205: Added IHasConfig interface for use with AutoPopulate and APTemplates, specifically.
        //  Adjusted IHasConfig to be public instead of internal to comply.
        public abstract class Tally : IHasElement, IHasConfig
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
            internal bool maxForced;
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
            protected MeterMaid meterMaid;
            //Stores the last meter received by this tally.
            internal string meter;
            //A color code for this tally, based on the percentage.
            public Color statusColor { get; protected set; }
            //The object that will figure out what color to associate with the current value of 
            //this tally. ColorCoderHigh and ColorCoderLow are the most common, but an overriding
            //ColorCoderMono can also be used.
            internal IColorCoder colorCoder { get; set; }

            public Tally(MeterMaid meterMaid, string name, IColorCoder colorCoder, double multiplier = 1)
            {
                this.meterMaid = meterMaid;
                programName = name;
                displayName = name;
                this.colorCoder = colorCoder;
                this.multiplier = multiplier;
                curr = 0;
                max = 0;
                maxForced = false;
                doNotReconstitute = false;
                percent = 0;
                readableCurr = "curr";
                readableMax = "max";
                meter = "[----------]";
                statusColor = cozy;
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

            internal string writeCommonConfig(string childConfig, int index)
            {
                double DEFAULT_MULTIPLIER = 1;

                //First up is the section header
                string config = $"[{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.Tally{index.ToString("D2")}]\n";

                //Next comes the name and the displayName
                config += $"Name = {newLineToMultiLine(programName)}\n";
                if (displayName != programName)
                { config += $"DisplayName = {newLineToMultiLine(displayName)}\n"; }

                //We'll slide the child config in here, so that things like the type and all the 
                //TallyItem stuff will be up close to the top.
                config += childConfig;

                //The last bits of common config are the Max and Multiplier.
                if (maxForced)
                //Because the multiplier has been applied directly to the max, to get the original
                //value, we need to do some math.
                { config += $"Max = {max / multiplier}\n"; }
                if (multiplier != DEFAULT_MULTIPLIER)
                { config += $"Multiplier = {multiplier}\n"; }
                //This is the end of this section. Add a new line before we go to the next.
                config += "\n";

                return config;
            }

            //writeConfig generates an ini config for this tally based on the information that was
            //passed into it at object creation.
            public abstract string writeConfig(int index);
        }

        public class TallyGeneric : Tally
        {
            //TallyGenerics now store their information in a bespoke object that also has methods
            //for dealing with that block type.
            internal ITallyGenericHandler handler;

            //TallyGeneric is quite similar to the regular Tally externally, only requiring a 
            //handler to be passed in alongside the name. We need to do a bit of work internally,
            //however.
            public TallyGeneric(MeterMaid meterMaid, string name, ITallyGenericHandler handler,
                IColorCoder colorCoder, double multiplier = 1) : base(meterMaid, name, colorCoder, multiplier)
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
                readableInt(ref readableMax, max);
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
                    statusColor = colorCoder.getColorCode(percent);
                    //Now, the meter.
                    meterMaid.getMeter(ref meter, percent);
                    //Last, we want to show curr as something you can actually read.
                    readableInt(ref readableCurr, curr);
                }
            }

            public override string writeConfig(int index)
            {
                //name type max? displayName multiplier lowgood
                //TallyGenerics have very little specific config, and getting it is straightforward.
                string config = $"Type = {handler.writeConfig()}\n";
                //We expect generics to use ColorCoderHigh. If it doesn't, we need to make a note.
                if (!(colorCoder is ColorCoderHigh))
                { config += $"ColorCoder = {colorCoder.getConfigPart()}\n"; }
                //Certain handlers may have their own config. Tack that on.
                //If the handler has its own config, it should also provide a newline at the end of
                //that config. So we don't need to worry about that.
                config += handler.getHandlerConfig();

                //Everything else is handled in the general config method.
                return writeCommonConfig(config, index);
            }
        }

        public class TallyCargo : Tally
        {
            //The only change to the constructor that TallyCargo needs is setting the default of 
            //isLow to 'true'
            public TallyCargo(MeterMaid meterMaid, string name, IColorCoder colorCoder, double multiplier = 1)
                : base(meterMaid, name, colorCoder, multiplier)
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
                readableInt(ref readableMax, max);
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
                    statusColor = colorCoder.getColorCode(percent);
                    //Now, the meter.
                    meterMaid.getMeter(ref meter, percent);
                    //Last, we want to show curr as something you can actually read.
                    readableInt(ref readableCurr, curr);
                }
            }

            public override string writeConfig(int index)
            {
                //name type max? displayName multiplier lowgood

                //TallyCargos are almost easier than TallyGenerics
                string config = "Type = Inventory\n";
                //TallyCargos generally use ColorCoderLow.
                if (!(colorCoder is ColorCoderLow))
                { config += $"ColorCoder = {colorCoder.getConfigPart()}\n"; }

                //Everything else is handled in the general config method.
                return writeCommonConfig(config, index);
            }
        }

        public class TallyItem : TallyCargo
        {
            //The item type that this tally will look for in inventories.
            internal MyItemType itemType { get; private set; }

            //TallyItems need a bit more data, so they'll know what kind of item they're looking
            //for. You can also set the max from the constructor, though I've stopped doing that.
            public TallyItem(MeterMaid meterMaid, string name, string typeID, string subTypeID, IColorCoder colorCoder,
                double max = 0, double multiplier = 1) : base(meterMaid, name, colorCoder, multiplier)
            {
                itemType = new MyItemType(typeID, subTypeID);
                forceMax(max);
            }

            //In AutoPopulate, I generate the MyItemType object before I generate a tally. May as well
            //allow that to be passed in whole cloth.
            public TallyItem(MeterMaid meterMaid, string name, MyItemType itemType, IColorCoder colorCoder,
                double max = 0, double multiplier = 1) : base(meterMaid, name, colorCoder, multiplier)
            {
                this.itemType = itemType;
                forceMax(max);
            }

            //Take an inventory and see how much of the itemType is in it.
            internal override void addInventoryToCurr(IMyInventory inventory)
            { curr += (double)inventory.GetItemAmount(itemType); }

            public override string writeConfig(int index)
            {
                //name type max? displayName multiplier lowgood

                //TallyItems are a bit more complicated than the other two Tally types.
                string config = $"Type = Item\n";
                //Every TallyItem needs a TypeID and SubTypeID
                config += $"ItemTypeID = {itemType.TypeId}\n";
                config += $"ItemSubTypeID = {itemType.SubtypeId}\n";
                //Like TallyGenerics, we expect TallyItems to use ColorCoderHigh
                if (!(colorCoder is ColorCoderHigh))
                { config += $"ColorCoder = {colorCoder.getConfigPart()}\n"; }

                //Everything else is handled in the general config method.
                return writeCommonConfig(config, index);
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

        //Abbreviate large numbers with K, M, B, or T to keep them legible.
        //The name would lead you to believe this takes ints, but it now takes doubles.
        //The string it outputs is basically an integer, though. Sorta. Just ignore the fact that 
        //it has a decimal now. Slight Precision Int?
        //string readable: The reference to the string that will hold the output.
        //double num: The number that should be rendered into a more readable form.
        public static void readableInt(ref string readable, double num)
        {
            readable = "";
            //If the number is less than ten, show the first decimal point.
            if (num < 10)
            { readable += (Math.Round(num, 1)); }
            //If the number is between ten and one thousand, trim the decimals but otherwise 
            //do nothing.
            else if (num < 1000)
            { readable += (int)num; }

            //If the number is between one thousand and ten thousand, replace the last three 
            //digits with a K and one decimal
            else if (num < 10000)
            { readable = Math.Round(num / 1000, 1) + "K"; }
            //If the number is between ten thousand and one million, replace the last three
            //digits with a K
            else if (num < 1000000)
            { readable = (int)(num / 1000) + "K"; }

            //If the number is between one million and ten million, replace the last six digits
            //with an M and one decimal
            else if (num < 10000000)
            { readable = Math.Round(num / 1000000, 1) + "M"; }
            //If the number is between ten million and one billion, replace the last six digits 
            //with an M
            else if (num < 1000000000)
            { readable = (int)(num / 1000000) + "M"; }

            //If the number is between one billion and ten billion, replace the last nine digits
            //with a B and one decimal
            else if (num < 10000000000)
            { readable = Math.Round(num / 1000000000, 1) + "B"; }
            //If the number is between ten billion and one trillion, replace the last six digits 
            //with a B
            else if (num < 1000000000000)
            { readable = (int)(num / 10000000000) + "B"; }

            //If the number is between one trillion and ten trillion, replace the last twelve 
            //digits with a T and one decimal
            else if (num < 10000000000000)
            { readable = Math.Round(num / 1000000000000, 1) + "T"; }
            //If the number is greater than ten trillion, replace the last twelve digits with a T
            else if (num > 10000000000000)
            { readable = (int)(num / 1000000000000) + "T"; }

            //If the number is larger than a quadrillion... Look, I'm not putting a Q on here, 
            //that'd be silly. You're on your own.
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
                subjectBlock = subject;
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

        //Stores a series of actions for ActionSet on and off states, and the block to apply them to.
        public class MultiActionPlanBlock : IHasActionPlan
        {
            //The TerminalBlock this ActionPlan will be manipulating
            IMyTerminalBlock subjectBlock;
            //The actions to be performed on the subject block when the ActionPlan is switched on
            internal List<Action<IMyTerminalBlock>> actionsOn { get; set; }
            //The actions to be performed on the subject block when the ActionPlan is switched off
            internal List<Action<IMyTerminalBlock>> actionsOff { get; set; }

            public MultiActionPlanBlock(IMyTerminalBlock subject)
            {
                subjectBlock = subject;
                actionsOn = null;
                actionsOff = null;
            }

            //Execute the actions associated with this ActionPlan on the subject block. The set that 
            //will be used depends on the boolean being passed in: true causes the actionOn, false 
            //causes the actionOff.
            public void takeAction(bool isOnAction)
            {
                List<Action<IMyTerminalBlock>> appropriateActions;

                if (isOnAction)
                { appropriateActions = actionsOn; }
                else
                { appropriateActions = actionsOff; }

                if (appropriateActions != null)
                {
                    foreach (Action<IMyTerminalBlock> action in appropriateActions)
                    { action.Invoke(subjectBlock); }
                }
            }

            //Determine if this ActionPlan has any actions defined
            public bool hasAction()
            { return actionsOn?.Count > 0 || actionsOff?.Count > 0; }

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

        //Stores and manipulates a Raycaster for an ActionSet
        public class ActionPlanRaycaster : IHasActionPlan, IHasConfigPart
        {
            //The Raycaster this ActionPlan will use for its scans
            Raycaster subjectRaycaster;
            //Will a scan be performed when this ActionPlan is switched on?
            internal bool scanOn { private get; set; }
            //Will a scan be performed when this ActionPlan is switched off?
            internal bool scanOff { private get; set; }

            public ActionPlanRaycaster(Raycaster subject)
            {
                subjectRaycaster = subject;
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
            { return $"Raycaster '{subjectRaycaster.programName}'"; }

            public string getConfigPart()
            { return $"{subjectRaycaster.programName}: {(scanOn ? "on" : "off")}"; }
        }

        //Stores a reference to an ActionSet and the information needed to manipulate it on behalf of a different ActionSet.
        public class ActionPlanActionSet : IHasActionPlan, IHasConfigPart
        {
            //There's a lot of booleans floating around in this object. So we'll replace some of 
            //them with consts in the hope of making the code more readable.
            public const bool ON_STATE = true;
            public const bool OFF_STATE = false;
            //The ActionSet this ActionPlan is manipulating
            internal ActionSet subjectSet;
            //The booleans that store what action should be taken when this plan is invoked. A value
            //of 'true' means the linked ActionSet will be set to 'on; a value of 'false' will set
            //it to off.
            internal bool reactionToOn, reactionToOff;
            //Booleans that indicate if an On or Off action have actually been configured for this 
            //plan.
            internal bool hasOn, hasOff;

            public ActionPlanActionSet(ActionSet subject)
            {
                subjectSet = subject;
                reactionToOn = OFF_STATE;
                reactionToOff = OFF_STATE;
                hasOn = false;
                hasOff = false;
            }

            public void setReactionToOn(bool action)
            {
                reactionToOn = action;
                hasOn = true;
            }

            public void setReactionToOff(bool action)
            {
                reactionToOff = action;
                hasOff = true;
            }

            //Invokes the configured action.
            public void takeAction(bool isOnAction)
            {
                try
                {
                    //If this action to be taken when the parent set is turned on
                    if (isOnAction)
                    {
                        //If an 'on' action is defined...
                        if (hasOn)
                        {
                            //So long as this set hasn't already acted, change its state.
                            if (!subjectSet.hasActed)
                            { subjectSet.setState(reactionToOn); }
                            else
                            {
                                Exception e = new InvalidOperationException();
                                e.Data.Add("Counter", 0);
                                throw e;
                            }
                        }
                    }
                    //If this action to be taken when the parent set is turned off
                    else
                    {
                        //If an 'off' action is defined...
                        if (hasOff)
                        {
                            if (!subjectSet.hasActed)
                            { subjectSet.setState(reactionToOff); }
                            else
                            {
                                Exception e = new InvalidOperationException();
                                e.Data.Add("Counter", 0);
                                throw e;
                            }
                        }
                    }
                }
                catch (InvalidOperationException e)
                {
                    int counter = (int)e.Data["Counter"];
                    e.Data.Add(counter, subjectSet.programName);
                    e.Data["Counter"] = ++counter;
                    subjectSet.setFault();
                    throw;
                }
            }

            //Determine if this ActionPlan has any actions defined
            public bool hasAction()
            { return hasOn || hasOff; }

            //Get an identifier to make our error messages more helpful
            public string getIdentifier()
            { return $"Controller for ActionSet {subjectSet.programName}"; }

            //An element of a state list that describes this ActionPlan
            public string getConfigPart()
            {
                if (isOn())
                { return $"{subjectSet.programName}: {(reactionToOn ? "on" : "off")}"; }
                else
                { return $"{subjectSet.programName}: {(reactionToOff ? "on" : "off")}"; }
            }

            //Though this object could store both an on action and an off action, it will only be
            //set up to store one. And this tells you which one.
            public bool isOn()
            { return hasOn; }
        }

        //Stores a trigger and the information needed to manipulate it on behalf of an ActionSet.
        public class ActionPlanTrigger : IHasActionPlan, IHasConfigPart
        {
            //There's a lot of booleans floating around in this object. So we'll replace some of 
            //them with consts in the hope of making the code more readable.
            /* It was a good idea, but given that the only time I set these states directly is in
             * the constructor...
            public const bool ARM = true;
            public const bool DISARM = false;
            */
            //The Trigger this ActionPlan is manipulating
            internal Trigger subjectTrigger;
            //The booleans that store what action should be taken when this plan is invoked. A value
            //of 'true' means the linked Trigger will be enabled; a value of 'false' will disable it.
            internal bool reactionToOn, reactionToOff;
            //Booleans that indicate if an On or Off action have actually been configured for this 
            //plan.
            internal bool hasOn, hasOff;

            public ActionPlanTrigger(Trigger subject)
            {
                this.subjectTrigger = subject;
                reactionToOn = false;
                reactionToOff = false;
                hasOn = false;
                hasOff = false;
            }

            public void setReactionToOn(bool action)
            {
                reactionToOn = action;
                hasOn = true;
            }

            public void setReactionToOff(bool action)
            {
                reactionToOff = action;
                hasOff = true;
            }

            //Enable or disable the linked trigger, based on the configuration.
            public void takeAction(bool isOnAction)
            {
                if (isOnAction)
                {
                    //If an 'on' action is defined...
                    if (hasOn)
                    { subjectTrigger.setEnabled(reactionToOn); }
                }
                else
                {
                    if (hasOff)
                    { subjectTrigger.setEnabled(reactionToOff); }
                }
            }

            //Determine if this ActionPlan has any actions defined
            public bool hasAction()
            { return hasOn || hasOff; }

            //Get an identifier to make our error messages more helpful
            public string getIdentifier()
            { return $"Controller for Trigger {subjectTrigger.programName}"; }

            //An element of a state list that describes this ActionPlan
            public string getConfigPart()
            {
                if (isOn())
                { return $"{subjectTrigger.programName}: {(reactionToOn ? "on" : "off")}"; }
                else
                { return $"{subjectTrigger.programName}: {(reactionToOff ? "on" : "off")}"; }
            }

            //Though this object could store both an on action and an off action, it will only be
            //set up to store one. And this tells you which one.
            public bool isOn()
            { return hasOn; }
        }

        //Stores two possible updateDelays for the UpdateDistributor.
        public class ActionPlanUpdate : IHasActionPlan, IHasConfig
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

            public string writeConfig(int index)
            {
                string config = "";
                int DEFAULT_DELAY_ON = 0;
                int DEFAULT_DELAY_OFF = 0;
                if (delayOn != DEFAULT_DELAY_ON)
                { config += $"DelayOn = {delayOn}\n"; }
                if (delayOff != DEFAULT_DELAY_OFF)
                { config += $"DelayOff = {delayOff}\n"; }
                return config;
            }
        }

        //Stores strings that will be sent by the IGC when this ActionPlan changes state.
        public class ActionPlanIGC : IHasActionPlan, IHasConfig
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

            public string writeConfig(int index)
            {
                string config = "";
                if (channel != "")
                { config += $"IGCChannel = {channel}\n"; }
                if (messageOn != "")
                { config += $"IGCMessageOn = {messageOn}\n"; }
                if (messageOff != "")
                { config += $"IGCMessageOff = {messageOff}\n"; }
                return config;
            }
        }

        //NOTE 20231205: Added IHasConfig interface for use with AutoPopulate and APTemplates, specifically.
        public class ActionSet : IHasElement, IHasConfig
        {
            //The list of ActionPlan objects that makes up this ActionSet
            List<IHasActionPlan> actionPlans;
            //The name displayed by this ActionSet in its Element
            internal string displayName { get; set; }
            //The true name of this ActionSet, stored primarily to be used in writeConfig.
            internal string programName { get; private set; }
            //The state of the ActionSets, which is used to determine how it will be displayed and
            //what set of actions it will take next.
            internal bool isOn { get; private set; }
            //Has this set taken an action this tic? Used for loop prevention with ActionPlan: ActionSet
            internal bool hasActed { get; private set; }
            //Colors used to represent each possible state of the ActionSet
            internal Color colorOn { private get; set; }
            internal Color colorOff { private get; set; }
            //The color that represents the current state of the ActionSet
            public Color statusColor { get; private set; }
            //Strings used to represent each possible state of the ActionSet
            internal string textOn { private get; set; }
            internal string textOff { private get; set; }
            //The string that represents the current state of the ActionSet
            public string statusText { get; private set; }

            public ActionSet(string name, bool initialState)
            {
                actionPlans = new List<IHasActionPlan>();
                displayName = name;
                programName = name;
                isOn = initialState;
                hasActed = false;
                //Again, I can't have default values for colors passed in through the constructor,
                //so I'm just setting them here.
                colorOn = green;
                colorOff = red;
                textOn = "Enabled";
                textOff = "Disabled";
                evaluateStatus();
            }

            //Choose a statusColor and a statusText, based on the current value of the 'state' variable.
            internal void evaluateStatus()
            {
                if (isOn)
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
            { actionPlans.Add(plan); }

            //Switch this ActionSet to its alternate state. The heavy lifting is done by the 
            //setState method.
            public void switchState()
            { setState(!isOn); }

            //Set this ActionSet to a given state, performing all associated actions and updating 
            //statusColor and statusText.
            public void setState(bool newState)
            {
                isOn = newState;
                //We need to set this now, so that the loop protection will know if it's in a loop
                hasActed = true;
                //Likewise, we need to set the sunny day display information now so that loop 
                //protection will be able to set the fault colors.
                evaluateStatus();
                foreach (IHasActionPlan plan in actionPlans)
                {
                    try
                    { plan.takeAction(newState); }
                    //Catches exceptions resulting from ActionSets calling themselves in an infinite
                    //loop. The heavy lifting is done in ActionPlan: ActionSet.
                    catch (InvalidOperationException)
                    { throw; }
                    //This try block is intended to catch exceptions caused by using an action 
                    //handler on a block that doesn't match the type of that handler. And everything
                    //else.
                    catch (Exception e)
                    {
                        //Because ActionSets can call other ActionSets, we may be dealing with a 
                        //fault that ocurred several objects downstream. If there's already an 
                        //identifier, we should leave it alone.
                        if (!e.Data.Contains("Identifier"))
                        { e.Data.Add("Identifier", plan.getIdentifier()); }
                        throw;
                    }
                }
            }

            public void resetHasActed()
            { hasActed = false; }

            public void setFault()
            {
                statusText = "Fault";
                statusColor = new Color(125, 125, 125);
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

            public string writeConfig(int index)
            {
                //Default values for this config. 
                Color DEFAULT_COLOR_ON = green;
                Color DEFAULT_COLOR_OFF = red;
                string DEFAULT_TEXT_ON = "Enabled";
                string DEFAULT_TEXT_OFF = "Disabled";
                //The string that will hold our finished config.
                string config = $"[{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.ActionSet{index.ToString("D2")}]\n";
                config += $"Name = {newLineToMultiLine(programName)}\n";
                //If we aren't using a default, we'll need to write some config.
                if (displayName != programName)
                { config += $"DisplayName = {newLineToMultiLine(displayName)}\n"; }
                if (colorOn != DEFAULT_COLOR_ON)
                { config += $"ColorOn = {getStringFromColor(colorOn)}\n"; }
                if (colorOff != DEFAULT_COLOR_OFF)
                { config += $"ColorOff = {getStringFromColor(colorOff)}\n"; }
                if (textOn != DEFAULT_TEXT_ON)
                { config += $"TextOn = {newLineToMultiLine(textOn)}\n"; }
                if (textOff != DEFAULT_TEXT_OFF)
                { config += $"TextOff = {newLineToMultiLine(textOff)}\n"; }

                //The config for an ActionSet can actually contain config for up to five types of
                //ActionPlans. They should all be at the front of the plan list, usually in this order:
                //ActionSet, Trigger, Raycaster, Update, IGC
                int planCounter = 0;
                IHasActionPlan currPlan = null;
                IHasConfigPart configPartPlan = null;
                List<string> setPlansOn = null;
                List<string> setPlansOff = null;
                List<string> triggerPlansOn = null;
                List<string> triggerPlansOff = null;
                List<string> raycasterPlans = null;
                while (planCounter != -1)
                {
                    //Before we run any checks, we need to make sure we haven't reached the end of
                    //the list. This can occur when the ActionSet has no ActionPlans for blocks on
                    //the grid.
                    if (planCounter >= actionPlans.Count)
                    { planCounter = -1; }
                    else
                    {
                        currPlan = actionPlans[planCounter];
                        //Update and IGC plans implement the IHasConfig interface
                        if (currPlan is IHasConfig)
                        //Because IHasConfig writes the entirety of the nessecary config, we just tack 
                        //these directly onto our config string.
                        //This covers ActionPlanUpdate and ActionPlanIGC.
                        {
                            config += $"{((IHasConfig)currPlan).writeConfig(index)}";
                            planCounter++;
                        }
                        //ActionSet, Trigger, and Raycaster plans provide config parts, and will 
                        //require a bit more work on this end.
                        else if (currPlan is IHasConfigPart)
                        {
                            configPartPlan = (IHasConfigPart)currPlan;
                            if (configPartPlan is ActionPlanTrigger)
                            {
                                if (triggerPlansOn == null)
                                {
                                    triggerPlansOn = new List<String>();
                                    triggerPlansOff = new List<String>();
                                }
                                if (((ActionPlanTrigger)configPartPlan).isOn())
                                { triggerPlansOn.Add(configPartPlan.getConfigPart()); }
                                else
                                { triggerPlansOff.Add(configPartPlan.getConfigPart()); }
                            }
                            //AP:AS is handled much like Triggers
                            else if (configPartPlan is ActionPlanActionSet)
                            {
                                if (setPlansOn == null)
                                {
                                    setPlansOn = new List<String>();
                                    setPlansOff = new List<String>();
                                }
                                if (((ActionPlanActionSet)configPartPlan).isOn())
                                { setPlansOn.Add(configPartPlan.getConfigPart()); }
                                else
                                { setPlansOff.Add(configPartPlan.getConfigPart()); }
                            }
                            //If it isn't a trigger plan or an ActionSet plan, it's an Raycaster 
                            //plan. Or I've changed something fundemental and forgotten to update 
                            //this code. 
                            else
                            {
                                if (raycasterPlans == null)
                                { raycasterPlans = new List<String>(); }
                                raycasterPlans.Add(((ActionPlanRaycaster)configPartPlan).getConfigPart());
                            }
                            planCounter++;
                        }
                        //If it doesn't implement IHasConfig or IHasConfigPart, it's a grid plan,
                        //and we'll put it in the corner and deal with it later.
                        else
                        { planCounter = -1; }
                    }
                }
                //We should've compiled config for all the PB plans at this point. But we still need
                //to see if we're going to be tacking on config for Triggers or ActionSets
                /*if (setPlansOn != null && setPlansOn.Count > 0)*/
                if (setPlansOn?.Count > 0)
                { config += $"ActionSetsLinkedToOn = {listToMultiLine(setPlansOn)}\n"; }
                /*if (setPlansOff != null && setPlansOff.Count > 0)*/
                if (setPlansOff?.Count > 0)
                { config += $"ActionSetsLinkedToOff = {listToMultiLine(setPlansOff)}\n"; }
                /*if (triggerPlansOn != null && triggerPlansOn.Count > 0)*/
                if (triggerPlansOn?.Count > 0)
                { config += $"TriggerLinkedToOn = {listToMultiLine(triggerPlansOn)}\n"; }
                /*if (triggerPlansOff != null && triggerPlansOff.Count > 0)*/
                if (triggerPlansOff?.Count > 0)
                { config += $"TriggerLinkedToOff = {listToMultiLine(triggerPlansOff)}\n"; }
                if (raycasterPlans?.Count > 0)
                { config += $"RaycastPerformedOnState = {listToMultiLine(raycasterPlans)}\n"; }

                //One last newline so we aren't running into the next declaration
                config += "\n";

                return config;
            }
        }

        public class Trigger : IHasElement, IHasConfig
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
            internal bool enabled { get; private set; }
            public string programName { get; private set; }
            string textOn, textOff;
            public string statusText { get; private set; }
            Color colorOn, colorOff;
            public Color statusColor { get; private set; }

            public Trigger(string programName, Tally targetTally, ActionSet targetSet, bool initialState)
            {
                this.programName = programName;
                this.targetTally = targetTally;
                this.targetSet = targetSet;
                greaterOrEqual = -1;
                lessOrEqual = -1;
                commandGreater = false;
                commandLess = false;
                hasGreater = false;
                hasLess = false;
                enabled = initialState;
                textOn = "Armed";
                textOff = "Disarmed";
                colorOn = yellow;
                colorOff = red;
                evaluateStatus();
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

            public void setEnabled(bool newState)
            {
                enabled = newState;
                evaluateStatus();
            }

            //Chcks to see if one of the configured thresholds has been reached
            //  returns: If the trigger wants an action to be taken, the returned Key/Value pair
            //    will contain a reference to the Trigger's linked ActionSet and the state it should
            //    be set to. If no action is required, the ActionSet will be null.
            public bool check(out ActionSet linkedSet, out bool desiredState)
            {
                //If the trigger is currently enabled...
                linkedSet = null;
                desiredState = false;
                if (enabled)
                {
                    //If our Greater command is configured, our set isn't already in the Greater 
                    //state, and we have exceeded our threshold...
                    if (hasGreater && targetSet.isOn != commandGreater && targetTally.percent >= greaterOrEqual)
                    {
                        linkedSet = targetSet;
                        desiredState = commandGreater;
                        return true;
                    }
                    else if (hasLess && targetSet.isOn != commandLess && targetTally.percent <= lessOrEqual)
                    {
                        linkedSet = targetSet;
                        desiredState = commandLess;
                        return true;
                    }
                }
                return false;
            }

            //Operate the ActionSet that this Trigger is tied to.
            private void tryTakeAction(bool command)
            { targetSet.setState(command); }

            //Choose a statusColor and a statusText, based on the current value of the 'state' variable.
            private void evaluateStatus()
            {
                if (enabled)
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

            //Determines if this trigger has at least one scenario configured.
            public bool hasScenario()
            { return hasGreater || hasLess; }

            //Returns a string identifier for use in error messages. Right now, this returns the 
            //name of the linked Tally and ActionSet. But if I end up implementing programNames for
            //these, I could just use that instead.
            public string getIdentifier()
            { return programName; }

            //A vertical arrangement of the element representing this Trigger's current state
            public string assembleElementStack()
            { return $"{programName}\n{(enabled ? textOn : textOff)}"; }

            //A horizontal arrangement of the element representing this ActionSet's current state
            public string assembleElementLine()
            //The logic for these numbers is over in the equivilant method in ActionSet
            { return $"{programName,-19} {(enabled ? textOn : textOff),18}"; }

            public string writeConfig(int index)
            {
                string config = $"[{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.Trigger{index.ToString("D2")}]\n";
                config += $"Name = {programName}\n";
                config += $"Tally = {targetTally.programName}\n";
                config += $"ActionSet = {targetSet.programName}\n";
                //Consult the two 'has' flags to see if we have config for this scenario.
                if (hasLess)
                {
                    config += $"LessOrEqualValue = {lessOrEqual}\n";
                    //Inline statements are a fickle beast. Leave off the parentheses here and it'll
                    //pitch a fit.
                    config += $"LessOrEqualCommand = {(commandLess ? "on" : "off")}\n";
                }
                if (hasGreater)
                {
                    config += $"GreaterOrEqualValue = {greaterOrEqual}\n";
                    config += $"GreaterOrEqualCommand = {(commandGreater ? "on" : "off")}\n";
                }
                return config;
            }
        }

        public class Raycaster : IHasConfig
        {
            //The stringbuilder we'll use to assemble reports.
            StringBuilder _sb;
            //The raycaster module that handles how actual scans will be performed.
            private RaycasterModuleBase scanModule;
            //The data struct that will hold information about the last entity we detected
            //TODO: Monitor. I don't think entityInfo serves a purpose that the report string doesn't
            //MyDetectedEntityInfo entityInfo;
            //Holds the report on the last entity detected, or informs that no entity was detected.
            string report;
            //Flag indicating if we've recently performed a scan.
            internal bool hasUpdate { get; private set; }
            internal string programName { get; private set; }

            public Raycaster(StringBuilder _sb, RaycasterModuleBase scanModule, string programName)
            {
                this._sb = _sb;
                this.scanModule = scanModule;
                this.programName = programName;
                report = $"{DateTime.Now.ToString("HH:mm:ss")}- Raycaster {programName} " +
                          $"reports: No data";
                hasUpdate = false;
            }

            public void addCamera(IMyCameraBlock camera)
            {
                scanModule.addCamera(camera);
                camera.EnableRaycast = true;
            }

            public double getModuleRequiredCharge()
            { return scanModule.requiredCharge; }

            public void scan()
            {
                MyDetectedEntityInfo entityInfo;
                double scanRange;
                //We've offloaded all the scanning work to the scanning module. Tell it to do the thing.
                IMyCameraBlock camera = scanModule.scan(out entityInfo, out scanRange);
                //We've offloaded the report writing to a different method. Tell it to do the thing.
                writeReport(entityInfo, scanRange, camera);
                //No matter what happened, set hasUpdate to true.
                hasUpdate = true;
            }

            private void writeReport(MyDetectedEntityInfo entityInfo, double scanRange, IMyCameraBlock camera)
            {
                //Clear the current contents of the StringBuilder
                _sb.Clear();
                //A null camera indicates no cameras had enough charge to perform this scan (Or the
                //user hasn't linked any cameras to this raycaster).
                if (camera == null)
                {
                    _sb.Append($"{DateTime.Now.ToString("HH:mm:ss")}- Raycaster {programName} " +
                        $"reports: No cameras have the required {getModuleRequiredCharge()} charge " +
                        $"needed to perform this scan.");
                }
                //Or, we might have just not hit anything.
                else if (entityInfo.IsEmpty())
                {
                    _sb.Append($"{DateTime.Now.ToString("HH:mm:ss")}- Raycaster {programName} " +
                        $"reports: Camera '{camera.CustomName}' detected no entities on a " +
                        $"{scanRange} meter scan.");
                }
                //But if we did make the scan, and that scan hit something...
                else
                {
                    _sb.Append($"{DateTime.Now.ToString("HH:mm:ss")}- Raycaster {programName} " +
                        $"reports: Camera '{camera.CustomName}' detected entity '{entityInfo.Name}' " +
                        $"on a {scanRange} meter scan.\n\n");
                    //The relationship between the player and the target
                    _sb.Append($"Relationship: {entityInfo.Relationship}\n");
                    //The target's type
                    _sb.Append($"Type: {entityInfo.Type}\n");
                    //The target's size
                    _sb.Append($"Size: {entityInfo.BoundingBox.Size.ToString("0.00")}\n");
                    //A handle that will make several of these calculations easier.
                    Vector3D target = entityInfo.HitPosition.Value;
                    //Distance to the target
                    _sb.Append($"Distance: {Vector3D.Distance(camera.GetPosition(), target).ToString("0.00")}\n");
                    //Coordinates of the point at which the target was struck
                    _sb.Append($"GPS:Raycast - {entityInfo.Name}:{target.X}:{target.Y}:{target.Z}:\n");
                }
                report = _sb.ToString();
                _sb.Clear();
            }

            public void updateClaimed()
            { hasUpdate = false; }

            public string toString()
            { return report; }

            public string writeConfig(int index)
            {
                //The string that will hold our finished config.
                string config = $"[{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.Raycaster{index.ToString("D2")}]\n";
                config += $"Name = {newLineToMultiLine(programName)}\n";
                //Literally everything else is handled in the scan module.
                config += scanModule.writeConfig();

                return config;
            }
        }

        public abstract class RaycasterModuleBase
        {
            //The camera pool that we will draw from to perform our raycasts
            protected List<IMyCameraBlock> cameras;
            //The ammount of charge that this module need to perform one of its configured scans.
            public double requiredCharge { get; protected set; }

            public RaycasterModuleBase()
            {
                cameras = new List<IMyCameraBlock>();
                requiredCharge = 0;
            }

            public void addCamera(IMyCameraBlock camera)
            {
                //A camera won't do us any good if we can't raycast from it.
                camera.EnableRaycast = true;
                cameras.Add(camera);
            }

            public abstract void configureModuleByArray(double[] configuration);
            public abstract IMyCameraBlock scan(out MyDetectedEntityInfo entityInfo, out double scanRange);
            public abstract string writeConfig();
        }

        public class RaycasterModuleLinear : RaycasterModuleBase
        {
            //The initial scan distance
            private double baseRange;
            //How much we multiply the scanRange by on each successive scan
            private double multiplier;
            //The maximum distance we will scan.
            private double maxRange;

            public RaycasterModuleLinear() : base()
            {
                int[] defaults = getModuleDefaultValues();
                baseRange = defaults[0];
                multiplier = defaults[1];
                maxRange = defaults[2];
            }

            public static string[] getModuleConfigurationKeys()
            { return new string[] { "BaseRange", "Multiplier", "MaxRange" }; }

            internal static int[] getModuleDefaultValues()
            { return new int[] { 1000, 3, 27000 }; }

            public override void configureModuleByArray(double[] configuration)
            {
                //Only override the default constructor values if the evaluator found us some valid config
                if (configuration[0] != -1) baseRange = configuration[0];
                if (configuration[1] != -1) multiplier = configuration[1];
                if (configuration[2] != -1) maxRange = configuration[2];

                //Determining the requiredCharge isn't a simple mathmatical equation, because the
                //range of the last scan might not match the rest of the formula. In order to figure
                //this value out, we need to go through the motions of performing a scan.
                double simulatedScanRange = baseRange;
                requiredCharge = baseRange;
                while (simulatedScanRange < maxRange)
                {
                    //Increment the range of our next scan by the multiplier
                    simulatedScanRange *= multiplier;
                    //Use the smaller of the maxRange or the simulatedScanRange. This will prevent
                    //us from exceeding our cap.
                    requiredCharge += Math.Min(maxRange, simulatedScanRange);
                    //requiredCharge += simulatedScanRange >= maxRange ? maxRange : simulatedScanRange;
                }
            }

            public override IMyCameraBlock scan(out MyDetectedEntityInfo entityInfo, out double scanRange)
            {
                //TODO: Monitor. I assume that this struct will be created with isEmpty = true, but
                //I'm not certain of that.
                entityInfo = new MyDetectedEntityInfo();
                scanRange = -1;
                IMyCameraBlock camera = getMostChargedCamera();
                //First thing's first: Can we even make this scan?
                if (camera == null || camera.AvailableScanRange < requiredCharge)
                //We can't make the scan. Return a null camera so the calling method will know what 
                //happened.
                { return null; }
                else
                {
                    //The initial scan range will be the base distance.
                    scanRange = baseRange;
                    //Perform a piddling initial scan, for no other purpose than establishing that the
                    //isEmpty flag on entityInfo is clear (Or that there's a block right in front of 
                    //the camera).
                    //Disabling for now to see if the entityInfo I created earlier carries the 
                    //isEmpty flag.
                    //entityInfo = camera.Raycast(1, 0, 0);
                    //While we haven't hit anything, and while we can make another scan...
                    while (entityInfo.IsEmpty() && scanRange < maxRange)
                    {
                        //Run a scan at the indicated range
                        entityInfo = camera.Raycast(scanRange, 0, 0);
                        //Prepare for the next iteration of the loop by calculating a new scan range
                        scanRange *= multiplier;
                        //If this new range is going to exceed our maximum...
                        if (scanRange > maxRange)
                        //Replace the calculated scanRange with max
                        { scanRange = maxRange; }
                    }
                    //Whatever happened is up for the host Raycaster to determine. The EntityInfo is
                    //already set, just need to return the camera we used.
                    return camera;
                }
            }

            private IMyCameraBlock getMostChargedCamera()
            {
                IMyCameraBlock candidateCamera = null;
                foreach (IMyCameraBlock camera in cameras)
                {
                    if (candidateCamera == null || camera.AvailableScanRange > candidateCamera.AvailableScanRange)
                    { candidateCamera = camera; }
                }
                return candidateCamera;
            }

            public override string writeConfig()
            {
                //BaseRange, Multiplier, MaxRange
                string[] configKeys = getModuleConfigurationKeys();
                int[] defaults = getModuleDefaultValues();
                string config = "Type = Linear\n";

                if (baseRange != defaults[0])
                { config += $"{configKeys[0]} = {baseRange}\n"; }
                if (multiplier != defaults[1])
                { config += $"{configKeys[1]} = {multiplier}\n"; }
                if (maxRange != defaults[2])
                { config += $"{configKeys[2]} = {maxRange}\n"; }

                return config;
            }
        }

        //Interface used by things that can be displayed by an MFD
        public interface IReportable
        {
            //Prepare a surface for displaying a reportable by setting the mode, colors, etc
            void setProfile();
            //Update the sprites or text on this reportable
            void update();
            //Perform an update without considering if one is needed.
            void forceUpdate();
        }

        //Intreface implemented by non-MFD reportables
        public interface IHasColors
        {
            Color foreColor { get; set; }
            Color backColor { get; set; }
        }

        public class MFD : IReportable
        {
            //The name of this MFD (Mostly used in interactions with the Storage string)
            public string programName { get; private set; }
            //The Reportable objects managed by this MFD
            private Dictionary<string, IReportable> pages;
            //The index of the report currently being displayed by the MFD
            internal int pageNumber { get; private set; }
            //The name of the report currently being displayed by the MFD.
            internal string pageName { get; private set; }

            public MFD(string programName)
            {
                this.programName = programName;
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
                /* CHECK: Removed in an attempt to convince servers to send clients new sptires.
                pages[pageName].update();
                */
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
                    /* CHECK: Removed in an attempt to convince servers to send clients new sptires.
                    pages[pageName].update();
                    */
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

            public void forceUpdate()
            { pages[pageName].forceUpdate(); }
        }

        public class GameScript : IReportable, IHasColors
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

            public void forceUpdate()
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

        public class Report : IReportable, IHasColors
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

            public void setAnchors(int columns, StringBuilder _sb)
            {
                //Malware's code for determining the viewport offset, which is the difference 
                //between an LCD's texture size and surface size. I have only the vaguest notions
                //of how it works.
                RectangleF viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f,
                    surface.SurfaceSize);
                //A string builder that we have to have before MeasureStringInPixels will tell us
                //the dimensions of our element
                _sb.Clear();
                //If there's no title, we don't need to leave any space for it.
                float titleY = 0;
                //If there's a title, though, we'll need to make room for that.
                if (!string.IsNullOrEmpty(title))
                {
                    //Feed our title into the stringbuilder
                    _sb.Append(title);
                    //Figure out how much vertical space we'll need to leave off to accomodate it.
                    titleY = surface.MeasureStringInPixels(_sb, font, fontSize).Y;
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
                        _sb.Clear();
                        //Force-feed it the string that we already have a perfectly good method for 
                        //building
                        _sb.Append(elements[i].assembleElementStack());
                        //Politely request the dimensions of the string we 'built'.
                        elementSize = surface.MeasureStringInPixels(_sb, font, fontSize);
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
                _sb.Clear();
            }

            //Re-draws this report, pulling new information from its elements to do so.
            public void update()
            {
                //A handle for elements we'll be working with
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
                        //If this element is actually a null, we don't have to do anything at all.
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

            public void forceUpdate()
            { update(); }

            //Prepare this surface for displaying the report.
            public void setProfile()
            {
                surface.ContentType = ContentType.SCRIPT;
                surface.Script = "";
                surface.ScriptForegroundColor = foreColor;
                surface.ScriptBackgroundColor = backColor;
                //To save on data transfer in multiplayer, the server only sends out updated sprites
                //if they've 'changed enough'. But sometimes (Especially when using MFDs), our 
                //elements won't meet that bar. So this iteration of setProfile will draw a throwaway
                //image in place of each of the elements, hopefully prompting the server to send out
                //the new sprites.
                //A handle for elements we'll be working with
                /*IHasElement element;*/
                //The sprite we'll be using to convey our information
                MySprite sprite;
                Vector2 spriteSize = new Vector2(100);
                using (MySpriteDrawFrame frame = surface.DrawFrame())
                {
                    //Toss a temporary sprite at the title's anchor, if we have one.
                    if (!string.IsNullOrEmpty(title))
                    {
                        //IconEnergy
                        sprite = MySprite.CreateSprite("IconEnergy", titleAnchor, spriteSize);
                        frame.Add(sprite);
                    }
                    for (int i = 0; i < elements.Count(); i++)
                    {
                        sprite = MySprite.CreateSprite("IconEnergy", anchors[i], spriteSize);
                        frame.Add(sprite);
                    }
                }
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
            //(20230404) Custom Data Brokers provide exactly one update. This is to clear the 
            //contents of an MFD that also contains a Log WOT.
            bool oneTimeUpdate;

            public CustomDataBroker(IMyTerminalBlock block)
            {
                this.block = block;
                oneTimeUpdate = true;
            }

            public string getData()
            //Pull the CustomData from this block
            { return block.CustomData; }

            public bool hasUpdate()
            {
                bool result = oneTimeUpdate;
                oneTimeUpdate = false;
                return result;
            }
        }

        public abstract class InfoBroker : IHasData
        {
            //The block this broker is pulling data from
            protected IMyTerminalBlock block;
            //Stores the last piece of information collected by this broker. Used to determine if
            //the broker has an update.
            string oldInfo;

            public InfoBroker(IMyTerminalBlock block)
            {
                this.block = block;
                oldInfo = "";
            }

            public abstract string getData();

            public bool hasUpdate()
            {
                //TODO: See if there's a more effecient way to do this.
                //If the DetailInfo of our block matches the oldInfo...
                if (getData() == oldInfo)
                //There is no update.
                { return false; }
                else
                {
                    //Store the new info in the oldInfo
                    oldInfo = getData();
                    //Indicate that we have an update.
                    return true;
                }
            }
        }

        public class DetailInfoBroker : InfoBroker
        {
            public DetailInfoBroker(IMyTerminalBlock block) : base(block)
            { }

            public override string getData()
            //Pull the DetailInfo from this block
            { return block.DetailedInfo; }
        }

        public class CustomInfoBroker : InfoBroker
        {
            public CustomInfoBroker(IMyTerminalBlock block) : base(block)
            { }

            public override string getData()
            //Pull the DetailInfo from this block
            { return block.CustomInfo; }
        }

        public class LogBroker : IHasData
        {
            //The EventLog this broker is pulling data from (One among the great multitudes)
            EventLog log;

            public LogBroker(EventLog log)
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

        public class WallOText : IReportable, IHasColors
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
            //29249429: Only passing through, but I think the reason was that, if you have a getter 
            //or a setter, they are used for all instances of getting and setting. Including the ones
            //in the getter and setter. In order to make this work, I'd need two variables: a private
            //one that actually holds the data, and the public one that uses getters and setters to
            //manipulate the private variable.
            //I believe convention is that the private variable is camel-cased, and the public variable
            //is... Uh, whatever you call it when you capitalize each word.
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

            public void forceUpdate()
            { surface.WriteText(prepareText(broker.getData())); }

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
                oldColor = cozy;
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
            //Try to add a block to this TallyHandler. If the block is compatible, it will be added
            //to the internal list and true will be returned. If the block is not compatible, false
            //is returned
            bool tryAddBlock(IMyTerminalBlock block);
            //Return the maximum value of the blocks in this handler
            double getMax();
            //Return the current value of the blocks in this handler
            double getCurr();
            //Get any configuration K/V pairs specific to this handler.
            string getHandlerConfig();
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

            public string getHandlerConfig()
            { return ""; }

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

            public string getHandlerConfig()
            { return ""; }

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

            public string getHandlerConfig()
            { return ""; }

            public string writeConfig()
            { return "JumpDrive"; }
        }

        //The user will probably only have one raycaster per tally. But who are we to judge?
        public class RaycastHandler : ITallyGenericHandler
        {
            List<IMyCameraBlock> subjects;
            Raycaster linkedRaycaster;

            public RaycastHandler()
            {
                subjects = new List<IMyCameraBlock>();
                linkedRaycaster = null;
            }

            public void linkRaycaster(Raycaster raycaster)
            { linkedRaycaster = raycaster; }

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

            public string getHandlerConfig()
            { return linkedRaycaster == null ? "" : $"Raycaster = {linkedRaycaster.programName}\n"; }

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
        public class PowerMaxHandler : ITallyGenericHandler
        {
            List<IMyPowerProducer> subjects;

            public PowerMaxHandler()
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

            public string getHandlerConfig()
            { return ""; }

            public string writeConfig()
            { return "PowerMax"; }
        }

        public class PowerCurrentHandler : ITallyGenericHandler
        {
            List<IMyPowerProducer> subjects;

            public PowerCurrentHandler()
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

            //For the max of PowerCurrent, we use the same DefinedOutput that we used for PowerMax
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
                { curr += producer.CurrentOutput; }
                return curr;
            }

            public string getHandlerConfig()
            { return ""; }

            public string writeConfig()
            { return "PowerCurrent"; }
        }

        public class IntegrityHandler : ITallyGenericHandler
        {
            List<IMySlimBlock> subjects;

            public IntegrityHandler()
            { subjects = new List<IMySlimBlock>(); }

            public bool tryAddBlock(IMyTerminalBlock block)
            {
                //For the rest of the handlers, as we cast the block to the handler's type, we perform
                //a check to see if that's actually a thing we can do. But in this case, we can't do a
                //simple cast, and besides, every TerminalBlock should have a Slimblock that can 
                //provide an integrity reading. So we just return 'true'.
                //(Plus, if something goes wrong in this process, all the evaluator knows to say
                //is 'type mismatch', and that's very unlikely to be what went wrong here.)
                //Get the grid this block is in, then find the slimblock that happens to be at the
                //same location as our terminal block
                IMySlimBlock prospect = block.CubeGrid.GetCubeBlock(block.Min);
                subjects.Add(prospect);
                return true;
            }

            public double getMax()
            {
                double max = 0;
                foreach (IMySlimBlock block in subjects)
                { max += block.MaxIntegrity; }
                return max;
            }

            public double getCurr()
            {
                double curr = 0;
                //The maximum health of a block is based on how complete it is. To get the current
                //health, we subtract the damage the block has taken from its completeness.
                foreach (IMySlimBlock block in subjects)
                { curr += block.BuildIntegrity - block.CurrentDamage; }
                return curr;
            }

            public string getHandlerConfig()
            { return ""; }

            public string writeConfig()
            { return "Integrity"; }
        }

        public class VentPressureHandler : ITallyGenericHandler
        {
            List<IMyAirVent> subjects;

            public VentPressureHandler()
            { subjects = new List<IMyAirVent>(); }

            public bool tryAddBlock(IMyTerminalBlock block)
            {
                IMyAirVent prospect = block as IMyAirVent;
                if (prospect == null)
                { return false; }
                else
                {
                    subjects.Add(prospect);
                    return true;
                }
            }

            //Vents report pressurization as decimal, with 1 corresponding to 100%. 
            public double getMax()
            {
                double max = 0;
                foreach (IMyAirVent vent in subjects)
                { max += 1; }
                return max;
            }

            public double getCurr()
            {
                double curr = 0;
                foreach (IMyAirVent vent in subjects)
                { curr += vent.GetOxygenLevel(); }
                return curr;
            }

            public string getHandlerConfig()
            { return ""; }

            public string writeConfig()
            { return "VentPressure"; }
        }

        public class PistonExtensionHandler : ITallyGenericHandler
        {
            List<IMyPistonBase> subjects;

            public PistonExtensionHandler()
            { subjects = new List<IMyPistonBase>(); }

            public bool tryAddBlock(IMyTerminalBlock block)
            {
                IMyPistonBase prospect = block as IMyPistonBase;
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
                foreach (IMyPistonBase piston in subjects)
                { max += piston.HighestPosition; }
                return max;
            }

            public double getCurr()
            {
                double curr = 0;
                foreach (IMyPistonBase piston in subjects)
                { curr += piston.CurrentPosition; }
                return curr;
            }

            public string getHandlerConfig()
            { return ""; }

            public string writeConfig()
            { return "PistonExtention"; }
        }

        public class RotorAngleHandler : ITallyGenericHandler
        {
            List<IMyMotorStator> subjects;

            public RotorAngleHandler()
            { subjects = new List<IMyMotorStator>(); }

            public bool tryAddBlock(IMyTerminalBlock block)
            {
                IMyMotorStator prospect = block as IMyMotorStator;
                if (prospect == null)
                { return false; }
                else
                {
                    subjects.Add(prospect);
                    return true;
                }
            }

            //Rotors move in a circle. Their maximum is 360.
            public double getMax()
            {
                double max = 0;
                foreach (IMyMotorStator rotor in subjects)
                { max += 360; }
                return max;
            }

            public double getCurr()
            {
                double curr = 0;
                foreach (IMyMotorStator rotor in subjects)
                //The rotor will not tell us the angle in degrees, even though the interface reports 
                //the angle in degrees, and the physical rotor has deliniations in degrees. So we 
                //need to convert the radians we can get to degrees.
                { curr += MathHelper.ToDegrees(rotor.Angle); }
                return curr;
            }

            public string getHandlerConfig()
            { return ""; }

            public string writeConfig()
            { return "RotorAngle"; }
        }

        public class ControllerSpeedHandler : ITallyGenericHandler
        {
            List<IMyShipController> subjects;

            public ControllerSpeedHandler()
            { subjects = new List<IMyShipController>(); }

            public bool tryAddBlock(IMyTerminalBlock block)
            {
                IMyShipController prospect = block as IMyShipController;
                if (prospect == null)
                { return false; }
                else
                {
                    subjects.Add(prospect);
                    return true;
                }
            }

            //We'll use 110 (The default speed limit) as the max. If the user is using a speed mod, 
            //they can adjust the maximum themselves.
            public double getMax()
            { return 110; }

            public double getCurr()
            {
                double curr = -1;
                foreach (IMyShipController controller in subjects)
                {
                    //Controller tallies are a bit different. Instead of keeping a running tally of
                    //a value from each block in the tally, we look through our subject list until
                    //we find a functional block, then ask it for whatever value we're looking for.
                    if (controller.IsFunctional)
                    {
                        curr = controller.GetShipSpeed();
                        break;
                    }
                }
                return curr;
            }

            public string getHandlerConfig()
            { return ""; }

            public string writeConfig()
            { return "ControllerSpeed"; }
        }

        public class ControllerGravityHandler : ITallyGenericHandler
        {
            List<IMyShipController> subjects;

            public ControllerGravityHandler()
            { subjects = new List<IMyShipController>(); }

            public bool tryAddBlock(IMyTerminalBlock block)
            {
                IMyShipController prospect = block as IMyShipController;
                if (prospect == null)
                { return false; }
                else
                {
                    subjects.Add(prospect);
                    return true;
                }
            }

            //We'll use 1 as the maximum for planetary gravity, even though Pertam weighs in at 1.2.
            //And don't even get me started on Omicron.
            public double getMax()
            { return 1; }

            public double getCurr()
            {
                double curr = 0;
                foreach (IMyShipController controller in subjects)
                {
                    //Formula courtesy of gothosan via Ye Olde KSH Forums
                    //https://forum.keenswh.com/threads/how-do-i-get-natural-gravity-vector-solved.7373786/
                    if (controller.IsFunctional)
                    {
                        curr = controller.GetNaturalGravity().Length() / 9.81;
                        break;
                    }
                }
                return curr;
            }

            public string getHandlerConfig()
            { return ""; }

            public string writeConfig()
            { return "ControllerGravity"; }
        }

        public class ControllerWeightHandler : ITallyGenericHandler
        {
            List<IMyShipController> subjects;

            public ControllerWeightHandler()
            { subjects = new List<IMyShipController>(); }

            public bool tryAddBlock(IMyTerminalBlock block)
            {
                IMyShipController prospect = block as IMyShipController;
                if (prospect == null)
                { return false; }
                else
                {
                    subjects.Add(prospect);
                    return true;
                }
            }

            //We have no idea what the maximum weight of a grid is, so we require the user to define one
            public double getMax()
            { return -1; }

            public double getCurr()
            {
                double curr = -1;
                foreach (IMyShipController controller in subjects)
                {
                    if (controller.IsFunctional)
                    {
                        curr = controller.GetNaturalGravity().Length() *
                            controller.CalculateShipMass().PhysicalMass;
                        break;
                    }
                }
                return curr;
            }

            public string getHandlerConfig()
            { return ""; }

            public string writeConfig()
            { return "ControllerWeight"; }
        }

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
                    //TODO: Return to its former glory when it won't turn everything yellow.
                    //ticWidget = new string[] { "[|----]", "[-|---]", "[--|--]", "[---|-]", "[----|]" };
                    ticWidget = new string[] { "|----", "-|---", "--|--", "---|-", "----|" };
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
                //(20230426) This version makes the timestamp cozy color'd. Or it's supposed to, it
                //came out as a sort of lilac when tested. Possibly due to the color of the background.
                //log.Insert(0, $"[color=#FFE1C8FF]{DateTime.Now.ToString("HH:mm:ss")}[/color]- {newEntry}");
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
                _sb.Clear();
            }
        }
    }
}

///<status> <date> </date>
///  <content>
///     
///  </content>
///</status>
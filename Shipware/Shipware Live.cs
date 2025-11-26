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
using VRage.Scripting.MemorySafeTypes; //For the MemorySafeStringBuilder used by TerminalActions.

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
        const double _VERSION = .8022;
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
        //Possibly the cheekiest thing I've done in pursuit of a reduced character count.
        const bool _TRUE = true;
        const bool _FALSE = false;
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
        //UpdateDistributor _distributor;
        //The new version of the distributor handles periodic events (Script updates, sprite refresh)
        //as well as tracking cooldowns on demanding script commands.
        Distributor _distributor;
        //The currently-active state machine
        StateMachineBase _activeMachine;
        //Machines that are waiting for their chance to be run. This list includes the active machine.
        //FAT: Using a dictionary is a bit of overkill, given that it only needs to detect if a 
        //machine of the same type has already been scheduled, and I've only got like three types of 
        //machine planned.
        Dictionary<string, StateMachineBase> _scheduledMachines;
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
        //Don't forget to initialize
        //IMyTextSurface _debugDisplay;

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
            _haveGoodConfig = _FALSE;
            _lastGoodConfigStamp = DateTime.Now;
            //These are basically local variables that will be passed to other methods once we're 
            //done here.
            textLog = new LimitedMessageLog(_sb, 15);
            firstRun = _FALSE;
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
            _log = new EventLog(_sb, $"Shipware v{_VERSION} - Recent Events", _TRUE);
            //The meterMaid that will generate ASCII meters for our tallies
            _meterMaid = new MeterMaid(_sb);
            //If we have a custom tag, we want to have that information front and center in the log
            if (_tag != $"{_SCRIPT_PREFIX}.{_DEFAULT_ID}")
            { _log.scriptTag = _tag; }
            //The distributer that handles updateDelays
            _distributor = new Distributor();
            //The dictionary that holds running and scheduled state machines
            _scheduledMachines = new Dictionary<string, StateMachineBase>();
            //DEBUG USE: The text surface we'll be using for debug prints
            //_debugDisplay = Me.GetSurface(1);
            //_debugDisplay.ContentType = ContentType.TEXT_AND_IMAGE;
            //_debugDisplay.WriteText("*DEBUG LOGGING ACTIVE*\n");
            /*
            //Try to initialize the ShieldBroker, which manages the actual ShieldAPI object used by
            //ShieldTally objects.
            shieldBroker = new ShieldBroker(Me);
            */
            //Clear the MyIni we used in this method.
            _iniReadWrite.Clear();
            //Last step is to make some decisions based on the version number.
            if (lastVersion == -1)
            { firstRun = _TRUE; }
            else if (lastVersion != _VERSION)
            { textLog.addNote($"Code updated from v{lastVersion} to v{_VERSION}."); }

            _log.add("Script initialization complete.");
        }

        public void firstRunSetup(LimitedMessageLog textLog)
        {
            //The only thing we really need to do in a firstRun scenario is add the SW.Init section.
            //But we can only do that if we can make sense of what's on the PB.
            MyIniParseResult parseResult;
            string initTag = $"{_SCRIPT_PREFIX}.Init";
            if (!_iniReadWrite.TryParse(Me.CustomData, out parseResult))
            {
                textLog.addError($"Cannot generate a {initTag} section because the parser encountered " +
                    $"an error on line {parseResult.LineNo} of the Programmable Block's config: {parseResult.Error}");
            }
            else
            {
                ensureInitHasRequiredKeys(textLog, _TRUE);
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

        //Assumes that PBConfig has already been loaded into _iniReadWrite
        public void ensureInitHasRequiredKeys(LimitedMessageLog textLog, bool firstRun = _FALSE)
        {
            string initTag = $"{_SCRIPT_PREFIX}.Init";
            bool hasInitSection = _iniReadWrite.ContainsSection(initTag);
            //We only need to log individual key generation if there's already an init section. 
            bool logKeyGeneration = hasInitSection && !firstRun;
            bool configAltered = _FALSE;
            string pbName = Me.CustomName;

            //If the Init section is missing entirely, we'll make one note for the entire lot.
            if (!hasInitSection && !firstRun)
            {
                textLog.addNote($"{initTag} section was missing from block '{pbName}' and " +
                  $"has been re-generated.");
            }

            //We're basically going to be doing the same thing over and over. Only the Keys and the
            //Values will change from one run to the next, so we'll put all those in arrays and just 
            //loop through them
            string[] requiredKeys = new string[]
            {
                //Color palette
                "ColorOptimal",
                "ColorNormal",
                "ColorCaution",
                "ColorWarning",
                "ColorCritical",
                //SpriteRefresh
                "MPSpriteSyncFrequency",
                //AutoPopulate
                "APExcludedBlockTypes",
                "APExcludedBlockSubTypes",
                "APExcludedDeclarations"
            };
            string[] defaultValues = new string[]
            {
                //Color palette
                "Green",
                "LightBlue",
                "Yellow",
                "Orange",
                "Red",
                //Sprite refresh
                "-1",
                //AutoPopulate
                //|----------------------------------------------------------|
                ("MyObjectBuilder_ConveyorSorter, MyObjectBuilder_ShipWelder,\n" +
                "MyObjectBuilder_ShipGrinder"),
                ($"StoreBlock, ContractBlock, {_SCRIPT_PREFIX}.FurnitureSubTypes,\n" +
                $"{_SCRIPT_PREFIX}.IsolatedCockpitSubTypes, {_SCRIPT_PREFIX}.ShelfSubTypes"),
                /*("StoreBlock, ContractBlock, PassengerBench, PassengerSeatLarge,\n" +
                "PassengerSeatSmallNew, PassengerSeatSmallOffset, BuggyCockpit,\n" +
                "OpenCockpitLarge, OpenCockpitSmall, LargeBlockCockpit,\n" +
                "CockpitOpen, SmallBlockStandingCockpit, RoverCockpit,\n" +
                "SpeederCockpitCompact, LargeBlockStandingCockpit,\n" +
                "LargeBlockLockerRoom, LargeBlockLockerRoomCorner, LargeBlockBed,\n" +
                "LargeCrate, LargeBlockHalfBed, LargeBlockHalfBedOffset,\n" +
                "LargeBlockInsetBed, LargeBlockInsetBookshelf, LargeBlockLockers,\n" +
                "LargeBlockInsetKitchen, LargeBlockWeaponRack, SmallBlockWeaponRack"),*/
                ("ThrustersGeneric")
            };
            bool[] includeDivider = new bool[]
            {
                //Color palette
                _FALSE,
                _FALSE,
                _FALSE,
                _FALSE,
                _FALSE,
                //Sprite refresh
                _TRUE,
                //AutoPopulate
                _TRUE,
                _TRUE,
                _TRUE
            };

            for (int i = 0; i < requiredKeys.Length; i++)
            {
                tryCheckForDefaultIniKey(initTag, requiredKeys[i], defaultValues[i], includeDivider[i],
                    ref configAltered, logKeyGeneration, pbName, textLog);
            }

            //If the configuration has changed (Because we decided to add KVPs to it), we'll take 
            //this opportunity to update the actual config on the PB.
            if (configAltered)
            { Me.CustomData = _iniReadWrite.ToString(); }
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
            int updateDelay = _distributor.getPeriodicFrequency("UpdateDelay");
            //The new distributor returns a -1 if there's no periodic by a given name loaded, which 
            //can happen now and again for updateDelay. In that case, just write a 0 to the storage
            //string.
            _iniReadWrite.Set("Data", "UpdateDelay", updateDelay == -1 ? 0 : updateDelay);
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
            //Update100 is the frequency used by the script when it's operating normally. On these
            //tics, blocks are polled and pages are drawn. The frequency is only active if the script
            //has successfully passed evaluation.
            if ((updateSource & UpdateType.Update100) != 0)
            {
                //Notify the distributor that a tic has occurred. If it's time for an update...
                /*if (_distributor.tic())
                {
                    compute();
                    update();
                    //And tell the log about it
                    _log.tic();
                }*/
                //With the new Distributor, all we do is tell it to tic. The periodics know what they 
                //need to do.
                _distributor.tic();
            }
            //Update10 is reserved for the operation and handling of state machines. It's only active
            //when a state machine is running.
            if ((updateSource & UpdateType.Update10) != 0)
            {
                if (_activeMachine != null)
                {
                    //Run the state machine's current step
                    bool machineHasMoreSteps = _activeMachine.next();
                    _log.machineStatus = _activeMachine.status;
                    if (!machineHasMoreSteps)
                    {
                        //Pull and post the machine's summary, if we're expecting one.
                        if (_activeMachine.generateLogs)
                        { _log.add(_activeMachine.getSummary()); }
                        //Special handling for specific machines would go here.
                        /* From the testbed script:
                        MachineNameLister nameLister = _activeMachine as MachineNameLister;
                        if (nameLister != null)
                        {
                            _sb.Append($"Final Lister report:\n {nameLister.sb.ToString()}");
                        }*/

                        _activeMachine.end();
                        _scheduledMachines.Remove(_activeMachine.MACHINE_NAME);
                        //Set the active machine to null. This will prompt the main method to check
                        //the queue for another machine on the next Update10.
                        _activeMachine = null;
                    }
                }
                //If there is no active machine...
                else
                {
                    if (_scheduledMachines.Count > 0)
                    {
                        //Grab the next state machine from the list
                        _activeMachine = _scheduledMachines.Values.ElementAt(0);
                        _activeMachine.begin();
                    }
                    else
                    {
                        //If there's nothing else, shut down the state machine frequency.
                        //MONITOR. Bitwise operators are heresy, and this is the most heretical of the bunch.
                        //Just in case I need to look this up again: & is the logical AND, &= is an AND-
                        //Assign. Or something. Think +=. ~ is the bitwise complement operator. So what 
                        //this is doing is setting the runtime frequency of the current frequency AND the
                        //complement of the Update10 operand, which is to say, every bit is 1 except for 
                        //the one actually associated with this flag.
                        Runtime.UpdateFrequency &= ~UpdateFrequency.Update10;
                        //Let the log know that there's nothing happening.
                        _log.machineStatus = "";
                    }
                }
                //Think the log is always echo'd at the end of main?
                //Echo(_log.toString());
                //DEBUG USE
                //_debugDisplay.WriteText($"{_activeMachine?.status ?? "No active machine"}\n", _TRUE);
            }
            //UpdateOnce is reserved for running a follow-up evaluated in the next tic, in order to
            //spread out the work load.
            //It will soon be replaced by a dedicated state machine for evaluation.
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
                            bool receivingReply = _FALSE;
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
                                receivingReply = _TRUE;
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
                                //DEBUG USE?
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
                                //_debugDisplay.WriteText($"Target MFD: {MFDTarget}   Entered Command: {MFDPageCommand}\n");
                                if (_MFDs == null)
                                { _log.add($"Received MFD command, but script configuration isn't loaded."); }
                                //If we have MFDs, and we actually know what MFD the user is talking about...
                                else if (_MFDs.ContainsKey(MFDTarget))
                                {
                                    MFD targetMFD = _MFDs[MFDTarget];
                                    /*
                                    _debugDisplay.WriteText($"Page keys in MFD {MFDTarget}:\n", _TRUE);
                                    //REMEMBER: Change the accessability level of MFD's dictionary
                                    //when done
                                    foreach (string pageName in targetMFD.pages.Keys)
                                    { _debugDisplay.WriteText($"  {pageName}\n", _TRUE); }
                                    */
                                    //If it's one of the easy commands...
                                    //Note: Performing toLowerInvariant in the checks is intentional.
                                    //PageCommand could also include the name of a specific page,
                                    //and the dictionary that page is stored in is case-sensitive.
                                    if (MFDPageCommand == "next")
                                    { targetMFD.flipPage(_TRUE); }
                                    else if (MFDPageCommand == "prev")
                                    { targetMFD.flipPage(_FALSE); }
                                    //If it isn't one of the easy commands, assume the user is trying 
                                    //to set the MFD to a specific page.
                                    else
                                    {
                                        //If the MFD declines to set the page to the one named in 
                                        //the command...
                                        if (!targetMFD.trySetPageByName(MFDPageCommand))
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
                                if (!isEmptyString(outcome))
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
                                if (!isEmptyString(_nonDeclarationPBConfig))
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
                                //We use catagories as a stand-in for a couple dozen subtypes. This 
                                //method will convert those to their types.
                                convertExclusionCatagoriesToSubtypes(excludedSubTypes);

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
                                outcome = "Carried out APExclusionReport command.\n";
                                //The excluded block list is going to be a bit long, so we'll start 
                                //with the declarations.
                                //This process is going to be rather ineffecient (We'll use the 
                                //LimitedMessageLog for exactly one thing, and copileAPTemplates 
                                //is doing a lot of work we won't use here). However, it makes good 
                                //use of existing code, and the ExclusionReport doesn't need to do 
                                //much heavy lifting anyway, so it should be fine.
                                //_debugDisplay.WriteText("Handle declarations\n");
                                MyIniValue iniValue = _iniReadWrite.Get($"{_SCRIPT_PREFIX}.Init", "APExcludedDeclarations");
                                if (!isEmptyString(iniValue.ToString()))
                                {
                                    string rawExcludedDeclarations = iniValue.ToString();
                                    outcome += $"These declarations are being excluded from consideration " +
                                        $"by AutoPopulate: {rawExcludedDeclarations}.\n";
                                    List<string> elements;
                                    elements = rawExcludedDeclarations.Split(',').Select(p => p.Trim()).ToList();
                                    //We should only ever encounter one message with this log instance. 
                                    //But, old habits die hard.
                                    LimitedMessageLog errorLog = new LimitedMessageLog(_sb, 5);
                                    compileAPTemplates(elements, errorLog);
                                    if (errorLog.getWarningTotal() > 0)
                                    { outcome += errorLog.warningsToString(); }
                                    outcome += "\n";
                                }

                                //_debugDisplay.WriteText("Block report variable init\n", _TRUE);
                                HashSet<string> excludedTypes = getAPExclusionsFromInit("APExcludedBlockTypes");
                                Dictionary<string, int> typeDiectionary = excludedTypes.ToDictionary(h => h, h => 0);
                                HashSet<string> excludedSubTypes = getAPExclusionsFromInit("APExcludedBlockSubTypes");
                                convertExclusionCatagoriesToSubtypes(excludedSubTypes);
                                Dictionary<string, int> subTypeDiectionary = excludedSubTypes.ToDictionary(h => h, h => 0);
                                int ignoreCount = 0;
                                int typeCount = 0;
                                int subTypeCount = 0;

                                //_debugDisplay.WriteText("Getting blocks\n", _TRUE);
                                List<IMyTerminalBlock> reportBlocks = new List<IMyTerminalBlock>();
                                findBlocks<IMyTerminalBlock>(reportBlocks, b => b.IsSameConstructAs(Me));

                                //_debugDisplay.WriteText("Analyzing blocks\n", _TRUE);
                                foreach (IMyTerminalBlock block in reportBlocks)
                                {
                                    if (MyIni.HasSection(block.CustomData, $"{_SCRIPT_PREFIX}.APIgnore"))
                                    { ignoreCount++; }
                                    if (typeDiectionary.ContainsKey(block.BlockDefinition.TypeIdString))
                                    {
                                        //_debugDisplay.WriteText($"Found block with type {block.BlockDefinition.TypeIdString}\n", _TRUE);
                                        typeDiectionary[block.BlockDefinition.TypeIdString]++;
                                        typeCount++;
                                    }
                                    if (subTypeDiectionary.ContainsKey(block.BlockDefinition.SubtypeId))
                                    {
                                        //_debugDisplay.WriteText($"Found block with subtype {block.BlockDefinition.SubtypeId}\n", _TRUE);
                                        subTypeDiectionary[block.BlockDefinition.SubtypeId]++;
                                        subTypeCount++;
                                    }
                                }

                                //_debugDisplay.WriteText("Writing report\n", _TRUE);
                                outcome += $"Of the {reportBlocks.Count} TerminalBlocks on this " +
                                    $"construct, the following {ignoreCount + typeCount + subTypeCount} " +
                                    $"blocks are being excluded from consideration by AutoPopulate:\n";
                                outcome += $"\n -{ignoreCount} blocks excluded by APIgnore\n";

                                outcome += $"\n -{typeCount} blocks excluded by type\n";
                                foreach (KeyValuePair<string, int> pair in typeDiectionary)
                                { outcome += $"  >{pair.Value} {pair.Key}\n"; }

                                outcome += $"\n -{subTypeCount} blocks excluded by subype\n";
                                foreach (KeyValuePair<string, int> pair in subTypeDiectionary)
                                { outcome += $"  >{pair.Value} {pair.Key}\n"; }

                                _iniReadWrite.Clear();
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
                            /*
                            foreach (IReportable reportable in _reports)
                            { reportable.setProfile(); }
                            _log.add($"Carried out ResetReports command, re-applying text surface " +
                                $"variables of {_reports.Length} Reports.");
                            */
                            //The scheduler will take care of any log entries or other notifications.
                            if (_distributor.tryAddCooldown("ResetReports", 10, out outcome))
                            { tryScheduleMachine(new SpriteRefreshMachine(this, _reports, _TRUE)); }
                            else
                            { _log.add(outcome); }
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
                            Action testAction = () => { _log.add("Periodic event firing"); };
                            PeriodicEvent testEvent = new PeriodicEvent(10, testAction);
                            _distributor.tryAddPeriodic("Test Event", testEvent);

                            if (!_distributor.tryAddCooldown("Test Cooldown", 20, out outcome))
                            { _log.add(outcome); }
                            //tryScheduleMachine(new FillerMachine(this, 30, _TRUE));
                            _log.add(_distributor.debugPrintContents());
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
                { desiredState = _TRUE; }
                else if (actionCommand == "off")
                { desiredState = _FALSE; }
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
            if (isEmptyString(outcome) && !isEmptyString(source))
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
            { return _TRUE; }
            else
            {
                //'trouble' is expected to contain something along the lines of "Clear Command".
                trouble = $"Received {trouble}, but there is no {groupName} block group on the grid.";
                return _FALSE;
            }
        }

        //A simple wrapper for GTS.GetBlocksOfType, which I probably don't use enough to justify
        //implementing. But here we are.
        //List<IMyTerminalBlock> blocks: Will hold any blocks we find when we're done.
        //Func collect: Optional checks that will be run to see if we got the right block. 
        //  Defaults to null.
        public void findBlocks<T>(List<T> blocks, Func<T, bool> collect = null) where T : class
        { GridTerminalSystem.GetBlocksOfType<T>(blocks, collect); }

        public bool isEmptyString(string input)
        { return String.IsNullOrEmpty(input); }

        internal bool tryScheduleMachine(StateMachineBase newMachine)
        {
            string machineName = newMachine.MACHINE_NAME;
            //DEBUG USE
            /*
            if (_activeMachine == null)
            { _debugDisplay.WriteText($"Trying to schedule machine '{machineName}'\n'", false); }
            */
            if (!_scheduledMachines.ContainsKey(machineName))
            {
                _scheduledMachines.Add(machineName, newMachine);
                //Start the state machine frequency
                Runtime.UpdateFrequency |= UpdateFrequency.Update10;
                //MONITOR. I know it doesn't fire when there's no active machine, but I never actually
                //checked to see if it works when there is an active machine.
                if (newMachine.generateLogs && _activeMachine != null)
                { _log.add($"{machineName} successfully added to scheduled tasks."); }
                return _TRUE;
            }
            else
            //If the same type of machine is already in the queue...
            {
                _log.add($"Cannot schedule {machineName} because an identical task is already scheduled.");
                return _FALSE;
            }
        }

        //Writes all the linked declarations to a single string.
        public string writeDeclarations(List<Tally> tallies, List<ActionSet> actions, List<Trigger> triggers,
            List<Raycaster> raycasters)
        {
            string declarations;
            string divider = ";=======================================\n\n";
            _sb.Clear();

            foreach (Tally tally in tallies)
            { _sb.Append(tally.writeConfig()); }
            if (tallies.Count > 0)
            { _sb.Append(divider); }

            foreach (ActionSet action in actions)
            { _sb.Append(action.writeConfig()); }
            if (actions.Count > 0)
            { _sb.Append(divider); }

            foreach (Trigger trigger in triggers)
            { _sb.Append(trigger.writeConfig()); }
            if (triggers.Count > 0)
            { _sb.Append(divider); }

            foreach (Raycaster raycaster in raycasters)
            { _sb.Append(raycaster.writeConfig()); }

            declarations = _sb.ToString();
            _sb.Clear();
            return declarations;
        }

        /// <summary name = "AutoPopulate">
        ///   <process>
        ///       AP starts by consulting the Excluded Declarations listing. It then hands that list 
        ///     to compileAPTemplates, which constructs the templates that AP will be using to 
        ///     identify what kinds of declarations are relevant to the grid. Excluded declarations
        ///     are filtered out at the end of that method.
        ///       There are three types of templates. All of them hold either a Tally or ActionSet 
        ///     object, alongside a lambda expression used to identify blocks on the grid relevant
        ///     to that template. TallyCargos also get a lambda that lets them identify containers
        ///     that can hold their item, while ActionSetTemplates get a function that writes their
        ///     discrete section.
        ///       Once the templates are set, we return to the main body of the AutoPopulate method,
        ///     and start laying the groundwork for the Roost set. This is just for the basics,
        ///     things that won't change: header, display name, text and colors, etc. The state lists
        ///     are set much later in the process.
        ///       The next step is to determine what templates are relevant to this grid. The first
        ///     way they're deemed relevent is if their declarations are already on the PB. If a 
        ///     declaration is already present, that template is removed from the ToCheck list and added
        ///     to an in-use list. It's also removed from the 'templates to write' list, because it's
        ///     already written.
        ///       We then move on to scanning the grid itself. Here, we keep a HashSet of block types
        ///     that we've already encountred, to keep us from doing the same work twice. For blocks
        ///     that we haven't encountered, we check the block against the conditional of every template
        ///     remaining in the ToCheck list. If there's a match, we add that template to one of the
        ///     In Use lists.
        ///       With all the templates relevant to this grid in hand, we can write the declarations
        ///     from the toWrite list to the PB. But there's a couple of things we need to take care 
        ///     of before moving on to the grid at large. First, we need to write the 
        ///     ActionSetsLinkedToOn/Off keys of the Roost set, which we do with a fairly intelligent 
        ///     process that preserves existing entries in each state list. After that, we build the 
        ///     APScreen MFD, using a dumb process that rejects existing config and substitutes our 
        ///     own.
        ///       At last we're ready to address the grid at large. In many ways, this process is 
        ///     similar to what we did in the grid identification pass we did earlier. Once again
        ///     we keep a record of block types that we've already encountered, but along side that
        ///     we store the config we wrote to the first block of that type we encountered in an
        ///     APConfigEntry. ConfigEntries contain two objects, the first being a single string 
        ///     containing a comma-seperated list of declarations to be linked to this block 
        ///     (Usually tallies. Always Tallies, actually, but APConfigEntry is written in such a 
        ///     way that a seperate instance of the object could be used to store a list of ActionSet 
        ///     links, if AP dealt with those). The second object is a bool, 
        ///   </process>
        ///   <notes>
        ///     <1>AutoPopulate can be split into three major regions: The PB pass, where the custom
        ///        data of the programmable block is read to see what config, if any, is already 
        ///        present on this grid. The Grid Identification (Or discovery) pass, where the 
        ///        blocks on the grid are analyzed to determing what Tallies and ActionSets are 
        ///        relevant. And the Grid Assignment pass, where config is written to the custom
        ///        data of the grid's blocks.</1>
        ///     <2>AP makes liberal use of String.Contains in the assignment pass, to determine if 
        ///        a link is already present. This is mitigated by the fact that it only does this 
        ///        once per block type, but that does mean a lot of processing is going to be 
        ///        frontloaded into those first few thousand instructions. And it will only get worse
        ///        the longer the strings - and list of templates - grows. See possible mitigation
        ///        in 20250605.</2>
        ///   </notes>
        /// </summary>
        /// <param name="apBlocks"></param>
        /// <param name="pbParse"></param>
        /// <param name="mode"></param>
        /// <param name="outcome"></param>
        /// <returns>
        ///   A boolean that returns true if the operation ran successfully (There may not be a 
        ///   provision for the operation not running successfully).
        /// </returns>
        public bool AutoPopulate(List<IMyTerminalBlock> apBlocks, MyIni pbParse, string mode, ref string outcome)
        {
            //DEBUG USE
            //_debugDisplay.WriteText("Entering AutoPopulate method.\n");

            MyIni blockParse = _iniRead;
            MyIniParseResult parseResult;
            LimitedMessageLog apLog = new LimitedMessageLog(_sb, 10);

            //We've already handled block blacklisting, but we still need to read and handle the
            //contents of the |APExcludedDeclarations| key.
            MyIniValue iniValue = pbParse.Get($"{_SCRIPT_PREFIX}.Init", "APExcludedDeclarations");
            List<string> excludedDeclarations = null;
            if (!isEmptyString(iniValue.ToString()))
            { excludedDeclarations = iniValue.ToString().Split(',').Select(p => p.Trim()).ToList(); }
            //DEBUG USE
            //_debugDisplay.WriteText("Finished compiling Exclusion hash\n", _TRUE);

            //We'll make two (ish) passes through the grid. The first pass is to determine what
            //templates we'll need to be using, the second will be to apply those templates. But 
            //first, we'll need the templates themselves. 
            List<APTemplate> templatesToCheck = compileAPTemplates(excludedDeclarations, apLog);
            //_debugDisplay.WriteText("APTemplates successfuly compiled.\n", _TRUE);
            //While the template system is an effecient way to determine what script objects
            //we'll need to set up, the order of the blocks we're handed is eldest to youngest on 
            //the grid, and that means our declarations would be in that order if we wrote them as
            //soon as we found them. To get a semi-sensible order, we won't write config until we 
            //have a complete picture - and we'll get that picture from this variable.
            List<APTemplate> templatesToWrite = new List<APTemplate>(templatesToCheck);

            List<TallyGenericTemplate> tallyGenericsInUse = new List<TallyGenericTemplate>();
            List<TallyInventoryTemplate> tallyInventoriesInUse = new List<TallyInventoryTemplate>();
            List<ActionSetTemplate> actionSetsInUse = new List<ActionSetTemplate>();
            //When I decide I need a template, I sort it into a specific list. And this function does that.
            Action<APTemplate> sendTemplateToUsed = (t) =>
            {
                if (t is TallyGenericTemplate)
                { tallyGenericsInUse.Add((TallyGenericTemplate)t); }
                else if (t is TallyInventoryTemplate)
                { tallyInventoriesInUse.Add((TallyInventoryTemplate)t); }
                else if (t is ActionSetTemplate)
                { actionSetsInUse.Add((ActionSetTemplate)t); }
                //Conceivably, a new kind of template could fall through here. But that's going to
                //be a me problem.
                templatesToCheck.Remove(t);
            };
            //A long time ago, I gave each of the main script objects a writeConfig method for 
            //reasons that I'm sure made sense at the time. Whatever the original purpose was, I've
            //kept writeConfig around because there's a lot of uses I can put it to. 
            //Unfortunately, those methods were built to write directly to a block's CustomData, 
            //and so they return a string. For AP, I'm doing a lot of my work through MyIni, so if
            //I want to use writeConfig, I need a method for loading raw config into keys and values.
            //And it's up here because I need it for the initial write of the Roost set. Most of its
            //use will be down in the grid discovery pass. 
            List<MyIniKey> iniKeys = new List<MyIniKey>();
            List<string> iniValues = new List<string>();
            Action<string, MyIni, MyIni> writeConfigToIni = (config, iniParse, iniOut) =>
            {
                //This is straight from the horses's mouth. We'll assume it's good.
                iniParse.TryParse(config);
                iniParse.GetKeys(iniKeys);
                foreach (MyIniKey key in iniKeys)
                { iniValues.Add((/*blockParse*/iniParse.Get(key)).ToString()); }
                for (int i = 0; i < iniKeys.Count; i++)
                { iniOut.Set(iniKeys[i], iniValues[i]); }
                //We'll be using this variable again, so we'll need to clear it to prevent cross-
                //contamination (MyIni.GetKeys should clear iniKeys for us) 
                iniValues.Clear();
            };
            //As mentioned, I want the roost set to be front and center. The best way to accomplish 
            //that is to write it right now, before we do anything else. 
            //But first, ask: Do we already have a roost section? And Does the user even want a roost set?
            string roostHeader = $"{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.ActionSet.Roost";
            //DEBUG USE
            //_debugDisplay.WriteText("Deciding if Roost needs writing\n", _TRUE);
            if (!pbParse.ContainsSection(roostHeader) && !(excludedDeclarations?.Contains("Roost") ?? _FALSE))
            {
                //DEBUG USE
                //_debugDisplay.WriteText("Starting Roost write process\n", _TRUE);
                //All we're going to do at this point is set up the basics. Linking our other sets
                //to this one is something that will come after we know what sets we have.
                ActionSet roostSet = new ActionSet("Roost", _FALSE);
                roostSet.displayName = _customID;
                roostSet.colorOn = red;
                roostSet.colorOff = green;
                roostSet.textOn = "Roosting";
                roostSet.textOff = "Active";
                //One of Roost's big things is that it puts the script in a state where it updates
                //less frequently. Under the hood, that's handled by a specific kind of ActionPlan.
                //TODO: When I update this to use a state machine instead, I'll need a reference to
                //the program here.
                ActionPlanUpdate updatePlan = new ActionPlanUpdate(this);
                updatePlan.delayOn = 8;
                roostSet.addActionPlan(updatePlan);

                //DEBUG USE
                //_debugDisplay.WriteText("Writing Roost set to pbParse\n", _TRUE);
                //That's it for now. Load this config into pbParse.
                writeConfigToIni(roostSet.writeConfig(), blockParse, pbParse);
            }
            //DEBUG USE
            //_debugDisplay.WriteText("Initial roost write complete\n", _TRUE);

            //The first way we can decide we need a given template is if it's already written in 
            //the config. We'll give the PB declarations a pass.
            int index = 0;
            APTemplate template;
            string sectionHeader;
            while (index != -1)
            {
                template = templatesToCheck[index];
                sectionHeader = $"{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.{template.getDeclarationType()}.{template.name}";
                if (pbParse.ContainsSection(sectionHeader))
                {
                    //Send this template to one of the specialized list and remove it from availability.
                    //Because removing an item essentially sends us to the next item, we won't increment 
                    //the index here.
                    sendTemplateToUsed(template);
                    //This template is already written, we won't need to write it again.
                    templatesToWrite.Remove(template);
                }
                else
                { index++; }
                if (index >= templatesToCheck.Count)
                { index = -1; }
            }
            //DEBUG USE
            //_debugDisplay.WriteText("Read of existing PBConfig complete.\n", _TRUE);

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
            MyDefinitionId blockDef;
            HashSet<MyDefinitionId> alreadyEncounteredBlockDefs = new HashSet<MyDefinitionId>();
            //_debugDisplay.WriteText("\nBeginning grid identification pass\n", _TRUE);
            foreach (IMyTerminalBlock block in apBlocks)
            {
                blockDef = block.BlockDefinition;
                //Before we do anything else, ask: Have we already analyzed a block with this definition?
                if (!alreadyEncounteredBlockDefs.Contains(blockDef))
                {
                    //_debugDisplay.WriteText($" Performing template comparison for novel block {block.CustomName}\n", _TRUE);
                    index = 0;
                    //In a real-world setting, the odds of using every single template is /incredibly/
                    //low. But we'll need to account for it anyway.
                    while (index != -1 && templatesToCheck.Count != 0)
                    {
                        template = templatesToCheck[index];
                        if (template.blockMatchesConditional(block))
                        {
                            //_debugDisplay.WriteText($"  >Block {block.CustomName} matches conditional of {template.name}\n", _TRUE);
                            sendTemplateToUsed(template);

                            //Before we move on, we need to actually add this template's declaration 
                            //to the PBconfig. Because templates for existing script objects were removed 
                            //from the pool in the config pass, we shouldn't need to worry about duplicates.
                            //And now that we've stripped the indexes from the declaration headers, 
                            //this process is a lot more straightforward.
                            /*writeConfigToIni(template.writePBConfig(), blockParse, pbParse);*/

                            //Removing a template from the list is effectively the same as incrementing
                            //the index, so we won't do that on this branch.
                        }
                        else
                        { index++; }
                        if (index >= templatesToCheck.Count)
                        { index = -1; }
                    }
                    //Last step is to add this definition to our list of already encountered block 
                    //types, so we don't waste time analyzing it again.
                    alreadyEncounteredBlockDefs.Add(blockDef);
                }
            }
            //DEBUG USE
            //_debugDisplay.WriteText("Grid discovery pass complete\n", _TRUE);
            //We now have a nearly complete picture of what template we'll need to write to config. 
            //Our last step is to toss the templates we aren't using.
            foreach (APTemplate unusedTemplate in templatesToCheck)
            { templatesToWrite.Remove(unusedTemplate); }
            foreach (APTemplate newDeclaration in templatesToWrite)
            { writeConfigToIni(newDeclaration.writePBConfig(), blockParse, pbParse); }
            //DEBUG USE
            //_debugDisplay.WriteText("Declarations added to config\n", _TRUE);

            //We have all our templates in hand. Before we finish with the PB, we need to update the 
            //links on the Roost ActionSet and write the generic report that will be displayed
            //on the PB itself.
            //Roost first.
            //We'll put links to our AP ActionSets in Roost's ActionSetsLinkedToOn and ActionSetsLinkedToOff
            //keys, preserving any existing config. The only thing that'll stop us from doing that 
            //is if the user told us they don't want a roost set.
            if (!(excludedDeclarations?.Contains("Roost") ?? _FALSE))
            {
                string existingLinks;
                List<string> splitLinks;
                HashSet<string> hashedLinks;
                Action<string, bool> addActionLinksToRoost = (key, isOn) =>
                {
                    existingLinks = pbParse.Get(roostHeader, key).ToString();
                    if (!isEmptyString(existingLinks))
                    {
                        //We'll be checking for existing config before we add an entry. Because we're not
                        //sure exactly how much existing config there might be, we'll make one pass through
                        //the string and add existing links to a hashset.
                        splitLinks = existingLinks.Split(',').Select(p => p.Trim()).ToList();
                        hashedLinks = new HashSet<string>();
                        foreach (string link in splitLinks)
                        {
                            int colonIndex = link.IndexOf(':');
                            //We can't be sure this config hasn't been modified. We'll need to account 
                            //for the possibility that it doesn't adhere to the state list format. 
                            if (colonIndex != -1)
                            { hashedLinks.Add(link.Substring(0, colonIndex)); }
                            else
                            //This is a state list, each entry SHOULD have a colon in it. If it doesn't, 
                            //the odds of it containing useful information are incredibly low. But we'll 
                            //give it a shot.
                            { hashedLinks.Add(link); }
                        }
                    }
                    else
                    {
                        hashedLinks = null;
                        splitLinks = new List<string>();
                    }

                    //Now that we've laid the groundwork, we can quickly and effeciently see if we 
                    //need to add links for newly discovered ActionSets
                    string desiredState;
                    foreach (ActionSetTemplate actionTemplate in actionSetsInUse)
                    {
                        string programName = actionTemplate.name;
                        desiredState = actionTemplate.getStateWhenRoost(isOn);
                        if (!(hashedLinks?.Contains(programName) ?? _FALSE) && !isEmptyString(desiredState))
                        { splitLinks.Add($"{programName}: {desiredState}"); }
                    }
                    //Send the value back to the PB parse.
                    pbParse.Set(roostHeader, key, listToMultiLine(splitLinks, 3, _FALSE));
                };
                //All of the heavy lifting is done in the local function above. All we do now is point
                //it in two directions.
                addActionLinksToRoost("ActionSetsLinkedToOn", _TRUE);
                addActionLinksToRoost("ActionSetsLinkedToOff", _FALSE);
            }
            //DEBUG USE
            //_debugDisplay.WriteText("Roost linking complete\n", _TRUE);

            //Next, the generic report
            //_debugDisplay.WriteText("\nWriting APScreen MFD\n", _TRUE);
            string sectionName = "";
            List<string> elements = new List<string>();
            string elementListing;
            Action<string> writeCommonSurfaceConfig = (title) =>
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
                $"{(actionSetsInUse.Count > 0 ? "SetReport, " : "")}Log, TargetScript, FactionScript");
            pbParse.Set(_tag, "Surface0MFD", "APScreen");

            if (tallyGenericsInUse.Count > 0 || tallyInventoriesInUse.Count > 0)
            {
                sectionName = "SW.TallyReport";
                //One nice thing when we were unnessecarily using dictionaries: We could get this 
                //list just by pulling the keys. Now we need to build it from scratch.
                foreach (APTemplate tallyTemplate in tallyGenericsInUse)
                { elements.Add(tallyTemplate.name); }
                foreach (APTemplate tallyTemplate in tallyInventoriesInUse)
                { elements.Add(tallyTemplate.name); }
                elementListing = listToMultiLine(elements, 3, _FALSE);
                pbParse.Set(sectionName, "Elements", elementListing);
                writeCommonSurfaceConfig("Tallies");
            }
            elements.Clear();
            if (actionSetsInUse.Count > 0)
            {
                sectionName = "SW.SetReport";
                foreach (APTemplate setTemplate in actionSetsInUse)
                { elements.Add(setTemplate.name); }
                elementListing = listToMultiLine(elements, 3, _FALSE);
                pbParse.Set(sectionName, "Elements", elementListing);
                writeCommonSurfaceConfig("Action Sets");
            }

            sectionName = "SW.Log";
            pbParse.Set(sectionName, "DataType", "Log");
            pbParse.Set(sectionName, "FontSize", ".8");
            pbParse.Set(sectionName, "CharPerLine", "30");
            pbParse.Set(sectionName, "ForeColor", "LightBlue");
            pbParse.Set(sectionName, "BackColor", "Black");

            sectionName = "SW.TargetScript";
            pbParse.Set(sectionName, "Script", "TSS_TargetingInfo");
            pbParse.Set(sectionName, "ForeColor", "LightBlue");
            pbParse.Set(sectionName, "BackColor", "Black");

            sectionName = "SW.FactionScript";
            pbParse.Set(sectionName, "Script", "TSS_FactionIcon");
            pbParse.Set(sectionName, "BackColor", "Black");

            //DEBUG USE
            //_debugDisplay.WriteText("APMFD compiled.\n", _TRUE);

            //All this stuff we've done so far won't mean much if we don't write to the PB.
            Me.CustomData = pbParse.ToString();

            //We know the templates we need, now it's time to write config for them to the grid.
            //_debugDisplay.WriteText($"Beginning Assignment pass\n", _TRUE);
            Dictionary<MyDefinitionId, APBlockConfig> storedConfig = new Dictionary<MyDefinitionId, APBlockConfig>();
            APBlockConfig blockConfig = null;
            //int debugBlockConfigsCreated = 0;
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
                    return _FALSE;
                }
                else
                { return _TRUE; }
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
                        //debugBlockConfigsCreated++;
                        //_debugDisplay.WriteText($"  >Created APBlockConfig object for block {b.CustomName}\n", _TRUE);

                        return _TRUE;
                    }
                    else
                    { return _FALSE; }
                }
                return _TRUE;
            };
            foreach (IMyTerminalBlock block in apBlocks)
            {
                //Reset for this run of the loop
                blockConfig = null;
                //isBadParse = _FALSE;
                blockDef = block.BlockDefinition;
                if (storedConfig.ContainsKey(blockDef))
                {
                    //_debugDisplay.WriteText($"  Retrieving stored configuration for block {block.CustomName}\n", _TRUE);
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
                            //I'd like to have a seperate forceWriteConfigToIni to use when the block
                            //is missing a Common Section, to make this more readable. But with the 
                            //character limit looming, I'm just going to cram everything into one line.
                            blockConfig.writeConfigToIni(_tag, blockParse, !blockParse.ContainsSection(_tag));
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
                    //_debugDisplay.WriteText($"  Finding applicapble templates for block {block.CustomName}\n", _TRUE);
                    //This is going to look pretty strange. But it's arranged the way it is so we 
                    //only parse the block's custom data if we know we have a template for it.
                    foreach (TallyGenericTemplate genTemplate in tallyGenericsInUse)
                    {
                        if (genTemplate.blockMatchesConditional(block))
                        {
                            //As the name suggests, this subroutine will create a block config object 
                            //for us if we haven't yet this run, and parse the block's CustomData.
                            //It returns false only if the parse fails
                            if (tryParseIniAndCreateBlockConfig(block, blockParse))
                            //As per always, the tally generic version of this doesn't have much going on.
                            { blockConfig.addLink("Tallies", genTemplate.name); }
                            else
                            //If we can't read the CustomData, there's no point in continuing.
                            { goto CannotWriteToThisBlockSoSkipToNext; }
                        }
                    }
                    //_debugDisplay.WriteText($"  >Beginning inventory loop\n", _TRUE);
                    foreach (TallyInventoryTemplate invTemplate in tallyInventoriesInUse)
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
                    //_debugDisplay.WriteText($"  >Beginning Set loop\n", _TRUE); 
                    foreach (ActionSetTemplate setTemplate in actionSetsInUse)
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
                        //_debugDisplay.WriteText($"  >Found matching templates, writing config to block\n", _TRUE);
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

            outcome = $"\nCarried out {mode} command. There are now declarations for " +
                $"{tallyGenericsInUse.Count + tallyInventoriesInUse.Count} AP Tallies and {actionSetsInUse.Count} " +
                $"AP ActionSets, with linking config written to {linkedBlockCount} / {apBlocks.Count} of considered " +
                $"blocks{(totalBadParseCount > 0 ? $" and {totalBadParseCount} blocks with unparsable config" : "")}.\n" +
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
            return _TRUE;
        }

        /* KEEP: This is the old version of AP's Assignment pass, relying on breaks and an isBadParse
         * boolean to figure out if templates still need to be checked. Preserved just in case 
         * anyone questions my decision to switch to goto.
        foreach (IMyTerminalBlock block in apBlocks)
        {
            //Reset for this run of the loop
            blockConfig = null;
            isBadParse = _FALSE;
            blockDef = block.BlockDefinition;
            if (storedConfig.ContainsKey(blockDef))
            {
                _debugDisplay.WriteText($"  Retrieving stored configuration for block {block.CustomName}\n", _TRUE);
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
                _debugDisplay.WriteText($"  Finding applicapble templates for block {block.CustomName}\n", _TRUE);
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
                _debugDisplay.WriteText($"  >Beginning inventory loop\n", _TRUE);
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
                _debugDisplay.WriteText($"  >Beginning Set loop\n", _TRUE);
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
                    _debugDisplay.WriteText($"  >Found matching templates, writing config to block\n", _TRUE);
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
            if (!isEmptyString(iniValue.ToString()))
            {
                elements = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                foreach (string element in elements)
                { blacklist.Add(element); }
            }
            return blacklist;
        }

        public void convertExclusionCatagoriesToSubtypes(HashSet<string> excludedSubTypes)
        {
            string FURNITURE_TYPES = $"{_SCRIPT_PREFIX}.FurnitureSubTypes";
            if (excludedSubTypes.Contains(FURNITURE_TYPES))
            {
                excludedSubTypes.Remove(FURNITURE_TYPES);
                excludedSubTypes.UnionWith(new string[]
                {
                    "PassengerBench", "PassengerSeatLarge", "PassengerSeatSmallNew",
                    "PassengerSeatSmallOffset", "LargeBlockBed", "LargeBlockHalfBed",
                    "LargeBlockHalfBedOffset", "LargeBlockInsetBed", "LargeBlockCaptainDesk",
                    "LargeBlockLabDeskSeat", "LargeBlockLabCornerDesk"
                });
            }
            string COCKPIT_TYPES = $"{_SCRIPT_PREFIX}.IsolatedCockpitSubTypes";
            if (excludedSubTypes.Contains(COCKPIT_TYPES))
            {
                excludedSubTypes.Remove(COCKPIT_TYPES);
                excludedSubTypes.UnionWith(new string[]
                {
                    "BuggyCockpit", "OpenCockpitLarge", "OpenCockpitSmall",
                    "LargeBlockCockpit", "CockpitOpen", "SmallBlockStandingCockpit",
                    "RoverCockpit", "SpeederCockpitCompact", "LargeBlockStandingCockpit",
                    "LargeBlockModularBridgeCockpit"
                });
            }
            string SHELF_TYPES = $"{_SCRIPT_PREFIX}.ShelfSubTypes";
            if (excludedSubTypes.Contains(SHELF_TYPES))
            {
                excludedSubTypes.Remove(SHELF_TYPES);
                excludedSubTypes.UnionWith(new string[]
                {
                    "LargeBlockLockerRoom", "LargeBlockLockerRoomCorner", "LargeCrate",
                    "LargeBlockInsetBookshelf", "LargeBlockLockers",
                    "LargeBlockInsetKitchen", "LargeBlockWeaponRack", "SmallBlockWeaponRack",
                    "SmallBlockKitchenFridge", "SmallBlockFirstAidCabinet", "LargeBlockLabCabinet",
                    "LargeFreezer"
                });
            }
            //DEBUG USE
            /*_debugDisplay.WriteText("Subtype Hashset contents:\n");
            foreach (string entry in excludedSubTypes)
            { _debugDisplay.WriteText($"  {entry}\n", _TRUE); }*/
        }

        //The current version of compileAPTemplates, which uses a dictionary throughout but then
        //trims that down to a list for AP's use.
        public List<APTemplate> compileAPTemplates(List<string> excludedDeclarations, LimitedMessageLog apLog)
        {
            //FAT? (20240705) The only time I use this dictionary as a dictionary is at the end of 
            //this method, for pruning excluded declarations. After that, I simply pull its values
            //for use back in AP. Concievably, there are effecient ways to handle that task without 
            //needing the dictionary in the first place.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            Dictionary<string, APTemplate> templates = new Dictionary<string, APTemplate>(comparer);

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
            //We'll include hydrogen engines in this, so the conditional is going to be a little long.
            tally = new TallyGeneric(_meterMaid, "Hydrogen", new GasHandler(), highGood);
            templates.Add(tally.programName, new TallyGenericTemplate(tally.programName, tally, b =>
                (b is IMyGasTank && (b.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(HYDROGEN_ID) ?? _FALSE)) ||
                (b is IMyPowerProducer && (b.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(HYDROGEN_ID) ?? _FALSE))));
            //Oxygen
            tally = new TallyGeneric(_meterMaid, "Oxygen", new GasHandler(), highGood);
            templates.Add(tally.programName, new TallyGenericTemplate(tally.programName, tally, b => b is IMyGasTank &&
                (b.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(OXYGEN_ID) ?? _FALSE)));
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
            actionSet = new ActionSet("Antennas", _FALSE);
            actionSet.displayName = "Antenna\nRange";
            actionSet.textOn = "Broad";
            actionSet.textOff = "Wifi";
            actionSet.colorOff = yellow;
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyRadioAntenna,
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", $"{_customID}", $"{_customID} Wifi", writeAntennaSection, "Off", "On"));
            //Beacons
            actionSet = new ActionSet("Beacons", _FALSE);
            actionSet.displayName = "Beacon";
            actionSet.textOn = "Online";
            actionSet.textOff = "Offline";
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyBeacon,
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "Off", "On"));
            //Spotlights
            actionSet = new ActionSet("Spotlights", _FALSE);
            actionSet.textOn = "Online";
            actionSet.textOff = "Offline";
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyReflectorLight,
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "Off", ""));
            //OreDetectors
            actionSet = new ActionSet("OreDetectors", _FALSE);
            actionSet.displayName = "Ore\nDetector";
            actionSet.textOn = "Scanning";
            actionSet.textOff = "Idle";
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyOreDetector,
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "Off", "On"));
            //Batteries
            actionSet = new ActionSet("Batteries", _FALSE);
            actionSet.textOn = "On Auto";
            actionSet.textOff = "Recharging";
            actionSet.colorOff = yellow;
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyBatteryBlock,
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "BatteryAuto", "BatteryRecharge", writeDiscreteSection, "Off", "On"));
            //Reactors
            actionSet = new ActionSet("Reactors", _FALSE);
            actionSet.textOn = "Active";
            actionSet.textOff = "Inactive";
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyReactor,
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "Off", ""));
            //EnginesHydrogen
            actionSet = new ActionSet("EnginesHydrogen", _FALSE);
            actionSet.displayName = "Engines";
            actionSet.textOn = "Running";
            actionSet.textOff = "Idle";
            //Hydrogen engines don't have a bespoke interface, and they're just difficult all around. 
            //But we want to be able to find them, so we look for power producers that consume hydrogen.
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyPowerProducer &&
                (b.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(HYDROGEN_ID) ?? _FALSE),
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "Off", ""));
            //h2/02 generators, AKA IceCrackers
            actionSet = new ActionSet("IceCrackers", _FALSE);
            actionSet.displayName = "Ice Crackers";
            actionSet.textOn = "Running";
            actionSet.textOff = "Idle";
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyGasGenerator,
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "", ""));
            //TanksHydrogen
            actionSet = new ActionSet("TanksHydrogen", _FALSE);
            actionSet.displayName = "Hydrogen\nTanks";
            actionSet.textOn = "Open";
            actionSet.textOff = "Filling";
            actionSet.colorOff = lightBlue;
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyGasTank &&
                (b.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(HYDROGEN_ID) ?? _FALSE),
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "TankStockpileOff", "TankStockpileOn", writeDiscreteSection, "Off", "On"));
            //TanksOxygen
            actionSet = new ActionSet("TanksOxygen", _FALSE);
            actionSet.displayName = "Oxygen\nTanks";
            actionSet.textOn = "Open";
            actionSet.textOff = "Filling";
            actionSet.colorOff = lightBlue;
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyGasTank &&
                (b.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(OXYGEN_ID) ?? _FALSE),
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "TankStockpileOff", "TankStockpileOn", writeDiscreteSection, "Off", "On"));
            //Gyros
            actionSet = new ActionSet("Gyroscopes", _FALSE);
            actionSet.displayName = "Gyros";
            actionSet.textOn = "Active";
            actionSet.textOff = "Inactive";
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyGyro,
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "Off", "On"));
            /* KEEP - It was a good idea at the time. Ultimately, this didn't work becauese Keen has
             * implemented some kind of optimization where only one thruster (Probably the eldest) 
             * of a given useage gets a ResourceSinkComponent. If you use all of the same type of
             * thruster, AP handles it fine. But because AP treats each SubTypeID as novel, all it
             * knows is it's encountering a new kind of thruster that it doesn't have config for and
             * doesn't have whatever criteria you're looking for.
            //ThrustersElectric
            actionSet = new ActionSet("ThrustersElectric", _FALSE);
            actionSet.displayName = "Electric\nThrusters";
            actionSet.textOn = "Online";
            actionSet.textOff = "Offline";
            //Thrusters are also difficult, because we don't really have a way to tell them apart 
            //(Outside of the IDs), and there's apparently no way to tell ion and atmo thrusters
            //apart without some form of string parsing. So instead, we'll put those two in the same
            //group, and define that group as, 'thrusters that don't use hydrogen'
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyThrust &&
                (!(b.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(HYDROGEN_ID)) ?? _FALSE),
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "Off", "On"));
            //ThrustersHydrogen
            actionSet = new ActionSet("ThrustersHydrogen", _FALSE);
            actionSet.displayName = "Hydrogen\nThrusters";
            actionSet.textOn = "Online";
            actionSet.textOff = "Offline";
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyThrust &&
                (b.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(HYDROGEN_ID) ?? _FALSE),
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "Off", "On"));
            */

            //ThrustersAtmo
            //To replace ThrustersElectric and ThrustersHydrogen, we'll switch out the ResourceSink 
            //check for string parsing the SubTypeID.
            actionSet = new ActionSet("ThrustersAtmospheric", _FALSE);
            actionSet.displayName = "Atmospheric\nThrusters";
            actionSet.textOn = "Online";
            actionSet.textOff = "Offline";
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyThrust &&
                (b.BlockDefinition.SubtypeId.Contains("Atmospheric")), $"{_SCRIPT_PREFIX}.{actionSet.programName}",
                "EnableOn", "EnableOff", writeDiscreteSection, "Off", "On"));

            //ThrustersIon
            //The trouble with Ion thrusters is that none of them have 'ion' in the SubType ID. That
            //means that the only way to 'identify' an Ion Thruster is to say, 'thrusters that don't
            //have Atmospheric or Hydrogen in the name.'
            //I expect false positives.
            actionSet = new ActionSet("ThrustersIon", _FALSE);
            actionSet.displayName = "Ion\nThrusters";
            actionSet.textOn = "Online";
            actionSet.textOff = "Offline";
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyThrust &&
                (!b.BlockDefinition.SubtypeId.Contains("Atmospheric") && !b.BlockDefinition.SubtypeId.Contains("Hydrogen")),
                $"{_SCRIPT_PREFIX}.{actionSet.programName}", "EnableOn", "EnableOff", writeDiscreteSection, "Off", "On"));

            //ThrustersHydro
            actionSet = new ActionSet("ThrustersHydrogen", _FALSE);
            actionSet.displayName = "Hydrogen\nThrusters";
            actionSet.textOn = "Online";
            actionSet.textOff = "Offline";
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyThrust &&
                (b.BlockDefinition.SubtypeId.Contains("Hydrogen")), $"{_SCRIPT_PREFIX}.{actionSet.programName}",
                "EnableOn", "EnableOff", writeDiscreteSection, "Off", "On"));

            //ThrustersGeneric
            actionSet = new ActionSet("ThrustersGeneric", _FALSE);
            actionSet.displayName = "Thrusters";
            actionSet.textOn = "Online";
            actionSet.textOff = "Offline";
            templates.Add(actionSet.programName, new ActionSetTemplate(actionSet.programName, actionSet, b => b is IMyThrust,
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
                        $"be matched to declarations: {orphanedExclusions}.");
                }
            }
            return templates.Values.ToList();
        }

        public abstract class APTemplate
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

            public string writePBConfig()
            { return declaration.writeConfig(); }

            public bool blockMatchesConditional(IMyTerminalBlock block)
            { return conditional(block); }

            public abstract string getDeclarationType();
        }

        //An extension of APTemplates specifically for Tallies.
        public class TallyGenericTemplate : APTemplate
        {
            //Tally generic templates don't have a lot to them.
            public TallyGenericTemplate(string name, IHasConfig declaration, Func<IMyTerminalBlock, bool> conditional)
                : base(name, declaration, conditional)
            { }

            public override string getDeclarationType()
            { return "Tally"; }
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

            public override string getDeclarationType()
            { return "Tally"; }
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
            //Also, I don't acutally use it anymore because I've switched to a more string-based
            //solution
            /*
            internal void loadPlansIntoRoostSet(ref ActionSet roostSet)
            {
                ActionPlanActionSet plan = null;
                if (!string.IsNullOrEmpty(stateWhenRoostOn))
                {
                    plan = new ActionPlanActionSet((ActionSet)declaration);
                    if (stateWhenRoostOn == "On")
                    { plan.setReactionToOn(_TRUE); }
                    else
                    { plan.setReactionToOn(_FALSE); }
                    roostSet.addActionPlan(plan);
                }
                if (!string.IsNullOrEmpty(stateWhenRoostOff))
                {
                    plan = new ActionPlanActionSet((ActionSet)declaration);
                    if (stateWhenRoostOff == "On")
                    { plan.setReactionToOff(_TRUE); }
                    else
                    { plan.setReactionToOff(_FALSE); }
                    roostSet.addActionPlan(plan);
                }
            }*/

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

            public override string getDeclarationType()
            { return "ActionSet"; }
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

            private void createNewConfigEntry(string key, string initialValue, bool isModified = _FALSE)
            { config.Add(key, new APConfigEntry(initialValue, isModified)); }

            //Creates an entry in the config dictionary from an entry in a MyIni parse, if the parse
            //actually contains the entry in question.
            //Returns true if the requested entry was found, false if it wasn't.
            private bool tryCreateConfigEntryFromIni(MyIni ini, string section, string key)
            {
                if (ini.ContainsKey(section, key))
                {
                    createNewConfigEntry(key, ini.Get(section, key).ToString());
                    return _TRUE;
                }
                return _FALSE;
            }

            public void addLink(string key, string link)
            {
                if (config.ContainsKey(key))
                { config[key].addLink(link); }
                else
                //A new entry will need to be printed, so we'll set it as modified.
                { createNewConfigEntry(key, link, _TRUE); }
            }

            public void writeConfigToIni(string section, MyIni ini, bool forceWrite = _FALSE)
            {
                foreach (KeyValuePair<string, APConfigEntry> pair in config)
                { pair.Value.writeEntryToIni(ini, section, pair.Key, forceWrite); }
                //If we've been holding on to a template, tell it to write its discrete section. 
                if (template != null)
                { template.writeDiscreteEntry(ini); }
            }
        }

        public class APConfigEntry
        {
            public string links { get; private set; }
            bool isModified;

            public APConfigEntry(string initialValue, bool startsModified = _FALSE)
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
                    isModified = _TRUE;
                }
            }

            //Overwrites an ini entry at the designated section and key, but only if this entry has
            //been modified (Or we're explicitly telling it to)
            public void writeEntryToIni(MyIni ini, string section, string key, bool forceWrite)
            {
                if (isModified || forceWrite)
                { ini.Set(section, key, links); }
            }
        }

        public void evaluateFull(LimitedMessageLog textLog, bool firstRun = _FALSE)
        {
            //We'll need a bunch of dictionaries and other lists to move data between the various 
            //sub-evaluates
            //We want our dictionaries to be case agnostic, and this is the comparer to make it happen.
            StringComparer comparer = StringComparer.OrdinalIgnoreCase;
            PaletteManager colorPalette = new PaletteManager(comparer);
            /*
            Dictionary<string, IColorCoder> colorPalette = new Dictionary<string, IColorCoder>(comparer);
            compileColors(colorPalette);
            */
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

            //_debugDisplay.WriteText("Beginning evaluation\n");
            //We'll go ahead and get a parse from the Storage string. 
            //The storage string can't be directly altered by the user, and it isn't a huge loss if
            //we can't read it, so we simply assume that it parsed correctly
            _iniRead.TryParse(Storage);

            //Now that we have that, we'll go ahead and get whatever update delay we had stored
            //_distributor.setDelay(_iniRead.Get("Data", "UpdateDelay").ToInt32(0));
            int updateDelay = _iniRead.Get("Data", "UpdateDelay").ToInt32(0);
            //We'll get the refreshFrequency during EvaluateInit, but we won't actually use i until
            //the end of this method.
            int refreshFrequency = -1;

            //Parse the PB's custom data. If it checks out, we can proceed.
            if (!_iniReadWrite.TryParse(Me.CustomData, out parseResult))
            //If we didn't get a useable parse, file a complaint.
            {
                textLog.addError($"The parser encountered an error on line {parseResult.LineNo} of the " +
                    $"Programmable Block's config: {parseResult.Error}");
            }
            else
            {
                //_debugDisplay.WriteText("Entering evaluateInit\n", _TRUE);
                evaluateInit(colorPalette, textLog, iniValue, out refreshFrequency);
                //_debugDisplay.WriteText("Entering evaluateDeclarations\n", _TRUE);
                evaluateDeclarations(Me, textLog, colorPalette, evalTallies, evalSets, evalTriggers,
                    evalRaycasters, usedElementNames, parseResult, iniValue);

                //_debugDisplay.WriteText("Deciding to evaluateGrid\n", _TRUE);
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
                    //_debugDisplay.WriteText("Decision made to evaluateGrid\n", _TRUE);
                    blockCount = evaluateGrid(textLog, colorPalette, evalTallies, evalSets, evalTriggers,
                        evalRaycasters, evalContainers, evalMFDs, evalReports, evalLogReports,
                        evalIndicators, parseResult, iniValue);
                }
            }

            //_debugDisplay.WriteText("Config evaluation complete\n", _TRUE);
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
                //_debugDisplay.WriteText("Entered error-free wrap-up\n", _TRUE);
                //_debugDisplay.WriteText($"Warning listing:\n{textLog.warningsToString()}\n", _TRUE);
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
                //_debugDisplay.WriteText("Finished containers\n", _TRUE);
                //Next, tear down the complicated data structures we've been using for evaluation into
                //the arrays we'll be using during execution
                _tallies = evalTallies.Values.ToArray();
                _triggers = evalTriggers.Values.ToArray();
                _reports = evalReports.ToArray();
                _indicators = evalIndicators.Values.ToArray();
                //_debugDisplay.WriteText("Finished array conversion\n", _TRUE);
                //In some cases, we'll port the contents of the eval dictionaries directly to the globals
                _sets = evalSets;
                _raycasters = evalRaycasters;
                _MFDs = evalMFDs;
                //_debugDisplay.WriteText("Finished dictionary hand-over\n", _TRUE);
                //There's one more step before the tallies are ready. We need to tell them that they
                //have all the data that they're going to get. 
                foreach (Tally finishTally in _tallies)
                { finishTally.finishSetup(); }
                //_debugDisplay.WriteText("Finished finishSetup tally calls\n", _TRUE);
                //We'll take this opportunity to call setProfile on all our Reportables
                foreach (IReportable reportable in _reports)
                { reportable.setProfile(); }
                //{ reportable.setProfile(); }
                //_debugDisplay.WriteText("Finished setProfile calls\n", _TRUE);

                //We need to clear up a few peristant bits that may or may not be coming over from
                //the previous script instance. First, any leftover state machines
                _activeMachine?.end();
                _activeMachine = null;
                _scheduledMachines.Clear();
                //We won't mess with Update10. At the end of this, we're setting Update100, which
                //will do the job for us.

                //One of the last things we need to do is set up the Distributor.
                _distributor.clearPeriodics();
                setUpdateDelay(updateDelay);
                //A value of -1 indicates the sprite refresher is disabled
                if (refreshFrequency > -1)
                {
                    //We don't let the user set the refresh frequency lower than 10.
                    if (refreshFrequency < 10)
                    {
                        textLog.addWarning($"{_SCRIPT_PREFIX}.Init, key 'MPSpriteSyncFrequency' " +
                            $"requested an invalid frequncy of {refreshFrequency}. Sync frequency has " +
                            $"been set to the lowest allowed value of 10 instead.");
                        refreshFrequency = 10;
                    }
                    Action refreshAction = () =>
                    {
                        tryScheduleMachine(new SpriteRefreshMachine(this, _reports, _FALSE));
                        //DEBUG USE
                        //_log.add("Sprite refresh scheduled.");
                    };
                    PeriodicEvent refreshEvent = new PeriodicEvent(refreshFrequency, refreshAction);
                    _distributor.addOrReplacePeriodic("SpriteRefresher", refreshEvent);
                }

                //Record this occasion for posterity
                _haveGoodConfig = _TRUE;
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

            //_debugDisplay.WriteText("Evaluation wrap-up complete:\n", _TRUE);

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

            //_debugDisplay.WriteText("Exit evaluation\n", _TRUE);

            //DEBUG USE
            /*
            List<string> colorNames = colorPalette.Keys.ToList();
            string palettePrint = $"Palette contains {colorNames.Count} colorCoders:\n";
            foreach (string name in colorNames) 
            { palettePrint += $"{name}\n";}
            textLog.addNote(palettePrint);
            */
            /*
            _debugDisplay.WriteText("colorPalette getConfigPart:\n", _TRUE);
            foreach (IColorCoder coder in colorPalette.Values)
            { _debugDisplay.WriteText($"  {coder.getConfigPart()}\n", _TRUE); }
            _debugDisplay.WriteText("Tally ColorCoder getConfigPart:\n", _TRUE);
            foreach (Tally tally in evalTallies.Values)
            { _debugDisplay.WriteText($"  {tally.colorCoder.getConfigPart()}\n", _TRUE); }
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

        //Assumes the PB's CustomData has been loaded into _iniReadWrite
        internal void evaluateInit(PaletteManager colorPalette, LimitedMessageLog textLog,
            MyIniValue iniValue, out int refreshFrequency)
        {
            ensureInitHasRequiredKeys(textLog);

            Color color;
            //IColorCoder colorCoder = null;
            string initTag = $"{_SCRIPT_PREFIX}.Init";
            Action<string> troubleLogger = message =>
            { textLog.addError($"{initTag}{message}"); };
            //Retrieve direct references to our main color coders so we can modify them
            ColorCoderLow lowGood = (ColorCoderLow)(colorPalette.getCoderDirect("LowGood"));
            ColorCoderHigh highGood = (ColorCoderHigh)(colorPalette.getCoderDirect("HighGood"));
            Action<string> setThresholdColor = thresholdName =>
            {
                if (colorPalette.tryGetColorFromConfig(troubleLogger, _iniReadWrite, initTag,
                    $"Color{thresholdName}", out color))
                {
                    lowGood.tryAssignColorByName(thresholdName, color);
                    highGood.tryAssignColorByName(thresholdName, color);
                }
            };
            setThresholdColor("Optimal");
            setThresholdColor("Normal");
            setThresholdColor("Caution");
            setThresholdColor("Warning");
            setThresholdColor("Critical");
            //<<<The functionallity from these calls has only been partially duplicated in 
            //ensureInitHasRequiredKeys.
            /*
            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue,
                lowGood, highGood, initTag, "Optimal", "Green", logKeyGeneration, ref configAltered);
            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue,
                lowGood, highGood, initTag, "Normal", "LightBlue", logKeyGeneration, ref configAltered);
            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue,
                lowGood, highGood, initTag, "Caution", "Yellow", logKeyGeneration, ref configAltered);
            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue,
                lowGood, highGood, initTag, "Warning", "Orange", logKeyGeneration, ref configAltered);
            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue,
                lowGood, highGood, initTag, "Critical", "Red", logKeyGeneration, ref configAltered);*/

            //(20230426) This is for color-coding text in the PB's DetailInfo.
            //It also might not be working. When I tested it, the color seemed a lot like a blue even
            //though we should've been retrieving a red.
            //textLog.errorColor = color;

            //The other thing we need to read from Init is the refresh frequency. But we'll just hand
            //that off to the main method
            refreshFrequency = _iniReadWrite.Get(initTag, "MPSpriteSyncFrequency").ToInt32(-1);
        }

        //assume that _iniRead has been loaded with a parse from the Save string and that _iniReadWrite
        //has been loaded with a parse from the PB (Or the block we're reading declarations from)
        internal void evaluateDeclarations(IMyTerminalBlock sourceBlock, LimitedMessageLog textLog,
            PaletteManager colorPalette, Dictionary<string, Tally> evalTallies,
            Dictionary<string, ActionSet> evalSets, Dictionary<string, Trigger> evalTriggers,
            Dictionary<string, Raycaster> evalRaycasters, HashSet<string> usedElementNames,
            MyIniParseResult parseResult, MyIniValue iniValue)
        {
            string declarationType = "";
            string declarationName = "";
            Color color = Color.Black;
            ColorCoderLow lowGood = (ColorCoderLow)(colorPalette.getCoderDirect("LowGood"));
            ColorCoderHigh highGood = (ColorCoderHigh)(colorPalette.getCoderDirect("HighGood"));
            IColorCoder colorCoder;
            List<string> raycasterTallySectionHeaders = new List<string>();
            List<TallyGeneric> raycasterTallies = new List<TallyGeneric>();
            //The troubleLogger we'll use to add errors to the textLog
            //TODO (20240730) Come back and replace references to this old error logger with refrences 
            //to the new, more capable logger. And alter the things it plugged into to deliver half-messages.
            Action<string> errorLoggerOLD = b => textLog.addError(b);
            Action<string> declarationErrorLogger = message =>
            { textLog.addError($"{declarationType} {declarationName}{message}"); };
            StringComparison compareMode = StringComparison.OrdinalIgnoreCase;
            //int index = 0;
            //_debugDisplay.WriteText("Initial Tally parse\n", _TRUE);
            //We make multiple (Two, at the moment) passes through the ActionSet configuration. We
            //need a list to store loaded ActionSets - in the order we read them - for later access,
            //as opposed to dumping them into the finished ActionSet pile.
            //(20240618) Because we no longer have to generate a specific index for 
            //section headers in the second pass, this is no longer required.
            //List<ActionSet> loadedActionSets = new List<ActionSet>();

            //We're going to add something of a third pass, specifically to cull triggers and 
            //raycasters that don't get all the information they need to be functional objects.
            //The checks to determine if the object is flawed will happen earlier in evaluate, but
            //we hold on to those objects until after the second ActionSet pass so it doesn't get
            //confused as to why the objects its looking for are gone.
            List<string> flawedTriggers = new List<string>();
            List<string> flawedRaycasters = new List<string>();

            List<string> sectionHeaders = new List<string>();
            string[] splitHeader;
            _iniReadWrite.GetSections(sectionHeaders);

            foreach (string sectionHeader in sectionHeaders)
            {
                splitHeader = sectionHeader.Split('.').Select(p => p.Trim()).ToArray();
                //All declarations have four elements. So if we've got four elements and a Shipware
                //prefix, we'll assume it's a declaration.
                if (splitHeader.Length == 4 && String.Equals(splitHeader[0], _SCRIPT_PREFIX, compareMode))
                {
                    declarationType = splitHeader[2];
                    declarationName = splitHeader[3];

                    //Tallies are up first. From Tally sections, we read:
                    //  DisplayName: A name that will be shown on screens instead of the program name
                    //  Type: The type of this tally. Cargo or Item are common, and there are a number
                    //    TallyGenerics like Battery, Gas, and PowerCurrent.
                    //  ItemTypeID: For TallyItems, the ID that will be fed into MyItemType
                    //  ItemSubyTypeID: For TallyItems, the sub type ID that will be fed into MyItemType
                    //  Max: A user-definable value that will be used in place of the evaluate-calculated
                    //    max. Required for some tallies, like TallyItems
                    //  Multiplyer: The multiplier that will be applied to curr and max of this tally. 
                    //  ColorCoder: The color coding scheme this tally will use. Can be lowGood, highGood,
                    //    or any color if the color should not change based on the tally's value.
                    if (declarationType.Equals("Tally", compareMode))
                    {
                        Tally tally = null;
                        string tallyType;
                        //Our next steps are going to be dictated by the TallyType. We should try and 
                        //figure out what that is.
                        tallyType = _iniReadWrite.Get(sectionHeader, "Type").ToString().ToLowerInvariant();
                        if (isEmptyString(tallyType))
                        { textLog.addError($"{declarationType} {declarationName} has a missing or unreadable Type."); }
                        //Now, we create a tally based on the type. For the TallyCargo, that's quite straightforward.
                        else if (tallyType == "inventory")
                        { tally = new TallyCargo(_meterMaid, declarationName, lowGood); }
                        //Creating a TallyItem is a bit more involved.
                        else if (tallyType == "item")
                        {
                            string typeID, subTypeID;
                            //We'll need a typeID and a subTypeID, and we'll need to complain if we can't
                            //get them
                            typeID = _iniReadWrite.Get(sectionHeader, "ItemTypeID").ToString();
                            if (isEmptyString(typeID))
                            { textLog.addError($"{declarationType} {declarationName} has a missing or unreadable ItemTypeID."); }
                            subTypeID = _iniReadWrite.Get(sectionHeader, "ItemSubTypeID").ToString();
                            if (isEmptyString(subTypeID))
                            { textLog.addError($"{declarationType} {declarationName} has a missing or unreadable ItemSubTypeID."); }
                            //If we have the data we were looking for, we can create a TallyItem
                            if (!isEmptyString(typeID) && !isEmptyString(subTypeID))
                            { tally = new TallyItem(_meterMaid, declarationName, typeID, subTypeID, highGood); }
                        }
                        //On to the TallyGenerics. We'll start with Batteries
                        else if (tallyType == "battery")
                        { tally = new TallyGeneric(_meterMaid, declarationName, new BatteryHandler(), highGood); }
                        //Gas, which works for both Hydrogen and Oxygen
                        else if (tallyType == "gas")
                        { tally = new TallyGeneric(_meterMaid, declarationName, new GasHandler(), highGood); }
                        else if (tallyType == "jumpdrive")
                        { tally = new TallyGeneric(_meterMaid, declarationName, new JumpDriveHandler(), highGood); }
                        else if (tallyType == "raycast")
                        {
                            tally = new TallyGeneric(_meterMaid, declarationName, new RaycastHandler(), highGood);
                            //Raycasters can get some of their information from other script objects,
                            //but those objects haven't been initiated yet. So we'll store the data we'll
                            //need to make decisions about this later.
                            raycasterTallySectionHeaders.Add(sectionHeader);
                            raycasterTallies.Add((TallyGeneric)tally);
                        }
                        else if (tallyType == "powermax")
                        { tally = new TallyGeneric(_meterMaid, declarationName, new PowerMaxHandler(), highGood); }
                        else if (tallyType == "powercurrent")
                        { tally = new TallyGeneric(_meterMaid, declarationName, new PowerCurrentHandler(), highGood); }
                        else if (tallyType == "integrity")
                        { tally = new TallyGeneric(_meterMaid, declarationName, new IntegrityHandler(), highGood); }
                        else if (tallyType == "ventpressure")
                        { tally = new TallyGeneric(_meterMaid, declarationName, new VentPressureHandler(), highGood); }
                        else if (tallyType == "pistonextension")
                        { tally = new TallyGeneric(_meterMaid, declarationName, new PistonExtensionHandler(), highGood); }
                        else if (tallyType == "rotorangle")
                        { tally = new TallyGeneric(_meterMaid, declarationName, new RotorAngleHandler(), highGood); }
                        else if (tallyType == "controllergravity")
                        { tally = new TallyGeneric(_meterMaid, declarationName, new ControllerGravityHandler(), highGood); }
                        else if (tallyType == "controllerspeed")
                        { tally = new TallyGeneric(_meterMaid, declarationName, new ControllerSpeedHandler(), highGood); }
                        else if (tallyType == "controllerweight")
                        { tally = new TallyGeneric(_meterMaid, declarationName, new ControllerWeightHandler(), highGood); }
                        //TODO: Aditional TallyTypes go here
                        else
                        {
                            //If we've gotten to this point, the user has given us a type that we can't 
                            //recognize. Scold them.
                            textLog.addError($"{declarationType} {declarationName} has un-recognized Type of '{tallyType}'.");
                        }
                        //If we've gotten to this point and we haven't put together enough information
                        //to make a proper tally, make a fake one using whatever data we have on hand.
                        //This will allow us to continue evaluation.
                        //(This fires if we can't read the tally type or we don't recognize the tally type)
                        if (tally == null)
                        { tally = new TallyCargo(_meterMaid, declarationName, lowGood); }

                        //Now that we have our tally, we need to check to see if there's any further
                        //configuration data. 
                        //First, the DisplayName
                        iniValue = _iniReadWrite.Get(sectionHeader, "DisplayName");
                        if (!iniValue.IsEmpty)
                        { tally.displayName = iniValue.ToString(); }
                        //Up next is the Multiplier. Note that, because of how forceMax works, the multiplier
                        //must be applied before the max.
                        iniValue = _iniReadWrite.Get(sectionHeader, "Multiplier");
                        if (!iniValue.IsEmpty)
                        { tally.multiplier = iniValue.ToDouble(); }
                        //Then the Max
                        iniValue = _iniReadWrite.Get(sectionHeader, "Max");
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
                            textLog.addError($"{declarationType} {declarationName}'s TallyType of '{tallyType}' requires a Max " +
                                $"to be set in configuration.");
                        }
                        //Last step is to check for a custom ColorCoder
                        if (colorPalette.tryGetCoderFromConfig(declarationErrorLogger, _iniReadWrite,
                            sectionHeader, "ColorCoder", out colorCoder))
                        { tally.colorCoder = colorCoder; }

                        //Thats all the possible tally config. Our last step is to make sure the tally's 
                        //name isn't going to cause problems
                        if (!isElementNameInUse(usedElementNames, tally.programName, sectionHeader, textLog))
                        {
                            //If the name checks out, go ahead and add the tally to our tally dictionary.
                            evalTallies.Add(tally.programName, tally);
                            //And add the name to our list of in-use Element names
                            usedElementNames.Add(tally.programName);
                        }
                    }

                    //ActionSets are up next. On this pass of ActionSet config, we read:
                    //  DisplayName: A name that will be shown on screens instead of the set name
                    //  ColorOn: The color that will be used for the set's element when it is on
                    //  ColorOff: The color that will be used for the set's element when it is off
                    //  TextOn: The text that will be diplsayed on the set's element when it is on
                    //  TextOff: The text that will be diplsayed on the set's element when it is off
                    //This is actually all the config that an ActionSet object holds. The remaining 
                    //(multitude) of keys you sometimes see in config set up ActionPlans that manipulate
                    //other script objects. We read those once we're sure all the possible script object
                    //have been initialized.
                    else if (declarationType.Equals("ActionSet", compareMode))
                    {
                        //ActionSets have a lot less going on than tallies, initially at least. We 
                        //need their name, but the only other thing we kinda want to know about them 
                        //is what their previous state was.
                        //We'll try to get that from the storage string, defaulting to false if we can't
                        //The extra defensiveness here is for situations where we might be dealing with 
                        //non-PB config or a new ActionSet.
                        bool state = _iniRead?.Get("ActionSets", declarationName).ToBoolean(_FALSE) ?? _FALSE;
                        ActionSet set = new ActionSet(declarationName, state);
                        //There are a few other bits of configuration that ActionSets may have
                        iniValue = _iniReadWrite.Get(sectionHeader, "DisplayName");
                        if (!iniValue.IsEmpty)
                        { set.displayName = iniValue.ToString(); }
                        if (colorPalette.tryGetColorFromConfig(declarationErrorLogger, _iniReadWrite,
                            sectionHeader, "ColorOn", out color))
                        { set.colorOn = color; }
                        if (colorPalette.tryGetColorFromConfig(declarationErrorLogger, _iniReadWrite,
                            sectionHeader, "ColorOff", out color))
                        { set.colorOff = color; }
                        iniValue = _iniReadWrite.Get(sectionHeader, "TextOn");
                        if (!iniValue.IsEmpty)
                        { set.textOn = iniValue.ToString(); }
                        iniValue = _iniReadWrite.Get(sectionHeader, "TextOff");
                        if (!iniValue.IsEmpty)
                        { set.textOff = iniValue.ToString(); }
                        //That's it. We should have all the initial configuration for this ActionSet.

                        //This process is functionally identical to what we did for Tallies.
                        if (!isElementNameInUse(usedElementNames, set.programName, sectionHeader, textLog))
                        {
                            //We might have changed what the set uses for status text or colors. A call to 
                            //evaluateStatus will set things right.
                            set.evaluateStatus();
                            //This ActionSet should be ready. Pass it to the dictionary.
                            evalSets.Add(set.programName, set);
                            //We'll need an ordered list of ActionSets for our second pass. Also add this 
                            //set to that list.
                            //(20240618) Because we no longer have to generate a specific index for 
                            //section headers in the second pass, this is no longer required.
                            //loadedActionSets.Add(set);
                            usedElementNames.Add(set.programName);
                        }
                    }

                    //From the initial pass of trigger configuration, we read:
                    //Basically Nothing: We'll take the name of the object from the header, then check
                    //the storage string to see if we can find an initial state for it. But because 
                    //we can't be sure that we've read the tally or the ActionSet that the trigger 
                    //refers to, we'll need to wait for the second pass to do most of the work.
                    else if (declarationType.Equals("Trigger", compareMode))
                    {
                        //Triggers can be armed or disarmed, and this state persists through loads much
                        //like ActionSets. We'll try to figure out if this trigger is supposed to be
                        //armed or disarmed, arming it if we can't tell.
                        bool state = _iniRead?.Get("Triggers", declarationName).ToBoolean(_TRUE) ?? _TRUE;
                        Trigger trigger = new Trigger(declarationName, state);
                        //If I decide to allow customization of Trigger elements, that would go here.

                        //And that's it. Everything else will have to wait for the second pass. Last 
                        //thing to do here is to check the name.
                        if (!isElementNameInUse(usedElementNames, trigger.programName, sectionHeader, textLog))
                        {
                            evalTriggers.Add(trigger.programName, trigger);
                            usedElementNames.Add(trigger.programName);
                        }
                    }

                    //Raycasters are now considered full script objects, with the powers and responsibilities
                    //thereof. From Raycaster configuration, we read:
                    //Name: The Element name of this Raycaster
                    //Type: What type of Raycaster this is
                    else if (declarationType.Equals("Raycaster", compareMode))
                    {
                        Raycaster raycaster = new Raycaster(_sb, declarationName);
                        RaycasterModuleBase scanModule = null;
                        string[] moduleConfigurationKeys = null;
                        double[] moduleConfigurationValues = null;
                        string raycasterType = _iniReadWrite.Get(sectionHeader, "Type").ToString().ToLowerInvariant();
                        if (isEmptyString(raycasterType))
                        { textLog.addError($"{declarationType} {declarationName} has a missing or unreadable Type."); }
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
                        { textLog.addError($"{declarationType} {declarationName} has un-recognized Type of '{raycasterType}'."); }
                        //Scanning modules have their own configuration, but they tell us everything we 
                        //need to get that configuration for them. They also handle default values on
                        //their end, so we can basically force-feed them raw config.
                        if (scanModule != null)
                        {
                            for (int i = 0; i < moduleConfigurationKeys.Length; i++)
                            {
                                moduleConfigurationValues[i] =
                                    _iniReadWrite.Get(sectionHeader, moduleConfigurationKeys[i]).ToDouble(-1);
                            }
                            //Send retrieved configuration to the scanning module
                            scanModule.configureModuleByArray(moduleConfigurationValues);
                            //We should have everything we need to make a new Raycaster.
                            raycaster = new Raycaster(_sb, scanModule, declarationName);
                        }
                        else
                        //If there's no scan module, this raycaster can't function, and trying to
                        //use it would probably cause a crash (Because there's no null checking out
                        //that way). We're going to keep this object for now, so the second pass on
                        //tallies can operate normally. But after that, we'll remove this object from 
                        //the working dictionary.
                        { flawedRaycasters.Add(declarationName); }
                        //Even if we ended up with a flawed raycaster, we're going to behave as if 
                        //we didn't. So next up is the name check.
                        if (!isElementNameInUse(usedElementNames, declarationName, sectionHeader, textLog))
                        {
                            evalRaycasters.Add(raycaster.programName, raycaster);
                            usedElementNames.Add(declarationName);
                        }
                    }
                    else
                    //If we get here, we have something that is Shipware config, that has four elements,
                    //but we don't recognize its type. That's grounds for issuing an error.
                    {
                        if (splitHeader[1] == _DECLARATION_PREFIX)
                        //This is definately a declaration, there's probaby something wrong with the
                        //specified type.
                        {
                            textLog.addError($"{sectionHeader} referenced the unknown declaration " +
                                $"type '{declarationType}'.");
                        }
                        else
                        //This isn't even a declaration. How even did we get here?
                        {
                            textLog.addWarning($"{sectionHeader} has the format of a declaration " +
                                $"header but lacks the '{_DECLARATION_PREFIX}' prefix and has been " +
                                $"discarded.");
                        }
                    }
                }
                //else
                //This branch is where non-declarations on the PB would head to. 
                //{ }
            }

            //Now that we have at least a framework for all our script objects, we make a second pass
            //to create any nessecary links from one script object to another 

            //From this point onward, we'll be setting the declarationType at the head of each loop.
            declarationType = "Raycaster";
            //Our first step on the second pass is to try to link raycaster tallies with their raycasters.
            //_debugDisplay.WriteText("Tally Raycast pass\n", _TRUE);
            for (int i = 0; i < raycasterTallies.Count; i++)
            {
                string sectionHeader = raycasterTallySectionHeaders[i];
                TallyGeneric raycasterTally = raycasterTallies[i];

                //Go back to this tally's declaration section and check to see if there's a raycaster 
                //defined there
                iniValue = _iniReadWrite.Get(sectionHeader, "Raycaster");
                if (iniValue.IsEmpty)
                {
                    //It's fine if we didn't find a value for the Raycaster key... unless we haven't
                    //already forced the tally's max.
                    if (!raycasterTally.maxForced)
                    {
                        textLog.addError($"{declarationType} {raycasterTally.programName}'s " +
                            $"Type of 'Raycaster' requires either a Max or a linked Raycaster to " +
                            $"be set in configuration.");
                    }
                }
                else
                {
                    string raycasterName = iniValue.ToString();
                    //If we have a raycaster name, but max on the tally has already been forced, we
                    //have a conflict. Inform the user.
                    if (raycasterTally.maxForced)
                    {
                        textLog.addWarning($"{declarationType} {raycasterTally.programName} specifies " +
                            $"both a Max and a linked Raycaster, '{raycasterName}'. Only one of these " +
                            $"values is required. The linked Raycaster has been ignored.");
                    }
                    else
                    {
                        //If the string we just retrieved matches the name of one of the raycasters we've 
                        //already retrieved...
                        Raycaster raycaster;
                        if (evalRaycasters.TryGetValue(raycasterName, out raycaster))
                        { raycasterTally.forceMax(raycaster.getModuleRequiredCharge()); }
                        else
                        {
                            textLog.addError($"{declarationType} {raycasterTally.programName} tried " +
                                $"to reference the unconfigured Raycaster '{raycasterName}'.");
                        }
                    }
                }
            }

            //Next is the second pass on Triggers.
            //From the second pass of trigger configuration, we read:
            //Tally: The name of the Tally this trigger will watch
            //ActionSet: The name of the ActionSet this trigger will operate
            //LessOrEqualValue: When the watched Tally falls below this value, the commandLess will be sent
            //LessOrEqualCommand: The command to be sent when we're under the threshold
            //GreaterOrEqualValue: When the watched Tally exceeds this value, the commandGreater will be sent
            //GreaterOrEqualCommand: The command to be sent when we're over the threshold
            declarationType = "Trigger";
            foreach (Trigger trigger in evalTriggers.Values)
            {
                Tally tally = null;
                ActionSet set = null;
                declarationName = trigger.programName;
                string sectionHeader = $"{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.Trigger.{declarationName}";
                //We already know this section header is here, so we won't bother with any checks to 
                //make sure it's there.

                //A trigger needs to have two pieces of information: A Tally to watch, and an 
                //ActionSet to manipulate.
                iniValue = _iniReadWrite.Get(sectionHeader, "Tally");
                if (!iniValue.IsEmpty)
                {
                    string tallyName = iniValue.ToString();
                    //Try to match the tallyName to a configured Tally
                    if (evalTallies.TryGetValue(tallyName, out tally))
                    { trigger.targetTally = tally; }
                    else
                    {
                        textLog.addError($"{declarationType} {declarationName} tried to reference " +
                            $"the unconfigured Tally '{tallyName}'.");
                    }
                }
                else
                { textLog.addError($"{declarationType} {declarationName} has a missing or unreadable Tally."); }

                iniValue = _iniReadWrite.Get(sectionHeader, "ActionSet");
                if (!iniValue.IsEmpty)
                {
                    string setName = iniValue.ToString();
                    if (evalSets.TryGetValue(setName, out set))
                    { trigger.targetSet = set; }
                    else
                    {
                        textLog.addError($"{declarationType} {declarationName} tried to reference " +
                            $"the unconfigured ActionSet '{setName}'.");
                    }
                }
                else
                { textLog.addError($"{declarationType} {declarationName} has a missing or unreadable ActionSet."); }

                //The tally and the set are the most important pieces of information we need, but 
                //the trigger still needs to know when it should act on those.
                tryGetCommandFromConfig(trigger, sectionHeader, _TRUE, "LessOrEqual",
                    iniValue, textLog);
                tryGetCommandFromConfig(trigger, sectionHeader, _FALSE, "GreaterOrEqual",
                    iniValue, textLog);
                //If we didn't find at least one scenario for this trigger, we can assume that 
                //something is wrong.
                if (!trigger.hasScenario())
                {
                    textLog.addError($"{declarationType} {declarationName} does not define a valid " +
                        $"LessOrEqual or GreaterOrEqual scenario.");
                }

                //Handling for what to do if there's no scenario is baked in to the object. But there
                //isn't a way to deal with not have a tally or a set, so if that's happened, this
                //object will need to be culled in the third pass.
                if (tally == null || set == null)
                { flawedTriggers.Add(declarationName); }
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
            List<KeyValuePair<string, bool>> parsedStateList = new List<KeyValuePair<string, bool>>();
            declarationType = "ActionSet";
            //_debugDisplay.WriteText("ActionSet script object pass\n", _TRUE); 
            foreach (ActionSet set in evalSets.Values)
            {
                declarationName = set.programName;
                string sectionHeader = $"{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.ActionSet.{declarationName}";
                string errorTitle = $"{declarationType} {declarationName}";
                string targetKey, troubleID;
                ActionSet targetSet = null;
                Trigger targetTrigger = null;
                Raycaster targetRaycaster = null;

                //We'll start with ActionPlanUpdate.
                //DelayOn and DelayOff. These will actually be stored in an ActionPlan, but we
                //need to know if one of the values is present before we create the object.
                //If no value is found, a zero will be returned.
                int delayOn = _iniReadWrite.Get(sectionHeader, $"DelayOn").ToInt32();
                int delayOff = _iniReadWrite.Get(sectionHeader, $"DelayOff").ToInt32();
                //If one of the delay values isn't 0...
                if (delayOn != 0 || delayOff != 0)
                {
                    //Create a new action plan
                    ActionPlanUpdate updatePlan = new ActionPlanUpdate(this);
                    //Store the values we got. No need to run any checks here, they'll be fine
                    //if we pass them zeros
                    updatePlan.delayOn = delayOn;
                    updatePlan.delayOff = delayOff;
                    //Add the update plan to this ActionSet.
                    set.addActionPlan(updatePlan);
                }

                //ActionPlanIGC
                iniValue = _iniReadWrite.Get(sectionHeader, $"IGCChannel");
                if (!iniValue.IsEmpty)
                {
                    string channel = iniValue.ToString();
                    //Create a new action plan, using the string we collected as the channel
                    ActionPlanIGC igcPlan = new ActionPlanIGC(IGC, channel);
                    iniValue = _iniReadWrite.Get(sectionHeader, $"IGCMessageOn");
                    if (!iniValue.IsEmpty)
                    { igcPlan.messageOn = iniValue.ToString(); }
                    iniValue = _iniReadWrite.Get(sectionHeader, $"IGCMessageOff");
                    if (!iniValue.IsEmpty)
                    { igcPlan.messageOff = iniValue.ToString(); }
                    //Last step is to make sure we got some config
                    if (igcPlan.hasAction())
                    { set.addActionPlan(igcPlan); }
                    else
                    {
                        textLog.addError($"{errorTitle} has configuration for sending an IGC message " +
                            $"on the channel '{channel}', but does not have readable config on what " +
                            $"messages should be sent.");
                    }
                }

                //ActionPlanActionSet
                targetKey = "ActionSetsLinkedToOn";
                iniValue = _iniReadWrite.Get(sectionHeader, targetKey);
                if (!iniValue.IsEmpty)
                {
                    troubleID = $"{errorTitle}'s {targetKey} list";
                    parseStateList(iniValue.ToString(), troubleID, errorLoggerOLD, parsedStateList);
                    foreach (KeyValuePair<string, bool> pair in parsedStateList)
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
                iniValue = _iniReadWrite.Get(sectionHeader, targetKey);
                if (!iniValue.IsEmpty)
                {
                    troubleID = $"{errorTitle}'s {targetKey} list";
                    parseStateList(iniValue.ToString(), troubleID, errorLoggerOLD, parsedStateList);
                    foreach (KeyValuePair<string, bool> pair in parsedStateList)
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
                iniValue = _iniReadWrite.Get(sectionHeader, targetKey);
                if (!iniValue.IsEmpty)
                {
                    troubleID = $"{errorTitle}'s {targetKey} list";
                    parseStateList(iniValue.ToString(), troubleID, errorLoggerOLD, parsedStateList);
                    foreach (KeyValuePair<string, bool> pair in parsedStateList)
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
                iniValue = _iniReadWrite.Get(sectionHeader, targetKey);
                if (!iniValue.IsEmpty)
                {
                    troubleID = $"{errorTitle}'s {targetKey} list";
                    parseStateList(iniValue.ToString(), troubleID, errorLoggerOLD, parsedStateList);
                    foreach (KeyValuePair<string, bool> pair in parsedStateList)
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
                iniValue = _iniReadWrite.Get(sectionHeader, targetKey);
                if (!iniValue.IsEmpty)
                {
                    troubleID = $"{errorTitle}'s {targetKey} list";
                    parseStateList(iniValue.ToString(), troubleID, errorLoggerOLD, parsedStateList);
                    foreach (KeyValuePair<string, bool> pair in parsedStateList)
                    {
                        //Try to match the named raycaster to one of our configured raycasters
                        if (evalRaycasters.TryGetValue(pair.Key, out targetRaycaster))
                        {
                            ActionPlanRaycaster raycasterPlan = new ActionPlanRaycaster(targetRaycaster);
                            //Unlike other state lists, for raycasters, the boolean portion of each 
                            //element tells us if we perform the scan when the ActionSet is switched
                            //On, or if we perform the scan when it's switched Off.
                            if (pair.Value)
                            { raycasterPlan.scanOn = _TRUE; }
                            else
                            { raycasterPlan.scanOff = _TRUE; }
                            set.addActionPlan(raycasterPlan);
                        }
                        //If we can't match the key from this pair to an existing set, log an error.
                        else
                        { textLog.addError($"{troubleID} references the unconfigured Raycaster {pair.Key}."); }
                    }
                }
            }

            //The 'third pass' is just removing any objects we marked as flawed on the first two passes.
            foreach (string flawedTriggerName in flawedTriggers)
            { evalTriggers.Remove(flawedTriggerName); }
            foreach (string flawedRaycasterName in flawedRaycasters)
            { evalTriggers.Remove(flawedRaycasterName); }

            //_debugDisplay.WriteText("evaluateDeclaration complete.\n", _TRUE);
            //If we don't have errors, but we also don't have any tallies or ActionSets...
            if (textLog.getErrorTotal() == 0 && evalTallies.Count == 0 && evalSets.Count == 0)
            { textLog.addError($"No readable configuration found on the programmable block."); }
        }


        internal int evaluateGrid(LimitedMessageLog textLog, PaletteManager colorPalette,
            Dictionary<string, Tally> evalTallies, Dictionary<string, ActionSet> evalSets,
            Dictionary<string, Trigger> evalTriggers, Dictionary<string, Raycaster> evalRaycasters,
            Dictionary<IMyInventory, List<TallyCargo>> evalContainers, Dictionary<string, MFD> evalMFDs,
            List<IReportable> evalReports, List<WallOText> evalLogReports,
            Dictionary<string, Indicator> evalIndicators, MyIniParseResult parseResult, MyIniValue iniValue)
        {
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            Dictionary<string, Action<IMyTerminalBlock>> actions = compileActions();
            List<KeyValuePair<string, bool>> parsedData = new List<KeyValuePair<string, bool>>();
            Action<string> warningLoggerOLD = b => textLog.addWarning(b);
            Tally tally;
            ActionSet actionSet;
            Color color = Color.White;
            string[] elementNames;
            string elementName = "";
            string sectionHeader = "";
            string configKey = "";
            string troubleID = "";
            int counter = 0;
            bool handled;

            findBlocks<IMyTerminalBlock>(blocks, b =>
                (b.IsSameConstructAs(Me) && MyIni.HasSection(b.CustomData, _tag)));
            //_debugDisplay.WriteText($" Construct contains {blocks.Count} blocks with Shipware config.\n", _TRUE);
            if (blocks.Count <= 0)
            { textLog.addError($"No blocks found on this construct with a {_tag} INI section."); }

            foreach (IMyTerminalBlock block in blocks)
            {
                Action<string> blockWarningLogger = message =>
                { textLog.addWarning($"Block {block}, section {sectionHeader}{message}"); };
                //_debugDisplay.WriteText($"  Beginning evaluation for block {block.CustomName}\n", _TRUE);
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
                    handled = _FALSE;
                    //On most builds, most of what we'll be dealing with are tallies. So let's start there.
                    //_debugDisplay.WriteText("    Tally handler\n", _TRUE);
                    if (_iniReadWrite.ContainsKey(_tag, "Tallies"))
                    { //This is grounds for declaring this block to be handled.
                        handled = _TRUE;
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
                    //_debugDisplay.WriteText("    Multiple inventory Tally handler\n", _TRUE);
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
                                handled = _TRUE;
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
                    //_debugDisplay.WriteText("    ActionSet handler\n", _TRUE);
                    if (_iniReadWrite.ContainsKey(_tag, "ActionSets"))
                    {
                        //From the main section, we read:
                        //ActionSets: The ActionSet section names that can be found elsewhere in this 
                        //  block's CustomData.
                        //We found something we understand, declare handled.
                        handled = _TRUE;
                        //Get the 'ActionSets' data
                        iniValue = _iniReadWrite.Get(_tag, "ActionSets");
                        //Pull the individual ActionSet names from the ActionSets key.
                        elementNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                        foreach (string name in elementNames)
                        {
                            //_debugDisplay.WriteText($"     Evaluating element '{name}'\n", _TRUE);
                            //First things first: Does this ActionSet even exist?
                            if (!evalSets.ContainsKey(name))
                            {
                                textLog.addWarning($"Block '{block.CustomName}' tried to reference the " +
                                    $"unconfigured ActionSet '{name}'.");
                            }
                            else
                            {
                                //_debugDisplay.WriteText("     Getting ActionSet reference\n", _TRUE);
                                actionSet = evalSets[name];
                                //The name of the discrete section that will configure this ActionSet 
                                //is the PREFIX plus the name of the ActionSet. We'll be using that a 
                                //lot, so let's put a handle on it.
                                sectionHeader = $"{_SCRIPT_PREFIX}.{name}";
                                //Check to see if the user has included an ACTION SECTION
                                //_debugDisplay.WriteText("     Checking for ACTION SECTION\n", _TRUE);
                                if (!_iniReadWrite.ContainsSection(sectionHeader))
                                {
                                    textLog.addWarning($"Block '{block.CustomName}' references the ActionSet " +
                                        $"'{name}', but contains no discrete '{sectionHeader}' section that would " +
                                        $"define actions.");
                                }
                                else
                                {
                                    //_debugDisplay.WriteText("     Determining ActionPlan type.\n", _TRUE);
                                    IHasActionPlan actionPlan = null;
                                    if (_iniReadWrite.ContainsKey(sectionHeader, "Action0Property"))
                                    {
                                        //_debugDisplay.WriteText("     Entering TerminalAction branch\n", _TRUE);
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
                                            retreivedPart = tryGetPartFromConfig(textLog, sectionHeader,
                                                counter, block, _iniReadWrite, iniValue, colorPalette);
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
                                    else if (_iniReadWrite.ContainsKey(sectionHeader, "ActionsOn")
                                        || _iniReadWrite.ContainsKey(sectionHeader, "ActionsOff"))
                                    //If the discrete section contains either ActionsOn or ActionsOff 
                                    //keys, we need to use a MultiActionPlanBlock.
                                    //From config for MultiActionPlanBlock, we read:
                                    //ActionsOn (Default: null): A list of actions to be performed 
                                    //  on this block when this ActionSet is set to 'on'.
                                    //ActionsOff (Default: null): A list of actions to be performed 
                                    //  on this block when thisActionSet is set to 'off'.
                                    {
                                        //_debugDisplay.WriteText("     Entering MAPB branch\n", _TRUE);
                                        MultiActionPlanBlock mapb = new MultiActionPlanBlock(block);
                                        mapb.actionsOn = getActionHandlersForMAPB(_iniReadWrite,
                                            sectionHeader, "ActionsOn", actions, textLog, block);
                                        mapb.actionsOff = getActionHandlersForMAPB(_iniReadWrite,
                                            sectionHeader, "ActionsOff", actions, textLog, block);

                                        actionPlan = mapb;
                                    }
                                    else if (_iniReadWrite.ContainsKey(sectionHeader, "ActionOn")
                                        || _iniReadWrite.ContainsKey(sectionHeader, "ActionOff"))
                                    //If we've got an ActionOn or ActionOff key, we use ActionPlanBlock.
                                    //APB is functionally identical to MAPB, just with a single action
                                    //instead of a multitude
                                    {
                                        //_debugDisplay.WriteText("     Entering APB branch\n", _TRUE);
                                        //Create a new block plan with this block as the subject
                                        ActionPlanBlock apb = new ActionPlanBlock(block);
                                        iniValue = _iniReadWrite.Get(sectionHeader, "ActionOn");
                                        if (!iniValue.IsEmpty)
                                        {
                                            apb.actionOn = retrieveActionHandler(iniValue.ToString(),
                                            actions, textLog, block, sectionHeader, "ActionOn");
                                        }

                                        iniValue = _iniReadWrite.Get(sectionHeader, "ActionOff");
                                        if (!iniValue.IsEmpty)
                                        {
                                            apb.actionOff = retrieveActionHandler(iniValue.ToString(),
                                            actions, textLog, block, sectionHeader, "ActionOff");
                                        }

                                        actionPlan = apb;
                                    }
                                    //If we came away with an action plan, and that plan has at least one action...
                                    if (actionPlan?.hasAction() ?? _FALSE)
                                    //Go ahead and add this ActionPlan to the ActionSet
                                    { actionSet.addActionPlan(actionPlan); }
                                    //If we didn't successfully register an action, complain.
                                    else
                                    {
                                        textLog.addWarning($"Block '{block.CustomName}', discrete section '{sectionHeader}', " +
                                            "does not define any actions to be taken when the ActionSet changes state. " +
                                            "If you're listing Terminal Actions, make sure you're starting at index 0.");
                                    }
                                }
                            }
                        }
                    }

                    //Tally and ActionSet configuration can be on almost any block. But some 
                    //configuration can only be used on certain block types
                    //Raycasters are now largely configured from the PB. But we still have to tell
                    //them where their cameras are.
                    //_debugDisplay.WriteText("    Camera handler\n", _TRUE);
                    if (block is IMyCameraBlock)
                    {
                        if (_iniReadWrite.ContainsKey(_tag, "Raycasters"))
                        {
                            handled = _TRUE;
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
                    //_debugDisplay.WriteText("    Surface handler\n", _TRUE);
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
                                handled = _TRUE;
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
                                        //_debugDisplay.WriteText("      Have MFD name\n", _TRUE);
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
                                    //_debugDisplay.WriteText($"        Beginning loop for element '{name}'\n", _TRUE);
                                    sectionHeader = $"{_SCRIPT_PREFIX}.{name}";
                                    if (!_iniReadWrite.ContainsSection(sectionHeader))
                                    {
                                        textLog.addWarning($"Surface provider '{block.CustomName}', key {configKey} declares the " +
                                            $"page '{name}', but contains no discrete '{sectionHeader}' section that would " +
                                            $"configure that page.");
                                    }
                                    else
                                    {
                                        //At this point, we should have everything we need to start constructing a report.
                                        reportable = null;
                                        //If this is a report, it will have an 'Elements' key.
                                        if (_iniReadWrite.ContainsKey(sectionHeader, "Elements"))
                                        {
                                            //_debugDisplay.WriteText("          Entering Report branch\n", _TRUE);
                                            iniValue = _iniReadWrite.Get(sectionHeader, "Elements");
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
                                                            $"section {sectionHeader} tried to reference the " +
                                                            $"unconfigured element '{element}'.");
                                                    }
                                                }
                                            }
                                            //Create a new report with the data we've collected so far.
                                            report = new Report(surface, elementRefs);
                                            //Now that we have a report, we need to see if the user wants anything 
                                            //special done with it.
                                            //Title
                                            iniValue = _iniReadWrite.Get(sectionHeader, "Title");
                                            if (!iniValue.IsEmpty)
                                            { report.title = iniValue.ToString(); }
                                            //FontSize
                                            iniValue = _iniReadWrite.Get(sectionHeader, "FontSize");
                                            if (!iniValue.IsEmpty)
                                            { report.fontSize = iniValue.ToSingle(); }
                                            //Font
                                            iniValue = _iniReadWrite.Get(sectionHeader, "Font");
                                            if (!iniValue.IsEmpty)
                                            { report.font = iniValue.ToString(); }
                                            //Columns. IMPORTANT: Set anchors is no longer called during object
                                            //creation, and therefore MUST be called before the report is finished.
                                            //iniValue = _iniReadWrite.Get(sectionHeader, "Columns");
                                            //=======================>>><<<

                                            Func<string, float> getPadding = (edge) =>
                                            { return (float)(_iniReadWrite.Get(sectionHeader, $"Padding{edge}").ToDouble(0)); };
                                            float padLeft = getPadding("Left");
                                            float padRight = getPadding("Right");
                                            float padTop = getPadding("Top");
                                            float padBottom = getPadding("Bottom");

                                            //Possibly I should've just broken down and written a seperate method for this.
                                            //Then I could just pass the values by reference and have the method handle 
                                            //setting them to 0.
                                            //But I can't overstate how much I hate having to pass half a dozen things into
                                            //a method just to generate a proper warning message.
                                            Func<string, float, string, float, bool> paddingExceeds100 =
                                                (firstEdgeName, firstEdgeValue, secondEdgeName, secondEdgeValue) =>
                                                {
                                                    if (firstEdgeValue + secondEdgeValue > 100)
                                                    {
                                                        textLog.addWarning($"Surface provider '{block.CustomName}', " +
                                                                $"section {sectionHeader} has padding values in excess " +
                                                                $"of 100% for edges {firstEdgeName} and {secondEdgeName} " +
                                                                $"which have been ignored.");
                                                        return _TRUE;
                                                    }
                                                    return _FALSE;
                                                };
                                            if (paddingExceeds100("Left", padLeft, "Right", padRight))
                                            {
                                                padLeft = 0;
                                                padRight = 0;
                                            }
                                            if (paddingExceeds100("Top", padTop, "Bottom", padBottom))
                                            {
                                                padTop = 0;
                                                padBottom = 0;
                                            }

                                            int columns = _iniReadWrite.Get(sectionHeader, "Columns").ToInt32(1);
                                            bool titleObeysPadding = _iniReadWrite.Get(sectionHeader, "TitleObeysPadding").ToBoolean(_FALSE);
                                            //Once we have all the data, we can call setAnchors.
                                            report.setAnchors(columns, padLeft, padRight, padTop, padBottom,
                                                titleObeysPadding, _sb);

                                            //We've should have all the available configuration for this report. Now we'll point
                                            //Reportable at it and move on.
                                            reportable = report;
                                        }

                                        //If this is a GameScript, it will have a 'Script' key.
                                        else if (_iniReadWrite.ContainsKey(sectionHeader, "Script"))
                                        {
                                            //_debugDisplay.WriteText("          Entering Script branch\n", _TRUE);
                                            //TODO? I could check the Script value against a list of 
                                            //available scripts if I wanted to. See 20230227 and 20240821
                                            //for thoughts on the matter.
                                            GameScript script = new GameScript(surface,
                                                _iniReadWrite.Get(sectionHeader, "Script").ToString());
                                            //Scripts are pretty straightforward. Off to reportable with them.
                                            reportable = script;
                                        }

                                        //If this is a WallOText, it will have a 'DataType' key.
                                        else if (_iniReadWrite.ContainsKey(sectionHeader, "DataType"))
                                        {
                                            //_debugDisplay.WriteText("          Entering WOT branch\n", _TRUE);
                                            string type = _iniReadWrite.Get(sectionHeader, "DataType").ToString().ToLowerInvariant();
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
                                                if (!_iniReadWrite.ContainsKey(sectionHeader, "DataSource"))
                                                {
                                                    textLog.addWarning($"Surface provider '{block.CustomName}', section " +
                                                        $"{sectionHeader} has a DataType of {type}, but a missing or " +
                                                        $"unreadable DataSource.");
                                                }
                                                else
                                                {
                                                    string source = _iniReadWrite.Get(sectionHeader, "DataSource").ToString();
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
                                                            $"{sectionHeader} tried to reference the unknown block '{source}' " +
                                                            $"as a DataSource.");
                                                    }
                                                }
                                            }
                                            //Raycasters get their data from Raycaster objects.
                                            else if (type == "raycaster")
                                            {
                                                //_debugDisplay.WriteText("          Entering Raycaster branch\n", _TRUE);
                                                //Check to see if the user provided a DataSource
                                                if (!_iniReadWrite.ContainsKey(sectionHeader, "DataSource"))
                                                {
                                                    textLog.addWarning($"Surface provider '{block.CustomName}', section " +
                                                        $"{sectionHeader} has a DataType of {type}, but a missing or " +
                                                        $"unreadable DataSource.");
                                                }
                                                else
                                                {
                                                    //_debugDisplay.WriteText("          Found DataSource\n", _TRUE);
                                                    string source = _iniReadWrite.Get(sectionHeader, "DataSource").ToString();
                                                    //Check our list of Raycasters to see if one has a matching key
                                                    if (evalRaycasters.ContainsKey(source))
                                                    { broker = new RaycastBroker(evalRaycasters[source]); }
                                                    //If we didn't find matching raycaster, complain.
                                                    else
                                                    {
                                                        textLog.addWarning($"Surface provider '{block.CustomName}', section " +
                                                            $"{sectionHeader} tried to reference the unknown Raycaster " +
                                                            $"'{source}' as a DataSource.");
                                                    }
                                                }
                                            }
                                            else
                                            //If we don't recognize the DataType, complain.
                                            {
                                                textLog.addWarning($"Surface provider '{block.CustomName}', section " +
                                                    $"{sectionHeader} tried to reference the unknown data type '{type}'.");
                                            }
                                            //If we came through that with some sort of broker
                                            if (broker != null)
                                            {
                                                //Create a new WallOText using our surface and the broker we've found.
                                                WallOText wall = new WallOText(surface, broker, _sb);
                                                //Configure any other settings that the user has seen fit to specify.
                                                //FontSize
                                                iniValue = _iniReadWrite.Get(sectionHeader, "FontSize");
                                                if (!iniValue.IsEmpty)
                                                { wall.fontSize = iniValue.ToSingle(); }
                                                //Font
                                                iniValue = _iniReadWrite.Get(sectionHeader, "Font");
                                                if (!iniValue.IsEmpty)
                                                { wall.font = iniValue.ToString(); }
                                                //CharPerLine
                                                iniValue = _iniReadWrite.Get(sectionHeader, "CharPerLine");
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
                                                            $"{sectionHeader} tried to set a CharPerLine limit with the {type} " +
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
                                            if (colorPalette.tryGetColorFromConfig(blockWarningLogger, _iniReadWrite,
                                                sectionHeader, "ForeColor", out color))
                                            { ((IHasColors)reportable).foreColor = color; }
                                            if (colorPalette.tryGetColorFromConfig(blockWarningLogger, _iniReadWrite,
                                                sectionHeader, "BackColor", out color))
                                            { ((IHasColors)reportable).backColor = color; }
                                        }
                                    }
                                    //There's a couple of extra steps that we need to go through if 
                                    //we're dealing with an MFD
                                    if (mfd != null && reportable != null)
                                    {
                                        //This page may have a ShowOnActionState key, meaning we need
                                        //to hook it to an ActionSet.
                                        iniValue = _iniReadWrite.Get(sectionHeader, "ShowOnActionState");
                                        if (!iniValue.IsEmpty)
                                        {
                                            troubleID = $"Surface provider '{block.CustomName}', section {sectionHeader}";
                                            parseStateList(iniValue.ToString(), troubleID, warningLoggerOLD, parsedData);
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
                                //_debugDisplay.WriteText("      End of Pages loop\n", _TRUE);
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
                                        mfd.trySetPageByName(_iniRead.Get("MFDs", mfd.programName).ToString());
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
                    //_debugDisplay.WriteText("    Light handler\n", _TRUE);
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
                        handled = _TRUE;
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
                //_debugDisplay.WriteText($"  Finished evaluation for block {block.CustomName}\n", _TRUE);
            }
            //_debugDisplay.WriteText(" End of EvaluateGrid\n", _TRUE);
            return blocks.Count;
        }

        internal abstract class StateMachineBase
        {
            //The name of the machine extending this base
            public string MACHINE_NAME { get; private set; }
            //Once the machine exceeds the instruction limit, it will yield.
            protected int INSTRUCTION_LIMIT { get; private set; }
            //A reference to the external program, mostly for the purpose of determining what this 
            //tic's instruction count is.
            protected MyGridProgram program { get; private set; }
            //The actual state machine
            protected IEnumerator<string> sequence;
            //The number of updates this instance has received
            public int uptime { get; private set; }
            //The total number of instructions this instance has used
            public int totalInstructions { get; private set; }
            //The machine's current status
            public string status { get; protected set; }
            //Is a summary of this machine's actions expected?
            public bool generateLogs { get; private set; }

            public StateMachineBase(MyGridProgram program, string machineName, double allowedPercentOfInstructions,
                bool createSummary)
            {
                this.program = program;
                MACHINE_NAME = machineName;
                INSTRUCTION_LIMIT = (int)(program.Runtime.MaxInstructionCount * allowedPercentOfInstructions);
                uptime = 0;
                totalInstructions = 0;
                status = $"{MACHINE_NAME} waiting to begin";
                generateLogs = createSummary;
            }

            internal abstract void begin();

            internal bool next()
            { return sequence.MoveNext(); }

            protected bool isInstructionLimitReached()
            {
                if (program.Runtime.CurrentInstructionCount > INSTRUCTION_LIMIT)
                {
                    updateStats();
                    return _TRUE;
                }
                else
                { return _FALSE; }
            }

            protected void updateStats()
            {
                uptime++;
                totalInstructions += program.Runtime.CurrentInstructionCount;
            }

            //internal abstract void end();
            //I may later decide that there's stuff I want to do with a state machine when it's 
            //finished. For now, all I really want to do is make sure the enumerator is disposed.
            internal void end()
            {
                sequence.Dispose();
                status = $"{MACHINE_NAME} completed.";
            }

            internal abstract string getSummary();

            protected string getStats()
            {
                return $"{MACHINE_NAME} used a total of {totalInstructions} / {program.Runtime.MaxInstructionCount} " +
                    $"({(int)(((double)totalInstructions / program.Runtime.MaxInstructionCount) * 100)}%) " +
                    $"of instructions allowed in one tic, distributed over {uptime} tics.";
            }
        }

        internal class SpriteRefreshMachine : StateMachineBase
        {
            //The maximum number of reports the machine will attempt to refrest in a single tic.
            const int MAX_REFRESH_PER_TIC = 20;
            //The minimum number of tics we want this process to be distributed over.
            //It's a double to avoid an ambiguous call to Math.Ceiling
            const double MIN_DESIRED_TICS = 4;
            //The reports this machine will refresh.
            IReportable[] reports;

            public SpriteRefreshMachine(MyGridProgram program, IReportable[] reports, bool createSummary) :
                base(program, "Sprite Refresher", .1, createSummary)
            { this.reports = reports; }

            internal override void begin()
            {
                sequence = refresherSequence();
                status = $"{MACHINE_NAME} started";
            }

            IEnumerator<string> refresherSequence()
            {
                //We want sprite refreshing to be distributed over a minimum of 4 tics. Or roughly
                //4 tics; depending on the numbers involved, we might end up with fewer here due
                //to rounding.
                //If we, somehow, have more than 80 seperate reports, we'll do 20 reports per tic and take
                //more than 4 tics to get the job done. But at 6 possible tics per second and a hard limit
                //of one refresh every 10 seconds, you'd have to have 1200 reports before you started 
                //giving the scheduler grief. And at that point, you've probably hit the instruction 
                //limit on the Update100 processes long ago.
                int refreshLimit = Math.Min((int)(Math.Ceiling(reports.Length / MIN_DESIRED_TICS)), MAX_REFRESH_PER_TIC);
                int index = 0;
                int targetIndex = refreshLimit;
                foreach (IReportable report in reports)
                {
                    report.refresh();
                    //The refresh method is part of a previous approach to this problem. All it 
                    //does is tell the report include or exclude an invisible sprite the next time 
                    //the report is drawn, forcing the server to re-sync the cache.
                    //This approach is focused on even distribution of networks load, so we'll call
                    //update immediately instead of waiting for it to come around on its own.
                    report.update();
                    index++;
                    //On the other state machines, this is the sort of place where we'd check against
                    //the instruction limit. For this one, we're just interested in distributing the
                    //network load semi-evenly, and we only check against an arbitrary internal limit.
                    //MONITOR. The limit is set so low that the instruction limit should never come 
                    //close to being hit, but it is theoretically possible. A call to isInstructionLimitReached()
                    //could be included here if needed.
                    if (index >= targetIndex)
                    {
                        targetIndex += refreshLimit;
                        status = $"{MACHINE_NAME} report {index}/{reports.Length}";
                        updateStats();
                        yield return status;
                    }
                }
            }

            internal override string getSummary()
            {
                return $"{MACHINE_NAME} finished. Re-sync'd sprites on {reports.Length} surfaces.\n" +
                    $"{getStats()}";
            }
        }

        //A state machine for debug use. It exists only to sit in the queue and be in the way of
        //other things happening.
        /*
        internal class FillerMachine : StateMachineBase
        {
            int targetLifetime;

            public FillerMachine(MyGridProgram program, int targetLifetime, bool createSummary) :
                base(program, "Filler", .1, createSummary)
            { this.targetLifetime = targetLifetime; }

            internal override void begin()
            {
                sequence = fillerSequence();
                status = $"{MACHINE_NAME} started";
            }

            IEnumerator<string> fillerSequence()
            {
                for (int i = 0; i < targetLifetime; i++)
                {
                    status = $"{MACHINE_NAME} tic {i}/{targetLifetime}";
                    updateStats();
                    yield return status;
                }
            }

            internal override string getSummary()
            {
                return $"{MACHINE_NAME} finished.\n" +
                    $"{getStats()}";
            }
        }*/

        /*
        private void checkPadding(string firstEdgeName, ref float firstEdgeValue, 
            string secondEdgeName, ref float secondeEdgeValue) 
        {
            if (firstEdgeValue + secondeEdgeValue > 100)
            {
                //TODO: Monitor. I'm almost certain this won't affect
                //the original values.
                firstEdgeValue = 0;
                secondeEdgeValue = 0;
                textLog.addWarning($"Surface provider '{block.CustomName}', " +
                        $"section {sectionHeader}'s padding values for " +
                        $"{firstEdgeName} and {secondEdgeName} exceeded " +
                        $"100% and have been ignored.");
            }
        }*/

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
                /*debug.WriteText($"    Wrote the following multiline config: {outcome}\n", _TRUE);*/
            }
            return outcome;
        }

        public Dictionary<string, Action<IMyTerminalBlock>> compileActions()
        {
            //Dictionary<string, Action<IMyTerminalBlock>> actions = new Dictionary<string, Action<IMyTerminalBlock>>();
            Dictionary<string, Action<IMyTerminalBlock>> actions =
                new Dictionary<string, Action<IMyTerminalBlock>>(StringComparer.OrdinalIgnoreCase);
            //A giant list of raw strings. It was the tallest nail when the hammer of character count
            //optimization came around.
            string blockName;
            string commandName = "Enable";
            string positive = "Positive";
            string negative = "Negative";

            //Functional Blocks==================
            //EnableOn
            actions.Add($"{commandName}On", b => ((IMyFunctionalBlock)b).Enabled = _TRUE);
            //EnableOff
            actions.Add($"{commandName}Off", b => ((IMyFunctionalBlock)b).Enabled = _FALSE);

            //Battery Blocks=====================
            blockName = "Battery";
            commandName = "charge";
            //BatteryAuto
            actions.Add($"{blockName}Auto", b => ((IMyBatteryBlock)b).ChargeMode = ChargeMode.Auto);
            //BatteryRecharge
            actions.Add($"{blockName}Re{commandName}", b => ((IMyBatteryBlock)b).ChargeMode = ChargeMode.Recharge);
            //BatteryDischarge
            actions.Add($"{blockName}Dis{commandName}", b => ((IMyBatteryBlock)b).ChargeMode = ChargeMode.Discharge);

            //Cameras
            //20240912: Doesn't actually seem to work. Not sure why. Leaving it in for now,
            //but not documenting its existence.
            //20250519: Disabled for character count.
            //actions.Add("CameraRaycastEnable", b => ((IMyCameraBlock)b).EnableRaycast = _TRUE);
            //actions.Add("CameraRaycastDisable", b => ((IMyCameraBlock)b).EnableRaycast = _FALSE);

            //Connectors=========================
            blockName = "Connector";
            //ConnectorLock
            actions.Add($"{blockName}Lock", b => ((IMyShipConnector)b).Connect());
            //ConnectorUnlock
            actions.Add($"{blockName}Unlock", b => ((IMyShipConnector)b).Disconnect());

            //Doors==============================
            blockName = "Door";
            //DoorOpen
            actions.Add($"{blockName}Open", b => ((IMyDoor)b).OpenDoor());
            //DoorClose
            actions.Add($"{blockName}Close", b => ((IMyDoor)b).CloseDoor());

            //GasTanks===========================
            blockName = "Tank";
            commandName = "Stockpile";
            //TankStockpileOn
            actions.Add($"{blockName}{commandName}On", b => ((IMyGasTank)b).Stockpile = _TRUE);
            //TankStockpileOff
            actions.Add($"{blockName}{commandName}Off", b => ((IMyGasTank)b).Stockpile = _FALSE);

            //Gyros==============================
            blockName = "Gyro";
            string stabilize = "Stabilize";
            //Gyro overides are set in RPM, but we can't say for sure what the max RPM of a given 
            //block may be. So instead, we use arbitrarily high numbers and let the block sort it out.
            //GyroOverrideOn
            commandName = "Override";
            //GyroOverrideOn
            actions.Add($"{blockName}{commandName}On", b => ((IMyGyro)b).GyroOverride = _TRUE);
            //GyroOverrideOff
            actions.Add($"{blockName}{commandName}Off", b => ((IMyGyro)b).GyroOverride = _FALSE);
            //GyroYawPositive
            actions.Add($"{blockName}Yaw{positive}", b => ((IMyGyro)b).Yaw = 9000);
            //GyroYawStabilize
            actions.Add($"{blockName}Yaw{stabilize}", b => ((IMyGyro)b).Yaw = 0);
            //GyroYawNegative
            actions.Add($"{blockName}Yaw{negative}", b => ((IMyGyro)b).Yaw = -9000);
            //Yes, I'm assigning PitchPositive to be -9000. Yes, that makes no sense. No, I don't 
            //know why it has to be this way to make it work correctly.
            //GyroPitchPositive
            commandName = "Pitch";
            actions.Add($"{blockName}{commandName}{positive}", b => ((IMyGyro)b).Pitch = -9000);
            //GyroPitchStabilize
            actions.Add($"{blockName}{commandName}{stabilize}", b => ((IMyGyro)b).Pitch = 0);
            //GyroPitchNegative
            actions.Add($"{blockName}{commandName}{negative}", b => ((IMyGyro)b).Pitch = 9000);
            //GyroRollPositive
            commandName = "Roll";
            actions.Add($"{blockName}{commandName}{positive}", b => ((IMyGyro)b).Roll = 9000);
            //GyroRollStabilize
            actions.Add($"{blockName}{commandName}{stabilize}", b => ((IMyGyro)b).Roll = 0);
            //GyroRollNegative
            actions.Add($"{blockName}{commandName}{negative}", b => ((IMyGyro)b).Roll = -9000);

            //LandingGear========================
            blockName = "Gear";
            commandName = "AutoLock";
            //GearAutoLockOn
            actions.Add($"{blockName}{commandName}On", b => ((IMyLandingGear)b).AutoLock = _TRUE);
            //GearAutoLockOff
            actions.Add($"{blockName}{commandName}Off", b => ((IMyLandingGear)b).AutoLock = _FALSE);
            //GearLock
            actions.Add($"{blockName}Lock", b => ((IMyLandingGear)b).Lock());
            //GearUnlock
            actions.Add($"{blockName}Unlock", b => ((IMyLandingGear)b).Unlock());

            //Jump Drives========================
            blockName = "JumpDrive";
            commandName = "Recharge";
            //JumpDriveRechargeOn
            actions.Add($"{blockName}{commandName}On", b => ((IMyJumpDrive)b).Recharge = _TRUE);
            //JumpDriveRechargeOff
            actions.Add($"{blockName}{commandName}Off", b => ((IMyJumpDrive)b).Recharge = _FALSE);

            //Parachutes=========================
            blockName = "Parachute";
            //ParachuteOpen
            actions.Add($"{blockName}Open", b => ((IMyParachute)b).OpenDoor());
            //ParachuteClose
            actions.Add($"{blockName}Close", b => ((IMyParachute)b).CloseDoor());
            commandName = "AutoDeploy";
            //ParachuteAutoDeployOn
            actions.Add($"{blockName}{commandName}On", b => ((IMyParachute)b).AutoDeploy = _TRUE);
            //ParachuteAutoDeployOff
            actions.Add($"{blockName}{commandName}Off", b => ((IMyParachute)b).AutoDeploy = _FALSE);

            //Pistons============================
            blockName = "Piston";
            //PistonExtend
            actions.Add($"{blockName}Extend", b => ((IMyPistonBase)b).Extend());
            //PistonRetract
            actions.Add($"{blockName}Retract", b => ((IMyPistonBase)b).Retract());

            //Rotors=============================
            /*
            //RotorLock
            actions.Add($"{blockName}Lock", b => ((IMyMotorAdvancedStator)b).RotorLock = _TRUE);
            //RotorUnlock
            actions.Add($"{blockName}Unlock", b => ((IMyMotorAdvancedStator)b).RotorLock = _FALSE);
            //RotorReverse
            actions.Add($"{blockName}Reverse", b => ((IMyMotorAdvancedStator)b).TargetVelocityRPM =
                ((IMyMotorAdvancedStator)b).TargetVelocityRPM * -1);
            //RotorPositive
            actions.Add($"{blockName}{positive}", b => ((IMyMotorAdvancedStator)b).TargetVelocityRPM =
                Math.Abs(((IMyMotorAdvancedStator)b).TargetVelocityRPM));
            //RotorNegative
            actions.Add($"{blockName}{negative}", b => ((IMyMotorAdvancedStator)b).TargetVelocityRPM =
                Math.Abs(((IMyMotorAdvancedStator)b).TargetVelocityRPM) * -1);*/
            blockName = "Rotor";
            //RotorLock
            actions.Add($"{blockName}Lock", b => ((IMyMotorStator)b).RotorLock = _TRUE);
            //RotorUnlock
            actions.Add($"{blockName}Unlock", b => ((IMyMotorStator)b).RotorLock = _FALSE);
            //RotorReverse
            actions.Add($"{blockName}Reverse", b => ((IMyMotorStator)b).TargetVelocityRPM =
                ((IMyMotorStator)b).TargetVelocityRPM * -1);
            //RotorPositive
            actions.Add($"{blockName}{positive}", b => ((IMyMotorStator)b).TargetVelocityRPM =
                Math.Abs(((IMyMotorStator)b).TargetVelocityRPM));
            //RotorNegative
            actions.Add($"{blockName}{negative}", b => ((IMyMotorStator)b).TargetVelocityRPM =
                Math.Abs(((IMyMotorStator)b).TargetVelocityRPM) * -1);

            //Sorters============================
            blockName = "Sorter";
            commandName = "Drain";
            //SorterDrainOn
            actions.Add($"{blockName}{commandName}On", b => ((IMyConveyorSorter)b).DrainAll = _TRUE);
            //SorterDrainOff
            actions.Add($"{blockName}{commandName}Off", b => ((IMyConveyorSorter)b).DrainAll = _FALSE);

            //Sound Block========================
            blockName = "Sound";
            //SoundPlay
            actions.Add($"{blockName}Play", b => ((IMySoundBlock)b).Play());
            //SoundStop
            actions.Add($"{blockName}Stop", b => ((IMySoundBlock)b).Stop());

            //Thrusters==========================
            blockName = "Thruster";
            commandName = "Override";
            //ThrusterOverrideMax
            actions.Add($"{blockName}{commandName}Max", b => ((IMyThrust)b).ThrustOverridePercentage = 1);
            //ThrusterOverrideOff
            actions.Add($"{blockName}{commandName}Off", b => ((IMyThrust)b).ThrustOverridePercentage = 0);

            //Timers=============================
            blockName = "Timer";
            //TimerTrigger
            actions.Add($"{blockName}Trigger", b => ((IMyTimerBlock)b).Trigger());
            //TimerStart
            actions.Add($"{blockName}Start", b => ((IMyTimerBlock)b).StartCountdown());
            //TimerStop
            actions.Add($"{blockName}Stop", b => ((IMyTimerBlock)b).StopCountdown());

            //Turrets and Custom Controllers=====
            blockName = "Turret";
            string controller = "Controller";
            string target = "Target";
            commandName = "Meteors";
            //TurretTargetMeteorsOn
            actions.Add($"{blockName}{target}{commandName}On", b => ((IMyLargeTurretBase)b).TargetMeteors = _TRUE);
            //TurretTargetMeteorsOff
            actions.Add($"{blockName}{target}{commandName}Off", b => ((IMyLargeTurretBase)b).TargetMeteors = _FALSE);
            //ControllerTargetMeteorsOn
            actions.Add($"{controller}{target}{commandName}On", b => ((IMyTurretControlBlock)b).TargetMeteors = _TRUE);
            //ControllerTargetMeteorsOff
            actions.Add($"{controller}{target}{commandName}Off", b => ((IMyTurretControlBlock)b).TargetMeteors = _FALSE);

            commandName = "Missiles";
            //TurretTargetMissilesOn
            actions.Add($"{blockName}{target}{commandName}On", b => ((IMyLargeTurretBase)b).TargetMissiles = _TRUE);
            //TurretTargetMissilesOff
            actions.Add($"{blockName}{target}{commandName}Off", b => ((IMyLargeTurretBase)b).TargetMissiles = _FALSE);
            //ControllerTargetMissilesOn
            actions.Add($"{controller}{target}{commandName}On", b => ((IMyTurretControlBlock)b).TargetMissiles = _TRUE);
            //ControllerTargetMissilesOff
            actions.Add($"{controller}{target}{commandName}Off", b => ((IMyTurretControlBlock)b).TargetMissiles = _FALSE);

            commandName = "SmallGrids";
            //TurretTargetSmallGridsOn
            actions.Add($"{blockName}{target}{commandName}On", b => ((IMyLargeTurretBase)b).TargetSmallGrids = _TRUE);
            //TurretTargetSmallGridsOff
            actions.Add($"{blockName}{target}{commandName}Off", b => ((IMyLargeTurretBase)b).TargetSmallGrids = _FALSE);
            //ControllerTargetSmallGridsOn
            actions.Add($"{controller}{target}{commandName}On", b => ((IMyTurretControlBlock)b).TargetSmallGrids = _TRUE);
            //ControllerTargetSmallGridsOff
            actions.Add($"{controller}{target}{commandName}Off", b => ((IMyTurretControlBlock)b).TargetSmallGrids = _FALSE);

            commandName = "LargeGrids";
            //TurretTargetLargeGridsOn
            actions.Add($"{blockName}{target}{commandName}On", b => ((IMyLargeTurretBase)b).TargetLargeGrids = _TRUE);
            //TurretTargetLargeGridsOff
            actions.Add($"{blockName}{target}{commandName}Off", b => ((IMyLargeTurretBase)b).TargetLargeGrids = _FALSE);
            //ControllerTargetLargeGridsOn
            actions.Add($"{controller}{target}{commandName}On", b => ((IMyTurretControlBlock)b).TargetLargeGrids = _TRUE);
            //ControllerTargetLargeGridsOff
            actions.Add($"{controller}{target}{commandName}Off", b => ((IMyTurretControlBlock)b).TargetLargeGrids = _FALSE);

            commandName = "Characters";
            //TurretTargetCharactersOn
            actions.Add($"{blockName}{target}{commandName}On", b => ((IMyLargeTurretBase)b).TargetCharacters = _TRUE);
            //TurretTargetCharactersOff
            actions.Add($"{blockName}{target}{commandName}Off", b => ((IMyLargeTurretBase)b).TargetCharacters = _FALSE);
            //ControllerTargetCharactersOn
            actions.Add($"{controller}{target}{commandName}On", b => ((IMyTurretControlBlock)b).TargetCharacters = _TRUE);
            //ControllerTargetCharactersOff
            actions.Add($"{controller}{target}{commandName}Off", b => ((IMyTurretControlBlock)b).TargetCharacters = _FALSE);

            commandName = "Stations";
            //TurretTargetStationsOn
            actions.Add($"{blockName}{target}{commandName}On", b => ((IMyLargeTurretBase)b).TargetStations = _TRUE);
            //TurretTargetStationsOff
            actions.Add($"{blockName}{target}{commandName}Off", b => ((IMyLargeTurretBase)b).TargetStations = _FALSE);
            //ControllerTargetStationsOn
            actions.Add($"{controller}{target}{commandName}On", b => ((IMyTurretControlBlock)b).TargetStations = _TRUE);
            //ControllerTargetStationsOff
            actions.Add($"{controller}{target}{commandName}Off", b => ((IMyTurretControlBlock)b).TargetStations = _FALSE);

            commandName = "Neutrals";
            //TurretTargetNeutralsOn
            actions.Add($"{blockName}{target}{commandName}On", b => ((IMyLargeTurretBase)b).TargetNeutrals = _TRUE);
            //TurretTargetNeutralsOff
            actions.Add($"{blockName}{target}{commandName}Off", b => ((IMyLargeTurretBase)b).TargetNeutrals = _FALSE);
            //ControllerTargetNeutralsOn
            actions.Add($"{controller}{target}{commandName}On", b => ((IMyTurretControlBlock)b).TargetNeutrals = _TRUE);
            //ControllerTargetNeutralsOff
            actions.Add($"{controller}{target}{commandName}Off", b => ((IMyTurretControlBlock)b).TargetNeutrals = _FALSE);

            commandName = "Enemies";
            //TurretTargetEnemiesOn
            actions.Add($"{blockName}{target}{commandName}On", b => ((IMyLargeTurretBase)b).TargetEnemies = _TRUE);
            //TurretTargetEnemiesOff
            actions.Add($"{blockName}{target}{commandName}Off", b => ((IMyLargeTurretBase)b).TargetEnemies = _FALSE);
            //For some reason, Turret Controller blocks don't have a setter for TargetEnemies. So 
            //instead, we have to use terminal actions.
            //TurretTargetEnemiesOn
            actions.Add($"{controller}{target}{commandName}On", b => b.SetValue("TargetEnemies", _TRUE));
            //TurretTargetEnemiesOff
            actions.Add($"{controller}{target}{commandName}Off", b => b.SetValue("TargetEnemies", _FALSE));
            /* This never worked, and despite some fairly rigorous testing (Where this worked in isolation),
             * I couldn't figure out why. It's something in the lambda expression, possibly the interpolated
             * string. (20250519)
            //ControllerTargetEnemiesOn
            actions.Add($"{controller}{target}{commandName}On", b => b.SetValue($"{target}{commandName}", _TRUE));
            //ControllerTargetEnemiesOff
            actions.Add($"{controller}{target}{commandName}Off", b => b.SetValue($"{target}{commandName}", _FALSE));
            */

            string subsystem = "Subsystem";
            commandName = "Default";
            //TurretSubsystemDefault
            actions.Add($"{blockName}{subsystem}{commandName}", b => ((IMyLargeTurretBase)b).SetTargetingGroup(""));
            //ControllerSubsystemDefault
            actions.Add($"{controller}{subsystem}{commandName}", b => ((IMyTurretControlBlock)b).SetTargetingGroup(""));

            commandName = "Weapons";
            //TurretSubsystemWeapons
            actions.Add($"{blockName}{subsystem}{commandName}", b => ((IMyLargeTurretBase)b).SetTargetingGroup(commandName));
            //ControllerSubsystemWeapons
            actions.Add($"{controller}{subsystem}{commandName}", b => ((IMyTurretControlBlock)b).SetTargetingGroup(commandName));

            commandName = "Propulsion";
            //TurretSubsystemPropulsion
            actions.Add($"{blockName}{subsystem}{commandName}", b => ((IMyLargeTurretBase)b).SetTargetingGroup(commandName));
            //ControllerSubsystemPropulsion
            actions.Add($"{controller}{subsystem}{commandName}", b => ((IMyTurretControlBlock)b).SetTargetingGroup(commandName));

            commandName = "PowerSystems";
            //TurretSubsystemPowerSystems
            actions.Add($"{blockName}{subsystem}{commandName}", b => ((IMyLargeTurretBase)b).SetTargetingGroup(commandName));
            //ControllerSubsystemPowerSystems
            actions.Add($"{controller}{subsystem}{commandName}", b => ((IMyTurretControlBlock)b).SetTargetingGroup(commandName));

            //Vents==============================
            blockName = "Vent";
            commandName = "pressurize";
            //VentPressurize
            actions.Add($"{blockName}{commandName}", b => ((IMyAirVent)b).Depressurize = _FALSE);
            //VentDepressurize
            actions.Add($"{blockName}De{commandName}", b => ((IMyAirVent)b).Depressurize = _TRUE);

            //Warheads===========================
            blockName = "Warhead";
            //WarheadArm
            actions.Add($"{blockName}Arm", b => ((IMyWarhead)b).IsArmed = _TRUE);
            //WarheadDisarm
            actions.Add($"{blockName}Disarm", b => ((IMyWarhead)b).IsArmed = _FALSE);
            commandName = "Countdown";
            //WarheadCountdownStart
            actions.Add($"{blockName}{commandName}Start", b => ((IMyWarhead)b).StartCountdown());
            //WarheadCountdownStop
            actions.Add($"{blockName}{commandName}Stop", b => ((IMyWarhead)b).StopCountdown());
            //WarheadDetonate
            actions.Add($"{blockName}Detonate", b => ((IMyWarhead)b).Detonate());

            //Weapons============================
            actions.Add("WeaponFireOnce", b => ((IMyUserControllableGun)b).ShootOnce());

            //Wheels=============================
            blockName = "Suspension";
            commandName = "Height";
            //SuspensionHeightPositive
            actions.Add($"{blockName}{commandName}{positive}", b => ((IMyMotorSuspension)b).Height = 9000);
            //SuspensionHeightNegative
            actions.Add($"{blockName}{commandName}{negative}", b => ((IMyMotorSuspension)b).Height = -9000);
            //SuspensionHeightZero
            actions.Add($"{blockName}{commandName}Zero", b => ((IMyMotorSuspension)b).Height = 0);
            commandName = "Propulsion";
            //SuspensionPropulsionPositive
            actions.Add($"{blockName}{commandName}{positive}", b => ((IMyMotorSuspension)b).PropulsionOverride = 1);
            //SuspensionPropulsionNegative
            actions.Add($"{blockName}{commandName}{negative}", b => ((IMyMotorSuspension)b).PropulsionOverride = -1);
            //SuspensionPropulsionZero
            actions.Add($"{blockName}{commandName}Zero", b => ((IMyMotorSuspension)b).PropulsionOverride = 0);
            //MergeBlock?
            return actions;
        }

        internal class PaletteManager
        {
            Dictionary<string, IColorCoder> colorPalette;

            public PaletteManager(StringComparer comparer = null)
            {
                if (comparer != null)
                { colorPalette = new Dictionary<string, IColorCoder>(comparer); }
                else
                { colorPalette = new Dictionary<string, IColorCoder>(); }
                //Isn't actually used for anything, this is just the color I've taken to applying to 
                //my lights, and I wanted it handy.
                addColor("Cozy", 255, 225, 200);
                //Goes with everything
                addColor("Black", 0, 0, 0);
                //We'll be using these in just a second, so we'll go ahead and put handles on them
                Color optimal = addColor("Green", 25, 225, 100);
                Color normal = addColor("LightBlue", 100, 200, 225);
                Color caution = addColor("Yellow", 255, 255, 0);
                Color warning = addColor("Orange", 255, 150, 0);
                Color critical = addColor("Red", 255, 0, 0);

                colorPalette.Add("LowGood", new ColorCoderLow(optimal, normal, caution, warning, critical));
                colorPalette.Add("HighGood", new ColorCoderHigh(optimal, normal, caution, warning, critical));
            }

            //Add a new color to the internal dictionary as a ColorCoderMono. Returns the Color object,
            //if I need it for some reason.
            private Color addColor(string name, int r, int g, int b)
            {
                Color newColor = new Color(r, g, b);
                colorPalette.Add(name, new ColorCoderMono(newColor, name));
                return newColor;
            }

            public bool tryGetColorFromConfig(Action<string> troubleLogger, MyIni iniReader,
                string sectionTag, string targetKey, out Color color)
            {
                IColorCoder coder;
                bool foundColor = tryGetCoderFromConfig(troubleLogger, iniReader, sectionTag,
                    targetKey, out coder);
                if (foundColor)
                { color = coder.getColorCode(-1); }
                else
                //We have to return a color, and Colors aren't nullable. So we'll fly the universal
                //shade of surrender.
                //TODO? Alternatively, I could go back to using ref instead of out.
                { color = Color.White; }
                return foundColor;
            }

            public bool tryGetCoderFromConfig(Action<string> troubleLogger, MyIni iniReader,
                string sectionTag, string targetKey, out IColorCoder coder)
            {
                MyIniValue iniValue = iniReader.Get(sectionTag, targetKey);
                coder = null;
                //Is there even a value to be found?
                if (!iniValue.IsEmpty)
                {
                    string rawValue = iniValue.ToString();
                    //We have something. Is it something we already recognize?
                    if (colorPalette.TryGetValue(rawValue, out coder))
                    //We're done here.
                    { return _TRUE; }
                    else
                    //We don't recognize this, which means it isn't one of the hard-coded colors. 
                    //But it may be something the user is defining as a custom color.
                    {
                        string[] elements = rawValue.Split(',').Select(p => p.Trim()).ToArray();
                        if (elements.Length == 3)
                        {
                            int[] values = new int[3];
                            bool haveFailed = _FALSE;
                            for (int i = 0; i <= 2; i++)
                            {
                                if (!Int32.TryParse(elements[i], out values[i]))
                                {
                                    haveFailed = _TRUE;
                                    troubleLogger($", key {targetKey}, element {i} could not be parsed" +
                                        " as an integer.");
                                }
                            }
                            if (haveFailed)
                            //Trouble is already logged. Just head home.
                            { return _FALSE; }
                            else
                            {
                                //Create the new color coder and add it to the dictionary.
                                coder = new ColorCoderMono(new Color(values[0], values[1], values[2]));
                                colorPalette.Add(rawValue, coder);
                                return _TRUE;
                            }
                        }
                        else
                        //If the value doesn't have three elements, we can't make sense of it. Complain.
                        {
                            troubleLogger($", key {targetKey} does not match a pre-defined color and " +
                                $"does not have three elements like a custom color.");
                            return _FALSE;
                        }
                    }
                }
                //If we didn't find something at the designated key, fail silently
                else
                { return _FALSE; }
            }

            //If we're trying to get a hard-coded entry in the dictionary, we can use this, which
            //dispenses with all the checks.
            public IColorCoder getCoderDirect(string name)
            { return colorPalette[name]; }
        }

        //Checks the specified ini section for the specified key. If the key is found, returns true.
        //If the key is absent, generate a new key with a defualt value and potentially log that 
        //this happened.
        private bool tryCheckForDefaultIniKey(string targetSection, string targetKey, string defaultValue,
            bool includeDivider, ref bool configAltered, bool logKeyGeneration, string blockName, LimitedMessageLog textLog)
        {
            MyIniValue iniValue;
            iniValue = _iniReadWrite.Get(targetSection, targetKey);
            if (iniValue.IsEmpty)
            {
                //We didn't even find one of these required keys. Generate a new one.
                _iniReadWrite.Set(targetSection, targetKey, defaultValue);
                if (includeDivider)
                { _iniReadWrite.SetComment(targetSection, targetKey, "-----------------------------------------"); }
                //We won't commmit the change to the PB's CustomData just yet. Instead, we'll set a
                //flag that'll let us know we need to do that back in evaluateInit
                configAltered = _TRUE;
                if (logKeyGeneration)
                {
                    textLog.addNote($"'{targetKey}' key was missing from '{targetSection}' section of " +
                        $"block '{blockName}' and has been re-generated.");
                }
                return _FALSE;
            }
            return _TRUE;
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
                return _TRUE;
            }
            //The second and more obvious scenario is if the name is already in use.
            else if (elementNames.Contains(name))
            {
                textLog.addError($"{declarationSection} tried to use the Element name '{name}', " +
                    $"which has already been claimed. All Element providers (Tally, ActionSet, " +
                    $"Trigger, Raycaster) must have their own, unique names.");
                return _TRUE;
            }
            //Barring those two cases, we're fine, and the element name is not in use.
            else
            { return _FALSE; }
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
            bool state = _FALSE;
            bool badState;
            parsedData.Clear();
            string[] pairs = stateList.Split(',').Select(p => p.Trim()).ToArray();

            foreach (string pair in pairs)
            {

                badState = _FALSE;
                string[] parts = pair.Split(':').Select(p => p.Trim()).ToArray();
                //The first part of the pair is the identifier.
                target = parts[0];
                if (parts.Length < 2)
                {
                    badState = _TRUE;
                    troubleLogger($"{troubleID} does not provide a state for the component " +
                        $"'{target}'. Valid states are 'on' and 'off'.");
                }
                //The second part is the desired state, but it's in the form of on/off
                else if (parts[1].ToLowerInvariant() == "on")
                { state = _TRUE; }
                else if (parts[1].ToLowerInvariant() == "off")
                { state = _FALSE; }
                else
                {
                    badState = _TRUE;
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

        internal ActionPart tryGetPartFromConfig(LimitedMessageLog textLog, string sectionHeader, int index,
            IMyTerminalBlock block, MyIni iniReader, MyIniValue iniValue, PaletteManager colorPalette)
        {
            //_debugDisplay.WriteText("Entering tryGetPartFromConfig\n", _TRUE);
            //Check the config for the presence of the target key
            string propertyKey = $"Action{index}Property";
            //The troubleLogger we'll use to add errors to the textLog. 
            //Because ActionParts are retrieved from the grid, we use a warning.
            Action<string> blockAndSectionWarningLogger = message =>
            { textLog.addWarning($"Block {block.CustomName}, section {sectionHeader}{message}"); };
            iniValue = iniReader.Get(sectionHeader, propertyKey);
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
                    blockAndSectionWarningLogger($" references the unknown property '{propertyName}' " +
                        $"as its {propertyKey}.");
                    retreivedPart = new ActionPart<bool>(propertyName);
                }
                else
                {
                    string valueOn = $"Action{index}ValueOn";
                    string valueOff = $"Action{index}ValueOff";

                    Action<string> getValue = (key) =>
                    { iniValue = iniReader.Get(sectionHeader, key); };

                    if (propertyDef.TypeName.ToLowerInvariant() == "boolean")
                    {
                        ActionPart<bool> typedPart = new ActionPart<bool>(propertyName);
                        bool typedValue;
                        Action<bool, string> handleValue = (isOn, key) =>
                        {
                            getValue(key);
                            if (!iniValue.IsEmpty && iniValue.TryGetBoolean(out typedValue))
                            {
                                typedPart.setValue(isOn, typedValue);
                            }
                        };
                        //20250902: I'd REALLY like to put this bit after all the type branches
                        //have run, and I could almost get away with that. Unfortunately, no
                        //combination of tricks I've tried so far has gotten me to the point
                        //where that would work. So it just goes into each branch.
                        handleValue(_TRUE, valueOn);
                        handleValue(_FALSE, valueOff);
                        retreivedPart = typedPart;
                    }
                    else if (propertyDef.TypeName.ToLowerInvariant() == "int64")
                    {
                        ActionPart<long> typedPart = new ActionPart<long>(propertyName);
                        long typedValue;
                        Action<bool, string> handleValue = (isOn, key) =>
                        {
                            getValue(key);
                            if (!iniValue.IsEmpty && iniValue.TryGetInt64(out typedValue))
                            {
                                typedPart.setValue(isOn, typedValue);
                            }
                        };
                        handleValue(_TRUE, valueOn);
                        handleValue(_FALSE, valueOff);
                        retreivedPart = typedPart;
                    }
                    else if (propertyDef.TypeName.ToLowerInvariant() == "single")
                    {
                        ActionPart<float> typedPart = new ActionPart<float>(propertyName);
                        float typedValue;
                        Action<bool, string> handleValue = (isOn, key) =>
                        {
                            getValue(key);
                            if (!iniValue.IsEmpty && iniValue.TryGetSingle(out typedValue))
                            {
                                typedPart.setValue(isOn, typedValue);
                            }
                        };
                        handleValue(_TRUE, valueOn);
                        handleValue(_FALSE, valueOff);
                        retreivedPart = typedPart;
                    }
                    else if (propertyDef.TypeName.ToLowerInvariant() == "color")
                    {
                        //Colors are a bit different
                        ActionPart<Color> typedPart = new ActionPart<Color>(propertyName);
                        Color typedValue;
                        if (colorPalette.tryGetColorFromConfig(blockAndSectionWarningLogger, iniReader,
                            sectionHeader, $"Action{index}ValueOn", out typedValue))
                        {
                            typedPart.setValue(_TRUE, typedValue);
                        }
                        if (colorPalette.tryGetColorFromConfig(blockAndSectionWarningLogger, iniReader,
                            sectionHeader, $"Action{index}ValueOff", out typedValue))
                        {
                            typedPart.setValue(_FALSE, typedValue);
                        }

                        retreivedPart = typedPart;
                    }
                    else if (propertyDef.TypeName.ToLowerInvariant() == "stringbuilder")
                    {
                        //Strings are even more different than colors. 
                        ActionPartString stringPart = new ActionPartString(propertyName, _sb);
                        string stringValue;
                        Action<bool, string> handleValue = (isOn, key) =>
                        {
                            getValue(key);
                            if (!iniValue.IsEmpty && iniValue.TryGetString(out stringValue))
                            { stringPart.setValue(isOn, stringValue); }
                        };
                        handleValue(_TRUE, valueOn);
                        handleValue(_FALSE, valueOff);
                        retreivedPart = stringPart;
                    }
                    else
                    {
                        //We're throwing an error here, so we can't use one of the loggers.
                        textLog.addError($"Block '{block.CustomName}', discrete section '{sectionHeader}', " +
                            $"references the property '{propertyName}' which uses the non-standard " +
                            $"type {propertyDef.TypeName}. Report this to the scripter, as the script " +
                            $"will need to be altered to handle this.");
                        retreivedPart = new ActionPart<bool>(propertyName);
                    }
                    //The last step is to make sure that we got a value /somewhere/
                    if (!retreivedPart.isHealthy() && propertyDef != null)
                    {
                        blockAndSectionWarningLogger($" does not specify a working Action{index}ValueOn " +
                            $"or Action{index}ValueOff for the property '{propertyName}'. If one was " +
                            $"specified, make sure that it matches the type '{propertyDef.TypeName}.'");
                    }
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
        /*internal ActionPart tryGetPartFromConfig(LimitedMessageLog textLog, string sectionHeader, int index,
            IMyTerminalBlock block, MyIni iniReader, MyIniValue iniValue, PaletteManager colorPalette)
        {
            _debugDisplay.WriteText("Entering tryGetPartFromConfig\n", _TRUE);
            //Check the config for the presence of the target key
            string propertyKey = $"Action{index}Property";
            //The troubleLogger we'll use to add errors to the textLog. 
            //Because ActionParts are retrieved from the grid, we use a warning.
            Action<string> blockWarningLogger = message =>
            { textLog.addWarning($"Block {block.CustomName}, section {sectionHeader}{message}"); };
            iniValue = iniReader.Get(sectionHeader, propertyKey);
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
                    textLog.addWarning($"Block '{block.CustomName}', section '{sectionHeader}', " +
                        $"references the unknown property '{propertyName}' as its {propertyKey}.");
                    retreivedPart = new ActionPart<bool>(propertyName);
                }
                else if (propertyDef.TypeName.ToLowerInvariant() == "boolean")
                {
                    //The process for each type is basically the same
                    ActionPart<bool> typedPart = new ActionPart<bool>(propertyName);
                    bool typedValue = _FALSE;
                    //Check for an valueOn
                    iniValue = iniReader.Get(sectionHeader, $"Action{index}ValueOn");
                    if (!iniValue.IsEmpty && iniValue.TryGetBoolean(out typedValue))
                    { typedPart.setValueOn(typedValue); }
                    //Check for an valueOff
                    iniValue = iniReader.Get(sectionHeader, $"Action{index}ValueOff");
                    if (!iniValue.IsEmpty && iniValue.TryGetBoolean(out typedValue))
                    { typedPart.setValueOff(typedValue); }
                    //Pass this ActionPart out to the un-type'd variable.
                    retreivedPart = typedPart;
                }
                else if (propertyDef.TypeName.ToLowerInvariant() == "stringbuilder")
                {
                    _debugDisplay.WriteText("Entering StringBuilder branch\n", _TRUE);
                    ActionPartString stringPart = new ActionPartString(propertyName, _sb);
                    string stringValue = "";
                    iniValue = iniReader.Get(sectionHeader, $"Action{index}ValueOn");
                    if (!iniValue.IsEmpty && iniValue.TryGetString(out stringValue))
                    { stringPart.setValue(stringValue, _TRUE); }
                    iniValue = iniReader.Get(sectionHeader, $"Action{index}ValueOff");
                    if (!iniValue.IsEmpty && iniValue.TryGetString(out stringValue))
                    { stringPart.setValue(stringValue, _FALSE); }
                    _debugDisplay.WriteText("Operating OnAction via tryTakeAction\n", _TRUE);
                    stringPart.takeAction(block, _TRUE);
                    _debugDisplay.WriteText("Test complete\n", _TRUE);

                    retreivedPart = stringPart;
                }
                /*{
                    _debugDisplay.WriteText("Entering StringBuilder branch\n", _TRUE);
                    ActionPart<MemorySafeStringBuilder> typedPart = new ActionPart<MemorySafeStringBuilder>(propertyName);
                    string typedValue = "";
                    iniValue = iniReader.Get(sectionHeader, $"Action{index}ValueOn");
                    if (!iniValue.IsEmpty && iniValue.TryGetString(out typedValue))
                    {
                        MemorySafeStringBuilder builder = new MemorySafeStringBuilder(typedValue); //This hurts my heart.
                        typedPart.setValueOn(builder);
                        _debugDisplay.WriteText($"ValueOn builder contents: {builder}\n", _TRUE); 
                    }
                    iniValue = iniReader.Get(sectionHeader, $"Action{index}ValueOff");
                    if (!iniValue.IsEmpty && iniValue.TryGetString(out typedValue))
                    {
                        MemorySafeStringBuilder builder = new MemorySafeStringBuilder(typedValue);
                        typedPart.setValueOff(builder);
                        _debugDisplay.WriteText($"ValueOff builder contents:{builder}\n", _TRUE);
                    }
                    _debugDisplay.WriteText("Operating OnAction\n", _TRUE);
                    typedPart.takeAction(block, _TRUE);

                    retreivedPart = typedPart;
                }END COMMENT HERE
                else if (propertyDef.TypeName.ToLowerInvariant() == "int64")
                {
                    ActionPart<long> typedPart = new ActionPart<long>(propertyName);
                    long typedValue = -1;
                    iniValue = iniReader.Get(sectionHeader, $"Action{index}ValueOn");
                    if (!iniValue.IsEmpty && iniValue.TryGetInt64(out typedValue))
                    { typedPart.setValueOn(typedValue); }
                    iniValue = iniReader.Get(sectionHeader, $"Action{index}ValueOff");
                    if (!iniValue.IsEmpty && iniValue.TryGetInt64(out typedValue))
                    { typedPart.setValueOff(typedValue); }

                    retreivedPart = typedPart;
                }
                else if (propertyDef.TypeName.ToLowerInvariant() == "single")
                {
                    ActionPart<float> typedPart = new ActionPart<float>(propertyName);
                    float typedValue = -1;
                    iniValue = iniReader.Get(sectionHeader, $"Action{index}ValueOn");
                    if (!iniValue.IsEmpty && iniValue.TryGetSingle(out typedValue))
                    { typedPart.setValueOn(typedValue); }
                    iniValue = iniReader.Get(sectionHeader, $"Action{index}ValueOff");
                    if (!iniValue.IsEmpty && iniValue.TryGetSingle(out typedValue))
                    { typedPart.setValueOff(typedValue); }

                    retreivedPart = typedPart;
                }
                else if (propertyDef.TypeName.ToLowerInvariant() == "color")
                {
                    //Colors are a bit different
                    ActionPart<Color> typedPart = new ActionPart<Color>(propertyName);
                    Color typedValue;
                    if (colorPalette.tryGetColorFromConfig(blockWarningLogger, iniReader,
                        sectionHeader, $"Action{index}ValueOn", out typedValue))
                    { typedPart.setValueOn(typedValue); }
                    if (colorPalette.tryGetColorFromConfig(blockWarningLogger, iniReader,
                        sectionHeader, $"Action{index}ValueOff", out typedValue))
                    { typedPart.setValueOff(typedValue); }

                    retreivedPart = typedPart;
                }
                else
                {
                    textLog.addError($"Block '{block.CustomName}', discrete section '{sectionHeader}', " +
                        $"references the property '{propertyName}' which uses the non-standard " +
                        $"type {propertyDef.TypeName}. Report this to the scripter, as the script " +
                        $"will need to be altered to handle this.");
                    retreivedPart = new ActionPart<bool>(propertyName);
                }
                //The last step is to make sure that we got a value /somewhere/
                if (!retreivedPart.isHealthy() && propertyDef != null)
                {
                    textLog.addWarning($"Block '{block.CustomName}', discrete section '{sectionHeader}', " +
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
        }*/

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
                    { trigger.setScenario(isLess, value, _TRUE); }
                    else if (commandString == "off")
                    { trigger.setScenario(isLess, value, _FALSE); }
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
        /* 20250515: Discontinued in favor of the new Distributor object.
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
                bool fire = _FALSE;
                //Take a notch off the delayCounter
                delayCounter--;
                //If we've reached the end of the delay counter...
                if (delayCounter < 0)
                {
                    //Reset the counter to the value dictated by the updateDelay
                    delayCounter = updateDelay;
                    //It's time to perform an update. Set 'fire' to true.
                    fire = _TRUE;
                }
                return fire;
            }
        }*/

        internal class Distributor
        {
            Dictionary<string, PeriodicEvent> periodics;
            Dictionary<string, Cooldown> cooldowns;

            internal Distributor()
            {
                periodics = new Dictionary<string, PeriodicEvent>();
                cooldowns = new Dictionary<string, Cooldown>();
            }

            internal bool tryAddPeriodic(string name, PeriodicEvent periodic)
            {
                if (periodics.ContainsKey(name))
                { return _FALSE; }
                else
                {
                    periodics.Add(name, periodic);
                    return _TRUE;
                }
            }

            internal void addOrReplacePeriodic(string name, PeriodicEvent periodic)
            {
                if (periodics.ContainsKey(name))
                { periodics[name] = periodic; }
                else
                { periodics.Add(name, periodic); }
            }

            internal int getPeriodicFrequency(string name)
            //{ return periodics[name]?.frequency ?? -1; } //Doesn't work? Doesn't null coalesce, at least
            {
                PeriodicEvent targetEvent;
                if (periodics.TryGetValue(name, out targetEvent))
                { return targetEvent.frequency; }
                else
                { return -1; }
            }

            internal void removePeriodic(string name)
            { periodics.Remove(name); }

            internal void clearPeriodics()
            { periodics.Clear(); }

            internal bool tryAddCooldown(string name, int duration, out string result)
            {
                Cooldown existing;
                if (!cooldowns.TryGetValue(name, out existing))
                {
                    Cooldown newCooldown = new Cooldown(duration, name);
                    cooldowns.Add(name, newCooldown);
                    result = "";
                    return _TRUE;
                }
                else
                {
                    result = existing.getTimeRemainingMessage();
                    return _FALSE;
                }
            }

            internal void tic()
            {
                foreach (PeriodicEvent periodic in periodics.Values)
                { periodic.tic(); }
                //Uses a while loop to avoid a 'collection modified' exception when we remove finished
                //cooldowns.
                Cooldown cooldown;
                int index = 0;
                while (index < cooldowns.Count)
                {
                    cooldown = cooldowns.Values.ElementAt(index);
                    if (cooldown.isFinished())
                    { cooldowns.Remove(cooldown.name); }
                    else
                    { index++; }
                }
                /*
                foreach (Cooldown cooldown in cooldowns.Values)
                {
                    if (cooldown.isFinished())
                    { cooldowns.Remove(cooldown.name); }
                }*/
            }

            public string checkCooldown(string name)
            {
                Cooldown cooldown;
                if (cooldowns.TryGetValue(name, out cooldown))
                { return cooldown.getTimeRemainingMessage(); }
                else
                { return $"{name} is not on cooldown."; }
            }

            internal string debugPrintContents()
            {
                string outcome = "Contained periodics:\n";
                foreach (KeyValuePair<string, PeriodicEvent> pair in periodics)
                { outcome += $" -{pair.Key} with frequency {pair.Value.frequency}\n"; }
                outcome += "Contained cooldowns:\n";
                foreach (KeyValuePair<string, Cooldown> pair in cooldowns)
                { outcome += $" -{pair.Key} with a remaining duration of {pair.Value.position}\n"; }
                return outcome;
            }
        }

        internal class PeriodicEvent
        {
            internal int frequency { get; private set; }
            int position;
            Action onFire;

            internal PeriodicEvent(int frequency, Action onFire)
            {
                this.frequency = frequency;
                position = frequency;
                this.onFire = onFire;
            }

            internal void tic()
            {
                position--;
                if (position <= 0)
                {
                    position = frequency;
                    onFire.Invoke();
                }
            }
        }

        internal class Cooldown
        {
            public int position { get; private set; }
            //Temporary? I need the name of the cooldown to remove it from the dictionary of cooldowns,
            //but I suspect it's going to pitch a fit when I try.
            internal string name { get; private set; }

            internal Cooldown(int duration, string name)
            {
                position = duration;
                this.name = name;
            }

            internal bool isFinished()
            {
                position--;
                if (position <= 0)
                { return _TRUE; }
                else
                { return _FALSE; }
            }

            internal string getTimeRemainingMessage()
            { return $"{name} is on cooldown for the next {(int)(position * 1.4)} seconds."; }
        }

        internal void setUpdateDelay(int delay = 0, Distributor distributor = null)
        {
            Action updateAction = () =>
            {
                compute();
                update();
                _log.tic();
            };
            PeriodicEvent periodicUpdate = new PeriodicEvent(delay, updateAction);
            _distributor.addOrReplacePeriodic("UpdateDelay", periodicUpdate);
            _log.scriptUpdateDelay = delay;
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

        public static string listToMultiLine(List<string> elements, int elementsPerLine = 3, bool isRawText = _TRUE)
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
        { string writeConfig(); }

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
                        return _FALSE;
                }
                return _TRUE;
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
                maxForced = _FALSE;
                doNotReconstitute = _FALSE;
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
                maxForced = _TRUE;
            }

            //compute compiles and analyzes data from this tally's subject blocks, then builds the 
            //strings and sets the colors needed to display that data
            internal abstract void compute();

            internal string writeCommonConfig(string childConfig)
            {
                double DEFAULT_MULTIPLIER = 1;

                //First up is the section header
                string config = $"[{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.Tally.{newLineToMultiLine(programName)}]\n";

                //Next comes the displayName
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
            public abstract string writeConfig();
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

            public override string writeConfig()
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
                return writeCommonConfig(config);
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

            public override string writeConfig()
            {
                //name type max? displayName multiplier lowgood

                //TallyCargos are almost easier than TallyGenerics
                string config = "Type = Inventory\n";
                //TallyCargos generally use ColorCoderLow.
                if (!(colorCoder is ColorCoderLow))
                { config += $"ColorCoder = {colorCoder.getConfigPart()}\n"; }

                //Everything else is handled in the general config method.
                return writeCommonConfig(config);
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

            public override string writeConfig()
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
                return writeCommonConfig(config);
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
            public abstract void takeAction(IMyTerminalBlock block, bool isOnAction);
            //Debug methods
            /*
            public abstract Type getPropertyType();
            public abstract string getAllTypes();
            */
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
                hasOn = _FALSE;
                hasOff = _FALSE;
            }

            public void setValue(bool isOn, T value)
            {
                if (isOn)
                {
                    valueOn = value;
                    hasOn = _TRUE;
                }
                else
                {
                    valueOff = value;
                    hasOff = _TRUE;
                }
            }
            /*
            public void setValueOn(T value)
            {
                valueOn = value;
                hasOn = _TRUE;
            }

            public void setValueOff(T value)
            {
                valueOff = value;
                hasOff = _TRUE;
            }
            */
            public override bool isHealthy()
            { return hasOn || hasOff; }

            public override void takeAction(IMyTerminalBlock block, bool isOnAction)
            {
                if (isOnAction && hasOn)
                { block.SetValue<T>(propertyID, valueOn); }
                else if (!isOnAction && hasOff)
                { block.SetValue<T>(propertyID, valueOff); }
            }

            /*
            //Debug method. Not used in code.
            public override Type getPropertyType()
            { return typeof(T); }

            public override string getAllTypes()
            {
                return $"T: {typeof(T)}, ValueOn: {valueOn?.GetType().ToString() ?? "<null>"}, " +
                    $"ValueOff: {valueOff?.GetType().ToString() ?? "<null>"}";
            }
            */
        }

        //An alternative version of ActionPart specifically for working with strings.
        public class ActionPartString : ActionPart
        {
            //A reference to the global StringBuilder.
            StringBuilder sb;
            //The property this ActionPart will target
            string propertyID;
            //The strings this ActionPart will use for on and off actions.
            private string valueOn, valueOff;
            //Strings can be nulled, so we'll skip hasOn and hasOff
            //private bool hasOn, hasOff;

            public ActionPartString(string propertyID, StringBuilder globalSB)
            {
                sb = globalSB;
                this.propertyID = propertyID;
                valueOn = null;
                valueOff = null;
            }

            public void setValue(bool isOn, string value)
            {
                if (isOn)
                { valueOn = value; }
                else
                { valueOff = value; }
            }

            public override bool isHealthy()
            { return valueOn != null || valueOff != null; }

            public override void takeAction(IMyTerminalBlock block, bool isOnAction)
            {
                //20251120: Keen recently replaced a bunch of objects with MemorySafe variants. But 
                //we still write code with the plain'ole C# objects; the conversion occurs somewhere 
                //under the hood, possibly when a variable goes out of scope. But when SetValue is 
                //dealing with strings, it expects a StringBuilder, not a MemorySafeStringBuilder. 
                //Thus this cast, to ensure that what we're handing to SetValue is in fact a StringBuilder
                //in the moment we hand it over.
                //The other solution I managed to get working involved creating new StringBuilders
                //each moment I needed to set a string value. But that hurt my heart.
                StringBuilder castSB = (StringBuilder)sb;
                if (isOnAction && valueOn != null)
                { block.SetValue(propertyID, castSB.Append(valueOn)); }
                if (!isOnAction && valueOff != null)
                { block.SetValue(propertyID, castSB.Append(valueOff)); }
                sb.Clear();
            }
            /*
            //Debug method. Not used in code.
            public override Type getPropertyType()
            { return typeof(string); }

            public override string getAllTypes()
            { return "Strings all the way down."; }
            */
        }

        //SCRAP: The original, test version of APS.
        //An alternative version of ActionPart specifically for working with strings.
        /*public class ActionPartString : ActionPart
        {
            StringBuilder sb;
            //The property this ActionPart will target
            string propertyID;
            //The strings this ActionPart will use for on and off actions.
            private string valueOn, valueOff;
            //Strings can be nulled, so we'll skip hasOn and hasOff
            //private bool hasOn, hasOff;

            public ActionPartString(string propertyID)
            {
                sb = new StringBuilder();
                this.propertyID = propertyID;
                valueOn = null;
                valueOff = null;
            }

            public void setValue(string value, bool isOn)
            {
                if (isOn)
                { valueOn = value; }
                else
                { valueOff = value; }
            }

            public override bool isHealthy()
            { return valueOn != null || valueOff != null; }

            //First build: To get as close as I know how to something I know works, StringBuilders 
            //will be instasiated the moment they're needed.
            public override void takeAction(IMyTerminalBlock block, bool isOnAction)
            {
                if (isOnAction && valueOn != null)
                { block.SetValue(propertyID, new StringBuilder(valueOn)); }
                if (!isOnAction && valueOff != null)
                { block.SetValue(propertyID, new StringBuilder(valueOff)); }
            }

            //Alternative approach: In an attempt to avoid stressing the garbage collector, we'll
            //pass a reference to a stored StringBuilder to SetValue. Hopefully the cast will keep
            //it a string builder just long enough to get the job done. 
            public void takeActionCast(IMyTerminalBlock block, bool isOnAction)
            {
                StringBuilder castSB = (StringBuilder)sb;
                if (isOnAction && valueOn != null)
                { block.SetValue(propertyID, castSB.Append(valueOn)); }
                if (!isOnAction && valueOff != null)
                { block.SetValue(propertyID, castSB.Append(valueOff)); }
                sb.Clear();
            }

            //Debug method. Not used in code.
            public override Type getPropertyType()
            { return typeof(string); }

            public override string getAllTypes()
            { return "Strings all the way down."; }
        } */

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
                subjectMFD = subject;
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
                { subjectMFD.trySetPageByName(pageOn); }
                else
                { subjectMFD.trySetPageByName(pageOff); }
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
                scanOn = _FALSE;
                scanOff = _FALSE;
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
            public const bool ON_STATE = _TRUE;
            public const bool OFF_STATE = _FALSE;
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
                hasOn = _FALSE;
                hasOff = _FALSE;
            }

            public void setReactionToOn(bool action)
            {
                reactionToOn = action;
                hasOn = _TRUE;
            }

            public void setReactionToOff(bool action)
            {
                reactionToOff = action;
                hasOff = _TRUE;
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
            public const bool ARM = _TRUE;
            public const bool DISARM = _FALSE;
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
                reactionToOn = _FALSE;
                reactionToOff = _FALSE;
                hasOn = _FALSE;
                hasOff = _FALSE;
            }

            public void setReactionToOn(bool action)
            {
                reactionToOn = action;
                hasOn = _TRUE;
            }

            public void setReactionToOff(bool action)
            {
                reactionToOff = action;
                hasOff = _TRUE;
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
            //UpdateDistributor distributor;
            //The method we use to manipulate the script's update delay is in the Program itself.
            //We'll need a reference to it.
            Program program;
            //How long the delay will be when this ActionPlan is on
            public int delayOn { get; internal set; }
            //How long the delay will be when this ActionPlan is off
            public int delayOff { get; internal set; }

            public ActionPlanUpdate(Program program)
            {
                this.program = program;
                delayOn = 0;
                delayOff = 0;
            }

            //Sets the delay on the distributor to one of the stored delay times. Passing in true 
            //will set the delay to delayOn, false will set it to delayOff.
            public void takeAction(bool isOnAction)
            {
                if (isOnAction)
                { program.setUpdateDelay(delayOn); }
                else
                { program.setUpdateDelay(delayOff); }
            }

            //Determine if this ActionPlan has any actions defined (This will probably never be called)
            public bool hasAction()
            { return delayOn != 0 || delayOff != 0; }

            public string getIdentifier()
            { return "The Distributor"; }

            public string writeConfig()
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

            public string writeConfig()
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
                hasActed = _FALSE;
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
                hasActed = _TRUE;
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
            { hasActed = _FALSE; }

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

            public string writeConfig()
            {
                //Default values for this config. 
                Color DEFAULT_COLOR_ON = green;
                Color DEFAULT_COLOR_OFF = red;
                string DEFAULT_TEXT_ON = "Enabled";
                string DEFAULT_TEXT_OFF = "Disabled";
                //The string that will hold our finished config.
                string config = $"[{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.ActionSet.{newLineToMultiLine(programName)}]\n";
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
                            config += $"{((IHasConfig)currPlan).writeConfig()}";
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
            internal Tally targetTally { private get; set; }
            //The ActionSet this Trigger will operate
            internal ActionSet targetSet { private get; set; }
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

            public Trigger(string programName, bool initialState)
            {
                targetTally = null;
                targetSet = null;
                commonSetup(programName, initialState);
            }

            public Trigger(string programName, Tally targetTally, ActionSet targetSet, bool initialState)
            {
                this.targetTally = targetTally;
                this.targetSet = targetSet;
                commonSetup(programName, initialState);
            }

            private void commonSetup(string programName, bool initialState)
            {
                this.programName = programName;
                greaterOrEqual = -1;
                lessOrEqual = -1;
                commandGreater = _FALSE;
                commandLess = _FALSE;
                hasGreater = _FALSE;
                hasLess = _FALSE;
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
                    hasLess = _TRUE;
                }
                else
                {
                    greaterOrEqual = value;
                    commandGreater = command;
                    hasGreater = _TRUE;
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
                desiredState = _FALSE;
                if (enabled)
                {
                    //If our Greater command is configured, our set isn't already in the Greater 
                    //state, and we have exceeded our threshold...
                    if (hasGreater && targetSet.isOn != commandGreater && targetTally.percent >= greaterOrEqual)
                    {
                        linkedSet = targetSet;
                        desiredState = commandGreater;
                        return _TRUE;
                    }
                    else if (hasLess && targetSet.isOn != commandLess && targetTally.percent <= lessOrEqual)
                    {
                        linkedSet = targetSet;
                        desiredState = commandLess;
                        return _TRUE;
                    }
                }
                return _FALSE;
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

            public string writeConfig()
            {
                string config = $"[{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.Trigger.{newLineToMultiLine(programName)}]\n";
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
            internal RaycasterModuleBase scanModule { private get; set; }
            //The data struct that will hold information about the last entity we detected
            //TODO: Monitor. I don't think entityInfo serves a purpose that the report string doesn't
            //MyDetectedEntityInfo entityInfo;
            //Holds the report on the last entity detected, or informs that no entity was detected.
            string report;
            //Flag indicating if we've recently performed a scan.
            internal bool hasUpdate { get; private set; }
            internal string programName { get; private set; }

            public Raycaster(StringBuilder _sb, string programName)
            { commonSetup(_sb, programName); }

            public Raycaster(StringBuilder _sb, RaycasterModuleBase scanModule, string programName)
            {
                this.scanModule = scanModule;
                commonSetup(_sb, programName);
            }

            private void commonSetup(StringBuilder _sb, string programName)
            {
                this._sb = _sb;
                this.programName = programName;
                report = $"{DateTime.Now.ToString("HH:mm:ss")}- Raycaster {programName} " +
                          $"reports: No data";
                hasUpdate = _FALSE;
            }

            public void addCamera(IMyCameraBlock camera)
            {
                scanModule.addCamera(camera);
                camera.EnableRaycast = _TRUE;
            }

            public double getModuleRequiredCharge()
            //Raycasters can be created without modules, so we might not be able to get this.
            //A null check is required.
            { return scanModule?.requiredCharge ?? -1; }

            public void scan()
            {
                MyDetectedEntityInfo entityInfo;
                double scanRange;
                //We've offloaded all the scanning work to the scanning module. Tell it to do the thing.
                IMyCameraBlock camera = scanModule.scan(out entityInfo, out scanRange);
                //We've offloaded the report writing to a different method. Tell it to do the thing.
                writeReport(entityInfo, scanRange, camera);
                //No matter what happened, set hasUpdate to true.
                hasUpdate = _TRUE;
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
            { hasUpdate = _FALSE; }

            public string toString()
            { return report; }

            public string writeConfig()
            {
                //The string that will hold our finished config.
                string config = $"[{_SCRIPT_PREFIX}.{_DECLARATION_PREFIX}.Raycaster.{newLineToMultiLine(programName)}]\n";
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
                camera.EnableRaycast = _TRUE;
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
            //Used to force sprite syncing for reports. Other objects get dummy methods.
            void refresh();
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
            //The actual Reportable this MFD is currently displaying.
            private IReportable currentPage;

            public MFD(string programName)
            {
                this.programName = programName;
                pages = new Dictionary<string, IReportable>(StringComparer.OrdinalIgnoreCase);
                pageNumber = 0;
                pageName = "";
                currentPage = null;
            }

            //Add a page to this MFD.
            public void addPage(string name, IReportable reportable)
            {
                pages.Add(name, reportable);
                if (currentPage == null)
                {
                    currentPage = reportable;
                    pageName = name;
                }
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
                //pageName = pages.ToArray()[pageNumber].Key;
                pageName = pages.Keys.ToArray()[pageNumber];

                finalizePage();
            }

            //Go to the page with the specified name
            public bool trySetPageByName(string name)
            {
                //If the page is actually in this MFD...
                if (pages.ContainsKey(name))
                {
                    pageName = name;
                    //Get the index of whatever page we've ended up at.
                    pageNumber = pages.Keys.ToList().IndexOf(name);

                    finalizePage();
                    return _TRUE;
                }
                else
                { return _FALSE; }
            }

            private void finalizePage()
            {
                IReportable newPage = pages[pageName];
                bool needsRefresh = _FALSE;
                //There is a very specific bug that ocurrs when flipping from a Keen app to one of
                //Shipware's reports, which can be handled by refreshing the report after it's drawn.
                //This is where we decide if we need to do that.
                if (currentPage is GameScript && newPage is Report)
                { needsRefresh = _TRUE; }
                currentPage = newPage;
                setProfile();
                //Attempt to update the surface right now to show the new page.
                /* CHECK: Removed in an attempt to convince servers to send clients new sptires.
                 * 20250515: Re-enabled in light of the newly added sprite refresh system.
                 */
                update();

                if (needsRefresh)
                { refresh(); }
            }

            //To set the surface up for the MFD, we simply call setProfile on whatever page we're on.
            public void setProfile()
            { currentPage.setProfile(); }

            //To update the MFD, we simply call update on whatever page we're on.
            public void update()
            { currentPage.update(); }

            public void forceUpdate()
            { currentPage.forceUpdate(); }

            public void refresh()
            { currentPage.refresh(); }
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

            //Keen's scripts run on the client, so they don't need to be refreshed.
            public void refresh()
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
            //To force synchronization of sprites to clients in multiplayer, we alter the number
            //of sprites drawn. This boolean tracks if the 'refresh sprite' is currently being
            //included or not.
            bool includeRefreshSprite;

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
                //We won't include a refresh sprite until we're explicitly told to do so. 
                includeRefreshSprite = _FALSE;
            }

            public void setAnchors(int columns, float padLeft, float padRight,
                float padTop, float padBottom, bool titleObeysPadding, StringBuilder _sb)
            {
                //Malware's code for determining the viewport offset, which is the difference 
                //between an LCD's texture size and surface size. 
                //For quite a while I thought it was doing something magical, but no, it really is 
                //just the upper-left corner and the height and width.
                //Initially, it describes where the viewport (Our visible screen area) lies on the 
                //texture (The square image that we actually draw things on, which is always larger
                //or equal to the surface size). But adjusting the position and size of the viewport 
                //is also how we'll apply padding values to this report.
                RectangleF viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f,
                    surface.SurfaceSize);

                //To apply the left and top offsets, we need to adjust the anchor point and then trim the
                //height and width to compensate.
                float offsetLeft = (padLeft / 100) * surface.SurfaceSize.X;
                float offsetTop = (padTop / 100) * surface.SurfaceSize.Y;
                viewport.X += offsetLeft;
                viewport.Width -= offsetLeft;
                viewport.Y += offsetTop;
                viewport.Height -= offsetTop;

                //For the right and bottom, all we need to do is reduce height and width.
                viewport.Width -= (padRight / 100) * surface.SurfaceSize.X;
                viewport.Height -= (padBottom / 100) * surface.SurfaceSize.Y;

                _sb.Clear();
                //Assume there's no title
                float titleHeight = 0;
                if (!string.IsNullOrEmpty(title))
                {
                    //Figure out how much vertical space the title's text requires.
                    _sb.Append(title);
                    titleHeight = surface.MeasureStringInPixels(_sb, font, fontSize).Y;
                    //Create the titleAnchor that we'll lash the title sprite to.
                    if (titleObeysPadding)
                    { titleAnchor = new Vector2(viewport.Width / 2 + viewport.X, viewport.Y); }
                    else
                    {
                        //We've already applied the padding values to the viewport. If we want the
                        //title anchor to be independent of those but still take into account any
                        //differences between TextureSize and SurfaceSize, we need to go back to 
                        //the original numbers.
                        titleAnchor = new Vector2(surface.TextureSize.X / 2,
                            (surface.TextureSize.Y - surface.SurfaceSize.Y) / 2);
                        //The element anchors will need to be positioned so they don't overlap with 
                        //the title. But if the padding value is large enough, that won't be an issue.
                        titleHeight = Math.Max(titleHeight - padTop, 0);
                    }
                }

                //With the title addressed, we can start gathering the information we need to build
                //the rest of the anchors.
                int rows = (int)(Math.Ceiling((double)elements.Count() / columns));
                float columnWidth = viewport.Width / columns;
                float rowHeight = (viewport.Height - titleHeight) / rows;
                int rowCounter = 1;
                Vector2 sectorCenter, anchor, elementSize;

                //We start off by figuring where the center of the first grid sector is. Where exactly
                //the anchor will be placed we'll work out in the first part of the loop.
                sectorCenter = new Vector2(columnWidth / 2, rowHeight / 2);
                sectorCenter += viewport.Position;
                sectorCenter.Y += titleHeight;

                for (int i = 0; i < elements.Count(); i++)
                {
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
                        //20250129: Apply the X portion of the viewport offset to correctly find the 
                        //leftmost edge of the screen. The offset does not need to be applied to the 
                        //Y value, because it's ultimately based off the initial positioning of the 
                        //first row, which does receive the offset.
                        sectorCenter.X += viewport.Position.X;
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
                    //If we're currently drawing the refresh sprite, we'll start with it.
                    if (includeRefreshSprite)
                    {
                        //Both the position and the size of the sprite will be 0, 0.
                        Vector2 zeroes = new Vector2(0, 0);
                        sprite = MySprite.CreateSprite("IconEnergy", zeroes, zeroes);
                        frame.Add(sprite);
                    }
                    //Next comes the title sprite, if available
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

            //When run, forces the server and client to re-sync this report's sprites on the next 
            //call to update(). Has no effect in single player (probably)
            public void refresh()
            { includeRefreshSprite = !includeRefreshSprite; }

            //Prepare this surface for displaying the report.
            public void setProfile()
            {
                //TESTING PURPOSES
                surface.ContentType = ContentType.SCRIPT;
                surface.Script = "";
                surface.ScriptForegroundColor = foreColor;
                surface.ScriptBackgroundColor = backColor;
            }

            //The code for the 'thunderbolt transitions', previously in the setProfile method above.
            //It's been completely replaced by the new Sprite Refresher state machine implementation,
            //but I just couldn't bear to part with it. Stupid nostolgia.
            /*
            public void transition()
            {
                //To save on data transfer in multiplayer, the server only sends out updated sprites
                //if they've 'changed enough'. But sometimes (Especially when using MFDs), our 
                //elements won't meet that bar. So this iteration of setProfile will draw a throwaway
                //image in place of each of the elements, hopefully prompting the server to send out
                //the new sprites.
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
            }*/
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
                oneTimeUpdate = _TRUE;
            }

            public string getData()
            //Pull the CustomData from this block
            { return block.CustomData; }

            public bool hasUpdate()
            {
                bool result = oneTimeUpdate;
                oneTimeUpdate = _FALSE;
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
                { return _FALSE; }
                else
                {
                    //Store the new info in the oldInfo
                    oldInfo = getData();
                    //Indicate that we have an update.
                    return _TRUE;
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
            { return _FALSE; }
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

            //I'm not sure how exactly text surfaces work. I think that under the hood, they're
            //essentially sprites, yet they never seem to have any probelm with text syncing.
            //Perhaps the text is sent to clients, and then the sprite is rendered locally?
            //Anyway, WOTs don't need to do anything when refresh is called.
            public void refresh()
            { }

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
                { return _FALSE; }
                else
                {
                    subjects.Add(prospect);
                    return _TRUE;
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
                    return _TRUE;
                }
                else
                {
                    //If it isn't a tank, we'll still accept it if it's a hydrogen engine.
                    IMyPowerProducer prospectiveEngine = block as IMyPowerProducer;
                    //All of the vanilla hydrogen engines end with HydrogenEngine. Any modded blocks 
                    //that follow this convention will also work with this tally. Assuming their 
                    //DetailInfo is formatted the same way...
                    //UPDATE 20250703: Added a check specifically for the new fusion reactors. 
                    if (prospectiveEngine != null
                        && (prospectiveEngine.BlockDefinition.SubtypeId.EndsWith("HydrogenEngine")
                         || prospectiveEngine.BlockDefinition.SubtypeId == "LargePrototechReactor"))
                    {
                        engines.Add(prospectiveEngine);
                        return _TRUE;
                    }
                    //If it isn't a GasTank and it isn't a Hydrogen engine, we don't want it.
                    else
                    { return _FALSE; }
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
                { return _FALSE; }
                else
                {
                    subjects.Add(prospect);
                    return _TRUE;
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
                { return _FALSE; }
                else
                {
                    subjects.Add(prospect);
                    return _TRUE;
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
                { return _FALSE; }
                else
                {
                    subjects.Add(prospect);
                    return _TRUE;
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
                { return _FALSE; }
                else
                {
                    subjects.Add(prospect);
                    return _TRUE;
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
                return _TRUE;
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
                { return _FALSE; }
                else
                {
                    subjects.Add(prospect);
                    return _TRUE;
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
                { return _FALSE; }
                else
                {
                    subjects.Add(prospect);
                    return _TRUE;
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
            { return "PistonExtension"; }
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
                { return _FALSE; }
                else
                {
                    subjects.Add(prospect);
                    return _TRUE;
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
                { return _FALSE; }
                else
                {
                    subjects.Add(prospect);
                    return _TRUE;
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
                { return _FALSE; }
                else
                {
                    subjects.Add(prospect);
                    return _TRUE;
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
                { return _FALSE; }
                else
                {
                    subjects.Add(prospect);
                    return _TRUE;
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
            //Current status of any active state machines.
            public string machineStatus { private get; set; }
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

            public EventLog(StringBuilder _sb, string title, bool showTicWidget = _FALSE, int maxEntries = 5)
            {
                log = new List<string>();
                this._sb = _sb;
                entriesText = "";
                this.title = title;
                scriptTag = "";
                machineStatus = "";
                this.maxEntries = maxEntries;
                hasUpdate = _FALSE;
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
                hasUpdate = _TRUE;
            }

            //Sets the 'updated' flag to false. Call after pulling the new log.
            public void updateClaimed()
            { hasUpdate = _FALSE; }

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
                //MachineStatus is an empty string if there's no status to report, so we can just
                //plug this in directly.
                _sb.Append($"{machineStatus}\n");
                //Get the entriesText and tack it on
                _sb.Append(entriesText);
                //That's our log. Just need to clear the global StringBuilder out before we go.
                string logOut = _sb.ToString();
                _sb.Clear();
                return logOut;
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
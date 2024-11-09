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
    //ARCHIVE: Created just prior to altering tallies to reference a global StringBuilder instead
    //  of each having their own

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
        //The default ID of the script, to be used if no custom ID is set.
        const string DEFAULT_ID = "ShipWare";
        //The ID for this instance of the script. 
        string ID;
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
        //The indicators that change color based on what a tally is doing
        Indicator[] indicators;
        //An EventLog that will... log events.
        Hammers.EventLog log;
        //Listens for Inter-Grid Communication
        IMyBroadcastListener listener;
        //Used to read information out of a block's CustomData
        MyIni iniReader;
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
            initiate();
            evaluate();
            //The main method Echos the event log every time it finishes running. But there's a lot
            //of stuff that can go wrong when parsing configuration, so we need an Echo here as well.
            Echo(log.toString());
        }

        public void Save()
        {
            //Clear out any data that may be lurking in the iniReader (It should be clear already, 
            //but why not be thorough?)
            iniReader.Clear();
            //Store the ID of this instance of the script in the Config section.
            iniReader.Set("Config", "ID", ID);
            //For every ActionSet named in our sets dictionary...
            foreach (string setName in sets.Keys)
            //Add an entry to the ActionSets section, with the name of the set as the key, storing
            //the current status of the set.
            { iniReader.Set("ActionSets", setName, sets[setName].state); }
            //For every MFD named in our sets dictionary...
            foreach (string MFDName in MFDs.Keys)
            //Add an entry to the MFDs section, with the name of the MFD as the key, storing
            //the current page shown by the MFD.
            { iniReader.Set("MFDs", MFDName, MFDs[MFDName].pageName); }
            //Commit the contents of the iniReader to the Storage string
            Storage = iniReader.ToString();
        }

        public void Main(string argument, UpdateType updateSource)
        {
            //Is this the update tic?
            if (updateSource == UpdateType.Update100)
            {
                //Notify the distributor that a tic has occurred. If it's time for an update...
                if (distributor.tic())
                {
                    //...by all means, do the update.
                    compute();
                    update();
                    //And tell the log about it
                    log.tic();
                }
            }
            //Is this the IGC wanting attention?
            else if (updateSource == UpdateType.IGC)
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
                    string command = argReader.Argument(0);
                    switch (command)
                    {
                        //A slightly less dead-simple method for passing messages to the IGC
                        //Sends the entirety of the command, minus the word "IGC" and the data tag.
                        //Also hopefully allows the user to include all the spaces their little 
                        //heart desires.
                        //Argument format: IGC <tag> <data>
                        //Argument example: IGC RemoteStart GateBay1
                        case "IGC":
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
                        case "MFD":
                            //If the user has given us the correct number of arguments...
                            if (argReader.ArgumentCount == 3)
                            {
                                string MFDTarget = argReader.Argument(1);
                                string MFDPageCommand = argReader.Argument(2);
                                //If we actually know what MFD the user is talking about...
                                if (MFDs.ContainsKey(MFDTarget))
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
                        case "Action":
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
                        //Populate is crazy useful, but I'm going to put all my effort into getting
                        //it working for ShipManager.
                        case "Populate":
                            log.add("The 'Populate' command has not been implemented for this script.");
                            break;
                        //Change the ID of this script, and updates the configuration of every block 
                        //on the grid to use the new ID.
                        //Argument format: ChangeID <name>
                        //Argument example: ChangeID Komodo
                        case "ChangeID":
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
                                //Now that we've replaced the old tag in the config, go ahead and 
                                //update the tag in memory
                                ID = newID;
                                //The best way to make sure this sticks and then works properly 
                                //afterward is to fully re-initialize the script.
                                Save();
                                initiate();
                                evaluate();
                                log.add($"ChangeID complete, {blocks.Count} blocks modified. The ID " +
                                    $"of this script instance is now '{ID}', and its tag is now '{tag}'.");
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
                        //Change the ID of this script, and updates the configuration of every block 
                        //on the grid to use the new ID. This version is used to convert old 
                        //configurations that used the [Capacity] tag to the new system.
                        //Argument format: ChangeIDOld <name>
                        //Argument example: ChangeIDOld Komodo
                        //TODO: Delete this prior to release
                        case "ChangeIDOld":
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
                                    (b.IsSameConstructAs(Me) && MyIni.HasSection(b.CustomData, "Capacity")));
                                //For every block that we found...
                                foreach (IMyTerminalBlock block in blocks)
                                //Replace instances of the old tags with the new tag. Include the 
                                //brackets to decrease the odds of us hitting something we shouldn't
                                { block.CustomData = block.CustomData.Replace($"[Capacity]", $"[{newTag}]"); }
                                //Now that we've replaced the old tag in the config, go ahead and 
                                //update the tag in memory
                                ID = newID;
                                //The best way to make sure this sticks and then works properly 
                                //afterward is to fully re-initialize the script.
                                Save();
                                initiate();
                                evaluate();
                                log.add($"ChangeID complete, {blocks.Count} blocks modified. The ID " +
                                    $"of this script instance is now '{ID}', and its tag is now '{tag}'.");
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
                        case "Evaluate":
                            //Evaluate will pull the state of ActionSets from the Storage string, 
                            //better make sure that's up to date
                            Save();
                            //Now we should be able to safely call Evaluate.
                            evaluate();
                            break;
                        //If the user just /has/ to have an update, right now, for some reason, we
                        //can accomodate them.
                        case "Update":
                            compute();
                            update();
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
            if (updateSource == UpdateType.Update100)
            { log.updateClaimed(); }
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

        //Attempt to operate an action set. Errors will be added to the event log if this is 
        //  unsuccessful. Returns true if it thinks it successfully carried out the command.
        //string actionTarget: The name of the ActionSet the user is trying to operate.
        //string actionCommand: The command that is to be performed on the ActionSet.
        //string source: The source of the command, used to make error messages more informative.
        //  Left blank for run commands, "IGC-directed " (Note the space!) for the IGC.
        public string tryTakeAction(string actionTarget, string actionCommand, string source)
        {
            //Stores what we think is the result of this method running.
            string outcome = "";
            bool fired = true;
            //If we actually know what ActionSet the user is talking about...
            if (sets.ContainsKey(actionTarget))
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

        public void initiate()
        {
            //Initiate some bits and pieces, though most of the work will be done in evaluate()
            iniReader = new MyIni();
            argReader = new MyCommandLine();
            _sb = new StringBuilder();
            //One of the first things we need to do is figure out if this script has a custom tag.
            //To do that, we check the Storage string.
            iniReader.TryParse(Storage);
            //Try to pull the ID from the Config section of the Storage string, using the default 
            //ID if nothing is found.
            ID = iniReader.Get("Config", "ID").ToString(DEFAULT_ID);
            //Build the tag by combining the constant PREFIX and the user-modifiable ID
            tag = $"{PREFIX}.{ID}";
            //Now that we have the tag, we can start instansiating the stuff that needs it.
            listener = IGC.RegisterBroadcastListener(tag);
            listener.SetMessageCallback(tag);
            //The log that will give us feedback in the PB's Detail Info area
            log = new Hammers.EventLog("Ship Manager - Recent Events", true);
            //If we have a custom tag, we want to have that information front and center in the log
            if (tag != $"{PREFIX}.{DEFAULT_ID}")
            { log.scriptTag = tag; }
            //The distributer that handles updateDelays
            distributor = new UpdateDistributor(log);
            //Assure the user that we made it this far.
            log.add("Script initialization complete.");
        }

        public Dictionary<string, Action<IMyTerminalBlock>> compileActions()
        {
            Dictionary<string, Action<IMyTerminalBlock>> actions = new Dictionary<string, Action<IMyTerminalBlock>>();
            //The actions that can be performed by this script, in no particular order:
            //Functional Blocks
            actions.Add("enableOn", b => ((IMyFunctionalBlock)b).Enabled = true);
            actions.Add("enableOff", b => ((IMyFunctionalBlock)b).Enabled = false);
            //Battery Blocks
            actions.Add("batteryAuto", b => ((IMyBatteryBlock)b).ChargeMode = ChargeMode.Auto);
            actions.Add("batteryRecharge", b => ((IMyBatteryBlock)b).ChargeMode = ChargeMode.Recharge);
            actions.Add("batteryDischarge", b => ((IMyBatteryBlock)b).ChargeMode = ChargeMode.Discharge);
            //Doors
            actions.Add("doorOpen", b => ((IMyDoor)b).OpenDoor());
            actions.Add("doorClose", b => ((IMyDoor)b).CloseDoor());
            //Vents
            actions.Add("ventPressurize", b => ((IMyAirVent)b).Depressurize = false);
            actions.Add("ventDepressurize", b => ((IMyAirVent)b).Depressurize = true);
            //GasTanks
            actions.Add("tankStockpileOn", b => ((IMyGasTank)b).Stockpile = true);
            actions.Add("tankStockpileOff", b => ((IMyGasTank)b).Stockpile = false);
            //Sorters
            actions.Add("sorterDrainOn", b => ((IMyConveyorSorter)b).DrainAll = true);
            actions.Add("sorterDrainOff", b => ((IMyConveyorSorter)b).DrainAll = false);
            //Timers
            actions.Add("timerTrigger", b => ((IMyTimerBlock)b).Trigger());
            actions.Add("timerStart", b => ((IMyTimerBlock)b).StartCountdown());
            actions.Add("timerStop", b => ((IMyTimerBlock)b).StopCountdown());
            //LandingGear
            actions.Add("gearAutoLockOn", b => ((IMyLandingGear)b).AutoLock = true);
            actions.Add("gearAutoLockOff", b => ((IMyLandingGear)b).AutoLock = false);
            actions.Add("gearLock", b => ((IMyLandingGear)b).Lock());
            actions.Add("gearUnlock", b => ((IMyLandingGear)b).Unlock());
            //Parachutes
            //How do parachutes even work? Is it OpenDoor and CloseDoor?
            //actions.Add("parachuteOpen", b => ((IMyParachute)b).OpenDoor());
            //actions.Add("parachuteClose", b => ((IMyParachute)b).CloseDoor());
            //Pistons
            actions.Add("pistonExtend", b => ((IMyPistonBase)b).Extend());
            actions.Add("pistonRetract", b => ((IMyPistonBase)b).Retract());
            //Rotors
            actions.Add("rotorLock", b => ((IMyMotorAdvancedStator)b).RotorLock = true);
            actions.Add("rotorUnlock", b => ((IMyMotorAdvancedStator)b).RotorLock = false);
            actions.Add("rotorReverse", b => ((IMyMotorAdvancedStator)b).TargetVelocityRPM =
                ((IMyMotorAdvancedStator)b).TargetVelocityRPM * -1);
            actions.Add("rotorPositive", b => ((IMyMotorAdvancedStator)b).TargetVelocityRPM =
                Math.Abs(((IMyMotorAdvancedStator)b).TargetVelocityRPM));
            actions.Add("rotorNegative", b => ((IMyMotorAdvancedStator)b).TargetVelocityRPM =
                Math.Abs(((IMyMotorAdvancedStator)b).TargetVelocityRPM) * -1);
            //Gatling Turrets
            //Character Large Meteor Missile Neutral Small Stations
            actions.Add("gatlingDefensive", b =>
            {
                //When defensive, Gatling Turrets target only missiles.
                //We've got a lot to do with this block. Let's just have a local and make one cast.
                IMyLargeGatlingTurret turret = (IMyLargeGatlingTurret)b;
                turret.SetValueBool("TargetLargeShips", false);
                turret.SetValueBool("TargetMissiles", true);
                turret.SetValueBool("TargetSmallShips", false);
                turret.SetValueBool("TargetStations", false);
            });
            actions.Add("gatlingOffensive", b =>
            {
                //When offensive, Gatling Turrets target large ships, small ships, and stations. 
                //They ignore missiles.
                IMyLargeGatlingTurret turret = (IMyLargeGatlingTurret)b;
                turret.SetValueBool("TargetLargeShips", true);
                turret.SetValueBool("TargetMissiles", false);
                turret.SetValueBool("TargetSmallShips", true);
                turret.SetValueBool("TargetStations", true);
            });
            actions.Add("swatterDefensive", b =>
            {
                //When defensive, swatters target missiles and meteors. They ignore characters and
                //small ships.
                IMyLargeInteriorTurret turret = (IMyLargeInteriorTurret)b;
                turret.SetValueBool("TargetSmallShips", false);
                turret.SetValueBool("TargetCharacters", false);
            });
            actions.Add("swatterOffensive", b =>
            {
                //When offensive, swatters target missiles, meteors, characters and small ships.
                IMyLargeInteriorTurret turret = (IMyLargeInteriorTurret)b;
                turret.SetValueBool("TargetSmallShips", true);
                turret.SetValueBool("TargetCharacters", true);
            });
            //Gyro? (Override)
            //Connector?
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
            //MFDs and ActionSets are special, though. We'll leave them in a dictionary.
            MFDs = new Dictionary<string, MFD>();
            sets = new Dictionary<string, ActionSet>();
            //We'll also need a dictionary of all possible actions
            Dictionary<string, Action<IMyTerminalBlock>> actions = compileActions(); 
            //We'll need to pass the GTS around a bit for this. May as well put an easy handle on it.
            IMyGridTerminalSystem GTS = GridTerminalSystem;
            //A couple of extra variables for working directly with MyIni
            MyIniValue iniValue = new MyIniValue();
            MyIniParseResult parseResult = new MyIniParseResult();
            //Also, a seperate MyIni object dedicated to reading the Storage string
            //TODO: Is this really needed?
            MyIni storageReader = new MyIni();
            //We'll need to do some configuration on tallies before we send them on their way. Let's
            //use an easy handle for it.
            Tally tally;
            //Sometimes, a more specialized handle for a tally is handy. Let's have a round of those.
            TallyCargo tallyCargo;
            TallyGeneric tallyGeneric;
            //ActionSets, too
            ActionSet set;
            //On the other hand, sometimes you need something a little bit generic.
            IReportable reportable;
            //Some blocks do multiple jobs, which means a block has to be subjected to multiple 
            //different sorters. This variable will tell us if at least one of those sorters knew 
            //how to handle the block.
            bool handled = false;
            //We'll need a string to store errors.
            string errors = "";
            //We'll use these strings to store the information we need to build a tally.
            string elementName = "";
            string tallyType = "";
            string MFDName = "";
            string addIn1 = "";
            string addIn2 = "";
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
            storageReader.TryParse(Storage);

            //Our first step will be to check the programmable block for tally configs.
            //From the PB, we read:
            //Tally<#>Name: The name that will be associated with this tally.
            //Tally<#>Type: The type of this tally. Acceptable tally types are:
            //  Volume, Item, Power, Oxygen, Hydrogen, JumpCharge, Raycast, PowerOutput (Solar/Wind),
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
            if (!iniReader.TryParse(Me.CustomData, out parseResult))
            //...file a complaint.
            {
                errors = $" -The parser was unable to read information from the Programmable Block. " +
                      $"Reason: {parseResult.Error}";
            }
            //The counter for this loop.
            int counter = 0;
            //As long as the counter isn't -1 (Which indicates that we've run out of tallies)...
            while (counter != -1)
            {
                //Look for another tally
                elementName = iniReader.Get(tag, $"Tally{counter}Name").ToString();
                //First thing's first: There's exactly one tally name we've reserved. Is the user
                //trying to use it?
                if (elementName.ToLowerInvariant() == "blank")
                //Complain. Righteously.
                {
                    errors += $" -The Tally name '{elementName}' is reserved by the script to indicate" +
                          $"where portions of the screen should be left empty. Please choose a " +
                          $"different name.";
                    //There's no way to recover from this. Stop evaluation until the user gets their
                    //act together.
                    break;
                }
                //We should probably also take this opportunity to make sure this tally name isn't 
                //already in use.
                else if (evalTallies.ContainsKey(elementName))
                {
                    errors += $" -The Tally name '{elementName}' is already in use. Tallies and " +
                        $"ActionSets must have their own, unique names.";
                    break;
                }
                //Now then. Did we get a tally name?
                else if (!string.IsNullOrEmpty(elementName))
                {
                    //Our next steps are going to be dictated by the TallyType. We should try and 
                    //figure out what that is.
                    tallyType = iniReader.Get(tag, $"Tally{counter}Type").ToString();
                    //If no type is defined...
                    if (string.IsNullOrEmpty(tallyType))
                    {
                        //... complain.
                        errors += $" -Tally {elementName} has a missing or unreadable TallyType.\n";
                        //Also, create a TallyCargo. This will let the rest of the script execute
                        //as normal, and hopefully prevent 'uninitialized tally' spam
                        tally = new TallyCargo(elementName);
                    }
                    //Now, we create a tally. The creation of a TallyCargo is quite straightforward.
                    else if (tallyType == "Inventory")
                    { tally = new TallyCargo(elementName); }
                    //Creating a TallyItem is a bit more involved.
                    else if (tallyType == "Item")
                    {
                        //We'll need a TypeID. We'll use the first AddIn string to store it
                        addIn1 = iniReader.Get(tag, $"Tally{counter}ItemTypeID").ToString();
                        //If we can't get it, complain.
                        if (string.IsNullOrEmpty(addIn1))
                        { errors += $" -Item Tally '{elementName}' has a missing or unreadable TallyItemTypeID.\n"; }
                        //And a SubTypeID, stored in AddIn2
                        addIn2 = iniReader.Get(tag, $"Tally{counter}ItemSubTypeID").ToString();
                        if (string.IsNullOrEmpty(addIn2))
                        { errors += $" -Item Tally '{elementName}' has a missing or unreadable TallyItemSubTypeID.\n"; }
                        //If we have the data we were looking for, we can create a TallyItem
                        if (!string.IsNullOrEmpty(addIn1) && !string.IsNullOrEmpty(addIn2))
                        { tally = new TallyItem(elementName, addIn1, addIn2); }
                        //If we're missing data, we'll just create a TallyCargo so the script can 
                        //continue. The error message should already be logged.
                        else
                        { tally = new TallyCargo(elementName); }
                    }
                    //Power and the other TallyGenerics are only marginally more complicated than
                    //Volume
                    else if (tallyType == "Battery")
                    { tally = new TallyGeneric(elementName, handleBattery); }
                    //Gas, which works for both Hydrogen and Oxygen
                    else if (tallyType == "Gas")
                    { tally = new TallyGeneric(elementName, handleGas); }
                    //JumpCharge
                    else if (tallyType == "JumpCharge")
                    { tally = new TallyGeneric(elementName, handleJumpCharge); }
                    //Raycst
                    else if (tallyType == "Raycast")
                    { tally = new TallyGeneric(elementName, handleRaycaster); }
                    //MaxOutput
                    else if (tallyType == "PowerProducer")
                    { tally = new TallyGeneric(elementName, handlePowerProducer); }
                    //TODO: Aditionally TallyTypes go here
                    else
                    {
                        //If we've gotten to this point, the user has given us a type that we can't 
                        //recognize. Scold them.
                        errors += $" -Tally {elementName}'s TallyType of '{tallyType}' cannot be handled" +
                            $"by this script. Be aware that TallyTypes are case-sensitive.\n";
                        //...Also, create a TallyCargo, so the rest of Evaluate will work.
                        tally = new TallyCargo(elementName);
                    }
                    //Now that we have our tally, we need to check to see if there's any further
                    //configuration data. 
                    //First, the DisplayName
                    iniValue = iniReader.Get(tag, $"Tally{counter}DisplayName");
                    if (!iniValue.IsEmpty)
                    { tally.name = iniValue.ToString(); }
                    //Then the Max
                    iniValue = iniReader.Get(tag, $"Tally{counter}Max");
                    if (!iniValue.IsEmpty)
                    { tally.forceMax(iniValue.ToDouble()); }
                    //There's a couple of TallyTypes that need to have a Max explicitly set (All 
                    //TallyItems, plus the TallyGenerics PowerProducer and Raycast). If that hasn't 
                    //happened, we need to complain.
                    else if (iniValue.IsEmpty && (tally is TallyItem || (tally is TallyGeneric &&
                        (((TallyGeneric)tally).currHandler == handlePowerProducer ||
                        ((TallyGeneric)tally).currHandler == handleRaycaster))))
                    {
                        errors += $" -Tally {elementName}'s TallyType of '{tallyType}' requires a Max " +
                            $"to be set in configuration.\n";
                    }
                    //Up next is the Multiplier
                    iniValue = iniReader.Get(tag, $"Tally{counter}Multiplier");
                    if (!iniValue.IsEmpty)
                    { tally.multiplier = iniValue.ToDouble(); }
                    //Last, LowGood
                    iniValue = iniReader.Get(tag, $"Tally{counter}LowGood");
                    if (!iniValue.IsEmpty)
                    { tally.lowGood(iniValue.ToBoolean()); }
                    //That's all the data we can glean from here. It's time to put this tally
                    //somewhere the rest of Evaluate can get to it.
                    evalTallies.Add(elementName, tally);
                    //Last step is to increment the counter, so we can look for the next tally.
                    counter++;
                }
                else
                //If we didn't find another tally, set the counter equal to -1 to indicate that 
                //we're done in this loop.
                { counter = -1; }
            }

            //ActionSets also get their initial configuration on the PB. 
            counter = 0;
            //As long as the counter isn't -1 (Which indicates that we've run out of ActionSets)...
            while (counter != -1)
            {
                //Look for another ActionSet
                elementName = iniReader.Get(tag, $"Action{counter}Name").ToString();
                //Make sure this Set name isn't in use.
                if (evalTallies.ContainsKey(elementName) || sets.ContainsKey(elementName))
                {
                    errors += $" -The ActionSet name '{elementName}' is already in use. Tallies and " +
                        $"ActionSets must have their own, unique names, and ActionSets cannot have " +
                        $"the same name as a Tally.";
                    break;
                }
                else if (!string.IsNullOrEmpty(elementName))
                {
                    //ActionSets have a lot less going on than tallies, initially at least. The only
                    //other thing we /need/ to know about them is what their previous state was.
                    //We'll try to get that from the storage string, defaulting to false if we can't
                    bool state = storageReader.Get("ActionSets", elementName).ToBoolean(false);
                    set = new ActionSet(elementName, state);
                    //There are a few other bits of configuration that ActionSets may have
                    //DisplayName
                    iniValue = iniReader.Get(tag, $"Action{counter}DisplayName");
                    if (!iniValue.IsEmpty)
                    { set.name = iniValue.ToString(); }
                    //OnColor
                    if (tryGetColorFromConfig(ref errors, ref color, $"Action{counter}", "ColorOn", 
                        tag, iniReader, Me))
                    { set.colorOn = color; }
                    //OffColor
                    if (tryGetColorFromConfig(ref errors, ref color, $"Action{counter}", "ColorOff",
                        tag, iniReader, Me))
                    { set.colorOff = color; }
                    //OnText
                    iniValue = iniReader.Get(tag, $"Action{counter}TextOn");
                    if (!iniValue.IsEmpty)
                    { set.textOn = iniValue.ToString(); }
                    //OffText
                    iniValue = iniReader.Get(tag, $"Action{counter}TextOff");
                    if (!iniValue.IsEmpty)
                    { set.textOff = iniValue.ToString(); }
                    //DelayOn and DelayOff. These will actually be stored in an ActionPlan, but we
                    //need to know if one of the values is present before we create the object.
                    int delayOn = iniReader.Get(tag, $"Action{counter}DelayOn").ToInt32();
                    int delayOff = iniReader.Get(tag, $"Action{counter}DelayOff").ToInt32();
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
                    set.setState(set.state);
                    //This ActionSet should be ready. Pass it to the list.
                    sets.Add(elementName, set);
                    //On to the next Set! Maybe.
                    counter++;
                }
                else
                //Again, a value of -1 indicates that we can't find another victim.
                { counter = -1; }
            }
            //If we don't have errors, but we also don't have any tallies or ActionSets...
            if (string.IsNullOrEmpty(errors) && evalTallies.Count == 0 && sets.Count == 0)
            { errors += " -No readable configuration found on the programmable block.\n"; }

            //Only if there were no errors with parsing the PB...
            if (string.IsNullOrEmpty(errors))
            {
                //...should we get the blocks on the grid with our section tag.
                //NOTE: This will never throw an error. Back when it used to exclude Me from the
                //  list it could have, but now, in order to reach this point, you must have config
                //  data on the PB.
                errors += Hammers.findBlocks<IMyTerminalBlock>(GTS, blocks, b =>
                    (b.IsSameConstructAs(Me) && MyIni.HasSection(b.CustomData, tag)),
                    $" -No blocks found on this construct with a [{tag}] INI section.");
            }

            //Every block we've found has some sort of configuration information for this script.
            //And we're going to read all of it.
            foreach (IMyTerminalBlock block in blocks)
            {
                //Whatever kind of block this is, we're going to need to see what's in its 
                //CustomData. If that isn't useable...
                if (!iniReader.TryParse(block.CustomData, out parseResult))
                //...complain.
                {
                    errors += $" -The parser was unable to read information from block " +
                          $"'{block.CustomName}'. Reason: {parseResult.Error}\n";
                }
                //My comedic, reference-based genius shall be preserved here for all eternity. Even
                //if it is now largely irrelevant to how ShipManager operates.
                //In the CargoManager, the data is handled by two seperate yet equally important
                //objects: the Tallies that store and calculate information and the Reports that 
                //display it. These are their stories.

                //There's a couple of keys that are present on multiple block types. We'll check for
                //those first.
                //If our block has a 'Tallies' key...
                if (parseResult.Success && iniReader.ContainsKey(tag, "Tallies"))
                {
                    //This is grounds for declaring this block to be handled.
                    handled = true;
                    //Get the 'Tallies' data
                    iniValue = iniReader.Get(tag, "Tallies");
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
                                    errors += $" -Block '{block.CustomName}' does not have an " +
                                        $"inventory and is not compatible with the TallyType of " +
                                        $"tally '{name}'.\n";
                                }
                            }
                            else if (tally is TallyGeneric)
                            {
                                tallyGeneric = (TallyGeneric)tally;
                                if (tallyGeneric.currHandler == handleGas)
                                {
                                    //Check to see if this block type is compatible with our tally.
                                    //I'm going to need to do this about half a dozen times but,
                                    //because I can't use 'is <type as variable>', I can't write it
                                    //into a specialized method.
                                    if (block is IMyGasTank)
                                    {
                                        //TallyGenerics have a flexible internal structure. Adding a 
                                        //block is as easy as calling 'addBlock'.
                                        tallyGeneric.addBlock(block);
                                        //That said, we will also need to call incrementMax
                                        tallyGeneric.incrementMax((double)(((IMyGasTank)block).Capacity));
                                    }
                                    //If the block is not compatible, complain.
                                    else
                                    {
                                        errors += $" -Block '{block.CustomName}' is not a gas tank " +
                                            $"and is not compatible with the TallyType of " +
                                            $"tally '{name}'.\n";
                                    }
                                }
                                //The rest of these are functionlly identical to the first.
                                else if (tallyGeneric.currHandler == handleBattery)
                                {
                                    if (block is IMyBatteryBlock)
                                    {
                                        tallyGeneric.addBlock(block);
                                        tallyGeneric.incrementMax((double)(((IMyBatteryBlock)block).MaxStoredPower));
                                    }
                                    else
                                    {
                                        errors += $" -Block '{block.CustomName}' is not a battery " +
                                            $"and is not compatible with the TallyType of " +
                                            $"tally '{name}'.\n";
                                    }
                                }
                                else if (tallyGeneric.currHandler == handleJumpCharge)
                                {
                                    if (block is IMyJumpDrive)
                                    {
                                        tallyGeneric.addBlock(block);
                                        tallyGeneric.incrementMax((double)(((IMyJumpDrive)block).MaxStoredPower));
                                    }
                                    else
                                    {
                                        errors += $" -Block '{block.CustomName}' is not a jump drive " +
                                            $"and is not compatible with the TallyType of " +
                                            $"tally '{name}'.\n";
                                    }
                                }
                                else if (tallyGeneric.currHandler == handlePowerProducer)
                                {
                                    if (block is IMyPowerProducer)
                                    {
                                        tallyGeneric.addBlock(block);
                                        //PowerProducers are required to have their max set manually.
                                        //As such, we won't even bother with the call to incrementMax.
                                    }
                                    else
                                    {
                                        errors += $" -Block '{block.CustomName}' is not a power producer " +
                                            $"and is not compatible with the TallyType of " +
                                            $"tally '{name}'.\n";
                                    }
                                }
                                else if (tallyGeneric.currHandler == handleRaycaster)
                                {
                                    if (block is IMyCameraBlock)
                                    {
                                        tallyGeneric.addBlock(block);
                                        //Like PowerProducers, raycasters also have their max set 
                                        //manually.
                                    }
                                    else
                                    {
                                        errors += $" -Block '{block.CustomName}' is not a camera " +
                                            $"and is not compatible with the TallyType of " +
                                            $"tally '{name}'.\n";
                                    }
                                }
                                //If we haven't managed to match a handler at this point...
                                else
                                //...I done goofed.
                                {
                                    errors += $" -Block '{block.CustomName}' refrenced the tally " +
                                        $"'{name}', which has a missing or un-recognized handler. " +
                                        $"Complain to the script writer, this should be impossible.\n";
                                }
                            }
                            else
                            //If a tally isn't a TallyCargo or a TallyGeneric, I done goofed.
                            {
                                errors += $" -Block '{block.CustomName}' refrenced the tally '{name}'," +
                                    $"which is neither a TallyCargo or a TallyGeneric. Complain to the " +
                                    $"script writer, this should be impossible.\n";
                            }
                        }
                        //If we can't find this name in evalTallies, complain.
                        else
                        {
                            errors += $" -Block '{block.CustomName}' tried to reference the " +
                                $"unconfigured tally '{name}'.\n";
                        }
                    }
                }

                //If the block has an inventory, it may have 'Inv<#>Tallies' keys instead. We need
                //to check for them.
                if (parseResult.Success && block.HasInventory)
                {
                    for (int i = 0; i < block.InventoryCount; i++)
                    {
                        if (iniReader.ContainsKey(tag, $"Inv{i}Tallies"))
                        {
                            //If we manage to find one of these keys, the block can be considered
                            //handled.
                            handled = true;
                            //Get the names of the specified tallies
                            iniValue = iniReader.Get(tag, $"Inv{i}Tallies");
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
                                        errors += $" -Block '{block.CustomName}' is not compatible " +
                                            $"with the TallyType of tally '{name}' referenced in key " +
                                            $"Inv{i}Tallies.\n";
                                    }
                                }
                                //If we can't find this name in evalTallies, complain.
                                else
                                {
                                    errors += $" -Block '{block.CustomName}' tried to reference the " +
                                        $"unconfigured tally '{name}' in key Inv{i}Tallies.\n";
                                }
                            }
                        }
                        //If there is no key, we fail silently.
                    }
                }

                //If the block has an 'ActionSets' key...
                if (parseResult.Success && iniReader.ContainsKey(tag, "ActionSets"))
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
                    iniValue = iniReader.Get(tag, "ActionSets");
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
                            if (iniReader.ContainsSection(discreetTag))
                            {
                                //Make a new action plan with this block as the subject.
                                ActionPlanBlock actionPlan = new ActionPlanBlock(block);
                                //Try to get the actionOn from the ACTION SECTION
                                iniValue = iniReader.Get(discreetTag, $"ActionOn");
                                if (!iniValue.IsEmpty)
                                {
                                    elementName = iniValue.ToString();
                                    //If this string matches entry in our 'actions' dictionary...
                                    if (actions.ContainsKey(elementName))
                                    //Use that entry to define the actionPlan's actionOn
                                    { actionPlan.actionOn = actions[elementName]; }
                                    //If there is no matching action, complain.
                                    else
                                    {
                                        errors += $" -Block '{block.CustomName}', discreet section '{discreetTag}', " +
                                            $"references the unknown action '{elementName}' as its ActionOn.\n"; 
                                    }
                                }
                                //Try to get the actionOff from the ACTION SECTION
                                iniValue = iniReader.Get(discreetTag, $"ActionOff");
                                if (!iniValue.IsEmpty)
                                {
                                    elementName = iniValue.ToString();
                                    //If this string has an entry in our 'actions' dictionary...
                                    if (actions.ContainsKey(elementName))
                                    //Use that entry to define the actionPlan's actionOff
                                    { actionPlan.actionOff = actions[elementName]; }
                                    //If there is no matching action, complain.
                                    else
                                    {
                                        errors += $" -Block '{block.CustomName}', discreet section '{discreetTag}', " +
                                            $"references the unknown action '{elementName}' as its ActionOff.\n";
                                    }
                                }
                                //If we have successfully registered at least one action...
                                if (actionPlan.actionOn != null || actionPlan.actionOff != null)
                                //Go ahead and add this ActionPlan to the ActionSet
                                { sets[name].addActionPlan(actionPlan); }
                                //If we didn't successfully register an action, complain.
                                else
                                {
                                    errors += $" -Block '{block.CustomName}', discreet section '{discreetTag}', " +
                                        $"does not define either an ActionOn or an ActionOff.\n";
                                }
                            }
                            //If there is no ACTION SECTION, complain.
                            else
                            {
                                errors += $" -Block '{block.CustomName}' references the ActionSet " +
                                    $"'{name}', but contains no discreet '{discreetTag}' section that would " +
                                    $"define actions.\n";
                            }
                        }
                        //If the set does not exist, complain.
                        else
                        {
                            errors += $" -Block '{block.CustomName}' tried to reference the " +
                                $"unconfigured ActionSet '{name}'.\n";
                        }
                    }
                }

                //On to block types.
                //The PB gets its own sorter. Because if we made it this far, it's handled.
                if (parseResult.Success && block == Me)
                { handled = true; }

                //If our parse was successful and this block is a surface provider, we need to 
                //configure some reports.
                if (parseResult.Success && block is IMyTextSurfaceProvider)
                {
                    //No matter what happens, we set the handled flag to indicate that we had a
                    //sorter that knew what to do with this.
                    handled = true;
                    //From SurfaceProviders, we read:
                    //Surface<#>Tallies: Which tallies we should show on the designated surface. 
                    //  NOTE: A tally by the name of 'blank' (Case insensitive) is used to indicate
                    //  an empty element on the Report's grid.
                    //Surface<#>MFD: As an alternative to the list of tallies to be displayed, the 
                    //  name of the MFD that will be displayed on this surface. MFDs are configured
                    //  using the same catagories as seen below, but with the name of the MFD, 
                    //  followed by the MFD page number, in place of Surface<#>.
                    //Surface<#>Title: (Default = "") The title of this report, which will appear at 
                    //  the top of its surface.
                    //Surface<#>Columns: (Default = 3) The number of columns to use when arranging 
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
                        if (iniReader.ContainsKey(tag, $"Surface{i}MFD"))
                        {
                            //Pull the name of the MFD from the main config
                            MFDName = iniReader.Get(tag, $"Surface{i}MFD").ToString();
                            //Construct the discreetTag of the section that will configure this MFD
                            discreetTag = $"{PREFIX}.{MFDName}";
                            //Is there a discreet section with config for this MFD?
                            if (iniReader.ContainsSection(discreetTag))
                            {
                                MFD newMFD = new MFD();
                                counter = 0;
                                //There's several keys that we could be looking for.
                                while (counter != -1)
                                {
                                    reportable = tryGetReportableFromConfig(ref errors, $"Page{counter}", 
                                        discreetTag, surfaceProvider.GetSurface(i), block, iniReader, 
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
                                        iniValue = iniReader.Get(discreetTag, $"Page{counter}LinkActionSetOn");
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
                                                errors += $" -Surface provider '{block.CustomName}', " +
                                                    $"discreet section '{discreetTag}', tried to " +
                                                    $"reference the unconfigured ActionSet '{elementName}' " +
                                                    $"in its LinkActionSetOn configuration.\n";
                                            }
                                        }
                                        iniValue = iniReader.Get(discreetTag, $"Page{counter}LinkActionSetOff");
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
                                                errors += $" -Surface provider '{block.CustomName}', " +
                                                    $"discreet section '{discreetTag}', tried to " +
                                                    $"reference the unconfigured ActionSet '{elementName}' " +
                                                    $"in its LinkActionSetOff configuration.\n";
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
                                    newMFD.trySetPage(storageReader.Get("MFDs", MFDName).ToString());
                                    //Add the new MFD to our reports and MFDs
                                    evalReports.Add(newMFD);
                                    MFDs.Add(MFDName, newMFD);
                                }
                                //If we didn't get at least one page, complain.
                                else
                                {
                                    errors += $" -Surface provider '{block.CustomName}', Surface {i}, " +
                                        $"specified the use of MFD '{MFDName}' but did not provide " +
                                        $"readable page configuration.\n";
                                }
                            }
                            //If there is no discreet section, complain.
                            else
                            {
                                errors += $" -Surface provider '{block.CustomName}', Surface {i}, " +
                                    $"declares the MFD '{MFDName}', but contains no discreet " +
                                    $"'{discreetTag}' section that would configure it.\n";
                            }
                        }
                        /*
                        //Are we supposed to display an MFD on this surface?
                        if (iniReader.ContainsKey(SECTION_TAG, $"Surface{i}MFD"))
                        {
                            MFDName = iniReader.Get(SECTION_TAG, $"Surface{i}MFD").ToString();
                            pages = new Dictionary<string, IReportable>();
                            counter = 0;
                            //There's several keys that we could be looking for.
                            while (iniReader.ContainsKey(SECTION_TAG, $"{MFDName}{counter}Elements") ||
                                iniReader.ContainsKey(SECTION_TAG, $"{MFDName}{counter}Script"))
                            {
                                //Generate and store a name for this page. This will be the prefix
                                //for evaluateReport, and the title of the page if the user doesn't
                                //specify one.
                                addIn1 = $"{MFDName}{counter}";
                                reportable = tryGetReportableFromConfig(ref errors, addIn1, 
                                    surfaceProvider.GetSurface(i), block, iniReader, evalTallies, sets);
                                //Did we get a reportable?
                                if (reportable != null)
                                {
                                    //Check to see if the user defined a title. If they didn't, we'll have 
                                    //to use addIn1 to address this MFD page.
                                    if (reportable is Report && !String.IsNullOrEmpty(((Report)reportable).title))
                                    { addIn1 = ((Report)reportable).title; }
                                    //There's one bit of configuration that's specific to MFDs, and 
                                    //that's linking to ActionSets. While we have both the 
                                    pages.Add(addIn1, reportable);
                                }
                                counter++;
                            }
                            //If we actually got configuration for at least one page...
                            if (pages.Count > 0)
                            {
                                MFD newMFD = new MFD(pages);
                                evalReports.Add(newMFD);
                                MFDs.Add(MFDName, newMFD);
                            }
                            //If we didn't, complain.
                            else
                            {
                                errors += $" -Surface provider '{block.CustomName}', Surface {i}, " +
                                    $"specified the use of MFD '{MFDName}' but did not provide " +
                                    $"readable page configuration.\n";
                            }
                        }
                        */
                        else
                        {
                            //If it isn't an MFD, pass it directly to the specialized method for sorting
                            reportable = tryGetReportableFromConfig(ref errors, $"Surface{i}", tag,
                                surfaceProvider.GetSurface(i), block, iniReader, evalTallies, sets);
                            //Only if we got a reportable...
                            if (reportable != null)
                            //...should we try adding it to our list.
                            { evalReports.Add(reportable); }
                        }
                    }
                    /* OLD
                    for (int i = 0; i < surfaceProvider.SurfaceCount; i++)
                    {
                        //Are we supposed to display a report on this surface?
                        if (iniReader.ContainsKey(SECTION_TAG, $"Surface{i}Tallies"))
                        {
                            report = evaluateReport(ref errors, $"Surface{i}",
                                surfaceProvider.GetSurface(i), block, iniReader, evalTallies);
                            evalReports.Add(report);
                        }
                        //Are we supposed to display an MFD on this surface?
                        else if (iniReader.ContainsKey(SECTION_TAG, $"Surface{i}MFD"))
                        {
                            MFDName = iniReader.Get(SECTION_TAG, $"Surface{i}MFD").ToString();
                            pages = new Dictionary<string, IReportable>();
                            counter = 0;
                            while (iniReader.ContainsKey(SECTION_TAG, $"{MFDName}{counter}Tallies"))
                            {
                                //Generate and store a name for this page. This will be the prefix
                                //for evaluateReport, and the title of the page if the user doesn't
                                //specify one.
                                addIn1 = $"{MFDName}{counter}";
                                report = evaluateReport(ref errors, addIn1, surfaceProvider.GetSurface(i),
                                    block, iniReader, evalTallies);
                                //Check to see if the user defined a title
                                if (!String.IsNullOrEmpty(report.title))
                                { addIn1 = report.title; }
                                pages.Add(addIn1, report);
                                counter++;
                            }
                            MFD newMFD = new MFD(pages);
                            evalReports.Add(newMFD);
                            MFDs.Add(MFDName, newMFD);
                        }
                    }
                     */
                }

                //This could also be an indicator light, something I totally didn't forget when I 
                //first wrote this. Let's check!
                if (parseResult.Success && block is IMyLightingBlock)
                {
                    //We'll hold off on setting the 'handled' flag for now.
                    //From lights, we read:
                    //Element: The Element (Singular) that this indicator group watches
                    iniValue = iniReader.Get(tag, "Element");
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
                            errors += $" -Lighting block '{block.CustomName}' tried to reference " +
                                $"the unconfigured element '{elementName}'. Note that lighting blocks can " +
                                $"only monitor one element.\n";
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
                        errors += $" -Lighting block {block.CustomName} has missing or unreadable Element. " +
                            $"Note that lighting blocks use the 'Element' key, singular.\n";
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
                    errors += $" -Block '{block.CustomName}' is missing proper configuration or is a" +
                        $"block type that cannot be handled by this script.\n";
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
            //We'll take this opportunity to call setProfile on all our Reportables
            foreach (IReportable screen in reports)
            { screen.setProfile(); }
            //There's one more step before the tallies are ready. We need to tell them that they
            //have all the data that they're going to get. 
            foreach (Tally finishTally in tallies)
            { finishTally.finishSetup(); }
            //There's probably still data in the iniReader. We don't need it anymore, and we don't
            //want it carrying over to any future evaluations.
            iniReader.Clear();

            //That should be it. So if we have no errors...
            if (errors == "")
            {
                //...Set the script into motion.
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
                //Also, brag.
                log.add($"Grid evaluation complete. Registered {tallies.Length} tallies, " +
                    $"{sets.Count} ActionSets, and {reports.Length} reports, as configured by " +
                    $"data on {blocks.Count} blocks.\nScript is now running.");
            }
            else
            {
                //Make sure the script isn't trying to run with errors.
                Runtime.UpdateFrequency = UpdateFrequency.None;
                //Also, complain.
                log.add($"Grid evaluation complete. The following errors are preventing script " +
                    $"execution:\n{errors}");
            }
        }
        /*DEPRECEATED
        //A specialized method used during grid evaluation to set up TallyGenerics. Takes the following:
        //string friendlyName: A readable version of the block type, for use in error messages
        //Func maxHandler: A lambda function that will be used to get a double, representing the 
        //  'maximum' of this tally.
        //Func currHandler: The currHandler we expect the tally to be using. Used here for the 
        //  purpose of error detection
        //IMyTerminalBlock block: The block that we've read the CustomData of, the block that we'll
        //  be passing to this tally if everything checks out.
        //MyIni iniReader: The INI Reader that holds information parsed from block
        //Dictionary evalTallies: The list of all tallies that were configured with data from the PB.
        //string errors: A string that we'll report any errors we encounter to.
        private bool evaluateGeneric(ref string errors, string friendlyName, Func<IMyTerminalBlock, double> maxHandler,
            Func<List<IMyTerminalBlock>, double> currHandler, IMyTerminalBlock block, MyIni iniReader, 
            Dictionary<string, Tally> evalTallies, bool ignoreType = false)
        {
            Tally tally;
            MyIniValue iniValue = iniReader.Get(SECTION_TAG, "Tallies");
            //Almost every time we call this, we're going to declare it to be handled at the end.
            bool handled = true;
            if (!iniValue.IsEmpty)
            {
                string[] tallyNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                //Now we need to get the Tallies referenced by the strings in TallyNames
                List<TallyGeneric>genericRefs = new List<TallyGeneric>();
                foreach (string name in tallyNames)
                {
                    //If this tally name is in evalTallies...
                    if (evalTallies.ContainsKey(name))
                    {
                        //...pull the tally out.
                        tally = evalTallies[name];
                        //If the tally is a TallyGeneric with a the correct handler...
                        if (tally is TallyGeneric && ((TallyGeneric)tally).currHandler == currHandler)
                        //...add it to our list of genericRefs
                        { genericRefs.Add((TallyGeneric)tally); }
                        else if (ignoreType)
                        //We're about to generate an error. If we anticipated this, fail silently 
                        //and withdraw the 'handled' designation
                        { handled = false; }
                        else
                        //If it isn't a TallyGeneric with the correct handler, and we aren't 
                        //ignoring this error, complain.
                        {
                            errors += $" -{friendlyName} block '{block.CustomName}' is not " +
                                $"compatible with the TallyType of tally '{name}'.\n";
                        }
                    }
                    else
                    //If the tally name isn't in evalTallies, complain.
                    {
                        errors += $" -{friendlyName} block '{block.CustomName}' tried to " +
                                $"reference the unconfigured tally '{name}'.\n";
                    }
                }
                //For each tally this block is supposed to report to...
                foreach (TallyGeneric genericTally in genericRefs)
                {
                    //...add the block to the tally's internal list
                    genericTally.addBlock(block);
                    //Increment the tally's max by the battery's max
                    genericTally.incrementMax(maxHandler(block));
                }
            }
            else
            //If we couldn't find a 'Tallies' section, complain.
            { errors += $" -{friendlyName} block {block.CustomName} has missing or unreadable Tallies.\n"; }
            return handled;
        }
        */

        private IReportable tryGetReportableFromConfig(ref string errors, string prefix, string sectionTag, 
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
                            errors += $" -Surface provider '{block.CustomName}', {prefix}" +
                                $", tried to reference the unconfigured element " +
                                $"'{name}'.\n";
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
                if (tryGetColorFromConfig(ref errors, ref color, prefix, "ForeColor", sectionTag, 
                    iniReader, block))
                { report.foreColor = color; }
                //Background color
                if (tryGetColorFromConfig(ref errors, ref color, prefix, "BackColor", sectionTag, 
                    iniReader, block))
                { report.backColor = color; }
                //Columns. IMPORTANT: Set anchors is no longer called during object
                //creation, and therefore MUST be called before the report is finished.
                iniValue = iniReader.Get(sectionTag, $"{prefix}Columns");
                //Call setAnchors, using a default value of 3 if we didn't get 
                //configuration data.
                report.setAnchors(iniValue.ToInt32(3));

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
                if (tryGetColorFromConfig(ref errors, ref color, prefix, "ForeColor", sectionTag, 
                    iniReader, block))
                { script.foreColor = color; }
                //Background color
                if (tryGetColorFromConfig(ref errors, ref color, prefix, "BackColor", sectionTag, 
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
                //CustomData, DetailInfo, and Raycasters (Eventually) need to have a data source
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
                            errors += $" -Surface provider '{block.CustomName}', {prefix}, tried " +
                                $"to reference the unknown block '{source}' as a DataSource.\n";
                        }
                    }
                    //If there is no data source, complain.
                    else
                    {
                        errors += $" -Surface provider '{block.CustomName}', {prefix}, has a " +
                            $"DataType of {type}, but a missing or unreadable DataSource.\n";
                    }
                }
                else
                //If we don't recognize the DataType, complain.
                {

                    errors += $" -Surface provider '{block.CustomName}', {prefix}, tried to " +
                        $"reference the unknown data type '{type}'.\n";
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
                    { wall.setCharPerLine(iniValue.ToInt32()); }
                    //Foreground color
                    if (tryGetColorFromConfig(ref errors, ref color, prefix, "ForeColor", sectionTag,
                        iniReader, block))
                    { wall.foreColor = color; }
                    //Background color
                    if (tryGetColorFromConfig(ref errors, ref color, prefix, "BackColor", sectionTag,
                        iniReader, block))
                    { wall.backColor = color; }
                    //Send this WallOText on its way with a fond fairwell.
                    reportable = wall;
                } 
            }

            //The last thing we need to do for all of these is check to see if there's any color 
            //configuration present.
            /*
            iniValue = iniReader.Get(SECTION_TAG, $"{prefix}ForeColor");
            if (!iniValue.IsEmpty)
            {
                //Attempt to set the foreColor using the provided configuration. If it fails...
                if (!Hammers.rgbColorFromString(ref report.foreColor, iniValue.ToString()))
                {
                    //...Complain.
                    errors += $" -Surface provider '{block.CustomName}', {prefix}" +
                            $", has missing or unreadable ForeColor.\n";
                }
            }
            //Background color
            iniValue = iniReader.Get(SECTION_TAG, $"{prefix}BackColor");
            if (!iniValue.IsEmpty)
            {
                //Attempt to set the backColor using the provided configuration. If it fails...
                if (!Hammers.rgbColorFromString(ref report.backColor, iniValue.ToString()))
                {
                    //...Complain.
                    errors += $" -Surface provider '{block.CustomName}', {prefix}" +
                            $", has missing or unreadable BackColor.\n";
                }
            }*/

            //All done? Last step is to add this report to our list of reports. So
            //we'll know where it lives.
            return reportable;
        }

        /*
        private Report evaluateReport(ref string errors, string prefix, IMyTextSurface surface,
            IMyTerminalBlock block, MyIni iniReader, Dictionary<string, Tally> evalTallies)
        {
            MyIniValue iniValue = iniReader.Get(SECTION_TAG, $"{prefix}Tallies");
            string[] tallyNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
            List<Tally> tallyRefs = new List<Tally>();
            Color color = Hammers.cozy;
            Report report;
            foreach (string name in tallyNames)
            {
                //Is this a blank slot in the report?
                if (name.ToLowerInvariant() == "blank")
                //Just add a null to the list. The report will know how to handle 
                //this.
                { tallyRefs.Add(null); }
                else
                {
                    //If it isn't a blank, we'll need to try and get the tally from 
                    //evalTallies, checking to see if it's there, first.
                    if (evalTallies.ContainsKey(name))
                    { tallyRefs.Add(evalTallies[name]); }
                    else
                    {
                        //And complain, if appropriate.
                        errors += $" -Surface provider '{block.CustomName}', {prefix}" +
                            $", tried to reference the unconfigured tally " +
                            $"'{name}'.\n";
                    }
                }
            }
            //Create a new report with the data we've collected so far.
            report = new Report(surface, tallyRefs);
            //Now that we have a report, we need to see if the user wants anything 
            //special done with it.
            //Title
            iniValue = iniReader.Get(SECTION_TAG, $"{prefix}Title");
            if (!iniValue.IsEmpty)
            { report.title = iniValue.ToString(); }
            //FontSize
            iniValue = iniReader.Get(SECTION_TAG, $"{prefix}FontSize");
            if (!iniValue.IsEmpty)
            { report.fontSize = iniValue.ToSingle(); }
            //Font
            iniValue = iniReader.Get(SECTION_TAG, $"{prefix}Font");
            if (!iniValue.IsEmpty)
            { report.font = iniValue.ToString(); }
            //Foreground color
            if (tryGetColorFromConfig(ref errors, ref color, prefix, "ForeColor", iniReader, block))
            { report.foreColor = color; }
            //Background color
            if (tryGetColorFromConfig(ref errors, ref color, prefix, "BackColor", iniReader, block))
            { report.backColor = color; }

            //===============START OLD
            iniValue = iniReader.Get(SECTION_TAG, $"{prefix}ForeColor");
            if (!iniValue.IsEmpty)
            {
                //Attempt to set the foreColor using the provided configuration. If it fails...
                if (!Hammers.rgbColorFromString(ref report.foreColor, iniValue.ToString()))
                {
                    //...Complain.
                    errors += $" -Surface provider '{block.CustomName}', {prefix}" +
                            $", has missing or unreadable ForeColor.\n";
                }
            }
            //Background color
            iniValue = iniReader.Get(SECTION_TAG, $"{prefix}BackColor");
            if (!iniValue.IsEmpty)
            {
                //Attempt to set the backColor using the provided configuration. If it fails...
                if (!Hammers.rgbColorFromString(ref report.backColor, iniValue.ToString()))
                {
                    //...Complain.
                    errors += $" -Surface provider '{block.CustomName}', {prefix}" +
                            $", has missing or unreadable BackColor.\n";
                }
            }
            //=============END OLD
            //Columns. IMPORTANT: Set anchors is no longer called during object
            //creation, and therefore MUST be called before the report is finished.
            iniValue = iniReader.Get(SECTION_TAG, $"{prefix}Columns");
            //Call setAnchors, using a default value of 3 if we didn't get 
            //configuration data.
            report.setAnchors(iniValue.ToInt32(3));

            //All done? Last step is to add this report to our list of reports. So
            //we'll know where it lives.
            return report;
        }
    */

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
        public bool tryGetColorFromConfig(ref string errors, ref Color color, string prefix, 
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
                            errors += $" -Block '{block.CustomName}', section {sectionTag}, prefix " +
                                $"{prefix}, has missing, unreadable, or unknown {target}.\n";
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
            //The name of this tally. While the CargoManager knows what this is, the Reports don't.
            //So we'll just store it here. Note that the 'DisplayName' config will over-write this
            //directly
            public string name { get; set; }
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
            bool maxForced;
            //How 'full' this tally is, as measured by the curr against the max. Shold be between
            //0 and 100
            public double percent { get; protected set; }
            //A representation of the current value of this tally, in a readable string format. It
            //doesn't get any fancy getters or setters, because apparently that causes trouble with
            //passing by reference in the inheriting objects.
            protected string readableCurr;
            //A readable string format for the maximum of this tally.
            private string readableMax;
            //A StringBuilder to build and store the ASCII-style meter representing the percentage
            //of this tally that is 'full'. 
            internal StringBuilder meter;
            //A color code for this tally, based on the percentage.
            public Color statusColor { get; protected set; }
            //The function we use to figure out what color to associate with the current value of 
            //this tally. Will be set to either handleColorCodeLow or handleColorCodeHigh.
            protected Func<double, Color> colorHandler { get; private set; }

            public Tally(string name, bool isLow = false, double multiplier = 1)
            {
                this.name = name;
                lowGood(isLow);
                this.multiplier = multiplier;
                curr = 0;
                max = 0;
                maxForced = false;
                percent = 0;
                readableCurr = "curr";
                readableMax = "max";
                meter = new StringBuilder("[----------]");
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
            { return meter.ToString(); }

            //Get the readableCurr
            internal string getReadableCurr()
            { return readableCurr; }

            //Get the readableMax
            internal string getReadableMax()
            { return readableMax; }

            //Arrange this tally's information into a reportable-friendly vertical element
            public string assembleElementStack()
            { return $"{name}\n{readableCurr} / {readableMax}\n{meter.ToString()}"; }

            //Arrange this tally's information into a reportable-friendly horizontal element
            //The arrangement is: A left-aligned space measuring 12 characters for the name, another,
            //similar space for the ratio and, lastly, the meter.
            //TODO: Figure out a circumstance in which to test this
            public string assembleElementLine()
            { return $"{name, -12}{($"{readableCurr} / {readableMax}"), -12}{meter.ToString()}"; }

            //Because of the way the data is arranged, Tally has to be told when it has all of its
            //data.
            internal void finishSetup()
            {
                //Apply the multiplier to the max.
                max = max * multiplier;
                //Max will never change unless re-initialized. So we'll figure out what readableMax
                //is once and just hold on to it.
                readableMax = Hammers.readableInt(ref readableMax, (int)max);
            }

            internal void clearCurr()
            { curr = 0; }

            //Increment the max by the value being passed in. Generally called when a block is added
            //to the tally, directly or indirectly. Has no effect if this Tally has already had its
            //max forced to be a certain value.
            internal void incrementMax(double val)
            {
                if (!maxForced)
                { max += val; }
            }

            //Set the max to the value being passed in, and ignore any future attempts to increment
            //the max.
            internal void forceMax(double val)
            {
                max = val;
                maxForced = true;
            }

            //Each class that inherits from Tally must implement its own method for Compute.
            internal abstract void compute();
        }

        public class TallyGeneric : Tally
        {
            //The internal structure of a TallyGeneric is a lot simpler than that of a TallyCargo.
            //These tallies contain direct references to the blocks they watch. However, we have to
            //build it as we go along, so we'll need a list instead of an array.
            List<IMyTerminalBlock> blocks;
            //The function that will be used to get the curent value of the blocks in this tally. 
            //It has a get method so we can use it to identify what kind of tally this is.
            public Func<List<IMyTerminalBlock>, double> currHandler { get; private set; }

            //TallyGeneric is quite similar to the regular Tally externally, only requiring a 
            //handler to be passed in alongside the name. We need to do a bit of work internally,
            //however.
            public TallyGeneric(string name, Func<List<IMyTerminalBlock>, double> handler, 
                bool isLow = false, double multiplier = 1) : base(name, isLow, multiplier)
            {
                blocks = new List<IMyTerminalBlock>();
                currHandler = handler;
            }

            internal void addBlock(IMyTerminalBlock block)
            { blocks.Add(block); }

            //Using curr and max, derive the remaining components needed to form a Report. Unlike
            //TallyCargo, we can compute curr from this method
            internal override void compute()
            {
                //Use the handler to calculate the curr of our blocks.
                curr = currHandler(blocks);
                //First thing we need to do is apply the multiplier.
                curr = curr * multiplier;
                //Now for the percent. We'll need it for everything else. But things will get
                //weird if it's more than 100.
                percent = Math.Min(curr / max, 100) * 100;
                //Next, get the color code from our color handler. 
                statusColor = colorHandler(percent);
                //Now, the meter.
                Hammers.drawMeter(ref meter, percent);
                //Last, we want to show curr as something you can actually read.
                readableCurr = Hammers.readableInt(ref readableCurr, (int)curr);
            }
        }

        public class TallyCargo : Tally
        {
            //The only change to the constructor that TallyCargo needs is setting the default of 
            //isLow to 'true'
            public TallyCargo(string name, bool isLow = true, double multiplier = 1) 
                : base(name, isLow, multiplier)
            { }

            //Take an inventory and see how full it currently is.
            internal virtual void addInventoryToCurr(IMyInventory inventory)
            { curr += (double)inventory.CurrentVolume; }

            //Using curr (Which is determined by calling calculateCurr() on all Containers associated
            //with this tally) and max (Which should already be set long before you consider calling
            //this), derive the remaining components needed to form a Report
            internal override void compute()
            {
                //First thing we need to do is apply the multiplier.
                curr = curr * multiplier;
                //Now for the percent. We'll need it for everything else. But things will get
                //weird if it's more than 100.
                percent = Math.Min(curr / max, 100) * 100;
                //Next, get the color code from our color handler. 
                statusColor = colorHandler(percent);
                //Now, the meter.
                Hammers.drawMeter(ref meter, percent);
                //Last, we want to show curr as something you can actually read.
                Hammers.readableInt(ref readableCurr, (int)curr);
            }
        }

        public class TallyItem : TallyCargo
        {
            //The item type that this tally will look for in inventories.
            MyItemType itemType;

            //TallyItems need a bit more data, so they'll know what kind of item they're looking
            //for. You can also set the max from the constructor, though I've stopped doing that.
            public TallyItem(string name, string typeID, string subTypeID, double max = 0,
                bool isLow = false, double multiplier = 1) : base(name, isLow, multiplier)
            {
                itemType = new MyItemType(typeID, subTypeID);
                base.forceMax(max);
            }

            //Take an inventory and see how much of the itemType is in it.
            internal override void addInventoryToCurr(IMyInventory inventory)
            { curr += (double)inventory.GetItemAmount(itemType); }
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

        //ActionPlans only need one thing in common.
        public interface IHasActionPlan
        {
            void takeAction(bool isOnAction);
        }

        //Stores a binary set of actions for a specific terminal block
        public class ActionPlanBlock : IHasActionPlan
        {
            //The TerminalBlock this ActionPlan will be manipulating
            IMyTerminalBlock subject;
            //The action to be performed on the subject block when the ActionPlan is switched on
            internal Action<IMyTerminalBlock> actionOn { get; set; }
            //The action to be performed on the subject block when the ActionPlan is switched off
            internal Action<IMyTerminalBlock> actionOff { get; set; }

            public ActionPlanBlock(IMyTerminalBlock subject)
            {
                this.subject = subject;
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
                { actionOn?.Invoke(subject); }
                else
                { actionOff?.Invoke(subject); }
            }
        }

        //Stores a binary set of MFD pages for a specific MFD
        public class ActionPlanMFD : IHasActionPlan
        {
            //The MFD this ActionPlan will be manipulating
            MFD subject;
            //Which page to switch to when the ActionPlan is switched on
            internal string pageOn { private get; set; }
            //Which page to switch to when the ActionPlan is switched off
            internal string pageOff { private get; set; }

            public ActionPlanMFD(MFD subject)
            {
                this.subject = subject;
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
                { subject.trySetPage(pageOn); }
                else
                { subject.trySetPage(pageOff); }
            }
        }

        //Stores two possible updateDelays for the UpdateDistributor.
        public class ActionPlanUpdate : IHasActionPlan
        {
            //A reference to the script's update distributor, ie, the thing we'll be manipulating.
            UpdateDistributor distributor;
            //How long the delay will be when this ActionPlan is on
            internal int delayOn { private get; set; }
            //How long the delay will be when this ActionPlan is off
            internal int delayOff { private get; set; }

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
        }

        public class ActionSet : IHasElement
        {
            //The list of ActionPlan objects that makes up this ActionSet
            List<IHasActionPlan> actionPlans;
            //The name of the ActionSet, which will be displayed in its Element
            internal string name { get; set; }
            //The state of the ActionSets, which is used to determine how it will be displayed and
            //what set of actions it will take next.
            internal bool state {  get; private set; }
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
                this.name = name;
                this.state = state;
                //Again, I can't have default values for colors passed in through the constructor,
                //so I'm just setting them here.
                colorOn = Hammers.green;
                colorOff = Hammers.red;
                //MONITOR: Does this work? It should set the statusColor based on the state.
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
            { actionPlans.Add(plan); }

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
                { plan.takeAction(state); }
                evaluateStatus();
            }

            //A vertical arrangement of the element representing this ActionSet's current state
            public string assembleElementStack()
            { return $"{name}\n{statusText}"; }

            //A horizontal arrangement of the element representing this ActionSet's current state
            public string assembleElementLine()
            //Lines on tallies are 38 characters across (12 for name, 12 for ratio, 12 for meter,
            //plus two guaranteed spaces seperating each component). To keep things consistant, 
            //we'll allocate 19 characters for an ActionSet name and 18 for its status, plus one
            //space of guaranteed seperation between the two.
            { return $"{name, -19} {statusText, 18}"; }
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
            Dictionary <string, IReportable> pages;
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

        /*
        public class MFD : IReportable
        {
            //The Reportable objects managed by this MFD
            Dictionary <string, IReportable> pages;
            //The index of the report currently being displayed by the MFD
            int pageNumber;
            //The name of the report currently being displayed by the MFD.
            string pageName;

            public MFD(Dictionary<string, IReportable> pages)
            {
                this.pages = pages;
                pageNumber = 0;
                pageName = pages.ToArray()[pageNumber].Key;
            }

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
            public bool setPage(string name)
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
        */

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
            {}

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
            //Pull the CustomData from this block
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

        //RaycastBroker when?

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
        /* OLD
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
            //The DataBroker we'll be consulting to get the text.
            IHasData broker;

            public WallOText(IMyTextSurface surface, IHasData broker)
            {
                this.surface = surface;
                this.broker = broker;
                //By default, this object will assume the current font and color settings of its
                //surface are what the user wants it to use.
                foreColor = surface.FontColor;
                backColor = surface.BackgroundColor;
                font = surface.Font;
                fontSize = surface.FontSize;
            }

            //See if we need to update this WallOText
            public void update()
            {
                //If there's an update...
                if (broker.hasUpdate())
                //Use data from the broker to re-write the surface.
                { surface.WriteText(broker.getData()); }
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
                surface.WriteText(broker.getData());
            }
        }*/

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

        //Volume, Item, Oxygen, Hydrogen, Power, Jump Charge, Raycast, Max Output (Solar/Wind),
        //HydrogenWithEngines?, ShieldIntegrity?
        public double handleBattery(List<IMyTerminalBlock> blocks)
        {
            double curr = 0;
            foreach (IMyBatteryBlock battery in blocks)
            { curr += battery.CurrentStoredPower; }
            return curr;
        }

        //Oxygen and Hydrogen tanks share an interface. So we /should/ be able to use the same 
        //handler for both of them.
        //...It kind of makes me wonder if I even need two discreet sorters in the evaluation method.
        //I could just rely on the user to feed the script the correct sets of tanks.
        //Of course, HydrogenWithEngines would need special handling. I should remember that I could
        //use the BlockDefinitions to figure out exactly what kind of block this is, as opposed to
        //checking the CustomName for 'hydrogen' like I did in previous iterations.
        public double handleGas(List<IMyTerminalBlock> blocks)
        {
            double curr = 0;
            foreach (IMyGasTank tank in blocks)
            { curr += tank.Capacity * tank.FilledRatio; }
            return curr;
        }

        public double handleJumpCharge(List<IMyTerminalBlock> blocks)
        {
            double curr = 0;
            foreach (IMyJumpDrive drive in blocks)
            { curr += drive.CurrentStoredPower; }
            return curr;
        }

        //The user will probably only have one raycaster per tally. But who are we to judge?
        public double handleRaycaster(List<IMyTerminalBlock> blocks)
        {
            double curr = 0;
            foreach (IMyCameraBlock camera in blocks)
            { curr += camera.AvailableScanRange; }
            return curr;
        }

        //Counterintuitively, the 'MaxOutput' of things like Solar Panels and Wind Turbines is not
        //fixed. It actually describes the ammount of power that the block is currently receiving
        //in its current enviroment, ie, how much of a panel's surface area is facing the sun, or
        //what kind of weather is the turbine in. The variable you'd expect to describe those 
        //things, CurrentOutput, instead describes how much energy the grid is drawing from this
        //PowerProvider.
        //Also: MaxOutput is in megawatts, while most PowerProducers generate power in the kilowatt
        //range. This handler will generally return a decimal.
        public double handlePowerProducer(List<IMyTerminalBlock> blocks)
        {
            double curr = 0;
            foreach (IMyPowerProducer producer in blocks)
            { curr += producer.MaxOutput; }
            return curr;
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
                StringBuilder output;
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

                public EventLog(string title, bool showTicWidget = false, int maxEntries = 5)
                {
                    log = new List<string>();
                    this.title = title;
                    scriptTag = "";
                    output = new StringBuilder();
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
                public void add(string entry)
                {
                    //Timestamp the new entry and place it at the front of the list.
                    log.Insert(0, $"{DateTime.Now.ToString("HH:mm:ss")}- {entry}");
                    //log.Add($"{DateTime.Now.ToString("HH:mm:ss")}- {entry}");
                    //If we've reached the maximum number of entries, remove the last one.
                    if (log.Count > maxEntries)
                    { log.RemoveAt(maxEntries); }
                    //{ log.RemoveAt(0); }
                    //Flag the log as having been recently updated.
                    hasUpdate = true;
                }

                //Sets the 'updated' flag to false. Call after pulling the new log.
                public void updateClaimed()
                { hasUpdate = false; }

                //Get the logged events in a readable format
                public string toString()
                {
                    //Clear the old output
                    output.Clear();
                    //Start with the title
                    output.Append(title);
                    //If the tic widget is currently in use...
                    if (ticWidgetIndex != -1)
                    //... pick the proper frame and add it.
                    { output.Append($" {ticWidget[ticWidgetIndex]}"); }
                    output.Append("\n");
                    //If we've got a custom scriptTag...
                    if (!String.IsNullOrEmpty(scriptTag))
                    //...tack it on
                    { output.Append($"Script Tag: {scriptTag}\n"); }
                    //If scriptUpdateDelay isn't 0...
                    if (scriptUpdateDelay != 0)
                    //...include a notice.
                    { output.Append($"Current Update Delay: {scriptUpdateDelay}\n"); }
                    //Take every string in the log...
                    foreach (string entry in log)
                    //...and tack it onto our output string, too
                    { output.Append("\n" + entry + "\n"); }
                    //Chuck the string we just built out to whom it may concern.
                    return output.ToString();
                }
            }

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
            public static string readableInt(ref string readable, int num)
            {
                readable = "";
                //If the number is greater than 10 million, just take the last 6 digits and replace them with an M
                if (num >= 10000000)
                { readable = (int)(num / 1000000) + "M"; }
                //If the number is between 10 million and 1 million, replace the last 6 digits with an M, and keep the 
                //first replaced digit as a decimal
                else if (num >= 1000000)
                { readable = Decimal.Round((num / 1000000), 1) + "M"; }
                //If the number is between a million and 10 thousand, replace the last 3 digits with a K
                else if (num >= 10000)
                { readable = (int)(num / 1000) + "K"; }
                //if the number is between 10 thousand and 1 thousand, replace the last 3 digits with a K and one decimal.
                else if (num >= 1000)
                { readable = Decimal.Round((num / 1000), 1) + "K"; }
                //If the number isn't greater than a thousand, why'd you call this in the first place?
                else
                { readable += num; }
                return readable;
            }
        }
    }
}

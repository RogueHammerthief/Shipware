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
    //ARCHIVE: Created just prior to tearing out CargoManager

    //The Holy Grail. My old hard-coded ship manager series, completely configurable from a grid's
    //CustomData. It's going to take some work.
    //Components:
    //  Readable Tallies:
    //    Volume, Item, Oxygen, Hydrogen, Power, Jump Charge, Raycast, Max Output (Solar/Wind),
    //    HydrogenWithEngines?, ShieldIntegrity?
    //  Other functions:
    //    Roost, Perch, Raycast, Multifunction Display?, Ascent?, Door Minder?, Damage Report?
    //  Support commands:
    //    IGC, Evaluate, Populate, Clear, AutoPopulate? Update? ChangeTag? Nuke?

    ///<status> <date>20201215</date>
    ///  <content>
    ///     Alright, gonna need a backup before I start tearing into the CargoManager.
    ///  </content>
    ///</status>

    ///<status> <date>20201214</date>
    ///  <content>
    ///     Trying to decide what to work on next. TallyGeneric seems to be holding up fairly well, 
    ///   though I've only tested a couple of the handlers (And the Hydrogen one didn't return the 
    ///   values I was expecting). Still, I think for now I'll work on getting evaluate() to work 
    ///   with the new tally types. That'll give me a lot more control - and flexibility - for 
    ///   testing purposes. After that... either the various Populate commands, or the ActionSet 
    ///   implementation. I'm kind of leaning towards the latter, given that I may need to know 
    ///   the general shape of that when I go to implement Populate.
    ///     Actually, Raycast might be a good canidate for the next project after Evaluate. Or 
    ///   possibly even before, given that there'll need to be a fair amount of special handling 
    ///   for it in Evaluate. 
    ///     Yes, that's a very good argument for starting with Evaluate. Which is why I went and
    ///   added a subtitle field to EventLog, and switched it to using a StringBuilder instead of
    ///   string concatenation, instead of working on Evaluate.
    ///     Part of my hesitance for working on Evaluate at the moment is the realization that I'm
    ///   going to need to check CargoManager to see if a tally is in it, then the list of 
    ///   TallyGenerics (Or a GenericManager, if I go with that) to see if the tally is in there. I
    ///   may even need to check the list of ActionSets to see if the 'tally' is in there, depending
    ///   on what kind of implementation I go with. Is this a problem? Should I be using a temporary
    ///   dictionary in evaluation to make these checks? And if I do that, why am I bothering with
    ///   the one in CargoManager?
    ///     After doing a bit of investigation, it does indeed appear that most of the functions in
    ///   CargoManager have to do with adding things to the internal dictionary (Which, to be clear,
    ///   I only use during evaluation). Over the process of the script, I've shifted more and more
    ///   functionality out of the Manager and into the Evaluation method itself. Now... I'm 
    ///   thinking it might be time to finish gutting it, or at least re-purposing it to be more 
    ///   broad. Because one thing it still does is manage Cotainers, and I think that 
    ///   implementation is worth keeping. 
    ///     Maybe it would be best if I worked through the steps:
    ///     1. All tallies and their configurations are read from the PB.
    ///     2. Blocks from the grid are added to the tallies as the Evaluate method encounters them.
    ///        Directly in the case of Generics, but with Containers acting as middlemen in the case
    ///        of CargoTally and ItemTally.
    ///     3. All tallies are finalized at the end of grid evaluation. More complex data structures
    ///        like lists are torn down to arrays for use during execution.
    ///     4. During Execution, the current value of all tallies are cleared, and new values are
    ///        determined by polling each block in turn. In the case of Containers, each block is
    ///        polled once (Sort of), and that value sent to all of the tallies associated with that
    ///        container. In the case of Generics, each block is polled directly from a list of 
    ///        blocks stored in the tally itself. If a block is on more that one Tally's list, the
    ///        block is polled redundantly (Though this should be a fairly cheap operation)
    ///     So... maybe a GenericManager would be used during execution, and would hold both an
    ///   array of containers and generic tallies? Or maybe containers and all the tallies?
    ///     Idea: It'd be nice if I could display ActionSet statii on the same screens that I do
    ///   Tallies. Maybe I could formalize a ScreenElement object that stores exactly what I need
    ///   to make a Sprite, then have both Tallies and ActionSets store an instance of that object?
    ///   Though, I'd loose some flexibility doing that. What if I want to come back later and make
    ///   a 'line' implementation?
    ///     Idea: One notion I came up with the other day was a 'Nuke' command, which would remove 
    ///   ALL of the custom data on the grid, regardless of whether or not it was part of this 
    ///   script. While I think that'd be useful, my cheif concerns are someone doing it accidently
    ///   (So it'd need some sort of confirmation), or someone hitting it maliciously (Which... I 
    ///   don't think I could do anything about. And even if I could, someone could theoretically 
    ///   write their own script to do just this, so... maybe it isn't worth worrying about.).
    ///     'It was the only way to be sure.'
    ///  </content>
    ///</status>

    ///<status> <date>20201212</date>
    ///  <content>
    ///     Wrote the rest of the basic handlers, which didn't take near as long as I thought it
    ///   would. Leaving off HydrogenWithEngines for now (Because it'll be a pain) and 
    ///   ShieldIntegrity (Because how, even?). Plugged the Hydrogen handler in to the temporary
    ///   code and it seemed to work (Although the values it was reporting weren't what I expected).
    ///  </content>
    ///</status>

    ///<status> <date>20201211</date>
    ///  <content>
    ///     Ran into an issue when trying to test the new TallyGeneric. Kept getting the 'No tally
    ///   config on the PB' message. I've noticed that setting the first tally name to 'blank' 
    ///   doesn't throw the bespoke error for that, which makes me think the loop isn't even running.
    ///   I've also noticed that the EventLog isn't being posted after a recompile, but it is being
    ///   posted after the Evaluate command is run, which... I don't currently have an explanation 
    ///   for. Could be related, though.
    ///     Worth noting, I plugged the 'finished' version of Capacity back into the PB, and it ran 
    ///   and failed in the expected ways. So this is something new I've broken.
    ///     ...Aaaaaand I'm dumb. Of course the old configs were still working for Capacity, because
    ///   they still had the Capacity section tags. As opposed to the ShipManager tags I'd set this
    ///   script to look for. 
    ///     <sigh>
    ///     I'm just going to switch this script back to using Capacity for the moment because 
    ///   Capacity seems to have most of the bugs ironed out, and I should be able to use it as a 
    ///   'last known good'. Also, it'll make a good test case if I decide to put in a 'ChangeTag'
    ///   command.
    ///     The power report seems to work, by the way. Once I remembered that I need to increment
    ///   max as I go along and call finishSetup once I'm done, anyhow.
    ///     Also, I moved setting the 'handled' flags so they'll always be set if there was a sorter
    ///   for that block type. 
    ///  </content>
    ///</status>

    ///<status> <date>20201210</date>
    ///  <content>
    ///     On Populate: What I'm thinking right now is that the basic Populate command would take
    ///   whatever is in the PB's Populate section and write it to all of the CustomDatas of every
    ///   block in the Populate group, completely replacing any ShipManager data already present.
    ///     Using the -add switch would read the keys present in the Populate section, and would 
    ///   either add the keys to the blocks in the group, or append the contents of the keys to the
    ///   group if the keys are already present.
    ///     The -ow switch would read the keys in the Populate section and use it to overwrite the
    ///   contents of the keys in the Populate group. If the block didn't already have the key, no
    ///   new data would be added.
    ///     Not sure if the -all switch would have a place in this implementation.
    ///     If I go with this iteration of Populate, I'd probably need AddTally and AddActionSet
    ///   commands to add information directly to the PB. Wonder if I could work in an addSurface
    ///   as well? Because this implementation just ain't going to be smart enough to figure all
    ///   this out itself.
    ///     I'm not sure if a version that is smart enough is an acheivable goal. I was able to 
    ///   (Basically) do it with RemoteStart, but that had /one/ job, a hell of a lot less 
    ///   customization, and an entirely different data structure.
    ///     On another note (The intended note, really, given that these are supposed to be status
    ///   reports), I think I've got the TallyGeneric class done, and I have a handlePower function
    ///   ready to be plugged into it. I think tommorrow I'll hard-code one in and see if it works.
    ///   Then maybe I'll give some more thought as to if I need (Or want, this isn't really a case
    ///   where I /need/) some sort of GenericManager to bundle all my TallyGenerics up in.
    ///  </content>
    ///</status>

    ///<status> <date>20201209</date>
    ///  <content>
    ///     I think that Perch/Roost are going to end up being very cut-down versions of RemoteStart.
    ///   Basically, I'll have a set of one-line handlers that work on one block, and then I'll 
    ///   store the block and its handler in a list in an ActionSet. And while I'm at it, I could
    ///   probably make it so that you don't need to use the names 'roost' and 'perch', but could
    ///   instead define your own names, much as you do with tallies. 
    ///     ...Actually, just add IGC access and some way of displaying an Action's current status,
    ///   and you've basically got a (slightly dumber and oddly more-flexible) RemoteStart. This 
    ///   version of the ActionSet would need to be able to set an UpdateFrequency delay interval, 
    ///   though. And non-zero delay intervals should probably be displayed somewhere. Maybe the 
    ///   title of the event log? Or add a subtitle that can be changed during execution. Then I 
    ///   could put other information there, like the current SECTION_TAG. And as long as I'm 
    ///   looking at it, I could probably switch the EventLog to using a StringBuilder.
    ///     It'd also need a way of displaying the status of an ActionSet. Maybe the state texts 
    ///   and colors could be set in the config as well? Action1OnText = Enabled, Action1OnColor = 
    ///   Green? Action1OffScript = TSS_ClockAnalog?
    ///     I was thinking that an alternative to Populate (Or at least the version I'm using now)
    ///   might be to have the user make a [Populate] section on the PB, and a Populate group on
    ///   the grid. Then when they ran the Populate command (Without specifying a group, but maybe
    ///   including switches like -all or -replace), the contents of the [Populate] section would
    ///   be written to every block in the Populate group, using the script's SECTION_TAG. That 
    ///   would be a fairly quick an intuitive way of handling it, but I think it'd be an all-or-
    ///   nothing proposition. You couldn't add or replace with it. Maybe if I switched to 
    ///   individual components getting their own section?
    ///  </content>
    ///</status>

    ///<status> <date>20201208</date>
    ///  <content>
    ///     Now the battle begin in earnest.
    ///     I think one of the first steps is going to be getting the tallies working. Right now, I'm
    ///   thinking the basis will be a Tally interface. The current Tally will become TallyCargo. A
    ///   new TallyGeneric will use handler Funcs to work with everything that doesn't involve an 
    ///   inventory.
    ///     For error detection during script evaluation, I'll need a way of telling what kind of 
    ///   tally I'm dealing with. I had been considering storing a string, or maybe an enumerator.
    ///   Wonder if I could I expose the handler with a get; method, then just run a comparison?
    ///     Also, while I've already decided that I'll be using a handler to get the current value
    ///   of a TallyGeneric, how will I get the max? Maybe in the sorters during evaluation?
    ///     I guess one question is, should I make a TallyManager to run alongside CargoManager? Or 
    ///   just use a list? 
    ///  </content>
    ///</status>

    partial class Program : MyGridProgram
    {
        //The section tag that will be applied to this script's data in CustomData.
        //TEST CODE: Leaving this as Capacity for the moment, so I can use that as a last known 
        //good configuration.
        const string SECTION_TAG = "Capacity";
        //The cargo manager that... basically does everything in this script.
        CargoManager cargo;
        //TEST CODE? A list of generic tallies that store and process information about power and
        //hydrogen and the like.
        List<ITally> generics;
        //The reports that tell about what the cargo manager is doing.
        Report[] reports;
        //The indicators that change color based on what a tally is doing
        Indicator[] indicators;
        //An EventLog that will... log events.
        Hammers.EventLog log;
        //Used to read information out of a block's CustomData
        MyIni iniReader;
        //Used to parse arguments entered as commands
        MyCommandLine argReader;

        public Program()
        {
            initiate();
            evaluate();
            //The main method Echos the event log every time it finishes running. But there's a lot
            //of stuff that can go wrong when parsing configuration, so we need an Echo here as well.
            Echo(log.toString());
        }

        public void Main(string argument, UpdateType updateSource)
        {
            //Is this the update tic?
            if (updateSource == UpdateType.Update100)
            {
                //GET ALL THE DATA
                cargo.calculateCurr();
                //TEST CODE?
                foreach (TallyGeneric generic in generics)
                { generic.compute(); }
                //WRITE ALL THE REPORTS
                foreach (Report report in reports)
                { report.update(); }
                //COLOR ALL THE LIGHTS
                foreach (Indicator indicator in indicators)
                { indicator.update(); }
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
                        //A dead-simple method for passing messages to the IGC
                        //Argument format: IGC <tag> <data>
                        //Argument example: IGC RemoteStart GateBay1
                        case "IGC":
                            IGC.SendBroadcastMessage(argReader.Argument(1), argReader.Argument(2));
                            log.add($"Sent the following IGC message on channel '{argReader.Argument(1)}'" +
                                $": {argReader.Argument(2)}.");
                            break;
                        //Populate is crazy useful, but I'm going to put all my effort into getting
                        //it working for ShipManager.
                        case "Populate":
                            log.add("The 'Populate' command has not been implemented for this script.");
                            break;
                        //Run the evaluate() method, checking for any changes to the grid or the 
                        //CustomData of its blocks.
                        case "Evaluate":
                            evaluate();
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
            //I've left this functionallity in, even though it's not currently in use. 
            log.clearUpdate();
        }

        public void initiate()
        {
            //Initiate some bits and pieces, though most of the work will be done in evaluate()
            iniReader = new MyIni();
            argReader = new MyCommandLine();
            log = new Hammers.EventLog("Ship Manager - Recent Events");
            //Assure the user that we made it this far.
            log.add("Script initialization complete.");
        }

        public void evaluate()
        {
            //We need to re-initialize these every time we do a new evaluation.
            cargo = new CargoManager();
            //We'll need the ability to move data around during evaluation. A list will suffice for
            //reports, but we'll need a dictionary to make the indicators work.
            List<Report> evalReports = new List<Report>();
            Dictionary<string, Indicator> evalIndicators = new Dictionary<string, Indicator>();
            //TEST CODE
            generics = new List<ITally>();
            //We'll need to pass the GTS around a bit for this. May as well put an easy handle on it.
            IMyGridTerminalSystem GTS = GridTerminalSystem;
            //A couple of extra variables for working directly with MyIni
            MyIniParseResult parseResult = new MyIniParseResult();
            MyIniValue iniValue = new MyIniValue();
            //We'll need to do some configuration on tallies before we send them on their way. Let's
            //use an easy handle for it.
            TallyCargo tally;
            //Ditto reports
            Report report;
            //Some blocks do multiple jobs, which means a block has to be subjected to multiple 
            //different sorters. This variable will tell us if at least one of those sorters knew 
            //how to handle the block.
            bool handled = false;
            //We'll need a string to store errors.
            string errors = "";
            //We'll use these strings to store the information we need to build a tally.
            string tallyName = "";
            string tallyTypeID = "";
            string tallySubTypeId = "";
            double tallyMax = -1;
            //The tallies a block reports to are stored in a delimited string. We'll need something
            //to hold those as something easier to work with.
            string[] tallyNames;
            //We'll need lists for our various tally types
            List<TallyCargo> cargoRefs;
            List<TallyGeneric> genericRefs;
            List<ITally> tallyRefs;
            //The ubiquitous list of terminal blocks.
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

            //Our first step will be to check the programmable block for tally configs.
            //From the PB, we read:
            //Tally<#>Name: The name that will be associated with this tally.
            //Tally<#>TallyTypeID: For TallyItems, the ID that will be fed into MyItemType
            //Tally<#>TallySubTypeID: For TallyItems, the sub type ID that will be fed into 
            //  MyItemType
            //Tally<#>Max: For TallyItems, the arbitrary number that will serve as the maximum for
            //  this tally.
            //Tally<#>DisplayName: A name that will be shown on screens instead of the defualt name
            //Tally<#>Multiplier: (Default = 1) The multiplier that will be applied to this tally. 
            //Tally<#>LowGood: (Default = true) Will this report be color-coded using the assumption
            //  that low numbers are a good thing?

            //Parse the PB's custom data. If it doesn't return something useable...
            if (!iniReader.TryParse(Me.CustomData, out parseResult))
            //...file a complaint.
            {
                errors = $"The parser was unable to read information from the Programmable Block. " +
                      $"Reason: {parseResult.Error}";
            }
            //The counter for this loop.
            int counter = 0;
            //As long as we don't have errors, and the counter isn't -1 (Which indicates that we've
            //run out of tallies)...
            while (string.IsNullOrEmpty(errors) && counter != -1)
            {
                //Look for another tally
                tallyName = iniReader.Get(SECTION_TAG, $"Tally{counter}Name").ToString();
                //First thing's first: There's exactly one tally name we've reserved. Is the user
                //trying to use it?
                if (tallyName.ToLowerInvariant() == "blank")
                //Complain. Righteously.
                {
                    errors += $"The Tally name '{tallyName}' is reserved by the script to indicate" +
                          $"where portions of the screen should be left empty. Please choose a " +
                          $"different name.";
                }
                //Now then. Did we get a tally name?
                else if (!string.IsNullOrEmpty(tallyName))
                {
                    //Quick! See if there's configuration data for a tally item!
                    tallyTypeID = iniReader.Get(SECTION_TAG, $"Tally{counter}TypeID").ToString();
                    //If the tallyTypeID we just tried to get isn't empty...
                    if (!string.IsNullOrEmpty(tallyTypeID))
                    //...this is a TallyItem, and we need more data.
                    {
                        //Try to get the subtype ID
                        tallySubTypeId = iniReader.Get(SECTION_TAG, $"Tally{counter}SubTypeID").ToString();
                        //If it didn't work, log an error
                        if (string.IsNullOrEmpty(tallySubTypeId))
                        { errors += $"Item Tally {tallyName} has a missing or unreadable TallySubTypeID.\n"; }
                        //Now try to get the max, returning a -1 if we can't figure out what it is
                        tallyMax = iniReader.Get(SECTION_TAG, $"Tally{counter}Max").ToDouble(-1);
                        if (tallyMax == -1)
                        { errors += $"Item Tally {tallyName} has a missing or unreadable TallyMax.\n"; }
                        //If we don't have any errors at this point, we should have all the data we 
                        //need for this tally.
                        if (string.IsNullOrEmpty(errors))
                        { tally = cargo.addTallyItem(tallyName, tallyTypeID, tallySubTypeId, tallyMax); }
                        else
                        //If we've encountered an error, we won't have a tally to work with in the
                        //rest of the loop. We need to get out.
                        { break; }
                    }
                    else
                    //Otherwise, it's a regular tally, and the name is all we need.
                    { tally = cargo.addTally(tallyName); }
                    //Now that we have our tally, we need to check to see if there's any further
                    //configuration data. 
                    //First, the DisplayName
                    iniValue = iniReader.Get(SECTION_TAG, $"Tally{counter}DisplayName");
                    if (!iniValue.IsEmpty)
                    { tally.name = iniValue.ToString(); }
                    //Then the Multiplier
                    iniValue = iniReader.Get(SECTION_TAG, $"Tally{counter}Multiplier");
                    if (!iniValue.IsEmpty)
                    { tally.multiplier = iniValue.ToDouble(); }
                    //Last, LowGood
                    iniValue = iniReader.Get(SECTION_TAG, $"Tally{counter}LowGood");
                    if (!iniValue.IsEmpty)
                    { tally.lowGood(iniValue.ToBoolean()); }

                    //That's all the data we're lookin for here. Increment the counter and we'll go 
                    //again
                    counter++;
                }
                else
                //If we didn't find another tally, set the counter equal to -1 to indicate that 
                //we're done in this loop.
                { counter = -1; }
            }
            //If we don't have errors, but we also don't have any tallies...
            if (string.IsNullOrEmpty(errors) && cargo.getTallyCount() == 0)
            { errors += "No tally configuration found on the programmable block.\n"; }

            //Only if there were no errors with parsing the PB...
            if (string.IsNullOrEmpty(errors))
            {
                //...should we get the blocks on the grid with our section tag.
                errors += Hammers.findBlocks<IMyTerminalBlock>(GTS, blocks, b =>
                    (b.IsSameConstructAs(Me) && MyIni.HasSection(b.CustomData, SECTION_TAG)),
                    $"No blocks found on this construct with a [{SECTION_TAG}] INI section.");
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
                    errors += $"The parser was unable to read information from block " +
                          $"'{block.CustomName}'. Reason: {parseResult.Error}\n";
                }

                //The PB gets its own sorter. Because if we made it this far, it's handled.
                if (parseResult.Success && block == Me)
                { handled = true; }

                //In the CargoManager, the data is handled by two seperate yet equally important
                //objects: the Tallies that store and calculate information and the Reports that 
                //display it. These are their stories.
                //...There may also be some lights invovled. 
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
                    //Surface<#>Columns: (Default = 3) The number of columns to use when arranging 
                    //  the reports on the designated surface.
                    //Surface<#>FontSize: (Default = 1f) The font size to be used
                    //Surface<#>Font: (Default = Debug) The font type to be used
                    //For every surface on this block...
                    for (int i = 0; i < ((IMyTextSurfaceProvider)block).SurfaceCount; i++)
                    {
                        //Are we supposed to display some tallies on this surface?
                        if (iniReader.ContainsKey(SECTION_TAG, $"Surface{i}Tallies"))
                        {
                            //Get the tallies we're supposed to display. Store it in the iniValue 
                            //for now...
                            iniValue = iniReader.Get(SECTION_TAG, $"Surface{i}Tallies");
                            //...because there's a lot of stuff we need to do before it's ready.
                            //Split on the comma delimeter, trim away the whitespace of each entry 
                            //in the resulting array of strings. By the way, this is not my code.
                            tallyNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                            //We have names, but we need the actual tallies that go with them to 
                            //make our report.
                            tallyRefs = new List<ITally>();
                            foreach (string name in tallyNames)
                            {
                                //Is this a blank slot in the report?
                                if (name.ToLowerInvariant() == "blank")
                                //Just add a null to the list. The report will know how to handle 
                                //this.
                                { tallyRefs.Add(null); }
                                else
                                {
                                    //If it isn't a blank, we'll need to get the tally from the 
                                    //CargoManager
                                    tally = cargo.getTallyByName(name);
                                    //We could conceivably be trying to reference a tally that 
                                    //doesn'texist, here. Better check.
                                    if (tally != null)
                                    { tallyRefs.Add(tally); }
                                    else
                                    //And complain, if appropriate.
                                    {
                                        errors += $"Surface provider '{block.CustomName}', surface " +
                                            $"{i}, tried to reference the unconfigured tally " +
                                            $"'{tallyName}'.\n";
                                    }
                                }
                            }
                            //That's all the data we're required to have.
                            report = new Report(((IMyTextSurfaceProvider)block).GetSurface(i), tallyRefs);
                            //We have a report. Now we need to see if the user wants anything 
                            //special done with it.
                            //Columns
                            iniValue = iniReader.Get(SECTION_TAG, $"Surface{i}Columns");
                            if (!iniValue.IsEmpty)
                            { report.setAnchors(iniValue.ToInt32()); }
                            //FontSize
                            iniValue = iniReader.Get(SECTION_TAG, $"Surface{i}FontSize");
                            if (!iniValue.IsEmpty)
                            { report.fontSize = iniValue.ToSingle(); }
                            //Font
                            iniValue = iniReader.Get(SECTION_TAG, $"Surface{i}Font");
                            if (!iniValue.IsEmpty)
                            { report.font = iniValue.ToString(); }

                            //All done? Last step is to add this report to our list of reports. So
                            //we'll know where it lives.
                            evalReports.Add(report);
                        }
                    }
                }

                //That hadles any surfaces and their Reports. The bulk of what this script watches
                //are made up of blocks with inventories.
                if (parseResult.Success && block.HasInventory)
                {
                    handled = true;
                    //From inventory blocks, we read:
                    //Tallies: The tallies this inventory should report to.
                    //If there is no Tallies key, we need to check for:
                    //Tallies<#>: A set of tallies tied specifically to one of the block's 
                    //inventories
                    iniValue = iniReader.Get(SECTION_TAG, "Tallies");
                    if (!iniValue.IsEmpty)
                    {
                        tallyNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                        //Now we need to get the Tallies referenced by the strings in TallyNames
                        cargoRefs = new List<TallyCargo>();
                        foreach (string name in tallyNames)
                        {
                            tally = cargo.getTallyByName(name);
                            //Just as with the reports, we need to check to make sure there's 
                            //actually a Tally.
                            if (tally != null)
                            { cargoRefs.Add(tally); }
                            else
                            {
                                errors += $"Inventory block '{block.CustomName}' tried to " +
                                    $"reference the unconfigured tally '{name}'.\n";
                            }
                        }
                        //For configurations tied to the 'Tallies' key, we use the same set of 
                        //Tallies for every inventory on the block.
                        for (int i = 0; i < block.InventoryCount; i++)
                        { cargo.addContainer(block.GetInventory(i), cargoRefs); }
                    }
                    //Didn't find a Tallies key? Maybe the user wants to configure the inventories
                    //individually.
                    else
                    {
                        //Time to bust out the counter. This time, we'll use it to see if we've 
                        //found any individual configurations.
                        counter = 0;
                        //For every inventory this block has...
                        for (int i = 0; i < block.InventoryCount; i++)
                        {
                            //...check to see if there's config specifically for this inventory.
                            iniValue = iniReader.Get(SECTION_TAG, $"Tallies{i}");
                            if (!iniValue.IsEmpty)
                            {
                                //Much as above, we turn the iniValue into an array of names...
                                tallyNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                                cargoRefs = new List<TallyCargo>();
                                foreach (string name in tallyNames)
                                {
                                    //...use the tally names to get tally references...
                                    tally = cargo.getTallyByName(name);
                                    if (tally != null)
                                    { cargoRefs.Add(tally); }
                                    else
                                    {
                                        errors += $"Inventory block '{block.CustomName}' tried to " +
                                            $"reference the unconfigured tally '{name}'.\n";
                                    }
                                }
                                //...And then add this container with the tallies we've found.
                                cargo.addContainer(block.GetInventory(i), cargoRefs);
                                //We'll also increment the counter, to indicate that we successfully
                                //found some individual inventory configuration.
                                counter++;
                            }
                        }
                        //If the counter is still 0...
                        if (counter == 0)
                        { errors += $"Inventory block {block.CustomName} has missing or unreadable Tallies.\n"; }
                    }
                }

                //This could also be an indicator light, something I totally didn't forget when I 
                //first wrote this. Let's check!
                if (parseResult.Success && block is IMyLightingBlock)
                {
                    handled = true;
                    //From lights, we read:
                    //Tally: The Tally (Singular) that this indicator group watches
                    iniValue = iniReader.Get(SECTION_TAG, "Tally");
                    if (!iniValue.IsEmpty)
                    {
                        tallyName = iniValue.ToString();
                        tally = cargo.getTallyByName(tallyName);
                        //Once again, our first step is to make sure there's a tally in CargoManager
                        if (tally != null)
                        {
                            //If we don't already have an entry for this tally in our dictionary...
                            if (!evalIndicators.ContainsKey(tallyName))
                            //... Add one
                            { evalIndicators.Add(tallyName, new Indicator(tally)); }
                            //Once we're sure there's an Indicator group in the dictionary, add 
                            //this light to it.
                            evalIndicators[tallyName].addLight((IMyLightingBlock)block);
                            //Also, declare 'handled'
                        }
                        else
                        {
                            errors += $"Lighting block '{block.CustomName}' tried to reference " +
                                $"the unconfigured tally '{tallyName}'.\n";
                        }
                    }
                    else
                    { errors += $"Lighting block {block.CustomName} has missing or unreadable Tally.\n"; }
                }

                //If we made it here, but the block hasn't been handled, it's time to complain.
                if (parseResult.Success && !handled)
                { errors += $"Block type of '{block.CustomName}' cannot be handled by this script.\n"; }

                //Set handled to 'false' for the next iteration of the loop.
                handled = false;
            }

            /*--- TEST CODE ---*/
            errors += Hammers.findBlocks<IMyTerminalBlock>(GTS, blocks, b =>
                (b.IsSameConstructAs(Me) && b is IMyBatteryBlock),
                "Didn't find any batteries. Wat.");
            TallyGeneric testTally = new TallyGeneric("Power", handlePower, false);
            foreach (IMyTerminalBlock block in blocks)
            {
                testTally.addBlock(block);
                testTally.incrementMax(((IMyBatteryBlock)block).MaxStoredPower);
            }
            testTally.finishSetup();
            generics.Add(testTally);

            errors += Hammers.findBlocks<IMyTerminalBlock>(GTS, blocks, b =>
                (b.IsSameConstructAs(Me) && b is IMyGasTank &&
                b.BlockDefinition.SubtypeId == "LargeHydrogenTank" ||
                b.BlockDefinition.SubtypeId == "SmallHydrogenTank" ||
                b.BlockDefinition.SubtypeId == "LargeHydrogenTankSmall" ||
                b.BlockDefinition.SubtypeId == "SmallHydrogenTankSmall" ),
                "Didn't find any Hydrogen tanks.");
            testTally = new TallyGeneric("Hydrogen", handleGas, false);
            foreach (IMyTerminalBlock block in blocks)
            {
                testTally.addBlock(block);
                testTally.incrementMax(((IMyGasTank)block).Capacity);
            }
            testTally.finishSetup();
            generics.Add(testTally);

            Report testReport = new Report(Me.GetSurface(0), generics);
            evalReports.Add(testReport);
            /*- END TEST CODE -*/

            //Time to finalize things. A call to calculateMax will finish configuration of the 
            //CargoManager, and pulling the arrays out of the evaluation data structures for reports
            //and indicators should save us a bit of overhead during updates.
            cargo.calculateMax();
            reports = evalReports.ToArray();
            indicators = evalIndicators.Values.ToArray();
            //There's probably still data in the iniReader. We don't need it anymore, and we don't
            //want it carrying over to any future evaluations.
            iniReader.Clear();

            //That should be it. So if we have no errors...
            if (errors == "")
            {
                //...Set the script into motion.
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
                //Also, brag.
                log.add($"Grid evaluation complete. Registered {cargo.getTallyCount()} tallies and " +
                    $"{evalReports.Count} reports.");
                log.add("Setup complete. Script is now running.");
            }
            else
            {
                log.add($"Grid evaluation complete. The following errors are preventing script " +
                    $"execution:\n{errors}");
            }
        }

        public interface ITally
        {
            //Properties and methods needed by Reports
            string name { get; set; }
            string readableCurr { get; }
            string readableMax { get; }
            Color code { get; }
            string getMeter();

            //Other methods that all Tallies should probably have.
            void finishSetup();
            /*void clearCurr();*/
            /*void incrementMax(double max);*/
            /*AddInventoryToCurr*/ //Ah. Ooops.
            void compute();
        }

        public class TallyGeneric : ITally
        {
            //The name of this tally. While the CargoManager knows what this is, the Reports don't.
            //So we'll just store it here.
            public string name { get; set; }
            //Sometimes, the data we get from blocks doesn't match up with what we see or expect
            //from the in-game interfaces (For instance, the volume measurements we get from
            //invetories are in kilo-liters, but in-game we see them in liters). This multiplier
            //will be applied to the curr and max values calculated by this tally, letting you 
            //adjust the scales to your liking.
            public double multiplier { private get; set; }
            //The current value of this tally, as adjusted by addInventoryToCurr
            public double curr { get; protected set; }
            //The maximum value of this tally, usually set shortly after object creation.
            public double max { get; protected set; }
            //How 'full' this tally is, as measured by the curr against the max. Shold be between
            //0 and 100
            public double percent { get; private set; }
            //A representation of the current value of this tally, in a readable string format.
            public string readableCurr { get; private set; }
            //A readable string format for the maximum of this tally.
            public string readableMax { get; private set; }
            //A StringBuilder to build and store the ASCII-style meter representing the percentage
            //of this tally that is 'full'.
            StringBuilder meter;
            //A color code for this tally, based on the percentage.
            public Color code { get; private set; }
            //The function we use to figure out what color to associate with the current value of 
            //this tally
            Func<double, Color> colorHandler;
            //The internal structure of a TallyGeneric is a lot simpler than that of a TallyCargo.
            //These tallies contain direct references to the blocks they watch. However, we have to
            //build it as we go along, so we'll need a list instead of an array.
            List<IMyTerminalBlock> blocks;
            //The function that will be used to get the curent value of the blocks in this tally. 
            //It has a get method so we can use it to identify what kind of tally this is.
            public Func<List<IMyTerminalBlock>, double> currHandler { get; private set; }

            public TallyGeneric(string name, Func<List<IMyTerminalBlock>,double> handler, bool isLow = true, double multiplier = 1)
            {
                this.name = name;
                currHandler = handler;
                lowGood(isLow);
                this.multiplier = multiplier;
                curr = 0;
                max = 0;
                percent = 0;
                readableCurr = "curr";
                readableMax = "max";
                meter = new StringBuilder("[----------]");
                code = Hammers.cozy;
                blocks = new List<IMyTerminalBlock>();
            }

            //Set the color code mode
            public void lowGood(bool isLow)
            {
                if (isLow)
                { colorHandler = Hammers.handleColorCodeLow; }
                else
                { colorHandler = Hammers.handleColorCodeHigh; }
            }

            //Get the meter
            public string getMeter()
            { return meter.ToString(); }

            public void addBlock(IMyTerminalBlock block)
            { blocks.Add(block); }

            //Because of the way the data is arranged, Tally has to be told when it has all of its
            //data.
            public void finishSetup()
            {
                //Apply the multiplier to the max.
                max = max * multiplier;
                //Max will never change unless re-initialized. So we'll figure out what readableMax
                //is once and just hold on to it.
                readableMax = Hammers.readableInt((int)max);
            }

            internal virtual void incrementMax(double val)
            { max += val; }

            //Using curr and max, derive the remaining components needed to form a Report. Unlike
            //TallyCargo, we can compute curr from this method
            public void compute()
            {
                //Use the handler to calculate the curr of our blocks.
                curr = currHandler(blocks);
                //Now, first thing we need to do is apply the multiplier.
                curr = curr * multiplier;
                //Now for the percent. We'll need it for everything else. But things will get
                //weird if it's more than 100.
                percent = Math.Min(curr / max, 100) * 100;
                //Next, get the color code from our color handler. 
                code = colorHandler(percent);
                //Now, the meter.
                Hammers.drawMeter(ref meter, percent);
                //Last, we want to show curr as something you can actually read.
                readableCurr = Hammers.readableInt((int)curr);
            }
        }

        public class TallyCargo : ITally
        {
            //The name of this tally. While the CargoManager knows what this is, the Reports don't.
            //So we'll just store it here.
            public string name { get; set; }
            //Sometimes, the data we get from blocks doesn't match up with what we see or expect
            //from the in-game interfaces (For instance, the volume measurements we get from
            //invetories are in kilo-liters, but in-game we see them in liters). This multiplier
            //will be applied to the curr and max values calculated by this tally, letting you 
            //adjust the scales to your liking.
            public double multiplier { private get; set; }
            //The current value of this tally, as adjusted by addInventoryToCurr
            public double curr { get; protected set; }
            //The maximum value of this tally, usually set shortly after object creation.
            public double max { get; protected set; }
            //How 'full' this tally is, as measured by the curr against the max. Shold be between
            //0 and 100
            public double percent { get; private set; }
            //A representation of the current value of this tally, in a readable string format.
            public string readableCurr { get; private set; }
            //A readable string format for the maximum of this tally.
            public string readableMax { get; private set; }
            //A StringBuilder to build and store the ASCII-style meter representing the percentage
            //of this tally that is 'full'.
            StringBuilder meter;
            //A color code for this tally, based on the percentage.
            public Color code { get; private set; }
            //The function we use to figure out what color to associate with the current value of 
            //this tally
            Func<double, Color> colorHandler;

            public TallyCargo(string name, bool isLow = true, double multiplier = 1)
            {
                this.name = name;
                lowGood(isLow);
                this.multiplier = multiplier;
                curr = 0;
                max = 0;
                percent = 0;
                readableCurr = "curr";
                readableMax = "max";
                meter = new StringBuilder("[----------]");
                code = Hammers.cozy;
            }

            //Set the color code mode
            public void lowGood(bool isLow)
            {
                if (isLow)
                { colorHandler = Hammers.handleColorCodeLow; }
                else
                { colorHandler = Hammers.handleColorCodeHigh; }
            }

            //Get the meter
            public string getMeter()
            { return meter.ToString(); }

            //Because of the way the data is arranged, Tally has to be told when it has all of its
            //data.
            public void finishSetup()
            {
                //Apply the multiplier to the max.
                max = max * multiplier;
                //Max will never change unless re-initialized. So we'll figure out what readableMax
                //is once and just hold on to it.
                readableMax = Hammers.readableInt((int)max);
            }

            internal void clearCurr()
            { curr = 0; }

            internal virtual void incrementMax(double val)
            { max += val; }

            //Take an inventory and see how full it currently is.
            internal virtual void addInventoryToCurr(IMyInventory inventory)
            { curr += (double)inventory.CurrentVolume; }

            //Using curr and max, derive the remaining components needed to form a Report
            public void compute()
            {
                //First thing we need to do is apply the multiplier.
                curr = curr * multiplier;
                //Now for the percent. We'll need it for everything else. But things will get
                //weird if it's more than 100.
                percent = Math.Min(curr / max, 100) * 100;
                //Next, get the color code from our color handler. 
                code = colorHandler(percent);
                //Now, the meter.
                Hammers.drawMeter(ref meter, percent);
                //Last, we want to show curr as something you can actually read.
                readableCurr = Hammers.readableInt((int)curr);
            }
        }

        public class TallyItem : TallyCargo
        {
            MyItemType itemType;

            public TallyItem(string name, string typeID, string subTypeID, double max,
                bool isLow = true, double multiplier = 1) : base(name, isLow, multiplier)
            {
                itemType = new MyItemType(typeID, subTypeID);
                base.max = max;
            }

            //Take an inventory and see how much of the itemType is in it.
            internal override void addInventoryToCurr(IMyInventory inventory)
            { curr += (double)inventory.GetItemAmount(itemType); }

            //TallyItems have their maximum set when they're created. So when someone tries to
            //increase the maximum, we smile, nod, and ignore them.
            internal override void incrementMax(double val)
            { }
        }

        //Stores a block inventory and all of the tallies that inventory is to be reported to. Also
        //has a couple of small methods that aids CargoManager in calculating the curr and max of 
        //its tallies. 
        public class Container
        {
            IMyInventory inventory;
            List<TallyCargo> tallies;

            public Container(IMyInventory inventory, List<TallyCargo> tallyList)
            {
                this.inventory = inventory;
                tallies = tallyList;
            }

            public void calculateMax()
            {
                //For every tally associated with this Container...
                foreach (TallyCargo tally in tallies)
                {
                    //TallyItems have their maximums set manually. We only need to concern 
                    //ourselves will regular tallies.
                    if (!(tally is TallyItem))
                    //Add the container's maximum volume to the tally's max
                    { tally.incrementMax((double)inventory.MaxVolume); }
                }
            }

            public void calculateCurr()
            {
                //For every tally associated with this Container...
                foreach (TallyCargo tally in tallies)
                //Add the container's current volume to the tally's curr
                { tally.addInventoryToCurr(inventory); }
            }
        }

        //Initialization steps:
        //1. Use addTally and addTallyItem to set up the tallies.
        //2. Use addContainer to link inventories to the tallies they'll report to
        //3. Use calculateMax to perform the calculations that we'll only need to do once, and only
        //   once everything else is in place.
        public class CargoManager
        {
            List<Container> containers;
            //Stores the tallies tracked by this script by their ID.
            Dictionary<string, TallyCargo> tallies;

            public CargoManager()
            {
                containers = new List<Container>();
                tallies = new Dictionary<string, TallyCargo>();
            }

            //Get the reference to the tally with the specified name.
            //string name: The name of the desired tally
            //Returns: The Tally by the specified name, or null if that tally isn't in the dicitonary
            public TallyCargo getTallyByName(string name)
            {
                //We can no longer be sure that a tally is in the dictionary. We need to check
                //before trying to access things.
                if (tallies.ContainsKey(name))
                { return tallies[name]; }
                else
                { return null; }
            }

            //FAT: This and addTallyItem are still set up with the assumption that a tally being
            //passed in might already exist in the dictionary. With the switch to all tallies being
            //read from the PB as the first step, this is no longer the case. While it's possible
            //that someone could accidently enter the same tally twice from the PB config, it 
            //shouldn't be a regular occurance anymore, so these methods could be simplified.
            //Add a tally to the CargoManager. Has no effect if the tally already exists. Tallies
            //don't do anything until they're linked to Containers with the addContainer method.
            //string name: the ID that will be associated with this tally.
            //Returns: The Tally that has just been created, or the one that already had that name
            public TallyCargo addTally(string name)
            {
                //Only if this tally doesn't already exist in the dictionary...
                if (!tallies.ContainsKey(name))
                //... should we create a new Tally and add it to the dictionary with the name as 
                //the key
                { tallies.Add(name, new TallyCargo(name)); }
                //Now that we're sure we have a tally, hand its reference back to whoever called
                //this.
                return tallies[name];
            }

            //Add an TallyItem to the CargoManager. If a regular tally with this name already 
            //exists in the manager, it will be replaced. If an item tally with this name already
            //exists, no action will be taken.
            //string name: the ID that will be associated with this tally.
            //string typeID: The Type ID that will be plugged into MyObjectBuilder.
            //string subtypeID: The Subtype ID that will be plugged into MyObjectBuilder
            //double max: A value that will be used as the maximum for this tally. This will 
            //  probably be arbitrary, and will represent what the user thinks is a good quantity
            //  of the item in question.
            public TallyCargo addTallyItem(string name, string typeID, string subtypeID, double max)
            {
                //If this tally doesn't already exist in the dictionary...
                if (!tallies.ContainsKey(name))
                //... create a new TallyItem with this key in the dictionary.
                { tallies.Add(name, new TallyItem(name, typeID, subtypeID, max)); }
                //If the tally is in the dictionary, but it isn't a TallyItem...
                if (tallies.ContainsKey(name) && !(tallies[name] is TallyItem))
                //(Note: Pulling the tally from the dictionary in the second statement is fine. 
                //Because of short-circuiting, if the first statement evaluates to false, the second
                //will never be tried.)
                {
                    //Remove the existing tally
                    tallies.Remove(name);
                    //Replace it with a new TallyItem
                    tallies.Add(name, new TallyItem(name, typeID, subtypeID, max));
                }
                //Send back the Tally with this name.
                return tallies[name];
            }

            //The new version of addContainer, because I accidently did all the tally-getting 
            //outside the object and decided I kinda like it there. 
            public void addContainer(IMyInventory inventory, List<TallyCargo> tallyRefs)
            { containers.Add(new Container(inventory, tallyRefs)); }

            //Calculates the maximum volume of all Containers in the Cargo object. Must be called
            //after all containers have been added but before they're used.
            public void calculateMax()
            {
                //Add each Container's maximum to all of its tally's maximums.
                foreach (Container container in containers)
                { container.calculateMax(); }
                //Once we have all the maximums set, we'll use the tally list to finish setup on
                //each tally. Which is to say, we convert max into a readable int.
                foreach (KeyValuePair<string, TallyCargo> tally in tallies)
                { tally.Value.finishSetup(); }
            }

            //Calculates the current volume of all Containers in the Cargo object. 
            public void calculateCurr()
            {
                //Before we try to calculate a new current volume, use our tally list to clear the
                //old current volumes.
                foreach (KeyValuePair<string, TallyCargo> tally in tallies)
                { tally.Value.clearCurr(); }
                //Tell each Container to report to its associated tallies
                foreach (Container container in containers)
                { container.calculateCurr(); }
                //Once the tallies have been set for this tic, we need to go through the tally list
                //again and compute the components we'll need to write our reports.
                foreach (KeyValuePair<string, TallyCargo> tally in tallies)
                { tally.Value.compute(); }
            }

            //Get the number of tallies being tracked by this CargoManager. Mostly used to show off
            //at the end of grid evaluation.
            public int getTallyCount()
            { return tallies.Count; }
        }

        public class Report
        {
            //The surface we'll be drawing this report on.
            IMyTextSurface surface;
            //The tallies that this report will be pulling data from. We use an array because, at
            //this point, everything should be set.
            ITally[] tallies;
            //The points on the screen that sprites will be anchored to.
            Vector2[] anchors;
            //A float storing the font size used for displaying the Tallies in this report.
            public float fontSize { private get; set; }
            //A string storing the name of the font for displaying Tallies in this report.
            public string font { private get; set; }

            public Report(IMyTextSurface surface, List<ITally> tallies)
            {
                this.surface = surface;
                //We won't be adding or removing tallies at this point, so we'll just pull the 
                //array out of the list and work with it directly.
                this.tallies = tallies.ToArray();
                //For every tally that we have, we'll need to have a place on the surface to anchor
                //its sprite.
                anchors = new Vector2[tallies.Count];
                //We'll go ahead and figure out the anchors for the default 3 columns. If this needs
                //to change, we can call it with a different number from outside the constructor.
                setAnchors(3);
                //Set the default font info.
                fontSize = 1f;
                font = "Debug";
            }

            public void setAnchors(int columns)
            {
                //Malware's code for determining the viewport offset, which is the difference 
                //between an LCD's texture size and surface size. I have only the vaguest notions
                //of how it works.
                RectangleF viewport = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f,
                    surface.SurfaceSize);
                int rows = (int)(Math.Ceiling((double)tallies.Count() / columns));
                float columnWidth = surface.SurfaceSize.X / columns;
                float rowHeight = surface.SurfaceSize.Y / rows;
                int gridX = 0, gridY = 0;
                Vector2 anchor;
                for (int i = 0; i < tallies.Count(); i++)
                {
                    //To find the X value of this anchor, go to the middle of the current column.
                    //To find the Y value of this anchor, drop a quarter of the way down the 
                    //current row. Because Magic Numbers!
                    anchor = new Vector2(columnWidth / 2 + columnWidth * gridX,
                        /*rowHeight / 2*/ rowHeight / 4 + rowHeight * gridY);
                    //Before we add this anchor to our list, adjust it based on the viewport offset.
                    anchors[i] = anchor + viewport.Position;
                    //Move to the next column for the next tally
                    gridX++;
                    //If we've reached the last column, move down to the next row.
                    if (gridX >= columns)
                    {
                        gridX = 0;
                        gridY++;
                    }
                }
            }

            private string assembleElement(ITally tally)
            //I considered including a StringBuilder into this class to make this bit faster. But
            //apparently, if you do it all in one go, it's fast enough.
            { return $"{tally.name}\n{tally.readableCurr} / {tally.readableMax}\n{tally.getMeter()}"; }

            public void update()
            {
                ITally tally;
                MySprite sprite;
                using (MySpriteDrawFrame frame = surface.DrawFrame())
                {
                    for (int i = 0; i < tallies.Count(); i++)
                    {
                        tally = tallies[i];
                        //If this tally is actually a null, we don't have to do anything at all.
                        if (tally != null)
                        {
                            //Create a new TextSprite using information stored in this tally.
                            sprite = MySprite.CreateText(assembleElement(tally), font, tally.code,
                                fontSize);
                            //Use the anchor associate with this tally to position the sprite.
                            sprite.Position = anchors[i];
                            //Add the sprite to our frame.
                            frame.Add(sprite);
                        }
                    }
                }
            }
        }
        
        //Similar to a Report, Indicators refer to a group of lights that reflect the status of a
        //tally. It's just a lot simpler, because the only thing you can do with a light is change
        //the color.
        //... Or at least, that's all we're /going/ to do with it.
        public class Indicator
        {
            //The lighting blocks that make up this Indicator
            List<IMyLightingBlock> lights;
            //The Tally that tells this Indicator what to do
            ITally tally;
            //The last color code set by this indicator. Used to make sure we're only changing all 
            //the light colors when we need to.
            Color oldColor;

            public Indicator(ITally tally)
            {
                lights = new List<IMyLightingBlock>();
                this.tally = tally;
                oldColor = Hammers.cozy;
            }

            public void addLight(IMyLightingBlock light)
            { lights.Add(light); }

            public void update()
            {
                //If the tally's color code has changed...
                if (tally.code != oldColor)
                {
                    //Go to each light in lights and change its color.
                    foreach (IMyLightingBlock light in lights)
                    { light.Color = tally.code; }
                    //Update oldColor to match the color we just set everything to.
                    oldColor = tally.code;
                }
            }
        }

        //Volume, Item, Oxygen, Hydrogen, Power, Jump Charge, Raycast, Max Output (Solar/Wind),
        //HydrogenWithEngines?, ShieldIntegrity?
        public double handlePower(List<IMyTerminalBlock> blocks)
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
        public double handleRaycast(List<IMyTerminalBlock> blocks)
        {
            double curr = 0;
            foreach (IMyCameraBlock camera in blocks)
            { curr += camera.AvailableScanRange; }
            return curr;
        }

        //Counterintuitively, the 'MaxOutput' of things like Solar Panels and Wind Turbines is not
        //fixed. It actually describes the ammount of power that the block is currently receiving
        //in its current enviroments, ie, how much of a panel's surface area is facing the sun, or
        //what kind of weather is the turbine in. The variable you'd expect to describe those 
        //things, CurrentOutput, instead describes how much energy the grid is drawing from this
        //PowerProvider.
        public double handlePowerOutput(List<IMyTerminalBlock> blocks)
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
                //A subtitle displayed just below the log's title. Unlike the title, the subtitle
                //can be modified during script execution, and used to communicate additional 
                //information about what the script is doing
                public string subtitle;
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

                public EventLog(string title, int maxEntries = 5)
                {
                    log = new List<string>();
                    this.title = title;
                    subtitle = "";
                    this.maxEntries = maxEntries;
                    hasUpdate = false;
                }

                //Add a string the the event log.
                //string entry: The entry to be added to the log.
                public void add(string entry)
                {
                    log.Add($"{DateTime.Now.ToString("HH:mm:ss")}- {entry}");
                    if (log.Count > maxEntries)
                    { log.RemoveAt(0); }
                    //Flag the log as having been recently updated.
                    hasUpdate = true;
                }

                //Sets the 'updated' flag to false. Call after pulling the new log.
                public void clearUpdate()
                { hasUpdate = false; }

                //Get the logged events in a readable format
                public string toString()
                {
                    //Clear the old output
                    output.Clear();
                    //Start with the title
                    output.Append(title + "\n");
                    //If we've got something in the subtitle string...
                    if (!String.IsNullOrEmpty(subtitle))
                    //...tack it on
                    { output.Append(subtitle + "\n"); }
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

            /* The old, purely string-and-concatenation based getMeter
            //Creates an ASCII meter for a visual representation of percentages.
            //double percent: The percentage (Between 0-100) that will be displayed
            //int length: How many characters will be used to display the percentage, not counting 
            //  the bookend brackets. Defaults to 10
            public static string getMeter(double percent, int length = 10)
            {
                //A lot of my 'max' values are just educated guesses. Percentages greater than a 
                //hundred happen. And they really screw up the meters. So we're just going to 
                //pretend that everyone's staying within 100.
                percent = Math.Min(percent, 100);
                string line = "[";
                //How many bars do we need?
                int bars = (int)((percent / 100) * length);
                //To make the meter, we have the first loop filling in solid lines...
                for (int i = 0; i < bars; ++i)
                { line += "|"; }
                //... And another loop filling in blanks.
                for (int i = bars; i < length; ++i)
                { line += " "; }
                line += "]";
                return line;
            }*/

            //Replaces powers of ten with Ks or Ms.
            //int num: The number that should be rendered into a more readable form.
            public static string readableInt(int num)
            {
                string readable = "";
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

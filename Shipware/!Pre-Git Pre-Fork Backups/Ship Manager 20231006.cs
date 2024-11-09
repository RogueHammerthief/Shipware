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
    //ARCHIVE: Created after moving to the new EvaluateGrid, before removing evaluateOld and other
    //orphaned methods.
    //The errors are due to the partial implementation of Multi-ActionPlanBlock. If need be, the old
    //version is commented out just above the new implementation.

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
        //Arrays that store the containers and tallies that this script watches. 
        Container[] _containers;
        Tally[] _tallies;
        //The reports that tell about what various script elements are doing.
        IReportable[] _reports;
        //MFDs that do the same thing as reports, only fancier. MFDs are stored in a dictionary, to
        //facilitate controlling them by name.
        Dictionary<string, MFD> _MFDs;
        //A log that tells you what's going wrong isn't much good if it doesn't update when something
        //goes wrong. We'll keep track of our log WOTs here, so we can force them to display even
        //when the rest of the script isn't running
        List<WallOText> _logReports;
        //Like MFDs, ActionSets need to be stored in a Dictionary, so we can find them at a moment's
        //notice.
        Dictionary<string, ActionSet> _sets;
        //Triggers don't even have names. An array will suffice.
        Trigger[] _triggers;
        //A raycaster object is used to perform Raycasts and compile reports about them. We address 
        //them by name.
        Dictionary<string, Raycaster> _raycasters;
        //The indicators that change color based on what a tally is doing
        Indicator[] _indicators;
        //An EventLog that will... log events.
        EventLog _log;
        //An object that generates ASCII meters for us, used by tallies.
        MeterMaid _meterMaid;
        //Listens for Inter-Grid Communication
        IMyBroadcastListener _listener;
        //Used to read information out of a block's CustomData
        MyIni _iniReadWrite;
        //A second instance of MyIni, handy for moving data between two different configurations.
        MyIni _iniRead;
        //A custom object patterned off of MyIni, used for manipulating sections while preserving
        //their formatting.
        RawTextIni _iniRaw;
        //Used to parse arguments entered as commands
        MyCommandLine _argReader;
        //A StringBuilder that we will pass out to the various objects that need one.
        StringBuilder _sb;
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
        string _nonDeclarationPBConfig;
        

        /*
        //Isn't actually used for anything, this is just the color I've taken to applying to 
        //my lights, and I wanted it handy.
        public static Color cozy = new Color(255, 225, 200);
        //Goes with everything
        public static Color black = new Color(0,0,0);
        //Optimal
        public static Color green = new Color(25, 225, 100);
        //Normal
        public static Color lightBlue = new Color(100, 200, 225);
        //Caution
        public static Color yellow = new Color(255, 255, 0);
        //Warning
        public static Color orange = new Color(255, 150, 0);
        //Critical
        public static Color red = new Color(255, 0, 0); */
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
            /*textLog.addNote("First run complete.\n" +
                " -Use the AutoPopulate command to generate basic configuration.\n" +
                " -The Clone command can quickly distribute config across identical blocks.\n" +
                " -The Evaluate command scans the grid for config and loads it into memory.");*/
            _log.add(outcome);
            //We'll let the call at the end of Program() echo the log.
        }

        public void evaluateFull(LimitedMessageLog textLog, bool firstRun = false)
        {
            //We'll need these to move data between the various sub-evaluates
            Dictionary<string, IColorCoder> colorPalette = compileColors();
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
            int blockCount = -1;

            _debugDisplay.WriteText("Beginning evaluation\n");
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
                _debugDisplay.WriteText("Entering evaluateInit\n", true);
                evaluateInit(colorPalette, textLog, iniValue);
                _debugDisplay.WriteText("Entering evaluateDeclarations\n", true);
                evaluateDeclarations(Me, textLog, colorPalette, evalTallies, evalSets, evalTriggers, 
                    evalRaycasters, usedElementNames, parseResult, iniValue);

                _debugDisplay.WriteText("Deciding to evaluateGrid\n", true);
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
                    _debugDisplay.WriteText("Decision made to evaluateGrid\n", true);
                    blockCount = evaluateGrid(textLog, colorPalette, evalTallies, evalSets, evalTriggers, 
                        evalRaycasters, evalContainers, evalMFDs, evalReports, evalLogReports, 
                        evalIndicators,  parseResult, iniValue);
                }
            }

            _debugDisplay.WriteText("Config evaluation complete\n", true);
            //It's time to make some decisions about the config we've read, and to tell the user 
            //about it. The first decision has to do with the logReports
            if (_logReports == null || textLog.getErrorTotal() == 0 || evalLogReports.Count >= _logReports.Count)
            { _logReports = evalLogReports; }
            //The main decision is, 'How much do we trust all this config we just read'? 
            string outcome = "Evaluation complete.\n";
            //string outcome = "Evaluation complete.\n";
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
            //We don't log individual key generation if this is a firstRun scenario or if we're 
            //generating all the keys at once.
            bool logKeyGeneration = !hasInitSection;
            bool configAltered = false;

            //If the Init section is missing entirely, we'll make one note for the entire lot.
            if (!hasInitSection)
            {
                textLog.addNote($"{initTag} section was missing from block '{Me.CustomName}' and " +
                  $"has been re-generated.");
            }

            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue,
                lowGood, highGood, "SW.Init", "Optimal", "Green", logKeyGeneration, ref configAltered);
            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue, 
                lowGood, highGood, "SW.Init", "Normal", "LightBlue", logKeyGeneration, ref configAltered);
            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue, 
                lowGood, highGood, "SW.Init", "Caution", "Yellow", logKeyGeneration, ref configAltered);
            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue, 
                lowGood, highGood, "SW.Init", "Warning", "Orange", logKeyGeneration, ref configAltered);
            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue, 
                lowGood, highGood, "SW.Init", "Critical", "Red", logKeyGeneration, ref configAltered);
            //(20230426) This is for color-coding text in the PB's DetailInfo.
            //It also might not be working. When I tested it, the color seemed a lot like a blue even
            //though we should've been retrieving a red.
            //textLog.errorColor = color;

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
            _debugDisplay.WriteText("Initial Tally parse\n", true);
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
            _debugDisplay.WriteText("Initial ActionSet parse\n", true);
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
            _debugDisplay.WriteText("Initial Trigger parse\n", true);
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
            _debugDisplay.WriteText("Initial Raycaster parse\n", true);
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
            _debugDisplay.WriteText("Tally Raycast pass\n", true);
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
            _debugDisplay.WriteText("ActionSet script object pass\n", true);
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

            _debugDisplay.WriteText("evaluateDeclaration complete.\n", true);
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
            ActionSet action;
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
                        //From each ActionSet section, we read:
                        //ActionOn (Default: null): The action to be performed on this block when this 
                        //  ActionSet is set to 'on'.
                        //ActionOff (Default: null): The action to be performed on this block when this
                        //  ActionSet is set to 'off'.
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
                                action = evalSets[name];
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
                                    else
                                    //If there wasn't an Action0Property key, this may be an ActionPlanBlock
                                    {

                                        //Create a new block plan with this block as the subject
                                        ActionPlanBlock blockPlan = new ActionPlanBlock(block);
                                        //Check to see if there's an ActionOn in the ACTION SECTION
                                        blockPlan.actionOn = retrieveActionHandler(textLog, discreteTag,
                                            "ActionOn", block, _iniReadWrite, actions);
                                        //Check to see if there's an ActionOff in the ACTION SECTION
                                        blockPlan.actionOff = retrieveActionHandler(textLog, discreteTag,
                                            "ActionOff", block, _iniReadWrite, actions);
                                        //Pass the BlockPlan to the generic ActionPlan
                                        actionPlan = blockPlan;
                                    }
                                    //If we have successfully registered at least one action...
                                    if (actionPlan.hasAction())
                                    //Go ahead and add this ActionPlan to the ActionSet
                                    { action.addActionPlan(actionPlan); }
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
                                                if (!evalSets.TryGetValue(pair.Key, out action))
                                                { textLog.addWarning($"{troubleID} tried to reference the unconfigured ActionSet {pair.Key}."); }
                                                else
                                                {
                                                    ActionPlanMFD mfdPlan = new ActionPlanMFD(mfd);
                                                    if (pair.Value)
                                                    { mfdPlan.pageOn = name; }
                                                    else
                                                    { mfdPlan.pageOff = name; }
                                                    action.addActionPlan(mfdPlan);
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

        //This code courtesy of Stack Exchange users Ari Roth and aloisdq
        //https://stackoverflow.com/questions/2395438/convert-system-drawing-color-to-rgb-and-hex-value
        public static string argbToHex(int a, int r, int g, int b)
        { return $"{a:X2}{r:X2}{g:X2}{b:X2}"; }

        public static string colorToHex(Color color)
        { return argbToHex(color.A, color.R, color.G, color.B); }

        //Abbreviate large numbers with K, M, B, or T to keep them legible.
        //The name would lead you to believe this takes ints, but it now takes doubles.
        //The string it outputs is basically an integer, though.
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

        public int convertPageConfig()
        {
            //All the page-related keys that might be in the config, minus the ones relating to 
            //ActionPlanMFD. Because we aren't trying to win any effeciency rewards, we'll just 
            //try to transfer all of them, regardless of what kind of config we're actually dealing with.
            string[] pageKeys = { "Elements", "Title", "Columns", "DataType", "DataSource", "CharPerLine",
                "Script", "FontSize", "Font", "ForeColor", "BackColor" };
            string[] commonKeys = { "Tallies", "Inv0Tallies", "Inv1Tallies", "ActionSets", "Raycasters",
                "IndicatorElement" };
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            IMyTextSurfaceProvider surfaceProvider;

            //Everything is a surface provider now. That's nice, I guess, but it does mean that we 
            //have to perform one more check to find things that actually have surfaces on them.
            findBlocks<IMyTerminalBlock>(blocks, (b => b.IsSameConstructAs(Me) && b is IMyTextSurfaceProvider 
                && ((IMyTextSurfaceProvider)b).SurfaceCount > 0 && MyIni.HasSection(b.CustomData, _tag)));
            foreach (IMyTerminalBlock block in blocks)
            {
                surfaceProvider = block as IMyTextSurfaceProvider;
                _iniReadWrite.Clear();
                _iniRead.TryParse(block.CustomData);
                //Before we get in to transfering page config, we need to transfer over the other
                //keys in the common section.
                transferKeys(_tag, _tag, "", commonKeys);

                for (int surfaceIndex = 0; surfaceIndex < surfaceProvider.SurfaceCount; surfaceIndex++)
                {
                    string surfacePrefix = $"Surface{surfaceIndex}";
                    string pagesThisSurface = "";

                    //There are two paths: The path of the MFD, and the path of the singular page.
                    if (_iniRead.ContainsKey(_tag, $"{surfacePrefix}MFD"))
                    {
                        string mfdName = _iniRead.Get(_tag, $"{surfacePrefix}MFD").ToString();
                        string mfdOldSectionName = $"{_SCRIPT_PREFIX}.{mfdName}";
                        string newPageName = "";
                        string pagePrefix = "";
                        int pageindex = 0;

                        _iniReadWrite.Set(_tag, $"{surfacePrefix}MFD", mfdName);
                        while (pageindex != -1)
                        {
                            pagePrefix = $"Page{pageindex}";
                            if (_iniRead.ContainsKey(mfdOldSectionName, $"{pagePrefix}Elements") ||
                                _iniRead.ContainsKey(mfdOldSectionName, $"{pagePrefix}Script") ||
                                _iniRead.ContainsKey(mfdOldSectionName, $"{pagePrefix}DataType"))
                            {
                                newPageName = $"{surfaceIndex}Page{pageindex}";
                                pagesThisSurface += $"{newPageName}, ";
                                transferKeys(mfdOldSectionName, $"{_SCRIPT_PREFIX}.{newPageName}", pagePrefix, pageKeys);
                                transferActionPlanMFD(mfdOldSectionName, $"{_SCRIPT_PREFIX}.{newPageName}", pagePrefix);
                                //We've moved all the keys out of the old discrete section at this 
                                //point. We do still need to delete the section header.
                                _iniRead.DeleteSection(mfdOldSectionName);
                                pageindex++;
                            }
                            else
                            { pageindex = -1; }
                        }  //Go to next page
                        //We're going to have two excess characters. Chuck 'em.
                        pagesThisSurface = pagesThisSurface.Remove(pagesThisSurface.Length - 2);
                    }
                    //Singular pages are much simpler.
                    else
                    {
                        if (_iniRead.ContainsKey(_tag, $"{surfacePrefix}Elements") ||
                            _iniRead.ContainsKey(_tag, $"{surfacePrefix}Script") ||
                            _iniRead.ContainsKey(_tag, $"{surfacePrefix}DataType"))
                        {
                            pagesThisSurface += surfacePrefix;
                            transferKeys(_tag, $"{_SCRIPT_PREFIX}.{surfacePrefix}", surfacePrefix, pageKeys);
                        }
                    }
                    //Last step is to write however many keys we got to the Pages key for this surface.
                    //Assuming there were any at all.
                    if (pagesThisSurface != "")
                    { _iniReadWrite.Set(_tag, $"{surfacePrefix}Pages", pagesThisSurface); }
                }  //Go to next surface
                //We've built a new common section in iniReadWrite, but there's still one in iniRead.
                //Before we merge our configs, we need to toss that.
                _iniRead.DeleteSection(_tag);
                //Our config is in two places at the moment: Transfered pages are in _iniReadWrite,
                //while any config that was left on the block is still in _iniRead. Our last step
                //will be stiching those two together and committing the modified config back to
                //the block's CustomData.
                block.CustomData = _iniReadWrite.ToString() + "\n" + _iniRead.ToString();
            }  //Move on to the next block
            return blocks.Count;
        }

        public void transferKeys(string oldSectionName, string newSectionName, string oldPrefix, 
            string[] keys)
        {
            MyIniValue iniValue;
            MyIniKey oldKey;
            for (int i = 0; i < keys.Length; i++)
            { 
                oldKey = new MyIniKey(oldSectionName, $"{oldPrefix}{keys[i]}");
                iniValue = _iniRead.Get(oldKey);
                if (!iniValue.IsEmpty)
                {
                    _iniReadWrite.Set(newSectionName, keys[i], iniValue.ToString());
                    _iniRead.Delete(oldKey);
                }
            }
        }

        public void transferActionPlanMFD(string oldSectionName, string newSectionName, string oldPrefix)
        {
            MyIniValue iniValue;
            MyIniKey oldKey;

            oldKey = new MyIniKey(oldSectionName, $"{oldPrefix}LinkActionSetOn");
            iniValue = _iniRead.Get(oldKey);
            if (!iniValue.IsEmpty)
            {
                _iniReadWrite.Set(newSectionName, "ShowOnActionState", $"{iniValue.ToString()}: On");
                _iniRead.Delete(oldKey);
            }
            //Technically, a page could have both a LinkActionSetOn and Off, and both of those would
            //need to be written to the new ShowOnActionState state list. But I'm pretty sure I never
            //did that, and it'd be a pain to set up, so I'm leaving handling for that off.
            //If that situation does come up, the LinkActionSetOn value will be over-written.
            oldKey = new MyIniKey(oldSectionName, $"{oldPrefix}LinkActionSetOff");
            iniValue = _iniRead.Get(oldKey);
            if (!iniValue.IsEmpty)
            {
                _iniReadWrite.Set(newSectionName, "ShowOnActionState", $"{iniValue.ToString()}: Off");
                _iniRead.Delete(oldKey);
            }
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
                        if (_argReader.TryParse(data))
                        {
                            //The string that will store the result of this IGC interaction. Will be 
                            //sent to the local log and potentially as a reply on the channel specified
                            //by the sender.
                            string outcome = "No reply";
                            //Stores the channel the sender wants us to send a response on.
                            string replyChannel = "";
                            //If a switch is present...
                            if (_argReader.Switches.Count > 0)
                            //Make our replyChannel equal to the first switch we come to.
                            //This is really not how switches are meant to be used, but maybe it'll work
                            {
                                MyCommandLine.SwitchEnumerator switches = _argReader.Switches.GetEnumerator();
                                switches.MoveNext();
                                replyChannel = switches.Current;
                            }

                            //If the first word in the IGC message is 'reply'...
                            if (_argReader.Argument(0).ToLowerInvariant() == "reply")
                            {
                                //Replace 'reply' in the data with an empty string
                                data = data.Replace(_argReader.Argument(0), "");
                                //Trim the remaining string
                                data = data.Trim();
                                //Display the remainder of the data string as the reply message.
                                outcome = ($"Received IGC reply: {data}");
                            }
                            //If the first argument is 'action'
                            else if (_argReader.Argument(0).ToLowerInvariant() == "action")
                            {
                                //Did we get the correct number of arguments?
                                if (_argReader.ArgumentCount == 3)
                                {
                                    //Run tryTakeAction with the directed command and store the
                                    //result in outcome
                                    outcome = tryMatchAction(_argReader.Argument(1),
                                        _argReader.Argument(2).ToLowerInvariant(), "IGC-directed ");
                                }
                                //If we got an unexpected number of arguments
                                else
                                {
                                    outcome = $"Received IGC-directed command '{data}', which " +
                                        $"has an incorrect number of arguments.";
                                }

                            }
                            //If we got exactly one argument
                            else if (_argReader.ArgumentCount == 1)
                            //Run tryTakeAction with the 'switch' command and store the result in 
                            //outcome.
                            { outcome = tryMatchAction(_argReader.Argument(0), "switch", "IGC-directed "); }
                            //If we have no idea what's happening
                            else
                            {
                                outcome = $"Received the following unrecognized command from the IGC:" +
                                    $" '{data}'.";
                            }
                            //Add an entry to the local log
                            _log.add(outcome);
                            //If we're supposed to send a reply message...
                            if (!String.IsNullOrEmpty(replyChannel))
                            { IGC.SendBroadcastMessage(replyChannel, $"reply [{_tag}] {outcome}"); }
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
                if (_argReader.TryParse(argument))
                {
                    //The first argument of a run command will (hopefully) tell us what we need to 
                    //be doing.
                    string command = _argReader.Argument(0).ToLowerInvariant();
                    //A string that will hold error messages, if needed.
                    string trouble = "";
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
                            data = data.Replace(_argReader.Argument(1), "");
                            data = data.Trim();
                            IGC.SendBroadcastMessage(_argReader.Argument(1), data);
                            _log.add($"Sent the following IGC message on channel '{_argReader.Argument(1)}'" +
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
                            if (_argReader.ArgumentCount == 3)
                            {
                                string MFDTarget = _argReader.Argument(1);
                                string MFDPageCommand = _argReader.Argument(2);
                                if (_MFDs == null)
                                { _log.add($"Received MFD command, but script configuration isn't loaded."); }
                                //If we have MFDs, and we actually know what MFD the user is talking about...
                                else if (_MFDs.ContainsKey(MFDTarget))
                                {
                                    //If it's one of the easy commands...
                                    //Note: Performing toLowerInvariant in the checks is intentional.
                                    //PageCommand could also include the name of a specific page,
                                    //and the dictionary that page is stored in is case-sensitive.
                                    if (MFDPageCommand.ToLowerInvariant() == "next")
                                    { _MFDs[MFDTarget].flipPage(true); }
                                    else if (MFDPageCommand.ToLowerInvariant() == "prev")
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
                            if (_argReader.ArgumentCount == 3)
                            {
                                string outcome = tryMatchAction(_argReader.Argument(1),
                                    _argReader.Argument(2).ToLowerInvariant(), "");
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
                            if (_argReader.ArgumentCount == 2)
                            {
                                //Store what should be the Raycaster's name
                                string raycasterName = _argReader.Argument(1);
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
                            else if (!_argReader.Switch("force"))
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
                                Me.CustomData += writeDeclarations(_tallies.ToList(), _sets.Values.ToList(),
                                    _triggers.ToList(), _raycasters.Values.ToList());
                                _log.add($"Carried out Reconstitute command. PB config has been reverted " +
                                    $"to last known good.");
                                //TEMP: We may be dealing with old config. This will get rid of it.
                                if (_iniReadWrite.TryParse(Me.CustomData))
                                {
                                    _iniReadWrite.DeleteSection($"{_tag}Init");
                                    Me.CustomData = _iniReadWrite.ToString();
                                    _log.add($"Removed old Init secion. Remember, this does not need " +
                                        $"to be done in the release version of the script.");
                                }
                                else
                                {
                                    _log.add($"Failed to remove old init section because the PB config" +
                                        $"could not be parsed.");
                                }
                            }
                            break;
                        //Simply replace the CustomData on blocks in the Target group with the CustomData
                        //from the first block in the Template group
                        //Argument format: Clone
                        case "clone":
                            List<IMyTerminalBlock> cloneBlocks = new List<IMyTerminalBlock>();
                            trouble = "Clone command";
                            if (!tryGetBlocksByGroupName("Template", cloneBlocks, ref trouble))
                            //If there is no Template group on the grid, complain
                            { _log.add(trouble); }
                            else
                            {
                                IMyTerminalBlock cloneTemplate = cloneBlocks[0];
                                //Pretty sure I don't need to manually clear this, but I'm going to anyway.
                                cloneBlocks.Clear();
                                if (!tryGetBlocksByGroupName("Target", cloneBlocks, ref trouble))
                                //If there is no Target group on the grid, complain
                                { _log.add(trouble); }
                                else
                                {
                                    foreach (IMyTerminalBlock block in cloneBlocks)
                                    { block.CustomData = cloneTemplate.CustomData; }
                                    _log.add($"Carried out Clone command, replacing the CustomData " +
                                        $"of {cloneBlocks.Count} blocks in the Target group with " +
                                        $"the CustomData from block '{cloneTemplate.CustomName}'.");
                                }
                            }
                            break;
                        //Copies the configuration from a 'Populate' section on the PB to members of
                        //the 'Target' group on the grid
                        //Argument format: Populate (Flag)
                        //Argument example: Populate -add
                        case "populate":
                            MyIniParseResult parseResult;
                            //We should be clearing them after every use but, because we're going to
                            //be using them to generate config, we're going to make sure.
                            _iniRead.Clear();
                            _iniReadWrite.Clear();
                            //Try and parse the config on the PB. Most of it should be fine, but 
                            //this will catch any errors in the Populate section.
                            if (_iniRead.TryParse(Me.CustomData, out parseResult))
                            {
                                //Is this Populate targeting the PB?
                                if (_iniRead.Get("Populate", "CustomTag").ToString() == "Me")
                                {
                                    string popPBOutcome = "";
                                    //We'll need to figure out if there's already some config, and 
                                    //for that, we'll need a list of the keys in the init section.
                                    List<MyIniKey> keys = new List<MyIniKey>();
                                    _iniRead.GetKeys($"{_tag}Init", keys);
                                    //If we have some config already, but we don't have any data in memory...
                                    if (keys.Count != 0 && (_tallies == null || _tallies.Length == 0) &&
                                        (_sets == null || _sets.Count == 0))
                                    {
                                        popPBOutcome = "Received Populate command targetting the " +
                                            "programmable block, but the data needed to re-write " +
                                            "the config is missing. Please only use this command " +
                                            "after the script has successfully evaluated and is " +
                                            "running.";
                                    }
                                    else
                                    {
                                        string newConfig = /*populatePB(Me.CustomData, _iniRead, out popPBOutcome)*/ "";

                                        if (!String.IsNullOrEmpty(newConfig))
                                        {
                                            //If it worked, we just received a new config for the PB.
                                            Me.CustomData = newConfig;
                                            //Queue up an evaluate.
                                            Save();
                                            Runtime.UpdateFrequency = UpdateFrequency.Once;
                                        }
                                    }
                                    _log.add(popPBOutcome);
                                    _iniRead.Clear();
                                }
                                //Is there even a Populate section on the PB?
                                else if (_iniRead.ContainsSection("Populate"))
                                {
                                    //Next question: Is there a Target group on the grid?
                                    List<IMyTerminalBlock> popBlocks = new List<IMyTerminalBlock>();
                                    trouble = "Populate command";
                                    if (!tryGetBlocksByGroupName("Target", popBlocks, ref trouble))
                                    //If there is no Target group on the grid, complain
                                    { _log.add(trouble); }
                                    else
                                    {
                                        int keys = 0;
                                        trouble = "";
                                        keys = populate(popBlocks, _iniRead, _iniRaw, _sb,
                                            out trouble, _argReader.Switch("merge"));
                                        if (keys != -1)
                                        {

                                            _log.add($"Carried out Populate command, writing {keys} " +
                                                $"keys to each of the {popBlocks.Count} blocks in " +
                                                $"the Target group.");
                                        }
                                        //If the Populate method encountered trouble
                                        else
                                        {
                                            _log.add($"Attempted to carry out Populate command, but " +
                                                $"encountered the following error: {trouble}");
                                        }
                                    }
                                }
                                else
                                {
                                    //If there was no populate section, add one
                                    Me.CustomData = "[Populate]\n\n" + Me.CustomData;
                                    //Let the user know that the way is prepared.
                                    _log.add("Received Populate command, but there was no Populate " +
                                        "section on the Programmable Block. One has been added, and " +
                                        "configuration can be entered there.");
                                }
                            }
                            else
                            //If the Populate section (Or anything else on the PB) can't be read, 
                            //do the user a solid and let them know why.
                            {
                                _log.add($"Received Populate command, but the parser was unable to read " +
                                    $"information from the Programmable Block. Reason: {parseResult.Error}");
                            }
                            break;
                        //Places the config from the specified section (Or the default script config
                        //section if no argument is offered) of the first block in the Target group
                        //in an [Existing] section on the PB
                        //Argument format: LoadExisting <SectionName>
                        //Argument example: LoadExisting SW.HangarDoors
                        case "loadexisting":
                            string sectionName;
                            //If the user didn't specify a section
                            if (_argReader.ArgumentCount == 1)
                            { sectionName = _tag; }
                            //If the user did specify a section
                            else if (_argReader.ArgumentCount == 2)
                            { sectionName = _argReader.Argument(1); }
                            //If the user has done something bizarre
                            else
                            {
                                _log.add($"Received LoadExisting command with an incorrect number of arguments.");
                                break;
                            }
                            List<IMyTerminalBlock> existingBlocks = new List<IMyTerminalBlock>();
                            trouble = "LoadExisting command";
                            if (!tryGetBlocksByGroupName("Template", existingBlocks, ref trouble))
                            //If there is no Template group on the grid, complain
                            { _log.add(trouble); }
                            else
                            {
                                IMyTerminalBlock targetBlock = existingBlocks[0];
                                MyIniParseResult existingParseResult = new MyIniParseResult();
                                if (!_iniRaw.tryLoad(_iniReadWrite, out existingParseResult, targetBlock.CustomData))
                                {
                                    _log.add($"Received LoadExisting command, but config on block " +
                                        $"{targetBlock.CustomName} was unreadable for the following " +
                                        $"reason: {existingParseResult.Error}.");
                                    break;
                                }
                                else
                                {
                                    string readSection;
                                    if (!_iniRaw.tryRetrieveSectionContents(sectionName, out readSection))
                                    {
                                        _log.add($"Received LoadExisting command, but config on block " +
                                          $"{targetBlock.CustomName} did not contain the specified " +
                                          $"section {sectionName}.");
                                        break;
                                    }
                                    readSection = $";From block {targetBlock.CustomName}, section {sectionName}.\n" + readSection;
                                    //MONITOR: I'm just assuming that the PB has a readable config. That
                                    //might not be the case.
                                    if (!_iniRaw.tryLoad(_iniReadWrite, out existingParseResult, Me.CustomData))
                                    {
                                        _log.add($"Received LoadExisting command, but config on the " +
                                            $"Programmable Block was unreadable for the following " +
                                            $"reason: {existingParseResult.Error}.");
                                        break;
                                    }
                                    //Clear any existing Existing sections from the PB.
                                    _iniRaw.tryDeleteSection("Existing");
                                    _iniRaw.addSection("Existing", readSection, 0);
                                    Me.CustomData = _iniRaw.toString();
                                    _iniRaw.clear();
                                    _iniReadWrite.Clear();
                                    _log.add($"Carried out LoadExisting command, adding config from the " +
                                        $"{sectionName} section of block '{targetBlock.CustomName}' to " +
                                        $"the Existing section of the Programmable Block.");
                                }
                            }
                            break;
                        //Deletes the contents of CustomData for every block in the Target group
                        //Argument format: TacticalNuke (Flag)
                        //Argument example: TacticalNuke -confirm
                        case "tacticalnuke":
                            if (_argReader.Switch("force"))
                            {
                                List<IMyTerminalBlock> tacBlocks = new List<IMyTerminalBlock>();
                                trouble = "TacticalNuke command";
                                if (!tryGetBlocksByGroupName("Target", tacBlocks, ref trouble))
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
                                    "ALL CustomData from blocks in the Target group. If you are " +
                                    "certain you want to do this, run the command with the " +
                                    "-force switch.");
                            }
                            break;
                        //Prints a list of properties of every block type in the Target group to
                        //the log.
                        //Argument format: Properties
                        case "properties":
                            //Is there a Target group on the grid?
                            List<IMyTerminalBlock> propBlocks = new List<IMyTerminalBlock>();
                            trouble = "Properties command";
                            if (!tryGetBlocksByGroupName("Target", propBlocks, ref trouble))
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
                        case "definitions":
                            //Is there a Target group on the grid?
                            List<IMyTerminalBlock> defBlocks = new List<IMyTerminalBlock>();
                            trouble = "Definitions command";
                            if (!tryGetBlocksByGroupName("Target", defBlocks, ref trouble))
                            //If there is no Target group on the grid, complain
                            { _log.add(trouble); }
                            else
                            {
                                //Prepare for the -items flag
                                bool includeItems = _argReader.Switch("items");
                                List<MyInventoryItem> items = new List<MyInventoryItem>();
                                string[] defPath;
                                _sb.Clear();
                                _sb.Append($"Block Definitions for members of the Target group:\n");
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
                        case "scripts":
                            List<string> scripts = new List<string>();
                            Me.GetSurface(0).GetScripts(scripts);
                            _sb.Clear();
                            _sb.Append("Available scripts:\n");
                            foreach (string script in scripts)
                            { _sb.Append($"  {script}\n"); }
                            _log.add(_sb.ToString());
                            _sb.Clear();
                            break;
                        //Writes a template to the Populate section of the PB for a given 
                        //configuration type.
                        //Argument format: Template <TemplateType> <args>
                        //Argument example: Template Surface Report Script blank Report, which writes
                        //a template for the first, second, and fourth surfaces on a block.
                        /*case "template":
                            bool success;
                            string templateOutcome = writeTemplate(_argReader, _sb, out success);
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
                                _log.add($"Added {_argReader.Argument(1)} template to the Populate section of this " +
                                    "Programmable Block.");
                            }
                            //If writeTemplate wasn't successful, we have an error that needs to be
                            //logged.
                            else
                            {
                                _log.add(templateOutcome);
                            }
                            _sb.Clear();
                            break;*/
                        //Search the grid for block types compatible without tallies, and automatically
                        //write the configuration needed to make them work.
                        //Argument format: AutoPopulate
                        case "autopopulate":
                            List<IMyTerminalBlock> apBlocks = new List<IMyTerminalBlock>();
                            List<IMyTerminalBlock> filteredBlocks = new List<IMyTerminalBlock>();
                            int blockCounter = 0;
                            string initTag = $"{_tag}Init";
                            bool isTargeted = _argReader.Switch("target");
                            bool isInclude = _argReader.Switch("include");
                            bool hasExistingConfig;
                            //First order of business is to figure out what kind of blocks we're 
                            //going to be looking at.
                            //If the -target flag is set, we only consider the Target group.
                            if (isTargeted)
                            {
                                //NOTE 20230117: Not converting this to use tryGetBlocksByGroupName
                                //because of the filtering done later on in the else branch by working.
                                //directly with IMyBlockGroup
                                IMyBlockGroup targetGroup = GridTerminalSystem.GetBlockGroupWithName("Target");
                                //One important question: Is there actually a Target group?
                                if (targetGroup == null)
                                {
                                    _log.add("Received AutoPopulate command with the -target flag set, " +
                                        "but there is no Target block group on the grid.");
                                    break;
                                }
                                else
                                {
                                    targetGroup.GetBlocks(apBlocks, b => b.IsSameConstructAs(Me) && b != Me);
                                    //We always include the PB, whether the user did or not.
                                    apBlocks.Add(Me);
                                }
                            }
                            //If the command isn't targeted, look for any blocks
                            else
                            { findBlocks<IMyTerminalBlock>(apBlocks, b => b.IsSameConstructAs(Me)); }
                            //Now that we have a set of blocks, we need to see if any of them have config.
                            filterBlocks(apBlocks, filteredBlocks, b => MyIni.HasSection(b.CustomData, _tag));
                            //We also need to check for the presence of Init config on the PB
                            if (MyIni.HasSection(Me.CustomData, initTag))
                            { filteredBlocks.Add(Me); }
                            hasExistingConfig = filteredBlocks.Count != 0;
                            //There are three situations in which AP will run: If there's no existing 
                            //config on the grid, if the -confirm switch is set, or if the -include
                            //switch is set.
                            if (!hasExistingConfig || _argReader.Switch("confirm") || isInclude)
                            {
                                List<Tally> tallyList = _tallies.ToList();
                                string outcome = autoPopulate(apBlocks, tallyList, _iniReadWrite, isInclude);
                                int apInstructions = Runtime.CurrentInstructionCount;
                                //TODO: Needs to be adapted to work with the new Reconstitute.
                                /*Me.CustomData = reconstitute(Me.CustomData, tallyList,
                                    _sets.Values.ToList(), _triggers.ToList());*/
                                int reconInstructions = Runtime.CurrentInstructionCount - apInstructions;
                                //log.add(outcome);
                                _log.add($"{outcome}\nAutoPopulate required {apInstructions} / " +
                                    $"{Runtime.MaxInstructionCount} ({(int)(((double)apInstructions) / Runtime.MaxInstructionCount * 100)}%) " +
                                    $"instructions, with Reconstitute requiring an additional {reconInstructions} / " +
                                    $"{Runtime.MaxInstructionCount} ({(int)(((double)reconInstructions) / Runtime.MaxInstructionCount * 100)}%).");

                                //Queue up an evaluate
                                Save();
                                Runtime.UpdateFrequency = UpdateFrequency.Once;
                            }
                            //If there's already Shipware configuration on the grid, complain. And
                            //point the user at a way to fix it.
                            else
                            {
                                //We may have a bunch of these. Time to bust out the StringBuilder.
                                _sb.Clear();
                                _sb.Append("Received AutoPopulate command, but there is already Shipware " +
                                    "configuration on the grid. The -include switch will preserve " +
                                    "existing tallies and try to match them to compatible blocks. The " +
                                    "-confirm switch will re-write tally initiators on the PB, which " +
                                    "can cause grid config to be orphaned.\n" +
                                    "The following subject blocks have existing Shipware config:\n");
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
                                _log.add(_sb.ToString());
                                _sb.Clear();
                            }
                            break;
                        //Clears Shipware sections and their contents from the members of the 
                        //'Target' group on the grid
                        //Argument format: Clear
                        case "clear":
                            List<IMyTerminalBlock> clearBlocks = new List<IMyTerminalBlock>();
                            trouble = "Clear command";
                            if (!tryGetBlocksByGroupName("Target", clearBlocks, ref trouble))
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
                            //Did the user include a new ID? And nothing else?
                            if (_argReader.ArgumentCount == 2)
                            {
                                //Put a handle on the ID the user wants to use
                                string newID = _argReader.Argument(1);
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
                                Me.CustomData = Me.CustomData.Replace($"[{_tag}Init]", $"[{newTag}Init]");
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
                            else if (_argReader.ArgumentCount > 2)
                            {
                                _log.add($"Received ChangeID command with too many arguments. New IDs " +
                                    $"cannot contain spaces.");
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
                        //when the script is partially compiled, which could be extremely helpful or
                        //terrible, depending on the circumstances.
                        case "update":
                            compute();
                            //The -force switch uses forceUpdate instead of regular update
                            if (_argReader.Switch("force"))
                            {
                                foreach (IReportable report in _reports)
                                { report.forceUpdate(); }
                                foreach (Indicator indicator in _indicators)
                                { indicator.update(); }
                            }
                            else
                            { update(); }
                            //The -performance switch logs the performance impact of this update
                            if (_argReader.Switch("performance"))
                            {
                                _log.add($"Update used {Runtime.CurrentInstructionCount} / {Runtime.MaxInstructionCount} " +
                                    $"({(int)(((double)Runtime.CurrentInstructionCount / Runtime.MaxInstructionCount) * 100)}%) " +
                                    $"of instructions allowed in this tic.\n");
                            }
                            break;
                        case "convert":
                            int blocksAdressed = convertPageConfig();
                            _log.add($"Carried out Convert command, altering the configuration of " +
                                $"{blocksAdressed} surface providers.\n");
                            break;
                        //Test function. What exactly it does changes from day to day.
                        case "test":
                            evaluate();
                            //_log.add("Test function executed.");
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
            { outcome = $"Carried out {source}command '{command}' for ActionSet '{targetSet.programName}'."; }
            return outcome;
        }
        /* TODO: Phase out redneckDeleteSection and replace it with RawTextIni
        //Writes configuration for Tallies, ActionSets, and Triggers, and plugs that config into the
        //provided string.
        public string reconstitute(string pbConfig, List<Tally> reconTallies, List<ActionSet> reconActions,
            List<Trigger> reconTriggers)
        {
            MyIniParseResult parseResult = new MyIniParseResult();
            string initTag = $"{tag}Init";
            iniReadWrite.Clear();
            iniRaw.clear();

            if (iniRaw.tryLoad(iniReadWrite, out parseResult, pbConfig))
            {
                int reconCounter = 0;
                iniRaw.tryDeleteSection(initTag);
                _sb.Clear();
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
            }
            else
            {
                log.add($"Received LoadExisting command, but config on the " +
                    $"Programmable Block was unreadable for the following " +
                    $"reason: {existingParseResult.Error}.");
            }

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
        }*/

        //Writes all the linked declarations to a single string.
        public string writeDeclarations(List<Tally> tallies, List<ActionSet> actions, List<Trigger> triggers, 
            List<Raycaster> raycasters)
        {
            string declarations;
            int counter = 0;
            _sb.Clear();

            foreach (Tally tally in tallies)
            { _sb.Append(tally.writeConfig(counter++)); }
            _sb.Append("\n");
            counter = 0;

            foreach (ActionSet action in actions)
            { _sb.Append(action.writeConfig(counter++)); }
            _sb.Append("\n");
            counter = 0;

            foreach (Trigger trigger in triggers)
            { _sb.Append(trigger.writeConfig(counter++)); }
            _sb.Append("\n");
            counter = 0;

            foreach (Raycaster raycaster in raycasters)
            { _sb.Append(raycaster.writeConfig(counter++)); }
            _sb.Append("\n");
            counter = 0;

            declarations = _sb.ToString();
            _sb.Clear();
            return declarations;
        }

        //Writes configuration for Tallies, ActionSets, and Triggers, and plugs that config into the
        //provided string.
        public string reconstituteOLD(string pbConfig, List<Tally> reconTallies, List<ActionSet> reconActions,
            List<Trigger> reconTriggers)
        {
            string initTag = $"{_tag}Init";
            List<string> reconSections = new List<string>();
            int reconTargetSection = redneckDeleteSection(pbConfig, initTag, ref reconSections);
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
                _sb.Append($"{action.writeConfig(reconCounter)}\n");
                reconCounter++;
            }
            reconCounter = 0;
            foreach (Trigger trigger in reconTriggers)
            {
                _sb.Append($"{trigger.writeConfig(reconCounter)}\n");
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

        /* A method used to write configuration data from a section on the PB to blocks in a 'Target'
         * group on the grid. Can also merge-in non-duplicate keys from a 'Existing' section
         *  -blocks: A list of blocks, containing the members of the block group 'Target'
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
            string populateTag = popReference.Get("Populate", "CustomTag").ToString(_tag);
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

            //Now that we have our new section, we need to go to each member of the Target group,
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
            //Target group. Our last steps are to clear out the variables we used...
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
        /* 
        public string populatePB(string pbConfig, MyIni configReader, out string outcome)
        {
            //The name of the section that holds our initiators.
            string initTag = $"{_tag}Init";
            LimitedMessageLog textLog = new LimitedMessageLog(_sb, 25);
            MyIniValue iniValue;
            string type = "";
            int ID = -1;
            string initiatorName = "<Name not found>";
            string newConfig = "";
            Dictionary<string, IColorCoder> colorPalette = compileColors();
            IColorCoder colorCoder = colorPalette["cozy"];
            Color color = colorCoder.getColorCode(-1);
            Tally newTally = null;
            ActionSet newAction = null;
            Trigger newTrigger = null;
            //We'll use these variables to pass information into Reconstitute. If we get that far.
            //For the time being, we'll point them at the globals.
            List<Tally> tallyList = _tallies.ToList();
            List<ActionSet> setList = _sets.Values.ToList();
            List<Trigger> triggerList = _triggers.ToList();
            //Our life will be much easier if we rebuild the Tally dictionary, so that's what we'll do.
            Dictionary<string, Tally> tallyDic = new Dictionary<string, Tally>();
            foreach (Tally tally in _tallies)
            { tallyDic.Add(tally.programName, tally); }
            //Triggers have names now! We need to know their names as well.
            Dictionary<string, Trigger> triggerDic = new Dictionary<string, Trigger>();
            foreach (Trigger trigger in _triggers)
            { triggerDic.Add(trigger.programName, trigger); }

            //There's a few pieces of data we need to have. Did we get them?
            iniValue = configReader.Get("Populate", "Type");
            if (iniValue.IsEmpty)
            { textLog.addError("Type key not found."); }
            else
            { type = iniValue.ToString(); }
            iniValue = configReader.Get("Populate", "ID");
            if (iniValue.IsEmpty)
            { textLog.addError("ID key not found."); }
            else
            { ID = iniValue.ToInt32(); }

            if (type == "Tally")
            {
                newTally = tryGetTallyFromConfig(configReader, "Populate", ID, colorPalette,  
                    ref color, colorCoder, iniValue, textLog);
                initiatorName = newTally?.programName ?? initiatorName;
                //We need to make some checks before we move on. Like, did we even just read a tally?
                if (newTally == null)
                {
                    textLog.addError($"Tally configuration not found. Make sure the numbers in the " +
                        $"configuration keys match the ID at the top of the Populate section.");
                }
                //Leave our blank alone.
                else if (initiatorName.ToLowerInvariant() == "blank")
                {
                    textLog.addError($"The Tally name '{initiatorName}' is reserved by the script to indicate" +
                            $"where portions of the screen should be left empty. Please choose a " +
                            $"different name.");
                }
                //We need to make a check for duplicates. Unless of course the duplicate is right at
                //the index we've been given, in which case we're fine.
                else if (!(ID < _tallies.Count() && initiatorName == _tallies[ID].programName))
                {
                    //Check for duplicates in the Tally and Action dictionaries.
                    if (tallyDic.ContainsKey(initiatorName))
                    { textLog.addError($"The Tally name '{initiatorName}' is already in use by another tally."); }
                    if (_sets.ContainsKey(initiatorName))
                    { textLog.addError($"The Tally name '{initiatorName}' is already in use by an action."); }
                    if (triggerDic.ContainsKey(initiatorName))
                    { textLog.addError($"The Tally name '{initiatorName}' is already in use by a trigger."); }
                }

                //If we still don't have any errors, prepare our data to be shipped off to the
                //Reconstitute method. That means adding (Or replacing) our new tally into the
                //Tally list, and just pointing at sets.
                if (textLog.getErrorTotal() == 0)
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
                //UPDATE 20220203: That was a terrible idea, and now we pass it null.
                //TODO: Bandaid fix here. If I end up keeping this method, I'll need to change this
                //so I'm not generating a new set of variables just to plug into this method
                newAction = tryGetActionSetFromConfig(configReader, null, "Populate", ID, iniValue,
                    textLog, compileColors(), new ColorCoderMono());
                initiatorName = newAction?.programName ?? initiatorName;
                //We need to make some checks before we move on. Like, did we even just read an action?
                if (newAction == null)
                {
                    textLog.addError($"Action configuration not found. Make sure the numbers in the " +
                        $"configuration keys match the ID at the top of the Populate section.");
                }
                //Leave our blank alone.
                else if (initiatorName.ToLowerInvariant() == "blank")
                {
                    textLog.addError($"The initiator name '{initiatorName}' is reserved by the script to " +
                        $"indicate where portions of the screen should be left empty. Please choose " +
                        $"a different name.");
                }
                //We need to make a check for duplicates. Unless of course the duplicate is right at
                //the index we've been given, in which case we're fine. 
                else if (!(ID < setList.Count() && initiatorName == setList[ID].programName))
                {
                    if (tallyDic.ContainsKey(initiatorName))
                    { textLog.addError($"The Action name '{initiatorName}' is already in use by a Tally."); }
                    if (_sets.ContainsKey(initiatorName))
                    { textLog.addError($"The Action name '{initiatorName}' is already in use by another Action."); }
                    if (triggerDic.ContainsKey(initiatorName))
                    { textLog.addError($"The Action name '{initiatorName}' is already in use by a trigger."); }
                }

                //If we still don't have any errors, prepare our data to be shipped off to the
                //Reconstitute method. That means adding (Or replacing) our new action into the
                //action list, and making a copy of the tally array as a list.
                if (textLog.getErrorTotal() == 0)
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
                tryGetTriggerFromConfig(configReader, null, tallyDic, _sets, "Populate", ID, iniValue,
                    ref newTally, ref newAction, ref newTrigger, textLog);
                initiatorName = newTrigger?.programName ?? initiatorName;
                if (newTrigger == null)
                {
                    textLog.addError($"Trigger configuration not found. Make sure the numbers in the " +
                        $"configuration keys match the ID at the top of the Populate section.");
                }
                else if (initiatorName.ToLowerInvariant() == "blank")
                {
                    textLog.addError($"The initiator name '{initiatorName}' is reserved by the script to " +
                        $"indicate where portions of the screen should be left empty. Please choose " +
                        $"a different name.");
                }
                //We need to make a check for duplicates. Unless of course the duplicate is right at
                //the index we've been given, in which case we're fine. 
                else if (!(ID < triggerList.Count() && initiatorName == triggerList[ID].programName))
                {
                    if (tallyDic.ContainsKey(initiatorName))
                    { textLog.addError($"The Trigger name '{initiatorName}' is already in use by a Tally."); }
                    if (_sets.ContainsKey(initiatorName))
                    { textLog.addError($"The Trigger name '{initiatorName}' is already in use by an Action."); }
                    if (triggerDic.ContainsKey(initiatorName))
                    { textLog.addError($"The Trigger name '{initiatorName}' is already in use by another trigger."); }
                }
                if (textLog.getErrorTotal() == 0)
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
            { textLog.addError("Initiator type must be either 'Tally', 'Action', or 'Trigger'."); }

            //If one of the type paths executed correctly, they should've already returned. If
            //we reach this point, something has gone wrong.
            outcome = $"Received Populate command, but the contents of the Populate section failed " +
                $"evaluation. Reason(s):\n{textLog.errorsToString()}";
            return newConfig;
        }*/
        /*
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
        */

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
        /*
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
                ";  colon). They will only be sent to the members of the Target group \n" +
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
                        ";  The name of the MFD whose pages this discrete section will\n" +
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
                                    "\n;  The names of the Elements (Tallies, ActionSets, or Triggers) whose\n" +
                                    ";  status will be displayed on this Surface. The word 'blank' can be used\n" +
                                    ";  to indicate that a hole should be left before the next element.\n");
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
                                    ";  TSS_Weather, TSS_Jukebox, TSS_TargetingInfo\n");
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
                                    ";  DataTypes are: Log, CustomData, DetailInfo, CustomInfo, and Raycaster.\n");
                                _sb.Append($"{prefix}{surface}DataType = < DataType Name >\n");
                                _sb.Append(dataFlag ? "" :
                                    "\n;  For the CustomData, DetailInfo, CustomInfo, and Raycaster DataTypes,\n" +
                                    ";  a DataSource must be specified. This will be the name of a block on the\n" +
                                    ";  grid for CustomData, DetailInfo, and CustomInfo types, and the name of\n" +
                                    ";  a Raycaster for the Raycaster type.\n");
                                _sb.Append($";{prefix}{surface}DataSource = < DataSource Name >\n");
                                dataFlag = true;
                                _sb.Append(fontFlag ? "" : fontSizeComment);
                                _sb.Append(fontSizeKey);
                                _sb.Append(fontFlag ? "" : fontComment);
                                _sb.Append(fontKey);
                                fontFlag = true;
                                _sb.Append(charCountFlag ? "" :
                                    "\n;  A rudimentary text wrap can be applied to these reports. It isn't\n" +
                                    ";  particularly efficient, so it cannot be used with the DetailInfo or" +
                                    ";  CustomInfo DataTypes (Due to how frequently those update).\n" +
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
                    ";  The Tally, ActionSet, or Trigger whose state will be used to define\n" +
                    ";  the color of this light.\n");
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
                    ";  The name of the ActionSet this discrete section will configure\n" +
                    ";  actions for. It will need to have the 'SW.' prefix, as in\n" +
                    ";  'SW.Raycaster'.\n");
                _sb.Append(
                    "CustomTag = < Name of target ActionSet, prefixed with 'SW.' >\n\n");
                _sb.Append(
                    ";  The action this block will perform when this ActionSet is set\n" +
                    ";  to 'on'. Common actions are EnableOn, BatteryRecharge,\n" +
                    ";  TankStockpileOn, TimerStart, and TurretDefensive.\n" +
                    ";  A full list can be found in the Action Listing section at:\n" +
                    ";  https://steamcommunity.com/sharedfiles/filedetails/?id=2776664161 \n");
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
                    ";  The name of the ActionSet this discrete section will configure\n" +
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
                    _iniRead.Clear();
                    _iniRead.TryParse(Me.CustomData);
                    ID = findOpening($"{_tag}Init", "Tally", _iniRead);
                    _iniRead.Clear();
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
                    ";  Battery, Gas, JumpDrive, Raycast, and PowerProducer.\n");
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
                    _iniRead.Clear();
                    _iniRead.TryParse(Me.CustomData);
                    ID = findOpening($"{_tag}Init", "Action", _iniRead);
                    _iniRead.Clear();
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
                    ";  The number of tics the script will wait before polling blocks for\n" +
                    ";  new data when this ActionSet is 'on'. This affects all aspects of\n" +
                    ";  the script's operation, and is intended as a way to reduce the amount\n" +
                    ";  of runtime your CPU is spending handling this script on idle grids.\n");
                _sb.Append(
                    $";Action{ID}DelayOn = < Integer value, default 0 >\n\n");
                _sb.Append(
                    ";  The number of tics to wait before polling blocks for new data\n" +
                    ";  when this ActionSet is 'off'.\n");
                _sb.Append(
                    $";Action{ID}DelayOff = < Integer value, default 0 >\n\n");
                _sb.Append(
                    ";  The 'channel' that IGC messages will be sent on when this ActionSet\n" +
                    ";  changes states. For other instances of the Shipware script, the channel\n" +
                    ";  is the Script Tag, which is visible at the top of the log if it's been\n" +
                    ";  changed from the default of 'SW.Shipware'.\n");
                _sb.Append(
                    $";Action{ID}IGCChannel = < Name of channel, default empty >\n\n");
                _sb.Append(
                    ";  The message that will be sent on the above channel when this\n" +
                    ";  ActionSet is switched 'on'. For invoking an action on another instance\n" +
                    ";  of the Shipware script, the format is:\n" +
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
                _sb.Append(
                    ";  A list of ActionSets and the states that they should be switched to\n" +
                    ";  when this ActionSet is switched 'on'. This state list has the following\n" +
                    ";  format: 'Reactors: On, HyGens: On, Batteries: Off'\n");
                _sb.Append(
                    $";Action{ID}LinkActionSetOn = < Comma-separated state list, default empty >\n\n");
                _sb.Append(
                    ";  A list of ActionSets and the states that they should be switched to\n" +
                    ";  when this ActionSet is switched 'off'.\n");
                _sb.Append(
                    $";Action{ID}LinkActionSetOff = < Comma-separated state list, default empty >\n\n");
                _sb.Append(
                    ";  A list of Triggers and whether they should be armed or disarmed\n" +
                    ";  when this ActionSet is switched 'on'. Uses the same state list format\n" +
                    ";  as LinkActionSet'.\n");
                _sb.Append(
                    $";Action{ID}LinkTriggerOn = < Comma-separated state list, default empty >\n\n");
                _sb.Append(
                    ";  A list of Triggers and whether they should be armed or disarmed\n" +
                    ";  when this ActionSet is switched 'off'.\n");
                _sb.Append(
                    $";Action{ID}LinkTriggerOff = < Comma-separated state list, default empty >\n\n");
                isSuccessful = true;
            }
            //Format: Template Trigger #
            else if (type == "trigger")
            {
                int ID;
                //If the user didn't deign to provide us with an ID...
                if (!Int32.TryParse(argReader.Argument(2), out ID))
                {
                    _iniRead.Clear();
                    _iniRead.TryParse(Me.CustomData);
                    ID = findOpeningTrigger($"{_tag}Init", _iniRead);
                    _iniRead.Clear();
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
                    ";  The name this Trigger will use in configuration.\n");
                _sb.Append(
                    $"Trigger{ID}Name = < Name of Trigger >\n\n");
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
        }*/

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

        public string autoPopulate(List<IMyTerminalBlock> gridBlocks, List<Tally> tallyList,
            MyIni configWriter, bool includeExistingItemTallies = false)
        {
            /*debug.WriteText("Entered AP method\n");*/
            const string ORE_TYPE = "MyObjectBuilder_Ore";
            const string INGOT_TYPE = "MyObjectBuilder_Ingot";
            //I may need this one day. May as well leave it here.
            //const string COMP_TYPE = "MyObjectBuilder_Component";
            const string AMMO_TYPE = "MyObjectBuilder_AmmoMagazine";
            //Tallies need ColorCoders now, but the objects created by AutoPopulate don't actually
            //need to do anything (Because they'll immediately be over-written by a proper evaluate).
            //So we'll make a couple of cardboard-cutouts
            ColorCoderHigh highGood = new ColorCoderHigh();
            ColorCoderLow lowGood = new ColorCoderLow();
            //Stores the number of tallies created
            int tallyCounter = 0;
            //Stores the number of blocks this method has altered. 
            int blockCounter = 0;
            //Stores the number of blocks where we've replaced configuration.
            int replacementCounter = 0;
            //When we need a new tally, we'll use this variable
            Tally newTally;
            //We'll use this list to store the tally names that we want to add to the generic report
            //generated at the end of AutoPopulate.
            List<string> reportElements = new List<string>();
            //There are two approaches we use to generate tally config, depending on if the tally in 
            //question is inventory-based or not. We'll keep a list of item and cargo based tallies
            //so we'll know which is which
            List<TallyCargo> cargoTallies = new List<TallyCargo>();
            //When we filter blocks out of the gridBlocks list, we'll put them in this list
            List<IMyTerminalBlock> filteredBlocks = new List<IMyTerminalBlock>();
            /*debug.WriteText("Objects initiated\n", true);*/

            //If we're including existing data, we'll want to be able to access it quickly.
            Dictionary<string, Tally> tallyDic = new Dictionary<string, Tally>();
            /*debug.WriteText("Populating lists\n", true);*/
            if (includeExistingItemTallies)
            {
                foreach (Tally tally in tallyList)
                {
                    tallyDic.Add(tally.programName, tally);
                    if (tally is TallyCargo)
                    {
                        cargoTallies.Add((TallyCargo)tally);
                        /*debug.WriteText($"  Including tally {tally.programName}\n", true);*/
                    }
                }
            }
            else
            //If we aren't including the old config, chuck the config we were handed.
            { tallyList.Clear(); }
            /*debug.WriteText("Tally dictionary populated\n", true);*/
            configWriter.Clear();

            //The first step will be to figure out how many of the AP tally types we'll need. We do 
            //this by looking at the blocks that are on the grid. AP Tallies are:
            //Power, Hydrogen, Oxygen, Cargo, Ice, Stone, Ore, Uranium, Solar, JumpDrives,
            //Gatling, Autocannon, Assault, Artillery, RailSmall, RailLarge, Rocket
            //(Also ShieldHealth, ShieldHeat, and ShieldRegen if I ever put Defense Shields
            //integration back in)
            //Batteries
            filterBlocks(gridBlocks, filteredBlocks, b => b is IMyBatteryBlock); //Really, what are the odds of not having a battery?
            //NOTE: Unless told otherwise, the last thing configureTally will do is clear the filteredBlock
            //list. So it'll be ready to go for the next set of blocks.
            configureTallyGeneric("Power", new BatteryHandler(), filteredBlocks, tallyDic, tallyList, highGood, 
                configWriter, reportElements, ref tallyCounter, ref replacementCounter, ref blockCounter);
            //Since Digi pointed me at the ResourceSourceComponent, I no longer need to hard-code 
            //guesstimates for solar panel max values. That means we can stuff all our solar panels
            //into the same group.
            filterBlocks(gridBlocks, filteredBlocks, b => b is IMySolarPanel);
            //PowerMax can now calculate its own maximums, we no longer need to dictate that.
            newTally = configureTallyGeneric("Solar", new PowerMaxHandler(), filteredBlocks, tallyDic, tallyList, highGood, 
                configWriter, reportElements, ref tallyCounter, ref replacementCounter, ref blockCounter);
            //However, PowerProducers use a megawatt scale, which isn't particularly useful for figuring
            //out how much solar power we're making. So we'll set the multiplier.
            if (newTally != null)
            { newTally.multiplier = 1000; }
            //We could also do JumpDrives? Let's do that.
            filterBlocks(gridBlocks, filteredBlocks, b => b is IMyJumpDrive);
            configureTallyGeneric("JumpCharge", new JumpDriveHandler(), filteredBlocks, tallyDic, tallyList, highGood,
                configWriter, reportElements, ref tallyCounter, ref replacementCounter, ref blockCounter);
            //Hydrogen and Oxygen tanks
            filterBlocks(gridBlocks, filteredBlocks, b => b is IMyGasTank);
            //Tanks hold either hydrogen or oxygen. Instead of going through the list twice, we'll
            //go ahead and allocate a couple of new lists so we can sort things properly in a single 
            //pass
            List<IMyTerminalBlock> hyTanks = new List<IMyTerminalBlock>();
            List<IMyTerminalBlock> oxyTanks = new List<IMyTerminalBlock>();
            //Code for sorting tanks based on their resource sink courtesy of Digi and Frigidman
            MyDefinitionId oxygenID = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Oxygen");
            MyDefinitionId hydrogenID = MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Hydrogen");
            //Next, we sift our tanks
            foreach (IMyGasTank tank in filteredBlocks)
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
            filterBlocks(gridBlocks, hyTanks, b => b is IMyPowerProducer &&
                b.BlockDefinition.SubtypeId.EndsWith("HydrogenEngine"));
            //We should have everything we need for the hydrogen and oxygen tallies.
            configureTallyGeneric("Hydrogen", new GasHandler(), hyTanks, tallyDic, tallyList, highGood, configWriter,
                reportElements, ref tallyCounter, ref replacementCounter, ref blockCounter, false);
            configureTallyGeneric("Oxygen", new GasHandler(), oxyTanks, tallyDic, tallyList, highGood, configWriter,
                reportElements, ref tallyCounter, ref replacementCounter, ref blockCounter, false);
            //We'll manually clear filteredBlocks, since we didn't work with it directly.
            filteredBlocks.Clear();
            /*debug.WriteText("TallyGenerics handled\n", true);*/

            //TallyCargo and TallyItem-based tallies are a bit more complex. We'll actually handle
            //them in two passes, with the first pass determining what tallies we need and generating
            //any tallies that are missing.
            //Later, we'll use these two item types to help us determine if an inventory can accomodate 
            //the 'ore' or 'cargo' types. For now, we'll just pass one in to configureTallyCargo so
            //it won't complain about not having a MyItemType.
            MyItemType ironOre = new MyItemType(ORE_TYPE, "Iron");
            MyItemType ironIngot = new MyItemType(INGOT_TYPE, "Iron");
            //Cargo. We exclude blocks that have the CargoContainer type, but can't be conveyor'd.
            filterBlocks(gridBlocks, filteredBlocks, b => (b is IMyShipConnector || b is IMyCargoContainer)
                && !(b.BlockDefinition.SubtypeId == "LargeBlockLockerRoom"
                || b.BlockDefinition.SubtypeId == "LargeBlockLockerRoomCorner"
                || b.BlockDefinition.SubtypeId == "LargeBlockLockers"
                || b.BlockDefinition.SubtypeId == "LargeBlockWeaponRack"
                || b.BlockDefinition.SubtypeId == "SmallBlockWeaponRack"));
            configureTallyCargo("Cargo", false, ironOre, -1, filteredBlocks.Count, filteredBlocks,
                highGood, lowGood, tallyDic, tallyList, cargoTallies, reportElements, ref tallyCounter);
            /*debug.WriteText("Cargo handled\n", true);*/
            //The Ore tally is generated if we have a drill or refinery.
            filterBlocks(gridBlocks, filteredBlocks, b => b is IMyShipDrill || b is IMyRefinery);
            configureTallyCargo("Ore", false, ironOre, -1, filteredBlocks.Count, filteredBlocks,
                highGood, lowGood, tallyDic, tallyList, cargoTallies, reportElements, ref tallyCounter, "", false);
            //So is the Stone tally.
            configureTallyCargo("Stone", true, new MyItemType(ORE_TYPE, "Stone"), 1000, filteredBlocks.Count,
                filteredBlocks, highGood, lowGood, tallyDic, tallyList, cargoTallies, reportElements, ref tallyCounter);
            //H2/02 generators generate the Ice tally.
            filterBlocks(gridBlocks, filteredBlocks, b => b is IMyGasGenerator);
            configureTallyCargo("Ice", true, new MyItemType(ORE_TYPE, "Ice"), 1000, filteredBlocks.Count,
                filteredBlocks, highGood, lowGood, tallyDic, tallyList, cargoTallies, reportElements, ref tallyCounter);
            //Reactors generate the Uranium tally
            filterBlocks(gridBlocks, filteredBlocks, b => b is IMyReactor);
            configureTallyCargo("Uranium", true, new MyItemType(INGOT_TYPE, "Uranium"), 50, filteredBlocks.Count,
                filteredBlocks, highGood, lowGood, tallyDic, tallyList, cargoTallies, reportElements, ref tallyCounter);
            /*debug.WriteText("Basic TallyItems handled\n", true);*/
            //Weapons are different.
            //Since the Warfare 2 update, we can no longer assume that a weapon will have its own 
            //interface. So the process of determining which of those blocks might be on the grid is 
            //now somewhat involved.
            List<IMyTerminalBlock> weapons = new List<IMyTerminalBlock>();
            filterBlocks(gridBlocks, weapons, b => b is IMyUserControllableGun);
            List<MyItemType> acceptedTypes = new List<MyItemType>();
            //For the time being, all we need to know about a given weapon type is, 'how many do we have.'
            //(Except Interior Turrets, which can't be piped and are lame)
            //And because we're using the 'can hold' appraoch, we'll also need an ammo type.
            //gatling autocannon assault artillery railSmall railLarge rocket
            int gatlingCount, autoCount, assaultCount, artilleryCount, railSmallCount, railLargeCount, rocketCount;
            gatlingCount = autoCount = assaultCount = artilleryCount = railSmallCount = railLargeCount = rocketCount = 0;
            MyItemType gatlingAmmo = new MyItemType(AMMO_TYPE, "NATO_25x184mm");
            MyItemType autoAmmo = new MyItemType(AMMO_TYPE, "AutocannonClip");
            MyItemType assaultAmmo = new MyItemType(AMMO_TYPE, "MediumCalibreAmmo");
            MyItemType artilleryAmmo = new MyItemType(AMMO_TYPE, "LargeCalibreAmmo");
            MyItemType railSmallAmmo = new MyItemType(AMMO_TYPE, "SmallRailgunAmmo");
            MyItemType railLargeAmmo = new MyItemType(AMMO_TYPE, "LargeRailgunAmmo");
            MyItemType rocketAmmo = new MyItemType(AMMO_TYPE, "Missile200mm");
            foreach (IMyTerminalBlock weapon in weapons)
            {
                acceptedTypes.Clear();
                weapon.GetInventory(0).GetAcceptedItems(acceptedTypes);
                if (acceptedTypes.Contains(gatlingAmmo))
                { gatlingCount++; }
                else if (acceptedTypes.Contains(autoAmmo))
                { autoCount++; }
                else if (acceptedTypes.Contains(assaultAmmo))
                { assaultCount++; }
                else if (acceptedTypes.Contains(artilleryAmmo))
                { artilleryCount++; }
                else if (acceptedTypes.Contains(railSmallAmmo))
                { railSmallCount++; }
                else if (acceptedTypes.Contains(railLargeAmmo))
                { railLargeCount++; }
                else if (acceptedTypes.Contains(rocketAmmo))
                { rocketCount++; }
            }
            //gat 10 auto 10 ass 5 art 3 rail 5 rail 5 rocket 4
            newTally = configureTallyCargo("GatlingAmmo", true, gatlingAmmo, 10, gatlingCount, null, highGood, lowGood,
                tallyDic, tallyList, cargoTallies, reportElements, ref tallyCounter, "Gatling\nDrums", false);
            newTally = configureTallyCargo("AutocannonAmmo", true, autoAmmo, 10, autoCount, null, highGood, lowGood,
                tallyDic, tallyList, cargoTallies, reportElements, ref tallyCounter, "Autocannon\nMagazines", false);
            newTally = configureTallyCargo("AssaultAmmo", true, assaultAmmo, 5, assaultCount, null, highGood, lowGood,
                tallyDic, tallyList, cargoTallies, reportElements, ref tallyCounter, "Assault\nShells", false);
            newTally = configureTallyCargo("ArtilleryAmmo", true, artilleryAmmo, 3, artilleryCount, null, highGood, lowGood,
                tallyDic, tallyList, cargoTallies, reportElements, ref tallyCounter, "Artillery\nShells", false);
            newTally = configureTallyCargo("RailSmallAmmo", true, railSmallAmmo, 5, railSmallCount, null, highGood, lowGood,
                tallyDic, tallyList, cargoTallies, reportElements, ref tallyCounter, "Railgun\nS. Sabot", false);
            newTally = configureTallyCargo("RailgunLargeAmmo", true, railLargeAmmo, 5, railLargeCount, null, highGood, lowGood,
                tallyDic, tallyList, cargoTallies, reportElements, ref tallyCounter, "Railgun\nL. Sabot", false);
            newTally = configureTallyCargo("RocketAmmo", true, rocketAmmo, 4, rocketCount, null, highGood, lowGood,
                tallyDic, tallyList, cargoTallies, reportElements, ref tallyCounter, "Rockets", false);
            /*debug.WriteText("Weapons handled\n", true);*/

            //Now for thirty questions. We'll go to each block on the grid that has an inventory, 
            //then to each inventory on that block, and then for each TallyItem in our list (And
            //the Cargo and Ore tallies), we'll ask that inventory if it can accomodate that tally's
            //item. And we'll store any config we write, so we can minimize the number of times we
            //have to go through this very expensive process.

            //The Thirty Questions apporach we now use to generate config for inventory blocks is
            //instruction intensive. This dictionary of previously generated configs means we only
            //have to go through the process once per block type.
            Dictionary<MyDefinitionId, string> generatedConfig = new Dictionary<MyDefinitionId, string>();
            //The block definition of the block we're currently looking at.
            MyDefinitionId blockID;
            //A reference to the inventory we're currently interrogating.
            IMyInventory inventory;
            //When we find a tally whose item can fit into this inventory, we'll add its name to 
            //this list.
            List<string> compatibleTallyNames = new List<string>();
            //string talliesConfig;
            //Holds the entirety of a block's Shipware config, section header, keys, and values.
            string scriptConfig;
            TallyItem itemTally;
            //The number of tallies we will allow before moving to a new line of config.
            //We'll use this variable again when we get to the report, but we'll change the value.
            int entriesPerLine = 6;
            //The first step, though, is to cut our list down to only things with inventories. But 
            //there's a laundry list of blocks with inventories we're going to exclude.
            //We'll exclude sorters because they don't really hold things
            //Gas tanks because no one cares how many bottles they have and also because we'd have
            //to do finagling to mesh these tallies with the gas tallies.
            //Cockpits, Interior Turrets, and the locker derivitives because we can't be sure they're 
            //piped, or we know they aren't. 
            //Welders, grinders, and stores because they don't accept push requests. 
            //Note: Because I keep coming back to this idea: No, there apparently isn't a good way
            //to tell if a block can be conveyored.
            //https://discord.com/channels/125011928711036928/216219467959500800/632478835182534676
            filterBlocks(gridBlocks, filteredBlocks, b => b.HasInventory &&
                !(b is IMyConveyorSorter || b is IMyGasTank || b is IMyCockpit || b is IMyStoreBlock
                || b is IMyLargeInteriorTurret || b is IMyShipWelder || b is IMyShipGrinder
                || b.BlockDefinition.SubtypeId == "LargeBlockLockerRoom"
                || b.BlockDefinition.SubtypeId == "LargeBlockLockerRoomCorner"
                || b.BlockDefinition.SubtypeId == "LargeBlockLockers"
                || b.BlockDefinition.SubtypeId == "LargeBlockWeaponRack"
                || b.BlockDefinition.SubtypeId == "SmallBlockWeaponRack"));
            /*debug.WriteText("Entering 30 questions loop\n", true);*/
            foreach (IMyTerminalBlock block in filteredBlocks)
            {
                blockID = block.BlockDefinition;
                /*debug.WriteText($"Current block ID: {blockID}\n", true);*/
                //If we haven't generated config for this block type yet...
                if (!generatedConfig.ContainsKey(blockID))
                {
                    /*debug.WriteText($"  No config found. Writing...\n", true);*/
                    scriptConfig = "";
                    //We have some work to do. We'll need to go to every inventory on the block...
                    for (int currentInventoryIndex = 0; currentInventoryIndex < block.InventoryCount; currentInventoryIndex++)
                    {
                        /*debug.WriteText($"    Checking inventory {currentInventoryIndex}\n", true);*/
                        inventory = block.GetInventory(currentInventoryIndex);
                        acceptedTypes.Clear();
                        compatibleTallyNames.Clear();
                        inventory.GetAcceptedItems(acceptedTypes);
                        //Then go through every tally we have to see if that inventory can 
                        //accomodate it
                        foreach (TallyCargo tally in cargoTallies)
                        {
                            itemTally = tally as TallyItem;
                            /*debug.WriteText($"      Checking compatability with tally {tally.programName}\n", true);*/
                            if (itemTally != null)
                            {
                                if (acceptedTypes.Contains(itemTally.itemType))
                                { compatibleTallyNames.Add(itemTally.programName); }
                            }
                            else if (tally.programName == "Cargo")
                            {
                                if (acceptedTypes.Contains(ironIngot) && acceptedTypes.Contains(ironOre))
                                { compatibleTallyNames.Add(tally.programName); }
                            }
                            else if (tally.programName == "Ore")
                            {
                                if (acceptedTypes.Contains(ironOre))
                                { compatibleTallyNames.Add(tally.programName); }
                            }
                        }
                        //Write config for this inventory, linking it to the compatible tallies we've found
                        scriptConfig = scriptConfig + getMultilineConfig(compatibleTallyNames,
                            (block.InventoryCount > 1 ? $"\nInv{currentInventoryIndex}Tallies" : "Tallies"),
                            entriesPerLine, _sb);
                        //If we actually have some config for compatible tallies...
                        if (!string.IsNullOrEmpty(scriptConfig))
                        {
                            //If this is the first bit of config we're writing, we'll need the 
                            //section header
                            if (currentInventoryIndex == 0)
                            { scriptConfig = $"[{_tag}]\n{scriptConfig}"; }
                        }
                        /*
                        else
                        { debug.WriteText($"    No compatible tallies found.\n", true); }*/
                    }
                    //Once we have config for this block type, we'll add it to the dictionary so 
                    //we can skip this process the next time around.
                    generatedConfig.Add(blockID, scriptConfig);
                }
                else
                //If we do have config stored, we'll use that.
                {
                    /*debug.WriteText($"  Existing config found\n", true);*/
                    scriptConfig = generatedConfig[blockID];
                }
                //Once we're sure we have some config, we'll chuck our old config and add the new
                //config to the block
                configWriter.TryParse(block.CustomData);
                if (configWriter.ContainsSection(_tag))
                {
                    configWriter.DeleteSection(_tag);
                    replacementCounter++;
                }
                block.CustomData = $"{configWriter.ToString()}\n\n{scriptConfig}";
                blockCounter++;
            }
            /*debug.WriteText($"30 questions complete\n", true);*/

            //The last step is to generate a generic report that will be displayed on the first 
            //surface of the PB itself.
            //We'll start by removing config that's already on the PB.
            if (MyIni.HasSection(Me.CustomData, _tag))
            {
                configWriter.TryParse(Me.CustomData);
                configWriter.DeleteSection(_tag);
                Me.CustomData = configWriter.ToString();
            }
            //Figure out how many columns we'll need. We won't have more than 3 columns.
            entriesPerLine = 3;
            int reportColumns = Math.Min(reportElements.Count, entriesPerLine);
            string reportConfig = getMultilineConfig(reportElements, "Surface0Elements", entriesPerLine, _sb);
            Me.CustomData =
                $"[{_tag}]\n" +
                $"{reportConfig}\n" +
                $"Surface0Columns = {reportColumns}\n" +
                $"Surface0FontSize = 1\n" +
                $"Surface0BackColor = 0,0,0";
            //We're done here. Let's talk about what happened.
            return $"Carried out AutoPopulate command, adding {tallyCounter} tallies and " +
                $"configuration on {blockCounter} blocks" +
                $"{(replacementCounter != 0 ? $" (With {replacementCounter} blocks having existing configuration replaced)" : "")}.";
        }

        //Takes blocks in the 'sourceList' that match the 'filter' conditions and adds them to the 'outList'.
        public void filterBlocks(List<IMyTerminalBlock> sourceList, List<IMyTerminalBlock> outList,
            Func<IMyTerminalBlock, bool> filter)
        {
            foreach (IMyTerminalBlock block in sourceList)
            {
                if (filter(block))
                { outList.Add(block); }
            }
        }

        public TallyGeneric configureTallyGeneric(string tallyName, ITallyGenericHandler handler,
            List<IMyTerminalBlock> filteredBlocks, Dictionary<string, Tally> tallyDic,
            List<Tally> tallyList, ColorCoderHigh highGood, MyIni configWriter, List<string> reportElements, 
            ref int tallyCounter, ref int replacementCounter, ref int blockCounter, bool clearFiltered = true)
        {
            TallyGeneric tally = null;
            //If we actaully found at least one block relevant to this tally...
            if (filteredBlocks.Count > 0)
            {
                //If we don't already have a tally by this name, generate one and make a note.
                if (!tallyDic.ContainsKey(tallyName))
                {
                    tally = new TallyGeneric(_meterMaid, tallyName, handler, highGood);
                    tallyList.Add(tally);
                    tallyCounter++;
                }
                else
                { tally = (TallyGeneric)tallyDic[tallyName]; }
                //So long as we found at least one block for this tally, we'll want it on our element list
                reportElements.Add(tally.programName);
                //Config for TallyGenerics is straightforward. We'll go ahead and write it now.
                foreach (IMyTerminalBlock block in filteredBlocks)
                {
                    if (MyIni.HasSection(block.CustomData, _tag))
                    {
                        configWriter.TryParse(block.CustomData);
                        configWriter.DeleteSection(_tag);
                        block.CustomData = configWriter.ToString();
                        replacementCounter++;
                    }
                    block.CustomData = $"{block.CustomData}\n\n[{_tag}]\nTallies = {tallyName}";
                    blockCounter++;
                }
            }
            //Usually, we'll want to clear our list of filteredBlocks for the next run.
            if (clearFiltered)
            { filteredBlocks.Clear(); }
            return tally;
        }

        //Returns: The specific tally we created, just in case we need to make some further modifications
        public TallyCargo configureTallyCargo(string tallyName, bool isItem, MyItemType type,
            int perBlock, int blockCount, List<IMyTerminalBlock> filteredBlocks, ColorCoderHigh highGood, 
            ColorCoderLow lowGood, Dictionary<string, Tally> tallyDic, List<Tally> tallyList, 
            List<TallyCargo> cargoTallies, List<string> reportElements, ref int tallyCounter, 
            string displayName = "", bool clearFiltered = true)
        {
            /*debug.WriteText($"Entered configureTallyCargo for tally '{tallyName}'\n", true);*/
            TallyCargo tally = null;
            //If we actaully found at least one block relevant to this tally...
            if (blockCount > 0)
            {
                //If we don't have a tally by this name yet, make one.
                if (!tallyDic.ContainsKey(tallyName))
                {
                    //MyItemType is a struct without an empty flag. That means we can't null it and
                    //we can't have it in a state where it is obviously not holding data. So we use 
                    //a bool to explicitly tell us if this is an item type or not.
                    if (isItem)
                    { tally = new TallyItem(_meterMaid, tallyName, type, highGood, perBlock * blockCount); }
                    else
                    { tally = new TallyCargo(_meterMaid, tallyName, lowGood); }
                    //Were we given a custom displayName?
                    if (!string.IsNullOrEmpty(displayName))
                    { tally.displayName = displayName; }
                    /*debug.WriteText($"  Created new tally\n", true);*/
                    tallyList.Add(tally);
                    cargoTallies.Add(tally);
                    tallyCounter++;
                }
                else
                { tally = (TallyCargo)tallyDic[tallyName]; }
                //So long as we found at least one block for this tally, we'll want it on our element list
                reportElements.Add(tallyName);
            }
            //We usually want to clear our filteredBlocks list in preperation for the next run of 
            //this method
            if (clearFiltered)
            { filteredBlocks.Clear(); }

            return tally;
        }

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

        public void initiate(out LimitedMessageLog textLog, out bool firstRun)
        {
            //Initiate some of the background objects the script needs to operate
            _iniReadWrite = new MyIni();
            _iniRead = new MyIni();
            _argReader = new MyCommandLine();
            _sb = new StringBuilder();
            _iniRaw = new RawTextIni(_sb);
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
            //The text surface we'll be using for debug prints
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

        public Dictionary<string, IColorCoder> compileColors()
        {
            Dictionary<string, IColorCoder> colorPalette = new Dictionary<string, IColorCoder>();
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
            //TurretPassive: Targeting disabled
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
            });
            //Custom turret controllers
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

        public void evaluate(bool firstRun = false)
        {
            //We'll need the ability to move data around during evaluation. A list will suffice for
            //reports, but we'll need a dictionary to make the tallies, containers, and indicators 
            //work.
            Dictionary<IMyInventory, List<TallyCargo>> evalContainers = new Dictionary<IMyInventory, List<TallyCargo>>();
            Dictionary<string, Tally> evalTallies = new Dictionary<string, Tally>();
            Dictionary<string, Trigger> evalTriggers = new Dictionary<string, Trigger>();
            List<IReportable> evalReports = new List<IReportable>();
            Dictionary<string, Indicator> evalIndicators = new Dictionary<string, Indicator>();
            //Anything that can provide an element (Tally, ActionSet, Trigger) must have a unique 
            //name. To make that determination easier, we'll keep all the names in one place.
            HashSet<string> usedElementNames = new HashSet<string>();
            //We make multiple (Two, at the moment) passes through the ActionSet configuration. We
            //need a list to store loaded ActionSets - in the order we read them - for later access.
            List<ActionSet> loadedActionSets = new List<ActionSet>();
            //List<string> loadedActionSets = new List<string>();
            //MFDs, ActionSets, and Raycasters are special, though. We'll leave them in a dictionary.
            _MFDs = new Dictionary<string, MFD>();
            _sets = new Dictionary<string, ActionSet>();
            _raycasters = new Dictionary<string, Raycaster>();
            //We keep a special list of WOTs configured to display the log, so we can update them 
            //even if the rest of the script isn't working.
            _logReports = new List<WallOText>();
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
            Trigger trigger = null;
            //On the other hand, sometimes you need something a little bit generic.
            IReportable reportable;
            //Some blocks do multiple jobs, which means a block has to be subjected to multiple 
            //different sorters. This variable will tell us if at least one of those sorters knew 
            //how to handle the block.
            bool handled = false;
            //We'll need a log to store errors.
            //string errors = "";
            LimitedMessageLog textLog = new LimitedMessageLog(_sb, 15);
            //We'll use these strings to store the information we need to build a tally.
            string initTag = $"{_tag}Init";
            string elementName = "";
            string MFDName = "";
            string addIn1 = "";
            //For things like ActionSets and MFDs, we use a discrete section in the INI for 
            //configuration. We'll store the name for these sections, which is the PREFIX followed
            //by the name of the object, in this string.
            string discreteTag = "";
            //Sometimes, we want a little color.
            Color color = Hammers.cozy;
            //The tallies a block reports to are stored in a delimited string. We'll need something
            //to hold those as something easier to work with.
            string[] elementNames;
            //The ubiquitous list of terminal blocks.
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            string evalNonDeclarationPBConfig = "";

            //We may as well go ahead an handle the firstRun scenario
            if (firstRun)
            {
                textLog.addNote("First run complete.\n" +
                    " -Use the AutoPopulate command to generate basic configuration.\n" +
                    " -The Template and Populate commands make writing your own config easier.\n" +
                    " -The Clone command can quickly distribute config across identical blocks.\n" +
                    " -The Evaluate command scans the grid for config and loads it into memory.");
            }

            //We'll go ahead and get a parse from the Storage string. 
            _iniRead.TryParse(Storage);

            //Now that we have that, we'll go ahead and set the update delay to whatever we have stored
            _distributor.setDelay(_iniRead.Get("Data", "UpdateDelay").ToInt32(0));

            //Parse the PB's custom data. If it doesn't return something useable...
            if (!_iniReadWrite.TryParse(Me.CustomData, out parseResult))
            //...file a complaint.
            {
                /*textLog.addError($"The parser was unable to read information from the Programmable Block. " +
                      $"Reason: {parseResult.Error}");*/
                textLog.addError($"The parser encountered an error on line {parseResult.LineNo} of the " +
                    $"Programmable Block's config: {parseResult.Error}");
            }

            //Let's start by getting the colors set. We store our default pallette (In the form of 
            //ColorCoders) in a dictionary so we can quickly match them, and re-use references to 
            //custom ColorCoders.
            Dictionary<string, IColorCoder> colorPalette = compileColors();
            //Because we use the actual color coders frequently, we'll put individual handles on them
            ColorCoderLow lowGood = (ColorCoderLow)(colorPalette["lowgood"]);
            ColorCoderHigh highGood = (ColorCoderHigh)(colorPalette["highgood"]);

            IColorCoder colorCoder = null;
            string configTag = "SW.Config";
            bool hasConfigSection = _iniReadWrite.ContainsSection(configTag);
            if (!hasConfigSection)
            {
                textLog.addNote($"SW.Config section was missing from block '{Me.CustomName}' and " +
                  $"has been re-generated.");
            }
            bool logKeyGeneration = !(firstRun || !hasConfigSection);
            bool configAltered = false;
            Action<string> troubleLogger = b => textLog.addError(b);

            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue,
                lowGood, highGood, "SW.Init", "Optimal", "Green", logKeyGeneration, ref configAltered);
            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue,
                lowGood, highGood, "SW.Init", "Normal", "LightBlue", logKeyGeneration, ref configAltered);
            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue,
                lowGood, highGood, "SW.Init", "Caution", "Yellow", logKeyGeneration, ref configAltered);
            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue,
                lowGood, highGood, "SW.Init", "Warning", "Orange", logKeyGeneration, ref configAltered);
            tryGetPaletteFromConfig(troubleLogger, textLog, colorPalette, color, colorCoder, iniValue,
                lowGood, highGood, "SW.Init", "Critical", "Red", logKeyGeneration, ref configAltered);

            //On to the init section. 
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
            //The counter for this loop.
            int counter = 0;
            //As long as the counter isn't -1 (Which indicates that we've run out of tallies)...
            while (counter != -1)
            {
                tally = tryGetTallyFromConfig(_iniReadWrite, initTag, counter, colorPalette, ref color, 
                    colorCoder, iniValue, textLog);
                if (tally == null)
                //If we didn't find another tally, set the counter equal to -1 to indicate that 
                //we're done in this loop.
                { counter = -1; }
                else if (isElementNameInUse(usedElementNames, tally.programName, $"Tally{counter}", textLog))
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
                    counter++;
                }
            }

            //ActionSets also get their configuration on the PB, though we're only going to
            //gather the basics in this loop.
            counter = 0;
            while (counter != -1)
            {
                set = tryGetActionSetFromConfig(_iniReadWrite, _iniRead, initTag, counter, iniValue,
                    textLog, colorPalette, colorCoder);
                if (set == null)
                //This process is functionally identical to what we did for Tallies.
                { counter = -1; }
                else if (isElementNameInUse(usedElementNames, set.programName, $"ActionSet{counter}", textLog))
                { break; }
                else
                {
                    //We might have changed what the set uses for status text or colors. A call to 
                    //evaluateStatus will set things right.
                    set.evaluateStatus();
                    //This ActionSet should be ready. Pass it to the dictionary.
                    _sets.Add(set.programName, set);
                    //We'll need an ordered list of ActionSets for our second pass. Also add this 
                    //set to that list.
                    loadedActionSets.Add(set);
                    //loadedActionSets.Add(set.programName);
                    usedElementNames.Add(set.programName);
                    counter++;
                }
            }

            //On to Triggers
            counter = 0;
            //As long as the counter isn't -1 (Which indicates that we've run out of Triggers)...
            while (counter != -1)
            {
                //Try to pull a trigger from the config at the specified index
                tryGetTriggerFromConfig(_iniReadWrite, _iniRead, evalTallies, _sets, initTag, counter, iniValue,
                    ref tally, ref set, ref trigger, textLog);
                if (trigger == null)
                { counter = -1; }
                else if (isElementNameInUse(usedElementNames, trigger.programName, $"Trigger{counter}", textLog))
                { break; }
                else
                {
                    evalTriggers.Add(trigger.programName, trigger);
                    usedElementNames.Add(trigger.programName);
                    counter++;
                }
            }

            //Now that we have objects corresponding to all the config on the PB, we can make the 
            //final pass where we handle ActionSets that manipulate other script objects.
            //foreach (ActionSet loadedSet in loadedActionSets)
            for (int i = 0; i < loadedActionSets.Count; i++)
            //foreach (string loadedSet in loadedActionSets)
            {
                set = loadedActionSets[i];
                readPBPlansFromConfig(_sets, evalTriggers, _iniReadWrite, initTag,
                    i, set, iniValue, textLog);
            }

            //If we don't have errors, but we also don't have any tallies or ActionSets...
            if (textLog.getErrorTotal() == 0 && evalTallies.Count == 0 && _sets.Count == 0)
            { textLog.addError($"No readable configuration found on the programmable block."); }

            //Only if there were no errors with parsing the PB...
            if (textLog.getErrorTotal() == 0)
            {
                //Hack-in for Reconstitute
                evalNonDeclarationPBConfig = stripDeclarations();
                //...should we get the blocks on the grid with our section tag. But first, we'll
                //see if we need to set up any raycasters
                findBlocks<IMyTerminalBlock>(blocks, b => (b.IsSameConstructAs(Me)
                    && b is IMyCameraBlock && MyIni.HasSection(b.CustomData, _tag)));
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
                    if (_iniReadWrite.TryParse(block.CustomData, out parseResult))
                    {
                        //Does this block have configuration for a raycaster?
                        if (_iniReadWrite.ContainsKey(_tag, "RaycasterName"))
                        {
                            elementName = _iniReadWrite.Get(_tag, "RaycasterName").ToString();
                            //Is the user trying to give this Raycaster the same name as a tally?
                            if (!evalTallies.ContainsKey(elementName))
                            {
                                //Because this is a bridge from the old version to the new version,
                                //we're going to make some assumptions
                                RaycasterModuleBase scanModule = new RaycasterModuleLinear();
                                string[] moduleConfigurationKeys = RaycasterModuleLinear.getModuleConfigurationKeys();
                                double[] moduleConfigurationValues = new double[3];
                                //Create a new Raycaster object with the default scanModule
                                Raycaster raycaster = new Raycaster(_sb, scanModule, elementName);
                                //Link this camera to our shiny new raycaster
                                raycaster.addCamera((IMyCameraBlock)block);
                                //Also create a new tally that will report the charge of this Raycaster
                                tallyGeneric = new TallyGeneric(_meterMaid, $"{elementName}Tally", new RaycastHandler(), highGood);
                                //... And add the camera to it. Won't do much if we don't do that!
                                tallyGeneric.tryAddBlock(block);
                                //Get the optional configuration information for this Raycaster and
                                //its assocaiated tally. 
                                //DisplayName (Affects Tally only)
                                iniValue = _iniReadWrite.Get(_tag, $"RaycasterDisplayName");
                                if (!iniValue.IsEmpty)
                                { tallyGeneric.displayName = iniValue.ToString(); }
                                //Since we still have a direct reference to the scanModule, we should
                                //be fine to do the configuration even after the actual Raycaster has 
                                //been created.
                                for (int i = 0; i < moduleConfigurationKeys.Length; i++)
                                {
                                    moduleConfigurationValues[i] =
                                        _iniReadWrite.Get(_tag, moduleConfigurationKeys[i]).ToDouble(-1);
                                }
                                //Send retrieved configuration to the scanning module
                                scanModule.configureModuleByArray(moduleConfigurationValues);
                                
                                //Use the calculated maximum charge for a single scan as this tally's
                                //max.
                                tallyGeneric.forceMax(scanModule.requiredCharge);
                                //Now that our Raycaster and Tally are ready, we'll add them to their
                                //respective data structures.
                                _raycasters.Add(elementName, raycaster);
                                evalTallies.Add($"{elementName}Tally", tallyGeneric);
                            }
                            //If the user is trying to give this raycaster the same name as a tally,
                            //complain. In the most understanding way possible.
                            else
                            {
                                textLog.addError($"The Raycaster name '{elementName}' is already in use " +
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

                findBlocks<IMyTerminalBlock>(blocks, b =>
                    (b.IsSameConstructAs(Me) && MyIni.HasSection(b.CustomData, _tag)));
                //NOTE: This will never throw an error. Back when it used to exclude Me from the
                //  list it could have, but now, in order to reach this point, you must have config
                //  data on the PB.
                if (blocks.Count <= 0)
                { textLog.addError($"No blocks found on this construct with a [{_tag}] INI section."); }
            }

            //Every block we've found has some sort of configuration information for this script.
            //And we're going to read all of it.
            foreach (IMyTerminalBlock block in blocks)
            {
                //Whatever kind of block this is, we're going to need to see what's in its 
                //CustomData. If that isn't useable...
                if (!_iniReadWrite.TryParse(block.CustomData, out parseResult))
                //...complain.
                {
                    /*textLog.addError($"The parser was unable to read information from block " +
                          $"'{block.CustomName}'. Reason: {parseResult.Error}");*/
                    textLog.addError($"The parser encountered an error on line {parseResult.LineNo} " +
                        $"of block '{block.CustomName}' config: {parseResult.Error}");
                }
                //My comedic, reference-based genius shall be preserved here for all eternity. Even
                //if it is now largely irrelevant to how ShipManager operates.
                //In the CargoManager, the data is handled by two seperate yet equally important
                //objects: the Tallies that store and calculate information and the Reports that 
                //display it. These are their stories.

                //There's a couple of keys that are present on multiple block types. We'll check for
                //those first.
                //If our block has a 'Tallies' key...
                if (parseResult.Success && _iniReadWrite.ContainsKey(_tag, "Tallies"))
                {
                    //This is grounds for declaring this block to be handled.
                    handled = true;
                    //Get the 'Tallies' data
                    iniValue = _iniReadWrite.Get(_tag, "Tallies");
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
                                    textLog.addError($"Block '{block.CustomName}' does not have an " +
                                        $"inventory and is not compatible with the TallyType of " +
                                        $"tally '{name}'.");
                                }
                            }
                            else if (tally is TallyGeneric)
                            {
                                tallyGeneric = (TallyGeneric)tally;

                                if (!tallyGeneric.tryAddBlock(block))
                                {
                                    textLog.addError($"Block '{block.CustomName}' is not a {tallyGeneric.getTypeAsString()} " +
                                            $"and is not compatible with the TallyType of tally '{name}'.");
                                }
                            }
                            else
                            //If a tally isn't a TallyCargo or a TallyGeneric or a TallyShield, I done goofed.
                            {
                                textLog.addError($"Block '{block.CustomName}' refrenced the tally '{name}'," +
                                    $"which is neither a TallyCargo or a TallyGeneric. Complain to the " +
                                    $"script writer, this should be impossible.");
                            }
                        }
                        //If we can't find this name in evalTallies, complain.
                        else
                        {
                            textLog.addError($"Block '{block.CustomName}' tried to reference the " +
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
                        if (_iniReadWrite.ContainsKey(_tag, $"Inv{i}Tallies"))
                        {
                            //If we manage to find one of these keys, the block can be considered
                            //handled.
                            handled = true;
                            //Get the names of the specified tallies
                            iniValue = _iniReadWrite.Get(_tag, $"Inv{i}Tallies");
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
                                        textLog.addError($"Block '{block.CustomName}' is not compatible " +
                                            $"with the TallyType of tally '{name}' referenced in key " +
                                            $"Inv{i}Tallies.");
                                    }
                                }
                                //If we can't find this name in evalTallies, complain.
                                else
                                {
                                    textLog.addError($"Block '{block.CustomName}' tried to reference the " +
                                        $"unconfigured tally '{name}' in key Inv{i}Tallies.");
                                }
                            }
                        }
                        //If there is no key, we fail silently.
                    }
                }

                //If the block has an 'ActionSets' key...
                if (parseResult.Success && _iniReadWrite.ContainsKey(_tag, "ActionSets"))
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
                    iniValue = _iniReadWrite.Get(_tag, "ActionSets");
                    //Pull the individual ActionSet names from the ActionSets key.
                    elementNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                    foreach (string name in elementNames)
                    {
                        //First things first: Does this ActionSet even exist?
                        if (_sets.ContainsKey(name))
                        {
                            //The name of the discrete section that will configure this ActionSet 
                            //is the PREFIX plus the name of the ActionSet. We'll be using that a 
                            //lot, so let's put a handle on it.
                            discreteTag = $"{_SCRIPT_PREFIX}.{name}";
                            //Check to see if the user has included an ACTION SECTION
                            if (_iniReadWrite.ContainsSection(discreteTag))
                            {
                                //Make a new, generic action plan.
                                IHasActionPlan actionPlan = null;
                                //If this is a camera block...
                                if (block is IMyCameraBlock)
                                {
                                    //The name of this raycaster should be on this block. Try to 
                                    //find it, using an empty string if we can't.
                                    string raycasterName = _iniReadWrite.Get(_tag, "RaycasterName").ToString("");
                                    ActionPlanRaycaster cameraPlan;
                                    //If we can actually find a raycaster matching the name we 'found'.
                                    if (_raycasters.ContainsKey(raycasterName))
                                    //Use the raycaster at the designated key to make a new ActionPlan
                                    { cameraPlan = new ActionPlanRaycaster(_raycasters[raycasterName]); }
                                    //Otherwise, create a new action plan with a null raycaster. This
                                    //will crash and burn the first time someone tries to use it, 
                                    //but if something went wrong with /this/ process, you can bet
                                    //we're complaining about it elsewhere.
                                    else
                                    { cameraPlan = new ActionPlanRaycaster(null); }
                                    //Look for an ActionOn entry
                                    iniValue = _iniReadWrite.Get(discreteTag, $"ActionOn");
                                    //There's only one value that we're looking for here
                                    if (iniValue.ToString() == "CameraRaycast")
                                    { cameraPlan.scanOn = true; }
                                    //Repeat the process for ActionOff
                                    iniValue = _iniReadWrite.Get(discreteTag, $"ActionOff");
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
                                    else
                                    {
                                        //Create a new block plan with this block as the subject
                                        ActionPlanBlock blockPlan = new ActionPlanBlock(block);
                                        //Check to see if there's an ActionOn in the ACTION SECTION
                                        blockPlan.actionOn = retrieveActionHandler(textLog, discreteTag,
                                            "ActionOn", block, _iniReadWrite, actions);
                                        //Check to see if there's an ActionOff in the ACTION SECTION
                                        blockPlan.actionOff = retrieveActionHandler(textLog, discreteTag,
                                            "ActionOff", block, _iniReadWrite, actions);
                                        //Pass the BlockPlan to the generic ActionPlan
                                        actionPlan = blockPlan;
                                    }
                                }
                                //If we have successfully registered at least one action...
                                if (actionPlan.hasAction())
                                //Go ahead and add this ActionPlan to the ActionSet
                                { _sets[name].addActionPlan(actionPlan); }
                                //If we didn't successfully register an action, complain.
                                else
                                {
                                    textLog.addError($"Block '{block.CustomName}', discrete section '{discreteTag}', " +
                                        $"does not define either an ActionOn or an ActionOff.");
                                }
                            }
                            //If there is no ACTION SECTION, complain.
                            else
                            {
                                textLog.addError($"Block '{block.CustomName}' references the ActionSet " +
                                    $"'{name}', but contains no discrete '{discreteTag}' section that would " +
                                    $"define actions.");
                            }
                        }
                        //If the set does not exist, complain.
                        else
                        {
                            textLog.addError($"Block '{block.CustomName}' tried to reference the " +
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
                        if (_iniReadWrite.ContainsKey(_tag, $"Surface{i}MFD"))
                        {
                            //Pull the name of the MFD from the main config
                            MFDName = _iniReadWrite.Get(_tag, $"Surface{i}MFD").ToString();
                            //So long as we don't already have an MFD by this name...
                            if (!_MFDs.ContainsKey(MFDName))
                            {
                                //Construct the discreteTag of the section that will configure this MFD
                                discreteTag = $"{_SCRIPT_PREFIX}.{MFDName}";
                                //Is there a discrete section with config for this MFD?
                                if (_iniReadWrite.ContainsSection(discreteTag))
                                {
                                    MFD newMFD = new MFD(MFDName);
                                    counter = 0;
                                    //There's several keys that we could be looking for.
                                    while (counter != -1)
                                    {
                                        reportable = tryGetReportableFromConfig(textLog, $"Page{counter}",
                                            discreteTag, surfaceProvider.GetSurface(i), block, _iniReadWrite,
                                            iniValue, colorCoder, evalTallies, _sets, evalTriggers, colorPalette);
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
                                            iniValue = _iniReadWrite.Get(discreteTag, $"Page{counter}LinkActionSetOn");
                                            if (!iniValue.IsEmpty)
                                            {
                                                //Store the name of the ActionSet the user wants to link 
                                                //to this page
                                                elementName = iniValue.ToString();
                                                //Check to see if this ActionSet even exists
                                                if (_sets.ContainsKey(elementName))
                                                {
                                                    ActionPlanMFD MFDPlan = new ActionPlanMFD(newMFD);
                                                    //Set this page as the ActionPlan's pageOn
                                                    MFDPlan.pageOn = addIn1;
                                                    //Add the ActionPlan to the ActionSet
                                                    _sets[elementName].addActionPlan(MFDPlan);
                                                }
                                                //If the ActionSet doesn't exist, complain.
                                                else
                                                {
                                                    textLog.addError($"Surface provider '{block.CustomName}', " +
                                                        $"discrete section '{discreteTag}', tried to " +
                                                        $"reference the unconfigured ActionSet '{elementName}' " +
                                                        $"in its LinkActionSetOn configuration.");
                                                }
                                            }
                                            iniValue = _iniReadWrite.Get(discreteTag, $"Page{counter}LinkActionSetOff");
                                            if (!iniValue.IsEmpty)
                                            {
                                                //Store the name of the ActionSet the user wants to link 
                                                //to this page
                                                elementName = iniValue.ToString();
                                                //Check to see if this ActionSet even exists
                                                if (_sets.ContainsKey(elementName))
                                                {
                                                    ActionPlanMFD MFDPlan = new ActionPlanMFD(newMFD);
                                                    //Set this page as the ActionPlan's pageOn
                                                    MFDPlan.pageOff = addIn1;
                                                    //Add the ActionPlan to the ActionSet
                                                    _sets[elementName].addActionPlan(MFDPlan);
                                                }
                                                //If the ActionSet doesn't exist, complain.
                                                else
                                                {
                                                    textLog.addError($"Surface provider '{block.CustomName}', " +
                                                        $"discrete section '{discreteTag}', tried to " +
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
                                        newMFD.trySetPage(_iniRead.Get("MFDs", MFDName).ToString());
                                        //Add the new MFD to our reports and MFDs
                                        evalReports.Add(newMFD);
                                        _MFDs.Add(MFDName, newMFD);
                                    }
                                    //If we didn't get at least one page, complain.
                                    else
                                    {
                                        textLog.addError($"Surface provider '{block.CustomName}', Surface {i}, " +
                                            $"specified the use of MFD '{MFDName}' but did not provide " +
                                            $"readable page configuration.");
                                    }
                                }
                                //If there is no discrete section, complain.
                                else
                                {
                                    textLog.addError($"Surface provider '{block.CustomName}', Surface {i}, " +
                                        $"declares the MFD '{MFDName}', but contains no discrete " +
                                        $"'{discreteTag}' section that would configure it.");
                                }
                            }
                            //If the name of this MFD is a duplicate, complain
                            else
                            {
                                textLog.addError($"Surface provider '{block.CustomName}', Surface {i}, " +
                                        $"declares the MFD '{MFDName}', but this name is already in use.");
                            }
                        }
                        else
                        {
                            //If it isn't an MFD, pass it directly to the specialized method for sorting
                            reportable = tryGetReportableFromConfig(textLog, $"Surface{i}", _tag,
                                surfaceProvider.GetSurface(i), block, _iniReadWrite, iniValue, colorCoder, 
                                evalTallies, _sets, evalTriggers, colorPalette);
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
                    iniValue = _iniReadWrite.Get(_tag, "Element");
                    if (!iniValue.IsEmpty)
                    {
                        elementName = iniValue.ToString();
                        IHasElement element = null;
                        //If the element is in evalTallies or sets... 
                        if (evalTallies.ContainsKey(elementName))
                        { element = evalTallies[elementName]; }
                        else if (_sets.ContainsKey(elementName))
                        { element = _sets[elementName]; }
                        else if (evalTriggers.ContainsKey(elementName))
                        { element = _sets[elementName]; }
                        //If we weren't able to find the element, complain.
                        else
                        {
                            textLog.addError($"Lighting block '{block.CustomName}' tried to reference " +
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
                        textLog.addError($"Lighting block {block.CustomName} has missing or unreadable Element. " +
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
                    textLog.addError($"Block '{block.CustomName}' is missing proper configuration or is a " +
                        $"block type that cannot be handled by this script.");
                }

                //Set handled to 'false' for the next iteration of the loop.
                handled = false;
            }

            //Time to finalize things. First, we need to build our array of containers using the
            //data we've collected. 
            Container container;
            //Build our execution array, based on how many entries we have in the container dictionary
            _containers = new Container[evalContainers.Count];
            counter = 0;
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
            //Next, tear down the complicated data structures we've been using for evaluation into
            //the arrays we'll be using during execution
            _tallies = evalTallies.Values.ToArray();
            _indicators = evalIndicators.Values.ToArray();
            _reports = evalReports.ToArray();
            _triggers = evalTriggers.Values.ToArray();
            //There's one more step before the tallies are ready. We need to tell them that they
            //have all the data that they're going to get. 
            foreach (Tally finishTally in _tallies)
            { finishTally.finishSetup(); }
            //We'll take this opportunity to call setProfile on all our Reportables
            foreach (IReportable screen in _reports)
            { screen.setProfile(); }
            //There's probably still data in the iniReader. We don't need it anymore, and we don't
            //want it carrying over to any future evaluations.
            _iniReadWrite.Clear();
            _iniRead.Clear();
            //NOTE: This is hacked in to work with Reconstitute
            _haveGoodConfig = true;
            _lastGoodConfigStamp = DateTime.Now;
            _nonDeclarationPBConfig = evalNonDeclarationPBConfig;
            /*
            //That should be it. So if we have no errors...
            if (textLog.getErrorTotal() == 0)
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
                    $"execution:\n{textLog.errorsToString()}");
            }
            */
            //The last step is to let the user know what happened.
            //DEBUG USE
            /*
            List<string> colorNames = colorPalette.Keys.ToList();
            string palettePrint = $"Palette contains {colorNames.Count} colorCoders:\n";
            foreach (string name in colorNames) 
            { palettePrint += $"{name}\n";}
            textLog.addNote(palettePrint);
            */
            //We use a simple string for this instead of the stringbuilder because LimitedErrorLog 
            //is using that in its toString() methods. 
            string outcome = "Grid evaluation complete.\n";
            if (textLog.getNoteTotal() > 0)
            { outcome += $"The following messages were logged:\n{textLog.notesToString()}\n"; }
            if (textLog.getErrorTotal() > 0)
            {
                //Make sure the script isn't trying to run with errors.
                Runtime.UpdateFrequency = UpdateFrequency.None;
                //File a complaint.
                outcome += $"The following errors are preventing script execution:\n{textLog.errorsToString()}";
                _log.add(outcome);
                //The script isn't running, that means our log reports aren't getting updated. 
                //Target them directly and force an update
                foreach (WallOText logReport in _logReports)
                { logReport.update(); }
            }
            else
            {
                //...Set the script into motion.
                Runtime.UpdateFrequency = UpdateFrequency.Update100;
                //Also, brag.
                outcome += $"Script is now running. Registered {_tallies.Length} tallies, " +
                    $"{_sets.Count} ActionSets, {_triggers.Length} triggers, and {_reports.Length} " +
                    $"reports, as configured by data on {blocks.Count} blocks. Evaluation used " +
                    $"{Runtime.CurrentInstructionCount} / {Runtime.MaxInstructionCount} " +
                    $"({(int)(((double)Runtime.CurrentInstructionCount / Runtime.MaxInstructionCount) * 100)}%) " +
                    $"of instructions allowed in this tic.";
                _log.add(outcome);
            }
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

        /* Scans the specified MyIni object for tally configuration. Can return a functional tally,
         * a stand-in tally and an error, or a null if no tally was found.
         *  -MyIni configReader: A MyIni object, loaded with the configuration we're wanting to scan
         *  -string targetSection: Which section in the configuration we should be scanning
         *  -int index: The index at which we're looking for a tally configuration.
         *  -MyIniValue iniValue: A reference to an iniValue object, so we don't need to allocate a 
         *   new one
         *  -LimitedErrorLog errors: The error log that we will report errors to.
         * Returns: A Tally object if some sort of configuration was found at the specified index 
         * (Even if that configuration has errors). A null if no config was found.
         */
        private Tally tryGetTallyFromConfig(MyIni configReader, string targetSection, int index,
            Dictionary<string, IColorCoder> colorPalette, ref Color color, IColorCoder colorCoder, 
            MyIniValue iniValue, LimitedMessageLog textLog)
        {
            Tally tally = null;
            string tallyName;
            string tallyType;
            ColorCoderLow lowGood = (ColorCoderLow)(colorPalette["lowgood"]);
            ColorCoderHigh highGood = (ColorCoderHigh)(colorPalette["highgood"]);
            Action<string> troubleLogger = b => textLog.addError(b);
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
                    textLog.addError($"Tally {tallyName} has a missing or unreadable TallyType.");
                    //Also, create a TallyCargo. This will let the rest of the script execute
                    //as normal, and hopefully prevent 'uninitialized tally' spam
                    tally = new TallyCargo(_meterMaid, tallyName, lowGood);
                }
                //Now, we create a tally based on the type. For the TallyCargo, that's quite straightforward.
                else if (tallyType == "Inventory")
                { tally = new TallyCargo(_meterMaid, tallyName, lowGood); }
                //Creating a TallyItem is a bit more involved.
                else if (tallyType == "Item")
                {
                    string typeID, subTypeID;
                    //We'll need a typeID and a subTypeID, and we'll need to complain if we can't
                    //get them
                    typeID = configReader.Get(targetSection, $"Tally{index}ItemTypeID").ToString();
                    if (string.IsNullOrEmpty(typeID))
                    { textLog.addError($"Item Tally '{tallyName}' has a missing or unreadable TallyItemTypeID."); }
                    subTypeID = configReader.Get(targetSection, $"Tally{index}ItemSubTypeID").ToString();
                    if (string.IsNullOrEmpty(subTypeID))
                    { textLog.addError($"Item Tally '{tallyName}' has a missing or unreadable TallyItemSubTypeID."); }
                    //If we have the data we were looking for, we can create a TallyItem
                    if (!string.IsNullOrEmpty(typeID) && !string.IsNullOrEmpty(subTypeID))
                    { tally = new TallyItem(_meterMaid, tallyName, typeID, subTypeID, highGood); }
                    //If we're missing data, we'll just create a TallyCargo so the script can 
                    //continue. The error message should already be logged.
                    else
                    { tally = new TallyCargo(_meterMaid, tallyName, lowGood); }
                }
                //Power and the other TallyGenerics are only marginally more complicated than Volume
                else if (tallyType == "Battery")
                { tally = new TallyGeneric(_meterMaid, tallyName, new BatteryHandler(), highGood); }
                //Gas, which works for both Hydrogen and Oxygen
                else if (tallyType == "Gas")
                { tally = new TallyGeneric(_meterMaid, tallyName, new GasHandler(), highGood); }
                else if (tallyType == "JumpDrive")
                { tally = new TallyGeneric(_meterMaid, tallyName, new JumpDriveHandler(), highGood); }
                else if (tallyType == "Raycast")
                { tally = new TallyGeneric(_meterMaid, tallyName, new RaycastHandler(), highGood); }
                else if (tallyType == "PowerMax")
                { tally = new TallyGeneric(_meterMaid, tallyName, new PowerMaxHandler(), highGood); }
                else if (tallyType == "PowerCurrent")
                { tally = new TallyGeneric(_meterMaid, tallyName, new PowerCurrentHandler(), highGood); }
                else if (tallyType == "Integrity")
                { tally = new TallyGeneric(_meterMaid, tallyName, new IntegrityHandler(), highGood); }
                else if (tallyType == "VentPressure")
                { tally = new TallyGeneric(_meterMaid, tallyName, new VentPressureHandler(), highGood); }
                else if (tallyType == "PistonExtension")
                { tally = new TallyGeneric(_meterMaid, tallyName, new PistonExtensionHandler(), highGood); }
                else if (tallyType == "RotorAngle")
                { tally = new TallyGeneric(_meterMaid, tallyName, new RotorAngleHandler(), highGood); }
                else if (tallyType == "ControllerGravity")
                { tally = new TallyGeneric(_meterMaid, tallyName, new ControllerGravityHandler(), highGood); }
                else if (tallyType == "ControllerSpeed")
                { tally = new TallyGeneric(_meterMaid, tallyName, new ControllerSpeedHandler(), highGood); }
                else if (tallyType == "ControllerWeight")
                { tally = new TallyGeneric(_meterMaid, tallyName, new ControllerWeightHandler(), highGood); }
                //TODO: Aditional TallyTypes go here
                else
                {
                    //If we've gotten to this point, the user has given us a type that we can't 
                    //recognize. Scold them.
                    textLog.addError($"Tally {tallyName}'s TallyType of '{tallyType}' cannot be handled " +
                        $"by this script. Be aware that TallyTypes are case-sensitive.");
                    //...Also, create a TallyCargo, so the rest of Evaluate will work.
                    tally = new TallyCargo(_meterMaid, tallyName, lowGood);
                }
                //Now that we have our tally, we need to check to see if there's any further
                //configuration data. 
                //First, the DisplayName
                iniValue = configReader.Get(targetSection, $"Tally{index}DisplayName");
                if (!iniValue.IsEmpty)
                { tally.displayName = iniValue.ToString(); }
                //Up next is the Multiplier. Note that, because of how forceMax works, the multiplier
                //must be applied before the max.
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
                    && (((TallyGeneric)tally).handler is RaycastHandler
                    || ((TallyGeneric)tally).handler is ControllerWeightHandler))))
                {
                    textLog.addError($"Tally {tallyName}'s TallyType of '{tallyType}' requires a Max " +
                        $"to be set in configuration.");
                }
                //Last step is to check for a custom ColorCoder
                if (tryGetColorFromConfig(troubleLogger, colorPalette, configReader, iniValue, Me, ref color, 
                    ref colorCoder, targetSection, $"Tally{index}ColorCoder"))
                { tally.colorCoder = colorCoder; }
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
        private ActionSet tryGetActionSetFromConfig(MyIni configReader, MyIni saveReader, string targetSection, int index,
            MyIniValue iniValue, LimitedMessageLog textLog, Dictionary<string, IColorCoder> colorPalette, IColorCoder colorCoder)
        {
            ActionSet set = null;
            Color color = Hammers.cozy;
            string setName = configReader.Get(targetSection, $"Action{index}Name").ToString();
            Action<string> troubleLogger = b => textLog.addError(b);
            if (!string.IsNullOrEmpty(setName))
            {
                //ActionSets have a lot less going on than tallies, initially at least. The only
                //other thing we /need/ to know about them is what their previous state was.
                //We'll try to get that from the storage string, defaulting to false if we can't
                //Also: because this method can be called from PopulatePB, which has no access to
                //and no need for the saveReader, we need to be able to handle a null here.
                bool state = saveReader?.Get("ActionSets", setName).ToBoolean(false) ?? false;
                set = new ActionSet(setName, state);

                //There are a few other bits of configuration that ActionSets may have
                iniValue = configReader.Get(targetSection, $"Action{index}DisplayName");
                if (!iniValue.IsEmpty)
                { set.displayName = iniValue.ToString(); }
                if (tryGetColorFromConfig(troubleLogger, colorPalette, configReader, iniValue, Me, 
                    ref color, ref colorCoder, targetSection, $"Action{index}ColorOn"))
                { set.colorOn = color; }
                if (tryGetColorFromConfig(troubleLogger, colorPalette, configReader, iniValue, Me, 
                    ref color, ref colorCoder, targetSection, $"Action{index}ColorOff"))
                { set.colorOff = color; }
                iniValue = configReader.Get(targetSection, $"Action{index}TextOn");
                if (!iniValue.IsEmpty)
                { set.textOn = iniValue.ToString(); }
                iniValue = configReader.Get(targetSection, $"Action{index}TextOff");
                if (!iniValue.IsEmpty)
                { set.textOff = iniValue.ToString(); }
                //That's it. We should have all the initial configuration for this ActionSet.
            }
            return set;
        }

        /* Scans the specified MyIni object for ActionSet configuration relating to other script
         *   objects
         * Dictionary evalSets: A dictionary containing the names and references for all the ActionSets
         *   we've loaded from config
         * Dictionary evalTriggers: A dictionary containing the names and references for all the Triggers
         *   we've loaded from config
         * MyIni configReader: The configuration we'll be reading. Should contain a parse of the PB's
         *   init section.
         * string targetSection: The name of the section we'll be reading from
         * int index: The index of the keys we'll be reading from
         * ActionSet set: The ActionSet we've already constructed from the config at this index
         * MyIniValue iniValue: A reference to a MyIniValue object, so we don't neeed a new one
         * LimitedMessageLog textLog: A reference to the message log that will hold our errors.
         * Returns: The ActionSet that was passed in, now filled with any configured ActionPlans.
         */
        private void readPBPlansFromConfig(Dictionary<string, ActionSet> evalSets,
            Dictionary<string, Trigger> evalTriggers, MyIni configReader, string targetSection,
            int index, ActionSet set, MyIniValue iniValue, LimitedMessageLog textLog)
        {
            Action<string> errorLogger = b => textLog.addError(b);
            //We'll start with ActionPlanUpdate.
            //DelayOn and DelayOff. These will actually be stored in an ActionPlan, but we
            //need to know if one of the values is present before we create the object.
            int delayOn = configReader.Get(targetSection, $"Action{index}DelayOn").ToInt32();
            int delayOff = configReader.Get(targetSection, $"Action{index}DelayOff").ToInt32();
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
            iniValue = configReader.Get(targetSection, $"Action{index}IGCChannel");
            if (!iniValue.IsEmpty)
            {
                string channel = iniValue.ToString();
                //Create a new action plan, using the string we collected as the channel
                ActionPlanIGC igcPlan = new ActionPlanIGC(IGC, channel);
                iniValue = configReader.Get(targetSection, $"Action{index}IGCMessageOn");
                if (!iniValue.IsEmpty)
                { igcPlan.messageOn = iniValue.ToString(); }
                iniValue = configReader.Get(targetSection, $"Action{index}IGCMessageOff");
                if (!iniValue.IsEmpty)
                { igcPlan.messageOff = iniValue.ToString(); }
                //Last step is to make sure we got some config
                if (igcPlan.hasAction())
                { set.addActionPlan(igcPlan); }
                else
                {
                    textLog.addError($"Action '{set.programName}' has configuration for sending " +
                        $"an IGC message on the channel '{channel}', but does not have readable " +
                        $"config on what messages should be sent.");
                }
            }

            //ActionPlanActionSet
            //The identifier that will be used to point the user to where an error is ocurring.
            string troubleID = "";
            List<KeyValuePair<string, bool>> parsedData = new List<KeyValuePair<string, bool>>();
            ActionSet targetSet = null;
            iniValue = configReader.Get(targetSection, $"Action{index}ActionSetsLinkedToOn");
            if (!iniValue.IsEmpty)
            {
                troubleID = $"ActionSet {set.programName}'s ActionSetsLinkedToOn list";
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
            iniValue = configReader.Get(targetSection, $"Action{index}ActionSetsLinkedToOff");
            if (!iniValue.IsEmpty)
            {
                troubleID = $"ActionSet {set.programName}'s ActionSetsLinkedToOff list";
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
            Trigger targetTrigger = null;
            iniValue = configReader.Get(targetSection, $"Action{index}TriggersLinkedToOn");
            if (!iniValue.IsEmpty)
            {
                troubleID = $"ActionSet {set.programName}'s TriggersLinkedToOn list";
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
            iniValue = configReader.Get(targetSection, $"Action{index}TriggersLinkedToOff");
            if (!iniValue.IsEmpty)
            {
                troubleID = $"ActionSet {set.programName}'s TriggersLinkedToOff list";
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
            //return set;
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

        public Action<IMyTerminalBlock> retrieveActionHandler(LimitedMessageLog textLog, string discreteTag, string target,
            IMyTerminalBlock block, MyIni iniReader, Dictionary<string, Action<IMyTerminalBlock>> actions)
        {
            //Check the config for the presence of the target key
            MyIniValue iniValue = iniReader.Get(discreteTag, target);
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
                    textLog.addWarning($"Block '{block.CustomName}', discrete section '{discreteTag}', " +
                        $"references the unknown action '{actionName}' as its {target}.");
                }
            }
            return retreivedAction;
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

        /* Scans the specified MyIni object for trigger configuration. Can return a functional trigger,
         *   a fake trigger and an error, or a null if no trigger config was found.
         * MyIni configReader: A MyIni object, loaded with the parse we're wanting to scan
         * MyIni saveReader: A MyIni object, loaded with a parse of the Storage string
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
        private void tryGetTriggerFromConfig(MyIni configReader, MyIni saveReader, Dictionary<string, Tally> evalTallies,
            Dictionary<string, ActionSet> evalSets, string targetSection, int index, MyIniValue iniValue,
            ref Tally targetTally, ref ActionSet targetSet, ref Trigger trigger, LimitedMessageLog textLog)
        {
            //From trigger configuration, we read:
            //Trigger<#>Name: The Element name of this Trigger
            //Trigger<#>Tally: The name of the Tally this trigger will watch
            //Trigger<#>ActionSet: The name of the ActionSet this trigger will operate
            //Trigger<#>LessOrEqualValue: When the watched Tally falls below this value, the 
            //  commandLess will be sent
            //Trigger<#>LessOrEqualCommand: The command to be sent when we're under the threshold
            //Trigger<#>GreaterOrEqualValue: When the watched Tally exceeds this value, the 
            //  commandGreater will be sent
            //Trigger<#>GreaterOrEqualCommand: The command to be sent when we're over the threshold
            trigger = null;
            string tallyName = "";
            targetTally = null;
            string setName = "";
            targetSet = null;

            string triggerName = configReader.Get(targetSection, $"Trigger{index}Name").ToString();
            if (!string.IsNullOrEmpty(triggerName))
            {
                //To make a new trigger, we need 3 additional pieces of information: A target tally, 
                //a target ActionSet, and an initial state.
                //First, Tallies
                tallyName = configReader.Get(targetSection, $"Trigger{index}Tally").ToString();
                if (!string.IsNullOrEmpty(tallyName))
                {
                    //Try to match the tallyName to a configured Tally
                    if (!evalTallies.TryGetValue(tallyName, out targetTally))
                    { textLog.addError($"Trigger '{triggerName}' tried to reference the unconfigured " +
                        $"Tally '{tallyName}'."); }
                }
                else
                { textLog.addError($"Trigger '{triggerName}' has a missing or unreadable Tally."); }

                //ActionSets
                setName = configReader.Get(targetSection, $"Trigger{index}ActionSet").ToString();
                if (!string.IsNullOrEmpty(setName))
                {
                    //Try to match the setName to a configured ActionSet
                    if (!evalSets.TryGetValue(setName, out targetSet))
                    { textLog.addError($"Trigger '{triggerName}' tried to reference the unconfigured " +
                        $"ActionSet '{setName}'."); }
                }
                else
                { textLog.addError($"Trigger '{triggerName}' has a missing or unreadable ActionSet."); }

                //We'll try to get the last known state of this trigger. If we can't tell, we'll arm it.
                bool initialState = saveReader?.Get("Triggers", triggerName).ToBoolean(true) ?? true;
                //If we've got the data we need, we'll make our new trigger.
                if (targetTally != null && targetSet != null)
                {
                    trigger = new Trigger(triggerName, targetTally, targetSet, initialState);
                    //Check for lessOrEqual and greaterOrEqual scenarios
                    tryGetCommandFromConfigOLD(configReader, trigger, targetSection, "LessOrEqual", index,
                        true, iniValue, textLog);
                    tryGetCommandFromConfigOLD(configReader, trigger, targetSection, "GreaterOrEqual", index,
                        false, iniValue, textLog);
                    //If I decide to allow customization of Trigger elements, that would go here.
                }
                if (!trigger.hasScenario())
                {
                    textLog.addError($"Trigger '{trigger.programName}' does not define a valid " +
                        $"LessOrEqual or GreaterOrEqual scenario.");
                }
            }
            else
            { }
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

        private void tryGetCommandFromConfigOLD(MyIni configReader, Trigger trigger, string targetSection,
            string prefix, int index, bool isLess, MyIniValue iniValue, LimitedMessageLog textLog)
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
                        textLog.addError($"Trigger {index} specifies a {prefix}Command of 'switch', " +
                            $"which cannot be used for triggers.");
                    }
                    else
                    {
                        textLog.addError($"Trigger {index} has a missing or invalid {prefix}Command. " +
                            $"Valid commands are 'on' and 'off'.");
                    }
                }
                else
                { textLog.addError($"Trigger {index} specifies a {prefix}Value but no {prefix}Command."); }
            }
        }

        private IReportable tryGetReportableFromConfig(LimitedMessageLog textLog, string prefix, string sectionTag,
            IMyTextSurface surface, IMyTerminalBlock block, MyIni iniReader, MyIniValue iniValue, IColorCoder colorCoder,
            Dictionary<string, Tally> evalTallies, Dictionary<string, ActionSet> sets, 
            Dictionary<string, Trigger> evalTriggers, Dictionary<string, IColorCoder> colorPalette)
        {
            Action<string> troubleLogger = b => textLog.addWarning(b);
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
                        else if (evalTriggers.ContainsKey(name))
                        { elementRefs.Add(evalTriggers[name]); }
                        else
                        {
                            //And complain, if appropriate.
                            textLog.addError($"Surface provider '{block.CustomName}', {prefix}" +
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
                /*
                //Foreground color
                if (tryGetColorFromConfig(textLog, colorPalette, iniReader, iniValue, block, 
                    ref color, colorCoder, sectionTag, $"{prefix}ForeColor"))
                { report.foreColor = color; }
                //Background color
                if (tryGetColorFromConfig(textLog, colorPalette, iniReader, iniValue, block,
                    ref color, colorCoder, sectionTag, $"{prefix}BackColor"))
                { report.backColor = color; }*/
                //Columns. IMPORTANT: Set anchors is no longer called during object
                //creation, and therefore MUST be called before the report is finished.
                iniValue = iniReader.Get(sectionTag, $"{prefix}Columns");
                //Call setAnchors, using a default value of 1 if we didn't get 
                //configuration data.
                report.setAnchors(iniValue.ToInt32(1), _sb);

                //We've should have all the available configuration for this report. Now we'll point
                //Reportable at it and move on.
                reportable = report;
            }
            //If this is a GameScript, it will have a 'Script' key.
            else if (iniReader.ContainsKey(sectionTag, $"{prefix}Script"))
            {
                GameScript script = new GameScript(surface,
                    iniReader.Get(sectionTag, $"{prefix}Script").ToString());
                /*
                //Foreground color
                if (tryGetColorFromConfig(textLog, ref color, prefix, "ForeColor", sectionTag,
                    iniReader, block))
                { script.foreColor = color; }
                //Background color
                if (tryGetColorFromConfig(textLog, ref color, prefix, "BackColor", sectionTag,
                    iniReader, block))
                { script.backColor = color; }*/
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
                { broker = new LogBroker(_log); }
                else if (type == "storage")
                { broker = new StorageBroker(this); }
                //CustomData, DetailInfo, CustomInfo, and Raycasters need to have a data source
                //specified.
                //CustomData, DetailInfo, and CustomInfo all get their data from blocks.
                else if (type == "customdata" || type == "detailinfo" || type == "custominfo")
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
                        else if (subject != null && type == "custominfo")
                        { broker = new CustomInfoBroker(subject); }
                        //If we didn't find a block, complain.
                        else
                        {
                            textLog.addError($"Surface provider '{block.CustomName}', {prefix}, tried " +
                                $"to reference the unknown block '{source}' as a DataSource.");
                        }
                    }
                    //If there is no data source, complain.
                    else
                    {
                        textLog.addError($"Surface provider '{block.CustomName}', {prefix}, has a " +
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
                        if (_raycasters.ContainsKey(source))
                        { broker = new RaycastBroker(_raycasters[source]); }
                        //If we didn't find matching raycaster, complain.
                        else
                        {
                            textLog.addError($"Surface provider '{block.CustomName}', {prefix}, tried " +
                                $"to reference the unknown Raycaster '{source}' as a DataSource.");
                        }
                    }
                    //If there is no data source, complain.
                    else
                    {
                        textLog.addError($"Surface provider '{block.CustomName}', {prefix}, has a " +
                            $"DataType of {type}, but a missing or unreadable DataSource.");
                    }
                }
                else
                //If we don't recognize the DataType, complain.
                {
                    textLog.addError($"Surface provider '{block.CustomName}', {prefix}, tried to " +
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
                    //The new customInfo is basically the same thing, so we'll add it to the list
                    {
                        if (type == "detailinfo" || type == "custominfo")
                        {
                            textLog.addError($"Surface provider '{block.CustomName}', {prefix}, tried to " +
                                $"set a CharPerLine limit with the {type} DataType. This is not allowed.");
                        }
                        else
                        { wall.setCharPerLine(iniValue.ToInt32()); }
                    }
                    /*
                    //Foreground color
                    if (tryGetColorFromConfig(textLog, ref color, prefix, "ForeColor", sectionTag,
                        iniReader, block))
                    { wall.foreColor = color; }
                    //Background color
                    if (tryGetColorFromConfig(textLog, ref color, prefix, "BackColor", sectionTag,
                        iniReader, block))
                    { wall.backColor = color; }*/
                    //One last thing: If this is a log, we want to know where it lives.
                    if (type == "log")
                    { _logReports.Add(wall); }
                    //Send this WallOText on its way with a fond fairwell.
                    reportable = wall;
                }
            }

            //One last step: All of our reportables have fore and back colors. If we actually have
            //a reportable at this point, we should see if the user wants to set them to something.
            if (reportable != null)
            {
                //Foreground color
                if (tryGetColorFromConfig(troubleLogger, colorPalette, iniReader, iniValue, block, ref color,
                    ref colorCoder, sectionTag, $"{prefix}ForeColor"))
                { ((IHasColors)reportable).foreColor = color; }
                //Background color
                if (tryGetColorFromConfig(troubleLogger, colorPalette, iniReader, iniValue, block, ref color,
                    ref colorCoder, sectionTag, $"{prefix}backColor"))
                { ((IHasColors)reportable).backColor = color; }
            }

            //Send our shiny new reportable on its way.
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
            { }

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
            { }

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
                statusColor = Hammers.cozy;
            }
            /*
            //Set the color code mode
            internal void setColorCoder(ColorCoderBase)
            {
                if (isLow)
                { colorHandler = Hammers.handleColorCodeLow; }
                else
                { colorHandler = Hammers.handleColorCodeHigh; }
            }
            */
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
                { config += $"Max = {max}\n"; }
                if (multiplier != DEFAULT_MULTIPLIER)
                { config += $"Multiplier = {multiplier}\n"; }

                return config;
            }

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

            internal override string writeConfig(int index)
            {   //name type max? displayName multiplier lowgood
                //Default values for this config. 
                /*
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
                { config += $"Tally{index}LowGood = true\n"; }*/

                //TallyGenerics have very little specific config, and getting it is straightforward.
                string config = $"Type = {handler.writeConfig()}\n";
                //We expect generics to use ColorCoderHigh. If it doesn't, we need to make a note.
                if (!(colorCoder is ColorCoderHigh))
                { config += $"ColorCoder = {colorCoder.getConfigPart()}\n"; }
                //Certain handlers may have their own config. Tack that on.
                config += $"{handler.getHandlerConfig()}\n";

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

            internal override string writeConfig(int index)
            {
                /*
                //name type max? displayName multiplier lowgood
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

                return config;*/
                
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

            internal override string writeConfig(int index)
            {
                /*
                //name type max? displayName multiplier lowgood
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

                return config;*/

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
                colorOn = Hammers.yellow;
                colorOff = Hammers.red;
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

        //ActionPlans only need one thing in common. Or at least, they did. Now we're getting all 
        //sensitive about things, and we need to know how to ask them if they have an action. And 
        //now we card them.
        public interface IHasActionPlan
        {
            void takeAction(bool isOnAction);
            bool hasAction();
            string getIdentifier();
        }

        //Stores a series of actions for ActionSet on and off states, and the block to apply them to.
        public class ActionPlanBlock : IHasActionPlan
        {
            //The TerminalBlock this ActionPlan will be manipulating
            IMyTerminalBlock subjectBlock;
            //The actions to be performed on the subject block when the ActionPlan is switched on
            internal List<Action<IMyTerminalBlock>> actionsOn { get; set; }
            //The actions to be performed on the subject block when the ActionPlan is switched off
            internal List<Action<IMyTerminalBlock>> actionsOff { get; set; }

            public ActionPlanBlock(IMyTerminalBlock subject)
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

                foreach (Action<IMyTerminalBlock> action in appropriateActions)
                { action.Invoke(subjectBlock); }
            }

            //Determine if this ActionPlan has any actions defined
            public bool hasAction()
            { return actionsOn?.Count > 0 || actionsOff?.Count > 0; }

            public string getIdentifier()
            { return $"Block '{subjectBlock.CustomName}'"; }
        }
        /*
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
        }*/

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
                    /*
                    if (subjectSet.hasActed)
                    {
                        Exception e = new InvalidOperationException();
                        e.Data.Add("Counter", 0);
                        throw e;
                    }*/
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

        public class ActionSet : IHasElement
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
            string statusText;
            
            public ActionSet(string name, bool initialState)
            {
                actionPlans = new List<IHasActionPlan>();
                displayName = name;
                programName = name;
                isOn = initialState;
                hasActed = false;
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
            {
                /* DEPRECEATED: PB plans will now be at the front of the list because they're on 
                 * the PB. This check is unneeded.
                //Because we need them for writeConfig, we make sure PB-based ActionPlans are at the 
                //top of the list.
                if (plan is ActionPlanUpdate || plan is ActionPlanIGC || plan is ActionPlanTrigger)
                { actionPlans.Insert(0, plan); }
                else
                { actionPlans.Add(plan); }
                */
                actionPlans.Add(plan);
            }

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
                Color DEFAULT_COLOR_ON = Hammers.green;
                Color DEFAULT_COLOR_OFF = Hammers.red;
                string DEFAULT_TEXT_ON = "Enabled";
                string DEFAULT_TEXT_OFF = "Disabled";
                //Strings that hold the state lists for the ActionSets and Triggers controlled by
                //this set.
                /*
                string setPlansOn = "";
                string setPlansOff = "";
                string triggerPlansOn = "";
                string triggerPlansOff = "";
                */
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
                                { setPlansOn.Add(configPartPlan.getConfigPart());  }
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

                return config;
            }
            /*
            public string writeConfig(int index)
            {
                //Default values for this config. 
                Color DEFAULT_COLOR_ON = Hammers.green;
                Color DEFAULT_COLOR_OFF = Hammers.red;
                string DEFAULT_TEXT_ON = "Enabled";
                string DEFAULT_TEXT_OFF = "Disabled";
                //Strings that hold the state lists for the ActionSets and Triggers controlled by
                //this set.
                string setPlansOn = "";
                string setPlansOff = "";
                string triggerPlansOn = "";
                string triggerPlansOff = "";
                //The string that will hold our finished config.
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

                //The config for an ActionSet can actually contain config for up to four types of
                //ActionPlans. They should all be at the front of the plan list, usually in this order:
                //ActionSet, Trigger, Update, IGC
                int planCounter = 0;
                IHasActionPlan currPlan = null;
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
                        //ActionSet and Trigger plans provide config parts, and will require a bit
                        //more work on this end.
                        else if (currPlan is IHasConfigPart)
                        {
                            if (currPlan is ActionPlanTrigger)
                            {
                                ActionPlanTrigger triggerPlan = (ActionPlanTrigger)currPlan;
                                if (triggerPlan.isOn())
                                { triggerPlansOn += $"{triggerPlan.getConfigPart()}, "; }
                                else
                                { triggerPlansOff += $"{triggerPlan.getConfigPart()}, "; }
                            }
                            //If it isn't a trigger plan, it's an ActionSet plan. Or I've changed
                            //something fundemental and forgotten to update this code. 
                            else
                            {
                                ActionPlanActionSet setPlan = (ActionPlanActionSet)currPlan;
                                if (setPlan.isOn())
                                { setPlansOn += $"{setPlan.getConfigPart()}, "; }
                                else
                                { setPlansOff += $"{setPlan.getConfigPart()}, "; }
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
                if (!String.IsNullOrEmpty(setPlansOn))
                //There will be a comma and space at the end of the config that we don't need.
                { config += $"Action{index}ActionSetsLinkedToOn = {setPlansOn.Remove(setPlansOn.Length - 2)}\n"; }
                if (!String.IsNullOrEmpty(setPlansOff))
                { config += $"Action{index}ActionSetsLinkedToOff = {setPlansOff.Remove(setPlansOff.Length - 2)}\n"; }
                if (!String.IsNullOrEmpty(triggerPlansOn))
                { config += $"Action{index}TriggersLinkedToOn = {triggerPlansOn.Remove(triggerPlansOn.Length - 2)}\n"; }
                if (!String.IsNullOrEmpty(triggerPlansOff))
                { config += $"Action{index}TriggersLinkedToOff = {triggerPlansOff.Remove(triggerPlansOff.Length - 2)}\n"; }

                return config;
            }*/
        }

        public static string newLineToMultiLine(string entry)
        {
            //If we're going to do a multiline, put each part on its own line.
            if (entry.Contains("\n"))
            { entry = $"\n|{entry.Replace("\n", "\n|")}"; }
            return entry;
        }

        public static string listToMultiLine(List<string> elements, int elementsPerLine = 3)
        {
            int elementsThisLine = 0;
            string outcome = "";
            //Multiline config always starts on the line after the key, with the newLine symbol. If
            //we can already tell that we're going to have multi-line config, best start off right.
            if (elements.Count > elementsPerLine)
            { outcome = "\n|"; }

            foreach (string element in elements)
            {
                if (elementsThisLine >= elementsPerLine)
                {
                    outcome += "\n|";
                    elementsThisLine = 0;
                }
                outcome += $"{element}, ";
                elementsThisLine++;
            }
            //Trim the trailing comma and space.
            outcome = outcome.Remove(outcome.Length - 2);
            return outcome;
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
                /*StringBuilder _sb = new StringBuilder("");*/
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
                        /*
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
                        */
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
            //List<IMyBatteryBlock> subjects;

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

                /*
                IMyBatteryBlock prospect = block as IMyBatteryBlock;
                if (prospect == null)
                { return false; }
                else
                {
                    subjects.Add(prospect);
                    return true;
                }*/
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
                foreach (IMySlimBlock block in subjects)
                { curr += block.BuildIntegrity - block.CurrentDamage; }
                return curr;
            }

            public string getHandlerConfig()
            { return ""; }

            public string writeConfig()
            { return "Block"; }
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
        }
    }
}

///<status> <date> </date>
///  <content>
///     
///  </content>
///</status>
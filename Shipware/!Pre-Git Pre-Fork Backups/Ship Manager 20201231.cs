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
    //ARCHIVE: Created just prior to trimming out a bunch of commented-out code to save on character
    //count.

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

    ///<OutstandingBugs>
    ///  <entry><title>Gas Tanks have Inventories</title>
    ///    Currently, IMyGasTanks are excluded from the Inventory sorter, even though they do have
    ///    inventories. This is because, in the current setup, sorters assume that if they're running,
    ///    the tallies listed on a grid block are compatible with them. And so they try to add the 
    ///    hydrogen tank's container to a gas tally, which doesn't work. Maybe a special case just
    ///    before the error would be thrown to see if the tally that's causing problems is a gas 
    ///    tally?
    ///   <update>
    ///    Fix applied. GasTanks still can't be added to a CargoTally, but at least everything else
    ///    appears to be working.
    ///   </update>
    ///   <status>Potential fix applied. Monitoring for unforseen consequences.</status>
    ///  </entry>
    ///  <entry><title>Empty Group Causes Infinite Loop</title>
    ///    If you create a tally by configuring it on the PB, but none of the blocks on the grid
    ///    are in that tally, the script will finish evaluation and then throw a script complexity
    ///    error on the first update. Cause unknown, but there're only so many loops...
    ///    Actually. Does this happen with TallyCargos, or just Generics?
    ///    Yes, yes it does. So it isn't something new.
    ///   <update>
    ///    If 'max' is set manually, this bug does not occur.
    ///   </update>
    ///   <status>Cause unknown</status>
    ///  </entry>
    ///  <entry><title></title>
    ///  </entry>
    ///</OutstandingBugs>

    ///<status> <date> </date>
    ///  <content>
    ///     
    ///  </content>
    ///</status>

    ///<status> <date>20201231</date>
    ///  <content>
    ///     Guess it's time for the MFD evaluator. Don't know why I always feel so aprehensive 
    ///   about writing these... Anyway. I think the best approach will be starting with a seperate
    ///   structure for the MFDs, based off the existing code for surfaces. Once I get that working,
    ///   I'll look for ways to merge the two to share common code (Or create a specialized method 
    ///   that works with both, as in evalGenerics). Right now, I'm thinking that I might be able to 
    ///   set up regular surfaces to use a pre-generated prefix (Surface0), then use that in place
    ///   of the prefixes based on the MFD's name.
    ///     Okay, added a lobe to the Surface sorter to handle MFDs, added data holding for MFDs to
    ///   the script and an update() entry for them, and added 'run' command handlers which should
    ///   let you navigate them. That just leaves the slow, tedious process of configuring a surface
    ///   to show one of these things so I can see if it's actually working. Maybe this is why I get 
    ///   apprehensive about writing sorters...
    ///  </content>
    ///</status>

    ///<status> <date>20201230</date>
    ///  <content>
    ///     I don't know if I knew and forgot, or just now figured this out, but: The zero X of a
    ///   surface is on the left side of the screen, and increasing the X moves you to the right.
    ///   The zero Y of a surface is on the /top/, and increasing the Y moves you down. That's why
    ///   I have to deduct from a sector center to get the text sprites to line up properly: they're
    ///   anchored at the top-center, and the sector center is too low for them. I have to move them 
    ///   up, and I do that by reducing the Y.
    ///     That's a really fundemental thing to just be figuring out now.
    ///     Okay, with the aid of my newfound but blindingly obvious understanding of the cartesian 
    ///   plane, I was able to re-write setAnchor and its supporting methods. It now properly 
    ///   displays titles and other elements accross every screen (5) that I've tried it on, so I
    ///   figure it's pretty much complete (Until I have to rip it apart to accomodate whatever 
    ///   it's going to take to display ActionSet states). Guess that means it's time to start in
    ///   on the rest of the MFD implementation.
    ///     I've decided to store the members of an MFD in a dictionary. That means that I'll need
    ///   to generate a title for the ones that aren't given one. Something along the lines of 
    ///   <MFDName><Page#>. 
    ///     Okay, think I've got the MFD object ready. Kinda weirding me out how quickly and easily
    ///   it came together. In fairness, that's probably because it's mostly re-arranging functionallity
    ///   I've already implemented elsewhere. And also because I haven't noticed this stuff that's 
    ///   wrong with it yet. Anyway, next step is to set up the surface sorter for it...
    ///  </content>
    ///</status>

    ///<status> <date>20201229</date>
    ///  <content>
    ///     Man, all that talk yesterday about order of operations with setAnchors was really spot
    ///   on. As it turns out, the order I had in place was causing a lot of problems that I was 
    ///   blaming on SE's surface weirdness. 
    ///     Long story short: My notion for adjusting anchor positions based on the size of the 
    ///   sprite was a good one. Unfortunately, because I was always calling setAnchor /before/ I
    ///   knew for certain what font or font size were going to be used for the element, the anchor
    ///   adjustment for elements that weren't using the defaults were always wrong.
    ///     So I have a couple of options. One is to take setAnchor out of the constructor and move
    ///   it to the last call I make before finishing with the report, making sure that it's always
    ///   called at least once, even if no configuration data is found. This could lead to exceptions
    ///   when I inevatbly forget that I need to make that call, probably frustrating ones that 
    ///   would be difficult to track down because the problem would be in 'known good code'. 
    ///     Alternatively, I could keep the call to setAnchors in the constructor and store the 
    ///   number of columns that would be used, then call setAnchors again any time a change is made
    ///   that would affect the grid layout. This would be safest, but it could lead to up to three 
    ///   (Four?) redundant calls to setAnchor, per surface. Granted that this is in evaluation, not
    ///   runtime, and that setAnchor is mostly just doing math (Even if it does contain a loop). 
    ///   Still, this seems wasteful.
    ///     Actually, there's a third option. I could just check for configuration data before I
    ///   create the object, and pass everything in with the constructor. It'd mean allocating more
    ///   variables during evaluation, though.
    ///     Right now... I'm leaning towards the first option. I'll just need to do a bunch of 
    ///   documentation to remind myself that there's a way things need to be.
    ///     As for setAnchor itself, it'll need some adjustments to make room for any titles that
    ///   may be floating around. What I may do is figure out the first anchor, adjusting for the
    ///   viewport offset and the title space, then derive the next anchor by moving right by 
    ///   rowWidth pixels, and the next anchor by the same process, but using the anchor I just
    ///   calculated as a starting point. More of a recursive approach than what I have now. I
    ///   think I'd still need to keep track of a GridX (Maybe rename as columnCounter?), so I 
    ///   know when I need to move down a row.
    ///     So, implemented that. Think it would probably work (Though I need to adjust rowHeight
    ///   based on how tall the title is). Only problem is, because I apply the modifier for element
    ///   height at the beginning and never again, it'll only work correctly if every element is the
    ///   same height. So I may have to go back to the drawing board. Maybe store the unadjusted 
    ///   grid sector center for figuring out where to go next, and modify the final version of the
    ///   anchor from that sector center?
    ///  </content>
    ///</status>

    ///<status> <date>20201228</date>
    ///  <content>
    ///     Not much today. Been hashing out some of the MFD related stuff. I don't think I'd quite
    ///   worked it out before, but all of the surface configuration information (Colors, etc) will 
    ///   need to be stored in each IReportable object. Also, I think I'll have to set a Report's
    ///   title at object construction, or actually store the number of columns in the report object.
    ///   Because I need to know if there's a title in order to properly set up the anchors, or I 
    ///   need to know how many columns there are to re-draw the anchors after I set the title. I 
    ///   guess I could also make sure that I scan the config for the title before I scan for the 
    ///   anchors, but that seems like a great way to make trouble for future-me-who-has-forgotten-
    ///   that-there-was-a-specific-order-to-this.
    ///     Actually, that won't work either, because I make the call to setAnchors with a default 
    ///   of 3 in the constructor, and can't guarantee that it'll ever be called after that.
    ///     So. Setting title during construction it is.
    ///  </content>
    ///</status>

    ///<status> <date>20201226</date>
    ///  <content>
    ///     MFD'able interface that requires objects to have an update() method that draws the 
    ///   content of the report (WriteText-based reports would only do this when the text changes)
    ///   and a profile() method that configures a surface to display the contents of a report. 
    ///   This is where I could do things like setting a surface to be a certain in-game script.
    ///     Object types implementing the MFD'able interface would be: The existing Report, a new
    ///   class based on WriteText, and a new class that is basically just a profile for switching
    ///   to one of the ingame scripts.
    ///  </content>
    ///</status>

    ///<status> <date>20201225</date>
    ///  <content>
    ///     MFD controls in-game surface properties, can change display types between things like
    ///   script and text, can set for in-game scripts, can modify surface colors? Maybe configure
    ///   with a 'Surface0MFD =', followed by the titles of the MFD screens, as opposed to the 
    ///   ususal 'Surface0Tallies'. Then configuration for each report would be based on the title
    ///   of that MFD page, 'StoresTallies=', 'PowerTallies=', etc. Actually, it would probably be
    ///   better to have a seperate place to set the title of the pages, so that you can use short
    ///   identrifiers for each one. But how do I set it up to have data pushed to it, as in the 
    ///   case of a Raycast report?
    ///     For the report anchors: If I found the center point of an element's area on the grid,
    ///   then moved up by half of the height of the sprite that would be displayed there, would
    ///   that line them up properly?
    ///     Answer: Sort of. I ended up moving /down/ from the centerpoint, which looks good on most
    ///   screens (Why down? I have no idea.) It's still a little low on, for instance, the Command
    ///   Chair's main screen.
    ///     For the title: Maybe if I modified the viewport in setAnchors to instead be an 'available
    ///   surface' variable. Then I could remove enough Y from that to make room for the title, and 
    ///   do the rest of my calculations based on it.
    ///     Thought: ActionSets may actually end up storing their state in Save().
    ///     Thought: Include a 'Template' command that fills the Populate section of the PB with 
    ///   one of the following templates: Subject, Indicator, Surface, MFD. The template would contain
    ///   all possible keys, with comments explaining what they do and what kinds of data they want.
    ///     Thought: If need be, I could make an 'error' class that stores a type, block, TallyName,
    ///   TallyType, and maybe an additional string for things like parse errors. Then I could 
    ///   actually write out all the errors in one place at the end of evaluation, and maybe save 
    ///   some characters. But hopefully it won't come to that.
    ///  </content>
    ///</status>

    ///<status> <date>20201224</date>
    ///  <content>
    ///     Don't need to dither about figuring out what to do today! Ima go bug-runnin'.
    ///     Think I'll start with why PowerProvider and Raycast aren't correctly returning currents
    ///   Some of the other isues look like they'll feed into each other.
    ///     Wait, actually, I may not even need to turn anything on for this. When I set one of
    ///   these up in the old manager, I had a magic number multiplying everything by 1000, because
    ///   the number that comes out of MaxPower is a decimal. Let's see what happens when I apply 
    ///   a multiplier...
    ///     As for raycast: I may not have even put configuration on the block.
    ///     Right on both counts. Both tallies are now operating properly, though I do kind of 
    ///   wonder if I should build the 1k multiplier into PowerProducer...
    ///     I can confirm that manually setting a max on a tally prevents the script complexity 
    ///   error of having no blocks. Now I just need to figure out why...
    ///     I can also confirm that I can add batteries to volume tallies. Also LCDs and projectors,
    ///   but not ore detectors, thrusters, or vents, apparently it has to draw the line somewhere.
    ///   The weirdest part of this is I mispelled the name of the tally on the PB, so everything I
    ///   have with configuration is pointing at something that doesn't exist... and it isn't 
    ///   complaining. Rather, the blocks that I can assign to any tally type I want are also not
    ///   complaining about being assigned to an un-configured tally.
    ///     I wonder if evalGeneric is correctly reporting errors? Though that wouldn't explain the 
    ///   LCD, or Projector... Actually, both of those implement IMySurfaceProvider (Why on the 
    ///   projector, I have no idea. Maybe for the console?). So the report sorter recognizes them.
    ///     Okay, changed evalGeneric to explicity take a reference to the error string instead of 
    ///   just the string itself, and now things are working. Don't know how I was getting into all
    ///   that trouble with duplicating error messages if it wasn't taking a reference before...
    ///     Something that came up during this is that Batteries also implement IMyPowerProducer.
    ///   Which I guess I hadn't picked up on because evalGeneric wasn't properly throwing errors.
    ///   May have to put it at the bottom of the list with a 'handled' check as well... Which will
    ///   be difficult, given that the place where I'd want to put that check is just before the 
    ///   'type incompatible' message, and PowerProducers use evalGeneric...
    ///     Moved the PowerProducer handler to the bottom of the list and put a global '!handled'
    ///   check on it, which I think I can get away with in this case because there isn't a ton of
    ///   overlap with other blocks. Moved the Inventory sorter to the very bottom of the list,
    ///   moved setting the 'handled' flag to the end of the sorter, added checks of the 'hanled'
    ///   flag before the sorter decides if it should complain matching to a TallyCargo. That seems
    ///   to have addressed the 'Hydrogen tank is a hydrogen tank and also has an inventory' issue.
    ///   Added a block type check in the inventory sorter before the complaint about missing or
    ///   unreadable Tallies, because surface providers and lights will not have that key. That 
    ///   seems to have addressed the 'Why does this cockpit not have a tally I don't care that 
    ///   you're using it as a display' issue.
    ///     Of course, I still can't add a hydrogen tank to a CargoTally (Because the Gas sorter is
    ///   catching it, then complaining that it doesn't know what to do with it). Still, this is an 
    ///   improvement.
    ///     I think that's all the bugs I found yesterday squashed, apart from the potential lead 
    ///   on the no max complexity error. Maybe I'll try to run it down tommorrow, along with making
    ///   some modifications to Report
    ///     Info: Sprites are indeed anchored top-center, probably explaining why I've never been
    ///   able to get the vertical alignment quite right.
    ///     Thought: Should I include the ability to title a report? A sprite that would be 
    ///   displayed at the top of the screen? Actually, an ideal would be to have dividers between
    ///   each column that could be labled, but how, even? Maybe instead of 'tallies', use 
    ///   'Row0Title' and 'Row0Elements'? There again is the problem of how to handle lines, if I
    ///   ever do that. Though, on that topic, I could use the foreground color of the surface to
    ///   determine the color of the title... And this could be quite helpful with the MFD.
    ///     Thought: Could I make it so that the event log could be sent to places other than the 
    ///   PB's DetailInfo? I mean, it's just a string, right? Of course, there's a lot that can go
    ///   wrong before you even get to grid evaluation, which is where surface targetting would take
    ///   place. Unless... maybe I could make a seperate check? Maybe with its own section tag, 
    ///   to make sure it's fast? I could even just mirror the DetailInfo of the PB to the surface,
    ///   that way it could display stuff like complexity errors. Wait, no, it'd crash before it'd 
    ///   get to the mirror. Guess the only answer would be to make my script airtight so those 
    ///   sorts of things don't happen.
    ///  </content>
    ///</status>

    ///<status> <date>20201223</date>
    ///  <content>
    ///     Made some tweaks to the file here in an attempt to remember what bugs I'm leaving alone
    ///   for the time being.
    ///     Today, I think I'll add evaluation sorters for the rest of the generics that I've 
    ///   implemented so far, then test to see if they're working properly (They /should/, they're
    ///   based on working code). After that... I haven't decided. I could do the the support code
    ///   for the Raycaster (And there's a thought: how am I going to specify a screen for raycast 
    ///   ouput? Maybe I need to begin immediate work on the MFD. And an IReporter interface to go
    ///   with it?). 
    ///     I could also work on the ActionSet implementation. I think one of the big things with 
    ///   it (After writing the evaluators, which I assume will be epic) is figuring how to display
    ///   the status of an ActionSet in the same Report objects that Tallies are displayed in. One
    ///   idea for that is... basically the same thing I'm doing everywhere else: implement a common
    ///   interface for Tallies and Actionsets (IReportable would make the most sense, though I'm
    ///   really starting to lean towards IHasElement), turn assembleElement into a Func I pass in
    ///   at the same time I pass in a Tally to a report, and have that Func cast the IReportable
    ///   into a form it can deal with. Alternatively, I could make IReportable require methods for
    ///   getElement1, getElement2, getElement3, and then have Report call those and arrange them 
    ///   however it wants. But, again, loss of flexibility.
    ///     Maybe I could make SetAnchors modular as well? The current implementation would become
    ///   GridAnchors, and I could write a new LineAnchors. I could maybe even have a CustomAnchors
    ///   that would allow you to specify your own anchor coordinates, somehow (As per usual, the 
    ///   problem with that idea is mostly 'how do I make this something I can evaluate from 
    ///   CustomData?)
    ///     Also: I still haven't done anything with the EventLog's subtitle. Although... I guess 
    ///   I haven't really implemented any of the functions that were going to use it, either.
    ///     Also: I'm kinda coming back around to the notion of having a list of all the tallies
    ///   at the top of the config, and then having the entries for them be like, 'PowerType' and
    ///   'PowerMax'
    ///     Anyway. Enough spculating, let's see how these other sorters catch fire.
    ///     Pulled out the old Sparrow MK2 and plugged the script in. Results:
    ///     -So, I kinda think it's letting me add batteries to a Cargo tally... Why would it be
    ///   doing that?
    ///     -The cockpit is complaining that it's an inventory block with no tallies, which is 
    ///   accurate, but that's because I'm using it as a display. Y'know, maybe I should do the
    ///   inventory sorter last, and only have it pitch a fit if the 'handled' flag isn't set?
    ///     -Weirdly, what /isn't/ pitching a fit is the tallies that are still empty. By sheer
    ///   coincidence, I put blocks in all the tallies but Solar, Raycast, and SwatterAmmoPort.
    ///   And it's running just fine. No complexity error. Important, but I'll sort through it
    ///   later.
    ///     -The PowerProvider handler doesn't seem to be working properly. At least, I'm getting
    ///   nothing but 0s from it.
    ///     -Raycast may not be working either, but it's hard to tell. Actually... I'm going to
    ///   need to do raycast setup in the raycast sorter anyway, may as well start... Okay, even
    ///   with raycast being enabled on the block every time evaluation runs, I'm getting a zero
    ///   for current charge
    ///     -The anchors being off is really noticable.
    ///     -THIS IS SO COOL. Seriously, even with the problems, even without Populate, even with
    ///   stumbling my way through unfamiliar settings and having to check the code itself to figure
    ///   out how I was supposed to format the configuration, I had the whole thing setup in about 
    ///   30 minutes. And when I saw a problem, I could address it immediately. It's awesome!
    ///     Thought: Rename script as 'Shipware'?
    ///     Point of concern: I'm at 88,000/100,000 characters at the moment. That's with a fair 
    ///   ammount of dead code still floating around, but still. I may end up needing to cut the
    ///   comments out of this after all.
    ///  </content>
    ///</status>

    ///<status> <date>20201222</date>
    ///  <content>
    ///     On hydrogen tanks having inventories: Right now, I'm thinking I'll just add an 
    ///   exception to the sorter for inventories and move on. I'll want to come back later and do
    ///   a more permanent solution, but I think I'll wait until I have a few more of these floating
    ///   around, colliding with more edge cases.
    ///     Before I do that, though, I want to see if I can run down this 6x error bug. It's wierdly
    ///   specific and therefore intruiging.
    ///     Man, this is bizarre. Booted it up today and got 4 error messages about the test tank.
    ///   Added an alt tank and got 10 error messages, some complaining about the test tank and some
    ///   about the alt tank... in a pattern of aaaabaaaab. Added a third tank, and now the pattern 
    ///   is aacaacaacaacbaacaacaacaacb. Recompile with the same data produced the same error pattern.
    ///     Okay, tried something different. Tried to add a refinery to the Hydrogen tally. That 
    ///   produced two errors, which may be consistent with the number of inventories. Actually, 
    ///   adding a gatling gun also produced two errors, so I'm getting that consistently. Maybe 
    ///   one when I run the check for a Tallies key and another when I run the check for a Tally0?
    ///   Nope, added a diferentiating section to the error messages, both were generated by the 
    ///   Tallies section.
    ///     Okay, went to a new grid. With just one tally, and one block in that tally, I get one 
    ///   incompatability error. Adding an additional block with two inventories produces two errors, 
    ///   each correctly referencing the offending block. Splitting that second block into its two
    ///   individual inventories producing three errors, one for the first block, and one for each 
    ///   inventory of the second block (Though the error message could make that clearer).
    ///     Aha. I tried to add a Hydrogen tank to the tally, and now I'm getting the duplicate
    ///   errors. But why? There is a loop, one for every element of the delimited string 'tallies',
    ///   but if that were the problem, wouldn't I be getting all the block errors in... blocks? As
    ///   opposed to some of one, then one of the other, then back to the first? Also, wouldn't I 
    ///   be getting the same number each time?
    ///     ... Are the blocks getting added to the evaluation list multiple times, somehow?
    ///     Okay. Added a log entry just before the evaluation list gets sorted, and it confirmed
    ///   that the expected number of blocks were being evaluated.
    ///     But I got the message /twice/. Why the /hell/ would this be running /twice/?
    ///     Okay, added a debugging int that gets incremented just prior to the block entry being
    ///   added to the log. Both times the message appears in the log, it has the same number (1).
    ///   So... apparently the evaluation section isn't running twice, but the log is getting 
    ///   posted multiple times? Do I have another call to it, somewhere?
    ///     ...I'm starting to get a suspicion.
    ///     <siiiiiiiiiiiiiiiiigh>
    ///     Okay. When I first started writing evaluateGeneric, I wasn't quite sure how I would 
    ///   handle things. Instead of making it return void, I made it return string, and that string
    ///   would be errors. I also made the call to it concatenate the existing error string with
    ///   whatever came out of evaluateGeneric. Problem is, I'm not building a new error string in
    ///   the method and tacking it on to the existing string on the way out. I'm passing the 
    ///   existing string in and adding any new errors to it directly. And because of how I had it
    ///   written, I was /also/ concatenating my error string with whatever came out of the method -
    ///   which was the error string. Thus explaining... practically everything, except why I was 
    ///   getting 6 errors the first day and 4 the second.
    ///     I think the takeaway here is that, if I have oddly repeating output like this, I should
    ///   look for a place where I'm combining something with itself.
    ///     Anyway. Fixed now. And so passes another productive morning.
    ///     The hydrogen tally seems to work, by the way.
    ///  </content>
    ///</status>

    ///<status> <date>20201221</date>
    ///  <content>
    ///     I think I'll try using a var. Maybe var typedBlock in evaluation and, when I come to a
    ///   section where I'd normally be making a lot of casts, instead use typedBlock as IMyWhatever.
    ///     Yeah, so, that didn't work. I remain suspicious of vars... Though I'm most likely just
    ///   not using them right.
    ///     Maybe what I'll do instead is write some methods to handle the repeating portions of 
    ///   evaluating TallyCargos and TallyGenerics. If I get those working to my satisfaction, I can
    ///   try converting them into local lambdas.
    ///     Got evaluateGeneric working. Passed the first test, batteries, with flying colors. Then
    ///   I tried some hydrogen tanks and realized I was going to need to pass the currHandler in 
    ///   along with everything else. Worth noting, I got spammed with errors when I tested it out
    ///   on one hydrogen tank. Got six errors all saying the same thing, when I should've gotten 
    ///   one. Unless I already have other tanks flagged for use with Capacity on the grid? Nope, 
    ///   just tried it after re-naming that one tank. Got six errors all citing it as the problem.
    ///   Maybe... am I getting an error for each block in blocks, for some reason? Apparently not,
    ///   added a bit more telemetry to the 'brag' message at the end of evaluate, and the script
    ///   references 12 blocks before the hydrogen tank is added.
    ///     Welp, I'm dumb. The key was right there in the error message: The hydrogen tanks are 
    ///   getting caught by the inventory sorter, because they have inventories. And the inventory
    ///   sorter is not happy about trying to work with the Hydrogen tally.
    ///     This is going to take some thinking. I can't just say, 'if is IMyCargoBock', because I
    ///   want to be able to check things that aren't cargo blocks, like turrets. And, yeah, maybe 
    ///   even the inventories of hydrogen tanks. So... do I have explicit keys for GenericTallies
    ///   and CargoTallies?
    ///     Thought: I could probably modify TallyGeneric to take a class, then store its blocks as 
    ///   that class. But I'm pretty sure I couldn't write the currHandlers in such a way that they
    ///   could just assume that they're being handed blocks of the right type, so the cast would
    ///   still be required.
    ///  </content>
    ///</status>

    ///<status> <date>20201219</date>
    ///  <content>
    ///     Nothing's jumping out at me on the no-block script complexity error. The only loops are 
    ///   in calculateCurr and drawMeter... unless, the error isn't in compute() at all, and is 
    ///   instead in one of the other loops in Main(). Maybe clearCurr? Don't see how that could be
    ///   the case... Anyway, I think I'm going to leave it for now. It is something I'll need to
    ///   run down, because it isn't unreasonable to assume that there will be a time when the user
    ///   has an empty tally during configuration. But for now, I want to get to testing other stuff.
    ///     I did tweak readableInt to take a reference to a string instead of generating one. That 
    ///   required a re-write in Tally (Because of the auto getter/setter thing) which, in turn,
    ///   required a re-write in Report.
    ///  </content>
    ///</status>

    ///<status> <date>20201218</date>
    ///  <content>
    ///     I'm looking through some of the stuff about lambdas and closures again. In spite of my
    ///   tests yesterday and the conclusions I reached, I've been seeing a lot about how C# 
    ///   captures variables for use in delegates. In fact, in one of the oft-cited articles 
    ///   (https://csharpindepth.com/Articles/Closures), the writer uses a variable declared outside
    ///   of the lambda in the lambda itself. He invokes the lambda once, changes the variable in
    ///   the main method, then invokes the lambda again, yielding a different result.
    ///     ...So, apparently I'm doing something wrong. But, the code I have at the moment seems
    ///   to be working. So maybe I won't jostle it.
    ///     Anyway. Looking things over, I think that my Inventory sorter may work for both 
    ///   TallyCargo and TallyItem. Because TallyItem extends TallyCargo. As for the name of un-
    ///   intialized tallies not showing up in the error messages... Nothing's jumping out at me
    ///   on that one.
    ///     Man, I must've been really off my game yesterday. In trying to run down the uninitialized
    ///   tally name thing, I've discovered that all un-initialized tallies are being flagged as
    ///   incompatible with TallyCargo. Which makes sense, when you think about it, but seriously,
    ///   what did I do yesterday that made me think this was working? Anyway, fix wasn't hard. Just
    ///   switched back to using ContainsKey instead of TryGetValue and the logic flowed a lot 
    ///   more... logically. I did not take this as an opportunity to try the lambda again, because
    ///   I realized that I'd have to re-declare it every time the foreach looped in order to use
    ///   the name variable, or declare it at the top along with a variable that could be set to
    ///   whatever name was. Maybe some other time. Oh, and whatever I did seems to have fixed the
    ///   missing name in the 'un-initialized tally' error... assuming that was a thing in the 
    ///   first place.
    ///     Yeesh, now I remember why I was creating fake tallies when there was a problem reading
    ///   data from the PB. If you don't, every block on the grid that references that tally 
    ///   complains. So I switched that back. Though I'm not quite sure why that was happening. The
    ///   script isn't even supposed to look at the grid unless there were no errors from parsing 
    ///   the PB. Was the break; somehow firing before the error got added to the log? Or... maybe
    ///   I was breaking out of something other than the PB evaluation loop? On the plus side, I've
    ///   confirmed that checking 'is TallyCargo' catches TallyItem as well.
    ///     Okay, new problem. I think the errors, or at least one of the errors, in the PB 
    ///   evaluation isn't being logged properly. Something to do with TallyItems, because I just
    ///   realized they're all just reporting the volume of the container they're pointed at.
    ///     Well, I was sort of right. Because there are two things that could go wrong with the 
    ///   creation of a TallyItem, I gave them each their own individual checks, then check again
    ///   at the end of that statement to see if I have the data I need. If I do, I create a 
    ///   TallyItem. If I don't, I create a TallyCargo, and assume that the error(s) have already
    ///   been logged. Except there used to be /three/ piece of data I needed, the third being Max,
    ///   and I was still checking to see if I got it. Which I never do, because I took the code
    ///   that previously set it somewhere else. Which explains both why my ItemTallies aren't 
    ///   initialzing properly and why I was getting spammed with error messages about uninitialized
    ///   tallies.
    ///     Good times. At least things seem to be working now, but I should try to remember this
    ///   in case I run into another situation where a bunch of tallies are reporting the same 
    ///   thing.
    ///     Started on grid evaluation for TallyGenerics. If I keep going like this, there's going
    ///   to be a /lot/ of duplicate code. Good opportunity for a local or external method.
    ///     Okay, for some reason, just adding 'TallyName = Power\nTallyType = Power' to the PB is
    ///   causing a script complexity error. And doing it after evaluation, not during.
    ///     ...Actually, now that I think about it, I wonder if having no blocks at all in a tally 
    ///   is causing issues with the compute() method?
    ///     ...And that seems to be the case. Added one battery to the tally, now it works fine. So...
    ///   What? Need to check at the end of evaluation to make sure all the tallies are actually 
    ///   pointing at something? Or maybe re-write compute so it doesn't pitch a fit if it doesn't
    ///   get anything? Actually, why is it pitching a fit now? Isn't it a foreach?
    ///     The loop in the handlePower is indeed a foreach. A double is initialized to 0 then, for 
    ///   every battery in the block list, that double is incremented. Then the double is returned.
    ///   So... Shouldn't that be looking at the list, noticing there's no blocks, and returning a
    ///   zero? Maybe the problem is somewhere else, one of the other methods in compute() that 
    ///   does't like having a zero passed in... Maybe drawMeter()?
    ///     Before I forget: Tallies and ActionSets could implement a common IHasElement interface
    ///   that Reports could use to figure out how to display them. Of course, I'd lose flexibility
    ///   doing that. Reports were supposed to be the end where you could decide to display things 
    ///   in a line, for instance.
    ///  </content>
    ///</status>

    ///<status> <date>20201217</date>
    ///  <content>
    ///     Didn't get a ton done yesterday. I kind of broke off during an existential crisis about
    ///   whether or not I should use a local function (Meaning a local lambda, because local 
    ///   functions aren't allowed) for some of the duplicate code in inventory grid evaluation. 
    ///     ... And, honestly, I'm still a little conflicted. 
    ///     Okay, did what I should've done yesterday and ran some tests. I used this code:
    ///     
    ///     string errors = "Beginning String";
    ///     IMyTerminalBlock block = Me;
    ///     Func<IMyTerminalBlock, string, string> stringTest = (incomingBlock, incomingString) =>
    ///     {
    ///         incomingBlock = null;
    ///         incomingString += "\nTacked String";
    ///         return incomingString;
    ///     };
    ///     errors = stringTest(block, errors);
    ///     Echo($"{block.CustomName}\n{errors}");
    ///     
    ///     And what it echo'd was the name of the programmable block and the concatenated string
    ///   (After I ironed out a few problems resulting from me being a dumbass). Some reading on
    ///   this issue (Which is a bit high level for me, unfortunately) seems to indicate that this
    ///   is because lambdas use 'closures', which apparently involves creating a new object to
    ///   store the data that you're passing in. Or something. Point is, handing a lambda function
    ///   an object and expecing it to have the reference won't work, and I can't explicitly tell
    ///   it that I'm passing something in by reference in this version of C, and now I have a new 
    ///   and exciting existential crisis because I'm wondering if all the handler functions I'm 
    ///   using throughout this script are behaving like I expect them to.
    ///     And it's only 10:00. What a productive morning.
    ///     Alright, worked on things a bit more. Tracked down and fixed all the compile errors, 
    ///   got things in a state where I could plug it into the PB and test it.
    ///     It failed.
    ///     I'm getting all sorts of runtime errors about converting from TallyGenerics to TallyCargos
    ///   and vice-versa. I'm thinking that this has something to do with my idea of creating a 
    ///   tally during PB evaluation in an attempt to let the rest of the script run. Maybe I should
    ///   switch back to break;?
    ///     Alright, that wasn't the problem. Problem was when I was doing the check to see if the 
    ///   tally was of a sort that needed to have its max set, I wasn't checking to see if it was a
    ///   tallyGeneric before trying to cast it. But... I've decided to leave the breaks in, as 
    ///   opposed to the crutch TallyCargos.
    ///     While I was at it, I did a bit of testing on the iffy logic I'm using in inventory 
    ///   evaluation. It seems to hold up - it correctly matched an existing tally, detected when
    ///   an inventory was being fed to a non-TallyCargo tally (CHECK- What about TallyItem?), and
    ///   and complained when it couldn't find the tally in question. Only oddity - the name of the
    ///   tally the script was looking for in that last case didn't show up in the error message.
    ///   And I really have no idea why.
    ///     Thought: Populate may need a -remove command, to go along with -add and -ow. That would
    ///   look at the keys in the Populate section of the PB and, if those keys found in the 
    ///   CustomData of the blocks in the populate group, whatever data is specified on the PB would
    ///   be removed from the blocks, if it is present.
    ///  </content>
    ///</status>

    ///<status> <date>20201216</date>
    ///  <content>
    ///     It occurred to me today that TallyGenerics need to have blocks added to them during
    ///   evaluation. That means I'll need to store them seperately. Or possibly in a seperate list
    ///   along side a list of all tallies, which I use to see if a tally has been defined. Or I 
    ///   stick with the single list, and make a check each time I try to add a block, throwing some
    ///   sort of incompatibility error if it isn't, in fact, a TallyGeneric that the block is 
    ///   being added to.
    ///  </content>
    ///</status>

    ///<status> <date>20201215</date>
    ///  <content>
    ///     Alright, gonna need a backup before I start tearing into the CargoManager.
    ///     Okay, so: Plan right now is to replace CargoManager with a TallyManager. The 
    ///   TallyManager would take arrays holding all the containers (Originally stored in a list) 
    ///   and all the tallies (Stored in a Dictionary) at the end of grid evaluation. During 
    ///   execution, TallyManager would first call calculateCurr() on its containers (Using a 
    ///   foreach loop, which should cost nothing if there're no containers). Then, it'll call 
    ///   compute() on all the tallies. Cargo and Item tallies will already have their grid data
    ///   from the call to calculateCurr, and the generic tallies will calculate theirs 
    ///   individually.
    ///     Okay, so, started work on that. I got about as far as starting on the actual grid 
    ///   evaluation, then decided that I should implement a system by which the user could set the
    ///   max of any tally, not just TallyItems (It was functionallity I was going to need for a 
    ///   TallyGeneric with a PowerOutput handler). So I decided I should move the check for having
    ///   max set out of tallyItem and to a place where I can do it once. Then I decided that I 
    ///   needed a new system for figuring out if max had been set manually, something that would
    ///   make it easy to recognize if I needed to ignore calls to incrementMax(). Then I really
    ///   got sidetracked and decided to make an abstract Tally object for all of my various Tallies
    ///   to inherit from, as opposed to having them implement an ITally interface.
    ///     I think it'll all be for the best in the long run. But I'll probably panic when I boot
    ///   this up tommorrow and see that half the project is coming back red.
    ///     Just need to remember and go back and take out the old check for Max in TallyItem 
    ///   creation.
    ///     Thought: I could make it so that the user could set an arbitrary max on any tally they
    ///   want, not just TallyItems. I could either do this by flipping the sign of the value when
    ///   max is deliberately set, then flipping it back when finalize() is called. Or... I could
    ///   just permanently store a forcedMax bool. Which I wouldn't need outside of initialization,
    ///   but is also much more readable, and costs exactly one bit.
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
    ///     I'm just going to switch this script back to using Capacity tags for the moment because 
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
        //CargoManager cargo;
        //TEST CODE? Arrays that store the containers and tallies that this script watches. May 
        //eventually be folded into a TallyManager.
        Container[] containers;
        Tally[] tallies;
        //The reports that tell about what the cargo manager is doing.
        Report[] reports;
        //MFDs that do the same thing as reports, only fancier. MFDs are store in a dictionary, to
        //facilitate controlling them by name.
        Dictionary<string, MFD> MFDs;
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
                //TEST CODE?
                foreach (Tally tally in tallies)
                { tally.clearCurr(); }
                foreach (Container container in containers)
                { container.calculateCurr(); }
                foreach (Tally tally in tallies)
                { tally.compute(); }
                //END TEST CODE?
                //WRITE ALL THE REPORTS
                foreach (Report report in reports)
                { report.update(); }
                //DISPLAY ALL THE MFDs
                foreach (MFD display in MFDs.Values)
                { display.update(); }
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
                        //Controls an MFD
                        //Argument format: MFD <name> <command>
                        //Argument example: MFD MainScreen Next
                        case "MFD":
                            string MFDTarget = argReader.Argument(1);
                            string MFDCommand = argReader.Argument(2).ToLowerInvariant();
                            //If we actually know what MFD the user is talking about...
                            if (MFDs.ContainsKey(MFDTarget))
                            {
                                //If it's one of the easy commands...
                                if (MFDCommand == "next")
                                { MFDs[MFDTarget].flipPage(true); }
                                if (MFDCommand == "last")
                                { MFDs[MFDTarget].flipPage(false); }
                                //If it isn't one of the easy commands, assume the user is trying 
                                //to set the MFD to a specific page.
                                else
                                {
                                    //If the MFD declines to set the page to the specified target...
                                    if (!MFDs[MFDTarget].setPage(MFDTarget))
                                    {
                                        //... Complain.
                                        log.add($"Received command to set MFD '{MFDTarget}' to unknown" +
                                            $"page '{MFDCommand}'.");
                                    }
                                }
                            }
                            //If we don't know what MFD the user is talking about, complain.
                            else
                            { log.add($"Received '{MFDCommand}' command for un-recognized MFD '{MFDTarget}'."); }
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
            //We'll need the ability to move data around during evaluation. A list will suffice for
            //reports and containers, but we'll need a dictionary to make the tallies and indicators 
            //work.
            List<Container> evalContainers = new List<Container>();
            Dictionary<string, Tally> evalTallies = new Dictionary<string, Tally>();
            List<Report> evalReports = new List<Report>();
            Dictionary<string, Indicator> evalIndicators = new Dictionary<string, Indicator>();
            //We'll need to pass the GTS around a bit for this. May as well put an easy handle on it.
            IMyGridTerminalSystem GTS = GridTerminalSystem;
            //A couple of extra variables for working directly with MyIni
            MyIniParseResult parseResult = new MyIniParseResult();
            MyIniValue iniValue = new MyIniValue();
            //We'll need to do some configuration on tallies before we send them on their way. Let's
            //use an easy handle for it.
            Tally tally;
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
            string tallyType = "";
            string MFDName = "";
            string addIn1 = "";
            string addIn2 = "";
            //The tallies a block reports to are stored in a delimited string. We'll need something
            //to hold those as something easier to work with.
            string[] tallyNames;
            //We'll need lists for our various tally types
            List<TallyCargo> cargoRefs;
            List<Tally> tallyRefs;
            //And a list specifically for the pages of an MFD
            Dictionary<string, IReportable> pages;
            //The ubiquitous list of terminal blocks.
            List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

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
            //Tally<#>Max: For TallyItems, the arbitrary number that will serve as the maximum for
            //  this tally.
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
                    errors += $" -The Tally name '{tallyName}' is reserved by the script to indicate" +
                          $"where portions of the screen should be left empty. Please choose a " +
                          $"different name.";
                }
                //Now then. Did we get a tally name?
                else if (!string.IsNullOrEmpty(tallyName))
                {
                    //Our next steps are going to be dictated by the TallyType. We should try and 
                    //figure out what that is.
                    tallyType = iniReader.Get(SECTION_TAG, $"Tally{counter}Type").ToString();
                    //If no type is defined...
                    if (string.IsNullOrEmpty(tallyType))
                    {
                        //... complain.
                        errors += $" -Tally {tallyName} has a missing or unreadable TallyType.\n";
                        //Also, create a TallyCargo. This will let the rest of the script execute
                        //as normal, and hopefully prevent 'uninitialized tally' spam
                        tally = new TallyCargo(tallyName);
                    }
                    //Now, we create a tally. The creation of a TallyCargo is quite straightforward.
                    else if (tallyType == "Volume")
                    { tally = new TallyCargo(tallyName); }
                    //Creating a TallyItem is a bit more involved.
                    else if (tallyType == "Item")
                    {
                        //We'll need a TypeID. We'll use the first AddIn string to store it
                        addIn1 = iniReader.Get(SECTION_TAG, $"Tally{counter}ItemTypeID").ToString();
                        //If we can't get it, complain.
                        if (string.IsNullOrEmpty(addIn1))
                        { errors += $" -Item Tally '{tallyName}' has a missing or unreadable TallyItemTypeID.\n"; }
                        //And a SubTypeID, stored in AddIn2
                        addIn2 = iniReader.Get(SECTION_TAG, $"Tally{counter}ItemSubTypeID").ToString();
                        if (string.IsNullOrEmpty(addIn2))
                        { errors += $" -Item Tally '{tallyName}' has a missing or unreadable TallyItemSubTypeID.\n"; }
                        //If we have the data we were looking for, we can create a TallyItem
                        if (!string.IsNullOrEmpty(addIn1) && !string.IsNullOrEmpty(addIn2))
                        { tally = new TallyItem(tallyName, addIn1, addIn2); }
                        //If we're missing data, we'll just create a TallyCargo so the script can 
                        //continue.
                        else
                        { tally = new TallyCargo(tallyName); }
                    }
                    //Power and the other TallyGenerics are only marginally more complicated than
                    //Volume
                    else if (tallyType == "Power")
                    { tally = new TallyGeneric(tallyName, handlePower); }
                    //Gas, which works for both Hydrogen and Oxygen
                    else if (tallyType == "Gas")
                    { tally = new TallyGeneric(tallyName, handleGas); }
                    //JumpCharge
                    else if (tallyType == "JumpCharge")
                    { tally = new TallyGeneric(tallyName, handleJumpCharge); }
                    //Raycst
                    else if (tallyType == "Raycast")
                    { tally = new TallyGeneric(tallyName, handleRaycast); }
                    //MaxOutput
                    else if (tallyType == "PowerOutput")
                    { tally = new TallyGeneric(tallyName, handlePowerOutput); }
                    //TODO: Aditionally TallyTypes go here
                    else
                    {
                        //If we've gotten to this point, the user has given us a type that we can't 
                        //recognize. Scold them.
                        errors += $" -Tally {tallyName}'s TallyType of '{tallyType}' cannot be handled" +
                            $"by this script. Be aware that TallyTypes are case-sensitive.\n";
                        //...Also, create a TallyCargo, so the rest of Evaluate will work.
                        tally = new TallyCargo(tallyName);
                    }
                    //Now that we have our tally, we need to check to see if there's any further
                    //configuration data. 
                    //First, the DisplayName
                    iniValue = iniReader.Get(SECTION_TAG, $"Tally{counter}DisplayName");
                    if (!iniValue.IsEmpty)
                    { tally.name = iniValue.ToString(); }
                    //Then the Max
                    iniValue = iniReader.Get(SECTION_TAG, $"Tally{counter}Max");
                    if (!iniValue.IsEmpty)
                    { tally.forceMax(iniValue.ToDouble()); }
                    //There's a couple of TallyTypes that need to have a Max explicitly set (All 
                    //TallyItems, plus the TallyGenerics PowerProducer and Raycast). If that hasn't 
                    //happened, we need to complain.
                    else if (iniValue.IsEmpty && (tally is TallyItem || (tally is TallyGeneric && 
                        (((TallyGeneric)tally).currHandler == handlePowerOutput || 
                        ((TallyGeneric)tally).currHandler == handleRaycast))))
                    {
                        errors += $" -Tally {tallyName}'s TallyType of '{tallyType}' requires a Max " +
                            $"to be set in configuration.\n";
                    }
                    //Up next is the Multiplier
                    iniValue = iniReader.Get(SECTION_TAG, $"Tally{counter}Multiplier");
                    if (!iniValue.IsEmpty)
                    { tally.multiplier = iniValue.ToDouble(); }
                    //Last, LowGood
                    iniValue = iniReader.Get(SECTION_TAG, $"Tally{counter}LowGood");
                    if (!iniValue.IsEmpty)
                    { tally.lowGood(iniValue.ToBoolean()); }
                    //That's all the data we can glean from here. It's time to put this tally
                    //somewhere the rest of Evaluate can get to it.
                    evalTallies.Add(tallyName, tally);
                    //Last step is to increment the counter, so we can look for the next tally.
                    counter++;
                }
                else
                //If we didn't find another tally, set the counter equal to -1 to indicate that 
                //we're done in this loop.
                { counter = -1; }
            }
            //If we don't have errors, but we also don't have any tallies...
            if (string.IsNullOrEmpty(errors) && evalTallies.Count == 0)
            { errors += " -No tally configuration found on the programmable block.\n"; }

            //Only if there were no errors with parsing the PB...
            if (string.IsNullOrEmpty(errors))
            {
                //...should we get the blocks on the grid with our section tag.
                errors += Hammers.findBlocks<IMyTerminalBlock>(GTS, blocks, b =>
                    (b.IsSameConstructAs(Me) && MyIni.HasSection(b.CustomData, SECTION_TAG)),
                    $" -No blocks found on this construct with a [{SECTION_TAG}] INI section.");
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
                //if it isn't completely relevant to how ShipManager operates.
                //In the CargoManager, the data is handled by two seperate yet equally important
                //objects: the Tallies that store and calculate information and the Reports that 
                //display it. These are their stories.

                //Each block that this script knows how to handle gets its own sorter. Those sorters
                //are:
                //The Programmable Block (Me), SurfaceProviders, Batteries, GasTanks, JumpDrives, 
                //PowerProviders, Cameras

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
                    //  name of the MFD that will be displayed on this surface.
                    //Surface<#>Title: (Default = "") The title of this report, which will appear at 
                    //  the top of its surface.
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
                            tallyRefs = new List<Tally>();
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
                                        errors += $" -Surface provider '{block.CustomName}', surface " +
                                            $"{i}, tried to reference the unconfigured tally " +
                                            $"'{tallyName}'.\n";
                                    }
                                }
                            }
                            //Create a new report with the data we've collected so far.
                            report = new Report(((IMyTextSurfaceProvider)block).GetSurface(i), tallyRefs);
                            //Now that we have a report, we need to see if the user wants anything 
                            //special done with it.
                            //Title
                            iniValue = iniReader.Get(SECTION_TAG, $"Surface{i}Title");
                            if (!iniValue.IsEmpty)
                            { report.title = iniValue.ToString(); }
                            //FontSize
                            iniValue = iniReader.Get(SECTION_TAG, $"Surface{i}FontSize");
                            if (!iniValue.IsEmpty)
                            { report.fontSize = iniValue.ToSingle(); }
                            //Font
                            iniValue = iniReader.Get(SECTION_TAG, $"Surface{i}Font");
                            if (!iniValue.IsEmpty)
                            { report.font = iniValue.ToString(); }
                            //Columns. IMPORTANT: Set anchors is no longer called during object
                            //creation, and therefore MUST be called before the report is finished.
                            iniValue = iniReader.Get(SECTION_TAG, $"Surface{i}Columns");
                            //Call setAnchors, using a default value of 3 if we didn't get 
                            //configuration data.
                            report.setAnchors(iniValue.ToInt32(3));

                            //All done? Last step is to add this report to our list of reports. So
                            //we'll know where it lives.
                            evalReports.Add(report);
                        }
                        //Are we supposed to be displaying an MFD on this surface?
                        else if (iniReader.ContainsKey(SECTION_TAG, $"Surface{i}MFD"))
                        {
                            //We'll want the name of the MFD to read the rest of the configuration
                            MFDName = iniReader.Get(SECTION_TAG, $"Surface{i}MFD").ToString();
                            //We'll need a dictionary to store the pages
                            pages = new Dictionary<string, IReportable>();
                            //We have no idea how many reports we'll need to display. That means it's
                            //time to bust out the counter.
                            counter = 0;
                            //TODO: This will need adjusting when I start setting it up for things 
                            //other than tallies. Maybe check for a 'type' key instead?
                            //As long as there's configuration data for this page of the MFD...
                            while (iniReader.ContainsKey(SECTION_TAG, $"{MFDName}{counter}Tallies"))
                            {
                                //Generate a name for this page of the report. We may over-ride it 
                                //later
                                addIn1 = $"{MFDName}{counter}";
                                //Get the tallies we're supposed to display. Store it in the iniValue 
                                //for now...
                                iniValue = iniReader.Get(SECTION_TAG, $"{MFDName}{counter}Tallies");
                                //...because there's a lot of stuff we need to do before it's ready.
                                //Split on the comma delimeter, trim away the whitespace of each entry 
                                //in the resulting array of strings. By the way, this is not my code.
                                tallyNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                                //We have names, but we need the actual tallies that go with them to 
                                //make our report.
                                tallyRefs = new List<Tally>();
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
                                            errors += $" -Surface provider '{block.CustomName}', MFD " +
                                                $"{MFDName}, page {counter}, tried to reference the unconfigured tally " +
                                                $"'{tallyName}'.\n";
                                        }
                                    }
                                }
                                //Create a new report with the data we've collected so far.
                                report = new Report(((IMyTextSurfaceProvider)block).GetSurface(i), tallyRefs);
                                //Now that we have a report, we need to see if the user wants anything 
                                //special done with it.
                                //Title
                                iniValue = iniReader.Get(SECTION_TAG, $"{MFDName}{counter}Title");
                                if (!iniValue.IsEmpty)
                                {
                                    //For an MFD, a specified title takes the place of the pre-
                                    //generated dictionary key.
                                    addIn1 = iniValue.ToString();
                                    report.title = addIn1;
                                }
                                //FontSize
                                iniValue = iniReader.Get(SECTION_TAG, $"{MFDName}{counter}FontSize");
                                if (!iniValue.IsEmpty)
                                { report.fontSize = iniValue.ToSingle(); }
                                //Font
                                iniValue = iniReader.Get(SECTION_TAG, $"{MFDName}{counter}Font");
                                if (!iniValue.IsEmpty)
                                { report.font = iniValue.ToString(); }
                                //Columns. IMPORTANT: Set anchors is no longer called during object
                                //creation, and therefore MUST be called before the report is finished.
                                iniValue = iniReader.Get(SECTION_TAG, $"{MFDName}{counter}Columns");
                                //Call setAnchors, using a default value of 3 if we didn't get 
                                //configuration data.
                                report.setAnchors(iniValue.ToInt32(3));

                                //All done? Last step is to add this report to our list of reports. So
                                //we'll know where it lives.
                                pages.Add(addIn1, report);
                                //Increment the counter for the next iteration.
                                counter++;
                            }
                            //That should be all the data available for this MFD. Add it to the list.
                            MFDs.Add(MFDName, new MFD(pages));
                        }
                            /* KNOWN GOOD
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
                                tallyRefs = new List<Tally>();
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
                                            errors += $" -Surface provider '{block.CustomName}', surface " +
                                                $"{i}, tried to reference the unconfigured tally " +
                                                $"'{tallyName}'.\n";
                                        }
                                    }
                                }
                                //Create a new report with the data we've collected so far.
                                report = new Report(((IMyTextSurfaceProvider)block).GetSurface(i), tallyRefs);
                                //Now that we have a report, we need to see if the user wants anything 
                                //special done with it.
                                //Title
                                iniValue = iniReader.Get(SECTION_TAG, $"Surface{i}Title");
                                if (!iniValue.IsEmpty)
                                { report.title = iniValue.ToString(); }
                                //FontSize
                                iniValue = iniReader.Get(SECTION_TAG, $"Surface{i}FontSize");
                                if (!iniValue.IsEmpty)
                                { report.fontSize = iniValue.ToSingle(); }
                                //Font
                                iniValue = iniReader.Get(SECTION_TAG, $"Surface{i}Font");
                                if (!iniValue.IsEmpty)
                                { report.font = iniValue.ToString(); }
                                //Columns. IMPORTANT: Set anchors is no longer called during object
                                //creation, and therefore MUST be called before the report is finished.
                                iniValue = iniReader.Get(SECTION_TAG, $"Surface{i}Columns");
                                //Call setAnchors, using a default value of 3 if we didn't get 
                                //configuration data.
                                report.setAnchors(iniValue.ToInt32(3));

                                //All done? Last step is to add this report to our list of reports. So
                                //we'll know where it lives.
                                evalReports.Add(report);*/
                    }
                }

                //Battery Sorter (Generic)
                if (parseResult.Success && block is IMyBatteryBlock)
                {
                    handled = true;
                    //From TallyGeneric blocks, we read:
                    //Tallies: The tallies this block should report to.
                    evaluateGeneric(ref errors, "Battery", b => (double)(((IMyBatteryBlock)b).MaxStoredPower), 
                        handlePower, block, iniReader, evalTallies);
                }
                
                //GasTank Sorter (Generic)
                if (parseResult.Success && block is IMyGasTank)
                {
                    handled = true;
                    evaluateGeneric(ref errors, "Gas Tank", b => (double)(((IMyGasTank)b).Capacity),
                        handleGas, block, iniReader, evalTallies);
                }

                //JumpDrive soter
                if (parseResult.Success && block is IMyJumpDrive)
                {
                    handled = true;
                    evaluateGeneric(ref errors, "Jump Drive", b => (double)(((IMyJumpDrive)b).MaxStoredPower),
                        handleJumpCharge, block, iniReader, evalTallies);
                }


                //Camera sorter
                if (parseResult.Success && block is IMyCameraBlock)
                {
                    handled = true;
                    //Raycasters have their maximum set manually. So, our maxHandler is a bit 
                    //different from the others.
                    evaluateGeneric(ref errors, "Raycaster", b => { return 0; },
                        handleRaycast, block, iniReader, evalTallies);
                    //TODO: A lot of the configuration for a Raycaster will probably need to go 
                    //right here.
                    ((IMyCameraBlock)block).EnableRaycast = true;
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
                        //If the tally is in our EvalTallies....
                        if (evalTallies.TryGetValue(tallyName, out tally))
                        {
                            //...we first need to see if it's already in the dictionary tracking
                            //Indicator light groups.
                            if (!evalIndicators.ContainsKey(tallyName))
                            //If it isn't, we add one.
                            { evalIndicators.Add(tallyName, new Indicator(tally));}
                            //Once we're sure there's an Indicator group in the dictionary, add 
                            //this light to it.
                            evalIndicators[tallyName].addLight((IMyLightingBlock)block);
                        }
                        //If we weren't able to find the tally in evalTallies, complain.
                        else
                        {
                            errors += $" -Lighting block '{block.CustomName}' tried to reference " +
                                $"the unconfigured tally '{tallyName}'. Note that lighting blocks can" +
                                $"only monitor one tally.\n";
                        }
                    }
                    else
                    {
                        errors += $" -Lighting block {block.CustomName} has missing or unreadable Tally." +
                            $"Note that lightling blocks use the 'Tally' key instead of the usual 'Tallies'.\n";
                    }
                }

                //PowerProvider sorter
                //TODO: Monitor the use of the 'handled' flag check, here. I think I can get away
                //with it because only 3 other block types inherit this interface. I do worry that 
                //power producers with inventories may end up causing me trouble, though.
                if (parseResult.Success && block is IMyPowerProducer && !handled)
                {
                    handled = true;
                    //Like the Raycaster, all PowerProducers have their maximums set manually.
                    evaluateGeneric(ref errors, "Power Producer", b => { return 0; },
                        handlePowerOutput, block, iniReader, evalTallies);
                }

                //There're a lot of blocks that have inventories, so we make this check last.
                if (parseResult.Success && block.HasInventory)
                {
                    //From inventory blocks, we read:
                    //Tallies: The tallies this inventory should report to.
                    //If there is no Tallies key, we need to check for:
                    //Tallies<#>: A set of tallies tied specifically to one of the block's inventories
                    iniValue = iniReader.Get(SECTION_TAG, "Tallies");
                    if (!iniValue.IsEmpty)
                    {
                        tallyNames = iniValue.ToString().Split(',').Select(p => p.Trim()).ToArray();
                        //Now we need to get the Tallies referenced by the strings in TallyNames
                        cargoRefs = new List<TallyCargo>();
                        foreach (string name in tallyNames)
                        {
                            //If this tally name is in evalTallies...
                            if (evalTallies.ContainsKey(name))
                            {
                                //...pull the tally out.
                                tally = evalTallies[name];
                                //If the tally is a TallyCargo...
                                if (tally is TallyCargo)
                                //...add it to our list of cargoRefs
                                { cargoRefs.Add((TallyCargo)tally); }
                                else if (!handled)
                                //If it isn't a TallyCargo and none of our other sorters knew what 
                                //to do with it, complain. 
                                {
                                    errors += $" -Inventory block '{block.CustomName}' is not " +
                                        $"compatible with the TallyType of tally '{name}'.\n";
                                }
                                //If it isn't a TallyCargo, but the 'handled' flag is set, fail
                                //silently in the hopes that one of the other sorters did indeed
                                //handle it.
                            }
                            else
                            //If the tally name isn't in evalTallies, complain.
                            {
                                errors += $" -Inventory block '{block.CustomName}' tried to " +
                                        $"reference the unconfigured tally '{name}'.\n";
                            }
                        }
                        //For configurations tied to the 'Tallies' key, we use the same set of 
                        //Tallies for every inventory on the block.
                        for (int i = 0; i < block.InventoryCount; i++)
                        {
                            evalContainers.Add(new Container(block.GetInventory(i), cargoRefs));
                            //We also need to increment the maximum of all the tallies we're adding
                            //to this container.
                            foreach (TallyCargo cargoTally in cargoRefs)
                            { cargoTally.incrementMax((double)(block.GetInventory(i).MaxVolume)); }
                        }
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
                                    //DUPLICATE CODE
                                    //If this tally name is in evalTallies...
                                    if (evalTallies.ContainsKey(name))
                                    {
                                        //...pull the tally out.
                                        tally = evalTallies[name];
                                        //If the tally is a TallyCargo...
                                        if (tally is TallyCargo)
                                        //...add it to our list of cargoRefs
                                        { cargoRefs.Add((TallyCargo)tally); }
                                        else if (!handled)
                                        //If it isn't a TallyCargo, complain. But only if the 
                                        //'handled' flag isn't already set
                                        {
                                            errors += $" -Inventory block '{block.CustomName}' Tallies{i}" +
                                                $" is not compatible with the TallyType of tally '{name}'.\n";
                                        }
                                    }
                                    else
                                    //If the tally name isn't in evalTallies, complain.
                                    {
                                        errors += $" -Inventory block '{block.CustomName}' Tallies{i} " +
                                            $"tried to reference the unconfigured tally '{name}'.\n";
                                    }
                                }
                                //...And then add this container with the tallies we've found.
                                evalContainers.Add(new Container(block.GetInventory(i), cargoRefs));
                                //We also need to increment the maximum of all the tallies we're adding
                                //to this container.
                                foreach (TallyCargo cargoTally in cargoRefs)
                                { cargoTally.incrementMax((double)(block.GetInventory(i).MaxVolume)); }
                                //We'll also increment the counter, to indicate that we successfully
                                //found some individual inventory configuration.
                                counter++;
                            }
                        }
                        //If the counter is still 0, and this is a block type that is supposed to 
                        //have configuration for 'Tallies'...
                        if (counter == 0 && !(block is IMyTextSurfaceProvider || block is IMyLightingBlock))
                        { errors += $" -Inventory block {block.CustomName} has missing or unreadable Tallies.\n"; }
                    }
                    //Because this sorter makes some of its decisions based on if a block has already 
                    //been handled, setting this flag is the last thing we do.
                    handled = true;
                }

                //If we made it here, but the block hasn't been handled, it's time to complain.
                if (parseResult.Success && !handled)
                { errors += $" -Block type of '{block.CustomName}' cannot be handled by this script.\n"; }

                //Set handled to 'false' for the next iteration of the loop.
                handled = false;
            }

            /*--- TEST CODE ---*/
            /*
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
            evalReports.Add(testReport);*/
            /*- END TEST CODE -*/

            //Time to finalize things. The first step is to tear down the complicated data structures
            //we've been using for evaluation into the arrays we'll be using during execution
            containers = evalContainers.ToArray();
            tallies = evalTallies.Values.ToArray();
            reports = evalReports.ToArray();
            indicators = evalIndicators.Values.ToArray();
            //There's one more step before the tallies are ready. We need to tell them that they
            //have all the data that they're going to get. Because the Cargo-based tallies don't
            //store their blocks internally, we'll start with finalizing the Containers.
            foreach (Container container in containers)
            { container.calculateMax(); }
            //Now we can finalize the Tallies
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
                log.add($"Grid evaluation complete. Registered {tallies.Length} tallies and " +
                    $"{reports.Length} reports, as configured by data on {blocks.Count} blocks.");
                log.add("Setup complete. Script is now running.");
            }
            else
            {
                log.add($"Grid evaluation complete. The following errors are preventing script " +
                    $"execution:\n{errors}");
            }
        }

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
        private void evaluateGeneric(ref string errors, string friendlyName, Func<IMyTerminalBlock, double> maxHandler,
            Func<List<IMyTerminalBlock>, double> currHandler, IMyTerminalBlock block, MyIni iniReader, 
            Dictionary<string, Tally> evalTallies)
        {
            Tally tally;
            MyIniValue iniValue = iniReader.Get(SECTION_TAG, "Tallies");
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
                        //...add it to our list of cargoRefs
                        { genericRefs.Add((TallyGeneric)tally); }
                        else
                        //If it isn't a TallyGeneric with the correct handler, complain.
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
        }

        /*
        public interface ITally
        {
            string name { get; set; }
            double multiplier { set; }
            string readableCurr { get; }
            string readableMax { get; }
            Color code { get; }
            string getMeter();
            void lowGood(bool isLow);
            void forceMax(double max);

            //Other methods that all Tallies should probably have.
            void finishSetup();
            void clearCurr();
            void incrementMax(double max);
            AddInventoryToCurr //Ah. Ooops.
            void compute();
        }
        */

        //Interface used by things that need to be displayed by a report. Mostly tallies, but also
        //ActionSets
        public interface IHasElement
        { }

        public abstract class Tally
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
            public Color code { get; protected set; }
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
                code = Hammers.cozy;
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
                code = colorHandler(percent);
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
                code = colorHandler(percent);
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

        /*
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

            public TallyGeneric(string name, Func<List<IMyTerminalBlock>,double> handler, bool isLow = false, double multiplier = 1)
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

            public TallyItem(string name, string typeID, string subTypeID, double max = 0,
                bool isLow = false, double multiplier = 1) : base(name, isLow, multiplier)
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
        }*/

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
        /*
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
        }*/

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
                    { pageNumber = pages.Count; }
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

        public class Report : IReportable
        {
            //The surface we'll be drawing this report on.
            IMyTextSurface surface;
            //The tallies that this report will be pulling data from. We use an array because, at
            //this point, everything should be set.
            Tally[] tallies;
            //The points on the screen that sprites will be anchored to.
            Vector2[] anchors;
            //A float storing the font size used for displaying the Tallies in this report.
            public float fontSize { private get; set; }
            //A string storing the name of the font for displaying Tallies in this report.
            public string font { private get; set; }
            //The colors that this report wants to use for its foreground and background.
            public Color foreColor { private get; set; }
            public Color backColor { private get; set; }
            //The title of this particular report, which will be displayed at the top of the screen.
            public string title { get; set; }
            //The title gets its very own anchor.
            Vector2 titleAnchor;

            public Report(IMyTextSurface surface, List<Tally> tallies, string title = "", float fontSize = 1f, string font = "Debug")
            {
                this.surface = surface;
                //We won't be adding or removing tallies at this point, so we'll just pull the 
                //array out of the list and work with it directly.
                this.tallies = tallies.ToArray();
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
                anchors = new Vector2[tallies.Count];
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
                //
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
                int rows = (int)(Math.Ceiling((double)tallies.Count() / columns));
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
                for (int i = 0; i < tallies.Count(); i++)
                {
                    //If a tally is null, we can safely ignore it.
                    //TODO: Monitor. It /shouldn't/ pitch a fit about uninitiated anchors if it
                    //never has to use them, but you never can tell.
                    if (tallies[i] != null)
                    {
                        //Clear the contents of the StringBuilder
                        element.Clear();
                        //Force-feed it the string that we already have a perfectly good method for 
                        //building
                        element.Append(assembleElement(tallies[i]));
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
                /*
                    //If a tally is null, we can safely ignore it.
                    //TODO: Monitor. It /shouldn't/ pitch a fit about uninitiated anchors if it
                    //never has to use them, but you never can tell.
                    if (tallies[i] != null)
                    {
                        //Clear the contents of the StringBuilder
                        element.Clear();
                        //Force-feed it the string that we already have a perfectly good method for 
                        //building
                        element.Append(assembleElement(tallies[i]));
                        //Politely request the dimensions of the string we 'built'.
                        elementSize = surface.MeasureStringInPixels(element, font, fontSize);
                        //To find the X value of this anchor, go to the middle of the current column.
                        //To find the Y value of this anchor, start by going to the center of the 
                        //current row
                        anchor = new Vector2(columnWidth / 2 + columnWidth * gridX,
                            rowHeight / 2 + rowHeight * gridY);
                        //Because the surface system doesn't actually anchor things by their center,
                        //(Vertically, anyway), we adjust the anchor's Y downward by half of its 
                        //height. And somehow, that makes everything better. 
                        //(Everything I've read indicates text sprites are anchored center-top.
                        //Shouldn't I be moving this /up/, then, from the point at the center of
                        //this square on the grid?)
                        anchor.Y -= elementSize.Y / 2;
                        //Before we add this anchor to our list, adjust it based on the viewport 
                        //offset.
                        anchors[i] = anchor + viewport.Position;
                    }
                    //Move to the next column for the next tally
                    gridX++;
                    //If we've reached the last column, move down to the next row.
                    if (gridX >= columns)
                    {
                        gridX = 0;
                        gridY++;
                    }
                }
                /*
                //The number of rows we'll have is the number of elements, divided by how many 
                //columns we're going to display them across.
                int rows = (int)(Math.Ceiling((double)tallies.Count() / columns));
                float columnWidth = surface.SurfaceSize.X / columns;
                float rowHeight = surface.SurfaceSize.Y / rows;
                int gridX = 0, gridY = 0;
                Vector2 anchor, elementSize;
                for (int i = 0; i < tallies.Count(); i++)
                {
                    //If a tally is null, we can safely ignore it.
                    //TODO: Monitor. It /shouldn't/ pitch a fit about uninitiated anchors if it
                    //never has to use them, but you never can tell.
                    if (tallies[i] != null)
                    {
                        //Clear the contents of the StringBuilder
                        element.Clear();
                        //Force-feed it the string that we already have a perfectly good method for 
                        //building
                        element.Append(assembleElement(tallies[i]));
                        //Politely request the dimensions of the string we 'built'.
                        elementSize = surface.MeasureStringInPixels(element, font, fontSize);
                        //To find the X value of this anchor, go to the middle of the current column.
                        //To find the Y value of this anchor, start by going to the center of the 
                        //current row
                        anchor = new Vector2(columnWidth / 2 + columnWidth * gridX,
                            rowHeight / 2 + rowHeight * gridY);
                        //Because the surface system doesn't actually anchor things by their center,
                        //(Vertically, anyway), we adjust the anchor's Y downward by half of its 
                        //height. And somehow, that makes everything better. 
                        //(Everything I've read indicates text sprites are anchored center-top.
                        //Shouldn't I be moving this /up/, then, from the point at the center of
                        //this square on the grid?)
                        anchor.Y -= elementSize.Y / 2;
                        //Before we add this anchor to our list, adjust it based on the viewport 
                        //offset.
                        anchors[i] = anchor + viewport.Position;
                    }
                    //Move to the next column for the next tally
                    gridX++;
                    //If we've reached the last column, move down to the next row.
                    if (gridX >= columns)
                    {
                        gridX = 0;
                        gridY++;
                    }
                }*/
            }

            private string assembleElement(Tally tally)
            //I considered including a StringBuilder into this class to make this bit faster. But
            //apparently, if you do it all in one go, it's fast enough.
            { return $"{tally.name}\n{tally.getReadableCurr()} / {tally.getReadableMax()}\n{tally.getMeter()}"; }

            //Re-draws this report, pulling new information from its elements to do so.
            public void update()
            {
                //A handle for tallies we'll be working with
                Tally tally;
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

            //Prepare this surface for displaying the report.
            public void setProfile()
            {
                surface.ContentType = ContentType.SCRIPT;
                surface.Script = "";
                surface.ScriptForegroundColor = foreColor;
                surface.ScriptBackgroundColor = backColor;
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
            Tally tally;
            //The last color code set by this indicator. Used to make sure we're only changing all 
            //the light colors when we need to.
            Color oldColor;

            public Indicator(Tally tally)
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
        //Also: MaxOutput is in megawatts, while most PowerProducers generate power in the kilowatt
        //range. This handler will generally return a decimal.
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
                    output = new StringBuilder();
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

/*
 * Shipware Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=2681807135
 * Shipware Documentation: https://steamcommunity.com/sharedfiles/filedetails/?id=2776664161
 * 
 * THIS CODE CONTAINS NOTHING THAT SHOULD BE EDITED BY THE USER
 */
const double ύ=.802;const string ό="Shipware";const string ϋ="SW";const string ϊ="Dec";string ω;string ψ;const string χ=
"Source";const string φ="Target";ǟ υ;MyIni τ;MyIni σ;StringBuilder ž;Ǫ ς;m[]π;w[]ο;Dictionary<string,Ơ>ξ;Ȫ[]ν;Dictionary<string,
Ǻ>μ;ɝ[]λ;Ǌ[]κ;Dictionary<string,ɭ>ι;List<Ƽ>θ;IMyBroadcastListener η;Ė ζ;Ц ρ;Dictionary<string,Ц>ώ;bool ϖ;DateTime Ϡ;
string Ϟ;static Color ϝ=new Color(25,225,100);static Color Ϝ=new Color(100,200,225);static Color ϛ=new Color(255,255,0);static
Color Ϛ=new Color(255,150,0);static Color ϙ=new Color(255,0,0);static Color Ϙ=new Color(255,225,200);static Color ϗ=new Color
(0,0,0);Program(){Ɓ å;bool ϑ;ϟ(out å,out ϑ);if(ϑ){ϔ(å);}else{Ѷ(å);}Echo(υ.ǫ());}void ϟ(out Ɓ å,out bool ϑ){τ=new MyIni();
σ=new MyIni();ž=new StringBuilder();ϖ=false;Ϡ=DateTime.Now;å=new Ɓ(ž,15);ϑ=false;τ.TryParse(Storage);double ϕ=τ.Get(
"Data","Version").ToDouble(-1);ω=τ.Get("Data","ID").ToString(ό);ψ=$"{ϋ}.{ω}";η=IGC.RegisterBroadcastListener(ψ);η.
SetMessageCallback(ψ);υ=new ǟ(ž,$"Shipware v{ύ} - Recent Events",true);ς=new Ǫ(ž);if(ψ!=$"{ϋ}.{ό}"){υ.Ǟ=ψ;}ζ=new Ė();ώ=new Dictionary<
string,Ц>();τ.Clear();if(ϕ==-1){ϑ=true;}else if(ϕ!=ύ){å.Ɩ($"Code updated from v{ϕ} to v{ύ}.");}υ.Ǔ(
"Script initialization complete.");}void ϔ(Ɓ å){MyIniParseResult ϓ;string ϐ=$"{ϋ}.Init";if(!τ.TryParse(Me.CustomData,out ϓ)){å.ƕ(
$"Cannot generate a {ϐ} section because the parser encountered "+$"an error on line {ϓ.LineNo} of the Programmable Block's config: {ϓ.Error}");}else{ϒ(å,true);}å.Ɩ(
"Use the AutoPopulate command to generate basic configuration.");å.Ɩ("The Clone command can quickly distribute config across identical blocks.");å.Ɩ(
"The Evaluate command scans the grid for config and loads it into memory.");string ĩ=$"First run complete.\nThe following messages were logged:\n{å.Ƒ()}";if(å.ƞ()>0){ĩ+=
$"The following errors were logged:\n{å.Ɯ()}";}υ.Ǔ(ĩ);}void ϒ(Ɓ å,bool ϑ=false){string ϐ=$"{ϋ}.Init";bool Ϗ=τ.ContainsSection(ϐ);bool ε=Ϗ&&!ϑ;bool δ=false;string Ή=
Me.CustomName;if(!Ϗ&&!ϑ){å.Ɩ($"{ϐ} section was missing from block '{Ή}' and "+$"has been re-generated.");}string[]Μ=new
string[]{"ColorOptimal","ColorNormal","ColorCaution","ColorWarning","ColorCritical","MPSpriteSyncFrequency",
"APExcludedBlockTypes","APExcludedBlockSubTypes","APExcludedDeclarations"};string[]Λ=new string[]{"Green","LightBlue","Yellow","Orange","Red",
"-1",("MyObjectBuilder_ConveyorSorter, MyObjectBuilder_ShipWelder,\n"+"MyObjectBuilder_ShipGrinder"),(
$"StoreBlock, ContractBlock, {ϋ}.FurnitureSubTypes,\n"+$"{ϋ}.IsolatedCockpitSubTypes, {ϋ}.ShelfSubTypes"),("ThrustersGeneric")};bool[]Κ=new bool[]{false,false,false,false,
false,true,true,true,true};for(int ņ=0;ņ<Μ.Length;ņ++){ъ(ϐ,Μ[ņ],Λ[ņ],Κ[ņ],ref δ,ε,Ή,å);}if(δ){Me.CustomData=τ.ToString();}}
void Save(){τ.Clear();τ.Set("Data","Version",ύ);τ.Set("Data","ID",ω);int Ι=ζ.đ("UpdateDelay");τ.Set("Data","UpdateDelay",Ι==
-1?0:Ι);if(ξ!=null){foreach(Ơ Θ in ξ.Values){τ.Set("ActionSets",Θ.u,Θ.Ù);}}if(ν!=null){foreach(Ȫ Ğ in ν){τ.Set("Triggers"
,Ğ.u,Ğ.ȣ);}}if(ι!=null){foreach(ɭ Η in ι.Values){τ.Set("MFDs",Η.u,Η.ɪ);}}Storage=τ.ToString();τ.Clear();}void Main(string
Ζ,UpdateType Ν){MyCommandLine Ε=null;Func<string,bool>Γ=(Α)=>{Ε=new MyCommandLine();return(Ε.TryParse(Α));};Func<string,
bool>Β=(Α)=>{Ε=new MyCommandLine();return(Ε.TryParse(Α.ToLowerInvariant()));};if((Ν&UpdateType.Update100)!=0){ζ.ģ();}if((Ν&
UpdateType.Update10)!=0){if(ρ!=null){bool ΐ=ρ.н();υ.ǜ=ρ.П;if(!ΐ){if(ρ.О){υ.Ǔ(ρ.й());}ρ.к();ώ.Remove(ρ.Ф);ρ=null;}}else{if(ώ.Count>
0){ρ=ώ.Values.ElementAt(0);ρ.г();}else{Runtime.UpdateFrequency&=~UpdateFrequency.Update10;υ.ǜ="";}}}else if((Ν&UpdateType
.Once)!=0){Ѷ(new Ɓ(ž,15));}else if((Ν&UpdateType.IGC)!=0){while(η.HasPendingMessage){MyIGCMessage ŋ=η.AcceptMessage();if(
ŋ.Tag==ψ){string Ώ=ŋ.Data.ToString();if(Β(Ώ)){string ĩ="No reply";string Ύ=null;bool Ό=false;Action Ί=()=>{MyCommandLine.
SwitchEnumerator Δ=Ε.Switches.GetEnumerator();Δ.MoveNext();Ύ=Δ.Current;};if(Ε.Argument(0)=="reply"){Ό=true;Γ(Ώ);Ώ=Ώ.Replace(Ε.Argument(0
),"");Ώ=Ώ.Trim();if(Ε.Switches.Count==1){Ί();Ώ=Ώ.Replace($"-{Ύ}","");Ώ=Ώ.Trim();ĩ=$"Received IGC reply from {Ύ}: {Ώ}";}
else{ĩ=($"Received IGC reply: {Ώ}");}}else if(Ε.Argument(0)=="action"){if(Ε.ArgumentCount==3){ĩ=Ϫ(Ε.Argument(1),Ε.Argument(2
),"IGC-directed ");}else{ĩ=$"Received IGC-directed command '{Ώ}', which "+$"has an incorrect number of arguments.";}}else
if(Ε.ArgumentCount==1){ĩ=Ϫ(Ε.Argument(0),"switch","IGC-directed ");}else{ĩ=
$"Received the following unrecognized command from the IGC:"+$" '{Ώ}'.";}if(!Ό&&Ε.Switches.Count==1){Γ(Ώ);Ί();IGC.SendBroadcastMessage(Ύ,$"reply {ĩ} -{ψ}");ĩ+=
$"\nSent reply on channel {Ύ}.";}υ.Ǔ(ĩ);}else{υ.Ǔ($"Received IGC-directed command '{Ώ}', which couldn't be "+$"handled by the argument reader.");}}}}
else{if(Β(Ζ)){string ǽ=Ε.Argument(0);string Ω="";string ĩ="";switch(ǽ){case"log":break;case"igc":Γ(Ζ);string Ώ=Ζ.Remove(0,4)
;Ώ=Ώ.Replace(Ε.Argument(1),"");Ώ=Ώ.Trim();IGC.SendBroadcastMessage(Ε.Argument(1),Ώ);υ.Ǔ(
$"Sent the following IGC message on channel '{Ε.Argument(1)}'"+$": {Ώ}.");break;case"mfd":if(Ε.ArgumentCount==3){string β=Ε.Argument(1);string α=Ε.Argument(2);if(ι==null){υ.Ǔ(
$"Received MFD command, but script configuration isn't loaded.");}else if(ι.ContainsKey(β)){ɭ ΰ=ι[β];if(α=="next"){ΰ.ɤ(true);}else if(α=="prev"){ΰ.ɤ(false);}else{if(!ΰ.ɢ(α)){υ.Ǔ(
$"Received command to set MFD '{β}' to unknown "+$"page '{α}'.");}}}else{υ.Ǔ($"Received '{α}' command for un-recognized MFD '{β}'.");}}else{υ.Ǔ(
$"Received MFD command with an incorrect number of arguments.");}break;case"action":if(Ε.ArgumentCount==3){ĩ=Ϫ(Ε.Argument(1),Ε.Argument(2),"");if(!Ϲ(ĩ)){υ.Ǔ(ĩ);}}else{υ.Ǔ(
$"Received Action command with an incorrect number of arguments.");}break;case"raycast":if(Ε.ArgumentCount==2){string ί=Ε.Argument(1);if(μ==null){υ.Ǔ(
$"Received Racast command, but script configuration isn't loaded.");}else if(μ.ContainsKey(ί)){μ[ί].Ȏ();}else{υ.Ǔ($"Received Raycast command for un-recognized Raycaster '{ί}'.");}}else{υ
.Ǔ($"Received Raycast command with an incorrect number of arguments.");}break;case"reconstitute":if(!ϖ){υ.Ǔ(
"Received Reconstitute command, but there is no last-good "+"config to reference. Please only use this command after the "+"script has successfully evaluated.");}else if(!Ε.Switch
("force")){υ.Ǔ("Received Reconstitute command, which will regenerate "+"declarations based on config that was read "+
$"{(DateTime.Now-Ϡ).Minutes} minutes ago "+$"({Ϡ.ToString("HH: mm: ss")}). If this is "+"acceptable, re-run this command with the -force flag.");}else{Me.
CustomData=$"{Ϟ}\n";if(!Ϲ(Ϟ)){Me.CustomData+=";=======================================\n\n";}Me.CustomData+=ϳ(ο.ToList(),ξ.Values.
ToList(),ν.ToList(),μ.Values.ToList());υ.Ǔ($"Carried out Reconstitute command. PB config has been reverted "+
$"to last known good.");}break;case"clone":List<IMyTerminalBlock>ή=new List<IMyTerminalBlock>();Ω="Clone command";if(!Ϥ(χ,ή,ref Ω)){υ.Ǔ(Ω);}
else{IMyTerminalBlock έ=ή[0];ή.Clear();if(!Ϥ(φ,ή,ref Ω)){υ.Ǔ(Ω);}else{foreach(IMyTerminalBlock Q in ή){Q.CustomData=έ.
CustomData;}υ.Ǔ($"Carried out Clone command, replacing the CustomData "+$"of {ή.Count} blocks in the {φ} "+
$"group with the CustomData from block '{έ.CustomName}'.");}}break;case"tacticalnuke":if(Ε.Switch("force")){List<IMyTerminalBlock>ά=new List<IMyTerminalBlock>();Ω=
"TacticalNuke command";if(!Ϥ(φ,ά,ref Ω)){υ.Ǔ(Ω);}else{foreach(IMyTerminalBlock Q in ά){Q.CustomData="";}υ.Ǔ(
$"Carried out TacticalNuke command, clearing the "+$"CustomData of {ά.Count} blocks.");}}else{υ.Ǔ("Received TacticalNuke command. TacticalNuke will remove "+
$"ALL CustomData from blocks in the {φ} group. "+"If you are certain you want to do this, run the command with the "+"-force switch.");}break;case"terminalproperties":
List<IMyTerminalBlock>Ϋ=new List<IMyTerminalBlock>();Ω="TerminalProperties command";if(!Ϥ(χ,Ϋ,ref Ω)){υ.Ǔ(Ω);}else{
Dictionary<Type,string>γ=new Dictionary<Type,string>();List<ITerminalProperty>Ϊ=new List<ITerminalProperty>();string Ψ;foreach(
IMyTerminalBlock Q in Ϋ){if(!γ.ContainsKey(Q.GetType())){Q.GetProperties(Ϊ);Ψ="";foreach(ITerminalProperty Χ in Ϊ){Ψ+=
$"  {Χ.Id}  {Χ.TypeName}\n";}γ.Add(Q.GetType(),Ψ);}}ž.Clear();string[]Φ;foreach(KeyValuePair<Type,string>Ũ in γ){Φ=Ũ.Key.ToString().Split('.');ž.
Append($"Properties for '{Φ[Φ.Length-1]}'\n{Ũ.Value}");}υ.Ǔ(ž.ToString());ž.Clear();}break;case"typedefinitions":List<
IMyTerminalBlock>Υ=new List<IMyTerminalBlock>();Ω="TypeDefinitions command";if(!Ϥ(χ,Υ,ref Ω)){υ.Ǔ(Ω);}else{bool Τ=Ε.Switch("items");List
<MyInventoryItem>Σ=new List<MyInventoryItem>();string[]Ρ;ž.Clear();ž.Append(
$"Type Definitions for members of the {χ} group:\n");foreach(IMyTerminalBlock Q in Υ){Ρ=Q.GetType().ToString().Split('.');ž.Append($" {Q.CustomName}:\n"+
$"  Interface: {Ρ[Ρ.Length-1]}\n"+$"  TypeID: {Q.BlockDefinition.TypeIdString}\n"+$"  SubTypeID: {Q.BlockDefinition.SubtypeId}\n"+$"\n");if(Τ&&Q.
HasInventory){Q.GetInventory().GetItems(Σ);ž.Append("  Items:\n");foreach(MyInventoryItem Π in Σ){ž.Append(
$"   Name: {Π.Type.ToString()}\n");ž.Append($"    TypeID: {Π.Type.TypeId}\n");ž.Append($"    SubTypeID: {Π.Type.SubtypeId}\n");}}}υ.Ǔ(ž.ToString());ž.
Clear();}break;case"surfacescripts":List<string>Ξ=new List<string>();Me.GetSurface(0).GetScripts(Ξ);ž.Clear();ž.Append(
"Available scripts:\n");foreach(string ϡ in Ξ){ž.Append($"  {ϡ}\n");}υ.Ǔ(ž.ToString());ž.Clear();break;case"autopopulate":MyIniParseResult ϓ;
if(!τ.TryParse(Me.CustomData,out ϓ)){υ.Ǔ("Received AutoPopulate command, but was unable to carry it "+
$"out due to a parsing error on line {ϓ.LineNo} of the "+$"Programmable Block's config: {ϓ.Error}");}else{HashSet<string>Ѕ=ʁ("APExcludedBlockTypes");HashSet<string>ɽ=ʁ(
"APExcludedBlockSubTypes");ɾ(ɽ);string ϭ="AutoPopulate";List<IMyTerminalBlock>ϯ=new List<IMyTerminalBlock>();if(Ε.Switch("target")){IMyBlockGroup
Ј=GridTerminalSystem.GetBlockGroupWithName(φ);if(Ј==null){υ.Ǔ("Received AutoPopulate command with the -target flag set, "
+$"but there is no {φ} block group on the grid.");break;}else{Ј.GetBlocks(ϯ,Ɗ=>Ɗ.IsSameConstructAs(Me)&&!Ѕ.Contains(Ɗ.
BlockDefinition.TypeIdString)&&!ɽ.Contains(Ɗ.BlockDefinition.SubtypeId)&&!MyIni.HasSection(Ɗ.CustomData,$"{ϋ}.APIgnore"));ϭ=
"Targeted AutoPopulate";}}else{Ϭ<IMyTerminalBlock>(ϯ,Ɗ=>Ɗ.IsSameConstructAs(Me)&&!Ѕ.Contains(Ɗ.BlockDefinition.TypeIdString)&&!ɽ.Contains(Ɗ.
BlockDefinition.SubtypeId)&&!MyIni.HasSection(Ɗ.CustomData,$"{ϋ}.APIgnore"));}bool Ї=ϰ(ϯ,τ,ϭ,ref ĩ);υ.Ǔ(ĩ);if(Ї){Save();Runtime.
UpdateFrequency=UpdateFrequency.Once;}}break;case"apexclusionreport":if(!τ.TryParse(Me.CustomData,out ϓ)){υ.Ǔ(
"Received APExclusionReport command, but was unable to carry it "+$"out due to a parsing error on line {ϓ.LineNo} of the "+$"Programmable Block's config: {ϓ.Error}");}else{ĩ=
"Carried out APExclusionReport command.\n";MyIniValue Ě=τ.Get($"{ϋ}.Init","APExcludedDeclarations");if(!Ϲ(Ě.ToString())){string І=Ě.ToString();ĩ+=
$"These declarations are being excluded from consideration "+$"by AutoPopulate: {І}.\n";List<string>Ŧ;Ŧ=І.Split(',').Select(Ĵ=>Ĵ.Trim()).ToList();Ɓ Љ=new Ɓ(ž,5);ɹ(Ŧ,Љ);if(Љ.ƚ()>0){
ĩ+=Љ.Ƙ();}ĩ+="\n";}HashSet<string>Ѕ=ʁ("APExcludedBlockTypes");Dictionary<string,int>Є=Ѕ.ToDictionary(Ђ=>Ђ,Ђ=>0);HashSet<
string>ɽ=ʁ("APExcludedBlockSubTypes");ɾ(ɽ);Dictionary<string,int>Ѓ=ɽ.ToDictionary(Ђ=>Ђ,Ђ=>0);int Ё=0;int Ѐ=0;int Ͽ=0;List<
IMyTerminalBlock>Ͼ=new List<IMyTerminalBlock>();Ϭ<IMyTerminalBlock>(Ͼ,Ɗ=>Ɗ.IsSameConstructAs(Me));foreach(IMyTerminalBlock Q in Ͼ){if(
MyIni.HasSection(Q.CustomData,$"{ϋ}.APIgnore")){Ё++;}if(Є.ContainsKey(Q.BlockDefinition.TypeIdString)){Є[Q.BlockDefinition.
TypeIdString]++;Ѐ++;}if(Ѓ.ContainsKey(Q.BlockDefinition.SubtypeId)){Ѓ[Q.BlockDefinition.SubtypeId]++;Ͽ++;}}ĩ+=
$"Of the {Ͼ.Count} TerminalBlocks on this "+$"construct, the following {Ё+Ѐ+Ͽ} "+$"blocks are being excluded from consideration by AutoPopulate:\n";ĩ+=
$"\n -{Ё} blocks excluded by APIgnore\n";ĩ+=$"\n -{Ѐ} blocks excluded by type\n";foreach(KeyValuePair<string,int>ħ in Є){ĩ+=$"  >{ħ.Value} {ħ.Key}\n";}ĩ+=
$"\n -{Ͽ} blocks excluded by subype\n";foreach(KeyValuePair<string,int>ħ in Ѓ){ĩ+=$"  >{ħ.Value} {ħ.Key}\n";}τ.Clear();υ.Ǔ(ĩ);}break;case"clear":List<
IMyTerminalBlock>Е=new List<IMyTerminalBlock>();Ω="Clear command";if(!Ϥ(φ,Е,ref Ω)){υ.Ǔ(Ω);}else{List<string>Д=new List<string>();string
[]Г;int В=0;foreach(IMyTerminalBlock Q in Е){τ.TryParse(Q.CustomData);τ.GetSections(Д);foreach(string Б in Д){Г=Б.Split(
'.');if(Г[0]==ϋ){τ.DeleteSection(Б);В++;}}Q.CustomData=τ.ToString();}τ.Clear();υ.Ǔ(
$"Clear command executed on {Е.Count} blocks. Removed "+$"{В} Shipware sections.");}break;case"changeid":Γ(Ζ);if(Ε.ArgumentCount==2){string А=Ε.Argument(1);string Џ=$"{ϋ}.{А}"
;List<IMyTerminalBlock>ϼ=new List<IMyTerminalBlock>();Ϭ<IMyTerminalBlock>(ϼ,Ɗ=>(Ɗ.IsSameConstructAs(Me)&&MyIni.HasSection
(Ɗ.CustomData,ψ)));foreach(IMyTerminalBlock Q in ϼ){Q.CustomData=Q.CustomData.Replace($"[{ψ}]",$"[{Џ}]");}ω=А;Save();Ɓ å;
bool ϑ;ϟ(out å,out ϑ);Runtime.UpdateFrequency=UpdateFrequency.Once;υ.Ǔ(
$"ChangeID complete, {ϼ.Count} blocks modified. The ID "+$"of this script instance is now '{ω}', and its tag is now '{ψ}'.");}else if(Ε.ArgumentCount>2){υ.Ǔ(
$"Received ChangeID command with too many arguments. Note "+$"that IDs can't contain spaces.");}else{υ.Ǔ($"Received ChangeID command with no new ID.");}break;case"integrate":List<
IMyTerminalBlock>Ў=new List<IMyTerminalBlock>();Ϭ<IMyTerminalBlock>(Ў,Ɗ=>(Ɗ.IsSameConstructAs(Me)&&MyIni.HasSection(Ɗ.CustomData,
$"{ϋ}.Integrate")));foreach(IMyTerminalBlock Q in Ў){Q.CustomData=Q.CustomData.Replace($"[{ϋ}.Integrate]",$"[{ψ}]");}υ.Ǔ(
$"Carried out Integrate command, replacing the '{ϋ}.Integrate' "+$"section headers on {Ў.Count} blocks with '{ψ}' headers.");break;case"evaluate":Save();Ѷ(new Ɓ(ž,15));break;case
"resetreports":if(ζ.Ĩ("ResetReports",10,out ĩ)){Ϸ(new з(this,λ,true));}else{υ.Ǔ(ĩ);}break;case"update":É();if(Ε.Switch("force")){
foreach(ɝ Ǹ in λ){Ǹ.ɲ();}foreach(Ǌ ϫ in κ){ϫ.Ǉ();}}else{Ǉ();}if(Ε.Switch("performance")){υ.Ǔ(
$"Update used {Runtime.CurrentInstructionCount} / {Runtime.MaxInstructionCount} "+$"({(int)(((double)Runtime.CurrentInstructionCount/Runtime.MaxInstructionCount)*100)}%) "+
$"of instructions allowed in this tic.\n");}break;case"test":Action Ѝ=()=>{υ.Ǔ("Periodic event firing");};Ħ Ќ=new Ħ(10,Ѝ);ζ.Ĕ("Test Event",Ќ);if(!ζ.Ĩ(
"Test Cooldown",20,out ĩ)){υ.Ǔ(ĩ);}υ.Ǔ(ζ.ı());break;default:υ.Ǔ($"Received un-recognized run command '{ǽ}'.");break;}}}Echo(υ.ǫ());if((
Ν&UpdateType.Update100)!=0){foreach(Ǻ Ћ in μ.Values){Ћ.Ǧ();}υ.Ǧ();}}void É(){foreach(w d in ο){d.Ñ();}foreach(m Њ in π){Њ
.f();}foreach(w d in ο){d.É();}Ơ Ƿ=null;bool Ͻ,ǿ;foreach(Ȫ Ğ in ν){Ͻ=Ğ.Ȁ(out Ƿ,out ǿ);if(Ͻ){υ.Ǔ(Ǿ(Ƿ,ǿ,ǿ?"on":"off",
$"Trigger {Ğ.u}'s "));}}}void Ǉ(){foreach(ɝ Ǹ in λ){Ǹ.Ǉ();}foreach(Ǌ ϫ in κ){ϫ.Ǉ();}}string Ϫ(string ϩ,string Ϩ,string ϧ){Ơ Ƿ;bool ǿ;if(ξ==
null){return"Received Action command, but script configuration isn't loaded.";}else if(ξ.TryGetValue(ϩ,out Ƿ)){if(Ϩ=="on"){ǿ
=true;}else if(Ϩ=="off"){ǿ=false;}else if(Ϩ=="switch"){ǿ=!Ƿ.Ù;}else{return
$"Received unknown {ϧ}command '{Ϩ}' for ActionSet "+$"'{ϩ}'. Valid commands for ActionSets are 'On', 'Off', and "+$"'Switch'.";}return Ǿ(Ƿ,ǿ,Ϩ,ϧ);}else{return
$"Received {ϧ}command '{Ϩ}' for un-recognized "+$"ActionSet '{ϩ}'.";}}string Ǿ(Ơ Ƿ,bool ǿ,string ǽ,string ϧ){string ĩ="";try{Ƿ.ș(ǿ);}catch(InvalidCastException e){
string ϥ="<ID not provided>";if(e.Data.Contains("Identifier")){ϥ=$"{e.Data["Identifier"]}";}ĩ=
$"An invalid cast exception occurred while running {ϧ}'{ǽ}' "+$"command for ActionSet '{Ƿ.u}' at {ϥ}. Make sure "+$"the action specified in configuration can be performed by {ϥ}.";}
catch(InvalidOperationException e){string Ϧ="<Trace failed>";if(e.Data.Contains("Counter")){Ϧ="Set Trace:\n";for(int ņ=(int)(
e.Data["Counter"]);ņ>=0;ņ--){Ϧ+=$"{e.Data[ņ]}\n";}}ĩ=$"A possible loop was detected while running {ϧ}'{ǽ}' command "+
$"for ActionSet '{Ƿ.u}'. Make sure {Ƿ.u} is "+$"not being called by one of the sets it is calling.\n\n{Ϧ}";}catch(Exception e){string ϥ="<ID not provided>";if(e.Data
.Contains("Identifier")){ϥ=$"{e.Data["Identifier"]}";}ĩ=$"An exception occurred while running {ϧ}'{ǽ}' command for "+
$"ActionSet '{Ƿ.u}' at {ϥ}.\n  Raw exception message:\n "+$"{e.Message}\n  Stack trace:\n{e.StackTrace}";}Ǉ();foreach(Ơ Θ in ξ.Values){Θ.ȗ();}if(Ϲ(ĩ)&&!Ϲ(ϧ)){ĩ=
$"Carried out {ϧ}command '{ǽ}' for ActionSet '{Ƿ.u}'. "+$"The set's state is now '{Ƿ.Ȝ}'.";}return ĩ;}bool Ϥ(string ϣ,List<IMyTerminalBlock>Ϣ,ref string Ω){GridTerminalSystem.
GetBlockGroupWithName(ϣ)?.GetBlocks(Ϣ);if(Ϣ.Count>0){return true;}else{Ω=$"Received {Ω}, but there is no {ϣ} block group on the grid.";return
false;}}void Ϭ<Č>(List<Č>ϼ,Func<Č,bool>Ϻ=null)where Č:class{GridTerminalSystem.GetBlocksOfType<Č>(ϼ,Ϻ);}bool Ϲ(string ϸ){
return String.IsNullOrEmpty(ϸ);}bool Ϸ(Ц ϵ){string ϴ=ϵ.Ф;if(!ώ.ContainsKey(ϴ)){ώ.Add(ϴ,ϵ);Runtime.UpdateFrequency|=
UpdateFrequency.Update10;if(ϵ.О&&ρ!=null){υ.Ǔ($"{ϴ} successfully added to scheduled tasks.");}return true;}else{υ.Ǔ(
$"Cannot schedule {ϴ} because an identical task is already scheduled.");return false;}}string ϳ(List<w>l,List<Ơ>ö,List<Ȫ>ϲ,List<Ǻ>ϱ){string ϻ;ž.Clear();foreach(w d in l){ž.Append(d.ş());}if(
l.Count>0){ž.Append(";=======================================\n\n");}foreach(Ơ Ü in ö){ž.Append(Ü.ş());}if(ö.Count>0){ž.
Append(";=======================================\n\n");}foreach(Ȫ Ğ in ϲ){ž.Append(Ğ.ş());}if(ϲ.Count>0){ž.Append(
";=======================================\n\n");}foreach(Ǻ Ʊ in ϱ){ž.Append(Ʊ.ş());}ϻ=ž.ToString();ž.Clear();return ϻ;}bool ϰ(List<IMyTerminalBlock>ϯ,MyIni Ϯ,string ϭ
,ref string ĩ){MyIni ˠ=σ;MyIniParseResult ϓ;Ɓ ɷ=new Ɓ(ž,10);MyIniValue Ě=Ϯ.Get($"{ϋ}.Init","APExcludedDeclarations");List
<string>ɸ=null;if(!Ϲ(Ě.ToString())){ɸ=Ě.ToString().Split(',').Select(Ĵ=>Ĵ.Trim()).ToList();}List<ͷ>ˏ=ɹ(ɸ,ɷ);List<ͷ>ʬ=new
List<ͷ>(ˏ);List<ˋ>ʫ=new List<ˋ>();List<ˉ>ʪ=new List<ˉ>();List<ˁ>ʩ=new List<ˁ>();Action<ͷ>ʨ=(ʧ)=>{if(ʧ is ˋ){ʫ.Add((ˋ)ʧ);}
else if(ʧ is ˉ){ʪ.Add((ˉ)ʧ);}else if(ʧ is ˁ){ʩ.Add((ˁ)ʧ);}ˏ.Remove(ʧ);};List<MyIniKey>ʦ=new List<MyIniKey>();List<string>ʥ=
new List<string>();Action<string,MyIni,MyIni>ʤ=(I,ʣ,ʭ)=>{ʣ.TryParse(I);ʣ.GetKeys(ʦ);foreach(MyIniKey ʀ in ʦ){ʥ.Add((ʣ.Get(ʀ
)).ToString());}for(int ņ=0;ņ<ʦ.Count;ņ++){ʭ.Set(ʦ[ņ],ʥ[ņ]);}ʥ.Clear();};string ʡ=$"{ϋ}.{ϊ}.ActionSet.Roost";if(!Ϯ.
ContainsSection(ʡ)&&!(ɸ?.Contains("Roost")??false)){Ơ ʠ=new Ơ("Roost",false);ʠ.v=ω;ʠ.Ƞ=ϙ;ʠ.ȟ=ϝ;ʠ.Ȟ="Roosting";ʠ.ȝ="Active";ð ʟ=new ð(
this);ʟ.ï=8;ʠ.ț(ʟ);ʤ(ʠ.ş(),ˠ,Ϯ);}int Ĭ=0;ͷ ʞ;string Ő;while(Ĭ!=-1){ʞ=ˏ[Ĭ];Ő=$"{ϋ}.{ϊ}.{ʞ.ˌ()}.{ʞ.E}";if(Ϯ.ContainsSection(Ő)
){ʨ(ʞ);ʬ.Remove(ʞ);}else{Ĭ++;}if(Ĭ>=ˏ.Count){Ĭ=-1;}}MyDefinitionId ʝ;HashSet<MyDefinitionId>ʜ=new HashSet<MyDefinitionId>
();foreach(IMyTerminalBlock Q in ϯ){ʝ=Q.BlockDefinition;if(!ʜ.Contains(ʝ)){Ĭ=0;while(Ĭ!=-1&&ˏ.Count!=0){ʞ=ˏ[Ĭ];if(ʞ.ˍ(Q))
{ʨ(ʞ);}else{Ĭ++;}if(Ĭ>=ˏ.Count){Ĭ=-1;}}ʜ.Add(ʝ);}}foreach(ͷ ʛ in ˏ){ʬ.Remove(ʛ);}foreach(ͷ ʢ in ʬ){ʤ(ʢ.ˎ(),ˠ,Ϯ);}if(!(ɸ?.
Contains("Roost")??false)){string ʮ;List<string>ʶ;HashSet<string>ʾ;Action<string,bool>ʼ=(ʀ,Ù)=>{ʮ=Ϯ.Get(ʡ,ʀ).ToString();if(!Ϲ(ʮ)
){ʶ=ʮ.Split(',').Select(Ĵ=>Ĵ.Trim()).ToList();ʾ=new HashSet<string>();foreach(string ʻ in ʶ){int ʺ=ʻ.IndexOf(':');if(ʺ!=-
1){ʾ.Add(ʻ.Substring(0,ʺ));}else{ʾ.Add(ʻ);}}}else{ʾ=null;ʶ=new List<string>();}string ǿ;foreach(ˁ ʹ in ʩ){string u=ʹ.E;ǿ=
ʹ.ͱ(Ù);if(!(ʾ?.Contains(u)??false)&&!Ϲ(ǿ)){ʶ.Add($"{u}: {ǿ}");}}Ϯ.Set(ʡ,ʀ,ŧ(ʶ,3,false));};ʼ("ActionSetsLinkedToOn",true);
ʼ("ActionSetsLinkedToOff",false);}string ʸ="";List<string>Ŧ=new List<string>();string ʽ;Action<string>ʷ=(ǖ)=>{Ϯ.Set(ʸ,
"Title",ǖ);Ϯ.Set(ʸ,"Columns","3");Ϯ.Set(ʸ,"FontSize",".5");Ϯ.Set(ʸ,"ForeColor","Yellow");Ϯ.Set(ʸ,"BackColor","Black");};Ϯ.Set(ψ
,"Surface0Pages",$"{((ʫ.Count>0||ʪ.Count>0)?"TallyReport, ":"")}"+
$"{(ʩ.Count>0?"SetReport, ":"")}Log, TargetScript, FactionScript");Ϯ.Set(ψ,"Surface0MFD","APScreen");if(ʫ.Count>0||ʪ.Count>0){ʸ="SW.TallyReport";foreach(ͷ ʵ in ʫ){Ŧ.Add(ʵ.E);}foreach(ͷ
ʵ in ʪ){Ŧ.Add(ʵ.E);}ʽ=ŧ(Ŧ,3,false);Ϯ.Set(ʸ,"Elements",ʽ);ʷ("Tallies");}Ŧ.Clear();if(ʩ.Count>0){ʸ="SW.SetReport";foreach(ͷ
ʂ in ʩ){Ŧ.Add(ʂ.E);}ʽ=ŧ(Ŧ,3,false);Ϯ.Set(ʸ,"Elements",ʽ);ʷ("Action Sets");}ʸ="SW.Log";Ϯ.Set(ʸ,"DataType","Log");Ϯ.Set(ʸ,
"FontSize",".8");Ϯ.Set(ʸ,"CharPerLine","30");Ϯ.Set(ʸ,"ForeColor","LightBlue");Ϯ.Set(ʸ,"BackColor","Black");ʸ="SW.TargetScript";Ϯ.
Set(ʸ,"Script","TSS_TargetingInfo");Ϯ.Set(ʸ,"ForeColor","LightBlue");Ϯ.Set(ʸ,"BackColor","Black");ʸ="SW.FactionScript";Ϯ.
Set(ʸ,"Script","TSS_FactionIcon");Ϯ.Set(ʸ,"BackColor","Black");Me.CustomData=Ϯ.ToString();Dictionary<MyDefinitionId,ˡ>ʴ=new
Dictionary<MyDefinitionId,ˡ>();ˡ ʳ=null;int ʲ=0;int ʱ=0;int ʰ=0;Func<IMyTerminalBlock,MyIni,bool>ʯ=(Ɗ,ʌ)=>{if(!ʌ.TryParse(Ɗ.
CustomData,out ϓ)){ʱ++;ɷ.ƛ($"Block {Ɗ.CustomName} failed to parse due to the following "+$"error on line {ϓ.LineNo}: {ϓ.Error}");
return false;}else{return true;}};Func<IMyTerminalBlock,MyIni,bool>ʚ=(Ɗ,ʌ)=>{if(ʳ==null){if(ʯ(Ɗ,ʌ)){ʳ=new ˡ(Ɗ,ʌ,ψ);ʲ++;return
true;}else{return false;}}return true;};foreach(IMyTerminalBlock Q in ϯ){ʳ=null;ʝ=Q.BlockDefinition;if(ʴ.ContainsKey(ʝ)){ʳ=ʴ
[ʝ];if(ʳ!=null){if(ʯ(Q,ˠ)){ʳ.ʤ(ψ,ˠ);Q.CustomData=ˠ.ToString();ʰ++;}}}else{foreach(ˋ ʄ in ʫ){if(ʄ.ˍ(Q)){if(ʚ(Q,ˠ)){ʳ.ѯ(
"Tallies",ʄ.E);}else{goto CannotWriteToThisBlockSoSkipToNext;}}}foreach(ˉ ʃ in ʪ){if(Q.InventoryCount==1){if(ʃ.ˆ(Q.GetInventory(0
))){if(ʚ(Q,ˠ)){ʳ.ѯ("Tallies",ʃ.E);}else{goto CannotWriteToThisBlockSoSkipToNext;}}}else if(Q.InventoryCount>1){for(int ņ=
0;ņ<Q.InventoryCount;ņ++){if(ʃ.ˆ(Q.GetInventory(ņ))){if(ʚ(Q,ˠ)){ʳ.ѯ($"Inv{ņ}Tallies",ʃ.E);}else{goto
CannotWriteToThisBlockSoSkipToNext;}}}}}foreach(ˁ ʂ in ʩ){if(ʂ.ˍ(Q)){if(ʚ(Q,ˠ)){ʳ.ѯ("ActionSets",ʂ.E);ʳ.ʞ=ʂ;}else{goto CannotWriteToThisBlockSoSkipToNext;
}}}ʴ.Add(ʝ,ʳ);if(ʳ!=null){ʳ.ʤ(ψ,ˠ);Q.CustomData=ˠ.ToString();ʰ++;}}CannotWriteToThisBlockSoSkipToNext:;}ĩ=
$"\nCarried out {ϭ} command. There are now declarations for "+$"{ʫ.Count+ʪ.Count} AP Tallies and {ʩ.Count} "+
$"AP ActionSets, with linking config written to {ʰ} / {ϯ.Count} of considered "+$"blocks{(ʱ>0?$" and {ʱ} blocks with unparsable config":"")}.\n"+
$"Autopopulate used {Runtime.CurrentInstructionCount} / {Runtime.MaxInstructionCount} "+$"({(int)(((double)Runtime.CurrentInstructionCount/Runtime.MaxInstructionCount)*100)}%) "+
$"of instructions allowed in this tic.\n";if(ɷ.ƞ()>0){ĩ+=$"\nThe following errors prevented AutoPopulate from running:\n{ɷ.Ɯ()}";}if(ɷ.ƚ()>0){ĩ+=
$"\nThe following warnings should be addressed:\n{ɷ.Ƙ()}";}if(ɷ.Ɠ()>0){ĩ+=$"\nThe following messages were logged:\n{ɷ.Ƒ()}";}Ϯ.Clear();ˠ.Clear();return true;}HashSet<string>ʁ(
string ʀ){HashSet<string>ɿ=new HashSet<string>();MyIniValue Ě=τ.Get($"{ϋ}.Init",ʀ);string[]Ŧ;if(!Ϲ(Ě.ToString())){Ŧ=Ě.ToString
().Split(',').Select(Ĵ=>Ĵ.Trim()).ToArray();foreach(string š in Ŧ){ɿ.Add(š);}}return ɿ;}void ɾ(HashSet<string>ɽ){string ɼ
=$"{ϋ}.FurnitureSubTypes";if(ɽ.Contains(ɼ)){ɽ.Remove(ɼ);ɽ.UnionWith(new string[]{"PassengerBench","PassengerSeatLarge",
"PassengerSeatSmallNew","PassengerSeatSmallOffset","LargeBlockBed","LargeBlockHalfBed","LargeBlockHalfBedOffset","LargeBlockInsetBed",
"LargeBlockCaptainDesk","LargeBlockLabDeskSeat","LargeBlockLabCornerDesk"});}string ɻ=$"{ϋ}.IsolatedCockpitSubTypes";if(ɽ.Contains(ɻ)){ɽ.Remove
(ɻ);ɽ.UnionWith(new string[]{"BuggyCockpit","OpenCockpitLarge","OpenCockpitSmall","LargeBlockCockpit","CockpitOpen",
"SmallBlockStandingCockpit","RoverCockpit","SpeederCockpitCompact","LargeBlockStandingCockpit","LargeBlockModularBridgeCockpit"});}string ɺ=
$"{ϋ}.ShelfSubTypes";if(ɽ.Contains(ɺ)){ɽ.Remove(ɺ);ɽ.UnionWith(new string[]{"LargeBlockLockerRoom","LargeBlockLockerRoomCorner","LargeCrate"
,"LargeBlockInsetBookshelf","LargeBlockLockers","LargeBlockInsetKitchen","LargeBlockWeaponRack","SmallBlockWeaponRack",
"SmallBlockKitchenFridge","SmallBlockFirstAidCabinet","LargeBlockLabCabinet","LargeFreezer"});}}List<ͷ>ɹ(List<string>ɸ,Ɓ ɷ){StringComparer ɶ=
StringComparer.OrdinalIgnoreCase;Dictionary<string,ͷ>ɵ=new Dictionary<string,ͷ>(ɶ);const string ɴ="MyObjectBuilder_Ore";const string ʅ
="MyObjectBuilder_Ingot";const string ɳ="MyObjectBuilder_AmmoMagazine";MyItemType ʆ=new MyItemType(ɴ,"Ice");MyItemType ʙ=
new MyItemType(ɴ,"Stone");MyItemType ʘ=new MyItemType(ɴ,"Iron");MyItemType ʗ=new MyItemType(ʅ,"Uranium");MyItemType ʖ=new
MyItemType(ɳ,"NATO_25x184mm");MyItemType ʕ=new MyItemType(ɳ,"AutocannonClip");MyItemType ʔ=new MyItemType(ɳ,"MediumCalibreAmmo");
MyItemType ʓ=new MyItemType(ɳ,"LargeCalibreAmmo");MyItemType ʒ=new MyItemType(ɳ,"SmallRailgunAmmo");MyItemType ʑ=new MyItemType(ɳ,
"LargeRailgunAmmo");MyItemType ʐ=new MyItemType(ɳ,"Missile200mm");MyDefinitionId ʏ=MyDefinitionId.Parse(
"MyObjectBuilder_GasProperties/Hydrogen");MyDefinitionId ʎ=MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Oxygen");w d;Ű ʍ=new Ű();Ů ʋ=new Ů();List<
MyItemType>ʊ=new List<MyItemType>();Func<IMyInventory,MyItemType,bool>ʉ=(ʈ,ʇ)=>{ʊ.Clear();ʈ.GetAcceptedItems(ʊ);return(ʊ.Contains(
ʇ));};d=new Å(ς,"Power",new ƶ(),ʋ);ɵ.Add(d.u,new ˋ(d.u,d,Ɗ=>Ɗ is IMyBatteryBlock));d=new Å(ς,"Hydrogen",new ƪ(),ʋ);ɵ.Add(
d.u,new ˋ(d.u,d,Ɗ=>(Ɗ is IMyGasTank&&(Ɗ.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(ʏ)??false))
||(Ɗ is IMyPowerProducer&&(Ɗ.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(ʏ)??false))));d=new Å(ς
,"Oxygen",new ƪ(),ʋ);ɵ.Add(d.u,new ˋ(d.u,d,Ɗ=>Ɗ is IMyGasTank&&(Ɗ.Components.Get<MyResourceSinkComponent>()?.
AcceptedResources.Contains(ʎ)??false)));d=new O(ς,"Cargo",ʍ);ɵ.Add(d.u,new ˉ(d.u,d,Ɗ=>Ɗ is IMyCargoContainer,ņ=>ʉ(ņ,ʆ)&&ʉ(ņ,ʗ)));d=new H(
ς,"Ice",ʆ,ʋ);d.Ë(4000);ɵ.Add(d.u,new ˉ(d.u,d,Ɗ=>Ɗ is IMyGasGenerator,ņ=>ʉ(ņ,ʆ)));d=new H(ς,"Stone",ʙ,ʍ);d.Ë(5000);ɵ.Add(d
.u,new ˉ(d.u,d,Ɗ=>Ɗ is IMyShipDrill||Ɗ is IMyRefinery,ņ=>ʉ(ņ,ʙ)));d=new O(ς,"Ore",ʍ);ɵ.Add(d.u,new ˉ(d.u,d,Ɗ=>Ɗ is
IMyShipDrill||Ɗ is IMyRefinery,ņ=>ʉ(ņ,ʘ)));d=new H(ς,"Uranium",ʗ,ʋ);d.Ë(50);ɵ.Add(d.u,new ˉ(d.u,d,Ɗ=>Ɗ is IMyReactor,ņ=>ʉ(ņ,ʗ)));d=
new Å(ς,"Solar",new Ư(),ʋ);d.N=100;ɵ.Add(d.u,new ˋ(d.u,d,Ɗ=>Ɗ is IMySolarPanel));d=new Å(ς,"JumpDrive",new Ƣ(),ʋ);d.v=
"Jump Charge";ɵ.Add(d.u,new ˋ(d.u,d,Ɗ=>Ɗ is IMyJumpDrive));Func<IMyTerminalBlock,MyItemType,bool>ʹ=(Ɗ,ņ)=>{return Ɗ is
IMyUserControllableGun&&ʉ(Ɗ.GetInventory(0),ņ);};d=new H(ς,"GatlingAmmo",ʖ,ʋ);d.Ë(20);d.v="Gatling\nDrums";ɵ.Add(d.u,new ˉ(d.u,d,Ɗ=>ʹ(Ɗ,ʖ),ņ=>
ʉ(ņ,ʖ)));d=new H(ς,"AutocannonAmmo",ʕ,ʋ);d.Ë(60);d.v="Autocannon\nClips";ɵ.Add(d.u,new ˉ(d.u,d,Ɗ=>ʹ(Ɗ,ʕ),ņ=>ʉ(ņ,ʕ)));d=
new H(ς,"AssaultAmmo",ʔ,ʋ);d.Ë(120);d.v="Cannon\nShells";ɵ.Add(d.u,new ˉ(d.u,d,Ɗ=>ʹ(Ɗ,ʔ),ņ=>ʉ(ņ,ʔ)));d=new H(ς,
"ArtilleryAmmo",ʓ,ʋ);d.Ë(40);d.v="Artillery\nShells";ɵ.Add(d.u,new ˉ(d.u,d,Ɗ=>ʹ(Ɗ,ʓ),ņ=>ʉ(ņ,ʓ)));d=new H(ς,"RailSmallAmmo",ʒ,ʋ);d.Ë(36)
;d.v="Railgun\nS. Sabots";ɵ.Add(d.u,new ˉ(d.u,d,Ɗ=>ʹ(Ɗ,ʒ),ņ=>ʉ(ņ,ʒ)));d=new H(ς,"RailLargeAmmo",ʑ,ʋ);d.Ë(12);d.v=
"Railgun\nL. Sabots";ɵ.Add(d.u,new ˉ(d.u,d,Ɗ=>ʹ(Ɗ,ʑ),ņ=>ʉ(ņ,ʑ)));d=new H(ς,"RocketAmmo",ʐ,ʋ);d.Ë(24);d.v="Rockets";ɵ.Add(d.u,new ˉ(d.u,d,Ɗ=>
ʹ(Ɗ,ʐ),ņ=>ʉ(ņ,ʐ)));Ơ ͳ;Action<MyIni,string,string,string>Ͳ=(ʌ,Ͷ,ͽ,Ά)=>{ʌ.Set(Ͷ,"ActionOn",ͽ);ʌ.Set(Ͷ,"ActionOff",Ά);};
Action<MyIni,string,string,string>Έ=(ʌ,Ͷ,ͽ,Ά)=>{ʌ.Set(Ͷ,"Action0Property","Radius");ʌ.Set(Ͷ,"Action0ValueOn","1500");ʌ.Set(Ͷ,
"Action0ValueOff","150");ʌ.Set(Ͷ,"Action1Property","HudText");ʌ.Set(Ͷ,"Action1ValueOn",ͽ);ʌ.Set(Ͷ,"Action1ValueOff",Ά);};ͳ=new Ơ(
"Antennas",false);ͳ.v="Antenna\nRange";ͳ.Ȟ="Broad";ͳ.ȝ="Wifi";ͳ.ȟ=ϛ;ɵ.Add(ͳ.u,new ˁ(ͳ.u,ͳ,Ɗ=>Ɗ is IMyRadioAntenna,$"{ϋ}.{ͳ.u}",
$"{ω}",$"{ω} Wifi",Έ,"Off","On"));ͳ=new Ơ("Beacons",false);ͳ.v="Beacon";ͳ.Ȟ="Online";ͳ.ȝ="Offline";ɵ.Add(ͳ.u,new ˁ(ͳ.u,ͳ,Ɗ=>Ɗ
is IMyBeacon,$"{ϋ}.{ͳ.u}","EnableOn","EnableOff",Ͳ,"Off","On"));ͳ=new Ơ("Spotlights",false);ͳ.Ȟ="Online";ͳ.ȝ="Offline";ɵ.
Add(ͳ.u,new ˁ(ͳ.u,ͳ,Ɗ=>Ɗ is IMyReflectorLight,$"{ϋ}.{ͳ.u}","EnableOn","EnableOff",Ͳ,"Off",""));ͳ=new Ơ("OreDetectors",false
);ͳ.v="Ore\nDetector";ͳ.Ȟ="Scanning";ͳ.ȝ="Idle";ɵ.Add(ͳ.u,new ˁ(ͳ.u,ͳ,Ɗ=>Ɗ is IMyOreDetector,$"{ϋ}.{ͳ.u}","EnableOn",
"EnableOff",Ͳ,"Off","On"));ͳ=new Ơ("Batteries",false);ͳ.Ȟ="On Auto";ͳ.ȝ="Recharging";ͳ.ȟ=ϛ;ɵ.Add(ͳ.u,new ˁ(ͳ.u,ͳ,Ɗ=>Ɗ is
IMyBatteryBlock,$"{ϋ}.{ͳ.u}","BatteryAuto","BatteryRecharge",Ͳ,"Off","On"));ͳ=new Ơ("Reactors",false);ͳ.Ȟ="Active";ͳ.ȝ="Inactive";ɵ.Add
(ͳ.u,new ˁ(ͳ.u,ͳ,Ɗ=>Ɗ is IMyReactor,$"{ϋ}.{ͳ.u}","EnableOn","EnableOff",Ͳ,"Off",""));ͳ=new Ơ("EnginesHydrogen",false);ͳ.v
="Engines";ͳ.Ȟ="Running";ͳ.ȝ="Idle";ɵ.Add(ͳ.u,new ˁ(ͳ.u,ͳ,Ɗ=>Ɗ is IMyPowerProducer&&(Ɗ.Components.Get<
MyResourceSinkComponent>()?.AcceptedResources.Contains(ʏ)??false),$"{ϋ}.{ͳ.u}","EnableOn","EnableOff",Ͳ,"Off",""));ͳ=new Ơ("IceCrackers",false)
;ͳ.v="Ice Crackers";ͳ.Ȟ="Running";ͳ.ȝ="Idle";ɵ.Add(ͳ.u,new ˁ(ͳ.u,ͳ,Ɗ=>Ɗ is IMyGasGenerator,$"{ϋ}.{ͳ.u}","EnableOn",
"EnableOff",Ͳ,"",""));ͳ=new Ơ("TanksHydrogen",false);ͳ.v="Hydrogen\nTanks";ͳ.Ȟ="Open";ͳ.ȝ="Filling";ͳ.ȟ=Ϝ;ɵ.Add(ͳ.u,new ˁ(ͳ.u,ͳ,Ɗ=>
Ɗ is IMyGasTank&&(Ɗ.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(ʏ)??false),$"{ϋ}.{ͳ.u}",
"TankStockpileOff","TankStockpileOn",Ͳ,"Off","On"));ͳ=new Ơ("TanksOxygen",false);ͳ.v="Oxygen\nTanks";ͳ.Ȟ="Open";ͳ.ȝ="Filling";ͳ.ȟ=Ϝ;ɵ.Add(
ͳ.u,new ˁ(ͳ.u,ͳ,Ɗ=>Ɗ is IMyGasTank&&(Ɗ.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(ʎ)??false),
$"{ϋ}.{ͳ.u}","TankStockpileOff","TankStockpileOn",Ͳ,"Off","On"));ͳ=new Ơ("Gyroscopes",false);ͳ.v="Gyros";ͳ.Ȟ="Active";ͳ.ȝ="Inactive"
;ɵ.Add(ͳ.u,new ˁ(ͳ.u,ͳ,Ɗ=>Ɗ is IMyGyro,$"{ϋ}.{ͳ.u}","EnableOn","EnableOff",Ͳ,"Off","On"));ͳ=new Ơ("ThrustersAtmospheric",
false);ͳ.v="Atmospheric\nThrusters";ͳ.Ȟ="Online";ͳ.ȝ="Offline";ɵ.Add(ͳ.u,new ˁ(ͳ.u,ͳ,Ɗ=>Ɗ is IMyThrust&&(Ɗ.BlockDefinition.
SubtypeId.Contains("Atmospheric")),$"{ϋ}.{ͳ.u}","EnableOn","EnableOff",Ͳ,"Off","On"));ͳ=new Ơ("ThrustersIon",false);ͳ.v=
"Ion\nThrusters";ͳ.Ȟ="Online";ͳ.ȝ="Offline";ɵ.Add(ͳ.u,new ˁ(ͳ.u,ͳ,Ɗ=>Ɗ is IMyThrust&&(!Ɗ.BlockDefinition.SubtypeId.Contains(
"Atmospheric")&&!Ɗ.BlockDefinition.SubtypeId.Contains("Hydrogen")),$"{ϋ}.{ͳ.u}","EnableOn","EnableOff",Ͳ,"Off","On"));ͳ=new Ơ(
"ThrustersHydrogen",false);ͳ.v="Hydrogen\nThrusters";ͳ.Ȟ="Online";ͳ.ȝ="Offline";ɵ.Add(ͳ.u,new ˁ(ͳ.u,ͳ,Ɗ=>Ɗ is IMyThrust&&(Ɗ.BlockDefinition
.SubtypeId.Contains("Hydrogen")),$"{ϋ}.{ͳ.u}","EnableOn","EnableOff",Ͳ,"Off","On"));ͳ=new Ơ("ThrustersGeneric",false);ͳ.v
="Thrusters";ͳ.Ȟ="Online";ͳ.ȝ="Offline";ɵ.Add(ͳ.u,new ˁ(ͳ.u,ͳ,Ɗ=>Ɗ is IMyThrust,$"{ϋ}.{ͳ.u}","EnableOn","EnableOff",Ͳ,
"Off","On"));if(ɸ!=null){int ņ=0;string ͼ;while(ņ<ɸ.Count){ͼ=ɸ[ņ];if(ɵ.ContainsKey(ͼ)){ɵ.Remove(ͼ);ɸ.RemoveAt(ņ);}else{ņ++;}}
if(ņ>0){string ͻ="";foreach(string ͺ in ɸ){ͻ+=$"{ͺ}, ";}ͻ=ͻ.Remove(ͻ.Length-2);ɷ.ƛ(
"The following entries from APExcludedDeclarations could not "+$"be matched to declarations: {ͻ}.");}}return ɵ.Values.ToList();}abstract class ͷ{public string E{get;private set;}
protected Š ˈ;Func<IMyTerminalBlock,bool>ʿ;public ͷ(string E,Š ˈ,Func<IMyTerminalBlock,bool>ʿ){this.E=E;this.ˈ=ˈ;this.ʿ=ʿ;}public
string ˎ(){return ˈ.ş();}public bool ˍ(IMyTerminalBlock Q){return ʿ(Q);}public abstract string ˌ();}class ˋ:ͷ{public ˋ(string
E,Š ˈ,Func<IMyTerminalBlock,bool>ʿ):base(E,ˈ,ʿ){}public override string ˌ(){return"Tally";}}class ˉ:ͷ{Func<IMyInventory,
bool>ˇ;public ˉ(string E,Š ˈ,Func<IMyTerminalBlock,bool>ʿ,Func<IMyInventory,bool>ˇ):base(E,ˈ,ʿ){this.ˇ=ˇ;}public bool ˆ(
IMyInventory J){return ˇ(J);}public override string ˌ(){return"Tally";}}class ˁ:ͷ{string ˀ,ˊ,ː;Action<MyIni,string,string,string>ˬ;
public string Ͱ{get;private set;}public string ˮ{get;private set;}public ˁ(string E,Š ˈ,Func<IMyTerminalBlock,bool>ʿ,string ˑ,
string ˊ,string ː,Action<MyIni,string,string,string>ˬ,string Ͱ,string ˮ):base(E,ˈ,ʿ){ˀ=ˑ;this.ˊ=ˊ;this.ː=ː;this.ˬ=ˬ;this.Ͱ=Ͱ;
this.ˮ=ˮ;}internal string ͱ(bool Ù){return Ù?Ͱ:ˮ;}public void ˤ(MyIni ˠ){ˬ.Invoke(ˠ,ˀ,ˊ,ː);}public string ˣ(){return String.
IsNullOrEmpty(Ͱ)?"":$"{E}: {Ͱ}";}public string ˢ(){return String.IsNullOrEmpty(ˮ)?"":$"{E}: {ˮ}";}public override string ˌ(){return
"ActionSet";}}class ˡ{internal Dictionary<string,Ѵ>I{get;private set;}internal ˁ ʞ;public ˡ(IMyTerminalBlock Q,MyIni ˠ,string ˑ){
int Ο=Q.InventoryCount;I=new Dictionary<string,Ѵ>();if(Ο>1){for(int ņ=0;ņ<Ο;ņ++){ѵ(ˠ,ˑ,$"Inv{ņ}Tallies");}}else{ѵ(ˠ,ˑ,
"Tallies");}ѵ(ˠ,ˑ,"ActionSets");ʞ=null;}private void Ѡ(string ʀ,string ѱ,bool Ѳ=false){I.Add(ʀ,new Ѵ(ѱ,Ѳ));}private bool ѵ(MyIni
ʌ,string ˑ,string ʀ){if(ʌ.ContainsKey(ˑ,ʀ)){Ѡ(ʀ,ʌ.Get(ˑ,ʀ).ToString());return true;}return false;}public void ѯ(string ʀ,
string ʻ){if(I.ContainsKey(ʀ)){I[ʀ].ѯ(ʻ);}else{Ѡ(ʀ,ʻ,true);}}public void ʤ(string ˑ,MyIni ʌ){foreach(KeyValuePair<string,Ѵ>ħ
in I){ħ.Value.ѭ(ʌ,ˑ,ħ.Key);}if(ʞ!=null){ʞ.ˤ(ʌ);}}}class Ѵ{public string ѳ{get;private set;}bool Ѳ;public Ѵ(string ѱ,bool Ѱ
=false){ѳ=ѱ;Ѳ=Ѱ;}public void ѯ(string Ѯ){if(!ѳ.Contains(Ѯ)){ѳ=$"{ѳ}, {Ѯ}";Ѳ=true;}}public void ѭ(MyIni ʌ,string ˑ,string
ʀ){if(Ѳ){ʌ.Set(ˑ,ʀ,ѳ);}}}void Ѷ(Ɓ å,bool ϑ=false){StringComparer ɶ=StringComparer.OrdinalIgnoreCase;ф Ŏ=new ф(ɶ);
Dictionary<string,w>ј=new Dictionary<string,w>(ɶ);Dictionary<string,Ơ>ї=new Dictionary<string,Ơ>(ɶ);Dictionary<string,Ȫ>і=new
Dictionary<string,Ȫ>(ɶ);Dictionary<string,Ǻ>ѕ=new Dictionary<string,Ǻ>(ɶ);Dictionary<IMyInventory,List<O>>ѿ=new Dictionary<
IMyInventory,List<O>>();List<ɝ>Ѿ=new List<ɝ>();List<Ƽ>ѽ=new List<Ƽ>();Dictionary<string,ɭ>Ѽ=new Dictionary<string,ɭ>(ɶ);Dictionary<
string,Ǌ>ѻ=new Dictionary<string,Ǌ>(ɶ);HashSet<string>є=new HashSet<string>(ɶ);string Ѻ="";MyIniParseResult ϓ;MyIniValue Ě=new
MyIniValue();int ѹ=-1;σ.TryParse(Storage);int Ι=σ.Get("Data","UpdateDelay").ToInt32(0);int ѝ=-1;if(!τ.TryParse(Me.CustomData,out ϓ
)){å.ƕ($"The parser encountered an error on line {ϓ.LineNo} of the "+$"Programmable Block's config: {ϓ.Error}");}else{ђ(Ŏ
,å,Ě,out ѝ);ћ(Me,å,Ŏ,ј,ї,і,ѕ,є,ϓ,Ě);if(å.ƞ()>0){å.ƛ("Errors in Programmable Block configuration have prevented grid "+
"configuration from being evaluated.");}else{Ѻ=Я();ѹ=ң(å,Ŏ,ј,ї,і,ѕ,ѿ,Ѽ,Ѿ,ѽ,ѻ,ϓ,Ě);}}if(θ==null||å.ƞ()==0||ѽ.Count>=θ.Count){θ=ѽ;}string ĩ=
"Evaluation complete.\n";if(å.ƞ()>0){ĩ+="Errors prevent the use of this configuration. ";if(ϖ){Runtime.UpdateFrequency=UpdateFrequency.Update100
;ĩ+=$"Execution continuing with last good configuration from "+$"{(DateTime.Now-Ϡ).Minutes} minutes ago "+
$"({Ϡ.ToString("HH: mm: ss")}).\n";}else{Runtime.UpdateFrequency=UpdateFrequency.None;ĩ+=
"Because there is no good configuration loaded, the script has been halted.\n";}ĩ+=$"\nThe following errors are preventing the use of this config:\n{å.Ɯ()}";}else{m Њ;int Ú=0;π=new m[ѿ.Count];
foreach(IMyInventory J in ѿ.Keys){Њ=new m(J,ѿ[J].ToArray());Њ.k();π[Ú]=Њ;Ú++;}ο=ј.Values.ToArray();ν=і.Values.ToArray();λ=Ѿ.
ToArray();κ=ѻ.Values.ToArray();ξ=ї;μ=ѕ;ι=Ѽ;foreach(w Ѹ in ο){Ѹ.Ì();}foreach(ɝ ɦ in λ){ɦ.ɨ();}ζ.ġ();Ɔ(Ι);if(ѝ>-1){if(ѝ<10){å.ƛ(
$"{ϋ}.Init, key 'MPSpriteSyncFrequency' "+$"requested an invalid frequncy of {ѝ}. Sync frequency has "+$"been set to the lowest allowed value of 10 instead.");ѝ=
10;}Action ѷ=()=>{Ϸ(new з(this,λ,false));};Ħ Ѭ=new Ħ(ѝ,ѷ);ζ.ē("SpriteRefresher",Ѭ);}ϖ=true;Ϡ=DateTime.Now;Ϟ=Ѻ;ĩ+=
$"Script is now running. Registered {ο.Length} tallies, "+$"{ξ.Count} ActionSets, {ν.Length} triggers, and {λ.Length} "+
$"reports, as configured by data on {ѹ} blocks. Evaluation used "+$"{Runtime.CurrentInstructionCount} / {Runtime.MaxInstructionCount} "+
$"({(int)(((double)Runtime.CurrentInstructionCount/Runtime.MaxInstructionCount)*100)}%) "+$"of instructions allowed in this tic.\n";Runtime.UpdateFrequency=UpdateFrequency.Update100;}if(å.Ɠ()>0){ĩ+=
$"\nThe following messages were logged:\n{å.Ƒ()}";}if(å.ƚ()>0){ĩ+=$"\nThe following warnings were logged:\n{å.Ƙ()}";}υ.Ǔ(ĩ);foreach(Ƽ ѫ in θ){ѫ.Ǉ();}τ.Clear();σ.Clear();
}void ђ(ф Ŏ,Ɓ å,MyIniValue Ě,out int ѝ){ϒ(å);Color ė;string ϐ=$"{ϋ}.Init";Action<string>Ł=ŋ=>{å.ƕ($"{ϐ}{ŋ}");};Ű ʍ=(Ű)(Ŏ.
я("LowGood"));Ů ʋ=(Ů)(Ŏ.я("HighGood"));Action<string>ќ=ў=>{if(Ŏ.р(Ł,τ,ϐ,$"Color{ў}",out ė)){ʍ.Ų(ў,ė);ʋ.Ų(ў,ė);}};ќ(
"Optimal");ќ("Normal");ќ("Caution");ќ("Warning");ќ("Critical");ѝ=τ.Get(ϐ,"MPSpriteSyncFrequency").ToInt32(-1);}void ћ(
IMyTerminalBlock љ,Ɓ å,ф Ŏ,Dictionary<string,w>ј,Dictionary<string,Ơ>ї,Dictionary<string,Ȫ>і,Dictionary<string,Ǻ>ѕ,HashSet<string>є,
MyIniParseResult ϓ,MyIniValue Ě){string ѓ="";string њ="";Color ė=Color.Black;Ű ʍ=(Ű)(Ŏ.я("LowGood"));Ů ʋ=(Ů)(Ŏ.я("HighGood"));Ŝ B;List<
string>ѩ=new List<string>();List<Å>Ѩ=new List<Å>();Action<string>ѧ=Ɗ=>å.ƕ(Ɗ);Action<string>Ѧ=ŋ=>{å.ƕ($"{ѓ} {њ}{ŋ}");};
StringComparison ѥ=StringComparison.OrdinalIgnoreCase;List<string>Ѥ=new List<string>();List<string>ѣ=new List<string>();List<string>Ѣ=
new List<string>();string[]Ѫ;τ.GetSections(Ѣ);foreach(string Ő in Ѣ){Ѫ=Ő.Split('.').Select(Ĵ=>Ĵ.Trim()).ToArray();if(Ѫ.
Length==4&&String.Equals(Ѫ[0],ϋ,ѥ)){ѓ=Ѫ[2];њ=Ѫ[3];if(ѓ.Equals("Tally",ѥ)){w d=null;string ѡ;ѡ=τ.Get(Ő,"Type").ToString().
ToLowerInvariant();if(Ϲ(ѡ)){å.ƕ($"{ѓ} {њ} has a missing or unreadable Type.");}else if(ѡ=="inventory"){d=new O(ς,њ,ʍ);}else if(ѡ=="item"
){string D,C;D=τ.Get(Ő,"ItemTypeID").ToString();if(Ϲ(D)){å.ƕ($"{ѓ} {њ} has a missing or unreadable ItemTypeID.");}C=τ.Get
(Ő,"ItemSubTypeID").ToString();if(Ϲ(C)){å.ƕ($"{ѓ} {њ} has a missing or unreadable ItemSubTypeID.");}if(!Ϲ(D)&&!Ϲ(C)){d=
new H(ς,њ,D,C,ʋ);}}else if(ѡ=="battery"){d=new Å(ς,њ,new ƶ(),ʋ);}else if(ѡ=="gas"){d=new Å(ς,њ,new ƪ(),ʋ);}else if(ѡ==
"jumpdrive"){d=new Å(ς,њ,new Ƣ(),ʋ);}else if(ѡ=="raycast"){d=new Å(ς,њ,new ƴ(),ʋ);ѩ.Add(Ő);Ѩ.Add((Å)d);}else if(ѡ=="powermax"){d=
new Å(ς,њ,new Ư(),ʋ);}else if(ѡ=="powercurrent"){d=new Å(ς,њ,new ƭ(),ʋ);}else if(ѡ=="integrity"){d=new Å(ς,њ,new ǰ(),ʋ);}
else if(ѡ=="ventpressure"){d=new Å(ς,њ,new ǯ(),ʋ);}else if(ѡ=="pistonextension"){d=new Å(ς,њ,new ǭ(),ʋ);}else if(ѡ==
"rotorangle"){d=new Å(ς,њ,new ǳ(),ʋ);}else if(ѡ=="controllergravity"){d=new Å(ς,њ,new Ǳ(),ʋ);}else if(ѡ=="controllerspeed"){d=new Å(
ς,њ,new ǵ(),ʋ);}else if(ѡ=="controllerweight"){d=new Å(ς,њ,new Ǭ(),ʋ);}else{å.ƕ(
$"{ѓ} {њ} has un-recognized Type of '{ѡ}'.");}if(d==null){d=new O(ς,њ,ʍ);}Ě=τ.Get(Ő,"DisplayName");if(!Ě.IsEmpty){d.v=Ě.ToString();}Ě=τ.Get(Ő,"Multiplier");if(!Ě.
IsEmpty){d.N=Ě.ToDouble();}Ě=τ.Get(Ő,"Max");if(!Ě.IsEmpty){d.Ë(Ě.ToDouble());}else if(Ě.IsEmpty&&(d is H||(d is Å&&((Å)d).Ä is
Ǭ))){å.ƕ($"{ѓ} {њ}'s TallyType of '{ѡ}' requires a Max "+$"to be set in configuration.");}if(Ŏ.ы(Ѧ,τ,Ő,"ColorCoder",out B
)){d.B=B;}if(!ш(є,d.u,Ő,å)){ј.Add(d.u,d);є.Add(d.u);}}else if(ѓ.Equals("ActionSet",ѥ)){bool ľ=σ?.Get("ActionSets",њ).
ToBoolean(false)??false;Ơ Θ=new Ơ(њ,ľ);Ě=τ.Get(Ő,"DisplayName");if(!Ě.IsEmpty){Θ.v=Ě.ToString();}if(Ŏ.р(Ѧ,τ,Ő,"ColorOn",out ė)){Θ
.Ƞ=ė;}if(Ŏ.р(Ѧ,τ,Ő,"ColorOff",out ė)){Θ.ȟ=ė;}Ě=τ.Get(Ő,"TextOn");if(!Ě.IsEmpty){Θ.Ȟ=Ě.ToString();}Ě=τ.Get(Ő,"TextOff");if
(!Ě.IsEmpty){Θ.ȝ=Ě.ToString();}if(!ш(є,Θ.u,Ő,å)){Θ.Ǽ();ї.Add(Θ.u,Θ);є.Add(Θ.u);}}else if(ѓ.Equals("Trigger",ѥ)){bool ľ=σ?
.Get("Triggers",њ).ToBoolean(true)??true;Ȫ Ğ=new Ȫ(њ,ľ);if(!ш(є,Ğ.u,Ő,å)){і.Add(Ğ.u,Ğ);є.Add(Ğ.u);}}else if(ѓ.Equals(
"Raycaster",ѥ)){Ǻ Ʊ=new Ǻ(ž,њ);Ȋ ǹ=null;string[]џ=null;double[]Ҁ=null;string ҍ=τ.Get(Ő,"Type").ToString().ToLowerInvariant();if(Ϲ(ҍ
)){å.ƕ($"{ѓ} {њ} has a missing or unreadable Type.");}else if(ҍ=="linear"){ǹ=new ɛ();џ=ɛ.ɘ();Ҁ=new double[џ.Length];}else
{å.ƕ($"{ѓ} {њ} has un-recognized Type of '{ҍ}'.");}if(ǹ!=null){for(int ņ=0;ņ<џ.Length;ņ++){Ҁ[ņ]=τ.Get(Ő,џ[ņ]).ToDouble(-1
);}ǹ.ɜ(Ҁ);Ʊ=new Ǻ(ž,ǹ,њ);}else{ѣ.Add(њ);}if(!ш(є,њ,Ő,å)){ѕ.Add(Ʊ.u,Ʊ);є.Add(њ);}}else{if(Ѫ[1]==ϊ){å.ƕ(
$"{Ő} referenced the unknown declaration "+$"type '{ѓ}'.");}else{å.ƛ($"{Ő} has the format of a declaration "+$"header but lacks the '{ϊ}' prefix and has been "+
$"discarded.");}}}}ѓ="Raycaster";for(int ņ=0;ņ<Ѩ.Count;ņ++){string Ő=ѩ[ņ];Å Ҡ=Ѩ[ņ];Ě=τ.Get(Ő,"Raycaster");if(Ě.IsEmpty){if(!Ҡ.q){å.ƕ(
$"{ѓ} {Ҡ.u}'s "+$"Type of 'Raycaster' requires either a Max or a linked Raycaster to "+$"be set in configuration.");}}else{string ί=Ě.
ToString();if(Ҡ.q){å.ƛ($"{ѓ} {Ҡ.u} specifies "+$"both a Max and a linked Raycaster, '{ί}'. Only one of these "+
$"values is required. The linked Raycaster has been ignored.");}else{Ǻ Ʊ;if(ѕ.TryGetValue(ί,out Ʊ)){Ҡ.Ë(Ʊ.ȏ());}else{å.ƕ($"{ѓ} {Ҡ.u} tried "+
$"to reference the unconfigured Raycaster '{ί}'.");}}}}ѓ="Trigger";foreach(Ȫ Ğ in і.Values){w d=null;Ơ Θ=null;њ=Ğ.u;string Ő=$"{ϋ}.{ϊ}.Trigger.{њ}";Ě=τ.Get(Ő,"Tally");if
(!Ě.IsEmpty){string ҟ=Ě.ToString();if(ј.TryGetValue(ҟ,out d)){Ğ.ȑ=d;}else{å.ƕ($"{ѓ} {њ} tried to reference "+
$"the unconfigured Tally '{ҟ}'.");}}else{å.ƕ($"{ѓ} {њ} has a missing or unreadable Tally.");}Ě=τ.Get(Ő,"ActionSet");if(!Ě.IsEmpty){string Ҟ=Ě.ToString()
;if(ї.TryGetValue(Ҟ,out Θ)){Ğ.Ƿ=Θ;}else{å.ƕ($"{ѓ} {њ} tried to reference "+$"the unconfigured ActionSet '{Ҟ}'.");}}else{å
.ƕ($"{ѓ} {њ} has a missing or unreadable ActionSet.");}Ď(Ğ,Ő,true,"LessOrEqual",Ě,å);Ď(Ğ,Ő,false,"GreaterOrEqual",Ě,å);if
(!Ğ.ǻ()){å.ƕ($"{ѓ} {њ} does not define a valid "+$"LessOrEqual or GreaterOrEqual scenario.");}if(d==null||Θ==null){Ѥ.Add(
њ);}}List<KeyValuePair<string,bool>>ҡ=new List<KeyValuePair<string,bool>>();ѓ="ActionSet";foreach(Ơ Θ in ї.Values){њ=Θ.u;
string Ő=$"{ϋ}.{ϊ}.ActionSet.{њ}";string Ҝ=$"{ѓ} {њ}";string о,ł;Ơ Ƿ=null;Ȫ қ=null;Ǻ Қ=null;int ï=τ.Get(Ő,$"DelayOn").ToInt32(
);int î=τ.Get(Ő,$"DelayOff").ToInt32();if(ï!=0||î!=0){ð ʟ=new ð(this);ʟ.ï=ï;ʟ.î=î;Θ.ț(ʟ);}Ě=τ.Get(Ő,$"IGCChannel");if(!Ě.
IsEmpty){string æ=Ě.ToString();ê ҝ=new ê(IGC,æ);Ě=τ.Get(Ő,$"IGCMessageOn");if(!Ě.IsEmpty){ҝ.é=Ě.ToString();}Ě=τ.Get(Ő,
$"IGCMessageOff");if(!Ě.IsEmpty){ҝ.è=Ě.ToString();}if(ҝ.U()){Θ.ț(ҝ);}else{å.ƕ($"{Ҝ} has configuration for sending an IGC message "+
$"on the channel '{æ}', but does not have readable config on what "+$"messages should be sent.");}}о="ActionSetsLinkedToOn";Ě=τ.Get(Ő,о);if(!Ě.IsEmpty){ł=$"{Ҝ}'s {о} list";ń(Ě.ToString(),
ł,ѧ,ҡ);foreach(KeyValuePair<string,bool>ħ in ҡ){if(ї.TryGetValue(ħ.Key,out Ƿ)){ä Ң=new ä(Ƿ);Ң.ß(ħ.Value);Θ.ț(Ң);}else{å.ƕ
($"{ł} references the unconfigured ActionSet {ħ.Key}.");}}}о="ActionSetsLinkedToOff";Ě=τ.Get(Ő,о);if(!Ě.IsEmpty){ł=
$"{Ҝ}'s {о} list";ń(Ě.ToString(),ł,ѧ,ҡ);foreach(KeyValuePair<string,bool>ħ in ҡ){if(ї.TryGetValue(ħ.Key,out Ƿ)){ä Ң=new ä(Ƿ);Ң.Þ(ħ.Value)
;Θ.ț(Ң);}else{å.ƕ($"{ł} references the unconfigured ActionSet {ħ.Key}.");}}}о="TriggersLinkedToOn";Ě=τ.Get(Ő,о);if(!Ě.
IsEmpty){ł=$"{Ҝ}'s {о} list";ń(Ě.ToString(),ł,ѧ,ҡ);foreach(KeyValuePair<string,bool>ħ in ҡ){if(і.TryGetValue(ħ.Key,out қ)){Ø ҧ=
new Ø(қ);ҧ.ß(ħ.Value);Θ.ț(ҧ);}else{å.ƕ($"{ł} references the unconfigured ActionSet {ħ.Key}.");}}}о="TriggersLinkedToOff";Ě=
τ.Get(Ő,о);if(!Ě.IsEmpty){ł=$"{Ҝ}'s {о} list";ń(Ě.ToString(),ł,ѧ,ҡ);foreach(KeyValuePair<string,bool>ħ in ҡ){if(і.
TryGetValue(ħ.Key,out қ)){Ø ҧ=new Ø(қ);ҧ.Þ(ħ.Value);Θ.ț(ҧ);}else{å.ƕ($"{ł} references the unconfigured ActionSet {ħ.Key}.");}}}о=
"RaycastPerformedOnState";Ě=τ.Get(Ő,о);if(!Ě.IsEmpty){ł=$"{Ҝ}'s {о} list";ń(Ě.ToString(),ł,ѧ,ҡ);foreach(KeyValuePair<string,bool>ħ in ҡ){if(ѕ.
TryGetValue(ħ.Key,out Қ)){ā Ҧ=new ā(Қ);if(ħ.Value){Ҧ.ÿ=true;}else{Ҧ.þ=true;}Θ.ț(Ҧ);}else{å.ƕ(
$"{ł} references the unconfigured Raycaster {ħ.Key}.");}}}}foreach(string ҥ in Ѥ){і.Remove(ҥ);}foreach(string Ҥ in ѣ){і.Remove(Ҥ);}if(å.ƞ()==0&&ј.Count==0&&ї.Count==0){å.ƕ(
$"No readable configuration found on the programmable block.");}}int ң(Ɓ å,ф Ŏ,Dictionary<string,w>ј,Dictionary<string,Ơ>ї,Dictionary<string,Ȫ>і,Dictionary<string,Ǻ>ѕ,Dictionary<
IMyInventory,List<O>>ѿ,Dictionary<string,ɭ>Ѽ,List<ɝ>Ѿ,List<Ƽ>ѽ,Dictionary<string,Ǌ>ѻ,MyIniParseResult ϓ,MyIniValue Ě){List<
IMyTerminalBlock>ϼ=new List<IMyTerminalBlock>();Dictionary<string,Action<IMyTerminalBlock>>ö=Л();List<KeyValuePair<string,bool>>ŀ=new
List<KeyValuePair<string,bool>>();Action<string>ҙ=Ɗ=>å.ƛ(Ɗ);w d;Ơ ͳ;Color ė=Color.White;string[]ч;string Ҍ="";string Ő="";
string ҋ="";string ł="";int Ú=0;bool Ҋ;Ϭ<IMyTerminalBlock>(ϼ,Ɗ=>(Ɗ.IsSameConstructAs(Me)&&MyIni.HasSection(Ɗ.CustomData,ψ)));
if(ϼ.Count<=0){å.ƕ($"No blocks found on this construct with a {ψ} INI section.");}foreach(IMyTerminalBlock Q in ϼ){Action<
string>Ō=ŋ=>{å.ƛ($"Block {Q}, section {Ő}{ŋ}");};if(!τ.TryParse(Q.CustomData,out ϓ)){å.ƛ(
$"Configuration on block '{Q.CustomName}' has been "+$"ignored because of the following parsing error on line {ϓ.LineNo}: "+$"{ϓ.Error}");}else{Ҋ=false;if(τ.ContainsKey(ψ,
"Tallies")){Ҋ=true;Ě=τ.Get(ψ,"Tallies");ч=Ě.ToString().Split(',').Select(Ĵ=>Ĵ.Trim()).ToArray();foreach(string E in ч){if(!ј.
ContainsKey(E)){å.ƛ($"Block '{Q.CustomName}' tried to reference the "+$"unconfigured Tally '{E}'.");}else{d=ј[E];if(d is O){if(!Q.
HasInventory){å.ƛ($"Block '{Q.CustomName}' does not have an "+$"inventory and is not compatible with the Type of "+$"Tally '{E}'.");
}else{for(int ņ=0;ņ<Q.InventoryCount;ņ++){IMyInventory J=Q.GetInventory(ņ);if(!ѿ.ContainsKey(J)){ѿ.Add(J,new List<O>());}
ѿ[J].Add((O)d);}}}else if(d is Å){if(!((Å)d).j(Q)){å.ƛ($"Block '{Q.CustomName}' is not a "+$"{((Å)d).P()} and is not "+
$"compatible with the Type of Tally '{E}'.");}}else{å.ƛ($"Block '{Q.CustomName}' refrenced the Tally "+$"'{E}', which has an unhandled Tally Type. Complain to "+
$"the script writer, this should be impossible.");}}}}if(Q.HasInventory){for(int ņ=0;ņ<Q.InventoryCount;ņ++){if(!τ.ContainsKey(ψ,$"Inv{ņ}Tallies")){}else{Ҋ=true;Ě=τ.Get
(ψ,$"Inv{ņ}Tallies");ч=Ě.ToString().Split(',').Select(Ĵ=>Ĵ.Trim()).ToArray();foreach(string E in ч){if(!ј.ContainsKey(E))
{å.ƛ($"Block '{Q.CustomName}' tried to reference the "+$"unconfigured Tally '{E}' in key Inv{ņ}Tallies.");}else{d=ј[E];if
(!(d is O)){å.ƛ($"Block '{Q.CustomName}' is not compatible "+$"with the Type of Tally '{E}' referenced in key "+
$"Inv{ņ}Tallies.");}else{IMyInventory J=Q.GetInventory(ņ);if(!ѿ.ContainsKey(J)){ѿ.Add(J,new List<O>());}ѿ[J].Add((O)d);}}}}}}if(τ.
ContainsKey(ψ,"ActionSets")){Ҋ=true;Ě=τ.Get(ψ,"ActionSets");ч=Ě.ToString().Split(',').Select(Ĵ=>Ĵ.Trim()).ToArray();foreach(string
E in ч){if(!ї.ContainsKey(E)){å.ƛ($"Block '{Q.CustomName}' tried to reference the "+$"unconfigured ActionSet '{E}'.");}
else{ͳ=ї[E];Ő=$"{ϋ}.{E}";if(!τ.ContainsSection(Ő)){å.ƛ($"Block '{Q.CustomName}' references the ActionSet "+
$"'{E}', but contains no discrete '{Ő}' section that would "+$"define actions.");}else{X ҁ=null;if(τ.ContainsKey(Ő,"Action0Property")){ø Ҏ=new ø(Q);ó Ŋ=null;Ú=0;while(Ú!=-1){Ŋ=ő(å,
Ő,Ú,Q,τ,Ě,Ŏ);if(Ŋ!=null){Ҏ.õ(Ŋ);Ú++;}else{Ú=-1;}}ҁ=Ҏ;}else if(τ.ContainsKey(Ő,"ActionsOn")||τ.ContainsKey(Ő,"ActionsOff")
){ü Ҙ=new ü(Q);Ҙ.û=ĺ(τ,Ő,"ActionsOn",ö,å,Q);Ҙ.ú=ĺ(τ,Ő,"ActionsOff",ö,å,Q);ҁ=Ҙ;}else if(τ.ContainsKey(Ő,"ActionOn")||τ.
ContainsKey(Ő,"ActionOff")){n җ=new n(Q);Ě=τ.Get(Ő,"ActionOn");if(!Ě.IsEmpty){җ.Ò=Ň(Ě.ToString(),ö,å,Q,Ő,"ActionOn");}Ě=τ.Get(Ő,
"ActionOff");if(!Ě.IsEmpty){җ.ý=Ň(Ě.ToString(),ö,å,Q,Ő,"ActionOff");}ҁ=җ;}if(ҁ.U()){ͳ.ț(ҁ);}else{å.ƛ(
$"Block '{Q.CustomName}', discrete section '{Ő}', "+$"does not define any actions to be taken when the ActionSet changes state.");}}}}}if(Q is IMyCameraBlock){if(τ.
ContainsKey(ψ,"Raycasters")){Ҋ=true;Ě=τ.Get(ψ,"Raycasters");ч=Ě.ToString().Split(',').Select(Ĵ=>Ĵ.Trim()).ToArray();foreach(string
E in ч){if(!ѕ.ContainsKey(E)){å.ƛ($"Camera '{Q.CustomName}' tried to reference the "+$"unconfigured Raycaster '{E}'.");}
else{ѕ[E].Ȑ((IMyCameraBlock)Q);}}}}if(Q is IMyTextSurfaceProvider){IMyTextSurfaceProvider Җ=(IMyTextSurfaceProvider)Q;for(
int ņ=0;ņ<Җ.SurfaceCount;ņ++){ҋ=$"Surface{ņ}Pages";if(τ.ContainsKey(ψ,ҋ)){IMyTextSurface ƻ=Җ.GetSurface(ņ);ɭ Η=null;ɝ ɦ=
null;Ҋ=true;Ě=τ.Get(ψ,ҋ);ч=Ě.ToString().Split(',').Select(Ĵ=>Ĵ.Trim()).ToArray();if(ч.Length>1){string ҕ=$"Surface{ņ}MFD";if
(!τ.ContainsKey(ψ,ҕ)){å.ƛ($"Surface provider '{Q.CustomName}', key {ҋ} "+
$"references multiple pages which must be managed by an MFD, "+$"but has no {ҕ} key to define that object's name.");}else{string Ҕ=τ.Get(ψ,ҕ).ToString();if(Ѽ.ContainsKey(Ҕ)){å.ƛ(
$"Surface provider '{Q.CustomName}', key {ҕ} "+$"declares the MFD '{Ҕ}' but this name is already in use.");}else{Η=new ɭ(Ҕ);}}}foreach(string E in ч){Ő=$"{ϋ}.{E}";if(
!τ.ContainsSection(Ő)){å.ƛ($"Surface provider '{Q.CustomName}', key {ҋ} declares the "+
$"page '{E}', but contains no discrete '{Ő}' section that would "+$"configure that page.");}else{ɦ=null;if(τ.ContainsKey(Ő,"Elements")){Ě=τ.Get(Ő,"Elements");ȼ Ǹ=null;List<µ>ғ=new List<
µ>();string[]Ŧ=Ě.ToString().Split(',').Select(Ĵ=>Ĵ.Trim()).ToArray();foreach(string š in Ŧ){if(š.ToLowerInvariant()==
"blank"){ғ.Add(null);}else{if(ј.ContainsKey(š)){ғ.Add(ј[š]);}else if(ї.ContainsKey(š)){ғ.Add(ї[š]);}else if(і.ContainsKey(š)){ғ
.Add(і[š]);}else{å.ƛ($"Surface provider '{Q.CustomName}', "+$"section {Ő} tried to reference the "+
$"unconfigured element '{š}'.");}}}Ǹ=new ȼ(ƻ,ғ);Ě=τ.Get(Ő,"Title");if(!Ě.IsEmpty){Ǹ.ǖ=Ě.ToString();}Ě=τ.Get(Ő,"FontSize");if(!Ě.IsEmpty){Ǹ.ƹ=Ě.
ToSingle();}Ě=τ.Get(Ő,"Font");if(!Ě.IsEmpty){Ǹ.ƺ=Ě.ToString();}Func<string,float>Ғ=(ґ)=>{return(float)(τ.Get(Ő,$"Padding{ґ}").
ToDouble(0));};float ȶ=Ғ("Left");float ȵ=Ғ("Right");float ȴ=Ғ("Top");float ȳ=Ғ("Bottom");Func<string,float,string,float,bool>Ґ=(
ҏ,ё,Ъ,х)=>{if(ё+х>100){å.ƛ($"Surface provider '{Q.CustomName}', "+$"section {Ő} has padding values in excess "+
$"of 100% for edges {ҏ} and {Ъ} "+$"which have been ignored.");return true;}return false;};if(Ґ("Left",ȶ,"Right",ȵ)){ȶ=0;ȵ=0;}if(Ґ("Top",ȴ,"Bottom",ȳ)){ȴ
=0;ȳ=0;}int ȷ=τ.Get(Ő,"Columns").ToInt32(1);bool Ȳ=τ.Get(Ő,"TitleObeysPadding").ToBoolean(false);Ǹ.ȸ(ȷ,ȶ,ȵ,ȴ,ȳ,Ȳ,ž);ɦ=Ǹ;}
else if(τ.ContainsKey(Ő,"Script")){ɞ ϡ=new ɞ(ƻ,τ.Get(Ő,"Script").ToString());ɦ=ϡ;}else if(τ.ContainsKey(Ő,"DataType")){
string Щ=τ.Get(Ő,"DataType").ToString().ToLowerInvariant();Ƀ Ʒ=null;if(Щ=="log"){Ʒ=new ƿ(υ);}else if(Щ=="storage"){Ʒ=new ƾ(
this);}else if(Щ=="customdata"||Щ=="detailinfo"||Щ=="custominfo"){if(!τ.ContainsKey(Ő,"DataSource")){å.ƛ(
$"Surface provider '{Q.CustomName}', section "+$"{Ő} has a DataType of {Щ}, but a missing or "+$"unreadable DataSource.");}else{string ϧ=τ.Get(Ő,"DataSource").
ToString();IMyTerminalBlock à=GridTerminalSystem.GetBlockWithName(ϧ);if(à!=null&&Щ=="customdata"){Ʒ=new Ɂ(à);}else if(à!=null&&Щ
=="detailinfo"){Ʒ=new ǁ(à);}else if(à!=null&&Щ=="custominfo"){Ʒ=new ǀ(à);}else{å.ƛ(
$"Surface provider '{Q.CustomName}', section "+$"{Ő} tried to reference the unknown block '{ϧ}' "+$"as a DataSource.");}}}else if(Щ=="raycaster"){if(!τ.ContainsKey(Ő,
"DataSource")){å.ƛ($"Surface provider '{Q.CustomName}', section "+$"{Ő} has a DataType of {Щ}, but a missing or "+
$"unreadable DataSource.");}else{string ϧ=τ.Get(Ő,"DataSource").ToString();if(ѕ.ContainsKey(ϧ)){Ʒ=new ƽ(ѕ[ϧ]);}else{å.ƛ(
$"Surface provider '{Q.CustomName}', section "+$"{Ő} tried to reference the unknown Raycaster "+$"'{ϧ}' as a DataSource.");}}}else{å.ƛ(
$"Surface provider '{Q.CustomName}', section "+$"{Ő} tried to reference the unknown data type '{Щ}'.");}if(Ʒ!=null){Ƽ Ш=new Ƽ(ƻ,Ʒ,ž);Ě=τ.Get(Ő,"FontSize");if(!Ě.
IsEmpty){Ш.ƹ=Ě.ToSingle();}Ě=τ.Get(Ő,"Font");if(!Ě.IsEmpty){Ш.ƺ=Ě.ToString();}Ě=τ.Get(Ő,"CharPerLine");if(!Ě.IsEmpty){if(Щ==
"detailinfo"||Щ=="custominfo"){å.ƛ($"Surface provider '{Q.CustomName}', section "+
$"{Ő} tried to set a CharPerLine limit with the {Щ} "+$"DataType. This is not allowed.");}else{Ш.Ǒ(Ě.ToInt32());}}if(Щ=="log"){ѽ.Add(Ш);}ɦ=Ш;}}if(ɦ!=null){if(Ŏ.р(Ō,τ,Ő,
"ForeColor",out ė)){((ɰ)ɦ).ɯ=ė;}if(Ŏ.р(Ō,τ,Ő,"BackColor",out ė)){((ɰ)ɦ).ɮ=ė;}}}if(Η!=null&&ɦ!=null){Ě=τ.Get(Ő,"ShowOnActionState");
if(!Ě.IsEmpty){ł=$"Surface provider '{Q.CustomName}', section {Ő}";ń(Ě.ToString(),ł,ҙ,ŀ);foreach(KeyValuePair<string,bool>
ħ in ŀ){if(!ї.TryGetValue(ħ.Key,out ͳ)){å.ƛ($"{ł} tried to reference the unconfigured ActionSet {ħ.Key}.");}else{ą Ч=new
ą(Η);if(ħ.Value){Ч.ă=E;}else{Ч.Ă=E;}ͳ.ț(Ч);}}}Η.ɧ(E,ɦ);}}if(Η!=null){if(Η.ɥ()==0){å.ƛ(
$"Surface provider '{Q.CustomName}' specified "+$"the use of MFD '{Η.u}' but did not provide readable "+$"page configuration for that MFD.");}else{Ѽ.Add(Η.u,Η);ɦ=Η;Η.ɢ
(σ.Get("MFDs",Η.u).ToString());}}if(ɦ!=null){Ѿ.Add(ɦ);}}}}if(Q is IMyLightingBlock){Ě=τ.Get(ψ,"IndicatorElement");if(!Ě.
IsEmpty){Ҍ=Ě.ToString();µ š=null;if(ј.ContainsKey(Ҍ)){š=ј[Ҍ];}else if(ї.ContainsKey(Ҍ)){š=ї[Ҍ];}else if(і.ContainsKey(Ҍ)){š=і[Ҍ
];}else{å.ƛ($"Lighting block '{Q.CustomName}' tried to reference "+$"the unconfigured element '{Ҍ}'.");}if(š!=null){if(!ѻ
.ContainsKey(Ҍ)){ѻ.Add(Ҍ,new Ǌ(š));}ѻ[Ҍ].ǈ((IMyLightingBlock)Q);}}else if(!Ҋ){å.ƛ(
$"Lighting block {Q.CustomName} has missing or unreadable "+$"IndicatorElement.");}Ҋ=true;}if(!Ҋ){å.ƛ($"Block '{Q.CustomName}' is missing proper configuration or is a "+
$"block type that cannot be handled by this script.");}}}return ϼ.Count;}abstract class Ц{public string Ф{get;private set;}protected int У{get;private set;}protected
MyGridProgram í{get;private set;}protected IEnumerator<string>Т;public int С{get;private set;}public int Р{get;private set;}public
string П{get;protected set;}public bool О{get;private set;}public Ц(MyGridProgram í,string ϴ,double Х,bool Ы){this.í=í;Ф=ϴ;У=(
int)(í.Runtime.MaxInstructionCount*Х);С=0;Р=0;П=$"{Ф} waiting to begin";О=Ы;}internal abstract void г();internal bool н(){
return Т.MoveNext();}protected bool м(){if(í.Runtime.CurrentInstructionCount>У){л();return true;}else{return false;}}protected
void л(){С++;Р+=í.Runtime.CurrentInstructionCount;}internal void к(){Т.Dispose();П=$"{Ф} completed.";}internal abstract
string й();protected string и(){return$"{Ф} used a total of {Р} / {í.Runtime.MaxInstructionCount} "+
$"({(int)(((double)Р/í.Runtime.MaxInstructionCount)*100)}%) "+$"of instructions allowed in one tic, distributed over {С} tics.";}}class з:Ц{const int ж=20;const double е=4;ɝ[]д;
public з(MyGridProgram í,ɝ[]д,bool Ы):base(í,"Sprite Refresher",.1,Ы){this.д=д;}internal override void г(){Т=в();П=
$"{Ф} started";}IEnumerator<string>в(){int б=Math.Min((int)(Math.Ceiling(д.Length/е)),ж);int Ĭ=0;int а=б;foreach(ɝ Ǹ in д){Ǹ.ɱ();Ǹ.Ǉ()
;Ĭ++;if(Ĭ>=а){а+=б;П=$"{Ф} report {Ĭ}/{д.Length}";л();yield return П;}}}internal override string й(){return
$"{Ф} finished. Re-sync'd sprites on {д.Length} surfaces.\n"+$"{и()}";}}string Я(){List<string>Ю=new List<string>();string Э=$"{ϋ}.{ϊ}";τ.GetSections(Ю);foreach(string ˑ in Ю){if(ˑ
.Contains(Э)){τ.DeleteSection(ˑ);}}return τ.ToString();}string Ь(List<string>Ŧ,string ʀ,int Н,StringBuilder ž){string ĩ=
"";int М=0;ž.Clear();if(Ŧ.Count>0){ž.Append($"{ʀ} = ");if(Ŧ.Count>Н){М=Н;}foreach(string š in Ŧ){if(М>=Н){ž.Append("\n|");
М=0;}ž.Append($"{š}, ");М++;}ĩ=ž.ToString();ĩ=ĩ.Remove(ĩ.Length-2);}return ĩ;}Dictionary<string,Action<IMyTerminalBlock>>
Л(){Dictionary<string,Action<IMyTerminalBlock>>ö=new Dictionary<string,Action<IMyTerminalBlock>>(StringComparer.
OrdinalIgnoreCase);string К;string Й="Enable";string И="Positive";string З="Negative";ö.Add($"{Й}On",Ɗ=>((IMyFunctionalBlock)Ɗ).Enabled=
true);ö.Add($"{Й}Off",Ɗ=>((IMyFunctionalBlock)Ɗ).Enabled=false);К="Battery";Й="charge";ö.Add($"{К}Auto",Ɗ=>((IMyBatteryBlock
)Ɗ).ChargeMode=ChargeMode.Auto);ö.Add($"{К}Re{Й}",Ɗ=>((IMyBatteryBlock)Ɗ).ChargeMode=ChargeMode.Recharge);ö.Add(
$"{К}Dis{Й}",Ɗ=>((IMyBatteryBlock)Ɗ).ChargeMode=ChargeMode.Discharge);К="Connector";ö.Add($"{К}Lock",Ɗ=>((IMyShipConnector)Ɗ).
Connect());ö.Add($"{К}Unlock",Ɗ=>((IMyShipConnector)Ɗ).Disconnect());К="Door";ö.Add($"{К}Open",Ɗ=>((IMyDoor)Ɗ).OpenDoor());ö.
Add($"{К}Close",Ɗ=>((IMyDoor)Ɗ).CloseDoor());К="Tank";Й="Stockpile";ö.Add($"{К}{Й}On",Ɗ=>((IMyGasTank)Ɗ).Stockpile=true);ö.
Add($"{К}{Й}Off",Ɗ=>((IMyGasTank)Ɗ).Stockpile=false);К="Gyro";string Ж="Stabilize";Й="Override";ö.Add($"{К}{Й}On",Ɗ=>((
IMyGyro)Ɗ).GyroOverride=true);ö.Add($"{К}{Й}Off",Ɗ=>((IMyGyro)Ɗ).GyroOverride=false);ö.Add($"{К}Yaw{И}",Ɗ=>((IMyGyro)Ɗ).Yaw=
9000);ö.Add($"{К}Yaw{Ж}",Ɗ=>((IMyGyro)Ɗ).Yaw=0);ö.Add($"{К}Yaw{З}",Ɗ=>((IMyGyro)Ɗ).Yaw=-9000);Й="Pitch";ö.Add($"{К}{Й}{И}",Ɗ
=>((IMyGyro)Ɗ).Pitch=-9000);ö.Add($"{К}{Й}{Ж}",Ɗ=>((IMyGyro)Ɗ).Pitch=0);ö.Add($"{К}{Й}{З}",Ɗ=>((IMyGyro)Ɗ).Pitch=9000);Й=
"Roll";ö.Add($"{К}{Й}{И}",Ɗ=>((IMyGyro)Ɗ).Roll=9000);ö.Add($"{К}{Й}{Ж}",Ɗ=>((IMyGyro)Ɗ).Roll=0);ö.Add($"{К}{Й}{З}",Ɗ=>((
IMyGyro)Ɗ).Roll=-9000);К="Gear";Й="AutoLock";ö.Add($"{К}{Й}On",Ɗ=>((IMyLandingGear)Ɗ).AutoLock=true);ö.Add($"{К}{Й}Off",Ɗ=>((
IMyLandingGear)Ɗ).AutoLock=false);ö.Add($"{К}Lock",Ɗ=>((IMyLandingGear)Ɗ).Lock());ö.Add($"{К}Unlock",Ɗ=>((IMyLandingGear)Ɗ).Unlock());
К="JumpDrive";Й="Recharge";ö.Add($"{К}{Й}On",Ɗ=>((IMyJumpDrive)Ɗ).Recharge=true);ö.Add($"{К}{Й}Off",Ɗ=>((IMyJumpDrive)Ɗ).
Recharge=false);К="Parachute";ö.Add($"{К}Open",Ɗ=>((IMyParachute)Ɗ).OpenDoor());ö.Add($"{К}Close",Ɗ=>((IMyParachute)Ɗ).CloseDoor
());Й="AutoDeploy";ö.Add($"{К}{Й}On",Ɗ=>((IMyParachute)Ɗ).AutoDeploy=true);ö.Add($"{К}{Й}Off",Ɗ=>((IMyParachute)Ɗ).
AutoDeploy=false);К="Piston";ö.Add($"{К}Extend",Ɗ=>((IMyPistonBase)Ɗ).Extend());ö.Add($"{К}Retract",Ɗ=>((IMyPistonBase)Ɗ).Retract(
));К="Rotor";ö.Add($"{К}Lock",Ɗ=>((IMyMotorStator)Ɗ).RotorLock=true);ö.Add($"{К}Unlock",Ɗ=>((IMyMotorStator)Ɗ).RotorLock=
false);ö.Add($"{К}Reverse",Ɗ=>((IMyMotorStator)Ɗ).TargetVelocityRPM=((IMyMotorStator)Ɗ).TargetVelocityRPM*-1);ö.Add($"{К}{И}"
,Ɗ=>((IMyMotorStator)Ɗ).TargetVelocityRPM=Math.Abs(((IMyMotorStator)Ɗ).TargetVelocityRPM));ö.Add($"{К}{З}",Ɗ=>((
IMyMotorStator)Ɗ).TargetVelocityRPM=Math.Abs(((IMyMotorStator)Ɗ).TargetVelocityRPM)*-1);К="Sorter";Й="Drain";ö.Add($"{К}{Й}On",Ɗ=>((
IMyConveyorSorter)Ɗ).DrainAll=true);ö.Add($"{К}{Й}Off",Ɗ=>((IMyConveyorSorter)Ɗ).DrainAll=false);К="Sound";ö.Add($"{К}Play",Ɗ=>((
IMySoundBlock)Ɗ).Play());ö.Add($"{К}Stop",Ɗ=>((IMySoundBlock)Ɗ).Stop());К="Thruster";Й="Override";ö.Add($"{К}{Й}Max",Ɗ=>((IMyThrust)Ɗ
).ThrustOverridePercentage=1);ö.Add($"{К}{Й}Off",Ɗ=>((IMyThrust)Ɗ).ThrustOverridePercentage=0);К="Timer";ö.Add(
$"{К}Trigger",Ɗ=>((IMyTimerBlock)Ɗ).Trigger());ö.Add($"{К}Start",Ɗ=>((IMyTimerBlock)Ɗ).StartCountdown());ö.Add($"{К}Stop",Ɗ=>((
IMyTimerBlock)Ɗ).StopCountdown());К="Turret";string Ǡ="Controller";string Ŀ="Target";Й="Meteors";ö.Add($"{К}{Ŀ}{Й}On",Ɗ=>((
IMyLargeTurretBase)Ɗ).TargetMeteors=true);ö.Add($"{К}{Ŀ}{Й}Off",Ɗ=>((IMyLargeTurretBase)Ɗ).TargetMeteors=false);ö.Add($"{Ǡ}{Ŀ}{Й}On",Ɗ=>((
IMyTurretControlBlock)Ɗ).TargetMeteors=true);ö.Add($"{Ǡ}{Ŀ}{Й}Off",Ɗ=>((IMyTurretControlBlock)Ɗ).TargetMeteors=false);Й="Missiles";ö.Add(
$"{К}{Ŀ}{Й}On",Ɗ=>((IMyLargeTurretBase)Ɗ).TargetMissiles=true);ö.Add($"{К}{Ŀ}{Й}Off",Ɗ=>((IMyLargeTurretBase)Ɗ).TargetMissiles=false);
ö.Add($"{Ǡ}{Ŀ}{Й}On",Ɗ=>((IMyTurretControlBlock)Ɗ).TargetMissiles=true);ö.Add($"{Ǡ}{Ŀ}{Й}Off",Ɗ=>((IMyTurretControlBlock)
Ɗ).TargetMissiles=false);Й="SmallGrids";ö.Add($"{К}{Ŀ}{Й}On",Ɗ=>((IMyLargeTurretBase)Ɗ).TargetSmallGrids=true);ö.Add(
$"{К}{Ŀ}{Й}Off",Ɗ=>((IMyLargeTurretBase)Ɗ).TargetSmallGrids=false);ö.Add($"{Ǡ}{Ŀ}{Й}On",Ɗ=>((IMyTurretControlBlock)Ɗ).TargetSmallGrids=
true);ö.Add($"{Ǡ}{Ŀ}{Й}Off",Ɗ=>((IMyTurretControlBlock)Ɗ).TargetSmallGrids=false);Й="LargeGrids";ö.Add($"{К}{Ŀ}{Й}On",Ɗ=>((
IMyLargeTurretBase)Ɗ).TargetLargeGrids=true);ö.Add($"{К}{Ŀ}{Й}Off",Ɗ=>((IMyLargeTurretBase)Ɗ).TargetLargeGrids=false);ö.Add($"{Ǡ}{Ŀ}{Й}On"
,Ɗ=>((IMyTurretControlBlock)Ɗ).TargetLargeGrids=true);ö.Add($"{Ǡ}{Ŀ}{Й}Off",Ɗ=>((IMyTurretControlBlock)Ɗ).
TargetLargeGrids=false);Й="Characters";ö.Add($"{К}{Ŀ}{Й}On",Ɗ=>((IMyLargeTurretBase)Ɗ).TargetCharacters=true);ö.Add($"{К}{Ŀ}{Й}Off",Ɗ=>(
(IMyLargeTurretBase)Ɗ).TargetCharacters=false);ö.Add($"{Ǡ}{Ŀ}{Й}On",Ɗ=>((IMyTurretControlBlock)Ɗ).TargetCharacters=true);
ö.Add($"{Ǡ}{Ŀ}{Й}Off",Ɗ=>((IMyTurretControlBlock)Ɗ).TargetCharacters=false);Й="Stations";ö.Add($"{К}{Ŀ}{Й}On",Ɗ=>((
IMyLargeTurretBase)Ɗ).TargetStations=true);ö.Add($"{К}{Ŀ}{Й}Off",Ɗ=>((IMyLargeTurretBase)Ɗ).TargetStations=false);ö.Add($"{Ǡ}{Ŀ}{Й}On",Ɗ=>
((IMyTurretControlBlock)Ɗ).TargetStations=true);ö.Add($"{Ǡ}{Ŀ}{Й}Off",Ɗ=>((IMyTurretControlBlock)Ɗ).TargetStations=false)
;Й="Neutrals";ö.Add($"{К}{Ŀ}{Й}On",Ɗ=>((IMyLargeTurretBase)Ɗ).TargetNeutrals=true);ö.Add($"{К}{Ŀ}{Й}Off",Ɗ=>((
IMyLargeTurretBase)Ɗ).TargetNeutrals=false);ö.Add($"{Ǡ}{Ŀ}{Й}On",Ɗ=>((IMyTurretControlBlock)Ɗ).TargetNeutrals=true);ö.Add($"{Ǡ}{Ŀ}{Й}Off",
Ɗ=>((IMyTurretControlBlock)Ɗ).TargetNeutrals=false);Й="Enemies";ö.Add($"{К}{Ŀ}{Й}On",Ɗ=>((IMyLargeTurretBase)Ɗ).
TargetEnemies=true);ö.Add($"{К}{Ŀ}{Й}Off",Ɗ=>((IMyLargeTurretBase)Ɗ).TargetEnemies=false);ö.Add($"{Ǡ}{Ŀ}{Й}On",Ɗ=>Ɗ.SetValue(
"TargetEnemies",true));ö.Add($"{Ǡ}{Ŀ}{Й}Off",Ɗ=>Ɗ.SetValue("TargetEnemies",false));string ѐ="Subsystem";Й="Default";ö.Add($"{К}{ѐ}{Й}",
Ɗ=>((IMyLargeTurretBase)Ɗ).SetTargetingGroup(""));ö.Add($"{Ǡ}{ѐ}{Й}",Ɗ=>((IMyTurretControlBlock)Ɗ).SetTargetingGroup(""))
;Й="Weapons";ö.Add($"{К}{ѐ}{Й}",Ɗ=>((IMyLargeTurretBase)Ɗ).SetTargetingGroup(Й));ö.Add($"{Ǡ}{ѐ}{Й}",Ɗ=>((
IMyTurretControlBlock)Ɗ).SetTargetingGroup(Й));Й="Propulsion";ö.Add($"{К}{ѐ}{Й}",Ɗ=>((IMyLargeTurretBase)Ɗ).SetTargetingGroup(Й));ö.Add(
$"{Ǡ}{ѐ}{Й}",Ɗ=>((IMyTurretControlBlock)Ɗ).SetTargetingGroup(Й));Й="PowerSystems";ö.Add($"{К}{ѐ}{Й}",Ɗ=>((IMyLargeTurretBase)Ɗ).
SetTargetingGroup(Й));ö.Add($"{Ǡ}{ѐ}{Й}",Ɗ=>((IMyTurretControlBlock)Ɗ).SetTargetingGroup(Й));К="Vent";Й="pressurize";ö.Add($"{К}{Й}",Ɗ=>(
(IMyAirVent)Ɗ).Depressurize=false);ö.Add($"{К}De{Й}",Ɗ=>((IMyAirVent)Ɗ).Depressurize=true);К="Warhead";ö.Add($"{К}Arm",Ɗ
=>((IMyWarhead)Ɗ).IsArmed=true);ö.Add($"{К}Disarm",Ɗ=>((IMyWarhead)Ɗ).IsArmed=false);Й="Countdown";ö.Add($"{К}{Й}Start",Ɗ
=>((IMyWarhead)Ɗ).StartCountdown());ö.Add($"{К}{Й}Stop",Ɗ=>((IMyWarhead)Ɗ).StopCountdown());ö.Add($"{К}Detonate",Ɗ=>((
IMyWarhead)Ɗ).Detonate());ö.Add("WeaponFireOnce",Ɗ=>((IMyUserControllableGun)Ɗ).ShootOnce());К="Suspension";Й="Height";ö.Add(
$"{К}{Й}{И}",Ɗ=>((IMyMotorSuspension)Ɗ).Height=9000);ö.Add($"{К}{Й}{З}",Ɗ=>((IMyMotorSuspension)Ɗ).Height=-9000);ö.Add($"{К}{Й}Zero"
,Ɗ=>((IMyMotorSuspension)Ɗ).Height=0);Й="Propulsion";ö.Add($"{К}{Й}{И}",Ɗ=>((IMyMotorSuspension)Ɗ).PropulsionOverride=1);
ö.Add($"{К}{Й}{З}",Ɗ=>((IMyMotorSuspension)Ɗ).PropulsionOverride=-1);ö.Add($"{К}{Й}Zero",Ɗ=>((IMyMotorSuspension)Ɗ).
PropulsionOverride=0);return ö;}class ф{Dictionary<string,Ŝ>Ŏ;public ф(StringComparer ɶ=null){if(ɶ!=null){Ŏ=new Dictionary<string,Ŝ>(ɶ);}
else{Ŏ=new Dictionary<string,Ŝ>();}у("Cozy",255,225,200);у("Black",0,0,0);Color ŭ=у("Green",25,225,100);Color Ŭ=у(
"LightBlue",100,200,225);Color ū=у("Yellow",255,255,0);Color č=у("Orange",255,150,0);Color Â=у("Red",255,0,0);Ŏ.Add("LowGood",new Ű
(ŭ,Ŭ,ū,č,Â));Ŏ.Add("HighGood",new Ů(ŭ,Ŭ,ū,č,Â));}private Color у(string E,int ƌ,int Ƌ,int Ɗ){Color с=new Color(ƌ,Ƌ,Ɗ);Ŏ.
Add(E,new À(с,E));return с;}public bool р(Action<string>Ł,MyIni ŏ,string п,string о,out Color ė){Ŝ т;bool ц=ы(Ł,ŏ,п,о,out т
);if(ц){ė=т.ś(-1);}else{ė=Color.White;}return ц;}public bool ы(Action<string>Ł,MyIni ŏ,string п,string о,out Ŝ т){
MyIniValue Ě=ŏ.Get(п,о);т=null;if(!Ě.IsEmpty){string ю=Ě.ToString();if(Ŏ.TryGetValue(ю,out т)){return true;}else{string[]Ŧ=ю.Split
(',').Select(Ĵ=>Ĵ.Trim()).ToArray();if(Ŧ.Length==3){int[]э=new int[3];bool ь=false;for(int ņ=0;ņ<=2;ņ++){if(!Int32.
TryParse(Ŧ[ņ],out э[ņ])){ь=true;Ł($", key {о}, element {ņ} could not be parsed"+" as an integer.");}}if(ь){return false;}else{т=
new À(new Color(э[0],э[1],э[2]));Ŏ.Add(ю,т);return true;}}else{Ł($", key {о} does not match a pre-defined color and "+
$"does not have three elements like a custom color.");return false;}}}else{return false;}}public Ŝ я(string E){return Ŏ[E];}}bool ъ(string Б,string о,string щ,bool Κ,ref
bool δ,bool ε,string К,Ɓ å){MyIniValue Ě;Ě=τ.Get(Б,о);if(Ě.IsEmpty){τ.Set(Б,о,щ);if(Κ){τ.SetComment(Б,о,
"-----------------------------------------");}δ=true;if(ε){å.Ɩ($"'{о}' key was missing from '{Б}' section of "+$"block '{К}' and has been re-generated.");}return
false;}return true;}bool ш(HashSet<string>ч,string E,string ĝ,Ɓ å){if(E.ToLowerInvariant()=="blank"){å.ƕ(
$"{ĝ} tried to use the Element name '{E}', "+"which is reserved by the script to indicate portions of a Report that should "+
"be left empty. Please choose a different name.");return true;}else if(ч.Contains(E)){å.ƕ($"{ĝ} tried to use the Element name '{E}', "+
$"which has already been claimed. All Element providers (Tally, ActionSet, "+$"Trigger, Raycaster) must have their own, unique names.");return true;}else{return false;}}void ń(string Ń,string ł,
Action<string>Ł,List<KeyValuePair<string,bool>>ŀ){string Ŀ="";bool ľ=false;bool Ľ;ŀ.Clear();string[]ļ=Ń.Split(',').Select(Ĵ=>Ĵ
.Trim()).ToArray();foreach(string ħ in ļ){Ľ=false;string[]Ņ=ħ.Split(':').Select(Ĵ=>Ĵ.Trim()).ToArray();Ŀ=Ņ[0];if(Ņ.Length
<2){Ľ=true;Ł($"{ł} does not provide a state for the component "+$"'{Ŀ}'. Valid states are 'on' and 'off'.");}else if(Ņ[1]
.ToLowerInvariant()=="on"){ľ=true;}else if(Ņ[1].ToLowerInvariant()=="off"){ľ=false;}else{Ľ=true;Ł(
$"{ł} attempts to set '{Ŀ}' to the invalid state "+$"'{Ņ[1]}'. Valid states are 'on' and 'off'.");}if(!Ľ){ŀ.Add(new KeyValuePair<string,bool>(Ŀ,ľ));}}}List<Action<
IMyTerminalBlock>>ĺ(MyIni Ĺ,string ĸ,string ķ,Dictionary<string,Action<IMyTerminalBlock>>ö,Ɓ å,IMyTerminalBlock Q){MyIniValue Ě=Ĺ.Get(ĸ,
ķ);List<Action<IMyTerminalBlock>>Ķ=null;if(!Ě.IsEmpty){string[]ĵ=null;Ķ=new List<Action<IMyTerminalBlock>>();ĵ=Ě.ToString
().Split(',').Select(Ĵ=>Ĵ.Trim()).ToArray();foreach(string Ļ in ĵ){Ķ.Add(Ň(Ļ,ö,å,Q,ĸ,ķ));}}return Ķ;}Action<
IMyTerminalBlock>Ň(string Ļ,Dictionary<string,Action<IMyTerminalBlock>>ö,Ɓ å,IMyTerminalBlock Q,string ĸ,string ķ){Action<
IMyTerminalBlock>Œ=null;if(ö.ContainsKey(Ļ)){Œ=ö[Ļ];}else{å.ƛ($"Block '{Q.CustomName}', discrete section '{ĸ}', "+
$"references the unknown action '{Ļ}' as its {ķ}.");}return Œ;}ó ő(Ɓ å,string Ő,int Ĭ,IMyTerminalBlock Q,MyIni ŏ,MyIniValue Ě,ф Ŏ){string ō=$"Action{Ĭ}Property";Action<
string>Ō=ŋ=>{å.ƛ($"Block {Q.CustomName}, section {Ő}{ŋ}");};Ě=ŏ.Get(Ő,ō);ó Ŋ=null;if(!Ě.IsEmpty){string ŉ=Ě.ToString(
"<missing>");ITerminalProperty ň=Q.GetProperty(ŉ);if(ň==null){å.ƛ($"Block '{Q.CustomName}', section '{Ő}', "+
$"references the unknown property '{ŉ}' as its {ō}.");Ŋ=new ó<bool>(ŉ);}else if(ň.TypeName.ToLowerInvariant()=="boolean"){ó<bool>ĳ=new ó<bool>(ŉ);bool Ĳ=false;Ě=ŏ.Get(Ő,
$"Action{Ĭ}ValueOn");if(!Ě.IsEmpty&&Ě.TryGetBoolean(out Ĳ)){ĳ.Ĉ(Ĳ);}Ě=ŏ.Get(Ő,$"Action{Ĭ}ValueOff");if(!Ě.IsEmpty&&Ě.TryGetBoolean(out Ĳ)){
ĳ.Ć(Ĳ);}Ŋ=ĳ;}else if(ň.TypeName.ToLowerInvariant()=="stringbuilder"){goto PretendThereWasNoPart;}else if(ň.TypeName.
ToLowerInvariant()=="int64"){ó<long>ĳ=new ó<long>(ŉ);long Ĳ=-1;Ě=ŏ.Get(Ő,$"Action{Ĭ}ValueOn");if(!Ě.IsEmpty&&Ě.TryGetInt64(out Ĳ)){ĳ.Ĉ(Ĳ
);}Ě=ŏ.Get(Ő,$"Action{Ĭ}ValueOff");if(!Ě.IsEmpty&&Ě.TryGetInt64(out Ĳ)){ĳ.Ć(Ĳ);}Ŋ=ĳ;}else if(ň.TypeName.ToLowerInvariant(
)=="single"){ó<float>ĳ=new ó<float>(ŉ);float Ĳ=-1;Ě=ŏ.Get(Ő,$"Action{Ĭ}ValueOn");if(!Ě.IsEmpty&&Ě.TryGetSingle(out Ĳ)){ĳ.
Ĉ(Ĳ);}Ě=ŏ.Get(Ő,$"Action{Ĭ}ValueOff");if(!Ě.IsEmpty&&Ě.TryGetSingle(out Ĳ)){ĳ.Ć(Ĳ);}Ŋ=ĳ;}else if(ň.TypeName.
ToLowerInvariant()=="color"){ó<Color>ĳ=new ó<Color>(ŉ);Color Ĳ;if(Ŏ.р(Ō,ŏ,Ő,$"Action{Ĭ}ValueOn",out Ĳ)){ĳ.Ĉ(Ĳ);}if(Ŏ.р(Ō,ŏ,Ő,
$"Action{Ĭ}ValueOff",out Ĳ)){ĳ.Ć(Ĳ);}Ŋ=ĳ;}else{å.ƕ($"Block '{Q.CustomName}', discrete section '{Ő}', "+
$"references the property '{ŉ}' which uses the non-standard "+$"type {ň.TypeName}. Report this to the scripter, as the script "+$"will need to be altered to handle this.");Ŋ=new ó<
bool>(ŉ);}if(!Ŋ.ò()&&ň!=null){å.ƛ($"Block '{Q.CustomName}', discrete section '{Ő}', "+
$"does not specify a working Action{Ĭ}ValueOn or Action{Ĭ}ValueOff for "+$"the property '{ŉ}'. If one was specified, make sure that it matches the "+$"type '{ň.TypeName}.'");}
PretendThereWasNoPart:return Ŋ;}else{return Ŋ;}}void Ď(Ȫ Ğ,string ĝ,bool Ĝ,string ě,MyIniValue Ě,Ɓ å){double ć;Ě=τ.Get(ĝ,$"{ě}Value");if(!Ě.
IsEmpty){ć=Ě.ToDouble();Ě=τ.Get(ĝ,$"{ě}Command");if(!Ě.IsEmpty){string ę=Ě.ToString().ToLowerInvariant();if(ę=="on"){Ğ.ȃ(Ĝ,ć,
true);}else if(ę=="off"){Ğ.ȃ(Ĝ,ć,false);}else if(ę=="switch"){å.ƕ($"{ĝ}: {Ğ.u} specifies "+
$"a {ě}Command of 'switch', which cannot be used for triggers.");}else{å.ƕ($"{ĝ}: {Ğ.u} has a missing "+$"or invalid {ě}Command. Valid commands are 'on' and 'off'.");}}else{å.ƕ(
$"{ĝ}: {Ğ.u} specifies a "+$"{ě}Value but no {ě}Command.");}}}static string Ę(Color ė){if(ė==Ϙ){return"cozy";}else if(ė==ϝ){return"green";}else if
(ė==Ϝ){return"lightBlue";}else if(ė==ϛ){return"yellow";}else if(ė==Ϛ){return"orange";}else if(ė==ϙ){return"red";}else{
return$"{ė.R}, {ė.G}, {ė.B}";}}class Ė{Dictionary<string,Ħ>ğ;Dictionary<string,Ģ>ĕ;internal Ė(){ğ=new Dictionary<string,Ħ>();ĕ
=new Dictionary<string,Ģ>();}internal bool Ĕ(string E,Ħ Ē){if(ğ.ContainsKey(E)){return false;}else{ğ.Add(E,Ē);return true
;}}internal void ē(string E,Ħ Ē){if(ğ.ContainsKey(E)){ğ[E]=Ē;}else{ğ.Add(E,Ē);}}internal int đ(string E){Ħ Đ;if(ğ.
TryGetValue(E,out Đ)){return Đ.ĥ;}else{return-1;}}internal void ď(string E){ğ.Remove(E);}internal void ġ(){ğ.Clear();}internal bool
Ĩ(string E,int İ,out string į){Ģ Į;if(!ĕ.TryGetValue(E,out Į)){Ģ ĭ=new Ģ(İ,E);ĕ.Add(E,ĭ);į="";return true;}else{į=Į.Ƈ();
return false;}}internal void ģ(){foreach(Ħ Ē in ğ.Values){Ē.ģ();}Ģ Ī;int Ĭ=0;while(Ĭ<ĕ.Count){Ī=ĕ.Values.ElementAt(Ĭ);if(Ī.ƈ()
){ĕ.Remove(Ī.E);}else{Ĭ++;}}}public string ī(string E){Ģ Ī;if(ĕ.TryGetValue(E,out Ī)){return Ī.Ƈ();}else{return
$"{E} is not on cooldown.";}}internal string ı(){string ĩ="Contained periodics:\n";foreach(KeyValuePair<string,Ħ>ħ in ğ){ĩ+=
$" -{ħ.Key} with frequency {ħ.Value.ĥ}\n";}ĩ+="Contained cooldowns:\n";foreach(KeyValuePair<string,Ģ>ħ in ĕ){ĩ+=
$" -{ħ.Key} with a remaining duration of {ħ.Value.Ġ}\n";}return ĩ;}}class Ħ{internal int ĥ{get;private set;}int Ġ;Action Ĥ;internal Ħ(int ĥ,Action Ĥ){this.ĥ=ĥ;Ġ=ĥ;this.Ĥ=Ĥ;}
internal void ģ(){Ġ--;if(Ġ<=0){Ġ=ĥ;Ĥ.Invoke();}}}class Ģ{public int Ġ{get;private set;}internal string E{get;private set;}
internal Ģ(int İ,string E){Ġ=İ;this.E=E;}internal bool ƈ(){Ġ--;if(Ġ<=0){return true;}else{return false;}}internal string Ƈ(){
return$"{E} is on cooldown for the next {(int)(Ġ*1.4)} seconds.";}}void Ɔ(int ƅ=0,Ė Ƅ=null){Action ƃ=()=>{É();Ǉ();υ.ģ();};Ħ Ƃ=
new Ħ(ƅ,ƃ);ζ.ē("UpdateDelay",Ƃ);υ.ǝ=ƅ;}class Ɓ{StringBuilder ž;int ƀ;List<string>Ɖ,ſ,Ž;int ż,Ż,ź;public string Ź{get;
private set;}public string Ÿ{get;private set;}public string ŷ{get;private set;}public Color Ŷ{set{Ź=ų(value);}}public Color ŵ{
set{Ÿ=ų(value);}}public Color Ŵ{set{ŷ=ų(value);}}public Ɓ(StringBuilder ž,int ƀ){this.ž=ž;this.ƀ=ƀ;ż=0;Ż=0;ź=0;Ɖ=new List<
string>();ſ=new List<string>();Ž=new List<string>();Ź=Ǝ(255,255,0,0);Ÿ=Ǝ(255,255,255,0);ŷ=Ǝ(255,100,200,225);}public void ƕ(
string Ɵ){if(Ɖ.Count<ƀ){Ɖ.Add(Ɵ);}else{ż++;}}public int ƞ(){return Ɖ.Count+ż;}public void Ɲ(){ż=0;Ɖ.Clear();}public string Ɯ()
{string Ɨ;ž.Clear();foreach(string Ũ in Ɖ){ž.Append($" -{Ũ}\n");}if(ż>0){ž.Append($" -And {ż} other errors.\n");}Ɨ=ž.
ToString();ž.Clear();return Ɨ;}public void ƛ(string č){if(ſ.Count<ƀ){ſ.Add(č);}else{Ż++;}}public int ƚ(){return ſ.Count+Ż;}
public void ƙ(){Ż=0;ſ.Clear();}public string Ƙ(){string Ɨ;ž.Clear();foreach(string Ũ in ſ){ž.Append($" -{Ũ}\n");}if(Ż>0){ž.
Append($" -And {Ż} other warnings.\n");}Ɨ=ž.ToString();ž.Clear();return Ɨ;}public void Ɩ(string Ɣ){if(Ž.Count<ƀ){Ž.Add(Ɣ);}
else{ź++;}}public int Ɠ(){return Ž.Count+ź;}public void ƒ(){ź=0;Ž.Clear();}public string Ƒ(){string Ɛ;ž.Clear();foreach(
string Ũ in Ž){ž.Append($" -{Ũ}\n");}if(ź>0){ž.Append($" -And {ź} other notes.\n");}Ɛ=ž.ToString();ž.Clear();return Ɛ;}public
void Ə(){Ɲ();ƙ();ƒ();}}static string Ǝ(int ƍ,int ƌ,int Ƌ,int Ɗ){return$"{ƍ:X2}{ƌ:X2}{Ƌ:X2}{Ɗ:X2}";}static string ų(Color ė){
return Ǝ(ė.A,ė.R,ė.G,ė.B);}static string œ(string Ũ){if(Ũ.Contains("\n")){Ũ=$"\n|{Ũ.Replace("\n","\n|")}";}return Ũ;}static
string ŧ(List<string>Ŧ,int ť=3,bool Ť=true){int ţ=0;string ĩ="";string Ţ=Ť?"|":"";if(Ŧ.Count>ť&&Ť){ĩ="\n|";}foreach(string š
in Ŧ){if(ţ>=ť){ĩ+=$"\n{Ţ}";ţ=0;}ĩ+=$"{š}, ";ţ++;}ĩ=ĩ.Remove(ĩ.Length-2);return ĩ;}interface Š{string ş();}interface ũ{
string Ş();}interface Ŝ:ũ{Color ś(double ª);}abstract class Ś:Ŝ{protected string E;public Color ř{internal get;set;}public
Color Ř{internal get;set;}public Color ŗ{internal get;set;}public Color Ŗ{internal get;set;}public Color ŕ{internal get;set;}
internal int Ŕ;internal int ŝ;internal int Ū;internal int ů;public Ś(Color ŭ,Color Ŭ,Color ū,Color č,Color Â){ř=ŭ;Ř=Ŭ;ŗ=ū;Ŗ=č;ŕ=
Â;}public Ś(){}internal bool Ų(string ű,Color ė){switch(ű){case"Optimal":ř=ė;break;case"Normal":Ř=ė;break;case"Caution":ŗ
=ė;break;case"Warning":Ŗ=ė;break;case"Critical":ŕ=ė;break;default:return false;}return true;}public string Ş(){return E;}
public abstract Color ś(double ª);}class Ű:Ś{public Ű(Color ŭ,Color Ŭ,Color ū,Color č,Color Â):base(ŭ,Ŭ,ū,č,Â){E="LowGood";Ŕ=0
;ŝ=55;Ū=70;ů=85;}public Ű():base(){E="LowGood";}public override Color ś(double ª){Color Á=Ř;if(ª<=Ŕ){Á=ř;}else if(ª>ů){Á=
ŕ;}else if(ª>Ū){Á=Ŗ;}else if(ª>ŝ){Á=ŗ;}return Á;}}class Ů:Ś{public Ů(Color ŭ,Color Ŭ,Color ū,Color č,Color Â):base(ŭ,Ŭ,ū,
č,Â){E="HighGood";Ŕ=100;ŝ=45;Ū=30;ů=15;}public Ů():base(){E="HighGood";}public override Color ś(double ª){Color Á=Ř;if(ª
>=Ŕ){Á=ř;}else if(ª<ů){Á=ŕ;}else if(ª<Ū){Á=Ŗ;}else if(ª<ŝ){Á=ŗ;}return Á;}}class À:Ŝ{string E;public Color º{private get;
set;}public À(){}public À(Color º,string E){this.E=E;this.º=º;}public À(Color º){this.º=º;E=$"{º.R}, {º.G}, {º.B}";}public
Color ś(double ª){return º;}public string Ş(){return E;}}interface µ{string z();string y();Color x{get;}}abstract class w:µ,Š
{public string v{get;set;}internal string u{get;private set;}public double N{protected get;set;}public double s{get;
protected set;}public double A{get;protected set;}internal bool q;internal bool o;public double ª{get;protected set;}protected
string Ã;protected string Ê;protected Ǫ F;internal string Ð;public Color x{get;protected set;}internal Ŝ B{get;set;}public w(Ǫ
F,string E,Ŝ B,double N=1){this.F=F;u=E;v=E;this.B=B;this.N=N;s=0;A=0;q=false;o=false;ª=0;Ã="curr";Ê="max";Ð=
"[----------]";x=Ϙ;}internal string Ï(){return Ð;}internal string Î(){return Ã;}internal string Í(){return Ê;}public string z(){return
$"{v}\n{Ã} / {Ê}\n{Ð}";}public string y(){return$"{v,-12}{($"{Ã} / {Ê}"),-12}{Ð}";}internal abstract void Ì();internal void Ñ(){s=0;}internal
void Ë(double L){A=L*N;q=true;}internal abstract void É();internal string È(string Ç){double Æ=1;string I=
$"[{ϋ}.{ϊ}.Tally.{œ(u)}]\n";if(v!=u){I+=$"DisplayName = {œ(v)}\n";}I+=Ç;if(q){I+=$"Max = {A/N}\n";}if(N!=Æ){I+=$"Multiplier = {N}\n";}I+="\n";
return I;}public abstract string ş();}class Å:w{internal ǅ Ä;public Å(Ǫ F,string E,ǅ Ä,Ŝ B,double N=1):base(F,E,B,N){this.Ä=Ä;
}internal bool j(IMyTerminalBlock Q){return Ä.j(Q);}internal string P(){return Ä.ş();}internal override void Ì(){if(!q){A
=Ä.Ǆ()*N;}c(ref Ê,A);}internal override void É(){if(A!=0){s=Ä.ǃ();s=s*N;ª=Math.Min(s/A,100)*100;x=B.ś(ª);F.Ï(ref Ð,ª);c(
ref Ã,s);}}public override string ş(){string I=$"Type = {Ä.ş()}\n";if(!(B is Ů)){I+=$"ColorCoder = {B.Ş()}\n";}I+=Ä.ǂ();
return È(I);}}class O:w{public O(Ǫ F,string E,Ŝ B,double N=1):base(F,E,B,N){}internal void M(double L){if(!q){A+=L;}}internal
override void Ì(){if(!q){A=A*N;}c(ref Ê,A);}internal virtual void K(IMyInventory J){s+=(double)J.CurrentVolume;}internal
override void É(){if(A!=0){s=s*N;ª=Math.Min(s/A,100)*100;x=B.ś(ª);F.Ï(ref Ð,ª);c(ref Ã,s);}}public override string ş(){string I=
"Type = Inventory\n";if(!(B is Ű)){I+=$"ColorCoder = {B.Ş()}\n";}return È(I);}}class H:O{internal MyItemType G{get;private set;}public H(Ǫ F
,string E,string D,string C,Ŝ B,double A=0,double N=1):base(F,E,B,N){G=new MyItemType(D,C);Ë(A);}public H(Ǫ F,string E,
MyItemType G,Ŝ B,double A=0,double N=1):base(F,E,B,N){this.G=G;Ë(A);}internal override void K(IMyInventory J){s+=(double)J.
GetItemAmount(G);}public override string ş(){string I=$"Type = Item\n";I+=$"ItemTypeID = {G.TypeId}\n";I+=
$"ItemSubTypeID = {G.SubtypeId}\n";if(!(B is Ů)){I+=$"ColorCoder = {B.Ş()}\n";}return È(I);}}class m{IMyInventory J;O[]l;public m(IMyInventory J,O[]l){
this.J=J;this.l=l;}public void k(){foreach(O d in l){{d.M((double)J.MaxVolume);}}}public void f(){foreach(O d in l){d.K(J);}
}}static void c(ref string Z,double Y){Z="";if(Y<10){Z+=(Math.Round(Y,1));}else if(Y<1000){Z+=(int)Y;}else if(Y<10000){Z=
Math.Round(Y/1000,1)+"K";}else if(Y<1000000){Z=(int)(Y/1000)+"K";}else if(Y<10000000){Z=Math.Round(Y/1000000,1)+"M";}else if
(Y<1000000000){Z=(int)(Y/1000000)+"M";}else if(Y<10000000000){Z=Math.Round(Y/1000000000,1)+"B";}else if(Y<1000000000000){
Z=(int)(Y/10000000000)+"B";}else if(Y<10000000000000){Z=Math.Round(Y/1000000000000,1)+"T";}else if(Y>10000000000000){Z=(
int)(Y/1000000000000)+"T";}}interface X{void W(bool V);bool U();string S();}class n:X{IMyTerminalBlock R;internal Action<
IMyTerminalBlock>Ò{get;set;}internal Action<IMyTerminalBlock>ý{get;set;}public n(IMyTerminalBlock à){R=à;Ò=null;ý=null;}public void W(
bool V){if(V){Ò?.Invoke(R);}else{ý?.Invoke(R);}}public bool U(){return Ò!=null||ý!=null;}public string S(){return
$"Block '{R.CustomName}'";}}class ü:X{IMyTerminalBlock R;internal List<Action<IMyTerminalBlock>>û{get;set;}internal List<Action<IMyTerminalBlock>
>ú{get;set;}public ü(IMyTerminalBlock à){R=à;û=null;ú=null;}public void W(bool V){List<Action<IMyTerminalBlock>>ù;if(V){ù
=û;}else{ù=ú;}if(ù!=null){foreach(Action<IMyTerminalBlock>Ü in ù){Ü.Invoke(R);}}}public bool U(){return û?.Count>0||ú?.
Count>0;}public string S(){return$"Block '{R.CustomName}'";}}class ø:X{IMyTerminalBlock R;private List<ó>ö;public ø(
IMyTerminalBlock à){this.R=à;ö=new List<ó>();}public void õ(ó ô){ö.Add(ô);}public void W(bool V){foreach(ó ô in ö){ô.W(R,V);}}public
bool U(){return ö.Count!=0;}public string S(){return$"Block '{R.CustomName}'";}}abstract class ó{public abstract bool ò();
public abstract Type ñ();public abstract void W(IMyTerminalBlock Q,bool V);}class ó<Č>:ó{string ĉ;private Č ċ,Ċ;private bool Ó
,Ý;public ó(string ĉ){this.ĉ=ĉ;Ó=false;Ý=false;}public void Ĉ(Č ć){ċ=ć;Ó=true;}public void Ć(Č ć){Ċ=ć;Ý=true;}public
override bool ò(){return Ó||Ý;}public override Type ñ(){return typeof(Č);}public override void W(IMyTerminalBlock Q,bool V){if(V
&&Ó){Q.SetValue<Č>(ĉ,ċ);}else if(!V&&Ý){Q.SetValue<Č>(ĉ,Ċ);}}}class ą:X{ɭ Ą;internal string ă{private get;set;}internal
string Ă{private get;set;}public ą(ɭ à){Ą=à;ă="";Ă="";}public void W(bool V){if(V){Ą.ɢ(ă);}else{Ą.ɢ(Ă);}}public bool U(){
return!String.IsNullOrEmpty(ă)||!String.IsNullOrEmpty(Ă);}public string S(){return
"Some MFD (Sorry, MFDs are supposed to work)";}}class ā:X,ũ{Ǻ Ā;internal bool ÿ{private get;set;}internal bool þ{private get;set;}public ā(Ǻ à){Ā=à;ÿ=false;þ=false;}
public void W(bool V){if(V){if(ÿ){Ā.Ȏ();}}else{if(þ){Ā.Ȏ();}}}public bool U(){return ÿ||þ;}public string S(){return
$"Raycaster '{Ā.u}'";}public string Ş(){return$"{Ā.u}: {(ÿ?"on":"off")}";}}class ä:X,ũ{public const bool ã=true;public const bool â=false;
internal Ơ á;internal bool Õ,Ô;internal bool Ó,Ý;public ä(Ơ à){á=à;Õ=â;Ô=â;Ó=false;Ý=false;}public void ß(bool Ü){Õ=Ü;Ó=true;}
public void Þ(bool Ü){Ô=Ü;Ý=true;}public void W(bool V){try{if(V){if(Ó){if(!á.ȡ){á.ș(Õ);}else{Exception Û=new
InvalidOperationException();Û.Data.Add("Counter",0);throw Û;}}}else{if(Ý){if(!á.ȡ){á.ș(Ô);}else{Exception Û=new InvalidOperationException();Û.
Data.Add("Counter",0);throw Û;}}}}catch(InvalidOperationException e){int Ú=(int)e.Data["Counter"];e.Data.Add(Ú,á.u);e.Data[
"Counter"]=++Ú;á.Ȗ();throw;}}public bool U(){return Ó||Ý;}public string S(){return$"Controller for ActionSet {á.u}";}public
string Ş(){if(Ù()){return$"{á.u}: {(Õ?"on":"off")}";}else{return$"{á.u}: {(Ô?"on":"off")}";}}public bool Ù(){return Ó;}}class
Ø:X,ũ{internal Ȫ Ö;internal bool Õ,Ô;internal bool Ó,Ý;public Ø(Ȫ à){this.Ö=à;Õ=false;Ô=false;Ó=false;Ý=false;}public
void ß(bool Ü){Õ=Ü;Ó=true;}public void Þ(bool Ü){Ô=Ü;Ý=true;}public void W(bool V){if(V){if(Ó){Ö.Ȃ(Õ);}}else{if(Ý){Ö.Ȃ(Ô);}}
}public bool U(){return Ó||Ý;}public string S(){return$"Controller for Trigger {Ö.u}";}public string Ş(){if(Ù()){return
$"{Ö.u}: {(Õ?"on":"off")}";}else{return$"{Ö.u}: {(Ô?"on":"off")}";}}public bool Ù(){return Ó;}}class ð:X,Š{Program í;public int ï{get;internal set
;}public int î{get;internal set;}public ð(Program í){this.í=í;ï=0;î=0;}public void W(bool V){if(V){í.Ɔ(ï);}else{í.Ɔ(î);}}
public bool U(){return ï!=0||î!=0;}public string S(){return"The Distributor";}public string ş(){string I="";int ì=0;int ë=0;if
(ï!=ì){I+=$"DelayOn = {ï}\n";}if(î!=ë){I+=$"DelayOff = {î}\n";}return I;}}class ê:X,Š{IMyIntergridCommunicationSystem ç;
internal string æ{get;set;}internal string é{get;set;}internal string è{get;set;}public ê(IMyIntergridCommunicationSystem ç,
string æ){this.ç=ç;this.æ=æ;é="";è="";}public void W(bool V){if(V){ç.SendBroadcastMessage(æ,é);}else{ç.SendBroadcastMessage(æ,
è);}}public bool U(){return!String.IsNullOrEmpty(é)||!String.IsNullOrEmpty(è);}public string S(){return
$"IGC on channel '{æ}'";}public string ş(){string I="";if(æ!=""){I+=$"IGCChannel = {æ}\n";}if(é!=""){I+=$"IGCMessageOn = {é}\n";}if(è!=""){I+=
$"IGCMessageOff = {è}\n";}return I;}}class Ơ:µ,Š{List<X>Ȉ;internal string v{get;set;}internal string u{get;private set;}internal bool Ù{get;
private set;}internal bool ȡ{get;private set;}internal Color Ƞ{private get;set;}internal Color ȟ{private get;set;}public Color
x{get;private set;}internal string Ȟ{private get;set;}internal string ȝ{private get;set;}public string Ȝ{get;private set;
}public Ơ(string E,bool Ȅ){Ȉ=new List<X>();v=E;u=E;Ù=Ȅ;ȡ=false;Ƞ=ϝ;ȟ=ϙ;Ȟ="Enabled";ȝ="Disabled";Ǽ();}internal void Ǽ(){if
(Ù){x=Ƞ;Ȝ=Ȟ;}else{x=ȟ;Ȝ=ȝ;}}public void ț(X Ș){Ȉ.Add(Ș);}public void Ț(){ș(!Ù);}public void ș(bool ȁ){Ù=ȁ;ȡ=true;Ǽ();
foreach(X Ș in Ȉ){try{Ș.W(ȁ);}catch(InvalidOperationException){throw;}catch(Exception e){if(!e.Data.Contains("Identifier")){e.
Data.Add("Identifier",Ș.S());}throw;}}}public void ȗ(){ȡ=false;}public void Ȗ(){Ȝ="Fault";x=new Color(125,125,125);}public
string z(){return$"{v}\n{Ȝ}";}public string y(){return$"{v,-19} {Ȝ,18}";}public string ş(){Color ȕ=ϝ;Color Ȕ=ϙ;string ȓ=
"Enabled";string Ȓ="Disabled";string I=$"[{ϋ}.{ϊ}.ActionSet.{œ(u)}]\n";if(v!=u){I+=$"DisplayName = {œ(v)}\n";}if(Ƞ!=ȕ){I+=
$"ColorOn = {Ę(Ƞ)}\n";}if(ȟ!=Ȕ){I+=$"ColorOff = {Ę(ȟ)}\n";}if(Ȟ!=ȓ){I+=$"TextOn = {œ(Ȟ)}\n";}if(ȝ!=Ȓ){I+=$"TextOff = {œ(ȝ)}\n";}int Ȣ=0;X ȥ=
null;ũ ȱ=null;List<string>ȯ=null;List<string>Ȯ=null;List<string>ȭ=null;List<string>Ȭ=null;List<string>ȫ=null;while(Ȣ!=-1){if
(Ȣ>=Ȉ.Count){Ȣ=-1;}else{ȥ=Ȉ[Ȣ];if(ȥ is Š){I+=$"{((Š)ȥ).ş()}";Ȣ++;}else if(ȥ is ũ){ȱ=(ũ)ȥ;if(ȱ is Ø){if(ȭ==null){ȭ=new
List<String>();Ȭ=new List<String>();}if(((Ø)ȱ).Ù()){ȭ.Add(ȱ.Ş());}else{Ȭ.Add(ȱ.Ş());}}else if(ȱ is ä){if(ȯ==null){ȯ=new List
<String>();Ȯ=new List<String>();}if(((ä)ȱ).Ù()){ȯ.Add(ȱ.Ş());}else{Ȯ.Add(ȱ.Ş());}}else{if(ȫ==null){ȫ=new List<String>();}
ȫ.Add(((ā)ȱ).Ş());}Ȣ++;}else{Ȣ=-1;}}}if(ȯ?.Count>0){I+=$"ActionSetsLinkedToOn = {ŧ(ȯ)}\n";}if(Ȯ?.Count>0){I+=
$"ActionSetsLinkedToOff = {ŧ(Ȯ)}\n";}if(ȭ?.Count>0){I+=$"TriggerLinkedToOn = {ŧ(ȭ)}\n";}if(Ȭ?.Count>0){I+=$"TriggerLinkedToOff = {ŧ(Ȭ)}\n";}if(ȫ?.Count>0){
I+=$"RaycastPerformedOnState = {ŧ(ȫ)}\n";}I+="\n";return I;}}class Ȫ:µ,Š{internal w ȑ{private get;set;}internal Ơ Ƿ{
private get;set;}double ȩ,Ȩ;bool ȧ,Ȱ;bool Ȧ,Ȥ;internal bool ȣ{get;private set;}public string u{get;private set;}string Ȟ,ȝ;
public string Ȝ{get;private set;}Color Ƞ,ȟ;public Color x{get;private set;}public Ȫ(string u,bool Ȅ){ȑ=null;Ƿ=null;ȅ(u,Ȅ);}
public Ȫ(string u,w ȑ,Ơ Ƿ,bool Ȅ){this.ȑ=ȑ;this.Ƿ=Ƿ;ȅ(u,Ȅ);}private void ȅ(string u,bool Ȅ){this.u=u;ȩ=-1;Ȩ=-1;ȧ=false;Ȱ=false
;Ȧ=false;Ȥ=false;ȣ=Ȅ;Ȟ="Armed";ȝ="Disarmed";Ƞ=ϛ;ȟ=ϙ;Ǽ();}public void ȃ(bool Ĝ,double ć,bool ǽ){if(Ĝ){Ȩ=ć;Ȱ=ǽ;Ȥ=true;}else
{ȩ=ć;ȧ=ǽ;Ȧ=true;}}public void Ȃ(bool ȁ){ȣ=ȁ;Ǽ();}public bool Ȁ(out Ơ Ȇ,out bool ǿ){Ȇ=null;ǿ=false;if(ȣ){if(Ȧ&&Ƿ.Ù!=ȧ&&ȑ.ª
>=ȩ){Ȇ=Ƿ;ǿ=ȧ;return true;}else if(Ȥ&&Ƿ.Ù!=Ȱ&&ȑ.ª<=Ȩ){Ȇ=Ƿ;ǿ=Ȱ;return true;}}return false;}private void Ǿ(bool ǽ){Ƿ.ș(ǽ);}
private void Ǽ(){if(ȣ){x=Ƞ;Ȝ=Ȟ;}else{x=ȟ;Ȝ=ȝ;}}public bool ǻ(){return Ȧ||Ȥ;}public string S(){return u;}public string z(){
return$"{u}\n{(ȣ?Ȟ:ȝ)}";}public string y(){return$"{u,-19} {(ȣ?Ȟ:ȝ),18}";}public string ş(){string I=
$"[{ϋ}.{ϊ}.Trigger.{œ(u)}]\n";I+=$"Tally = {ȑ.u}\n";I+=$"ActionSet = {Ƿ.u}\n";if(Ȥ){I+=$"LessOrEqualValue = {Ȩ}\n";I+=
$"LessOrEqualCommand = {(Ȱ?"on":"off")}\n";}if(Ȧ){I+=$"GreaterOrEqualValue = {ȩ}\n";I+=$"GreaterOrEqualCommand = {(ȧ?"on":"off")}\n";}return I;}}class Ǻ:Š{
StringBuilder ž;internal Ȋ ǹ{private get;set;}string Ǹ;internal bool ǚ{get;private set;}internal string u{get;private set;}public Ǻ(
StringBuilder ž,string u){ȅ(ž,u);}public Ǻ(StringBuilder ž,Ȋ ǹ,string u){this.ǹ=ǹ;ȅ(ž,u);}private void ȅ(StringBuilder ž,string u){
this.ž=ž;this.u=u;Ǹ=$"{DateTime.Now.ToString("HH:mm:ss")}- Raycaster {u} "+$"reports: No data";ǚ=false;}public void Ȑ(
IMyCameraBlock ư){ǹ.Ȑ(ư);ư.EnableRaycast=true;}public double ȏ(){return ǹ?.ȇ??-1;}public void Ȏ(){MyDetectedEntityInfo Ȍ;double ȋ;
IMyCameraBlock ư=ǹ.Ȏ(out Ȍ,out ȋ);ȍ(Ȍ,ȋ,ư);ǚ=true;}private void ȍ(MyDetectedEntityInfo Ȍ,double ȋ,IMyCameraBlock ư){ž.Clear();if(ư==
null){ž.Append($"{DateTime.Now.ToString("HH:mm:ss")}- Raycaster {u} "+$"reports: No cameras have the required {ȏ()} charge "
+$"needed to perform this scan.");}else if(Ȍ.IsEmpty()){ž.Append($"{DateTime.Now.ToString("HH:mm:ss")}- Raycaster {u} "+
$"reports: Camera '{ư.CustomName}' detected no entities on a "+$"{ȋ} meter scan.");}else{ž.Append($"{DateTime.Now.ToString("HH:mm:ss")}- Raycaster {u} "+
$"reports: Camera '{ư.CustomName}' detected entity '{Ȍ.Name}' "+$"on a {ȋ} meter scan.\n\n");ž.Append($"Relationship: {Ȍ.Relationship}\n");ž.Append($"Type: {Ȍ.Type}\n");ž.Append(
$"Size: {Ȍ.BoundingBox.Size.ToString("0.00")}\n");Vector3D Ŀ=Ȍ.HitPosition.Value;ž.Append($"Distance: {Vector3D.Distance(ư.GetPosition(),Ŀ).ToString("0.00")}\n");ž.
Append($"GPS:Raycast - {Ȍ.Name}:{Ŀ.X}:{Ŀ.Y}:{Ŀ.Z}:\n");}Ǹ=ž.ToString();ž.Clear();}public void Ǧ(){ǚ=false;}public string ǫ(){
return Ǹ;}public string ş(){string I=$"[{ϋ}.{ϊ}.Raycaster.{œ(u)}]\n";I+=ǹ.ş();return I;}}abstract class Ȋ{protected List<
IMyCameraBlock>ȉ;public double ȇ{get;protected set;}public Ȋ(){ȉ=new List<IMyCameraBlock>();ȇ=0;}public void Ȑ(IMyCameraBlock ư){ư.
EnableRaycast=true;ȉ.Add(ư);}public abstract void ɜ(double[]ɖ);public abstract IMyCameraBlock Ȏ(out MyDetectedEntityInfo Ȍ,out double
ȋ);public abstract string ş();}class ɛ:Ȋ{private double ɚ;private double N;private double ə;public ɛ():base(){int[]ɑ=ɗ();
ɚ=ɑ[0];N=ɑ[1];ə=ɑ[2];}public static string[]ɘ(){return new string[]{"BaseRange","Multiplier","MaxRange"};}internal static
int[]ɗ(){return new int[]{1000,3,27000};}public override void ɜ(double[]ɖ){if(ɖ[0]!=-1)ɚ=ɖ[0];if(ɖ[1]!=-1)N=ɖ[1];if(ɖ[2]!=-
1)ə=ɖ[2];double ɕ=ɚ;ȇ=ɚ;while(ɕ<ə){ɕ*=N;ȇ+=Math.Min(ə,ɕ);}}public override IMyCameraBlock Ȏ(out MyDetectedEntityInfo Ȍ,
out double ȋ){Ȍ=new MyDetectedEntityInfo();ȋ=-1;IMyCameraBlock ư=ɔ();if(ư==null||ư.AvailableScanRange<ȇ){return null;}else{
ȋ=ɚ;while(Ȍ.IsEmpty()&&ȋ<ə){Ȍ=ư.Raycast(ȋ,0,0);ȋ*=N;if(ȋ>ə){ȋ=ə;}}return ư;}}private IMyCameraBlock ɔ(){IMyCameraBlock ɓ=
null;foreach(IMyCameraBlock ư in ȉ){if(ɓ==null||ư.AvailableScanRange>ɓ.AvailableScanRange){ɓ=ư;}}return ɓ;}public override
string ş(){string[]ɒ=ɘ();int[]ɑ=ɗ();string I="Type = Linear\n";if(ɚ!=ɑ[0]){I+=$"{ɒ[0]} = {ɚ}\n";}if(N!=ɑ[1]){I+=
$"{ɒ[1]} = {N}\n";}if(ə!=ɑ[2]){I+=$"{ɒ[2]} = {ə}\n";}return I;}}interface ɝ{void ɨ();void Ǉ();void ɲ();void ɱ();}interface ɰ{Color ɯ{get;
set;}Color ɮ{get;set;}}class ɭ:ɝ{public string u{get;private set;}private Dictionary<string,ɝ>ɬ;internal int ɫ{get;private
set;}internal string ɪ{get;private set;}private ɝ ɩ;public ɭ(string u){this.u=u;ɬ=new Dictionary<string,ɝ>(StringComparer.
OrdinalIgnoreCase);ɫ=0;ɪ="";ɩ=null;}public void ɧ(string E,ɝ ɦ){ɬ.Add(E,ɦ);if(ɩ==null){ɩ=ɦ;ɪ=E;}}public int ɥ(){return ɬ.Count;}public
void ɤ(bool ɣ){if(ɣ){ɫ++;if(ɫ>=ɬ.Count){ɫ=0;}}else{ɫ--;if(ɫ<0){ɫ=ɬ.Count-1;}}ɪ=ɬ.Keys.ToArray()[ɫ];ɡ();}public bool ɢ(string
E){if(ɬ.ContainsKey(E)){ɪ=E;ɫ=ɬ.Keys.ToList().IndexOf(E);ɡ();return true;}else{return false;}}private void ɡ(){ɝ ɠ=ɬ[ɪ];
bool ɟ=false;if(ɩ is ɞ&&ɠ is ȼ){ɟ=true;}ɩ=ɠ;ɨ();Ǉ();if(ɟ){ɱ();}}public void ɨ(){ɩ.ɨ();}public void Ǉ(){ɩ.Ǉ();}public void ɲ(
){ɩ.ɲ();}public void ɱ(){ɩ.ɱ();}}class ɞ:ɝ,ɰ{IMyTextSurface ƻ;public Color ɯ{get;set;}public Color ɮ{get;set;}public
string Ƚ{get;set;}public ɞ(IMyTextSurface ƻ,string Ƚ){this.ƻ=ƻ;this.Ƚ=Ƚ;ɯ=ƻ.ScriptForegroundColor;ɮ=ƻ.ScriptBackgroundColor;}
public void Ǉ(){}public void ɲ(){}public void ɱ(){}public void ɨ(){ƻ.ContentType=ContentType.SCRIPT;ƻ.Script=Ƚ;ƻ.
ScriptForegroundColor=ɯ;ƻ.ScriptBackgroundColor=ɮ;}}class ȼ:ɝ,ɰ{IMyTextSurface ƻ;µ[]Ŧ;Vector2[]Ȼ;public float ƹ{private get;set;}public
string ƺ{private get;set;}public Color ɯ{get;set;}public Color ɮ{get;set;}public string ǖ{get;set;}Vector2 Ⱥ;bool ȹ;public ȼ(
IMyTextSurface ƻ,List<µ>Ŧ,string ǖ="",float ƹ=1f,string ƺ="Debug"){this.ƻ=ƻ;this.Ŧ=Ŧ.ToArray();this.ǖ=ǖ;this.ƹ=ƹ;this.ƺ=ƺ;ɯ=ƻ.
ScriptForegroundColor;ɮ=ƻ.ScriptBackgroundColor;Ȼ=new Vector2[Ŧ.Count];ȹ=false;}public void ȸ(int ȷ,float ȶ,float ȵ,float ȴ,float ȳ,bool Ȳ,
StringBuilder ž){RectangleF Ⱦ=new RectangleF((ƻ.TextureSize-ƻ.SurfaceSize)/2f,ƻ.SurfaceSize);float Ʉ=(ȶ/100)*ƻ.SurfaceSize.X;float ɐ=
(ȴ/100)*ƻ.SurfaceSize.Y;Ⱦ.X+=Ʉ;Ⱦ.Width-=Ʉ;Ⱦ.Y+=ɐ;Ⱦ.Height-=ɐ;Ⱦ.Width-=(ȵ/100)*ƻ.SurfaceSize.X;Ⱦ.Height-=(ȳ/100)*ƻ.
SurfaceSize.Y;ž.Clear();float Ɏ=0;if(!string.IsNullOrEmpty(ǖ)){ž.Append(ǖ);Ɏ=ƻ.MeasureStringInPixels(ž,ƺ,ƹ).Y;if(Ȳ){Ⱥ=new Vector2(Ⱦ
.Width/2+Ⱦ.X,Ⱦ.Y);}else{Ⱥ=new Vector2(ƻ.TextureSize.X/2,(ƻ.TextureSize.Y-ƻ.SurfaceSize.Y)/2);Ɏ=Math.Max(Ɏ-ȴ,0);}}int ɍ=(
int)(Math.Ceiling((double)Ŧ.Count()/ȷ));float Ɍ=Ⱦ.Width/ȷ;float ɋ=(Ⱦ.Height-Ɏ)/ɍ;int Ɋ=1;Vector2 ɉ,Ɉ,ɇ;ɉ=new Vector2(Ɍ/2,ɋ/
2);ɉ+=Ⱦ.Position;ɉ.Y+=Ɏ;for(int ņ=0;ņ<Ŧ.Count();ņ++){if(Ŧ[ņ]!=null){ž.Clear();ž.Append(Ŧ[ņ].z());ɇ=ƻ.
MeasureStringInPixels(ž,ƺ,ƹ);Ɉ=new Vector2(ɉ.X,ɉ.Y);Ɉ.Y-=ɇ.Y/2;Ȼ[ņ]=Ɉ;}if(Ɋ==ȷ){ɉ.X=Ɍ/2;ɉ.X+=Ⱦ.Position.X;ɉ.Y+=ɋ;Ɋ=1;}else{ɉ.X+=Ɍ;Ɋ++;}}ž.
Clear();}public void Ǉ(){µ š;MySprite Ɇ;using(MySpriteDrawFrame ɏ=ƻ.DrawFrame()){if(ȹ){Vector2 Ʌ=new Vector2(0,0);Ɇ=MySprite.
CreateSprite("IconEnergy",Ʌ,Ʌ);ɏ.Add(Ɇ);}if(!string.IsNullOrEmpty(ǖ)){Ɇ=MySprite.CreateText(ǖ,ƺ,ƻ.ScriptForegroundColor,ƹ);Ɇ.
Position=Ⱥ;ɏ.Add(Ɇ);}for(int ņ=0;ņ<Ŧ.Count();ņ++){š=Ŧ[ņ];if(š!=null){Ɇ=MySprite.CreateText(š.z(),ƺ,š.x,ƹ);Ɇ.Position=Ȼ[ņ];ɏ.Add(
Ɇ);}}}}public void ɲ(){Ǉ();}public void ɱ(){ȹ=!ȹ;}public void ɨ(){ƻ.ContentType=ContentType.SCRIPT;ƻ.Script="";ƻ.
ScriptForegroundColor=ɯ;ƻ.ScriptBackgroundColor=ɮ;}}interface Ƀ{string ɂ();bool ǚ();}class Ɂ:Ƀ{IMyTerminalBlock Q;bool ɀ;public Ɂ(
IMyTerminalBlock Q){this.Q=Q;ɀ=true;}public string ɂ(){return Q.CustomData;}public bool ǚ(){bool į=ɀ;ɀ=false;return į;}}abstract class ȿ
:Ƀ{protected IMyTerminalBlock Q;string Ƕ;public ȿ(IMyTerminalBlock Q){this.Q=Q;Ƕ="";}public abstract string ɂ();public
bool ǚ(){if(ɂ()==Ƕ){return false;}else{Ƕ=ɂ();return true;}}}class ǁ:ȿ{public ǁ(IMyTerminalBlock Q):base(Q){}public override
string ɂ(){return Q.DetailedInfo;}}class ǀ:ȿ{public ǀ(IMyTerminalBlock Q):base(Q){}public override string ɂ(){return Q.
CustomInfo;}}class ƿ:Ƀ{ǟ Ɛ;public ƿ(ǟ Ɛ){this.Ɛ=Ɛ;}public string ɂ(){return Ɛ.ǫ();}public bool ǚ(){return Ɛ.ǚ;}}class ƾ:Ƀ{
MyGridProgram í;public ƾ(MyGridProgram í){this.í=í;}public string ɂ(){return í.Storage;}public bool ǚ(){return false;}}class ƽ:Ƀ{Ǻ Ʊ;
public ƽ(Ǻ Ʊ){this.Ʊ=Ʊ;}public string ɂ(){return Ʊ.ǫ();}public bool ǚ(){return Ʊ.ǚ;}}class Ƽ:ɝ,ɰ{IMyTextSurface ƻ;public Color
ɯ{get;set;}public Color ɮ{get;set;}public string ƺ{get;set;}public float ƹ{get;set;}int Ƹ;Ƀ Ʒ;StringBuilder ž;public Ƽ(
IMyTextSurface ƻ,Ƀ Ʒ,StringBuilder ž){this.ƻ=ƻ;this.Ʒ=Ʒ;this.ž=ž;ɯ=ƻ.FontColor;ɮ=ƻ.BackgroundColor;ƺ=ƻ.Font;ƹ=ƻ.FontSize;Ƹ=0;}public
void Ǒ(int ǐ){if(ǐ>=0){Ƹ=ǐ;}}private string Ǐ(string ǎ){if(Ƹ>0){string[]Ǎ=ǎ.Split(' ');int ǌ=0;ž.Clear();foreach(string ǋ in
Ǎ){ž.Append($"{ǋ} ");if(ǋ.Contains('\n')){ǌ=0;}else{ǌ+=ǋ.Length+1;if(ǌ>Ƹ){ž.Append("\n");ǌ=0;}}}ǎ=ž.ToString();}return ǎ;
}public void Ǉ(){if(Ʒ.ǚ()){ƻ.WriteText(Ǐ(Ʒ.ɂ()));}}public void ɲ(){ƻ.WriteText(Ǐ(Ʒ.ɂ()));}public void ɱ(){}public void ɨ(
){ƻ.ContentType=ContentType.TEXT_AND_IMAGE;ƻ.FontColor=ɯ;ƻ.BackgroundColor=ɮ;ƻ.Font=ƺ;ƻ.FontSize=ƹ;ƻ.WriteText(Ǐ(Ʒ.ɂ()));
}}class Ǌ{List<IMyLightingBlock>ǉ;µ š;Color ǒ;public Ǌ(µ š){ǉ=new List<IMyLightingBlock>();this.š=š;ǒ=Ϙ;}public void ǈ(
IMyLightingBlock ǆ){ǉ.Add(ǆ);}public void Ǉ(){if(š.x!=ǒ){foreach(IMyLightingBlock ǆ in ǉ){ǆ.Color=š.x;}ǒ=š.x;}}}interface ǅ{bool j(
IMyTerminalBlock Q);double Ǆ();double ǃ();string ǂ();string ş();}class ƶ:ǅ{List<IMyBatteryBlock>ơ;public ƶ(){ơ=new List<IMyBatteryBlock>
();}public bool j(IMyTerminalBlock Q){IMyBatteryBlock Ƭ=Q as IMyBatteryBlock;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);
return true;}}public double Ǆ(){double A=0;foreach(IMyBatteryBlock ƫ in ơ){A+=ƫ.MaxStoredPower;}return A;}public double ǃ(){
double s=0;foreach(IMyBatteryBlock ƫ in ơ){s+=ƫ.CurrentStoredPower;}return s;}public string ǂ(){return"";}public string ş(){
return"Battery";}}class ƪ:ǅ{List<IMyGasTank>Ʃ;List<IMyTerminalBlock>ƨ;public ƪ(){Ʃ=new List<IMyGasTank>();ƨ=new List<
IMyTerminalBlock>();}public bool j(IMyTerminalBlock Q){IMyGasTank Ƨ=Q as IMyGasTank;if(Ƨ!=null){Ʃ.Add(Ƨ);return true;}else{
IMyPowerProducer Ʀ=Q as IMyPowerProducer;if(Ʀ!=null&&Ʀ.BlockDefinition.SubtypeId.EndsWith("HydrogenEngine")){ƨ.Add(Ʀ);return true;}else{
return false;}}}public double Ǆ(){double A=0;string[]Ņ;string[]ƥ={"(","L/","L)"};foreach(IMyGasTank Ƥ in Ʃ){A+=Ƥ.Capacity;}
foreach(IMyTerminalBlock ƣ in ƨ){Ņ=ƣ.DetailedInfo.Split(ƥ,System.StringSplitOptions.None);A+=Double.Parse(Ņ[2]);}return A;}
public double ǃ(){double s=0;foreach(IMyGasTank Ƥ in Ʃ){s+=Ƥ.Capacity*Ƥ.FilledRatio;}foreach(IMyTerminalBlock ƣ in ƨ){s+=ƣ.
Components.Get<MyResourceSourceComponent>().RemainingCapacity;}return s;}public string ǂ(){return"";}public string ş(){return"Gas"
;}}class Ƣ:ǅ{List<IMyJumpDrive>ơ;public Ƣ(){ơ=new List<IMyJumpDrive>();}public bool j(IMyTerminalBlock Q){IMyJumpDrive Ƭ=
Q as IMyJumpDrive;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);return true;}}public double Ǆ(){double A=0;foreach(IMyJumpDrive
Ƶ in ơ){A+=Ƶ.MaxStoredPower;}return A;}public double ǃ(){double s=0;foreach(IMyJumpDrive Ƶ in ơ){s+=Ƶ.CurrentStoredPower;
}return s;}public string ǂ(){return"";}public string ş(){return"JumpDrive";}}class ƴ:ǅ{List<IMyCameraBlock>ơ;Ǻ Ƴ;public ƴ
(){ơ=new List<IMyCameraBlock>();Ƴ=null;}public void Ʋ(Ǻ Ʊ){Ƴ=Ʊ;}public bool j(IMyTerminalBlock Q){IMyCameraBlock Ƭ=Q as
IMyCameraBlock;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);return true;}}public double Ǆ(){return-1;}public double ǃ(){double s=0;foreach(
IMyCameraBlock ư in ơ){s+=ư.AvailableScanRange;}return s;}public string ǂ(){return Ƴ==null?"":$"Raycaster = {Ƴ.u}\n";}public string ş(
){return"Raycast";}}class Ư:ǅ{List<IMyPowerProducer>ơ;public Ư(){ơ=new List<IMyPowerProducer>();}public bool j(
IMyTerminalBlock Q){IMyPowerProducer Ƭ=Q as IMyPowerProducer;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);return true;}}public double Ǆ(){
double A=0;foreach(IMyPowerProducer Ʈ in ơ){A+=Ʈ.Components.Get<MyResourceSourceComponent>().DefinedOutput;}return A;}public
double ǃ(){double s=0;foreach(IMyPowerProducer Ʈ in ơ){s+=Ʈ.MaxOutput;}return s;}public string ǂ(){return"";}public string ş()
{return"PowerMax";}}class ƭ:ǅ{List<IMyPowerProducer>ơ;public ƭ(){ơ=new List<IMyPowerProducer>();}public bool j(
IMyTerminalBlock Q){IMyPowerProducer Ƭ=Q as IMyPowerProducer;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);return true;}}public double Ǆ(){
double A=0;foreach(IMyPowerProducer Ʈ in ơ){A+=Ʈ.Components.Get<MyResourceSourceComponent>().DefinedOutput;}return A;}public
double ǃ(){double s=0;foreach(IMyPowerProducer Ʈ in ơ){s+=Ʈ.CurrentOutput;}return s;}public string ǂ(){return"";}public string
ş(){return"PowerCurrent";}}class ǰ:ǅ{List<IMySlimBlock>ơ;public ǰ(){ơ=new List<IMySlimBlock>();}public bool j(
IMyTerminalBlock Q){IMySlimBlock Ƭ=Q.CubeGrid.GetCubeBlock(Q.Min);ơ.Add(Ƭ);return true;}public double Ǆ(){double A=0;foreach(
IMySlimBlock Q in ơ){A+=Q.MaxIntegrity;}return A;}public double ǃ(){double s=0;foreach(IMySlimBlock Q in ơ){s+=Q.BuildIntegrity-Q.
CurrentDamage;}return s;}public string ǂ(){return"";}public string ş(){return"Integrity";}}class ǯ:ǅ{List<IMyAirVent>ơ;public ǯ(){ơ=
new List<IMyAirVent>();}public bool j(IMyTerminalBlock Q){IMyAirVent Ƭ=Q as IMyAirVent;if(Ƭ==null){return false;}else{ơ.Add
(Ƭ);return true;}}public double Ǆ(){double A=0;foreach(IMyAirVent Ǯ in ơ){A+=1;}return A;}public double ǃ(){double s=0;
foreach(IMyAirVent Ǯ in ơ){s+=Ǯ.GetOxygenLevel();}return s;}public string ǂ(){return"";}public string ş(){return"VentPressure";
}}class ǭ:ǅ{List<IMyPistonBase>ơ;public ǭ(){ơ=new List<IMyPistonBase>();}public bool j(IMyTerminalBlock Q){IMyPistonBase
Ƭ=Q as IMyPistonBase;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);return true;}}public double Ǆ(){double A=0;foreach(
IMyPistonBase Ǵ in ơ){A+=Ǵ.HighestPosition;}return A;}public double ǃ(){double s=0;foreach(IMyPistonBase Ǵ in ơ){s+=Ǵ.CurrentPosition
;}return s;}public string ǂ(){return"";}public string ş(){return"PistonExtension";}}class ǳ:ǅ{List<IMyMotorStator>ơ;
public ǳ(){ơ=new List<IMyMotorStator>();}public bool j(IMyTerminalBlock Q){IMyMotorStator Ƭ=Q as IMyMotorStator;if(Ƭ==null){
return false;}else{ơ.Add(Ƭ);return true;}}public double Ǆ(){double A=0;foreach(IMyMotorStator ǲ in ơ){A+=360;}return A;}public
double ǃ(){double s=0;foreach(IMyMotorStator ǲ in ơ){s+=MathHelper.ToDegrees(ǲ.Angle);}return s;}public string ǂ(){return"";}
public string ş(){return"RotorAngle";}}class ǵ:ǅ{List<IMyShipController>ơ;public ǵ(){ơ=new List<IMyShipController>();}public
bool j(IMyTerminalBlock Q){IMyShipController Ƭ=Q as IMyShipController;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);return true;}}
public double Ǆ(){return 110;}public double ǃ(){double s=-1;foreach(IMyShipController Ǡ in ơ){if(Ǡ.IsFunctional){s=Ǡ.
GetShipSpeed();break;}}return s;}public string ǂ(){return"";}public string ş(){return"ControllerSpeed";}}class Ǳ:ǅ{List<
IMyShipController>ơ;public Ǳ(){ơ=new List<IMyShipController>();}public bool j(IMyTerminalBlock Q){IMyShipController Ƭ=Q as
IMyShipController;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);return true;}}public double Ǆ(){return 1;}public double ǃ(){double s=0;foreach(
IMyShipController Ǡ in ơ){if(Ǡ.IsFunctional){s=Ǡ.GetNaturalGravity().Length()/9.81;break;}}return s;}public string ǂ(){return"";}public
string ş(){return"ControllerGravity";}}class Ǭ:ǅ{List<IMyShipController>ơ;public Ǭ(){ơ=new List<IMyShipController>();}public
bool j(IMyTerminalBlock Q){IMyShipController Ƭ=Q as IMyShipController;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);return true;}}
public double Ǆ(){return-1;}public double ǃ(){double s=-1;foreach(IMyShipController Ǡ in ơ){if(Ǡ.IsFunctional){s=Ǡ.
GetNaturalGravity().Length()*Ǡ.CalculateShipMass().PhysicalMass;break;}}return s;}public string ǂ(){return"";}public string ş(){return
"ControllerWeight";}}class ǟ{List<string>Ɛ;string ǖ;public string Ǟ{private get;set;}public int ǝ{private get;set;}public string ǜ{private
get;set;}StringBuilder ž;string ǡ;int ǔ;public bool ǚ{get;private set;}string[]Ǚ;int ǘ=-1;int Ǘ;public ǟ(StringBuilder ž,
string ǖ,bool Ǖ=false,int ǔ=5){Ɛ=new List<string>();this.ž=ž;ǡ="";this.ǖ=ǖ;Ǟ="";ǜ="";this.ǔ=ǔ;ǚ=false;ǝ=0;if(Ǖ){Ǚ=new string[]
{"|----","-|---","--|--","---|-","----|"};ǘ=0;Ǘ=1;}}public void ģ(){ǘ+=Ǘ;if(ǘ==0||ǘ==4){Ǘ*=-1;}}public void Ǔ(string Ǜ){Ɛ
.Insert(0,$"{DateTime.Now.ToString("HH:mm:ss")}- {Ǜ}");if(Ɛ.Count>ǔ){Ɛ.RemoveAt(ǔ);}ž.Clear();foreach(string Ũ in Ɛ){ž.
Append($"\n{Ũ}\n");}ǡ=ž.ToString();ž.Clear();ǚ=true;}public void Ǧ(){ǚ=false;}public string ǫ(){ž.Clear();ž.Append(ǖ);if(ǘ!=-1
){ž.Append($" {Ǚ[ǘ]}");}ž.Append("\n");if(!String.IsNullOrEmpty(Ǟ)){ž.Append($"Script Tag: {Ǟ}\n");}if(ǝ!=0){ž.Append(
$"Current Update Delay: {ǝ}\n");}ž.Append($"{ǜ}\n");ž.Append(ǡ);return ž.ToString();}}class Ǫ{StringBuilder ž;int ǩ;string[]Ǩ;public Ǫ(StringBuilder ž
,int Ǣ=10){this.ž=ž;ǩ=Ǣ;Ǩ=new string[ǩ+1];string ǧ="";for(int ņ=0;ņ<Ǩ.Length;ņ++){Ǥ(ref ǧ,ņ,ǩ);Ǩ[ņ]=ǧ;}}public void Ï(ref
string Ð,double ª){Ð=Ǩ[ǥ(ª,ǩ)];}public void Ï(ref string Ð,double ª,int Ǣ){int ǣ=ǥ(ª,Ǣ);Ǥ(ref Ð,ǣ,Ǣ);}private int ǥ(double ª,
int Ǣ){ª=Math.Min(ª,100);return(int)((ª/100)*Ǣ);}private void Ǥ(ref string Ð,int ǣ,int Ǣ){ž.Clear();ž.Append('[');for(int ņ
=0;ņ<ǣ;++ņ){ž.Append('|');}for(int ņ=ǣ;ņ<Ǣ;++ņ){ž.Append(' ');}ž.Append(']');Ð=ž.ToString();ž.Clear();}}
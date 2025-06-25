/*
 * Shipware Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=2681807135
 * Shipware Documentation: https://steamcommunity.com/sharedfiles/filedetails/?id=2776664161
 * 
 * THIS CODE CONTAINS NOTHING THAT SHOULD BE EDITED BY THE USER
 */
const double ό=.802;const string ϋ="Shipware";const string ϊ="SW";const string ω="Dec";string ψ;string χ;const string φ=
"Source";const string υ="Target";ǟ τ;MyIni σ;MyIni ς;StringBuilder Ɓ;Ǫ ρ;l[]ο;v[]ξ;Dictionary<string,Ơ>ν;Ȩ[]μ;Dictionary<string,
Ǻ>λ;ɧ[]κ;ǋ[]ι;Dictionary<string,ɫ>θ;List<ƻ>η;IMyBroadcastListener ζ;ġ ε;Ч π;Dictionary<string,Ч>ύ;bool ϕ;DateTime ϟ;
string ϝ;static Color Ϝ=new Color(25,225,100);static Color ϛ=new Color(100,200,225);static Color Ϛ=new Color(255,255,0);static
Color ϙ=new Color(255,150,0);static Color Ϙ=new Color(255,0,0);static Color ϗ=new Color(255,225,200);static Color ϖ=new Color
(0,0,0);Program(){Ƃ Ě;bool ϐ;Ϟ(out Ě,out ϐ);if(ϐ){ϓ(Ě);}else{ҁ(Ě);}Echo(τ.ǫ());}void Ϟ(out Ƃ Ě,out bool ϐ){σ=new MyIni();
ς=new MyIni();Ɓ=new StringBuilder();ϕ=false;ϟ=DateTime.Now;Ě=new Ƃ(Ɓ,15);ϐ=false;σ.TryParse(Storage);double ϔ=σ.Get(
"Data","Version").ToDouble(-1);ψ=σ.Get("Data","ID").ToString(ϋ);χ=$"{ϊ}.{ψ}";ζ=IGC.RegisterBroadcastListener(χ);ζ.
SetMessageCallback(χ);τ=new ǟ(Ɓ,$"Shipware v{ό} - Recent Events",true);ρ=new Ǫ(Ɓ);if(χ!=$"{ϊ}.{ϋ}"){τ.Ǟ=χ;}ε=new ġ();ύ=new Dictionary<
string,Ч>();σ.Clear();if(ϔ==-1){ϐ=true;}else if(ϔ!=ό){Ě.ƕ($"Code updated from v{ϔ} to v{ό}.");}τ.Ǔ(
"Script initialization complete.");}void ϓ(Ƃ Ě){MyIniParseResult ϒ;string Ϗ=$"{ϊ}.Init";if(!σ.TryParse(Me.CustomData,out ϒ)){Ě.Ɵ(
$"Cannot generate a {Ϗ} section because the parser encountered "+$"an error on line {ϒ.LineNo} of the Programmable Block's config: {ϒ.Error}");}else{ϑ(Ě,true);}Ě.ƕ(
"Use the AutoPopulate command to generate basic configuration.");Ě.ƕ("The Clone command can quickly distribute config across identical blocks.");Ě.ƕ(
"The Evaluate command scans the grid for config and loads it into memory.");string ĩ=$"First run complete.\nThe following messages were logged:\n{Ě.Ƒ()}";if(Ě.Ɲ()>0){ĩ+=
$"The following errors were logged:\n{Ě.ƛ()}";}τ.Ǔ(ĩ);}void ϑ(Ƃ Ě,bool ϐ=false){string Ϗ=$"{ϊ}.Init";bool ώ=σ.ContainsSection(Ϗ);bool δ=ώ&&!ϐ;bool γ=false;string Έ=
Me.CustomName;if(!ώ&&!ϐ){Ě.ƕ($"{Ϗ} section was missing from block '{Έ}' and "+$"has been re-generated.");}string[]Λ=new
string[]{"ColorOptimal","ColorNormal","ColorCaution","ColorWarning","ColorCritical","MPSpriteSyncFrequency",
"APExcludedBlockTypes","APExcludedBlockSubTypes","APExcludedDeclarations"};string[]Κ=new string[]{"Green","LightBlue","Yellow","Orange","Red",
"-1",("MyObjectBuilder_ConveyorSorter, MyObjectBuilder_ShipWelder,\n"+"MyObjectBuilder_ShipGrinder"),(
$"StoreBlock, ContractBlock, {ϊ}.FurnitureSubTypes,\n"+$"{ϊ}.IsolatedCockpitSubTypes, {ϊ}.ShelfSubTypes"),("ThrustersGeneric")};bool[]Ι=new bool[]{false,false,false,false,
false,true,true,true,true};for(int ņ=0;ņ<Λ.Length;ņ++){ы(Ϗ,Λ[ņ],Κ[ņ],Ι[ņ],ref γ,δ,Έ,Ě);}if(γ){Me.CustomData=σ.ToString();}}
void Save(){σ.Clear();σ.Set("Data","Version",ό);σ.Set("Data","ID",ψ);int Θ=ε.Đ("UpdateDelay");σ.Set("Data","UpdateDelay",Θ==
-1?0:Θ);if(ν!=null){foreach(Ơ Η in ν.Values){σ.Set("ActionSets",Η.s,Η.Ø);}}if(μ!=null){foreach(Ȩ ğ in μ){σ.Set("Triggers"
,ğ.s,ğ.ȡ);}}if(θ!=null){foreach(ɫ Ζ in θ.Values){σ.Set("MFDs",Ζ.s,Ζ.ɨ);}}Storage=σ.ToString();σ.Clear();}void Main(string
Ε,UpdateType Μ){MyCommandLine Δ=null;Func<string,bool>Β=(ΐ)=>{Δ=new MyCommandLine();return(Δ.TryParse(ΐ));};Func<string,
bool>Α=(ΐ)=>{Δ=new MyCommandLine();return(Δ.TryParse(ΐ.ToLowerInvariant()));};if((Μ&UpdateType.Update100)!=0){ε.ģ();}if((Μ&
UpdateType.Update10)!=0){if(π!=null){bool Ώ=π.р();τ.ǜ=π.С;if(!Ώ){if(π.Р){τ.Ǔ(π.м());}π.н();ύ.Remove(π.Ц);π=null;}}else{if(ύ.Count>
0){π=ύ.Values.ElementAt(0);π.с();}else{Runtime.UpdateFrequency&=~UpdateFrequency.Update10;τ.ǜ="";}}}else if((Μ&UpdateType
.Once)!=0){ҁ(new Ƃ(Ɓ,15));}else if((Μ&UpdateType.IGC)!=0){while(ζ.HasPendingMessage){MyIGCMessage ŋ=ζ.AcceptMessage();if(
ŋ.Tag==χ){string Ύ=ŋ.Data.ToString();if(Α(Ύ)){string ĩ="No reply";string Ό=null;bool Ί=false;Action Ή=()=>{MyCommandLine.
SwitchEnumerator Γ=Δ.Switches.GetEnumerator();Γ.MoveNext();Ό=Γ.Current;};if(Δ.Argument(0)=="reply"){Ί=true;Β(Ύ);Ύ=Ύ.Replace(Δ.Argument(0
),"");Ύ=Ύ.Trim();if(Δ.Switches.Count==1){Ή();Ύ=Ύ.Replace($"-{Ό}","");Ύ=Ύ.Trim();ĩ=$"Received IGC reply from {Ό}: {Ύ}";}
else{ĩ=($"Received IGC reply: {Ύ}");}}else if(Δ.Argument(0)=="action"){if(Δ.ArgumentCount==3){ĩ=ϩ(Δ.Argument(1),Δ.Argument(2
),"IGC-directed ");}else{ĩ=$"Received IGC-directed command '{Ύ}', which "+$"has an incorrect number of arguments.";}}else
if(Δ.ArgumentCount==1){ĩ=ϩ(Δ.Argument(0),"switch","IGC-directed ");}else{ĩ=
$"Received the following unrecognized command from the IGC:"+$" '{Ύ}'.";}if(!Ί&&Δ.Switches.Count==1){Β(Ύ);Ή();IGC.SendBroadcastMessage(Ό,$"reply {ĩ} -{χ}");ĩ+=
$"\nSent reply on channel {Ό}.";}τ.Ǔ(ĩ);}else{τ.Ǔ($"Received IGC-directed command '{Ύ}', which couldn't be "+$"handled by the argument reader.");}}}}
else{if(Α(Ε)){string ǽ=Δ.Argument(0);string Ψ="";string ĩ="";switch(ǽ){case"log":break;case"igc":Β(Ε);string Ύ=Ε.Remove(0,4)
;Ύ=Ύ.Replace(Δ.Argument(1),"");Ύ=Ύ.Trim();IGC.SendBroadcastMessage(Δ.Argument(1),Ύ);τ.Ǔ(
$"Sent the following IGC message on channel '{Δ.Argument(1)}'"+$": {Ύ}.");break;case"mfd":if(Δ.ArgumentCount==3){string α=Δ.Argument(1);string ΰ=Δ.Argument(2);if(θ==null){τ.Ǔ(
$"Received MFD command, but script configuration isn't loaded.");}else if(θ.ContainsKey(α)){ɫ ί=θ[α];if(ΰ=="next"){ί.ɣ(true);}else if(ΰ=="prev"){ί.ɣ(false);}else{if(!ί.ɡ(ΰ)){τ.Ǔ(
$"Received command to set MFD '{α}' to unknown "+$"page '{ΰ}'.");}}}else{τ.Ǔ($"Received '{ΰ}' command for un-recognized MFD '{α}'.");}}else{τ.Ǔ(
$"Received MFD command with an incorrect number of arguments.");}break;case"action":if(Δ.ArgumentCount==3){ĩ=ϩ(Δ.Argument(1),Δ.Argument(2),"");if(!Ϲ(ĩ)){τ.Ǔ(ĩ);}}else{τ.Ǔ(
$"Received Action command with an incorrect number of arguments.");}break;case"raycast":if(Δ.ArgumentCount==2){string ή=Δ.Argument(1);if(λ==null){τ.Ǔ(
$"Received Racast command, but script configuration isn't loaded.");}else if(λ.ContainsKey(ή)){λ[ή].ȏ();}else{τ.Ǔ($"Received Raycast command for un-recognized Raycaster '{ή}'.");}}else{τ
.Ǔ($"Received Raycast command with an incorrect number of arguments.");}break;case"reconstitute":if(!ϕ){τ.Ǔ(
"Received Reconstitute command, but there is no last-good "+"config to reference. Please only use this command after the "+"script has successfully evaluated.");}else if(!Δ.Switch
("force")){τ.Ǔ("Received Reconstitute command, which will regenerate "+"declarations based on config that was read "+
$"{(DateTime.Now-ϟ).Minutes} minutes ago "+$"({ϟ.ToString("HH: mm: ss")}). If this is "+"acceptable, re-run this command with the -force flag.");}else{Me.
CustomData=$"{ϝ}\n";if(!Ϲ(ϝ)){Me.CustomData+=";=======================================\n\n";}Me.CustomData+=ϳ(ξ.ToList(),ν.Values.
ToList(),μ.ToList(),λ.Values.ToList());τ.Ǔ($"Carried out Reconstitute command. PB config has been reverted "+
$"to last known good.");}break;case"clone":List<IMyTerminalBlock>έ=new List<IMyTerminalBlock>();Ψ="Clone command";if(!ϣ(φ,έ,ref Ψ)){τ.Ǔ(Ψ);}
else{IMyTerminalBlock ά=έ[0];έ.Clear();if(!ϣ(υ,έ,ref Ψ)){τ.Ǔ(Ψ);}else{foreach(IMyTerminalBlock P in έ){P.CustomData=ά.
CustomData;}τ.Ǔ($"Carried out Clone command, replacing the CustomData "+$"of {έ.Count} blocks in the {υ} "+
$"group with the CustomData from block '{ά.CustomName}'.");}}break;case"tacticalnuke":if(Δ.Switch("force")){List<IMyTerminalBlock>Ϋ=new List<IMyTerminalBlock>();Ψ=
"TacticalNuke command";if(!ϣ(υ,Ϋ,ref Ψ)){τ.Ǔ(Ψ);}else{foreach(IMyTerminalBlock P in Ϋ){P.CustomData="";}τ.Ǔ(
$"Carried out TacticalNuke command, clearing the "+$"CustomData of {Ϋ.Count} blocks.");}}else{τ.Ǔ("Received TacticalNuke command. TacticalNuke will remove "+
$"ALL CustomData from blocks in the {υ} group. "+"If you are certain you want to do this, run the command with the "+"-force switch.");}break;case"terminalproperties":
List<IMyTerminalBlock>Ϊ=new List<IMyTerminalBlock>();Ψ="TerminalProperties command";if(!ϣ(φ,Ϊ,ref Ψ)){τ.Ǔ(Ψ);}else{
Dictionary<Type,string>β=new Dictionary<Type,string>();List<ITerminalProperty>Ω=new List<ITerminalProperty>();string Χ;foreach(
IMyTerminalBlock P in Ϊ){if(!β.ContainsKey(P.GetType())){P.GetProperties(Ω);Χ="";foreach(ITerminalProperty Φ in Ω){Χ+=
$"  {Φ.Id}  {Φ.TypeName}\n";}β.Add(P.GetType(),Χ);}}Ɓ.Clear();string[]Υ;foreach(KeyValuePair<Type,string>ŧ in β){Υ=ŧ.Key.ToString().Split('.');Ɓ.
Append($"Properties for '{Υ[Υ.Length-1]}'\n{ŧ.Value}");}τ.Ǔ(Ɓ.ToString());Ɓ.Clear();}break;case"typedefinitions":List<
IMyTerminalBlock>Τ=new List<IMyTerminalBlock>();Ψ="TypeDefinitions command";if(!ϣ(φ,Τ,ref Ψ)){τ.Ǔ(Ψ);}else{bool Σ=Δ.Switch("items");List
<MyInventoryItem>Ρ=new List<MyInventoryItem>();string[]Π;Ɓ.Clear();Ɓ.Append(
$"Type Definitions for members of the {φ} group:\n");foreach(IMyTerminalBlock P in Τ){Π=P.GetType().ToString().Split('.');Ɓ.Append($" {P.CustomName}:\n"+
$"  Interface: {Π[Π.Length-1]}\n"+$"  TypeID: {P.BlockDefinition.TypeIdString}\n"+$"  SubTypeID: {P.BlockDefinition.SubtypeId}\n"+$"\n");if(Σ&&P.
HasInventory){P.GetInventory().GetItems(Ρ);Ɓ.Append("  Items:\n");foreach(MyInventoryItem Ο in Ρ){Ɓ.Append(
$"   Name: {Ο.Type.ToString()}\n");Ɓ.Append($"    TypeID: {Ο.Type.TypeId}\n");Ɓ.Append($"    SubTypeID: {Ο.Type.SubtypeId}\n");}}}τ.Ǔ(Ɓ.ToString());Ɓ.
Clear();}break;case"surfacescripts":List<string>Ν=new List<string>();Me.GetSurface(0).GetScripts(Ν);Ɓ.Clear();Ɓ.Append(
"Available scripts:\n");foreach(string Ϡ in Ν){Ɓ.Append($"  {Ϡ}\n");}τ.Ǔ(Ɓ.ToString());Ɓ.Clear();break;case"autopopulate":MyIniParseResult ϒ;
if(!σ.TryParse(Me.CustomData,out ϒ)){τ.Ǔ("Received AutoPopulate command, but was unable to carry it "+
$"out due to a parsing error on line {ϒ.LineNo} of the "+$"Programmable Block's config: {ϒ.Error}");}else{HashSet<string>Ѕ=ʁ("APExcludedBlockTypes");HashSet<string>ɽ=ʁ(
"APExcludedBlockSubTypes");ɾ(ɽ);string Ϭ="AutoPopulate";List<IMyTerminalBlock>Ϯ=new List<IMyTerminalBlock>();if(Δ.Switch("target")){IMyBlockGroup
Ј=GridTerminalSystem.GetBlockGroupWithName(υ);if(Ј==null){τ.Ǔ("Received AutoPopulate command with the -target flag set, "
+$"but there is no {υ} block group on the grid.");break;}else{Ј.GetBlocks(Ϯ,Ŵ=>Ŵ.IsSameConstructAs(Me)&&!Ѕ.Contains(Ŵ.
BlockDefinition.TypeIdString)&&!ɽ.Contains(Ŵ.BlockDefinition.SubtypeId)&&!MyIni.HasSection(Ŵ.CustomData,$"{ϊ}.APIgnore"));Ϭ=
"Targeted AutoPopulate";}}else{ϫ<IMyTerminalBlock>(Ϯ,Ŵ=>Ŵ.IsSameConstructAs(Me)&&!Ѕ.Contains(Ŵ.BlockDefinition.TypeIdString)&&!ɽ.Contains(Ŵ.
BlockDefinition.SubtypeId)&&!MyIni.HasSection(Ŵ.CustomData,$"{ϊ}.APIgnore"));}bool Ї=ϯ(Ϯ,σ,Ϭ,ref ĩ);τ.Ǔ(ĩ);if(Ї){Save();Runtime.
UpdateFrequency=UpdateFrequency.Once;}}break;case"apexclusionreport":if(!σ.TryParse(Me.CustomData,out ϒ)){τ.Ǔ(
"Received APExclusionReport command, but was unable to carry it "+$"out due to a parsing error on line {ϒ.LineNo} of the "+$"Programmable Block's config: {ϒ.Error}");}else{ĩ=
"Carried out APExclusionReport command.\n";MyIniValue ě=σ.Get($"{ϊ}.Init","APExcludedDeclarations");if(!Ϲ(ě.ToString())){string І=ě.ToString();ĩ+=
$"These declarations are being excluded from consideration "+$"by AutoPopulate: {І}.\n";List<string>ť;ť=І.Split(',').Select(Ļ=>Ļ.Trim()).ToList();Ƃ Љ=new Ƃ(Ɓ,5);ɹ(ť,Љ);if(Љ.Ƙ()>0){
ĩ+=Љ.Ɩ();}ĩ+="\n";}HashSet<string>Ѕ=ʁ("APExcludedBlockTypes");Dictionary<string,int>Є=Ѕ.ToDictionary(Ђ=>Ђ,Ђ=>0);HashSet<
string>ɽ=ʁ("APExcludedBlockSubTypes");ɾ(ɽ);Dictionary<string,int>Ѓ=ɽ.ToDictionary(Ђ=>Ђ,Ђ=>0);int Ё=0;int Ѐ=0;int Ͽ=0;List<
IMyTerminalBlock>Ͼ=new List<IMyTerminalBlock>();ϫ<IMyTerminalBlock>(Ͼ,Ŵ=>Ŵ.IsSameConstructAs(Me));foreach(IMyTerminalBlock P in Ͼ){if(
MyIni.HasSection(P.CustomData,$"{ϊ}.APIgnore")){Ё++;}if(Є.ContainsKey(P.BlockDefinition.TypeIdString)){Є[P.BlockDefinition.
TypeIdString]++;Ѐ++;}if(Ѓ.ContainsKey(P.BlockDefinition.SubtypeId)){Ѓ[P.BlockDefinition.SubtypeId]++;Ͽ++;}}ĩ+=
$"Of the {Ͼ.Count} TerminalBlocks on this "+$"construct, the following {Ё+Ѐ+Ͽ} "+$"blocks are being excluded from consideration by AutoPopulate:\n";ĩ+=
$"\n -{Ё} blocks excluded by APIgnore\n";ĩ+=$"\n -{Ѐ} blocks excluded by type\n";foreach(KeyValuePair<string,int>Ĩ in Є){ĩ+=$"  >{Ĩ.Value} {Ĩ.Key}\n";}ĩ+=
$"\n -{Ͽ} blocks excluded by subype\n";foreach(KeyValuePair<string,int>Ĩ in Ѓ){ĩ+=$"  >{Ĩ.Value} {Ĩ.Key}\n";}σ.Clear();τ.Ǔ(ĩ);}break;case"clear":List<
IMyTerminalBlock>Е=new List<IMyTerminalBlock>();Ψ="Clear command";if(!ϣ(υ,Е,ref Ψ)){τ.Ǔ(Ψ);}else{List<string>Д=new List<string>();string
[]Г;int В=0;foreach(IMyTerminalBlock P in Е){σ.TryParse(P.CustomData);σ.GetSections(Д);foreach(string Б in Д){Г=Б.Split(
'.');if(Г[0]==ϊ){σ.DeleteSection(Б);В++;}}P.CustomData=σ.ToString();}σ.Clear();τ.Ǔ(
$"Clear command executed on {Е.Count} blocks. Removed "+$"{В} Shipware sections.");}break;case"changeid":Β(Ε);if(Δ.ArgumentCount==2){string А=Δ.Argument(1);string Џ=$"{ϊ}.{А}"
;List<IMyTerminalBlock>ϼ=new List<IMyTerminalBlock>();ϫ<IMyTerminalBlock>(ϼ,Ŵ=>(Ŵ.IsSameConstructAs(Me)&&MyIni.HasSection
(Ŵ.CustomData,χ)));foreach(IMyTerminalBlock P in ϼ){P.CustomData=P.CustomData.Replace($"[{χ}]",$"[{Џ}]");}ψ=А;Save();Ƃ Ě;
bool ϐ;Ϟ(out Ě,out ϐ);Runtime.UpdateFrequency=UpdateFrequency.Once;τ.Ǔ(
$"ChangeID complete, {ϼ.Count} blocks modified. The ID "+$"of this script instance is now '{ψ}', and its tag is now '{χ}'.");}else if(Δ.ArgumentCount>2){τ.Ǔ(
$"Received ChangeID command with too many arguments. Note "+$"that IDs can't contain spaces.");}else{τ.Ǔ($"Received ChangeID command with no new ID.");}break;case"integrate":List<
IMyTerminalBlock>Ў=new List<IMyTerminalBlock>();ϫ<IMyTerminalBlock>(Ў,Ŵ=>(Ŵ.IsSameConstructAs(Me)&&MyIni.HasSection(Ŵ.CustomData,
$"{ϊ}.Integrate")));foreach(IMyTerminalBlock P in Ў){P.CustomData=P.CustomData.Replace($"[{ϊ}.Integrate]",$"[{χ}]");}τ.Ǔ(
$"Carried out Integrate command, replacing the '{ϊ}.Integrate' "+$"section headers on {Ў.Count} blocks with '{χ}' headers.");break;case"evaluate":Save();ҁ(new Ƃ(Ɓ,15));break;case
"resetreports":if(ε.ĳ("ResetReports",10,out ĩ)){Ϸ(new к(this,κ,true));}else{τ.Ǔ(ĩ);}break;case"update":Ç();if(Δ.Switch("force")){
foreach(ɧ Ǹ in κ){Ǹ.ɰ();}foreach(ǋ Ϫ in ι){Ϫ.Ǉ();}}else{Ǉ();}if(Δ.Switch("performance")){τ.Ǔ(
$"Update used {Runtime.CurrentInstructionCount} / {Runtime.MaxInstructionCount} "+$"({(int)(((double)Runtime.CurrentInstructionCount/Runtime.MaxInstructionCount)*100)}%) "+
$"of instructions allowed in this tic.\n");}break;case"test":Action Ѝ=()=>{τ.Ǔ("Periodic event firing");};ħ Ќ=new ħ(10,Ѝ);ε.ē("Test Event",Ќ);if(!ε.ĳ(
"Test Cooldown",20,out ĩ)){τ.Ǔ(ĩ);}τ.Ǔ(ε.ī());break;default:τ.Ǔ($"Received un-recognized run command '{ǽ}'.");break;}}}Echo(τ.ǫ());if((
Μ&UpdateType.Update100)!=0){foreach(Ǻ Ћ in λ.Values){Ћ.Ǧ();}τ.Ǧ();}}void Ç(){foreach(v d in ξ){d.Ê();}foreach(l Њ in ο){Њ
.f();}foreach(v d in ξ){d.Ç();}Ơ Ȇ=null;bool Ͻ,ǿ;foreach(Ȩ ğ in μ){Ͻ=ğ.ȇ(out Ȇ,out ǿ);if(Ͻ){τ.Ǔ(Ǿ(Ȇ,ǿ,ǿ?"on":"off",
$"Trigger {ğ.s}'s "));}}}void Ǉ(){foreach(ɧ Ǹ in κ){Ǹ.Ǉ();}foreach(ǋ Ϫ in ι){Ϫ.Ǉ();}}string ϩ(string Ϩ,string ϧ,string Ϧ){Ơ Ȇ;bool ǿ;if(ν==
null){return"Received Action command, but script configuration isn't loaded.";}else if(ν.TryGetValue(Ϩ,out Ȇ)){if(ϧ=="on"){ǿ
=true;}else if(ϧ=="off"){ǿ=false;}else if(ϧ=="switch"){ǿ=!Ȇ.Ø;}else{return
$"Received unknown {Ϧ}command '{ϧ}' for ActionSet "+$"'{Ϩ}'. Valid commands for ActionSets are 'On', 'Off', and "+$"'Switch'.";}return Ǿ(Ȇ,ǿ,ϧ,Ϧ);}else{return
$"Received {Ϧ}command '{ϧ}' for un-recognized "+$"ActionSet '{Ϩ}'.";}}string Ǿ(Ơ Ȇ,bool ǿ,string ǽ,string Ϧ){string ĩ="";try{Ȇ.Ȗ(ǿ);}catch(InvalidCastException e){
string Ϥ="<ID not provided>";if(e.Data.Contains("Identifier")){Ϥ=$"{e.Data["Identifier"]}";}ĩ=
$"An invalid cast exception occurred while running {Ϧ}'{ǽ}' "+$"command for ActionSet '{Ȇ.s}' at {Ϥ}. Make sure "+$"the action specified in configuration can be performed by {Ϥ}.";}
catch(InvalidOperationException e){string ϥ="<Trace failed>";if(e.Data.Contains("Counter")){ϥ="Set Trace:\n";for(int ņ=(int)(
e.Data["Counter"]);ņ>=0;ņ--){ϥ+=$"{e.Data[ņ]}\n";}}ĩ=$"A possible loop was detected while running {Ϧ}'{ǽ}' command "+
$"for ActionSet '{Ȇ.s}'. Make sure {Ȇ.s} is "+$"not being called by one of the sets it is calling.\n\n{ϥ}";}catch(Exception e){string Ϥ="<ID not provided>";if(e.Data
.Contains("Identifier")){Ϥ=$"{e.Data["Identifier"]}";}ĩ=$"An exception occurred while running {Ϧ}'{ǽ}' command for "+
$"ActionSet '{Ȇ.s}' at {Ϥ}.\n  Raw exception message:\n "+$"{e.Message}\n  Stack trace:\n{e.StackTrace}";}Ǉ();foreach(Ơ Η in ν.Values){Η.Ȕ();}if(Ϲ(ĩ)&&!Ϲ(Ϧ)){ĩ=
$"Carried out {Ϧ}command '{ǽ}' for ActionSet '{Ȇ.s}'. "+$"The set's state is now '{Ȇ.Ț}'.";}return ĩ;}bool ϣ(string Ϣ,List<IMyTerminalBlock>ϡ,ref string Ψ){GridTerminalSystem.
GetBlockGroupWithName(Ϣ)?.GetBlocks(ϡ);if(ϡ.Count>0){return true;}else{Ψ=$"Received {Ψ}, but there is no {Ϣ} block group on the grid.";return
false;}}void ϫ<ċ>(List<ċ>ϼ,Func<ċ,bool>Ϻ=null)where ċ:class{GridTerminalSystem.GetBlocksOfType<ċ>(ϼ,Ϻ);}bool Ϲ(string ϸ){
return String.IsNullOrEmpty(ϸ);}bool Ϸ(Ч ϵ){string ϴ=ϵ.Ц;if(!ύ.ContainsKey(ϴ)){ύ.Add(ϴ,ϵ);Runtime.UpdateFrequency|=
UpdateFrequency.Update10;if(ϵ.Р&&π!=null){τ.Ǔ($"{ϴ} successfully added to scheduled tasks.");}return true;}else{τ.Ǔ(
$"Cannot schedule {ϴ} because an identical task is already scheduled.");return false;}}string ϳ(List<v>k,List<Ơ>õ,List<Ȩ>ϲ,List<Ǻ>ϱ){string ϻ;string ϰ=
";=======================================\n\n";Ɓ.Clear();foreach(v d in k){Ɓ.Append(d.ũ());}if(k.Count>0){Ɓ.Append(ϰ);}foreach(Ơ Û in õ){Ɓ.Append(Û.ũ());}if(õ.Count>0
){Ɓ.Append(ϰ);}foreach(Ȩ ğ in ϲ){Ɓ.Append(ğ.ũ());}if(ϲ.Count>0){Ɓ.Append(ϰ);}foreach(Ǻ Ʊ in ϱ){Ɓ.Append(Ʊ.ũ());}ϻ=Ɓ.
ToString();Ɓ.Clear();return ϻ;}bool ϯ(List<IMyTerminalBlock>Ϯ,MyIni ϭ,string Ϭ,ref string ĩ){MyIni ˑ=ς;MyIniParseResult ϒ;Ƃ ɷ=
new Ƃ(Ɓ,10);MyIniValue ě=ϭ.Get($"{ϊ}.Init","APExcludedDeclarations");List<string>ɸ=null;if(!Ϲ(ě.ToString())){ɸ=ě.ToString()
.Split(',').Select(Ļ=>Ļ.Trim()).ToList();}List<Ͷ>ʬ=ɹ(ɸ,ɷ);List<Ͷ>ʫ=new List<Ͷ>(ʬ);List<ˋ>ʪ=new List<ˋ>();List<ˉ>ʩ=new
List<ˉ>();List<ˁ>ʨ=new List<ˁ>();Action<Ͷ>ʧ=(ʦ)=>{if(ʦ is ˋ){ʪ.Add((ˋ)ʦ);}else if(ʦ is ˉ){ʩ.Add((ˉ)ʦ);}else if(ʦ is ˁ){ʨ.Add
((ˁ)ʦ);}ʬ.Remove(ʦ);};List<MyIniKey>ʥ=new List<MyIniKey>();List<string>ʤ=new List<string>();Action<string,MyIni,MyIni>ʣ=(
I,ʭ,ʢ)=>{ʭ.TryParse(I);ʭ.GetKeys(ʥ);foreach(MyIniKey ʀ in ʥ){ʤ.Add((ʭ.Get(ʀ)).ToString());}for(int ņ=0;ņ<ʥ.Count;ņ++){ʢ.
Set(ʥ[ņ],ʤ[ņ]);}ʤ.Clear();};string ʠ=$"{ϊ}.{ω}.ActionSet.Roost";if(!ϭ.ContainsSection(ʠ)&&!(ɸ?.Contains("Roost")??false)){Ơ
ʟ=new Ơ("Roost",false);ʟ.u=ψ;ʟ.Ȟ=Ϙ;ʟ.ȝ=Ϝ;ʟ.Ȝ="Roosting";ʟ.ț="Active";ï ʞ=new ï(this);ʞ.í=8;ʟ.Ș(ʞ);ʣ(ʟ.ũ(),ˑ,ϭ);}int ĭ=0;Ͷ
ʝ;string Ő;while(ĭ!=-1){ʝ=ʬ[ĭ];Ő=$"{ϊ}.{ω}.{ʝ.ˌ()}.{ʝ.E}";if(ϭ.ContainsSection(Ő)){ʧ(ʝ);ʫ.Remove(ʝ);}else{ĭ++;}if(ĭ>=ʬ.
Count){ĭ=-1;}}MyDefinitionId ʜ;HashSet<MyDefinitionId>ʛ=new HashSet<MyDefinitionId>();foreach(IMyTerminalBlock P in Ϯ){ʜ=P.
BlockDefinition;if(!ʛ.Contains(ʜ)){ĭ=0;while(ĭ!=-1&&ʬ.Count!=0){ʝ=ʬ[ĭ];if(ʝ.ˍ(P)){ʧ(ʝ);}else{ĭ++;}if(ĭ>=ʬ.Count){ĭ=-1;}}ʛ.Add(ʜ);}}
foreach(Ͷ ʡ in ʬ){ʫ.Remove(ʡ);}foreach(Ͷ ʮ in ʫ){ʣ(ʮ.ˎ(),ˑ,ϭ);}if(!(ɸ?.Contains("Roost")??false)){string ʶ;List<string>ʾ;
HashSet<string>ʽ;Action<string,bool>ʼ=(ʀ,Ø)=>{ʶ=ϭ.Get(ʠ,ʀ).ToString();if(!Ϲ(ʶ)){ʾ=ʶ.Split(',').Select(Ļ=>Ļ.Trim()).ToList();ʽ=
new HashSet<string>();foreach(string ʻ in ʾ){int ʺ=ʻ.IndexOf(':');if(ʺ!=-1){ʽ.Add(ʻ.Substring(0,ʺ));}else{ʽ.Add(ʻ);}}}else{
ʽ=null;ʾ=new List<string>();}string ǿ;foreach(ˁ ʹ in ʨ){string s=ʹ.E;ǿ=ʹ.Ͱ(Ø);if(!(ʽ?.Contains(s)??false)&&!Ϲ(ǿ)){ʾ.Add(
$"{s}: {ǿ}");}}ϭ.Set(ʠ,ʀ,Ŧ(ʾ,3,false));};ʼ("ActionSetsLinkedToOn",true);ʼ("ActionSetsLinkedToOff",false);}string ʸ="";List<string>ť
=new List<string>();string ʷ;Action<string>ʵ=(ǖ)=>{ϭ.Set(ʸ,"Title",ǖ);ϭ.Set(ʸ,"Columns","3");ϭ.Set(ʸ,"FontSize",".5");ϭ.
Set(ʸ,"ForeColor","Yellow");ϭ.Set(ʸ,"BackColor","Black");};ϭ.Set(χ,"Surface0Pages",
$"{((ʪ.Count>0||ʩ.Count>0)?"TallyReport, ":"")}"+$"{(ʨ.Count>0?"SetReport, ":"")}Log, TargetScript, FactionScript");ϭ.Set(χ,"Surface0MFD","APScreen");if(ʪ.Count>0||ʩ.
Count>0){ʸ="SW.TallyReport";foreach(Ͷ ʴ in ʪ){ť.Add(ʴ.E);}foreach(Ͷ ʴ in ʩ){ť.Add(ʴ.E);}ʷ=Ŧ(ť,3,false);ϭ.Set(ʸ,"Elements",ʷ);
ʵ("Tallies");}ť.Clear();if(ʨ.Count>0){ʸ="SW.SetReport";foreach(Ͷ ʂ in ʨ){ť.Add(ʂ.E);}ʷ=Ŧ(ť,3,false);ϭ.Set(ʸ,"Elements",ʷ)
;ʵ("Action Sets");}ʸ="SW.Log";ϭ.Set(ʸ,"DataType","Log");ϭ.Set(ʸ,"FontSize",".8");ϭ.Set(ʸ,"CharPerLine","30");ϭ.Set(ʸ,
"ForeColor","LightBlue");ϭ.Set(ʸ,"BackColor","Black");ʸ="SW.TargetScript";ϭ.Set(ʸ,"Script","TSS_TargetingInfo");ϭ.Set(ʸ,"ForeColor"
,"LightBlue");ϭ.Set(ʸ,"BackColor","Black");ʸ="SW.FactionScript";ϭ.Set(ʸ,"Script","TSS_FactionIcon");ϭ.Set(ʸ,"BackColor",
"Black");Me.CustomData=ϭ.ToString();Dictionary<MyDefinitionId,ˠ>ʳ=new Dictionary<MyDefinitionId,ˠ>();ˠ ʲ=null;int ʱ=0;int ʰ=0;
Func<IMyTerminalBlock,MyIni,bool>ʯ=(Ŵ,ʌ)=>{if(!ʌ.TryParse(Ŵ.CustomData,out ϒ)){ʱ++;ɷ.ƙ(
$"Block {Ŵ.CustomName} failed to parse due to the following "+$"error on line {ϒ.LineNo}: {ϒ.Error}");return false;}else{return true;}};Func<IMyTerminalBlock,MyIni,bool>ʚ=(Ŵ,ʌ)=>{if
(ʲ==null){if(ʯ(Ŵ,ʌ)){ʲ=new ˠ(Ŵ,ʌ,χ);return true;}else{return false;}}return true;};foreach(IMyTerminalBlock P in Ϯ){ʲ=
null;ʜ=P.BlockDefinition;if(ʳ.ContainsKey(ʜ)){ʲ=ʳ[ʜ];if(ʲ!=null){if(ʯ(P,ˑ)){ʲ.ʣ(χ,ˑ,!ˑ.ContainsSection(χ));P.CustomData=ˑ.
ToString();ʰ++;}}}else{foreach(ˋ ʄ in ʪ){if(ʄ.ˍ(P)){if(ʚ(P,ˑ)){ʲ.ѱ("Tallies",ʄ.E);}else{goto CannotWriteToThisBlockSoSkipToNext;
}}}foreach(ˉ ʃ in ʩ){if(P.InventoryCount==1){if(ʃ.ˆ(P.GetInventory(0))){if(ʚ(P,ˑ)){ʲ.ѱ("Tallies",ʃ.E);}else{goto
CannotWriteToThisBlockSoSkipToNext;}}}else if(P.InventoryCount>1){for(int ņ=0;ņ<P.InventoryCount;ņ++){if(ʃ.ˆ(P.GetInventory(ņ))){if(ʚ(P,ˑ)){ʲ.ѱ(
$"Inv{ņ}Tallies",ʃ.E);}else{goto CannotWriteToThisBlockSoSkipToNext;}}}}}foreach(ˁ ʂ in ʨ){if(ʂ.ˍ(P)){if(ʚ(P,ˑ)){ʲ.ѱ("ActionSets",ʂ.E);ʲ
.ʝ=ʂ;}else{goto CannotWriteToThisBlockSoSkipToNext;}}}ʳ.Add(ʜ,ʲ);if(ʲ!=null){ʲ.ʣ(χ,ˑ);P.CustomData=ˑ.ToString();ʰ++;}}
CannotWriteToThisBlockSoSkipToNext:;}ĩ=$"\nCarried out {Ϭ} command. There are now declarations for "+$"{ʪ.Count+ʩ.Count} AP Tallies and {ʨ.Count} "+
$"AP ActionSets, with linking config written to {ʰ} / {Ϯ.Count} of considered "+$"blocks{(ʱ>0?$" and {ʱ} blocks with unparsable config":"")}.\n"+
$"Autopopulate used {Runtime.CurrentInstructionCount} / {Runtime.MaxInstructionCount} "+$"({(int)(((double)Runtime.CurrentInstructionCount/Runtime.MaxInstructionCount)*100)}%) "+
$"of instructions allowed in this tic.\n";if(ɷ.Ɲ()>0){ĩ+=$"\nThe following errors prevented AutoPopulate from running:\n{ɷ.ƛ()}";}if(ɷ.Ƙ()>0){ĩ+=
$"\nThe following warnings should be addressed:\n{ɷ.Ɩ()}";}if(ɷ.Ɠ()>0){ĩ+=$"\nThe following messages were logged:\n{ɷ.Ƒ()}";}ϭ.Clear();ˑ.Clear();return true;}HashSet<string>ʁ(
string ʀ){HashSet<string>ɿ=new HashSet<string>();MyIniValue ě=σ.Get($"{ϊ}.Init",ʀ);string[]ť;if(!Ϲ(ě.ToString())){ť=ě.ToString
().Split(',').Select(Ļ=>Ļ.Trim()).ToArray();foreach(string Š in ť){ɿ.Add(Š);}}return ɿ;}void ɾ(HashSet<string>ɽ){string ɼ
=$"{ϊ}.FurnitureSubTypes";if(ɽ.Contains(ɼ)){ɽ.Remove(ɼ);ɽ.UnionWith(new string[]{"PassengerBench","PassengerSeatLarge",
"PassengerSeatSmallNew","PassengerSeatSmallOffset","LargeBlockBed","LargeBlockHalfBed","LargeBlockHalfBedOffset","LargeBlockInsetBed",
"LargeBlockCaptainDesk","LargeBlockLabDeskSeat","LargeBlockLabCornerDesk"});}string ɻ=$"{ϊ}.IsolatedCockpitSubTypes";if(ɽ.Contains(ɻ)){ɽ.Remove
(ɻ);ɽ.UnionWith(new string[]{"BuggyCockpit","OpenCockpitLarge","OpenCockpitSmall","LargeBlockCockpit","CockpitOpen",
"SmallBlockStandingCockpit","RoverCockpit","SpeederCockpitCompact","LargeBlockStandingCockpit","LargeBlockModularBridgeCockpit"});}string ɺ=
$"{ϊ}.ShelfSubTypes";if(ɽ.Contains(ɺ)){ɽ.Remove(ɺ);ɽ.UnionWith(new string[]{"LargeBlockLockerRoom","LargeBlockLockerRoomCorner","LargeCrate"
,"LargeBlockInsetBookshelf","LargeBlockLockers","LargeBlockInsetKitchen","LargeBlockWeaponRack","SmallBlockWeaponRack",
"SmallBlockKitchenFridge","SmallBlockFirstAidCabinet","LargeBlockLabCabinet","LargeFreezer"});}}List<Ͷ>ɹ(List<string>ɸ,Ƃ ɷ){StringComparer ɶ=
StringComparer.OrdinalIgnoreCase;Dictionary<string,Ͷ>ɵ=new Dictionary<string,Ͷ>(ɶ);const string ɴ="MyObjectBuilder_Ore";const string ʅ
="MyObjectBuilder_Ingot";const string ɳ="MyObjectBuilder_AmmoMagazine";MyItemType ʆ=new MyItemType(ɴ,"Ice");MyItemType ʙ=
new MyItemType(ɴ,"Stone");MyItemType ʘ=new MyItemType(ɴ,"Iron");MyItemType ʗ=new MyItemType(ʅ,"Uranium");MyItemType ʖ=new
MyItemType(ɳ,"NATO_25x184mm");MyItemType ʕ=new MyItemType(ɳ,"AutocannonClip");MyItemType ʔ=new MyItemType(ɳ,"MediumCalibreAmmo");
MyItemType ʓ=new MyItemType(ɳ,"LargeCalibreAmmo");MyItemType ʒ=new MyItemType(ɳ,"SmallRailgunAmmo");MyItemType ʑ=new MyItemType(ɳ,
"LargeRailgunAmmo");MyItemType ʐ=new MyItemType(ɳ,"Missile200mm");MyDefinitionId ʏ=MyDefinitionId.Parse(
"MyObjectBuilder_GasProperties/Hydrogen");MyDefinitionId ʎ=MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Oxygen");v d;ű ʍ=new ű();ŭ ʋ=new ŭ();List<
MyItemType>ʊ=new List<MyItemType>();Func<IMyInventory,MyItemType,bool>ʉ=(ʈ,ʇ)=>{ʊ.Clear();ʈ.GetAcceptedItems(ʊ);return(ʊ.Contains(
ʇ));};d=new Ã(ρ,"Power",new ƶ(),ʋ);ɵ.Add(d.s,new ˋ(d.s,d,Ŵ=>Ŵ is IMyBatteryBlock));d=new Ã(ρ,"Hydrogen",new ƪ(),ʋ);ɵ.Add(
d.s,new ˋ(d.s,d,Ŵ=>(Ŵ is IMyGasTank&&(Ŵ.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(ʏ)??false))
||(Ŵ is IMyPowerProducer&&(Ŵ.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(ʏ)??false))));d=new Ã(ρ
,"Oxygen",new ƪ(),ʋ);ɵ.Add(d.s,new ˋ(d.s,d,Ŵ=>Ŵ is IMyGasTank&&(Ŵ.Components.Get<MyResourceSinkComponent>()?.
AcceptedResources.Contains(ʎ)??false)));d=new N(ρ,"Cargo",ʍ);ɵ.Add(d.s,new ˉ(d.s,d,Ŵ=>Ŵ is IMyCargoContainer,ņ=>ʉ(ņ,ʆ)&&ʉ(ņ,ʗ)));d=new H(
ρ,"Ice",ʆ,ʋ);d.È(4000);ɵ.Add(d.s,new ˉ(d.s,d,Ŵ=>Ŵ is IMyGasGenerator,ņ=>ʉ(ņ,ʆ)));d=new H(ρ,"Stone",ʙ,ʍ);d.È(5000);ɵ.Add(d
.s,new ˉ(d.s,d,Ŵ=>Ŵ is IMyShipDrill||Ŵ is IMyRefinery,ņ=>ʉ(ņ,ʙ)));d=new N(ρ,"Ore",ʍ);ɵ.Add(d.s,new ˉ(d.s,d,Ŵ=>Ŵ is
IMyShipDrill||Ŵ is IMyRefinery,ņ=>ʉ(ņ,ʘ)));d=new H(ρ,"Uranium",ʗ,ʋ);d.È(50);ɵ.Add(d.s,new ˉ(d.s,d,Ŵ=>Ŵ is IMyReactor,ņ=>ʉ(ņ,ʗ)));d=
new Ã(ρ,"Solar",new Ư(),ʋ);d.A=100;ɵ.Add(d.s,new ˋ(d.s,d,Ŵ=>Ŵ is IMySolarPanel));d=new Ã(ρ,"JumpDrive",new Ƣ(),ʋ);d.u=
"Jump Charge";ɵ.Add(d.s,new ˋ(d.s,d,Ŵ=>Ŵ is IMyJumpDrive));Func<IMyTerminalBlock,MyItemType,bool>ͳ=(Ŵ,ņ)=>{return Ŵ is
IMyUserControllableGun&&ʉ(Ŵ.GetInventory(0),ņ);};d=new H(ρ,"GatlingAmmo",ʖ,ʋ);d.È(20);d.u="Gatling\nDrums";ɵ.Add(d.s,new ˉ(d.s,d,Ŵ=>ͳ(Ŵ,ʖ),ņ=>
ʉ(ņ,ʖ)));d=new H(ρ,"AutocannonAmmo",ʕ,ʋ);d.È(60);d.u="Autocannon\nClips";ɵ.Add(d.s,new ˉ(d.s,d,Ŵ=>ͳ(Ŵ,ʕ),ņ=>ʉ(ņ,ʕ)));d=
new H(ρ,"AssaultAmmo",ʔ,ʋ);d.È(120);d.u="Cannon\nShells";ɵ.Add(d.s,new ˉ(d.s,d,Ŵ=>ͳ(Ŵ,ʔ),ņ=>ʉ(ņ,ʔ)));d=new H(ρ,
"ArtilleryAmmo",ʓ,ʋ);d.È(40);d.u="Artillery\nShells";ɵ.Add(d.s,new ˉ(d.s,d,Ŵ=>ͳ(Ŵ,ʓ),ņ=>ʉ(ņ,ʓ)));d=new H(ρ,"RailSmallAmmo",ʒ,ʋ);d.È(36)
;d.u="Railgun\nS. Sabots";ɵ.Add(d.s,new ˉ(d.s,d,Ŵ=>ͳ(Ŵ,ʒ),ņ=>ʉ(ņ,ʒ)));d=new H(ρ,"RailLargeAmmo",ʑ,ʋ);d.È(12);d.u=
"Railgun\nL. Sabots";ɵ.Add(d.s,new ˉ(d.s,d,Ŵ=>ͳ(Ŵ,ʑ),ņ=>ʉ(ņ,ʑ)));d=new H(ρ,"RocketAmmo",ʐ,ʋ);d.È(24);d.u="Rockets";ɵ.Add(d.s,new ˉ(d.s,d,Ŵ=>
ͳ(Ŵ,ʐ),ņ=>ʉ(ņ,ʐ)));Ơ Ͳ;Action<MyIni,string,string,string>ͱ=(ʌ,ʹ,ͼ,ͽ)=>{ʌ.Set(ʹ,"ActionOn",ͼ);ʌ.Set(ʹ,"ActionOff",ͽ);};
Action<MyIni,string,string,string>Ά=(ʌ,ʹ,ͼ,ͽ)=>{ʌ.Set(ʹ,"Action0Property","Radius");ʌ.Set(ʹ,"Action0ValueOn","1500");ʌ.Set(ʹ,
"Action0ValueOff","150");ʌ.Set(ʹ,"Action1Property","HudText");ʌ.Set(ʹ,"Action1ValueOn",ͼ);ʌ.Set(ʹ,"Action1ValueOff",ͽ);};Ͳ=new Ơ(
"Antennas",false);Ͳ.u="Antenna\nRange";Ͳ.Ȝ="Broad";Ͳ.ț="Wifi";Ͳ.ȝ=Ϛ;ɵ.Add(Ͳ.s,new ˁ(Ͳ.s,Ͳ,Ŵ=>Ŵ is IMyRadioAntenna,$"{ϊ}.{Ͳ.s}",
$"{ψ}",$"{ψ} Wifi",Ά,"Off","On"));Ͳ=new Ơ("Beacons",false);Ͳ.u="Beacon";Ͳ.Ȝ="Online";Ͳ.ț="Offline";ɵ.Add(Ͳ.s,new ˁ(Ͳ.s,Ͳ,Ŵ=>Ŵ
is IMyBeacon,$"{ϊ}.{Ͳ.s}","EnableOn","EnableOff",ͱ,"Off","On"));Ͳ=new Ơ("Spotlights",false);Ͳ.Ȝ="Online";Ͳ.ț="Offline";ɵ.
Add(Ͳ.s,new ˁ(Ͳ.s,Ͳ,Ŵ=>Ŵ is IMyReflectorLight,$"{ϊ}.{Ͳ.s}","EnableOn","EnableOff",ͱ,"Off",""));Ͳ=new Ơ("OreDetectors",false
);Ͳ.u="Ore\nDetector";Ͳ.Ȝ="Scanning";Ͳ.ț="Idle";ɵ.Add(Ͳ.s,new ˁ(Ͳ.s,Ͳ,Ŵ=>Ŵ is IMyOreDetector,$"{ϊ}.{Ͳ.s}","EnableOn",
"EnableOff",ͱ,"Off","On"));Ͳ=new Ơ("Batteries",false);Ͳ.Ȝ="On Auto";Ͳ.ț="Recharging";Ͳ.ȝ=Ϛ;ɵ.Add(Ͳ.s,new ˁ(Ͳ.s,Ͳ,Ŵ=>Ŵ is
IMyBatteryBlock,$"{ϊ}.{Ͳ.s}","BatteryAuto","BatteryRecharge",ͱ,"Off","On"));Ͳ=new Ơ("Reactors",false);Ͳ.Ȝ="Active";Ͳ.ț="Inactive";ɵ.Add
(Ͳ.s,new ˁ(Ͳ.s,Ͳ,Ŵ=>Ŵ is IMyReactor,$"{ϊ}.{Ͳ.s}","EnableOn","EnableOff",ͱ,"Off",""));Ͳ=new Ơ("EnginesHydrogen",false);Ͳ.u
="Engines";Ͳ.Ȝ="Running";Ͳ.ț="Idle";ɵ.Add(Ͳ.s,new ˁ(Ͳ.s,Ͳ,Ŵ=>Ŵ is IMyPowerProducer&&(Ŵ.Components.Get<
MyResourceSinkComponent>()?.AcceptedResources.Contains(ʏ)??false),$"{ϊ}.{Ͳ.s}","EnableOn","EnableOff",ͱ,"Off",""));Ͳ=new Ơ("IceCrackers",false)
;Ͳ.u="Ice Crackers";Ͳ.Ȝ="Running";Ͳ.ț="Idle";ɵ.Add(Ͳ.s,new ˁ(Ͳ.s,Ͳ,Ŵ=>Ŵ is IMyGasGenerator,$"{ϊ}.{Ͳ.s}","EnableOn",
"EnableOff",ͱ,"",""));Ͳ=new Ơ("TanksHydrogen",false);Ͳ.u="Hydrogen\nTanks";Ͳ.Ȝ="Open";Ͳ.ț="Filling";Ͳ.ȝ=ϛ;ɵ.Add(Ͳ.s,new ˁ(Ͳ.s,Ͳ,Ŵ=>
Ŵ is IMyGasTank&&(Ŵ.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(ʏ)??false),$"{ϊ}.{Ͳ.s}",
"TankStockpileOff","TankStockpileOn",ͱ,"Off","On"));Ͳ=new Ơ("TanksOxygen",false);Ͳ.u="Oxygen\nTanks";Ͳ.Ȝ="Open";Ͳ.ț="Filling";Ͳ.ȝ=ϛ;ɵ.Add(
Ͳ.s,new ˁ(Ͳ.s,Ͳ,Ŵ=>Ŵ is IMyGasTank&&(Ŵ.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(ʎ)??false),
$"{ϊ}.{Ͳ.s}","TankStockpileOff","TankStockpileOn",ͱ,"Off","On"));Ͳ=new Ơ("Gyroscopes",false);Ͳ.u="Gyros";Ͳ.Ȝ="Active";Ͳ.ț="Inactive"
;ɵ.Add(Ͳ.s,new ˁ(Ͳ.s,Ͳ,Ŵ=>Ŵ is IMyGyro,$"{ϊ}.{Ͳ.s}","EnableOn","EnableOff",ͱ,"Off","On"));Ͳ=new Ơ("ThrustersAtmospheric",
false);Ͳ.u="Atmospheric\nThrusters";Ͳ.Ȝ="Online";Ͳ.ț="Offline";ɵ.Add(Ͳ.s,new ˁ(Ͳ.s,Ͳ,Ŵ=>Ŵ is IMyThrust&&(Ŵ.BlockDefinition.
SubtypeId.Contains("Atmospheric")),$"{ϊ}.{Ͳ.s}","EnableOn","EnableOff",ͱ,"Off","On"));Ͳ=new Ơ("ThrustersIon",false);Ͳ.u=
"Ion\nThrusters";Ͳ.Ȝ="Online";Ͳ.ț="Offline";ɵ.Add(Ͳ.s,new ˁ(Ͳ.s,Ͳ,Ŵ=>Ŵ is IMyThrust&&(!Ŵ.BlockDefinition.SubtypeId.Contains(
"Atmospheric")&&!Ŵ.BlockDefinition.SubtypeId.Contains("Hydrogen")),$"{ϊ}.{Ͳ.s}","EnableOn","EnableOff",ͱ,"Off","On"));Ͳ=new Ơ(
"ThrustersHydrogen",false);Ͳ.u="Hydrogen\nThrusters";Ͳ.Ȝ="Online";Ͳ.ț="Offline";ɵ.Add(Ͳ.s,new ˁ(Ͳ.s,Ͳ,Ŵ=>Ŵ is IMyThrust&&(Ŵ.BlockDefinition
.SubtypeId.Contains("Hydrogen")),$"{ϊ}.{Ͳ.s}","EnableOn","EnableOff",ͱ,"Off","On"));Ͳ=new Ơ("ThrustersGeneric",false);Ͳ.u
="Thrusters";Ͳ.Ȝ="Online";Ͳ.ț="Offline";ɵ.Add(Ͳ.s,new ˁ(Ͳ.s,Ͳ,Ŵ=>Ŵ is IMyThrust,$"{ϊ}.{Ͳ.s}","EnableOn","EnableOff",ͱ,
"Off","On"));if(ɸ!=null){int ņ=0;string ͻ;while(ņ<ɸ.Count){ͻ=ɸ[ņ];if(ɵ.ContainsKey(ͻ)){ɵ.Remove(ͻ);ɸ.RemoveAt(ņ);}else{ņ++;}}
if(ņ>0){string ͺ="";foreach(string ͷ in ɸ){ͺ+=$"{ͷ}, ";}ͺ=ͺ.Remove(ͺ.Length-2);ɷ.ƙ(
"The following entries from APExcludedDeclarations could not "+$"be matched to declarations: {ͺ}.");}}return ɵ.Values.ToList();}abstract class Ͷ{public string E{get;private set;}
protected ş ˈ;Func<IMyTerminalBlock,bool>ʿ;public Ͷ(string E,ş ˈ,Func<IMyTerminalBlock,bool>ʿ){this.E=E;this.ˈ=ˈ;this.ʿ=ʿ;}public
string ˎ(){return ˈ.ũ();}public bool ˍ(IMyTerminalBlock P){return ʿ(P);}public abstract string ˌ();}class ˋ:Ͷ{public ˋ(string
E,ş ˈ,Func<IMyTerminalBlock,bool>ʿ):base(E,ˈ,ʿ){}public override string ˌ(){return"Tally";}}class ˉ:Ͷ{Func<IMyInventory,
bool>ˇ;public ˉ(string E,ş ˈ,Func<IMyTerminalBlock,bool>ʿ,Func<IMyInventory,bool>ˇ):base(E,ˈ,ʿ){this.ˇ=ˇ;}public bool ˆ(
IMyInventory J){return ˇ(J);}public override string ˌ(){return"Tally";}}class ˁ:Ͷ{string ˀ,ˊ,ˏ;Action<MyIni,string,string,string>ˤ;
public string ˮ{get;private set;}public string ˬ{get;private set;}public ˁ(string E,ş ˈ,Func<IMyTerminalBlock,bool>ʿ,string ː,
string ˊ,string ˏ,Action<MyIni,string,string,string>ˤ,string ˮ,string ˬ):base(E,ˈ,ʿ){ˀ=ː;this.ˊ=ˊ;this.ˏ=ˏ;this.ˤ=ˤ;this.ˮ=ˮ;
this.ˬ=ˬ;}internal string Ͱ(bool Ø){return Ø?ˮ:ˬ;}public void ˣ(MyIni ˑ){ˤ.Invoke(ˑ,ˀ,ˊ,ˏ);}public string ˢ(){return String.
IsNullOrEmpty(ˮ)?"":$"{E}: {ˮ}";}public string ˡ(){return String.IsNullOrEmpty(ˬ)?"":$"{E}: {ˬ}";}public override string ˌ(){return
"ActionSet";}}class ˠ{internal Dictionary<string,Ѷ>I{get;private set;}internal ˁ ʝ;public ˠ(IMyTerminalBlock P,MyIni ˑ,string ː){
int Ξ=P.InventoryCount;I=new Dictionary<string,Ѷ>();if(Ξ>1){for(int ņ=0;ņ<Ξ;ņ++){Ѹ(ˑ,ː,$"Inv{ņ}Tallies");}}else{Ѹ(ˑ,ː,
"Tallies");}Ѹ(ˑ,ː,"ActionSets");ʝ=null;}private void Ж(string ʀ,string ѳ,bool Ѵ=false){I.Add(ʀ,new Ѷ(ѳ,Ѵ));}private bool Ѹ(MyIni
ʌ,string ː,string ʀ){if(ʌ.ContainsKey(ː,ʀ)){Ж(ʀ,ʌ.Get(ː,ʀ).ToString());return true;}return false;}public void ѱ(string ʀ,
string ʻ){if(I.ContainsKey(ʀ)){I[ʀ].ѱ(ʻ);}else{Ж(ʀ,ʻ,true);}}public void ʣ(string ː,MyIni ʌ,bool ѷ=false){foreach(KeyValuePair
<string,Ѷ>Ĩ in I){Ĩ.Value.ѯ(ʌ,ː,Ĩ.Key,ѷ);}if(ʝ!=null){ʝ.ˣ(ʌ);}}}class Ѷ{public string ѵ{get;private set;}bool Ѵ;public Ѷ(
string ѳ,bool Ѳ=false){ѵ=ѳ;Ѵ=Ѳ;}public void ѱ(string Ѱ){if(!ѵ.Contains(Ѱ)){ѵ=$"{ѵ}, {Ѱ}";Ѵ=true;}}public void ѯ(MyIni ʌ,string
ː,string ʀ,bool ѷ){if(Ѵ||ѷ){ʌ.Set(ː,ʀ,ѵ);}}}void ҁ(Ƃ Ě,bool ϐ=false){StringComparer ɶ=StringComparer.OrdinalIgnoreCase;ч
Ŏ=new ч(ɶ);Dictionary<string,v>љ=new Dictionary<string,v>(ɶ);Dictionary<string,Ơ>ј=new Dictionary<string,Ơ>(ɶ);Dictionary
<string,Ȩ>ї=new Dictionary<string,Ȩ>(ɶ);Dictionary<string,Ǻ>і=new Dictionary<string,Ǻ>(ɶ);Dictionary<IMyInventory,List<N>
>ѿ=new Dictionary<IMyInventory,List<N>>();List<ɧ>Ѿ=new List<ɧ>();List<ƻ>ѽ=new List<ƻ>();Dictionary<string,ɫ>Ҁ=new
Dictionary<string,ɫ>(ɶ);Dictionary<string,ǋ>Ѽ=new Dictionary<string,ǋ>(ɶ);HashSet<string>ѕ=new HashSet<string>(ɶ);string ѻ="";
MyIniParseResult ϒ;MyIniValue ě=new MyIniValue();int Ѻ=-1;ς.TryParse(Storage);int Θ=ς.Get("Data","UpdateDelay").ToInt32(0);int ў=-1;if(!
σ.TryParse(Me.CustomData,out ϒ)){Ě.Ɵ($"The parser encountered an error on line {ϒ.LineNo} of the "+
$"Programmable Block's config: {ϒ.Error}");}else{џ(Ŏ,Ě,ě,out ў);ћ(Me,Ě,Ŏ,љ,ј,ї,і,ѕ,ϒ,ě);if(Ě.Ɲ()>0){Ě.ƙ(
"Errors in Programmable Block configuration have prevented grid "+"configuration from being evaluated.");}else{ѻ=б();Ѻ=Ҩ(Ě,Ŏ,љ,ј,ї,і,ѿ,Ҁ,Ѿ,ѽ,Ѽ,ϒ,ě);}}if(η==null||Ě.Ɲ()==0||ѽ.Count>=η.
Count){η=ѽ;}string ĩ="Evaluation complete.\n";if(Ě.Ɲ()>0){ĩ+="Errors prevent the use of this configuration. ";if(ϕ){Runtime.
UpdateFrequency=UpdateFrequency.Update100;ĩ+=$"Execution continuing with last good configuration from "+
$"{(DateTime.Now-ϟ).Minutes} minutes ago "+$"({ϟ.ToString("HH: mm: ss")}).\n";}else{Runtime.UpdateFrequency=UpdateFrequency.None;ĩ+=
"Because there is no good configuration loaded, the script has been halted.\n";}ĩ+=$"\nThe following errors are preventing the use of this config:\n{Ě.ƛ()}";}else{l Њ;int Ù=0;ο=new l[ѿ.Count];
foreach(IMyInventory J in ѿ.Keys){Њ=new l(J,ѿ[J].ToArray());Њ.j();ο[Ù]=Њ;Ù++;}ξ=љ.Values.ToArray();μ=ї.Values.ToArray();κ=Ѿ.
ToArray();ι=Ѽ.Values.ToArray();ν=ј;λ=і;θ=Ҁ;foreach(v ѹ in ξ){ѹ.Ï();}foreach(ɧ ɥ in κ){ɥ.ɲ();}π?.н();π=null;ύ.Clear();ε.Ī();Ƈ(Θ)
;if(ў>-1){if(ў<10){Ě.ƙ($"{ϊ}.Init, key 'MPSpriteSyncFrequency' "+
$"requested an invalid frequncy of {ў}. Sync frequency has "+$"been set to the lowest allowed value of 10 instead.");ў=10;}Action Ѯ=()=>{Ϸ(new к(this,κ,false));};ħ є=new ħ(ў,Ѯ);ε.Ē
("SpriteRefresher",є);}ϕ=true;ϟ=DateTime.Now;ϝ=ѻ;ĩ+=$"Script is now running. Registered {ξ.Length} tallies, "+
$"{ν.Count} ActionSets, {μ.Length} triggers, and {κ.Length} "+$"reports, as configured by data on {Ѻ} blocks. Evaluation used "+
$"{Runtime.CurrentInstructionCount} / {Runtime.MaxInstructionCount} "+$"({(int)(((double)Runtime.CurrentInstructionCount/Runtime.MaxInstructionCount)*100)}%) "+
$"of instructions allowed in this tic.\n";Runtime.UpdateFrequency=UpdateFrequency.Update100;}if(Ě.Ɠ()>0){ĩ+=$"\nThe following messages were logged:\n{Ě.Ƒ()}";}if
(Ě.Ƙ()>0){ĩ+=$"\nThe following warnings were logged:\n{Ě.Ɩ()}";}τ.Ǔ(ĩ);foreach(ƻ Ѡ in η){Ѡ.Ǉ();}σ.Clear();ς.Clear();}void
џ(ч Ŏ,Ƃ Ě,MyIniValue ě,out int ў){ϑ(Ě);Color ė;string Ϗ=$"{ϊ}.Init";Action<string>ł=ŋ=>{Ě.Ɵ($"{Ϗ}{ŋ}");};ű ʍ=(ű)(Ŏ.э(
"LowGood"));ŭ ʋ=(ŭ)(Ŏ.э("HighGood"));Action<string>ѝ=ќ=>{if(Ŏ.ф(ł,σ,Ϗ,$"Color{ќ}",out ė)){ʍ.Ű(ќ,ė);ʋ.Ű(ќ,ė);}};ѝ("Optimal");ѝ(
"Normal");ѝ("Caution");ѝ("Warning");ѝ("Critical");ў=σ.Get(Ϗ,"MPSpriteSyncFrequency").ToInt32(-1);}void ћ(IMyTerminalBlock њ,Ƃ Ě,
ч Ŏ,Dictionary<string,v>љ,Dictionary<string,Ơ>ј,Dictionary<string,Ȩ>ї,Dictionary<string,Ǻ>і,HashSet<string>ѕ,
MyIniParseResult ϒ,MyIniValue ě){string ѡ="";string ѥ="";Color ė=Color.Black;ű ʍ=(ű)(Ŏ.э("LowGood"));ŭ ʋ=(ŭ)(Ŏ.э("HighGood"));ś B;List<
string>Ѭ=new List<string>();List<Ã>ѫ=new List<Ã>();Action<string>Ѫ=Ŵ=>Ě.Ɵ(Ŵ);Action<string>ѩ=ŋ=>{Ě.Ɵ($"{ѡ} {ѥ}{ŋ}");};
StringComparison Ѩ=StringComparison.OrdinalIgnoreCase;List<string>ѧ=new List<string>();List<string>ѭ=new List<string>();List<string>Ѧ=
new List<string>();string[]Ѥ;σ.GetSections(Ѧ);foreach(string Ő in Ѧ){Ѥ=Ő.Split('.').Select(Ļ=>Ļ.Trim()).ToArray();if(Ѥ.
Length==4&&String.Equals(Ѥ[0],ϊ,Ѩ)){ѡ=Ѥ[2];ѥ=Ѥ[3];if(ѡ.Equals("Tally",Ѩ)){v d=null;string ѣ;ѣ=σ.Get(Ő,"Type").ToString().
ToLowerInvariant();if(Ϲ(ѣ)){Ě.Ɵ($"{ѡ} {ѥ} has a missing or unreadable Type.");}else if(ѣ=="inventory"){d=new N(ρ,ѥ,ʍ);}else if(ѣ=="item"
){string D,C;D=σ.Get(Ő,"ItemTypeID").ToString();if(Ϲ(D)){Ě.Ɵ($"{ѡ} {ѥ} has a missing or unreadable ItemTypeID.");}C=σ.Get
(Ő,"ItemSubTypeID").ToString();if(Ϲ(C)){Ě.Ɵ($"{ѡ} {ѥ} has a missing or unreadable ItemSubTypeID.");}if(!Ϲ(D)&&!Ϲ(C)){d=
new H(ρ,ѥ,D,C,ʋ);}}else if(ѣ=="battery"){d=new Ã(ρ,ѥ,new ƶ(),ʋ);}else if(ѣ=="gas"){d=new Ã(ρ,ѥ,new ƪ(),ʋ);}else if(ѣ==
"jumpdrive"){d=new Ã(ρ,ѥ,new Ƣ(),ʋ);}else if(ѣ=="raycast"){d=new Ã(ρ,ѥ,new ƴ(),ʋ);Ѭ.Add(Ő);ѫ.Add((Ã)d);}else if(ѣ=="powermax"){d=
new Ã(ρ,ѥ,new Ư(),ʋ);}else if(ѣ=="powercurrent"){d=new Ã(ρ,ѥ,new ƭ(),ʋ);}else if(ѣ=="integrity"){d=new Ã(ρ,ѥ,new ǰ(),ʋ);}
else if(ѣ=="ventpressure"){d=new Ã(ρ,ѥ,new ǯ(),ʋ);}else if(ѣ=="pistonextension"){d=new Ã(ρ,ѥ,new ǭ(),ʋ);}else if(ѣ==
"rotorangle"){d=new Ã(ρ,ѥ,new ǳ(),ʋ);}else if(ѣ=="controllergravity"){d=new Ã(ρ,ѥ,new Ǳ(),ʋ);}else if(ѣ=="controllerspeed"){d=new Ã(
ρ,ѥ,new ǵ(),ʋ);}else if(ѣ=="controllerweight"){d=new Ã(ρ,ѥ,new Ǭ(),ʋ);}else{Ě.Ɵ(
$"{ѡ} {ѥ} has un-recognized Type of '{ѣ}'.");}if(d==null){d=new N(ρ,ѥ,ʍ);}ě=σ.Get(Ő,"DisplayName");if(!ě.IsEmpty){d.u=ě.ToString();}ě=σ.Get(Ő,"Multiplier");if(!ě.
IsEmpty){d.A=ě.ToDouble();}ě=σ.Get(Ő,"Max");if(!ě.IsEmpty){d.È(ě.ToDouble());}else if(ě.IsEmpty&&(d is H||(d is Ã&&((Ã)d).m is
Ǭ))){Ě.Ɵ($"{ѡ} {ѥ}'s TallyType of '{ѣ}' requires a Max "+$"to be set in configuration.");}if(Ŏ.ё(ѩ,σ,Ő,"ColorCoder",out B
)){d.B=B;}if(!щ(ѕ,d.s,Ő,Ě)){љ.Add(d.s,d);ѕ.Add(d.s);}}else if(ѡ.Equals("ActionSet",Ѩ)){bool Ŀ=ς?.Get("ActionSets",ѥ).
ToBoolean(false)??false;Ơ Η=new Ơ(ѥ,Ŀ);ě=σ.Get(Ő,"DisplayName");if(!ě.IsEmpty){Η.u=ě.ToString();}if(Ŏ.ф(ѩ,σ,Ő,"ColorOn",out ė)){Η
.Ȟ=ė;}if(Ŏ.ф(ѩ,σ,Ő,"ColorOff",out ė)){Η.ȝ=ė;}ě=σ.Get(Ő,"TextOn");if(!ě.IsEmpty){Η.Ȝ=ě.ToString();}ě=σ.Get(Ő,"TextOff");if
(!ě.IsEmpty){Η.ț=ě.ToString();}if(!щ(ѕ,Η.s,Ő,Ě)){Η.Ǽ();ј.Add(Η.s,Η);ѕ.Add(Η.s);}}else if(ѡ.Equals("Trigger",Ѩ)){bool Ŀ=ς?
.Get("Triggers",ѥ).ToBoolean(true)??true;Ȩ ğ=new Ȩ(ѥ,Ŀ);if(!щ(ѕ,ğ.s,Ő,Ě)){ї.Add(ğ.s,ğ);ѕ.Add(ğ.s);}}else if(ѡ.Equals(
"Raycaster",Ѩ)){Ǻ Ʊ=new Ǻ(Ɓ,ѥ);ȉ ǹ=null;string[]Ҏ=null;double[]ҡ=null;string ҟ=σ.Get(Ő,"Type").ToString().ToLowerInvariant();if(Ϲ(ҟ
)){Ě.Ɵ($"{ѡ} {ѥ} has a missing or unreadable Type.");}else if(ҟ=="linear"){ǹ=new ɜ();Ҏ=ɜ.ɘ();ҡ=new double[Ҏ.Length];}else
{Ě.Ɵ($"{ѡ} {ѥ} has un-recognized Type of '{ҟ}'.");}if(ǹ!=null){for(int ņ=0;ņ<Ҏ.Length;ņ++){ҡ[ņ]=σ.Get(Ő,Ҏ[ņ]).ToDouble(-1
);}ǹ.ɝ(ҡ);Ʊ=new Ǻ(Ɓ,ǹ,ѥ);}else{ѭ.Add(ѥ);}if(!щ(ѕ,ѥ,Ő,Ě)){і.Add(Ʊ.s,Ʊ);ѕ.Add(ѥ);}}else{if(Ѥ[1]==ω){Ě.Ɵ(
$"{Ő} referenced the unknown declaration "+$"type '{ѡ}'.");}else{Ě.ƙ($"{Ő} has the format of a declaration "+$"header but lacks the '{ω}' prefix and has been "+
$"discarded.");}}}}ѡ="Raycaster";for(int ņ=0;ņ<ѫ.Count;ņ++){string Ő=Ѭ[ņ];Ã Ҟ=ѫ[ņ];ě=σ.Get(Ő,"Raycaster");if(ě.IsEmpty){if(!Ҟ.o){Ě.Ɵ(
$"{ѡ} {Ҟ.s}'s "+$"Type of 'Raycaster' requires either a Max or a linked Raycaster to "+$"be set in configuration.");}}else{string ή=ě.
ToString();if(Ҟ.o){Ě.ƙ($"{ѡ} {Ҟ.s} specifies "+$"both a Max and a linked Raycaster, '{ή}'. Only one of these "+
$"values is required. The linked Raycaster has been ignored.");}else{Ǻ Ʊ;if(і.TryGetValue(ή,out Ʊ)){Ҟ.È(Ʊ.ȍ());}else{Ě.Ɵ($"{ѡ} {Ҟ.s} tried "+
$"to reference the unconfigured Raycaster '{ή}'.");}}}}ѡ="Trigger";foreach(Ȩ ğ in ї.Values){v d=null;Ơ Η=null;ѥ=ğ.s;string Ő=$"{ϊ}.{ω}.Trigger.{ѥ}";ě=σ.Get(Ő,"Tally");if
(!ě.IsEmpty){string Ҡ=ě.ToString();if(љ.TryGetValue(Ҡ,out d)){ğ.Ƿ=d;}else{Ě.Ɵ($"{ѡ} {ѥ} tried to reference "+
$"the unconfigured Tally '{Ҡ}'.");}}else{Ě.Ɵ($"{ѡ} {ѥ} has a missing or unreadable Tally.");}ě=σ.Get(Ő,"ActionSet");if(!ě.IsEmpty){string ҝ=ě.ToString()
;if(ј.TryGetValue(ҝ,out Η)){ğ.Ȇ=Η;}else{Ě.Ɵ($"{ѡ} {ѥ} tried to reference "+$"the unconfigured ActionSet '{ҝ}'.");}}else{Ě
.Ɵ($"{ѡ} {ѥ} has a missing or unreadable ActionSet.");}Ġ(ğ,Ő,true,"LessOrEqual",ě,Ě);Ġ(ğ,Ő,false,"GreaterOrEqual",ě,Ě);if
(!ğ.ǻ()){Ě.Ɵ($"{ѡ} {ѥ} does not define a valid "+$"LessOrEqual or GreaterOrEqual scenario.");}if(d==null||Η==null){ѧ.Add(
ѥ);}}List<KeyValuePair<string,bool>>Ҝ=new List<KeyValuePair<string,bool>>();ѡ="ActionSet";foreach(Ơ Η in ј.Values){ѥ=Η.s;
string Ő=$"{ϊ}.{ω}.ActionSet.{ѥ}";string қ=$"{ѡ} {ѥ}";string т,Ń;Ơ Ȇ=null;Ȩ Қ=null;Ǻ ҙ=null;int í=σ.Get(Ő,$"DelayOn").ToInt32(
);int ì=σ.Get(Ő,$"DelayOff").ToInt32();if(í!=0||ì!=0){ï ʞ=new ï(this);ʞ.í=í;ʞ.ì=ì;Η.Ș(ʞ);}ě=σ.Get(Ő,$"IGCChannel");if(!ě.
IsEmpty){string å=ě.ToString();é Ң=new é(IGC,å);ě=σ.Get(Ő,$"IGCMessageOn");if(!ě.IsEmpty){Ң.è=ě.ToString();}ě=σ.Get(Ő,
$"IGCMessageOff");if(!ě.IsEmpty){Ң.ç=ě.ToString();}if(Ң.U()){Η.Ș(Ң);}else{Ě.Ɵ($"{қ} has configuration for sending an IGC message "+
$"on the channel '{å}', but does not have readable config on what "+$"messages should be sent.");}}т="ActionSetsLinkedToOn";ě=σ.Get(Ő,т);if(!ě.IsEmpty){Ń=$"{қ}'s {т} list";Ņ(ě.ToString(),
Ń,Ѫ,Ҝ);foreach(KeyValuePair<string,bool>Ĩ in Ҝ){if(ј.TryGetValue(Ĩ.Key,out Ȇ)){â ҧ=new â(Ȇ);ҧ.ã(Ĩ.Value);Η.Ș(ҧ);}else{Ě.Ɵ
($"{Ń} references the unconfigured ActionSet {Ĩ.Key}.");}}}т="ActionSetsLinkedToOff";ě=σ.Get(Ő,т);if(!ě.IsEmpty){Ń=
$"{қ}'s {т} list";Ņ(ě.ToString(),Ń,Ѫ,Ҝ);foreach(KeyValuePair<string,bool>Ĩ in Ҝ){if(ј.TryGetValue(Ĩ.Key,out Ȇ)){â ҧ=new â(Ȇ);ҧ.Ü(Ĩ.Value)
;Η.Ș(ҧ);}else{Ě.Ɵ($"{Ń} references the unconfigured ActionSet {Ĩ.Key}.");}}}т="TriggersLinkedToOn";ě=σ.Get(Ő,т);if(!ě.
IsEmpty){Ń=$"{қ}'s {т} list";Ņ(ě.ToString(),Ń,Ѫ,Ҝ);foreach(KeyValuePair<string,bool>Ĩ in Ҝ){if(ї.TryGetValue(Ĩ.Key,out Қ)){Ö Ҧ=
new Ö(Қ);Ҧ.ã(Ĩ.Value);Η.Ș(Ҧ);}else{Ě.Ɵ($"{Ń} references the unconfigured ActionSet {Ĩ.Key}.");}}}т="TriggersLinkedToOff";ě=
σ.Get(Ő,т);if(!ě.IsEmpty){Ń=$"{қ}'s {т} list";Ņ(ě.ToString(),Ń,Ѫ,Ҝ);foreach(KeyValuePair<string,bool>Ĩ in Ҝ){if(ї.
TryGetValue(Ĩ.Key,out Қ)){Ö Ҧ=new Ö(Қ);Ҧ.Ü(Ĩ.Value);Η.Ș(Ҧ);}else{Ě.Ɵ($"{Ń} references the unconfigured ActionSet {Ĩ.Key}.");}}}т=
"RaycastPerformedOnState";ě=σ.Get(Ő,т);if(!ě.IsEmpty){Ń=$"{қ}'s {т} list";Ņ(ě.ToString(),Ń,Ѫ,Ҝ);foreach(KeyValuePair<string,bool>Ĩ in Ҝ){if(і.
TryGetValue(Ĩ.Key,out ҙ)){ā ҥ=new ā(ҙ);if(Ĩ.Value){ҥ.ÿ=true;}else{ҥ.ð=true;}Η.Ș(ҥ);}else{Ě.Ɵ(
$"{Ń} references the unconfigured Raycaster {Ĩ.Key}.");}}}}foreach(string Ҥ in ѧ){ї.Remove(Ҥ);}foreach(string ң in ѭ){ї.Remove(ң);}if(Ě.Ɲ()==0&&љ.Count==0&&ј.Count==0){Ě.Ɵ(
$"No readable configuration found on the programmable block.");}}int Ҩ(Ƃ Ě,ч Ŏ,Dictionary<string,v>љ,Dictionary<string,Ơ>ј,Dictionary<string,Ȩ>ї,Dictionary<string,Ǻ>і,Dictionary<
IMyInventory,List<N>>ѿ,Dictionary<string,ɫ>Ҁ,List<ɧ>Ѿ,List<ƻ>ѽ,Dictionary<string,ǋ>Ѽ,MyIniParseResult ϒ,MyIniValue ě){List<
IMyTerminalBlock>ϼ=new List<IMyTerminalBlock>();Dictionary<string,Action<IMyTerminalBlock>>õ=Н();List<KeyValuePair<string,bool>>Ł=new
List<KeyValuePair<string,bool>>();Action<string>Ҍ=Ŵ=>Ě.ƙ(Ŵ);v d;Ơ Ͳ;Color ė=Color.White;string[]Э;string ҋ="";string Ő="";
string Ҋ="";string Ń="";int Ù=0;bool ҍ;ϫ<IMyTerminalBlock>(ϼ,Ŵ=>(Ŵ.IsSameConstructAs(Me)&&MyIni.HasSection(Ŵ.CustomData,χ)));
if(ϼ.Count<=0){Ě.Ɵ($"No blocks found on this construct with a {χ} INI section.");}foreach(IMyTerminalBlock P in ϼ){Action<
string>Ō=ŋ=>{Ě.ƙ($"Block {P}, section {Ő}{ŋ}");};if(!σ.TryParse(P.CustomData,out ϒ)){Ě.ƙ(
$"Configuration on block '{P.CustomName}' has been "+$"ignored because of the following parsing error on line {ϒ.LineNo}: "+$"{ϒ.Error}");}else{ҍ=false;if(σ.ContainsKey(χ,
"Tallies")){ҍ=true;ě=σ.Get(χ,"Tallies");Э=ě.ToString().Split(',').Select(Ļ=>Ļ.Trim()).ToArray();foreach(string E in Э){if(!љ.
ContainsKey(E)){Ě.ƙ($"Block '{P.CustomName}' tried to reference the "+$"unconfigured Tally '{E}'.");}else{d=љ[E];if(d is N){if(!P.
HasInventory){Ě.ƙ($"Block '{P.CustomName}' does not have an "+$"inventory and is not compatible with the Type of "+$"Tally '{E}'.");
}else{for(int ņ=0;ņ<P.InventoryCount;ņ++){IMyInventory J=P.GetInventory(ņ);if(!ѿ.ContainsKey(J)){ѿ.Add(J,new List<N>());}
ѿ[J].Add((N)d);}}}else if(d is Ã){if(!((Ã)d).Q(P)){Ě.ƙ($"Block '{P.CustomName}' is not a "+$"{((Ã)d).O()} and is not "+
$"compatible with the Type of Tally '{E}'.");}}else{Ě.ƙ($"Block '{P.CustomName}' refrenced the Tally "+$"'{E}', which has an unhandled Tally Type. Complain to "+
$"the script writer, this should be impossible.");}}}}if(P.HasInventory){for(int ņ=0;ņ<P.InventoryCount;ņ++){if(!σ.ContainsKey(χ,$"Inv{ņ}Tallies")){}else{ҍ=true;ě=σ.Get
(χ,$"Inv{ņ}Tallies");Э=ě.ToString().Split(',').Select(Ļ=>Ļ.Trim()).ToArray();foreach(string E in Э){if(!љ.ContainsKey(E))
{Ě.ƙ($"Block '{P.CustomName}' tried to reference the "+$"unconfigured Tally '{E}' in key Inv{ņ}Tallies.");}else{d=љ[E];if
(!(d is N)){Ě.ƙ($"Block '{P.CustomName}' is not compatible "+$"with the Type of Tally '{E}' referenced in key "+
$"Inv{ņ}Tallies.");}else{IMyInventory J=P.GetInventory(ņ);if(!ѿ.ContainsKey(J)){ѿ.Add(J,new List<N>());}ѿ[J].Add((N)d);}}}}}}if(σ.
ContainsKey(χ,"ActionSets")){ҍ=true;ě=σ.Get(χ,"ActionSets");Э=ě.ToString().Split(',').Select(Ļ=>Ļ.Trim()).ToArray();foreach(string
E in Э){if(!ј.ContainsKey(E)){Ě.ƙ($"Block '{P.CustomName}' tried to reference the "+$"unconfigured ActionSet '{E}'.");}
else{Ͳ=ј[E];Ő=$"{ϊ}.{E}";if(!σ.ContainsSection(Ő)){Ě.ƙ($"Block '{P.CustomName}' references the ActionSet "+
$"'{E}', but contains no discrete '{Ő}' section that would "+$"define actions.");}else{X Ғ=null;if(σ.ContainsKey(Ő,"Action0Property")){ö Ҙ=new ö(P);ò Ŋ=null;Ù=0;while(Ù!=-1){Ŋ=ő(Ě,
Ő,Ù,P,σ,ě,Ŏ);if(Ŋ!=null){Ҙ.ô(Ŋ);Ù++;}else{Ù=-1;}}Ғ=Ҙ;}else if(σ.ContainsKey(Ő,"ActionsOn")||σ.ContainsKey(Ő,"ActionsOff")
){û Җ=new û(P);Җ.ú=ĺ(σ,Ő,"ActionsOn",õ,Ě,P);Җ.ù=ĺ(σ,Ő,"ActionsOff",õ,Ě,P);Ғ=Җ;}else if(σ.ContainsKey(Ő,"ActionOn")||σ.
ContainsKey(Ő,"ActionOff")){S ҕ=new S(P);ě=σ.Get(Ő,"ActionOn");if(!ě.IsEmpty){ҕ.þ=ŏ(ě.ToString(),õ,Ě,P,Ő,"ActionOn");}ě=σ.Get(Ő,
"ActionOff");if(!ě.IsEmpty){ҕ.ü=ŏ(ě.ToString(),õ,Ě,P,Ő,"ActionOff");}Ғ=ҕ;}if(Ғ.U()){Ͳ.Ș(Ғ);}else{Ě.ƙ(
$"Block '{P.CustomName}', discrete section '{Ő}', "+$"does not define any actions to be taken when the ActionSet changes state.");}}}}}if(P is IMyCameraBlock){if(σ.
ContainsKey(χ,"Raycasters")){ҍ=true;ě=σ.Get(χ,"Raycasters");Э=ě.ToString().Split(',').Select(Ļ=>Ļ.Trim()).ToArray();foreach(string
E in Э){if(!і.ContainsKey(E)){Ě.ƙ($"Camera '{P.CustomName}' tried to reference the "+$"unconfigured Raycaster '{E}'.");}
else{і[E].Ȏ((IMyCameraBlock)P);}}}}if(P is IMyTextSurfaceProvider){IMyTextSurfaceProvider Ҕ=(IMyTextSurfaceProvider)P;for(
int ņ=0;ņ<Ҕ.SurfaceCount;ņ++){Ҋ=$"Surface{ņ}Pages";if(σ.ContainsKey(χ,Ҋ)){IMyTextSurface ƺ=Ҕ.GetSurface(ņ);ɫ Ζ=null;ɧ ɥ=
null;ҍ=true;ě=σ.Get(χ,Ҋ);Э=ě.ToString().Split(',').Select(Ļ=>Ļ.Trim()).ToArray();if(Э.Length>1){string җ=$"Surface{ņ}MFD";if
(!σ.ContainsKey(χ,җ)){Ě.ƙ($"Surface provider '{P.CustomName}', key {Ҋ} "+
$"references multiple pages which must be managed by an MFD, "+$"but has no {җ} key to define that object's name.");}else{string ғ=σ.Get(χ,җ).ToString();if(Ҁ.ContainsKey(ғ)){Ě.ƙ(
$"Surface provider '{P.CustomName}', key {җ} "+$"declares the MFD '{ғ}' but this name is already in use.");}else{Ζ=new ɫ(ғ);}}}foreach(string E in Э){Ő=$"{ϊ}.{E}";if(
!σ.ContainsSection(Ő)){Ě.ƙ($"Surface provider '{P.CustomName}', key {Ҋ} declares the "+
$"page '{E}', but contains no discrete '{Ő}' section that would "+$"configure that page.");}else{ɥ=null;if(σ.ContainsKey(Ő,"Elements")){ě=σ.Get(Ő,"Elements");ȼ Ǹ=null;List<z>ґ=new List<
z>();string[]ť=ě.ToString().Split(',').Select(Ļ=>Ļ.Trim()).ToArray();foreach(string Š in ť){if(Š.ToLowerInvariant()==
"blank"){ґ.Add(null);}else{if(љ.ContainsKey(Š)){ґ.Add(љ[Š]);}else if(ј.ContainsKey(Š)){ґ.Add(ј[Š]);}else if(ї.ContainsKey(Š)){ґ
.Add(ї[Š]);}else{Ě.ƙ($"Surface provider '{P.CustomName}', "+$"section {Ő} tried to reference the "+
$"unconfigured element '{Š}'.");}}}Ǹ=new ȼ(ƺ,ґ);ě=σ.Get(Ő,"Title");if(!ě.IsEmpty){Ǹ.ǖ=ě.ToString();}ě=σ.Get(Ő,"FontSize");if(!ě.IsEmpty){Ǹ.Ƹ=ě.
ToSingle();}ě=σ.Get(Ő,"Font");if(!ě.IsEmpty){Ǹ.ƹ=ě.ToString();}Func<string,float>Ґ=(ҏ)=>{return(float)(σ.Get(Ő,$"Padding{ҏ}").
ToDouble(0));};float ȶ=Ґ("Left");float ȵ=Ґ("Right");float ȴ=Ґ("Top");float ȳ=Ґ("Bottom");Func<string,float,string,float,bool>Ѣ=(
ѓ,П,Ь,Ы)=>{if(П+Ы>100){Ě.ƙ($"Surface provider '{P.CustomName}', "+$"section {Ő} has padding values in excess "+
$"of 100% for edges {ѓ} and {Ь} "+$"which have been ignored.");return true;}return false;};if(Ѣ("Left",ȶ,"Right",ȵ)){ȶ=0;ȵ=0;}if(Ѣ("Top",ȴ,"Bottom",ȳ)){ȴ
=0;ȳ=0;}int ȷ=σ.Get(Ő,"Columns").ToInt32(1);bool ȹ=σ.Get(Ő,"TitleObeysPadding").ToBoolean(false);Ǹ.ȸ(ȷ,ȶ,ȵ,ȴ,ȳ,ȹ,Ɓ);ɥ=Ǹ;}
else if(σ.ContainsKey(Ő,"Script")){ɑ Ϡ=new ɑ(ƺ,σ.Get(Ő,"Script").ToString());ɥ=Ϡ;}else if(σ.ContainsKey(Ő,"DataType")){
string Ъ=σ.Get(Ő,"DataType").ToString().ToLowerInvariant();ɂ ƽ=null;if(Ъ=="log"){ƽ=new ƾ(τ);}else if(Ъ=="storage"){ƽ=new ǁ(
this);}else if(Ъ=="customdata"||Ъ=="detailinfo"||Ъ=="custominfo"){if(!σ.ContainsKey(Ő,"DataSource")){Ě.ƙ(
$"Surface provider '{P.CustomName}', section "+$"{Ő} has a DataType of {Ъ}, but a missing or "+$"unreadable DataSource.");}else{string Ϧ=σ.Get(Ő,"DataSource").
ToString();IMyTerminalBlock Ò=GridTerminalSystem.GetBlockWithName(Ϧ);if(Ò!=null&&Ъ=="customdata"){ƽ=new ɀ(Ò);}else if(Ò!=null&&Ъ
=="detailinfo"){ƽ=new ǀ(Ò);}else if(Ò!=null&&Ъ=="custominfo"){ƽ=new ƿ(Ò);}else{Ě.ƙ(
$"Surface provider '{P.CustomName}', section "+$"{Ő} tried to reference the unknown block '{Ϧ}' "+$"as a DataSource.");}}}else if(Ъ=="raycaster"){if(!σ.ContainsKey(Ő,
"DataSource")){Ě.ƙ($"Surface provider '{P.CustomName}', section "+$"{Ő} has a DataType of {Ъ}, but a missing or "+
$"unreadable DataSource.");}else{string Ϧ=σ.Get(Ő,"DataSource").ToString();if(і.ContainsKey(Ϧ)){ƽ=new Ƽ(і[Ϧ]);}else{Ě.ƙ(
$"Surface provider '{P.CustomName}', section "+$"{Ő} tried to reference the unknown Raycaster "+$"'{Ϧ}' as a DataSource.");}}}else{Ě.ƙ(
$"Surface provider '{P.CustomName}', section "+$"{Ő} tried to reference the unknown data type '{Ъ}'.");}if(ƽ!=null){ƻ Щ=new ƻ(ƺ,ƽ,Ɓ);ě=σ.Get(Ő,"FontSize");if(!ě.
IsEmpty){Щ.Ƹ=ě.ToSingle();}ě=σ.Get(Ő,"Font");if(!ě.IsEmpty){Щ.ƹ=ě.ToString();}ě=σ.Get(Ő,"CharPerLine");if(!ě.IsEmpty){if(Ъ==
"detailinfo"||Ъ=="custominfo"){Ě.ƙ($"Surface provider '{P.CustomName}', section "+
$"{Ő} tried to set a CharPerLine limit with the {Ъ} "+$"DataType. This is not allowed.");}else{Щ.ǒ(ě.ToInt32());}}if(Ъ=="log"){ѽ.Add(Щ);}ɥ=Щ;}}if(ɥ!=null){if(Ŏ.ф(Ō,σ,Ő,
"ForeColor",out ė)){((ɮ)ɥ).ɭ=ė;}if(Ŏ.ф(Ō,σ,Ő,"BackColor",out ė)){((ɮ)ɥ).ɬ=ė;}}}if(Ζ!=null&&ɥ!=null){ě=σ.Get(Ő,"ShowOnActionState");
if(!ě.IsEmpty){Ń=$"Surface provider '{P.CustomName}', section {Ő}";Ņ(ě.ToString(),Ń,Ҍ,Ł);foreach(KeyValuePair<string,bool>
Ĩ in Ł){if(!ј.TryGetValue(Ĩ.Key,out Ͳ)){Ě.ƙ($"{Ń} tried to reference the unconfigured ActionSet {Ĩ.Key}.");}else{ą Ш=new
ą(Ζ);if(Ĩ.Value){Ш.ă=E;}else{Ш.Ă=E;}Ͳ.Ș(Ш);}}}Ζ.ɦ(E,ɥ);}}if(Ζ!=null){if(Ζ.ɤ()==0){Ě.ƙ(
$"Surface provider '{P.CustomName}' specified "+$"the use of MFD '{Ζ.s}' but did not provide readable "+$"page configuration for that MFD.");}else{Ҁ.Add(Ζ.s,Ζ);ɥ=Ζ;Ζ.ɡ
(ς.Get("MFDs",Ζ.s).ToString());}}if(ɥ!=null){Ѿ.Add(ɥ);}}}}if(P is IMyLightingBlock){ě=σ.Get(χ,"IndicatorElement");if(!ě.
IsEmpty){ҋ=ě.ToString();z Š=null;if(љ.ContainsKey(ҋ)){Š=љ[ҋ];}else if(ј.ContainsKey(ҋ)){Š=ј[ҋ];}else if(ї.ContainsKey(ҋ)){Š=ї[ҋ
];}else{Ě.ƙ($"Lighting block '{P.CustomName}' tried to reference "+$"the unconfigured element '{ҋ}'.");}if(Š!=null){if(!Ѽ
.ContainsKey(ҋ)){Ѽ.Add(ҋ,new ǋ(Š));}Ѽ[ҋ].ǈ((IMyLightingBlock)P);}}else if(!ҍ){Ě.ƙ(
$"Lighting block {P.CustomName} has missing or unreadable "+$"IndicatorElement.");}ҍ=true;}if(!ҍ){Ě.ƙ($"Block '{P.CustomName}' is missing proper configuration or is a "+
$"block type that cannot be handled by this script.");}}}return ϼ.Count;}abstract class Ч{public string Ц{get;private set;}protected int Х{get;private set;}protected
MyGridProgram î{get;private set;}protected IEnumerator<string>Ф;public int У{get;private set;}public int Т{get;private set;}public
string С{get;protected set;}public bool Р{get;private set;}public Ч(MyGridProgram î,string ϴ,double з,bool е){this.î=î;Ц=ϴ;Х=(
int)(î.Runtime.MaxInstructionCount*з);У=0;Т=0;С=$"{Ц} waiting to begin";Р=е;}internal abstract void с();internal bool р(){
return Ф.MoveNext();}protected bool п(){if(î.Runtime.CurrentInstructionCount>Х){о();return true;}else{return false;}}protected
void о(){У++;Т+=î.Runtime.CurrentInstructionCount;}internal void н(){Ф.Dispose();С=$"{Ц} completed.";}internal abstract
string м();protected string л(){return$"{Ц} used a total of {Т} / {î.Runtime.MaxInstructionCount} "+
$"({(int)(((double)Т/î.Runtime.MaxInstructionCount)*100)}%) "+$"of instructions allowed in one tic, distributed over {У} tics.";}}class к:Ч{const int й=20;const double и=4;ɧ[]ж;
public к(MyGridProgram î,ɧ[]ж,bool е):base(î,"Sprite Refresher",.1,е){this.ж=ж;}internal override void с(){Ф=д();С=
$"{Ц} started";}IEnumerator<string>д(){int г=Math.Min((int)(Math.Ceiling(ж.Length/и)),й);int ĭ=0;int в=г;foreach(ɧ Ǹ in ж){Ǹ.ɯ();Ǹ.Ǉ()
;ĭ++;if(ĭ>=в){в+=г;С=$"{Ц} report {ĭ}/{ж.Length}";о();yield return С;}}}internal override string м(){return
$"{Ц} finished. Re-sync'd sprites on {ж.Length} surfaces.\n"+$"{л()}";}}string б(){List<string>а=new List<string>();string Я=$"{ϊ}.{ω}";σ.GetSections(а);foreach(string ː in а){if(ː
.Contains(Я)){σ.DeleteSection(ː);}}return σ.ToString();}string Ю(List<string>ť,string ʀ,int З,StringBuilder Ɓ){string ĩ=
"";int О=0;Ɓ.Clear();if(ť.Count>0){Ɓ.Append($"{ʀ} = ");if(ť.Count>З){О=З;}foreach(string Š in ť){if(О>=З){Ɓ.Append("\n|");
О=0;}Ɓ.Append($"{Š}, ");О++;}ĩ=Ɓ.ToString();ĩ=ĩ.Remove(ĩ.Length-2);}return ĩ;}Dictionary<string,Action<IMyTerminalBlock>>
Н(){Dictionary<string,Action<IMyTerminalBlock>>õ=new Dictionary<string,Action<IMyTerminalBlock>>(StringComparer.
OrdinalIgnoreCase);string М;string Л="Enable";string К="Positive";string Й="Negative";õ.Add($"{Л}On",Ŵ=>((IMyFunctionalBlock)Ŵ).Enabled=
true);õ.Add($"{Л}Off",Ŵ=>((IMyFunctionalBlock)Ŵ).Enabled=false);М="Battery";Л="charge";õ.Add($"{М}Auto",Ŵ=>((IMyBatteryBlock
)Ŵ).ChargeMode=ChargeMode.Auto);õ.Add($"{М}Re{Л}",Ŵ=>((IMyBatteryBlock)Ŵ).ChargeMode=ChargeMode.Recharge);õ.Add(
$"{М}Dis{Л}",Ŵ=>((IMyBatteryBlock)Ŵ).ChargeMode=ChargeMode.Discharge);М="Connector";õ.Add($"{М}Lock",Ŵ=>((IMyShipConnector)Ŵ).
Connect());õ.Add($"{М}Unlock",Ŵ=>((IMyShipConnector)Ŵ).Disconnect());М="Door";õ.Add($"{М}Open",Ŵ=>((IMyDoor)Ŵ).OpenDoor());õ.
Add($"{М}Close",Ŵ=>((IMyDoor)Ŵ).CloseDoor());М="Tank";Л="Stockpile";õ.Add($"{М}{Л}On",Ŵ=>((IMyGasTank)Ŵ).Stockpile=true);õ.
Add($"{М}{Л}Off",Ŵ=>((IMyGasTank)Ŵ).Stockpile=false);М="Gyro";string И="Stabilize";Л="Override";õ.Add($"{М}{Л}On",Ŵ=>((
IMyGyro)Ŵ).GyroOverride=true);õ.Add($"{М}{Л}Off",Ŵ=>((IMyGyro)Ŵ).GyroOverride=false);õ.Add($"{М}Yaw{К}",Ŵ=>((IMyGyro)Ŵ).Yaw=
9000);õ.Add($"{М}Yaw{И}",Ŵ=>((IMyGyro)Ŵ).Yaw=0);õ.Add($"{М}Yaw{Й}",Ŵ=>((IMyGyro)Ŵ).Yaw=-9000);Л="Pitch";õ.Add($"{М}{Л}{К}",Ŵ
=>((IMyGyro)Ŵ).Pitch=-9000);õ.Add($"{М}{Л}{И}",Ŵ=>((IMyGyro)Ŵ).Pitch=0);õ.Add($"{М}{Л}{Й}",Ŵ=>((IMyGyro)Ŵ).Pitch=9000);Л=
"Roll";õ.Add($"{М}{Л}{К}",Ŵ=>((IMyGyro)Ŵ).Roll=9000);õ.Add($"{М}{Л}{И}",Ŵ=>((IMyGyro)Ŵ).Roll=0);õ.Add($"{М}{Л}{Й}",Ŵ=>((
IMyGyro)Ŵ).Roll=-9000);М="Gear";Л="AutoLock";õ.Add($"{М}{Л}On",Ŵ=>((IMyLandingGear)Ŵ).AutoLock=true);õ.Add($"{М}{Л}Off",Ŵ=>((
IMyLandingGear)Ŵ).AutoLock=false);õ.Add($"{М}Lock",Ŵ=>((IMyLandingGear)Ŵ).Lock());õ.Add($"{М}Unlock",Ŵ=>((IMyLandingGear)Ŵ).Unlock());
М="JumpDrive";Л="Recharge";õ.Add($"{М}{Л}On",Ŵ=>((IMyJumpDrive)Ŵ).Recharge=true);õ.Add($"{М}{Л}Off",Ŵ=>((IMyJumpDrive)Ŵ).
Recharge=false);М="Parachute";õ.Add($"{М}Open",Ŵ=>((IMyParachute)Ŵ).OpenDoor());õ.Add($"{М}Close",Ŵ=>((IMyParachute)Ŵ).CloseDoor
());Л="AutoDeploy";õ.Add($"{М}{Л}On",Ŵ=>((IMyParachute)Ŵ).AutoDeploy=true);õ.Add($"{М}{Л}Off",Ŵ=>((IMyParachute)Ŵ).
AutoDeploy=false);М="Piston";õ.Add($"{М}Extend",Ŵ=>((IMyPistonBase)Ŵ).Extend());õ.Add($"{М}Retract",Ŵ=>((IMyPistonBase)Ŵ).Retract(
));М="Rotor";õ.Add($"{М}Lock",Ŵ=>((IMyMotorStator)Ŵ).RotorLock=true);õ.Add($"{М}Unlock",Ŵ=>((IMyMotorStator)Ŵ).RotorLock=
false);õ.Add($"{М}Reverse",Ŵ=>((IMyMotorStator)Ŵ).TargetVelocityRPM=((IMyMotorStator)Ŵ).TargetVelocityRPM*-1);õ.Add($"{М}{К}"
,Ŵ=>((IMyMotorStator)Ŵ).TargetVelocityRPM=Math.Abs(((IMyMotorStator)Ŵ).TargetVelocityRPM));õ.Add($"{М}{Й}",Ŵ=>((
IMyMotorStator)Ŵ).TargetVelocityRPM=Math.Abs(((IMyMotorStator)Ŵ).TargetVelocityRPM)*-1);М="Sorter";Л="Drain";õ.Add($"{М}{Л}On",Ŵ=>((
IMyConveyorSorter)Ŵ).DrainAll=true);õ.Add($"{М}{Л}Off",Ŵ=>((IMyConveyorSorter)Ŵ).DrainAll=false);М="Sound";õ.Add($"{М}Play",Ŵ=>((
IMySoundBlock)Ŵ).Play());õ.Add($"{М}Stop",Ŵ=>((IMySoundBlock)Ŵ).Stop());М="Thruster";Л="Override";õ.Add($"{М}{Л}Max",Ŵ=>((IMyThrust)Ŵ
).ThrustOverridePercentage=1);õ.Add($"{М}{Л}Off",Ŵ=>((IMyThrust)Ŵ).ThrustOverridePercentage=0);М="Timer";õ.Add(
$"{М}Trigger",Ŵ=>((IMyTimerBlock)Ŵ).Trigger());õ.Add($"{М}Start",Ŵ=>((IMyTimerBlock)Ŵ).StartCountdown());õ.Add($"{М}Stop",Ŵ=>((
IMyTimerBlock)Ŵ).StopCountdown());М="Turret";string Ǡ="Controller";string ŀ="Target";Л="Meteors";õ.Add($"{М}{ŀ}{Л}On",Ŵ=>((
IMyLargeTurretBase)Ŵ).TargetMeteors=true);õ.Add($"{М}{ŀ}{Л}Off",Ŵ=>((IMyLargeTurretBase)Ŵ).TargetMeteors=false);õ.Add($"{Ǡ}{ŀ}{Л}On",Ŵ=>((
IMyTurretControlBlock)Ŵ).TargetMeteors=true);õ.Add($"{Ǡ}{ŀ}{Л}Off",Ŵ=>((IMyTurretControlBlock)Ŵ).TargetMeteors=false);Л="Missiles";õ.Add(
$"{М}{ŀ}{Л}On",Ŵ=>((IMyLargeTurretBase)Ŵ).TargetMissiles=true);õ.Add($"{М}{ŀ}{Л}Off",Ŵ=>((IMyLargeTurretBase)Ŵ).TargetMissiles=false);
õ.Add($"{Ǡ}{ŀ}{Л}On",Ŵ=>((IMyTurretControlBlock)Ŵ).TargetMissiles=true);õ.Add($"{Ǡ}{ŀ}{Л}Off",Ŵ=>((IMyTurretControlBlock)
Ŵ).TargetMissiles=false);Л="SmallGrids";õ.Add($"{М}{ŀ}{Л}On",Ŵ=>((IMyLargeTurretBase)Ŵ).TargetSmallGrids=true);õ.Add(
$"{М}{ŀ}{Л}Off",Ŵ=>((IMyLargeTurretBase)Ŵ).TargetSmallGrids=false);õ.Add($"{Ǡ}{ŀ}{Л}On",Ŵ=>((IMyTurretControlBlock)Ŵ).TargetSmallGrids=
true);õ.Add($"{Ǡ}{ŀ}{Л}Off",Ŵ=>((IMyTurretControlBlock)Ŵ).TargetSmallGrids=false);Л="LargeGrids";õ.Add($"{М}{ŀ}{Л}On",Ŵ=>((
IMyLargeTurretBase)Ŵ).TargetLargeGrids=true);õ.Add($"{М}{ŀ}{Л}Off",Ŵ=>((IMyLargeTurretBase)Ŵ).TargetLargeGrids=false);õ.Add($"{Ǡ}{ŀ}{Л}On"
,Ŵ=>((IMyTurretControlBlock)Ŵ).TargetLargeGrids=true);õ.Add($"{Ǡ}{ŀ}{Л}Off",Ŵ=>((IMyTurretControlBlock)Ŵ).
TargetLargeGrids=false);Л="Characters";õ.Add($"{М}{ŀ}{Л}On",Ŵ=>((IMyLargeTurretBase)Ŵ).TargetCharacters=true);õ.Add($"{М}{ŀ}{Л}Off",Ŵ=>(
(IMyLargeTurretBase)Ŵ).TargetCharacters=false);õ.Add($"{Ǡ}{ŀ}{Л}On",Ŵ=>((IMyTurretControlBlock)Ŵ).TargetCharacters=true);
õ.Add($"{Ǡ}{ŀ}{Л}Off",Ŵ=>((IMyTurretControlBlock)Ŵ).TargetCharacters=false);Л="Stations";õ.Add($"{М}{ŀ}{Л}On",Ŵ=>((
IMyLargeTurretBase)Ŵ).TargetStations=true);õ.Add($"{М}{ŀ}{Л}Off",Ŵ=>((IMyLargeTurretBase)Ŵ).TargetStations=false);õ.Add($"{Ǡ}{ŀ}{Л}On",Ŵ=>
((IMyTurretControlBlock)Ŵ).TargetStations=true);õ.Add($"{Ǡ}{ŀ}{Л}Off",Ŵ=>((IMyTurretControlBlock)Ŵ).TargetStations=false)
;Л="Neutrals";õ.Add($"{М}{ŀ}{Л}On",Ŵ=>((IMyLargeTurretBase)Ŵ).TargetNeutrals=true);õ.Add($"{М}{ŀ}{Л}Off",Ŵ=>((
IMyLargeTurretBase)Ŵ).TargetNeutrals=false);õ.Add($"{Ǡ}{ŀ}{Л}On",Ŵ=>((IMyTurretControlBlock)Ŵ).TargetNeutrals=true);õ.Add($"{Ǡ}{ŀ}{Л}Off",
Ŵ=>((IMyTurretControlBlock)Ŵ).TargetNeutrals=false);Л="Enemies";õ.Add($"{М}{ŀ}{Л}On",Ŵ=>((IMyLargeTurretBase)Ŵ).
TargetEnemies=true);õ.Add($"{М}{ŀ}{Л}Off",Ŵ=>((IMyLargeTurretBase)Ŵ).TargetEnemies=false);õ.Add($"{Ǡ}{ŀ}{Л}On",Ŵ=>Ŵ.SetValue(
"TargetEnemies",true));õ.Add($"{Ǡ}{ŀ}{Л}Off",Ŵ=>Ŵ.SetValue("TargetEnemies",false));string ђ="Subsystem";Л="Default";õ.Add($"{М}{ђ}{Л}",
Ŵ=>((IMyLargeTurretBase)Ŵ).SetTargetingGroup(""));õ.Add($"{Ǡ}{ђ}{Л}",Ŵ=>((IMyTurretControlBlock)Ŵ).SetTargetingGroup(""))
;Л="Weapons";õ.Add($"{М}{ђ}{Л}",Ŵ=>((IMyLargeTurretBase)Ŵ).SetTargetingGroup(Л));õ.Add($"{Ǡ}{ђ}{Л}",Ŵ=>((
IMyTurretControlBlock)Ŵ).SetTargetingGroup(Л));Л="Propulsion";õ.Add($"{М}{ђ}{Л}",Ŵ=>((IMyLargeTurretBase)Ŵ).SetTargetingGroup(Л));õ.Add(
$"{Ǡ}{ђ}{Л}",Ŵ=>((IMyTurretControlBlock)Ŵ).SetTargetingGroup(Л));Л="PowerSystems";õ.Add($"{М}{ђ}{Л}",Ŵ=>((IMyLargeTurretBase)Ŵ).
SetTargetingGroup(Л));õ.Add($"{Ǡ}{ђ}{Л}",Ŵ=>((IMyTurretControlBlock)Ŵ).SetTargetingGroup(Л));М="Vent";Л="pressurize";õ.Add($"{М}{Л}",Ŵ=>(
(IMyAirVent)Ŵ).Depressurize=false);õ.Add($"{М}De{Л}",Ŵ=>((IMyAirVent)Ŵ).Depressurize=true);М="Warhead";õ.Add($"{М}Arm",Ŵ
=>((IMyWarhead)Ŵ).IsArmed=true);õ.Add($"{М}Disarm",Ŵ=>((IMyWarhead)Ŵ).IsArmed=false);Л="Countdown";õ.Add($"{М}{Л}Start",Ŵ
=>((IMyWarhead)Ŵ).StartCountdown());õ.Add($"{М}{Л}Stop",Ŵ=>((IMyWarhead)Ŵ).StopCountdown());õ.Add($"{М}Detonate",Ŵ=>((
IMyWarhead)Ŵ).Detonate());õ.Add("WeaponFireOnce",Ŵ=>((IMyUserControllableGun)Ŵ).ShootOnce());М="Suspension";Л="Height";õ.Add(
$"{М}{Л}{К}",Ŵ=>((IMyMotorSuspension)Ŵ).Height=9000);õ.Add($"{М}{Л}{Й}",Ŵ=>((IMyMotorSuspension)Ŵ).Height=-9000);õ.Add($"{М}{Л}Zero"
,Ŵ=>((IMyMotorSuspension)Ŵ).Height=0);Л="Propulsion";õ.Add($"{М}{Л}{К}",Ŵ=>((IMyMotorSuspension)Ŵ).PropulsionOverride=1);
õ.Add($"{М}{Л}{Й}",Ŵ=>((IMyMotorSuspension)Ŵ).PropulsionOverride=-1);õ.Add($"{М}{Л}Zero",Ŵ=>((IMyMotorSuspension)Ŵ).
PropulsionOverride=0);return õ;}class ч{Dictionary<string,ś>Ŏ;public ч(StringComparer ɶ=null){if(ɶ!=null){Ŏ=new Dictionary<string,ś>(ɶ);}
else{Ŏ=new Dictionary<string,ś>();}ц("Cozy",255,225,200);ц("Black",0,0,0);Color Ŭ=ц("Green",25,225,100);Color ū=ц(
"LightBlue",100,200,225);Color č=ц("Yellow",255,255,0);Color Â=ц("Orange",255,150,0);Color ä=ц("Red",255,0,0);Ŏ.Add("LowGood",new ű
(Ŭ,ū,č,Â,ä));Ŏ.Add("HighGood",new ŭ(Ŭ,ū,č,Â,ä));}private Color ц(string E,int ƌ,int Ƌ,int Ŵ){Color х=new Color(ƌ,Ƌ,Ŵ);Ŏ.
Add(E,new À(х,E));return х;}public bool ф(Action<string>ł,MyIni œ,string у,string т,out Color ė){ś ш;bool ь=ё(ł,œ,у,т,out ш
);if(ь){ė=ш.Ś(-1);}else{ė=Color.White;}return ь;}public bool ё(Action<string>ł,MyIni œ,string у,string т,out ś ш){
MyIniValue ě=œ.Get(у,т);ш=null;if(!ě.IsEmpty){string ѐ=ě.ToString();if(Ŏ.TryGetValue(ѐ,out ш)){return true;}else{string[]ť=ѐ.Split
(',').Select(Ļ=>Ļ.Trim()).ToArray();if(ť.Length==3){int[]я=new int[3];bool ю=false;for(int ņ=0;ņ<=2;ņ++){if(!Int32.
TryParse(ť[ņ],out я[ņ])){ю=true;ł($", key {т}, element {ņ} could not be parsed"+" as an integer.");}}if(ю){return false;}else{ш=
new À(new Color(я[0],я[1],я[2]));Ŏ.Add(ѐ,ш);return true;}}else{ł($", key {т} does not match a pre-defined color and "+
$"does not have three elements like a custom color.");return false;}}}else{return false;}}public ś э(string E){return Ŏ[E];}}bool ы(string Б,string т,string ъ,bool Ι,ref
bool γ,bool δ,string М,Ƃ Ě){MyIniValue ě;ě=σ.Get(Б,т);if(ě.IsEmpty){σ.Set(Б,т,ъ);if(Ι){σ.SetComment(Б,т,
"-----------------------------------------");}γ=true;if(δ){Ě.ƕ($"'{т}' key was missing from '{Б}' section of "+$"block '{М}' and has been re-generated.");}return
false;}return true;}bool щ(HashSet<string>Э,string E,string Ğ,Ƃ Ě){if(E.ToLowerInvariant()=="blank"){Ě.Ɵ(
$"{Ğ} tried to use the Element name '{E}', "+"which is reserved by the script to indicate portions of a Report that should "+
"be left empty. Please choose a different name.");return true;}else if(Э.Contains(E)){Ě.Ɵ($"{Ğ} tried to use the Element name '{E}', "+
$"which has already been claimed. All Element providers (Tally, ActionSet, "+$"Trigger, Raycaster) must have their own, unique names.");return true;}else{return false;}}void Ņ(string ń,string Ń,
Action<string>ł,List<KeyValuePair<string,bool>>Ł){string ŀ="";bool Ŀ=false;bool ľ;Ł.Clear();string[]Ľ=ń.Split(',').Select(Ļ=>Ļ
.Trim()).ToArray();foreach(string Ĩ in Ľ){ľ=false;string[]ļ=Ĩ.Split(':').Select(Ļ=>Ļ.Trim()).ToArray();ŀ=ļ[0];if(ļ.Length
<2){ľ=true;ł($"{Ń} does not provide a state for the component "+$"'{ŀ}'. Valid states are 'on' and 'off'.");}else if(ļ[1]
.ToLowerInvariant()=="on"){Ŀ=true;}else if(ļ[1].ToLowerInvariant()=="off"){Ŀ=false;}else{ľ=true;ł(
$"{Ń} attempts to set '{ŀ}' to the invalid state "+$"'{ļ[1]}'. Valid states are 'on' and 'off'.");}if(!ľ){Ł.Add(new KeyValuePair<string,bool>(ŀ,Ŀ));}}}List<Action<
IMyTerminalBlock>>ĺ(MyIni Ĺ,string ĸ,string ķ,Dictionary<string,Action<IMyTerminalBlock>>õ,Ƃ Ě,IMyTerminalBlock P){MyIniValue ě=Ĺ.Get(ĸ,
ķ);List<Action<IMyTerminalBlock>>Ķ=null;if(!ě.IsEmpty){string[]ĵ=null;Ķ=new List<Action<IMyTerminalBlock>>();ĵ=ě.ToString
().Split(',').Select(Ļ=>Ļ.Trim()).ToArray();foreach(string Ň in ĵ){Ķ.Add(ŏ(Ň,õ,Ě,P,ĸ,ķ));}}return Ķ;}Action<
IMyTerminalBlock>ŏ(string Ň,Dictionary<string,Action<IMyTerminalBlock>>õ,Ƃ Ě,IMyTerminalBlock P,string ĸ,string ķ){Action<
IMyTerminalBlock>Œ=null;if(õ.ContainsKey(Ň)){Œ=õ[Ň];}else{Ě.ƙ($"Block '{P.CustomName}', discrete section '{ĸ}', "+
$"references the unknown action '{Ň}' as its {ķ}.");}return Œ;}ò ő(Ƃ Ě,string Ő,int ĭ,IMyTerminalBlock P,MyIni œ,MyIniValue ě,ч Ŏ){string ō=$"Action{ĭ}Property";Action<
string>Ō=ŋ=>{Ě.ƙ($"Block {P.CustomName}, section {Ő}{ŋ}");};ě=œ.Get(Ő,ō);ò Ŋ=null;if(!ě.IsEmpty){string ŉ=ě.ToString(
"<missing>");ITerminalProperty ň=P.GetProperty(ŉ);if(ň==null){Ě.ƙ($"Block '{P.CustomName}', section '{Ő}', "+
$"references the unknown property '{ŉ}' as its {ō}.");Ŋ=new ò<bool>(ŉ);}else if(ň.TypeName.ToLowerInvariant()=="boolean"){ò<bool>Ĵ=new ò<bool>(ŉ);bool Ď=false;ě=œ.Get(Ő,
$"Action{ĭ}ValueOn");if(!ě.IsEmpty&&ě.TryGetBoolean(out Ď)){Ĵ.ć(Ď);}ě=œ.Get(Ő,$"Action{ĭ}ValueOff");if(!ě.IsEmpty&&ě.TryGetBoolean(out Ď)){
Ĵ.Č(Ď);}Ŋ=Ĵ;}else if(ň.TypeName.ToLowerInvariant()=="stringbuilder"){goto PretendThereWasNoPart;}else if(ň.TypeName.
ToLowerInvariant()=="int64"){ò<long>Ĵ=new ò<long>(ŉ);long Ď=-1;ě=œ.Get(Ő,$"Action{ĭ}ValueOn");if(!ě.IsEmpty&&ě.TryGetInt64(out Ď)){Ĵ.ć(Ď
);}ě=œ.Get(Ő,$"Action{ĭ}ValueOff");if(!ě.IsEmpty&&ě.TryGetInt64(out Ď)){Ĵ.Č(Ď);}Ŋ=Ĵ;}else if(ň.TypeName.ToLowerInvariant(
)=="single"){ò<float>Ĵ=new ò<float>(ŉ);float Ď=-1;ě=œ.Get(Ő,$"Action{ĭ}ValueOn");if(!ě.IsEmpty&&ě.TryGetSingle(out Ď)){Ĵ.
ć(Ď);}ě=œ.Get(Ő,$"Action{ĭ}ValueOff");if(!ě.IsEmpty&&ě.TryGetSingle(out Ď)){Ĵ.Č(Ď);}Ŋ=Ĵ;}else if(ň.TypeName.
ToLowerInvariant()=="color"){ò<Color>Ĵ=new ò<Color>(ŉ);Color Ď;if(Ŏ.ф(Ō,œ,Ő,$"Action{ĭ}ValueOn",out Ď)){Ĵ.ć(Ď);}if(Ŏ.ф(Ō,œ,Ő,
$"Action{ĭ}ValueOff",out Ď)){Ĵ.Č(Ď);}Ŋ=Ĵ;}else{Ě.Ɵ($"Block '{P.CustomName}', discrete section '{Ő}', "+
$"references the property '{ŉ}' which uses the non-standard "+$"type {ň.TypeName}. Report this to the scripter, as the script "+$"will need to be altered to handle this.");Ŋ=new ò<
bool>(ŉ);}if(!Ŋ.ñ()&&ň!=null){Ě.ƙ($"Block '{P.CustomName}', discrete section '{Ő}', "+
$"does not specify a working Action{ĭ}ValueOn or Action{ĭ}ValueOff for "+$"the property '{ŉ}'. If one was specified, make sure that it matches the "+$"type '{ň.TypeName}.'");}
PretendThereWasNoPart:return Ŋ;}else{return Ŋ;}}void Ġ(Ȩ ğ,string Ğ,bool ĝ,string Ĝ,MyIniValue ě,Ƃ Ě){double Ć;ě=σ.Get(Ğ,$"{Ĝ}Value");if(!ě.
IsEmpty){Ć=ě.ToDouble();ě=σ.Get(Ğ,$"{Ĝ}Command");if(!ě.IsEmpty){string ę=ě.ToString().ToLowerInvariant();if(ę=="on"){ğ.ȃ(ĝ,Ć,
true);}else if(ę=="off"){ğ.ȃ(ĝ,Ć,false);}else if(ę=="switch"){Ě.Ɵ($"{Ğ}: {ğ.s} specifies "+
$"a {Ĝ}Command of 'switch', which cannot be used for triggers.");}else{Ě.Ɵ($"{Ğ}: {ğ.s} has a missing "+$"or invalid {Ĝ}Command. Valid commands are 'on' and 'off'.");}}else{Ě.Ɵ(
$"{Ğ}: {ğ.s} specifies a "+$"{Ĝ}Value but no {Ĝ}Command.");}}}static string Ę(Color ė){if(ė==ϗ){return"cozy";}else if(ė==Ϝ){return"green";}else if
(ė==ϛ){return"lightBlue";}else if(ė==Ϛ){return"yellow";}else if(ė==ϙ){return"orange";}else if(ė==Ϙ){return"red";}else{
return$"{ė.R}, {ė.G}, {ė.B}";}}class ġ{Dictionary<string,ħ>Ė;Dictionary<string,Ģ>Ĕ;internal ġ(){Ė=new Dictionary<string,ħ>();Ĕ
=new Dictionary<string,Ģ>();}internal bool ē(string E,ħ đ){if(Ė.ContainsKey(E)){return false;}else{Ė.Add(E,đ);return true
;}}internal void Ē(string E,ħ đ){if(Ė.ContainsKey(E)){Ė[E]=đ;}else{Ė.Add(E,đ);}}internal int Đ(string E){ħ ď;if(Ė.
TryGetValue(E,out ď)){return ď.ĥ;}else{return-1;}}internal void ĕ(string E){Ė.Remove(E);}internal void Ī(){Ė.Clear();}internal bool
ĳ(string E,int Ĳ,out string ı){Ģ İ;if(!Ĕ.TryGetValue(E,out İ)){Ģ į=new Ģ(Ĳ,E);Ĕ.Add(E,į);ı="";return true;}else{ı=İ.ƈ();
return false;}}internal void ģ(){foreach(ħ đ in Ė.Values){đ.ģ();}Ģ Į;int ĭ=0;while(ĭ<Ĕ.Count){Į=Ĕ.Values.ElementAt(ĭ);if(Į.Ɖ()
){Ĕ.Remove(Į.E);}else{ĭ++;}}}public string Ĭ(string E){Ģ Į;if(Ĕ.TryGetValue(E,out Į)){return Į.ƈ();}else{return
$"{E} is not on cooldown.";}}internal string ī(){string ĩ="Contained periodics:\n";foreach(KeyValuePair<string,ħ>Ĩ in Ė){ĩ+=
$" -{Ĩ.Key} with frequency {Ĩ.Value.ĥ}\n";}ĩ+="Contained cooldowns:\n";foreach(KeyValuePair<string,Ģ>Ĩ in Ĕ){ĩ+=
$" -{Ĩ.Key} with a remaining duration of {Ĩ.Value.Ħ}\n";}return ĩ;}}class ħ{internal int ĥ{get;private set;}int Ħ;Action Ĥ;internal ħ(int ĥ,Action Ĥ){this.ĥ=ĥ;Ħ=ĥ;this.Ĥ=Ĥ;}
internal void ģ(){Ħ--;if(Ħ<=0){Ħ=ĥ;Ĥ.Invoke();}}}class Ģ{public int Ħ{get;private set;}internal string E{get;private set;}
internal Ģ(int Ĳ,string E){Ħ=Ĳ;this.E=E;}internal bool Ɖ(){Ħ--;if(Ħ<=0){return true;}else{return false;}}internal string ƈ(){
return$"{E} is on cooldown for the next {(int)(Ħ*1.4)} seconds.";}}void Ƈ(int Ɔ=0,ġ ƅ=null){Action Ƅ=()=>{Ç();Ǉ();τ.ģ();};ħ ƃ=
new ħ(Ɔ,Ƅ);ε.Ē("UpdateDelay",ƃ);τ.ǝ=Ɔ;}class Ƃ{StringBuilder Ɓ;int Ɗ;List<string>ƀ,ſ,ž;int Ž,ż,Ż;public string ź{get;
private set;}public string Ź{get;private set;}public string Ÿ{get;private set;}public Color ŷ{set{ź=ų(value);}}public Color Ŷ{
set{Ź=ų(value);}}public Color ŵ{set{Ÿ=ų(value);}}public Ƃ(StringBuilder Ɓ,int Ɗ){this.Ɓ=Ɓ;this.Ɗ=Ɗ;Ž=0;ż=0;Ż=0;ƀ=new List<
string>();ſ=new List<string>();ž=new List<string>();ź=Ǝ(255,255,0,0);Ź=Ǝ(255,255,255,0);Ÿ=Ǝ(255,100,200,225);}public void Ɵ(
string ƞ){if(ƀ.Count<Ɗ){ƀ.Add(ƞ);}else{Ž++;}}public int Ɲ(){return ƀ.Count+Ž;}public void Ɯ(){Ž=0;ƀ.Clear();}public string ƛ()
{string ƚ;Ɓ.Clear();foreach(string ŧ in ƀ){Ɓ.Append($" -{ŧ}\n");}if(Ž>0){Ɓ.Append($" -And {Ž} other errors.\n");}ƚ=Ɓ.
ToString();Ɓ.Clear();return ƚ;}public void ƙ(string Â){if(ſ.Count<Ɗ){ſ.Add(Â);}else{ż++;}}public int Ƙ(){return ſ.Count+ż;}
public void Ɨ(){ż=0;ſ.Clear();}public string Ɩ(){string ƚ;Ɓ.Clear();foreach(string ŧ in ſ){Ɓ.Append($" -{ŧ}\n");}if(ż>0){Ɓ.
Append($" -And {ż} other warnings.\n");}ƚ=Ɓ.ToString();Ɓ.Clear();return ƚ;}public void ƕ(string Ɣ){if(ž.Count<Ɗ){ž.Add(Ɣ);}
else{Ż++;}}public int Ɠ(){return ž.Count+Ż;}public void ƒ(){Ż=0;ž.Clear();}public string Ƒ(){string Ɛ;Ɓ.Clear();foreach(
string ŧ in ž){Ɓ.Append($" -{ŧ}\n");}if(Ż>0){Ɓ.Append($" -And {Ż} other notes.\n");}Ɛ=Ɓ.ToString();Ɓ.Clear();return Ɛ;}public
void Ə(){Ɯ();Ɨ();ƒ();}}static string Ǝ(int ƍ,int ƌ,int Ƌ,int Ŵ){return$"{ƍ:X2}{ƌ:X2}{Ƌ:X2}{Ŵ:X2}";}static string ų(Color ė){
return Ǝ(ė.A,ė.R,ė.G,ė.B);}static string Ũ(string ŧ){if(ŧ.Contains("\n")){ŧ=$"\n|{ŧ.Replace("\n","\n|")}";}return ŧ;}static
string Ŧ(List<string>ť,int Ť=3,bool ţ=true){int Ţ=0;string ĩ="";string š=ţ?"|":"";if(ť.Count>Ť&&ţ){ĩ="\n|";}foreach(string Š
in ť){if(Ţ>=Ť){ĩ+=$"\n{š}";Ţ=0;}ĩ+=$"{Š}, ";Ţ++;}ĩ=ĩ.Remove(ĩ.Length-2);return ĩ;}interface ş{string ũ();}interface Ş{
string Ŝ();}interface ś:Ş{Color Ś(double µ);}abstract class ř:ś{protected string E;public Color Ř{internal get;set;}public
Color ŗ{internal get;set;}public Color Ŗ{internal get;set;}public Color ŕ{internal get;set;}public Color Ŕ{internal get;set;}
internal int ŝ;internal int Ū;internal int Ů;internal int Ų;public ř(Color Ŭ,Color ū,Color č,Color Â,Color ä){Ř=Ŭ;ŗ=ū;Ŗ=č;ŕ=Â;Ŕ=
ä;}public ř(){}internal bool Ű(string ů,Color ė){switch(ů){case"Optimal":Ř=ė;break;case"Normal":ŗ=ė;break;case"Caution":Ŗ
=ė;break;case"Warning":ŕ=ė;break;case"Critical":Ŕ=ė;break;default:return false;}return true;}public string Ŝ(){return E;}
public abstract Color Ś(double µ);}class ű:ř{public ű(Color Ŭ,Color ū,Color č,Color Â,Color ä):base(Ŭ,ū,č,Â,ä){E="LowGood";ŝ=0
;Ū=55;Ů=70;Ų=85;}public ű():base(){E="LowGood";}public override Color Ś(double µ){Color Á=ŗ;if(µ<=ŝ){Á=Ř;}else if(µ>Ų){Á=
Ŕ;}else if(µ>Ů){Á=ŕ;}else if(µ>Ū){Á=Ŗ;}return Á;}}class ŭ:ř{public ŭ(Color Ŭ,Color ū,Color č,Color Â,Color ä):base(Ŭ,ū,č,
Â,ä){E="HighGood";ŝ=100;Ū=45;Ů=30;Ų=15;}public ŭ():base(){E="HighGood";}public override Color Ś(double µ){Color Á=ŗ;if(µ
>=ŝ){Á=Ř;}else if(µ<Ų){Á=Ŕ;}else if(µ<Ů){Á=ŕ;}else if(µ<Ū){Á=Ŗ;}return Á;}}class À:ś{string E;public Color º{private get;
set;}public À(){}public À(Color º,string E){this.E=E;this.º=º;}public À(Color º){this.º=º;E=$"{º.R}, {º.G}, {º.B}";}public
Color Ś(double µ){return º;}public string Ŝ(){return E;}}interface z{string y();string x();Color w{get;}}abstract class v:z,ş
{public string u{get;set;}internal string s{get;private set;}public double A{protected get;set;}public double q{get;
protected set;}public double R{get;protected set;}internal bool o;internal bool ª;public double µ{get;protected set;}protected
string É;protected string Ð;protected Ǫ F;internal string Î;public Color w{get;protected set;}internal ś B{get;set;}public v(Ǫ
F,string E,ś B,double A=1){this.F=F;s=E;u=E;this.B=B;this.A=A;q=0;R=0;o=false;ª=false;µ=0;É="curr";Ð="max";Î=
"[----------]";w=ϗ;}internal string Í(){return Î;}internal string Ì(){return É;}internal string Ë(){return Ð;}public string y(){return
$"{u}\n{É} / {Ð}\n{Î}";}public string x(){return$"{u,-12}{($"{É} / {Ð}"),-12}{Î}";}internal abstract void Ï();internal void Ê(){q=0;}internal
void È(double L){R=L*A;o=true;}internal abstract void Ç();internal string Æ(string Å){double Ä=1;string I=
$"[{ϊ}.{ω}.Tally.{Ũ(s)}]\n";if(u!=s){I+=$"DisplayName = {Ũ(u)}\n";}I+=Å;if(o){I+=$"Max = {R/A}\n";}if(A!=Ä){I+=$"Multiplier = {A}\n";}I+="\n";
return I;}public abstract string ũ();}class Ã:v{internal ǅ m;public Ã(Ǫ F,string E,ǅ m,ś B,double A=1):base(F,E,B,A){this.m=m;
}internal bool Q(IMyTerminalBlock P){return m.Q(P);}internal string O(){return m.ũ();}internal override void Ï(){if(!o){R
=m.Ǆ()*A;}c(ref Ð,R);}internal override void Ç(){if(R!=0){q=m.ǃ();q=q*A;µ=Math.Min(q/R,100)*100;w=B.Ś(µ);F.Í(ref Î,µ);c(
ref É,q);}}public override string ũ(){string I=$"Type = {m.ũ()}\n";if(!(B is ŭ)){I+=$"ColorCoder = {B.Ŝ()}\n";}I+=m.ǂ();
return Æ(I);}}class N:v{public N(Ǫ F,string E,ś B,double A=1):base(F,E,B,A){}internal void M(double L){if(!o){R+=L;}}internal
override void Ï(){if(!o){R=R*A;}c(ref Ð,R);}internal virtual void K(IMyInventory J){q+=(double)J.CurrentVolume;}internal
override void Ç(){if(R!=0){q=q*A;µ=Math.Min(q/R,100)*100;w=B.Ś(µ);F.Í(ref Î,µ);c(ref É,q);}}public override string ũ(){string I=
"Type = Inventory\n";if(!(B is ű)){I+=$"ColorCoder = {B.Ŝ()}\n";}return Æ(I);}}class H:N{internal MyItemType G{get;private set;}public H(Ǫ F
,string E,string D,string C,ś B,double R=0,double A=1):base(F,E,B,A){G=new MyItemType(D,C);È(R);}public H(Ǫ F,string E,
MyItemType G,ś B,double R=0,double A=1):base(F,E,B,A){this.G=G;È(R);}internal override void K(IMyInventory J){q+=(double)J.
GetItemAmount(G);}public override string ũ(){string I=$"Type = Item\n";I+=$"ItemTypeID = {G.TypeId}\n";I+=
$"ItemSubTypeID = {G.SubtypeId}\n";if(!(B is ŭ)){I+=$"ColorCoder = {B.Ŝ()}\n";}return Æ(I);}}class l{IMyInventory J;N[]k;public l(IMyInventory J,N[]k){
this.J=J;this.k=k;}public void j(){foreach(N d in k){{d.M((double)J.MaxVolume);}}}public void f(){foreach(N d in k){d.K(J);}
}}static void c(ref string Z,double Y){Z="";if(Y<10){Z+=(Math.Round(Y,1));}else if(Y<1000){Z+=(int)Y;}else if(Y<10000){Z=
Math.Round(Y/1000,1)+"K";}else if(Y<1000000){Z=(int)(Y/1000)+"K";}else if(Y<10000000){Z=Math.Round(Y/1000000,1)+"M";}else if
(Y<1000000000){Z=(int)(Y/1000000)+"M";}else if(Y<10000000000){Z=Math.Round(Y/1000000000,1)+"B";}else if(Y<1000000000000){
Z=(int)(Y/10000000000)+"B";}else if(Y<10000000000000){Z=Math.Round(Y/1000000000000,1)+"T";}else if(Y>10000000000000){Z=(
int)(Y/1000000000000)+"T";}}interface X{void W(bool V);bool U();string n();}class S:X{IMyTerminalBlock Ñ;internal Action<
IMyTerminalBlock>þ{get;set;}internal Action<IMyTerminalBlock>ü{get;set;}public S(IMyTerminalBlock Ò){Ñ=Ò;þ=null;ü=null;}public void W(
bool V){if(V){þ?.Invoke(Ñ);}else{ü?.Invoke(Ñ);}}public bool U(){return þ!=null||ü!=null;}public string n(){return
$"Block '{Ñ.CustomName}'";}}class û:X{IMyTerminalBlock Ñ;internal List<Action<IMyTerminalBlock>>ú{get;set;}internal List<Action<IMyTerminalBlock>
>ù{get;set;}public û(IMyTerminalBlock Ò){Ñ=Ò;ú=null;ù=null;}public void W(bool V){List<Action<IMyTerminalBlock>>ý;if(V){ý
=ú;}else{ý=ù;}if(ý!=null){foreach(Action<IMyTerminalBlock>Û in ý){Û.Invoke(Ñ);}}}public bool U(){return ú?.Count>0||ù?.
Count>0;}public string n(){return$"Block '{Ñ.CustomName}'";}}class ö:X{IMyTerminalBlock Ñ;private List<ò>õ;public ö(
IMyTerminalBlock Ò){this.Ñ=Ò;õ=new List<ò>();}public void ô(ò ó){õ.Add(ó);}public void W(bool V){foreach(ò ó in õ){ó.W(Ñ,V);}}public
bool U(){return õ.Count!=0;}public string n(){return$"Block '{Ñ.CustomName}'";}}abstract class ò{public abstract bool ñ();
public abstract Type ø();public abstract void W(IMyTerminalBlock P,bool V);}class ò<ċ>:ò{string Ĉ;private ċ Ċ,ĉ;private bool Ý
,Þ;public ò(string Ĉ){this.Ĉ=Ĉ;Ý=false;Þ=false;}public void ć(ċ Ć){Ċ=Ć;Ý=true;}public void Č(ċ Ć){ĉ=Ć;Þ=true;}public
override bool ñ(){return Ý||Þ;}public override Type ø(){return typeof(ċ);}public override void W(IMyTerminalBlock P,bool V){if(V
&&Ý){P.SetValue<ċ>(Ĉ,Ċ);}else if(!V&&Þ){P.SetValue<ċ>(Ĉ,ĉ);}}}class ą:X{ɫ Ą;internal string ă{private get;set;}internal
string Ă{private get;set;}public ą(ɫ Ò){Ą=Ò;ă="";Ă="";}public void W(bool V){if(V){Ą.ɡ(ă);}else{Ą.ɡ(Ă);}}public bool U(){
return!String.IsNullOrEmpty(ă)||!String.IsNullOrEmpty(Ă);}public string n(){return
"Some MFD (Sorry, MFDs are supposed to work)";}}class ā:X,Ş{Ǻ Ā;internal bool ÿ{private get;set;}internal bool ð{private get;set;}public ā(Ǻ Ò){Ā=Ò;ÿ=false;ð=false;}
public void W(bool V){if(V){if(ÿ){Ā.ȏ();}}else{if(ð){Ā.ȏ();}}}public bool U(){return ÿ||ð;}public string n(){return
$"Raycaster '{Ā.s}'";}public string Ŝ(){return$"{Ā.s}: {(ÿ?"on":"off")}";}}class â:X,Ş{public const bool á=true;public const bool à=false;
internal Ơ ß;internal bool Ô,Ó;internal bool Ý,Þ;public â(Ơ Ò){ß=Ò;Ô=à;Ó=à;Ý=false;Þ=false;}public void ã(bool Û){Ô=Û;Ý=true;}
public void Ü(bool Û){Ó=Û;Þ=true;}public void W(bool V){try{if(V){if(Ý){if(!ß.ȟ){ß.Ȗ(Ô);}else{Exception Ú=new
InvalidOperationException();Ú.Data.Add("Counter",0);throw Ú;}}}else{if(Þ){if(!ß.ȟ){ß.Ȗ(Ó);}else{Exception Ú=new InvalidOperationException();Ú.
Data.Add("Counter",0);throw Ú;}}}}catch(InvalidOperationException e){int Ù=(int)e.Data["Counter"];e.Data.Add(Ù,ß.s);e.Data[
"Counter"]=++Ù;ß.ȓ();throw;}}public bool U(){return Ý||Þ;}public string n(){return$"Controller for ActionSet {ß.s}";}public
string Ŝ(){if(Ø()){return$"{ß.s}: {(Ô?"on":"off")}";}else{return$"{ß.s}: {(Ó?"on":"off")}";}}public bool Ø(){return Ý;}}class
Ö:X,Ş{internal Ȩ Õ;internal bool Ô,Ó;internal bool Ý,Þ;public Ö(Ȩ Ò){this.Õ=Ò;Ô=false;Ó=false;Ý=false;Þ=false;}public
void ã(bool Û){Ô=Û;Ý=true;}public void Ü(bool Û){Ó=Û;Þ=true;}public void W(bool V){if(V){if(Ý){Õ.Ȃ(Ô);}}else{if(Þ){Õ.Ȃ(Ó);}}
}public bool U(){return Ý||Þ;}public string n(){return$"Controller for Trigger {Õ.s}";}public string Ŝ(){if(Ø()){return
$"{Õ.s}: {(Ô?"on":"off")}";}else{return$"{Õ.s}: {(Ó?"on":"off")}";}}public bool Ø(){return Ý;}}class ï:X,ş{Program î;public int í{get;internal set
;}public int ì{get;internal set;}public ï(Program î){this.î=î;í=0;ì=0;}public void W(bool V){if(V){î.Ƈ(í);}else{î.Ƈ(ì);}}
public bool U(){return í!=0||ì!=0;}public string n(){return"The Distributor";}public string ũ(){string I="";int ë=0;int ê=0;if
(í!=ë){I+=$"DelayOn = {í}\n";}if(ì!=ê){I+=$"DelayOff = {ì}\n";}return I;}}class é:X,ş{IMyIntergridCommunicationSystem æ;
internal string å{get;set;}internal string è{get;set;}internal string ç{get;set;}public é(IMyIntergridCommunicationSystem æ,
string å){this.æ=æ;this.å=å;è="";ç="";}public void W(bool V){if(V){æ.SendBroadcastMessage(å,è);}else{æ.SendBroadcastMessage(å,
ç);}}public bool U(){return!String.IsNullOrEmpty(è)||!String.IsNullOrEmpty(ç);}public string n(){return
$"IGC on channel '{å}'";}public string ũ(){string I="";if(å!=""){I+=$"IGCChannel = {å}\n";}if(è!=""){I+=$"IGCMessageOn = {è}\n";}if(ç!=""){I+=
$"IGCMessageOff = {ç}\n";}return I;}}class Ơ:z,ş{List<X>Ƞ;internal string u{get;set;}internal string s{get;private set;}internal bool Ø{get;
private set;}internal bool ȟ{get;private set;}internal Color Ȟ{private get;set;}internal Color ȝ{private get;set;}public Color
w{get;private set;}internal string Ȝ{private get;set;}internal string ț{private get;set;}public string Ț{get;private set;
}public Ơ(string E,bool Ȅ){Ƞ=new List<X>();u=E;s=E;Ø=Ȅ;ȟ=false;Ȟ=Ϝ;ȝ=Ϙ;Ȝ="Enabled";ț="Disabled";Ǽ();}internal void Ǽ(){if
(Ø){w=Ȟ;Ț=Ȝ;}else{w=ȝ;Ț=ț;}}public void Ș(X ȕ){Ƞ.Add(ȕ);}public void ȗ(){Ȗ(!Ø);}public void Ȗ(bool ȁ){Ø=ȁ;ȟ=true;Ǽ();
foreach(X ȕ in Ƞ){try{ȕ.W(ȁ);}catch(InvalidOperationException){throw;}catch(Exception e){if(!e.Data.Contains("Identifier")){e.
Data.Add("Identifier",ȕ.n());}throw;}}}public void Ȕ(){ȟ=false;}public void ȓ(){Ț="Fault";w=new Color(125,125,125);}public
string y(){return$"{u}\n{Ț}";}public string x(){return$"{u,-19} {Ț,18}";}public string ũ(){Color Ȓ=Ϝ;Color ȑ=Ϙ;string Ȑ=
"Enabled";string ș="Disabled";string I=$"[{ϊ}.{ω}.ActionSet.{Ũ(s)}]\n";if(u!=s){I+=$"DisplayName = {Ũ(u)}\n";}if(Ȟ!=Ȓ){I+=
$"ColorOn = {Ę(Ȟ)}\n";}if(ȝ!=ȑ){I+=$"ColorOff = {Ę(ȝ)}\n";}if(Ȝ!=Ȑ){I+=$"TextOn = {Ũ(Ȝ)}\n";}if(ț!=ș){I+=$"TextOff = {Ũ(ț)}\n";}int Ȥ=0;X Ȱ=
null;Ş Ȯ=null;List<string>ȭ=null;List<string>Ȭ=null;List<string>ȫ=null;List<string>Ȫ=null;List<string>ȩ=null;while(Ȥ!=-1){if
(Ȥ>=Ƞ.Count){Ȥ=-1;}else{Ȱ=Ƞ[Ȥ];if(Ȱ is ş){I+=$"{((ş)Ȱ).ũ()}";Ȥ++;}else if(Ȱ is Ş){Ȯ=(Ş)Ȱ;if(Ȯ is Ö){if(ȫ==null){ȫ=new
List<String>();Ȫ=new List<String>();}if(((Ö)Ȯ).Ø()){ȫ.Add(Ȯ.Ŝ());}else{Ȫ.Add(Ȯ.Ŝ());}}else if(Ȯ is â){if(ȭ==null){ȭ=new List
<String>();Ȭ=new List<String>();}if(((â)Ȯ).Ø()){ȭ.Add(Ȯ.Ŝ());}else{Ȭ.Add(Ȯ.Ŝ());}}else{if(ȩ==null){ȩ=new List<String>();}
ȩ.Add(((ā)Ȯ).Ŝ());}Ȥ++;}else{Ȥ=-1;}}}if(ȭ?.Count>0){I+=$"ActionSetsLinkedToOn = {Ŧ(ȭ)}\n";}if(Ȭ?.Count>0){I+=
$"ActionSetsLinkedToOff = {Ŧ(Ȭ)}\n";}if(ȫ?.Count>0){I+=$"TriggerLinkedToOn = {Ŧ(ȫ)}\n";}if(Ȫ?.Count>0){I+=$"TriggerLinkedToOff = {Ŧ(Ȫ)}\n";}if(ȩ?.Count>0){
I+=$"RaycastPerformedOnState = {Ŧ(ȩ)}\n";}I+="\n";return I;}}class Ȩ:z,ş{internal v Ƿ{private get;set;}internal Ơ Ȇ{
private get;set;}double ȧ,Ȧ;bool ȯ,ȥ;bool ȣ,Ȣ;internal bool ȡ{get;private set;}public string s{get;private set;}string Ȝ,ț;
public string Ț{get;private set;}Color Ȟ,ȝ;public Color w{get;private set;}public Ȩ(string s,bool Ȅ){Ƿ=null;Ȇ=null;ȅ(s,Ȅ);}
public Ȩ(string s,v Ƿ,Ơ Ȇ,bool Ȅ){this.Ƿ=Ƿ;this.Ȇ=Ȇ;ȅ(s,Ȅ);}private void ȅ(string s,bool Ȅ){this.s=s;ȧ=-1;Ȧ=-1;ȯ=false;ȥ=false
;ȣ=false;Ȣ=false;ȡ=Ȅ;Ȝ="Armed";ț="Disarmed";Ȟ=Ϛ;ȝ=Ϙ;Ǽ();}public void ȃ(bool ĝ,double Ć,bool ǽ){if(ĝ){Ȧ=Ć;ȥ=ǽ;Ȣ=true;}else
{ȧ=Ć;ȯ=ǽ;ȣ=true;}}public void Ȃ(bool ȁ){ȡ=ȁ;Ǽ();}public bool ȇ(out Ơ Ȁ,out bool ǿ){Ȁ=null;ǿ=false;if(ȡ){if(ȣ&&Ȇ.Ø!=ȯ&&Ƿ.µ
>=ȧ){Ȁ=Ȇ;ǿ=ȯ;return true;}else if(Ȣ&&Ȇ.Ø!=ȥ&&Ƿ.µ<=Ȧ){Ȁ=Ȇ;ǿ=ȥ;return true;}}return false;}private void Ǿ(bool ǽ){Ȇ.Ȗ(ǽ);}
private void Ǽ(){if(ȡ){w=Ȟ;Ț=Ȝ;}else{w=ȝ;Ț=ț;}}public bool ǻ(){return ȣ||Ȣ;}public string n(){return s;}public string y(){
return$"{s}\n{(ȡ?Ȝ:ț)}";}public string x(){return$"{s,-19} {(ȡ?Ȝ:ț),18}";}public string ũ(){string I=
$"[{ϊ}.{ω}.Trigger.{Ũ(s)}]\n";I+=$"Tally = {Ƿ.s}\n";I+=$"ActionSet = {Ȇ.s}\n";if(Ȣ){I+=$"LessOrEqualValue = {Ȧ}\n";I+=
$"LessOrEqualCommand = {(ȥ?"on":"off")}\n";}if(ȣ){I+=$"GreaterOrEqualValue = {ȧ}\n";I+=$"GreaterOrEqualCommand = {(ȯ?"on":"off")}\n";}return I;}}class Ǻ:ş{
StringBuilder Ɓ;internal ȉ ǹ{private get;set;}string Ǹ;internal bool ǚ{get;private set;}internal string s{get;private set;}public Ǻ(
StringBuilder Ɓ,string s){ȅ(Ɓ,s);}public Ǻ(StringBuilder Ɓ,ȉ ǹ,string s){this.ǹ=ǹ;ȅ(Ɓ,s);}private void ȅ(StringBuilder Ɓ,string s){
this.Ɓ=Ɓ;this.s=s;Ǹ=$"{DateTime.Now.ToString("HH:mm:ss")}- Raycaster {s} "+$"reports: No data";ǚ=false;}public void Ȏ(
IMyCameraBlock ư){ǹ.Ȏ(ư);ư.EnableRaycast=true;}public double ȍ(){return ǹ?.ȱ??-1;}public void ȏ(){MyDetectedEntityInfo ȋ;double Ȋ;
IMyCameraBlock ư=ǹ.ȏ(out ȋ,out Ȋ);Ȍ(ȋ,Ȋ,ư);ǚ=true;}private void Ȍ(MyDetectedEntityInfo ȋ,double Ȋ,IMyCameraBlock ư){Ɓ.Clear();if(ư==
null){Ɓ.Append($"{DateTime.Now.ToString("HH:mm:ss")}- Raycaster {s} "+$"reports: No cameras have the required {ȍ()} charge "
+$"needed to perform this scan.");}else if(ȋ.IsEmpty()){Ɓ.Append($"{DateTime.Now.ToString("HH:mm:ss")}- Raycaster {s} "+
$"reports: Camera '{ư.CustomName}' detected no entities on a "+$"{Ȋ} meter scan.");}else{Ɓ.Append($"{DateTime.Now.ToString("HH:mm:ss")}- Raycaster {s} "+
$"reports: Camera '{ư.CustomName}' detected entity '{ȋ.Name}' "+$"on a {Ȋ} meter scan.\n\n");Ɓ.Append($"Relationship: {ȋ.Relationship}\n");Ɓ.Append($"Type: {ȋ.Type}\n");Ɓ.Append(
$"Size: {ȋ.BoundingBox.Size.ToString("0.00")}\n");Vector3D ŀ=ȋ.HitPosition.Value;Ɓ.Append($"Distance: {Vector3D.Distance(ư.GetPosition(),ŀ).ToString("0.00")}\n");Ɓ.
Append($"GPS:Raycast - {ȋ.Name}:{ŀ.X}:{ŀ.Y}:{ŀ.Z}:\n");}Ǹ=Ɓ.ToString();Ɓ.Clear();}public void Ǧ(){ǚ=false;}public string ǫ(){
return Ǹ;}public string ũ(){string I=$"[{ϊ}.{ω}.Raycaster.{Ũ(s)}]\n";I+=ǹ.ũ();return I;}}abstract class ȉ{protected List<
IMyCameraBlock>Ȉ;public double ȱ{get;protected set;}public ȉ(){Ȉ=new List<IMyCameraBlock>();ȱ=0;}public void Ȏ(IMyCameraBlock ư){ư.
EnableRaycast=true;Ȉ.Add(ư);}public abstract void ɝ(double[]ɖ);public abstract IMyCameraBlock ȏ(out MyDetectedEntityInfo ȋ,out double
Ȋ);public abstract string ũ();}class ɜ:ȉ{private double ɛ;private double A;private double ɚ;public ɜ():base(){int[]ə=ɗ();
ɛ=ə[0];A=ə[1];ɚ=ə[2];}public static string[]ɘ(){return new string[]{"BaseRange","Multiplier","MaxRange"};}internal static
int[]ɗ(){return new int[]{1000,3,27000};}public override void ɝ(double[]ɖ){if(ɖ[0]!=-1)ɛ=ɖ[0];if(ɖ[1]!=-1)A=ɖ[1];if(ɖ[2]!=-
1)ɚ=ɖ[2];double ɕ=ɛ;ȱ=ɛ;while(ɕ<ɚ){ɕ*=A;ȱ+=Math.Min(ɚ,ɕ);}}public override IMyCameraBlock ȏ(out MyDetectedEntityInfo ȋ,
out double Ȋ){ȋ=new MyDetectedEntityInfo();Ȋ=-1;IMyCameraBlock ư=ɔ();if(ư==null||ư.AvailableScanRange<ȱ){return null;}else{
Ȋ=ɛ;while(ȋ.IsEmpty()&&Ȋ<ɚ){ȋ=ư.Raycast(Ȋ,0,0);Ȋ*=A;if(Ȋ>ɚ){Ȋ=ɚ;}}return ư;}}private IMyCameraBlock ɔ(){IMyCameraBlock ɓ=
null;foreach(IMyCameraBlock ư in Ȉ){if(ɓ==null||ư.AvailableScanRange>ɓ.AvailableScanRange){ɓ=ư;}}return ɓ;}public override
string ũ(){string[]ɒ=ɘ();int[]ə=ɗ();string I="Type = Linear\n";if(ɛ!=ə[0]){I+=$"{ɒ[0]} = {ɛ}\n";}if(A!=ə[1]){I+=
$"{ɒ[1]} = {A}\n";}if(ɚ!=ə[2]){I+=$"{ɒ[2]} = {ɚ}\n";}return I;}}interface ɧ{void ɲ();void Ǉ();void ɰ();void ɯ();}interface ɮ{Color ɭ{get;
set;}Color ɬ{get;set;}}class ɫ:ɧ{public string s{get;private set;}private Dictionary<string,ɧ>ɪ;internal int ɩ{get;private
set;}internal string ɨ{get;private set;}private ɧ ɱ;public ɫ(string s){this.s=s;ɪ=new Dictionary<string,ɧ>(StringComparer.
OrdinalIgnoreCase);ɩ=0;ɨ="";ɱ=null;}public void ɦ(string E,ɧ ɥ){ɪ.Add(E,ɥ);if(ɱ==null){ɱ=ɥ;ɨ=E;}}public int ɤ(){return ɪ.Count;}public
void ɣ(bool ɢ){if(ɢ){ɩ++;if(ɩ>=ɪ.Count){ɩ=0;}}else{ɩ--;if(ɩ<0){ɩ=ɪ.Count-1;}}ɨ=ɪ.Keys.ToArray()[ɩ];ɠ();}public bool ɡ(string
E){if(ɪ.ContainsKey(E)){ɨ=E;ɩ=ɪ.Keys.ToList().IndexOf(E);ɠ();return true;}else{return false;}}private void ɠ(){ɧ ɟ=ɪ[ɨ];
bool ɞ=false;if(ɱ is ɑ&&ɟ is ȼ){ɞ=true;}ɱ=ɟ;ɲ();Ǉ();if(ɞ){ɯ();}}public void ɲ(){ɱ.ɲ();}public void Ǉ(){ɱ.Ǉ();}public void ɰ(
){ɱ.ɰ();}public void ɯ(){ɱ.ɯ();}}class ɑ:ɧ,ɮ{IMyTextSurface ƺ;public Color ɭ{get;set;}public Color ɬ{get;set;}public
string Ȳ{get;set;}public ɑ(IMyTextSurface ƺ,string Ȳ){this.ƺ=ƺ;this.Ȳ=Ȳ;ɭ=ƺ.ScriptForegroundColor;ɬ=ƺ.ScriptBackgroundColor;}
public void Ǉ(){}public void ɰ(){}public void ɯ(){}public void ɲ(){ƺ.ContentType=ContentType.SCRIPT;ƺ.Script=Ȳ;ƺ.
ScriptForegroundColor=ɭ;ƺ.ScriptBackgroundColor=ɬ;}}class ȼ:ɧ,ɮ{IMyTextSurface ƺ;z[]ť;Vector2[]Ȼ;public float Ƹ{private get;set;}public
string ƹ{private get;set;}public Color ɭ{get;set;}public Color ɬ{get;set;}public string ǖ{get;set;}Vector2 Ⱥ;bool Ƚ;public ȼ(
IMyTextSurface ƺ,List<z>ť,string ǖ="",float Ƹ=1f,string ƹ="Debug"){this.ƺ=ƺ;this.ť=ť.ToArray();this.ǖ=ǖ;this.Ƹ=Ƹ;this.ƹ=ƹ;ɭ=ƺ.
ScriptForegroundColor;ɬ=ƺ.ScriptBackgroundColor;Ȼ=new Vector2[ť.Count];Ƚ=false;}public void ȸ(int ȷ,float ȶ,float ȵ,float ȴ,float ȳ,bool ȹ,
StringBuilder Ɓ){RectangleF Ʉ=new RectangleF((ƺ.TextureSize-ƺ.SurfaceSize)/2f,ƺ.SurfaceSize);float ɐ=(ȶ/100)*ƺ.SurfaceSize.X;float Ɏ=
(ȴ/100)*ƺ.SurfaceSize.Y;Ʉ.X+=ɐ;Ʉ.Width-=ɐ;Ʉ.Y+=Ɏ;Ʉ.Height-=Ɏ;Ʉ.Width-=(ȵ/100)*ƺ.SurfaceSize.X;Ʉ.Height-=(ȳ/100)*ƺ.
SurfaceSize.Y;Ɓ.Clear();float ɍ=0;if(!string.IsNullOrEmpty(ǖ)){Ɓ.Append(ǖ);ɍ=ƺ.MeasureStringInPixels(Ɓ,ƹ,Ƹ).Y;if(ȹ){Ⱥ=new Vector2(Ʉ
.Width/2+Ʉ.X,Ʉ.Y);}else{Ⱥ=new Vector2(ƺ.TextureSize.X/2,(ƺ.TextureSize.Y-ƺ.SurfaceSize.Y)/2);ɍ=Math.Max(ɍ-ȴ,0);}}int Ɍ=(
int)(Math.Ceiling((double)ť.Count()/ȷ));float ɋ=Ʉ.Width/ȷ;float Ɋ=(Ʉ.Height-ɍ)/Ɍ;int ɉ=1;Vector2 Ɉ,ɇ,Ɇ;Ɉ=new Vector2(ɋ/2,Ɋ/
2);Ɉ+=Ʉ.Position;Ɉ.Y+=ɍ;for(int ņ=0;ņ<ť.Count();ņ++){if(ť[ņ]!=null){Ɓ.Clear();Ɓ.Append(ť[ņ].y());Ɇ=ƺ.
MeasureStringInPixels(Ɓ,ƹ,Ƹ);ɇ=new Vector2(Ɉ.X,Ɉ.Y);ɇ.Y-=Ɇ.Y/2;Ȼ[ņ]=ɇ;}if(ɉ==ȷ){Ɉ.X=ɋ/2;Ɉ.X+=Ʉ.Position.X;Ɉ.Y+=Ɋ;ɉ=1;}else{Ɉ.X+=ɋ;ɉ++;}}Ɓ.
Clear();}public void Ǉ(){z Š;MySprite ɏ;using(MySpriteDrawFrame Ʌ=ƺ.DrawFrame()){if(Ƚ){Vector2 Ƀ=new Vector2(0,0);ɏ=MySprite.
CreateSprite("IconEnergy",Ƀ,Ƀ);Ʌ.Add(ɏ);}if(!string.IsNullOrEmpty(ǖ)){ɏ=MySprite.CreateText(ǖ,ƹ,ƺ.ScriptForegroundColor,Ƹ);ɏ.
Position=Ⱥ;Ʌ.Add(ɏ);}for(int ņ=0;ņ<ť.Count();ņ++){Š=ť[ņ];if(Š!=null){ɏ=MySprite.CreateText(Š.y(),ƹ,Š.w,Ƹ);ɏ.Position=Ȼ[ņ];Ʌ.Add(
ɏ);}}}}public void ɰ(){Ǉ();}public void ɯ(){Ƚ=!Ƚ;}public void ɲ(){ƺ.ContentType=ContentType.SCRIPT;ƺ.Script="";ƺ.
ScriptForegroundColor=ɭ;ƺ.ScriptBackgroundColor=ɬ;}}interface ɂ{string Ɂ();bool ǚ();}class ɀ:ɂ{IMyTerminalBlock P;bool ȿ;public ɀ(
IMyTerminalBlock P){this.P=P;ȿ=true;}public string Ɂ(){return P.CustomData;}public bool ǚ(){bool ı=ȿ;ȿ=false;return ı;}}abstract class Ⱦ
:ɂ{protected IMyTerminalBlock P;string Ƕ;public Ⱦ(IMyTerminalBlock P){this.P=P;Ƕ="";}public abstract string Ɂ();public
bool ǚ(){if(Ɂ()==Ƕ){return false;}else{Ƕ=Ɂ();return true;}}}class ǀ:Ⱦ{public ǀ(IMyTerminalBlock P):base(P){}public override
string Ɂ(){return P.DetailedInfo;}}class ƿ:Ⱦ{public ƿ(IMyTerminalBlock P):base(P){}public override string Ɂ(){return P.
CustomInfo;}}class ƾ:ɂ{ǟ Ɛ;public ƾ(ǟ Ɛ){this.Ɛ=Ɛ;}public string Ɂ(){return Ɛ.ǫ();}public bool ǚ(){return Ɛ.ǚ;}}class ǁ:ɂ{
MyGridProgram î;public ǁ(MyGridProgram î){this.î=î;}public string Ɂ(){return î.Storage;}public bool ǚ(){return false;}}class Ƽ:ɂ{Ǻ Ʊ;
public Ƽ(Ǻ Ʊ){this.Ʊ=Ʊ;}public string Ɂ(){return Ʊ.ǫ();}public bool ǚ(){return Ʊ.ǚ;}}class ƻ:ɧ,ɮ{IMyTextSurface ƺ;public Color
ɭ{get;set;}public Color ɬ{get;set;}public string ƹ{get;set;}public float Ƹ{get;set;}int Ʒ;ɂ ƽ;StringBuilder Ɓ;public ƻ(
IMyTextSurface ƺ,ɂ ƽ,StringBuilder Ɓ){this.ƺ=ƺ;this.ƽ=ƽ;this.Ɓ=Ɓ;ɭ=ƺ.FontColor;ɬ=ƺ.BackgroundColor;ƹ=ƺ.Font;Ƹ=ƺ.FontSize;Ʒ=0;}public
void ǒ(int Ǒ){if(Ǒ>=0){Ʒ=Ǒ;}}private string ǐ(string Ǐ){if(Ʒ>0){string[]ǎ=Ǐ.Split(' ');int Ǎ=0;Ɓ.Clear();foreach(string ǌ in
ǎ){Ɓ.Append($"{ǌ} ");if(ǌ.Contains('\n')){Ǎ=0;}else{Ǎ+=ǌ.Length+1;if(Ǎ>Ʒ){Ɓ.Append("\n");Ǎ=0;}}}Ǐ=Ɓ.ToString();}return Ǐ;
}public void Ǉ(){if(ƽ.ǚ()){ƺ.WriteText(ǐ(ƽ.Ɂ()));}}public void ɰ(){ƺ.WriteText(ǐ(ƽ.Ɂ()));}public void ɯ(){}public void ɲ(
){ƺ.ContentType=ContentType.TEXT_AND_IMAGE;ƺ.FontColor=ɭ;ƺ.BackgroundColor=ɬ;ƺ.Font=ƹ;ƺ.FontSize=Ƹ;ƺ.WriteText(ǐ(ƽ.Ɂ()));
}}class ǋ{List<IMyLightingBlock>Ǌ;z Š;Color ǉ;public ǋ(z Š){Ǌ=new List<IMyLightingBlock>();this.Š=Š;ǉ=ϗ;}public void ǈ(
IMyLightingBlock ǆ){Ǌ.Add(ǆ);}public void Ǉ(){if(Š.w!=ǉ){foreach(IMyLightingBlock ǆ in Ǌ){ǆ.Color=Š.w;}ǉ=Š.w;}}}interface ǅ{bool Q(
IMyTerminalBlock P);double Ǆ();double ǃ();string ǂ();string ũ();}class ƶ:ǅ{List<IMyBatteryBlock>ơ;public ƶ(){ơ=new List<IMyBatteryBlock>
();}public bool Q(IMyTerminalBlock P){IMyBatteryBlock Ƭ=P as IMyBatteryBlock;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);
return true;}}public double Ǆ(){double R=0;foreach(IMyBatteryBlock ƫ in ơ){R+=ƫ.MaxStoredPower;}return R;}public double ǃ(){
double q=0;foreach(IMyBatteryBlock ƫ in ơ){q+=ƫ.CurrentStoredPower;}return q;}public string ǂ(){return"";}public string ũ(){
return"Battery";}}class ƪ:ǅ{List<IMyGasTank>Ʃ;List<IMyTerminalBlock>ƨ;public ƪ(){Ʃ=new List<IMyGasTank>();ƨ=new List<
IMyTerminalBlock>();}public bool Q(IMyTerminalBlock P){IMyGasTank Ƨ=P as IMyGasTank;if(Ƨ!=null){Ʃ.Add(Ƨ);return true;}else{
IMyPowerProducer Ʀ=P as IMyPowerProducer;if(Ʀ!=null&&Ʀ.BlockDefinition.SubtypeId.EndsWith("HydrogenEngine")){ƨ.Add(Ʀ);return true;}else{
return false;}}}public double Ǆ(){double R=0;string[]ļ;string[]ƥ={"(","L/","L)"};foreach(IMyGasTank Ƥ in Ʃ){R+=Ƥ.Capacity;}
foreach(IMyTerminalBlock ƣ in ƨ){ļ=ƣ.DetailedInfo.Split(ƥ,System.StringSplitOptions.None);R+=Double.Parse(ļ[2]);}return R;}
public double ǃ(){double q=0;foreach(IMyGasTank Ƥ in Ʃ){q+=Ƥ.Capacity*Ƥ.FilledRatio;}foreach(IMyTerminalBlock ƣ in ƨ){q+=ƣ.
Components.Get<MyResourceSourceComponent>().RemainingCapacity;}return q;}public string ǂ(){return"";}public string ũ(){return"Gas"
;}}class Ƣ:ǅ{List<IMyJumpDrive>ơ;public Ƣ(){ơ=new List<IMyJumpDrive>();}public bool Q(IMyTerminalBlock P){IMyJumpDrive Ƭ=
P as IMyJumpDrive;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);return true;}}public double Ǆ(){double R=0;foreach(IMyJumpDrive
Ƶ in ơ){R+=Ƶ.MaxStoredPower;}return R;}public double ǃ(){double q=0;foreach(IMyJumpDrive Ƶ in ơ){q+=Ƶ.CurrentStoredPower;
}return q;}public string ǂ(){return"";}public string ũ(){return"JumpDrive";}}class ƴ:ǅ{List<IMyCameraBlock>ơ;Ǻ Ƴ;public ƴ
(){ơ=new List<IMyCameraBlock>();Ƴ=null;}public void Ʋ(Ǻ Ʊ){Ƴ=Ʊ;}public bool Q(IMyTerminalBlock P){IMyCameraBlock Ƭ=P as
IMyCameraBlock;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);return true;}}public double Ǆ(){return-1;}public double ǃ(){double q=0;foreach(
IMyCameraBlock ư in ơ){q+=ư.AvailableScanRange;}return q;}public string ǂ(){return Ƴ==null?"":$"Raycaster = {Ƴ.s}\n";}public string ũ(
){return"Raycast";}}class Ư:ǅ{List<IMyPowerProducer>ơ;public Ư(){ơ=new List<IMyPowerProducer>();}public bool Q(
IMyTerminalBlock P){IMyPowerProducer Ƭ=P as IMyPowerProducer;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);return true;}}public double Ǆ(){
double R=0;foreach(IMyPowerProducer Ʈ in ơ){R+=Ʈ.Components.Get<MyResourceSourceComponent>().DefinedOutput;}return R;}public
double ǃ(){double q=0;foreach(IMyPowerProducer Ʈ in ơ){q+=Ʈ.MaxOutput;}return q;}public string ǂ(){return"";}public string ũ()
{return"PowerMax";}}class ƭ:ǅ{List<IMyPowerProducer>ơ;public ƭ(){ơ=new List<IMyPowerProducer>();}public bool Q(
IMyTerminalBlock P){IMyPowerProducer Ƭ=P as IMyPowerProducer;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);return true;}}public double Ǆ(){
double R=0;foreach(IMyPowerProducer Ʈ in ơ){R+=Ʈ.Components.Get<MyResourceSourceComponent>().DefinedOutput;}return R;}public
double ǃ(){double q=0;foreach(IMyPowerProducer Ʈ in ơ){q+=Ʈ.CurrentOutput;}return q;}public string ǂ(){return"";}public string
ũ(){return"PowerCurrent";}}class ǰ:ǅ{List<IMySlimBlock>ơ;public ǰ(){ơ=new List<IMySlimBlock>();}public bool Q(
IMyTerminalBlock P){IMySlimBlock Ƭ=P.CubeGrid.GetCubeBlock(P.Min);ơ.Add(Ƭ);return true;}public double Ǆ(){double R=0;foreach(
IMySlimBlock P in ơ){R+=P.MaxIntegrity;}return R;}public double ǃ(){double q=0;foreach(IMySlimBlock P in ơ){q+=P.BuildIntegrity-P.
CurrentDamage;}return q;}public string ǂ(){return"";}public string ũ(){return"Integrity";}}class ǯ:ǅ{List<IMyAirVent>ơ;public ǯ(){ơ=
new List<IMyAirVent>();}public bool Q(IMyTerminalBlock P){IMyAirVent Ƭ=P as IMyAirVent;if(Ƭ==null){return false;}else{ơ.Add
(Ƭ);return true;}}public double Ǆ(){double R=0;foreach(IMyAirVent Ǯ in ơ){R+=1;}return R;}public double ǃ(){double q=0;
foreach(IMyAirVent Ǯ in ơ){q+=Ǯ.GetOxygenLevel();}return q;}public string ǂ(){return"";}public string ũ(){return"VentPressure";
}}class ǭ:ǅ{List<IMyPistonBase>ơ;public ǭ(){ơ=new List<IMyPistonBase>();}public bool Q(IMyTerminalBlock P){IMyPistonBase
Ƭ=P as IMyPistonBase;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);return true;}}public double Ǆ(){double R=0;foreach(
IMyPistonBase Ǵ in ơ){R+=Ǵ.HighestPosition;}return R;}public double ǃ(){double q=0;foreach(IMyPistonBase Ǵ in ơ){q+=Ǵ.CurrentPosition
;}return q;}public string ǂ(){return"";}public string ũ(){return"PistonExtension";}}class ǳ:ǅ{List<IMyMotorStator>ơ;
public ǳ(){ơ=new List<IMyMotorStator>();}public bool Q(IMyTerminalBlock P){IMyMotorStator Ƭ=P as IMyMotorStator;if(Ƭ==null){
return false;}else{ơ.Add(Ƭ);return true;}}public double Ǆ(){double R=0;foreach(IMyMotorStator ǲ in ơ){R+=360;}return R;}public
double ǃ(){double q=0;foreach(IMyMotorStator ǲ in ơ){q+=MathHelper.ToDegrees(ǲ.Angle);}return q;}public string ǂ(){return"";}
public string ũ(){return"RotorAngle";}}class ǵ:ǅ{List<IMyShipController>ơ;public ǵ(){ơ=new List<IMyShipController>();}public
bool Q(IMyTerminalBlock P){IMyShipController Ƭ=P as IMyShipController;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);return true;}}
public double Ǆ(){return 110;}public double ǃ(){double q=-1;foreach(IMyShipController Ǡ in ơ){if(Ǡ.IsFunctional){q=Ǡ.
GetShipSpeed();break;}}return q;}public string ǂ(){return"";}public string ũ(){return"ControllerSpeed";}}class Ǳ:ǅ{List<
IMyShipController>ơ;public Ǳ(){ơ=new List<IMyShipController>();}public bool Q(IMyTerminalBlock P){IMyShipController Ƭ=P as
IMyShipController;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);return true;}}public double Ǆ(){return 1;}public double ǃ(){double q=0;foreach(
IMyShipController Ǡ in ơ){if(Ǡ.IsFunctional){q=Ǡ.GetNaturalGravity().Length()/9.81;break;}}return q;}public string ǂ(){return"";}public
string ũ(){return"ControllerGravity";}}class Ǭ:ǅ{List<IMyShipController>ơ;public Ǭ(){ơ=new List<IMyShipController>();}public
bool Q(IMyTerminalBlock P){IMyShipController Ƭ=P as IMyShipController;if(Ƭ==null){return false;}else{ơ.Add(Ƭ);return true;}}
public double Ǆ(){return-1;}public double ǃ(){double q=-1;foreach(IMyShipController Ǡ in ơ){if(Ǡ.IsFunctional){q=Ǡ.
GetNaturalGravity().Length()*Ǡ.CalculateShipMass().PhysicalMass;break;}}return q;}public string ǂ(){return"";}public string ũ(){return
"ControllerWeight";}}class ǟ{List<string>Ɛ;string ǖ;public string Ǟ{private get;set;}public int ǝ{private get;set;}public string ǜ{private
get;set;}StringBuilder Ɓ;string ǡ;int ǔ;public bool ǚ{get;private set;}string[]Ǚ;int ǘ=-1;int Ǘ;public ǟ(StringBuilder Ɓ,
string ǖ,bool Ǖ=false,int ǔ=5){Ɛ=new List<string>();this.Ɓ=Ɓ;ǡ="";this.ǖ=ǖ;Ǟ="";ǜ="";this.ǔ=ǔ;ǚ=false;ǝ=0;if(Ǖ){Ǚ=new string[]
{"|----","-|---","--|--","---|-","----|"};ǘ=0;Ǘ=1;}}public void ģ(){ǘ+=Ǘ;if(ǘ==0||ǘ==4){Ǘ*=-1;}}public void Ǔ(string Ǜ){Ɛ
.Insert(0,$"{DateTime.Now.ToString("HH:mm:ss")}- {Ǜ}");if(Ɛ.Count>ǔ){Ɛ.RemoveAt(ǔ);}Ɓ.Clear();foreach(string ŧ in Ɛ){Ɓ.
Append($"\n{ŧ}\n");}ǡ=Ɓ.ToString();Ɓ.Clear();ǚ=true;}public void Ǧ(){ǚ=false;}public string ǫ(){Ɓ.Clear();Ɓ.Append(ǖ);if(ǘ!=-1
){Ɓ.Append($" {Ǚ[ǘ]}");}Ɓ.Append("\n");if(!String.IsNullOrEmpty(Ǟ)){Ɓ.Append($"Script Tag: {Ǟ}\n");}if(ǝ!=0){Ɓ.Append(
$"Current Update Delay: {ǝ}\n");}Ɓ.Append($"{ǜ}\n");Ɓ.Append(ǡ);return Ɓ.ToString();}}class Ǫ{StringBuilder Ɓ;int ǩ;string[]Ǩ;public Ǫ(StringBuilder Ɓ
,int Ǣ=10){this.Ɓ=Ɓ;ǩ=Ǣ;Ǩ=new string[ǩ+1];string ǧ="";for(int ņ=0;ņ<Ǩ.Length;ņ++){Ǥ(ref ǧ,ņ,ǩ);Ǩ[ņ]=ǧ;}}public void Í(ref
string Î,double µ){Î=Ǩ[ǥ(µ,ǩ)];}public void Í(ref string Î,double µ,int Ǣ){int ǣ=ǥ(µ,Ǣ);Ǥ(ref Î,ǣ,Ǣ);}private int ǥ(double µ,
int Ǣ){µ=Math.Min(µ,100);return(int)((µ/100)*Ǣ);}private void Ǥ(ref string Î,int ǣ,int Ǣ){Ɓ.Clear();Ɓ.Append('[');for(int ņ
=0;ņ<ǣ;++ņ){Ɓ.Append('|');}for(int ņ=ǣ;ņ<Ǣ;++ņ){Ɓ.Append(' ');}Ɓ.Append(']');Î=Ɓ.ToString();Ɓ.Clear();}}
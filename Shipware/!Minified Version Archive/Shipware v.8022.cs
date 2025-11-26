/*
 * Shipware Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=2681807135
 * Shipware Documentation: https://steamcommunity.com/sharedfiles/filedetails/?id=2776664161
 * 
 * THIS CODE CONTAINS NOTHING THAT SHOULD BE EDITED BY THE USER
 */
const double Ϗ=.8022;const string ώ="Shipware";const string ύ="SW";const string ό="Dec";string ϋ;string ϊ;const string ω
="Source";const string ψ="Target";const bool χ=true;const bool φ=false;ǜ υ;MyIni ϐ;MyIni τ;StringBuilder Ƃ;Ǩ ς;o[]ρ;y[]π;
Dictionary<string,Ș>ο;Ȣ[]ξ;Dictionary<string,Ȅ>ν;ɱ[]μ;ǆ[]λ;Dictionary<string,ɪ>κ;List<Ƶ>ι;IMyBroadcastListener σ;ē θ;Ю ϑ;
Dictionary<string,Ю>Ϥ;bool Ϣ;DateTime ϡ;string Ϡ;static Color ϟ=new Color(25,225,100);static Color Ϟ=new Color(100,200,225);static
Color ϝ=new Color(255,255,0);static Color Ϝ=new Color(255,150,0);static Color ϛ=new Color(255,0,0);static Color Ϛ=new Color(
255,225,200);static Color ϙ=new Color(0,0,0);Program(){ź Ė;bool ϓ;Ϙ(out Ė,out ϓ);if(ϓ){ϖ(Ė);}else{Ѵ(Ė);}Echo(υ.Ǫ());}void Ϙ
(out ź Ė,out bool ϓ){ϐ=new MyIni();τ=new MyIni();Ƃ=new StringBuilder();Ϣ=φ;ϡ=DateTime.Now;Ė=new ź(Ƃ,15);ϓ=φ;ϐ.TryParse(
Storage);double ϗ=ϐ.Get("Data","Version").ToDouble(-1);ϋ=ϐ.Get("Data","ID").ToString(ώ);ϊ=$"{ύ}.{ϋ}";σ=IGC.
RegisterBroadcastListener(ϊ);σ.SetMessageCallback(ϊ);υ=new ǜ(Ƃ,$"Shipware v{Ϗ} - Recent Events",χ);ς=new Ǩ(Ƃ);if(ϊ!=$"{ύ}.{ώ}"){υ.Ǜ=ϊ;}θ=new ē();
Ϥ=new Dictionary<string,Ю>();ϐ.Clear();if(ϗ==-1){ϓ=χ;}else if(ϗ!=Ϗ){Ė.ƍ($"Code updated from v{ϗ} to v{Ϗ}.");}υ.ǐ(
"Script initialization complete.");}void ϖ(ź Ė){MyIniParseResult ϕ;string ϒ=$"{ύ}.Init";if(!ϐ.TryParse(Me.CustomData,out ϕ)){Ė.Ɨ(
$"Cannot generate a {ϒ} section because the parser encountered "+$"an error on line {ϕ.LineNo} of the Programmable Block's config: {ϕ.Error}");}else{ϔ(Ė,χ);}Ė.ƍ(
"Use the AutoPopulate command to generate basic configuration.");Ė.ƍ("The Clone command can quickly distribute config across identical blocks.");Ė.ƍ(
"The Evaluate command scans the grid for config and loads it into memory.");string ħ=$"First run complete.\nThe following messages were logged:\n{Ė.Ɖ()}";if(Ė.ƕ()>0){ħ+=
$"The following errors were logged:\n{Ė.Ɠ()}";}υ.ǐ(ħ);}void ϔ(ź Ė,bool ϓ=φ){string ϒ=$"{ύ}.Init";bool ϣ=ϐ.ContainsSection(ϒ);bool η=ϣ&&!ϓ;bool ε=φ;string Ο=Me.
CustomName;if(!ϣ&&!ϓ){Ė.ƍ($"{ϒ} section was missing from block '{Ο}' and "+$"has been re-generated.");}string[]Ξ=new string[]{
"ColorOptimal","ColorNormal","ColorCaution","ColorWarning","ColorCritical","MPSpriteSyncFrequency","APExcludedBlockTypes",
"APExcludedBlockSubTypes","APExcludedDeclarations"};string[]Ν=new string[]{"Green","LightBlue","Yellow","Orange","Red","-1",(
"MyObjectBuilder_ConveyorSorter, MyObjectBuilder_ShipWelder,\n"+"MyObjectBuilder_ShipGrinder"),($"StoreBlock, ContractBlock, {ύ}.FurnitureSubTypes,\n"+
$"{ύ}.IsolatedCockpitSubTypes, {ύ}.ShelfSubTypes"),("ThrustersGeneric")};bool[]Μ=new bool[]{φ,φ,φ,φ,φ,χ,χ,χ,χ};for(int Ǡ=0;Ǡ<Ξ.Length;Ǡ++){ї(ϒ,Ξ[Ǡ],Ν[Ǡ],Μ[Ǡ],ref ε,η,Ο,Ė
);}if(ε){Me.CustomData=ϐ.ToString();}}void Save(){ϐ.Clear();ϐ.Set("Data","Version",Ϗ);ϐ.Set("Data","ID",ϋ);int Λ=θ.č(
"UpdateDelay");ϐ.Set("Data","UpdateDelay",Λ==-1?0:Λ);if(ο!=null){foreach(Ș Κ in ο.Values){ϐ.Set("ActionSets",Κ.w,Κ.ï);}}if(ξ!=null){
foreach(Ȣ ě in ξ){ϐ.Set("Triggers",ě.w,ě.ț);}}if(κ!=null){foreach(ɪ Ι in κ.Values){ϐ.Set("MFDs",Ι.w,Ι.ɧ);}}Storage=ϐ.ToString()
;ϐ.Clear();}void Main(string Π,UpdateType Θ){MyCommandLine Ζ=null;Func<string,bool>Ε=(Γ)=>{Ζ=new MyCommandLine();return(Ζ
.TryParse(Γ));};Func<string,bool>Δ=(Γ)=>{Ζ=new MyCommandLine();return(Ζ.TryParse(Γ.ToLowerInvariant()));};if((Θ&
UpdateType.Update100)!=0){θ.ğ();}if((Θ&UpdateType.Update10)!=0){if(ϑ!=null){bool Β=ϑ.У();υ.Ǚ=ϑ.Ш;if(!Β){if(ϑ.а){υ.ǐ(ϑ.П());}ϑ.Р();
Ϥ.Remove(ϑ.Э);ϑ=null;}}else{if(Ϥ.Count>0){ϑ=Ϥ.Values.ElementAt(0);ϑ.Ф();}else{Runtime.UpdateFrequency&=~UpdateFrequency.
Update10;υ.Ǚ="";}}}else if((Θ&UpdateType.Once)!=0){Ѵ(new ź(Ƃ,15));}else if((Θ&UpdateType.IGC)!=0){while(σ.HasPendingMessage){
MyIGCMessage Ň=σ.AcceptMessage();if(Ň.Tag==ϊ){string Α=Ň.Data.ToString();if(Δ(Α)){string ħ="No reply";string ΐ=null;bool Ώ=φ;Action
Η=()=>{MyCommandLine.SwitchEnumerator Ύ=Ζ.Switches.GetEnumerator();Ύ.MoveNext();ΐ=Ύ.Current;};if(Ζ.Argument(0)=="reply"){
Ώ=χ;Ε(Α);Α=Α.Replace(Ζ.Argument(0),"");Α=Α.Trim();if(Ζ.Switches.Count==1){Η();Α=Α.Replace($"-{ΐ}","");Α=Α.Trim();ħ=
$"Received IGC reply from {ΐ}: {Α}";}else{ħ=($"Received IGC reply: {Α}");}}else if(Ζ.Argument(0)=="action"){if(Ζ.ArgumentCount==3){ħ=ϯ(Ζ.Argument(1),Ζ.
Argument(2),"IGC-directed ");}else{ħ=$"Received IGC-directed command '{Α}', which "+$"has an incorrect number of arguments.";}}
else if(Ζ.ArgumentCount==1){ħ=ϯ(Ζ.Argument(0),"switch","IGC-directed ");}else{ħ=
$"Received the following unrecognized command from the IGC:"+$" '{Α}'.";}if(!Ώ&&Ζ.Switches.Count==1){Ε(Α);Η();IGC.SendBroadcastMessage(ΐ,$"reply {ħ} -{ϊ}");ħ+=
$"\nSent reply on channel {ΐ}.";}υ.ǐ(ħ);}else{υ.ǐ($"Received IGC-directed command '{Α}', which couldn't be "+$"handled by the argument reader.");}}}}
else{if(Δ(Π)){string Ƿ=Ζ.Argument(0);string ζ="";string ħ="";switch(Ƿ){case"log":break;case"igc":Ε(Π);string Α=Π.Remove(0,4)
;Α=Α.Replace(Ζ.Argument(1),"");Α=Α.Trim();IGC.SendBroadcastMessage(Ζ.Argument(1),Α);υ.ǐ(
$"Sent the following IGC message on channel '{Ζ.Argument(1)}'"+$": {Α}.");break;case"mfd":if(Ζ.ArgumentCount==3){string δ=Ζ.Argument(1);string γ=Ζ.Argument(2);if(κ==null){υ.ǐ(
$"Received MFD command, but script configuration isn't loaded.");}else if(κ.ContainsKey(δ)){ɪ β=κ[δ];if(γ=="next"){β.ɢ(χ);}else if(γ=="prev"){β.ɢ(φ);}else{if(!β.ɠ(γ)){υ.ǐ(
$"Received command to set MFD '{δ}' to unknown "+$"page '{γ}'.");}}}else{υ.ǐ($"Received '{γ}' command for un-recognized MFD '{δ}'.");}}else{υ.ǐ(
$"Received MFD command with an incorrect number of arguments.");}break;case"action":if(Ζ.ArgumentCount==3){ħ=ϯ(Ζ.Argument(1),Ζ.Argument(2),"");if(!Ѐ(ħ)){υ.ǐ(ħ);}}else{υ.ǐ(
$"Received Action command with an incorrect number of arguments.");}break;case"raycast":if(Ζ.ArgumentCount==2){string α=Ζ.Argument(1);if(ν==null){υ.ǐ(
$"Received Racast command, but script configuration isn't loaded.");}else if(ν.ContainsKey(α)){ν[α].Ȉ();}else{υ.ǐ($"Received Raycast command for un-recognized Raycaster '{α}'.");}}else{υ
.ǐ($"Received Raycast command with an incorrect number of arguments.");}break;case"reconstitute":if(!Ϣ){υ.ǐ(
"Received Reconstitute command, but there is no last-good "+"config to reference. Please only use this command after the "+"script has successfully evaluated.");}else if(!Ζ.Switch
("force")){υ.ǐ("Received Reconstitute command, which will regenerate "+"declarations based on config that was read "+
$"{(DateTime.Now-ϡ).Minutes} minutes ago "+$"({ϡ.ToString("HH: mm: ss")}). If this is "+"acceptable, re-run this command with the -force flag.");}else{Me.
CustomData=$"{Ϡ}\n";if(!Ѐ(Ϡ)){Me.CustomData+=";=======================================\n\n";}Me.CustomData+=ϻ(π.ToList(),ο.Values.
ToList(),ξ.ToList(),ν.Values.ToList());υ.ǐ($"Carried out Reconstitute command. PB config has been reverted "+
$"to last known good.");}break;case"clone":List<IMyTerminalBlock>ΰ=new List<IMyTerminalBlock>();ζ="Clone command";if(!Ϩ(ω,ΰ,ref ζ)){υ.ǐ(ζ);}
else{IMyTerminalBlock ί=ΰ[0];ΰ.Clear();if(!Ϩ(ψ,ΰ,ref ζ)){υ.ǐ(ζ);}else{foreach(IMyTerminalBlock A in ΰ){A.CustomData=ί.
CustomData;}υ.ǐ($"Carried out Clone command, replacing the CustomData "+$"of {ΰ.Count} blocks in the {ψ} "+
$"group with the CustomData from block '{ί.CustomName}'.");}}break;case"tacticalnuke":if(Ζ.Switch("force")){List<IMyTerminalBlock>ή=new List<IMyTerminalBlock>();ζ=
"TacticalNuke command";if(!Ϩ(ψ,ή,ref ζ)){υ.ǐ(ζ);}else{foreach(IMyTerminalBlock A in ή){A.CustomData="";}υ.ǐ(
$"Carried out TacticalNuke command, clearing the "+$"CustomData of {ή.Count} blocks.");}}else{υ.ǐ("Received TacticalNuke command. TacticalNuke will remove "+
$"ALL CustomData from blocks in the {ψ} group. "+"If you are certain you want to do this, run the command with the "+"-force switch.");}break;case"terminalproperties":
List<IMyTerminalBlock>έ=new List<IMyTerminalBlock>();ζ="TerminalProperties command";if(!Ϩ(ω,έ,ref ζ)){υ.ǐ(ζ);}else{
Dictionary<Type,string>ά=new Dictionary<Type,string>();List<ITerminalProperty>Ϋ=new List<ITerminalProperty>();string Ϊ;foreach(
IMyTerminalBlock A in έ){if(!ά.ContainsKey(A.GetType())){A.GetProperties(Ϋ);Ϊ="";foreach(ITerminalProperty Ω in Ϋ){Ϊ+=
$"  {Ω.Id}  {Ω.TypeName}\n";}ά.Add(A.GetType(),Ϊ);}}Ƃ.Clear();string[]Ψ;foreach(KeyValuePair<Type,string>Š in ά){Ψ=Š.Key.ToString().Split('.');Ƃ.
Append($"Properties for '{Ψ[Ψ.Length-1]}'\n{Š.Value}");}υ.ǐ(Ƃ.ToString());Ƃ.Clear();}break;case"typedefinitions":List<
IMyTerminalBlock>Χ=new List<IMyTerminalBlock>();ζ="TypeDefinitions command";if(!Ϩ(ω,Χ,ref ζ)){υ.ǐ(ζ);}else{bool Φ=Ζ.Switch("items");List
<MyInventoryItem>Υ=new List<MyInventoryItem>();string[]Τ;Ƃ.Clear();Ƃ.Append(
$"Type Definitions for members of the {ω} group:\n");foreach(IMyTerminalBlock A in Χ){Τ=A.GetType().ToString().Split('.');Ƃ.Append($" {A.CustomName}:\n"+
$"  Interface: {Τ[Τ.Length-1]}\n"+$"  TypeID: {A.BlockDefinition.TypeIdString}\n"+$"  SubTypeID: {A.BlockDefinition.SubtypeId}\n"+$"\n");if(Φ&&A.
HasInventory){A.GetInventory().GetItems(Υ);Ƃ.Append("  Items:\n");foreach(MyInventoryItem Σ in Υ){Ƃ.Append(
$"   Name: {Σ.Type.ToString()}\n");Ƃ.Append($"    TypeID: {Σ.Type.TypeId}\n");Ƃ.Append($"    SubTypeID: {Σ.Type.SubtypeId}\n");}}}υ.ǐ(Ƃ.ToString());Ƃ.
Clear();}break;case"surfacescripts":List<string>Ρ=new List<string>();Me.GetSurface(0).GetScripts(Ρ);Ƃ.Clear();Ƃ.Append(
"Available scripts:\n");foreach(string ϥ in Ρ){Ƃ.Append($"  {ϥ}\n");}υ.ǐ(Ƃ.ToString());Ƃ.Clear();break;case"autopopulate":MyIniParseResult ϕ;
if(!ϐ.TryParse(Me.CustomData,out ϕ)){υ.ǐ("Received AutoPopulate command, but was unable to carry it "+
$"out due to a parsing error on line {ϕ.LineNo} of the "+$"Programmable Block's config: {ϕ.Error}");}else{HashSet<string>Љ=ʄ("APExcludedBlockTypes");HashSet<string>ʁ=ʄ(
"APExcludedBlockSubTypes");ʂ(ʁ);string ϲ="AutoPopulate";List<IMyTerminalBlock>ϴ=new List<IMyTerminalBlock>();if(Ζ.Switch("target")){IMyBlockGroup
Ќ=GridTerminalSystem.GetBlockGroupWithName(ψ);if(Ќ==null){υ.ǐ("Received AutoPopulate command with the -target flag set, "
+$"but there is no {ψ} block group on the grid.");break;}else{Ќ.GetBlocks(ϴ,Ƙ=>Ƙ.IsSameConstructAs(Me)&&!Љ.Contains(Ƙ.
BlockDefinition.TypeIdString)&&!ʁ.Contains(Ƙ.BlockDefinition.SubtypeId)&&!MyIni.HasSection(Ƙ.CustomData,$"{ύ}.APIgnore"));ϲ=
"Targeted AutoPopulate";}}else{Ϭ<IMyTerminalBlock>(ϴ,Ƙ=>Ƙ.IsSameConstructAs(Me)&&!Љ.Contains(Ƙ.BlockDefinition.TypeIdString)&&!ʁ.Contains(Ƙ.
BlockDefinition.SubtypeId)&&!MyIni.HasSection(Ƙ.CustomData,$"{ύ}.APIgnore"));}bool Ћ=ϵ(ϴ,ϐ,ϲ,ref ħ);υ.ǐ(ħ);if(Ћ){Save();Runtime.
UpdateFrequency=UpdateFrequency.Once;}}break;case"apexclusionreport":if(!ϐ.TryParse(Me.CustomData,out ϕ)){υ.ǐ(
"Received APExclusionReport command, but was unable to carry it "+$"out due to a parsing error on line {ϕ.LineNo} of the "+$"Programmable Block's config: {ϕ.Error}");}else{ħ=
"Carried out APExclusionReport command.\n";MyIniValue ė=ϐ.Get($"{ύ}.Init","APExcludedDeclarations");if(!Ѐ(ė.ToString())){string Њ=ė.ToString();ħ+=
$"These declarations are being excluded from consideration "+$"by AutoPopulate: {Њ}.\n";List<string>Ş;Ş=Њ.Split(',').Select(Ļ=>Ļ.Trim()).ToList();ź Ѝ=new ź(Ƃ,5);ɽ(Ş,Ѝ);if(Ѝ.Ƒ()>0){
ħ+=Ѝ.Ə();}ħ+="\n";}HashSet<string>Љ=ʄ("APExcludedBlockTypes");Dictionary<string,int>Ј=Љ.ToDictionary(І=>І,І=>0);HashSet<
string>ʁ=ʄ("APExcludedBlockSubTypes");ʂ(ʁ);Dictionary<string,int>Ї=ʁ.ToDictionary(І=>І,І=>0);int Ѕ=0;int Є=0;int Ѓ=0;List<
IMyTerminalBlock>Ђ=new List<IMyTerminalBlock>();Ϭ<IMyTerminalBlock>(Ђ,Ƙ=>Ƙ.IsSameConstructAs(Me));foreach(IMyTerminalBlock A in Ђ){if(
MyIni.HasSection(A.CustomData,$"{ύ}.APIgnore")){Ѕ++;}if(Ј.ContainsKey(A.BlockDefinition.TypeIdString)){Ј[A.BlockDefinition.
TypeIdString]++;Є++;}if(Ї.ContainsKey(A.BlockDefinition.SubtypeId)){Ї[A.BlockDefinition.SubtypeId]++;Ѓ++;}}ħ+=
$"Of the {Ђ.Count} TerminalBlocks on this "+$"construct, the following {Ѕ+Є+Ѓ} "+$"blocks are being excluded from consideration by AutoPopulate:\n";ħ+=
$"\n -{Ѕ} blocks excluded by APIgnore\n";ħ+=$"\n -{Є} blocks excluded by type\n";foreach(KeyValuePair<string,int>Ħ in Ј){ħ+=$"  >{Ħ.Value} {Ħ.Key}\n";}ħ+=
$"\n -{Ѓ} blocks excluded by subype\n";foreach(KeyValuePair<string,int>Ħ in Ї){ħ+=$"  >{Ħ.Value} {Ħ.Key}\n";}ϐ.Clear();υ.ǐ(ħ);}break;case"clear":List<
IMyTerminalBlock>Й=new List<IMyTerminalBlock>();ζ="Clear command";if(!Ϩ(ψ,Й,ref ζ)){υ.ǐ(ζ);}else{List<string>И=new List<string>();string
[]З;int Ж=0;foreach(IMyTerminalBlock A in Й){ϐ.TryParse(A.CustomData);ϐ.GetSections(И);foreach(string Е in И){З=Е.Split(
'.');if(З[0]==ύ){ϐ.DeleteSection(Е);Ж++;}}A.CustomData=ϐ.ToString();}ϐ.Clear();υ.ǐ(
$"Clear command executed on {Й.Count} blocks. Removed "+$"{Ж} Shipware sections.");}break;case"changeid":Ε(Π);if(Ζ.ArgumentCount==2){string Д=Ζ.Argument(1);string Г=$"{ύ}.{Д}"
;List<IMyTerminalBlock>ϱ=new List<IMyTerminalBlock>();Ϭ<IMyTerminalBlock>(ϱ,Ƙ=>(Ƙ.IsSameConstructAs(Me)&&MyIni.HasSection
(Ƙ.CustomData,ϊ)));foreach(IMyTerminalBlock A in ϱ){A.CustomData=A.CustomData.Replace($"[{ϊ}]",$"[{Г}]");}ϋ=Д;Save();ź Ė;
bool ϓ;Ϙ(out Ė,out ϓ);Runtime.UpdateFrequency=UpdateFrequency.Once;υ.ǐ(
$"ChangeID complete, {ϱ.Count} blocks modified. The ID "+$"of this script instance is now '{ϋ}', and its tag is now '{ϊ}'.");}else if(Ζ.ArgumentCount>2){υ.ǐ(
$"Received ChangeID command with too many arguments. Note "+$"that IDs can't contain spaces.");}else{υ.ǐ($"Received ChangeID command with no new ID.");}break;case"integrate":List<
IMyTerminalBlock>В=new List<IMyTerminalBlock>();Ϭ<IMyTerminalBlock>(В,Ƙ=>(Ƙ.IsSameConstructAs(Me)&&MyIni.HasSection(Ƙ.CustomData,
$"{ύ}.Integrate")));foreach(IMyTerminalBlock A in В){A.CustomData=A.CustomData.Replace($"[{ύ}.Integrate]",$"[{ϊ}]");}υ.ǐ(
$"Carried out Integrate command, replacing the '{ύ}.Integrate' "+$"section headers on {В.Count} blocks with '{ϊ}' headers.");break;case"evaluate":Save();Ѵ(new ź(Ƃ,15));break;case
"resetreports":if(θ.İ("ResetReports",10,out ħ)){Ͼ(new Ч(this,μ,χ));}else{υ.ǐ(ħ);}break;case"update":Ë();if(Ζ.Switch("force")){foreach(
ɱ Ȍ in μ){Ȍ.ɯ();}foreach(ǆ ϰ in λ){ϰ.ǂ();}}else{ǂ();}if(Ζ.Switch("performance")){υ.ǐ(
$"Update used {Runtime.CurrentInstructionCount} / {Runtime.MaxInstructionCount} "+$"({(int)(((double)Runtime.CurrentInstructionCount/Runtime.MaxInstructionCount)*100)}%) "+
$"of instructions allowed in this tic.\n");}break;case"test":Action Б=()=>{υ.ǐ("Periodic event firing");};ĥ А=new ĥ(10,Б);θ.Đ("Test Event",А);if(!θ.İ(
"Test Cooldown",20,out ħ)){υ.ǐ(ħ);}υ.ǐ(θ.Ĩ());break;default:υ.ǐ($"Received un-recognized run command '{Ƿ}'.");break;}}}Echo(υ.Ǫ());if((
Θ&UpdateType.Update100)!=0){foreach(Ȅ Џ in ν.Values){Џ.ǟ();}υ.ǟ();}}void Ë(){foreach(y k in π){k.Í();}foreach(o Ў in ρ){Ў
.l();}foreach(y k in π){k.Ë();}Ș Ȁ=null;bool К,ǹ;foreach(Ȣ ě in ξ){К=ě.ǻ(out Ȁ,out ǹ);if(К){υ.ǐ(Ǹ(Ȁ,ǹ,ǹ?"on":"off",
$"Trigger {ě.w}'s "));}}}void ǂ(){foreach(ɱ Ȍ in μ){Ȍ.ǂ();}foreach(ǆ ϰ in λ){ϰ.ǂ();}}string ϯ(string Ϯ,string ϭ,string ϫ){Ș Ȁ;bool ǹ;if(ο==
null){return"Received Action command, but script configuration isn't loaded.";}else if(ο.TryGetValue(Ϯ,out Ȁ)){if(ϭ=="on"){ǹ
=χ;}else if(ϭ=="off"){ǹ=φ;}else if(ϭ=="switch"){ǹ=!Ȁ.ï;}else{return$"Received unknown {ϫ}command '{ϭ}' for ActionSet "+
$"'{Ϯ}'. Valid commands for ActionSets are 'On', 'Off', and "+$"'Switch'.";}return Ǹ(Ȁ,ǹ,ϭ,ϫ);}else{return$"Received {ϫ}command '{ϭ}' for un-recognized "+$"ActionSet '{Ϯ}'.";}}
string Ǹ(Ș Ȁ,bool ǹ,string Ƿ,string ϫ){string ħ="";try{Ȁ.Ȑ(ǹ);}catch(InvalidCastException e){string ϩ="<ID not provided>";if(e
.Data.Contains("Identifier")){ϩ=$"{e.Data["Identifier"]}";}ħ=
$"An invalid cast exception occurred while running {ϫ}'{Ƿ}' "+$"command for ActionSet '{Ȁ.w}' at {ϩ}. Make sure "+$"the action specified in configuration can be performed by {ϩ}.";}
catch(InvalidOperationException e){string Ϫ="<Trace failed>";if(e.Data.Contains("Counter")){Ϫ="Set Trace:\n";for(int Ǡ=(int)(
e.Data["Counter"]);Ǡ>=0;Ǡ--){Ϫ+=$"{e.Data[Ǡ]}\n";}}ħ=$"A possible loop was detected while running {ϫ}'{Ƿ}' command "+
$"for ActionSet '{Ȁ.w}'. Make sure {Ȁ.w} is "+$"not being called by one of the sets it is calling.\n\n{Ϫ}";}catch(Exception e){string ϩ="<ID not provided>";if(e.Data
.Contains("Identifier")){ϩ=$"{e.Data["Identifier"]}";}ħ=$"An exception occurred while running {ϫ}'{Ƿ}' command for "+
$"ActionSet '{Ȁ.w}' at {ϩ}.\n  Raw exception message:\n "+$"{e.Message}\n  Stack trace:\n{e.StackTrace}";}ǂ();foreach(Ș Κ in ο.Values){Κ.Ț();}if(Ѐ(ħ)&&!Ѐ(ϫ)){ħ=
$"Carried out {ϫ}command '{Ƿ}' for ActionSet '{Ȁ.w}'. "+$"The set's state is now '{Ȁ.ȓ}'.";}return ħ;}bool Ϩ(string ϧ,List<IMyTerminalBlock>Ϧ,ref string ζ){GridTerminalSystem.
GetBlockGroupWithName(ϧ)?.GetBlocks(Ϧ);if(Ϧ.Count>0){return χ;}else{ζ=$"Received {ζ}, but there is no {ϧ} block group on the grid.";return φ;
}}void Ϭ<ó>(List<ó>ϱ,Func<ó,bool>Ё=null)where ó:class{GridTerminalSystem.GetBlocksOfType<ó>(ϱ,Ё);}bool Ѐ(string Ͽ){return
String.IsNullOrEmpty(Ͽ);}bool Ͼ(Ю Ͻ){string ϼ=Ͻ.Э;if(!Ϥ.ContainsKey(ϼ)){Ϥ.Add(ϼ,Ͻ);Runtime.UpdateFrequency|=UpdateFrequency.
Update10;if(Ͻ.а&&ϑ!=null){υ.ǐ($"{ϼ} successfully added to scheduled tasks.");}return χ;}else{υ.ǐ(
$"Cannot schedule {ϼ} because an identical task is already scheduled.");return φ;}}string ϻ(List<y>n,List<Ș>ù,List<Ȣ>Ϻ,List<Ȅ>Ϲ){string ϸ;string Ϸ=
";=======================================\n\n";Ƃ.Clear();foreach(y k in n){Ƃ.Append(k.Ţ());}if(n.Count>0){Ƃ.Append(Ϸ);}foreach(Ș Õ in ù){Ƃ.Append(Õ.Ţ());}if(ù.Count>0
){Ƃ.Append(Ϸ);}foreach(Ȣ ě in Ϻ){Ƃ.Append(ě.Ţ());}if(Ϻ.Count>0){Ƃ.Append(Ϸ);}foreach(Ȅ ƭ in Ϲ){Ƃ.Append(ƭ.Ţ());}ϸ=Ƃ.
ToString();Ƃ.Clear();return ϸ;}bool ϵ(List<IMyTerminalBlock>ϴ,MyIni ϳ,string ϲ,ref string ħ){MyIni ˣ=τ;MyIniParseResult ϕ;ź ɻ=
new ź(Ƃ,10);MyIniValue ė=ϳ.Get($"{ύ}.Init","APExcludedDeclarations");List<string>ɼ=null;if(!Ѐ(ė.ToString())){ɼ=ė.ToString()
.Split(',').Select(Ļ=>Ļ.Trim()).ToList();}List<Ά>Ό=ɽ(ɼ,ɻ);List<Ά>ʈ=new List<Ά>(Ό);List<ˍ>ʬ=new List<ˍ>();List<ˌ>ʫ=new
List<ˌ>();List<ˈ>ʪ=new List<ˈ>();Action<Ά>ʩ=(ʨ)=>{if(ʨ is ˍ){ʬ.Add((ˍ)ʨ);}else if(ʨ is ˌ){ʫ.Add((ˌ)ʨ);}else if(ʨ is ˈ){ʪ.Add
((ˈ)ʨ);}Ό.Remove(ʨ);};List<MyIniKey>ʧ=new List<MyIniKey>();List<string>ʦ=new List<string>();Action<string,MyIni,MyIni>ʥ=(
K,ʤ,ʣ)=>{ʤ.TryParse(K);ʤ.GetKeys(ʧ);foreach(MyIniKey ĝ in ʧ){ʦ.Add((ʤ.Get(ĝ)).ToString());}for(int Ǡ=0;Ǡ<ʧ.Count;Ǡ++){ʣ.
Set(ʧ[Ǡ],ʦ[Ǡ]);}ʦ.Clear();};string ʡ=$"{ύ}.{ό}.ActionSet.Roost";if(!ϳ.ContainsSection(ʡ)&&!(ɼ?.Contains("Roost")??φ)){Ș ʠ=
new Ș("Roost",φ);ʠ.x=ϋ;ʠ.Ȏ=ϛ;ʠ.ȍ=ϟ;ʠ.ȕ="Roosting";ʠ.Ȕ="Active";î ʟ=new î(this);ʟ.í=8;ʠ.Ȓ(ʟ);ʥ(ʠ.Ţ(),ˣ,ϳ);}int ī=0;Ά ʞ;
string Ĵ;while(ī!=-1){ʞ=Ό[ī];Ĵ=$"{ύ}.{ό}.{ʞ.ˎ()}.{ʞ.G}";if(ϳ.ContainsSection(Ĵ)){ʩ(ʞ);ʈ.Remove(ʞ);}else{ī++;}if(ī>=Ό.Count){ī=
-1;}}MyDefinitionId ʝ;HashSet<MyDefinitionId>ʜ=new HashSet<MyDefinitionId>();foreach(IMyTerminalBlock A in ϴ){ʝ=A.
BlockDefinition;if(!ʜ.Contains(ʝ)){ī=0;while(ī!=-1&&Ό.Count!=0){ʞ=Ό[ī];if(ʞ.Ͳ(A)){ʩ(ʞ);}else{ī++;}if(ī>=Ό.Count){ī=-1;}}ʜ.Add(ʝ);}}
foreach(Ά ʛ in Ό){ʈ.Remove(ʛ);}foreach(Ά ʚ in ʈ){ʥ(ʚ.ͳ(),ˣ,ϳ);}if(!(ɼ?.Contains("Roost")??φ)){string ʢ;List<string>ʙ;HashSet<
string>ʮ;Action<string,bool>ʼ=(ĝ,ï)=>{ʢ=ϳ.Get(ʡ,ĝ).ToString();if(!Ѐ(ʢ)){ʙ=ʢ.Split(',').Select(Ļ=>Ļ.Trim()).ToList();ʮ=new
HashSet<string>();foreach(string ʻ in ʙ){int ʺ=ʻ.IndexOf(':');if(ʺ!=-1){ʮ.Add(ʻ.Substring(0,ʺ));}else{ʮ.Add(ʻ);}}}else{ʮ=null;ʙ
=new List<string>();}string ǹ;foreach(ˈ ʹ in ʪ){string w=ʹ.G;ǹ=ʹ.ͱ(ï);if(!(ʮ?.Contains(w)??φ)&&!Ѐ(ǹ)){ʙ.Add($"{w}: {ǹ}");
}}ϳ.Set(ʡ,ĝ,ş(ʙ,3,φ));};ʼ("ActionSetsLinkedToOn",χ);ʼ("ActionSetsLinkedToOff",φ);}string ʸ="";List<string>Ş=new List<
string>();string ʷ;Action<string>ʶ=(Ǔ)=>{ϳ.Set(ʸ,"Title",Ǔ);ϳ.Set(ʸ,"Columns","3");ϳ.Set(ʸ,"FontSize",".5");ϳ.Set(ʸ,
"ForeColor","Yellow");ϳ.Set(ʸ,"BackColor","Black");};ϳ.Set(ϊ,"Surface0Pages",$"{((ʬ.Count>0||ʫ.Count>0)?"TallyReport, ":"")}"+
$"{(ʪ.Count>0?"SetReport, ":"")}Log, TargetScript, FactionScript");ϳ.Set(ϊ,"Surface0MFD","APScreen");if(ʬ.Count>0||ʫ.Count>0){ʸ="SW.TallyReport";foreach(Ά ʵ in ʬ){Ş.Add(ʵ.G);}foreach(Ά
ʵ in ʫ){Ş.Add(ʵ.G);}ʷ=ş(Ş,3,φ);ϳ.Set(ʸ,"Elements",ʷ);ʶ("Tallies");}Ş.Clear();if(ʪ.Count>0){ʸ="SW.SetReport";foreach(Ά ʅ
in ʪ){Ş.Add(ʅ.G);}ʷ=ş(Ş,3,φ);ϳ.Set(ʸ,"Elements",ʷ);ʶ("Action Sets");}ʸ="SW.Log";ϳ.Set(ʸ,"DataType","Log");ϳ.Set(ʸ,
"FontSize",".8");ϳ.Set(ʸ,"CharPerLine","30");ϳ.Set(ʸ,"ForeColor","LightBlue");ϳ.Set(ʸ,"BackColor","Black");ʸ="SW.TargetScript";ϳ.
Set(ʸ,"Script","TSS_TargetingInfo");ϳ.Set(ʸ,"ForeColor","LightBlue");ϳ.Set(ʸ,"BackColor","Black");ʸ="SW.FactionScript";ϳ.
Set(ʸ,"Script","TSS_FactionIcon");ϳ.Set(ʸ,"BackColor","Black");Me.CustomData=ϳ.ToString();Dictionary<MyDefinitionId,ˤ>ʴ=new
Dictionary<MyDefinitionId,ˤ>();ˤ ʳ=null;int ʲ=0;int ʱ=0;Func<IMyTerminalBlock,MyIni,bool>ʰ=(Ƙ,ʭ)=>{if(!ʭ.TryParse(Ƙ.CustomData,out
ϕ)){ʲ++;ɻ.ƒ($"Block {Ƙ.CustomName} failed to parse due to the following "+$"error on line {ϕ.LineNo}: {ϕ.Error}");return
φ;}else{return χ;}};Func<IMyTerminalBlock,MyIni,bool>ʯ=(Ƙ,ʭ)=>{if(ʳ==null){if(ʰ(Ƙ,ʭ)){ʳ=new ˤ(Ƙ,ʭ,ϊ);return χ;}else{
return φ;}}return χ;};foreach(IMyTerminalBlock A in ϴ){ʳ=null;ʝ=A.BlockDefinition;if(ʴ.ContainsKey(ʝ)){ʳ=ʴ[ʝ];if(ʳ!=null){if(ʰ
(A,ˣ)){ʳ.ʥ(ϊ,ˣ,!ˣ.ContainsSection(ϊ));A.CustomData=ˣ.ToString();ʱ++;}}}else{foreach(ˍ ʘ in ʬ){if(ʘ.Ͳ(A)){if(ʯ(A,ˣ)){ʳ.Ѹ(
"Tallies",ʘ.G);}else{goto CannotWriteToThisBlockSoSkipToNext;}}}foreach(ˌ ʖ in ʫ){if(A.InventoryCount==1){if(ʖ.ˉ(A.GetInventory(0
))){if(ʯ(A,ˣ)){ʳ.Ѹ("Tallies",ʖ.G);}else{goto CannotWriteToThisBlockSoSkipToNext;}}}else if(A.InventoryCount>1){for(int Ǡ=
0;Ǡ<A.InventoryCount;Ǡ++){if(ʖ.ˉ(A.GetInventory(Ǡ))){if(ʯ(A,ˣ)){ʳ.Ѹ($"Inv{Ǡ}Tallies",ʖ.G);}else{goto
CannotWriteToThisBlockSoSkipToNext;}}}}}foreach(ˈ ʅ in ʪ){if(ʅ.Ͳ(A)){if(ʯ(A,ˣ)){ʳ.Ѹ("ActionSets",ʅ.G);ʳ.ʞ=ʅ;}else{goto CannotWriteToThisBlockSoSkipToNext;
}}}ʴ.Add(ʝ,ʳ);if(ʳ!=null){ʳ.ʥ(ϊ,ˣ);A.CustomData=ˣ.ToString();ʱ++;}}CannotWriteToThisBlockSoSkipToNext:;}ħ=
$"\nCarried out {ϲ} command. There are now declarations for "+$"{ʬ.Count+ʫ.Count} AP Tallies and {ʪ.Count} "+
$"AP ActionSets, with linking config written to {ʱ} / {ϴ.Count} of considered "+$"blocks{(ʲ>0?$" and {ʲ} blocks with unparsable config":"")}.\n"+
$"Autopopulate used {Runtime.CurrentInstructionCount} / {Runtime.MaxInstructionCount} "+$"({(int)(((double)Runtime.CurrentInstructionCount/Runtime.MaxInstructionCount)*100)}%) "+
$"of instructions allowed in this tic.\n";if(ɻ.ƕ()>0){ħ+=$"\nThe following errors prevented AutoPopulate from running:\n{ɻ.Ɠ()}";}if(ɻ.Ƒ()>0){ħ+=
$"\nThe following warnings should be addressed:\n{ɻ.Ə()}";}if(ɻ.Ƌ()>0){ħ+=$"\nThe following messages were logged:\n{ɻ.Ɖ()}";}ϳ.Clear();ˣ.Clear();return χ;}HashSet<string>ʄ(
string ĝ){HashSet<string>ʃ=new HashSet<string>();MyIniValue ė=ϐ.Get($"{ύ}.Init",ĝ);string[]Ş;if(!Ѐ(ė.ToString())){Ş=ė.ToString
().Split(',').Select(Ļ=>Ļ.Trim()).ToArray();foreach(string ř in Ş){ʃ.Add(ř);}}return ʃ;}void ʂ(HashSet<string>ʁ){string ʆ
=$"{ύ}.FurnitureSubTypes";if(ʁ.Contains(ʆ)){ʁ.Remove(ʆ);ʁ.UnionWith(new string[]{"PassengerBench","PassengerSeatLarge",
"PassengerSeatSmallNew","PassengerSeatSmallOffset","LargeBlockBed","LargeBlockHalfBed","LargeBlockHalfBedOffset","LargeBlockInsetBed",
"LargeBlockCaptainDesk","LargeBlockLabDeskSeat","LargeBlockLabCornerDesk"});}string ʀ=$"{ύ}.IsolatedCockpitSubTypes";if(ʁ.Contains(ʀ)){ʁ.Remove
(ʀ);ʁ.UnionWith(new string[]{"BuggyCockpit","OpenCockpitLarge","OpenCockpitSmall","LargeBlockCockpit","CockpitOpen",
"SmallBlockStandingCockpit","RoverCockpit","SpeederCockpitCompact","LargeBlockStandingCockpit","LargeBlockModularBridgeCockpit"});}string ɾ=
$"{ύ}.ShelfSubTypes";if(ʁ.Contains(ɾ)){ʁ.Remove(ɾ);ʁ.UnionWith(new string[]{"LargeBlockLockerRoom","LargeBlockLockerRoomCorner","LargeCrate"
,"LargeBlockInsetBookshelf","LargeBlockLockers","LargeBlockInsetKitchen","LargeBlockWeaponRack","SmallBlockWeaponRack",
"SmallBlockKitchenFridge","SmallBlockFirstAidCabinet","LargeBlockLabCabinet","LargeFreezer"});}}List<Ά>ɽ(List<string>ɼ,ź ɻ){StringComparer ɺ=
StringComparer.OrdinalIgnoreCase;Dictionary<string,Ά>ɹ=new Dictionary<string,Ά>(ɺ);const string ɸ="MyObjectBuilder_Ore";const string ɷ
="MyObjectBuilder_Ingot";const string ɶ="MyObjectBuilder_AmmoMagazine";MyItemType ɵ=new MyItemType(ɸ,"Ice");MyItemType ɴ=
new MyItemType(ɸ,"Stone");MyItemType ɿ=new MyItemType(ɸ,"Iron");MyItemType ɳ=new MyItemType(ɷ,"Uranium");MyItemType ʇ=new
MyItemType(ɶ,"NATO_25x184mm");MyItemType ʗ=new MyItemType(ɶ,"AutocannonClip");MyItemType ʕ=new MyItemType(ɶ,"MediumCalibreAmmo");
MyItemType ʔ=new MyItemType(ɶ,"LargeCalibreAmmo");MyItemType ʓ=new MyItemType(ɶ,"SmallRailgunAmmo");MyItemType ʒ=new MyItemType(ɶ,
"LargeRailgunAmmo");MyItemType ʑ=new MyItemType(ɶ,"Missile200mm");MyDefinitionId ʐ=MyDefinitionId.Parse(
"MyObjectBuilder_GasProperties/Hydrogen");MyDefinitionId ʏ=MyDefinitionId.Parse("MyObjectBuilder_GasProperties/Oxygen");y k;Ũ ʎ=new Ũ();ŧ ʍ=new ŧ();List<
MyItemType>ʌ=new List<MyItemType>();Func<IMyInventory,MyItemType,bool>ʋ=(ʊ,ʉ)=>{ʌ.Clear();ʊ.GetAcceptedItems(ʌ);return(ʌ.Contains(
ʉ));};k=new Ç(ς,"Power",new Ʀ(),ʍ);ɹ.Add(k.w,new ˍ(k.w,k,Ƙ=>Ƙ is IMyBatteryBlock));k=new Ç(ς,"Hydrogen",new ƣ(),ʍ);ɹ.Add(
k.w,new ˍ(k.w,k,Ƙ=>(Ƙ is IMyGasTank&&(Ƙ.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(ʐ)??φ))||(Ƙ
is IMyPowerProducer&&(Ƙ.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(ʐ)??φ))));k=new Ç(ς,"Oxygen"
,new ƣ(),ʍ);ɹ.Add(k.w,new ˍ(k.w,k,Ƙ=>Ƙ is IMyGasTank&&(Ƙ.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.
Contains(ʏ)??φ)));k=new O(ς,"Cargo",ʎ);ɹ.Add(k.w,new ˌ(k.w,k,Ƙ=>Ƙ is IMyCargoContainer,Ǡ=>ʋ(Ǡ,ɵ)&&ʋ(Ǡ,ɳ)));k=new J(ς,"Ice",ɵ,ʍ);
k.Ì(4000);ɹ.Add(k.w,new ˌ(k.w,k,Ƙ=>Ƙ is IMyGasGenerator,Ǡ=>ʋ(Ǡ,ɵ)));k=new J(ς,"Stone",ɴ,ʎ);k.Ì(5000);ɹ.Add(k.w,new ˌ(k.w,
k,Ƙ=>Ƙ is IMyShipDrill||Ƙ is IMyRefinery,Ǡ=>ʋ(Ǡ,ɴ)));k=new O(ς,"Ore",ʎ);ɹ.Add(k.w,new ˌ(k.w,k,Ƙ=>Ƙ is IMyShipDrill||Ƙ is
IMyRefinery,Ǡ=>ʋ(Ǡ,ɿ)));k=new J(ς,"Uranium",ɳ,ʍ);k.Ì(50);ɹ.Add(k.w,new ˌ(k.w,k,Ƙ=>Ƙ is IMyReactor,Ǡ=>ʋ(Ǡ,ɳ)));k=new Ç(ς,"Solar",new
ƫ(),ʍ);k.B=100;ɹ.Add(k.w,new ˍ(k.w,k,Ƙ=>Ƙ is IMySolarPanel));k=new Ç(ς,"JumpDrive",new ơ(),ʍ);k.x="Jump Charge";ɹ.Add(k.w
,new ˍ(k.w,k,Ƙ=>Ƙ is IMyJumpDrive));Func<IMyTerminalBlock,MyItemType,bool>ͽ=(Ƙ,Ǡ)=>{return Ƙ is IMyUserControllableGun&&ʋ
(Ƙ.GetInventory(0),Ǡ);};k=new J(ς,"GatlingAmmo",ʇ,ʍ);k.Ì(20);k.x="Gatling\nDrums";ɹ.Add(k.w,new ˌ(k.w,k,Ƙ=>ͽ(Ƙ,ʇ),Ǡ=>ʋ(Ǡ,
ʇ)));k=new J(ς,"AutocannonAmmo",ʗ,ʍ);k.Ì(60);k.x="Autocannon\nClips";ɹ.Add(k.w,new ˌ(k.w,k,Ƙ=>ͽ(Ƙ,ʗ),Ǡ=>ʋ(Ǡ,ʗ)));k=new J(
ς,"AssaultAmmo",ʕ,ʍ);k.Ì(120);k.x="Cannon\nShells";ɹ.Add(k.w,new ˌ(k.w,k,Ƙ=>ͽ(Ƙ,ʕ),Ǡ=>ʋ(Ǡ,ʕ)));k=new J(ς,"ArtilleryAmmo",
ʔ,ʍ);k.Ì(40);k.x="Artillery\nShells";ɹ.Add(k.w,new ˌ(k.w,k,Ƙ=>ͽ(Ƙ,ʔ),Ǡ=>ʋ(Ǡ,ʔ)));k=new J(ς,"RailSmallAmmo",ʓ,ʍ);k.Ì(36);k
.x="Railgun\nS. Sabots";ɹ.Add(k.w,new ˌ(k.w,k,Ƙ=>ͽ(Ƙ,ʓ),Ǡ=>ʋ(Ǡ,ʓ)));k=new J(ς,"RailLargeAmmo",ʒ,ʍ);k.Ì(12);k.x=
"Railgun\nL. Sabots";ɹ.Add(k.w,new ˌ(k.w,k,Ƙ=>ͽ(Ƙ,ʒ),Ǡ=>ʋ(Ǡ,ʒ)));k=new J(ς,"RocketAmmo",ʑ,ʍ);k.Ì(24);k.x="Rockets";ɹ.Add(k.w,new ˌ(k.w,k,Ƙ=>
ͽ(Ƙ,ʑ),Ǡ=>ʋ(Ǡ,ʑ)));Ș ͼ;Action<MyIni,string,string,string>ͻ=(ʭ,ʹ,ͺ,ͷ)=>{ʭ.Set(ʹ,"ActionOn",ͺ);ʭ.Set(ʹ,"ActionOff",ͷ);};
Action<MyIni,string,string,string>Ͷ=(ʭ,ʹ,ͺ,ͷ)=>{ʭ.Set(ʹ,"Action0Property","Radius");ʭ.Set(ʹ,"Action0ValueOn","1500");ʭ.Set(ʹ,
"Action0ValueOff","150");ʭ.Set(ʹ,"Action1Property","HudText");ʭ.Set(ʹ,"Action1ValueOn",ͺ);ʭ.Set(ʹ,"Action1ValueOff",ͷ);};ͼ=new Ș(
"Antennas",φ);ͼ.x="Antenna\nRange";ͼ.ȕ="Broad";ͼ.Ȕ="Wifi";ͼ.ȍ=ϝ;ɹ.Add(ͼ.w,new ˈ(ͼ.w,ͼ,Ƙ=>Ƙ is IMyRadioAntenna,$"{ύ}.{ͼ.w}",$"{ϋ}",
$"{ϋ} Wifi",Ͷ,"Off","On"));ͼ=new Ș("Beacons",φ);ͼ.x="Beacon";ͼ.ȕ="Online";ͼ.Ȕ="Offline";ɹ.Add(ͼ.w,new ˈ(ͼ.w,ͼ,Ƙ=>Ƙ is IMyBeacon,
$"{ύ}.{ͼ.w}","EnableOn","EnableOff",ͻ,"Off","On"));ͼ=new Ș("Spotlights",φ);ͼ.ȕ="Online";ͼ.Ȕ="Offline";ɹ.Add(ͼ.w,new ˈ(ͼ.w,ͼ,Ƙ=>Ƙ is
IMyReflectorLight,$"{ύ}.{ͼ.w}","EnableOn","EnableOff",ͻ,"Off",""));ͼ=new Ș("OreDetectors",φ);ͼ.x="Ore\nDetector";ͼ.ȕ="Scanning";ͼ.Ȕ=
"Idle";ɹ.Add(ͼ.w,new ˈ(ͼ.w,ͼ,Ƙ=>Ƙ is IMyOreDetector,$"{ύ}.{ͼ.w}","EnableOn","EnableOff",ͻ,"Off","On"));ͼ=new Ș("Batteries",φ);
ͼ.ȕ="On Auto";ͼ.Ȕ="Recharging";ͼ.ȍ=ϝ;ɹ.Add(ͼ.w,new ˈ(ͼ.w,ͼ,Ƙ=>Ƙ is IMyBatteryBlock,$"{ύ}.{ͼ.w}","BatteryAuto",
"BatteryRecharge",ͻ,"Off","On"));ͼ=new Ș("Reactors",φ);ͼ.ȕ="Active";ͼ.Ȕ="Inactive";ɹ.Add(ͼ.w,new ˈ(ͼ.w,ͼ,Ƙ=>Ƙ is IMyReactor,$"{ύ}.{ͼ.w}",
"EnableOn","EnableOff",ͻ,"Off",""));ͼ=new Ș("EnginesHydrogen",φ);ͼ.x="Engines";ͼ.ȕ="Running";ͼ.Ȕ="Idle";ɹ.Add(ͼ.w,new ˈ(ͼ.w,ͼ,Ƙ=>Ƙ
is IMyPowerProducer&&(Ƙ.Components.Get<MyResourceSinkComponent>()?.AcceptedResources.Contains(ʐ)??φ),$"{ύ}.{ͼ.w}",
"EnableOn","EnableOff",ͻ,"Off",""));ͼ=new Ș("IceCrackers",φ);ͼ.x="Ice Crackers";ͼ.ȕ="Running";ͼ.Ȕ="Idle";ɹ.Add(ͼ.w,new ˈ(ͼ.w,ͼ,Ƙ=>
Ƙ is IMyGasGenerator,$"{ύ}.{ͼ.w}","EnableOn","EnableOff",ͻ,"",""));ͼ=new Ș("TanksHydrogen",φ);ͼ.x="Hydrogen\nTanks";ͼ.ȕ=
"Open";ͼ.Ȕ="Filling";ͼ.ȍ=Ϟ;ɹ.Add(ͼ.w,new ˈ(ͼ.w,ͼ,Ƙ=>Ƙ is IMyGasTank&&(Ƙ.Components.Get<MyResourceSinkComponent>()?.
AcceptedResources.Contains(ʐ)??φ),$"{ύ}.{ͼ.w}","TankStockpileOff","TankStockpileOn",ͻ,"Off","On"));ͼ=new Ș("TanksOxygen",φ);ͼ.x=
"Oxygen\nTanks";ͼ.ȕ="Open";ͼ.Ȕ="Filling";ͼ.ȍ=Ϟ;ɹ.Add(ͼ.w,new ˈ(ͼ.w,ͼ,Ƙ=>Ƙ is IMyGasTank&&(Ƙ.Components.Get<MyResourceSinkComponent>()?.
AcceptedResources.Contains(ʏ)??φ),$"{ύ}.{ͼ.w}","TankStockpileOff","TankStockpileOn",ͻ,"Off","On"));ͼ=new Ș("Gyroscopes",φ);ͼ.x="Gyros";ͼ.
ȕ="Active";ͼ.Ȕ="Inactive";ɹ.Add(ͼ.w,new ˈ(ͼ.w,ͼ,Ƙ=>Ƙ is IMyGyro,$"{ύ}.{ͼ.w}","EnableOn","EnableOff",ͻ,"Off","On"));ͼ=new
Ș("ThrustersAtmospheric",φ);ͼ.x="Atmospheric\nThrusters";ͼ.ȕ="Online";ͼ.Ȕ="Offline";ɹ.Add(ͼ.w,new ˈ(ͼ.w,ͼ,Ƙ=>Ƙ is
IMyThrust&&(Ƙ.BlockDefinition.SubtypeId.Contains("Atmospheric")),$"{ύ}.{ͼ.w}","EnableOn","EnableOff",ͻ,"Off","On"));ͼ=new Ș(
"ThrustersIon",φ);ͼ.x="Ion\nThrusters";ͼ.ȕ="Online";ͼ.Ȕ="Offline";ɹ.Add(ͼ.w,new ˈ(ͼ.w,ͼ,Ƙ=>Ƙ is IMyThrust&&(!Ƙ.BlockDefinition.
SubtypeId.Contains("Atmospheric")&&!Ƙ.BlockDefinition.SubtypeId.Contains("Hydrogen")),$"{ύ}.{ͼ.w}","EnableOn","EnableOff",ͻ,"Off"
,"On"));ͼ=new Ș("ThrustersHydrogen",φ);ͼ.x="Hydrogen\nThrusters";ͼ.ȕ="Online";ͼ.Ȕ="Offline";ɹ.Add(ͼ.w,new ˈ(ͼ.w,ͼ,Ƙ=>Ƙ is
IMyThrust&&(Ƙ.BlockDefinition.SubtypeId.Contains("Hydrogen")),$"{ύ}.{ͼ.w}","EnableOn","EnableOff",ͻ,"Off","On"));ͼ=new Ș(
"ThrustersGeneric",φ);ͼ.x="Thrusters";ͼ.ȕ="Online";ͼ.Ȕ="Offline";ɹ.Add(ͼ.w,new ˈ(ͼ.w,ͼ,Ƙ=>Ƙ is IMyThrust,$"{ύ}.{ͼ.w}","EnableOn",
"EnableOff",ͻ,"Off","On"));if(ɼ!=null){int Ǡ=0;string Ί;while(Ǡ<ɼ.Count){Ί=ɼ[Ǡ];if(ɹ.ContainsKey(Ί)){ɹ.Remove(Ί);ɼ.RemoveAt(Ǡ);}
else{Ǡ++;}}if(Ǡ>0){string Ή="";foreach(string Έ in ɼ){Ή+=$"{Έ}, ";}Ή=Ή.Remove(Ή.Length-2);ɻ.ƒ(
"The following entries from APExcludedDeclarations could not "+$"be matched to declarations: {Ή}.");}}return ɹ.Values.ToList();}abstract class Ά{public string G{get;private set;}
protected Ř ʽ;Func<IMyTerminalBlock,bool>ˋ;public Ά(string G,Ř ʽ,Func<IMyTerminalBlock,bool>ˋ){this.G=G;this.ʽ=ʽ;this.ˋ=ˋ;}public
string ͳ(){return ʽ.Ţ();}public bool Ͳ(IMyTerminalBlock A){return ˋ(A);}public abstract string ˎ();}class ˍ:Ά{public ˍ(string
G,Ř ʽ,Func<IMyTerminalBlock,bool>ˋ):base(G,ʽ,ˋ){}public override string ˎ(){return"Tally";}}class ˌ:Ά{Func<IMyInventory,
bool>ˊ;public ˌ(string G,Ř ʽ,Func<IMyTerminalBlock,bool>ˋ,Func<IMyInventory,bool>ˊ):base(G,ʽ,ˋ){this.ˊ=ˊ;}public bool ˉ(
IMyInventory Q){return ˊ(Q);}public override string ˎ(){return"Tally";}}class ˈ:Ά{string ˇ,ˆ,ˁ;Action<MyIni,string,string,string>ˀ;
public string ʿ{get;private set;}public string ʾ{get;private set;}public ˈ(string G,Ř ʽ,Func<IMyTerminalBlock,bool>ˋ,string ˢ,
string ˆ,string ˁ,Action<MyIni,string,string,string>ˀ,string ʿ,string ʾ):base(G,ʽ,ˋ){ˇ=ˢ;this.ˆ=ˆ;this.ˁ=ˁ;this.ˀ=ˀ;this.ʿ=ʿ;
this.ʾ=ʾ;}internal string ͱ(bool ï){return ï?ʿ:ʾ;}public void Ͱ(MyIni ˣ){ˀ.Invoke(ˣ,ˇ,ˆ,ˁ);}public string ˮ(){return String.
IsNullOrEmpty(ʿ)?"":$"{G}: {ʿ}";}public string ˬ(){return String.IsNullOrEmpty(ʾ)?"":$"{G}: {ʾ}";}public override string ˎ(){return
"ActionSet";}}class ˤ{internal Dictionary<string,ѻ>K{get;private set;}internal ˈ ʞ;public ˤ(IMyTerminalBlock A,MyIni ˣ,string ˢ){
int ˡ=A.InventoryCount;K=new Dictionary<string,ѻ>();if(ˡ>1){for(int Ǡ=0;Ǡ<ˡ;Ǡ++){ˏ(ˣ,ˢ,$"Inv{Ǡ}Tallies");}}else{ˏ(ˣ,ˢ,
"Tallies");}ˏ(ˣ,ˢ,"ActionSets");ʞ=null;}private void ˠ(string ĝ,string ˑ,bool ː=φ){K.Add(ĝ,new ѻ(ˑ,ː));}private bool ˏ(MyIni ʭ,
string ˢ,string ĝ){if(ʭ.ContainsKey(ˢ,ĝ)){ˠ(ĝ,ʭ.Get(ˢ,ĝ).ToString());return χ;}return φ;}public void Ѹ(string ĝ,string ʻ){if(K
.ContainsKey(ĝ)){K[ĝ].Ѹ(ʻ);}else{ˠ(ĝ,ʻ,χ);}}public void ʥ(string ˢ,MyIni ʭ,bool ѵ=φ){foreach(KeyValuePair<string,ѻ>Ħ in K
){Ħ.Value.Ѷ(ʭ,ˢ,Ħ.Key,ѵ);}if(ʞ!=null){ʞ.Ͱ(ʭ);}}}class ѻ{public string Ѻ{get;private set;}bool ː;public ѻ(string ˑ,bool ѹ=
φ){Ѻ=ˑ;ː=ѹ;}public void Ѹ(string ѷ){if(!Ѻ.Contains(ѷ)){Ѻ=$"{Ѻ}, {ѷ}";ː=χ;}}public void Ѷ(MyIni ʭ,string ˢ,string ĝ,bool ѵ
){if(ː||ѵ){ʭ.Set(ˢ,ĝ,Ѻ);}}}void Ѵ(ź Ė,bool ϓ=φ){StringComparer ɺ=StringComparer.OrdinalIgnoreCase;њ ŀ=new њ(ɺ);Dictionary
<string,y>ѣ=new Dictionary<string,y>(ɺ);Dictionary<string,Ș>Ѩ=new Dictionary<string,Ș>(ɺ);Dictionary<string,Ȣ>Ѣ=new
Dictionary<string,Ȣ>(ɺ);Dictionary<string,Ȅ>Ѡ=new Dictionary<string,Ȅ>(ɺ);Dictionary<IMyInventory,List<O>>Ґ=new Dictionary<
IMyInventory,List<O>>();List<ɱ>ҏ=new List<ɱ>();List<Ƶ>Ҏ=new List<Ƶ>();Dictionary<string,ɪ>ҍ=new Dictionary<string,ɪ>(ɺ);Dictionary<
string,ǆ>Ҍ=new Dictionary<string,ǆ>(ɺ);HashSet<string>џ=new HashSet<string>(ɺ);string ҋ="";MyIniParseResult ϕ;MyIniValue ė=new
MyIniValue();int Ҋ=-1;τ.TryParse(Storage);int Λ=τ.Get("Data","UpdateDelay").ToInt32(0);int ҁ=-1;if(!ϐ.TryParse(Me.CustomData,out ϕ
)){Ė.Ɨ($"The parser encountered an error on line {ϕ.LineNo} of the "+$"Programmable Block's config: {ϕ.Error}");}else{Ѽ(ŀ
,Ė,ė,out ҁ);ѥ(Me,Ė,ŀ,ѣ,Ѩ,Ѣ,Ѡ,џ,ϕ,ė);if(Ė.ƕ()>0){Ė.ƒ("Errors in Programmable Block configuration have prevented grid "+
"configuration from being evaluated.");}else{ҋ=л();Ҋ=ү(Ė,ŀ,ѣ,Ѩ,Ѣ,Ѡ,Ґ,ҍ,ҏ,Ҏ,Ҍ,ϕ,ė);}}if(ι==null||Ė.ƕ()==0||Ҏ.Count>=ι.Count){ι=Ҏ;}string ħ=
"Evaluation complete.\n";if(Ė.ƕ()>0){ħ+="Errors prevent the use of this configuration. ";if(Ϣ){Runtime.UpdateFrequency=UpdateFrequency.Update100
;ħ+=$"Execution continuing with last good configuration from "+$"{(DateTime.Now-ϡ).Minutes} minutes ago "+
$"({ϡ.ToString("HH: mm: ss")}).\n";}else{Runtime.UpdateFrequency=UpdateFrequency.None;ħ+=
"Because there is no good configuration loaded, the script has been halted.\n";}ħ+=$"\nThe following errors are preventing the use of this config:\n{Ė.Ɠ()}";}else{o Ў;int æ=0;ρ=new o[Ґ.Count];
foreach(IMyInventory Q in Ґ.Keys){Ў=new o(Q,Ґ[Q].ToArray());Ў.m();ρ[æ]=Ў;æ++;}π=ѣ.Values.ToArray();ξ=Ѣ.Values.ToArray();μ=ҏ.
ToArray();λ=Ҍ.Values.ToArray();ο=Ѩ;ν=Ѡ;κ=ҍ;foreach(y Ҁ in π){Ҁ.Î();}foreach(ɱ ɤ in μ){ɤ.ɰ();}ϑ?.Р();ϑ=null;Ϥ.Clear();θ.ı();ſ(Λ)
;if(ҁ>-1){if(ҁ<10){Ė.ƒ($"{ύ}.Init, key 'MPSpriteSyncFrequency' "+
$"requested an invalid frequncy of {ҁ}. Sync frequency has "+$"been set to the lowest allowed value of 10 instead.");ҁ=10;}Action ѿ=()=>{Ͼ(new Ч(this,μ,φ));};ĥ Ѿ=new ĥ(ҁ,ѿ);θ.ď(
"SpriteRefresher",Ѿ);}Ϣ=χ;ϡ=DateTime.Now;Ϡ=ҋ;ħ+=$"Script is now running. Registered {π.Length} tallies, "+
$"{ο.Count} ActionSets, {ξ.Length} triggers, and {μ.Length} "+$"reports, as configured by data on {Ҋ} blocks. Evaluation used "+
$"{Runtime.CurrentInstructionCount} / {Runtime.MaxInstructionCount} "+$"({(int)(((double)Runtime.CurrentInstructionCount/Runtime.MaxInstructionCount)*100)}%) "+
$"of instructions allowed in this tic.\n";Runtime.UpdateFrequency=UpdateFrequency.Update100;}if(Ė.Ƌ()>0){ħ+=$"\nThe following messages were logged:\n{Ė.Ɖ()}";}if
(Ė.Ƒ()>0){ħ+=$"\nThe following warnings were logged:\n{Ė.Ə()}";}υ.ǐ(ħ);foreach(Ƶ ѽ in ι){ѽ.ǂ();}ϐ.Clear();τ.Clear();}void
Ѽ(њ ŀ,ź Ė,MyIniValue ė,out int ҁ){ϔ(Ė);Color Ĕ;string ϒ=$"{ύ}.Init";Action<string>ц=Ň=>{Ė.Ɨ($"{ϒ}{Ň}");};Ũ ʎ=(Ũ)(ŀ.ь(
"LowGood"));ŧ ʍ=(ŧ)(ŀ.ь("HighGood"));Action<string>ѧ=Ѧ=>{if(ŀ.ъ(ц,ϐ,ϒ,$"Color{Ѧ}",out Ĕ)){ʎ.Ū(Ѧ,Ĕ);ʍ.Ū(Ѧ,Ĕ);}};ѧ("Optimal");ѧ(
"Normal");ѧ("Caution");ѧ("Warning");ѧ("Critical");ҁ=ϐ.Get(ϒ,"MPSpriteSyncFrequency").ToInt32(-1);}void ѥ(IMyTerminalBlock Ѥ,ź Ė,
њ ŀ,Dictionary<string,y>ѣ,Dictionary<string,Ș>Ѩ,Dictionary<string,Ȣ>Ѣ,Dictionary<string,Ȅ>Ѡ,HashSet<string>џ,
MyIniParseResult ϕ,MyIniValue ė){string ў="";string ѝ="";Color Ĕ=Color.Black;Ũ ʎ=(Ũ)(ŀ.ь("LowGood"));ŧ ʍ=(ŧ)(ŀ.ь("HighGood"));Ŕ D;List<
string>ќ=new List<string>();List<Ç>ѡ=new List<Ç>();Action<string>ћ=Ƙ=>Ė.Ɨ(Ƙ);Action<string>ѳ=Ň=>{Ė.Ɨ($"{ў} {ѝ}{Ň}");};
StringComparison Ѳ=StringComparison.OrdinalIgnoreCase;List<string>ѱ=new List<string>();List<string>Ѱ=new List<string>();List<string>ѯ=
new List<string>();string[]Ѯ;ϐ.GetSections(ѯ);foreach(string Ĵ in ѯ){Ѯ=Ĵ.Split('.').Select(Ļ=>Ļ.Trim()).ToArray();if(Ѯ.
Length==4&&String.Equals(Ѯ[0],ύ,Ѳ)){ў=Ѯ[2];ѝ=Ѯ[3];if(ў.Equals("Tally",Ѳ)){y k=null;string ѭ;ѭ=ϐ.Get(Ĵ,"Type").ToString().
ToLowerInvariant();if(Ѐ(ѭ)){Ė.Ɨ($"{ў} {ѝ} has a missing or unreadable Type.");}else if(ѭ=="inventory"){k=new O(ς,ѝ,ʎ);}else if(ѭ=="item"
){string F,E;F=ϐ.Get(Ĵ,"ItemTypeID").ToString();if(Ѐ(F)){Ė.Ɨ($"{ў} {ѝ} has a missing or unreadable ItemTypeID.");}E=ϐ.Get
(Ĵ,"ItemSubTypeID").ToString();if(Ѐ(E)){Ė.Ɨ($"{ў} {ѝ} has a missing or unreadable ItemSubTypeID.");}if(!Ѐ(F)&&!Ѐ(E)){k=
new J(ς,ѝ,F,E,ʍ);}}else if(ѭ=="battery"){k=new Ç(ς,ѝ,new Ʀ(),ʍ);}else if(ѭ=="gas"){k=new Ç(ς,ѝ,new ƣ(),ʍ);}else if(ѭ==
"jumpdrive"){k=new Ç(ς,ѝ,new ơ(),ʍ);}else if(ѭ=="raycast"){k=new Ç(ς,ѝ,new ư(),ʍ);ќ.Add(Ĵ);ѡ.Add((Ç)k);}else if(ѭ=="powermax"){k=
new Ç(ς,ѝ,new ƫ(),ʍ);}else if(ѭ=="powercurrent"){k=new Ç(ς,ѝ,new Ʃ(),ʍ);}else if(ѭ=="integrity"){k=new Ç(ς,ѝ,new Ǯ(),ʍ);}
else if(ѭ=="ventpressure"){k=new Ç(ς,ѝ,new ǭ(),ʍ);}else if(ѭ=="pistonextension"){k=new Ç(ς,ѝ,new ǫ(),ʍ);}else if(ѭ==
"rotorangle"){k=new Ç(ς,ѝ,new ǲ(),ʍ);}else if(ѭ=="controllergravity"){k=new Ç(ς,ѝ,new ǯ(),ʍ);}else if(ѭ=="controllerspeed"){k=new Ç(
ς,ѝ,new ǰ(),ʍ);}else if(ѭ=="controllerweight"){k=new Ç(ς,ѝ,new Ǵ(),ʍ);}else{Ė.Ɨ(
$"{ў} {ѝ} has un-recognized Type of '{ѭ}'.");}if(k==null){k=new O(ς,ѝ,ʎ);}ė=ϐ.Get(Ĵ,"DisplayName");if(!ė.IsEmpty){k.x=ė.ToString();}ė=ϐ.Get(Ĵ,"Multiplier");if(!ė.
IsEmpty){k.B=ė.ToDouble();}ė=ϐ.Get(Ĵ,"Max");if(!ė.IsEmpty){k.Ì(ė.ToDouble());}else if(ė.IsEmpty&&(k is J||(k is Ç&&((Ç)k).Æ is
Ǵ))){Ė.Ɨ($"{ў} {ѝ}'s TallyType of '{ѭ}' requires a Max "+$"to be set in configuration.");}if(ŀ.ч(ѳ,ϐ,Ĵ,"ColorCoder",out D
)){k.D=D;}if(!ѕ(џ,k.w,Ĵ,Ė)){ѣ.Add(k.w,k);џ.Add(k.w);}}else if(ў.Equals("ActionSet",Ѳ)){bool я=τ?.Get("ActionSets",ѝ).
ToBoolean(φ)??φ;Ș Κ=new Ș(ѝ,я);ė=ϐ.Get(Ĵ,"DisplayName");if(!ė.IsEmpty){Κ.x=ė.ToString();}if(ŀ.ъ(ѳ,ϐ,Ĵ,"ColorOn",out Ĕ)){Κ.Ȏ=Ĕ;}if
(ŀ.ъ(ѳ,ϐ,Ĵ,"ColorOff",out Ĕ)){Κ.ȍ=Ĕ;}ė=ϐ.Get(Ĵ,"TextOn");if(!ė.IsEmpty){Κ.ȕ=ė.ToString();}ė=ϐ.Get(Ĵ,"TextOff");if(!ė.
IsEmpty){Κ.Ȕ=ė.ToString();}if(!ѕ(џ,Κ.w,Ĵ,Ė)){Κ.Ƕ();Ѩ.Add(Κ.w,Κ);џ.Add(Κ.w);}}else if(ў.Equals("Trigger",Ѳ)){bool я=τ?.Get(
"Triggers",ѝ).ToBoolean(χ)??χ;Ȣ ě=new Ȣ(ѝ,я);if(!ѕ(џ,ě.w,Ĵ,Ė)){Ѣ.Add(ě.w,ě);џ.Add(ě.w);}}else if(ў.Equals("Raycaster",Ѳ)){Ȅ ƭ=new
Ȅ(Ƃ,ѝ);ɞ ȋ=null;string[]Ѭ=null;double[]ѫ=null;string Ѫ=ϐ.Get(Ĵ,"Type").ToString().ToLowerInvariant();if(Ѐ(Ѫ)){Ė.Ɨ(
$"{ў} {ѝ} has a missing or unreadable Type.");}else if(Ѫ=="linear"){ȋ=new ɚ();Ѭ=ɚ.ɖ();ѫ=new double[Ѭ.Length];}else{Ė.Ɨ($"{ў} {ѝ} has un-recognized Type of '{Ѫ}'.");
}if(ȋ!=null){for(int Ǡ=0;Ǡ<Ѭ.Length;Ǡ++){ѫ[Ǡ]=ϐ.Get(Ĵ,Ѭ[Ǡ]).ToDouble(-1);}ȋ.ɛ(ѫ);ƭ=new Ȅ(Ƃ,ȋ,ѝ);}else{Ѱ.Add(ѝ);}if(!ѕ(џ,ѝ
,Ĵ,Ė)){Ѡ.Add(ƭ.w,ƭ);џ.Add(ѝ);}}else{if(Ѯ[1]==ό){Ė.Ɨ($"{Ĵ} referenced the unknown declaration "+$"type '{ў}'.");}else{Ė.ƒ(
$"{Ĵ} has the format of a declaration "+$"header but lacks the '{ό}' prefix and has been "+$"discarded.");}}}}ў="Raycaster";for(int Ǡ=0;Ǡ<ѡ.Count;Ǡ++){string Ĵ
=ќ[Ǡ];Ç ѩ=ѡ[Ǡ];ė=ϐ.Get(Ĵ,"Raycaster");if(ė.IsEmpty){if(!ѩ.u){Ė.Ɨ($"{ў} {ѩ.w}'s "+
$"Type of 'Raycaster' requires either a Max or a linked Raycaster to "+$"be set in configuration.");}}else{string α=ė.ToString();if(ѩ.u){Ė.ƒ($"{ў} {ѩ.w} specifies "+
$"both a Max and a linked Raycaster, '{α}'. Only one of these "+$"values is required. The linked Raycaster has been ignored.");}else{Ȅ ƭ;if(Ѡ.TryGetValue(α,out ƭ)){ѩ.Ì(ƭ.ȉ());}else{Ė.
Ɨ($"{ў} {ѩ.w} tried "+$"to reference the unconfigured Raycaster '{α}'.");}}}}ў="Trigger";foreach(Ȣ ě in Ѣ.Values){y k=
null;Ș Κ=null;ѝ=ě.w;string Ĵ=$"{ύ}.{ό}.Trigger.{ѝ}";ė=ϐ.Get(Ĵ,"Tally");if(!ė.IsEmpty){string ҫ=ė.ToString();if(ѣ.TryGetValue
(ҫ,out k)){ě.ȁ=k;}else{Ė.Ɨ($"{ў} {ѝ} tried to reference "+$"the unconfigured Tally '{ҫ}'.");}}else{Ė.Ɨ(
$"{ў} {ѝ} has a missing or unreadable Tally.");}ė=ϐ.Get(Ĵ,"ActionSet");if(!ė.IsEmpty){string Ҫ=ė.ToString();if(Ѩ.TryGetValue(Ҫ,out Κ)){ě.Ȁ=Κ;}else{Ė.Ɨ(
$"{ў} {ѝ} tried to reference "+$"the unconfigured ActionSet '{Ҫ}'.");}}else{Ė.Ɨ($"{ў} {ѝ} has a missing or unreadable ActionSet.");}Ĝ(ě,Ĵ,χ,
"LessOrEqual",ė,Ė);Ĝ(ě,Ĵ,φ,"GreaterOrEqual",ė,Ė);if(!ě.ǵ()){Ė.Ɨ($"{ў} {ѝ} does not define a valid "+
$"LessOrEqual or GreaterOrEqual scenario.");}if(k==null||Κ==null){ѱ.Add(ѝ);}}List<KeyValuePair<string,bool>>ҩ=new List<KeyValuePair<string,bool>>();ў="ActionSet";
foreach(Ș Κ in Ѩ.Values){ѝ=Κ.w;string Ĵ=$"{ύ}.{ό}.ActionSet.{ѝ}";string Ҩ=$"{ў} {ѝ}";string ф,ё;Ș Ȁ=null;Ȣ Ҭ=null;Ȅ ҧ=null;int
í=ϐ.Get(Ĵ,$"DelayOn").ToInt32();int ì=ϐ.Get(Ĵ,$"DelayOff").ToInt32();if(í!=0||ì!=0){î ʟ=new î(this);ʟ.í=í;ʟ.ì=ì;Κ.Ȓ(ʟ);}ė
=ϐ.Get(Ĵ,$"IGCChannel");if(!ė.IsEmpty){string ç=ė.ToString();è Ҧ=new è(IGC,ç);ė=ϐ.Get(Ĵ,$"IGCMessageOn");if(!ė.IsEmpty){Ҧ
.ƙ=ė.ToString();}ė=ϐ.Get(Ĵ,$"IGCMessageOff");if(!ė.IsEmpty){Ҧ.ș=ė.ToString();}if(Ҧ.X()){Κ.Ȓ(Ҧ);}else{Ė.Ɨ(
$"{Ҩ} has configuration for sending an IGC message "+$"on the channel '{ç}', but does not have readable config on what "+$"messages should be sent.");}}ф=
"ActionSetsLinkedToOn";ė=ϐ.Get(Ĵ,ф);if(!ė.IsEmpty){ё=$"{Ҩ}'s {ф} list";ѓ(ė.ToString(),ё,ћ,ҩ);foreach(KeyValuePair<string,bool>Ħ in ҩ){if(Ѩ.
TryGetValue(Ħ.Key,out Ȁ)){à ҥ=new à(Ȁ);ҥ.Ø(Ħ.Value);Κ.Ȓ(ҥ);}else{Ė.Ɨ($"{ё} references the unconfigured ActionSet {Ħ.Key}.");}}}ф=
"ActionSetsLinkedToOff";ė=ϐ.Get(Ĵ,ф);if(!ė.IsEmpty){ё=$"{Ҩ}'s {ф} list";ѓ(ė.ToString(),ё,ћ,ҩ);foreach(KeyValuePair<string,bool>Ħ in ҩ){if(Ѩ.
TryGetValue(Ħ.Key,out Ȁ)){à ҥ=new à(Ȁ);ҥ.Ö(Ħ.Value);Κ.Ȓ(ҥ);}else{Ė.Ɨ($"{ё} references the unconfigured ActionSet {Ħ.Key}.");}}}ф=
"TriggersLinkedToOn";ė=ϐ.Get(Ĵ,ф);if(!ė.IsEmpty){ё=$"{Ҩ}'s {ф} list";ѓ(ė.ToString(),ё,ћ,ҩ);foreach(KeyValuePair<string,bool>Ħ in ҩ){if(Ѣ.
TryGetValue(Ħ.Key,out Ҭ)){ñ Ҥ=new ñ(Ҭ);Ҥ.Ø(Ħ.Value);Κ.Ȓ(Ҥ);}else{Ė.Ɨ($"{ё} references the unconfigured ActionSet {Ħ.Key}.");}}}ф=
"TriggersLinkedToOff";ė=ϐ.Get(Ĵ,ф);if(!ė.IsEmpty){ё=$"{Ҩ}'s {ф} list";ѓ(ė.ToString(),ё,ћ,ҩ);foreach(KeyValuePair<string,bool>Ħ in ҩ){if(Ѣ.
TryGetValue(Ħ.Key,out Ҭ)){ñ Ҥ=new ñ(Ҭ);Ҥ.Ö(Ħ.Value);Κ.Ȓ(Ҥ);}else{Ė.Ɨ($"{ё} references the unconfigured ActionSet {Ħ.Key}.");}}}ф=
"RaycastPerformedOnState";ė=ϐ.Get(Ĵ,ф);if(!ė.IsEmpty){ё=$"{Ҩ}'s {ф} list";ѓ(ė.ToString(),ё,ћ,ҩ);foreach(KeyValuePair<string,bool>Ħ in ҩ){if(Ѡ.
TryGetValue(Ħ.Key,out ҧ)){ä Ҳ=new ä(ҧ);if(Ħ.Value){Ҳ.â=χ;}else{Ҳ.á=χ;}Κ.Ȓ(Ҳ);}else{Ė.Ɨ(
$"{ё} references the unconfigured Raycaster {Ħ.Key}.");}}}}foreach(string ұ in ѱ){Ѣ.Remove(ұ);}foreach(string Ұ in Ѱ){Ѣ.Remove(Ұ);}if(Ė.ƕ()==0&&ѣ.Count==0&&Ѩ.Count==0){Ė.Ɨ(
$"No readable configuration found on the programmable block.");}}int ү(ź Ė,њ ŀ,Dictionary<string,y>ѣ,Dictionary<string,Ș>Ѩ,Dictionary<string,Ȣ>Ѣ,Dictionary<string,Ȅ>Ѡ,Dictionary<
IMyInventory,List<O>>Ґ,Dictionary<string,ɪ>ҍ,List<ɱ>ҏ,List<Ƶ>Ҏ,Dictionary<string,ǆ>Ҍ,MyIniParseResult ϕ,MyIniValue ė){List<
IMyTerminalBlock>ϱ=new List<IMyTerminalBlock>();Dictionary<string,Action<IMyTerminalBlock>>ù=е();List<KeyValuePair<string,bool>>ѐ=new
List<KeyValuePair<string,bool>>();Action<string>Ү=Ƙ=>Ė.ƒ(Ƙ);y k;Ș ͼ;Color Ĕ=Color.White;string[]є;string ҭ="";string Ĵ="";
string ң="";string ё="";int æ=0;bool җ;Ϭ<IMyTerminalBlock>(ϱ,Ƙ=>(Ƙ.IsSameConstructAs(Me)&&MyIni.HasSection(Ƙ.CustomData,ϊ)));
if(ϱ.Count<=0){Ė.Ɨ($"No blocks found on this construct with a {ϊ} INI section.");}foreach(IMyTerminalBlock A in ϱ){Action<
string>Җ=Ň=>{Ė.ƒ($"Block {A}, section {Ĵ}{Ň}");};if(!ϐ.TryParse(A.CustomData,out ϕ)){Ė.ƒ(
$"Configuration on block '{A.CustomName}' has been "+$"ignored because of the following parsing error on line {ϕ.LineNo}: "+$"{ϕ.Error}");}else{җ=φ;if(ϐ.ContainsKey(ϊ,
"Tallies")){җ=χ;ė=ϐ.Get(ϊ,"Tallies");є=ė.ToString().Split(',').Select(Ļ=>Ļ.Trim()).ToArray();foreach(string G in є){if(!ѣ.
ContainsKey(G)){Ė.ƒ($"Block '{A.CustomName}' tried to reference the "+$"unconfigured Tally '{G}'.");}else{k=ѣ[G];if(k is O){if(!A.
HasInventory){Ė.ƒ($"Block '{A.CustomName}' does not have an "+$"inventory and is not compatible with the Type of "+$"Tally '{G}'.");
}else{for(int Ǡ=0;Ǡ<A.InventoryCount;Ǡ++){IMyInventory Q=A.GetInventory(Ǡ);if(!Ґ.ContainsKey(Q)){Ґ.Add(Q,new List<O>());}
Ґ[Q].Add((O)k);}}}else if(k is Ç){if(!((Ç)k).R(A)){Ė.ƒ($"Block '{A.CustomName}' is not a "+$"{((Ç)k).P()} and is not "+
$"compatible with the Type of Tally '{G}'.");}}else{Ė.ƒ($"Block '{A.CustomName}' refrenced the Tally "+$"'{G}', which has an unhandled Tally Type. Complain to "+
$"the script writer, this should be impossible.");}}}}if(A.HasInventory){for(int Ǡ=0;Ǡ<A.InventoryCount;Ǡ++){if(!ϐ.ContainsKey(ϊ,$"Inv{Ǡ}Tallies")){}else{җ=χ;ė=ϐ.Get(ϊ,
$"Inv{Ǡ}Tallies");є=ė.ToString().Split(',').Select(Ļ=>Ļ.Trim()).ToArray();foreach(string G in є){if(!ѣ.ContainsKey(G)){Ė.ƒ(
$"Block '{A.CustomName}' tried to reference the "+$"unconfigured Tally '{G}' in key Inv{Ǡ}Tallies.");}else{k=ѣ[G];if(!(k is O)){Ė.ƒ(
$"Block '{A.CustomName}' is not compatible "+$"with the Type of Tally '{G}' referenced in key "+$"Inv{Ǡ}Tallies.");}else{IMyInventory Q=A.GetInventory(Ǡ);if(!Ґ.
ContainsKey(Q)){Ґ.Add(Q,new List<O>());}Ґ[Q].Add((O)k);}}}}}}if(ϐ.ContainsKey(ϊ,"ActionSets")){җ=χ;ė=ϐ.Get(ϊ,"ActionSets");є=ė.
ToString().Split(',').Select(Ļ=>Ļ.Trim()).ToArray();foreach(string G in є){if(!Ѩ.ContainsKey(G)){Ė.ƒ(
$"Block '{A.CustomName}' tried to reference the "+$"unconfigured ActionSet '{G}'.");}else{ͼ=Ѩ[G];Ĵ=$"{ύ}.{G}";if(!ϐ.ContainsSection(Ĵ)){Ė.ƒ(
$"Block '{A.CustomName}' references the ActionSet "+$"'{G}', but contains no discrete '{Ĵ}' section that would "+$"define actions.");}else{c ҕ=null;if(ϐ.ContainsKey(Ĵ,
"Action0Property")){ú Ҕ=new ú(A);õ ņ=null;æ=0;while(æ!=-1){ņ=ĵ(Ė,Ĵ,æ,A,ϐ,ė,ŀ);if(ņ!=null){Ҕ.ø(ņ);æ++;}else{æ=-1;}}ҕ=Ҕ;}else if(ϐ.
ContainsKey(Ĵ,"ActionsOn")||ϐ.ContainsKey(Ĵ,"ActionsOff")){þ ғ=new þ(A);ғ.ý=ƽ(ϐ,Ĵ,"ActionsOn",ù,Ė,A);ғ.ü=ƽ(ϐ,Ĵ,"ActionsOff",ù,Ė,A);
ҕ=ғ;}else if(ϐ.ContainsKey(Ĵ,"ActionOn")||ϐ.ContainsKey(Ĵ,"ActionOff")){V Ғ=new V(A);ė=ϐ.Get(Ĵ,"ActionOn");if(!ė.IsEmpty)
{Ғ.Ó=Ŀ(ė.ToString(),ù,Ė,A,Ĵ,"ActionOn");}ė=ϐ.Get(Ĵ,"ActionOff");if(!ė.IsEmpty){Ғ.S=Ŀ(ė.ToString(),ù,Ė,A,Ĵ,"ActionOff");}ҕ
=Ғ;}if(ҕ?.X()??φ){ͼ.Ȓ(ҕ);}else{Ė.ƒ($"Block '{A.CustomName}', discrete section '{Ĵ}', "+
"does not define any actions to be taken when the ActionSet changes state. "+"If you're listing Terminal Actions, make sure you're starting at index 0.");}}}}}if(A is IMyCameraBlock){if(ϐ.
ContainsKey(ϊ,"Raycasters")){җ=χ;ė=ϐ.Get(ϊ,"Raycasters");є=ė.ToString().Split(',').Select(Ļ=>Ļ.Trim()).ToArray();foreach(string G
in є){if(!Ѡ.ContainsKey(G)){Ė.ƒ($"Camera '{A.CustomName}' tried to reference the "+$"unconfigured Raycaster '{G}'.");}else
{Ѡ[G].Ȋ((IMyCameraBlock)A);}}}}if(A is IMyTextSurfaceProvider){IMyTextSurfaceProvider ґ=(IMyTextSurfaceProvider)A;for(int
Ǡ=0;Ǡ<ґ.SurfaceCount;Ǡ++){ң=$"Surface{Ǡ}Pages";if(ϐ.ContainsKey(ϊ,ң)){IMyTextSurface ƴ=ґ.GetSurface(Ǡ);ɪ Ι=null;ɱ ɤ=null;
җ=χ;ė=ϐ.Get(ϊ,ң);є=ė.ToString().Split(',').Select(Ļ=>Ļ.Trim()).ToArray();if(є.Length>1){string Ң=$"Surface{Ǡ}MFD";if(!ϐ.
ContainsKey(ϊ,Ң)){Ė.ƒ($"Surface provider '{A.CustomName}', key {ң} "+$"references multiple pages which must be managed by an MFD, "
+$"but has no {Ң} key to define that object's name.");}else{string ҡ=ϐ.Get(ϊ,Ң).ToString();if(ҍ.ContainsKey(ҡ)){Ė.ƒ(
$"Surface provider '{A.CustomName}', key {Ң} "+$"declares the MFD '{ҡ}' but this name is already in use.");}else{Ι=new ɪ(ҡ);}}}foreach(string G in є){Ĵ=$"{ύ}.{G}";if(
!ϐ.ContainsSection(Ĵ)){Ė.ƒ($"Surface provider '{A.CustomName}', key {ң} declares the "+
$"page '{G}', but contains no discrete '{Ĵ}' section that would "+$"configure that page.");}else{ɤ=null;if(ϐ.ContainsKey(Ĵ,"Elements")){ė=ϐ.Get(Ĵ,"Elements");ȷ Ȍ=null;List<À>Ҡ=new List<
À>();string[]Ş=ė.ToString().Split(',').Select(Ļ=>Ļ.Trim()).ToArray();foreach(string ř in Ş){if(ř.ToLowerInvariant()==
"blank"){Ҡ.Add(null);}else{if(ѣ.ContainsKey(ř)){Ҡ.Add(ѣ[ř]);}else if(Ѩ.ContainsKey(ř)){Ҡ.Add(Ѩ[ř]);}else if(Ѣ.ContainsKey(ř)){Ҡ
.Add(Ѣ[ř]);}else{Ė.ƒ($"Surface provider '{A.CustomName}', "+$"section {Ĵ} tried to reference the "+
$"unconfigured element '{ř}'.");}}}Ȍ=new ȷ(ƴ,Ҡ);ė=ϐ.Get(Ĵ,"Title");if(!ė.IsEmpty){Ȍ.Ǔ=ė.ToString();}ė=ϐ.Get(Ĵ,"FontSize");if(!ė.IsEmpty){Ȍ.Ƴ=ė.
ToSingle();}ė=ϐ.Get(Ĵ,"Font");if(!ė.IsEmpty){Ȍ.Ƹ=ė.ToString();}Func<string,float>ҟ=(Ҟ)=>{return(float)(ϐ.Get(Ĵ,$"Padding{Ҟ}").
ToDouble(0));};float ȵ=ҟ("Left");float Ȱ=ҟ("Right");float Ȼ=ҟ("Top");float ɐ=ҟ("Bottom");Func<string,float,string,float,bool>ҝ=(
Ҝ,қ,Қ,ҙ)=>{if(қ+ҙ>100){Ė.ƒ($"Surface provider '{A.CustomName}', "+$"section {Ĵ} has padding values in excess "+
$"of 100% for edges {Ҝ} and {Қ} "+$"which have been ignored.");return χ;}return φ;};if(ҝ("Left",ȵ,"Right",Ȱ)){ȵ=0;Ȱ=0;}if(ҝ("Top",Ȼ,"Bottom",ɐ)){Ȼ=0;ɐ=0;
}int ȱ=ϐ.Get(Ĵ,"Columns").ToInt32(1);bool Ɏ=ϐ.Get(Ĵ,"TitleObeysPadding").ToBoolean(φ);Ȍ.Ȳ(ȱ,ȵ,Ȱ,Ȼ,ɐ,Ɏ,Ƃ);ɤ=Ȍ;}else if(ϐ.
ContainsKey(Ĵ,"Script")){ȹ ϥ=new ȹ(ƴ,ϐ.Get(Ĵ,"Script").ToString());ɤ=ϥ;}else if(ϐ.ContainsKey(Ĵ,"DataType")){string Ҙ=ϐ.Get(Ĵ,
"DataType").ToString().ToLowerInvariant();ȿ ǎ=null;if(Ҙ=="log"){ǎ=new ƹ(υ);}else if(Ҙ=="storage"){ǎ=new Ʒ(this);}else if(Ҙ==
"customdata"||Ҙ=="detailinfo"||Ҙ=="custominfo"){if(!ϐ.ContainsKey(Ĵ,"DataSource")){Ė.ƒ(
$"Surface provider '{A.CustomName}', section "+$"{Ĵ} has a DataType of {Ҙ}, but a missing or "+$"unreadable DataSource.");}else{string ϫ=ϐ.Get(Ĵ,"DataSource").
ToString();IMyTerminalBlock Ù=GridTerminalSystem.GetBlockWithName(ϫ);if(Ù!=null&&Ҙ=="customdata"){ǎ=new Ƚ(Ù);}else if(Ù!=null&&Ҙ
=="detailinfo"){ǎ=new ƻ(Ù);}else if(Ù!=null&&Ҙ=="custominfo"){ǎ=new ƺ(Ù);}else{Ė.ƒ(
$"Surface provider '{A.CustomName}', section "+$"{Ĵ} tried to reference the unknown block '{ϫ}' "+$"as a DataSource.");}}}else if(Ҙ=="raycaster"){if(!ϐ.ContainsKey(Ĵ,
"DataSource")){Ė.ƒ($"Surface provider '{A.CustomName}', section "+$"{Ĵ} has a DataType of {Ҙ}, but a missing or "+
$"unreadable DataSource.");}else{string ϫ=ϐ.Get(Ĵ,"DataSource").ToString();if(Ѡ.ContainsKey(ϫ)){ǎ=new ƶ(Ѡ[ϫ]);}else{Ė.ƒ(
$"Surface provider '{A.CustomName}', section "+$"{Ĵ} tried to reference the unknown Raycaster "+$"'{ϫ}' as a DataSource.");}}}else{Ė.ƒ(
$"Surface provider '{A.CustomName}', section "+$"{Ĵ} tried to reference the unknown data type '{Ҙ}'.");}if(ǎ!=null){Ƶ М=new Ƶ(ƴ,ǎ,Ƃ);ė=ϐ.Get(Ĵ,"FontSize");if(!ė.
IsEmpty){М.Ƴ=ė.ToSingle();}ė=ϐ.Get(Ĵ,"Font");if(!ė.IsEmpty){М.Ƹ=ė.ToString();}ė=ϐ.Get(Ĵ,"CharPerLine");if(!ė.IsEmpty){if(Ҙ==
"detailinfo"||Ҙ=="custominfo"){Ė.ƒ($"Surface provider '{A.CustomName}', section "+
$"{Ĵ} tried to set a CharPerLine limit with the {Ҙ} "+$"DataType. This is not allowed.");}else{М.Ǎ(ė.ToInt32());}}if(Ҙ=="log"){Ҏ.Add(М);}ɤ=М;}}if(ɤ!=null){if(ŀ.ъ(Җ,ϐ,Ĵ,
"ForeColor",out Ĕ)){((ɭ)ɤ).ɬ=Ĕ;}if(ŀ.ъ(Җ,ϐ,Ĵ,"BackColor",out Ĕ)){((ɭ)ɤ).ɫ=Ĕ;}}}if(Ι!=null&&ɤ!=null){ė=ϐ.Get(Ĵ,"ShowOnActionState");
if(!ė.IsEmpty){ё=$"Surface provider '{A.CustomName}', section {Ĵ}";ѓ(ė.ToString(),ё,Ү,ѐ);foreach(KeyValuePair<string,bool>
Ħ in ѐ){if(!Ѩ.TryGetValue(Ħ.Key,out ͼ)){Ė.ƒ($"{ё} tried to reference the unconfigured ActionSet {Ħ.Key}.");}else{ā Я=new
ā(Ι);if(Ħ.Value){Я.Ċ=G;}else{Я.ò=G;}ͼ.Ȓ(Я);}}}Ι.ɥ(G,ɤ);}}if(Ι!=null){if(Ι.ɣ()==0){Ė.ƒ(
$"Surface provider '{A.CustomName}' specified "+$"the use of MFD '{Ι.w}' but did not provide readable "+$"page configuration for that MFD.");}else{ҍ.Add(Ι.w,Ι);ɤ=Ι;Ι.ɠ
(τ.Get("MFDs",Ι.w).ToString());}}if(ɤ!=null){ҏ.Add(ɤ);}}}}if(A is IMyLightingBlock){ė=ϐ.Get(ϊ,"IndicatorElement");if(!ė.
IsEmpty){ҭ=ė.ToString();À ř=null;if(ѣ.ContainsKey(ҭ)){ř=ѣ[ҭ];}else if(Ѩ.ContainsKey(ҭ)){ř=Ѩ[ҭ];}else if(Ѣ.ContainsKey(ҭ)){ř=Ѣ[ҭ
];}else{Ė.ƒ($"Lighting block '{A.CustomName}' tried to reference "+$"the unconfigured element '{ҭ}'.");}if(ř!=null){if(!Ҍ
.ContainsKey(ҭ)){Ҍ.Add(ҭ,new ǆ(ř));}Ҍ[ҭ].ǃ((IMyLightingBlock)A);}}else if(!җ){Ė.ƒ(
$"Lighting block {A.CustomName} has missing or unreadable "+$"IndicatorElement.");}җ=χ;}if(!җ){Ė.ƒ($"Block '{A.CustomName}' is missing proper configuration or is a "+
$"block type that cannot be handled by this script.");}}}return ϱ.Count;}abstract class Ю{public string Э{get;private set;}protected int Ь{get;private set;}protected
MyGridProgram ë{get;private set;}protected IEnumerator<string>Ы;public int Ъ{get;private set;}public int Щ{get;private set;}public
string Ш{get;protected set;}public bool а{get;private set;}public Ю(MyGridProgram ë,string ϼ,double Ц,bool Х){this.ë=ë;Э=ϼ;Ь=(
int)(ë.Runtime.MaxInstructionCount*Ц);Ъ=0;Щ=0;Ш=$"{Э} waiting to begin";а=Х;}internal abstract void Ф();internal bool У(){
return Ы.MoveNext();}protected bool Т(){if(ë.Runtime.CurrentInstructionCount>Ь){С();return χ;}else{return φ;}}protected void С
(){Ъ++;Щ+=ë.Runtime.CurrentInstructionCount;}internal void Р(){Ы.Dispose();Ш=$"{Э} completed.";}internal abstract string
П();protected string О(){return$"{Э} used a total of {Щ} / {ë.Runtime.MaxInstructionCount} "+
$"({(int)(((double)Щ/ë.Runtime.MaxInstructionCount)*100)}%) "+$"of instructions allowed in one tic, distributed over {Ъ} tics.";}}class Ч:Ю{const int Н=20;const double б=4;ɱ[]п;
public Ч(MyGridProgram ë,ɱ[]п,bool Х):base(ë,"Sprite Refresher",.1,Х){this.п=п;}internal override void Ф(){Ы=о();Ш=
$"{Э} started";}IEnumerator<string>о(){int н=Math.Min((int)(Math.Ceiling(п.Length/б)),Н);int ī=0;int м=н;foreach(ɱ Ȍ in п){Ȍ.ɮ();Ȍ.ǂ()
;ī++;if(ī>=м){м+=н;Ш=$"{Э} report {ī}/{п.Length}";С();yield return Ш;}}}internal override string П(){return
$"{Э} finished. Re-sync'd sprites on {п.Length} surfaces.\n"+$"{О()}";}}string л(){List<string>к=new List<string>();string й=$"{ύ}.{ό}";ϐ.GetSections(к);foreach(string ˢ in к){if(ˢ
.Contains(й)){ϐ.DeleteSection(ˢ);}}return ϐ.ToString();}string и(List<string>Ş,string ĝ,int з,StringBuilder Ƃ){string ħ=
"";int ж=0;Ƃ.Clear();if(Ş.Count>0){Ƃ.Append($"{ĝ} = ");if(Ş.Count>з){ж=з;}foreach(string ř in Ş){if(ж>=з){Ƃ.Append("\n|");
ж=0;}Ƃ.Append($"{ř}, ");ж++;}ħ=Ƃ.ToString();ħ=ħ.Remove(ħ.Length-2);}return ħ;}Dictionary<string,Action<IMyTerminalBlock>>
е(){Dictionary<string,Action<IMyTerminalBlock>>ù=new Dictionary<string,Action<IMyTerminalBlock>>(StringComparer.
OrdinalIgnoreCase);string д;string г="Enable";string в="Positive";string р="Negative";ù.Add($"{г}On",Ƙ=>((IMyFunctionalBlock)Ƙ).Enabled=χ
);ù.Add($"{г}Off",Ƙ=>((IMyFunctionalBlock)Ƙ).Enabled=φ);д="Battery";г="charge";ù.Add($"{д}Auto",Ƙ=>((IMyBatteryBlock)Ƙ).
ChargeMode=ChargeMode.Auto);ù.Add($"{д}Re{г}",Ƙ=>((IMyBatteryBlock)Ƙ).ChargeMode=ChargeMode.Recharge);ù.Add($"{д}Dis{г}",Ƙ=>((
IMyBatteryBlock)Ƙ).ChargeMode=ChargeMode.Discharge);д="Connector";ù.Add($"{д}Lock",Ƙ=>((IMyShipConnector)Ƙ).Connect());ù.Add(
$"{д}Unlock",Ƙ=>((IMyShipConnector)Ƙ).Disconnect());д="Door";ù.Add($"{д}Open",Ƙ=>((IMyDoor)Ƙ).OpenDoor());ù.Add($"{д}Close",Ƙ=>((
IMyDoor)Ƙ).CloseDoor());д="Tank";г="Stockpile";ù.Add($"{д}{г}On",Ƙ=>((IMyGasTank)Ƙ).Stockpile=χ);ù.Add($"{д}{г}Off",Ƙ=>((
IMyGasTank)Ƙ).Stockpile=φ);д="Gyro";string Л="Stabilize";г="Override";ù.Add($"{д}{г}On",Ƙ=>((IMyGyro)Ƙ).GyroOverride=χ);ù.Add(
$"{д}{г}Off",Ƙ=>((IMyGyro)Ƙ).GyroOverride=φ);ù.Add($"{д}Yaw{в}",Ƙ=>((IMyGyro)Ƙ).Yaw=9000);ù.Add($"{д}Yaw{Л}",Ƙ=>((IMyGyro)Ƙ).Yaw=0);
ù.Add($"{д}Yaw{р}",Ƙ=>((IMyGyro)Ƙ).Yaw=-9000);г="Pitch";ù.Add($"{д}{г}{в}",Ƙ=>((IMyGyro)Ƙ).Pitch=-9000);ù.Add(
$"{д}{г}{Л}",Ƙ=>((IMyGyro)Ƙ).Pitch=0);ù.Add($"{д}{г}{р}",Ƙ=>((IMyGyro)Ƙ).Pitch=9000);г="Roll";ù.Add($"{д}{г}{в}",Ƙ=>((IMyGyro)Ƙ).
Roll=9000);ù.Add($"{д}{г}{Л}",Ƙ=>((IMyGyro)Ƙ).Roll=0);ù.Add($"{д}{г}{р}",Ƙ=>((IMyGyro)Ƙ).Roll=-9000);д="Gear";г="AutoLock";ù
.Add($"{д}{г}On",Ƙ=>((IMyLandingGear)Ƙ).AutoLock=χ);ù.Add($"{д}{г}Off",Ƙ=>((IMyLandingGear)Ƙ).AutoLock=φ);ù.Add(
$"{д}Lock",Ƙ=>((IMyLandingGear)Ƙ).Lock());ù.Add($"{д}Unlock",Ƙ=>((IMyLandingGear)Ƙ).Unlock());д="JumpDrive";г="Recharge";ù.Add(
$"{д}{г}On",Ƙ=>((IMyJumpDrive)Ƙ).Recharge=χ);ù.Add($"{д}{г}Off",Ƙ=>((IMyJumpDrive)Ƙ).Recharge=φ);д="Parachute";ù.Add($"{д}Open",Ƙ=>
((IMyParachute)Ƙ).OpenDoor());ù.Add($"{д}Close",Ƙ=>((IMyParachute)Ƙ).CloseDoor());г="AutoDeploy";ù.Add($"{д}{г}On",Ƙ=>((
IMyParachute)Ƙ).AutoDeploy=χ);ù.Add($"{д}{г}Off",Ƙ=>((IMyParachute)Ƙ).AutoDeploy=φ);д="Piston";ù.Add($"{д}Extend",Ƙ=>((IMyPistonBase
)Ƙ).Extend());ù.Add($"{д}Retract",Ƙ=>((IMyPistonBase)Ƙ).Retract());д="Rotor";ù.Add($"{д}Lock",Ƙ=>((IMyMotorStator)Ƙ).
RotorLock=χ);ù.Add($"{д}Unlock",Ƙ=>((IMyMotorStator)Ƙ).RotorLock=φ);ù.Add($"{д}Reverse",Ƙ=>((IMyMotorStator)Ƙ).TargetVelocityRPM=
((IMyMotorStator)Ƙ).TargetVelocityRPM*-1);ù.Add($"{д}{в}",Ƙ=>((IMyMotorStator)Ƙ).TargetVelocityRPM=Math.Abs(((
IMyMotorStator)Ƙ).TargetVelocityRPM));ù.Add($"{д}{р}",Ƙ=>((IMyMotorStator)Ƙ).TargetVelocityRPM=Math.Abs(((IMyMotorStator)Ƙ).
TargetVelocityRPM)*-1);д="Sorter";г="Drain";ù.Add($"{д}{г}On",Ƙ=>((IMyConveyorSorter)Ƙ).DrainAll=χ);ù.Add($"{д}{г}Off",Ƙ=>((
IMyConveyorSorter)Ƙ).DrainAll=φ);д="Sound";ù.Add($"{д}Play",Ƙ=>((IMySoundBlock)Ƙ).Play());ù.Add($"{д}Stop",Ƙ=>((IMySoundBlock)Ƙ).Stop());
д="Thruster";г="Override";ù.Add($"{д}{г}Max",Ƙ=>((IMyThrust)Ƙ).ThrustOverridePercentage=1);ù.Add($"{д}{г}Off",Ƙ=>((
IMyThrust)Ƙ).ThrustOverridePercentage=0);д="Timer";ù.Add($"{д}Trigger",Ƙ=>((IMyTimerBlock)Ƙ).Trigger());ù.Add($"{д}Start",Ƙ=>((
IMyTimerBlock)Ƙ).StartCountdown());ù.Add($"{д}Stop",Ƙ=>((IMyTimerBlock)Ƙ).StopCountdown());д="Turret";string ǝ="Controller";string ȃ=
"Target";г="Meteors";ù.Add($"{д}{ȃ}{г}On",Ƙ=>((IMyLargeTurretBase)Ƙ).TargetMeteors=χ);ù.Add($"{д}{ȃ}{г}Off",Ƙ=>((
IMyLargeTurretBase)Ƙ).TargetMeteors=φ);ù.Add($"{ǝ}{ȃ}{г}On",Ƙ=>((IMyTurretControlBlock)Ƙ).TargetMeteors=χ);ù.Add($"{ǝ}{ȃ}{г}Off",Ƙ=>((
IMyTurretControlBlock)Ƙ).TargetMeteors=φ);г="Missiles";ù.Add($"{д}{ȃ}{г}On",Ƙ=>((IMyLargeTurretBase)Ƙ).TargetMissiles=χ);ù.Add(
$"{д}{ȃ}{г}Off",Ƙ=>((IMyLargeTurretBase)Ƙ).TargetMissiles=φ);ù.Add($"{ǝ}{ȃ}{г}On",Ƙ=>((IMyTurretControlBlock)Ƙ).TargetMissiles=χ);ù.Add
($"{ǝ}{ȃ}{г}Off",Ƙ=>((IMyTurretControlBlock)Ƙ).TargetMissiles=φ);г="SmallGrids";ù.Add($"{д}{ȃ}{г}On",Ƙ=>((
IMyLargeTurretBase)Ƙ).TargetSmallGrids=χ);ù.Add($"{д}{ȃ}{г}Off",Ƙ=>((IMyLargeTurretBase)Ƙ).TargetSmallGrids=φ);ù.Add($"{ǝ}{ȃ}{г}On",Ƙ=>((
IMyTurretControlBlock)Ƙ).TargetSmallGrids=χ);ù.Add($"{ǝ}{ȃ}{г}Off",Ƙ=>((IMyTurretControlBlock)Ƙ).TargetSmallGrids=φ);г="LargeGrids";ù.Add(
$"{д}{ȃ}{г}On",Ƙ=>((IMyLargeTurretBase)Ƙ).TargetLargeGrids=χ);ù.Add($"{д}{ȃ}{г}Off",Ƙ=>((IMyLargeTurretBase)Ƙ).TargetLargeGrids=φ);ù.
Add($"{ǝ}{ȃ}{г}On",Ƙ=>((IMyTurretControlBlock)Ƙ).TargetLargeGrids=χ);ù.Add($"{ǝ}{ȃ}{г}Off",Ƙ=>((IMyTurretControlBlock)Ƙ).
TargetLargeGrids=φ);г="Characters";ù.Add($"{д}{ȃ}{г}On",Ƙ=>((IMyLargeTurretBase)Ƙ).TargetCharacters=χ);ù.Add($"{д}{ȃ}{г}Off",Ƙ=>((
IMyLargeTurretBase)Ƙ).TargetCharacters=φ);ù.Add($"{ǝ}{ȃ}{г}On",Ƙ=>((IMyTurretControlBlock)Ƙ).TargetCharacters=χ);ù.Add($"{ǝ}{ȃ}{г}Off",Ƙ=>
((IMyTurretControlBlock)Ƙ).TargetCharacters=φ);г="Stations";ù.Add($"{д}{ȃ}{г}On",Ƙ=>((IMyLargeTurretBase)Ƙ).
TargetStations=χ);ù.Add($"{д}{ȃ}{г}Off",Ƙ=>((IMyLargeTurretBase)Ƙ).TargetStations=φ);ù.Add($"{ǝ}{ȃ}{г}On",Ƙ=>((IMyTurretControlBlock)Ƙ
).TargetStations=χ);ù.Add($"{ǝ}{ȃ}{г}Off",Ƙ=>((IMyTurretControlBlock)Ƙ).TargetStations=φ);г="Neutrals";ù.Add(
$"{д}{ȃ}{г}On",Ƙ=>((IMyLargeTurretBase)Ƙ).TargetNeutrals=χ);ù.Add($"{д}{ȃ}{г}Off",Ƙ=>((IMyLargeTurretBase)Ƙ).TargetNeutrals=φ);ù.Add(
$"{ǝ}{ȃ}{г}On",Ƙ=>((IMyTurretControlBlock)Ƙ).TargetNeutrals=χ);ù.Add($"{ǝ}{ȃ}{г}Off",Ƙ=>((IMyTurretControlBlock)Ƙ).TargetNeutrals=φ);г
="Enemies";ù.Add($"{д}{ȃ}{г}On",Ƙ=>((IMyLargeTurretBase)Ƙ).TargetEnemies=χ);ù.Add($"{д}{ȃ}{г}Off",Ƙ=>((IMyLargeTurretBase
)Ƙ).TargetEnemies=φ);ù.Add($"{ǝ}{ȃ}{г}On",Ƙ=>Ƙ.SetValue("TargetEnemies",χ));ù.Add($"{ǝ}{ȃ}{г}Off",Ƙ=>Ƙ.SetValue(
"TargetEnemies",φ));string љ="Subsystem";г="Default";ù.Add($"{д}{љ}{г}",Ƙ=>((IMyLargeTurretBase)Ƙ).SetTargetingGroup(""));ù.Add(
$"{ǝ}{љ}{г}",Ƙ=>((IMyTurretControlBlock)Ƙ).SetTargetingGroup(""));г="Weapons";ù.Add($"{д}{љ}{г}",Ƙ=>((IMyLargeTurretBase)Ƙ).
SetTargetingGroup(г));ù.Add($"{ǝ}{љ}{г}",Ƙ=>((IMyTurretControlBlock)Ƙ).SetTargetingGroup(г));г="Propulsion";ù.Add($"{д}{љ}{г}",Ƙ=>((
IMyLargeTurretBase)Ƙ).SetTargetingGroup(г));ù.Add($"{ǝ}{љ}{г}",Ƙ=>((IMyTurretControlBlock)Ƙ).SetTargetingGroup(г));г="PowerSystems";ù.Add(
$"{д}{љ}{г}",Ƙ=>((IMyLargeTurretBase)Ƙ).SetTargetingGroup(г));ù.Add($"{ǝ}{љ}{г}",Ƙ=>((IMyTurretControlBlock)Ƙ).SetTargetingGroup(г))
;д="Vent";г="pressurize";ù.Add($"{д}{г}",Ƙ=>((IMyAirVent)Ƙ).Depressurize=φ);ù.Add($"{д}De{г}",Ƙ=>((IMyAirVent)Ƙ).
Depressurize=χ);д="Warhead";ù.Add($"{д}Arm",Ƙ=>((IMyWarhead)Ƙ).IsArmed=χ);ù.Add($"{д}Disarm",Ƙ=>((IMyWarhead)Ƙ).IsArmed=φ);г=
"Countdown";ù.Add($"{д}{г}Start",Ƙ=>((IMyWarhead)Ƙ).StartCountdown());ù.Add($"{д}{г}Stop",Ƙ=>((IMyWarhead)Ƙ).StopCountdown());ù.Add
($"{д}Detonate",Ƙ=>((IMyWarhead)Ƙ).Detonate());ù.Add("WeaponFireOnce",Ƙ=>((IMyUserControllableGun)Ƙ).ShootOnce());д=
"Suspension";г="Height";ù.Add($"{д}{г}{в}",Ƙ=>((IMyMotorSuspension)Ƙ).Height=9000);ù.Add($"{д}{г}{р}",Ƙ=>((IMyMotorSuspension)Ƙ).
Height=-9000);ù.Add($"{д}{г}Zero",Ƙ=>((IMyMotorSuspension)Ƙ).Height=0);г="Propulsion";ù.Add($"{д}{г}{в}",Ƙ=>((
IMyMotorSuspension)Ƙ).PropulsionOverride=1);ù.Add($"{д}{г}{р}",Ƙ=>((IMyMotorSuspension)Ƙ).PropulsionOverride=-1);ù.Add($"{д}{г}Zero",Ƙ=>((
IMyMotorSuspension)Ƙ).PropulsionOverride=0);return ù;}class њ{Dictionary<string,Ŕ>ŀ;public њ(StringComparer ɺ=null){if(ɺ!=null){ŀ=new
Dictionary<string,Ŕ>(ɺ);}else{ŀ=new Dictionary<string,Ŕ>();}ј("Cozy",255,225,200);ј("Black",0,0,0);Color Ŧ=ј("Green",25,225,100);
Color ť=ј("LightBlue",100,200,225);Color Ť=ј("Yellow",255,255,0);Color ċ=ј("Orange",255,150,0);Color Ä=ј("Red",255,0,0);ŀ.Add
("LowGood",new Ũ(Ŧ,ť,Ť,ċ,Ä));ŀ.Add("HighGood",new ŧ(Ŧ,ť,Ť,ċ,Ä));}private Color ј(string G,int Ƅ,int ƃ,int Ƙ){Color ы=new
Color(Ƅ,ƃ,Ƙ);ŀ.Add(G,new Â(ы,G));return ы;}public bool ъ(Action<string>ц,MyIni Ĺ,string х,string ф,out Color Ĕ){Ŕ у;bool щ=ч(
ц,Ĺ,х,ф,out у);if(щ){Ĕ=у.œ(-1);}else{Ĕ=Color.White;}return щ;}public bool ч(Action<string>ц,MyIni Ĺ,string х,string ф,out
Ŕ у){MyIniValue ė=Ĺ.Get(х,ф);у=null;if(!ė.IsEmpty){string т=ė.ToString();if(ŀ.TryGetValue(т,out у)){return χ;}else{string
[]Ş=т.Split(',').Select(Ļ=>Ļ.Trim()).ToArray();if(Ş.Length==3){int[]с=new int[3];bool ш=φ;for(int Ǡ=0;Ǡ<=2;Ǡ++){if(!Int32
.TryParse(Ş[Ǡ],out с[Ǡ])){ш=χ;ц($", key {ф}, element {Ǡ} could not be parsed"+" as an integer.");}}if(ш){return φ;}else{у
=new Â(new Color(с[0],с[1],с[2]));ŀ.Add(т,у);return χ;}}else{ц($", key {ф} does not match a pre-defined color and "+
$"does not have three elements like a custom color.");return φ;}}}else{return φ;}}public Ŕ ь(string G){return ŀ[G];}}bool ї(string Е,string ф,string і,bool Μ,ref bool ε,
bool η,string д,ź Ė){MyIniValue ė;ė=ϐ.Get(Е,ф);if(ė.IsEmpty){ϐ.Set(Е,ф,і);if(Μ){ϐ.SetComment(Е,ф,
"-----------------------------------------");}ε=χ;if(η){Ė.ƍ($"'{ф}' key was missing from '{Е}' section of "+$"block '{д}' and has been re-generated.");}return φ;}
return χ;}bool ѕ(HashSet<string>є,string G,string Ě,ź Ė){if(G.ToLowerInvariant()=="blank"){Ė.Ɨ(
$"{Ě} tried to use the Element name '{G}', "+"which is reserved by the script to indicate portions of a Report that should "+
"be left empty. Please choose a different name.");return χ;}else if(є.Contains(G)){Ė.Ɨ($"{Ě} tried to use the Element name '{G}', "+
$"which has already been claimed. All Element providers (Tally, ActionSet, "+$"Trigger, Raycaster) must have their own, unique names.");return χ;}else{return φ;}}void ѓ(string ђ,string ё,Action<
string>ц,List<KeyValuePair<string,bool>>ѐ){string ȃ="";bool я=φ;bool ю;ѐ.Clear();string[]э=ђ.Split(',').Select(Ļ=>Ļ.Trim()).
ToArray();foreach(string Ħ in э){ю=φ;string[]ƞ=Ħ.Split(':').Select(Ļ=>Ļ.Trim()).ToArray();ȃ=ƞ[0];if(ƞ.Length<2){ю=χ;ц(
$"{ё} does not provide a state for the component "+$"'{ȃ}'. Valid states are 'on' and 'off'.");}else if(ƞ[1].ToLowerInvariant()=="on"){я=χ;}else if(ƞ[1].ToLowerInvariant(
)=="off"){я=φ;}else{ю=χ;ц($"{ё} attempts to set '{ȃ}' to the invalid state "+
$"'{ƞ[1]}'. Valid states are 'on' and 'off'.");}if(!ю){ѐ.Add(new KeyValuePair<string,bool>(ȃ,я));}}}List<Action<IMyTerminalBlock>>ƽ(MyIni ľ,string ĸ,string ķ,
Dictionary<string,Action<IMyTerminalBlock>>ù,ź Ė,IMyTerminalBlock A){MyIniValue ė=ľ.Get(ĸ,ķ);List<Action<IMyTerminalBlock>>Ľ=null;
if(!ė.IsEmpty){string[]ļ=null;Ľ=new List<Action<IMyTerminalBlock>>();ļ=ė.ToString().Split(',').Select(Ļ=>Ļ.Trim()).ToArray
();foreach(string ĺ in ļ){Ľ.Add(Ŀ(ĺ,ù,Ė,A,ĸ,ķ));}}return Ľ;}Action<IMyTerminalBlock>Ŀ(string ĺ,Dictionary<string,Action<
IMyTerminalBlock>>ù,ź Ė,IMyTerminalBlock A,string ĸ,string ķ){Action<IMyTerminalBlock>Ķ=null;if(ù.ContainsKey(ĺ)){Ķ=ù[ĺ];}else{Ė.ƒ(
$"Block '{A.CustomName}', discrete section '{ĸ}', "+$"references the unknown action '{ĺ}' as its {ķ}.");}return Ķ;}õ ĵ(ź Ė,string Ĵ,int ī,IMyTerminalBlock A,MyIni Ĺ,
MyIniValue ė,њ ŀ){string Ŋ=$"Action{ī}Property";Action<string>ň=Ň=>{Ė.ƒ($"Block {A.CustomName}, section {Ĵ}{Ň}");};ė=Ĺ.Get(Ĵ,Ŋ);õ
ņ=null;if(!ė.IsEmpty){string Ņ=ė.ToString("<missing>");ITerminalProperty ń=A.GetProperty(Ņ);if(ń==null){ň(
$" references the unknown property '{Ņ}' "+$"as its {Ŋ}.");ņ=new õ<bool>(Ņ);}else{string ć=$"Action{ī}ValueOn";string Ć=$"Action{ī}ValueOff";Action<string>Ń=(ĝ)=>
{ė=Ĺ.Get(Ĵ,ĝ);};if(ń.TypeName.ToLowerInvariant()=="boolean"){õ<bool>ł=new õ<bool>(Ņ);bool Ł;Action<bool,string>Ĳ=(ï,ĝ)=>{
Ń(ĝ);if(!ė.IsEmpty&&ė.TryGetBoolean(out Ł)){ł.Ą(ï,Ł);}};Ĳ(χ,ć);Ĳ(φ,Ć);ņ=ł;}else if(ń.TypeName.ToLowerInvariant()=="int64"
){õ<long>ł=new õ<long>(Ņ);long Ł;Action<bool,string>Ĳ=(ï,ĝ)=>{Ń(ĝ);if(!ė.IsEmpty&&ė.TryGetInt64(out Ł)){ł.Ą(ï,Ł);}};Ĳ(χ,ć
);Ĳ(φ,Ć);ņ=ł;}else if(ń.TypeName.ToLowerInvariant()=="single"){õ<float>ł=new õ<float>(Ņ);float Ł;Action<bool,string>Ĳ=(ï,
ĝ)=>{Ń(ĝ);if(!ė.IsEmpty&&ė.TryGetSingle(out Ł)){ł.Ą(ï,Ł);}};Ĳ(χ,ć);Ĳ(φ,Ć);ņ=ł;}else if(ń.TypeName.ToLowerInvariant()==
"color"){õ<Color>ł=new õ<Color>(Ņ);Color Ł;if(ŀ.ъ(ň,Ĺ,Ĵ,$"Action{ī}ValueOn",out Ł)){ł.Ą(χ,Ł);}if(ŀ.ъ(ň,Ĺ,Ĵ,$"Action{ī}ValueOff"
,out Ł)){ł.Ą(φ,Ł);}ņ=ł;}else if(ń.TypeName.ToLowerInvariant()=="stringbuilder"){ĉ ŉ=new ĉ(Ņ,Ƃ);string ĳ;Action<bool,
string>Ĳ=(ï,ĝ)=>{Ń(ĝ);if(!ė.IsEmpty&&ė.TryGetString(out ĳ)){ŉ.Ą(ï,ĳ);}};Ĳ(χ,ć);Ĳ(φ,Ć);ņ=ŉ;}else{Ė.Ɨ(
$"Block '{A.CustomName}', discrete section '{Ĵ}', "+$"references the property '{Ņ}' which uses the non-standard "+
$"type {ń.TypeName}. Report this to the scripter, as the script "+$"will need to be altered to handle this.");ņ=new õ<bool>(Ņ);}if(!ņ.ô()&&ń!=null){ň(
$" does not specify a working Action{ī}ValueOn "+$"or Action{ī}ValueOff for the property '{Ņ}'. If one was "+
$"specified, make sure that it matches the type '{ń.TypeName}.'");}}return ņ;}else{return ņ;}}void Ĝ(Ȣ ě,string Ě,bool ę,string Ę,MyIniValue ė,ź Ė){double ă;ė=ϐ.Get(Ě,$"{Ę}Value");if(!
ė.IsEmpty){ă=ė.ToDouble();ė=ϐ.Get(Ě,$"{Ę}Command");if(!ė.IsEmpty){string ĕ=ė.ToString().ToLowerInvariant();if(ĕ=="on"){ě.
Ȃ(ę,ă,χ);}else if(ĕ=="off"){ě.Ȃ(ę,ă,φ);}else if(ĕ=="switch"){Ė.Ɨ($"{Ě}: {ě.w} specifies "+
$"a {Ę}Command of 'switch', which cannot be used for triggers.");}else{Ė.Ɨ($"{Ě}: {ě.w} has a missing "+$"or invalid {Ę}Command. Valid commands are 'on' and 'off'.");}}else{Ė.Ɨ(
$"{Ě}: {ě.w} specifies a "+$"{Ę}Value but no {Ę}Command.");}}}static string Ğ(Color Ĕ){if(Ĕ==Ϛ){return"cozy";}else if(Ĕ==ϟ){return"green";}else if
(Ĕ==Ϟ){return"lightBlue";}else if(Ĕ==ϝ){return"yellow";}else if(Ĕ==Ϝ){return"orange";}else if(Ĕ==ϛ){return"red";}else{
return$"{Ĕ.R}, {Ĕ.G}, {Ĕ.B}";}}class ē{Dictionary<string,ĥ>Ē;Dictionary<string,ŋ>đ;internal ē(){Ē=new Dictionary<string,ĥ>();đ
=new Dictionary<string,ŋ>();}internal bool Đ(string G,ĥ Ď){if(Ē.ContainsKey(G)){return φ;}else{Ē.Add(G,Ď);return χ;}}
internal void ď(string G,ĥ Ď){if(Ē.ContainsKey(G)){Ē[G]=Ď;}else{Ē.Add(G,Ď);}}internal int č(string G){ĥ Č;if(Ē.TryGetValue(G,out
Č)){return Č.ģ;}else{return-1;}}internal void Ġ(string G){Ē.Remove(G);}internal void ı(){Ē.Clear();}internal bool İ(
string G,int į,out string Į){ŋ ĭ;if(!đ.TryGetValue(G,out ĭ)){ŋ Ĭ=new ŋ(į,G);đ.Add(G,Ĭ);Į="";return χ;}else{Į=ĭ.ƀ();return φ;}}
internal void ğ(){foreach(ĥ Ď in Ē.Values){Ď.ğ();}ŋ ĩ;int ī=0;while(ī<đ.Count){ĩ=đ.Values.ElementAt(ī);if(ĩ.Ɓ()){đ.Remove(ĩ.G);}
else{ī++;}}}public string Ī(string G){ŋ ĩ;if(đ.TryGetValue(G,out ĩ)){return ĩ.ƀ();}else{return$"{G} is not on cooldown.";}}
internal string Ĩ(){string ħ="Contained periodics:\n";foreach(KeyValuePair<string,ĥ>Ħ in Ē){ħ+=
$" -{Ħ.Key} with frequency {Ħ.Value.ģ}\n";}ħ+="Contained cooldowns:\n";foreach(KeyValuePair<string,ŋ>Ħ in đ){ħ+=
$" -{Ħ.Key} with a remaining duration of {Ħ.Value.Ĥ}\n";}return ħ;}}class ĥ{internal int ģ{get;private set;}int Ĥ;Action Ģ;internal ĥ(int ģ,Action Ģ){this.ģ=ģ;Ĥ=ģ;this.Ģ=Ģ;}
internal void ğ(){Ĥ--;if(Ĥ<=0){Ĥ=ģ;Ģ.Invoke();}}}class ŋ{public int Ĥ{get;private set;}internal string G{get;private set;}
internal ŋ(int į,string G){Ĥ=į;this.G=G;}internal bool Ɓ(){Ĥ--;if(Ĥ<=0){return χ;}else{return φ;}}internal string ƀ(){return
$"{G} is on cooldown for the next {(int)(Ĥ*1.4)} seconds.";}}void ſ(int ž=0,ē Ž=null){Action ż=()=>{Ë();ǂ();υ.ğ();};ĥ Ż=new ĥ(ž,ż);θ.ď("UpdateDelay",Ż);υ.ǚ=ž;}class ź{
StringBuilder Ƃ;int Ź;List<string>ŷ,Ŷ,ŵ;int Ŵ,ų,Ų;public string ű{get;private set;}public string Ű{get;private set;}public string ů{
get;private set;}public Color Ů{set{ű=Ŭ(value);}}public Color ŭ{set{Ű=Ŭ(value);}}public Color Ÿ{set{ů=Ŭ(value);}}public ź(
StringBuilder Ƃ,int Ź){this.Ƃ=Ƃ;this.Ź=Ź;Ŵ=0;ų=0;Ų=0;ŷ=new List<string>();Ŷ=new List<string>();ŵ=new List<string>();ű=Ɔ(255,255,0,0);
Ű=Ɔ(255,255,255,0);ů=Ɔ(255,100,200,225);}public void Ɨ(string Ɩ){if(ŷ.Count<Ź){ŷ.Add(Ɩ);}else{Ŵ++;}}public int ƕ(){return
ŷ.Count+Ŵ;}public void Ɣ(){Ŵ=0;ŷ.Clear();}public string Ɠ(){string Ǝ;Ƃ.Clear();foreach(string Š in ŷ){Ƃ.Append($" -{Š}\n"
);}if(Ŵ>0){Ƃ.Append($" -And {Ŵ} other errors.\n");}Ǝ=Ƃ.ToString();Ƃ.Clear();return Ǝ;}public void ƒ(string ċ){if(Ŷ.Count<
Ź){Ŷ.Add(ċ);}else{ų++;}}public int Ƒ(){return Ŷ.Count+ų;}public void Ɛ(){ų=0;Ŷ.Clear();}public string Ə(){string Ǝ;Ƃ.
Clear();foreach(string Š in Ŷ){Ƃ.Append($" -{Š}\n");}if(ų>0){Ƃ.Append($" -And {ų} other warnings.\n");}Ǝ=Ƃ.ToString();Ƃ.Clear
();return Ǝ;}public void ƍ(string ƌ){if(ŵ.Count<Ź){ŵ.Add(ƌ);}else{Ų++;}}public int Ƌ(){return ŵ.Count+Ų;}public void Ɗ(){
Ų=0;ŵ.Clear();}public string Ɖ(){string ƈ;Ƃ.Clear();foreach(string Š in ŵ){Ƃ.Append($" -{Š}\n");}if(Ų>0){Ƃ.Append(
$" -And {Ų} other notes.\n");}ƈ=Ƃ.ToString();Ƃ.Clear();return ƈ;}public void Ƈ(){Ɣ();Ɛ();Ɗ();}}static string Ɔ(int ƅ,int Ƅ,int ƃ,int Ƙ){return
$"{ƅ:X2}{Ƅ:X2}{ƃ:X2}{Ƙ:X2}";}static string Ŭ(Color Ĕ){return Ɔ(Ĕ.A,Ĕ.R,Ĕ.G,Ĕ.B);}static string š(string Š){if(Š.Contains("\n")){Š=
$"\n|{Š.Replace("\n","\n|")}";}return Š;}static string ş(List<string>Ş,int ŝ=3,bool Ŝ=χ){int ś=0;string ħ="";string Ś=Ŝ?"|":"";if(Ş.Count>ŝ&&Ŝ){ħ=
"\n|";}foreach(string ř in Ş){if(ś>=ŝ){ħ+=$"\n{Ś}";ś=0;}ħ+=$"{ř}, ";ś++;}ħ=ħ.Remove(ħ.Length-2);return ħ;}interface Ř{string
Ţ();}interface ŗ{string ŕ();}interface Ŕ:ŗ{Color œ(double º);}abstract class Œ:Ŕ{protected string G;public Color ő{
internal get;set;}public Color Ő{internal get;set;}public Color ŏ{internal get;set;}public Color Ŏ{internal get;set;}public
Color ō{internal get;set;}internal int Ŗ;internal int Ō;internal int ţ;internal int ū;public Œ(Color Ŧ,Color ť,Color Ť,Color
ċ,Color Ä){ő=Ŧ;Ő=ť;ŏ=Ť;Ŏ=ċ;ō=Ä;}public Œ(){}internal bool Ū(string ũ,Color Ĕ){switch(ũ){case"Optimal":ő=Ĕ;break;case
"Normal":Ő=Ĕ;break;case"Caution":ŏ=Ĕ;break;case"Warning":Ŏ=Ĕ;break;case"Critical":ō=Ĕ;break;default:return φ;}return χ;}public
string ŕ(){return G;}public abstract Color œ(double º);}class Ũ:Œ{public Ũ(Color Ŧ,Color ť,Color Ť,Color ċ,Color Ä):base(Ŧ,ť,Ť
,ċ,Ä){G="LowGood";Ŗ=0;Ō=55;ţ=70;ū=85;}public Ũ():base(){G="LowGood";}public override Color œ(double º){Color Ã=Ő;if(º<=Ŗ)
{Ã=ő;}else if(º>ū){Ã=ō;}else if(º>ţ){Ã=Ŏ;}else if(º>Ō){Ã=ŏ;}return Ã;}}class ŧ:Œ{public ŧ(Color Ŧ,Color ť,Color Ť,Color ċ
,Color Ä):base(Ŧ,ť,Ť,ċ,Ä){G="HighGood";Ŗ=100;Ō=45;ţ=30;ū=15;}public ŧ():base(){G="HighGood";}public override Color œ(
double º){Color Ã=Ő;if(º>=Ŗ){Ã=ő;}else if(º<ū){Ã=ō;}else if(º<ţ){Ã=Ŏ;}else if(º<Ō){Ã=ŏ;}return Ã;}}class Â:Ŕ{string G;public
Color Á{private get;set;}public Â(){}public Â(Color Á,string G){this.G=G;this.Á=Á;}public Â(Color Á){this.Á=Á;G=
$"{Á.R}, {Á.G}, {Á.B}";}public Color œ(double º){return Á;}public string ŕ(){return G;}}interface À{string µ();string ª();Color z{get;}}
abstract class y:À,Ř{public string x{get;set;}internal string w{get;private set;}public double B{protected get;set;}public
double v{get;protected set;}public double C{get;protected set;}internal bool u;internal bool s;public double º{get;protected
set;}protected string q;protected string Å;protected Ǩ H;internal string Ò;public Color z{get;protected set;}internal Ŕ D{
get;set;}public y(Ǩ H,string G,Ŕ D,double B=1){this.H=H;w=G;x=G;this.D=D;this.B=B;v=0;C=0;u=φ;s=φ;º=0;q="curr";Å="max";Ò=
"[----------]";z=Ϛ;}internal string Ñ(){return Ò;}internal string Ð(){return q;}internal string Ï(){return Å;}public string µ(){return
$"{x}\n{q} / {Å}\n{Ò}";}public string ª(){return$"{x,-12}{($"{q} / {Å}"),-12}{Ò}";}internal abstract void Î();internal void Í(){v=0;}internal
void Ì(double M){C=M*B;u=χ;}internal abstract void Ë();internal string Ê(string É){double È=1;string K=
$"[{ύ}.{ό}.Tally.{š(w)}]\n";if(x!=w){K+=$"DisplayName = {š(x)}\n";}K+=É;if(u){K+=$"Max = {C/B}\n";}if(B!=È){K+=$"Multiplier = {B}\n";}K+="\n";
return K;}public abstract string Ţ();}class Ç:y{internal ǀ Æ;public Ç(Ǩ H,string G,ǀ Æ,Ŕ D,double B=1):base(H,G,D,B){this.Æ=Æ;
}internal bool R(IMyTerminalBlock A){return Æ.R(A);}internal string P(){return Æ.Ţ();}internal override void Î(){if(!u){C
=Æ.ƿ()*B;}j(ref Å,C);}internal override void Ë(){if(C!=0){v=Æ.Ǐ();v=v*B;º=Math.Min(v/C,100)*100;z=D.œ(º);H.Ñ(ref Ò,º);j(
ref q,v);}}public override string Ţ(){string K=$"Type = {Æ.Ţ()}\n";if(!(D is ŧ)){K+=$"ColorCoder = {D.ŕ()}\n";}K+=Æ.Ʋ();
return Ê(K);}}class O:y{public O(Ǩ H,string G,Ŕ D,double B=1):base(H,G,D,B){}internal void N(double M){if(!u){C+=M;}}internal
override void Î(){if(!u){C=C*B;}j(ref Å,C);}internal virtual void L(IMyInventory Q){v+=(double)Q.CurrentVolume;}internal
override void Ë(){if(C!=0){v=v*B;º=Math.Min(v/C,100)*100;z=D.œ(º);H.Ñ(ref Ò,º);j(ref q,v);}}public override string Ţ(){string K=
"Type = Inventory\n";if(!(D is Ũ)){K+=$"ColorCoder = {D.ŕ()}\n";}return Ê(K);}}class J:O{internal MyItemType I{get;private set;}public J(Ǩ H
,string G,string F,string E,Ŕ D,double C=0,double B=1):base(H,G,D,B){I=new MyItemType(F,E);Ì(C);}public J(Ǩ H,string G,
MyItemType I,Ŕ D,double C=0,double B=1):base(H,G,D,B){this.I=I;Ì(C);}internal override void L(IMyInventory Q){v+=(double)Q.
GetItemAmount(I);}public override string Ţ(){string K=$"Type = Item\n";K+=$"ItemTypeID = {I.TypeId}\n";K+=
$"ItemSubTypeID = {I.SubtypeId}\n";if(!(D is ŧ)){K+=$"ColorCoder = {D.ŕ()}\n";}return Ê(K);}}class o{IMyInventory Q;O[]n;public o(IMyInventory Q,O[]n){
this.Q=Q;this.n=n;}public void m(){foreach(O k in n){{k.N((double)Q.MaxVolume);}}}public void l(){foreach(O k in n){k.L(Q);}
}}static void j(ref string f,double d){f="";if(d<10){f+=(Math.Round(d,1));}else if(d<1000){f+=(int)d;}else if(d<10000){f=
Math.Round(d/1000,1)+"K";}else if(d<1000000){f=(int)(d/1000)+"K";}else if(d<10000000){f=Math.Round(d/1000000,1)+"M";}else if
(d<1000000000){f=(int)(d/1000000)+"M";}else if(d<10000000000){f=Math.Round(d/1000000000,1)+"B";}else if(d<1000000000000){
f=(int)(d/10000000000)+"B";}else if(d<10000000000000){f=Math.Round(d/1000000000000,1)+"T";}else if(d>10000000000000){f=(
int)(d/1000000000000)+"T";}}interface c{void Z(bool Y);bool X();string W();}class V:c{IMyTerminalBlock U;internal Action<
IMyTerminalBlock>Ó{get;set;}internal Action<IMyTerminalBlock>S{get;set;}public V(IMyTerminalBlock Ù){U=Ù;Ó=null;S=null;}public void Z(
bool Y){if(Y){Ó?.Invoke(U);}else{S?.Invoke(U);}}public bool X(){return Ó!=null||S!=null;}public string W(){return
$"Block '{U.CustomName}'";}}class þ:c{IMyTerminalBlock U;internal List<Action<IMyTerminalBlock>>ý{get;set;}internal List<Action<IMyTerminalBlock>
>ü{get;set;}public þ(IMyTerminalBlock Ù){U=Ù;ý=null;ü=null;}public void Z(bool Y){List<Action<IMyTerminalBlock>>û;if(Y){û
=ý;}else{û=ü;}if(û!=null){foreach(Action<IMyTerminalBlock>Õ in û){Õ.Invoke(U);}}}public bool X(){return ý?.Count>0||ü?.
Count>0;}public string W(){return$"Block '{U.CustomName}'";}}class ú:c{IMyTerminalBlock U;private List<õ>ù;public ú(
IMyTerminalBlock Ù){this.U=Ù;ù=new List<õ>();}public void ø(õ ö){ù.Add(ö);}public void Z(bool Y){foreach(õ ö in ù){ö.Z(U,Y);}}public
bool X(){return ù.Count!=0;}public string W(){return$"Block '{U.CustomName}'";}}abstract class õ{public abstract bool ô();
public abstract void Z(IMyTerminalBlock A,bool Y);}class õ<ó>:õ{string ÿ;private ó ć,Ć;private bool Û,Ú;public õ(string ÿ){
this.ÿ=ÿ;Û=φ;Ú=φ;}public void Ą(bool ï,ó ă){if(ï){ć=ă;Û=χ;}else{Ć=ă;Ú=χ;}}public override bool ô(){return Û||Ú;}public
override void Z(IMyTerminalBlock A,bool Y){if(Y&&Û){A.SetValue<ó>(ÿ,ć);}else if(!Y&&Ú){A.SetValue<ó>(ÿ,Ć);}}}class ĉ:õ{
StringBuilder Ĉ;string ÿ;private string ć,Ć;public ĉ(string ÿ,StringBuilder ą){Ĉ=ą;this.ÿ=ÿ;ć=null;Ć=null;}public void Ą(bool ï,
string ă){if(ï){ć=ă;}else{Ć=ă;}}public override bool ô(){return ć!=null||Ć!=null;}public override void Z(IMyTerminalBlock A,
bool Y){StringBuilder Ă=(StringBuilder)Ĉ;if(Y&&ć!=null){A.SetValue(ÿ,Ă.Append(ć));}if(!Y&&Ć!=null){A.SetValue(ÿ,Ă.Append(Ć))
;}Ĉ.Clear();}}class ā:c{ɪ Ā;internal string Ċ{private get;set;}internal string ò{private get;set;}public ā(ɪ Ù){Ā=Ù;Ċ="";
ò="";}public void Z(bool Y){if(Y){Ā.ɠ(Ċ);}else{Ā.ɠ(ò);}}public bool X(){return!String.IsNullOrEmpty(Ċ)||!String.
IsNullOrEmpty(ò);}public string W(){return"Some MFD (Sorry, MFDs are supposed to work)";}}class ä:c,ŗ{Ȅ ã;internal bool â{private get
;set;}internal bool á{private get;set;}public ä(Ȅ Ù){ã=Ù;â=φ;á=φ;}public void Z(bool Y){if(Y){if(â){ã.Ȉ();}}else{if(á){ã.
Ȉ();}}}public bool X(){return â||á;}public string W(){return$"Raycaster '{ã.w}'";}public string ŕ(){return
$"{ã.w}: {(â?"on":"off")}";}}class à:c,ŗ{public const bool ß=χ;public const bool å=φ;internal Ș Þ;internal bool Ý,Ü;internal bool Û,Ú;public à(Ș Ù
){Þ=Ù;Ý=å;Ü=å;Û=φ;Ú=φ;}public void Ø(bool Õ){Ý=Õ;Û=χ;}public void Ö(bool Õ){Ü=Õ;Ú=χ;}public void Z(bool Y){try{if(Y){if(Û
){if(!Þ.Ȗ){Þ.Ȑ(Ý);}else{Exception Ô=new InvalidOperationException();Ô.Data.Add("Counter",0);throw Ô;}}}else{if(Ú){if(!Þ.Ȗ
){Þ.Ȑ(Ü);}else{Exception Ô=new InvalidOperationException();Ô.Data.Add("Counter",0);throw Ô;}}}}catch(
InvalidOperationException e){int æ=(int)e.Data["Counter"];e.Data.Add(æ,Þ.w);e.Data["Counter"]=++æ;Þ.ȯ();throw;}}public bool X(){return Û||Ú;}
public string W(){return$"Controller for ActionSet {Þ.w}";}public string ŕ(){if(ï()){return$"{Þ.w}: {(Ý?"on":"off")}";}else{
return$"{Þ.w}: {(Ü?"on":"off")}";}}public bool ï(){return Û;}}class ñ:c,ŗ{internal Ȣ ð;internal bool Ý,Ü;internal bool Û,Ú;
public ñ(Ȣ Ù){this.ð=Ù;Ý=φ;Ü=φ;Û=φ;Ú=φ;}public void Ø(bool Õ){Ý=Õ;Û=χ;}public void Ö(bool Õ){Ü=Õ;Ú=χ;}public void Z(bool Y){if
(Y){if(Û){ð.ǽ(Ý);}}else{if(Ú){ð.ǽ(Ü);}}}public bool X(){return Û||Ú;}public string W(){return
$"Controller for Trigger {ð.w}";}public string ŕ(){if(ï()){return$"{ð.w}: {(Ý?"on":"off")}";}else{return$"{ð.w}: {(Ü?"on":"off")}";}}public bool ï(){
return Û;}}class î:c,Ř{Program ë;public int í{get;internal set;}public int ì{get;internal set;}public î(Program ë){this.ë=ë;í=
0;ì=0;}public void Z(bool Y){if(Y){ë.ſ(í);}else{ë.ſ(ì);}}public bool X(){return í!=0||ì!=0;}public string W(){return
"The Distributor";}public string Ţ(){string K="";int ê=0;int é=0;if(í!=ê){K+=$"DelayOn = {í}\n";}if(ì!=é){K+=$"DelayOff = {ì}\n";}return
K;}}class è:c,Ř{IMyIntergridCommunicationSystem ġ;internal string ç{get;set;}internal string ƙ{get;set;}internal string ș
{get;set;}public è(IMyIntergridCommunicationSystem ġ,string ç){this.ġ=ġ;this.ç=ç;ƙ="";ș="";}public void Z(bool Y){if(Y){ġ
.SendBroadcastMessage(ç,ƙ);}else{ġ.SendBroadcastMessage(ç,ș);}}public bool X(){return!String.IsNullOrEmpty(ƙ)||!String.
IsNullOrEmpty(ș);}public string W(){return$"IGC on channel '{ç}'";}public string Ţ(){string K="";if(ç!=""){K+=$"IGCChannel = {ç}\n";}
if(ƙ!=""){K+=$"IGCMessageOn = {ƙ}\n";}if(ș!=""){K+=$"IGCMessageOff = {ș}\n";}return K;}}class Ș:À,Ř{List<c>ȗ;internal
string x{get;set;}internal string w{get;private set;}internal bool ï{get;private set;}internal bool Ȗ{get;private set;}
internal Color Ȏ{private get;set;}internal Color ȍ{private get;set;}public Color z{get;private set;}internal string ȕ{private
get;set;}internal string Ȕ{private get;set;}public string ȓ{get;private set;}public Ș(string G,bool Ǿ){ȗ=new List<c>();x=G;
w=G;ï=Ǿ;Ȗ=φ;Ȏ=ϟ;ȍ=ϛ;ȕ="Enabled";Ȕ="Disabled";Ƕ();}internal void Ƕ(){if(ï){z=Ȏ;ȓ=ȕ;}else{z=ȍ;ȓ=Ȕ;}}public void Ȓ(c ȏ){ȗ.
Add(ȏ);}public void ȑ(){Ȑ(!ï);}public void Ȑ(bool Ǽ){ï=Ǽ;Ȗ=χ;Ƕ();foreach(c ȏ in ȗ){try{ȏ.Z(Ǽ);}catch(
InvalidOperationException){throw;}catch(Exception e){if(!e.Data.Contains("Identifier")){e.Data.Add("Identifier",ȏ.W());}throw;}}}public void Ț(){
Ȗ=φ;}public void ȯ(){ȓ="Fault";z=new Color(125,125,125);}public string µ(){return$"{x}\n{ȓ}";}public string ª(){return
$"{x,-19} {ȓ,18}";}public string Ţ(){Color Ȯ=ϟ;Color ȭ=ϛ;string Ȭ="Enabled";string ȫ="Disabled";string K=$"[{ύ}.{ό}.ActionSet.{š(w)}]\n";
if(x!=w){K+=$"DisplayName = {š(x)}\n";}if(Ȏ!=Ȯ){K+=$"ColorOn = {Ğ(Ȏ)}\n";}if(ȍ!=ȭ){K+=$"ColorOff = {Ğ(ȍ)}\n";}if(ȕ!=Ȭ){K+=
$"TextOn = {š(ȕ)}\n";}if(Ȕ!=ȫ){K+=$"TextOff = {š(Ȕ)}\n";}int Ȫ=0;c ȩ=null;ŗ Ȩ=null;List<string>ȧ=null;List<string>Ȧ=null;List<string>ȥ=null;
List<string>Ȥ=null;List<string>ȣ=null;while(Ȫ!=-1){if(Ȫ>=ȗ.Count){Ȫ=-1;}else{ȩ=ȗ[Ȫ];if(ȩ is Ř){K+=$"{((Ř)ȩ).Ţ()}";Ȫ++;}else
if(ȩ is ŗ){Ȩ=(ŗ)ȩ;if(Ȩ is ñ){if(ȥ==null){ȥ=new List<String>();Ȥ=new List<String>();}if(((ñ)Ȩ).ï()){ȥ.Add(Ȩ.ŕ());}else{Ȥ.
Add(Ȩ.ŕ());}}else if(Ȩ is à){if(ȧ==null){ȧ=new List<String>();Ȧ=new List<String>();}if(((à)Ȩ).ï()){ȧ.Add(Ȩ.ŕ());}else{Ȧ.Add
(Ȩ.ŕ());}}else{if(ȣ==null){ȣ=new List<String>();}ȣ.Add(((ä)Ȩ).ŕ());}Ȫ++;}else{Ȫ=-1;}}}if(ȧ?.Count>0){K+=
$"ActionSetsLinkedToOn = {ş(ȧ)}\n";}if(Ȧ?.Count>0){K+=$"ActionSetsLinkedToOff = {ş(Ȧ)}\n";}if(ȥ?.Count>0){K+=$"TriggerLinkedToOn = {ş(ȥ)}\n";}if(Ȥ?.Count>
0){K+=$"TriggerLinkedToOff = {ş(Ȥ)}\n";}if(ȣ?.Count>0){K+=$"RaycastPerformedOnState = {ş(ȣ)}\n";}K+="\n";return K;}}class
Ȣ:À,Ř{internal y ȁ{private get;set;}internal Ș Ȁ{private get;set;}double ȡ,Ƞ;bool ȟ,Ȟ;bool ȝ,Ȝ;internal bool ț{get;
private set;}public string w{get;private set;}string ȕ,Ȕ;public string ȓ{get;private set;}Color Ȏ,ȍ;public Color z{get;private
set;}public Ȣ(string w,bool Ǿ){ȁ=null;Ȁ=null;ǿ(w,Ǿ);}public Ȣ(string w,y ȁ,Ș Ȁ,bool Ǿ){this.ȁ=ȁ;this.Ȁ=Ȁ;ǿ(w,Ǿ);}private
void ǿ(string w,bool Ǿ){this.w=w;ȡ=-1;Ƞ=-1;ȟ=φ;Ȟ=φ;ȝ=φ;Ȝ=φ;ț=Ǿ;ȕ="Armed";Ȕ="Disarmed";Ȏ=ϝ;ȍ=ϛ;Ƕ();}public void Ȃ(bool ę,
double ă,bool Ƿ){if(ę){Ƞ=ă;Ȟ=Ƿ;Ȝ=χ;}else{ȡ=ă;ȟ=Ƿ;ȝ=χ;}}public void ǽ(bool Ǽ){ț=Ǽ;Ƕ();}public bool ǻ(out Ș Ǻ,out bool ǹ){Ǻ=null
;ǹ=φ;if(ț){if(ȝ&&Ȁ.ï!=ȟ&&ȁ.º>=ȡ){Ǻ=Ȁ;ǹ=ȟ;return χ;}else if(Ȝ&&Ȁ.ï!=Ȟ&&ȁ.º<=Ƞ){Ǻ=Ȁ;ǹ=Ȟ;return χ;}}return φ;}private void Ǹ
(bool Ƿ){Ȁ.Ȑ(Ƿ);}private void Ƕ(){if(ț){z=Ȏ;ȓ=ȕ;}else{z=ȍ;ȓ=Ȕ;}}public bool ǵ(){return ȝ||Ȝ;}public string W(){return w;}
public string µ(){return$"{w}\n{(ț?ȕ:Ȕ)}";}public string ª(){return$"{w,-19} {(ț?ȕ:Ȕ),18}";}public string Ţ(){string K=
$"[{ύ}.{ό}.Trigger.{š(w)}]\n";K+=$"Tally = {ȁ.w}\n";K+=$"ActionSet = {Ȁ.w}\n";if(Ȝ){K+=$"LessOrEqualValue = {Ƞ}\n";K+=
$"LessOrEqualCommand = {(Ȟ?"on":"off")}\n";}if(ȝ){K+=$"GreaterOrEqualValue = {ȡ}\n";K+=$"GreaterOrEqualCommand = {(ȟ?"on":"off")}\n";}return K;}}class Ȅ:Ř{
StringBuilder Ƃ;internal ɞ ȋ{private get;set;}string Ȍ;internal bool Ǘ{get;private set;}internal string w{get;private set;}public Ȅ(
StringBuilder Ƃ,string w){ǿ(Ƃ,w);}public Ȅ(StringBuilder Ƃ,ɞ ȋ,string w){this.ȋ=ȋ;ǿ(Ƃ,w);}private void ǿ(StringBuilder Ƃ,string w){
this.Ƃ=Ƃ;this.w=w;Ȍ=$"{DateTime.Now.ToString("HH:mm:ss")}- Raycaster {w} "+$"reports: No data";Ǘ=φ;}public void Ȋ(
IMyCameraBlock Ƭ){ȋ.Ȋ(Ƭ);Ƭ.EnableRaycast=χ;}public double ȉ(){return ȋ?.ɜ??-1;}public void Ȉ(){MyDetectedEntityInfo Ȇ;double ȅ;
IMyCameraBlock Ƭ=ȋ.Ȉ(out Ȇ,out ȅ);ȇ(Ȇ,ȅ,Ƭ);Ǘ=χ;}private void ȇ(MyDetectedEntityInfo Ȇ,double ȅ,IMyCameraBlock Ƭ){Ƃ.Clear();if(Ƭ==null)
{Ƃ.Append($"{DateTime.Now.ToString("HH:mm:ss")}- Raycaster {w} "+$"reports: No cameras have the required {ȉ()} charge "+
$"needed to perform this scan.");}else if(Ȇ.IsEmpty()){Ƃ.Append($"{DateTime.Now.ToString("HH:mm:ss")}- Raycaster {w} "+
$"reports: Camera '{Ƭ.CustomName}' detected no entities on a "+$"{ȅ} meter scan.");}else{Ƃ.Append($"{DateTime.Now.ToString("HH:mm:ss")}- Raycaster {w} "+
$"reports: Camera '{Ƭ.CustomName}' detected entity '{Ȇ.Name}' "+$"on a {ȅ} meter scan.\n\n");Ƃ.Append($"Relationship: {Ȇ.Relationship}\n");Ƃ.Append($"Type: {Ȇ.Type}\n");Ƃ.Append(
$"Size: {Ȇ.BoundingBox.Size.ToString("0.00")}\n");Vector3D ȃ=Ȇ.HitPosition.Value;Ƃ.Append($"Distance: {Vector3D.Distance(Ƭ.GetPosition(),ȃ).ToString("0.00")}\n");Ƃ.
Append($"GPS:Raycast - {Ȇ.Name}:{ȃ.X}:{ȃ.Y}:{ȃ.Z}:\n");}Ȍ=Ƃ.ToString();Ƃ.Clear();}public void ǟ(){Ǘ=φ;}public string Ǫ(){
return Ȍ;}public string Ţ(){string K=$"[{ύ}.{ό}.Raycaster.{š(w)}]\n";K+=ȋ.Ţ();return K;}}abstract class ɞ{protected List<
IMyCameraBlock>ɝ;public double ɜ{get;protected set;}public ɞ(){ɝ=new List<IMyCameraBlock>();ɜ=0;}public void Ȋ(IMyCameraBlock Ƭ){Ƭ.
EnableRaycast=χ;ɝ.Add(Ƭ);}public abstract void ɛ(double[]ɔ);public abstract IMyCameraBlock Ȉ(out MyDetectedEntityInfo Ȇ,out double ȅ)
;public abstract string Ţ();}class ɚ:ɞ{private double ə;private double B;private double ɘ;public ɚ():base(){int[]ɗ=ɕ();ə=
ɗ[0];B=ɗ[1];ɘ=ɗ[2];}public static string[]ɖ(){return new string[]{"BaseRange","Multiplier","MaxRange"};}internal static
int[]ɕ(){return new int[]{1000,3,27000};}public override void ɛ(double[]ɔ){if(ɔ[0]!=-1)ə=ɔ[0];if(ɔ[1]!=-1)B=ɔ[1];if(ɔ[2]!=-
1)ɘ=ɔ[2];double ɓ=ə;ɜ=ə;while(ɓ<ɘ){ɓ*=B;ɜ+=Math.Min(ɘ,ɓ);}}public override IMyCameraBlock Ȉ(out MyDetectedEntityInfo Ȇ,
out double ȅ){Ȇ=new MyDetectedEntityInfo();ȅ=-1;IMyCameraBlock Ƭ=ɒ();if(Ƭ==null||Ƭ.AvailableScanRange<ɜ){return null;}else{
ȅ=ə;while(Ȇ.IsEmpty()&&ȅ<ɘ){Ȇ=Ƭ.Raycast(ȅ,0,0);ȅ*=B;if(ȅ>ɘ){ȅ=ɘ;}}return Ƭ;}}private IMyCameraBlock ɒ(){IMyCameraBlock ɟ=
null;foreach(IMyCameraBlock Ƭ in ɝ){if(ɟ==null||Ƭ.AvailableScanRange>ɟ.AvailableScanRange){ɟ=Ƭ;}}return ɟ;}public override
string Ţ(){string[]ɲ=ɖ();int[]ɗ=ɕ();string K="Type = Linear\n";if(ə!=ɗ[0]){K+=$"{ɲ[0]} = {ə}\n";}if(B!=ɗ[1]){K+=
$"{ɲ[1]} = {B}\n";}if(ɘ!=ɗ[2]){K+=$"{ɲ[2]} = {ɘ}\n";}return K;}}interface ɱ{void ɰ();void ǂ();void ɯ();void ɮ();}interface ɭ{Color ɬ{get;
set;}Color ɫ{get;set;}}class ɪ:ɱ{public string w{get;private set;}private Dictionary<string,ɱ>ɩ;internal int ɨ{get;private
set;}internal string ɧ{get;private set;}private ɱ ɦ;public ɪ(string w){this.w=w;ɩ=new Dictionary<string,ɱ>(StringComparer.
OrdinalIgnoreCase);ɨ=0;ɧ="";ɦ=null;}public void ɥ(string G,ɱ ɤ){ɩ.Add(G,ɤ);if(ɦ==null){ɦ=ɤ;ɧ=G;}}public int ɣ(){return ɩ.Count;}public
void ɢ(bool ɡ){if(ɡ){ɨ++;if(ɨ>=ɩ.Count){ɨ=0;}}else{ɨ--;if(ɨ<0){ɨ=ɩ.Count-1;}}ɧ=ɩ.Keys.ToArray()[ɨ];ɑ();}public bool ɠ(string
G){if(ɩ.ContainsKey(G)){ɧ=G;ɨ=ɩ.Keys.ToList().IndexOf(G);ɑ();return χ;}else{return φ;}}private void ɑ(){ɱ ɏ=ɩ[ɧ];bool Ⱥ=φ
;if(ɦ is ȹ&&ɏ is ȷ){Ⱥ=χ;}ɦ=ɏ;ɰ();ǂ();if(Ⱥ){ɮ();}}public void ɰ(){ɦ.ɰ();}public void ǂ(){ɦ.ǂ();}public void ɯ(){ɦ.ɯ();}
public void ɮ(){ɦ.ɮ();}}class ȹ:ɱ,ɭ{IMyTextSurface ƴ;public Color ɬ{get;set;}public Color ɫ{get;set;}public string ȸ{get;set;}
public ȹ(IMyTextSurface ƴ,string ȸ){this.ƴ=ƴ;this.ȸ=ȸ;ɬ=ƴ.ScriptForegroundColor;ɫ=ƴ.ScriptBackgroundColor;}public void ǂ(){}
public void ɯ(){}public void ɮ(){}public void ɰ(){ƴ.ContentType=ContentType.SCRIPT;ƴ.Script=ȸ;ƴ.ScriptForegroundColor=ɬ;ƴ.
ScriptBackgroundColor=ɫ;}}class ȷ:ɱ,ɭ{IMyTextSurface ƴ;À[]Ş;Vector2[]ȶ;public float Ƴ{private get;set;}public string Ƹ{private get;set;}
public Color ɬ{get;set;}public Color ɫ{get;set;}public string Ǔ{get;set;}Vector2 ȴ;bool ȳ;public ȷ(IMyTextSurface ƴ,List<À>Ş,
string Ǔ="",float Ƴ=1f,string Ƹ="Debug"){this.ƴ=ƴ;this.Ş=Ş.ToArray();this.Ǔ=Ǔ;this.Ƴ=Ƴ;this.Ƹ=Ƹ;ɬ=ƴ.ScriptForegroundColor;ɫ=ƴ.
ScriptBackgroundColor;ȶ=new Vector2[Ş.Count];ȳ=φ;}public void Ȳ(int ȱ,float ȵ,float Ȱ,float Ȼ,float ɐ,bool Ɏ,StringBuilder Ƃ){RectangleF ɍ=
new RectangleF((ƴ.TextureSize-ƴ.SurfaceSize)/2f,ƴ.SurfaceSize);float Ɍ=(ȵ/100)*ƴ.SurfaceSize.X;float ɋ=(Ȼ/100)*ƴ.
SurfaceSize.Y;ɍ.X+=Ɍ;ɍ.Width-=Ɍ;ɍ.Y+=ɋ;ɍ.Height-=ɋ;ɍ.Width-=(Ȱ/100)*ƴ.SurfaceSize.X;ɍ.Height-=(ɐ/100)*ƴ.SurfaceSize.Y;Ƃ.Clear();
float Ɋ=0;if(!string.IsNullOrEmpty(Ǔ)){Ƃ.Append(Ǔ);Ɋ=ƴ.MeasureStringInPixels(Ƃ,Ƹ,Ƴ).Y;if(Ɏ){ȴ=new Vector2(ɍ.Width/2+ɍ.X,ɍ.Y);
}else{ȴ=new Vector2(ƴ.TextureSize.X/2,(ƴ.TextureSize.Y-ƴ.SurfaceSize.Y)/2);Ɋ=Math.Max(Ɋ-Ȼ,0);}}int ɉ=(int)(Math.Ceiling((
double)Ş.Count()/ȱ));float Ɉ=ɍ.Width/ȱ;float ɇ=(ɍ.Height-Ɋ)/ɉ;int Ɇ=1;Vector2 Ʌ,Ʉ,Ƀ;Ʌ=new Vector2(Ɉ/2,ɇ/2);Ʌ+=ɍ.Position;Ʌ.Y+=
Ɋ;for(int Ǡ=0;Ǡ<Ş.Count();Ǡ++){if(Ş[Ǡ]!=null){Ƃ.Clear();Ƃ.Append(Ş[Ǡ].µ());Ƀ=ƴ.MeasureStringInPixels(Ƃ,Ƹ,Ƴ);Ʉ=new Vector2
(Ʌ.X,Ʌ.Y);Ʉ.Y-=Ƀ.Y/2;ȶ[Ǡ]=Ʉ;}if(Ɇ==ȱ){Ʌ.X=Ɉ/2;Ʌ.X+=ɍ.Position.X;Ʌ.Y+=ɇ;Ɇ=1;}else{Ʌ.X+=Ɉ;Ɇ++;}}Ƃ.Clear();}public void ǂ(){
À ř;MySprite ɂ;using(MySpriteDrawFrame Ɂ=ƴ.DrawFrame()){if(ȳ){Vector2 ɀ=new Vector2(0,0);ɂ=MySprite.CreateSprite(
"IconEnergy",ɀ,ɀ);Ɂ.Add(ɂ);}if(!string.IsNullOrEmpty(Ǔ)){ɂ=MySprite.CreateText(Ǔ,Ƹ,ƴ.ScriptForegroundColor,Ƴ);ɂ.Position=ȴ;Ɂ.Add(ɂ);
}for(int Ǡ=0;Ǡ<Ş.Count();Ǡ++){ř=Ş[Ǡ];if(ř!=null){ɂ=MySprite.CreateText(ř.µ(),Ƹ,ř.z,Ƴ);ɂ.Position=ȶ[Ǡ];Ɂ.Add(ɂ);}}}}public
void ɯ(){ǂ();}public void ɮ(){ȳ=!ȳ;}public void ɰ(){ƴ.ContentType=ContentType.SCRIPT;ƴ.Script="";ƴ.ScriptForegroundColor=ɬ;ƴ
.ScriptBackgroundColor=ɫ;}}interface ȿ{string Ⱦ();bool Ǘ();}class Ƚ:ȿ{IMyTerminalBlock A;bool ȼ;public Ƚ(IMyTerminalBlock
A){this.A=A;ȼ=χ;}public string Ⱦ(){return A.CustomData;}public bool Ǘ(){bool Į=ȼ;ȼ=φ;return Į;}}abstract class ƨ:ȿ{
protected IMyTerminalBlock A;string Ƽ;public ƨ(IMyTerminalBlock A){this.A=A;Ƽ="";}public abstract string Ⱦ();public bool Ǘ(){if(Ⱦ
()==Ƽ){return φ;}else{Ƽ=Ⱦ();return χ;}}}class ƻ:ƨ{public ƻ(IMyTerminalBlock A):base(A){}public override string Ⱦ(){return
A.DetailedInfo;}}class ƺ:ƨ{public ƺ(IMyTerminalBlock A):base(A){}public override string Ⱦ(){return A.CustomInfo;}}class ƹ
:ȿ{ǜ ƈ;public ƹ(ǜ ƈ){this.ƈ=ƈ;}public string Ⱦ(){return ƈ.Ǫ();}public bool Ǘ(){return ƈ.Ǘ;}}class Ʒ:ȿ{MyGridProgram ë;
public Ʒ(MyGridProgram ë){this.ë=ë;}public string Ⱦ(){return ë.Storage;}public bool Ǘ(){return φ;}}class ƶ:ȿ{Ȅ ƭ;public ƶ(Ȅ ƭ)
{this.ƭ=ƭ;}public string Ⱦ(){return ƭ.Ǫ();}public bool Ǘ(){return ƭ.Ǘ;}}class Ƶ:ɱ,ɭ{IMyTextSurface ƴ;public Color ɬ{get;
set;}public Color ɫ{get;set;}public string Ƹ{get;set;}public float Ƴ{get;set;}int ƾ;ȿ ǎ;StringBuilder Ƃ;public Ƶ(
IMyTextSurface ƴ,ȿ ǎ,StringBuilder Ƃ){this.ƴ=ƴ;this.ǎ=ǎ;this.Ƃ=Ƃ;ɬ=ƴ.FontColor;ɫ=ƴ.BackgroundColor;Ƹ=ƴ.Font;Ƴ=ƴ.FontSize;ƾ=0;}public
void Ǎ(int ǌ){if(ǌ>=0){ƾ=ǌ;}}private string ǋ(string Ǌ){if(ƾ>0){string[]ǉ=Ǌ.Split(' ');int ǈ=0;Ƃ.Clear();foreach(string Ǉ in
ǉ){Ƃ.Append($"{Ǉ} ");if(Ǉ.Contains('\n')){ǈ=0;}else{ǈ+=Ǉ.Length+1;if(ǈ>ƾ){Ƃ.Append("\n");ǈ=0;}}}Ǌ=Ƃ.ToString();}return Ǌ;
}public void ǂ(){if(ǎ.Ǘ()){ƴ.WriteText(ǋ(ǎ.Ⱦ()));}}public void ɯ(){ƴ.WriteText(ǋ(ǎ.Ⱦ()));}public void ɮ(){}public void ɰ(
){ƴ.ContentType=ContentType.TEXT_AND_IMAGE;ƴ.FontColor=ɬ;ƴ.BackgroundColor=ɫ;ƴ.Font=Ƹ;ƴ.FontSize=Ƴ;ƴ.WriteText(ǋ(ǎ.Ⱦ()));
}}class ǆ{List<IMyLightingBlock>ǅ;À ř;Color Ǆ;public ǆ(À ř){ǅ=new List<IMyLightingBlock>();this.ř=ř;Ǆ=Ϛ;}public void ǃ(
IMyLightingBlock ǁ){ǅ.Add(ǁ);}public void ǂ(){if(ř.z!=Ǆ){foreach(IMyLightingBlock ǁ in ǅ){ǁ.Color=ř.z;}Ǆ=ř.z;}}}interface ǀ{bool R(
IMyTerminalBlock A);double ƿ();double Ǐ();string Ʋ();string Ţ();}class Ʀ:ǀ{List<IMyBatteryBlock>ƚ;public Ʀ(){ƚ=new List<IMyBatteryBlock>
();}public bool R(IMyTerminalBlock A){IMyBatteryBlock ƥ=A as IMyBatteryBlock;if(ƥ==null){return φ;}else{ƚ.Add(ƥ);return χ
;}}public double ƿ(){double C=0;foreach(IMyBatteryBlock Ƥ in ƚ){C+=Ƥ.MaxStoredPower;}return C;}public double Ǐ(){double v
=0;foreach(IMyBatteryBlock Ƥ in ƚ){v+=Ƥ.CurrentStoredPower;}return v;}public string Ʋ(){return"";}public string Ţ(){
return"Battery";}}class ƣ:ǀ{List<IMyGasTank>Ƣ;List<IMyTerminalBlock>Ƨ;public ƣ(){Ƣ=new List<IMyGasTank>();Ƨ=new List<
IMyTerminalBlock>();}public bool R(IMyTerminalBlock A){IMyGasTank Ơ=A as IMyGasTank;if(Ơ!=null){Ƣ.Add(Ơ);return χ;}else{IMyPowerProducer
Ɵ=A as IMyPowerProducer;if(Ɵ!=null&&(Ɵ.BlockDefinition.SubtypeId.EndsWith("HydrogenEngine")||Ɵ.BlockDefinition.SubtypeId
=="LargePrototechReactor")){Ƨ.Add(Ɵ);return χ;}else{return φ;}}}public double ƿ(){double C=0;string[]ƞ;string[]Ɲ={"(","L/"
,"L)"};foreach(IMyGasTank Ɯ in Ƣ){C+=Ɯ.Capacity;}foreach(IMyTerminalBlock ƛ in Ƨ){ƞ=ƛ.DetailedInfo.Split(Ɲ,System.
StringSplitOptions.None);C+=Double.Parse(ƞ[2]);}return C;}public double Ǐ(){double v=0;foreach(IMyGasTank Ɯ in Ƣ){v+=Ɯ.Capacity*Ɯ.
FilledRatio;}foreach(IMyTerminalBlock ƛ in Ƨ){v+=ƛ.Components.Get<MyResourceSourceComponent>().RemainingCapacity;}return v;}public
string Ʋ(){return"";}public string Ţ(){return"Gas";}}class ơ:ǀ{List<IMyJumpDrive>ƚ;public ơ(){ƚ=new List<IMyJumpDrive>();}
public bool R(IMyTerminalBlock A){IMyJumpDrive ƥ=A as IMyJumpDrive;if(ƥ==null){return φ;}else{ƚ.Add(ƥ);return χ;}}public
double ƿ(){double C=0;foreach(IMyJumpDrive Ʊ in ƚ){C+=Ʊ.MaxStoredPower;}return C;}public double Ǐ(){double v=0;foreach(
IMyJumpDrive Ʊ in ƚ){v+=Ʊ.CurrentStoredPower;}return v;}public string Ʋ(){return"";}public string Ţ(){return"JumpDrive";}}class ư:ǀ{
List<IMyCameraBlock>ƚ;Ȅ Ư;public ư(){ƚ=new List<IMyCameraBlock>();Ư=null;}public void Ʈ(Ȅ ƭ){Ư=ƭ;}public bool R(
IMyTerminalBlock A){IMyCameraBlock ƥ=A as IMyCameraBlock;if(ƥ==null){return φ;}else{ƚ.Add(ƥ);return χ;}}public double ƿ(){return-1;}
public double Ǐ(){double v=0;foreach(IMyCameraBlock Ƭ in ƚ){v+=Ƭ.AvailableScanRange;}return v;}public string Ʋ(){return Ư==
null?"":$"Raycaster = {Ư.w}\n";}public string Ţ(){return"Raycast";}}class ƫ:ǀ{List<IMyPowerProducer>ƚ;public ƫ(){ƚ=new List<
IMyPowerProducer>();}public bool R(IMyTerminalBlock A){IMyPowerProducer ƥ=A as IMyPowerProducer;if(ƥ==null){return φ;}else{ƚ.Add(ƥ);
return χ;}}public double ƿ(){double C=0;foreach(IMyPowerProducer ƪ in ƚ){C+=ƪ.Components.Get<MyResourceSourceComponent>().
DefinedOutput;}return C;}public double Ǐ(){double v=0;foreach(IMyPowerProducer ƪ in ƚ){v+=ƪ.MaxOutput;}return v;}public string Ʋ(){
return"";}public string Ţ(){return"PowerMax";}}class Ʃ:ǀ{List<IMyPowerProducer>ƚ;public Ʃ(){ƚ=new List<IMyPowerProducer>();}
public bool R(IMyTerminalBlock A){IMyPowerProducer ƥ=A as IMyPowerProducer;if(ƥ==null){return φ;}else{ƚ.Add(ƥ);return χ;}}
public double ƿ(){double C=0;foreach(IMyPowerProducer ƪ in ƚ){C+=ƪ.Components.Get<MyResourceSourceComponent>().DefinedOutput;}
return C;}public double Ǐ(){double v=0;foreach(IMyPowerProducer ƪ in ƚ){v+=ƪ.CurrentOutput;}return v;}public string Ʋ(){return
"";}public string Ţ(){return"PowerCurrent";}}class Ǯ:ǀ{List<IMySlimBlock>ƚ;public Ǯ(){ƚ=new List<IMySlimBlock>();}public
bool R(IMyTerminalBlock A){IMySlimBlock ƥ=A.CubeGrid.GetCubeBlock(A.Min);ƚ.Add(ƥ);return χ;}public double ƿ(){double C=0;
foreach(IMySlimBlock A in ƚ){C+=A.MaxIntegrity;}return C;}public double Ǐ(){double v=0;foreach(IMySlimBlock A in ƚ){v+=A.
BuildIntegrity-A.CurrentDamage;}return v;}public string Ʋ(){return"";}public string Ţ(){return"Integrity";}}class ǭ:ǀ{List<IMyAirVent>
ƚ;public ǭ(){ƚ=new List<IMyAirVent>();}public bool R(IMyTerminalBlock A){IMyAirVent ƥ=A as IMyAirVent;if(ƥ==null){return
φ;}else{ƚ.Add(ƥ);return χ;}}public double ƿ(){double C=0;foreach(IMyAirVent Ǭ in ƚ){C+=1;}return C;}public double Ǐ(){
double v=0;foreach(IMyAirVent Ǭ in ƚ){v+=Ǭ.GetOxygenLevel();}return v;}public string Ʋ(){return"";}public string Ţ(){return
"VentPressure";}}class ǫ:ǀ{List<IMyPistonBase>ƚ;public ǫ(){ƚ=new List<IMyPistonBase>();}public bool R(IMyTerminalBlock A){
IMyPistonBase ƥ=A as IMyPistonBase;if(ƥ==null){return φ;}else{ƚ.Add(ƥ);return χ;}}public double ƿ(){double C=0;foreach(IMyPistonBase
ǳ in ƚ){C+=ǳ.HighestPosition;}return C;}public double Ǐ(){double v=0;foreach(IMyPistonBase ǳ in ƚ){v+=ǳ.CurrentPosition;}
return v;}public string Ʋ(){return"";}public string Ţ(){return"PistonExtension";}}class ǲ:ǀ{List<IMyMotorStator>ƚ;public ǲ(){ƚ
=new List<IMyMotorStator>();}public bool R(IMyTerminalBlock A){IMyMotorStator ƥ=A as IMyMotorStator;if(ƥ==null){return φ;
}else{ƚ.Add(ƥ);return χ;}}public double ƿ(){double C=0;foreach(IMyMotorStator Ǳ in ƚ){C+=360;}return C;}public double Ǐ()
{double v=0;foreach(IMyMotorStator Ǳ in ƚ){v+=MathHelper.ToDegrees(Ǳ.Angle);}return v;}public string Ʋ(){return"";}public
string Ţ(){return"RotorAngle";}}class ǰ:ǀ{List<IMyShipController>ƚ;public ǰ(){ƚ=new List<IMyShipController>();}public bool R(
IMyTerminalBlock A){IMyShipController ƥ=A as IMyShipController;if(ƥ==null){return φ;}else{ƚ.Add(ƥ);return χ;}}public double ƿ(){return
110;}public double Ǐ(){double v=-1;foreach(IMyShipController ǝ in ƚ){if(ǝ.IsFunctional){v=ǝ.GetShipSpeed();break;}}return v
;}public string Ʋ(){return"";}public string Ţ(){return"ControllerSpeed";}}class ǯ:ǀ{List<IMyShipController>ƚ;public ǯ(){ƚ
=new List<IMyShipController>();}public bool R(IMyTerminalBlock A){IMyShipController ƥ=A as IMyShipController;if(ƥ==null){
return φ;}else{ƚ.Add(ƥ);return χ;}}public double ƿ(){return 1;}public double Ǐ(){double v=0;foreach(IMyShipController ǝ in ƚ){
if(ǝ.IsFunctional){v=ǝ.GetNaturalGravity().Length()/9.81;break;}}return v;}public string Ʋ(){return"";}public string Ţ(){
return"ControllerGravity";}}class Ǵ:ǀ{List<IMyShipController>ƚ;public Ǵ(){ƚ=new List<IMyShipController>();}public bool R(
IMyTerminalBlock A){IMyShipController ƥ=A as IMyShipController;if(ƥ==null){return φ;}else{ƚ.Add(ƥ);return χ;}}public double ƿ(){return-1
;}public double Ǐ(){double v=-1;foreach(IMyShipController ǝ in ƚ){if(ǝ.IsFunctional){v=ǝ.GetNaturalGravity().Length()*ǝ.
CalculateShipMass().PhysicalMass;break;}}return v;}public string Ʋ(){return"";}public string Ţ(){return"ControllerWeight";}}class ǜ{List<
string>ƈ;string Ǔ;public string Ǜ{private get;set;}public int ǚ{private get;set;}public string Ǚ{private get;set;}
StringBuilder Ƃ;string Ǟ;int Ǒ;public bool Ǘ{get;private set;}string[]ǖ;int Ǖ=-1;int ǔ;public ǜ(StringBuilder Ƃ,string Ǔ,bool ǒ=φ,int
Ǒ=5){ƈ=new List<string>();this.Ƃ=Ƃ;Ǟ="";this.Ǔ=Ǔ;Ǜ="";Ǚ="";this.Ǒ=Ǒ;Ǘ=φ;ǚ=0;if(ǒ){ǖ=new string[]{"|----","-|---","--|--",
"---|-","----|"};Ǖ=0;ǔ=1;}}public void ğ(){Ǖ+=ǔ;if(Ǖ==0||Ǖ==4){ǔ*=-1;}}public void ǐ(string ǘ){ƈ.Insert(0,
$"{DateTime.Now.ToString("HH:mm:ss")}- {ǘ}");if(ƈ.Count>Ǒ){ƈ.RemoveAt(Ǒ);}Ƃ.Clear();foreach(string Š in ƈ){Ƃ.Append($"\n{Š}\n");}Ǟ=Ƃ.ToString();Ƃ.Clear();Ǘ=χ;}
public void ǟ(){Ǘ=φ;}public string Ǫ(){Ƃ.Clear();Ƃ.Append(Ǔ);if(Ǖ!=-1){Ƃ.Append($" {ǖ[Ǖ]}");}Ƃ.Append("\n");if(!String.
IsNullOrEmpty(Ǜ)){Ƃ.Append($"Script Tag: {Ǜ}\n");}if(ǚ!=0){Ƃ.Append($"Current Update Delay: {ǚ}\n");}Ƃ.Append($"{Ǚ}\n");Ƃ.Append(Ǟ);
string ǩ=Ƃ.ToString();Ƃ.Clear();return ǩ;}}class Ǩ{StringBuilder Ƃ;int ǧ;string[]Ǧ;public Ǩ(StringBuilder Ƃ,int ǡ=10){this.Ƃ=Ƃ
;ǧ=ǡ;Ǧ=new string[ǧ+1];string ǥ="";for(int Ǡ=0;Ǡ<Ǧ.Length;Ǡ++){ǣ(ref ǥ,Ǡ,ǧ);Ǧ[Ǡ]=ǥ;}}public void Ñ(ref string Ò,double º)
{Ò=Ǧ[Ǥ(º,ǧ)];}public void Ñ(ref string Ò,double º,int ǡ){int Ǣ=Ǥ(º,ǡ);ǣ(ref Ò,Ǣ,ǡ);}private int Ǥ(double º,int ǡ){º=Math.
Min(º,100);return(int)((º/100)*ǡ);}private void ǣ(ref string Ò,int Ǣ,int ǡ){Ƃ.Clear();Ƃ.Append('[');for(int Ǡ=0;Ǡ<Ǣ;++Ǡ){Ƃ.
Append('|');}for(int Ǡ=Ǣ;Ǡ<ǡ;++Ǡ){Ƃ.Append(' ');}Ƃ.Append(']');Ò=Ƃ.ToString();Ƃ.Clear();}}
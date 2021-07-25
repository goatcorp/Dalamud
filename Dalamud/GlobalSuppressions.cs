// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// General
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1118:Parameter should not span multiple lines", Justification = "Preventing long lines", Scope = "namespaceanddescendants", Target = "~N:Dalamud")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1124:Do not use regions", Justification = "I like regions", Scope = "namespaceanddescendants", Target = "~N:Dalamud")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1123:Do not place regions within elements", Justification = "I like regions in elements too", Scope = "namespaceanddescendants", Target = "~N:Dalamud")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1503:Braces should not be omitted", Justification = "This is annoying", Scope = "namespaceanddescendants", Target = "~N:Dalamud")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1512:Single-line comments should not be followed by blank line", Justification = "I like this better", Scope = "namespaceanddescendants", Target = "~N:Dalamud")]
[assembly: SuppressMessage("StyleCop.CSharp.LayoutRules", "SA1515:Single-line comment should be preceded by blank line", Justification = "I like this better", Scope = "namespaceanddescendants", Target = "~N:Dalamud")]
[assembly: SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1127:Generic type constraints should be on their own line", Justification = "I like this better", Scope = "namespaceanddescendants", Target = "~N:Dalamud")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1633:File should have header", Justification = "We don't do those yet")]

// Extensions
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Group extensions with the relevant class", Scope = "type", Target = "~T:Dalamud.Interface.FontAwesomeExtensions")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Enum followed by an extension", Scope = "type", Target = "~T:Dalamud.Interface.FontAwesomeExtensions")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Group extensions with the relevant class", Scope = "type", Target = "~T:Dalamud.Game.Internal.Gui.Structs.JobFlagsExt")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Enum followed by an extension", Scope = "type", Target = "~T:Dalamud.Game.Internal.Gui.Structs.JobFlagsExt")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Group extensions with the relevant class", Scope = "type", Target = "~T:Dalamud.Game.Text.XivChatTypeExtensions")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Enum followed by an extension", Scope = "type", Target = "~T:Dalamud.Game.Text.XivChatTypeExtensions")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Group attributes with the relevant class", Scope = "type", Target = "~T:Dalamud.Game.Text.XivChatTypeInfoAttribute")]

// DalamudStartInfo.cs
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Interop", Scope = "type", Target = "~T:Dalamud.DalamudStartInfo")]

// PartyFinder
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Explicit struct layout", Scope = "type", Target = "~T:Dalamud.Game.Internal.Gui.Structs.PartyFinder.Packet")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1202:Elements should be ordered by access", Justification = "Explicit struct layout", Scope = "type", Target = "~T:Dalamud.Game.Internal.Gui.Structs.PartyFinder.Listing")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "TODO", Scope = "type", Target = "~T:Dalamud.Game.Internal.Gui.Structs.PartyFinder.Packet")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "TODO", Scope = "type", Target = "~T:Dalamud.Game.Internal.Gui.Structs.PartyFinder.Listing")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "TODO", Scope = "type", Target = "~T:Dalamud.Game.ClientState.Structs.PartyMember")]

// <type>Offsets.cs
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "Offset classes goto the end of file.", Scope = "type", Target = "~T:Dalamud.Game.ClientState.Actors.TargetOffsets")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Group offset classes with the relevant class.", Scope = "type", Target = "~T:Dalamud.Game.ClientState.Actors.TargetOffsets")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "Group offset classes with the relevant class.", Scope = "type", Target = "~T:Dalamud.Game.Internal.Gui.Structs.AddonOffsets")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Document the offset usage instead.", Scope = "type", Target = "~T:Dalamud.Game.ClientState.Actors.TargetOffsets")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Document the offset usage instead.", Scope = "type", Target = "~T:Dalamud.Game.Internal.Gui.Structs.AddonOffsets")]

// Breaking api changes: these should be split into a PartyFinder subdirectory
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "breaking api change", Scope = "type", Target = "~T:Dalamud.Game.Internal.Gui.Structs.SearchAreaFlags")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "breaking api change", Scope = "type", Target = "~T:Dalamud.Game.Internal.Gui.Structs.JobFlags")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "breaking api change", Scope = "type", Target = "~T:Dalamud.Game.Internal.Gui.Structs.ObjectiveFlags")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "breaking api change", Scope = "type", Target = "~T:Dalamud.Game.Internal.Gui.Structs.ConditionFlags")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "breaking api change", Scope = "type", Target = "~T:Dalamud.Game.Internal.Gui.Structs.DutyFinderSettingsFlags")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "breaking api change", Scope = "type", Target = "~T:Dalamud.Game.Internal.Gui.Structs.LootRuleFlags")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "breaking api change", Scope = "type", Target = "~T:Dalamud.Game.Internal.Gui.Structs.Category")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "breaking api change", Scope = "type", Target = "~T:Dalamud.Game.Internal.Gui.Structs.DutyType")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "breaking api change", Scope = "type", Target = "~T:Dalamud.Game.Internal.Gui.PartyFinderListingEventArgs")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1307:Accessible fields should begin with upper-case letter", Justification = "breaking api change", Scope = "type", Target = "~T:Dalamud.Game.ClientState.Structs.PartyMember")]
[assembly: SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "breaking api change", Scope = "member", Target = "~E:Dalamud.Game.Internal.Gui.PartyFinderGui.ReceiveListing")]

// Breaking api changes
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "breaking api change", Scope = "member", Target = "~F:Dalamud.Game.Internal.BaseAddressResolver.DebugScannedValues")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "breaking api change", Scope = "member", Target = "~F:Dalamud.Game.Internal.Gui.Addon.Addon.Address")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "breaking api change", Scope = "member", Target = "~F:Dalamud.Game.Internal.Gui.Addon.Addon.addonStruct")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "breaking api change", Scope = "member", Target = "~F:Dalamud.Game.Internal.Gui.GameGui.GetBaseUIObject")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "breaking api change", Scope = "member", Target = "~F:Dalamud.Game.Text.SeStringHandling.Payload.DataResolver")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "breaking api change", Scope = "type", Target = "~T:Dalamud.Game.ClientState.Actors.Types.PartyMember")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "breaking api change", Scope = "member", Target = "~F:Dalamud.Game.Text.SeStringHandling.Payload.START_BYTE")]
[assembly: SuppressMessage("StyleCop.CSharp.NamingRules", "SA1310:Field names should not contain underscore", Justification = "breaking api change", Scope = "member", Target = "~F:Dalamud.Game.Text.SeStringHandling.Payload.END_BYTE")]
[assembly: SuppressMessage("CodeQuality", "IDE0052:Remove unread private members", Justification = "Unused, but eventually, maybe.", Scope = "member", Target = "~F:Dalamud.Game.ClientState.PartyList.address")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1204:Static elements should appear before instance elements", Justification = "breaking api change, move to util", Scope = "type", Target = "~T:Dalamud.Game.Text.EnumExtensions")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "breaking api change, move to util", Scope = "type", Target = "~T:Dalamud.Game.Text.EnumExtensions")]
[assembly: SuppressMessage("StyleCop.CSharp.OrderingRules", "SA1201:Elements should appear in the correct order", Justification = "breaking api change", Scope = "member", Target = "~F:Dalamud.Game.Internal.Framework.StatsHistory")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "breaking api change", Scope = "member", Target = "~F:Dalamud.Game.Internal.Framework.StatsHistory")]
[assembly: SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "breaking api change", Scope = "member", Target = "~F:Dalamud.Game.Internal.Framework.StatsHistory")]
[assembly: SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "Appears to be a bug, it is being used correctly", Scope = "member", Target = "~M:Dalamud.Data.DataManager.Initialize(System.String)")]

// I mostly didnt care to do these.
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "breaking api change", Scope = "member", Target = "~F:Dalamud.Game.Internal.Network.GameNetwork.OnNetworkMessage")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "breaking api change.", Scope = "type", Target = "~T:Dalamud.Game.Network.Structures.MarketBoardCurrentOfferings")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "breaking api change.", Scope = "type", Target = "~T:Dalamud.Game.Network.Structures.MarketBoardCurrentOfferings")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "breaking api change.", Scope = "type", Target = "~T:Dalamud.Game.Network.Structures.MarketBoardHistory")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "breaking api change.", Scope = "type", Target = "~T:Dalamud.Game.Network.Structures.MarketBoardHistory")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "breaking api change.", Scope = "type", Target = "~T:Dalamud.Game.Network.Structures.MarketTaxRates")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "breaking api change.", Scope = "type", Target = "~T:Dalamud.Game.Network.Structures.MarketTaxRates")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "breaking api change.", Scope = "type", Target = "~T:Dalamud.Game.Network.MarketBoardItemRequest")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "breaking api change.", Scope = "type", Target = "~T:Dalamud.Game.Network.MarketBoardItemRequest")]

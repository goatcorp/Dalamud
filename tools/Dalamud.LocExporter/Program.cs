// See https://aka.ms/new-console-template for more information

using CheapLoc;

Console.WriteLine("=> Starting loc export...");

var dalamud = typeof(Dalamud.Localization).Assembly;
Loc.ExportLocalizableForAssembly(dalamud, true);

Console.WriteLine("=> Finished loc export!");

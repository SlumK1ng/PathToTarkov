using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SPT.Reflection.Patching;
using EFT;
using EFT.Interactive;

namespace PTT.Patches;

internal class InitAllExfiltrationPointsPatch : ModulePatch
{
    private static ExfiltrationControllerClass _cachedController;
    private static MongoID _cachedLocationId;
    private static LocationExitClass[] _cachedSettings;
    private static bool _cachedGiveAuthority;

    private static bool IsNotScavExfil(ExfiltrationPoint x)
    {
        return x is not ScavExfiltrationPoint || x is SharedExfiltrationPoint;
    }

    private static bool IsScavExfil(ExfiltrationPoint x)
    {
        return x is ScavExfiltrationPoint;
    }

    protected override MethodBase GetTargetMethod()
    {
        return typeof(ExfiltrationControllerClass).GetMethod("InitAllExfiltrationPoints", BindingFlags.Public | BindingFlags.Instance);
    }

    [PatchPostfix]
    protected static void PatchPostfix(ref ExfiltrationControllerClass __instance, MongoID locationId, LocationExitClass[] settings, bool justLoadSettings = false, string disabledScavExits = "", bool giveAuthority = true)
    {
        // Store references for later filtering
        _cachedController = __instance;
        _cachedLocationId = locationId;
        _cachedSettings = settings;
        _cachedGiveAuthority = giveAuthority;

        // For now, just get all exfils without filtering - we'll filter them later when CurrentLocationDataService is initialized
        ExfiltrationPoint[] allExfils = GetAllExfilsForPmc();
        __instance.ExfiltrationPoints = allExfils;
        LoadExfilSettings(allExfils, locationId, settings, giveAuthority);
    }

    public static void ApplyExfilFiltering()
    {
        if (_cachedController == null || Plugin.CurrentLocationDataService == null)
        {
            Helpers.Logger.Warning("Cannot apply exfil filtering - controller or service not available");
            return;
        }

        ExfiltrationPoint[] allExfils = GetAllExfilsForPmcFiltered();
        _cachedController.ExfiltrationPoints = allExfils;
        LoadExfilSettings(allExfils, _cachedLocationId, _cachedSettings, _cachedGiveAuthority);
        
        Helpers.Logger.Info($"Applied exfil filtering - {allExfils.Length} exfils available");
    }

    private static ExfiltrationPoint[] GetAllExfilsForPmc()
    {
        ExfiltrationPoint[] allOriginalExfils = LocationScene.GetAllObjects<ExfiltrationPoint>(false).ToArray();
        IEnumerable<ExfiltrationPoint> scavExfils = allOriginalExfils.Where(new Func<ExfiltrationPoint, bool>(IsScavExfil));
        IEnumerable<ExfiltrationPoint> pmcExfils = allOriginalExfils.Where(new Func<ExfiltrationPoint, bool>(IsNotScavExfil));

        List<ExfiltrationPoint> accExfils = pmcExfils.ToList();

        foreach (ExfiltrationPoint scavExfil in scavExfils)
        {
            if (!pmcExfils.Any(k => k.Settings.Name == scavExfil.Settings.Name))
            {
                Helpers.Logger.Info($"Added scav exfil '{scavExfil.Settings.Name}' for pmc");
                accExfils.Add(scavExfil);
            }
        }

        // TODO: Fix filtering - need to filter after CurrentLocationDataService is initialized
        // For now, return all exfils to fix the issue where no exfils are shown
        return [.. accExfils];
    }

    private static ExfiltrationPoint[] GetAllExfilsForPmcFiltered()
    {
        ExfiltrationPoint[] allOriginalExfils = LocationScene.GetAllObjects<ExfiltrationPoint>(false).ToArray();
        IEnumerable<ExfiltrationPoint> scavExfils = allOriginalExfils.Where(new Func<ExfiltrationPoint, bool>(IsScavExfil));
        IEnumerable<ExfiltrationPoint> pmcExfils = allOriginalExfils.Where(new Func<ExfiltrationPoint, bool>(IsNotScavExfil));

        List<ExfiltrationPoint> accExfils = pmcExfils.ToList();

        foreach (ExfiltrationPoint scavExfil in scavExfils)
        {
            if (!pmcExfils.Any(k => k.Settings.Name == scavExfil.Settings.Name))
            {
                Helpers.Logger.Info($"Added scav exfil '{scavExfil.Settings.Name}' for pmc");
                accExfils.Add(scavExfil);
            }
        }

        IEnumerable<ExfiltrationPoint> filteredExfils = accExfils.Where(Plugin.CurrentLocationDataService.IsExfiltrationPointEnabled);
        return [.. filteredExfils];
    }

    private static void LoadExfilSettings(ExfiltrationPoint[] allExfils, MongoID locationId, LocationExitClass[] settings, bool giveAuthority)
    {
        foreach (ExfiltrationPoint exfiltrationPoint in allExfils)
        {
            LocationExitClass locationExit = settings.FirstOrDefault(exitClass => exitClass.Name == exfiltrationPoint.Settings.Name);

            if (locationExit != null)
            {
                int num = Array.IndexOf(allExfils, exfiltrationPoint) + 1;
                MongoID mongoID = locationId.Add(num + 1);
                exfiltrationPoint.LoadSettings(mongoID, locationExit, giveAuthority);
            }
        }
    }
}


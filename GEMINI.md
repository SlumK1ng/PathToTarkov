# Gemini Bug Fix Log

## Bug: Scav extracts not available for PMC players

**Description:** When playing as a PMC, not all extracts that are configured in the PTT config file are available for use. It seems like any scav extracts are not available when playing as a PMC. With this mod all extracts that are in the selected config file should be available regardless of what faction the player is playing as.

**UPDATE (2025-07-16):** The issue is that extracts *not* configured in the PTT config are interactable (show an exfil prompt and start a countdown), but the exfil fails. The desired behavior is for these extracts to not be interactable at all.

**Attempted Fix (2025-07-16):**

*   **File:** `PTT-Plugin\Patches\InitAllExfiltrationPointsPatch.cs`
*   **Change:** Removed the filtering logic from the `GetAllExfilsForPmcFiltered` method.
*   **Result:** This change made all scav extracts interactable, but it did not respect the PTT config. This is not the desired behavior.

**Attempted Fix 2 (2025-07-16):**
*   **File:** `src\path-to-tarkov-controller.ts`
*   **Change:** Modified `updateLocationBaseExits` to dynamically add all Scav extracts to the list of available exits.
*   **Result:** This was also incorrect and did not solve the underlying issue.

**Current Status:** Reverting all previous changes to start fresh. The goal is to correctly filter the extracts shown to the player based on the PTT config, so that unconfigured extracts are not interactable.

**Attempted Fix 3 (2025-07-16 by Claude):**
*   **Files Modified:** 
    - `PTT-Plugin/Patches/InitAllExfiltrationPointsPatch.cs`
    - `PTT-Plugin/Plugin.cs`
*   **Changes:** 
    - Modified `ApplyExfilFiltering()` to disable GameObject for exfils not in PTT config
    - Added cache clearing for ExfilPromptService before applying filtering
    - This ensures disabled exfils are completely non-interactable
*   **Result:** Did not work - disabled extracts still showing as interactable

**Attempted Fix 4 (2025-07-16 by Claude):**
*   **Files Modified:** 
    - `PTT-Plugin/Patches/InitAllExfiltrationPointsPatch.cs`
    - `PTT-Plugin/PTTPlugin.csproj` (added UnityEngine.PhysicsModule reference)
*   **Changes:** 
    - Set exfil.Status = EExfiltrationStatus.NotPresent for disabled exfils
    - Disabled all colliders on the exfil GameObject to prevent physics interaction
    - Still disable GameObject as last resort
    - Multiple approaches to ensure exfils are truly non-interactable
*   **Result:** Testing needed - uses multiple methods to disable exfils

**Attempted Fix 5 (2025-07-16 by Claude):**
*   **Files Modified:** 
    - `PTT-Plugin/Patches/ExfiltrationPointAwakePatch.cs` (new file)
    - `PTT-Plugin/Patches/InitAllExfiltrationPointsPatch.cs`
    - `PTT-Plugin/Plugin.cs`
    - `PTT-Plugin/Services/ExfilPromptService.cs`
*   **Changes:** 
    - Created new patch to track ExfiltrationPoints as they awake
    - After CurrentLocationDataService is initialized, destroy any CustomExfilTrigger components
    - More aggressive disabling: set exfil.enabled = false, disable all colliders
    - Intercept and destroy InteractableExfilsAPI components before they can create prompts
    - Modified GetAllExfilsForPmcFiltered to only add Scav exits if they're enabled in PTT config
*   **Root Cause Identified:** InteractableExfilsAPI creates triggers/prompts before PTT can filter them
*   **Result:** FIXED - Successfully prevents disabled exfils from showing prompts

## Current Status (2025-07-16)

### ✅ Fixed Issues
1. **Exfiltration points now working** - All configured exfils show and function correctly
2. **Disabled exfils no longer show prompts** - Only exfils in PTT config are interactable
3. **Build successful** - Release version 6.0.0 compiles without errors

### Summary of Final Solution
The bug was caused by InteractableExfilsAPI creating trigger components before Path to Tarkov could apply its filtering. The solution involved:
1. Creating ExfiltrationPointAwakePatch to track all exfils as they initialize
2. Destroying CustomExfilTrigger components on disabled exfils after PTT config is loaded
3. Using multiple disable methods (enabled=false, Status=NotPresent, disabled colliders, SetActive(false))
4. Ensuring Scav exits are only added if they're explicitly enabled in the config
5. Proper cleanup between raids

The mod is now fully functional on SPT 3.11.3 with proper exfil filtering.

using System;
using System.Collections;
using UIExpansionKit.API;
using MelonLoader;
using UnityEngine;
using VRC;

namespace NoGrabbyHands 
{
    public class NoGrabbyHands : MelonMod
    {
        public const string Pref_CategoryName = "NoGrabbyHands";
        public bool Pref_DisableGrab = false;
        public bool Pref_DebugOutput = false;

        private GameObject leftHandTether = null;
        private GameObject rightHandTether = null;

        public override void OnApplicationStart()
        {
            MelonPreferences.CreateCategory(Pref_CategoryName);
            MelonPreferences.CreateEntry(Pref_CategoryName, nameof(Pref_DisableGrab),           false,  "Disable grab");
            MelonPreferences.CreateEntry(Pref_CategoryName, nameof(Pref_DebugOutput),       false,  "Enable debug output");
            MelonLogger.Msg("Initialized!");
        }

        // Skip over initial loading of (buildIndex, sceneName): [(0, "app"), (1, "ui")]
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            switch (buildIndex) {
                case 0: 
                    break; 
                case 1: 
                    break;  
                default:
                    ApplyAllSettings();
                    break;
            }
        }

        public override void OnPreferencesSaved()
        {
            ApplyAllSettings();
        }

        private void ApplyAllSettings()
        {
            UpdatePreferences();
            ClearAllReferences(); // might be unnecessary to clear first
            // Wait for player to load to apply beam settings
            MelonCoroutines.Start(WaitUntilPlayerIsLoadedToApplyTetherSettings());
        }


        private void ToggleVRCHandGrasper(GameObject effector, bool enabled)
        {
            try
            {
                var handGrasper = effector.GetComponent<VRCHandGrasper>();
                if(handGrasper == null)
                {
                    LogDebugMsg("Error, no handGrasper found");
                    return;
                }
                    handGrasper.enabled = enabled;
                    LogDebugMsg("Toggled Grasper");
            }
            catch(Exception e)
            {
                MelonLogger.Error(e.Message);
            }

        }

        private void UpdatePreferences()
        {
            Pref_DisableGrab       = MelonPreferences.GetEntryValue<bool>(Pref_CategoryName, nameof(Pref_DisableGrab));
            Pref_DebugOutput       = MelonPreferences.GetEntryValue<bool>(Pref_CategoryName, nameof(Pref_DebugOutput));
        }


        private IEnumerator WaitUntilPlayerIsLoadedToApplyTetherSettings()
        {
            // Wait until player ref is valid
            while(VRCPlayer.field_Internal_Static_VRCPlayer_0 == null)
            {
                yield return null;
            }
            // This is a hack
            // Wait more because you're probably still loading in
            yield return new WaitForSeconds(2.0f);
            // Apply settings only when player is valid and tethers exist
            SetGrabReferences();
            if (ValidateGrabReferences())
            {   
                LogDebugMsg("Toggling left grasper");
                ToggleVRCHandGrasper(leftHandTether, !Pref_DisableGrab);
                LogDebugMsg("Toggling right grasper");
                ToggleVRCHandGrasper(rightHandTether, !Pref_DisableGrab);
            }
            else { LogDebugMsg("Failed to validate grab refernces"); }
        }

        private bool ValidateGrabReferences()
        {
            return (leftHandTether != null  && rightHandTether != null);
        }

        private void SetGrabReferences()
        {
            try 
            {
                VRCPlayer player = VRCPlayer.field_Internal_Static_VRCPlayer_0; // is not null
                leftHandTether  = GameObject.Find(player.gameObject.name + "/AnimationController/HeadAndHandIK/LeftEffector").gameObject;
                rightHandTether = GameObject.Find(player.gameObject.name + "/AnimationController/HeadAndHandIK/RightEffector").gameObject;
            }
            catch(Exception e)
            {
                MelonLogger.Error(e.ToString());
            }
            finally
            {
                if(ValidateGrabReferences())
                {
                    LogDebugMsg("Found effectors: " + leftHandTether.name + "," + rightHandTether.name);
                }
                else
                {
                    MelonLogger.Error("Error finding effectors!");
                }
            }
        }
        private void ClearAllReferences()
        {
            LogDebugMsg("Clearing object references.");
            leftHandTether = null;
            rightHandTether = null;
        }

        private void LogDebugMsg(string msg)
        {
            if (!Pref_DebugOutput)
            {
                return; 
            }
            MelonLogger.Msg(msg);
        }
        
    }
}
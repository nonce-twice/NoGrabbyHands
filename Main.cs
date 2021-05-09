using MelonLoader;
using System;
using System.Collections;
using UnityEngine;

namespace NoGrabbyHands
{
    public class NoGrabbyHands : MelonMod
    {
        public const string Pref_CategoryName = "NoGrabbyHands";
        public bool Pref_DisableGrab = false;
        public bool Pref_DebugOutput = false;

        private bool isLoading = true;

        private GameObject leftHandTether = null;
        private GameObject rightHandTether = null;
        private VRCHandGrasper leftHandGrasper = null;
        private VRCHandGrasper rightHandGrasper = null;

        public override void OnApplicationStart()
        {
            MelonPreferences.CreateCategory(Pref_CategoryName);
            MelonPreferences.CreateEntry(Pref_CategoryName, nameof(Pref_DisableGrab), false, "Disable grab");
            MelonPreferences.CreateEntry(Pref_CategoryName, nameof(Pref_DebugOutput), false, "Enable debug output");
            MelonLogger.Msg("Initialized!");
        }

        // Skip over initial loading of (buildIndex, sceneName): [(0, "app"), (1, "ui")]
        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            base.OnSceneWasLoaded(buildIndex, sceneName);
            switch (buildIndex)
            {
                case 0:
                    isLoading = true;
                    break;

                case 1:
                    isLoading = true;
                    break;

                default:
                    isLoading = false;
                    ApplyAllSettings();
                    break;
            }
        }

        public override void OnPreferencesSaved()
        {
            ApplyAllSettings();
        }

        private void UpdatePreferences()
        {
            Pref_DisableGrab = MelonPreferences.GetEntryValue<bool>(Pref_CategoryName, nameof(Pref_DisableGrab));
            Pref_DebugOutput = MelonPreferences.GetEntryValue<bool>(Pref_CategoryName, nameof(Pref_DebugOutput));
        }

        private void ApplyAllSettings()
        {
            UpdatePreferences();
            ClearAllReferences(); // might be unnecessary to clear first
            // Wait for player to load to apply beam settings
            MelonCoroutines.Start(GrabSettingsCoroutine());
        }

        private void ToggleGrabEnabled(bool enabled)
        {
            try
            {
                leftHandGrasper.enabled = enabled;
                rightHandGrasper.enabled = enabled;
                LogDebugMsg("Graspers " + (enabled ? "enabled" : "disabled"));
            }
            catch (Exception e)
            {
                MelonLogger.Error(e.Message);
            }
        }

        private VRCHandGrasper GetHandGrasper(GameObject effector)
        {
            var handGrasper = effector.GetComponent<VRCHandGrasper>();
            return handGrasper;
        }

        private bool GrasperIsGrabbing(VRCHandGrasper grasper)
        {
            if (grasper == null)
            {
                return false;
            }
            return grasper.field_Internal_VRC_Pickup_0 != null;
        }

        private IEnumerator GrabSettingsCoroutine()
        {
            while (isLoading)
            {
                LogDebugMsg("Waiting for scene to load...");
                yield return new WaitForSeconds(5.0f);
            }

            while (VRCPlayer.field_Internal_Static_VRCPlayer_0 == null)
            {
                yield return null;
            }
            LogDebugMsg("VRCPlayer valid! Setting effector references...");

            while (!SetEffectorReferences())
            {
                yield return new WaitForSeconds(1.0f);
            }

            LogDebugMsg("Effector references set! Setting grasper references...");

            while (!SetGrasperReferences())
            {
                yield return new WaitForSeconds(1.0f);
            }
            LogDebugMsg("Grasper references set! Toggling grab");

            while (GrasperIsGrabbing(leftHandGrasper) || GrasperIsGrabbing(rightHandGrasper))
            {
                yield return null;
            }

            ToggleGrabEnabled(!Pref_DisableGrab);
        }

        private bool ValidateGrabReferences()
        {
            return (leftHandTether != null && rightHandTether != null
                && leftHandGrasper != null && rightHandGrasper != null);
        }

        private bool SetEffectorReferences()
        {
            if (leftHandTether != null && rightHandTether != null)
            {
                return true;
            }
            try
            {
                VRCPlayer player = VRCPlayer.field_Internal_Static_VRCPlayer_0; // is not null
                leftHandTether = GameObject.Find(player.gameObject.name + "/AnimationController/HeadAndHandIK/LeftEffector").gameObject;
                rightHandTether = GameObject.Find(player.gameObject.name + "/AnimationController/HeadAndHandIK/RightEffector").gameObject;
                return (leftHandTether != null && rightHandTether != null);
            }
            catch (Exception e)
            {
                MelonLogger.Error(e.ToString());
                return false;
            }
        }

        private bool SetGrasperReferences()
        {
            if (leftHandGrasper != null && rightHandGrasper != null)
            {
                return true;
            }
            try
            {
                leftHandGrasper = GetHandGrasper(leftHandTether);
                rightHandGrasper = GetHandGrasper(rightHandTether);
                return (leftHandGrasper != null && rightHandGrasper != null);
            }
            catch (Exception e)
            {
                MelonLogger.Error(e.ToString());
                return false;
            }
        }

        private void SetGrabReferences()
        {
            try
            {
                VRCPlayer player = VRCPlayer.field_Internal_Static_VRCPlayer_0; // is not null
                leftHandTether = GameObject.Find(player.gameObject.name + "/AnimationController/HeadAndHandIK/LeftEffector").gameObject;
                rightHandTether = GameObject.Find(player.gameObject.name + "/AnimationController/HeadAndHandIK/RightEffector").gameObject;
                leftHandGrasper = GetHandGrasper(leftHandTether);
                rightHandGrasper = GetHandGrasper(rightHandTether);
            }
            catch (Exception e)
            {
                MelonLogger.Error(e.ToString());
            }
            finally
            {
                if (ValidateGrabReferences())
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
            leftHandGrasper = null;
            rightHandGrasper = null;
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
using System;
using Fusion;
using Fusion.Photon.Realtime;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ThirdPersonCam;

[HarmonyPatch(typeof(CameraControl), nameof(CameraControl.LateUpdate))]
static class CameraControlUpdatePatch
{
    private static ThirdPersonCamPlugin.Toggle originalViewMode;
    private static bool toggledToThirdPerson = false;

    static void Prefix(CameraControl __instance)
    {
        __instance.thirdPersonActive = ThirdPersonCamPlugin.toggle3rdPerson.Value == ThirdPersonCamPlugin.Toggle.On;
        __instance.allowThirdPerson = ThirdPersonCamPlugin.toggle3rdPerson.Value == ThirdPersonCamPlugin.Toggle.On;
    }

    static void Postfix(CameraControl __instance)
    {
        __instance.thirdPersonActive = ThirdPersonCamPlugin.toggle3rdPerson.Value == ThirdPersonCamPlugin.Toggle.On;
        __instance.allowThirdPerson = ThirdPersonCamPlugin.toggle3rdPerson.Value == ThirdPersonCamPlugin.Toggle.On;
        if (Global.code.OnGUI) return;
        if (ThirdPersonCamPlugin.toggle3rdPersonKeys.Value.IsDown())
        {
            // Toggle third person and set the value in the config opposite of what is there
            ThirdPersonCamPlugin.toggle3rdPerson.Value = ThirdPersonCamPlugin.toggle3rdPerson.Value == ThirdPersonCamPlugin.Toggle.On ? ThirdPersonCamPlugin.Toggle.Off : ThirdPersonCamPlugin.Toggle.On;
            ThirdPersonCamPlugin.ToggleThird(ThirdPersonCamPlugin.toggle3rdPerson.Value == ThirdPersonCamPlugin.Toggle.On);
            Global.code.uiInventory.Refresh();
        }

        if (Global.code.uiCombat.VehicleGroup.activeSelf && ThirdPersonCamPlugin.autoToggle.Value == ThirdPersonCamPlugin.Toggle.On)
        {
            // Only store the original view mode and toggle to third person once.
            if (!toggledToThirdPerson)
            {
                originalViewMode = ThirdPersonCamPlugin.toggle3rdPerson.Value;
                ThirdPersonCamPlugin.toggle3rdPerson.Value = ThirdPersonCamPlugin.Toggle.On;
                ThirdPersonCamPlugin.ToggleThird(true);
                Global.code.uiInventory.Refresh();
                toggledToThirdPerson = true;
            }
        }
        else if (toggledToThirdPerson) // Only revert to original view once.
        {
            ThirdPersonCamPlugin.toggle3rdPerson.Value = originalViewMode;
            ThirdPersonCamPlugin.ToggleThird(originalViewMode == ThirdPersonCamPlugin.Toggle.On);
            toggledToThirdPerson = false; // Reset the flag after reverting to original view.
        }

        if (ThirdPersonCamPlugin.scrollingCamera.Value == ThirdPersonCamPlugin.Toggle.Off) return;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        __instance.currentDistance -= (scroll * __instance.zoomDistance) * ThirdPersonCamPlugin.scrollingSensitivity.Value;
        __instance.currentDistance = Mathf.Clamp(__instance.currentDistance, 0.0f, 5f);
    }
}

[HarmonyPatch(typeof(CameraControl), nameof(CameraControl.Update))]
static class CameraControlUpdateModePatch
{
    static void Prefix(CameraControl __instance)
    {
        __instance.thirdPersonActive = ThirdPersonCamPlugin.toggle3rdPerson.Value == ThirdPersonCamPlugin.Toggle.On;
    }

    static void Postfix(CameraControl __instance)
    {
        __instance.thirdPersonActive = ThirdPersonCamPlugin.toggle3rdPerson.Value == ThirdPersonCamPlugin.Toggle.On;
    }
}

[HarmonyPatch(typeof(PlayerDummy), nameof(PlayerDummy.RefreshNetworkingDummy))]
static class PlayerDummyMountSkinPatch
{
    static bool Prefix(PlayerDummy __instance, Changed<PlayerDummy> changed)
    {
        if (ThirdPersonCamPlugin.toggle3rdPerson.Value != ThirdPersonCamPlugin.Toggle.On)
        {
            changed.Behaviour.tempClothes.DeleteItems();
            return true;
        }

        changed.Behaviour.anim.avatar = changed.Behaviour.avatarNormal;
        changed.Behaviour.tempClothes.DeleteItems();
        if (changed.Behaviour.HelmetIndex != -1)
            PlayerDummy.GenerateTempClothes(changed.Behaviour, changed.Behaviour.HelmetIndex, changed.Behaviour.IsFemale);
        if (changed.Behaviour.ArmorIndex != -1)
            PlayerDummy.GenerateTempClothes(changed.Behaviour, changed.Behaviour.ArmorIndex, changed.Behaviour.IsFemale);
        if (changed.Behaviour.ClothesIndex != -1)
            PlayerDummy.GenerateTempClothes(changed.Behaviour, changed.Behaviour.ClothesIndex, changed.Behaviour.IsFemale);
        if (changed.Behaviour.MaskIndex != -1)
            PlayerDummy.GenerateTempClothes(changed.Behaviour, changed.Behaviour.MaskIndex, changed.Behaviour.IsFemale);
        if (changed.Behaviour.ShoesIndex != -1)
            PlayerDummy.GenerateTempClothes(changed.Behaviour, changed.Behaviour.ShoesIndex, changed.Behaviour.IsFemale);
        if (changed.Behaviour.GlovesIndex != -1)
            PlayerDummy.GenerateTempClothes(changed.Behaviour, changed.Behaviour.GlovesIndex, changed.Behaviour.IsFemale);
        if (changed.Behaviour.PantsIndex != -1)
            PlayerDummy.GenerateTempClothes(changed.Behaviour, changed.Behaviour.PantsIndex, changed.Behaviour.IsFemale);
        if (changed.Behaviour.BackpackIndex != -1)
            PlayerDummy.GenerateTempClothes(changed.Behaviour, changed.Behaviour.BackpackIndex, changed.Behaviour.IsFemale);
        if (changed.Behaviour.HelmetIndex == -1)
            PlayerDummy.GenerateHair(changed.Behaviour, changed.Behaviour.HairIndex, changed.Behaviour.IsFemale, changed.Behaviour.HairColor);
        if (changed.Behaviour.IsFemale)
        {
            Transform original = Object.Instantiate(RM.code.skinsFemale.items[changed.Behaviour.SkinIndex]);
            PlayerDummy.MountSkin(original, changed.Behaviour.transform);
            changed.Behaviour.tempClothes.AddItem(original);
        }
        else
        {
            Transform original = Object.Instantiate(RM.code.skinsMale.items[changed.Behaviour.SkinIndex]);
            PlayerDummy.MountSkin(original, changed.Behaviour.transform);
            changed.Behaviour.tempClothes.AddItem(original);
        }

        if ((bool)(Object)changed.Behaviour.bra)
            changed.Behaviour.bra.SetActive(changed.Behaviour.ClothesIndex == -1);
        if (!(bool)(Object)changed.Behaviour.panties)
            return true;
        changed.Behaviour.panties.SetActive(changed.Behaviour.PantsIndex == -1);
        return false;
    }
}

[HarmonyPatch(typeof(PlayerDummy), nameof(PlayerDummy.WeaponInHandChanged))]
static class PlayerDummyWeaponInHandChangedPatch
{
    static bool Prefix(PlayerDummy __instance, Changed<PlayerDummy> changed)
    {
        try
        {
            if (!changed.Behaviour.Object)
                return true;
            if (ThirdPersonCamPlugin.toggle3rdPerson.Value != ThirdPersonCamPlugin.Toggle.On)
            {
                if (changed.Behaviour.weaponInHand)
                    Object.Destroy(changed.Behaviour.weaponInHand.gameObject);
                return true;
            }

            if (changed.Behaviour.weaponInHand)
                Object.Destroy(changed.Behaviour.weaponInHand.gameObject);
            if (changed.Behaviour.WeaponInHandIndex == -1)
                return true;
            changed.Behaviour.weaponInHand = Utility.Instantiate(RM.code.allItems.items[changed.Behaviour.WeaponInHandIndex]);
            switch (changed.Behaviour.weaponInHand.GetComponent<WeaponRaycast>().weaponIndex)
            {
                case 1:
                    changed.Behaviour.weaponInHand.SetParent(changed.Behaviour.rGripRifle);
                    break;
                case 2:
                    changed.Behaviour.weaponInHand.SetParent(changed.Behaviour.rGrip);
                    break;
                case 3:
                    changed.Behaviour.weaponInHand.SetParent(changed.Behaviour.rGrip);
                    break;
                case 4:
                    changed.Behaviour.weaponInHand.SetParent(changed.Behaviour.rGripSpear);
                    break;
                case 5:
                    changed.Behaviour.weaponInHand.SetParent(changed.Behaviour.rGripTwohandAxe);
                    break;
                case 6:
                    changed.Behaviour.weaponInHand.SetParent(changed.Behaviour.lGrip);
                    break;
                case 11:
                    changed.Behaviour.weaponInHand.SetParent(changed.Behaviour.rGrip);
                    break;
                case 12:
                    changed.Behaviour.weaponInHand.SetParent(changed.Behaviour.rGrip);
                    break;
            }

            if (changed.Behaviour.weaponInHand.TryGetComponent(out Rigidbody component1))
                Object.Destroy(component1);
            if (changed.Behaviour.weaponInHand.TryGetComponent(out Interaction component2))
                Object.Destroy(component2);
            if (changed.Behaviour.weaponInHand.TryGetComponent(out Outline component3))
                Object.Destroy(component3);
            changed.Behaviour.weaponInHand.localEulerAngles = Vector3.zero;
            changed.Behaviour.weaponInHand.localPosition = Vector3.zero;
            changed.Behaviour.weaponInHand.localScale = Vector3.one;
            if ((bool)(Object)changed.Behaviour.weaponInHand.GetComponent<Collider>())
                changed.Behaviour.weaponInHand.GetComponent<Collider>().enabled = false;
            foreach (Collider componentsInChild in (Component[])changed.Behaviour.weaponInHand.GetComponentsInChildren<Collider>())
                componentsInChild.enabled = false;
            changed.Behaviour._WeaponRaycast = changed.Behaviour.weaponInHand.GetComponent<WeaponRaycast>();
            return false;
        }
        catch
        {
            return false;
        }
    }
}

[HarmonyPatch(typeof(PlayerDummy), nameof(PlayerDummy.RefreshCharacterModel))]
static class PlayerDummyRefreshCharacterModelPatch
{
    static bool Prefix(PlayerDummy __instance)
    {
        return !Mainframe.code.HasSpawnedPlayerDummy; // Causes some issues when refreshing the character. Look into a better way to do this.
    }
}
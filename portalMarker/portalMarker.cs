using BepInEx;
using HarmonyLib;
using System.Reflection;
using UnityEngine;


namespace PortalMarker
{
    [BepInPlugin("sebdei.portalmarker", "Marks all portals on the minimap when renaming and using them", "1.1.0")]
    [BepInProcess("valheim.exe")]
    public class PortalMarker : BaseUnityPlugin
    {
        void Awake()
        {
            Harmony harmony = new Harmony("sebdei.portalmarker");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(TeleportWorld), "SetText")]
        static class PinOnEditName
        {
            static void Postfix(string text, TeleportWorld __instance)
            {
                Vector3? position = __instance?.gameObject?.transform?.position;
              
                if (position.HasValue)
                {
                    ReplacePin(position.Value, text);
                }
            }
        }

        [HarmonyPatch(typeof(TeleportWorld), "Teleport")]
        static class PinOnUse
        {
            static void Postfix(TeleportWorld __instance, ZNetView ___m_nview)
            {
                string text = __instance.GetText();

                if (text != null)
                {
                    ReplaceOrigin(__instance, text);
                    ReplaceTarget(___m_nview, text);
                }
            }

            private static void ReplaceTarget(ZNetView ___m_nview, string text)
            {
                ZDOID zDOID = ___m_nview.GetZDO().GetZDOID("target");
                ZDO zDO = zDOID == null ? null : ZDOMan.instance.GetZDO(zDOID);
                Vector3? targetPosition = zDO?.GetPosition();

                if (targetPosition.HasValue)
                {
                    ReplacePin(targetPosition.Value, text);
                }
            }

            private static void ReplaceOrigin(TeleportWorld __instance, string text)
            {
                Vector3? originPosition = __instance?.gameObject?.transform?.position;

                if (originPosition.HasValue)
                {
                    ReplacePin(originPosition.Value, text);
                }
            }
        }

        [HarmonyPatch(typeof(Piece), "OnDestroy")]
        static class UnpinOnDestroy
        {
            static void Prefix(Piece __instance)
            {
                GameObject gameObject = __instance.gameObject;

                if (gameObject != null && gameObject.name == "portal_wood")
                {
                    TeleportWorld teleportWorld = gameObject.GetComponent<TeleportWorld>();
                    Vector3? position = teleportWorld?.transform?.position;

                    if (position.HasValue)
                    {
                        RemovePin(position.Value);
                    }
                }
            }
        }

        private static void RemovePin(Vector3 position)
        {
            var flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
            Minimap.PinData pin = (Minimap.PinData)typeof(Minimap).GetMethod("GetClosestPin", flags).Invoke(Minimap.instance, new object[] { position, 1L });

            if (pin != null)
            {
                Minimap.instance.RemovePin(pin);
            }
        }

        private static void ReplacePin(Vector3 position, string text)
        {
            RemovePin(position);
            AddPin(position, text);
        }

        private static void AddPin(Vector3 position, string text)
        {
            Minimap.instance.AddPin(position, Minimap.PinType.Icon4, text, true, false, 0L);
        }
    }
}

using System;
using System.Reflection;
using System.Text;
using HarmonyLib;
using TMPro;
using UnityEngine;
using static ObjectPing.ObjectPingPlugin;
using Object = UnityEngine.Object;

namespace ObjectPing;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
static class ZNetScene_Awake_Patch
{
    private static GameObject placementEffect = new("PingVisualEffect");
    private static GameObject visualEffect = new("PingVisualEffect");
    private static GameObject visualEffect2 = new("PingVisualEffect2");
    private static GameObject soundEffect = new("PingSoundEffect");

    static void Postfix(ZNetScene __instance)
    {
        _placementmarkerContainer.SetActive(false);

        Player? p = Game.instance.m_playerPrefab.GetComponent<Player>();
        if (p.m_placementMarkerInstance == null)
            p.m_placementMarkerInstance = Object.Instantiate(p.m_placeMarker, _placementmarkerContainer.transform, false);

        _placementmarkercopy.transform.SetParent(_placementmarkerContainer.transform);
        /* Add components to the main prefab */
        var zNetView = _placementmarkercopy.AddComponent<ZNetView>();
        zNetView.m_persistent = true;
        zNetView.m_distant = true;
        zNetView.m_syncInitialScale = true;
        PingDestruction pingDestruction = _placementmarkercopy.AddComponent<PingDestruction>();
        pingDestruction.m_triggerOnAwake = true;
        pingDestruction.m_timeout = 5f;

        /* Create the marker for ping and set the parent to our main GO */
        placementEffect = Object.Instantiate(p.m_placementMarkerInstance, _placementmarkercopy.transform, false);
        placementEffect.transform.localPosition = Vector3.zero;
        placementEffect.transform.localScale = new Vector3(10f, 10f, 10f);

        /* Clone the sledge hit for the ping visual effect and set the parent to our main GO */
        GameObject fetch = __instance.GetPrefab("vfx_sledge_hit");
        GameObject fetch2 = fetch.transform.Find("waves").gameObject;
        visualEffect = Object.Instantiate(fetch2, _placementmarkercopy.transform, false);

        /* Clone the lootspawn sound effect and set the parent to our main GO */
        GameObject fetchSound = __instance.GetPrefab("sfx_lootspawn");
        soundEffect = Object.Instantiate(fetchSound, _placementmarkercopy.transform, false);
        // Remove the ZNetView from the sound effect
        Object.Destroy(soundEffect.GetComponent<ZNetView>());
        ZSFX audioModule = soundEffect.GetComponent<ZSFX>();

        /* Clone the ward's activation effect since the flash effect isn't a prefab */
        GameObject fetch3 = __instance.GetPrefab("fx_guardstone_activate");
        visualEffect2 = Object.Instantiate(fetch3, _placementmarkercopy.transform, false);
        Object.Destroy(visualEffect2.GetComponent<ZNetView>());
        visualEffect2.transform.localPosition = new Vector3(0f, 0f, 0f);
        visualEffect2.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        ParticleSystem.MainModule mainModule = visualEffect2.gameObject.transform.Find("Dome").GetComponent<ParticleSystem>().main;
        mainModule.startColor = new ParticleSystem.MinMaxGradient(Color.red);

        //Adjusting the audio settings to give it some cool reverb.
        audioModule.m_minPitch = 0.8F;
        audioModule.m_maxPitch = 0.85F;
        audioModule.m_distanceReverb = true;
        audioModule.m_vol = 1F;
        audioModule.m_useCustomReverbDistance = true;
        audioModule.m_customReverbDistance = 10F;
        audioModule.m_delay = 1;
        audioModule.m_time = 1;

        /* Add that shit to ZNetScene */
        __instance.m_namedPrefabs.Add(_placementmarkercopy.name.GetStableHashCode(), _placementmarkercopy);
    }
}

[HarmonyPatch(typeof(Hud), nameof(Hud.UpdateCrosshair))]
static class PlayerUpdatePatch
{
    private static Camera? _cam;
    private static int _layermask = 1;
    private const int Layer1 = 1;
    private const int Layer2 = 2;
    private const int Layer3 = 4;
    private const int Layer4 = 5;
    private const int Layer5 = 8;
    private const int Layer6 = 13;
    private const int Layer7 = 14;
    private const int Layer8 = 16;
    private const int Layer9 = 17;
    private const int Layer10 = 18;
    private const int Layer11 = 19;
    private const int Layer12 = 21;
    private const int Layer13 = 23;
    private const int Layer14 = 24;
    private const int Layer15 = 26;
    private const int Layer16 = 27;
    private const int Layer17 = 31;
    static int _layermask1 = 1 << Layer1;
    static int _layermask2 = 1 << Layer2;
    static int _layermask3 = 1 << Layer3;
    static int _layermask4 = 1 << Layer4;
    static int _layermask5 = 1 << Layer5;
    static int _layermask6 = 1 << Layer6;
    static int _layermask7 = 1 << Layer7;
    static int _layermask8 = 1 << Layer8;
    static int _layermask9 = 1 << Layer9;
    static int _layermask10 = 1 << Layer10;
    static int _layermask11 = 1 << Layer11;
    static int _layermask12 = 1 << Layer12;
    static int _layermask13 = 1 << Layer13;
    static int _layermask14 = 1 << Layer14;
    static int _layermask15 = 1 << Layer15;
    static int _layermask16 = 1 << Layer16;
    static int _layermask17 = 1 << Layer17;

    static int _finalmask = ~(_layermask1 | _layermask2 | _layermask3 | _layermask4 | _layermask5
                              | _layermask6 | _layermask7 | _layermask8 | _layermask9 | _layermask10
                              | _layermask11 | _layermask12 | _layermask13 | _layermask14 | _layermask15
                              | _layermask16 | _layermask17);

    private static Transform visualEffect = null!;

    static void Postfix(Hud __instance, Player player, float bowDrawPercentage)
    {
        if (!_keyboardShortcut.Value.IsKeyDown() || !player.TakeInput()) return;
        _cam = GameCamera.instance.m_camera;
        if (_cam == null) return;
        if (_cam.transform == null) return;
        Transform transform = _cam.transform;
        _cam.ScreenPointToRay(transform.position);
        if (!Physics.Raycast(transform.position, transform.forward, out RaycastHit raycastHit, Mathf.Infinity, _finalmask))
            return;
        Vector3 point = raycastHit.point;
        string taggedFab = raycastHit.collider.transform.root.gameObject.name.Replace("(Clone)", "");
        if (taggedFab == "Player")
            return;
        string localizedToken = CheckAndGrab(raycastHit.collider.transform.root.gameObject);
        taggedFab = string.IsNullOrEmpty(localizedToken) ? taggedFab : localizedToken;
        ObjectPingLogger.LogDebug($"You targeted {taggedFab}");
        GameObject fetch = ZNetScene.instance.GetPrefab("PingPrefab");
        Quaternion quaternion = Quaternion.Euler(0.0f, 22.5f * (float)16, 0.0f);

        Object.Instantiate(fetch, point, Quaternion.identity).transform.rotation = Quaternion.LookRotation(raycastHit.transform.forward, raycastHit.normal);
        DoPingText(point, taggedFab);
    }

    public static void DoPingText(Vector3 pos, string taggedFab = "")
    {
        DamageText.WorldTextInstance worldTextInstance = new()
        {
            m_worldPos = pos,
            m_gui = Object.Instantiate(DamageText.instance.m_worldTextBase, DamageText.instance.transform)
        };
        worldTextInstance.m_textField = worldTextInstance.m_gui.GetComponent<TMP_Text>();
        DamageText.instance.m_worldTexts.Add(worldTextInstance);
        worldTextInstance.m_textField.color = ObjectPingPlugin.TextColor.Value;
        worldTextInstance.m_textField.fontSize = 24;
        worldTextInstance.m_textField.text = taggedFab;
        worldTextInstance.m_timer = -2f;
    }

    private static string CheckAndGrab(GameObject obj)
    {
        Type[] componentTypes = new Type[]
        {
            typeof(Fermenter),
            typeof(Piece),
            typeof(Fireplace),
            typeof(CookingStation),
            typeof(MineRock5),
            typeof(MineRock),
            typeof(Destructible),
            typeof(TreeLog),
            typeof(TreeBase),
            typeof(Pickable),
            typeof(Plant),
            typeof(Beehive),
            typeof(SapCollector),
            typeof(HoverText),
            typeof(Character),
        };

        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // TODO: Add helper methods to write this better/split it out. Too damn lazy at the moment.
        foreach (Type type in componentTypes)
        {
            Component component = obj.GetComponent(type);
            if (component != null)
            {
                // Try to get the value of the m_name field
                FieldInfo fieldInfo = type.GetField("m_name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    string name = (string)fieldInfo.GetValue(component);
                    sb.AppendLine(name.StartsWith("$") ? Localization.instance.Localize(name) : name);
                }
                else
                {
                    // If m_name is not a field, try to get it as a property
                    PropertyInfo propInfo = type.GetProperty("m_name", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (propInfo != null && propInfo.PropertyType == typeof(string))
                    {
                        string name = (string)propInfo.GetValue(component);
                        sb.AppendLine(name.StartsWith("$") ? Localization.instance.Localize(name) : name);
                    }
                    else
                    {
                        FieldInfo fieldInfoText = type.GetField("m_text", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (fieldInfoText != null)
                        {
                            string name = (string)fieldInfoText.GetValue(component);
                            sb.AppendLine(name.StartsWith("$") ? Localization.instance.Localize(name) : name);
                        }
                        else
                        {
                            // Attempt to use a GetHoverName method in the component
                            MethodInfo methodInfoHoverName = type.GetMethod("GetHoverName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (methodInfoHoverName != null && methodInfoHoverName.ReturnType == typeof(string))
                            {
                                string name = (string)methodInfoHoverName.Invoke(component, null);
                                sb.AppendLine(name.StartsWith("$") ? Localization.instance.Localize(name) : name);
                            }
                            else
                            {
                                MethodInfo methodInfoHoverText = type.GetMethod("GetHoverText", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                                if (methodInfoHoverText != null && methodInfoHoverText.ReturnType == typeof(string))
                                {
                                    string name = (string)methodInfoHoverText.Invoke(component, null);
                                    sb.AppendLine(name.StartsWith("$") ? Localization.instance.Localize(name) : name);
                                }
                            }
                        }
                    }
                }
            }
        }

        return sb.ToString().TrimEnd();
    }
}
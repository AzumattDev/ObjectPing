using HarmonyLib;
using UnityEngine;
using static ObjectPing.ObjectPingPlugin;

namespace ObjectPing;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
static class ZNetScene_Awake_Patch
{
    private static GameObject visualEffect = new("PingVisualEffect");
    private static GameObject soundEffect = new("PingSoundEffect");

    static void Postfix(ZNetScene __instance)
    {
        
        _placementmarkerContainer.SetActive(false);

        Player? p = Game.instance.m_playerPrefab.GetComponent<Player>();
        if (p.m_placementMarkerInstance == null)
            p.m_placementMarkerInstance =
                Object.Instantiate(p.m_placeMarker, _placementmarkerContainer.transform, false);
        
        _placementmarkercopy.transform.SetParent(_placementmarkerContainer.transform);
        /* Add components to the main prefab */
        _placementmarkercopy.AddComponent<ZNetView>();
        TimedDestruction timedDestruction = _placementmarkercopy.AddComponent<TimedDestruction>();
        timedDestruction.m_triggerOnAwake = true;
        timedDestruction.m_timeout = 5f;

        /* Create the marker for ping and set the parent to our main GO */
        Object.Instantiate(p.m_placementMarkerInstance, _placementmarkercopy.transform, false).transform
            .localPosition = Vector3.zero;

        /* Clone the sledge hit for the ping visual effect and set the parent to our main GO */
        GameObject fetch = __instance.GetPrefab("vfx_sledge_hit");
        GameObject fetch2 = fetch.transform.Find("waves").gameObject;
        visualEffect = Object.Instantiate(fetch2, _placementmarkercopy.transform, false);

        /* Clone the lootspawn sound effect and set the parent to our main GO */
        GameObject fetchSound = __instance.GetPrefab("sfx_lootspawn");
        soundEffect = Object.Instantiate(fetchSound, _placementmarkercopy.transform, false);
        ZSFX audioModule = soundEffect.GetComponent<ZSFX>();

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
        __instance.m_namedPrefabs.Add(_placementmarkercopy.name.GetStableHashCode(),
            _placementmarkercopy);
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

    static int _finalmask = ~(_layermask1 | _layermask2 | _layermask3 | _layermask4 | _layermask5 | _layermask6 |
                              _layermask7 |
                              _layermask8 | _layermask9 | _layermask10 | _layermask11 | _layermask12 | _layermask13 |
                              _layermask14 | _layermask15 | _layermask16 | _layermask17);

    private static Transform visualEffect = null!;

    static void Postfix(Hud __instance, Player player, float bowDrawPercentage)
    {
        if (!_keyboardShortcut.Value.IsDown()) return;
        _cam = GameCamera.instance.m_camera;
        if (_cam?.transform == null) return;
        Transform? transform = _cam?.transform;
        _cam.ScreenPointToRay(transform.position);
        if (!Physics.Raycast(transform.position, transform.forward, out RaycastHit raycastHit, Mathf.Infinity,
                _finalmask)) return;
        Vector3 point = raycastHit.point;
        ObjectPingLogger.LogDebug(
            $"You targeted {raycastHit.collider.transform.root.gameObject.name.Replace("(Clone)", "")}");

        GameObject fetch = ZNetScene.instance.GetPrefab("PingPrefab");
        Quaternion quaternion = Quaternion.Euler(0.0f, 22.5f * (float)16, 0.0f);

        Object.Instantiate(
                fetch, point,
                Quaternion.identity).transform.rotation =
            Quaternion.LookRotation(raycastHit.transform.forward, raycastHit.normal);
        ;
    }
}
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Mathematics;
using Photon.Pun;
using Zorro.Core;
using Zorro.Settings;
using Zorro.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SettingsExtender;

namespace ProgressMap;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        SettingsRegistry.Register("Roster");
        Harmony.CreateAndPatchAll(typeof(Plugin));
    }

    private void Start()
    {
        SettingsHandler.Instance.AddSetting(new ProgressBarDisplayModeSetting());
        SettingsHandler.Instance.AddSetting(new RosterSideSetting());
        SettingsHandler.Instance.AddSetting(new ProgressBarDisplayRangeSetting());
    }

    [HarmonyPatch(typeof(RunManager), "StartRun")]
    [HarmonyPostfix]
    static void Post_LoadIsland()
    {
        Logger.LogInfo("Patch running ProgressMap");
        GameObject progressMap = new GameObject("ProgressMap", typeof(ProgressMap));
    }
}

public class ProgressMap : MonoBehaviourPunCallbacks
{
    private GameObject overlay;
    private GameObject peakGO;
    private TMP_FontAsset mainFont;
    private Dictionary<Character, GameObject> characterLabels = new();
    const float totalMountainHeight = 1920f; // in meters
    const float barHeightPixels = 800f;
    const float Offset = 60f;
    const float bottomOffset = 50f;

    private ProgressBarDisplayMode displayMode = ProgressBarDisplayMode.Full;
    private float displayRange = 100f;

    private RosterSide rosterSide;
    private RosterSide lastRosterSide;

    private void Update()
    {
        var currentRosterSide = SettingsHandler.Instance.GetSetting<RosterSideSetting>().Value;
        if (currentRosterSide != lastRosterSide)
        {
            lastRosterSide = currentRosterSide;
            ReloadRosterSide();
        }
    }

    // Reload for changing the RosterSide
    private void ReloadRosterSide()
    {
        rosterSide = SettingsHandler.Instance.GetSetting<RosterSideSetting>().Value;

        // Base references
        var barRect = overlay.transform.Find("AltitudeBar").GetComponent<RectTransform>();
        RectTransform peakRect = peakGO.GetComponent<RectTransform>();

        // Clear old labels
        foreach (var kvp in characterLabels.Values)
            Destroy(kvp);
        characterLabels.Clear();

        // Determine side behavior
        if (rosterSide == RosterSide.Left || rosterSide == RosterSide.Right)
        {
            Vector2 sideAnchor = (rosterSide == RosterSide.Right) ? new Vector2(1, 0.5f) : new Vector2(0, 0.5f);
            float sideOffset = (rosterSide == RosterSide.Right) ? -Offset : Offset;
            float labelOffsetSign = (rosterSide == RosterSide.Right) ? -1f : 1f;

            // Move vertical bar
            barRect.anchorMin = barRect.anchorMax = sideAnchor;
            barRect.sizeDelta = new Vector2(10, barHeightPixels);
            barRect.anchoredPosition = new Vector2(sideOffset, bottomOffset);

            // Peak text
            peakRect.anchorMin = peakRect.anchorMax = sideAnchor;
            peakRect.anchoredPosition = new Vector2(sideOffset, bottomOffset + barHeightPixels / 2f);

            // Re-add characters vertically
            foreach (var character in Character.AllCharacters)
            {
                AddCharacter(character);
                var labelRect = characterLabels[character].GetComponent<RectTransform>();
                labelRect.anchoredPosition += new Vector2(labelOffsetSign * 50f, 0);
            }
        }
        else if (rosterSide == RosterSide.Top)
        {
            Vector2 topAnchor = new Vector2(0.5f, 1f);
            float topOffset = -Offset;

            // Change bar to horizontal
            barRect.anchorMin = barRect.anchorMax = topAnchor;
            barRect.sizeDelta = new Vector2(800f, 10f);
            barRect.anchoredPosition = new Vector2(0, topOffset);

            // Move peak label to right of the bar
            peakRect.anchorMin = peakRect.anchorMax = topAnchor;
            peakRect.anchoredPosition = new Vector2(Offset + 400f, topOffset - 20f);

            // Re-add character labels
            foreach (var character in Character.AllCharacters)
            {
                AddCharacter(character);
                var labelRect = characterLabels[character].GetComponent<RectTransform>();
                labelRect.anchoredPosition += new Vector2(0, -40f); // below bar
            }
        }
    }

    private void Awake()
    {
        if (overlay != null)
        {
            Object.DestroyImmediate(overlay);
        }

        overlay = new GameObject("ProgressMap");
        Canvas canvas = overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = overlay.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        // Load font
        if (mainFont == null)
        {
            TMP_FontAsset[] fontAssets = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            mainFont = fontAssets.FirstOrDefault(a => a.faceInfo.familyName == "Daruma Drop One");
        }

        // PEAK header
        peakGO = new GameObject("PeakText", typeof(RectTransform), typeof(TextMeshProUGUI));
        peakGO.transform.SetParent(overlay.transform, false);

        TextMeshProUGUI peakText = peakGO.GetComponent<TextMeshProUGUI>();
        peakText.font = mainFont;
        peakText.text = "PEAK";
        peakText.color = new Color(1f, 1f, 1f, 0.3f);

        // get roster side setting
        rosterSide = SettingsHandler.Instance.GetSetting<RosterSideSetting>().Value;

        // decide anchor + offset
        Vector2 sideAnchor = (rosterSide == RosterSide.Right) ? new Vector2(1, 0.5f) : new Vector2(0, 0.5f);
        float sideOffset = (rosterSide == RosterSide.Right) ? -Offset : Offset;

        RectTransform peakRect = peakGO.GetComponent<RectTransform>();
        peakRect.sizeDelta = peakText.GetPreferredValues();
        peakRect.anchorMin = peakRect.anchorMax = sideAnchor;
        peakRect.pivot = new Vector2(0.5f, 0f);
        peakRect.anchoredPosition = new Vector2(sideOffset, bottomOffset + (barHeightPixels / 2));

        // Add vertical bar
        GameObject barGO = new GameObject("AltitudeBar");
        barGO.transform.SetParent(overlay.transform, false);

        RectTransform barRect = barGO.AddComponent<RectTransform>();
        barRect.anchorMin = barRect.anchorMax = sideAnchor;
        barRect.sizeDelta = new Vector2(10, barHeightPixels);
        barRect.anchoredPosition = new Vector2(sideOffset, bottomOffset);

        Image barImage = barGO.AddComponent<Image>();
        barImage.color = new Color(0.75f, 0.75f, 0.69f, 0.3f);
    }

    private void Start()
    {
        characterLabels.Clear();
        foreach (var character in Character.AllCharacters)
        {
            AddCharacter(character);
        }

        displayMode = SettingsHandler.Instance.GetSetting<ProgressBarDisplayModeSetting>().Value;
        displayRange = SettingsHandler.Instance.GetSetting<ProgressBarDisplayRangeSetting>().Value;
    }

    private void LateUpdate()
    {
        displayMode = SettingsHandler.Instance.GetSetting<ProgressBarDisplayModeSetting>().Value;
        rosterSide = SettingsHandler.Instance.GetSetting<RosterSideSetting>().Value;
        displayRange = SettingsHandler.Instance.GetSetting<ProgressBarDisplayRangeSetting>().Value;

        float sideOffset = (rosterSide == RosterSide.Right) ? -Offset - 50f : Offset + 50f;

        peakGO.SetActive(displayMode == ProgressBarDisplayMode.Full);

        foreach (var character in Character.AllCharacters)
        {
            float height = character.refs.stats.heightInMeters;

            // Skip if dead (altitude 8000m) Hide Dead Players in Roster
            if (height <= 0f || height >= 7999f)
            {
                if (characterLabels.ContainsKey(character))
                {
                    characterLabels[character].SetActive(false); // hide label if already exists
                }
                continue;
            }

            if (!characterLabels.ContainsKey(character))
            {
                AddCharacter(character);
            }

            GameObject labelGO = characterLabels[character];
            labelGO.SetActive(true);

            string nickname = character.refs.view.Owner.NickName;

            TextMeshProUGUI label = labelGO.GetComponentInChildren<TextMeshProUGUI>();
            label.text = $"{nickname} {height}m";
            label.gameObject.GetComponent<RectTransform>().sizeDelta = label.GetPreferredValues() * 1.1f;

            label.color = character.refs.customization.PlayerColor;
            labelGO.GetComponentInChildren<Image>().color = character.refs.customization.PlayerColor;

            float pixelY = 0;
            float pixelX = 0;

            if (rosterSide == RosterSide.Top)
            {
                if (displayMode == ProgressBarDisplayMode.Full)
                {
                    float normalized = Mathf.InverseLerp(0f, totalMountainHeight, height);
                    pixelX = Mathf.Lerp(-400f, 400f, normalized); // horizontal bar length = 800
                }
                else if (displayMode == ProgressBarDisplayMode.Centered)
                {
                    float localH = Character.localCharacter.refs.stats.heightInMeters;
                    float logH = Mathf.Log(localH);
                    float logMin = logH - Mathf.Log(displayRange);
                    float logMax = logH + Mathf.Log(displayRange);
                    float logValue = Mathf.Log(height);

                    float normalized = Mathf.InverseLerp(logMin, logMax, logValue);
                    normalized = Mathf.Clamp01(normalized);

                    pixelX = Mathf.Lerp(-400f, 400f, normalized);
                }
            }
            else
            {
                // Existing vertical logic
                if (displayMode == ProgressBarDisplayMode.Full)
                {
                    float normalized = Mathf.InverseLerp(0f, totalMountainHeight, height);
                    pixelY = Mathf.Lerp(-barHeightPixels / 2f, barHeightPixels / 2f, normalized);
                }
                else if (displayMode == ProgressBarDisplayMode.Centered)
                {
                    float localH = Character.localCharacter.refs.stats.heightInMeters;
                    float logH = Mathf.Log(localH);
                    float logMin = logH - Mathf.Log(displayRange);
                    float logMax = logH + Mathf.Log(displayRange);
                    float logValue = Mathf.Log(height);

                    float normalized = Mathf.InverseLerp(logMin, logMax, logValue);
                    normalized = Mathf.Clamp01(normalized);

                    pixelY = Mathf.Lerp(-barHeightPixels / 2f, barHeightPixels / 2f, normalized);
                }
            }

            RectTransform labelRect = labelGO.GetComponent<RectTransform>();
            if (rosterSide == RosterSide.Top)
                labelRect.anchoredPosition = new Vector2(pixelX, -Offset - 50f);
            else
                labelRect.anchoredPosition = new Vector2(sideOffset, bottomOffset + pixelY);
        }
    }

    public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
    {
        Debug.Log("Adding player to map");
        StartCoroutine(WaitAndAddPlayer(newPlayer));
    }

    private IEnumerator WaitAndAddPlayer(Photon.Realtime.Player newPlayer)
    {
        yield return new WaitUntil(() => PlayerHandler.GetPlayerCharacter(newPlayer) != null);

        ProgressMap map = GameObject.Find("ProgressMap").GetComponent<ProgressMap>();
        map.AddCharacter(PlayerHandler.GetPlayerCharacter(newPlayer));
    }

    public override void OnPlayerLeftRoom(Photon.Realtime.Player leavingPlayer)
    {
        Debug.Log("Removing player from map");
        ProgressMap map = GameObject.Find("ProgressMap").GetComponent<ProgressMap>();
        map.RemoveCharacter(PlayerHandler.GetPlayerCharacter(leavingPlayer));
    }

    public void AddCharacter(Character character)
    {
        Debug.Log($"Adding character {character}");

        float height = character.refs.stats.heightInMeters;

        // Skip creating labels for dead players (altitude 8000m)
        if (height <= 0f || height >= 7999f)
        {
            return;
        }

        if (!characterLabels.ContainsKey(character))
        {
            // save the player's name
            string nickname = character.refs.view.Owner.NickName;

            // Parent label object
            GameObject labelGO = new GameObject($"Label_{nickname}");
            labelGO.transform.SetParent(overlay.transform, false);

            // Get side preference
            RosterSide rosterSide = SettingsHandler.Instance.GetSetting<RosterSideSetting>().Value;

            Vector2 sideAnchor;
            TextAlignmentOptions sideAlign;
            Vector2 textOffset;

            // Side-dependent variables
            if (rosterSide == RosterSide.Left)
            {
                sideAnchor = new Vector2(0, 0.5f);
                sideAlign = TextAlignmentOptions.Left;
                textOffset = new Vector2(20, 0);
            }
            else if (rosterSide == RosterSide.Right)
            {
                sideAnchor = new Vector2(1, 0.5f);
                sideAlign = TextAlignmentOptions.Right;
                textOffset = new Vector2(-20, 0);
            }
            else // TOP
            {
                sideAnchor = new Vector2(0.5f, 1f);
                sideAlign = TextAlignmentOptions.Center;
                textOffset = new Vector2(0, -5f); // slightly below the bar
            }

            RectTransform labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = labelRect.anchorMax = sideAnchor;

            // Dot marker
            GameObject markerGO = new GameObject($"Marker");
            markerGO.transform.SetParent(labelGO.transform, false);

            Image marker = markerGO.AddComponent<Image>();
            marker.color = character.refs.customization.PlayerColor;

            RectTransform markerRect = markerGO.GetComponent<RectTransform>();
            markerRect.anchorMin = markerRect.anchorMax = sideAnchor;
            markerRect.pivot = new Vector2(0.5f, 0.5f);
            markerRect.sizeDelta = (rosterSide == RosterSide.Top) ? new Vector2(5, 10) : new Vector2(10, 5);

            // Text label
            GameObject textGO = new GameObject($"Text");
            textGO.transform.SetParent(labelGO.transform, false);

            TextMeshProUGUI labelText = textGO.AddComponent<TextMeshProUGUI>();
            labelText.color = character.refs.customization.PlayerColor;
            labelText.font = mainFont;
            labelText.fontSize = 18;
            labelText.alignment = sideAlign;

            RectTransform textRect = textGO.GetComponent<RectTransform>();
            textRect.anchorMin = textRect.anchorMax = sideAnchor;
            textRect.pivot = sideAnchor;
            textRect.anchoredPosition = textOffset;

            characterLabels[character] = labelGO;
        }
    }

    public void RemoveCharacter(Character character)
    {
        GameObject labelGO = characterLabels[character];
        Object.DestroyImmediate(labelGO);
        characterLabels.Remove(character);
    }
}

internal static class MyPluginInfo
{
    public const string PLUGIN_GUID = "com.yourname.progressmap";
    public const string PLUGIN_NAME = "Progress Map";
    public const string PLUGIN_VERSION = "1.0.0";
}

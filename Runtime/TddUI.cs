using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

namespace SaturaSpace
{

/// <summary>
/// Runtime API for TDD UI interaction.
/// Provides discovery, clicking, and text reading for UI elements.
/// Use in TddScenario scripts or PlayMode tests.
///
/// Usage:
///   var clickables = TddUI.GetClickables();
///   TddUI.Click("Canvas/StartButton");
///   string text = TddUI.GetText("Canvas/ScoreText");
///   yield return TddUI.ClickAndWait("Canvas/StartButton", 0.5f);
/// </summary>
public static class TddUI
{
    // ── Discovery ────────────────────────────────────────────────────

    /// <summary>
    /// Scan all interactive UI elements currently in the scene.
    /// Optionally filter by type name or path substring.
    /// </summary>
    public static List<TddClickable> GetClickables(string filter = null)
    {
        var elements = CollectInteractableElements();

        // Occlusion detection
        for (int i = 0; i < elements.Count; i++)
        {
            var a = elements[i];
            var rectA = a.screenRect;

            for (int j = 0; j < elements.Count; j++)
            {
                if (i == j) continue;
                var b = elements[j];
                if (b.renderOrder <= a.renderOrder) continue;

                if (rectA.Overlaps(b.screenRect))
                    a.blockedBy.Add(b.path);
            }
        }

        elements.Sort((a, b) => a.renderOrder.CompareTo(b.renderOrder));

        if (!string.IsNullOrEmpty(filter))
        {
            elements.RemoveAll(e =>
                e.type.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                e.path.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0);
        }

        return elements;
    }

    /// <summary>Find a GameObject by its hierarchy path.</summary>
    public static GameObject Find(string path)
    {
        return GameObject.Find(path);
    }

    /// <summary>
    /// Extract text from TMP_Text or legacy Text on a GameObject (or its children).
    /// Returns empty string if not found.
    /// </summary>
    public static string GetText(string objectPath)
    {
        var go = GameObject.Find(objectPath);
        if (go == null) return "";
        return ExtractText(go);
    }

    // ── Interaction ──────────────────────────────────────────────────

    /// <summary>
    /// Simulate a UI click on a GameObject found by path.
    /// Dispatches PointerDown → PointerClick → PointerUp through EventSystem.
    /// Returns true if the element was found and clicked.
    /// </summary>
    public static bool Click(string objectPath)
    {
        var go = GameObject.Find(objectPath);
        if (go == null) return false;
        return Click(go);
    }

    /// <summary>
    /// Simulate a UI click on a GameObject.
    /// Dispatches PointerDown → PointerClick → PointerUp through EventSystem.
    /// Returns true if the click was dispatched.
    /// </summary>
    public static bool Click(GameObject go)
    {
        if (go == null) return false;

        var eventSystem = EventSystem.current;
        if (eventSystem == null) return false;

        var rectTransform = go.GetComponent<RectTransform>();
        Vector2 screenPos = Vector2.zero;

        if (rectTransform != null)
        {
            var corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            var canvas = go.GetComponentInParent<Canvas>();
            if (canvas != null) canvas = canvas.rootCanvas;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                var cam = canvas.worldCamera ?? Camera.main;
                if (cam != null)
                {
                    for (int i = 0; i < 4; i++)
                        corners[i] = cam.WorldToScreenPoint(corners[i]);
                }
            }

            screenPos = new Vector2(
                (corners[0].x + corners[2].x) * 0.5f,
                (corners[0].y + corners[2].y) * 0.5f);
        }

        var pointerData = new PointerEventData(eventSystem)
        {
            position = screenPos
        };

        pointerData.pointerPressRaycast = new RaycastResult { gameObject = go };

        var pressHandler = ExecuteEvents.ExecuteHierarchy(go, pointerData, ExecuteEvents.pointerDownHandler);
        pointerData.pointerPress = pressHandler;
        ExecuteEvents.ExecuteHierarchy(go, pointerData, ExecuteEvents.pointerClickHandler);
        ExecuteEvents.ExecuteHierarchy(go, pointerData, ExecuteEvents.pointerUpHandler);

        return true;
    }

    // ── Coroutine helpers ────────────────────────────────────────────

    /// <summary>Wait until a GameObject exists at the given path, or timeout.</summary>
    public static IEnumerator WaitForElement(string path, float timeout = 5f)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (GameObject.Find(path) == null)
        {
            if (Time.realtimeSinceStartup >= deadline)
                yield break;
            yield return null;
        }
    }

    /// <summary>Wait until text at the given path equals expected, or timeout.</summary>
    public static IEnumerator WaitForText(string path, string expected, float timeout = 5f)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (true)
        {
            if (GetText(path) == expected)
                yield break;
            if (Time.realtimeSinceStartup >= deadline)
                yield break;
            yield return null;
        }
    }

    /// <summary>Click an element by path and wait for the given duration.</summary>
    public static IEnumerator ClickAndWait(string path, float seconds = 0.5f)
    {
        Click(path);
        float deadline = Time.realtimeSinceStartup + seconds;
        while (Time.realtimeSinceStartup < deadline)
            yield return null;
    }

    // ── Internals ────────────────────────────────────────────────────

    static List<TddClickable> CollectInteractableElements()
    {
        var elements = new List<TddClickable>();
        var seen = new HashSet<int>();

        // 1. Scan Graphics with raycastTarget or Button
        var graphics = UnityEngine.Object.FindObjectsByType<Graphic>(FindObjectsSortMode.None);
        foreach (var g in graphics)
        {
            if (!g.gameObject.activeInHierarchy) continue;

            bool hasButton = g.GetComponent<Button>() != null;
            if (!g.raycastTarget && !hasButton) continue;

            var canvas = g.canvas;
            if (canvas == null) continue;

            var screenRect = GetScreenRect(g.rectTransform, canvas);
            if (screenRect.width < 1f || screenRect.height < 1f) continue;

            int goId = g.gameObject.GetInstanceID();
            if (!seen.Add(goId)) continue;

            string typeName;
            var sel = g.GetComponent<Selectable>();
            if (sel != null)
                typeName = ResolveSelectableTypeName(sel);
            else
                typeName = g.GetType().Name;

            bool interactable = sel == null || sel.interactable;
            int renderOrder = canvas.sortingOrder * 100000 + GetHierarchyOrder(g.transform);

            elements.Add(new TddClickable
            {
                instanceId = goId,
                path = GetGameObjectPath(g.transform),
                type = typeName,
                interactable = interactable,
                text = ExtractText(g.gameObject),
                screenRect = screenRect,
                renderOrder = renderOrder,
                blockedBy = new List<string>()
            });
        }

        // 2. Scan Selectables not already found via Graphics
        var selectables = UnityEngine.Object.FindObjectsByType<Selectable>(FindObjectsSortMode.None);
        foreach (var sel in selectables)
        {
            if (!sel.gameObject.activeInHierarchy) continue;

            int goId = sel.gameObject.GetInstanceID();
            if (!seen.Add(goId)) continue;

            var rectTransform = sel.GetComponent<RectTransform>();
            if (rectTransform == null) continue;

            var canvas = sel.GetComponentInParent<Canvas>();
            if (canvas == null) continue;

            var screenRect = GetScreenRect(rectTransform, canvas);
            if (screenRect.width < 1f || screenRect.height < 1f) continue;

            string typeName = ResolveSelectableTypeName(sel);
            int renderOrder = canvas.sortingOrder * 100000 + GetHierarchyOrder(sel.transform);

            elements.Add(new TddClickable
            {
                instanceId = goId,
                path = GetGameObjectPath(sel.transform),
                type = typeName,
                interactable = sel.interactable,
                text = ExtractText(sel.gameObject),
                screenRect = screenRect,
                renderOrder = renderOrder,
                blockedBy = new List<string>()
            });
        }

        return elements;
    }

    static string ResolveSelectableTypeName(Selectable sel)
    {
        if (sel is Button) return "Button";
        if (sel is Toggle) return "Toggle";
        if (sel is Slider) return "Slider";
        if (sel is Scrollbar) return "Scrollbar";
        if (sel is Dropdown) return "Dropdown";
        if (sel is InputField) return "InputField";
        var name = sel.GetType().Name;
        if (name == "TMP_InputField") return "InputField";
        if (name == "TMP_Dropdown") return "Dropdown";
        return name;
    }

    static Rect GetScreenRect(RectTransform rectTransform, Canvas canvas)
    {
        var corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            var cam = canvas.worldCamera ?? Camera.main;
            if (cam == null) return Rect.zero;

            for (int i = 0; i < 4; i++)
                corners[i] = cam.WorldToScreenPoint(corners[i]);
        }

        float minX = Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float minY = Mathf.Min(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        float maxX = Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
        float maxY = Mathf.Max(corners[0].y, corners[1].y, corners[2].y, corners[3].y);
        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    static string ExtractText(GameObject go)
    {
        var tmp = go.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            var t = tmp.text ?? "";
            return t.Length > 200 ? t.Substring(0, 200) : t;
        }
        var tmpChild = go.GetComponentInChildren<TMP_Text>();
        if (tmpChild != null)
        {
            var t = tmpChild.text ?? "";
            return t.Length > 200 ? t.Substring(0, 200) : t;
        }
        return "";
    }

    internal static string GetGameObjectPath(Transform t)
    {
        var parts = new List<string>();
        while (t != null)
        {
            parts.Add(t.name);
            t = t.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    static int GetHierarchyOrder(Transform t)
    {
        int order = 0;
        int depth = 0;
        var current = t;
        while (current != null)
        {
            order += current.GetSiblingIndex() * (1 + depth * 100);
            current = current.parent;
            depth++;
        }
        return order;
    }
}

/// <summary>
/// Data class representing a discovered UI element.
/// </summary>
public class TddClickable
{
    public int instanceId;
    public string path;
    public string type;
    public bool interactable;
    public string text;
    public Rect screenRect;
    public int renderOrder;
    public List<string> blockedBy;

    public override string ToString()
    {
        var s = $"[{type}] {path} id={instanceId}";
        if (!interactable) s += " DISABLED";
        if (!string.IsNullOrEmpty(text))
        {
            var display = text.Replace("\n", "\\n");
            if (display.Length > 80) display = display.Substring(0, 80) + "...";
            s += $" text=\"{display}\"";
        }
        if (blockedBy != null && blockedBy.Count > 0)
            s += $" BLOCKED_BY=[{string.Join(", ", blockedBy)}]";
        return s;
    }
}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SatelliteListUI: 显示当前场景中由 GravityManager 管理的 NBody 列表及其主要参数。
/// 使用方式（Editor）:
/// 1) 在 Canvas 下创建一个 Panel 作为列表面板 (panel)，在其中放一个 ScrollView。
/// 2) 在 ScrollView -> Content 下放一个名为 EntryTemplate 的空 GameObject，EntryTemplate 内包含 TextMeshProUGUI 元件，子对象可命名为: NameText, MassText, PositionText, VelocityText。将 EntryTemplate 设为 inactive。
/// 3) 将 GravityManager 拖入脚本的 gravityManager 字段，将 panel、entryTemplate、contentParent（Content 物体）和 toggleButton 关联。
/// 4) 运行时点击 toggleButton 打开/关闭列表，脚本会克隆 EntryTemplate 并填充数据。
/// </summary>
public class SatelliteListUI : MonoBehaviour
{
    [Header("References")]
    public GravityManager gravityManager;
    public GameObject panel; // 主面板（显示/隐藏）
    public GameObject entryTemplate; // 在 Content 下的 template（设为 inactive）
    public Transform contentParent; // ScrollView -> Content
    public Button toggleButton;

    [Header("Refresh")]
    public float refreshInterval = 0.6f; // 列表自动刷新间隔（秒）

    private Coroutine refreshCoroutine;

    private void Awake()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(TogglePanel);

        // Try to resolve gravityManager automatically if not assigned in Inspector
        if (gravityManager == null)
        {
            // Prefer the newer API when available to avoid deprecation warnings.
            gravityManager = Object.FindAnyObjectByType<GravityManager>();
            if (gravityManager == null)
            {
                // If there's a bootstrap object, try to use its assigned GravityManager
                var bootstrap = Object.FindAnyObjectByType<SimulationBootstrap>();
                if (bootstrap != null)
                {
                    gravityManager = bootstrap.gravityManager;
                }
            }

            if (gravityManager == null)
            {
                Debug.LogWarning("SatelliteListUI: gravityManager not assigned and could not be found in scene. Assign it in the Inspector or add a GravityManager to the scene.");
            }
        }

        if (panel != null)
            panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(TogglePanel);
    }

    public void TogglePanel()
    {
        if (panel == null) return;

        bool willShow = !panel.activeSelf;
        panel.SetActive(willShow);

        if (willShow)
        {
            RefreshList();
            if (refreshCoroutine == null)
                refreshCoroutine = StartCoroutine(PeriodicRefresh());
        }
        else
        {
            if (refreshCoroutine != null)
            {
                StopCoroutine(refreshCoroutine);
                refreshCoroutine = null;
            }
        }
    }

    private IEnumerator PeriodicRefresh()
    {
        while (true)
        {
            yield return new WaitForSeconds(refreshInterval);
            RefreshList();
        }
    }

    /// <summary>
    /// 清理旧条目并根据 gravityManager.Bodies 创建新条目。
    /// EntryTemplate 的结构约定（在 Editor 中创建）:
    /// - EntryTemplate (inactive)
    ///   - NameText (TextMeshProUGUI)
    ///   - SemimajorAxisText (TextMeshProUGUI)
    ///   - EccentricityText (TextMeshProUGUI)
    ///   - InclinationText (TextMeshProUGUI)
    ///   - ArgumentOfPerigeeText (TextMeshProUGUI)
    ///   - RAANText (TextMeshProUGUI)
    ///   - TrueAnomalyText (TextMeshProUGUI)
    /// </summary>
    public void RefreshList()
    {
        if (gravityManager == null || contentParent == null || entryTemplate == null)
        {
            Debug.LogWarning("SatelliteListUI: Missing references. Assign GravityManager, Content parent and EntryTemplate in Inspector.");
            return;
        }

        // Remove previous instantiated children but keep the template if it's parented under contentParent
        List<Transform> toDestroy = new List<Transform>();
        for (int i = 0; i < contentParent.childCount; i++)
        {
            Transform child = contentParent.GetChild(i);
            if (child.gameObject == entryTemplate) continue;
            toDestroy.Add(child);
        }
        foreach (var t in toDestroy)
            DestroyImmediate(t.gameObject);

        var bodies = gravityManager.Bodies;
        if (bodies == null) return;

        var central = gravityManager.CentralBody;

        foreach (var body in bodies)
        {
            // Optionally skip central body if you don't want to show Earth
            // if (body.isCentralBody) continue;

            GameObject entry = Instantiate(entryTemplate, contentParent);
            entry.SetActive(true);

            // Find text components by name and set values (tolerant if some don't exist)
            TrySetText(entry.transform, "NameText", body.name);
            TrySetText(entry.transform, "MassText", $"Mass: {body.mass:G3}");
            TrySetText(entry.transform, "PositionText", $"Pos: ({body.transform.position.x:F1}, {body.transform.position.y:F1}, {body.transform.position.z:F1})");
            TrySetText(entry.transform, "VelocityText", $"Vel: ({body.velocity.x:F2}, {body.velocity.y:F2}, {body.velocity.z:F2})");

            // Orbital parameters: inclination, semi-major axis, eccentricity, orbital period, RAAN
            if (central != null && !body.isCentralBody)
            {
                // Use central body's true mass if available
                float centralMass = central != null ? (float)central.trueMass : central.mass;
                var op = OrbitalCalculations.CalculateOrbitalParameters(centralMass, central.transform.position, body.transform, body.velocity);
                if (op.isValid)
                {
                    TrySetText(entry.transform, "InclinationText", $"i: {op.inclination:F2}°");
                    TrySetText(entry.transform, "SemimajorAxisText", $"a: {op.semiMajorAxis:F1}");
                    TrySetText(entry.transform, "EccentricityText", $"e: {op.eccentricity:F4}");
                    TrySetText(entry.transform, "ArgumentOfPerigeeText", $"ω: {op.argumentOfPerigee:F2}°");
                    TrySetText(entry.transform, "RAANText", $"Ω: {op.RAAN:F2}°");
                    TrySetText(entry.transform, "TrueAnomalyText", $"ν: {op.trueAnomaly:F2}°");
                }
                else
                {
                    TrySetText(entry.transform, "InclinationText", "i: N/A");
                    TrySetText(entry.transform, "SemimajorAxisText", "a: N/A");
                    TrySetText(entry.transform, "EccentricityText", "e: N/A");
                    TrySetText(entry.transform, "ArgumentOfPerigeeText", "ω: N/A");
                    TrySetText(entry.transform, "RAANText", "Ω: N/A");
                    TrySetText(entry.transform, "TrueAnomalyText", "ν: N/A");
                }
            }
            else
            {
                TrySetText(entry.transform, "InclinationText", "Inc: N/A");
                TrySetText(entry.transform, "SemiMajorAxisText", "SMA: N/A");
                TrySetText(entry.transform, "EccentricityText", "e: N/A");
                TrySetText(entry.transform, "OrbitalPeriodText", "T: N/A");
                TrySetText(entry.transform, "RAANText", "RAAN: N/A");
            }

            // Optionally add a button on the entry to focus camera
            var btn = entry.GetComponent<Button>();
            if (btn != null)
            {
                var captured = body;
                btn.onClick.AddListener(() => OnEntryClicked(captured));
            }
        }
    }

    private void OnEntryClicked(NBody body)
    {
        if (body == null) return;

        // 找到 CameraController 并切换到追踪该天体（如果可用）
    var cam = Object.FindAnyObjectByType<CameraController>();
        if (cam != null)
        {
            int index = cam.Bodies.IndexOf(body);
            if (index >= 0)
            {
                cam.currentIndex = index;
                cam.ReturnToTracking();
                cam.UpdateTrajectoryRender(index);
            }
        }
    }

    private void TrySetText(Transform parent, string childName, string value)
    {
        // Try to find a child transform with the exact name (search all descendants)
        Transform found = null;
        var allTransforms = parent.GetComponentsInChildren<Transform>(true);
        foreach (var tr in allTransforms)
        {
            if (tr.name == childName)
            {
                found = tr;
                break;
            }
        }

        if (found != null)
        {
            var tmp = found.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = value;
                return;
            }

            var txt = found.GetComponent<Text>();
            if (txt != null)
            {
                txt.text = value;
                return;
            }
        }

        // If no exact-name match, try a case-insensitive "contains" match for more flexibility
        foreach (var tr in allTransforms)
        {
            if (tr.name != null && tr.name.ToLower().Contains(childName.ToLower()))
            {
                var tmp2 = tr.GetComponent<TextMeshProUGUI>();
                if (tmp2 != null)
                {
                    tmp2.text = value;
                    return;
                }
                var txt2 = tr.GetComponent<Text>();
                if (txt2 != null)
                {
                    txt2.text = value;
                    return;
                }
            }
        }

        // As a last resort, do not overwrite arbitrary text components to avoid clobbering other fields.
        // This prevents earlier-set fields like NameText being overwritten when a later field is missing.
    }
}

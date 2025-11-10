using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Manages the user interface for switching between Free Cam and Track Cam modes.
/// Controls the visibility of panels and highlights active buttons.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button freeCamButton;
    public Button trackCamButton;
    public Button instructionsButton;

    [Header("Panels")]
    public GameObject objectPlacementPanel;
    public GameObject objectInfoPanel;
    public GameObject thrustButtons;
    public GameObject maneuverNodePanel;
    public GameObject burnControlsPanel;
    public GameObject apogeePerigeePanel;
    public GameObject instructionsPanel;
    public GameObject toggleOptionsPanel;
    public GameObject dropdown;
    public GameObject placeTLEPanel;
    public GameObject placementSelectPanel;
    public GameObject cameraControls;

    [Header("UI - Input Fields")]
    public TMP_InputField nameInputField;
    public TMP_InputField positionInputField;
    public TMP_InputField massInputField;
    public TMP_InputField radiusInputField;
    public TMP_InputField velocityInputField;

    [Header("UI - Buttons")]
    public Button placeObjectButton;
    public Button placementModeButton;
    public Button burnControlButton;

    [Header("UI - Text Displays")]
    public TMP_Text earthCamButtonText;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI apogeeText;
    public TextMeshProUGUI perigeeText;
    public TextMeshProUGUI semiMajorAxisText;
    public TextMeshProUGUI eccentricityText;
    public TextMeshProUGUI orbitalPeriodText;
    public TextMeshProUGUI inclinationText;
    public TextMeshProUGUI raanText;
    public TextMeshProUGUI deltaVText;

    [Header("UI Flags")]
    public bool showInstructionText = true;
    public bool isTracking = true;
    public bool earthCamPressed = true;
    private bool inFreePlacementMode = true;
    private bool inFreeThrustMode = true;

    private SimContext ctx;

    public void Initialize(SimContext ctx)
    {
        this.ctx = ctx;

        instructionText.text =
  "<b>欢迎来到轨道模拟器 !</b>\n" +
 "<b>追踪摄像机模式已激活 !</b>\n\n" +
 "\u00A0\u00A0\u00A0\u00A0<b>──────── 控制说明 ────────</b>\n" +
 "- 下拉菜单: 选择要追踪的物体\n" +
 "- Esc键: 关闭游戏\n" +
 "- 右键鼠标: 旋转摄像机视角\n" +
 "- 鼠标滚轮: 放大/缩小</b>\n" +
 "- 时间调节器: 调整时间速度 (重置: 'R'键)\n" +
 "- 地球视角按钮: 切换'地球视角'或'卫星视角'\n" +
 "     * 地球视角: 将画面中心对准地球\n" +
 "     * 卫星视角: 将画面中心对准所选卫星\n" +
 "\u00A0\u00A0\u00A0\u00A0<b>──────── 推进控制 ────────</b>\n" +
 "- 顺行/逆行: 在轨道上加速或减速</b>\n" +
 "- 左/右: 调整横向移动 (改变轨道倾角)\n" +
 "- 径向向内/径向向外: 朝向或远离你所环绕的行星推进\n" +
 "\u00A0\u00A0\u00A0\u00A0</b>──────── 机动节点 ────────</b>\n" +
  "- 从下拉菜单中选择一种推进类型</b>\n" +
  "- 点击'设置'创建一个节点</b>\n" +
  "- 使用滑块调整推进时机</b>\n" +
  "- 点击'放置'以完成机动操作</b>\n" +
 "切换到自由视角以自由探索或放置卫星";

        ShowObjectPlacementPanel(false);
        ShowPlaceTLEPanel(false, false);
        ShowManeuverNodes(true);
        burnControlsPanel.SetActive(true);
        ShowOrbitInfoPanel(true);
        SetButtonState(freeCamButton, false);
        SetButtonState(trackCamButton, true);
        trackCamButton.Select();
        trackCamButton.interactable = false;
        placementSelectPanel.SetActive(false);
        instructionsPanel.SetActive(showInstructionText);
        cameraControls.SetActive(true);
        UpdateButtonText();
        deltaVText.text = "";
    }

    /// <summary>
    /// Handles the "Free Cam" button press event.
    /// Switches UI and controls into free camera placement mode.
    /// </summary>
    public void OnFreeCamPressed()
    {
        instructionText.text =
        "<b>自由视角模式已激活 !</b>\n" +
"你可以自由移动以探索或放置卫星\n" +
"\u00A0\u00A0\u00A0\u00A0<b>──────── 控制说明 ────────</b>\n" +
"- WASD: 移动视角\n" +
"- 右键鼠标: 旋转摄像机视角\n" +
"- Esc键: 关闭游戏\n" +
"\u00A0\u00A0\u00A0\u00A0<b>──────── 放置卫星 ────────</b>\n" +
"- 命名是可选的 (默认为 'Satellite (n)')\n" +
"- 设置质量 (500 - 1,000,000 kg)\n" +
"- 设置半径 (1-50)\n" +
"  * 格式: 5,45,3\n" +
"  * 不要使用括号、负数或非数字字符\n" +
"- 点击 '放置卫星' 以生成";
        isTracking = false;
        ShowObjectPlacementPanel(true);
        ShowPlaceTLEPanel(true, false);
        ShowManeuverNodes(false);
        burnControlsPanel.SetActive(false);

        ShowOrbitInfoPanel(false);
        SetButtonState(freeCamButton, true);
        SetButtonState(trackCamButton, false);
        ShowThrustButtonsPanel(false);
        ShowApogeePerigeePanel(false);

        toggleOptionsPanel.SetActive(false);
        dropdown.SetActive(false);

        freeCamButton.interactable = false;
        trackCamButton.interactable = true;
        placementSelectPanel.SetActive(true);

        if (velocityInputField != null)
        {
            velocityInputField.interactable = false;
        }

        if (nameInputField != null && massInputField != null && radiusInputField != null && positionInputField != null)
        {
            nameInputField.interactable = true;

            positionInputField.interactable = true;

            massInputField.interactable = true;

            radiusInputField.interactable = true;

            placeObjectButton.interactable = true;
        }
        EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>
    /// Handles the "Track Cam" button press event.
    /// Switches UI and controls into tracking mode.
    /// </summary>
    public void OnTrackCamPressed()
    {
        instructionText.text =
    "<b>追踪摄像机模式已激活 !</b>\n\n" +
"\u00A0\u00A0\u00A0\u00A0<b>──────── 控制说明 ────────</b>\n" +
"- 下拉菜单: 选择要追踪的物体\n" +
"- Esc键: 关闭游戏\n" +
"- 右键鼠标: 旋转摄像机视角\n" +
"- 鼠标滚轮: 放大/缩小\n" +
"- 时间调节器: 调整时间速度 (重置: 'R'键)\n" +
"- 地球视角按钮: 切换'地球视角'或'卫星视角'\n" +
"     * 地球视角: 将画面中心对准地球\n" +
"     * 卫星视角: 将画面中心对准所选卫星\n" +
"\u00A0\u00A0\u00A0\u00A0<b>──────── 推进控制 ────────</b>\n" +
"- 顺行/逆行: 在轨道上加速或减速\n" +
"- 左/右: 调整横向移动 (改变轨道倾角)\n" +
"- 径向向内/径向向外: 朝向或远离你所环绕的行星推进\n" +
"\u00A0\u00A0\u00A0\u00A0<b>──────── 机动节点 ──────────</b>\n" +
"- 从下拉菜单中选择一种推进类型\n" +
"- 点击'设置'创建一个节点\n" +
"- 使用滑块调整推进时机\n" +
"- 点击'放置'以完成机动操作\n" +
"切换到自由视角以自由探索或放置卫星";
        isTracking = true;

        ShowObjectPlacementPanel(false);
        ShowPlaceTLEPanel(false, false);
        ShowManeuverNodes(true);
        burnControlsPanel.SetActive(true);

        ShowOrbitInfoPanel(true);
        SetButtonState(freeCamButton, false);
        SetButtonState(trackCamButton, true);
        ShowThrustButtonsPanel(true);
        ShowApogeePerigeePanel(true);

        toggleOptionsPanel.SetActive(true);
        dropdown.SetActive(true);

        trackCamButton.interactable = false;
        freeCamButton.interactable = true;
        placementSelectPanel.SetActive(false);

        if (velocityInputField != null)
        {
            velocityInputField.interactable = false;
        }

        if (nameInputField != null && massInputField != null && radiusInputField != null && positionInputField != null)
        {
            nameInputField.text = null;
            nameInputField.interactable = false;

            positionInputField.text = null;
            positionInputField.interactable = false;

            massInputField.text = null;
            massInputField.interactable = false;

            radiusInputField.text = null;
            radiusInputField.interactable = false;

            placeObjectButton.interactable = false;
        }
        EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>
    /// Updates the Earth Cam button text when toggled.
    /// </summary>
    public void OnEarthCamPressed()
    {
        if (earthCamPressed)
        {
            earthCamButtonText.text = "Satellite Cam(卫星视角)";
            earthCamPressed = false;
        }
        else
        {
            earthCamButtonText.text = "Earth Cam(地球视角)";
            earthCamPressed = true;
        }
    }

    /// <summary>
    /// Shows or hides multiple panels depending on placement/tracking mode.
    /// </summary>
    /// <param name="showObjectPlacementPanel">Whether to show the object placement panel.</param>
    /// <param name="showThrustButtonsPanel">Whether to show the thrust buttons panel.</param>
    public void ShowSelectPanels(bool showObjectPlacementPanel, bool showThrustButtonsPanel, bool showDropdownSection, bool pauseFlag)
    {
        // If were tracking and any of the booleans are false
        if (!showObjectPlacementPanel)
        {
            toggleOptionsPanel.SetActive(false);
            freeCamButton.interactable = false;
            trackCamButton.interactable = false;
        }
        else
        {
            toggleOptionsPanel.SetActive(true);
            if (isTracking)
            {
                freeCamButton.interactable = true;
            }
            else
            {
                trackCamButton.interactable = true;
            }
        }
        if (!freeCamButton.interactable)
        {
            ShowObjectPlacementPanel(showObjectPlacementPanel);
            ShowPlaceTLEPanel(true, pauseFlag);
            ShowManeuverNodes(false);
        }
        ShowThrustButtonsPanel(showThrustButtonsPanel);

        if (!showDropdownSection)
        {
            dropdown.SetActive(false);
        }
        else
        {
            dropdown.SetActive(true);
        }
    }

    /// <summary>
    /// Toggles the visibility of the object placement panel.
    /// </summary>
    /// <param name="show">True to show, false to hide.</param>
    private void ShowObjectPlacementPanel(bool show)
    {
        objectPlacementPanel.SetActive(show);
    }

    private void ShowPlaceTLEPanel(bool show, bool pauseFlag)
    {
        if (!show)
        {
            placeTLEPanel.SetActive(show);
            objectPlacementPanel.SetActive(show);
        }
        // show is always true here
        else
        {
            if (pauseFlag)
            {
                placeTLEPanel.SetActive(!show);
                objectPlacementPanel.SetActive(!show);
            }
            else
            {
                if (!inFreePlacementMode)
                {
                    placeTLEPanel.SetActive(show);
                    objectPlacementPanel.SetActive(!show);
                }
                else
                {
                    placeTLEPanel.SetActive(!show);
                    objectPlacementPanel.SetActive(show);
                }
            }

        }

    }

    private void ShowManeuverNodes(bool show)
    {
        if (!show)
        {
            thrustButtons.SetActive(show);
            maneuverNodePanel.SetActive(show);
        }
        else
        {
            // Show is always true here btw
            if (!inFreeThrustMode)
            {
                thrustButtons.SetActive(!show);
                maneuverNodePanel.SetActive(show);
            }
            else
            {
                thrustButtons.SetActive(show);
                maneuverNodePanel.SetActive(!show);
            }
        }

    }

    /// <summary>
    /// Toggles the visibility of the thrust buttons panel.
    /// </summary>
    /// <param name="show">True to show, false to hide.</param>
    private void ShowThrustButtonsPanel(bool show)
    {
        if (inFreeThrustMode)
        {
            thrustButtons.SetActive(show);
        }
        else
        {
            maneuverNodePanel.SetActive(show);
        }

    }

    /// <summary>
    /// Toggles the visibility of the apogee and perigee panel.
    /// </summary>
    /// <param name="show">True to show, false to hide.</param>
    public void ShowApogeePerigeePanel(bool show)
    {
        apogeePerigeePanel.SetActive(show);
    }

    /// <summary>
    /// Toggles the visibility of the general object info panel.
    /// </summary>
    /// <param name="show">True to show, false to hide.</param>
    private void ShowOrbitInfoPanel(bool show)
    {
        objectInfoPanel.SetActive(show);
    }

    /// <summary>
    /// Toggles visibility of the feedback/instructions panel.
    /// </summary>
    public void ShowFeedbackPanel()
    {
        showInstructionText = !showInstructionText;
        UpdateButtonText(); // Update the button text when toggling
        instructionsPanel.SetActive(showInstructionText);
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void SwitchPlacementMode()
    {
        inFreePlacementMode = !inFreePlacementMode;
        TMP_Text placementModeButtonText = placementModeButton.GetComponentInChildren<TMP_Text>();

        placementModeButtonText.text = inFreePlacementMode ? "Switch to TLE Input" : "Switch to Manual Input";
        if (!isTracking)
        {
            ShowPlaceTLEPanel(true, false);
        }

    }

    public void SwitchBurnMode()
    {
        inFreeThrustMode = !inFreeThrustMode;
        TMP_Text burnControlsText = burnControlButton.GetComponentInChildren<TMP_Text>();

        burnControlsText.text = inFreeThrustMode ? "Use Maneuver Nodes" : "Use Free Thrust";
        if (isTracking)
        {
            ShowManeuverNodes(true);
        }

    }

    /// <summary>
    /// Updates the feedback button's text to reflect visibility state.
    /// </summary>
    private void UpdateButtonText()
    {
        TMP_Text tmpButtonText = instructionsButton.GetComponentInChildren<TMP_Text>();
        if (tmpButtonText != null)
        {
            tmpButtonText.text = showInstructionText ? "Hide Instructions" : "Show Instructions";
        }
    }

    /// <summary>
    /// Updates the visual state (color) of a button.
    /// </summary>
    /// <param name="button">The button to modify.</param>
    /// <param name="isPressed">True if the button is active/selected.</param>
    private void SetButtonState(Button button, bool isPressed)
    {
        ColorBlock colors = button.colors;
        Color newColor;

        if (isPressed)
        {
            ColorUtility.TryParseHtmlString("#008CDB", out newColor); // Dark blue for active state.
        }
        else
        {
            ColorUtility.TryParseHtmlString("#008CDB", out newColor); // Purple for inactive state.
        }

        colors.normalColor = newColor;
        button.colors = colors;

        button.Select();
        button.OnDeselect(null); // Force the button to refresh its visual state.
    }

    /// <summary>
    /// Updates orbit-related UI fields like apogee, perigee, and eccentricity.
    /// </summary>
    /// <param name="apogee">Apogee in km.</param>
    /// <param name="perigee">Perigee in km.</param>
    /// <param name="semiMajorAxis">Semi-major axis in km.</param>
    /// <param name="eccentricity">Orbital eccentricity (unitless).</param>
    /// <param name="orbitalPeriod">Orbital period in seconds.</param>
    /// <param name="inclination">Inclination in degrees.</param>
    /// <param name="RAAN">Right Ascension of Ascending Node in degrees.</param>
    public void UpdateOrbitUI(float apogee, float perigee, float semiMajorAxis, float eccentricity, float orbitalPeriod, float inclination, float RAAN)
    {
        SetText(apogeeText, "Apogee(远地点)", apogee);
        SetText(perigeeText, "Perigee(近地点)", perigee);
        SetText(semiMajorAxisText, "Semi-Major Axis(半长轴)", semiMajorAxis * 10f);
        SetText(eccentricityText, "Eccentricity(偏心率)", eccentricity, "", "F3");
        SetText(orbitalPeriodText, "Orbital Period(轨道周期)", orbitalPeriod, "s");
        SetText(inclinationText, "Inclination(倾斜率)", inclination, "°");
        SetText(raanText, "RAAN(升交点赤经)", RAAN, "°", "F1");
    }

    public void UpdateDeltaV(float deltaV)
    {
        if (deltaV != 0f)
        {
            SetText(deltaVText, "DeltaV", deltaV, "km/s", "F3");
        }
        else
        {
            deltaVText.text = "";
        }

    }

    /// <summary>
    /// Sets formatted text to a UI element with optional unit and precision.
    /// </summary>
    /// <param name="textElement">UI element to update.</param>
    /// <param name="label">Label for the field ("Apogee").</param>
    /// <param name="value">Numerical value.</param>
    /// <param name="unit">Unit of measurement (default: "km").</param>
    /// <param name="format">String format (default: "F0").</param>
    private void SetText(TextMeshProUGUI textElement, string label, float value, string unit = "km", string format = "F0")
    {
        if (textElement != null)
            textElement.text = value >= 0 ? $"{label}: {value.ToString(format)} {unit}".Trim() : string.Empty;
    }
}

using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Manages the placement of celestial bodies in the scene.
/// Handles user input for specifying radius, name, and mass of new bodies,
/// and transitions between placement and tracking modes.
/// </summary>
/// ///管理天体在场景中的位置。

///处理用户输入指定新物体的半径，名称和质量；

///放置和跟踪模式之间的转换。
public class ObjectPlacementManager : MonoBehaviour
{
    [Header("References - Core")]
    public Camera mainCamera;
    public GameObject spherePrefab; // Placeholder GameObject without NBody script  没有NBody脚本的占位游戏对象
    public GravityManager gravityManager;
    public CameraController cameraController;
    public VelocityDragManager velocityDragManager;

    [Header("References - UI")]
    public TMP_InputField objectNameInputField;
    public TMP_InputField nameInputField;
    public TMP_InputField massInput;
    public TMP_InputField massInputField;
    public TMP_InputField radiusInput;
    public TMP_InputField radiusInputField;
    public TMP_InputField positionInput;
    public TextMeshProUGUI feedbackText;
    public Button placeObjectButton;

    [Header("TLE Placement")]
    public TMP_InputField tleNameInputField;
    public TMP_InputField tleMassInputField;
    public TMP_InputField tleLine1InputField;
    public TMP_InputField tleLine2InputField;
    public Button placeTLEObjectButton;

    [Header("Ghost Preview")]
    public GameObject ghostPreviewPrefab;
    private GameObject ghostInstance;

    [Header("Placement State")]
    public GameObject lastPlacedGameObject; // Reference to the last placed placeholder GameObject  引用最后放置的占位符GameObject
    private bool isInPlacementMode = false;
    private int satelliteCount = 0;
    private bool objectIsPlaced = false;

    private const float MaxSatelliteDistance = 50000f; // in sim units (adjust as needed)  在模拟单位（根据需要调整）
    private const int MaxSatelliteNameLength = 15;

    private SimContext ctx;

    public void Initialize(SimContext ctx)
    {
        this.ctx = ctx;
        this.cameraController = ctx.CameraController;

        positionInput.onValueChanged.AddListener(OnPositionInputChanged);

        if (ghostPreviewPrefab != null)
        {
            ghostInstance = Instantiate(ghostPreviewPrefab);
            ghostInstance.SetActive(false); // Start hidden
        }
    }

    /// <summary>
    /// Starts the placement process for a new celestial body.
    /// Parses radius and mass input, instantiates a placeholder object,
    /// and initializes velocity drag UI.
    /// </summary>
    /// ///开始一个新天体的放置过程。

    ///解析半径和质量输入，实例化一个占位符对象；

    ///初始化速度拖动UI。
    public void StartPlacement()
    {
        if (lastPlacedGameObject != null)
        {
            feedbackText.text = "You must set the velocity of the current planet before placing another.";
            return;
        }

        if (!isInPlacementMode)
        {
            feedbackText.text = "You must be in FreeCam mode to place planets.";
            return;
        }

        string customName = objectNameInputField?.text;

        if (!string.IsNullOrWhiteSpace(customName) && customName.Length > MaxSatelliteNameLength)
        {
            feedbackText.text = $"Satellite name too long. Max {MaxSatelliteNameLength} characters.";
            return;
        }

        if (string.IsNullOrWhiteSpace(customName))
        {
            customName = $"Satellite {satelliteCount}";
        }

        Vector3 parsedPosition;

        // If no input, use fallback camera-based position  如果没有输入，则使用基于摄像头的回退位置
        if (string.IsNullOrWhiteSpace(positionInput.text))
        {
            parsedPosition = mainCamera.transform.position + mainCamera.transform.forward * 10f;
        }
        else
        {
            // Try to parse the input  尝试解析输入
            if (!ParsingUtils.TryParseVector3(positionInput.text, out parsedPosition))
            {
                feedbackText.text = "Invalid position input. Please use numeric x,y,z format.";
                return;
            }

            // Validate distance  验证距离
            float distanceFromEarth = Vector3.Distance(Vector3.zero, parsedPosition);
            float minDistance = 638f;
            float maxDistance = 5000f;

            if (distanceFromEarth < minDistance || distanceFromEarth > maxDistance)
            {
                feedbackText.text = $"Invalid position: must be between {minDistance * 10f:N0} km and {maxDistance * 10f:N0} km from Earth's center.";
                return;
            }
        }



        string radiusText = radiusInput.text;
        if (string.IsNullOrWhiteSpace(radiusText))
        {
            feedbackText.text = "Please enter a radius in the format x,y,z. Numbers only.";
            return;
        }

        if (!ParsingUtils.TryParseVector3(radiusText, out Vector3 parsedRadius))
        {
            feedbackText.text = "Invalid radius. Use numeric x,y,z.";
            return;
        }

        string massText = massInput.text;
        if (string.IsNullOrWhiteSpace(massText))
        {
            feedbackText.text = "Please enter a numeric mass between 5 and 1,000,000 kg.";
            return;
        }

        if (!ParsingUtils.TryParseMass(massText, out float mass))
        {
            feedbackText.text = "Invalid mass. Enter a number between 500 and 1,000,000.";
            return;
        }

        float placeholderMass = mass;

        parsedRadius = new Vector3(
            Mathf.Clamp(parsedRadius.x, .5f, 1f),
            Mathf.Clamp(parsedRadius.y, .5f, 1f),
            Mathf.Clamp(parsedRadius.z, .5f, 1f)
        );

        objectIsPlaced = true;

        lastPlacedGameObject = Instantiate(spherePrefab);
        lastPlacedGameObject.transform.localScale = new Vector3(parsedRadius.x * 1f, parsedRadius.y * 1f, parsedRadius.z * 1f);
        lastPlacedGameObject.transform.position = parsedPosition;

        // if (ParsingUtils.TryParseVector3(positionInput.text, out Vector3 parsedPosition))
        // {
        //     lastPlacedGameObject.transform.position = parsedPosition;
        // }
        // else
        // {
        //     lastPlacedGameObject.transform.position = mainCamera.transform.position + mainCamera.transform.forward * 10f;
        // }


        if (ghostInstance != null)
        {
            ghostInstance.SetActive(false);
        }

        satelliteCount++;

        lastPlacedGameObject.name = customName;
        lastPlacedGameObject.tag = "Planet";

        if (velocityDragManager != null)
        {
            velocityDragManager.ResetDragManager();
            velocityDragManager.planet = lastPlacedGameObject;
            velocityDragManager.placeholderMass = placeholderMass;
        }

        SetupCameraTracking(lastPlacedGameObject);
        // CameraController camController = gravityManager.GetComponent<CameraController>();
        // if (camController != null)
        // {
        //     camController.RefreshBodiesList();
        //     camController.SetTargetPlaceholder(lastPlacedGameObject.transform);
        //     if (camController.IsFreeCamMode)
        //     {
        //         camController.ReturnToTracking();
        //     }
        //     camController.SetInEarthView(false);
        // }

        ClearAndUnfocusInputField(radiusInput);
        ClearAndUnfocusInputField(positionInput);
        ClearAndUnfocusInputField(objectNameInputField);
        ClearAndUnfocusInputField(massInput);

        if (nameInputField != null && massInputField != null && radiusInputField != null)
        {
            nameInputField.interactable = false;

            positionInput.interactable = false;

            massInputField.interactable = false;

            radiusInputField.interactable = false;

            placeObjectButton.interactable = false;
        }

        feedbackText.text =
    "Setting Satellite Velocity:\n\n" +
"• Click the satellite and drag.\n" +
"• Set the desired direction.\n" +
"• Use input field to adjust speed.";
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void PlaceObjectFromTLE()
    {
        if (!TLEParser.TryParseTLE(tleLine1InputField.text, tleLine2InputField.text, out Vector3 position, out Vector3 velocity))
        {
            feedbackText.text = "Invalid TLE input. Check formatting.";
            return;
        }

        string name = !string.IsNullOrWhiteSpace(tleNameInputField.text) ? tleNameInputField.text : $"TLE Satellite {satelliteCount + 1}";
        if (!ParsingUtils.TryParseMass(tleMassInputField.text, out float mass))
        {
            feedbackText.text = "Invalid mass. Enter a number between 500 and 1,000,000.";
            return;
        }

        satelliteCount++;
        lastPlacedGameObject = Instantiate(spherePrefab);
        lastPlacedGameObject.name = name;
        lastPlacedGameObject.tag = "Planet";
        lastPlacedGameObject.transform.position = position;
        lastPlacedGameObject.transform.localScale = Vector3.one * 1f;

        objectIsPlaced = true;

        // Apply velocity directly
        if (velocityDragManager != null)
        {
            velocityDragManager.planet = lastPlacedGameObject;
            velocityDragManager.placeholderMass = mass;
        }

        SetupCameraTracking(lastPlacedGameObject);
        // CameraController camController = gravityManager.GetComponent<CameraController>();
        // if (camController != null)
        // {
        //     camController.RefreshBodiesList();
        //     camController.SetTargetPlaceholder(lastPlacedGameObject.transform);
        //     if (camController.IsFreeCamMode)
        //     {
        //         camController.ReturnToTracking();
        //     }
        //     camController.SetInEarthView(false);
        // }

        // Apply velocity directly
        if (velocityDragManager != null)
        {
            velocityDragManager.planet = lastPlacedGameObject;
            velocityDragManager.placeholderMass = mass;
            velocityDragManager.ApplyVelocityToPlanet(velocity);
        }

        ClearAndUnfocusInputField(tleNameInputField);
        ClearAndUnfocusInputField(tleMassInputField);
        ClearAndUnfocusInputField(tleLine1InputField);
        ClearAndUnfocusInputField(tleLine2InputField);
    }

    /// <summary>
    /// Cancels the current placement process and removes the placeholder object.
    /// Also resets velocity UI and tracking camera.
    /// </summary>
    /// ///取消当前放置过程并移除占位符对象。

    ///也重置速度UI和跟踪相机。
    public void CancelPlacement()
    {
        if (lastPlacedGameObject != null)
        {
            Destroy(lastPlacedGameObject);
            lastPlacedGameObject = null;
        }

        feedbackText.text = "";

        velocityDragManager.dragLineRenderer.positionCount = 0;

        cameraController.UpdateTrajectoryRender(cameraController.currentIndex);
        cameraController.isTrackingPlaceholder = false;
        cameraController.ReturnToTracking();
    }

    private void SetupCameraTracking(GameObject target)
    {
        // CameraController camController = CameraController.Instance;
        if (cameraController != null)
        {
            cameraController.RefreshBodiesList();
            cameraController.SetTargetPlaceholder(target.transform);
            if (cameraController.IsFreeCamMode)
            {
                cameraController.ReturnToTracking();
            }
            cameraController.SetInEarthView(false);
        }
    }

    /// <summary>
    /// Handles changes to the position input field during object placement.
    /// Moves the camera so that it faces the desired target position and shows a ghost preview at that position.
    /// </summary>
    /// <param name="input">The string input from the user, expected in "x,y,z" format.</param>
    /// ///处理对象放置过程中位置输入字段的变化。
    /// ///移动相机，使其面向目标位置，并在该位置显示鬼影预览。
    /// <param name=“input“>用户输入的字符串，格式为”x,y,z”
    // private void OnPositionInputChanged(string input)
    // {
    //     if (mainCamera == null)
    //         return;

    //     if (ParsingUtils.TryParseVector3(input, out Vector3 targetPosition))
    //     {
    //         ghostInstance.SetActive(true);
    //         ghostInstance.transform.position = targetPosition;

    //         float placementDistance = 10f;

    //         Vector3 directionToOrigin = (Vector3.zero - targetPosition).normalized;

    //         // Move camera so the object will be placed at targetPosition
    //         Vector3 cameraPosition = targetPosition - directionToOrigin * placementDistance;

    //         Quaternion rotation = Quaternion.LookRotation(directionToOrigin, Vector3.up);

    //         mainCamera.transform.SetPositionAndRotation(cameraPosition, rotation);
    //     }
    //     else
    //     {
    //         ghostInstance.SetActive(false); // Hide if input is invalid
    //     }
    // }


    private void OnPositionInputChanged(string input)
    {
        if (mainCamera == null)
            return;

        // Hide and clear feedback on empty input
        if (string.IsNullOrWhiteSpace(input))
        {
            ghostInstance.SetActive(false);
            feedbackText.text = "";
            return;
        }

        if (ParsingUtils.TryParseVector3(input, out Vector3 targetPosition))
        {
            float distanceFromEarth = Vector3.Distance(Vector3.zero, targetPosition);
            float minDistance = 638f;
            float maxDistance = 5000f;

            if (distanceFromEarth < minDistance || distanceFromEarth > maxDistance)
            {
                ghostInstance.SetActive(false);
                feedbackText.text = $"Distance must be between {minDistance * 10f:N0} km and {maxDistance * 10f:N0} km from Earth.";
                return;
            }

            // Valid position and distance
            ghostInstance.SetActive(true);
            ghostInstance.transform.position = targetPosition;

            float placementDistance = 10f;
            Vector3 directionToOrigin = (Vector3.zero - targetPosition).normalized;

            Vector3 cameraPosition = targetPosition - directionToOrigin * placementDistance;
            Quaternion rotation = Quaternion.LookRotation(directionToOrigin, Vector3.up);

            mainCamera.transform.SetPositionAndRotation(cameraPosition, rotation);
            feedbackText.text = ""; // Clear any previous messages
        }
        else
        {
            ghostInstance.SetActive(false);
            feedbackText.text = "Invalid format. Use numeric x,y,z values.";
        }
    }


    /// <summary>
    /// Clears and unfocuses the specified TMP input field.
    /// </summary>
    /// <param name="inputField">The input field to clear and unfocus.</param>
    /// 
    /// ///清除并取消指定TMP输入字段的焦点。
    /// <param name="inputField">要清除和取消聚焦的输入字段

    private void ClearAndUnfocusInputField(TMP_InputField inputField)
    {
        if (inputField != null)
        {
            inputField.text = "";
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    /// <summary>
    /// Enables placement mode (FreeCam) to allow new objects to be added.
    /// Called by the Free Cam button.
    /// </summary>
    /// ///启用放置模式（FreeCam），允许添加新对象。
    ///由Free Cam按钮调用。
    public void BreakToFreeCam()
    {
        //Debug.Log("Switching to FreeCam...");
        isInPlacementMode = true;
    }

    /// <summary>
    /// Disables FreeCam mode and reverts back to tracking mode.
    /// Cancels placement if an object was being placed.
    /// </summary>
    /// ///禁用FreeCam模式，并返回到跟踪模式。
    ///如果对象正在放置，则取消放置。
    public void ExitFreeCam()
    {
        //Debug.Log("Exiting FreeCam...");

        if (objectIsPlaced)
        {
            objectIsPlaced = false;
            CancelPlacement();
        }
        else
        {
            cameraController.ReturnToTracking();
        }
        isInPlacementMode = false;
    }

    /// <summary>
    /// Resets the reference to the last placed placeholder GameObject.
    /// Clears feedback text.
    /// </summary>
    /// ///重置最后放置的占位符GameObject的引用。

    ///清除反馈文本。
    public void ResetLastPlacedGameObject()
    {
        feedbackText.text = "";
        lastPlacedGameObject = null;
    }
}
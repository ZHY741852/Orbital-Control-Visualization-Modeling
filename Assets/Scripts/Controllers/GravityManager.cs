using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;


/// <summary>
/// Manages the registration, deregistration, and tracking of celestial bodies (NBody objects).
/// Tracks all NBody instances in the scene and provides access to their states.
/// 管理天体（NBody）的注册、注销和跟踪。
/// 跟踪场景中所有的NBody实例并提供对其状态的访问。
/// </summary>
/// 

public class GravityManager : MonoBehaviour
{
    [Header("References - Scripts")]
    public NBody CentralBody { get; private set; }
    private CameraController cameraController;
    private LineVisibilityManager lineVisibilityManager;
    private SimContext ctx;

    [Header("Body Tracking")]
    private List<NBody> bodies = new List<NBody>();
    public List<NBody> Bodies => bodies;

    [Header("Simulation Settings")]
    public float simulationTime = 0f;
    public float minCollisionDistance = 0.5f;

    [Header("References - UI")]
    public TMP_Dropdown bodyDropdown;

    /// <summary>
    /// Called by SimulationBootstrap once all public refs are set.
    /// SimulationBootstrap调用一旦所有公共refs设置。
    /// </summary>
    public void Initialize(SimContext ctx)
    {
        this.ctx = ctx;
        this.lineVisibilityManager = ctx.LineVisibilityManager;
        this.cameraController = ctx.CameraController;
        bodyDropdown.ClearOptions();
        var allBodies = FindObjectsByType<NBody>(FindObjectsSortMode.None);
        foreach (var body in allBodies.OrderByDescending(b => b.isCentralBody))
            RegisterBody(body);

        if (ctx.CameraController == null)
            Debug.LogError("GravityManager: CameraController missing from context!");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    void FixedUpdate()
    {
        simulationTime += Time.fixedDeltaTime;
    }

    /// <summary>
    /// Registers a new NBody object into the simulation.
    /// </summary>
    /// <param name="body">The NBody object to register.</param>
    /// ///在模拟中注册一个新的NBody对象。
    /// <param name="body">要注册的NBody对象
    public void RegisterBody(NBody body)
    {
        if (body.isCentralBody) CentralBody = body;

        if (!bodies.Contains(body))
        {
            bodies.Add(body);
            if (body.name != "Earth")
            {
                bodyDropdown.options.Add(new TMP_Dropdown.OptionData(body.name));
                bodyDropdown.RefreshShownValue();
            }
        }

        if (lineVisibilityManager != null)
        {
            lineVisibilityManager.RegisterNBody(body);
            Debug.Log($"[GRAVITY MANAGER]: Registered NBody with LineVisibilityManager: {body.gameObject.name}");
        }
        else
        {
            Debug.LogError("[GRAVITY MANAGER]: LineVisibilityManager.Instance is null. Ensure LineVisibilityManager is in the scene.");
        }
    }

    /// <summary>
    /// Deregisters an NBody object from the simulation.
    /// </summary>
    /// <param name="body">The NBody object to deregister.</param>
    /// 从模拟中注销一个NBody对象。
    /// </param name="body">要注销的NBody对象
    public void DeregisterBody(NBody body)
    {
        if (body == CentralBody) CentralBody = null;

        if (bodies.Contains(body))
        {
            bodies.Remove(body);
        }

        int indexToRemove = bodyDropdown.options.FindIndex(option => option.text == body.name);
        if (indexToRemove != -1)
        {
            bodyDropdown.options.RemoveAt(indexToRemove);
            bodyDropdown.RefreshShownValue();
        }
    }

    /// <summary>
    /// Handles a collision between two bodies by removing the one with lesser mass.
    /// </summary>
    /// <param name="bodyA">The first body involved in the collision.</param>
    /// <param name="bodyB">The second body involved in the collision.</param>
    /// 通过移除质量较小的物体来处理两个物体之间的碰撞。
    /// <param name="bodyA">第一个涉及到碰撞的主体。</param>

    /// <param name="bodyB">第二个涉及到碰撞的主体
    public void HandleCollision(NBody bodyA, NBody bodyB)
    {
        NBody bodyToRemove = (bodyA.mass < bodyB.mass) ? bodyA : bodyB;

        if (cameraController != null && cameraController.IsTracking(bodyToRemove))
        {
            cameraController.SwitchToNextValidBody(bodyToRemove);
        }

        DeregisterBody(bodyToRemove);
        Debug.Log(bodyToRemove.gameObject);
        Destroy(bodyToRemove.gameObject);

        if (cameraController != null)
        {
            cameraController.RefreshBodiesList();
        }

        cameraController.UpdateDropdownSelection();

        Debug.Log($"[GRAVITY MANAGER]: Removed {bodyToRemove.name} due to collision.");
    }
}
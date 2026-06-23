using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FruitSpawner : MonoBehaviour
{
    public enum DropMode { Crane, LaserPointer, ParabolicThrow }

    public GameObject fruitPrefab;
    public FruitData[] spawnableFruits;  // level 0〜4 のみ入れる
    public FruitData[] allFruits;        // level 0〜10 全部
    public float spawnY = 6f;
    public float moveRangeX = 1.8f;
    public float moveRangeZ = 1.8f;
    public float dropCooldown = 0.7f;

    [Header("Drop Mode")]
    public DropMode currentMode = DropMode.Crane;
    public Text modeText;          // Canvas > Interaction の Text
    public Transform pointerOrigin; // レーザー用。未設定ならCamera.mainで代用(将来XRコントローラーを割り当てる想定)

    [Header("Parabolic Throw")]
    public Transform controllerTransform; // 投擲用。未設定ならマウス(落下面への投影)で代用
    public float maxThrowSpeed = 8f;
    public float swingSampleWindow = 0.15f; // スイング速度を計算するための直近サンプル時間(秒)

    private GameObject previewFruit;
    private FruitData nextData;
    private float lastDropTime;
    private LineRenderer laserLine;
    private Light laserLight;
    private LineRenderer trajectoryLine;
    private InputAction throwButtonAction;
    private readonly List<(Vector3 pos, float time)> swingSamples = new();
    private bool isSwinging;

    void OnEnable()
    {
        Fruit.OnMergeRequest += HandleMerge;
        // コントローラーのトリガーボタンでの投擲用(実機が無い場合は単に押されない状態になるだけ)
        throwButtonAction = new InputAction(binding: "<XRController>{RightHand}/triggerButton");
        throwButtonAction.Enable();
    }

    void OnDisable()
    {
        Fruit.OnMergeRequest -= HandleMerge;
        throwButtonAction?.Disable();
    }

    void Start()
    {
        SetupLaserLine();
        SetupTrajectoryLine();
        PrepareNext();
        UpdateModeText();
    }

    void SetupLaserLine()
    {
        var go = new GameObject("LaserLine");
        go.transform.SetParent(transform);
        laserLine = go.AddComponent<LineRenderer>();
        laserLine.positionCount = 2;
        laserLine.startWidth = 0.18f;
        laserLine.endWidth = 0.18f;
        laserLine.alignment = LineAlignment.View; // 常にカメラ正面を向かせて見切れを防ぐ
        laserLine.numCapVertices = 4;
        laserLine.material = new Material(Shader.Find("Sprites/Default"));
        laserLine.startColor = laserLine.endColor = Color.red;
        laserLine.enabled = false;

        var lightGo = new GameObject("LaserSpot");
        lightGo.transform.SetParent(transform);
        laserLight = lightGo.AddComponent<Light>();
        laserLight.type = LightType.Point;
        laserLight.color = Color.red;
        laserLight.range = 3f;
        laserLight.intensity = 8f;
        laserLight.enabled = false;
    }

    void SetupTrajectoryLine()
    {
        var go = new GameObject("TrajectoryLine");
        go.transform.SetParent(transform);
        trajectoryLine = go.AddComponent<LineRenderer>();
        trajectoryLine.positionCount = 0;
        trajectoryLine.startWidth = 0.05f;
        trajectoryLine.endWidth = 0.05f;
        trajectoryLine.alignment = LineAlignment.View;
        trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
        trajectoryLine.startColor = trajectoryLine.endColor = Color.green;
        trajectoryLine.enabled = false;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) ToggleMode();

        if (previewFruit == null) return;

        switch (currentMode)
        {
            case DropMode.Crane:
                UpdatePointerDrop(GetCraneTarget());
                break;
            case DropMode.LaserPointer:
                UpdatePointerDrop(GetLaserTarget());
                break;
            case DropMode.ParabolicThrow:
                UpdateParabolicThrow();
                break;
        }
    }

    // クレーン/レーザー共通: 指示位置に追従させ、クリックでその場に落とす
    void UpdatePointerDrop(Vector3? hit)
    {
        if (hit.HasValue)
        {
            var pos = previewFruit.transform.position;
            pos.x = Mathf.Clamp(hit.Value.x, -moveRangeX, moveRangeX);
            pos.z = Mathf.Clamp(hit.Value.z, -moveRangeZ, moveRangeZ);
            previewFruit.transform.position = pos;
        }

        if (Input.GetMouseButtonDown(0) && Time.time - lastDropTime > dropCooldown)
        {
            FinishDrop(Vector3.zero);
        }
    }

    // クレーン方式: マウス位置からカメラ視点でレイを飛ばし、落下面との交点を使う
    Vector3? GetCraneTarget()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        var plane = new Plane(Vector3.up, new Vector3(0, spawnY, 0));
        return plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : null;
    }

    // レーザーポインター方式: コントローラーから放射されるRaycastで落下地点を指示する
    // pointerOrigin未割り当て時(デスクトップ確認用)はマウスのスクリーン座標からレイを飛ばす
    Vector3? GetLaserTarget()
    {
        Vector3 originPos;
        Ray ray;
        if (pointerOrigin != null)
        {
            originPos = pointerOrigin.position;
            ray = new Ray(pointerOrigin.position, pointerOrigin.forward);
        }
        else
        {
            originPos = Camera.main.transform.position;
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        }

        var plane = new Plane(Vector3.up, new Vector3(0, spawnY, 0));
        Vector3? result = plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : null;
        var endPoint = result ?? ray.GetPoint(10f);

        laserLine.enabled = true;
        laserLine.SetPosition(0, originPos);
        laserLine.SetPosition(1, endPoint);

        // 着弾点のすぐ上に光源を置き、真下のフルーツを照らす
        laserLight.enabled = true;
        laserLight.transform.position = endPoint + Vector3.up * 0.3f;

        return result;
    }

    // 放物線投擲方式: ボタンを押しながら動かした軌跡(スイング)から速度を推定し、離した時にその速度で投げる
    void UpdateParabolicThrow()
    {
        bool held = IsThrowInputHeld();
        bool released = WasThrowInputReleased();

        if (held)
        {
            var samplePos = GetSwingSourcePosition();
            if (samplePos.HasValue) RecordSwingSample(samplePos.Value);
            isSwinging = true;

            var velocity = ComputeSwingVelocity();
            ShowTrajectoryPreview(previewFruit.transform.position, velocity);
        }
        else
        {
            trajectoryLine.enabled = false;
        }

        if (released && isSwinging && Time.time - lastDropTime > dropCooldown)
        {
            var velocity = ComputeSwingVelocity();
            FinishDrop(velocity);
            isSwinging = false;
            swingSamples.Clear();
            trajectoryLine.enabled = false;
        }
    }

    bool IsThrowInputHeld() => controllerTransform != null ? throwButtonAction.IsPressed() : Input.GetMouseButton(0);
    bool WasThrowInputReleased() => controllerTransform != null ? throwButtonAction.WasReleasedThisFrame() : Input.GetMouseButtonUp(0);

    // スイングの基準位置: コントローラーがあればその位置、なければマウスを落下面に投影した位置で代用
    Vector3? GetSwingSourcePosition()
    {
        if (controllerTransform != null) return controllerTransform.position;
        return GetCraneTarget();
    }

    void RecordSwingSample(Vector3 pos)
    {
        swingSamples.Add((pos, Time.time));
        while (swingSamples.Count > 0 && Time.time - swingSamples[0].time > swingSampleWindow)
            swingSamples.RemoveAt(0);
    }

    Vector3 ComputeSwingVelocity()
    {
        if (swingSamples.Count < 2) return Vector3.zero;
        var oldest = swingSamples[0];
        var newest = swingSamples[^1];
        float dt = newest.time - oldest.time;
        if (dt <= 0.0001f) return Vector3.zero;
        return Vector3.ClampMagnitude((newest.pos - oldest.pos) / dt, maxThrowSpeed);
    }

    // 放物線の軌道予測線を表示する(重力に従ったシンプルな弾道シミュレーション)
    void ShowTrajectoryPreview(Vector3 origin, Vector3 velocity)
    {
        if (velocity.magnitude < 0.05f)
        {
            trajectoryLine.enabled = false;
            return;
        }

        const int maxSteps = 60;
        const float dt = 0.05f;
        var points = new List<Vector3>(maxSteps);
        for (int i = 0; i < maxSteps; i++)
        {
            float t = i * dt;
            var pos = origin + velocity * t + 0.5f * Physics.gravity * (t * t);
            points.Add(pos);
            if (pos.y <= 0f) break;
        }

        trajectoryLine.positionCount = points.Count;
        trajectoryLine.SetPositions(points.ToArray());
        trajectoryLine.enabled = true;
    }

    void ToggleMode()
    {
        currentMode = (DropMode)(((int)currentMode + 1) % System.Enum.GetValues(typeof(DropMode)).Length);

        laserLine.enabled = currentMode == DropMode.LaserPointer;
        laserLight.enabled = currentMode == DropMode.LaserPointer;
        trajectoryLine.enabled = false;
        isSwinging = false;
        swingSamples.Clear();

        UpdatePreviewVisibility();
        UpdateModeText();
    }

    // レーザーポインター中はプレビュー中のフルーツを非表示にする(着地点はレーザーだけで示す)
    void UpdatePreviewVisibility()
    {
        if (previewFruit == null) return;
        previewFruit.GetComponent<MeshRenderer>().enabled = currentMode != DropMode.LaserPointer;
    }

    void UpdateModeText()
    {
        if (modeText == null) return;
        string label = currentMode switch
        {
            DropMode.Crane => "クレーン",
            DropMode.LaserPointer => "レーザーポインター",
            DropMode.ParabolicThrow => "放物線投擲",
            _ => currentMode.ToString(),
        };
        modeText.text = $"モード: {label} (Space で切り替え)";
    }

    void PrepareNext()
    {
        nextData = spawnableFruits[Random.Range(0, spawnableFruits.Length)];
        previewFruit = Instantiate(fruitPrefab, new Vector3(0, spawnY, 0), Quaternion.identity);
        previewFruit.GetComponent<Rigidbody>().isKinematic = true;
        previewFruit.GetComponent<Fruit>().Initialize(nextData);
        UpdatePreviewVisibility();
    }

    // クレーン/レーザーはその場に落とす(velocity = 0)、放物線投擲は推定したスイング速度で投げる
    void FinishDrop(Vector3 velocity)
    {
        var rb = previewFruit.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.linearVelocity = velocity;
        previewFruit.GetComponent<Fruit>().isDropped = true;
        previewFruit.GetComponent<MeshRenderer>().enabled = true; // 落下後は常に表示
        previewFruit = null;
        lastDropTime = Time.time;
        Invoke(nameof(PrepareNext), dropCooldown);
    }

    void HandleMerge(Fruit a, Fruit b)
    {
        int nextLevel = a.data.level + 1;
        Vector3 mid = (a.transform.position + b.transform.position) * 0.5f;

        Destroy(a.gameObject);
        Destroy(b.gameObject);

        if (nextLevel >= allFruits.Length) return;  // スイカ同士は消滅

        var newFruit = Instantiate(fruitPrefab, mid, Quaternion.identity);
        newFruit.GetComponent<Fruit>().Initialize(allFruits[nextLevel]);
        newFruit.GetComponent<Fruit>().isDropped = true;

        GameManager.Instance?.AddScore(allFruits[nextLevel].score);
    }
}

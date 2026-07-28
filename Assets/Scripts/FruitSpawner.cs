using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class FruitSpawner : MonoBehaviour
{
    public enum DropMode { Crane, LaserPointer, ParabolicThrow }

    public GameObject fruitPrefab;
    public FruitData[] spawnableFruits;  // level 0〜4 のみ入れる
    public FruitData[] allFruits;        // level 0〜10 全部
    public float spawnY = 6f;
    public float moveRangeX = 1.5f;
    public float moveRangeZ = 1.5f;
    public float dropCooldown = 0.7f;

    [Header("Drop Mode")]
    public DropMode currentMode = DropMode.Crane;
    public Text modeText;          // Canvas > Interaction の Text
    public Text fruitText;         // Canvas > Fruit の Text (現在操作中)
    public Text nextFruitText;     // Canvas > NextFruit の Text (次のフルーツ)
    public Transform pointerOrigin; // レーザー用。未設定ならCamera.mainで代用(将来XRコントローラーを割り当てる想定)
    public Vector3 laserOriginWorldPos = new Vector3(3.5f, 9f, -3f); // 固定発射点（Inspectorで調整可）

    [Header("Audio")]
    public AudioClip mergeSound;

    [Header("Parabolic Throw")]
    public Transform controllerTransform; // 投擲用。未設定ならマウス(落下面への投影)で代用
    public float maxThrowSpeed = 8f;
    public float minThrowSpeed = 0.3f; // これ未満はマウスのブレとみなして無視する(長押し停止時に変な方向へ飛ぶのを防止)
    public float swingSampleWindow = 0.15f; // スイング速度を計算するための直近サンプル時間(秒)

    private GameObject previewFruit;
    private FruitData nextData;
    private FruitData upcomingData; // 次の次のフルーツ(先読み)
    private float lastDropTime;
    private LineRenderer laserLine;
    private Light laserLight;
    private LineRenderer trajectoryLine;
    private InputAction throwButtonAction;
    private readonly List<(Vector3 pos, float time)> swingSamples = new();
    private bool isSwinging;
    private bool throwBlockedByUI;

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
        // タイトル画面で選択されたモードを適用
        if (PlayerPrefs.HasKey("DropMode"))
        {
            currentMode = (DropMode)PlayerPrefs.GetInt("DropMode");
            PlayerPrefs.DeleteKey("DropMode");
        }

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
        // UIボタン上でのマウスDown開始をブロックする(投擲の誤発射防止)
        if (Input.GetMouseButtonDown(0) && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            throwBlockedByUI = true;
        if (Input.GetMouseButtonUp(0))
            throwBlockedByUI = false;

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

        if (Input.GetMouseButtonDown(0) && Time.time - lastDropTime > dropCooldown
            && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
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

    // レーザーポインター方式: 固定発射点からビームを飛ばし、Physics.Raycastで実際のフルーツ/床面を照準する
    Vector3? GetLaserTarget()
    {
        // 発射点: XRコントローラー割当済みならその位置、なければ固定ワールド座標
        Vector3 originPos = pointerOrigin != null ? pointerOrigin.position : laserOriginWorldPos;

        // マウス/コントローラーの向きをカメラ経由で取得してPhysics.Raycastを飛ばす
        Ray aimRay = pointerOrigin != null
            ? new Ray(pointerOrigin.position, pointerOrigin.forward)
            : Camera.main.ScreenPointToRay(Input.mousePosition);

        Vector3 hitPoint;
        if (Physics.Raycast(aimRay, out RaycastHit hit, Mathf.Infinity,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            hitPoint = hit.point;
        }
        else
        {
            // 何も当たらない場合は床面(Y=0)にフォールバック
            var floor = new Plane(Vector3.up, Vector3.zero);
            if (!floor.Raycast(aimRay, out float dist))
            {
                laserLine.enabled = false;
                laserLight.enabled = false;
                return null;
            }
            hitPoint = aimRay.GetPoint(dist);
        }

        // X,Zをボックス範囲内にクランプ
        hitPoint.x = Mathf.Clamp(hitPoint.x, -moveRangeX, moveRangeX);
        hitPoint.z = Mathf.Clamp(hitPoint.z, -moveRangeZ, moveRangeZ);

        // 固定発射点→着弾点へのビームを描画
        laserLine.enabled = true;
        laserLine.SetPosition(0, originPos);
        laserLine.SetPosition(1, hitPoint);

        // 着弾点を照らすスポットライト
        laserLight.enabled = true;
        laserLight.transform.position = hitPoint + Vector3.up * 0.5f;

        return hitPoint; // UpdatePointerDropはX,Zのみ使用しspawnYは維持される
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

    bool IsThrowInputHeld() => controllerTransform != null ? throwButtonAction.IsPressed() : Input.GetMouseButton(0) && !throwBlockedByUI;
    bool WasThrowInputReleased() => controllerTransform != null ? throwButtonAction.WasReleasedThisFrame() : Input.GetMouseButtonUp(0) && !throwBlockedByUI;

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
        var velocity = (newest.pos - oldest.pos) / dt;
        if (velocity.magnitude < minThrowSpeed) return Vector3.zero; // マウスのブレによるノイズを無視
        return Vector3.ClampMagnitude(velocity, maxThrowSpeed);
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

    // レーザーポインター中はプレビューフルーツを非表示にする
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
            DropMode.Crane => "Crane (クレーン)",
            DropMode.LaserPointer => "Laser (レーザー)",
            DropMode.ParabolicThrow => "Throw (とうてき)",
            _ => currentMode.ToString(),
        };
        modeText.text = $"モード: {label} ";
    }

    void PrepareNext()
    {
        // 先読みがなければ初回として両方ランダム生成
        if (upcomingData == null)
            upcomingData = spawnableFruits[Random.Range(0, spawnableFruits.Length)];

        nextData = upcomingData;
        upcomingData = spawnableFruits[Random.Range(0, spawnableFruits.Length)];

        previewFruit = Instantiate(fruitPrefab, new Vector3(0, spawnY, 0), Quaternion.identity);
        previewFruit.GetComponent<Rigidbody>().isKinematic = true;
        previewFruit.GetComponent<Collider>().enabled = false; // 落下前は当たり判定を無効化
        previewFruit.GetComponent<Fruit>().Initialize(nextData);
        UpdatePreviewVisibility();

        if (fruitText != null) fruitText.text = "先のフルーツ: " + nextData.fruitName;
        if (nextFruitText != null) nextFruitText.text = "今のフルーツ: " + upcomingData.fruitName;
    }

    // クレーン/レーザーはその場に落とす(velocity = 0)、放物線投擲は推定したスイング速度で投げる
    void FinishDrop(Vector3 velocity)
    {
        var rb = previewFruit.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.linearVelocity = velocity;
        previewFruit.GetComponent<Fruit>().isDropped = true;
        previewFruit.GetComponent<MeshRenderer>().enabled = true;
        previewFruit.GetComponent<Collider>().enabled = true; // レーザーモード中に無効化していたコライダーを復元
        previewFruit = null;
        lastDropTime = Time.time;
        Invoke(nameof(PrepareNext), dropCooldown);
    }

    void HandleMerge(Fruit a, Fruit b)
    {
        int nextLevel = a.data.level + 1;
        Vector3 mid = (a.transform.position + b.transform.position) * 0.5f;

        if (mergeSound != null)
            AudioSource.PlayClipAtPoint(mergeSound, mid);

        Destroy(a.gameObject);
        Destroy(b.gameObject);

        if (nextLevel >= allFruits.Length) return;  // スイカ同士は消滅

        var newFruit = Instantiate(fruitPrefab, mid, Quaternion.identity);
        newFruit.GetComponent<Fruit>().Initialize(allFruits[nextLevel]);
        newFruit.GetComponent<Fruit>().isDropped = true;

        GameManager.Instance?.AddScore(allFruits[nextLevel].score);
    }
}

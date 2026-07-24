using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

public class FreeLookRightClickGate : MonoBehaviour
{
    public float buttonRotateSpeed = 90f; // ボタン長押し時の回転速度(度/秒)

    CinemachineInputAxisController axisController;
    CinemachineOrbitalFollow orbitalFollow;
    Vector2 buttonInput;

    void Awake()
    {
        axisController = GetComponent<CinemachineInputAxisController>();
        orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
    }

    void Start()
    {
        WireButton("Left",  new Vector2( 1,  0));
        WireButton("Right", new Vector2(-1,  0));
        WireButton("Up",    new Vector2( 0,  1));
        WireButton("Down",  new Vector2( 0, -1));
    }

    void WireButton(string objectName, Vector2 dir)
    {
        var go = GameObject.Find(objectName);
        if (go == null)
        {
            Debug.LogWarning($"FreeLookRightClickGate: '{objectName}' が見つかりません");
            return;
        }
        var et = go.GetComponent<EventTrigger>() ?? go.AddComponent<EventTrigger>();

        // pressed フラグで「本当に押されている間のみ」減算し、二重減算を防ぐ
        bool pressed = false;
        AddTrigger(et, EventTriggerType.PointerDown, _ => { if (!pressed) { buttonInput += dir; pressed = true; } });
        AddTrigger(et, EventTriggerType.PointerUp,   _ => { if (pressed) { buttonInput -= dir; pressed = false; } });
        AddTrigger(et, EventTriggerType.PointerExit, _ => { if (pressed) { buttonInput -= dir; pressed = false; } });
    }

    void AddTrigger(EventTrigger et, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        et.triggers.Add(entry);
    }

    void Update()
    {
        bool look = Input.GetMouseButton(1);
        var x = axisController.GetController("Look Orbit X");
        var y = axisController.GetController("Look Orbit Y");
        if (x != null) x.Enabled = look;
        if (y != null) y.Enabled = look;

        // ボタン入力 + 矢印キー入力を合算して軌道軸に適用
        Vector2 arrowInput = new Vector2(
            -Input.GetAxisRaw("Horizontal"), // 左右反転(右キー→右回転)
             Input.GetAxisRaw("Vertical")
        );
        Vector2 totalInput = buttonInput + arrowInput;
        if (totalInput != Vector2.zero && orbitalFollow != null)
        {
            orbitalFollow.HorizontalAxis.Value += totalInput.x * buttonRotateSpeed * Time.deltaTime;
            orbitalFollow.VerticalAxis.Value   += totalInput.y * buttonRotateSpeed * Time.deltaTime;
        }
    }
}

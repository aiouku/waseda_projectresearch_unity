using UnityEngine;
using UnityEngine.InputSystem;

public class QuestControllerInput : MonoBehaviour
{
    // Input Actionsをコードで直接定義
    InputAction triggerRight;
    InputAction gripRight;
    InputAction thumbstickRight;
    InputAction buttonA;
    InputAction buttonB;

    void OnEnable()
    {
        // 右トリガー
        triggerRight = new InputAction(binding: "<XRController>{RightHand}/trigger");
        triggerRight.Enable();

        // 右グリップ
        gripRight = new InputAction(binding: "<XRController>{RightHand}/grip");
        gripRight.Enable();

        // 右スティック
        thumbstickRight = new InputAction(binding: "<XRController>{RightHand}/thumbstick");
        thumbstickRight.Enable();

        // Aボタン
        buttonA = new InputAction(binding: "<XRController>{RightHand}/primaryButton");
        buttonA.Enable();

        // Bボタン
        buttonB = new InputAction(binding: "<XRController>{RightHand}/secondaryButton");
        buttonB.Enable();
    }

    void Update()
    {
        float trigger = triggerRight.ReadValue<float>();
        float grip = gripRight.ReadValue<float>();
        Vector2 stick = thumbstickRight.ReadValue<Vector2>();

        if (trigger > 0.1f)
            Debug.Log($"右トリガー: {trigger}");

        if (grip > 0.1f)
            Debug.Log($"右グリップ: {grip}");

        if (stick.magnitude > 0.1f)
            Debug.Log($"右スティック: {stick}");

        if (buttonA.WasPressedThisFrame())
            Debug.Log("Aボタン押下");

        if (buttonB.WasPressedThisFrame())
            Debug.Log("Bボタン押下");
    }

    void OnDisable()
    {
        triggerRight?.Disable();
        gripRight?.Disable();
        thumbstickRight?.Disable();
        buttonA?.Disable();
        buttonB?.Disable();
    }
}
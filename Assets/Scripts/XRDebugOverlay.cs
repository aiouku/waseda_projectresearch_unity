using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Management;

// 動作確認用の一時デバッグ表示。原因が分かったら削除してOK。
public class XRDebugOverlay : MonoBehaviour
{
    void OnGUI()
    {
        var sb = new StringBuilder();

        var xrSettings = XRGeneralSettings.Instance;
        bool xrRunning = xrSettings != null && xrSettings.Manager != null && xrSettings.Manager.activeLoader != null;
        sb.AppendLine($"XR Loader稼働中: {xrRunning} ({(xrRunning ? xrSettings.Manager.activeLoader.name : "なし")})");

        sb.AppendLine("---接続中のInput Systemデバイス---");
        foreach (var device in InputSystem.devices)
        {
            sb.AppendLine($"{device.displayName} ({device.layout}) usages=[{string.Join(",", device.usages)}]");
        }

        var rightHand = XRController.rightHand;
        sb.AppendLine("---RightHand XRController---");
        sb.AppendLine(rightHand == null ? "見つかりません" : rightHand.displayName);

        if (rightHand != null)
        {
            var triggerPressed = rightHand.TryGetChildControl<UnityEngine.InputSystem.Controls.ButtonControl>("triggerPressed");
            var trigger = rightHand.TryGetChildControl<UnityEngine.InputSystem.Controls.AxisControl>("trigger");
            sb.AppendLine($"trigger(軸): {(trigger != null ? trigger.ReadValue().ToString("F2") : "コントロールなし")}");
            sb.AppendLine($"triggerPressed: {(triggerPressed != null ? triggerPressed.isPressed.ToString() : "コントロールなし")}");
        }

        GUI.Label(new Rect(10, 10, 800, 400), sb.ToString());
    }
}

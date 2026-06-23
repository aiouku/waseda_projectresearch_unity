using Unity.Cinemachine;
using UnityEngine;

public class FreeLookRightClickGate : MonoBehaviour
{
    CinemachineInputAxisController axisController;

    void Awake()
    {
        axisController = GetComponent<CinemachineInputAxisController>();
    }

    void Update()
    {
        bool look = Input.GetMouseButton(1);
        var x = axisController.GetController("Look Orbit X");
        var y = axisController.GetController("Look Orbit Y");
        if (x != null) x.Enabled = look;
        if (y != null) y.Enabled = look;
    }
}

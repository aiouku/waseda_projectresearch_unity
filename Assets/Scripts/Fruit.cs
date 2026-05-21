using UnityEngine;

public class Fruit : MonoBehaviour
{
    public FruitData data;
    public bool isMerging = false;
    public bool isDropped = false;

    public static System.Action<Fruit, Fruit> OnMergeRequest;

    public void Initialize(FruitData newData)
    {
        data = newData;
        transform.localScale = Vector3.one * data.radius * 2f;
        GetComponent<MeshRenderer>().material = data.material;
    }

    void OnCollisionEnter(Collision col)
    {
        if (isMerging) return;
        var other = col.gameObject.GetComponent<Fruit>();
        if (other == null || other.isMerging) return;
        if (other.data.level != data.level) return;
        if (GetInstanceID() > other.GetInstanceID()) return;  // 自分が小さいIDの時だけ

        isMerging = true;
        other.isMerging = true;
        OnMergeRequest?.Invoke(this, other);
    }
}
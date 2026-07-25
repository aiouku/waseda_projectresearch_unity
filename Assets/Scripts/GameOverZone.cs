using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameOverZone : MonoBehaviour
{
    public float overflowDelay = 1.5f;

    readonly Dictionary<Fruit, Coroutine> tracked = new();

    void OnTriggerStay(Collider other)
    {
        var fruit = other.GetComponent<Fruit>();
        if (fruit == null || !fruit.isDropped || fruit.isMerging) return;
        if (!tracked.ContainsKey(fruit))
            tracked[fruit] = StartCoroutine(CheckOverflow(fruit));
    }

    void OnTriggerExit(Collider other)
    {
        var fruit = other.GetComponent<Fruit>();
        if (fruit != null) CancelTracking(fruit);
    }

    void CancelTracking(Fruit fruit)
    {
        if (tracked.TryGetValue(fruit, out var co))
        {
            if (co != null) StopCoroutine(co);
            tracked.Remove(fruit);
        }
    }

    IEnumerator CheckOverflow(Fruit fruit)
    {
        yield return new WaitForSeconds(overflowDelay);

        if (fruit == null || !fruit.isDropped || fruit.isMerging) yield break;

        GameManager.Instance?.GameOver();
    }
}

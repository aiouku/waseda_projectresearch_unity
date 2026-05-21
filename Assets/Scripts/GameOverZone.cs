using UnityEngine;
using System.Collections.Generic;

public class GameOverZone : MonoBehaviour
{
    public float gameOverDelay = 2f;
    private Dictionary<Fruit, float> staying = new();

    void OnTriggerStay(Collider other)
    {
        var fruit = other.GetComponent<Fruit>();
        if (fruit == null || !fruit.isDropped || fruit.isMerging) return;

        if (!staying.ContainsKey(fruit)) staying[fruit] = Time.time;
        else if (Time.time - staying[fruit] > gameOverDelay)
        {
            GameManager.Instance.GameOver();
        }
    }

    void OnTriggerExit(Collider other)
    {
        var fruit = other.GetComponent<Fruit>();
        if (fruit != null) staying.Remove(fruit);
    }
}
using System.Collections;
using UnityEngine;

public class GameOverZone : MonoBehaviour
{
    public float overflowDelay = 1.5f; // カゴから溢れた状態が何秒続いたらゲームオーバーか

    void OnTriggerEnter(Collider other)
    {
        var fruit = other.GetComponent<Fruit>();
        if (fruit == null || !fruit.isDropped || fruit.isMerging) return;
        StartCoroutine(CheckOverflow(fruit));
    }

    IEnumerator CheckOverflow(Fruit fruit)
    {
        float elapsed = 0f;
        while (elapsed < overflowDelay)
        {
            yield return null;

            // フルーツが消えた(合体など)場合はキャンセル
            if (fruit == null || !fruit.isDropped || fruit.isMerging) yield break;

            // カゴの高さより下に戻ったらキャンセル
            if (fruit.transform.position.y < transform.position.y) yield break;

            elapsed += Time.deltaTime;
        }

        GameManager.Instance?.GameOver();
    }
}

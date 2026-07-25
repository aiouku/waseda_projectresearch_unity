using UnityEngine;

public class GameOverWall : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        var fruit = collision.gameObject.GetComponent<Fruit>();
        if (fruit == null || !fruit.isDropped || fruit.isMerging) return;

        GameManager.Instance?.GameOver();
    }
}

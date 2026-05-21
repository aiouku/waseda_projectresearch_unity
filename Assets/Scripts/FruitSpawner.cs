using UnityEngine;

public class FruitSpawner : MonoBehaviour
{
    public GameObject fruitPrefab;
    public FruitData[] spawnableFruits;  // level 0〜4 のみ入れる
    public FruitData[] allFruits;        // level 0〜10 全部
    public float spawnY = 6f;
    public float moveRangeX = 1.8f;
    public float dropCooldown = 0.7f;

    private GameObject previewFruit;
    private FruitData nextData;
    private float lastDropTime;

    void OnEnable() { Fruit.OnMergeRequest += HandleMerge; }
    void OnDisable() { Fruit.OnMergeRequest -= HandleMerge; }

    void Start()
    {
        PrepareNext();
    }

    void Update()
    {
        if (previewFruit == null) return;

        // マウスX座標を -moveRangeX 〜 +moveRangeX にマップ
        float mouseX = (Input.mousePosition.x / Screen.width - 0.5f) * 2f * moveRangeX;
        var pos = previewFruit.transform.position;
        pos.x = Mathf.Clamp(mouseX, -moveRangeX, moveRangeX);
        previewFruit.transform.position = pos;

        if (Input.GetMouseButtonDown(0) && Time.time - lastDropTime > dropCooldown)
        {
            Drop();
        }
    }

    void PrepareNext()
    {
        nextData = spawnableFruits[Random.Range(0, spawnableFruits.Length)];
        previewFruit = Instantiate(fruitPrefab, new Vector3(0, spawnY, 0), Quaternion.identity);
        previewFruit.GetComponent<Rigidbody>().isKinematic = true;
        previewFruit.GetComponent<Fruit>().Initialize(nextData);
    }

    void Drop()
    {
        var rb = previewFruit.GetComponent<Rigidbody>();
        rb.isKinematic = false;
        previewFruit.GetComponent<Fruit>().isDropped = true;
        previewFruit = null;
        lastDropTime = Time.time;
        Invoke(nameof(PrepareNext), dropCooldown);
    }

    void HandleMerge(Fruit a, Fruit b)
    {
        int nextLevel = a.data.level + 1;
        Vector3 mid = (a.transform.position + b.transform.position) * 0.5f;

        Destroy(a.gameObject);
        Destroy(b.gameObject);

        if (nextLevel >= allFruits.Length) return;  // スイカ同士は消滅

        var newFruit = Instantiate(fruitPrefab, mid, Quaternion.identity);
        newFruit.GetComponent<Fruit>().Initialize(allFruits[nextLevel]);
        newFruit.GetComponent<Fruit>().isDropped = true;

        GameManager.Instance?.AddScore(allFruits[nextLevel].score);
    }
}
using UnityEngine;

[CreateAssetMenu(fileName = "FruitData", menuName = "Suika/FruitData")]
public class FruitData : ScriptableObject
{
    public int level;            // 0〜10
    public string fruitName;
    public float radius;         // スケール
    public Material material;
    public int score;
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TableManager : MonoBehaviour
{
    [System.Serializable]
    public class PlayerData
    {
        public int id;
        public string name;
        public int score;
    }

    public GameObject rowPrefab;          // 预制体
    public Transform contentParent;       // ScrollView 的 Content

    // 示例数据
    private List<PlayerData> playerList = new List<PlayerData>()
    {
        new PlayerData(){ id=1, name="Alice", score=90 },
        new PlayerData(){ id=2, name="Bob", score=85 },
        new PlayerData(){ id=3, name="Carol", score=78 },
    };

    void Start()
    {
        PopulateTable();
    }

    void PopulateTable()
    {
        foreach (var player in playerList)
        {
            GameObject row = Instantiate(rowPrefab, contentParent);
            row.transform.Find("IdText").GetComponent<TextMeshProUGUI>().text = player.id.ToString();
            row.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = player.name;
            row.transform.Find("ScoreText").GetComponent<TextMeshProUGUI>().text = player.score.ToString();
        }
    }
}
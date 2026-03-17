using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;                     // ← Added for TextMeshPro

public class GameManager : MonoBehaviour
{
    [Header("References - Assign in Inspector")]
    public GameObject cardPrefab;
    public Transform boardParent;
    public GridLayoutGroup gridLayout;
    public TextMeshProUGUI scoreText;        // ← CHANGED to TMP (better quality)

    private List<Card> cards = new List<Card>();
    private List<Card> flippedCards = new List<Card>();
    private int rows = 4, cols = 4;
    private int score = 0;
    private int combo = 0;

    private AudioSource audioSource;
    public AudioClip flipSound, matchSound, mismatchSound, gameOverSound;

    private const string SAVE_KEY = "CardMatchSave";

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        LoadProgress();
        if (cards.Count == 0)
        {
            GenerateBoard(4, 4);
        }
        UpdateScoreUI();
    }

    private void OnApplicationQuit()
    {
        SaveProgress();
    }

    public void GenerateBoard(int r, int c)
    {
        rows = r;
        cols = c;
        ClearBoard();

        List<int> ids = Enumerable.Range(0, (r * c) / 2).ToList();
        ids.AddRange(ids);
        ids = ids.OrderBy(x => Random.value).ToList();

        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = c;

        for (int i = 0; i < r * c; i++)
        {
            GameObject go = Instantiate(cardPrefab, boardParent);
            Card card = go.GetComponent<Card>();
            card.id = ids[i];
            card.frontSprite = GetSpriteForId(card.id);
            card.backSprite = GetBackSprite();
            cards.Add(card);

            Button btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() => OnCardClicked(card));
        }

        float cellSize = Mathf.Min(Screen.width / c * 0.85f, Screen.height / r * 0.85f);
        gridLayout.cellSize = new Vector2(cellSize, cellSize);
    }

    private void ClearBoard()
    {
        foreach (Transform child in boardParent)
            Destroy(child.gameObject);
        cards.Clear();
        flippedCards.Clear();
    }

    private Sprite GetSpriteForId(int id)
    {
        Texture2D tex = new Texture2D(128, 128);
        Color color = new Color((id % 5) * 0.2f + 0.3f, (id % 3) * 0.3f + 0.3f, (id % 7) * 0.15f + 0.4f);
        for (int x = 0; x < 128; x++)
            for (int y = 0; y < 128; y++)
                tex.SetPixel(x, y, color);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
    }

    private Sprite GetBackSprite()
    {
        Texture2D tex = new Texture2D(128, 128);
        for (int x = 0; x < 128; x++)
            for (int y = 0; y < 128; y++)
                tex.SetPixel(x, y, new Color(0.1f, 0.1f, 0.2f));
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(0.5f, 0.5f));
    }

    private void OnCardClicked(Card card)
    {
        if (card.IsFlipped() || card.IsMatched() || flippedCards.Count >= 2) return;

        card.Flip(true);
        flippedCards.Add(card);
        PlaySound("flip");

        if (flippedCards.Count == 2)
            StartCoroutine(CheckMatch());
    }

    private IEnumerator CheckMatch()
    {
        yield return new WaitForSeconds(0.3f);

        Card c1 = flippedCards[0];
        Card c2 = flippedCards[1];

        if (c1.id == c2.id)
        {
            c1.SetMatched();
            c2.SetMatched();
            score += 100 + (combo * 50);
            combo++;
            PlaySound("match");
            UpdateScoreUI();
            SaveProgress();

            if (IsGameWon())
            {
                PlaySound("gameover");
                Debug.Log("🎉 Game Won! Final Score: " + score);
            }
        }
        else
        {
            c1.Flip(false);
            c2.Flip(false);
            combo = 0;
            PlaySound("mismatch");
        }

        flippedCards.Clear();
    }

    private bool IsGameWon() => cards.TrueForAll(c => c.IsMatched());

    private void UpdateScoreUI()
    {
        if (scoreText != null)
            scoreText.text = $"Score: {score} | Combo: {combo}";
    }

    private void PlaySound(string type)
    {
        AudioClip clip = type switch
        {
            "flip" => flipSound,
            "match" => matchSound,
            "mismatch" => mismatchSound,
            "gameover" => gameOverSound,
            _ => null
        };

        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    [System.Serializable]
    private class GameSave
    {
        public int rows, cols;
        public int score;
        public int combo;
    }

    public void SaveProgress()
    {
        GameSave save = new GameSave { rows = rows, cols = cols, score = score, combo = combo };
        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(save));
        PlayerPrefs.Save();
    }

    public void LoadProgress()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

        GameSave save = JsonUtility.FromJson<GameSave>(PlayerPrefs.GetString(SAVE_KEY));
        score = save.score;
        combo = save.combo;
        GenerateBoard(save.rows, save.cols);
    }
}
// Card.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Card : MonoBehaviour
{
    public int id;                    // matching pair ID
    public Sprite frontSprite;
    public Sprite backSprite;

    private Image frontImage;
    private bool isFlipped = false;
    private bool isMatched = false;

    private void Awake() => frontImage = transform.GetChild(0).GetComponent<Image>();

    public void Flip(bool faceUp)
    {
        if (isMatched || isFlipped == faceUp) return;
        StartCoroutine(FlipCoroutine(faceUp));
    }

    private IEnumerator FlipCoroutine(bool faceUp)
    {
        // Flip to back (hide)
        for (float i = 1; i >= 0; i -= 0.1f)
        {
            transform.localScale = new Vector3(i, 1, 1);
            yield return new WaitForSeconds(0.015f);
        }

        frontImage.sprite = faceUp ? frontSprite : backSprite;
        isFlipped = faceUp;

        // Flip to front
        for (float i = 0; i <= 1; i += 0.1f)
        {
            transform.localScale = new Vector3(i, 1, 1);
            yield return new WaitForSeconds(0.015f);
        }
    }

    public void SetMatched() => isMatched = true;
    public bool IsFlipped() => isFlipped;
    public bool IsMatched() => isMatched;
}
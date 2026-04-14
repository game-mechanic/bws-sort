using UnityEngine;
using UnityEngine.UI;

public class HandUI : Singleton<HandUI>
{
    private Transform handTransform;
    private Image hand;
    public Vector2 offset;
    public Sprite idle;
    public Sprite click;
    public float followSpeed = 10;
    private Vector2 mousePosition;

    public Vector2 MousePosition { get => mousePosition; set => mousePosition = value; }

    void Start()
    {
        handTransform = transform.GetChild(0);
        hand = handTransform.GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.mousePosition.x < 0 || Input.mousePosition.y < 0
        || Input.mousePosition.x > Screen.width || Input.mousePosition.y > Screen.height)
            return;

        MousePosition = Vector3.Lerp(handTransform.position, Input.mousePosition + new Vector3(offset.x, offset.y), followSpeed * Time.deltaTime);
        handTransform.position = MousePosition;
        if (Input.GetMouseButtonDown(0)) hand.sprite = click;
        else if (Input.GetMouseButtonUp(0)) hand.sprite = idle;
    }
}

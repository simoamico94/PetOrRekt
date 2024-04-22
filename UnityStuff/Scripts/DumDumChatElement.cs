using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class DumDumChatElement : MonoBehaviour
{
	public RectTransform textObject;

	public TMP_Text id;
	public TMP_Text timestamp;
	public TMP_Text message;

	public Image background;

	public Color userColor;
	public Color othersColor;

	public float height;
	private RectTransform rectTransform;

	void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    public void SetData(string id, string username, long timestamp, string message, bool isMine)
    {
        this.id.text = username + " | " + id;
        this.message.text = message;

		DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).LocalDateTime;
		TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
		DateTime localDateTime = TimeZoneInfo.ConvertTime(dateTime, localTimeZone);
		string format;
		if (localTimeZone.Id.StartsWith("en-US"))
		{
			format = "MM/dd/yyyy h:mm:ss tt";
		}
		else
		{
			format = "dd/MM/yyyy HH:mm:ss";
		}

		this.timestamp.text = localDateTime.ToString(format);

		background.color = isMine ? userColor : othersColor;

		//rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, height);

		if (isMine)
		{
			// Align to the right
			textObject.anchorMin = new Vector2(1, textObject.anchorMin.y);
			textObject.anchorMax = new Vector2(1, textObject.anchorMax.y);
			textObject.pivot = new Vector2(1, textObject.pivot.y);
		}
		else
		{
			// Align to the left
			textObject.anchorMin = new Vector2(0, textObject.anchorMin.y);
			textObject.anchorMax = new Vector2(0, textObject.anchorMax.y);
			textObject.pivot = new Vector2(0, textObject.pivot.y);
		}

		textObject.sizeDelta = new Vector2(800, textObject.sizeDelta.y);
	}
}

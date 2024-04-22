using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class DumDumLeaderboardElement : MonoBehaviour
{
	public TMP_Text rankingText;
	public TMP_Text nameText;
	public TMP_Text idText;
	public TMP_Text scoreText;
	public TMP_Text petTimesText;
	public TMP_Text petStreakText;
	public TMP_Text maxPetStreakText;

	public DumDumUser user;
	public int ranking;

	public void SetData(DumDumUser user, int ranking)
	{
		this.user = user;
		this.ranking = ranking;	

		rankingText.text = ranking.ToString();
		nameText.text = user.username;
		idText.text = user.id;
		scoreText.text = user.points.ToString() + " DDP"; //Pensare se mettere "K" , "M" se il numero è troppo grande
		petTimesText.text = "Pet Times: " + user.petTimes.ToString();
		petStreakText.text = "Current Pet Streak: " + user.petStreak.ToString();
		maxPetStreakText.text = "Max Pet Streak: " + user.maxPetStreak.ToString();
	}
}

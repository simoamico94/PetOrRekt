using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DumDumVotingHandler : MonoBehaviour
{
    public List<Button> choices = new List<Button>();
	[SerializeField] private Color highlightColor = Color.green;

    public int actualIndex;
	
	// Start is called before the first frame update
	void Awake()
    {
		choices[0].onClick.AddListener(() => Select(0));
		choices[1].onClick.AddListener(() => Select(1));
		choices[2].onClick.AddListener(() => Select(2));
		choices[3].onClick.AddListener(() => Select(3));
		choices[4].onClick.AddListener(() => Select(4));

		Select(0);
	}

    public void SetVotes(List<int> votes)
    {
        for (int i = 0; i < choices.Count; i++)
        {
			choices[i].transform.GetChild(1).GetComponent<TMP_Text>().text = "Current Votes: " + votes[i].ToString();
		}
    }

	public void ToggleButtonsInteractables(bool canInteract)
	{
		foreach(Button button in choices)
		{
			button.interactable = canInteract;
		}
	}

    public void Select(int index)
    {
		for (int i = 0; i < choices.Count; i++)
		{
            if(i == index)
            {
                choices[i].transform.GetChild(0).GetComponent<Image>().color = highlightColor;
            }
            else
            {
				choices[i].transform.GetChild(0).GetComponent<Image>().color = Color.white;
			}
		}

        actualIndex = index;
	}
}

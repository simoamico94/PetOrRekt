using SimpleJSON;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

[Serializable]
public class DumDumUser
{
	public string id;
	public string shortId;
	public string username;
	public string referredWallet;
	public string points;
	public long lastPetTime;
	public List<int> currentVotes;
	public long stakedCred;
	public int petTimes;
	public int petStreak;
	public int maxPetStreak;
}

[Serializable]
public class DumDumInfo
{
	public float version;
	public float stakedCredMultiplier;
	public float referralPointsMultiplier;
	public float streakMultiplier;
	public long nextUpdate;
	public double petTimeInterval;
	public int basePetPoints;
	public List<int> dumDumState;
}

//Cose da fare:
//Creare LUA che gestisce tutte queste cose: Referral, Registrazione, GetInfo varie, Classifica, Pet, Accessori 
//Cose da fare solo una volta che tutto il resto è stato fatto: Staking e Chat
public class SocialManager : MonoBehaviour
{
	//private string dumdumID = "WbZvJLuh06y4Rp0P0uQZt0CD0uT2uPhd1IUQB4p6XWo";
	private string dumdumID = "gFFFqz2n2hnmx1v7uX2Ri0UsVTX0ntmXEufnCOh91CE"; //dumdumRC
	private string baseUrl = "https://cu.ao-testnet.xyz/dry-run?process-id=";

	private string baseJsonBody = @"{
            ""Id"":""1234"",
            ""Target"":""GAMEPROCESSID"",
            ""Owner"":""OWNERID"",
            ""Tags"":[{""name"":""Action"",""value"":""ACTIONID""}]
        }";

	private int baseTimeout = 5;
	private int extendedTimeout = 30;
	private int actualTimeout = 5;

	public AOConnectManager manager => AOConnectManager.main;

	private string currentAddress => manager.CurrentAddress;

	public DumDumManager dumDumManager;
	public ScreenshotHandler screenshotHandler;
	public DumDumLeaderboardElement leaderboardInfoScreenshot;

	[Header("Info")]
	public DumDumInfo dumDumInfo;
	public DumDumUser user;
	public float supportedVersion = 0.1f;

	[Header("UI")]
	public GameObject menuButton;
	public Button petButton;
	public TMP_Text nextUpdateText;
	public Button screenshotButton;

	[Header("General")]
	public Button openMenuButton;
	public Button closeMenuButton;
	public TMP_Text menuButtonText;
	public TMP_Text welcomeText;
	public GameObject registeredPanel;
	public GameObject notRegisteredPanel;
	public GameObject loadingPanel;

	[Header("Register")]
	public TMP_InputField usernameInputField;
	public TMP_InputField referralInputField;
	public Button registerButton;

	[Header("Info Panel")]
	public TMP_Text titleText;
	public TMP_Text referralPointsText;
	public TMP_Text petTimeInterval;
	public TMP_Text petPointsFormulaText;

	[Header("Leaderboard")]
	public Button refreshLeaderboard;
	public DumDumLeaderboardElement leaderboardElementPrefab;
	public DumDumLeaderboardElement selfLeaderboardElement;
	public List<DumDumLeaderboardElement> leaderboardElements = new List<DumDumLeaderboardElement>();
	public Transform leaderboardElementParent;

	[Header("Voting")]
	public Button refreshVoting;
	public Button voteButton;
	public List<DumDumVotingHandler> voteHandlers;

	[Header("Chat")]
	public Button refreshChatButton;
	public TMP_InputField messageInputField;
	public Button sendMessageButton;
	public DumDumChatElement chatElementPrefab;
	public List<DumDumChatElement> chatElements = new List<DumDumChatElement>();
	public Transform chatElementParent;
	public ScrollRect chatScrollRect;

	[Header("Staking")]
	public Button stakeButton;
	public Button unstakeButton;

	private bool sentAwakeMessage = false;
	private int maxLeaderboardCount = 20;
	private bool isMenuOpen = false;

	private bool petting = false;
	private bool scrollNextUpdate = false;

	private float getInfoRateMobile = 5;
	private float getInfoRateDesktop = 10;

	[DllImport("__Internal")]
	private static extern void DumDumRegisterWithReferralJS(string pid, string username, string referral);

	[DllImport("__Internal")]
	private static extern void DumDumRegisterJS(string pid, string username);

	[DllImport("__Internal")]
	private static extern void DumDumPetJS(string pid);

	[DllImport("__Internal")]
	private static extern void DumDumVoteJS(string pid, string data);

	[DllImport("__Internal")]
	private static extern void DumDumSendChatJS(string pid, string data);

	private void Awake()
	{
		user = null;
	}

	void Start()
    {
        AOConnectManager.main.OnCurrentAddressChange += OnCurrentAddressChanged;

		openMenuButton.onClick.AddListener(OpenMenu);
		closeMenuButton.onClick.AddListener(CloseMenu);
		refreshLeaderboard.onClick.AddListener(GetLeaderboard);
		refreshVoting.onClick.AddListener(GetCurrentVotes);
		refreshChatButton.onClick.AddListener(() => StartCoroutine(ScrollDownChat()));
		petButton.onClick.AddListener(SendMessagePet);
		registerButton.onClick.AddListener(SendMessageRegister);
		voteButton.onClick.AddListener(SendMessageVote);
		sendMessageButton.onClick.AddListener(SendMessageChat);
		screenshotButton.onClick.AddListener(MakeScreenshot);

		registeredPanel.SetActive(false);
		notRegisteredPanel.SetActive(false);

		StartCoroutine(SendPostRequest("GetDumDumInfo", SetDumDumInfo));

		if (Application.isEditor) //DEBUG
		{
			manager.CurrentAddress = "vRrw1H_bgr2gF_7dtrkVN6kjmtXkKAA4BwJH8JXE2ys";
			//manager.CurrentAddress = "WZqL88rXfsny194M6ofBBUbUM5bLYVas9eEH1HujBasd";
		}

		if(IsMobile())
		{
			menuButton.SetActive(false);
		}

	}

	private void Update()
	{
		if(dumDumInfo != null && dumDumInfo.nextUpdate > 0)
		{
			DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(dumDumInfo.nextUpdate).LocalDateTime;
			//TimeZoneInfo localTimeZone = TimeZoneInfo.Local;
			//DateTime localDateTime = TimeZoneInfo.ConvertTime(dateTime, localTimeZone);
			//string format;
			//if (localTimeZone.Id.StartsWith("en-US"))
			//{
			//	format = "MM/dd/yyyy h:mm:ss tt";
			//}
			//else
			//{
			//	format = "dd/MM/yyyy HH:mm:ss";
			//}
			//nextUpdate.text = "Next Update will be on " + localDateTime.ToString(format);

			DateTimeOffset now = DateTimeOffset.UtcNow;
			TimeSpan timeRemaining = dateTime - now;

			string timeToNextUpdate = "00:00:00";

			if (timeRemaining.TotalMilliseconds > 0)
			{
				timeToNextUpdate = timeRemaining.Hours.ToString("D2") + ":" + timeRemaining.Minutes.ToString("D2") + ":" + timeRemaining.Seconds.ToString("D2");
			}
			else
			{
				timeRemaining = now - dateTime;

				if(timeRemaining.TotalSeconds > 3)
				{
					if (!sentAwakeMessage)
					{
						sentAwakeMessage = true;
						Debug.LogError("NextUpdate is passed!");

						if (!IsMobile() && !Application.isEditor && !string.IsNullOrEmpty(currentAddress))
						{
							manager.SendMessageToProcess(dumdumID, "Awake", "Awake");
							StartCoroutine(SendPostRequest("GetUserInfo", SetUserInfo, 3, ownerId: currentAddress));
							StartCoroutine(SendPostRequest("GetUserInfo", SetUserInfo, 10, ownerId: currentAddress));
						}
					}
				}
			}

			nextUpdateText.text = "Time to next update: " + timeToNextUpdate;
		}

		screenshotButton.gameObject.SetActive(user != null && !isMenuOpen);
	}

	private void MakeScreenshot()
	{
		string text = dumDumManager.HasPet ? "I pet Dumdum! :D" : "I didn't pet Dumdum :(";

		string shareText = $"Look at current @permadumdum outfit! Install arconnect on your browser and go to ar://dumdum to start to earn points and decide future outfits. Use my referral to register: {user.id} #PetToEarn #PetorRekt @aoTheComputer";
		string userText = user.username;
		leaderboardInfoScreenshot.SetData(selfLeaderboardElement.user, selfLeaderboardElement.ranking);
		screenshotHandler.CaptureAndShare(text, shareText, userText, "dumdum.png");
	}

	private void OpenMenu()
	{
		if(string.IsNullOrEmpty(currentAddress))
		{
			manager.ConnectWallet();
		}
		else
		{
			if(user == null)
			{
				registeredPanel.SetActive(false);
				notRegisteredPanel.SetActive(true);
				welcomeText.text = "Create an account";
			}
			else
			{
				registeredPanel.SetActive(true);
				notRegisteredPanel.SetActive(false);
				welcomeText.text = $"{user.username} | {user.id}";
			}

			openMenuButton.GetComponent<Animator>().SetTrigger("Open");
			isMenuOpen = true;
			nextUpdateText.gameObject.SetActive(false);
			dumDumManager.canRotate = false;
		}
	}

	private void CloseMenu()
	{
		openMenuButton.GetComponent<Animator>().SetTrigger("Close");
		isMenuOpen = false;
		nextUpdateText.gameObject.SetActive(true);
		dumDumManager.canRotate = true;

		registeredPanel.SetActive(false);
		notRegisteredPanel.SetActive(false);
	}

	private void OnCurrentAddressChanged()
	{
		if(string.IsNullOrEmpty(currentAddress))
		{
			Debug.LogError("Address is null or empty!!");
			menuButtonText.text = "Connect Wallet";
			return;
		}

		user = null;

		registeredPanel.SetActive(false);
		notRegisteredPanel.SetActive(false);
		//loadingPanel.SetActive(true);
		welcomeText.text = "Checking Registration";

		//menuButtonText.text = manager.ShortenProcessID(currentAddress);
		menuButtonText.text = "Dashboard";

		openMenuButton.GetComponent<Animator>().SetTrigger("Open");
		isMenuOpen = true;
		nextUpdateText.gameObject.SetActive(false);
		dumDumManager.canRotate = false;

		GetUserInfo();
		GetLeaderboard();
		GetCurrentVotes();
		GetCurrentChat();
	}

	public void RegistrationCallback(string action)
	{
		if(action.Contains("UserInfo"))
		{
			JSONNode jsonNode = JSON.Parse(action);
			UpdateUserInfo(jsonNode);
			manager.OpenAlert("Account creation completed!");
		}
		else if(action.Contains("Failed"))
		{
			Debug.LogError(action); //Should be handled in advance and never go here
		}
		else
		{
			Debug.LogError("Unhandled action: " + action);
		}
	}

	public void PetCallback(string action)
	{
		petting = false;

		if (action.Contains("UserInfo"))
		{
			JSONNode jsonNode = JSON.Parse(action);
			JSONNode player = jsonNode["UserInfo"];
			if(player["id"] == currentAddress)
			{
				UpdateUserInfo(jsonNode);
			}
			else
			{
				manager.OpenAlert("Current address is " + currentAddress + " but received callback with user " + player["id"]);
			}
		}
		else 
		{
			if(action.ToLower().Contains("wait"))
			{
				petButton.gameObject.SetActive(false);
			}
			else
			{
				petButton.gameObject.SetActive(true);
			}
			manager.OpenAlert(action);
			Debug.LogError(action); //Should be handled in advance and never go here
		}
	}

	public void VoteCallback(string action)
	{
		if (action.Contains("UserInfo"))
		{
			JSONNode jsonNode = JSON.Parse(action);
			JSONNode player = jsonNode["UserInfo"];
			if (player["id"] == currentAddress)
			{
				UpdateUserInfo(jsonNode);
				GetCurrentVotes();
				manager.OpenAlert("Vote registered!");
			}
			else
			{
				manager.OpenAlert("Current address is " + currentAddress + " but received callback with user " + player["id"]);
			}
		}
		else
		{
			manager.OpenAlert(action);
			Debug.LogError(action); //Should be handled in advance and never go here
		}
	}

	public void SendChatMessageCallback(string action)
	{
		if (action.Contains("MessageData"))
		{
			JSONNode jsonNode = JSON.Parse(action);
			JSONNode message = jsonNode["MessageData"];

			if (message["Sender"] == currentAddress)
			{
				//string id = message["Sender"];
				//string username = message["Username"];
				//long timestamp = message["Timestamp"].AsLong;
				//string msg = message["Message"];

				//DumDumChatElement element = Instantiate(chatElementPrefab, chatElementParent);
				//element.SetData(id, username, timestamp, msg, id == currentAddress);
				//chatElements.Add(element);

				//GetCurrentChat();
				//messageInputField.text = "";
				//manager.OpenAlert("Message Sent!");

				scrollNextUpdate = true;
			}
			else
			{
				manager.OpenAlert("Current address is " + currentAddress + " but received callback with user " + message["id"]);
			}
		}
		else
		{
			manager.OpenAlert(action);
			Debug.LogError(action); //Should be handled in advance and never go here
		}
	}

	private void CheckIfPet()
	{
		if(user != null && !string.IsNullOrEmpty(currentAddress) && !petting)
		{
			dumDumManager.HasPet = user.lastPetTime > (dumDumInfo.nextUpdate - dumDumInfo.petTimeInterval);
			petButton.gameObject.SetActive(!dumDumManager.HasPet);
		}
	}

	// Set Callbacks Methods

	private void SetDumDumInfo(bool result, string jsonString)
	{
		string request = "GetDumDumInfo";

		if (!result)
		{
			Debug.LogError($"No Data in {request}");
			StartCoroutine(SendPostRequest(request, SetDumDumInfo, 3));
			return;
		}

		JSONNode fullJsonNode = JSON.Parse(jsonString);

		if (fullJsonNode.HasKey("Messages"))
		{
			jsonString = fullJsonNode["Messages"].AsArray[0]["Data"];

			if (string.IsNullOrEmpty(jsonString))
			{
				Debug.LogError($"No Data in {request}");
				StartCoroutine(SendPostRequest(request, SetDumDumInfo, 3));
				return;
			}

			JSONNode jsonNode = JSON.Parse(jsonString);

			if (jsonNode.HasKey("DumDumInfo"))
			{
				JSONNode info = jsonNode["DumDumInfo"];

				dumDumInfo = new DumDumInfo();

				dumDumInfo.version = info["Version"].AsFloat;
				dumDumInfo.streakMultiplier = info["StreakMultiplier"].AsFloat;
				dumDumInfo.stakedCredMultiplier = info["StakedCredMultiplier"].AsFloat;
				dumDumInfo.referralPointsMultiplier = info["ReferralPointsMultiplier"].AsFloat;
				dumDumInfo.basePetPoints = info["BasePetPoints"].AsInt;
				dumDumInfo.nextUpdate = info["NextUpdate"].AsLong;
				dumDumInfo.petTimeInterval = info["PetTimeInterval"].AsDouble;

				JSONArray currentState = info["DumDumState"].AsArray;
				dumDumInfo.dumDumState = new List<int>();
				foreach (JSONNode n in currentState)
				{
					dumDumInfo.dumDumState.Add(n.AsInt);
				}

				if (dumDumInfo.version != supportedVersion)
				{
					manager.OpenAlert("Unsupported Version! Go on ar://dumdum to use the last version");

					Debug.LogError("Unsupported Version! Go on ar://dumdum to use the last version");
					return;
				}

				titleText.text = $"Pet or Rekt (v{dumDumInfo.version})";

				TimeSpan timeSpan = TimeSpan.FromMilliseconds(dumDumInfo.petTimeInterval);
				int hours = timeSpan.Hours;
				int minutes = timeSpan.Minutes;
				string petInterval = string.Concat(hours > 0 ? (hours + " hours ") : "", minutes > 0 ? (minutes + " minutes ") : "");
				petTimeInterval.text = "You can pet and vote once every " + petInterval;
				petPointsFormulaText.text = $"Every time you pet DumDum, you will earn an amount of DDP based on this formula:\nDDP = {dumDumInfo.basePetPoints} + (CurrentStreak * {dumDumInfo.streakMultiplier}) + (StakedCRED * {dumDumInfo.stakedCredMultiplier})";
				referralPointsText.text = $"When one of the users you referred earns some DDP, you will earn {dumDumInfo.referralPointsMultiplier * 100}% of that amount";

				dumDumManager.UpdateDumDum(dumDumInfo.dumDumState);

				CheckIfPet();

				sentAwakeMessage = false;
			}
			else
			{
				Debug.LogError($"No Key DumDumInfo in {request}");
				StartCoroutine(SendPostRequest(request, SetDumDumInfo, 3));
				return;
			}
		}
		else
		{
			Debug.LogError($"No Key Messages in {request}");
			StartCoroutine(SendPostRequest(request, SetDumDumInfo, 3));
			return;
		}

		StartCoroutine(SendPostRequest(request, SetDumDumInfo, 3));
	}

	[ContextMenu("GetUserInfo")]
	public void GetUserInfo()
	{
		StartCoroutine(SendPostRequest("GetUserInfo", SetUserInfo, ownerId: currentAddress));
	}

	//Segnare i punti sul profilo
	//Segnare i CRED nella zona staking
	//Fare calcolo con il timestamp se posso fare pet o no e in update controllare che se raggiungiamo live la possibilità di fare pet lo abilitiamo, altrimenti si aggiorna live 
	//Aggiornare stato toggles per i voti
	private void SetUserInfo(bool result, string jsonString)
	{
		string request = "GetUserInfo";

		if (!result)
		{
			Debug.LogError($"Error in {request}");
			StartCoroutine(SendPostRequest(request, SetUserInfo, 3, ownerId: currentAddress));
			return;
		}

		JSONNode fullJsonNode = JSON.Parse(jsonString);

		if (fullJsonNode.HasKey("Messages"))
		{
			jsonString = fullJsonNode["Messages"].AsArray[0]["Data"];

			if (string.IsNullOrEmpty(jsonString))
			{
				Debug.LogError($"No Data in {request}");
				StartCoroutine(SendPostRequest(request, SetUserInfo, 3, ownerId: currentAddress));
				return;
			}

			if(jsonString == "User not registered!")
			{
				registeredPanel.SetActive(false);
				notRegisteredPanel.SetActive(true);
				loadingPanel.SetActive(false);

				welcomeText.text = "Create an account";

				return;
			}

			JSONNode jsonNode = JSON.Parse(jsonString);

			if(jsonNode.HasKey("UserInfo"))
			{
				UpdateUserInfo(jsonNode);
			}
			else
			{
				Debug.LogError($"No Key UserInfo in {request}");
				StartCoroutine(SendPostRequest(request, SetUserInfo, 3, ownerId: currentAddress));

				//if(self != null)
				//{
				//	loadingPanel.SetActive(false);
				//}
				return;
			}
		}
		else
		{
			Debug.LogError($"No Key Messages in {request}");
			StartCoroutine(SendPostRequest(request, SetUserInfo, 3, ownerId: currentAddress));
			return;
		}

		if (IsMobile() /*|| Application.isEditor*/)
		{
			StartCoroutine(SendPostRequest(request, SetUserInfo, 3, ownerId: currentAddress));
		}
	}

	private void UpdateUserInfo(JSONNode jsonNode)
	{
		JSONNode player = jsonNode["UserInfo"];

		user = new DumDumUser();

		user.id = player["id"];
		user.shortId = manager.ShortenProcessID(user.id);
		user.username = player["username"];
		user.points = player["points"];
		user.petTimes = player["petTimes"].AsInt;
		user.petStreak = player["petStreak"].AsInt;
		user.maxPetStreak = player["maxPetStreak"].AsInt;
		user.referredWallet = player["referredWallet"];
		user.stakedCred = player["stakedCred"].AsLong;
		user.lastPetTime = player["lastPetTime"].AsLong;

		JSONArray currentVotes = player["currentVotes"].AsArray;
		user.currentVotes = new List<int>();

		foreach (JSONNode vote in currentVotes)
		{
			user.currentVotes.Add(vote.AsInt);
		}

		welcomeText.text = $"{user.username} | {user.id}";

		for (int i = 0; i < voteHandlers.Count; i++)
		{
			if (user.currentVotes[i] == -1)
			{
				voteHandlers[i].Select(0);
			}
			else
			{
				voteHandlers[i].Select(user.currentVotes[i] - 1);
			}
		}

		voteButton.gameObject.SetActive(user.currentVotes.Contains(-1));

		foreach(DumDumVotingHandler votingHandler in voteHandlers)
		{
			votingHandler.ToggleButtonsInteractables(user.currentVotes.Contains(-1));
		}

		CheckIfPet();

		if (isMenuOpen)
		{
			registeredPanel.SetActive(true);
			notRegisteredPanel.SetActive(false);
		}
		loadingPanel.SetActive(false);
	}

	private void GetLeaderboard()
	{
		StartCoroutine(SendPostRequest("GetLeaderboard", UpdateLeaderboard));
	}

	private void UpdateLeaderboard(bool result, string jsonString)
	{
		string request = "GetLeaderboard";

		if (!result)
		{
			Debug.LogError($"Error in {request}");
			StartCoroutine(SendPostRequest(request, UpdateLeaderboard, 3));
			return;
		}

		JSONNode fullJsonNode = JSON.Parse(jsonString);

		if (fullJsonNode.HasKey("Messages"))
		{
			jsonString = fullJsonNode["Messages"].AsArray[0]["Data"];

			if (string.IsNullOrEmpty(jsonString))
			{
				Debug.LogError($"No Data in {request}");
				StartCoroutine(SendPostRequest(request, UpdateLeaderboard, 3));
				return;
			}

			JSONNode jsonNode = JSON.Parse(jsonString);

			if(jsonNode.HasKey("Leaderboard"))
			{
				JSONArray leaderboard = jsonNode["Leaderboard"].AsArray;

				if (leaderboardElements != null && leaderboardElements.Count > 0)
				{
					foreach (DumDumLeaderboardElement leaderboardElement in leaderboardElements)
					{
						Destroy(leaderboardElement.gameObject);
					}
				}

				leaderboardElements = new List<DumDumLeaderboardElement>();

				int i = 1;
				bool currentAddressFound = false;

				foreach (JSONNode player in leaderboard)
				{
					DumDumUser user = new DumDumUser();

					user.id = player["id"];
					user.username = player["username"];
					user.points = player["points"];
					user.petTimes = player["petTimes"];
					user.petStreak = player["petStreak"];
					user.maxPetStreak = player["maxPetStreak"];

					DumDumLeaderboardElement element = Instantiate(leaderboardElementPrefab, leaderboardElementParent);
					element.SetData(user, i);
					leaderboardElements.Add(element);

					if (user.id == currentAddress)
					{
						selfLeaderboardElement.gameObject.SetActive(true);
						selfLeaderboardElement.SetData(user, i);
						currentAddressFound = true;
					}

					i++;

					if(i > maxLeaderboardCount)
					{
						break;
					}
				}

				if(!currentAddressFound)
				{
					selfLeaderboardElement.gameObject.SetActive(false);
				}
			}
			else
			{
				Debug.LogError($"No Key Leaderboard in {request}");
				StartCoroutine(SendPostRequest(request, UpdateLeaderboard, 3));
				return;
			}
		}
		else
		{
			Debug.LogError($"No Key Messages in {request}");
			StartCoroutine(SendPostRequest(request, UpdateLeaderboard, 3));
			return;
		}
	}

	private void GetCurrentVotes()
	{
		StartCoroutine(SendPostRequest("GetCurrentVotes", SetCurrentVotes));
	}

	private void SetCurrentVotes(bool result, string jsonString)
	{
		string request = "GetCurrentVotes";

		if (!result)
		{
			Debug.LogError($"Error in {request}");
			StartCoroutine(SendPostRequest(request, SetCurrentVotes, 3));
			return;
		}

		JSONNode fullJsonNode = JSON.Parse(jsonString);

		if (fullJsonNode.HasKey("Messages"))
		{
			jsonString = fullJsonNode["Messages"].AsArray[0]["Data"];

			if (string.IsNullOrEmpty(jsonString))
			{
				Debug.LogError($"No Data in {request}");
				StartCoroutine(SendPostRequest(request, SetCurrentVotes, 3));
				return;
			}

			JSONNode jsonNode = JSON.Parse(jsonString);

			if (jsonNode.HasKey("CurrentVotes"))
			{
				JSONArray currentVotesArray = jsonNode["CurrentVotes"].AsArray;

				List<List<int>> currentVotes = new List<List<int>>();
				foreach (JSONNode subArray in currentVotesArray)
				{
					List<int> votes = new List<int>();
					foreach (JSONNode vote in subArray.AsArray)
					{
						votes.Add(vote.AsInt);
					}

					currentVotes.Add(votes);
				}

				for (int i = 0; i < voteHandlers.Count; i++)
				{
					voteHandlers[i].SetVotes(currentVotes[i]);
				}
			}
            else
			{
				Debug.LogError($"No Key CurrentVotes in {request}");
				StartCoroutine(SendPostRequest(request, SetCurrentVotes, 3));
				return;
			}
		}
		else
		{
			Debug.LogError($"No Key Messages in {request}");
			StartCoroutine(SendPostRequest(request, SetCurrentVotes, 3));
			return;
		}
	}

	protected IEnumerator ScrollDownChat()
	{
		yield return null;
		chatScrollRect.ScrollToBottom();
	}

	private void GetCurrentChat()
	{
		StartCoroutine(SendPostRequest("GetCurrentChat", SetCurrentChat));
	}

	private void SetCurrentChat(bool result, string jsonString)
	{
		string request = "GetCurrentChat";

		if (!result)
		{
			Debug.LogError($"Error in {request}");
			StartCoroutine(SendPostRequest(request, SetCurrentChat, 3));
			return;
		}

		JSONNode fullJsonNode = JSON.Parse(jsonString);

		if (fullJsonNode.HasKey("Messages"))
		{
			jsonString = fullJsonNode["Messages"].AsArray[0]["Data"];

			if (string.IsNullOrEmpty(jsonString))
			{
				Debug.LogError($"No Data in {request}");
				StartCoroutine(SendPostRequest(request, SetCurrentChat, 3));
				return;
			}

			JSONNode jsonNode = JSON.Parse(jsonString);

			if (jsonNode.HasKey("CurrentChat"))
			{
				JSONArray chatHistory = jsonNode["CurrentChat"].AsArray;

				if(/*chatHistory.Count < */chatElements.Count > 0) //We have less element on server => there was a reset
				{
					foreach (DumDumChatElement leaderboardElement in chatElements)
					{
						Destroy(leaderboardElement.gameObject);
					}

					chatElements = new List<DumDumChatElement>();
				}

				//int i = 1;
				for (int j = chatElements.Count; j < chatHistory.Count; j++) //Add only new messages
				{
					string id = chatHistory[j]["Sender"];
					string username = chatHistory[j]["Username"];
					long timestamp = chatHistory[j]["Timestamp"].AsLong;
					string message = chatHistory[j]["Message"];

					DumDumChatElement element = Instantiate(chatElementPrefab, chatElementParent);
					element.SetData(id, username, timestamp, message, id == currentAddress );
					chatElements.Add(element);

					//i++;

					//if (i > maxLeaderboardCount)
					//{
					//	break;
					//}
				}

				if(scrollNextUpdate)
				{
					scrollNextUpdate = false;
					StartCoroutine(ScrollDownChat());
				}
			}
			else
			{
				Debug.LogError($"No Key CurrentChat in {request}");
				StartCoroutine(SendPostRequest(request, SetCurrentChat, 3));
				return;
			}
		}
		else
		{
			Debug.LogError($"No Key Messages in {request}");
			StartCoroutine(SendPostRequest(request, SetCurrentChat, 3));
			return;
		}

		StartCoroutine(SendPostRequest(request, SetCurrentChat, 1));
	}

	private void SendMessageRegister()
	{
		if(string.IsNullOrEmpty(usernameInputField.text))
		{
			manager.OpenAlert("You must enter a username!");
		}
		else if(usernameInputField.text.Length > 15)
		{
			manager.OpenAlert("Username must be max 15 chars!");
		}
		else
		{
			if(string.IsNullOrEmpty(referralInputField.text))
			{
				DumDumRegisterJS(dumdumID, usernameInputField.text);
			}
			else
			{
				DumDumRegisterWithReferralJS(dumdumID, usernameInputField.text, referralInputField.text);
			}

			loadingPanel.SetActive(true);
		}
	}

	private void SendMessagePet()
    {
		//Fare un Send Message con Action=Pet , aspettare per double check la risposta che sia ok e appena arriva fare animazione di festeggiamento e al posto del bottone di Pet dire tra quanto si può fare il prossimo Pet
		//Dovrebbe essere dopo 24h, ma possiamo iniziare che si può fare Pet ogni ora per testare

        if(!dumDumManager.HasPet)
		{
			DumDumPetJS(dumdumID);
			petButton.gameObject.SetActive(false);
			petting = true;
		}
		else
		{
			GetUserInfo();
			manager.OpenAlert("Already pet!"); //Should be handled already?
		}
	}

	private void SendMessageVote()
	{
		if(user.currentVotes.Contains(-1))
		{
			string vote = "[";

			for (int i = 0; i < voteHandlers.Count; i++)
			{
				vote += (voteHandlers[i].actualIndex + 1).ToString() + ",";
				voteHandlers[i].ToggleButtonsInteractables(false);
			}

			vote = vote.Substring(0, vote.Length - 1) + "]";

			Debug.Log(vote);
			DumDumVoteJS(dumdumID, vote);

			voteButton.gameObject.SetActive(false);
		}
		else
		{
			GetUserInfo();
			manager.OpenAlert("Already voted!");
		}
	}

	private void SendMessageChat()
	{
		if(string.IsNullOrEmpty(messageInputField.text))
		{
			manager.OpenAlert("Message is empty!");
		}
		else if(messageInputField.text.Length > 250)
		{
			manager.OpenAlert("Message is too long!");
		}
		else
		{
			string text = messageInputField.text;
			DumDumSendChatJS(dumdumID, text);
			messageInputField.text = "";
		}
	}

	private void SendMessageStake()
	{

	}

	private IEnumerator SendPostRequest(string actionID, Action<bool, string> callback, float delay = 0, string targetProcessID = "", string ownerId = "1234")
	{
		if (delay > 0)
		{
			yield return new WaitForSeconds(delay);
		}

		if (string.IsNullOrEmpty(targetProcessID))
		{
			targetProcessID = dumdumID;
		}

		string url = baseUrl + targetProcessID;
		string jsonBody = baseJsonBody.Replace("GAMEPROCESSID", targetProcessID).Replace("ACTIONID", actionID).Replace("OWNERID", ownerId);

		UnityWebRequest request = new UnityWebRequest(url, "POST");

		byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
		request.uploadHandler = new UploadHandlerRaw(bodyRaw);
		request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
		request.timeout = actualTimeout;
		request.SetRequestHeader("Content-Type", "application/json");

		yield return request.SendWebRequest();

		if (request.result != UnityWebRequest.Result.Success)
		{
			if (request.error.Contains("timeout"))
			{
				actualTimeout = extendedTimeout;
			}
			Debug.LogError("Error: " + request.error);
			callback.Invoke(false, "Error: " + request.error);
		}
		else
		{
			actualTimeout = baseTimeout;

			if (string.IsNullOrEmpty(request.downloadHandler.text))
			{
				Debug.LogError("JSON is null!");
				callback.Invoke(false, "JSON is null!");
			}
			else
			{
				callback.Invoke(true, request.downloadHandler.text);
			}
		}
	}

	private bool IsMobile()
	{
#if UNITY_EDITOR
		return false;
#elif UNITY_WEBGL
		return Application.isMobilePlatform;
#elif UNITY_ANDROID || UNITY_IOS
		return true;
#else
		return false;
#endif
	}
}

﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour {
    public Running Running = null;
	public bool isLocalGame = true;
    public static GameManager Instance = null;
	public bool isGameFinished = false;
    public bool isRoundFinished = false;

    public PlayerController LocalPlayer = null;

    public RoundTimer Timer = null;
    public Scoreboard Scores = null;

    public Dictionary<int, RobotStateMachine> PlayerList
        { get; protected set; }
    public Dictionary<int, RobotStateMachine> AlivePlayerList
        { get; protected set; }

	private bool exitStarted = false;


    void Awake()
    {
        if (GameManager.Instance == null)
        {
            GameManager.Instance = this;
            this.Initialize();
        }
        else if (GameManager.Instance != this)
        {
            Destroy(gameObject);
        }

        if (this.Running != null) return;

        this.Running = this.gameObject.GetComponent<Running>();

        if (this.Running == null)
        {
            Debug.LogError(this.GetType().Name + ": No Running script found!");
        }
    }

    void Start() {

        GameObject TaggedTimer = GameObject.FindGameObjectWithTag("Timer");

        if (TaggedTimer == null) {
            Debug.LogError(this.GetType().Name + ": No Tagged Timer script found!");
        }
        else {
          this.Timer = TaggedTimer.GetComponent<RoundTimer>();
        }

        GameObject TaggedSb = GameObject.FindGameObjectWithTag("Score");

        if (TaggedSb == null) {
            Debug.LogError(this.GetType().Name + ": No Tagged Scoreboard script found!");
        }
        else {
          this.Scores = TaggedSb.GetComponent<Scoreboard>();
        }

		    InvokeRepeating("WaitForPlayersToBeReady", 0f, 0.3f);
    }

	void WaitForPlayersToBeReady()
	{
		if (PlayerList.Count < NetworkGameManager.nbPlayersForThisGame)
			return;

		foreach(KeyValuePair<int,RobotStateMachine> player in this.PlayerList)
		{
			if (!player.Value.PlayerController.isPlayerReady) {
				return;
			}
		}

		Timer.callTimerRPC();
		CancelInvoke ();
	}

    // Game ending, round ending management
    void FixedUpdate() {
        if (Timer != null && !isGameFinished && Timer.hasTimerStarted && Timer.remainingTime == 0f)
        {
            TimeoutEnding();
            isRoundFinished = true;
        }
        else if (isRoundFinished && !isGameFinished)
        {
            isRoundFinished = false;
            StartCoroutine(NextRound());
        }
    	else if (isGameFinished && !exitStarted) {
    		exitStarted = true;
    		Invoke ("LeaveAfterEnding",3.0f);
    	}
    }

    // Calls the right leaving routine
	private void LeaveAfterEnding (){
		if (PhotonNetwork.offlineMode) {
            StartCoroutine(LeaveTo("Launcher"));
			return;
		}
		if (PhotonNetwork.isMasterClient) {
            StartCoroutine(LeaveAfterAll());
		} else {
            StartCoroutine(LeaveTo("Lobby"));
		}
	}

    // Prepare the scene for a new round
    IEnumerator NextRound()
    {
        yield return new WaitForSeconds(5f);
        PhotonNetwork.LoadLevel("Sandbox");
    }

    // Leave to launcher or lobby
    IEnumerator LeaveTo(string level)
    {
        yield return new WaitForSeconds(5f);
        PhotonNetwork.LeaveRoom();
        PhotonNetwork.LoadLevel(level);
    }

    // Master leaves the room after others
    IEnumerator LeaveAfterAll()
    {
        while (true)
        {
            if (PhotonNetwork.room.PlayerCount == 1)
            {
                StartCoroutine(LeaveTo("Lobby"));
                yield break;
            }
        }
    }

    private RobotStateMachine SearchForMaxHealthPlayers() {
		int MaxHP = 0;
		RobotStateMachine Winner = null;
		foreach (KeyValuePair<int,RobotStateMachine> alivePlayer in this.AlivePlayerList) {
			if (alivePlayer.Value.PlayerController.PlayerHealth.Health >= MaxHP) {
				Winner = alivePlayer.Value;
				MaxHP = alivePlayer.Value.PlayerController.PlayerHealth.Health;
			}
		}
		return Winner;
	}

    protected virtual void Initialize() {
        this.AlivePlayerList = new Dictionary<int, RobotStateMachine>();
        this.PlayerList = new Dictionary<int, RobotStateMachine>();
    }

    public virtual void RemovePlayerFromGame(int playerID) {
        RobotStateMachine robotStateMachine = null;

        try {
            robotStateMachine = this.AlivePlayerList[playerID];
        } catch (KeyNotFoundException exception) {
            Debug.LogWarning(
                "RemovePlayerFromGame: key " + playerID + " was not found");
            Debug.LogWarning(exception.Message);
        }

        if (robotStateMachine != null) {
            this.AlivePlayerList.Remove(playerID);
        }

        robotStateMachine = this.PlayerList[playerID];

        if (robotStateMachine == null) return;

        this.PlayerList.Remove(playerID);
    }

    public virtual void AddPlayerToGame(PlayerController playerAvatar) {
		if (!playerAvatar.photonView.isMine)
			isLocalGame = false;

        this.AlivePlayerList.Add(
            playerAvatar.ID,
            playerAvatar.RobotStateMachine
        );

        this.PlayerList.Add(
            playerAvatar.ID,
            playerAvatar.RobotStateMachine
        );
    }

    public virtual void UpdateDeadListToOthers(
        PlayerController playerController) {
        this.UpdateDeadList(playerController.ID);

        playerController.UpdateDeadToOthers();

        RobotStateMachine robotStateMachine =
             playerController.RobotStateMachine;

        if (robotStateMachine == null) return;

        robotStateMachine.SetState(new RobotDefeatState());
    }

    public virtual void UpdateDeadList(int playerID) {
		try {
			RobotStateMachine robotStateMachine = null;

			robotStateMachine = this.AlivePlayerList [playerID];

			if (robotStateMachine == null)
				return;

			this.AlivePlayerList.Remove (playerID);

			if (!this.IsLastTeamStanding())
				return;

			if (this.AlivePlayerList.Count <= 0)
				return;

			foreach (KeyValuePair<int,RobotStateMachine> winner in GameManager.Instance.AlivePlayerList) {
				if (winner.Value != null && winner.Value.PlayerController.photonView.isMine)
					winner.Value.SetState (new RobotVictoryState ());
			}
            isRoundFinished = true;
			//isGameFinished = true;

		} catch (KeyNotFoundException exception) {
			Debug.LogWarning (
				"UpdateDeadList: key " + playerID + " was not found");
			Debug.LogWarning (exception.Message);
		}
	}

    // If only only team left, declare the win
    public virtual bool IsLastTeamStanding() {
  	    string teamFound = null;
  	    foreach(KeyValuePair<int,RobotStateMachine> pair in GameManager.Instance.AlivePlayerList){
  		    if (pair.Value.PlayerController.Team != teamFound) {
  			    if (teamFound == null)
  				    teamFound = pair.Value.PlayerController.Team;
  			    else
  				    return false; // there are at least 2 different teams;
  		    }
  	    }
        ManageEndRound(teamFound);
        return true;
    }

    // Time off ! Get the best team an declare the win - TODO : Get team health points and not single player health points
    protected void TimeoutEnding()
    {
        RobotStateMachine Winner = null;
        Winner = SearchForMaxHealthPlayers();
        if (Winner.PlayerController.photonView.isMine)
            Winner.SetState(new RobotVictoryState());
        ManageEndRound(Winner.PlayerController.Team);
    }

    private void ManageEndRound(string victoriousTeam)
    {
        // If no two different teams are found
        if (Timer.Countdown != null)
        {
            Timer.Countdown.ManageKoSprite();
            Timer.photonView.RPC("ClientDisplayKo", PhotonTargets.AllViaServer);
        }
        Scores.AddVictory(victoriousTeam);
        if (Scores.CheckForGameVictory())
        {
            isGameFinished = true;
        }
    }
}

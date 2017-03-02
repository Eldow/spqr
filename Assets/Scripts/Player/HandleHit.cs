﻿using UnityEngine;

public class HandleHit : Photon.MonoBehaviour {
    protected PlayerController PlayerController = null;

    void Start() {
        this.PlayerController 
            = this.transform.root.GetComponent<PlayerController>();
    }

    void OnCollisionEnter(Collision other) {
        if (!this.CheckIfValid(other)) return;

        this.HandleOpponent(other);
        this.HandlePlayer(other);
    }

    protected virtual bool CheckIfValid(Collision other) {
        return this.photonView.isMine && 
            this.transform.root.CompareTag(PlayerController.Player);
    }

    protected virtual void HandleOpponent(Collision other) {
        if (!other.transform.root.CompareTag(PlayerController.Opponent)) {
            return;
        }

        if (!(this.PlayerController.RobotStateMachine.CurrentState is 
            RobotAttackState)) {
            return;
        }

        RobotAttackState robotAttackState = 
            (RobotAttackState)this.PlayerController.RobotStateMachine
            .CurrentState;

        if (!HandleHit.IsAttackActive(robotAttackState)) return;

        int opponentID = -1;

        if ((opponentID = this.GetOpponentID(other)) == -1) {
            return;
        }

        this.photonView.RPC("GetHit", PhotonTargets.AllViaServer, 
            robotAttackState.Damage, robotAttackState.Hitstun,
            opponentID);
    }

    public static bool IsAttackActive(RobotAttackState robotAttackState) {
        return robotAttackState.CurrentFrame >= 
            robotAttackState.MinActiveState && 
            robotAttackState.CurrentFrame <= 
            robotAttackState.MaxActiveState;
    }

    protected virtual void HandlePlayer(Collision other) {
        if (!other.transform.root.CompareTag(PlayerController.Player)) return;
    }

    protected virtual void SendHitstun(PlayerController who, int hitstun) {
        who.RobotStateMachine.SetState(new RobotHitstunState(hitstun));
    }

    protected virtual int GetOpponentID(Collision other) {
        PlayerController opponentController = 
            other.transform.root.GetComponent<PlayerController>();

        if (opponentController == null) {
            return -1;
        }

        return opponentController.ID;
    }

    [PunRPC]
    public void GetHit(int damage, int hitstun, int playerID) {
        /* Used once per client, so we need to send the hit to the right 
         * Robot! */
        PlayerController who = 
            GameManager.Instance.PlayerList[playerID].PlayerController;

        who.PlayerHealth.Health -= damage*100;
        this.SendHitstun(who, hitstun);
    }
}
﻿using System;
using UnityEngine;

public class RobotLoseState : RobotState {
    public override State HandleInput(StateMachine stateMachine) {
        return null;
    }

    public override void Update(StateMachine stateMachine) {

    }

    public override void Enter(StateMachine stateMachine) {
        if (!(stateMachine is RobotStateMachine)) return;

        RobotStateMachine robotStateMachine =
            ((RobotStateMachine) stateMachine);

        /* to improve by putting all children in one GameObject, to be 
         * destroyed after.
         */
        Transform robot = robotStateMachine.PlayerController.transform;

        foreach (Transform child in robot) {
            GameObject.Destroy(child.gameObject);
        }

        try {
            GameObject robotRemains =
                GameObject.Instantiate(
                        Resources.Load("Robot/RobotRemains", typeof(GameObject)))
                    as GameObject;

            robotRemains.transform.parent = robotStateMachine.transform;
        } catch (ArgumentException argumentException) {
            Debug.LogError(argumentException.Message);
            Debug.LogError("Failed to load Robot/RobotRemains!");
        } catch (Exception exception) {
            Debug.LogError(exception.Message);
        }

        Debug.Log("LOSE ENTER!");
    }

    public override void Exit(StateMachine stateMachine) {
        Debug.Log("LOSE EXIT!");
    }
}

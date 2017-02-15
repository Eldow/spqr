﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotWalkState : RobotState {

    public override RobotState HandleInput(
    RobotStateMachine stateMachine, XboxInput xboxInput) {

        if (Input.GetKeyDown( xboxInput.A )) {
            return new RobotAttackState();
        }

        if (Input.GetKeyDown( xboxInput.B )) {
            return new RobotBlockState();
        }

        if (Mathf.Abs( xboxInput.getLeftStickX() ) <= 0.2f &&
            Mathf.Abs( xboxInput.getLeftStickY() ) <= 0.2f) {
            return new RobotIdleState();
        } else {
            if (xboxInput.RT()) {
                return new RobotRunState();
            }
            else return null;
        }
    }

    public override void Update(RobotStateMachine stateMachine) {
        stateMachine.PlayerController.UnlockedMovement();
    }

    public override void Enter(RobotStateMachine stateMachine) {
        Debug.Log("WALK ENTER!");
        stateMachine.Animator.SetBool("IsWalk", true);
    }

    public override void Exit(RobotStateMachine stateMachine) {
        Debug.Log("WALK EXIT!");
        stateMachine.Animator.SetBool("IsWalk", false);
    }
}

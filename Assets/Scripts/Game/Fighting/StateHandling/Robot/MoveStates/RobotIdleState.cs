﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RobotIdleState : RobotState {

    public override RobotState HandleInput(
    RobotStateMachine stateMachine, XboxInput xboxInput) {

        if (!stateMachine.Animator.GetCurrentAnimatorStateInfo(0)
            .IsName("RobotIdle")) {
            return null;
        }

        if (Input.GetKeyDown( xboxInput.A )) {
            return new RobotAttackState();
        }

        if (Input.GetKeyDown( xboxInput.B )) {
            return new RobotBlockState();
        }

        if (Mathf.Abs( xboxInput.getLeftStickX() ) <= 0.2f &&
            Mathf.Abs( xboxInput.getLeftStickY() ) <= 0.2f) {
            return null;
        } else {
            if (xboxInput.RT()) {
                return new RobotRunState();
            }
            else return new RobotWalkState();
        }
    }

    public override void Update(RobotStateMachine stateMachine) {

    }

    public override void Enter(RobotStateMachine stateMachine) {
        Debug.Log("IDLE ENTER!");
        stateMachine.Animator.SetBool("IsWalk", false);
    }

    public override void Exit(RobotStateMachine stateMachine) {
        Debug.Log("IDLE EXIT!");
    }
}

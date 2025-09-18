using UnityEngine;

namespace Core
{
    public class JumpState : PlayerBaseState
    {
        public JumpState(PlayerController player, PlayerAnimation playerAnimator) : base(player, playerAnimator)
        {
        }


        public override void OnEnter()
        {
            if (Player.InGroundedState)
            {
                Player.AddVelocity(new Vector3(0, Player.AntiBump, 0));
            }

            Player.InGroundedState = false;
            Player.SetJumpedLastFrame(false);

            PlayerAnimation.SetMoveBlendValue(Player.RunMaxBlendValue);
            PlayerAnimation.IsGrounded = false;
            PlayerAnimation.IsJumping = true;
        }

        public override void OnExit()
        {
            PlayerAnimation.IsJumping = false;
        }
    }
}
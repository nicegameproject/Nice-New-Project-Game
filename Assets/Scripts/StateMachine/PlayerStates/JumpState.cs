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

            PlayerAnimation.IsJumping = true;
            Player.SetGroundedState(false);
            Player.SetJumpedLastFrame(false);
        }

        public override void OnExit()
        {
            PlayerAnimation.IsJumping = false;
        }
    }
}
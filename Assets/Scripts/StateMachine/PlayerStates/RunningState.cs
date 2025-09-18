namespace Core
{
    public class WalkingState : PlayerBaseState
    {
        public WalkingState(PlayerController player, PlayerAnimation playerAnimator) : base(player, playerAnimator){ }

        public override void OnEnter()
        {
            Player.InGroundedState = true;

            PlayerAnimation.IsGrounded = true;
        }

    }
}
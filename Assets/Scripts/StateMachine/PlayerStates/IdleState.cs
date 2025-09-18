namespace Core
{
    public class IdleState : PlayerBaseState
    {
        public IdleState(PlayerController player, PlayerAnimation playerAnimator) : base(player, playerAnimator)
        {
        }


        public override void OnEnter()
        {
            Player.InGroundedState = true;

            PlayerAnimation.IsGrounded = true;
        }

    }
}
namespace Core
{
    public class RunningState : PlayerBaseState
    {
        public RunningState(PlayerController player, PlayerAnimation playerAnimator) : base(player, playerAnimator){ }

        public override void OnEnter()
        {
            Player.InGroundedState = true;
            Player.SetSpeed(Player.RunSpeed);
            Player.SetAcceleration(Player.RunAcceleration);
            
            PlayerAnimation.SetMoveBlendValue(Player.RunMaxBlendValue);
            PlayerAnimation.IsGrounded = true;
        }

    }
}
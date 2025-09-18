namespace Core
{
    public class SprintState : PlayerBaseState
    {
        public SprintState(PlayerController player, PlayerAnimation playerAnimator) : base(player, playerAnimator)
        {
        }

        public override void OnEnter()
        {
            Player.InGroundedState = true;
            Player.SetSpeed(Player.SprintSpeed);
            Player.SetAcceleration(Player.SprintAcceleration);
            
            PlayerAnimation.SetMoveBlendValue(Player.SprintMaxBlendValue);
            PlayerAnimation.IsGrounded = true;
        }

        public override void OnExit()
        {
            Player.SetSpeed(Player.RunSpeed);
            Player.SetAcceleration(Player.RunAcceleration);
        }
    }
}
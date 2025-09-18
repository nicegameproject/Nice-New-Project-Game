namespace Core
{
    public class CrouchState : PlayerBaseState
    {
        public CrouchState(PlayerController player, PlayerAnimation playerAnimator) : base(player, playerAnimator)
        {
        }


        public override void OnEnter()
        {
            Player.InGroundedState = true;
            Player.IsCrouching = true;
            Player.SetSpeed(Player.WalkSpeed);
            Player.SetAcceleration(Player.WalkAcceleration);
            
            PlayerAnimation.SetMoveBlendValue(Player.WalkMaxBlendValue);
            PlayerAnimation.IsGrounded = true;
            PlayerAnimation.IsCrouching = true;
        }

        public override void OnExit()
        {
            Player.IsCrouching = false;
            Player.SetSpeed(Player.RunSpeed);
            Player.SetAcceleration(Player.RunAcceleration);
            
            PlayerAnimation.IsGrounded = Player.InGroundedState;
            PlayerAnimation.IsCrouching = false;
        }
    }
}
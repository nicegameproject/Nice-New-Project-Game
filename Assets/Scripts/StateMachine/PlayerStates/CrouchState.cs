namespace Core
{
    public class CrouchState : PlayerBaseState
    {
        public CrouchState(PlayerController player, PlayerAnimation playerAnimator) : base(player, playerAnimator)
        {
        }


        public override void OnEnter()
        {
            Player.SetGroundedState(true);
            Player.IsTryingToCrouch = true;
            PlayerAnimation.IsCrouching = true;
        }

        public override void OnExit()
        {
            Player.IsTryingToCrouch = false;
            PlayerAnimation.IsCrouching = false;
        }
    }
}
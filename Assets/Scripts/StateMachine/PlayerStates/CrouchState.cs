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
        }
    }
}
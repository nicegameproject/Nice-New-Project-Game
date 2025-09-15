using Vector3 = UnityEngine.Vector3;

namespace Core
{
    public class FallingState : PlayerBaseState
    {
        public FallingState(PlayerController player, PlayerAnimation playerAnimator) : base(player, playerAnimator)
        {
        }
        
        public override void OnEnter()
        {
            if (Player.InGroundedState)
            {
                Player.AddVelocity(new Vector3(0, Player.AntiBump, 0));
            }

            PlayerAnimation.IsFalling = true;
            Player.SetGroundedState(false);
            Player.SetJumpedLastFrame(false);
        }

        public override void OnExit()
        {
            PlayerAnimation.IsFalling = false;
        }
    }
}
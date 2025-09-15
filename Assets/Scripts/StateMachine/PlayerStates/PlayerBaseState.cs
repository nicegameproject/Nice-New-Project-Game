namespace Core
{
    public abstract class PlayerBaseState : BaseState
    {
        protected PlayerController Player { get; private set; }
        protected PlayerAnimation PlayerAnimation { get; private set; }

        protected PlayerBaseState(PlayerController player, PlayerAnimation playerAnimator)
        {
            this.Player = player;
            this.PlayerAnimation = playerAnimator;
        }
    }
}
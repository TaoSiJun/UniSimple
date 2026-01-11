namespace UniSimple.FSM
{
    public abstract class StateBase : IState
    {
        protected StateMachine Machine;

        public virtual void OnCreate(StateMachine machine)
        {
            Machine = machine;
        }

        public virtual void OnEnter(object args)
        {
        }

        public virtual void OnPause()
        {
        }

        public virtual void OnResume()
        {
        }

        public virtual void OnExit()
        {
        }

        public virtual void OnUpdate()
        {
        }
    }
}
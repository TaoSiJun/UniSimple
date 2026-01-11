namespace UniSimple.FSM
{
    public interface IState
    {
        void OnCreate(StateMachine machine);

        void OnEnter(object args = null);

        void OnUpdate();

        void OnExit();

        void OnResume();

        void OnPause();
    }
}
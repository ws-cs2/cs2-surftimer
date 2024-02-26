namespace WST;

public class OnOneSecondEvent
{
}

public class PlaytimeSystem : System
{
    public PlaytimeSystem(EventManager eventManager, EntityManager entityManager, Database database)
        : base(eventManager, entityManager, database)
    {
        EventManager.Subscribe<OnOneSecondEvent>(OnEachSecond);
    }

    public void OnEachSecond(OnOneSecondEvent e)
    {
        
    }
    
}
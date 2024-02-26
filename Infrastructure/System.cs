namespace WST;

public abstract class System
{
    protected System(EventManager eventManager, EntityManager entityManager, Database database)
    {
        EventManager = eventManager;
        EntityManager = entityManager;
        Database = database;
    }

    protected EventManager EventManager { get; }
    protected EntityManager EntityManager { get; }
    protected Database Database { get; }
}
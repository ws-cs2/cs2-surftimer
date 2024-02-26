namespace WST;

public class EntityManager
{
    private readonly List<Entity> _entities = new();
    private readonly List<System> _systems = new();

    public void AddEntity(Entity entity)
    {
        var existingEntity = _entities.Find(e => e.Id == entity.Id);
        if (existingEntity != null)
        {
            Console.WriteLine($"Entity with id {entity.Id} already exists, replacing");
            _entities.Remove(existingEntity);
        }

        _entities.Add(entity);
        Console.WriteLine("Added entity id: " + entity.Id);
        PrintEntityIdsOnOneLine();
    }

    // For Singleton entities like the map
    public T FindEntity<T>() where T : Entity
    {
        var entity = _entities.Find(s => s is T);
        return (T)entity;
    }

    public T FindEntity<T>(string id) where T : Entity
    {
        var e = _entities.Find(entity => entity is T && entity.Id == id);
        return (T)e;
    }

    public T FindEntityOrThrow<T>(string id) where T : Entity
    {
        var e = FindEntity<T>(id);
        if (e == null) throw new Exception($"Entity of type {typeof(T).Name} with id {id} not found");
        return e;
    }

    public List<T> Entities<T>() where T : Entity
    {
        return _entities.OfType<T>().ToList();
    }


    public void RemoveEntity<T>() where T : Entity
    {
        _entities.RemoveAll(entity => entity is T);
        Console.WriteLine("Removed all entities of type " + typeof(T).Name);
        PrintEntityIdsOnOneLine();
    }

    public void RemoveEntity<T>(string id) where T : Entity
    {

        _entities.RemoveAll(entity => entity is T && entity.Id == id);
        Console.WriteLine("Removed entity id: " + id);
        PrintEntityIdsOnOneLine();
    }

    public void AddSystem(System system)
    {
        _systems.Add(system);
    }

    private void PrintEntityIdsOnOneLine()
    {
        Console.WriteLine("Entities: " + string.Join(", ", _entities.Select(e => e.Id)));
    }
    
    public List<string> DebugEntities()
    {
        var result = new List<string>();
        Console.WriteLine("Entities:");
        foreach (var entity in _entities)
        {
            var entityName = entity.GetType().Name;
            var components = string.Join(", ", entity.Components.Select(c => c.Key.Name));
            Console.WriteLine($"  {entityName} ({entity.Id}) [{components}]");

            result.Add($"{entityName} ({entity.Id}) [{components}]");

        }

        return result;
    }
}
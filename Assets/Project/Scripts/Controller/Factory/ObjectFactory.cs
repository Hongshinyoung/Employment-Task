using UnityEngine;

public class ObjectFactory : IObjectFactory
{
    public GameObject Instantiate(GameObject prefab, Transform parent)
    {
        return Object.Instantiate(prefab, parent);
    }
}

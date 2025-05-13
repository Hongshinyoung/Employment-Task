using UnityEngine;

public interface IObjectFactory
{
    GameObject Instantiate(GameObject prefab, Transform parent);
}
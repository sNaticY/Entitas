using Entitas;
using Entitas.Unity;
using UnityEngine;

public class EntityLinkController : MonoBehaviour
{
    void Start()
    {
        var gameContext = SampleContexts.Create().GetGame();
        gameContext.CreateContextObserver();
        var entity = gameContext.CreateEntity();

        var go = new GameObject();
        go.Link(entity);

        entity.AddMyGameObject(go);

//        go.Unlink();

        Destroy(go);
    }
}

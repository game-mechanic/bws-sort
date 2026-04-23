using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class ParticlePool
{
    #region Reveal FX Pool
    static ParticleSystem RevealFx => GameSettings.Instance.BubbleFXPrefab;
    private static ObjectPool<ParticleSystem> revealFxPool;
    private static Transform revealFxParent;

    public static ObjectPool<ParticleSystem> RevealFxPool { get => revealFxPool; }
    #endregion

    public static void Init()
    {
        #region FX Pools Initialization

        // Reveal FX pool
        if (RevealFxPool == null && RevealFx != null)
        {
            revealFxPool = new ObjectPool<ParticleSystem>(
                createFunc: () =>
                {
                    var ps = GameObject.Instantiate(RevealFx, revealFxParent);
                    ps.gameObject.SetActive(false);
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    return ps;
                },
                actionOnGet: ps =>
                {
                    ps.gameObject.SetActive(true);
                    ps.Clear(true);
                },
                actionOnRelease: ps =>
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    ps.gameObject.SetActive(false);
                },
                actionOnDestroy: ps => GameObject.Destroy(ps.gameObject),
                collectionCheck: false,
                defaultCapacity: 8,
                maxSize: 64
            );
        }
        #endregion
    }
    
    public static void PlayRevealFx(Vector3 midPoint)
    {
        if (!InputHandler.Instance)
            return;
        var fx = revealFxPool.Get();
        fx.transform.position = midPoint;
        fx.Play();
        InputHandler.Instance.StartCoroutine(ReleaseSpoolFxWhenDone(fx, revealFxPool));
    }
    private static IEnumerator ReleaseSpoolFxWhenDone(ParticleSystem ps, ObjectPool<ParticleSystem> pool)
    {
        // Ensure at least one frame passes so IsAlive updates correctly
        yield return null;
        yield return new WaitUntil(() => !ps.IsAlive(true));
        pool?.Release(ps);
    }
}
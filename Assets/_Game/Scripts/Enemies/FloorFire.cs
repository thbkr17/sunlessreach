using UnityEngine;
using System.Collections;
using SunlessReach.Player;
using SunlessReach.Core;

namespace SunlessReach.Enemies
{
    // A patch of floor fire: harmless and small at first so you can step clear, then it grows, deals damage, and burns out.
    public class FloorFire : MonoBehaviour
    {
        private const float SmallFraction = 0.3f;

        private float _growDelay = 1f;       // harmless window
        private float _growTime = 0.35f;     // small -> full
        private float _burnDuration = 3f;    // how long it stays dangerous
        private int _damage = 1;
        private Vector3 _fullScale = new Vector3(2f, 1.8f, 1.5f);

        private BoxCollider _col;
        private GameObject _visual;
        private AudioSource _loop;
        private AudioClip _loopClip;
        private float _loopVolume = 0.45f;
        private bool _dangerous;

        public static FloorFire Spawn(GameObject visualPrefab, Vector3 position, Vector3 fullScale, int damage,
                                      float growDelay = 1f, float burnDuration = 3f,
                                      AudioClip loopClip = null, float loopVolume = 0.45f)
        {
            var go = new GameObject("FloorFire");
            go.transform.position = position;
            int layer = LayerMask.NameToLayer("EnemyAttack");
            if (layer >= 0) go.layer = layer;

            var ff = go.AddComponent<FloorFire>();
            ff._fullScale = fullScale;
            ff._damage = damage;
            ff._growDelay = growDelay;
            ff._burnDuration = burnDuration;
            ff._loopClip = loopClip;
            ff._loopVolume = loopVolume;

            if (visualPrefab != null)
            {
                ff._visual = Instantiate(visualPrefab, go.transform);
                ff._visual.transform.localPosition = Vector3.zero;
                ff._visual.transform.localRotation = Quaternion.identity;
            }
            return ff;
        }

        private void Awake()
        {
            _col = gameObject.AddComponent<BoxCollider>();
            _col.isTrigger = true;
            _col.enabled = false;
        }

        private void Start()
        {
            transform.localScale = _fullScale * SmallFraction;

            if (_loopClip != null)
            {
                _loop = gameObject.AddComponent<AudioSource>();
                _loop.clip = _loopClip;
                _loop.loop = true;
                _loop.playOnAwake = false;
                _loop.spatialBlend = 0f;
                _loop.volume = _loopVolume * AudioPrefs.SfxVolume;
                _loop.Play();
            }

            StartCoroutine(Sequence());
        }

        private IEnumerator Sequence()
        {
            yield return new WaitForSeconds(_growDelay);

            Vector3 from = _fullScale * SmallFraction;
            float t = 0f;
            while (t < _growTime)
            {
                t += Time.deltaTime;
                transform.localScale = Vector3.Lerp(from, _fullScale, t / _growTime);
                yield return null;
            }
            transform.localScale = _fullScale;
            _col.enabled = true;
            _dangerous = true;

            yield return new WaitForSeconds(_burnDuration);

            if (_visual != null)
                foreach (var ps in _visual.GetComponentsInChildren<ParticleSystem>(true))
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            _col.enabled = false;
            _dangerous = false;
            if (_loop != null) StartCoroutine(FadeOutLoop());
            Destroy(gameObject, 1.2f);
        }

        private IEnumerator FadeOutLoop()
        {
            float start = _loop.volume, t = 0f;
            while (t < 1f && _loop != null)
            {
                t += Time.deltaTime / 1f;
                _loop.volume = Mathf.Lerp(start, 0f, t);
                yield return null;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            if (!_dangerous || !other.CompareTag("Player")) return;
            var hp = other.GetComponent<PlayerHealth>();
            if (hp == null) hp = other.GetComponentInParent<PlayerHealth>();
            if (hp == null) return;
            Vector3 dir = other.transform.position - transform.position;
            dir.z = 0f;
            dir.y = Mathf.Abs(dir.y) + 0.4f;
            hp.TakeDamage(_damage, dir.normalized, 5f);
        }
    }
}

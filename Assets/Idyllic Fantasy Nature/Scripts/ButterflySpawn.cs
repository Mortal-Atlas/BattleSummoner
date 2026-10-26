using UnityEngine;

namespace IdyllicFantasyNature
{
    public class ButterflySpawn : MonoBehaviour
    {
        [Tooltip("child with the mesh renderer to make the butterfly invisible when the animation is not running")]
        [SerializeField] private GameObject _butterflyChild;
        // the spawn area script to get the data from the set range and cooldown
        private ButterflySpawnArea _area;
        // the cooldown to respawn the butterfly
        private float _cooldown = 0;
        // indicates if the animation is playing to stop the cooldown for the respawn
        private bool _isPlaying = false;
        // animator of the butterfly
        private Animator _animator;
        // anchor position when not bound to an area
        private Vector3 _initialPosition;

        // Start is called before the first frame update
        void Start()
        {
            _initialPosition = transform.position;
            _animator = GetComponent<Animator>();
            _area = GetComponentInParent<ButterflySpawnArea>();

            // sets the animator off to not play the animation before the cooldown reaches 0
            if (_animator != null)
            {
                _animator.enabled = false;
            }

            // get the set cooldown safely
            if (_area != null)
            {
                _cooldown = Random.Range(_area.MinCooldown, _area.MaxCooldown);
            }
            else
            {
                _cooldown = Random.Range(2f, 8f);
            }

            // makes the butterfly invisible until the animation starts playing
            if (_butterflyChild != null)
            {
                _butterflyChild.SetActive(false);
            }
            else if (transform.childCount > 0)
            {
                _butterflyChild = transform.GetChild(0).gameObject;
                _butterflyChild.SetActive(false);
            }
        }

        // Update is called once per frame
        void Update()
        {
            ActivateButterfly();
        }

        /// <summary>
        /// activates the butterfly when the cooldown reaches 0 and the animation isn't already playing
        /// </summary>
        private void ActivateButterfly()
        {
            if (!_isPlaying)
            {
                if (_cooldown <= 0)
                {
                    // activates the animator to play the animation
                    if (_animator != null)
                    {
                        _animator.enabled = true;
                    }

                    // resets the cooldown for the respawn
                    if (_area != null)
                    {
                        _cooldown = Random.Range(_area.MinCooldown, _area.MaxCooldown);
                    }
                    else
                    {
                        _cooldown = Random.Range(3f, 10f);
                    }

                    // makes the butterfly visible
                    if (_butterflyChild != null)
                    {
                        _butterflyChild.SetActive(true);
                    }

                    // determines a random position for the butterfly in the spawn area
                    if (_area != null && _area.Collider != null)
                    {
                        transform.position = new Vector3(
                            Random.Range(_area.Collider.bounds.min.x, _area.Collider.bounds.max.x),
                            Random.Range(_area.Collider.bounds.min.y, _area.Collider.bounds.max.y),
                            Random.Range(_area.Collider.bounds.min.z, _area.Collider.bounds.max.z));
                    }
                    else
                    {
                        Vector2 circle = Random.insideUnitCircle * 5.0f;
                        transform.position = _initialPosition + new Vector3(circle.x, Random.Range(0.2f, 2.0f), circle.y);
                    }

                    _isPlaying = true;
                }
                else
                {
                    _cooldown -= Time.deltaTime;
                }
            }
        }

        /// <summary>
        /// animation event will be used at the end of the butterfly animation
        /// Resets the stats to enable the cooldown for the respawn
        /// </summary>
        public void AnimationEnded()
        {
            _isPlaying = false;
            if (_animator != null)
            {
                _animator.enabled = false;
            }
            if (_butterflyChild != null)
            {
                _butterflyChild.SetActive(false);
            }
        }
    }
}

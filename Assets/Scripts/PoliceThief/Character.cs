using System;
using UnityEngine;

namespace PoliceThief
{
    public class Character : MonoBehaviour
    {
        private Rigidbody rigidBody;

        [SerializeField] public string identity;

        [HideInInspector] public int lastActionIndex;
        [HideInInspector] private const float Scale = 10.0f;

        [SerializeField] public float speed = 1f;

        private float yPosition;

        // Start is called before the first frame update
        void Awake()
        {
            rigidBody = GetComponent<Rigidbody>();
            rigidBody.useGravity = false;
        }

        private void Start()
        {
            yPosition = transform.position.y;
        }

        /// <summary>
        ///  apply force to direction d, scaled by some scale and character speed
        /// </summary>
        /// <param name="d"></param>
        public void Act(Vector2 d)
        {
            var forceDirection = new Vector3(d[0], 0, d[1]);
            rigidBody.AddForce(Scale * speed * forceDirection);
        }

        private void FixedUpdate()
        {
            var newPosition = transform.position;
            newPosition.x = newPosition.x > Config.LandSize
                ? newPosition.x - Config.LandSize
                : newPosition.x < 0
                    ? newPosition.x + Config.LandSize
                    : newPosition.x;

            newPosition.z = newPosition.z > Config.LandSize
                ? newPosition.z - Config.LandSize
                : newPosition.z < 0
                    ? newPosition.z + Config.LandSize
                    : newPosition.z;
            newPosition.y = yPosition;
            transform.position = newPosition;
        }

        public void RandomResetPosition()
        {
            transform.rotation = Quaternion.identity;
            transform.position = new Vector3(UnityEngine.Random.Range(0, Config.LandSize), yPosition,
                UnityEngine.Random.Range(0, Config.LandSize));
            rigidBody.velocity = Vector3.zero;
            rigidBody.angularVelocity = Vector3.zero;
        }
    }
}
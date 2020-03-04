using MLAgents;
using UnityEngine;

namespace PoliceThief
{
    public class Police : Agent
    {
        private World world;

        private Rigidbody rigidBody;

        private Character character;
        private void Awake()
        {
            character = GetComponent<Character>();
            rigidBody = GetComponent<Rigidbody>();
            world = transform.parent.GetComponent<World>();
        }

        public override void InitializeAgent()
        {

        }

        public override void CollectObservations()
        {
            // todo: add vision cone (partial observation)
//            foreach(Pedestrian pedestrian in world.pedestrians)
//            {
//                var pedestrianPosition = pedestrian.gameObject.transform.position;
//                var pedestrianVelocity = pedestrian.gameObject.rigidBody.velocity;
//                AddVectorObs(pedestrianPosition.x / Config.LandSize);
//                AddVectorObs(pedestrianPosition.z / Config.LandSize);
//                AddVectorObs(pedestrianVelocity.x);
//                AddVectorObs(pedestrianVelocity.z);
//            }
//
//            var myPosition = transform.position;
//            var myVelocity = rigidBody.position;
//            AddVectorObs(myPosition.x / Config.LandSize);
//            AddVectorObs(myPosition.z / Config.LandSize);
//            AddVectorObs(myVelocity.x);
//            AddVectorObs(myVelocity.z);
//
//            foreach (var thiefAgent in world.thieves)
//            {
//                AddVectorObs(thiefAgent.gameObject.transform.position.x / Config.LandSize);
//                AddVectorObs(thiefAgent.gameObject.transform.position.z / Config.LandSize);
//                AddVectorObs(thiefAgent.rigidBody.velocity.x);
//                AddVectorObs(thiefAgent.rigidBody.velocity.z);
//            }

        }

        public override float[] Heuristic()
        {
            var action = new float[2];
            action[0] = Input.GetAxis("Horizontal");
            action[1] = Input.GetAxis("Vertical");
            return action;
        }

        public override void AgentAction(float[] vectorAction)
        {
            character.Act(new Vector2(vectorAction[0], vectorAction[1]));
        }

        public override void AgentReset()
        {
            character.RandomResetPosition();;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Agent")) return;
            if (collision.transform.GetComponent<Character>().identity != "Thief") return;
            Debug.Log("police caught thief");
            Done();
        }
    }
}

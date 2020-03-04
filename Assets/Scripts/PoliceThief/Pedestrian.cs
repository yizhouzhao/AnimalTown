using System;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

namespace PoliceThief
{
    public class Pedestrian : MonoBehaviour
    {
        private Character character;
        private List<Vector2> actions = new List<Vector2>();

        private void Awake()
        {
            actions.Add(new Vector2(1, 0));
            actions.Add(new Vector2(0, 1));
            actions.Add(new Vector2(-1, 0));
            actions.Add(new Vector2(0, -1));
            character = GetComponent<Character>();
        }


        public void Act(int i)
        {
            character.Act(actions[i]);
        }

        // Start is called before the first frame update
        void Start()
        {
            character.lastActionIndex = UnityEngine.Random.Range(0, 5);
        }

        void FixedUpdate()
        {
//            float prob = UnityEngine.Random.Range(0f, 1f);
//            int offset = (prob < 0.3f) ? -1 : (prob < 0.7f) ? 0 : 1;
//            int i = (character.lastActionIndex + offset) % (actions.Count );
//
//            if (character.lastActionIndex < 0)
//            {
//                character.lastActionIndex = character.lastActionIndex + (actions.Count);
//            }
//            Act(character.lastActionIndex);

            Act(UnityEngine.Random.Range(0, 4));
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace PoliceThief
{
    public class World : MonoBehaviour
    {
        [HideInInspector]
        public List<Pedestrian> pedestrians = new List<Pedestrian>();

        [HideInInspector]
        public List<Police> polices = new List<Police>();

        [HideInInspector]
        public List<Thief> thieves = new List<Thief>();

        [HideInInspector]
        private int worldAgentNum;

        public MonoBehaviour GetAgent(int idx)
        {
            Assert.IsTrue(idx >= 0 && idx < pedestrians.Count + polices.Count + thieves.Count);
            if (idx < pedestrians.Count)
                return pedestrians[idx];
            else if (idx < (polices.Count + pedestrians.Count))
                return polices[idx - pedestrians.Count];

            return thieves[idx - polices.Count - pedestrians.Count];
        }

        // Start is called before the first frame update
        void Awake()
        {
            pedestrians = GetComponentsInChildren<Pedestrian>().ToList();
            polices = GetComponentsInChildren<Police>().ToList();
            thieves = GetComponentsInChildren<Thief>().ToList();
            worldAgentNum = pedestrians.Count + polices.Count + thieves.Count;
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void FixedUpdate()
        {

        }
    }
}
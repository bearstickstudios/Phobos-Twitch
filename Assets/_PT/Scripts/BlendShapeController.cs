using System.Collections;
using UnityEngine;

namespace PhobosTwitch
{
    public class BlendShapeController : MonoBehaviour
    {
        public SkinnedMeshRenderer smr;
    
        [Header("Blendshape Mapping")]
        public string visorName = "visor";
        public string antennaName = "antenna";
    
        private int visorIndex;
        private int antennaIndex;

        void Start()
        {
            visorIndex = smr.sharedMesh.GetBlendShapeIndex(visorName);
            antennaIndex = smr.sharedMesh.GetBlendShapeIndex(antennaName);
        }

        // Formerly EatAnimation
        public IEnumerator ScanAnimation()
        {
            if (visorIndex == -1) yield break;

            // Flip visor up (0 to 100)
            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 6f; // Snappy mechanical movement
                smr.SetBlendShapeWeight(visorIndex, Mathf.Lerp(0, 100, t));
                yield return null;
            }

            // Hold visor open for data extraction
            yield return new WaitForSeconds(0.75f);

            // Flip visor down (100 to 0)
            t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 6f;
                smr.SetBlendShapeWeight(visorIndex, Mathf.Lerp(100, 0, t));
                yield return null;
            }
        }

        // Formerly Ear Wiggling
        public IEnumerator AdjustAntenna()
        {
            if (antennaIndex == -1) yield break;

            // Ping-pong blendshape to simulate tuning a frequency
            float t = 0;
            while (t < 1.5f) // Total adjustment time
            {
                t += Time.deltaTime;
                smr.SetBlendShapeWeight(antennaIndex, Mathf.PingPong(t * 300f, 100f));
                yield return null;
            }
        
            // Reset to default stowed/neutral position
            smr.SetBlendShapeWeight(antennaIndex, 0f);
        }
    }
}
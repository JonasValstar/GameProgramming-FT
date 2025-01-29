using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace Unity.FPS.Game
{
    public class Damageable : MonoBehaviour
    {
        [Tooltip("Multiplier to apply to the total received damage")]
        public float critMultiplier = 1f;

        [Tooltip("Multiplier applied to the individually received damage types")]
        public SerializedDictionary<Elements, float> elementMults;

        [Range(0, 1)] [Tooltip("Multiplier to apply to self damage")]
        public float SensibilityToSelfdamage = 0.5f;

        public Health Health { get; private set; }

        void Awake()
        {
            // find the health component either at the same level, or higher in the hierarchy
            Health = GetComponent<Health>();
            if (!Health)
            {
                Health = GetComponentInParent<Health>();
            }
        }

        public void InflictDamage(Dictionary<Elements, float> damage, float critChance, bool isExplosionDamage, GameObject damageSource)
        {
            if (Health)
            {
                // calculate total damage
                float totalDamage = 0;
                foreach(Elements type in damage.Keys) {
                    totalDamage += damage[type] * (elementMults.ContainsKey(type) ? elementMults[type] : 1);
                }

                // calculate if crit
                if (Random.Range(0, 100f) <= critChance) {
                    totalDamage *= critMultiplier;
                }

                // potentially reduce damages if inflicted by self
                if (Health.gameObject == damageSource)
                {
                    totalDamage *= SensibilityToSelfdamage;
                }

                // apply the damages
                Health.TakeDamage(totalDamage, damageSource);
            }
        }
    }
}
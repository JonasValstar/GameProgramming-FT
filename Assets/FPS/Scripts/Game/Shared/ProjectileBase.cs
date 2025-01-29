using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Game
{
    public abstract class ProjectileBase : MonoBehaviour
    {
        public GameObject Owner { get; private set; }
        public Vector3 InitialPosition { get; private set; }
        public Vector3 InitialDirection { get; private set; }
        public Vector3 InheritedMuzzleVelocity { get; private set; }
        public float InitialCharge { get; private set; }

        public UnityAction OnShoot;

        // damage variables
        public Dictionary<Elements, float> Damage;
        public float critChance;

        // speed variables
        public float Speed;
        public float Acceleration;

        // events
        public Dictionary<FunctionType, List<UnityEvent>> functionEvents;
        public List<UnityEvent> timeEvents;
        public List<float> timeDelays;

        public void Shoot(WeaponController controller)
        {
            Owner = controller.Owner;
            InitialPosition = transform.position;
            InitialDirection = transform.forward;
            InheritedMuzzleVelocity = controller.MuzzleWorldVelocity;
            InitialCharge = controller.CurrentCharge;

            // get the damages
            Damage = controller.modDamage;
            critChance = controller.modStats[StatType.critChance];

            // get the speed data
            Speed = controller.modStats[StatType.bulletVel];
            Acceleration = controller.modStats[StatType.bulletAcc];

            // get the events
            functionEvents = controller.modFunctions;
            foreach(TimerData timer in controller.modTimerData) {
                if (timer.onWeapon) {
                    timeEvents.Add(timer.callEvent);
                    timeDelays.Add(timer.timeDelay);
                }
            }

            OnShoot?.Invoke();
        }
    }
}
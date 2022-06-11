﻿using RoR2;
using UnityEngine;

namespace R2API.ScriptableObjects {
    [CreateAssetMenu(fileName = "New DamageColor", menuName = "R2API/Colors/DamageColor")]
    public class SerializableDamageColor : ScriptableObject {
        public Color color;
        public DamageColorIndex DamageColorIndex { get; internal set; }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using cakeslice;

namespace cakeslice
{
    public class OutlineAnimation : MonoBehaviour
    {
        [SerializeField]
        private float _pulseSpeed = 1f;
        bool pingPong = false;

        private OutlineEffect _outlineEffect;
        // Use this for initialization
        void Start()
        {
            _outlineEffect = GetComponent<OutlineEffect>();
        }

        // Update is called once per frame
        void Update()
        {
            Color c = _outlineEffect.lineColor0;

            if(pingPong)
            {
                c.a += Time.deltaTime * _pulseSpeed;

                if(c.a >= 1)
                    pingPong = false;
            }
            else
            {
                c.a -= Time.deltaTime * _pulseSpeed;

                if(c.a <= 0)
                    pingPong = true;
            }

            c.a = Mathf.Clamp01(c.a);
            _outlineEffect.lineColor0 = c;
            _outlineEffect.UpdateMaterialsPublicProperties();
        }
    }
}
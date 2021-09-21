using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ambiens.archtoolkit.atvideo.models
{
    [Serializable]
    public class MVideoClip
    {
        [SerializeField] private Vector3AnimationCurve PositionCurve;
        [SerializeField] private Vector3AnimationCurve PivotCurve;
        [SerializeField] private Vector3AnimationCurve ForwardCurve;

        public string ID;
        public string Name="Clip";
        public float Duration = 10;
        public List<MKeyFrame> KeyFrames=new List<MKeyFrame>();

        public MTransition LeftTransition = new MTransition() { isLeftTransition = true };
        public MTransition RightTransition = new MTransition() { isLeftTransition = false };

        public void RefreshCurves()
        {
            this.RefreshForwardCurve();
            this.RefreshPivotCurve();
            this.RefreshPositionCurve();
        }
        private void RefreshPositionCurve()
        {
            PositionCurve = new Vector3AnimationCurve();
            
            for (int i=0; i<this.KeyFrames.Count; i++)
            {
                var frame = this.KeyFrames[i];
                float time = (float)(i)*1f / (float)(this.KeyFrames.Count-1);
                PositionCurve.AddKey(time, frame.CameraPosition);
            }
        }
        private void RefreshForwardCurve()
        {
            ForwardCurve = new Vector3AnimationCurve();

            for (int i = 0; i < this.KeyFrames.Count; i++)
            {
                var frame = this.KeyFrames[i];
                float time = (float)(i ) * 1f / (float)(this.KeyFrames.Count-1);
                ForwardCurve.AddKey(time, frame.CameraForward);
            }
        }
        private void RefreshPivotCurve()
        {
            PivotCurve = new Vector3AnimationCurve();

            for (int i = 0; i < this.KeyFrames.Count; i++)
            {
                var frame = this.KeyFrames[i];
                float time = (float)(i ) * 1f / (float)(this.KeyFrames.Count-1);
                PivotCurve.AddKey(time, frame.CameraPivot);
            }
        }
        public Vector3 GetPosition(float time)
        {
            if (this.PositionCurve == null) this.RefreshPositionCurve();
            return this.PositionCurve.Evaluate(time);
        }
        public Vector3 GetPivot(float time)
        {
            if (this.PivotCurve == null) this.RefreshPivotCurve();
            return this.PivotCurve.Evaluate(time);
        }
        public Vector3 GetForward(float time)
        {
            if (this.ForwardCurve == null) this.RefreshForwardCurve();
            return this.ForwardCurve.Evaluate(time);
        }
    }

    [Serializable]
    public class Vector3AnimationCurve
    {
        [SerializeField] private AnimationCurve m_CurveX;
        [SerializeField] private AnimationCurve m_CurveY;
        [SerializeField] private AnimationCurve m_CurveZ;

        public Vector3AnimationCurve()
        {
            m_CurveX = new AnimationCurve();
            m_CurveY = new AnimationCurve();
            m_CurveZ = new AnimationCurve();
        }

        public void AddKey(float time, Vector3 value)
        {
            m_CurveX.AddKey(time, value.x);
            m_CurveY.AddKey(time, value.y);
            m_CurveZ.AddKey(time, value.z);
        }
        public Vector3 Evaluate(float time)
        {
            return new Vector3(m_CurveX.Evaluate(time), m_CurveY.Evaluate(time), m_CurveZ.Evaluate(time));
        }
    }
}
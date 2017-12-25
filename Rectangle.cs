﻿using Gist.Extensions.RectExt;
using nobnak.Gist;
using nobnak.Gist.Extensions.Behaviour;
using nobnak.Gist.Extensions.ComponentExt;
using nobnak.Gist.Layer2;
using nobnak.Gist.Primitive;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LevelDesign {

    [ExecuteInEditMode]
    public class Rectangle : MonoBehaviour {

        [SerializeField] protected Layer layer;
        [Range(0f, 10f)]
        [SerializeField] protected float borderThickness = 0.1f;
        [SerializeField] protected Rect localSize = new Rect(-0.5f, -0.5f, 1f, 1f);

        protected DefferedMatrix localToLayer = new DefferedMatrix();
        protected Validator validator = new Validator();

        protected Rect layerInside;
        protected Vector2 layerInsideMin;
        protected Vector2 layerInsideMax;
        protected Rect layerOutside;
        protected Vector2 layerOutsideMin;
        protected Vector2 layerOutsideMax;

        protected GLFigure gl;

        #region Unity
        void OnEnable() {
            if (layer == null) {
                enabled = false;
                return;
            }

            gl = new GLFigure();

            validator.Reset();
            validator.Validation += () => {
                layer.LayerValidator.CheckValidation();
                Rebuild();
                transform.hasChanged = false;
            };
            validator.SetCheckers(() => layer.LayerValidator.IsValid && !transform.hasChanged);
            layer.LayerValidator.Invalidated += () => validator.Invalidate();
        }
        void OnRenderObject() {
            if (!this.IsActiveAndEnabledAlsoInEditMode()
                || !this.IsActiveLayer()
                || !validator.CheckValidation())
                return;

            var view = Camera.current.worldToCameraMatrix * layer.LayerToWorld.Matrix;
            gl.CurrentColor = Color.white;
            gl.DrawQuad(view * Matrix4x4.TRS(
                layerInside.center, Quaternion.identity, layerInside.size));
            gl.CurrentColor *= 0.5f;
            gl.DrawQuad(view * Matrix4x4.TRS(
                layerOutside.center, Quaternion.identity, layerOutside.size));
        }
        void OnValidate() {
            validator.Invalidate();
        }
        void OnDisable() {
            gl.Dispose();
        }
        #endregion

        #region Message
        protected virtual void CrownLayer(Layer layer) {
            this.layer = layer;
            if (layer != null)
                enabled = true;
        }
        #endregion

        public SideEnum Side(Vector2 layerPoint) {
            var outsideContained = Contains(layerOutsideMin, layerOutsideMax, layerPoint);
            if (!outsideContained)
                return SideEnum.Outside;

            var insideContained = Contains(layerInsideMin, layerInsideMax, layerPoint);
            return insideContained ? SideEnum.Inside : SideEnum.Border;

        }
        public Vector2 ClosestPoint(Vector2 layerPoint, SideEnum side = SideEnum.Inside) {
            switch (side) {
                case SideEnum.Outside:
                    return layerOutside.ClosestPoint(layerPoint);
                default:
                    return layerInside.ClosestPoint(layerPoint);
            }
        }

        protected void Rebuild() {
            localToLayer.Reset(layer.LocalToLayer.Matrix,
                Matrix4x4.TRS(transform.localPosition, Quaternion.identity, transform.localScale));

            layerInsideMin = localToLayer.TransformPoint(localSize.min);
            layerInsideMax = localToLayer.TransformPoint(localSize.max);
            layerInside = Rect.MinMaxRect(layerInsideMin.x, layerInsideMin.y, 
                layerInsideMax.x, layerInsideMax.y);

            layerOutsideMin = layerInsideMin - borderThickness * Vector2.one;
            layerOutsideMax = layerInsideMax + borderThickness * Vector2.one;
            layerOutside = Rect.MinMaxRect(layerOutsideMin.x, layerOutsideMin.y,
                layerOutsideMax.x, layerOutsideMax.y);
        }

        public static bool Contains(Vector2 min, Vector2 max, Vector2 point) {
            var minx = min.x;
            var miny = min.y;
            var maxx = max.x;
            var maxy = max.y;
            var px = point.x;
            var py = point.y;

            return minx <= px && px < maxx && miny <= py && py < maxy;
        }
    }
}
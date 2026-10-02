using UnityEngine;
using System.Collections.Generic;

namespace FrioAndino.Environment
{
    /// <summary>
    /// Suelo jugable (muro de pirca + tierra/nieve + puente de madera).
    ///
    /// La superficie es fisicamente una sola linea recta a 0 grados exactos.
    /// Como se consigue eso sin atascos:
    ///
    ///   1. Rigidbody2D ESTATICO en el objeto de colisiones. Un suelo sin Rigidbody2D
    ///      es "estatico implicito" y Unity lo recalcula peor; declararlo estatico es
    ///      mas rapido y estable.
    ///   2. Un BoxCollider2D por tramo (tierra izquierda, puente, tierra derecha), todos
    ///      con EXACTAMENTE la misma Y superior.
    ///   3. CompositeCollider2D con geometryType = Outlines: funde los tramos en un solo
    ///      contorno continuo. Esto es lo que elimina las "costuras" entre colliders
    ///      vecinos, que es la causa clasica de que el personaje se trabe al caminar.
    ///   4. PhysicsMaterial2D con friction = 0 y bounciness = 0: sin friccion el
    ///      personaje no se pega a los bordes ni pierde velocidad de forma irregular.
    /// </summary>
    [DisallowMultipleComponent]
    public class GroundSurface : MonoBehaviour
    {
        /// <summary>Altura del mundo donde pisan los personajes.</summary>
        public float SurfaceY { get; private set; }
        public Transform ColliderRoot { get; private set; }
        public CompositeCollider2D Composite { get; private set; }

        private readonly List<BoxCollider2D> _tramos = new List<BoxCollider2D>();
        private PhysicsMaterial2D _material;

        /// <summary>
        /// Construye la fisica del suelo.
        /// </summary>
        /// <param name="topY">Y del mundo de la superficie transitable (la linea de pisada).</param>
        /// <param name="thickness">Grosor del bloque de pirca hacia abajo.</param>
        /// <param name="tramos">Cada Vector2 es (X izquierda, X derecha) de un tramo.</param>
        public void Build(float topY, float thickness, params Vector2[] tramos)
        {
            SurfaceY = topY;

            var root = new GameObject("Colisiones_Suelo");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.zero;
            ColliderRoot = root.transform;

            int suelo = LayerMask.NameToLayer("Suelo");
            if (suelo >= 0) root.layer = suelo;

            // 1 -- cuerpo estatico
            var rb = root.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
            rb.simulated = true;

            // 4 -- material sin friccion ni rebote
            _material = new PhysicsMaterial2D("Pirca_SinFriccion") { friction = 0f, bounciness = 0f };

            // 3 -- el compuesto va ANTES que las cajas, para que usedByComposite las enganche
            Composite = root.AddComponent<CompositeCollider2D>();
            Composite.geometryType = CompositeCollider2D.GeometryType.Outlines;
            Composite.generationType = CompositeCollider2D.GenerationType.Manual;
            Composite.sharedMaterial = _material;
            Composite.offsetDistance = 0f;
            Composite.edgeRadius = 0.01f;   // redondea microscopicamente: nada de aristas que enganchen

            // 2 -- un tramo por cada trozo de suelo, todos a la misma altura
            if (tramos == null || tramos.Length == 0)
                tramos = new[] { new Vector2(-50f, 50f) };

            foreach (var t in tramos) AddTramo(t.x, t.y, thickness);

            Composite.GenerateGeometry();
        }

        /// <summary>Anade un tramo mas de suelo a la misma altura (ej. una plataforma del puente).</summary>
        public BoxCollider2D AddTramo(float xIzquierda, float xDerecha, float thickness)
        {
            if (ColliderRoot == null) return null;

            var box = ColliderRoot.gameObject.AddComponent<BoxCollider2D>();
            float ancho = Mathf.Abs(xDerecha - xIzquierda);
            float centro = (xIzquierda + xDerecha) * 0.5f;

            // La cara SUPERIOR queda exactamente en SurfaceY para todos los tramos.
            box.size = new Vector2(ancho, thickness);
            box.offset = new Vector2(centro, SurfaceY - thickness * 0.5f);
            box.sharedMaterial = _material;
            Componer(box);                  // lo absorbe el CompositeCollider2D

            _tramos.Add(box);
            return box;
        }

        /// <summary>
        /// Marca un collider para que lo funda el CompositeCollider2D.
        /// En Unity 2023/6 la propiedad cambio de nombre a compositeOperation.
        /// </summary>
        private static void Componer(Collider2D col)
        {
#if UNITY_2023_1_OR_NEWER
            col.compositeOperation = Collider2D.CompositeOperation.Merge;
#else
            col.usedByComposite = true;
#endif
        }

        /// <summary>Regenera el contorno tras anadir o mover tramos.</summary>
        public void Rebuild()
        {
            if (Composite != null) Composite.GenerateGeometry();
        }

        /// <summary>Muros invisibles en los extremos para que nadie se salga del escenario.</summary>
        public void AddBordes(float xIzquierda, float xDerecha, float alto)
        {
            if (ColliderRoot == null) return;

            var izq = ColliderRoot.gameObject.AddComponent<BoxCollider2D>();
            izq.size = new Vector2(0.5f, alto);
            izq.offset = new Vector2(xIzquierda - 0.25f, SurfaceY + alto * 0.5f);
            izq.sharedMaterial = _material;
            Componer(izq);

            var der = ColliderRoot.gameObject.AddComponent<BoxCollider2D>();
            der.size = new Vector2(0.5f, alto);
            der.offset = new Vector2(xDerecha + 0.25f, SurfaceY + alto * 0.5f);
            der.sharedMaterial = _material;
            Componer(der);

            Rebuild();
        }
    }
}

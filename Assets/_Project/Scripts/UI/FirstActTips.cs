using UnityEngine;

namespace MoonShadow
{
    internal sealed class FirstActTips
    {
        private readonly struct Lesson
        {
            public readonly float Start;
            public readonly string Keyboard, Controller;

            public Lesson(float position, string keyboard, string controller)
            {
                Start = position;
                Keyboard = keyboard;
                Controller = controller;
            }
        }

        private static readonly Lesson[] Lessons =
        {
            new(0f, "Muévete con A / D.", "Muévete con el stick izquierdo."),
            new(2.6f, "Salta con Espacio.", "Salta con A."),
            new(5.2f, "Pulsa E frente a una puerta para entrar.", "Pulsa ↓ en la cruceta frente a una puerta."),
            new(8.0f, "Ataca con J o usa L para un golpe pesado.", "Ataca con X o usa Y para un golpe pesado."),
            new(11f, "Pulsa Shift para esquivar. Usa K para un parry.", "Pulsa RB para esquivar. Usa LB para un parry."),
            new(15f, "Apunta con el mouse. Mantén F para disparar. R recarga.", "Apunta con el stick derecho. Mantén RT. B recarga."),
            new(21f, "Abre el inventario con I. Usa un botiquín con H.", "Abre el inventario con View. Cura con ↑ en la cruceta.")
        };

        private int index;
        private float showing;
        private float pause;
        public string Text(RuinsDemo world)
        {
            if (showing <= 0f || index >= Lessons.Length)
                return null;
            var lesson = Lessons[index];
            if (world.Player.Input.UsingTouch)
                return index switch
                {
                    0 => "Desliza el control izquierdo para moverte.",
                    1 => "Toca SALTO para saltar.",
                    2 => "Toca USAR frente a una puerta para entrar.",
                    3 => "Toca ESPADA o FUERTE para encadenar ataques.",
                    4 => "DASH esquiva. PARRY devuelve los golpes.",
                    5 => "Arrastra APUNTAR para dirigir el disparo continuo. R recarga.",
                    _ => "BOLSA abre el inventario. CURA usa un botiquín."
                };
            return world.Player.Input.UsingGamepad ? lesson.Controller : lesson.Keyboard;
        }

        public void Tick(RuinsDemo world)
        {
            if (index >= Lessons.Length)
                return;
            if (showing > 0f)
            {
                showing -= Time.deltaTime;
                if (showing <= 0f)
                {
                    index++;
                    pause = 1.1f;
                }
                return;
            }
            pause = Mathf.Max(0f, pause - Time.deltaTime);
            if (pause <= 0f && world.Player.Position.x >= Lessons[index].Start)
                showing = 4.25f;
        }
    }
}

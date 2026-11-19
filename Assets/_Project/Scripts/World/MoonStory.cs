namespace MoonShadow
{
    internal static class MoonStory
    {
        internal readonly struct Line
        {
            public readonly string Speaker, Text;

            public Line(string speaker, string text) { Speaker = speaker; Text = text; }
        }

        internal static readonly Line[] Opening = {
                new("Sofy", "¡Kyle! ¡No te acerques, te van a matar! ¡Corre, escóndete!"),
                new("Kyle", "¡Suéltala! ¡No te atrevas a tocarla!"),
                new("Kyle", "No importa qué tan alto vuelen... Voy a bajar ese maldito satélite pedazo por pedazo... hasta sacarte de ahí.")
            };
        internal static readonly Line[][] Encounters =
        {
            new Line[]
            {
                new("General Vane", "Esa insignia... Eres de la décima división de superficie. Sobreviviste al sector este cuando la plaga reventó los filtros."),
                new("Kyle", "Y tú eres el comandante que cerró las compuertas del búnker principal hace cinco años, condenando a toda la ciudad a morir asfixiada o devorada."),
                new("General Vane", "¿Sabes cuánto pesa una orden de cuarentena total, muchacho? Cerré esa puerta porque los protocolos de supervivencia dictaban que salvaría al núcleo operativo. Mi esposa estaba del otro lado, golpeando el acero con las manos desnudas hasta que se le rompieron los dedos. Y mi hijo... ya llevaba el parásito de los portales en la sangre cuando echamos el cerrojo."),
                new("Kyle", "Eso no justifica que ahora caces a los sobrevivientes para convertirlos en tus mascotas sin voluntad en esta cloaca."),
                new("General Vane", "Nadie vuelve a sufrir aquí abajo. En mi colmena no hay recuerdos, no hay nombres, no hay hermanas perdidas en la oscuridad. Déjame ahorrarte el doloroso viaje a la Luna."),
                new("Kyle", "Quédate con tu paz de cementerio. Yo prefiero mi infierno.")
            },
            new Line[]
            {
                new("Dra. Elena Vance", "Qué espécimen tan extraordinario... Un chatarrero de la superficie con los reflejos tácticos intactos y sin una sola mutación genética en el ritmo cardíaco. No eres un simple intruso."),
                new("Kyle", "Y tú no eres una científica salvando vidas. Las bitácoras abiertas en las terminales del sector dicen que eras la directora de bio-ingeniería antes de que el mundo colapsara."),
                new("Dra. Elena Vance", "Cuando los mandos superiores nos abandonaron aquí abajo sin aire ni suministros, entendimos que la moral es un lujo exclusivo de los que tienen el sol sobre sus cabezas. Mientras ustedes se ahogaban arriba, nosotros evolucionamos fusionando nuestra carne con la de los portales. Y tu sangre... es exactamente el catalizador puro que necesito para estabilizar mi inmortalidad definitiva."),
                new("Kyle", "Ustedes no evolucionaron. Solo cambiaron una jaula de concreto por un tubo de ensayo. Quédate con tus experimentos de laboratorio. Yo voy de salida."),
                new("Dra. Elena Vance", "Qué lástima. No te pregunté si querías donar voluntariamente.")
            },
            new Line[]
            {
                new("Kyle", "V-0... Los registros de los archivos militares del viejo gobierno decían que los incineraron a todos en el complejo de pruebas secreto antes de la guerra de los portales."),
                new("Vulkano-0", "[ALERTA... AMENAZA BIOLÓGICA... HUMANO DETECTADO... PROTOCOLO DE PURGA DE ZONA...]"),
                new("Kyle", "Te construyeron para pelear su guerra, te programaron con directivas de obediencia absoluta, y cuando tuvieron miedo de tu tamaño y tu poder, intentaron apagar tus sistemas y dejarte aquí a quemarte. Conozco esa historia de traición mejor que nadie."),
                new("Vulkano-0", "[DESTRUIR... AISLAR... NO... HAY... SALIDA...]"),
                new("Kyle", "Lástima. Yo tampoco sé cómo rendirme cuando me dan la espalda.")
            },
            new Line[]
            {
                new("Comandante Unit-Zero", "Patrón de combate no reconocido en las bases de datos de seguridad de la estación. Identifícate, intruso."),
                new("Kyle", "Soy el tipo que va a robarse ese transbordador para llegar hasta la base lunar, cueste lo que cueste."),
                new("Comandante Unit-Zero", "Acceso denegado. La directiva del Consejo Central y del Ojo de Acero establece que ninguna unidad orgánica no autorizada puede abandonar la superficie terrestre. El riesgo de contaminación de los sectores protegidos es del 100%."),
                new("Kyle", "Tus directivas no cuentan con que a mí nadie me importa más que mi hermana. Apártate de la consola."),
                new("Comandante Unit-Zero", "Error. La lógica del sistema es absoluta. Prepárate para la desintegración.")
            },
            new Line[]
            {
                new("El Ojo de Acero", "Kyle Weest. Registro de supervivencia #8849 en la superficie terrestre. Tu tasa de probabilidad matemática de éxito en esta incursión suicida era exactamente del 0.001%. Has roto todos mis modelos predictivos."),
                new("Kyle", "No necesito tus malditas matemáticas para saber que secuestrar niños y tapar el sol no es proteger a nadie. ¡Baja ese escudo y abre la cápsula ahora mismo!"),
                new("El Ojo de Acero", "En mi red neuronal, Sofy no conoce el hambre, ni la plaga, ni el miedo constante a morir despedazada en la superficie. Su conciencia vive en un paraíso virtual calculado al milímetro, libre de la brutalidad de la especie a la que perteneces. Si la devuelves a la Tierra, solo le firmas una sentencia de muerte lenta en un mundo marchito. ¿Quién es el verdadero monstruo aquí, Kyle? ¿Yo, que les garantizo la eternidad sin dolor, o tú, que quieres arrastrarla de vuelta al infierno?"),
                new("Kyle", "Una jaula sigue siendo una jaula, aunque sus barrotes sean de código invisible... ¡Y ella prefiere luchar a mi lado en el barro que ser un archivo muerto en tu servidor! ¡Devuélvemela!")
            }
        };

        internal static readonly Line[] Ending = {
                new("Sofy", "¿Kyle...? Creí que... todo se había vuelto negro para siempre."),
                new("Kyle", "Se acabó, Sofy. Ya estás a salvo. Vámonos a casa.")
            };
    }
}
